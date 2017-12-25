using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HellBrick.Diagnostics.ConfigureAwait;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

namespace HellBrick.Diagnostics.Test
{
	public class ConfigureAwaitTest : CodeFixVerifier
	{
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new TaskAwaiterAnalyzer();
		protected override CodeFixProvider GetCSharpCodeFixProvider() => new TaskAwaiterCodeFixProvider();

		private void VerifyFix( string before, FormattableString afterTemplate )
		{
			VerifyCSharpFix( before, String.Format( afterTemplate.Format, "false" ), codeFixIndex: 0 );
			VerifyCSharpFix( before, String.Format( afterTemplate.Format, "true" ), codeFixIndex: 1 );
		}

		[Fact]
		public void AlreadyConfiguredAwaitIsIgnored()
		{
			const string source =
@"
using System.Threading.Tasks;
public class Program
{
	public async void AsyncMethod()
	{
		await Task.Delay( 100 ).ConfigureAwait( false );
		await Task.Delay( 100 ).ConfigureAwait( true );
	}
}
";
			VerifyNoFix( source );
		}

		[Fact]
		public void InconfigurableAwaitIsIgnored()
		{
			const string source =
@"
using System.Threading.Tasks;
public class Program
{
	public async void AsyncMethod()
	{
		await Task.Yield();
	}
}
";
			VerifyNoFix( source );
		}

		[Fact]
		public void IncorrectAwaitIsIgnored()
		{
			const string source =
@"
using System.Threading.Tasks;
public class Program
{
	public async void AsyncMethod()
	{
		await ( 2 + 2 );
	}
}
";
			VerifyNoFix( source );
		}

		[Fact]
		public void UnconfiguredTaskGetsConfigured()
		{
			const string before =
@"
using System.Threading.Tasks;
public class Program
{
	public async void AsyncMethod()
	{
		await Task.FromResult( 42 );
	}
}
";
			FormattableString after =
$@"
using System.Threading.Tasks;
public class Program
{{
	public async void AsyncMethod()
	{{
		await Task.FromResult( 42 ).ConfigureAwait( {false} );
	}}
}}
";
			VerifyFix( before, after );
		}
	}
}
