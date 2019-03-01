using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HellBrick.Diagnostics.Utils;
using HellBrick.Diagnostics.Utils.MultiChanges;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace HellBrick.Diagnostics.EnforceStatic
{
	[ExportCodeFixProvider( LanguageNames.CSharp, Name = nameof( MethodShouldBeStaticCodeFixProvider ) ), Shared]
	public class MethodShouldBeStaticCodeFixProvider : CodeFixProvider
	{
		private static readonly string[] _preferredModifierSeparator = new[] { "," };

		public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create( MethodShouldBeStaticAnalyzer.DiagnosticId );

		public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

		public override Task RegisterCodeFixesAsync( CodeFixContext context )
		{
			CodeAction codeFix = CodeAction.Create( "Make static", ct => UpdateSolutionAsync( context, ct ) );
			context.RegisterCodeFix( codeFix, context.Diagnostics[ 0 ] );
			return Task.CompletedTask;
		}

		private static async Task<Solution> UpdateSolutionAsync( CodeFixContext context, CancellationToken cancellationToken )
		{
			Solution solution = context.Document.Project.Solution;
			SyntaxNode root = await context.Document.GetSyntaxRootAsync( cancellationToken ).ConfigureAwait( false );
			DocumentOptionSet options = await context.Document.GetOptionsAsync( cancellationToken ).ConfigureAwait( false );

			MethodDeclarationSyntax oldDeclaration = (MethodDeclarationSyntax) root.FindNode( context.Span );

			IChange declarationChange = new DeclarationChange( oldDeclaration, options );
			IEnumerable<IChange> allChanges = new[] { declarationChange };

			return solution.ApplyChanges( allChanges, cancellationToken );
		}

		private class DeclarationChange : IChange
		{
			private readonly MethodDeclarationSyntax _methodDeclaration;
			private readonly DocumentOptionSet _options;

			public DeclarationChange( MethodDeclarationSyntax methodDeclaration, DocumentOptionSet options )
			{
				_methodDeclaration = methodDeclaration;
				_options = options;
			}

			public SyntaxNode ReplacedNode => _methodDeclaration;

			public SyntaxNode ComputeReplacementNode( SyntaxNode replacedNode )
			{
				MethodDeclarationSyntax oldDeclaration = (MethodDeclarationSyntax) replacedNode;
				SyntaxTokenList modifierList = oldDeclaration.Modifiers;

				CodeStyleOption<string> option = _options.GetOption( CSharpCodeStyleOptions.PreferredModifierOrder );

				string[] orderedModifiers
					= option
					.Value
					.Split( _preferredModifierSeparator, StringSplitOptions.RemoveEmptyEntries )
					.Select( modifier => modifier.Trim() )
					.ToArray();

				int staticOrder = Array.IndexOf( orderedModifiers, "static" );

				int indexOfLastModifierThatShouldPrecedeStatic
					= modifierList
					.Select( ( modifierToken, index ) => (modifierToken, index) )
					.Aggregate
					(
						-1,
						( previousIndex, pair )
							=> Array.IndexOf( orderedModifiers, pair.modifierToken.Text ) < staticOrder
								? pair.index
								: previousIndex
					);

				MethodDeclarationSyntax newDeclaration
					= oldDeclaration
					.WithModifiers
					(
						oldDeclaration.Modifiers.Insert( indexOfLastModifierThatShouldPrecedeStatic + 1, Token( SyntaxKind.StaticKeyword ) )
					);

				return newDeclaration;
			}
		}
	}
}
