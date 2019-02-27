using System;
using System.Linq;
using HellBrick.Diagnostics.Assertions;
using HellBrick.Diagnostics.ValueTypeToNullComparing;
using Xunit;

using static HellBrick.Diagnostics.Test.Token;

namespace HellBrick.Diagnostics.Test
{
	internal static class Token
	{
		public static object Null { get; } = new object();
		public static object Operator { get; } = new object();
	}

#pragma warning disable HBStructEquatabilityMethodsMissing // Structs should provide equatability methods 
	internal readonly struct StructNullComparisonAnalyzerVerifier
	{
		private readonly string _equalityOperator;

		public StructNullComparisonAnalyzerVerifier( string equalityOperator )
			=> _equalityOperator = equalityOperator ?? throw new ArgumentNullException( nameof( equalityOperator ), "The replacement equality operator was not specified." );

		public SourceStructNullComparisonAnalyzerVerifier Source( FormattableString source, params string[] additionalSources )
			=> new SourceStructNullComparisonAnalyzerVerifier( _equalityOperator, source, additionalSources );
	}

	internal readonly struct SourceStructNullComparisonAnalyzerVerifier
	{
		private readonly AnalyzerVerifier<ValueTypeToNullComparingAnalyzer, ValueTypeToNullComparingCodeFixProvider, string[], MultiSourceCollectionFactory> _verifier;
		private readonly string[] _sourcesWithNullReplaced;

		public SourceStructNullComparisonAnalyzerVerifier( string equalityOperator, FormattableString source, string[] additionalSources )
		{
			(string originalSource, string sourceWithNullReplaced) = CreateCodeStrings();

			_verifier
				= AnalyzerVerifier
				.UseAnalyzer<ValueTypeToNullComparingAnalyzer>()
				.UseCodeFix<ValueTypeToNullComparingCodeFixProvider>()
				.Sources( Concat( originalSource, additionalSources ) );

			_sourcesWithNullReplaced = Concat( sourceWithNullReplaced, additionalSources );

			(string Before, string After) CreateCodeStrings()
			{
				return
				(
					Before: RenderCodeString( "null" ),
					After: RenderCodeString( "default" )
				);

				string RenderCodeString( string nullReplacement )
				{

					string[] arguments
						= source
						.GetArguments()
						.Select( originalArg => RenderArgument( originalArg ) )
						.ToArray();

					return String.Format( source.Format, arguments );

					string RenderArgument( object argument )
						=> argument == Null ? nullReplacement
						: argument == Operator ? equalityOperator
						: throw new NotSupportedException( $"'{argument}' is not a supported placeholder value." );
				}
			}

			T[] Concat<T>( T firstItem, T[] otherItems )
			{
				T[] result = new T[ otherItems.Length + 1 ];
				result[ 0 ] = firstItem;
				otherItems.CopyTo( result, 1 );
				return result;
			}
		}

		public void ShouldHaveNoDiagnostics() => _verifier.ShouldHaveNoDiagnostics();
		public void ShouldHaveNullReplacedWithDefault() => _verifier.ShouldHaveFix( _sourcesWithNullReplaced );
	}
#pragma warning restore HBStructEquatabilityMethodsMissing // Structs should provide equatability methods 

	public abstract class StructToNullComparingTest
	{
		private readonly StructNullComparisonAnalyzerVerifier _verifier;

		protected StructToNullComparingTest( string equalityOperator )
			=> _verifier = new StructNullComparisonAnalyzerVerifier( equalityOperator );

		[Fact]
		public void NullReplacedWithDefault()
			=> _verifier
			.Source
			(
$@"
using System;

namespace ConsoleApplication1
{{
	class TypeName
	{{
		public void M()
		{{
			SomeStruct target = default( SomeStruct );
			var bl = target {Operator} {Null};
		}}

		private struct SomeStruct
		{{
			public static bool operator ==( SomeStruct x, SomeStruct y ) => true;
			public static bool operator !=( SomeStruct x, SomeStruct y ) => !( x == y );
		}}
	}}
}}"
			)
			.ShouldHaveNullReplacedWithDefault();

		[Fact]
		public void NullReplacedWithDefaultsStatementWhenNullIsOnTheLeft()
			=> _verifier
			.Source
			(
$@"
using System;

namespace ConsoleApplication1
{{
	class TypeName
	{{
		public void M()
		{{
			SomeStruct target = default( SomeStruct );
			var bl = {Null} {Operator} target;
		}}

		private struct SomeStruct
		{{
			public static bool operator ==( SomeStruct x, SomeStruct y ) => true;
			public static bool operator !=( SomeStruct x, SomeStruct y ) => !( x == y );
		}}
	}}
}}"
			)
			.ShouldHaveNullReplacedWithDefault();

		[Fact]
		public void NullReplacedWithDefaultStatementWhenDefaultToNullCompared()
			=> _verifier
			.Source
			(
$@"
using System;

namespace ConsoleApplication1
{{
	class TypeName
	{{
		public void M()
		{{
			SomeStruct target = default( SomeStruct );
			var bl = default ( SomeStruct ) {Operator} {Null};
		}}

		private struct SomeStruct
		{{
			public static bool operator ==( SomeStruct x, SomeStruct y ) => true;
			public static bool operator !=( SomeStruct x, SomeStruct y ) => !( x == y );
		}}
	}}
}}"
			)
			.ShouldHaveNullReplacedWithDefault();

		[Fact]
		public void NamespacePrefixIsAddedIfTargetingStructIsOutOfCurrentNamespace()
		{
			FormattableString externalStructFormat = $@"
using System;
using ThridParty;

namespace ConsoleApplication1
{{
	class SomeClass
	{{
		public void M()
		{{
			var target = EmptyStructFactory.CreateDefaultEmptyStruct();
			var bl = target {Operator} {Null};
		}}
	}}
}}
";
			const string emptyStructFile = @"
using System;

namespace ValueTypes
{
	public struct EmptyStruct
	{
		public static bool operator ==( EmptyStruct x, EmptyStruct y ) => true;
		public static bool operator !=( EmptyStruct x, EmptyStruct y ) => !( x == y );
	}
}";
			const string emptyStructFactoryFile = @"
using System;
using ValueTypes;

namespace ThridParty
{
	public static class EmptyStructFactory
	{
		public static EmptyStruct CreateDefaultEmptyStruct() => default( EmptyStruct );
	}
}";

			_verifier
				.Source( externalStructFormat, emptyStructFile, emptyStructFactoryFile )
				.ShouldHaveNullReplacedWithDefault();
		}

		[Fact]
		public void NullableStructAnalysysSkipped()
			=> _verifier
			.Source
			(
$@"
using System;
using ValueTypes;

namespace ConsoleApplication1
{{
	class SomeClass
	{{
		public void M()
		{{
			EmptyStruct? target = new EmptyStruct()
			var booooooool = target {Operator} {Null};
		}}
	}}

	public struct EmptyStruct
	{{
		public static bool operator ==( SomeStruct x, SomeStruct y ) => true;
		public static bool operator !=( SomeStruct x, SomeStruct y ) => !( x == y );
	}}
}}"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void NullReplacedWithDefaultWhenValueIsGenericInstantiatedWithAValueType()
			=> _verifier
			.Source
			(
$@"
using System;

namespace Namespace
{{
	class SomeClass
	{{
		private static bool Method()
			=> Is
			(
				new Wrapper<int>(),
				x => x.Value {Operator} {Null}
			);

		private static bool Is<T>( T value, Func<T, bool> predicate ) => predicate( value );

		private class Wrapper<T>
		{{
			public T Value {{ get; set; }}
		}}
	}}
}}"
			)
			.ShouldHaveNullReplacedWithDefault();
	}

	public class StructEqualsNullTest : StructToNullComparingTest
	{
		public StructEqualsNullTest() : base( "==" ) { }
	}

	public class StructNotEqualsNullTest : StructToNullComparingTest
	{
		public StructNotEqualsNullTest() : base( "!=" ) { }
	}
}
