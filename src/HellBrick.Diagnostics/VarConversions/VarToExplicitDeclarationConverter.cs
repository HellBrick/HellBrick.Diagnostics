using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HellBrick.Diagnostics.VarConversions
{
	internal class VarToExplicitDeclarationConverter : IDeclarationConverter
	{
		public string Title => "Convert 'var' to explicit declaration";

		public bool CanConvert( VariableDeclarationSyntax declaration, SemanticModel semanticModel )
		{
			return
				declaration.Type.IsVar &&
				GetNamedSymbol( declaration.Type, semanticModel ) != null;
		}

		public string ConvertTypeName( TypeSyntax typeSyntax, SemanticModel semanticModel ) => GetNamedSymbol( typeSyntax, semanticModel ).Name;

		private static INamedTypeSymbol GetNamedSymbol( TypeSyntax typeSyntax, SemanticModel semanticModel ) => semanticModel.GetSymbolInfo( typeSyntax ).Symbol as INamedTypeSymbol;
	}
}
