using System;
using System.Composition;
using System.Collections.Generic;
using System.Collections.Immutable;
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

namespace HellBrick.Diagnostics.AccessModifiers
{
	[ExportCodeFixProvider( LanguageNames.CSharp, Name = nameof( AccessModifierCodeFixProvider ) ), Shared]
	public class AccessModifierCodeFixProvider : CodeFixProvider
	{
		public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create( AccessModifierAnalyzer.DiagnosticID );
		public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

		public sealed override async Task RegisterCodeFixesAsync( CodeFixContext context )
		{
			SyntaxNode root = await context.Document.GetSyntaxRootAsync( context.CancellationToken ).ConfigureAwait( false );
			SyntaxNode node = root.FindNode( context.Span );
			bool isClassMember = node.Ancestors().Any( n => n.IsKind( SyntaxKind.ClassDeclaration ) );
			SyntaxToken missingKeyword = SyntaxFactory.Token( isClassMember ? SyntaxKind.PrivateKeyword : SyntaxKind.InternalKeyword );
			IDeclarationHandler handler = DeclarationHandlers.HandlerLookup[ node.Kind() ];
			SyntaxTokenList oldModifiers = handler.GetModifiers( node );
			SyntaxTokenList newModifiers = oldModifiers.Insert( 0, missingKeyword );
			SyntaxNode newNode = handler.WithModifiers( node, newModifiers );
			newNode = newNode.WithLeadingTrivia( node.GetLeadingTrivia() );
			SyntaxNode newRoot = root.ReplaceNode( node, newNode );
			Document newDocument = context.Document.WithSyntaxRoot( newRoot );

			CodeAction codeFix = CodeAction.Create( "Add missing access modifier", c => Task.FromResult( newDocument ), nameof( AccessModifierCodeFixProvider ) );
			context.RegisterCodeFix( codeFix, context.Diagnostics[ 0 ] );
		}
	}
}