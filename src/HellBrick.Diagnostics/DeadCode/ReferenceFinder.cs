using System;
using System.Collections.Immutable;
using HellBrick.Diagnostics.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace HellBrick.Diagnostics.DeadCode
{
	internal static class ReferenceFinder
	{
		[NoCapture]
		public static void FindReferencedSymbols( SemanticModel semanticModel, Action<ISymbol> onReferenceFound )
			=> FindReferencedSymbols( semanticModel, onReferenceFound, ( callback, symbol ) => callback( symbol ) );

		[NoCapture]
		public static void FindReferencedSymbols<TContext>( SemanticModel semanticModel, TContext context, Action<TContext, ISymbol> onReferenceFound )
		{
			Walker<TContext> walker = new Walker<TContext>( semanticModel, context, onReferenceFound );
			walker.Visit( semanticModel.SyntaxTree.GetRoot() );
		}

		private class Walker<TContext> : CSharpSyntaxWalker
		{
			private readonly SemanticModel _semanticModel;
			private readonly TContext _context;
			private readonly Action<TContext, ISymbol> _onReferenceFound;

			public Walker( SemanticModel semanticModel, TContext context, Action<TContext, ISymbol> onReferenceFound )
				: base( SyntaxWalkerDepth.Node )
			{
				_semanticModel = semanticModel;
				_context = context;
				_onReferenceFound = onReferenceFound;
			}

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
					_onReferenceFound( _context, definition );
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
