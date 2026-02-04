using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Shape2SqlServer.Core
{
    /// <summary>
    /// Old tracing class
    /// </summary>
    public static class Shape2SqlServerTrace
    {
		private static TraceSource _source;

        /// <summary>
        /// Gets the shared trace source used for logging errors and diagnostic information within the Shape2SqlServer component.
        /// </summary>
        /// <remarks>The returned trace source is initialized with the name "Shape2SqlServer" and a default source level of
        /// <see cref="SourceLevels.Error"/>. This property always returns the same instance for the lifetime of the
        /// application.</remarks>
        public static TraceSource Source 
		{ 
			get { 
				if (_source == null)
				{
					_source = new TraceSource("Shape2SqlServer", SourceLevels.Error);
				}
				return _source; 
			} 
		}
	}
}
