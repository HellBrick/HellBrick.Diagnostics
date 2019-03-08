using HellBrick.Diagnostics.Assertions;
using HellBrick.Diagnostics.CommentedCode;
using Xunit;

namespace HellBrick.Diagnostics.Test
{
	public class CommentedCodeTest
	{
		private readonly AnalyzerVerifier<CommentedCodeAnalyzer, CommentedCodeCodeFixProvider> _verifier
			= AnalyzerVerifier
			.UseAnalyzer<CommentedCodeAnalyzer>()
			.UseCodeFix<CommentedCodeCodeFixProvider>();

		[Fact]
		public void RealSingleLineCommentIsIgnored()
			=> _verifier
			.Source
			(
@"using System;
public class C
{
	public void M()
	{
		// This is a proper comment, don't touch it.
		Console.WriteLine( 42 );
	}
}"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void RealCommentBlockIsIgnored()
			=> _verifier
			.Source
			(
@"using System;
public class C
{
	public void M()
	{
		// This line is not a commented out code.
		// And neither is this line.
		Console.WriteLine( 42 );
	}
}"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void RealMultiLineCommentIsIgnored()
			=> _verifier
			.Source
			(
@"using System;
public class C
{
	public void M()
	{
		/*
		 * No one creates comments like this.
		 * Doesn't mean we should remove them though.
		 * */
		Console.WriteLine( 42 );
	}
}"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void RealCommentBlockBetweenStructMembersIsIgnored()
			=> _verifier
			.Source
			(
@"using System;
public readonly struct S
{
	private int _field;

	// This tests an important diagnostic detection corner case
	//	where the uncommented block node is the whole struct definition
	// that has diagnostics outside the block span.
	public void M()
	{
	}
}"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void SingleLineCodeCommentIsRemoved()
			=> _verifier
			.Source
			(
@"public class C
{
	public void M()
	{
		// Console.WriteLine( ""42"" );
	}
}"
			)
			.ShouldHaveFix
			(
@"public class C
{
	public void M()
	{
	}
}"
			);

		[Fact]
		public void CodeCommentBlockIsRemoved()
			=> _verifier
			.Source
			(
@"public class C
{
	public void M()
	{
		// const string text = ""42"";
		// Console.WriteLine( text );
	}
}"
			)
			.ShouldHaveFix
			(
@"public class C
{
	public void M()
	{
	}
}"
			);

		[Fact]
		public void MultiLineCodeCommentIsRemoved()
			=> _verifier
			.Source
			(
@"public class C
{
	public void M()
	{
		/*
		const string text = ""42"";
		Console.WriteLine( text );
		*/
	}
}"
			)
			.ShouldHaveFix
			(
@"public class C
{
	public void M()
	{
	}
}"
			);

		[Fact]
		public void CodeCommentBlockWithPrecedingCommentIsRemoved()
			=> _verifier
			.Source
			(
@"public class C
{
	public void M()
	{
		//	This was used back in the days, but no longer is.
		// But maaaaybe it will be needed in the future. (Spoiler: it won't.)
		// const string text = ""42"";
		// Console.WriteLine( text );
	}
}"
			)
			.ShouldHaveFix
			(
@"public class C
{
	public void M()
	{
	}
}"
			);

		[Fact]
		public void CodeCommentBlockWithPrecedingMultiLineCommentIsRemoved()
			=> _verifier
			.Source
			(
@"public class C
{
	public void M()
	{
		/* Someone probably thought it was a good idea to save this for the future.
		 * But it wasn't. */
		// const string text = ""42"";
		// Console.WriteLine( text );
	}
}"
			)
			.ShouldHaveFix
			(
@"public class C
{
	public void M()
	{
	}
}"
			);

		[Fact]
		public void MultiLineCodeCommentWithPrecedingCommentBlockIsRemoved()
			=> _verifier
			.Source
			(
@"public class C
{
	public void M()
	{
		// You'll probably be surprised (but I hope you won't be)...
		//	But this is still not a good idea.
		/*
		const string text = ""42"";
		Console.WriteLine( text );
		*/
	}
}"
			)
			.ShouldHaveFix
			(
@"public class C
{
	public void M()
	{
	}
}"
			);

		[Fact]
		public void SingleLineCodeCommentPrecedingRealCodeIsRemoved()
			=> _verifier
			.Source
			(
@"public class C
{
	public void M()
	{
		// Console.WriteLine( ""42"" );
		Console.WriteLine( ""64"" );
	}
}"
			)
			.ShouldHaveFix
			(
@"public class C
{
	public void M()
	{
		Console.WriteLine( ""64"" );
	}
}"
			);

		[Fact]
		public void InlineCodeCommentIsRemoved()
			=> _verifier
			.Source
			(
@"public class C
{
	public void M()
	{
		Console.WriteLine( 64/*.ToString()*/ );
	}
}"
			)
			.ShouldHaveFix
			(
@"public class C
{
	public void M()
	{
		Console.WriteLine( 64 );
	}
}"
			);

		[Fact]
		public void CodeCommentBlockSurroundedByDirectivesIsRemoved()
			=> _verifier
			.Source
			(
@"public class C
{
	public void M()
	{
		#region Please don't do this
		// Console.WriteLine( 64 );
		#endregion
	}
}"
			)
			.ShouldHaveFix
			(
@"public class C
{
	public void M()
	{
		#region Please don't do this
		#endregion
	}
}"
			);

		[Fact]
		public void MultiLineCodeCommentSurroundedByDirectivesIsRemoved()
			=> _verifier
			.Source
			(
@"public class C
{
	public void M()
	{
		#region Please don't do this either
		/* Console.WriteLine( 64 ); */
		#endregion
	}
}"
			)
			.ShouldHaveFix
			(
@"public class C
{
	public void M()
	{
		#region Please don't do this either
		#endregion
	}
}"
			);

		[Fact]
		public void EndOfFileCodeCommentBlockIsRemoved()
			=> _verifier
			.Source
			(
@"namespace N1
{
	public class C
	{
		public void M()
		{
		}
	}
}

// namespace N2
// {
// }
"
			)
			.ShouldHaveFix
			(
@"namespace N1
{
	public class C
	{
		public void M()
		{
		}
	}
}
"
			);

		[Fact]
		public void EndOfFileMultiLineCodeCommentIsRemoved()
			=> _verifier
			.Source
			(
@"namespace N1
{
	public class C
	{
		public void M()
		{
		}
	}
}

/*namespace N2
{
}*/
"
			)
			.ShouldHaveFix
			(
@"namespace N1
{
	public class C
	{
		public void M()
		{
		}
	}
}
"
			);

		[Fact]
		public void CommentedOutFileIsCleared()
			=> _verifier
			.Source
			(
@"/*namespace N1
{
	public class C
	{
		public void M()
		{
		}
	}
}

namespace N2
{
}*/"
			)
			.ShouldHaveFix( "" );

		[Fact]
		public void RealCommentBlockInsideMethodInvocationIsIgnored()
			=> _verifier
			.Source
			(
@"public class C
{
	public void M()
		=> Inner
		(
			// This is a comment, no need to touch it
			128
		);

	private void Inner( int number ) { }
}"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void EndOfLineCodeCommentIsRemoved()
			=> _verifier
			.Source
			(
@"public class C
{
	public int M()
	{
		System.Console.WriteLine( 42 ); // this.Is.Stupid( but.Tests.Trivia.Corner.Case() );
	}
}"
			)
			.ShouldHaveFix
			(
@"public class C
{
	public int M()
	{
		System.Console.WriteLine( 42 );
	}
}"
			);

		[Fact]
		public void EndOfLineMultiLineCodeCommentIsRemoved()
			=> _verifier
			.Source
			(
@"public class C
{
	public int M()
	{
		System.Console.WriteLine( 42 ); /* this.Is.Stupid();
		but.Tests.An.Even.Trickier.Trivia.Corner.Case(); */
	}
}"
			)
			.ShouldHaveFix
			(
@"public class C
{
	public int M()
	{
		System.Console.WriteLine( 42 );
	}
}"
			);

		[Fact]
		public void CodeCommentFollowingXmlCommentIsRemoved()
			=> _verifier
			.Source
			(
@"public class C
{
	public int M()
	{
		/// This is the heart and soul of <see cref=""M"" />.
		// System.Console.WriteLine( 42 );
	}
}"
			)
			.ShouldHaveFix
			(
@"public class C
{
	public int M()
	{
		/// This is the heart and soul of <see cref=""M"" />.
	}
}"
			);

		[Fact]
		public void CommentedOutXmlCommentIsRemoved()
			=> _verifier
			.Source
			(
@"public class C
{
	///// <summary>Why would anyone do such a thing?</summary>
	public int M()
	{
	}
}"
			)
			.ShouldHaveFix
			(
@"public class C
{
	public int M()
	{
	}
}"
			);

		[Fact]
		public void RealCommentInsideEnabledConditionalBlockIsIgnored()
		{
			const string symbol = "Flag";
			_verifier
				.WithParseOptions( o => o.WithPreprocessorSymbols( symbol ) )
				.Source
				(
$@"public class C
{{
	public int M()
	{{
#if {symbol}
		// This is a normal comment
		System.Console.WriteLine( 42 );
#endif
	}}
}}"
				)
				.ShouldHaveNoDiagnostics();
		}

		[Fact]
		public void CodeCommentInsideEnabledConditionalBlockIsRemoved()
		{
			const string symbol = "Flag";
			_verifier
				.WithParseOptions( o => o.WithPreprocessorSymbols( symbol ) )
				.Source
				(
$@"public class C
{{
	public int M()
	{{
#if {symbol}
		// This is conditional commented out code
		// System.Console.WriteLine( 42 );
#endif
	}}
}}"
				)
				.ShouldHaveFix
				(
$@"public class C
{{
	public int M()
	{{
#if {symbol}
#endif
	}}
}}"
				);
		}

		[Fact]
		public void DisabledConditionalBlockIsIgnored()
			=> _verifier
			.Source
			(
@"public class C
{
	public int M()
	{
#if SomeUnknownSymbol
		// This is a normal comment
		System.Console.WriteLine( 42 );

		// See no evil, hear no evil
		// System.Console.WriteLine( 42 );
#endif
	}
}"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void InlineIdentifierLikeCommentInArrayInitializerIsIgnored()
			=> _verifier
			.Source
			(
@"public class C
{
	public int[] M()
		=> new int[]
		{
			/*id1*/ 42,
			/*id2*/ 64,
		};
}"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void InlineIdentifierLikeCommentPrecededByXmlCommentInArrayInitializerIsIgnored()
			=> _verifier
			.Source
			(
@"public class C
{
	public int[] M()
		=> new int[]
		{
			/// <see cref=""M"" />
			/*id1*/ 42,
			/*id2*/ 64,
		};
}"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void OneWordCommentBeforeVariableDeclarationIsIgnored()
			=> _verifier
			.Source
			(
@"public class C
{
	public void M()
	{
		// ThisIsATrickyFalsePositive
		int x = 42;
	}

}"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void LineTrailingOneWordCommentBeforeExplicitlyTypedVariableDeclarationIsIgnored()
			=> _verifier
			.Source
			(
@"public static class C
{
	public static void AllocateBuffer()
	{
		int bufferLength = 42; // ThisIsAnotherTrickyFalsePositive
		byte[] buffer = new byte[bufferLength];
	}
}"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void LineTrailingOneWordCommentBeforeAutoTypedVariableDeclarationIsIgnored()
			=> _verifier
			.Source
			(
@"public static class C
{
	public static void AllocateBuffer()
	{
		var bufferLength = 42; // WHYYYYYYYYYYYYYYYYYYYYYYY
		var buffer = new byte[bufferLength];
	}
}"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void RealCommentWithAbsoluteUrlIsIgnored()
			=> _verifier
			.Source
			(
@"public class C
{
	public static void M()
	{
		// This is a possible false positive, because the url below is a valid label + comment.
		// https://stackoverflow.com/a/1732454/1870435
		int localVar = 42;
	}
}"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void CommentedOutLabelWithTrailingCommentIsRemoved()
			=> _verifier
			.Source
			(
@"public class C
{
	public static void M()
	{
		// This is a possible false negative caused by a url/label false positive fix.
		//	This is a real label though, notice the whitespace between the colon token and the comment.
		// https: //stackoverflow.com/a/1732454/1870435
		int localVar = 42;
	}
}"
			)
			.ShouldHaveFix
			(
@"public class C
{
	public static void M()
	{
		int localVar = 42;
	}
}"
			);

		[Fact]
		public void CodeCommentWithAbsoluteUrlCommentIsRemoved()
			=> _verifier
			.Source
			(
@"public class C
{
	public static void M()
	{
		// This is related to the false positive caused by the url below being a valid label + comment.
		// https://stackoverflow.com/a/1732454/1870435
		// int localVar = 42;
	}
}"
			)
			.ShouldHaveFix
			(
@"public class C
{
	public static void M()
	{
	}
}"
			);
	}
}
