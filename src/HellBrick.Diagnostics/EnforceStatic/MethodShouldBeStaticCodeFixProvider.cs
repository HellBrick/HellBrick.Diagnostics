using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HellBrick.Diagnostics.Utils;
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
			CodeAction codeFix = CodeAction.Create( "Make static", ct => UpdateDocumentAsync( context, ct ) );
			context.RegisterCodeFix( codeFix, context.Diagnostics[ 0 ] );
			return Task.CompletedTask;
		}

		private static async Task<Document> UpdateDocumentAsync( CodeFixContext context, CancellationToken cancellationToken )
		{
			SyntaxNode root = await context.Document.GetSyntaxRootAsync( cancellationToken ).ConfigureAwait( false );
			DocumentOptionSet options = await context.Document.GetOptionsAsync( cancellationToken ).ConfigureAwait( false );

			MethodDeclarationSyntax oldDeclaration = (MethodDeclarationSyntax) root.FindNode( context.Span );
			SyntaxTokenList modifierList = oldDeclaration.Modifiers;

			CodeStyleOption<string> option = options.GetOption( CSharpCodeStyleOptions.PreferredModifierOrder );

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

			SyntaxNode newRoot = root.ReplaceNode( oldDeclaration, newDeclaration );
			Document newDocument = context.Document.WithSyntaxRoot( newRoot );
			return newDocument;
		}
	}
}
