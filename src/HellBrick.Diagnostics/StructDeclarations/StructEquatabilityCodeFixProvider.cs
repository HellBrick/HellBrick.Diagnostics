using System.Composition;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using HellBrick.Diagnostics.Utils;
using Microsoft.CodeAnalysis.Options;

namespace HellBrick.Diagnostics.StructDeclarations
{
	[ExportCodeFixProvider( LanguageNames.CSharp, Name = nameof( StructEquatabilityCodeFixProvider ) ), Shared]
	public class StructEquatabilityCodeFixProvider : CodeFixProvider
	{
		public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create( StructEquatabilityRules.DiagnosticId );
		public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

		public sealed override Task RegisterCodeFixesAsync( CodeFixContext context )
		{
			CodeAction codeFix = CodeAction.Create( "Generate missing equatability members", c => EnforceRulesAsync( context, c ), nameof( StructEquatabilityCodeFixProvider ) );
			context.RegisterCodeFix( codeFix, context.Diagnostics );
			return Task.CompletedTask;
		}

		private async Task<Document> EnforceRulesAsync( CodeFixContext context, CancellationToken cancellationToken )
		{
			SyntaxNode root = await context.Document.GetSyntaxRootAsync( cancellationToken ).ConfigureAwait( false );
			SemanticModel semanticModel = await context.Document.GetSemanticModelAsync( cancellationToken ).ConfigureAwait( false );
			DocumentOptionSet options = await context.Document.GetOptionsAsync( cancellationToken ).ConfigureAwait( false );
			StructDeclarationSyntax oldStructDeclaration = root.FindNode( context.Span ).FirstAncestorOrSelf<StructDeclarationSyntax>();
			StructDeclarationSyntax newStructDeclaration = oldStructDeclaration;

			INamedTypeSymbol structType = semanticModel.GetDeclaredSymbol( oldStructDeclaration );
			TypeSyntax structTypeName = SyntaxFactory.ParseTypeName( structType.ToMinimalDisplayString( semanticModel, oldStructDeclaration.GetLocation().SourceSpan.End + 1 ) );

			ISymbol[] fieldSymbols = newStructDeclaration
				.EnumerateDataFields()
				.SelectMany( f => f.Declaration.Variables )
				.Select( f => semanticModel.GetDeclaredSymbol( f ) )
				.ToArray();

			ISymbol[] propertySymbols = newStructDeclaration
				.EnumerateDataProperties()
				.Select( p => semanticModel.GetDeclaredSymbol( p ) )
				.ToArray();

			ISymbol[] fieldsAndProperties = Enumerable.Concat( fieldSymbols, propertySymbols ).ToArray();

			IEquatabilityRule[] brokenRules = context.Diagnostics
				.Select( diagnostic => StructEquatabilityRules.Rules[ diagnostic.Properties[ StructEquatabilityRules.RuleIdPropertyKey ] ] )
				.OrderBy( rule => rule, StructEquatabilityRules.RuleComparer )
				.ToArray();

			foreach ( IEquatabilityRule rule in brokenRules )
			{
				if ( cancellationToken.IsCancellationRequested )
					break;

				newStructDeclaration = rule.Enforce( newStructDeclaration, structType, structTypeName, semanticModel, fieldsAndProperties, options );
			}

			SyntaxNode newRoot = root.ReplaceNode( oldStructDeclaration, newStructDeclaration );
			return context.Document.WithSyntaxRoot( newRoot );
		}
	}
}
