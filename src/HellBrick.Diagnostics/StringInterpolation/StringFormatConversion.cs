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
	internal struct StringFormatConversion
	{
		public static readonly StringFormatConversion None = default( StringFormatConversion );

		public InvocationExpressionSyntax FormatCall { get; }
		public InterpolatedStringExpressionSyntax InterpolatedString { get; }

		public bool IsSuccess => FormatCall != null && InterpolatedString != null;

		private StringFormatConversion( InvocationExpressionSyntax formatCall, InterpolatedStringExpressionSyntax interpolatedString )
		{
			FormatCall = formatCall;
			InterpolatedString = interpolatedString;
		}

		public static StringFormatConversion TryCreateConversion( InvocationExpressionSyntax formatCall )
		{
			var formatString = ( formatCall.ArgumentList.Arguments.FirstOrDefault()?.Expression as LiteralExpressionSyntax )?.Token.ValueText;
			if ( formatString == null )
				return None;

			var arguments = formatCall.ArgumentList.Arguments.Skip( 1 ).Select( arg => arg.Expression ).ToList();
			var parts = FormatStringParser.Parse( formatString, arguments );
			if ( parts == null )
				return None;

			InterpolatedStringExpressionSyntax interpolatedString =
				SyntaxFactory.InterpolatedStringExpression
				(
					SyntaxFactory.Token( SyntaxKind.InterpolatedStringStartToken ),
					SyntaxFactory.List<InterpolatedStringContentSyntax>( parts )
				);

			return new StringFormatConversion( formatCall, interpolatedString );
      }
	}
}
