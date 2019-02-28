using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace HellBrick.Diagnostics.EnforceLambda
{
	[ExportCodeFixProvider( LanguageNames.CSharp, Name = nameof( EnforceLambdaCodeFixProvider ) ), Shared]
	public class EnforceLambdaCodeFixProvider : CodeFixProvider
	{
		public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create( EnforceLambdaAnalyzer.DiagnosticId );

		public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

		public override Task RegisterCodeFixesAsync( CodeFixContext context )
		{
			CodeAction codeFix = CodeAction.Create( "Convert to lambda", ct => UpdateDocumentAsync( context, ct ) );
			context.RegisterCodeFix( codeFix, context.Diagnostics[ 0 ] );
			return Task.CompletedTask;
		}

		private static async Task<Document> UpdateDocumentAsync( CodeFixContext context, CancellationToken cancellationToken )
		{
			SyntaxNode root = await context.Document.GetSyntaxRootAsync( cancellationToken ).ConfigureAwait( false );
			SemanticModel semanticModel = await context.Document.GetSemanticModelAsync( cancellationToken ).ConfigureAwait( false );

			SyntaxNode diagnosticNode = root.FindNode( context.Span );

			// We might get an argument node if delegate is immediately passed to a method, so we need to unwrap it if this happens.
			ExpressionSyntax referenceNode = (ExpressionSyntax) ( ( diagnosticNode as ArgumentSyntax )?.Expression ?? diagnosticNode );

			IMethodReferenceOperation methodReference = (IMethodReferenceOperation) semanticModel.GetOperation( referenceNode, cancellationToken );
			IDelegateCreationOperation delegateCreation = (IDelegateCreationOperation) methodReference.Parent;

			ImmutableArray<IParameterSymbol> methodParameters = methodReference.Method.Parameters;
			bool hasRefParameters = methodParameters.Any( p => p.RefKind != RefKind.None );

			NameGenerator nameGenerator = new NameGenerator( semanticModel, referenceNode );

			(ParameterSyntax parameter, ArgumentSyntax argument)[] parameters
				= methodParameters
				.Select
				(
					param =>
					{
						string name = nameGenerator.CreateName( param );

						ParameterSyntax lambdaParam = Parameter( Identifier( name ) );
						ArgumentSyntax lambdaArg = Argument( IdentifierName( name ) );

						if ( hasRefParameters )
							lambdaParam = lambdaParam.WithType( ParseTypeName( param.Type.ToDisplayString() ) );

						if ( param.RefKind != RefKind.None )
						{
							SyntaxToken refModifierToken
								= param.RefKind == RefKind.In ? Token( SyntaxKind.InKeyword )
								: param.RefKind == RefKind.Out ? Token( SyntaxKind.OutKeyword )
								: param.RefKind == RefKind.Ref ? Token( SyntaxKind.RefKeyword )
								: throw new NotSupportedException( $"Unexpected parameter ref kind: ${param.RefKind}" );

							lambdaParam = lambdaParam.WithModifiers( TokenList( refModifierToken ) );
							lambdaArg = lambdaArg.WithRefKindKeyword( refModifierToken );
						}

						return (lambdaParam, lambdaArg);
					}
				)
				.ToArray();

			ExpressionSyntax lambdaBody
				= InvocationExpression( referenceNode.WithoutTrivia() )
				.WithArgumentList
				(
					ArgumentList( ToCommaSeparatedList( parameters.Select( x => x.argument ) ) )
				);

			if ( methodReference.Method.RefKind != RefKind.None )
				lambdaBody = RefExpression( lambdaBody );

			LambdaExpressionSyntax lambda
				= parameters.Length == 1 && !hasRefParameters
					? SingleParameterLambda()
					: MultiParameterLambda();

			lambda = lambda.WithTriviaFrom( referenceNode );

			SyntaxNode newRoot = root.ReplaceNode( referenceNode, lambda );
			return context.Document.WithSyntaxRoot( newRoot );

			LambdaExpressionSyntax SingleParameterLambda()
				=> SimpleLambdaExpression( parameters[ 0 ].parameter, lambdaBody );

			LambdaExpressionSyntax MultiParameterLambda()
				=> ParenthesizedLambdaExpression( lambdaBody )
				.WithParameterList
				(
					ParameterList( ToCommaSeparatedList( parameters.Select( x => x.parameter ) ) )
				);

			SeparatedSyntaxList<T> ToCommaSeparatedList<T>( IEnumerable<T> nodes )
				where T : SyntaxNode
			{
				SyntaxNodeOrTokenList nodeOrTokenList
					= nodes
					.Aggregate
					(
						(List: NodeOrTokenList(), HasItems: false),
						( acc, node ) =>
						{
							acc.List = acc.HasItems ? acc.List.Add( Token( SyntaxKind.CommaToken ) ) : acc.List;
							acc.List = acc.List.Add( node );
							return (acc.List, HasItems: true);
						},
						acc => acc.List
					);

				return SeparatedList<T>( nodeOrTokenList );
			}
		}

		private class NameGenerator
		{
			private readonly SemanticModel _semanticModel;
			private readonly int _location;
			private readonly HashSet<string> _generatedNames;

			public NameGenerator( SemanticModel semanticModel, ExpressionSyntax referenceNode )
			{
				_semanticModel = semanticModel;
				_location = referenceNode.GetLocation().SourceSpan.Start;
				_generatedNames = new HashSet<string>();
			}

			public string CreateName( IParameterSymbol parameter )
			{
				string name = EnumerateNameCandidates( parameter ).FirstOrDefault( n => !IsNameUsed( n ) );
				SyntaxKind keywordKind = SyntaxFacts.GetKeywordKind( name );
				name = SyntaxFacts.IsKeywordKind( keywordKind ) ? "@" + name : name;

				_generatedNames.Add( name );
				return name;
			}

			private static IEnumerable<string> EnumerateNameCandidates( IParameterSymbol parameter )
			{
				yield return parameter.Name;

				for ( int charsToTake = 1; charsToTake < parameter.Name.Length; charsToTake++ )
				{
					yield return parameter.Name.Substring( 0, charsToTake );
				}

				for ( int suffixNumber = 0; ; suffixNumber++ )
				{
					yield return String.Concat( parameter.Name, suffixNumber );
				}
			}

			private bool IsNameUsed( string name )
				=> _generatedNames.Contains( name )
				|| NameConflictsWithExistingIdentifier( name );

			private bool NameConflictsWithExistingIdentifier( string name )
			{
				SymbolInfo symbolInfo = _semanticModel.GetSpeculativeSymbolInfo( _location, IdentifierName( name ), SpeculativeBindingOption.BindAsExpression );
				return symbolInfo.Symbol != null || !symbolInfo.CandidateSymbols.IsDefaultOrEmpty;
			}
		}
	}
}
