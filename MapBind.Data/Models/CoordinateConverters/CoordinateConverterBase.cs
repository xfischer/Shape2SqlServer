using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MapBind.Data.Models.Geometry;
using Microsoft.SqlServer.Types;

namespace MapBind.Data.Models.CoordinateConverters
{
	public abstract class CoordinateConverterBase
	{
		public abstract void TransformPoint(double x, double y, out double outX, out double outY);		
	}


}
