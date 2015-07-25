using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace HellBrick.Diagnostics.Utils
{
	public static partial class SyntaxNodeExtensions
	{
		public static IEnumerable<T> EnumerateSelectedNodes<T>( this SyntaxNode root, TextSpan selection ) where T : SyntaxNode
		{
			IEnumerable<T> selectedNodes;

			if ( selection.Length > 0 )
				selectedNodes = root.DescendantNodes( selection ).OfType<T>();
			else
			{
				var node = root.FindNode( selection ).FirstAncestorOrSelf<T>();
				selectedNodes = node != null ? Enumerable.Repeat( node, 1 ) : Enumerable.Empty<T>();
			}

			return selectedNodes;
		}
	}
}
