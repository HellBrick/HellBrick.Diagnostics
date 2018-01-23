using HellBrick.Diagnostics.DeadCode;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

namespace HellBrick.Diagnostics.Test
{
	public class UnusedParameterTest : CodeFixVerifier
	{
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new UnusedParameterAnalyzer();
		protected override CodeFixProvider GetCSharpCodeFixProvider() => new UnusedParameterCodeFixProvider();

		[Fact]
		public void UnusedClassMethodParameterIsRemoved()
		{
			const string source =
@"public class C
{
	public void Something( int good1, string bad, int good2 )
	{
		var x = good1;
		var y = good2;
	}
}";
			const string result =
@"public class C
{
	public void Something( int good1, int good2 )
	{
		var x = good1;
		var y = good2;
	}
}";
			VerifyCSharpFix( source, result );
		}

		[Fact]
		public void CorrespondingArgumentIsRemovedWhenCalledFromSameClass()
		{
			const string source =
@"public class C
{
	public void Proxy() => Something( 42, default( string ), 64 );
	public void Something( int good1, string bad, int good2 )
	{
		var x = good1;
		var y = good2;
	}
}";
			const string result =
@"public class C
{
	public void Proxy() => Something( 42, 64 );
	public void Something( int good1, int good2 )
	{
		var x = good1;
		var y = good2;
	}
}";
			VerifyCSharpFix( source, result );
		}

		[Fact]
		public void CorrespondingArgumentIsRemovedWhenCalledFromAnotherClass()
		{
			const string source =
@"public class C
{
	public void Something( int good1, string bad, int good2 )
	{
		var x = good1;
		var y = good2;
	}
}

public class D
{
	public void Proxy() => new C().Something( 42, ""asdf"" + ""qwer"", 0 );
}";
			const string result =
@"public class C
{
	public void Something( int good1, int good2 )
	{
		var x = good1;
		var y = good2;
	}
}

public class D
{
	public void Proxy() => new C().Something( 42, 0 );
}";
			VerifyCSharpFix( source, result );
		}

		[Fact]
		public void UnusedThisParameterIsNotRemoved()
		{
			const string source =
@"
public static class Extensions
{
	public static int ExtensionMethod<T>( this T instance, int value ) => value;
}
";
			VerifyNoFix( source );
		}

		[Fact]
		public void UnusedParameterIsNotRemovedIfMethodImplementsInterface()
		{
			const string source =
@"public interface I
{
	void Method( int arg );
}

public class C : I
{
	public void Method( int arg )
	{
	}
}

public class D : I
{
	void I.Method( int arg )
	{
	}
}";
			VerifyNoFix( source );
		}

		[Fact]
		public void UnusedParameterIsNotRemovedIfMethodIsVirtual()
		{
			const string source =
@"public class C
{
	public virtual void Method( int arg )
	{
	}
}

public class D : C
{
	public override void Method( int arg )
	{
		while ( true )
		{
		}
	}
}";
			VerifyNoFix( source );
		}

		[Fact]
		public void UnusedConstructorParameterIsRemoved()
		{
			const string source =
@"public class C
{
	public C( int unused )
	{
	}
}

public class D : C
{
	public D() : base( 42 )
	{
	}
}";
			const string result =
@"public class C
{
	public C()
	{
	}
}

public class D : C
{
	public D() : base()
	{
	}
}";
			VerifyCSharpFix( source, result );
		}

		[Fact]
		public void ExpressionBodyIsExaminedCorrectly()
		{
			const string before =
@"
public class C
{
	public string DoMagic( int unused, string used ) => used;
}
";

			const string after =
@"
public class C
{
	public string DoMagic( string used ) => used;
}
";
			VerifyCSharpFix( before, after );
		}

		[Fact]
		public void BaseCallAndExpressionBodyAreExaminedCorrectly()
		{
			const string before =
@"
public class CustomException : Exception
{
	public CustomException( string usedByBase, string usedByBody, string unused )
		: base( usedByBase )
		=> Line = usedByBody;

	public string Line { get; }
}
";

			const string after =
@"
public class CustomException : Exception
{
	public CustomException( string usedByBase, string usedByBody )
		: base( usedByBase )
		=> Line = usedByBody;

	public string Line { get; }
}
";
			VerifyCSharpFix( before, after );
		}

		[Fact]
		public void BaseCallAndBlockBodyAreExaminedCorrectly()
		{
			const string before =
@"
public class CustomException : Exception
{
	public CustomException( string usedByBase, string usedByBody, string unused )
		: base( usedByBase )
	{
		Line = usedByBody;
	}

	public string Line { get; }
}
";

			const string after =
@"
public class CustomException : Exception
{
	public CustomException( string usedByBase, string usedByBody )
		: base( usedByBase )
	{
		Line = usedByBody;
	}

	public string Line { get; }
}
";
			VerifyCSharpFix( before, after );
		}

		[Fact]
		public void UnusedLambdaParameterIsNotRemoved()
		{
			const string source =
@"public class C
{
	public C()
	{
		var func1 = ( int x, string _ ) => x;
		var func2 = ( int x, string _ ) => { return x; };
	}
}";
			VerifyNoFix( source );
		}

		[Fact]
		public void EntryPointArgumentIsNotRemoved()
		{
			const string source =
@"internal class Program
{
	private static void Main( string[] args )
	{
	}
}";
			VerifyNoFix( source );
		}

		[Fact]
		public void AllUnusedMethodParametersAreRemoved()
		{
			const string source =
@"public class C
{
	public void Proxy() => Something( default( string ), 42, 64 );
	public void Something( string unused1, int good, int unused2 )
	{
		var x = good;
	}
}";
			const string result =
@"public class C
{
	public void Proxy() => Something( 42 );
	public void Something( int good )
	{
		var x = good;
	}
}";
			VerifyCSharpFix( source, result );
		}

		[Fact]
		public void CorrectArgumentIsRemovedIfArgumentOrderDiffersFromParameterOrder()
		{
			const string source =
@"public class C
{
	public void Proxy() => Something( 42, good2: 64, bad: ""100"" );
	public void Something( int good1, string bad, int good2 )
	{
		var x = good1;
		var y = good2;
	}
}";
			const string result =
@"public class C
{
	public void Proxy() => Something( 42, good2: 64 );
	public void Something( int good1, int good2 )
	{
		var x = good1;
		var y = good2;
	}
}";
			VerifyCSharpFix( source, result );
		}

		[Fact]
		public void ArgumentIsNotRemovedIfDefaultValueIsPassedtoOptionalParameter()
		{
			const string source =
@"public class C
{
	public void Proxy() => Something( 42 );
	public void Something( int good, string bad = null )
	{
		var x = good;
	}
}";
			const string result =
@"public class C
{
	public void Proxy() => Something( 42 );
	public void Something( int good )
	{
		var x = good;
	}
}";
			VerifyCSharpFix( source, result );
		}

		[Fact]
		public void NestedCallArgumentIsRemovedCompletely()
		{
			const string source =
@"public class C
{
	public string Proxy() => Something( 42, Something( 64, 100 ) );
	public string Something( int good, string bad ) => good.ToString();
}";
			const string result =
@"public class C
{
	public string Proxy() => Something( 42 );
	public string Something( int good ) => good.ToString();
}";
			VerifyCSharpFix( source, result );
		}

		[Fact]
		public void NestedCallArgumentIsAdjustedCorrectlyIfPassedAsParameterThatIsNotRemoved()
		{
			const string source =
@"public class C
{
	public int Proxy() => Something( Something( 42, ""asdf"" ), ""qwerty"" );
	public int Something( int good, string bad ) => good * 2;
}";
			const string result =
@"public class C
{
	public int Proxy() => Something( Something( 42 ) );
	public int Something( int good ) => good * 2;
}";
			VerifyCSharpFix( source, result );
		}
	}
}
