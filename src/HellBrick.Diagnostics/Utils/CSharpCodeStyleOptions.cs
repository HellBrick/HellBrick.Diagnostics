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

		public static Option<CodeStyleOption<string>> PreferredModifierOrder { get; }
			= _csharpCodeStyleOptionsType
			.GetRuntimeField( nameof( PreferredModifierOrder ) )
			.GetValue( null )
			as Option<CodeStyleOption<string>>;
	}
}
