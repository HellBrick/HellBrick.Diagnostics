using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using HellBrick.Diagnostics.Utils;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using HellBrick.Diagnostics.StructDeclarations;

namespace HellBrick.Diagnostics.EnforceReadOnly
{
	[ExportCodeFixProvider( LanguageNames.CSharp, Name = nameof( EnforceReadOnlyCodeFixProvider ) ), Shared]
	public class EnforceReadOnlyCodeFixProvider : CodeFixProvider
	{
		private readonly SyntaxToken _readonlyModifier = Token( SyntaxKind.ReadOnlyKeyword );

		public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } =
			ImmutableArray.Create
			(
				EnforceReadOnlyAnalyzer.DiagnosticID,
				ReadOnlyStructFieldsAnalyzer.DiagnosticID
			);

		public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

		public sealed override Task RegisterCodeFixesAsync( CodeFixContext context )
		{
			if ( context.CancellationToken.IsCancellationRequested )
				return TaskHelper.CanceledTask;

			var diagnostic = context.Diagnostics.First();
			var codeFix = CodeAction.Create( "Make read-only", cancellationToken => MakeReadOnly( context, diagnostic, cancellationToken ) );
			context.RegisterCodeFix( codeFix, diagnostic );

			return TaskHelper.CompletedTask;
		}

		private async Task<Document> MakeReadOnly( CodeFixContext context, Diagnostic diagnostic, CancellationToken cancellationToken )
		{
			var root = await context.Document.GetSyntaxRootAsync( cancellationToken ).ConfigureAwait( false );

			var diagnosticSpan = diagnostic.Location.SourceSpan;
			var fieldDeclaration = root.FindToken( diagnosticSpan.Start ).Parent.AncestorsAndSelf().OfType<FieldDeclarationSyntax>().FirstOrDefault();
			var newDeclaration = WithReadOnlyAdded( fieldDeclaration );
			var newRoot = root.ReplaceNode( fieldDeclaration, newDeclaration );

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