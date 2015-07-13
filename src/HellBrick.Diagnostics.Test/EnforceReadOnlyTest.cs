using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;
using System.Linq;
using HellBrick.Diagnostics.EnforceReadOnly;

namespace HellBrick.Diagnostics.Test
{
	[TestClass]
	public class EnforceReadOnlyTest : CodeFixVerifier
	{
		#region Common

		protected override CodeFixProvider GetCSharpCodeFixProvider() => new EnforceReadOnlyCodeFixProvider();
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
		private string _x;
		
		public ClassName()
		{
			_x = ""42"";
		}
	}
}";

			var expectedCode = @"
using System;
namespace NS
{
	class ClassName
	{
		private readonly string _x;
		
		public ClassName()
		{
			_x = ""42"";
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
		private string _x;
		
		private void DoStuff()
		{
			_x = ""42"";
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
		private string _x;
		
		private int SomeProperty
		{
			get
			{
				_x = ""42"";
				return _x;
			}
		}
	}
}";
			VerifyNoFix( sourceCode );
		}

		[TestMethod]
		public void ValueTypeFieldAssignedToByIndexerIsNotFixed()
		{
			var sourceCode = @"
using System;
using System.Collections.Specialized;
namespace NS
{
	class ClassName
	{
		private StructName _struct;
		
		private void DoSomething()
		{
			_struct[ 0 ] = ""53"";
		}
	}

	struct StructName
	{
		public string this[ int index ]
		{
			get { return ""42""; }
			set { }
		}
	}
}";
			VerifyNoFix( sourceCode );
		}

		[TestMethod]
		public void ReferenceTypeFieldAssignedToByIndexerIsFixed()
		{
			var sourceCode = @"
using System;
namespace NS
{
	class ClassName
	{
		private int[] _array = new int[8];
		
		private void DoSomething()
		{
			_array[0] = ""42"";
		}
	}
}";
			var expectedCode = @"
using System;
namespace NS
{
	class ClassName
	{
		private readonly int[] _array = new int[8];
		
		private void DoSomething()
		{
			_array[0] = ""42"";
		}
	}
}";
			VerifyFix( sourceCode, expectedCode, "_array" );
		}

		[TestMethod]
		public void ReferenceTypeFieldThatHasFieldAssignedToIsFixed()
		{
			var sourceCode = @"
using System;
using System.Collections.Specialized;
namespace NS
{
	class ClassName
	{
		private AnotherClass _class;
		
		private void DoSomething()
		{
			_class.X = 53;
		}
	}

	class AnotherClass
	{
		public int X;
	}
}";
			var expectedCode = @"
using System;
using System.Collections.Specialized;
namespace NS
{
	class ClassName
	{
		private readonly AnotherClass _class;
		
		private void DoSomething()
		{
			_class.X = 53;
		}
	}

	class AnotherClass
	{
		public int X;
	}
}";
			VerifyFix( sourceCode, expectedCode, "_class" );
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
		private string _x;
		private string _y;
		
		public ClassName()
		{
			Interlocked.Increment( ref _x );
			Set( ""42"", out _y );
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
		private readonly string _x;
		private readonly string _y;
		
		public ClassName()
		{
			Interlocked.Increment( ref _x );
			Set( ""42"", out _y );
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
		private string _x;
		private string _y;
		
		public void DoStuff()
		{
			Interlocked.Increment( ref _x );
			Set( ""42"", out _y );
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
		private string _x;
		
		public ClassName()
		{
			Action lambda = () => _x = ""42"";
		}
	}
}";
			VerifyNoFix( sourceCode );
		}

		[TestMethod]
		public void IncrementedFieldIsNotFixed()
		{
			var sourceCode = @"
using System;
namespace NS
{
	class ClassName
	{
		private string _x;
		private string _y;
		
		public void Method()
		{
			_x++;
			_y--;
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
		private string _x;
		
		private class NestedClass
		{
			public NestedClass( ClassName parent )
			{
				parent._x = ""42"";
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
		private string _x, _y;
		
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
		private string _x;
		
		public ClassName()
		{
		}
	}
	
	partial class ClassName
	{		
		public DoStuff()
		{
			_x = ""42"";
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
		public string _x;
		internal string _y;
		protected string _z;
		
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
		private readonly string _x;
		private const string _y = ""53"";
		
		public ClassName()
		{
		}
	}
}";
			VerifyNoFix( sourceCode );
		}

		[TestMethod]
		public void StaticFieldAssignedToInNonStaticConstructorIsNotFixed()
		{
			var sourceCode = @"
using System;
namespace NS
{
	class ClassName
	{
		private static string _x;
		
		public ClassName()
		{
			_x = ""42"";
		}
	}
}";
			VerifyNoFix( sourceCode );
		}

		[TestMethod]
		public void FieldAssignedToByOperatorAssignmentIsNotFixed()
		{
			var sourceCode = @"
using System;
namespace NS
{
	class ClassName
	{
		private string _a;
		private string _b;
		private string _c;
		private string _d;
		private string _e;
		private string _f;
		private string _g;
		private string _h;
		private string _i;
		private string _j;
		
		public SomeMethod()
		{
			_a += 1;
			_b &= 1;
			_c /= 1;
			_d ^= 1;
			_e <<= 1;
			_f %= 1;
			_g *= 1;
			_h |= 1;
			_i >>= 1;
			_j -= 1;
		}
	}
}";

			VerifyNoFix( sourceCode );
		}

		[TestMethod]
		public void ValueTypeFieldIsNotFixed()
		{
			var sourceCode = @"
using System;
namespace NS
{
	class ClassName
	{
		private StructName _a;
		
		public ClassName()
		{
			_a = default(StructName);
		}
	}

	struct StructName
	{
	}
}";

			VerifyNoFix( sourceCode );
		}
	}
}