using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HellBrick.Diagnostics.ValueTypeToNullComparing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

namespace HellBrick.Diagnostics.Test
{
	public class StructToNullComparingTest : CodeFixVerifier
	{
		protected override CodeFixProvider GetCSharpCodeFixProvider() => new ValueTypeToNullComparingCodeFixProvider();
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new ValueTypeToNullComparingAnalyzer();

		private (string Before, string After) CreateCodeStrings( FormattableString formatString, string type )
			=>
			(
				Before: String.Format( formatString.Format, "null" ),
				After: String.Format( formatString.Format, $"default( {type} )" )
			);

		private void VerifyNullIsReplaced( FormattableString formatString, string type )
		{
			(string before, string after) = CreateCodeStrings( formatString, type );
			VerifyCSharpFix( before, after );
		}

		[Theory]
		[InlineData( "==" )]
		[InlineData( "!=" )]
		public void NullReplacedWithDefault( string comparisonOperator )
		{
			const string testCaseFormat = @"
using System;

namespace ConsoleApplication1
{{
	class TypeName
	{{
		public void M()
		{{
			SomeStruct target = default( SomeStruct );
			var bl = target {0} {1};
		}}

		private struct SomeStruct
		{{
		}}
	}}
}}";
			string test = string.Format( testCaseFormat, comparisonOperator, "null" );
			string result = string.Format( testCaseFormat, comparisonOperator, "default( SomeStruct )" );
			VerifyCSharpFix( test, result );
		}

		[Fact]
		public void NullReplacedWithDefaultsStatementWhenNullIsOnTheLeft()
		{
			FormattableString reversedFormat = $@"
using System;

namespace ConsoleApplication1
{{
	class TypeName
	{{
		public void M()
		{{
			SomeStruct target = default( SomeStruct );
			var bl = {0} == target;
		}}

		private struct SomeStruct
		{{
		}}
	}}
}}";
			VerifyNullIsReplaced( reversedFormat, "SomeStruct" );
		}

		[Fact]
		public void NullReplacedWithDefaultStatementWhenDefaultToNullCompared()
		{
			FormattableString defaultToNullComparingFormat = $@"
using System;

namespace ConsoleApplication1
{{
	class TypeName
	{{
		public void M()
		{{
			SomeStruct target = default( SomeStruct );
			var bl = default ( SomeStruct ) == {0};
		}}

		private struct SomeStruct
		{{
		}}
	}}
}}";
			VerifyNullIsReplaced( defaultToNullComparingFormat, "SomeStruct" );
		}

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
			var bl = target == {0};
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
			(string before, string after) = CreateCodeStrings( externalStructFormat, "ValueTypes.EmptyStruct" );
			VerifyCSharpFix( new[] { before, emptyStructFile, emptyStructFactoryFile }, new[] { after, emptyStructFile, emptyStructFactoryFile } );
		}

		[Fact]
		public void NullableStructAnalysysSkipped()
		{
			const string nullableTestCase = @"
using System;
using ValueTypes;

namespace ConsoleApplication1
{
	class SomeClass
	{
		public void M()
		{
			EmptyStruct? target = new EmptyStruct()
			var booooooool = target == null;
		}
	}

	public struct EmptyStruct
	{
	}
}";
			VerifyCSharpFix( new[] { nullableTestCase }, new[] { nullableTestCase } );
		}

		[Fact]
		public void RoslynBugIsWorkedAround()
		{
			const string code = @"
using System;

namespace RoslynBug
{
	class SomeClass
	{
		private static bool Method()
			=> Is
			(
				new Wrapper<int>(),
				x => x.Value != null
			);

		private static bool Is<T>( T value, Func<T, bool> predicate ) => predicate( value );

		private class Wrapper<T>
		{
			public T Value { get; set; }
		}
	}
}";
			VerifyNoFix( code );
		}
	}
}