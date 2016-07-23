using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HellBrick.Diagnostics.DeadCode;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace HellBrick.Diagnostics.Test
{
	[TestClass]
	public class UnusedParameterTest : CodeFixVerifier
	{
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new UnusedParameterAnalyzer();
		protected override CodeFixProvider GetCSharpCodeFixProvider() => new UnusedParameterCodeFixProvider();

		[TestMethod]
		public void UnusedClassMethodParameterIsRemoved()
		{
			const string source =
@"public class C
{
	public void Something( int good1, string bad, int good2 )
	{
		var x = good1;
		var y = good2;
	}
}";
			const string result =
@"public class C
{
	public void Something( int good1, int good2 )
	{
		var x = good1;
		var y = good2;
	}
}";
			VerifyCSharpFix( source, result );
		}

		[TestMethod]
		public void CorrespondingArgumentIsRemovedWhenCalledFromSameClass()
		{
			const string source =
@"public class C
{
	public void Proxy() => Something( 42, default( string ), 64 );
	public void Something( int good1, string bad, int good2 )
	{
		var x = good1;
		var y = good2;
	}
}";
			const string result =
@"public class C
{
	public void Proxy() => Something( 42, 64 );
	public void Something( int good1, int good2 )
	{
		var x = good1;
		var y = good2;
	}
}";
			VerifyCSharpFix( source, result );
		}

		[TestMethod]
		public void CorrespondingArgumentIsRemovedWhenCalledFromAnotherClass()
		{
			const string source =
@"public class C
{
	public void Something( int good1, string bad, int good2 )
	{
		var x = good1;
		var y = good2;
	}
}

public class D
{
	public void Proxy() => new C().Something( 42, ""asdf"" + ""qwer"", 0 );
}";
			const string result =
@"public class C
{
	public void Something( int good1, int good2 )
	{
		var x = good1;
		var y = good2;
	}
}

public class D
{
	public void Proxy() => new C().Something( 42, 0 );
}";
			VerifyCSharpFix( source, result );
		}

		[TestMethod]
		public void UnusedParameterIsNotRemovedIfMethodImplementsInterface()
		{
			const string source =
@"public interface I
{
	void Method( int arg );
}

public class C : I
{
	public void Method( int arg )
	{
	}
}

public class D : I
{
	void I.Method( int arg )
	{
	}
}";
			VerifyNoFix( source );
		}

		[TestMethod]
		public void UnusedParameterIsNotRemovedIfMethodIsVirtual()
		{
			const string source =
@"public class C
{
	public virtual void Method( int arg )
	{
	}
}

public class D : C
{
	public override void Method( int arg )
	{
		while ( true )
		{
		}
	}
}";
			VerifyNoFix( source );
		}

		[TestMethod]
		public void UnusedConstructorParameterIsRemoved()
		{
			const string source =
@"public class C
{
	public C( int unused )
	{
	}
}

public class D : C
{
	public D() : base( 42 )
	{
	}
}";
			const string result =
@"public class C
{
	public C()
	{
	}
}

public class D : C
{
	public D() : base()
	{
	}
}";
			VerifyCSharpFix( source, result );
		}

		[TestMethod]
		public void UnusedLambdaParameterIsNotRemoved()
		{
			const string source =
@"public class C
{
	public C()
	{
		var func1 = ( int x, string _ ) => x;
		var func2 = ( int x, string _ ) => { return x; };
	}
}";
			VerifyNoFix( source );
		}

		[TestMethod]
		public void EntryPointArgumentIsNotRemoved()
		{
			const string source =
@"internal class Program
{
	private static void Main( string[] args )
	{
	}
}";
			VerifyNoFix( source );
		}

		[TestMethod]
		public void AllUnusedMethodParametersAreRemoved()
		{
			const string source =
@"public class C
{
	public void Proxy() => Something( default( string ), 42, 64 );
	public void Something( string unused1, int good, int unused2 )
	{
		var x = good;
	}
}";
			const string result =
@"public class C
{
	public void Proxy() => Something( 42 );
	public void Something( int good )
	{
		var x = good;
	}
}";
			VerifyCSharpFix( source, result );
		}

		[TestMethod]
		public void CorrectArgumentIsRemovedIfArgumentOrderDiffersFromParameterOrder()
		{
			const string source =
@"public class C
{
	public void Proxy() => Something( 42, good2: 64, bad: ""100"" );
	public void Something( int good1, string bad, int good2 )
	{
		var x = good1;
		var y = good2;
	}
}";
			const string result =
@"public class C
{
	public void Proxy() => Something( 42, good2: 64 );
	public void Something( int good1, int good2 )
	{
		var x = good1;
		var y = good2;
	}
}";
			VerifyCSharpFix( source, result );
		}

		[TestMethod]
		public void ArgumentIsNotRemovedIfDefaultValueIsPassedtoOptionalParameter()
		{
			const string source =
@"public class C
{
	public void Proxy() => Something( 42 );
	public void Something( int good, string bad = null )
	{
		var x = good;
	}
}";
			const string result =
@"public class C
{
	public void Proxy() => Something( 42 );
	public void Something( int good )
	{
		var x = good;
	}
}";
			VerifyCSharpFix( source, result );
		}

		[TestMethod]
		public void NestedCallArgumentIsRemovedCompletely()
		{
			const string source =
@"public class C
{
	public string Proxy() => Something( 42, Something( 64, 100 ) );
	public string Something( int good, string bad ) => good.ToString();
}";
			const string result =
@"public class C
{
	public string Proxy() => Something( 42 );
	public string Something( int good ) => good.ToString();
}";
			VerifyCSharpFix( source, result );
		}

		[TestMethod]
		public void NestedCallArgumentIsAdjustedCorrectlyIfPassedAsParameterThatIsNotRemoved()
		{
			const string source =
@"public class C
{
	public int Proxy() => Something( Something( 42, ""asdf"" ), ""qwerty"" );
	public int Something( int good, string bad ) => good * 2;
}";
			const string result =
@"public class C
{
	public int Proxy() => Something( Something( 42 ) );
	public int Something( int good ) => good * 2;
}";
			VerifyCSharpFix( source, result );
		}
	}
}
