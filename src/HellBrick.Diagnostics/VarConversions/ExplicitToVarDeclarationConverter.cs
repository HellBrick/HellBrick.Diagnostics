using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HellBrick.Diagnostics.VarConversions
{
	internal class ExplicitToVarDeclarationConverter : IDeclarationConverter
	{
		public string Title => "Convert explicit declaration to 'var'";
		public bool CanConvert( TypeSyntax declarationType, SemanticModel semanticModel ) => !declarationType.IsVar;
		public string ConvertTypeName( TypeSyntax typeSyntax, SemanticModel semanticModel ) => "var";
	}
}
