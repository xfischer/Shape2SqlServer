using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shape2SqlServer.Core
{
	internal class BoundingBox
	{
		public double minX { get; set; }
		public double minY { get; set; }
		public double maxX { get; set; }
		public double maxY { get; set; }

		public double Width
		{
			get { return maxX - minX; }
		}

		public double Height
		{
			get { return maxY - minY; }
		}

		public double AspectRatio
		{
			get { return this.Width / this.Height; }
		}

		public BoundingBox(double minx, double miny, double maxx, double maxy)
		{
			this.minX = minx;
			this.minY = miny;
			this.maxX = maxx;
			this.maxY = maxy;
		}

		public BoundingBox()
		{
		}

		public override string ToString()
		{
			return string.Format("[{0}, {1}, {2}, {3}]", minX, minY, maxX, maxY);
		}

		public BoundingBox Clone()
		{
			return new BoundingBox(minX, minY, maxX, maxY);
		}


	}
}


