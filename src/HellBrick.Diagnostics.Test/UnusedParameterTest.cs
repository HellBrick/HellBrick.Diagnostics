using HellBrick.Diagnostics.DeadCode;
using HellBrick.Diagnostics.Assertions;
using Xunit;

namespace HellBrick.Diagnostics.Test
{
	public class UnusedParameterTest
	{
		private readonly AnalyzerVerifier<UnusedParameterAnalyzer, UnusedParameterCodeFixProvider> _verifier
			= AnalyzerVerifier
			.UseAnalyzer<UnusedParameterAnalyzer>()
			.UseCodeFix<UnusedParameterCodeFixProvider>();

		[Fact]
		public void UnusedClassMethodParameterIsRemoved()
			=> _verifier
			.Source
			(
@"public class C
{
	public void Something( int good1, string bad, int good2 )
	{
		var x = good1;
		var y = good2;
	}
}"
			)
			.ShouldHaveFix
			(
@"public class C
{
	public void Something( int good1, int good2 )
	{
		var x = good1;
		var y = good2;
	}
}"
			);

		[Fact]
		public void CorrespondingArgumentIsRemovedWhenCalledFromSameClass()
			=> _verifier
			.Source
			(
@"public class C
{
	public void Proxy() => Something( 42, default( string ), 64 );
	public void Something( int good1, string bad, int good2 )
	{
		var x = good1;
		var y = good2;
	}
}"
			)
			.ShouldHaveFix
			(
@"public class C
{
	public void Proxy() => Something( 42, 64 );
	public void Something( int good1, int good2 )
	{
		var x = good1;
		var y = good2;
	}
}"
			);

		[Fact]
		public void CorrespondingArgumentIsRemovedWhenCalledFromAnotherClass()
			=> _verifier
			.Source
			(
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
}"
			)
			.ShouldHaveFix
			(
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
}"
			);

		[Fact]
		public void UnusedThisParameterIsNotRemoved()
			=> _verifier
			.Source
			(
@"
public static class Extensions
{
	public static int ExtensionMethod<T>( this T instance, int value ) => value;
}
"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void UnusedParameterIsNotRemovedIfMethodImplementsInterface()
			=> _verifier
			.Source
			(
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
}"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void UnusedParameterIsNotRemovedIfMethodIsVirtual()
			=> _verifier
			.Source
			(
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
}"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void UnusedConstructorParameterIsRemoved()
			=> _verifier
			.Source
			(
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
}"
			)
			.ShouldHaveFix
			(
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
}"
			);

		[Fact]
		public void ExpressionBodyIsExaminedCorrectly()
			=> _verifier
			.Source
			(
@"
public class C
{
	public string DoMagic( int unused, string used ) => used;
}
"
			)
			.ShouldHaveFix
			(
@"
public class C
{
	public string DoMagic( string used ) => used;
}
"
			);

		[Fact]
		public void BaseCallAndExpressionBodyAreExaminedCorrectly()
			=> _verifier
			.Source
			(
@"
public class CustomException : Exception
{
	public CustomException( string usedByBase, string usedByBody, string unused )
		: base( usedByBase )
		=> Line = usedByBody;

	public string Line { get; }
}
"
			)
			.ShouldHaveFix
			(
@"
public class CustomException : Exception
{
	public CustomException( string usedByBase, string usedByBody )
		: base( usedByBase )
		=> Line = usedByBody;

	public string Line { get; }
}
"
			);

		[Fact]
		public void BaseCallAndBlockBodyAreExaminedCorrectly()
			=> _verifier
			.Source
			(
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
"
			)
			.ShouldHaveFix
			(
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
"
			);

		[Fact]
		public void UnusedLambdaParameterIsNotRemoved()
			=> _verifier
			.Source
			(
@"public class C
{
	public C()
	{
		var func1 = ( int x, string _ ) => x;
		var func2 = ( int x, string _ ) => { return x; };
	}
}"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void EntryPointArgumentIsNotRemoved()
			=> _verifier
			.Source
			(
@"internal class Program
{
	private static void Main( string[] args )
	{
	}
}"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void AllUnusedMethodParametersAreRemoved()
			=> _verifier
			.Source
			(
@"public class C
{
	public void Proxy() => Something( default( string ), 42, 64 );
	public void Something( string unused1, int good, int unused2 )
	{
		var x = good;
	}
}"
			)
			.ShouldHaveFix
			(
@"public class C
{
	public void Proxy() => Something( 42 );
	public void Something( int good )
	{
		var x = good;
	}
}"
			);

		[Fact]
		public void CorrectArgumentIsRemovedIfArgumentOrderDiffersFromParameterOrder()
			=> _verifier
			.Source
			(
@"public class C
{
	public void Proxy() => Something( 42, good2: 64, bad: ""100"" );
	public void Something( int good1, string bad, int good2 )
	{
		var x = good1;
		var y = good2;
	}
}"
			)
			.ShouldHaveFix
			(
@"public class C
{
	public void Proxy() => Something( 42, good2: 64 );
	public void Something( int good1, int good2 )
	{
		var x = good1;
		var y = good2;
	}
}"
			);

		[Fact]
		public void ArgumentIsNotRemovedIfDefaultValueIsPassedtoOptionalParameter()
			=> _verifier
			.Source
			(
@"public class C
{
	public void Proxy() => Something( 42 );
	public void Something( int good, string bad = null )
	{
		var x = good;
	}
}"
			)
			.ShouldHaveFix
			(
@"public class C
{
	public void Proxy() => Something( 42 );
	public void Something( int good )
	{
		var x = good;
	}
}"
			);

		[Fact]
		public void NestedCallArgumentIsRemovedCompletely()
			=> _verifier
			.Source
			(
@"public class C
{
	public string Proxy() => Something( 42, Something( 64, 100 ) );
	public string Something( int good, string bad ) => good.ToString();
}"
			)
			.ShouldHaveFix
			(
@"public class C
{
	public string Proxy() => Something( 42 );
	public string Something( int good ) => good.ToString();
}"
			);

		[Fact]
		public void NestedCallArgumentIsAdjustedCorrectlyIfPassedAsParameterThatIsNotRemoved()
			=> _verifier
			.Source
			(
@"public class C
{
	public int Proxy() => Something( Something( 42, ""asdf"" ), ""qwerty"" );
	public int Something( int good, string bad ) => good * 2;
}"
			)
			.ShouldHaveFix
			(
@"public class C
{
	public int Proxy() => Something( Something( 42 ) );
	public int Something( int good ) => good * 2;
}"
			);

		[Fact]
		public void NoFalsePositiveForInterfaceMethodWithParameterDefaultValue()
			=> _verifier
			.Source
			(
@"
public interface I
{
	void InterfaceMethod( int normalParam, string defaultParam = "" );
}
"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void TypeArgumentsAreNotAddedToCallSiteIfTheyCanStillBeInferred()
			=> _verifier
			.Source
			(
@"
using System;
public class C
{
	public static TOut Convert<TIn, TOut>( TIn arg, Func<TIn, TOut> converter ) => converter( default );
	public static void CallSite() => Convert( 42, ( int number ) => number.ToString() );
}
"
			)
			.ShouldHaveFix
			(
@"
using System;
public class C
{
	public static TOut Convert<TIn, TOut>( Func<TIn, TOut> converter ) => converter( default );
	public static void CallSite() => Convert( ( int number ) => number.ToString() );
}
"
			);

		[Fact]
		public void TypeArgumentsAreAddedToSimpleCallSite()
			=> _verifier
			.Source
			(
@"
using System;
public class C
{
	public static TOut Convert<TIn, TOut>( TIn arg, Func<TIn, TOut> converter ) => converter( default );
	public static void CallSite() => Convert( 42, number => number.ToString() );
}
"
			)
			.ShouldHaveFix
			(
@"
using System;
public class C
{
	public static TOut Convert<TIn, TOut>( Func<TIn, TOut> converter ) => converter( default );
	public static void CallSite() => Convert<int, string>( number => number.ToString() );
}
"
			);

		[Fact]
		public void TypeArgumentsAreAddedToInvocationChainCallSite()
			=> _verifier
			.Source
			(
@"
using System;
public class ConverterFactory
{
	public Converter<TOut> To<TOut>();
}
public class Converter<TOut>
{
	public TOut Convert<TIn>( TIn arg, Func<TIn, TOut> converter ) => converter( default );
}
public class Program
{
	private readonly object[] _converterFactories;
	public static void CallSite() => ( _converterFactories[ 0 ] as ConverterFactory ).To<string>().Convert( 42, number => number.ToString() );
}
"
			)
			.ShouldHaveFix
			(
@"
using System;
public class ConverterFactory
{
	public Converter<TOut> To<TOut>();
}
public class Converter<TOut>
{
	public TOut Convert<TIn>( Func<TIn, TOut> converter ) => converter( default );
}
public class Program
{
	private readonly object[] _converterFactories;
	public static void CallSite() => ( _converterFactories[ 0 ] as ConverterFactory ).To<string>().Convert<int>( number => number.ToString() );
}
"
			);
	}
}
