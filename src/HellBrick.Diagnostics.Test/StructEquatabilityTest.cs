﻿using HellBrick.Diagnostics.StructDeclarations;
using HellBrick.Diagnostics.Assertions;
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
		public void OneFieldStructHasEquatabilityMembersGenerated()
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

	public override int GetHashCode() => EqualityComparer<object>.Default.GetHashCode( _field );
	public bool Equals( OneFieldStruct other ) => EqualityComparer<object>.Default.Equals( _field, other._field );
	public override bool Equals( object obj ) => obj is OneFieldStruct other && Equals( other );

	public static bool operator ==( OneFieldStruct x, OneFieldStruct y ) => x.Equals( y );
	public static bool operator !=( OneFieldStruct x, OneFieldStruct y ) => !x.Equals( y );
}
"
			);

		[Fact]
		public void ManyFieldStructHasEquatabilityMembersGenerated()
			=> _verifier
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
	}
}
