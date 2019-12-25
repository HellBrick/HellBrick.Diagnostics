using HellBrick.Diagnostics.Assertions;
using HellBrick.Diagnostics.StructDeclarations;
using HellBrick.Diagnostics.Utils;
using Microsoft.CodeAnalysis.CodeStyle;
using Xunit;

namespace HellBrick.Diagnostics.Test
{
	public class StructEquatabilityTest
	{
		private readonly AnalyzerVerifier<StructAnalyzer, StructEquatabilityCodeFixProvider> _verifier
			= AnalyzerVerifier
			.UseAnalyzer<StructAnalyzer>()
			.UseCodeFix<StructEquatabilityCodeFixProvider>();

		[Fact]
		public void NonReadonlyStructIsIgnored()
			=> _verifier
			.Source
			(
@"
public struct NonReadonlyStruct
{
	private readonly int _value;
}
"
			)
			.ShouldHaveNoDiagnostics();

		[Fact]
		public void ZeroFieldStructHasEquatabilityMembersGenerated()
			=> _verifier
			.Source
			(
@"
using System;
namespace Namespace
{
	public readonly struct ZeroFieldStruct
	{	
	}
}
" )
			.ShouldHaveFix
			(
@"
using System;
namespace Namespace
{
	public readonly struct ZeroFieldStruct : IEquatable<ZeroFieldStruct>
	{
		public override int GetHashCode() => 0;
		public bool Equals( ZeroFieldStruct other ) => true;
		public override bool Equals( object obj ) => obj is ZeroFieldStruct other && Equals( other );

		public static bool operator ==( ZeroFieldStruct x, ZeroFieldStruct y ) => x.Equals( y );
		public static bool operator !=( ZeroFieldStruct x, ZeroFieldStruct y ) => !x.Equals( y );
	}
}
"
			);

		[Fact]
		public void OneReferenceTypeFieldStructHasEquatabilityMembersGenerated()
			=> _verifier
			.Source
			(
@"
using System;
using System.Collections.Generic;
public readonly struct OneFieldStruct
{
	private readonly object _field;
}
"
			)
			.ShouldHaveFix
			(
@"
using System;
using System.Collections.Generic;
public readonly struct OneFieldStruct : IEquatable<OneFieldStruct>
{
	private readonly object _field;

	public override int GetHashCode() => _field?.GetHashCode() ?? 0;
	public bool Equals( OneFieldStruct other ) => EqualityComparer<object>.Default.Equals( _field, other._field );
	public override bool Equals( object obj ) => obj is OneFieldStruct other && Equals( other );

	public static bool operator ==( OneFieldStruct x, OneFieldStruct y ) => x.Equals( y );
	public static bool operator !=( OneFieldStruct x, OneFieldStruct y ) => !x.Equals( y );
}
"
			);

		[Fact]
		public void OneValueTypeFieldStructHasEquatabilityMembersGenerated()
			=> _verifier
			.Source
			(
@"
using System;
using System.Collections.Generic;
public readonly struct OneFieldStruct
{
	private readonly int _field;
}
"
			)
			.ShouldHaveFix
			(
@"
using System;
using System.Collections.Generic;
public readonly struct OneFieldStruct : IEquatable<OneFieldStruct>
{
	private readonly int _field;

	public override int GetHashCode() => _field.GetHashCode();
	public bool Equals( OneFieldStruct other ) => EqualityComparer<int>.Default.Equals( _field, other._field );
	public override bool Equals( object obj ) => obj is OneFieldStruct other && Equals( other );

	public static bool operator ==( OneFieldStruct x, OneFieldStruct y ) => x.Equals( y );
	public static bool operator !=( OneFieldStruct x, OneFieldStruct y ) => !x.Equals( y );
}
"
			);

		[Fact]
		public void ManyFieldStructHasEquatabilityMembersGenerated()
			=> _verifier
			.WithOptions
			(
				o
				=> o
				.WithProperFormatting()
				.WithChangedOption( CSharpCodeStyleOptions.VarForBuiltInTypes, new CodeStyleOption<bool>( false, NotificationOption.Warning ) )
			)
			.Source
			(
@"
using System;
using System.Collections.Generic;
public readonly struct ManyFieldStruct
{
	private readonly int _number;
	public string Text { get; }
}
"
			)
			.ShouldHaveFix
			(
@"
using System;
using System.Collections.Generic;
public readonly struct ManyFieldStruct : IEquatable<ManyFieldStruct>
{
	private readonly int _number;
	public string Text { get; }

	public override int GetHashCode() => (_number, Text).GetHashCode();
	public bool Equals( ManyFieldStruct other ) => (_number, Text) == (other._number, other.Text);
	public override bool Equals( object obj ) => obj is ManyFieldStruct other && Equals( other );

	public static bool operator ==( ManyFieldStruct x, ManyFieldStruct y ) => x.Equals( y );
	public static bool operator !=( ManyFieldStruct x, ManyFieldStruct y ) => !x.Equals( y );
}
"
			);

		[Fact]
		public void OverridesAreGeneratedIfInterfaceAndOperatorsAlreadyExist()
			=> _verifier
			.Source
			(
@"
using System;
public readonly struct StructWithoutOverrides : IEquatable<StructWithoutOverrides>
{
	public bool Equals( StructWithoutOverrides other ) => true;

	public static bool operator ==( StructWithoutOverrides x, StructWithoutOverrides y ) => true;
	public static bool operator !=( StructWithoutOverrides x, StructWithoutOverrides y ) => false;
}
"
			)
			.ShouldHaveFix
			(
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
"
			);

		[Fact]
		public void OperatorsAreGeneratedIfInterfaceAndOverridesAlreadyExist()
			=> _verifier
			.Source
			(
@"
using System;
public readonly struct StructWithoutOperators : IEquatable<StructWithoutOperators>
{
	public override int GetHashCode() => 0;
	public bool Equals( StructWithoutOperators other ) => true;
	public override bool Equals( object obj ) => obj is StructWithoutOperators && Equals( (StructWithoutOperators) obj );
}
"
			)
			.ShouldHaveFix
			(
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
"
			);

		[Fact]
		public void InterfaceIsImplementedIfIOverridesAndOperatorsAlreadyExist()
			=> _verifier
			.Source
			(
@"
using System;
public readonly struct StructWithoutInterface
{
	public override int GetHashCode() => 0;
	public override bool Equals( object obj ) => true;
}
"
			)
			.ShouldHaveFix
			(
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
"
			);

		[Fact]
		public void GenericStructHasMembersImplementedCorrectly()
			=> _verifier
			.Source
			(
@"
using System;
public readonly struct GenericStruct<T>
{
	private readonly T _field;
}
"
			)
			.ShouldHaveFix
			(
@"
using System;
public readonly struct GenericStruct<T> : IEquatable<GenericStruct<T>>
{
	private readonly T _field;

	public override int GetHashCode() => _field?.GetHashCode() ?? 0;
	public bool Equals( GenericStruct<T> other ) => System.Collections.Generic.EqualityComparer<T>.Default.Equals( _field, other._field );
	public override bool Equals( object obj ) => obj is GenericStruct<T> other && Equals( other );

	public static bool operator ==( GenericStruct<T>x, GenericStruct<T>y ) => x.Equals( y );
	public static bool operator !=( GenericStruct<T>x, GenericStruct<T>y ) => !x.Equals( y );
}
"
			);
	}
}
