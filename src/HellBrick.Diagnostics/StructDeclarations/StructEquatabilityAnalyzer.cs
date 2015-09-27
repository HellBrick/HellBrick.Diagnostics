using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HellBrick.Diagnostics.StructDeclarations
{
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public class StructEquatabilityAnalyzer : DiagnosticAnalyzer
	{
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = StructEquatabilityRules.Descriptors.Values.ToImmutableArray();
		public override void Initialize( AnalysisContext context ) => context.RegisterSyntaxNodeAction( ReportMissingEqualityMembers, SyntaxKind.StructDeclaration );

		private void ReportMissingEqualityMembers( SyntaxNodeAnalysisContext context )
		{
			StructDeclarationSyntax structDeclaration = context.Node as StructDeclarationSyntax;
			ITypeSymbol structType = context.SemanticModel.GetDeclaredSymbol( structDeclaration );
			Location location = structDeclaration.Identifier.GetLocation();

			foreach ( IEquatabilityRule rule in StructEquatabilityRules.Rules.Values )
			{
				if ( rule.IsViolatedBy( structDeclaration, structType, context.SemanticModel ) )
				{
					Diagnostic diagnostic = Diagnostic.Create( StructEquatabilityRules.Descriptors[ rule.ID ], location, structType.Name );
					context.ReportDiagnostic( diagnostic );
				}
			}
		}
	}
}