using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HellBrick.Diagnostics.StringInterpolation
{
	internal static class FormatStringParser
	{
		/// <remarks>
		/// This is based on http://referencesource.microsoft.com/#mscorlib/system/text/stringbuilder.cs,2c3b4c2e7c43f5a4
		/// </remarks>
		public static IReadOnlyList<InterpolatedStringContentSyntax> Parse( string formatString, IReadOnlyList<ExpressionSyntax> arguments )
		{
			if ( formatString == null )
				throw new ArgumentNullException( "format" );

			int pos = 0;
			int len = formatString.Length;
			char ch = '\x0';
			StringBuilder currentTextBuilder = new StringBuilder();
			List<InterpolatedStringContentSyntax> parts = new List<InterpolatedStringContentSyntax>();

			while ( true )
			{
				int p = pos;
				int i = pos;
				while ( pos < len )
				{
					ch = formatString[ pos ];

					pos++;
					if ( ch == '}' )
					{
						if ( pos < len && formatString[ pos ] == '}' ) // Treat as escape character for }}
							pos++;
						else
							return null;
					}

					if ( ch == '{' )
					{
						if ( pos < len && formatString[ pos ] == '{' ) // Treat as escape character for {{
							pos++;
						else
						{
							pos--;
							break;
						}
					}

					currentTextBuilder.Append( ch );
				}

				// When we get here, it means we've finished parsing a text part.
				if ( currentTextBuilder.Length > 0 )
				{
					string text = currentTextBuilder.ToString();
					var token = SyntaxFactory.Token( SyntaxTriviaList.Empty, SyntaxKind.InterpolatedStringTextToken, text, text, SyntaxTriviaList.Empty );
					var textPart = SyntaxFactory.InterpolatedStringText( token );
					parts.Add( textPart );
					currentTextBuilder = new StringBuilder();
				}

				if ( pos == len )
					break;

				pos++;
				if ( pos == len || ( ch = formatString[ pos ] ) < '0' || ch > '9' )
					return null;

				int argIndex = 0;
				do
				{
					argIndex = argIndex * 10 + ch - '0';
					pos++;
					if ( pos == len )
						return null;

					ch = formatString[ pos ];
				}
				while ( ch >= '0' && ch <= '9' && argIndex < 1000000 );

				if ( argIndex >= arguments.Count )
					return null;

				while ( pos < len && ( ch = formatString[ pos ] ) == ' ' )
					pos++;

				bool leftJustify = false;
				int width = 0;
				if ( ch == ',' )
				{
					pos++;
					while ( pos < len && formatString[ pos ] == ' ' ) pos++;

					if ( pos == len )
						return null;

					ch = formatString[ pos ];
					if ( ch == '-' )
					{
						leftJustify = true;
						pos++;
						if ( pos == len )
							return null;

						ch = formatString[ pos ];
					}

					if ( ch < '0' || ch > '9' )
						return null;

					do
					{
						width = width * 10 + ch - '0';
						pos++;
						if ( pos == len )
							return null;

						ch = formatString[ pos ];
					}
					while ( ch >= '0' && ch <= '9' && width < 1000000 );
				}

				while ( pos < len && ( ch = formatString[ pos ] ) == ' ' )
					pos++;

				StringBuilder holeFormatBuilder = null;
				if ( ch == ':' )
				{
					pos++;
					p = pos;
					i = pos;
					while ( true )
					{
						if ( pos == len ) return null;
						ch = formatString[ pos ];
						pos++;
						if ( ch == '{' )
						{
							if ( pos < len && formatString[ pos ] == '{' )  // Treat as escape character for {{
								pos++;
							else
								return null;
						}
						else if ( ch == '}' )
						{
							if ( pos < len && formatString[ pos ] == '}' )  // Treat as escape character for }}
								pos++;
							else
							{
								pos--;
								break;
							}
						}

						if ( holeFormatBuilder == null )
						{
							holeFormatBuilder = new StringBuilder();
						}
						holeFormatBuilder.Append( ch );
					}
				}

				if ( ch != '}' )
					return null;

				pos++;

				//	By this moment we've parsed everything we need to know about the current hole
				var argument = arguments[ argIndex ].WithLeadingTrivia( SyntaxTriviaList.Empty ).WithTrailingTrivia( SyntaxTriviaList.Empty );
				var alignment = TryCreateAlignmentExpression( width, leftJustify );
				var formatClause = TryCreateFormatClauseExpression( holeFormatBuilder );
				var interpolationPart = SyntaxFactory.Interpolation( argument, alignment, formatClause );
				parts.Add( interpolationPart );
			}

			return parts;
		}

		private static InterpolationAlignmentClauseSyntax TryCreateAlignmentExpression( int width, bool leftJustify )
		{
			if ( width == 0 )
				return null;

			LiteralExpressionSyntax widthExpression = SyntaxFactory.LiteralExpression( SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal( width ) );
			ExpressionSyntax alignmentValueExpression = leftJustify ?
				SyntaxFactory.PrefixUnaryExpression( SyntaxKind.UnaryMinusExpression, widthExpression ) as ExpressionSyntax :
				widthExpression as ExpressionSyntax;

			InterpolationAlignmentClauseSyntax alignmentClause = SyntaxFactory.InterpolationAlignmentClause( SyntaxFactory.Token( SyntaxKind.CommaToken ), alignmentValueExpression );
			return alignmentClause;
		}

		private static InterpolationFormatClauseSyntax TryCreateFormatClauseExpression( StringBuilder holeFormatBuilder )
		{
			try
			{
				if ( holeFormatBuilder == null )
					return null;

				return SyntaxFactory.InterpolationFormatClause
				(
					SyntaxFactory.Token( SyntaxKind.ColonToken ),
					SyntaxFactory.Token
					(
						SyntaxTriviaList.Empty,
						SyntaxKind.InterpolatedStringTextToken,
						holeFormatBuilder.ToString(),
						holeFormatBuilder.ToString(),
						SyntaxTriviaList.Empty
					)
				);
			}
			catch
			{
				throw;
			}
		}
	}
}
