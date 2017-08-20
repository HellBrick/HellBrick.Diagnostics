using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using TestHelper;
using System.Linq;
using HellBrick.Diagnostics.EnforceReadOnly;
using Xunit;

namespace HellBrick.Diagnostics.Test
{
	public class EnforceReadOnlyTest : CodeFixVerifier
	{
		#region Common

		protected override CodeFixProvider GetCSharpCodeFixProvider() => new EnforceReadOnlyCodeFixProvider();
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new EnforceReadOnlyAnalyzer();

		private void VerifyFix( string sourceCode, string expectedCode, params string[] variableNames )
		{
			DiagnosticResult[] expectedDiagnostics = variableNames
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

		[Fact]
		public void FieldAssignedToInConstructorIsFixed()
		{
			string sourceCode = @"
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

			string expectedCode = @"
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

		[Fact]
		public void FiedAssignedToInMethodIsNotFixed()
		{
			string sourceCode = @"
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

		[Fact]
		public void FiedAssignedToInPropertyIsNotFixed()
		{
			string sourceCode = @"
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

		[Fact]
		public void ValueTypeFieldAssignedToByIndexerIsNotFixed()
		{
			string sourceCode = @"
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

		[Fact]
		public void ReferenceTypeFieldAssignedToByIndexerIsFixed()
		{
			string sourceCode = @"
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
			string expectedCode = @"
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

		[Fact]
		public void ReferenceTypeFieldThatHasFieldAssignedToIsFixed()
		{
			string sourceCode = @"
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
			string expectedCode = @"
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

		[Fact]
		public void FieldReferencedByRefInConstructorIsFixed()
		{
			string sourceCode = @"
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
			string expectedCode = @"
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

		[Fact]
		public void FieldReferencedByRefInMethodIsNotFixed()
		{
			string sourceCode = @"
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

		[Fact]
		public void FieldAssignedToInLambdaIsNotFixed()
		{
			string sourceCode = @"
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

		[Fact]
		public void IncrementedFieldIsNotFixed()
		{
			string sourceCode = @"
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

		[Fact]
		public void FiedAssignedToInNestedClassIsNotFixed()
		{
			string sourceCode = @"
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

		[Fact]
		public void MultiDeclaratorIsNotFixed()
		{
			string sourceCode = @"
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

		[Fact]
		public void PartialClassIsNotFixed()
		{
			string sourceCode = @"
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

		[Fact]
		public void PublicFieldIsNotFixed()
		{
			string sourceCode = @"
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

		[Fact]
		public void ReadOnlyFieldIsNotFixed()
		{
			string sourceCode = @"
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

		[Fact]
		public void StaticFieldAssignedToInNonStaticConstructorIsNotFixed()
		{
			string sourceCode = @"
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

		[Fact]
		public void FieldAssignedToByOperatorAssignmentIsNotFixed()
		{
			string sourceCode = @"
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

		[Fact]
		public void ValueTypeFieldIsNotFixed()
		{
			string sourceCode = @"
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

		[Fact]
		public void PrimitiveTypeFieldIsFixed()
		{
			string sourceCode = @"
using System;
namespace NS
{
	class ClassName
	{
		private int _a;
		
		public ClassName()
		{
			_a = 42;
		}
	}
}";
			string expectedCode = @"
using System;
namespace NS
{
	class ClassName
	{
		private readonly int _a;
		
		public ClassName()
		{
			_a = 42;
		}
	}
}";

			VerifyFix( sourceCode, expectedCode, "_a" );
		}
	}
}