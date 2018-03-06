using System;
using HellBrick.Diagnostics.ConfigureAwait;
using HellBrick.Diagnostics.Assertions;
using Xunit;

namespace HellBrick.Diagnostics.Test
{
	internal static partial class VerifierExtensions
	{
		public static AnalyzerVerifier<TaskAwaiterAnalyzer, TaskAwaiterCodeFixProvider, string, SingleSourceCollectionFactory> ShouldHaveConfigureAwaitFixed
		(
			this AnalyzerVerifier<TaskAwaiterAnalyzer, TaskAwaiterCodeFixProvider, string, SingleSourceCollectionFactory> verifier,
			FormattableString fixedSourceTemplate
		)
			=> verifier
			.ShouldHaveFix( 0, String.Format( fixedSourceTemplate.Format, "false" ) )
			.ShouldHaveFix( 1, String.Format( fixedSourceTemplate.Format, "true" ) );
	}

	public class ConfigureAwaitTest
	{
		private readonly AnalyzerVerifier<TaskAwaiterAnalyzer, TaskAwaiterCodeFixProvider> _verifier
				= AnalyzerVerifier
				.UseAnalyzer<TaskAwaiterAnalyzer>()
				.UseCodeFix<TaskAwaiterCodeFixProvider>();

		[Fact]
		public void AlreadyConfiguredAwaitIsIgnored()
			=> _verifier
			.Source
			(
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
"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void InconfigurableAwaitIsIgnored()
			=> _verifier
			.Source
			(
@"
using System.Threading.Tasks;
public class Program
{
	public async void AsyncMethod()
	{
		await Task.Yield();
	}
}
"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void IncorrectAwaitIsIgnored()
			=> _verifier
			.Source
			(
@"
using System.Threading.Tasks;
public class Program
{
	public async void AsyncMethod()
	{
		await ( 2 + 2 );
	}
}
"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void UnconfiguredTaskGetsConfigured()
			=> _verifier
			.Source
			(
@"
using System.Threading.Tasks;
public class Program
{
	public async void AsyncMethod()
	{
		await Task.FromResult( 42 );
	}
}
"
			)
			.ShouldHaveConfigureAwaitFixed
			(
$@"
using System.Threading.Tasks;
public class Program
{{
	public async void AsyncMethod()
	{{
		await Task.FromResult( 42 ).ConfigureAwait( {false} );
	}}
}}
"
			);

		[Fact]
		public void UnconfiguredCustomTaskGetsConfigured()
			=> _verifier
			.Source
			(
@"
using System.Threading.Tasks;

public class CustomTask
{
	public ConfiguredCustomTaskAwaiter ConfigureAwait( bool ) => default;
}

public class Program
{
	public async void AsyncMethod()
	{
		await new CustomTask();
	}
}
"
			)
			.ShouldHaveConfigureAwaitFixed
			(
$@"
using System.Threading.Tasks;

public class CustomTask
{{
	public ConfiguredCustomTaskAwaiter ConfigureAwait( bool ) => default;
}}

public class Program
{{
	public async void AsyncMethod()
	{{
		await new CustomTask().ConfigureAwait( {false} );
	}}
}}
"
			);
	}
}
