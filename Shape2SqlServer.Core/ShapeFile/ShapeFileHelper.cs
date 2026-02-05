using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using NetTopologySuite.Geometries;
using ProjNet.CoordinateSystems.Transformations;
using NetTopologySuite.IO;
using System.Diagnostics;

namespace Shape2SqlServer.Core
{
	internal static class ShapeFileHelper
	{
		public static void CheckFiles(string shapeFileName)
		{
			try
			{

				Shape2SqlServerTrace.Source.TraceInformation("Checking shape files");
				if (!File.Exists(shapeFileName))
					throw new ArgumentException("File " + shapeFileName + " not found!");

				string dbfFilePath = ShapeFileHelper.GetdBaseFile(shapeFileName);
				if (!File.Exists(dbfFilePath))
					throw new ArgumentException("File " + dbfFilePath + " not found!");

				string prjFilePath = ShapeFileHelper.GetProjectFile(shapeFileName);
				if (!File.Exists(prjFilePath))
					throw new ArgumentException("File " + prjFilePath + " not found!");
			}
			catch (Exception ex)
			{
				Shape2SqlServerTrace.Source.TraceEvent(TraceEventType.Error, 1, "CheckFiles: " + ex.Message);
				throw;
			}

		}

		public static string GetProjectFile(string shapeFileName)
		{
			return Path.Combine(Directory.GetParent(shapeFileName).FullName, Path.GetFileNameWithoutExtension(shapeFileName) + ".prj");
		}

		public static string GetdBaseFile(string shapeFileName)
		{
			return Path.Combine(Directory.GetParent(shapeFileName).FullName, Path.GetFileNameWithoutExtension(shapeFileName) + ".dbf");
		}

		/// <summary>
		/// Returns dictionnary with key: ColName, value: sqltype
		/// </summary>
		/// <param name="fieldDescriptors"></param>
		/// <returns></returns>
		public static List<SqlColumnDescriptor> TranslateDbfTypesToSql(DbaseFieldDescriptor[] fieldDescriptors)
		{
			List<SqlColumnDescriptor> ret = new List<SqlColumnDescriptor>();
			foreach (DbaseFieldDescriptor desc in fieldDescriptors)
			{
				ret.Add(new SqlColumnDescriptor(SqlServerModel.CleanSQLName(desc.Name), GetSqlType(desc), desc.Type));
			}

			return ret;
		}

		public static string GetSqlType(DbaseFieldDescriptor dbfType)
		{

			switch (dbfType.Type.Name)
			{
				case "Int64": return "[bigint]";
				case "Byte[]": return "[varbinary](MAX)";
				case "Boolean": return "[bit]";
				case "DateTime": return "[datetime]";
				case "DateTimeOffset": return "[DATETIMEOFFSET]";
				case "Decimal": return "[decimal]";
				case "Double": return "[float]";
				case "Int32": return "[int]";
				case "String":
				case "Char[]": return "[nvarchar](255)";
				case "Single": return "[real]";
				case "Int16": return "[smallint]";
				case "Object": return "[sql_variant]";
				case "TimeSpan": return "[time]";
				case "Byte": return "[tinyint]";
				case "Guid": return "[uniqueidentifier]";
				case "byte": return "[varbinary](1)";

				default:
					return "[nvarchar](MAX)";
			}
		}


		#region ShapeFile Reader converters

		//TODO extract methods (reproject and shape2sql)
		public static Geometry ReprojectGeometry(Geometry geom, ICoordinateTransformation trans)
		{
			Geometry geomOut = null; // BUGGY GeometryTransform.TransformGeometry(GeometryFactory.Default, geom, trans.MathTransform);


			switch (geom.OgcGeometryType)
			{
				#region NotSupported
				case OgcGeometryType.CircularString:
				case OgcGeometryType.CompoundCurve:
				case OgcGeometryType.Curve:
				case OgcGeometryType.MultiCurve:
				case OgcGeometryType.MultiSurface:
				case OgcGeometryType.PolyhedralSurface:
				case OgcGeometryType.Surface:
				case OgcGeometryType.TIN:
				case OgcGeometryType.GeometryCollection:

					throw new NotSupportedException("Type " + geom.OgcGeometryType.ToString() + " not supported");
				#endregion NotSupported

				#region Point
				case OgcGeometryType.Point:

					Coordinate[] coordlistPoint = ShapeFileHelper.transformCoordinates(trans, ((Point)geom).Coordinates);
					geomOut = new Point(coordlistPoint[0]);

					break;
				#endregion

				#region MultiPoint
				case OgcGeometryType.MultiPoint:

					geomOut = ShapeFileHelper.transformMultiPoint(trans, ((MultiPoint)geom));

					break;
				#endregion

				#region LineString
				case OgcGeometryType.LineString:

					Coordinate[] coordlist = ShapeFileHelper.transformCoordinates(trans, ((LineString)geom).Coordinates);
					geomOut = new LineString(coordlist);

					break;
				#endregion LineString

				#region MultiLineString
				case OgcGeometryType.MultiLineString:

					List<LineString> lines = new List<LineString>();
					foreach (var lineString in ((MultiLineString)geom).Geometries)
					{
						lines.Add(new LineString(ShapeFileHelper.transformCoordinates(trans, ((LineString)lineString).Coordinates)));
					}

					geomOut = new MultiLineString(lines.ToArray());

					break;
				#endregion LineString

				#region Polygon
				case OgcGeometryType.Polygon:

					geomOut = ShapeFileHelper.transformPolygon(trans, (Polygon)geom);

					break;
				#endregion Polygon

				#region MultiPolygon
				case OgcGeometryType.MultiPolygon:

					MultiPolygon multiPoly = ((MultiPolygon)geom);
					List<Polygon> polygons = new List<Polygon>();

					foreach (var poly in multiPoly.Geometries)
						polygons.Add(ShapeFileHelper.transformPolygon(trans, (Polygon)poly));

					geomOut = new MultiPolygon(polygons.ToArray());

					break;
				#endregion MultiPolygon
			}


			return geomOut;
		}

		public static Envelope ReprojectEnvelope(ICoordinateTransformation trans, Envelope envelope)
		{
			Envelope ret;
			try
			{
				if (trans == null)
					ret = envelope;
				else
				{
					Coordinate nwCoord = new Coordinate(envelope.MinX, envelope.MaxY);
					Coordinate seCoord = new Coordinate(envelope.MaxX, envelope.MinY);
					Coordinate[] retCoords = ShapeFileHelper.transformCoordinates(trans, new Coordinate[] { nwCoord, seCoord });
					ret = new Envelope(retCoords[0], retCoords[1]);
				}
			}
			catch (Exception)
			{
				throw;
			}
			return ret;
		}

		public static Polygon transformPolygon(ICoordinateTransformation trans, Polygon polygon)
		{
			if (trans == null)
				return polygon;

			Coordinate[] extRing = ShapeFileHelper.transformCoordinates(trans, polygon.ExteriorRing.Coordinates);

			List<LinearRing> holes = new List<LinearRing>();
			foreach (var hole in polygon.Holes)
			{
				Coordinate[] holeCoords = ShapeFileHelper.transformCoordinates(trans, hole.Coordinates);
				holes.Add(new LinearRing(holeCoords));
			}

			return new Polygon(new LinearRing(extRing), holes.ToArray());
		}

		public static MultiPoint transformMultiPoint(ICoordinateTransformation trans, MultiPoint multiPoint)
		{
			if (trans == null)
				return multiPoint;

			Coordinate[] coords = ShapeFileHelper.transformCoordinates(trans, multiPoint.Coordinates);


			return new MultiPoint(coords.Select(c => new Point(c)).ToArray());
		}

		public static Coordinate[] transformCoordinates(ICoordinateTransformation trans, Coordinate[] source)
		{
			if (trans == null)
				return source;

			List<Coordinate> coordlist = new List<Coordinate>(source.Length);
			foreach (var c in source)
			{
				double[] coords = trans.MathTransform.Transform(new double[] { c.X, c.Y, c.Z });
				var coord = new Coordinate(coords[0], coords[1]); 
				if (coords.Length>2) coord.Z = coords[2]; 
				
				coordlist.Add(coord);
			}

			return coordlist.Reverse<Coordinate>().ToArray();
		}

		#endregion
	}
}
