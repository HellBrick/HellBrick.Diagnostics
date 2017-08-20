using System.Threading.Tasks;

namespace HellBrick.Diagnostics.Utils
{
	public static class TaskHelper
	{
		public static Task CanceledTask { get; } = CreateCanceledTask();

		private static Task CreateCanceledTask()
		{
			TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
			tcs.SetCanceled();
			return tcs.Task;
		}
	}
}
