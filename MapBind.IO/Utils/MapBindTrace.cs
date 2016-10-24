using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace MapBind.IO.Utils
{
	public static class MapBindTrace
	{
		private static TraceSource _source;

		public static TraceSource Source 
		{ 
			get { 
				if (_source == null)
				{
					_source = new TraceSource("MapBind.IO", SourceLevels.Error);
				}
				return _source; 
			} 
		}
	}
}
