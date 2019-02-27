using System;
using System.Collections.Generic;

namespace HellBrick.Diagnostics.Utils
{
	public static partial class EnumerableExtensions
	{
		[NoCapture]
		public static void ForEach<T>( this IEnumerable<T> sequence, Action<T> action )
			=> sequence.ForEach( action, ( capturedAction, item ) => capturedAction( item ) );

		[NoCapture]
		public static void ForEach<TItem, TContext>( this IEnumerable<TItem> sequence, TContext context, Action<TContext, TItem> action )
		{
			foreach ( TItem item in sequence )
			{
				action( context, item );
			}
		}
	}
}
