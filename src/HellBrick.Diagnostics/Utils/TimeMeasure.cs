using System;
using System.Diagnostics;

namespace HellBrick.Diagnostics.Utils
{
	public class TimeMeasure : IDisposable
	{
		private readonly string _activityName;
		private readonly Stopwatch _stopwatch;
		private readonly Action<string, TimeSpan> _onMeasured;

		private TimeMeasure( string activityName, Action<string, TimeSpan> onMeasured )
		{
			_activityName = activityName;
			_onMeasured = onMeasured;
			_stopwatch = Stopwatch.StartNew();
		}

		public static TimeMeasure ToDebug( string activityName )
		{
			return new TimeMeasure( activityName, ( activity, time ) => Debug.WriteLine( $"{time} - {activity}" ) );
		}

		public void Dispose()
		{
			_stopwatch.Stop();
			_onMeasured( _activityName, _stopwatch.Elapsed );
		}
	}
}
