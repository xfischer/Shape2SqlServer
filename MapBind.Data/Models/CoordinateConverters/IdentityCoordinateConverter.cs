using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MapBind.Data.Models.CoordinateConverters
{
	internal sealed class IdentityCoordinateConverter : CoordinateConverterBase
	{
		public override void TransformPoint(double x, double y, out double outX, out double outY)
		{
			outX = x;
			outY = y;
		}
	}
}
