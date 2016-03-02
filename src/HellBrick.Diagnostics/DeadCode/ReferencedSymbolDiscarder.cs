using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace HellBrick.Diagnostics.DeadCode
{
	internal class ReferencedSymbolDiscarder : CSharpSyntaxWalker
	{
		private readonly HashSet<ISymbol> _candidates;
		private readonly SemanticModel _semanticModel;

		public ReferencedSymbolDiscarder( SemanticModel semanticModel, HashSet<ISymbol> candidates )
			: base( SyntaxWalkerDepth.Node )
		{
			_semanticModel = semanticModel;
			_candidates = candidates;
		}

		public override void DefaultVisit( SyntaxNode node )
		{
			ISymbol symbol = _semanticModel.GetSymbolInfo( node ).Symbol;
			TryRemove( symbol );

			ITypeSymbol returnTypeSymbol = ( symbol as IPropertySymbol )?.Type ?? ( symbol as IMethodSymbol )?.ReturnType;
			TryRemove( returnTypeSymbol );

			ImmutableArray<ITypeSymbol> genericArgumentTypes = ( returnTypeSymbol as INamedTypeSymbol )?.TypeArguments ?? ImmutableArray<ITypeSymbol>.Empty;
			foreach ( ITypeSymbol genericArgumentType in genericArgumentTypes )
				TryRemove( genericArgumentType );

			base.DefaultVisit( node );
		}

		private void TryRemove( ISymbol symbol )
		{
			ISymbol definition = TryGetDefinition( symbol );
			if ( definition != null )
				_candidates.Remove( definition );
		}

		private ISymbol TryGetDefinition( ISymbol symbol )
		{
			symbol = symbol?.OriginalDefinition;

			IMethodSymbol methodSymbol = symbol as IMethodSymbol;
			if ( methodSymbol?.ReducedFrom != null )
				return methodSymbol.ReducedFrom;

			return symbol;
		}
	}
}
