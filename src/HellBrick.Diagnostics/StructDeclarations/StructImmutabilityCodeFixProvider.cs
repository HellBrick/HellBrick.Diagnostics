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

namespace HellBrick.Diagnostics.StructDeclarations
{
	[ExportCodeFixProvider( LanguageNames.CSharp, Name = nameof( StructEquatabilityCodeFixProvider ) ), Shared]
	public sealed class StructImmutabilityCodeFixProvider : CodeFixProvider
	{
		private readonly SyntaxToken _readonlyModifier = SyntaxFactory.Token( SyntaxKind.ReadOnlyKeyword );

		public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create( StructImmutabilityAnalyzer.DiagnosticId );
		public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

		public override Task RegisterCodeFixesAsync( CodeFixContext context )
		{
			Diagnostic diagnostic = context.Diagnostics[ 0 ];
			CodeAction codeFix = CodeAction.Create( $"Make struct readonly", cancellationToken => CreateDocumentWithReadonlyModifierAddedToStructAsync( cancellationToken ) );
			context.RegisterCodeFix( codeFix, diagnostic );
			return Task.CompletedTask;

			async Task<Document> CreateDocumentWithReadonlyModifierAddedToStructAsync( CancellationToken cancellationToken )
			{
				SyntaxNode root = await context.Document.GetSyntaxRootAsync( cancellationToken ).ConfigureAwait( false );
				SyntaxNode structIdentifierNode = root.FindNode( context.Span );
				StructDeclarationSyntax structDeclaration = structIdentifierNode.AncestorsAndSelf().OfType<StructDeclarationSyntax>().First();

				StructDeclarationSyntax newStructDeclaration = structDeclaration.AddModifiers( _readonlyModifier );
				SyntaxNode newRoot = root.ReplaceNode( structDeclaration, newStructDeclaration );
				return context.Document.WithSyntaxRoot( newRoot );
			}
		}
	}
}
