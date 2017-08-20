using System.Composition;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using HellBrick.Diagnostics.Utils;

namespace HellBrick.Diagnostics.AccessModifiers
{
	[ExportCodeFixProvider( LanguageNames.CSharp, Name = nameof( AccessModifierCodeFixProvider ) ), Shared]
	public class AccessModifierCodeFixProvider : CodeFixProvider
	{
		public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create( AccessModifierAnalyzer.DiagnosticID );
		public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

		public sealed override Task RegisterCodeFixesAsync( CodeFixContext context )
		{
			CodeAction codeFix = CodeAction.Create( "Add missing access modifier", c => AddAccessModifierAsync( context, c ), nameof( AccessModifierCodeFixProvider ) );
			context.RegisterCodeFix( codeFix, context.Diagnostics[ 0 ] );
			return Task.CompletedTask;
		}

		private async Task<Document> AddAccessModifierAsync( CodeFixContext context, CancellationToken cancellationToken )
		{
			SyntaxNode root = await context.Document.GetSyntaxRootAsync( cancellationToken ).ConfigureAwait( false );
			SyntaxNode node = root.FindNode( context.Span );
			SyntaxNode newNode = WithMissingModifierAdded( node );
			SyntaxNode newRoot = root.ReplaceNode( node, newNode );
			Document newDocument = context.Document.WithSyntaxRoot( newRoot );
			return newDocument;
		}

		private static SyntaxNode WithMissingModifierAdded( SyntaxNode node )
		{
			bool isClassMember = node.Ancestors().Any( n => n.IsKind( SyntaxKind.ClassDeclaration ) );
			SyntaxToken missingKeyword = SyntaxFactory.Token( isClassMember ? SyntaxKind.PrivateKeyword : SyntaxKind.InternalKeyword );
			IDeclarationHandler handler = DeclarationHandlers.HandlerLookup[ node.Kind() ];
			SyntaxTokenList oldModifiers = handler.GetModifiers( node );

			//	We need to remove the leading trivia before attaching new modifier, otherwise the modifier will be added before the trivia.
			SyntaxTriviaList leadingTrivia = node.GetLeadingTrivia();
			node = node.WithLeadingTrivia();

			//	Now add the modifier
			SyntaxTokenList newModifiers = oldModifiers.Insert( 0, missingKeyword );
			node = handler.WithModifiers( node, newModifiers );

			//	And reattach the trivia
			node = node.WithLeadingTrivia( leadingTrivia );
			return node;
		}
	}
}
