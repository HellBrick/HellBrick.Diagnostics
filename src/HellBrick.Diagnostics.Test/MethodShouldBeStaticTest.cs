using HellBrick.Diagnostics.Assertions;
using HellBrick.Diagnostics.EnforceStatic;
using HellBrick.Diagnostics.Utils;
using Microsoft.CodeAnalysis.CodeStyle;
using Xunit;

namespace HellBrick.Diagnostics.Test
{
	public class MethodShouldBeStaticTest
	{
		private readonly AnalyzerVerifier<MethodShouldBeStaticAnalyzer, MethodShouldBeStaticCodeFixProvider> _verifier
			= AnalyzerVerifier
			.UseAnalyzer<MethodShouldBeStaticAnalyzer>()
			.UseCodeFix<MethodShouldBeStaticCodeFixProvider>();

		[Fact]
		public void ConstructorIsIgnored()
			=> _verifier
			.Source
			(
@"public class C
{
	private C()
		: this( 42 )
	{
	}

	private C( int value )
	{
	}
}"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void PartialMethodIsIgnored()
			=> _verifier
			.Source
			(
@"public class C
{
	private partial void PartialMethod() { }
}"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void NonPrivateMethodIsIgnored()
			=> _verifier
			.Source
			(
@"public class C
{
	public void PublicMethod() { }
	internal void InternalMethod() { }
	protected void ProtectedMethod() { }
	internal protected void InternalProtectedMethod() { }
	private protected void PrivateProtectedMethod() { }
}"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void LocalMethodIsIgnored()
			=> _verifier
			.Source
			(
@"public class C
{
	private static void PrivateMethod()
	{
		void LocalMethod() { }
	}
}"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void ExplicitMethodImplementationIsIgnored()
			=> _verifier
			.Source
			(
@"public class C : System.IEquatable<object>
{
	bool System.IEquatable<object>.Equals( object other ) => false;
}"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void EmptyMethodIsFixed()
			=> _verifier
			.Source
			(
@"public class C
{
	private void EmptyMethod() { }
}"
			)
			.ShouldHaveFix
			(
@"public class C
{
	private static void EmptyMethod() { }
}"
			);

		[Fact]
		public void MethodThatReferencesAnyMembersByNameofIsFixed()
			=> _verifier
			.Source
			(
@"public class C
{
	private string NameofReference()
		=> nameof( _staticField )
		+ nameof( _instanceField )
		+ nameof( _staticProperty )
		+ nameof( _instanceProperty )
		+ nameof( StaticMethod )
		+ nameof( InstanceMethod )
		+ nameof( GetType )
		+ nameof( StaticEvent )
		+ nameof( InstanceEvent );

	private static int _staticField = 42;
	private int _instanceField = 64;

	private static string _staticProperty = ""42"";
	private string _instanceProperty = ""64"";

	private static int StaticMethod() => 128;
	private string InstanceMethod() => base.GetType();

	private static System.Action StaticEvent;
	private System.Action InstanceEvent;
}"
			)
			.ShouldHaveFix
			(
@"public class C
{
	private static string NameofReference()
		=> nameof( _staticField )
		+ nameof( _instanceField )
		+ nameof( _staticProperty )
		+ nameof( _instanceProperty )
		+ nameof( StaticMethod )
		+ nameof( InstanceMethod )
		+ nameof( GetType )
		+ nameof( StaticEvent )
		+ nameof( InstanceEvent );

	private static int _staticField = 42;
	private int _instanceField = 64;

	private static string _staticProperty = ""42"";
	private string _instanceProperty = ""64"";

	private static int StaticMethod() => 128;
	private string InstanceMethod() => base.GetType();

	private static System.Action StaticEvent;
	private System.Action InstanceEvent;
}"
			);

		[Fact]
		public void MethodThatReferencesStaticFieldIsFixed()
			=> _verifier
			.Source
			(
@"public class C
{
	private string StaticFieldReference() => _field.ToString();

	private static int _field;
}"
			)
			.ShouldHaveFix
			(
@"public class C
{
	private static string StaticFieldReference() => _field.ToString();

	private static int _field;
}"
			);

		[Fact]
		public void MethodThatReferencesStaticPropertyIsFixed()
			=> _verifier
			.Source
			(
@"public class C
{
	private string StaticPropertyReference() => ( ( () => Property ) as System.Action )();

	public static string Property { get; }
}"
			)
			.ShouldHaveFix
			(
@"public class C
{
	private static string StaticPropertyReference() => ( ( () => Property ) as System.Action )();

	public static string Property { get; }
}"
			);

		[Fact]
		public void MethodThatReferencesStaticMethodIsFixed()
			=> _verifier
			.Source
			(
@"public class C
{
	private string StaticMethodReference() => AnotherMethod();

	private static int AnotherMethod() => 42;
}"
			)
			.ShouldHaveFix
			(
@"public class C
{
	private static string StaticMethodReference() => AnotherMethod();

	private static int AnotherMethod() => 42;
}"
			);

		[Fact]
		public void MethodThatReferencesStaticMethodGroupIsFixed()
			=> _verifier
			.Source
			(
@"public class C
{
	private System.Func<int> StaticMethodGroupReference() => AnotherMethod;

	private static int AnotherMethod() => 42;
}"
			)
			.ShouldHaveFix
			(
@"public class C
{
	private static System.Func<int> StaticMethodGroupReference() => AnotherMethod;

	private static int AnotherMethod() => 42;
}"
			);

		[Fact]
		public void MethodThatReferencesStaticMethodByLambdaIsFixed()
			=> _verifier
			.Source
			(
@"public class C
{
	private System.Func<int> StaticMethodLambdaReference() => () => AnotherMethod();

	private static int AnotherMethod() => 42;
}"
			)
			.ShouldHaveFix
			(
@"public class C
{
	private static System.Func<int> StaticMethodLambdaReference() => () => AnotherMethod();

	private static int AnotherMethod() => 42;
}"
			);

		[Fact]
		public void MethodThatReferencesStaticEventIsFixed()
			=> _verifier
			.Source
			(
@"public class C
{
	private string StaticEventReference() => Event += () => { };

	private static System.Action Event;
}"
			)
			.ShouldHaveFix
			(
@"public class C
{
	private static string StaticEventReference() => Event += () => { };

	private static System.Action Event;
}"
			);

		[Fact]
		public void MethodThatReferencesInstanceFieldIsIgnored()
			=> _verifier
			.Source
			(
@"public class C
{
	private int InstanceFieldReference() => _field;

	private int _field;
}"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void MethodThatReferencesInstancePropertyIsIgnored()
			=> _verifier
			.Source
			(
@"public class C
{
	private string InstancePropertyReference() => Number.ToString();

	public int Number { get; }
}"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void MethodThatReferencesInstanceMethodIsIgnored()
			=> _verifier
			.Source
			(
@"public class C
{
	private int InstanceMethodReference() => AnotherMethod() * 2;

	private int AnotherMethod() => _theAnswer;
	private readonly int _theAnswer = 42;
}"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void MethodThatReferencesInstanceMethodGroupIsIgnored()
			=> _verifier
			.Source
			(
@"public class C
{
	private System.Func<string> StaticMethodGroupReference() => AnotherMethod;

	private string AnotherMethod() => base.GetType();
}"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void MethodThatReferencesInstanceMethodByLambdaIsIgnored()
			=> _verifier
			.Source
			(
@"public class C
{
	private System.Func<string> StaticMethodLambdaReference() => () => AnotherMethod();

	private string AnotherMethod() => this.GetType();
}"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void MethodThatReferencesInstantEventIsFixed()
			=> _verifier
			.Source
			(
@"public class C
{
	private string StaticEventReference() => Event += () => { };

	private System.Action Event;
}"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void ModifierIsInsertedIfThereAreNoOtherModifiers()
			=> _verifier
			.Source
			(
@"public class C
{
	void Method() { }
}"
			)
			.ShouldHaveFix
			(
@"public class C
{
	static void Method() { }
}"
			);

		[Fact]
		public void ModifierIsInsertedAtCorrectPlaceByDefault()
			=> _verifier
			.Source
			(
@"public class C
{
	private async unsafe void AsyncMethod() { }
}"
			)
			.ShouldHaveFix
			(
@"public class C
{
	private static async unsafe void AsyncMethod() { }
}"
			);

		[Fact]
		public void ModifierIsInsertedAtCorrectPlaceIfCustomOrderIsSpecified()
			=> _verifier
			.WithOptions
			(
				os => os.WithChangedOption
				(
					CSharpCodeStyleOptions.PreferredModifierOrder,
					new CodeStyleOption<string>( "unsafe, async, private, static", NotificationOption.Silent )
				)
			)
			.Source
			(
@"public class C
{
	unsafe async private void AsyncMethod() { }
}"
			)
			.ShouldHaveFix
			(
@"public class C
{
	unsafe async private static void AsyncMethod() { }
}"
			);

		[Fact]
		public void ModifierIsInsertedAfterLastPrecedingModifierIfOrderIsMessedUp()
			=> _verifier
			.WithOptions
			(
				os => os.WithChangedOption
				(
					CSharpCodeStyleOptions.PreferredModifierOrder,
					new CodeStyleOption<string>( "private, static, unsafe, async", NotificationOption.Silent )
				)
			)
			.Source
			(
@"public class C
{
	unsafe private async void AsyncMethod() { }
}"
			)
			.ShouldHaveFix
			(
@"public class C
{
	unsafe private static async void AsyncMethod() { }
}"
			);
	}
}
