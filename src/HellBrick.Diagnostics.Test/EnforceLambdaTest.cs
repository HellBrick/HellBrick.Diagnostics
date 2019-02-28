using HellBrick.Diagnostics.Assertions;
using HellBrick.Diagnostics.EnforceLambda;
using Xunit;

namespace HellBrick.Diagnostics.Test
{
	public class EnforceLambdaTest
	{
		private readonly AnalyzerVerifier<EnforceLambdaAnalyzer, EnforceLambdaCodeFixProvider> _verifier
			= AnalyzerVerifier
			.UseAnalyzer<EnforceLambdaAnalyzer>()
			.UseCodeFix<EnforceLambdaCodeFixProvider>();

		[Fact]
		public void LambdaIsIgnored()
			=> _verifier
			.Source
			(
@"using System;
public static class C
{
	public static Action GetAction() => () => Inner( 42 );

	private static void Inner( int number ) { }
}"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void InstanceQualifiedMethodGroupIsIgnored()
			=> _verifier
			.Source
			(
@"using System;
public class C
{
	public C( string text ) { }

	public Action GetAction() => new C( ""42"" ).Inner;

	private void Inner() { }
}"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void MethodGroupPassedToEventSubscriptionIsIgnored()
			=> _verifier
			.Source
			(
@"using System;
public static class C
{
	public static void Subscribe() => _event += Inner;
	public static void Unsubscribe() => _event -= Inner;

	private static void Inner() { }
	private static event Action _event;
}"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void MethodGroupPassedToDelegateSubscriptionIsIgnored()
			=> _verifier
			.Source
			(
@"using System;
public static class C
{
	public static void Subscribe() => _event += Inner;
	public static void Unsubscribe() => _event -= Inner;

	private static void Inner() { }
	private static Action _event;
}"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void StaticMethodGroupWithNoParametersIsFixed()
			=> _verifier
			.Source
			(
@"using System;
public static class C
{
	public static Action GetAction() => Inner;

	private static void Inner() { }
}"
			)
			.ShouldHaveFix
			(
@"using System;
public static class C
{
	public static Action GetAction() => () => Inner();

	private static void Inner() { }
}"
			);

		[Fact]
		public void StaticMethodGroupWithSingleParameterIsFixed()
			=> _verifier
			.Source
			(
@"using System;
public static class C
{
	public static void Main()
	{
		Action<int> action = Inner;
	}

	private static void Inner( int number ) { }
}"
			)
			.ShouldHaveFix
			(
@"using System;
public static class C
{
	public static void Main()
	{
		Action<int> action = number => Inner( number );
	}

	private static void Inner( int number ) { }
}"
			);

		[Fact]
		public void StaticMethodGroupWithMultipleParameterIsFixed()
			=> _verifier
			.Source
			(
@"using System;
public static class C
{
	public static void Main() => Invoke( Inner );

	private static void Invoke( Action<int, string> action ) => action( default, default );
	private static void Inner( int number, string text ) { }
}"
			)
			.ShouldHaveFix
			(
@"using System;
public static class C
{
	public static void Main() => Invoke( ( number, text ) => Inner( number, text ) );

	private static void Invoke( Action<int, string> action ) => action( default, default );
	private static void Inner( int number, string text ) { }
}"
			);

		[Fact]
		public void InstanceMethodGroupIsFixed()
			=> _verifier
			.Source
			(
@"using System;
public class C
{
	public Action GetAction() => Inner;

	private void Inner() { }
}"
			)
			.ShouldHaveFix
			(
@"using System;
public class C
{
	public Action GetAction() => () => Inner();

	private void Inner() { }
}"
			);

		[Fact]
		public void ThisQualifiedMethodGroupIsFixed()
			=> _verifier
			.Source
			(
@"using System;
public class C
{
	public Action GetAction() => this.Inner;

	private void Inner() { }
}"
			)
			.ShouldHaveFix
			(
@"using System;
public class C
{
	public Action GetAction() => () => this.Inner();

	private void Inner() { }
}"
			);

		[Fact]
		public void TypeQualifiedMethodGroupIsFixed()
			=> _verifier
			.Source
			(
@"using System;
public class C
{
	public Action GetAction() => C.Inner;

	private static void Inner() { }
}"
			)
			.ShouldHaveFix
			(
@"using System;
public class C
{
	public Action GetAction() => () => C.Inner();

	private static void Inner() { }
}"
			);

		[Fact]
		public void MethodGroupWithSingleRefParameterIsFixed()
			=> _verifier
			.Source
			(
@"
public delegate int Parser( in string text );

public class C
{
	public Parser GetParser() => Inner;

	private static int Inner( in string text ) => 42
}"
			)
			.ShouldHaveFix
			(
@"
public delegate int Parser( in string text );

public class C
{
	public Parser GetParser() => ( in string text ) => Inner( in text );

	private static int Inner( in string text ) => 42
}"
			);

		[Fact]
		public void MethodGroupWithMultipleRefParametersIsFixed()
			=> _verifier
			.Source
			(
@"
public delegate bool Parser( in string text, bool flag, out int number );

public class C
{
	public Parser GetParser() => Inner;

	private static bool Inner( in string text, bool flag, out int number )
	{
		number = 42;
		return true;
	}
}"
			)
			.ShouldHaveFix
			(
@"
public delegate bool Parser( in string text, bool flag, out int number );

public class C
{
	public Parser GetParser() => ( in string text, bool flag, out int number ) => Inner( in text, flag, out number );

	private static bool Inner( in string text, bool flag, out int number )
	{
		number = 42;
		return true;
	}
}"
			);

		[Fact]
		public void MethodGroupWithRefReturnIsFixed()
			=> _verifier
			.Source
			(
@"
public delegate ref readonly object Allocator( int size );

public class C
{
	private static readonly object _obj = new object();

	public Allocator GetAllocator() => Inner;

	private static ref readonly object Inner( int size ) => ref _obj;
}"
			)
			.ShouldHaveFix
			(
@"
public delegate ref readonly object Allocator( int size );

public class C
{
	private static readonly object _obj = new object();

	public Allocator GetAllocator() => size => ref Inner( size );

	private static ref readonly object Inner( int size ) => ref _obj;
}"
			);

		[Fact]
		public void MethodGroupWithRefReturnAndRefParamIsFixed()
			=> _verifier
			.Source
			(
@"
public delegate ref int Updater( ref int valueRef );

public class C
{
	public Updater GetUpdater() => Inner;

	private static ref int Inner( ref int valueRef ) => ref valueRef;
}"
			)
			.ShouldHaveFix
			(
@"
public delegate ref int Updater( ref int valueRef );

public class C
{
	public Updater GetUpdater() => ( ref int valueRef ) => ref Inner( ref valueRef );

	private static ref int Inner( ref int valueRef ) => ref valueRef;
}"
			);

		[Fact]
		public void ParameterNamesDontConflictWithNamesInScope()
			=> _verifier
			.Source
			(
@"using System;
public static class C
{
	private static object flag, flag0, flag1;
	private void f();
	private void f( int param );
	private int fl { get; }
	private class fla { }

	public static void Main()
	{
		object number, text, t, te;
		void te() { }

		Invoke( Inner );
	}

	private static void Invoke( Action<int, string, bool> action ) => action( default, default, default );
	private static void Inner( int number, string text, bool flag ) { }
}"
			)
			.ShouldHaveFix
			(
@"using System;
public static class C
{
	private static object flag, flag0, flag1;
	private void f();
	private void f( int param );
	private int fl { get; }
	private class fla { }

	public static void Main()
	{
		object number, text, t, te;
		void te() { }

		Invoke( ( n, tex, flag2 ) => Inner( n, tex, flag2 ) );
	}

	private static void Invoke( Action<int, string, bool> action ) => action( default, default, default );
	private static void Inner( int number, string text, bool flag ) { }
}"
			);

		[Fact]
		public void ParameterNamesDontConflictWithEachOther()
			=> _verifier
			.Source
			(
@"using System;
public static class C
{
	public static void Main()
	{
		object intX, intY;

		Invoke( Inner );
	}

	private static void Invoke( Action<int, int> action ) => action( default, default );
	private static void Inner( int intX, int intY ) { }
}"
			)
			.ShouldHaveFix
			(
@"using System;
public static class C
{
	public static void Main()
	{
		object intX, intY;

		Invoke( ( i, @in ) => Inner( i, @in ) );
	}

	private static void Invoke( Action<int, int> action ) => action( default, default );
	private static void Inner( int intX, int intY ) { }
}"
			);

		[Fact]
		public void ParameterNamesAreEscapedIfTheyConflictWithKeywords()
			=> _verifier
			.Source
			(
@"using System;
public static class C
{
	public static void Main()
	{
		object intValue, i, @in;

		Invoke( Inner );
	}

	private static void Invoke( Action<int> action ) => action( default );
	private static void Inner( int intValue ) { }
}"
			)
			.ShouldHaveFix
			(
@"using System;
public static class C
{
	public static void Main()
	{
		object intValue, i, @in;

		Invoke( @int => Inner( @int ) );
	}

	private static void Invoke( Action<int> action ) => action( default );
	private static void Inner( int intValue ) { }
}"
			);

		[Fact]
		public void TriviaIsPreserved()
			=> _verifier
			.Source
			(
@"using System;
public static class C
{
	public static void Main()
	{
		Invoke
		(
			Inner // TODO: something
		);
	}

	private static void Invoke( Action<int> action ) => action( default );
	private static void Inner( int intValue ) { }
}"
			)
			.ShouldHaveFix
			(
@"using System;
public static class C
{
	public static void Main()
	{
		Invoke
		(
			intValue => Inner( intValue ) // TODO: something
		);
	}

	private static void Invoke( Action<int> action ) => action( default );
	private static void Inner( int intValue ) { }
}"
			);
	}
}
