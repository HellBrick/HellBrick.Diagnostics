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

		public bool CanConvert( TypeSyntax declarationType, SemanticModel semanticModel )
		{
			return
				declarationType.IsVar &&
				GetTypeSymbol( declarationType, semanticModel ).IsProperlyNamedType();
		}

		public string ConvertTypeName( TypeSyntax typeSyntax, SemanticModel semanticModel )
		{
			return GetTypeSymbol( typeSyntax, semanticModel ).ToMinimalDisplayString( semanticModel, typeSyntax.SpanStart );
		}

		private static ITypeSymbol GetTypeSymbol( TypeSyntax typeSyntax, SemanticModel semanticModel ) => semanticModel.GetSymbolInfo( typeSyntax ).Symbol as ITypeSymbol;
	}

	internal static class TypeSymbolExtensions
	{
		public static bool IsProperlyNamedType( this ITypeSymbol typeSymbol ) =>
			typeSymbol != null &&
			!typeSymbol.IsAnonymousType &&
			( typeSymbol as IArrayTypeSymbol )?.ElementType.IsProperlyNamedType() != false &&
			( typeSymbol as INamedTypeSymbol )?.TypeArguments.All( argument => IsProperlyNamedType( argument ) ) != false;
	}
}
