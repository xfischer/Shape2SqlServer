using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MapBind.Data.Models.Geometry
{
	public interface IGeometry
	{
		enGeometryType Type { get; }
	}
}
