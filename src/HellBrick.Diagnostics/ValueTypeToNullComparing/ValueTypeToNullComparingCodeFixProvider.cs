using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HellBrick.Diagnostics.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HellBrick.Diagnostics.ValueTypeToNullComparing
{
	[ExportCodeFixProvider( LanguageNames.CSharp, Name = nameof( ValueTypeToNullComparingCodeFixProvider ) ), Shared]
	public class ValueTypeToNullComparingCodeFixProvider : CodeFixProvider
	{
		private const string _title = "Replace with `default` statement";

		public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create( ValueTypeToNullComparingAnalyzer.DiagnosticId );
		public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

		public sealed override Task RegisterCodeFixesAsync( CodeFixContext context )
		{
			Diagnostic diagnostic = context.Diagnostics.First();

			context.RegisterCodeFix
			(
				CodeAction.Create
				(
					title: _title,
					createChangedDocument: c => ReplaceNullWithDefalutSyntax( context.Document, diagnostic, c ),
					equivalenceKey: _title
				),
					diagnostic
			);
			return TaskHelper.CompletedTask;
		}

		private async Task<Document> ReplaceNullWithDefalutSyntax( Document document, Diagnostic diagnostic, CancellationToken token )
		{
			SyntaxNode documentRoot = await document
				.GetSyntaxRootAsync( token )
				.ConfigureAwait( false );

			BinaryExpressionSyntax equalStatement = documentRoot
				.FindNode( diagnostic.Location.SourceSpan ) as BinaryExpressionSyntax;

			SemanticModel model = await document
				.GetSemanticModelAsync()
				.ConfigureAwait( false );

			return document.WithSyntaxRoot
			(
				equalStatement.Right.IsKind( SyntaxKind.NullLiteralExpression )
					? ReplaceNodeWithDefaultSyntax( diagnostic, documentRoot, model, equalStatement.Right, equalStatement.Left )
					: ReplaceNodeWithDefaultSyntax( diagnostic, documentRoot, model, equalStatement.Left, equalStatement.Right )
			);
		}

		private static SyntaxNode ReplaceNodeWithDefaultSyntax( Diagnostic diagnostic, SyntaxNode documentRoot, SemanticModel model, ExpressionSyntax nodeToReplace, ExpressionSyntax typeNode )
			=> documentRoot.ReplaceNode( nodeToReplace, CreateDefaultSyntax( diagnostic.Location, model, typeNode, nodeToReplace ) );

		private static DefaultExpressionSyntax CreateDefaultSyntax( Location location, SemanticModel model, ExpressionSyntax node, ExpressionSyntax nodeToReplace )
		{
			string typeName = model.GetTypeInfo( node ).Type.ToMinimalDisplayString( model, location.SourceSpan.Start );
			return
				SyntaxFactory
				.DefaultExpression( SyntaxFactory.ParseTypeName( typeName ) )
				.WithLeadingTrivia( nodeToReplace.GetLeadingTrivia() )
				.WithTrailingTrivia( nodeToReplace.GetTrailingTrivia() );
		}
	}
}










