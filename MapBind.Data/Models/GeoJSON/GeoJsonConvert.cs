using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Text;
using System.Linq;
using System.Xml.Linq;
using Microsoft.SqlServer.Types;
using System.Diagnostics;

namespace MapBind.Data.Models.GeoJson
{
	/// <summary>
	/// This class contains conversion methods for going from SqlGeography types
	/// to GeoJson types and vice versa.
	/// </summary>
	public static class GeoJsonConvert
	{
		
		#region GeoJson -> SqlGeography
		/// <summary>
		/// Build an SqlGeography Point from a GeoJson Point.
		/// </summary>
		/// <param name="point">GeoJson Point</param>
		/// <returns>SqlGeography Point</returns>
		public static SqlGeography GeographyFromGeoJsonPoint(Models.GeoJson.Point point, int SRID)
		{
			var geob = new SqlGeographyBuilder();
			geob.SetSrid(SRID);
			geob.BeginGeography(OpenGisGeographyType.Point);
			geob.BeginFigure(point.coordinates[0], point.coordinates[1]);
			geob.EndFigure();
			geob.EndGeography();
			var geog = geob.ConstructedGeography;
			return geog;
		}

		/// <summary>
		/// Build an SqlGeography Polygon from a GeoJson Polygon.
		/// </summary>
		/// <param name="poly">GeoJson Polygon</param>
		/// <returns>SqlGeography Polygon</returns>
		public static SqlGeography GeographyFromGeoJsonPolygon(Models.GeoJson.Polygon poly, int SRID)
		{
			var geob = new SqlGeographyBuilder();
			geob.SetSrid(SRID);
			geob.BeginGeography(OpenGisGeographyType.Polygon);
			geob.BeginFigure(poly.coordinates[0][0][0], poly.coordinates[0][0][1]);
			foreach (var pair in poly.coordinates[0].Skip(1))
			{
				geob.AddLine(pair[0], pair[1]);
			}
			geob.EndFigure();
			geob.EndGeography();
			var geog = geob.ConstructedGeography;
			Debug.WriteLine(geog.AsGml().Value);
			return geog;
		}



		/// <summary>
		/// Build an SqlGeography LineString from a GeoJson LineString.
		/// </summary>
		/// <param name="line">GeoJson LineString</param>
		/// <returns>SqlGeography LineString</returns>
		public static SqlGeography GeographyFromGeoJsonLineString(Models.GeoJson.LineString line, int SRID)
		{
			var geob = new SqlGeographyBuilder();
			geob.SetSrid(SRID);
			geob.BeginGeography(OpenGisGeographyType.LineString);
			geob.BeginFigure(line.coordinates[0][0], line.coordinates[0][1]);
			foreach (var pair in line.coordinates.Skip(1))
			{
				geob.AddLine(pair[0], pair[1]);
			}
			geob.EndFigure();
			geob.EndGeography();
			var geog = geob.ConstructedGeography;
			return geog;
		}

		#endregion GeoJson -> SqlGeography

		#region Helpers

		private static double[] reduceCoords(double lon, double lat, int numDigitsMantissa)
		{
			return new double[] { Math.Round(lon, numDigitsMantissa), Math.Round(lat, numDigitsMantissa) };
		}

		#region SqlGeography

		private static void CheckGeographyType(SqlGeography geography, params string[] permitted)
		{
			string stype = (string)geography.STGeometryType();
			if (!permitted.Contains(stype))
			{
				throw new ArgumentException("This conversion cannot handle " + stype + ", it can only handle the following types: " + string.Join(",", permitted));
			}
		}

		private static double[][][] PolygonCoordinatesFromGeography(SqlGeography polygon, int numDigitsMantissa)
		{
			Debug.Assert((string)polygon.STGeometryType() == "Polygon");

			// convert only the first (exterior) ring
			var ring = polygon.STGeometryN(1);
			var points = new List<double[]>();

			for (int i = 1; i <= ring.STNumPoints(); i++)
			{
				var point = new double[2];
				var sp = ring.STPointN(i);
				//we can safely round the lat/long to 5 decimal places as thats 1.11m at equator, reduces data transfered to client
				point = reduceCoords(sp.Long.Value, sp.Lat.Value, numDigitsMantissa);
				//point[0] = Math.Round((double)sp.Long, numDigitsMantissa);
				//point[1] = Math.Round((double)sp.Lat, numDigitsMantissa);
				points.Add(point);
			}

			var coordinates = new double[1][][];
			coordinates[0] = points.ToArray();
			return coordinates;
		}

		#endregion

		#region SqlGeometry

		private static void CheckGeometryType(SqlGeometry Geometry, params string[] permitted)
		{
			string stype = (string)Geometry.STGeometryType();
			if (!permitted.Contains(stype))
			{
				throw new ArgumentException("This conversion cannot handle " + stype + ", it can only handle the following types: " + string.Join(",", permitted));
			}
		}

		private static double[][][] PolygonCoordinatesFromGeometry(SqlGeometry polygon, int numDigitsMantissa)
		{
			Debug.Assert((string)polygon.STGeometryType() == "Polygon");

			// convert only the first (exterior) ring
			var ring = polygon.STGeometryN(1);
			var points = new List<double[]>();

			for (int i = 1; i <= ring.STNumPoints(); i++)
			{
				var point = new double[2];
				var sp = ring.STPointN(i);
				//we can safely round the lat/long to 5 decimal places as thats 1.11m at equator, reduces data transfered to client
				point = reduceCoords(sp.STX.Value, sp.STY.Value, numDigitsMantissa);
				//point[0] = Math.Round((double)sp.Long, numDigitsMantissa);
				//point[1] = Math.Round((double)sp.Lat, numDigitsMantissa);
				points.Add(point);
			}

			var coordinates = new double[1][][];
			coordinates[0] = points.ToArray();
			return coordinates;
		}

		#endregion

		#endregion Helpers

		#region SqlGeography -> GeoJson

		public static object GeoJsonFromGeography(SqlGeography geography, int numDigitsMantissa)
		{
			object ret = null;

			OpenGisGeographyType geoType;
			if (Enum.TryParse<OpenGisGeographyType>(geography.STGeometryType().ToString(), out geoType))
			{
				switch (geoType)
				{
					case OpenGisGeographyType.GeometryCollection:
						ret = GeoJsonConvert.GeoJsonGeometryCollectionFromGeography(geography, numDigitsMantissa);
						break;
					case OpenGisGeographyType.LineString:
						ret = GeoJsonConvert.GeoJsonLineStringFromGeography(geography, numDigitsMantissa);
						break;
					case OpenGisGeographyType.MultiLineString:
						ret = GeoJsonConvert.GeoJsonMultiLineStringFromGeography(geography, numDigitsMantissa);
						break;
					case OpenGisGeographyType.MultiPoint:
						ret = GeoJsonConvert.GeoJsonMultiPointFromGeography(geography, numDigitsMantissa);
						break;
					case OpenGisGeographyType.MultiPolygon:
						ret = GeoJsonConvert.GeoJsonMultiPolygonFromGeography(geography, numDigitsMantissa);
						break;
					case OpenGisGeographyType.Point:
						ret = GeoJsonConvert.GeoJsonPointFromGeography(geography, numDigitsMantissa);
						break;
					case OpenGisGeographyType.Polygon:
						ret = GeoJsonConvert.GeoJsonPolygonFromGeography(geography, numDigitsMantissa);
						break;

				}
			}
			else
			{
				throw new InvalidCastException("Cannot cast geography type");
			}

			return ret;
		}

		public static object ToGeoJson(this SqlGeography geography, int numDigitsMantissa)
		{
			return GeoJsonConvert.GeoJsonFromGeography(geography, numDigitsMantissa);
		}

		private static Models.GeoJson.GeometryCollection GeoJsonGeometryCollectionFromGeography(SqlGeography geography, int numDigitsMantissa)
		{
			Debug.Assert(geography.STGeometryType().ToString() == OpenGisGeographyType.GeometryCollection.ToString());

			object[] listGeom = new object[geography.STNumGeometries().Value];

			for (var g = 1; g <= geography.STNumGeometries(); g++)
			{
				var geo = geography.STGeometryN(g);
				listGeom[g - 1] = geo.ToGeoJson(numDigitsMantissa);
			}

			Models.GeoJson.GeometryCollection geomCol = new Models.GeoJson.GeometryCollection()
			{
				geometries = listGeom.ToArray()
			};

			return geomCol;
		}

		private static Models.GeoJson.Point GeoJsonPointFromGeography(SqlGeography geography, int numDigitsMantissa)
		{
			Debug.Assert(geography.STGeometryType().ToString() == OpenGisGeographyType.Point.ToString());

			var point = new Models.GeoJson.Point(0, 0) { coordinates = reduceCoords(geography.Long.Value, geography.Lat.Value, numDigitsMantissa) };
			//we can safely round the lat/long to 5 decimal places as thats 1.11m at equator, reduces data transfered to client
			
			//point.coordinates[0] = Math.Round((double)geography.Long, numDigitsMantissa);
			//point.coordinates[1] = Math.Round((double)geography.Lat, numDigitsMantissa);
			return point;
		}

		private static Models.GeoJson.MultiPoint GeoJsonMultiPointFromGeography(SqlGeography geography, int numDigitsMantissa)
		{
			Debug.Assert(geography.STGeometryType().ToString() == OpenGisGeographyType.MultiPoint.ToString());

			var pointscoords = new double[geography.STNumGeometries().Value][];

			for (var g = 1; g <= geography.STNumGeometries(); g++)
			{
				var geo = geography.STGeometryN(g);
				pointscoords[g - 1] = new double[2];
				pointscoords[g - 1] = reduceCoords(geo.Long.Value, geo.Lat.Value, numDigitsMantissa);
				//pointscoords[g - 1][0] = Math.Round((double)geo.Long, numDigitsMantissa);
				//pointscoords[g - 1][1] = Math.Round((double)geo.Lat, numDigitsMantissa);
			}

			var multiPoint = new Models.GeoJson.MultiPoint()
			{
				coordinates = pointscoords
			};

			return multiPoint;
		}

		private static Models.GeoJson.LineString GeoJsonLineStringFromGeography(SqlGeography geography, int numDigitsMantissa)
		{
			Debug.Assert(geography.STGeometryType().ToString() == OpenGisGeographyType.LineString.ToString());

			var points = new List<double[]>();
			for (int i = 1; i <= geography.STNumPoints(); i++)
			{
				var point = new double[2];
				var sp = geography.STPointN(i);
				//we can safely round the lat/long to 5 decimal places as thats 1.11m at equator, reduces data transfered to client
				point = reduceCoords(sp.Long.Value, sp.Lat.Value, numDigitsMantissa);
				//point[0] = Math.Round((double)sp.Long, numDigitsMantissa);
				//point[1] = Math.Round((double)sp.Lat, numDigitsMantissa);
				points.Add(point);
			}
			return new Models.GeoJson.LineString()
			{
				coordinates = points.ToArray()
			};
		}

		private static Models.GeoJson.MultiLineString GeoJsonMultiLineStringFromGeography(SqlGeography geography, int numDigitsMantissa)
		{
			Debug.Assert(geography.STGeometryType().ToString() == OpenGisGeographyType.MultiLineString.ToString());

			var lscoords = new List<double[][]>();
			for (var g = 1; g <= geography.STNumGeometries(); g++)
			{
				var geo = geography.STGeometryN(g);
				var points = new List<double[]>();

				for (int i = 1; i <= geo.STNumPoints(); i++)
				{
					var point = new double[2];
					var sp = geography.STPointN(i);
					//we can safely round the lat/long to 5 decimal places as thats 1.11m at equator, reduces data transfered to client
					point = reduceCoords(sp.Long.Value, sp.Lat.Value, numDigitsMantissa);
					//point[0] = Math.Round((double)sp.Long, numDigitsMantissa);
					//point[1] = Math.Round((double)sp.Lat, numDigitsMantissa);
					points.Add(point);
				}
				lscoords.Add(points.ToArray());
			}
			var mls = new Models.GeoJson.MultiLineString()
			{
				coordinates = lscoords.ToArray()
			};
			return mls;
		}

		private static Models.GeoJson.MultiPolygon GeoJsonMultiPolygonFromGeography(SqlGeography geography, int numDigitsMantissa)
		{
			Debug.Assert(geography.STGeometryType().ToString() == OpenGisGeographyType.MultiPolygon.ToString());

			var polycoords = new List<double[][][]>();

			// For a MultiPolygon the same is repeated for each contained polygon
			for (var g = 1; g <= geography.STNumGeometries(); g++)
			{
				var geo = geography.STGeometryN(g);
				polycoords.Add(PolygonCoordinatesFromGeography(geo, numDigitsMantissa));
			}

			// return a MultiPolygon containing the coordinates of the polygons
			var mpoly = new Models.GeoJson.MultiPolygon()
			{
				coordinates = polycoords.ToArray()
			};
			return mpoly;
		}

		private static Models.GeoJson.Polygon GeoJsonPolygonFromGeography(SqlGeography geography, int numDigitsMantissa)
		{

			Debug.Assert(geography.STGeometryType().ToString() == OpenGisGeographyType.Polygon.ToString());

			var geoType = (string)geography.STGeometryType();

			// return a MultiPolygon containing the coordinates of the polygons
			var mpoly = new Models.GeoJson.Polygon()
			{
				coordinates = PolygonCoordinatesFromGeography(geography, numDigitsMantissa)
			};
			return mpoly;
		}

		#endregion SqlGeography -> GeoJson

		#region SqlGeometry -> GeoJson

		public static object GeoJsonFromGeometry(SqlGeometry Geometry, int numDigitsMantissa)
		{
			object ret = null;

			OpenGisGeometryType geoType;
			if (Enum.TryParse<OpenGisGeometryType>(Geometry.STGeometryType().ToString(), out geoType))
			{
				switch (geoType)
				{
					case OpenGisGeometryType.GeometryCollection:
						ret = GeoJsonConvert.GeoJsonGeometryCollectionFromGeometry(Geometry, numDigitsMantissa);
						break;
					case OpenGisGeometryType.LineString:
						ret = GeoJsonConvert.GeoJsonLineStringFromGeometry(Geometry, numDigitsMantissa);
						break;
					case OpenGisGeometryType.MultiLineString:
						ret = GeoJsonConvert.GeoJsonMultiLineStringFromGeometry(Geometry, numDigitsMantissa);
						break;
					case OpenGisGeometryType.MultiPoint:
						ret = GeoJsonConvert.GeoJsonMultiPointFromGeometry(Geometry, numDigitsMantissa);
						break;
					case OpenGisGeometryType.MultiPolygon:
						ret = GeoJsonConvert.GeoJsonMultiPolygonFromGeometry(Geometry, numDigitsMantissa);
						break;
					case OpenGisGeometryType.Point:
						ret = GeoJsonConvert.GeoJsonPointFromGeometry(Geometry, numDigitsMantissa);
						break;
					case OpenGisGeometryType.Polygon:
						ret = GeoJsonConvert.GeoJsonPolygonFromGeometry(Geometry, numDigitsMantissa);
						break;

				}
			}
			else
			{
				throw new InvalidCastException("Cannot cast Geometry type");
			}

			return ret;
		}

		public static object ToGeoJson(this SqlGeometry Geometry, int numDigitsMantissa)
		{
			return GeoJsonConvert.GeoJsonFromGeometry(Geometry, numDigitsMantissa);
		}

		private static Models.GeoJson.GeometryCollection GeoJsonGeometryCollectionFromGeometry(SqlGeometry Geometry, int numDigitsMantissa)
		{
			Debug.Assert(Geometry.STGeometryType().ToString() == OpenGisGeometryType.GeometryCollection.ToString());

			object[] listGeom = new object[Geometry.STNumGeometries().Value];

			for (var g = 1; g <= Geometry.STNumGeometries(); g++)
			{
				var geo = Geometry.STGeometryN(g);
				listGeom[g - 1] = geo.ToGeoJson(numDigitsMantissa);
			}

			Models.GeoJson.GeometryCollection geomCol = new Models.GeoJson.GeometryCollection()
			{
				geometries = listGeom.ToArray()
			};

			return geomCol;
		}

		private static Models.GeoJson.Point GeoJsonPointFromGeometry(SqlGeometry Geometry, int numDigitsMantissa)
		{
			Debug.Assert(Geometry.STGeometryType().ToString() == OpenGisGeometryType.Point.ToString());

			var point = new Models.GeoJson.Point(0,0);
			//we can safely round the lat/long to 5 decimal places as thats 1.11m at equator, reduces data transfered to client
			point.coordinates = reduceCoords(Geometry.STX.Value, Geometry.STY.Value, numDigitsMantissa);
			//point.coordinates[0] = Math.Round((double)Geometry.Long, numDigitsMantissa);
			//point.coordinates[1] = Math.Round((double)Geometry.Lat, numDigitsMantissa);
			return point;
		}

		private static Models.GeoJson.MultiPoint GeoJsonMultiPointFromGeometry(SqlGeometry Geometry, int numDigitsMantissa)
		{
			Debug.Assert(Geometry.STGeometryType().ToString() == OpenGisGeometryType.MultiPoint.ToString());

			var pointscoords = new double[Geometry.STNumGeometries().Value][];

			for (var g = 1; g <= Geometry.STNumGeometries(); g++)
			{
				var geo = Geometry.STGeometryN(g);
				pointscoords[g - 1] = new double[2];
				pointscoords[g - 1] = reduceCoords(geo.STX.Value, geo.STY.Value, numDigitsMantissa);
				//pointscoords[g - 1][0] = Math.Round((double)geo.Long, numDigitsMantissa);
				//pointscoords[g - 1][1] = Math.Round((double)geo.Lat, numDigitsMantissa);
			}

			var multiPoint = new Models.GeoJson.MultiPoint()
			{
				coordinates = pointscoords
			};

			return multiPoint;
		}

		private static Models.GeoJson.LineString GeoJsonLineStringFromGeometry(SqlGeometry Geometry, int numDigitsMantissa)
		{
			Debug.Assert(Geometry.STGeometryType().ToString() == OpenGisGeometryType.LineString.ToString());

			var points = new List<double[]>();
			for (int i = 1; i <= Geometry.STNumPoints(); i++)
			{
				var point = new double[2];
				var sp = Geometry.STPointN(i);
				//we can safely round the lat/long to 5 decimal places as thats 1.11m at equator, reduces data transfered to client
				point = reduceCoords(sp.STX.Value, sp.STY.Value, numDigitsMantissa);
				//point[0] = Math.Round((double)sp.Long, numDigitsMantissa);
				//point[1] = Math.Round((double)sp.Lat, numDigitsMantissa);
				points.Add(point);
			}
			return new Models.GeoJson.LineString()
			{
				coordinates = points.ToArray()
			};
		}

		private static Models.GeoJson.MultiLineString GeoJsonMultiLineStringFromGeometry(SqlGeometry Geometry, int numDigitsMantissa)
		{
			Debug.Assert(Geometry.STGeometryType().ToString() == OpenGisGeometryType.MultiLineString.ToString());

			var lscoords = new List<double[][]>();
			for (var g = 1; g <= Geometry.STNumGeometries(); g++)
			{
				var geo = Geometry.STGeometryN(g);
				var points = new List<double[]>();

				for (int i = 1; i <= geo.STNumPoints(); i++)
				{
					var point = new double[2];
					var sp = Geometry.STPointN(i);
					//we can safely round the lat/long to 5 decimal places as thats 1.11m at equator, reduces data transfered to client
					point = reduceCoords(sp.STX.Value, sp.STY.Value, numDigitsMantissa);
					//point[0] = Math.Round((double)sp.Long, numDigitsMantissa);
					//point[1] = Math.Round((double)sp.Lat, numDigitsMantissa);
					points.Add(point);
				}
				lscoords.Add(points.ToArray());
			}
			var mls = new Models.GeoJson.MultiLineString()
			{
				coordinates = lscoords.ToArray()
			};
			return mls;
		}

		private static Models.GeoJson.MultiPolygon GeoJsonMultiPolygonFromGeometry(SqlGeometry Geometry, int numDigitsMantissa)
		{
			Debug.Assert(Geometry.STGeometryType().ToString() == OpenGisGeometryType.MultiPolygon.ToString());

			var polycoords = new List<double[][][]>();

			// For a MultiPolygon the same is repeated for each contained polygon
			for (var g = 1; g <= Geometry.STNumGeometries(); g++)
			{
				var geo = Geometry.STGeometryN(g);
				polycoords.Add(PolygonCoordinatesFromGeometry(geo, numDigitsMantissa));
			}

			// return a MultiPolygon containing the coordinates of the polygons
			var mpoly = new Models.GeoJson.MultiPolygon()
			{
				coordinates = polycoords.ToArray()
			};
			return mpoly;
		}

		private static Models.GeoJson.Polygon GeoJsonPolygonFromGeometry(SqlGeometry Geometry, int numDigitsMantissa)
		{

			Debug.Assert(Geometry.STGeometryType().ToString() == OpenGisGeometryType.Polygon.ToString());

			var geoType = (string)Geometry.STGeometryType();

			// return a MultiPolygon containing the coordinates of the polygons
			var mpoly = new Models.GeoJson.Polygon()
			{
				coordinates = PolygonCoordinatesFromGeometry(Geometry, numDigitsMantissa)
			};
			return mpoly;
		}

		#endregion SqlGeometry -> GeoJson
	}
}