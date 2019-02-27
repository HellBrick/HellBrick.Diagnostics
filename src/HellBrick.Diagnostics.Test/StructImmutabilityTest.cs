using HellBrick.Diagnostics.Assertions;
using HellBrick.Diagnostics.StructDeclarations;
using Xunit;

namespace HellBrick.Diagnostics.Test
{
	public class StructImmutabilityTest
	{
		private readonly AnalyzerVerifier<StructAnalyzer, StructImmutabilityCodeFixProvider> _verifier
			= AnalyzerVerifier
			.UseAnalyzer<StructAnalyzer>()
			.UseCodeFix<StructImmutabilityCodeFixProvider>();

		[Fact]
		public void ReadonlyStructIsIgnored()
			=> _verifier
			.Source
			(
@"
public readonly struct AlreadyReadonly
{
	public int Property { get; }
}
"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void RefStructIsIgnored()
			=> _verifier
			.Source
			(
@"
public ref struct RefStruct
{
	public int Property { get; }
}
"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void MutableStructIsIgnored()
			=> _verifier
			.Source
			(
@"
public struct MutableStruct
{
	private int _currentIndex;
}
"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void ImmutableStructWithFieldsOnlyGetsReadonlyModifier()
			=> _verifier
			.Source
			(
@"
public struct ImmutableStruct
{
	private readonly int _field;
}
"
			)
			.ShouldHaveFix
			(
@"
public readonly struct ImmutableStruct
{
	private readonly int _field;
}
"
			);

		[Fact]
		public void ImmutableStructWithPropertiesOnlyGetsReadonlyModifier()
			=> _verifier
			.Source
			(
@"
public struct ImmutableStruct
{
	public int Property { get; }
}
"
			)
			.ShouldHaveFix
			(
@"
public readonly struct ImmutableStruct
{
	public int Property { get; }
}
"
			);

		[Fact]
		public void ImmutableStructWithBothFieldsAndPropertiesGetsReadonlyModifier()
			=> _verifier
			.Source
			(
@"
public struct ImmutableStruct
{
	private readonly int _field;
	public int Property { get; }
}
"
			)
			.ShouldHaveFix
			(
@"
public readonly struct ImmutableStruct
{
	private readonly int _field;
	public int Property { get; }
}
"
			);
	}
}
