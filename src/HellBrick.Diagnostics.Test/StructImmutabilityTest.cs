using HellBrick.Diagnostics.StructDeclarations;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

namespace HellBrick.Diagnostics.Test
{
	public class StructImmutabilityTest : CodeFixVerifier
	{
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new StructAnalyzer();
		protected override CodeFixProvider GetCSharpCodeFixProvider() => new StructImmutabilityCodeFixProvider();

		[Fact]
		public void ReadonlyStructIsIgnored()
		{
			const string source =
@"
public readonly struct AlreadyReadonly
{
	public int Property { get; }
}
";
			VerifyNoFix( source );
		}

		[Fact]
		public void RefStructIsIgnored()
		{
			const string source =
@"
public ref struct RefStruct
{
	public int Property { get; }
}
";
			VerifyNoFix( source );
		}

		[Fact]
		public void MutableStructIsIgnored()
		{
			const string source =
@"
public struct MutableStruct
{
	private int _currentIndex;
}
";
			VerifyNoFix( source );
		}

		[Fact]
		public void ImmutableStructWithFieldsOnlyGetsReadonlyModifier()
		{
			const string before =
@"
public struct ImmutableStruct
{
	private readonly int _field;
}
";
			const string after =
@"
public readonly struct ImmutableStruct
{
	private readonly int _field;
}
";
			VerifyCSharpFix( before, after );
		}

		[Fact]
		public void ImmutableStructWithPropertiesOnlyGetsReadonlyModifier()
		{
			const string before =
@"
public struct ImmutableStruct
{
	public int Property { get; }
}
";
			const string after =
@"
public readonly struct ImmutableStruct
{
	public int Property { get; }
}
";
			VerifyCSharpFix( before, after );
		}

		[Fact]
		public void ImmutableStructWithBothFieldsAndPropertiesGetsReadonlyModifier()
		{
			const string before =
@"
public struct ImmutableStruct
{
	private readonly int _field;
	public int Property { get; }
}
";
			const string after =
@"
public readonly struct ImmutableStruct
{
	private readonly int _field;
	public int Property { get; }
}
";
			VerifyCSharpFix( before, after );
		}
	}
}
