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
		public static void FindReferences( SemanticModel semanticModel, Action<ISymbol, SyntaxNode> onReferenceFound )
			=> FindReferences( semanticModel, onReferenceFound, ( callback, symbol, referenceNode ) => callback( symbol, referenceNode ) );

		[NoCapture]
		public static void FindReferences<TContext>( SemanticModel semanticModel, TContext context, Action<TContext, ISymbol, SyntaxNode> onReferenceFound )
		{
			Walker<TContext> walker = new Walker<TContext>( semanticModel, context, onReferenceFound );
			walker.Visit( semanticModel.SyntaxTree.GetRoot() );
		}

		private class Walker<TContext> : CSharpSyntaxWalker
		{
			private readonly SemanticModel _semanticModel;
			private readonly TContext _context;
			private readonly Action<TContext, ISymbol, SyntaxNode> _onReferenceFound;

			public Walker( SemanticModel semanticModel, TContext context, Action<TContext, ISymbol, SyntaxNode> onReferenceFound )
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
				TryAdd( symbol, node );

				ITypeSymbol returnTypeSymbol = ( symbol as IPropertySymbol )?.Type ?? ( symbol as IMethodSymbol )?.ReturnType;
				TryAdd( returnTypeSymbol, node );

				ImmutableArray<ITypeSymbol> genericArgumentTypes = ( returnTypeSymbol as INamedTypeSymbol )?.TypeArguments ?? ImmutableArray<ITypeSymbol>.Empty;
				foreach ( ITypeSymbol genericArgumentType in genericArgumentTypes )
					TryAdd( genericArgumentType, node );

				base.DefaultVisit( node );
			}

			private void TryAdd( ISymbol symbol, SyntaxNode referenceNode )
			{
				ISymbol definition = TryGetDefinition( symbol );
				if ( definition != null )
					_onReferenceFound( _context, definition, referenceNode );
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
