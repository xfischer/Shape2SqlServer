#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using NetTopologySuite.Geometries;
using ProjNet.CoordinateSystems.Transformations;
using NetTopologySuite.IO;
using Microsoft.Extensions.Logging;

namespace Shape2SqlServer.Core;

internal static class ShapeFileHelper
{
	public static void CheckFiles(string shapeFileName)
	{
		try
		{
			Shape2SqlServerLoggerFactory.Logger.LogInformation("Checking shape files");
			if (!File.Exists(shapeFileName))
				throw new ArgumentException($"File {shapeFileName} not found!");

			string dbfFilePath = GetdBaseFile(shapeFileName);
			if (!File.Exists(dbfFilePath))
				throw new ArgumentException($"File {dbfFilePath} not found!");

			string prjFilePath = GetProjectFile(shapeFileName);
			if (!File.Exists(prjFilePath))
				throw new ArgumentException($"File {prjFilePath} not found!");
		}
		catch (Exception ex)
		{
			Shape2SqlServerLoggerFactory.Logger.LogError(ex, "CheckFiles: {Message}", ex.Message);
			throw;
		}

	}

	public static string GetProjectFile(string shapeFileName)
	{
		return Path.Combine(Directory.GetParent(shapeFileName)!.FullName, $"{Path.GetFileNameWithoutExtension(shapeFileName)}.prj");
	}

	public static string GetdBaseFile(string shapeFileName)
	{
		return Path.Combine(Directory.GetParent(shapeFileName)!.FullName, $"{Path.GetFileNameWithoutExtension(shapeFileName)}.dbf");
	}

	/// <summary>
	/// Returns dictionnary with key: ColName, value: sqltype
	/// </summary>
	/// <param name="fieldDescriptors"></param>
	/// <returns></returns>
	public static List<SqlColumnDescriptor> TranslateDbfTypesToSql(DbaseFieldDescriptor[] fieldDescriptors)
	{
		List<SqlColumnDescriptor> ret = [];
		foreach (DbaseFieldDescriptor desc in fieldDescriptors)
		{
			ret.Add(new(SqlServerModel.CleanSQLName(desc.Name), GetSqlType(desc), desc.Type));
		}

		return ret;
	}

	public static string GetSqlType(DbaseFieldDescriptor dbfType)
	{
		return dbfType.Type.Name switch
		{
			"Int64" => "[bigint]",
			"Byte[]" => "[varbinary](MAX)",
			"Boolean" => "[bit]",
			"DateTime" => "[datetime]",
			"DateTimeOffset" => "[DATETIMEOFFSET]",
			"Decimal" => "[decimal]",
			"Double" => "[float]",
			"Int32" => "[int]",
			"String" or "Char[]" => "[nvarchar](255)",
			"Single" => "[real]",
			"Int16" => "[smallint]",
			"Object" => "[sql_variant]",
			"TimeSpan" => "[time]",
			"Byte" => "[tinyint]",
			"Guid" => "[uniqueidentifier]",
			"byte" => "[varbinary](1)",
			_ => "[nvarchar](MAX)"
		};
	}

	#region ShapeFile Reader converters

	//TODO extract methods (reproject and shape2sql)
	public static Geometry? ReprojectGeometry(Geometry geom, ICoordinateTransformation trans)
	{
		Geometry? geomOut = null; // BUGGY GeometryTransform.TransformGeometry(GeometryFactory.Default, geom, trans.MathTransform);

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

				throw new NotSupportedException($"Type {geom.OgcGeometryType} not supported");
			#endregion NotSupported

			#region Point
			case OgcGeometryType.Point:

				Coordinate[] coordlistPoint = transformCoordinates(trans, ((Point)geom).Coordinates);
				geomOut = new Point(coordlistPoint[0]);

				break;
			#endregion

			#region MultiPoint
			case OgcGeometryType.MultiPoint:

				geomOut = transformMultiPoint(trans, (MultiPoint)geom);

				break;
			#endregion

			#region LineString
			case OgcGeometryType.LineString:

				Coordinate[] coordlist = transformCoordinates(trans, ((LineString)geom).Coordinates);
				geomOut = new LineString(coordlist);

				break;
			#endregion LineString

			#region MultiLineString
			case OgcGeometryType.MultiLineString:

				List<LineString> lines = [];
				foreach (var lineString in ((MultiLineString)geom).Geometries)
				{
					lines.Add(new LineString(transformCoordinates(trans, ((LineString)lineString).Coordinates)));
				}

				geomOut = new MultiLineString(lines.ToArray());

				break;
			#endregion LineString

			#region Polygon
			case OgcGeometryType.Polygon:

				geomOut = transformPolygon(trans, (Polygon)geom);

				break;
			#endregion Polygon

			#region MultiPolygon
			case OgcGeometryType.MultiPolygon:

				MultiPolygon multiPoly = (MultiPolygon)geom;
				List<Polygon> polygons = [];

				foreach (var poly in multiPoly.Geometries)
					polygons.Add(transformPolygon(trans, (Polygon)poly));

				geomOut = new MultiPolygon(polygons.ToArray());

				break;
			#endregion MultiPolygon
		}

		return geomOut;
	}

	public static Envelope ReprojectEnvelope(ICoordinateTransformation? trans, Envelope envelope)
	{
		Envelope ret;
		try
		{
			if (trans == null)
				ret = envelope;
			else
			{
				Coordinate nwCoord = new(envelope.MinX, envelope.MaxY);
				Coordinate seCoord = new(envelope.MaxX, envelope.MinY);
				Coordinate[] retCoords = transformCoordinates(trans, [nwCoord, seCoord]);
				ret = new(retCoords[0], retCoords[1]);
			}
		}
		catch (Exception)
		{
			throw;
		}
		return ret;
	}

	public static Polygon transformPolygon(ICoordinateTransformation? trans, Polygon polygon)
	{
		if (trans == null)
			return polygon;

		Coordinate[] extRing = transformCoordinates(trans, polygon.ExteriorRing.Coordinates);

		List<LinearRing> holes = [];
		foreach (var hole in polygon.Holes)
		{
			Coordinate[] holeCoords = transformCoordinates(trans, hole.Coordinates);
			holes.Add(new(holeCoords));
		}

		return new(new LinearRing(extRing), holes.ToArray());
	}

	public static MultiPoint transformMultiPoint(ICoordinateTransformation? trans, MultiPoint multiPoint)
	{
		if (trans == null)
			return multiPoint;

		Coordinate[] coords = transformCoordinates(trans, multiPoint.Coordinates);

		return new(coords.Select(c => new Point(c)).ToArray());
	}

	public static Coordinate[] transformCoordinates(ICoordinateTransformation? trans, Coordinate[] source)
	{
		if (trans == null)
			return source;

		List<Coordinate> coordlist = new(source.Length);
		foreach (var c in source)
		{
			double[] coords = trans.MathTransform.Transform([c.X, c.Y, c.Z]);
			var coord = new Coordinate(coords[0], coords[1]);
			if (coords.Length > 2) coord.Z = coords[2];

			coordlist.Add(coord);
		}

		return [.. coordlist.Reverse<Coordinate>()];
	}

	#endregion
}
