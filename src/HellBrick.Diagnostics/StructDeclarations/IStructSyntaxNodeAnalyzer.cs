using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
