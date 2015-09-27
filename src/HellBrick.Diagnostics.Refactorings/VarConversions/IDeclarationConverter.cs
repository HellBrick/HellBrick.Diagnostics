using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HellBrick.Diagnostics.VarConversions
{
	internal interface IDeclarationConverter
	{
		string Title { get; }

		bool CanConvert( TypeSyntax declarationType, SemanticModel semanticModel );
		string ConvertTypeName( TypeSyntax typeSyntax, SemanticModel semanticModel );
	}
}
