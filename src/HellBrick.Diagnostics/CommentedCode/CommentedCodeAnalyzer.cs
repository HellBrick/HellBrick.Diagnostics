using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace HellBrick.Diagnostics.CommentedCode
{
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public class CommentedCodeAnalyzer : DiagnosticAnalyzer
	{
		public const string DiagnosticId = IDPrefix.Value + "CommentedCode";

		private const int _commentMarkerLength = 2;

		private static readonly DiagnosticDescriptor _rule = new DiagnosticDescriptor
		(
			DiagnosticId,
			"Code is commented out",
			"Remove commented out code",
			DiagnosticCategory.Design,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			customTags: WellKnownDiagnosticTags.Unnecessary
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create( _rule );

		public override void Initialize( AnalysisContext context )
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.None );

			context.RegisterSyntaxTreeAction( c => AnalyzeTree( c ) );
		}

		private void AnalyzeTree( SyntaxTreeAnalysisContext context )
		{
			SyntaxNode root = context.Tree.GetRoot( context.CancellationToken );

			new CommentBlockDiscoverer( cb => ReportIfContainsCode( cb ), context.CancellationToken ).Visit( root );

			void ReportIfContainsCode( CommentBlock commentBlock )
			{
				SyntaxNode originalEnclosingNode = root.FindNode( commentBlock.Span );

				// If the enclosing node contains errors, it's going to be difficult to determine if uncommenting the block causes new errors.
				// Also, since the code doesn't compile anyway, a false negative if fine here.
				// So we bail and don't analyze it any further.
				if ( originalEnclosingNode.GetDiagnostics().Any( d => d.Severity == DiagnosticSeverity.Error ) )
					return;

				CommentBlock? candidate = commentBlock;

				while ( candidate is CommentBlock currentCandidate && !context.CancellationToken.IsCancellationRequested )
				{
					Location uncommentedErrorLocation = UncommentAndGetFirstErrorLocation( in currentCandidate, originalEnclosingNode.FullSpan.Length );

					if ( uncommentedErrorLocation == null )
					{
						// If commented out code is preceded by a real comment, the comment typically describes the code and has no value of its own.
						// So we report the whole original block to be removed, not just the code part.
						ReportBlock( in commentBlock );
						break;
					}
					else
					{
						candidate = currentCandidate.TryTrimFirstCommentAtOrAfter( uncommentedErrorLocation.GetLineSpan().StartLinePosition.Line );
					}
				}
			}

			Location UncommentAndGetFirstErrorLocation( in CommentBlock commentBlock, int originalEnclosingBlockLength )
			{
				SourceText originalSourceText = context.Tree.GetText( context.CancellationToken );

				SourceText sourceTextWithBlockUncommented = commentBlock.GetSourceTextWithBlockUncommented( originalSourceText );
				int sourceLengthDiff = originalSourceText.Length - sourceTextWithBlockUncommented.Length;

				SyntaxTree treeWithBlockUncommented = SyntaxFactory.ParseSyntaxTree
				(
					sourceTextWithBlockUncommented,
					options: context.Tree.Options,
					cancellationToken: context.CancellationToken
				);

				SyntaxNode rootWithBlockUncommented = treeWithBlockUncommented.GetRoot( context.CancellationToken );
				TextSpan uncommentedBlockSpan = TextSpan.FromBounds( commentBlock.Span.Start, commentBlock.Span.End - sourceLengthDiff );
				TextSpan enclosingSpan = TextSpan.FromBounds
				(
					Math.Max( uncommentedBlockSpan.Start - 1, rootWithBlockUncommented.FullSpan.Start ),
					Math.Min( uncommentedBlockSpan.End + 1, rootWithBlockUncommented.FullSpan.End )
				);

				SyntaxNode nodeEnclosingUncommentedBlock = rootWithBlockUncommented
					.FindNode( enclosingSpan )
					.AncestorsAndSelf()
					.FirstOrDefault( n => n.FullSpan.Length >= originalEnclosingBlockLength - sourceLengthDiff );

				Location firstErrorLocation = FindFirstDiagnosticLocation() ?? FindFirstUrlLocation();
				return firstErrorLocation;

				Location FindFirstDiagnosticLocation()
					=> nodeEnclosingUncommentedBlock
					.GetDiagnostics()
					.FirstOrDefault( d => d.Severity == DiagnosticSeverity.Error )
					?.Location;

				/// <remarks>
				/// There's a (very) common false positive for a comment that starts with a url.
				/// The problem is, when it's uncommented, it suddenly becomes a valid label that's immediately followed by a line comment.
				/// Since any such labels is logically a comment text and a comment text is supposed to produce an error once uncommented,
				/// we try to detect this and produce an artificial error of our own, thus triggerring the common comment splitting mechanism.
				/// </remarks>
				Location FindFirstUrlLocation()
					=> nodeEnclosingUncommentedBlock
					.DescendantNodesAndSelf( n => n.GetLocation().SourceSpan.IntersectsWith( uncommentedBlockSpan ) )
					.Where( n => n.GetLocation().SourceSpan.Start >= uncommentedBlockSpan.Start )
					.OfType<LabeledStatementSyntax>()
					.FirstOrDefault( l => IsUncommentedUrl( l ) )
					?.Identifier
					.GetLocation();

				/// <remarks>
				/// This is the weirdest url check that I've ever seen, and I've seen plenty of them =)
				/// Here's the thing: we need some way to differentiate uncommented urls from real labels with trailing comments.
				/// So we rely on the whitespace between the colon token and the comment.
				/// If there is some whitespace between them, then it's clearly not a url and we consider it a real label.
				/// If there is none, then it's probably a url, so we consider it one.
				/// We don't even bother to check if comment text produces a valid url,
				/// because the worst thing that can happen here is a false negative on a label followed immediately (w/o any whitespace) by a trailing comment,
				/// and I think we can live with it.
				/// </remarks>
				bool IsUncommentedUrl( LabeledStatementSyntax labeledStatement )
					=> labeledStatement.ColonToken is var colon
					&& colon.HasTrailingTrivia
					&& colon.TrailingTrivia[ 0 ] is var firstTrivia
					&& firstTrivia.IsKind( SyntaxKind.SingleLineCommentTrivia );
			}

			void ReportBlock( in CommentBlock commentBlock )
			{
				Location location = Location.Create( context.Tree, commentBlock.Span );
				Diagnostic diagnostic = Diagnostic.Create( _rule, location );
				context.ReportDiagnostic( diagnostic );
			}
		}

		private static bool IsCommentTrivia( in SyntaxTrivia trivia )
			=> trivia.IsKind( SyntaxKind.MultiLineCommentTrivia )
			|| trivia.IsKind( SyntaxKind.SingleLineCommentTrivia );

		private class CommentBlockDiscoverer : CSharpSyntaxWalker
		{
			private readonly Action<CommentBlock> _onCommentBlockFound;
			private readonly CancellationToken _cancellationToken;

			public CommentBlockDiscoverer( Action<CommentBlock> onCommentBlockFound, CancellationToken cancellationToken )
				: base( SyntaxWalkerDepth.Trivia )
			{
				_onCommentBlockFound = onCommentBlockFound;
				_cancellationToken = cancellationToken;
			}

			public override void Visit( SyntaxNode node )
			{
				if ( !_cancellationToken.IsCancellationRequested )
					base.Visit( node );
			}

			public override void VisitToken( SyntaxToken token )
			{
				if ( !_cancellationToken.IsCancellationRequested )
					base.VisitToken( token );
			}

			public override void VisitLeadingTrivia( SyntaxToken token )
			{
				if ( token.HasLeadingTrivia )
					VisitTriviaList( token.LeadingTrivia, isLeadingTrivia: true );
			}

			public override void VisitTrailingTrivia( SyntaxToken token )
			{
				if ( token.HasTrailingTrivia )
					VisitTriviaList( token.TrailingTrivia, isLeadingTrivia: false );
			}

			private void VisitTriviaList( SyntaxTriviaList triviaList, bool isLeadingTrivia )
			{
				// It looks like Roslyn always considers trivia list that spans the whole line a leading trivia.
				// Therefore, if this is a leading trivia list, it was preceded with an endline.
				bool endlineBeforeCommentFound = isLeadingTrivia;

				bool commentFound = false;
				int previousMeaningfulTriviaIndex = -1;
				int commentBlockEndCandidateIndex = -1;
				int triviaIndex = 0;

				while ( triviaIndex < triviaList.Count )
				{
					SyntaxTrivia currentTrivia = triviaList[ triviaIndex ];
					SyntaxToken token = currentTrivia.Token;
					int tokenLine = token.GetLocation().GetLineSpan().StartLinePosition.Line;

					if ( IsCommentTrivia( currentTrivia ) )
					{
						commentFound = true;
						commentBlockEndCandidateIndex = triviaIndex;
					}
					else if ( currentTrivia.IsKind( SyntaxKind.EndOfLineTrivia ) )
					{
						// If a comment block ends with an EOL, we typically want to remove the trailing EOL with the comment, so we consider it to be a part of the comment block.
						// However, we do **not** want to do that if the comment doesn't occupy the whole line, because then EOL should be preserved for the code before the comment.
						if ( endlineBeforeCommentFound )
							commentBlockEndCandidateIndex = triviaIndex;

						if ( !commentFound )
							endlineBeforeCommentFound = true;
					}
					else if ( currentTrivia.IsKind( SyntaxKind.WhitespaceTrivia ) )
					{
						// Whitespace may be a part of a comment block, so we don't report the block yet.
						// However, we don't advance the end candidate either, because it may be a trailing whitespace of an inline comment,
						// which we **don't** want to remove with the comment.
					}
					else
					{
						TryReportCommentBlock();

						// Structured trivia always occupies the whole line and hides the EOL inside its structure.
						// So if we've encountered a structured trivia, it means the following comment (if it will be found) is going to have an implicit preceding EOL.
						endlineBeforeCommentFound = currentTrivia.HasStructure;

						commentFound = false;
						previousMeaningfulTriviaIndex = triviaIndex;
					}

					triviaIndex++;
				}

				TryReportCommentBlock();

				void TryReportCommentBlock()
				{
					if ( commentFound )
						_onCommentBlockFound( new CommentBlock( triviaList, previousMeaningfulTriviaIndex + 1, commentBlockEndCandidateIndex ) );
				}
			}
		}

		private readonly struct CommentBlock : IEquatable<CommentBlock>
		{
			private readonly SyntaxTriviaList _triviaList;
			private readonly int _startIndex;
			private readonly int _endIndex;

			public CommentBlock( SyntaxTriviaList triviaList, int startIndex, int endIndex )
			{
				_triviaList = triviaList;
				_startIndex = startIndex;
				_endIndex = endIndex;

				Span = TextSpan.FromBounds( triviaList[ startIndex ].Span.Start, triviaList[ endIndex ].Span.End );
			}

			public TextSpan Span { get; }

			public SourceText GetSourceTextWithBlockUncommented( SourceText originalSourceText )
			{
				string uncommentedBlockText = GetUncommentedBlockText( originalSourceText );
				return originalSourceText.Replace( Span, uncommentedBlockText );
			}

			private string GetUncommentedBlockText( SourceText originalSourceText )
			{
				StringWriter writer = new StringWriter( new StringBuilder( Span.Length ) );

				for ( int i = _startIndex; i <= _endIndex; i++ )
				{
					SyntaxTrivia currentTrivia = _triviaList[ i ];

					(int charsToTrimFromStart, int charsToTrimFromEnd)
						= currentTrivia.IsKind( SyntaxKind.SingleLineCommentTrivia ) ? (_commentMarkerLength, 0)
						: currentTrivia.IsKind( SyntaxKind.MultiLineCommentTrivia ) ? (_commentMarkerLength, _commentMarkerLength)
						: (0, 0);

					TextSpan spanToCopy = TextSpan.FromBounds
					(
						currentTrivia.Span.Start + charsToTrimFromStart,
						currentTrivia.Span.End - charsToTrimFromEnd
					);

					originalSourceText.GetSubText( spanToCopy ).Write( writer );
				}

				return writer.ToString();
			}

			public CommentBlock? TryTrimFirstCommentAtOrAfter( int lineToRemove )
			{
				bool foundCommentToTrim = false;

				for ( int i = _startIndex; i <= _endIndex; i++ )
				{
					SyntaxTrivia currentTrivia = _triviaList[ i ];
					if ( IsCommentTrivia( currentTrivia ) )
					{
						if ( foundCommentToTrim )
							return new CommentBlock( _triviaList, i, _endIndex );
						else
							foundCommentToTrim = lineToRemove <= currentTrivia.GetLocation().GetLineSpan().StartLinePosition.Line;
					}
				}

				return null;
			}

			public override int GetHashCode() => (_triviaList, _startIndex, _endIndex, Span).GetHashCode();
			public bool Equals( CommentBlock other ) => (_triviaList, _startIndex, _endIndex, Span) == (other._triviaList, other._startIndex, other._endIndex, other.Span);
			public override bool Equals( object obj ) => obj is CommentBlock other && Equals( other );

			public static bool operator ==( CommentBlock x, CommentBlock y ) => x.Equals( y );
			public static bool operator !=( CommentBlock x, CommentBlock y ) => !x.Equals( y );
		}
	}
}
