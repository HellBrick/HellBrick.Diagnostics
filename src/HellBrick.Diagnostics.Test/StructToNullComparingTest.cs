﻿using System;
using System.Linq;
using HellBrick.Diagnostics.ValueTypeToNullComparing;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

using static HellBrick.Diagnostics.Test.Token;

namespace HellBrick.Diagnostics.Test
{
	internal static class Token
	{
		public static object Null { get; } = new object();
		public static object Operator { get; } = new object();
	}

	public class StructToNullComparingTest : CodeFixVerifier
	{
		protected override CodeFixProvider GetCSharpCodeFixProvider() => new ValueTypeToNullComparingCodeFixProvider();
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new ValueTypeToNullComparingAnalyzer();

		private (string Before, string After) CreateCodeStrings( FormattableString formatString, string equalityOperator = null )
		{
			return
			(
				Before: RenderCodeString( "null" ),
				After: RenderCodeString( "default" )
			);

			string RenderCodeString( string nullReplacement )
			{
				string[] arguments
					= formatString
					.GetArguments()
					.Select( originalArg => RenderArgument( originalArg ) )
					.ToArray();

				return String.Format( formatString.Format, arguments );

				string RenderArgument( object argument )
					=> argument == Null ? nullReplacement
					: argument == Operator ? ( equalityOperator ?? throw new ArgumentNullException( nameof( equalityOperator ), "The replacement equality operator was not specified." ) )
					: throw new NotSupportedException( $"'{argument}' is not a supported placeholder value." );
			}
		}

		private void VerifyNullIsReplaced( FormattableString formatString )
		{
			(string before, string after) = CreateCodeStrings( formatString );
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
			public static bool operator ==( SomeStruct x, SomeStruct y ) => true;
			public static bool operator !=( SomeStruct x, SomeStruct y ) => !( x == y );
		}}
	}}
}}";
			string test = String.Format( testCaseFormat, comparisonOperator, "null" );
			string result = String.Format( testCaseFormat, comparisonOperator, "default" );
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
			var bl = {Null} == target;
		}}

		private struct SomeStruct
		{{
			public static bool operator ==( SomeStruct x, SomeStruct y ) => true;
			public static bool operator !=( SomeStruct x, SomeStruct y ) => !( x == y );
		}}
	}}
}}";
			VerifyNullIsReplaced( reversedFormat );
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
			var bl = default ( SomeStruct ) == {Null};
		}}

		private struct SomeStruct
		{{
			public static bool operator ==( SomeStruct x, SomeStruct y ) => true;
			public static bool operator !=( SomeStruct x, SomeStruct y ) => !( x == y );
		}}
	}}
}}";
			VerifyNullIsReplaced( defaultToNullComparingFormat );
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
			var bl = target == {Null};
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
			(string before, string after) = CreateCodeStrings( externalStructFormat );
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
		public static bool operator ==( SomeStruct x, SomeStruct y ) => true;
		public static bool operator !=( SomeStruct x, SomeStruct y ) => !( x == y );
	}
}";
			VerifyCSharpFix( new[] { nullableTestCase }, new[] { nullableTestCase } );
		}

		[Fact]
		public void NullReplacedWithDefaultWhenValueIsGenericInstantiatedWithAValueType()
		{
			FormattableString codeFormat = $@"
using System;

namespace Namespace
{{
	class SomeClass
	{{
		private static bool Method()
			=> Is
			(
				new Wrapper<int>(),
				x => x.Value != {Null}
			);

		private static bool Is<T>( T value, Func<T, bool> predicate ) => predicate( value );

		private class Wrapper<T>
		{{
			public T Value {{ get; set; }}
		}}
	}}
}}";
			VerifyNullIsReplaced( codeFormat );
		}
	}
}
