using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HellBrick.Diagnostics.StringInterpolation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HellBrick.Diagnostics.Test
{
	[TestClass]
	public class StringFormatToStringInterpolationRefactoringTest
	{
		private StringFormatToStringInterpolationRefactoring _refactoringProvider;

		[TestInitialize]
		public void Initialize()
		{
			_refactoringProvider = new StringFormatToStringInterpolationRefactoring();
      }

		[TestMethod]
		public void CallWithoutAlignmentOrFormatIsConverted()
		{
			const string sourceCode = @"var x = System.String.Format( ""asdf {0} qwer"", 42 );";
			const string expectedCode = @"var x = $""asdf {42} qwer"";";
			_refactoringProvider.ShouldProvideRefactoring( sourceCode, expectedCode );
		}

		[TestMethod]
		public void CallWithFormatIsConverted()
		{
			const string sourceCode = @"var x = System.String.Format( ""{0:g2}"", 42 );";
			const string expectedCode = @"var x = $""{42:g2}"";";
			_refactoringProvider.ShouldProvideRefactoring( sourceCode, expectedCode );
		}

		[TestMethod]
		public void CallWithAlignmentIsConverted()
		{
			const string sourceCode = @"var x = System.String.Format( ""{0,5}"", 42 );";
			const string expectedCode = @"var x = $""{42,5}"";";
			_refactoringProvider.ShouldProvideRefactoring( sourceCode, expectedCode );
		}

		[TestMethod]
		public void CallWithAlignmentAndFormatIsConverted()
		{
			const string sourceCode = @"var x = System.String.Format( ""{0,5:g2}"", 42 );";
			const string expectedCode = @"var x = $""{42,5:g2}"";";
			_refactoringProvider.ShouldProvideRefactoring( sourceCode, expectedCode );
		}

		[TestMethod]
		public void CallWithoutConstantFormatIsNotConverted()
		{
			const string sourceCode = @"string f = ""{0}""; var x = System.String.Format( f, 42 );";
			_refactoringProvider.ShouldNotProvideRefactoring( sourceCode );
		}

		[TestMethod]
		public void CallWithIncorrectFormatIsNotConverted()
		{
			const string sourceCode = @"var x = System.String.Format( ""{0} {1}"", 42 );";
			_refactoringProvider.ShouldNotProvideRefactoring( sourceCode );
		}
	}
}
