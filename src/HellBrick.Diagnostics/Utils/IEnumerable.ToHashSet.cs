using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HellBrick.Diagnostics.Utils
{
	public static partial class EnumerableExtensions
	{
		public static HashSet<T> ToHashSet<T>( this IEnumerable<T> sequence )
		{
			return new HashSet<T>( sequence );
		}
	}
}
