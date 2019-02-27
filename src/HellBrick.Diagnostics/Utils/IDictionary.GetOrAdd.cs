using System;
using System.Collections.Generic;

namespace HellBrick.Diagnostics.Utils
{
	public static partial class DictionaryExtensions
	{
		public static TValue GetOrAdd<TKey, TValue>( this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> valueFactory )
		{
			if ( dictionary.TryGetValue( key, out TValue value ) )
			{
				return value;
			}
			else
			{
				TValue newValue = valueFactory( key );
				dictionary.Add( key, newValue );
				return newValue;
			}
		}
	}
}
