using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HellBrick.Diagnostics.StructDeclarations
{
	internal interface IStructSyntaxNodeAnalyzer
	{
		ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
		void AnalyzeStructSyntaxNode( StructDeclarationSyntax structDeclaration, ITypeSymbol structType, SyntaxNodeAnalysisContext context );
	}
}
