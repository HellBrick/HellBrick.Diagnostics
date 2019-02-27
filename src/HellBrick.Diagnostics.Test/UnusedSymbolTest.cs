using HellBrick.Diagnostics.Assertions;
using HellBrick.Diagnostics.DeadCode;
using Xunit;

namespace HellBrick.Diagnostics.Test
{
	public class UnusedSymbolTest
	{
		private readonly AnalyzerVerifier<UnusedSymbolAnalyzer, UnusedSymbolCodeFixProvider> _verifier
			= AnalyzerVerifier
			.UseAnalyzer<UnusedSymbolAnalyzer>()
			.UseCodeFix<UnusedSymbolCodeFixProvider>();

		[Fact]
		public void UnusedPrivateMemberIsRemoved()
			=> _verifier
			.Source
			(
@"public class C
{
	private int _number;
}"
			)
			.ShouldHaveFix
			(
@"public class C
{
}"
			);

		[Fact]
		public void UnusedPublicMemberIsNotRemoved()
			=> _verifier
			.Source
			(
@"public class C
{
	public void Whatever();
}"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void UnusedInternalMemberIsRemoved()
			=> _verifier
			.Source
			(
@"public class C
{
	internal string Text { get; set; }
}"
			)
			.ShouldHaveFix
			(
@"public class C
{
}"
			);

		[Fact]
		public void UsedPrivateMemberIsNotRemoved()
			=> _verifier
			.Source
			(
@"public class C
{
	private int _number;
	public C()
	{
		_number = 42;
	}
}"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void PrivateMemberReferencedByNameofIsNotRemoved()
			=> _verifier
			.Source
			(
@"
public class C
{
	private int _field;
	private string Property => ""text"";
	private void Method() {}

	public string ConcatStuff()
		=> nameof( _field )
		+ nameof( Property )
		+ nameof( Method );
}
"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void UsedInternalMemberIsNotRemoved()
		{
			const string source1 =
@"public class C
{
	internal string Text { get; set; }
}";
			const string source2 =
@"public class D
{
	public string GetText() => new C().Text;
}";

			_verifier.Sources( source1, source2 ).ShouldHaveNoDiagnostics();
		}

		[Fact]
		public void UnusedInternalMemberIsNotRemovedIfHasInternalsVisibleTo()
			=> _verifier
			.Source
			(
@"using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo( ""Tests.dll"" )]

public class C
{
	internal string Text { get; set; }
}"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void PrivateMemberUsedByAnotherFileOfPartialClassIsNotRemoved()
		{
			const string source1 =
@"public partial class C
{
	private int _number;
}";
			const string source2 =
@"public partial class C
{
	public int Number => _number;
}";

			_verifier.Sources( source1, source2 ).ShouldHaveNoDiagnostics();
		}
	}
}
