using System.Collections.Generic;

namespace HellBrick.Diagnostics.Utils
{
	public static partial class DictionaryExtensions
	{
		public static TValue GetValueOrDefault<TKey, TValue>( this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key )
			=> dictionary.TryGetValue( key, out TValue value )
				? value
				: default;
	}
}
