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
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace HellBrick.Diagnostics
{
	[ExportCodeFixProvider(Common.RulePrefix + EnforceReadOnlyAnalyzer.DiagnosticID + Common.CodeFixSuffix, LanguageNames.CSharp), Shared]
	public class EnforceReadOnlyCodeFix: CodeFixProvider
	{
		private SyntaxToken _readonlyModifier = Token( SyntaxKind.ReadOnlyKeyword ).WithTrailingTrivia( SyntaxTrivia( SyntaxKind.WhitespaceTrivia, " " ) );

		public sealed override ImmutableArray<string> GetFixableDiagnosticIds() => ImmutableArray.Create( EnforceReadOnlyAnalyzer.DiagnosticID );
		public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

		public sealed override async Task ComputeFixesAsync( CodeFixContext context )
		{
			var root = await context.Document.GetSyntaxRootAsync( context.CancellationToken ).ConfigureAwait( false );

			var diagnostic = context.Diagnostics.First();
			var diagnosticSpan = diagnostic.Location.SourceSpan;
			var fieldDeclaration = root.FindToken( diagnosticSpan.Start ).Parent.AncestorsAndSelf().OfType<FieldDeclarationSyntax>().FirstOrDefault();
			var newDeclaration = fieldDeclaration.AddModifiers( _readonlyModifier );
			var newRoot = root.ReplaceNode( fieldDeclaration, newDeclaration );
			var changedDocument = context.Document.WithSyntaxRoot( newRoot );

			var codeFix = CodeAction.Create( "Make read-only", changedDocument );
			context.RegisterFix( codeFix, diagnostic );
		}
	}
}