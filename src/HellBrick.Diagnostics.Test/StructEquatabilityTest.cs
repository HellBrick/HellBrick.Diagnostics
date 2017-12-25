using HellBrick.Diagnostics.StructDeclarations;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

namespace HellBrick.Diagnostics.Test
{
	public class StructEquatabilityTest : CodeFixVerifier
	{
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new StructAnalyzer();
		protected override CodeFixProvider GetCSharpCodeFixProvider() => new StructEquatabilityCodeFixProvider();

		[Fact]
		public void NonReadonlyStructIsIgnored()
		{
			const string source =
@"
public struct NonReadonlyStruct
{
	private readonly int _value;
}
";
			VerifyNoFix( source );
		}

		[Fact]
		public void ZeroFieldStructHasEquatabilityMembersGenerated()
		{
			const string before =
@"
using System;
public readonly struct ZeroFieldStruct
{	
}
";
			const string after =
@"
using System;
public readonly struct ZeroFieldStruct : IEquatable<ZeroFieldStruct>
{
	public override int GetHashCode() => 0;
	public bool Equals( ZeroFieldStruct other ) => true;
	public override bool Equals( object obj ) => obj is ZeroFieldStruct other && Equals( other );

	public static bool operator ==( ZeroFieldStruct x, ZeroFieldStruct y ) => x.Equals( y );
	public static bool operator !=( ZeroFieldStruct x, ZeroFieldStruct y ) => !x.Equals( y );
}
";
			VerifyCSharpFix( before, after );
		}

		[Fact]
		public void OneFieldStructHasEquatabilityMembersGenerated()
		{
			const string before =
@"
using System;
using System.Collections.Generic;
public readonly struct OneFieldStruct
{
	private readonly object _field;
}
";
			const string after =
@"
using System;
using System.Collections.Generic;
public readonly struct OneFieldStruct : IEquatable<OneFieldStruct>
{
	private readonly object _field;

	public override int GetHashCode() => EqualityComparer<object>.Default.GetHashCode( _field );
	public bool Equals( OneFieldStruct other ) => EqualityComparer<object>.Default.Equals( _field, other._field );
	public override bool Equals( object obj ) => obj is OneFieldStruct other && Equals( other );

	public static bool operator ==( OneFieldStruct x, OneFieldStruct y ) => x.Equals( y );
	public static bool operator !=( OneFieldStruct x, OneFieldStruct y ) => !x.Equals( y );
}
";
			VerifyCSharpFix( before, after );
		}

		[Fact]
		public void ManyFieldStructHasEquatabilityMembersGenerated()
		{
			const string before =
@"
using System;
using System.Collections.Generic;
public readonly struct ManyFieldStruct
{
	private readonly int _number;
	public string Text { get; }
}
";
			const string after =
@"
using System;
using System.Collections.Generic;
public readonly struct ManyFieldStruct : IEquatable<ManyFieldStruct>
{
	private readonly int _number;
	public string Text { get; }

	public override int GetHashCode()
	{
		unchecked
		{
			const int prime = -1521134295;
			int hash = 12345701;
			hash = hash * prime + EqualityComparer<int>.Default.GetHashCode( _number );
			hash = hash * prime + EqualityComparer<string>.Default.GetHashCode( Text );
			return hash;
		}
	}

	public bool Equals( ManyFieldStruct other ) => EqualityComparer<int>.Default.Equals( _number, other._number ) && Text == other.Text;
	public override bool Equals( object obj ) => obj is ManyFieldStruct other && Equals( other );

	public static bool operator ==( ManyFieldStruct x, ManyFieldStruct y ) => x.Equals( y );
	public static bool operator !=( ManyFieldStruct x, ManyFieldStruct y ) => !x.Equals( y );
}
";
			VerifyCSharpFix( before, after );
		}

		[Fact]
		public void OverridesAreGeneratedIfInterfaceAndOperatorsAlreadyExist()
		{
			const string before =
@"
using System;
public readonly struct StructWithoutOverrides : IEquatable<StructWithoutOverrides>
{
	public bool Equals( StructWithoutOverrides other ) => true;

	public static bool operator ==( StructWithoutOverrides x, StructWithoutOverrides y ) => true;
	public static bool operator !=( StructWithoutOverrides x, StructWithoutOverrides y ) => false;
}
";
			const string after =
@"
using System;
public readonly struct StructWithoutOverrides : IEquatable<StructWithoutOverrides>
{
	public bool Equals( StructWithoutOverrides other ) => true;

	public static bool operator ==( StructWithoutOverrides x, StructWithoutOverrides y ) => true;
	public static bool operator !=( StructWithoutOverrides x, StructWithoutOverrides y ) => false;

	public override int GetHashCode() => 0;
	public override bool Equals( object obj ) => obj is StructWithoutOverrides other && Equals( other );
}
";
			VerifyCSharpFix( before, after );
		}

		[Fact]
		public void OperatorsAreGeneratedIfInterfaceAndOverridesAlreadyExist()
		{
			const string before =
@"
using System;
public readonly struct StructWithoutOperators : IEquatable<StructWithoutOperators>
{
	public override int GetHashCode() => 0;
	public bool Equals( StructWithoutOperators other ) => true;
	public override bool Equals( object obj ) => obj is StructWithoutOperators && Equals( (StructWithoutOperators) obj );
}
";
			const string after =
@"
using System;
public readonly struct StructWithoutOperators : IEquatable<StructWithoutOperators>
{
	public override int GetHashCode() => 0;
	public bool Equals( StructWithoutOperators other ) => true;
	public override bool Equals( object obj ) => obj is StructWithoutOperators && Equals( (StructWithoutOperators) obj );

	public static bool operator ==( StructWithoutOperators x, StructWithoutOperators y ) => x.Equals( y );
	public static bool operator !=( StructWithoutOperators x, StructWithoutOperators y ) => !x.Equals( y );
}
";
			VerifyCSharpFix( before, after );
		}

		[Fact]
		public void InterfaceIsImplementedIfIOverridesAndOperatorsAlreadyExist()
		{
			const string before =
@"
using System;
public readonly struct StructWithoutInterface
{
	public override int GetHashCode() => 0;
	public override bool Equals( object obj ) => true;
}
";
			const string after =
@"
using System;
public readonly struct StructWithoutInterface : IEquatable<StructWithoutInterface>
{
	public override int GetHashCode() => 0;
	public override bool Equals( object obj ) => true;
	public bool Equals( StructWithoutInterface other ) => true;

	public static bool operator ==( StructWithoutInterface x, StructWithoutInterface y ) => x.Equals( y );
	public static bool operator !=( StructWithoutInterface x, StructWithoutInterface y ) => !x.Equals( y );
}
";
			VerifyCSharpFix( before, after );
		}
	}
}
