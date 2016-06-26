using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using HellBrick.Diagnostics.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HellBrick.Diagnostics.StructDeclarations
{
	public class StructEquatabilityAnalyzer : IStructSyntaxNodeAnalyzer
	{
		public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = StructEquatabilityRules.Descriptors.Values.ToImmutableArray();

		public void AnalyzeStructSyntaxNode( StructDeclarationSyntax structDeclaration, ITypeSymbol structType, SyntaxNodeAnalysisContext context )
		{
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