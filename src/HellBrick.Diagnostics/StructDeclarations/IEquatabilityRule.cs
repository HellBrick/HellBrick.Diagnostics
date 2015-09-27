using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HellBrick.Diagnostics.StructDeclarations
{
	internal interface IEquatabilityRule
	{
		string ID { get; }
		string RuleText { get; }
		bool IsViolatedBy( StructDeclarationSyntax structDeclaration, ITypeSymbol structType, SemanticModel semanticModel );
		StructDeclarationSyntax Enforce( StructDeclarationSyntax structDeclaration, SemanticModel semanticModel, ISymbol[] fieldsAndProperties );
	}
}
