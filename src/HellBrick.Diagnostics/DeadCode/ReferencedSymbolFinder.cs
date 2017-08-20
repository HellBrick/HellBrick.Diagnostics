using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace HellBrick.Diagnostics.DeadCode
{
	internal class ReferencedSymbolFinder : CSharpSyntaxWalker
	{
		private readonly HashSet<ISymbol> _referencedSymbols = new HashSet<ISymbol>();
		private readonly SemanticModel _semanticModel;

		public ReferencedSymbolFinder( SemanticModel semanticModel )
			: base( SyntaxWalkerDepth.Node )
			=> _semanticModel = semanticModel;

		public IEnumerable<ISymbol> ReferencedSymbols => _referencedSymbols;

		public override void DefaultVisit( SyntaxNode node )
		{
			ISymbol symbol = _semanticModel.GetSymbolInfo( node ).Symbol;
			TryAdd( symbol );

			ITypeSymbol returnTypeSymbol = ( symbol as IPropertySymbol )?.Type ?? ( symbol as IMethodSymbol )?.ReturnType;
			TryAdd( returnTypeSymbol );

			ImmutableArray<ITypeSymbol> genericArgumentTypes = ( returnTypeSymbol as INamedTypeSymbol )?.TypeArguments ?? ImmutableArray<ITypeSymbol>.Empty;
			foreach ( ITypeSymbol genericArgumentType in genericArgumentTypes )
				TryAdd( genericArgumentType );

			base.DefaultVisit( node );
		}

		private void TryAdd( ISymbol symbol )
		{
			ISymbol definition = TryGetDefinition( symbol );
			if ( definition != null )
				_referencedSymbols.Add( definition );
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
