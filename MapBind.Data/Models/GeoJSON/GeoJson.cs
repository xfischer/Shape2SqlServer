
namespace MapBind.Data.Models.GeoJson
{
    ///////////////////////////////////////////////////////////////////////////////////////
    //
    // The classes in this namespace will be serialized using the JSON JavaScriptSerializer
    // which is part of MVC2. These classes correspond to the definitions
    // at http://geojson.org
    //
    // Note: the object called "properties" in a Feature is usually implemented as 
	// Dicionary<string,string> but could be an anonymous object if you prefer.


	public class FeatureCollection
	{
		public string type { get { return "FeatureCollection"; } }
		public Feature[] features { get; set; }
	}

	public class Feature
	{
		public string type { get { return "Feature"; } }
		public string id { get; set; }
		public object geometry { get; set; }
		public object properties { get; set; }
	}

	public class GeometryCollection
	{
		public string type { get { return "GeometryCollection"; } }
		public object[] geometries { get; set; }		
	}

	public class Point
	{
		public string type { get { return "Point"; } }
		public double[] coordinates { get; set; }

		public Point(double x, double y)
		{
			coordinates = new double[] { x, y };
		}
	}

	public class MultiPoint
	{
		public string type { get { return "MultiPoint"; } }
		public double[][] coordinates { get; set; }
	}

	public class LineString
	{
		public string type { get { return "LineString"; } }
		public double[][] coordinates { get; set; }
	}

	public class MultiLineString 
	{
		public string type { get { return "MultiLineString"; } }
		public double[][][] coordinates { get; set; }
	}

	public class Polygon
	{
		public string type { get { return "Polygon"; } }
		public double[][][] coordinates { get; set; }
	}

	public class MultiPolygon
	{
		public string type { get { return "MultiPolygon"; } }
		public double[][][][] coordinates { get; set; }
	}

	#region "Old"
	//public class FeatureCollection
	//  {
	//      public string type { get { return GetType().Name; } }
	//      public Feature[] features { get; set; }
	//  }

	//  public class Feature
	//  {
	//      public string type { get { return GetType().Name; } }
	//      public Geometry geometry { get; set; }
	//      public object properties { get; set; }
	//  }

	//  // not currently used
	//  public class GeometryCollection
	//  {
	//      public string type { get { return GetType().Name; } }
	//      public Geometry[] geometries { get; set; }
	//  }

	//  public class Geometry
	//  {
	//      public string type { get { return GetType().Name; } }
	//  }

	//  public class Point : Geometry
	//  {
	//      public double[] coordinates { get; set; }
	//  }

	//  public class LineString : Geometry
	//  {
	//      public double[][] coordinates { get; set; }
	//  }

	//  public class Polygon : Geometry
	//  {
	//      public double[][][] coordinates { get; set; }
	//  }

	//  public class MultiPoint : Geometry
	//  {
	//      public double[][] coordinates { get; set; }
	//  }

	//  public class MultiLineString : Geometry
	//  {
	//      public double[][][] coordinates { get; set; }
	//  }

	//  public class MultiPolygon : Geometry
	//  {
	//      public double[][][][] coordinates { get; set; }
	//  }
#endregion
}