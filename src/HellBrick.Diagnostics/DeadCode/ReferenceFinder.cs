using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace HellBrick.Diagnostics.DeadCode
{
	internal static class ReferenceFinder
	{
		public static IReadOnlyCollection<ISymbol> FindReferencedSymbols( SemanticModel semanticModel )
		{
			ReferencedSymbolFinder walker = new ReferencedSymbolFinder( semanticModel );
			walker.Visit( semanticModel.SyntaxTree.GetRoot() );
			return walker.ReferencedSymbols;
		}
	}
}
