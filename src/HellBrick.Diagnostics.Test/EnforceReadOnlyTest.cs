using HellBrick.Diagnostics.EnforceReadOnly;
using Xunit;
using HellBrick.Diagnostics.Assertions;

namespace HellBrick.Diagnostics.Test
{
	public class EnforceReadOnlyTest
	{
		private readonly AnalyzerVerifier<EnforceReadOnlyAnalyzer, EnforceReadOnlyCodeFixProvider> _verifier
			= AnalyzerVerifier
			.UseAnalyzer<EnforceReadOnlyAnalyzer>()
			.UseCodeFix<EnforceReadOnlyCodeFixProvider>();

		[Fact]
		public void FieldAssignedToInConstructorIsFixed()
			=> _verifier
			.Source
			(
@"
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
}"
			)
			.ShouldHaveFix
			(
@"
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
}"
			);

		[Fact]
		public void FiedAssignedToInMethodIsNotFixed()
			=> _verifier
			.Source
			(
@"
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
}"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void FiedAssignedToInPropertyIsNotFixed()
			=> _verifier
			.Source
			(
@"
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
}"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void ValueTypeFieldAssignedToByIndexerIsNotFixed()
			=> _verifier
			.Source
			(
@"
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
}"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void ReferenceTypeFieldAssignedToByIndexerIsFixed()
			=> _verifier
			.Source
			(
@"
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
}"
			)
			.ShouldHaveFix
			(
@"
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
}"
			);

		[Fact]
		public void ReferenceTypeFieldThatHasFieldAssignedToIsFixed()
			=> _verifier
			.Source
			(
@"
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
}"
			)
			.ShouldHaveFix
			(
@"
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
}"
			);

		[Fact]
		public void FieldReferencedByRefInConstructorIsFixed()
			=> _verifier
			.Source
			(
@"
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
}"
			)
			.ShouldHaveFix
			(
@"
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
}"
			);

		[Fact]
		public void FieldReferencedByRefInMethodIsNotFixed()
			=> _verifier
			.Source
			(
@"
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
}"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void FieldAssignedToInLambdaIsNotFixed()
			=> _verifier
			.Source
			(
@"
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
}"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void IncrementedFieldIsNotFixed()
			=> _verifier
			.Source
			(
@"
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
}"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void FiedAssignedToInNestedClassIsNotFixed()
			=> _verifier
			.Source
			(
@"
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
}"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void MultiDeclaratorIsNotFixed()
			=> _verifier
			.Source
			(
@"
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
}"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void PartialClassIsNotFixed()
			=> _verifier
			.Source
			(
@"
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
		public void DoStuff()
		{
			_x = ""42"";
		}
	}
}"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void PublicFieldIsNotFixed()
			=> _verifier
			.Source
			(
@"
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
}"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void ReadOnlyFieldIsNotFixed()
			=> _verifier
			.Source
			(
@"
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
}"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void StaticFieldAssignedToInNonStaticConstructorIsNotFixed()
			=> _verifier
			.Source
			(
@"
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
}"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void FieldAssignedToByOperatorAssignmentIsNotFixed()
			=> _verifier
			.Source
			(
@"
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
		
		public void SomeMethod()
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
}"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void ValueTypeFieldIsNotFixed()
			=> _verifier
			.Source
			(
@"
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
}"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void PrimitiveTypeFieldIsFixed()
			=> _verifier
			.Source
			(
@"
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
}"
			)
			.ShouldHaveFix
			(
@"
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
}"
			);

		[Fact]
		public void ReadOnlyStructFieldIsFixed()
			=> _verifier
			.Source
			(
@"
using System;
namespace NS
{
	class ClassName
	{
		private Struct _a;
		
		public ClassName() => _a = new Struct( ""text"" );
	}

	readonly struct Struct
	{
		private readonly string _value;

		public Struct( string value ) => _value = value;
	}
}"
			)
			.ShouldHaveFix
			(
@"
using System;
namespace NS
{
	class ClassName
	{
		private readonly Struct _a;
		
		public ClassName() => _a = new Struct( ""text"" );
	}

	readonly struct Struct
	{
		private readonly string _value;

		public Struct( string value ) => _value = value;
	}
}"
			);

		[Fact]
		public void FieldAssignedByLocalMethodIsNotFixed()
			=> _verifier
			.Source
			(
@"
public class ClassName
{
	private object _value;

	public ClassName()
	{
		void LocalMethod()
		{
			_value = new object();
		}
	}
}"
			)
			.ShouldHaveNoDiagnostics();
	}
}
