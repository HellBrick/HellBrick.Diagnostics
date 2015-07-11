using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HellBrick.Diagnostics.Utils
{
	internal static class TaskHelper
	{
		public static Task CompletedTask { get; } = Task.FromResult( false );
		public static Task CanceledTask { get; } = CreateCanceledTask();

		private static Task CreateCanceledTask()
		{
			TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
			tcs.SetCanceled();
			return tcs.Task;
		}
	}
}
