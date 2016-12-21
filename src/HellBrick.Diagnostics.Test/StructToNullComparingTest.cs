using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HellBrick.Diagnostics.ValueTypeToNullComparing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace HellBrick.Diagnostics.Test
{
	[TestClass]
	public class StructToNullComparingTest : CodeFixVerifier
	{
		protected override CodeFixProvider GetCSharpCodeFixProvider() => new ValueTypeToNullComparingCodeFixProvider();
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new ValueTypeToNullComparingAnalyzer();

		private const string _testCaseFormat = @"
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
		[TestMethod]
		public void NullReplacedWithDefaultForEqualsStatement()
		{
			const string equals = "==";
			string test = string.Format( _testCaseFormat, equals, "null" );
			string result = string.Format( _testCaseFormat, equals, "default( SomeStruct )" );
			VerifyCSharpFix( test, result );
		}

		[TestMethod]
		public void NullReplacedWithDefaultForNonEqualsStatement()
		{
			const string equals = "!=";
			string test = string.Format( _testCaseFormat, equals, "null" );
			string result = string.Format( _testCaseFormat, equals, "default( SomeStruct )" );
			VerifyCSharpFix( test, result );
		}
		private const string _reversedFormat = @"
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
		[TestMethod]
		public void NullReplacedWithDefaultsStatementWhenNullIsOnTheLeft()
		{
			string test = string.Format( _reversedFormat, "null" );
			string result = string.Format( _reversedFormat, "default( SomeStruct )" );
			VerifyCSharpFix( test, result );
		}

		private const string _defaultToNullComparingFormat = @"
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

		[TestMethod]
		public void NullReplacedWithDefaultStatementWhenDefaultToNullCompared()
		{
			string test = string.Format( _defaultToNullComparingFormat, "null" );
			string result = string.Format( _defaultToNullComparingFormat, "default( SomeStruct )" );
			VerifyCSharpFix( test, result );
		}

		private const string _externalStructFormat = @"
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

		private const string _emptyStructFile = @"
using System;

namespace ValueTypes
{
	public struct EmptyStruct
	{
	}
}";


		private const string _emptyStructFactoryFile = @"
using System;
using ValueTypes;

namespace ThridParty
{
	public static class EmptyStructFactory
	{
		public static EmptyStruct CreateDefaultEmptyStruct() => default( EmptyStruct );
	}
}";

		[TestMethod]
		public void NamespacePrefixIsAddedIfTargetingStructIsOutOfCurrentNamespace()
		{
			string test = string.Format( _externalStructFormat, "null" );
			string result = string.Format( _externalStructFormat, "default( ValueTypes.EmptyStruct )" );
			VerifyCSharpFix( new[] { test, _emptyStructFile, _emptyStructFactoryFile }, new[] { result, _emptyStructFile, _emptyStructFactoryFile } );
		}

		private const string _nullableTestCase = @"
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

		[TestMethod]
		public void NullableStructAnalysysSkipped()
		{
			VerifyCSharpFix( new[] { _nullableTestCase }, new[] { _nullableTestCase } );
		}
	}
}