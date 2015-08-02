using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HellBrick.Diagnostics.Utils
{
	public static partial class EnumerableExtensions
	{
		public static IEnumerable<T> Concat<T>( params IEnumerable<T>[] sequences ) => sequences.SelectMany( s => s );
	}
}
