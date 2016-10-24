using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MapBind.Data.Models
{
	public class Metrics
	{
		public int NumberOfMultiPolygon { get; set; }
		public int NumberOfPolygon { get; set; }
		public int NumberOfMultiLineString { get; set; }
		public int NumberOfLineString { get; set; }
		public int NumberOfPoint { get; set; }
		public int NumberOfMultiPoint { get; set; }
		public int NumberOfGemetryCollection { get; set; }

		private Dictionary<string, TimeSpan> _taskTimeSpan { get; set; }
		private Dictionary<string, DateTime> _taskCheckPoints { get; set; }

		private enMetricsType _metricsType;

		public Metrics(enMetricsType MetricsType)
		{
			_metricsType = MetricsType;
			if (_metricsType != enMetricsType.None)
			{
				this._taskTimeSpan = new Dictionary<string, TimeSpan>();
				this._taskCheckPoints = new Dictionary<string, DateTime>();
			}
		}

		public void Start(string taskName)
		{
#if DEBUG
			if (_metricsType != enMetricsType.None)
			{
				_taskCheckPoints[taskName] = DateTime.Now;
				if (!_taskTimeSpan.ContainsKey(taskName))
					_taskTimeSpan[taskName] = new TimeSpan();
			}
#endif
		}

		public void Stop(string taskName)
		{
#if DEBUG
			if (_metricsType != enMetricsType.None)
			{
				_taskTimeSpan[taskName] += DateTime.Now - _taskCheckPoints[taskName];
			}
#endif
		}

		public Dictionary<string, TimeSpan> GetTaskTimes()
		{
			if (_metricsType != enMetricsType.None)
			{
				return _taskTimeSpan;
			}
			else return new Dictionary<string, TimeSpan>();
		}
	}

	[Serializable()]
	public enum enMetricsType
	{
		None,
		OnlyTime,
		TimeAndFeatureCount
	}
}