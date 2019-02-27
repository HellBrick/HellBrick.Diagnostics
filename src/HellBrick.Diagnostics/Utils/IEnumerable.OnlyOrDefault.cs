using System.Collections.Generic;

namespace HellBrick.Diagnostics.Utils
{
	public static partial class EnumerableExtensions
	{
		public static T OnlyOrDefault<T>( this IEnumerable<T> sequence )
		{
			using ( IEnumerator<T> enumerator = sequence.GetEnumerator() )
			{
				if ( !enumerator.MoveNext() )
					return default;

				T onlyCandidate = enumerator.Current;
				if ( enumerator.MoveNext() )
					return default;

				return onlyCandidate;
			}
		}
	}
}
