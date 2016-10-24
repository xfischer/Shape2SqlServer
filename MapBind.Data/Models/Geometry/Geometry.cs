using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MapBind.Data.Models.Geometry
{
	public class FeatureCollection 
	{
		public List<Feature> features { get; set; }
	}

	public class Feature 
	{
		public string id { get; set; }
		public IGeometry geometry { get; set; }
		public object properties { get; set; }
	}

	public class GeometryCollection : IGeometry 
	{
		public enGeometryType Type { get { return enGeometryType.GeometryCollection; } }
		public List<IGeometry> geometries { get; set; }

		public GeometryCollection()
		{
			this.geometries = new List<IGeometry>();
		}
	}


	public class Point : IGeometry, IEquatable<Point> 
	{
		public enGeometryType Type { get { return enGeometryType.Point; } }

		public double x { get; set; }
		public double y { get; set; }

		public Point(double x, double y)
		{
			this.x = x;
			this.y = y;
		}

		#region IEquatable<Point> Membres

		public bool Equals(Point other)
		{
			return this.x.Equals(other.x) && this.y.Equals(other.y);
		}

		#endregion
	}

	public class LineString : IGeometry 
	{
		public enGeometryType Type { get { return enGeometryType.LineString; } }

		public List<Point> points { get; set; }

		public LineString()
		{
			this.points = new List<Point>();
		}

		public void AddPoint(Point point)
		{
			points.Add(point);
		}
	}

	public class Polygon : IGeometry 
	{
		public enGeometryType Type { get { return enGeometryType.Polygon; } }
		public LineString exteriorRing { get; set; }
		public List<LineString> interiorRings { get; set; }

		public Polygon()
		{
			this.exteriorRing = new LineString();
			this.interiorRings = new List<LineString>();
		}
	}

	public class MultiPoint : IGeometry 
	{
		public enGeometryType Type { get { return enGeometryType.MultiPoint; } }

		public List<Point> coordinates { get; set; }

		public MultiPoint()
		{
			this.coordinates = new List<Point>();
		}
	}

	public class MultiLineString : IGeometry 
	{
		public enGeometryType Type { get { return enGeometryType.MultiLineString; } }

		public List<LineString> lineStrings { get; set; }

		public MultiLineString()
		{
			this.lineStrings = new List<LineString>();
		}
	}

	public class MultiPolygon : IGeometry 
	{
		public enGeometryType Type { get { return enGeometryType.MultiPolygon; } }

		public List<Polygon> polygons { get; set; }

		public MultiPolygon()
		{
			this.polygons = new List<Polygon>();
		}
	}


	public enum enGeometryType
	{
		MultiPolygon,
		Polygon,
		MultiLineString,
		LineString,
		MultiPoint,
		Point,
		GeometryCollection
	}

}
