using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace HellBrick.Diagnostics.AccessModifiers
{
	internal interface IDeclarationHandler
	{
		SyntaxKind Kind { get; }
		SyntaxTokenList GetModifiers( SyntaxNode node );
		SyntaxNode WithModifiers( SyntaxNode node, SyntaxTokenList newModifiers );
	}

	internal abstract class DeclarationHandler<T> : IDeclarationHandler
		where T : SyntaxNode
	{
		public abstract SyntaxKind Kind { get; }
		protected abstract SyntaxTokenList GetModifiers( T node );
		protected abstract SyntaxNode WithModifiers( T node, SyntaxTokenList newModifiers );

		public SyntaxTokenList GetModifiers( SyntaxNode node ) => GetModifiers( node as T );
		public SyntaxNode WithModifiers( SyntaxNode node, SyntaxTokenList newModifiers ) => WithModifiers( node as T, newModifiers );
	}
}
