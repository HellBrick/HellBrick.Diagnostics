using System.Collections.Generic;
using System.Collections.Immutable;
using HellBrick.Diagnostics.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace HellBrick.Diagnostics.DeadCode
{
	internal static class ReferenceFinder
	{
		public static IReadOnlyCollection<ISymbol> FindReferencedSymbols( SemanticModel semanticModel )
		{
			Walker walker = new Walker( semanticModel );
			walker.Visit( semanticModel.SyntaxTree.GetRoot() );
			return walker.ReferencedSymbols;
		}

		private class Walker : CSharpSyntaxWalker
		{
			private readonly HashSet<ISymbol> _referencedSymbols = new HashSet<ISymbol>();
			private readonly SemanticModel _semanticModel;

			public Walker( SemanticModel semanticModel )
				: base( SyntaxWalkerDepth.Node )
				=> _semanticModel = semanticModel;

			public IReadOnlyCollection<ISymbol> ReferencedSymbols => _referencedSymbols;

			public override void DefaultVisit( SyntaxNode node )
			{
				SymbolInfo symbolInfo = _semanticModel.GetSymbolInfo( node );
				ISymbol symbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.OnlyOrDefault();
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
}
