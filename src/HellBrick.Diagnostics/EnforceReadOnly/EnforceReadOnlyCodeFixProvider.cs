using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HellBrick.Diagnostics.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace HellBrick.Diagnostics.EnforceReadOnly
{
	[ExportCodeFixProvider( LanguageNames.CSharp, Name = nameof( EnforceReadOnlyCodeFixProvider ) ), Shared]
	public class EnforceReadOnlyCodeFixProvider : CodeFixProvider
	{
		private readonly SyntaxToken _readonlyModifier = Token( SyntaxKind.ReadOnlyKeyword );

		public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create( EnforceReadOnlyAnalyzer.DiagnosticID );

		public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

		public sealed override Task RegisterCodeFixesAsync( CodeFixContext context )
		{
			if ( context.CancellationToken.IsCancellationRequested )
				return TaskHelper.CanceledTask;

			Diagnostic diagnostic = context.Diagnostics.First();
			CodeAction codeFix = CodeAction.Create( "Make read-only", cancellationToken => MakeReadOnlyAsync( context, diagnostic, cancellationToken ), nameof( EnforceReadOnlyCodeFixProvider ) );
			context.RegisterCodeFix( codeFix, diagnostic );

			return Task.CompletedTask;
		}

		private async Task<Document> MakeReadOnlyAsync( CodeFixContext context, Diagnostic diagnostic, CancellationToken cancellationToken )
		{
			SyntaxNode root = await context.Document.GetSyntaxRootAsync( cancellationToken ).ConfigureAwait( false );

			TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;
			FieldDeclarationSyntax fieldDeclaration = root.FindToken( diagnosticSpan.Start ).Parent.AncestorsAndSelf().OfType<FieldDeclarationSyntax>().FirstOrDefault();
			FieldDeclarationSyntax newDeclaration = WithReadOnlyAdded( fieldDeclaration );
			SyntaxNode newRoot = root.ReplaceNode( fieldDeclaration, newDeclaration );

			return context.Document.WithSyntaxRoot( newRoot );
		}

		private FieldDeclarationSyntax WithReadOnlyAdded( FieldDeclarationSyntax fieldDeclaration )
		{
			SyntaxTriviaList leadingTrivia = fieldDeclaration.GetLeadingTrivia();
			fieldDeclaration = fieldDeclaration.WithLeadingTrivia();
			fieldDeclaration = fieldDeclaration.AddModifiers( _readonlyModifier );
			fieldDeclaration = fieldDeclaration.WithLeadingTrivia( leadingTrivia );
			return fieldDeclaration;
		}
	}
}
