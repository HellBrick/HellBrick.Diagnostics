using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace HellBrick.Diagnostics.AccessModifiers
{
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public class AccessModifierAnalyzer : DiagnosticAnalyzer
	{
		public const string DiagnosticID = IDPrefix.Value + "MissingAccessModifier";

		private const string _title = "Access modifier is missing";
		private const string _category = DiagnosticCategory.Style;

		private static readonly DiagnosticDescriptor _rule = new DiagnosticDescriptor( DiagnosticID, _title, _title, _category, DiagnosticSeverity.Warning, isEnabledByDefault: true );
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create( _rule );

		public override void Initialize( AnalysisContext context )
		{
			context.RegisterSyntaxNodeAction( FindMissingVisibilityModifiers, DeclarationHandlers.SupportedSyntaxKinds );
		}

		private void FindMissingVisibilityModifiers( SyntaxNodeAnalysisContext context )
		{
			if ( IsInterfaceMember( context.Node ) || IsExplicitInterfaceMember( context.Node ) )
				return;

			IDeclarationHandler handler = DeclarationHandlers.HandlerLookup[ context.Node.Kind() ];
			SyntaxTokenList modifiers = handler.GetModifiers( context.Node );
			if ( !modifiers.Any( m => IsVisibilityModifier( m ) ) )
				context.ReportDiagnostic( Diagnostic.Create( _rule, context.Node.ChildTokens().First().GetLocation() ) );
		}

		private static bool IsInterfaceMember( SyntaxNode node ) => node.Ancestors().Any( n => n.IsKind( SyntaxKind.InterfaceDeclaration ) );
		private static bool IsExplicitInterfaceMember( SyntaxNode node ) => node.DescendantNodes().Any( n => n.IsKind( SyntaxKind.ExplicitInterfaceSpecifier ) );

		private static bool IsVisibilityModifier( SyntaxToken token ) =>
			token.IsKind( SyntaxKind.PublicKeyword ) ||
			token.IsKind( SyntaxKind.InternalKeyword ) ||
			token.IsKind( SyntaxKind.PrivateKeyword ) ||
			token.IsKind( SyntaxKind.ProtectedKeyword );
	}
}