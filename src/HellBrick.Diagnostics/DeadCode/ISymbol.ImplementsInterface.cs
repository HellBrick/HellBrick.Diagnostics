using System.Linq;
using Microsoft.CodeAnalysis;

namespace HellBrick.Diagnostics.DeadCode
{
	public static partial class SymbolExtensions
	{
		public static bool ImplementsInterface( this ISymbol symbol )
			=>
			(
				from @interface in symbol.ContainingType.AllInterfaces
				from interfaceMember in @interface.GetMembers()
				let implementation = symbol.ContainingType.FindImplementationForInterfaceMember( interfaceMember )
				where symbol == implementation
				select 0
			)
			.Any();
	}
}
