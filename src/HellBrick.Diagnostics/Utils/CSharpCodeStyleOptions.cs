using System;
using System.Reflection;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Options;

namespace HellBrick.Diagnostics.Utils
{
	public static class CSharpCodeStyleOptions
	{
		private static readonly Type _csharpCodeStyleOptionsType
			= typeof( CSharpFormattingOptions )
			.GetTypeInfo()
			.Assembly
			.GetType( "Microsoft.CodeAnalysis.CSharp.CodeStyle.CSharpCodeStyleOptions" );

		public static Option<CodeStyleOption<bool>> UseImplicitTypeForIntrinsicTypes { get; }
			= _csharpCodeStyleOptionsType
			.GetRuntimeField( nameof( UseImplicitTypeForIntrinsicTypes ) )
			.GetValue( null ) as Option<CodeStyleOption<bool>>;
	}
}
