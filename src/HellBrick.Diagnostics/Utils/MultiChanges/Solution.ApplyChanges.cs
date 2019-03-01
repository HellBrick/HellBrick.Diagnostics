using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace HellBrick.Diagnostics.Utils.MultiChanges
{
	internal static partial class SolutionExtensions
	{
		public static Solution ApplyChanges( this Solution solution, IEnumerable<IChange> changes, CancellationToken cancellationToken )
			=> changes
			.GroupBy( change => change.ReplacedNode.SyntaxTree )
			.Aggregate
			(
				solution,
				( oldSolution, syntaxTreeChangeGroup ) =>
				{
					SyntaxTree syntaxTree = syntaxTreeChangeGroup.Key;
					Dictionary<SyntaxNode, IChange> changeLookup = syntaxTreeChangeGroup.ToDictionary( change => change.ReplacedNode );
					SyntaxNode oldRoot = syntaxTree.GetRoot( cancellationToken );
					SyntaxNode newRoot
						= oldRoot
						.ReplaceNodes
						(
							changeLookup.Keys,
							( originalNode, rewrittenNode ) => changeLookup[ originalNode ].ComputeReplacementNode( rewrittenNode )
						);

					DocumentId documentID = oldSolution.GetDocumentId( syntaxTree );
					return oldSolution.WithDocumentSyntaxRoot( documentID, newRoot );
				}
			);
	}
}
