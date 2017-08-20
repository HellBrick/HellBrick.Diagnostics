using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HellBrick.Diagnostics.StructDeclarations
{
	internal interface IEquatabilityRule
	{
		string ID { get; }
		string RuleText { get; }
		bool IsViolatedBy( StructDeclarationSyntax structDeclaration, ITypeSymbol structType, SemanticModel semanticModel );
		StructDeclarationSyntax Enforce( StructDeclarationSyntax structDeclaration, INamedTypeSymbol structType, SemanticModel semanticModel, ISymbol[] fieldsAndProperties );
	}
}
