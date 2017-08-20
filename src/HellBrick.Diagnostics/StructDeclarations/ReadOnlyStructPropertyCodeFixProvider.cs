using System.Composition;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HellBrick.Diagnostics.StructDeclarations
{
	[ExportCodeFixProvider( LanguageNames.CSharp, Name = nameof( ReadOnlyStructPropertyCodeFixProvider ) ), Shared]
	public class ReadOnlyStructPropertyCodeFixProvider : CodeFixProvider
	{
		public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create( ReadOnlyStructPropertyAnalyzer.DiagnosticID );
		public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

		public sealed override async Task RegisterCodeFixesAsync( CodeFixContext context )
		{
			SyntaxNode root = await context.Document.GetSyntaxRootAsync( context.CancellationToken ).ConfigureAwait( false );
			AccessorDeclarationSyntax setter = root.FindNode( context.Span ) as AccessorDeclarationSyntax;
			PropertyDeclarationSyntax property = setter.FirstAncestorOrSelf<PropertyDeclarationSyntax>();

			SyntaxNode newRoot = root.RemoveNode( setter, SyntaxRemoveOptions.KeepNoTrivia );
			Document newDocument = context.Document.WithSyntaxRoot( newRoot );
			CodeAction codeFix = CodeAction.Create( "Remove the setter", c => Task.FromResult( newDocument ), nameof( ReadOnlyStructPropertyCodeFixProvider ) );
			context.RegisterCodeFix( codeFix, context.Diagnostics[ 0 ] );
		}
	}
}