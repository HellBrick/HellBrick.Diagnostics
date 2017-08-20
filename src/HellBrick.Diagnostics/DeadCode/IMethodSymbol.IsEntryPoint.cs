using Microsoft.CodeAnalysis;

namespace HellBrick.Diagnostics.DeadCode
{
	public static partial class SymbolExtensions
	{
		public static bool IsEntryPoint( this IMethodSymbol symbol )
			=> symbol.IsStatic && symbol.MetadataName == "Main" && symbol.ContainingSymbol?.MetadataName == "Program";
	}
}
