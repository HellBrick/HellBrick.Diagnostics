using Microsoft.CodeAnalysis;

namespace HellBrick.Diagnostics.Utils.MultiChanges
{
	internal interface IChange
	{
		SyntaxNode ReplacedNode { get; }
		SyntaxNode ComputeReplacementNode( SyntaxNode replacedNode );
	}
}
