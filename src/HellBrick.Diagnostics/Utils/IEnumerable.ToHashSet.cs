using System.Collections.Generic;

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
