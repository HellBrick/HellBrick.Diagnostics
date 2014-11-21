using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;
using System.Linq;

namespace HellBrick.Diagnostics.Test
{
	[TestClass]
	public class EnforceReadOnlyTest: CodeFixVerifier
	{
		#region Common

		protected override CodeFixProvider GetCSharpCodeFixProvider() => new EnforceReadOnlyCodeFix();
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new EnforceReadOnlyAnalyzer();

		private void VerifyFix( string sourceCode, string expectedCode, params string[] variableNames )
		{
			var expectedDiagnostics = variableNames
				.Select(
					name =>
					new DiagnosticResult
					{
						Id = EnforceReadOnlyAnalyzer.DiagnosticID,
						Severity = DiagnosticSeverity.Warning,
						Message = name
					} )
				.ToArray();

			VerifyCSharpDiagnostic( sourceCode, expectedDiagnostics );
			VerifyCSharpFix( sourceCode, expectedCode );
		}

		private void VerifyNoFix( string sourceCode )
		{
			VerifyFix( sourceCode, sourceCode );
		}

		#endregion

		[TestMethod]
		public void FieldAssignedToInConstructorIsFixed()
		{
			var sourceCode = @"
using System;
namespace NS
{
	class ClassName
	{
		private int _x;
		
		public ClassName()
		{
			_x = 42;
		}
	}
}";

			var expectedCode = @"
using System;
namespace NS
{
	class ClassName
	{
		private readonly int _x;
		
		public ClassName()
		{
			_x = 42;
		}
	}
}";
			VerifyFix( sourceCode, expectedCode, "_x" );
		}

		[TestMethod]
		public void FiedAssignedToInMethodIsNotFixed()
		{
			var sourceCode = @"
using System;
namespace NS
{
	class ClassName
	{
		private int _x;
		
		private void DoStuff()
		{
			_x = 42;
		}
	}
}";
			VerifyNoFix( sourceCode );
		}

		[TestMethod]
		public void FiedAssignedToInPropertyIsNotFixed()
		{
			var sourceCode = @"
using System;
namespace NS
{
	class ClassName
	{
		private int _x;
		
		private int SomeProperty
		{
			get
			{
				_x = 42;
				return _x;
			}
		}
	}
}";
			VerifyNoFix( sourceCode );
		}

		[TestMethod]
		public void FieldReferencedByRefInConstructorIsFixed()
		{
			var sourceCode = @"
using System;
using System.Threading;
namespace NS
{
	class ClassName
	{
		private int _x;
		private int _y;
		
		public ClassName()
		{
			Interlocked.Increment( ref _x );
			Set( 42, out _y );
		}

		public void Set( int value, out int reference )
		{
			reference = value;
		}
	}
}";
			var expectedCode = @"
using System;
using System.Threading;
namespace NS
{
	class ClassName
	{
		private readonly int _x;
		private readonly int _y;
		
		public ClassName()
		{
			Interlocked.Increment( ref _x );
			Set( 42, out _y );
		}

		public void Set( int value, out int reference )
		{
			reference = value;
		}
	}
}";
			VerifyFix( sourceCode, expectedCode, "_x", "_y" );
		}

		[TestMethod]
		public void FieldReferencedByRefInMethodIsNotFixed()
		{
			var sourceCode = @"
using System;
using System.Threading;
namespace NS
{
	class ClassName
	{
		private int _x;
		private int _y;
		
		public void DoStuff()
		{
			Interlocked.Increment( ref _x );
			Set( 42, out _y );
		}

		public void Set( int value, out int reference )
		{
			reference = value;
		}
	}
}";
			VerifyNoFix( sourceCode );
		}

		[TestMethod]
		public void FieldAssignedToInLambdaIsNotFixed()
		{
			var sourceCode = @"
using System;
namespace NS
{
	class ClassName
	{
		private int _x;
		
		public ClassName()
		{
			Action lambda = () => _x = 42;
		}
	}
}";
			VerifyNoFix( sourceCode );
		}

		[TestMethod]
		public void FiedAssignedToInNestedClassIsNotFixed()
		{
			var sourceCode = @"
using System;
namespace NS
{
	class ClassName
	{
		private int _x;
		
		private class NestedClass
		{
			public NestedClass( ClassName parent )
			{
				parent._x = 42;
			}
		}
	}
}";
			VerifyNoFix( sourceCode );
		}

		[TestMethod]
		public void MultiDeclaratorIsNotFixed()
		{
			var sourceCode = @"
using System;
namespace NS
{
	class ClassName
	{
		private int _x, _y;
		
		public ClassName()
		{
		}
	}
}";
			VerifyNoFix( sourceCode );
		}

		[TestMethod]
		public void PartialClassIsNotFixed()
		{
			var sourceCode = @"
using System;
namespace NS
{
	partial class ClassName
	{
		private int _x;
		
		public ClassName()
		{
		}
	}
	
	partial class ClassName
	{		
		public DoStuff()
		{
			_x = 42;
		}
	}
}";
			VerifyNoFix( sourceCode );
		}

		[TestMethod]
		public void PublicFieldIsNotFixed()
		{
			var sourceCode = @"
using System;
namespace NS
{
	class ClassName
	{
		public int _x;
		internal int _y;
		protected int _z;
		
		public ClassName()
		{
		}
	}
}";
			VerifyNoFix( sourceCode );
		}

		[TestMethod]
		public void ReadOnlyFieldIsNotFixed()
		{
			var sourceCode = @"
using System;
namespace NS
{
	class ClassName
	{
		private readonly int _x;
		private const int _y = 53;
		
		public ClassName()
		{
		}
	}
}";
			VerifyNoFix( sourceCode );
		}
	}
}