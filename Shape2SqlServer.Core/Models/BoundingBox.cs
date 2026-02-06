#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shape2SqlServer.Core;

internal class BoundingBox
{
	public double minX { get; set; }
	public double minY { get; set; }
	public double maxX { get; set; }
	public double maxY { get; set; }

	public double Width => maxX - minX;

	public double Height => maxY - minY;

	public double AspectRatio => Width / Height;

	public BoundingBox(double minx, double miny, double maxx, double maxy)
	{
		minX = minx;
		minY = miny;
		maxX = maxx;
		maxY = maxy;
	}

	public BoundingBox()
	{
	}

	public override string ToString() =>
		string.Format("[{0}, {1}, {2}, {3}]", minX, minY, maxX, maxY);

	public BoundingBox Clone() => new(minX, minY, maxX, maxY);
}
