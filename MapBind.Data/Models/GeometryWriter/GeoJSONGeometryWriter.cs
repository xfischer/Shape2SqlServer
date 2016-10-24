using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MapBind.Data.Models;
using Microsoft.SqlServer.Types;
using System.Diagnostics;

namespace MapBind.Data.Models.GeometryWriter
{
	public sealed class GeoJSONGeometryWriter : GeometryWriterBase<string, double[]>
	{

		List<GeoJson.Feature> _features = new List<GeoJson.Feature>();
		CoordinateConverters.CoordinateConverterBase _coordConverter;


		public void Init(CoordinateConverters.CoordinateConverterBase coordConverter, int outputWidth, int outputHeight, Style.LayerStyle layerStyle, Metrics metrics)
		{
			_coordConverter = coordConverter;
		}


		public override void WritePolygon(Microsoft.SqlServer.Types.SqlGeometry polygon)
		{
			try
			{
				GeoJson.Polygon poly = this.GeoJsonPolygonFromSqlGeometry(polygon.STExteriorRing(), base.GetPolygonInteriorRings(polygon));

				GeoJson.Feature feature = new GeoJson.Feature();
				feature.geometry = poly;
				_features.Add(feature);
			}
			catch (Exception v_ex)
			{
				throw;
			}
		}

		public override void WriteMultiPolygon(Microsoft.SqlServer.Types.SqlGeometry geom)
		{
			try
			{
				List<double[][][]> polygons = new List<double[][][]>(geom.STNumGeometries().Value);
				for (int i = 1; i <= geom.STNumGeometries().Value; i++)
				{
					SqlGeometry curPoly = geom.STGeometryN(i);
					List<SqlGeometry> interiorRings = null;
					int numIntRings = curPoly.STNumInteriorRing().Value;
					if (numIntRings > 0)
					{
						interiorRings = new List<SqlGeometry>(numIntRings);
						for (int j = 1; j <= numIntRings; j++)
							interiorRings.Add(curPoly.STInteriorRingN(j));
					}

					polygons.Add(this.GeoJsonPolygonFromSqlGeometry(geom.STExteriorRing(), interiorRings).coordinates);
				}


				GeoJson.MultiPolygon multiPoly = new GeoJson.MultiPolygon();
				multiPoly.coordinates = polygons.ToArray();
				_features.Add(new GeoJson.Feature() { geometry = multiPoly });
			}
			catch (Exception)
			{
				throw;
			}
		}

		public override void WritePoint(Microsoft.SqlServer.Types.SqlGeometry geom)
		{
			try
			{
				double[] ptCoords = this.ConvertPoint(geom.STPointN(1));
				GeoJson.Point pt = new GeoJson.Point(ptCoords[0], ptCoords[1]);
				_features.Add(new GeoJson.Feature() { geometry = pt });
			}
			catch (Exception)
			{
				throw;
			}
		}

		public override void WriteMultiPoint(Microsoft.SqlServer.Types.SqlGeometry geom)
		{
			try
			{
				List<double[]> ptCoords = new List<double[]>();

				for (int i = 1; i < geom.STNumPoints().Value; i++)
					ptCoords.Add(this.ConvertPoint(geom.STPointN(i)));

				GeoJson.MultiPoint multiPt = new GeoJson.MultiPoint();
				multiPt.coordinates = ptCoords.ToArray();
				_features.Add(new GeoJson.Feature() { geometry = multiPt });
			}
			catch (Exception)
			{
				throw;
			}
		}

		public override void WriteMultiLineString(Microsoft.SqlServer.Types.SqlGeometry geom)
		{
			List<double[][]> lineStrings = new List<double[][]>();
			try
			{
				for (int i = 1; i <= geom.STNumGeometries().Value; i++)
					lineStrings.Add(this.GeoJsonLineStringFromSqlGeometry(geom.STGeometryN(i)).coordinates);
			}
			catch (Exception)
			{
				throw;
			}
			_features.Add(new GeoJson.Feature() { geometry = new GeoJson.MultiLineString() { coordinates = lineStrings.ToArray() } });
		}

		public override void WriteLineString(Microsoft.SqlServer.Types.SqlGeometry geom)
		{
			try
			{
				List<double[]> coords = this.ConvertAndAccumulateDistinctPoints(geom);
				if (coords.Count>0)
				{
					GeoJson.LineString linestring = new GeoJson.LineString() { coordinates = coords.ToArray() };
				_features.Add(new GeoJson.Feature() { geometry = linestring });
				}
			}
			catch (Exception)
			{
				throw;
			}
		}

		public override double[] ConvertPoint(Microsoft.SqlServer.Types.SqlGeometry point)
		{
			double[] coords = new double[2];
			_coordConverter.TransformPoint(point.STX.Value, point.STY.Value, out coords[0], out coords[1]);
			return coords;
		}

		public override string GetOutput()
		{
			return "use GeoJSON instead";
			//GeoJson.FeatureCollection featCol = new GeoJson.FeatureCollection();
			//featCol.features = _features.ToArray();
			//ServiceStack.Text.JsConfig.ExcludeTypeInfo = true;
			//return ServiceStack.Text.JsonSerializer.SerializeToString(featCol, typeof(GeoJson.FeatureCollection));
		}

		public override void Dispose()
		{

		}



		#region Helpers

		private List<double[]> ConvertAndAccumulateDistinctPoints(SqlGeometry pointList)
		{
			List<double[]> list = new List<double[]>();

			double lastX = 0, lastY = 0;
			int numPoints = pointList.STNumPoints().Value;
			for (int i = 1; i <= numPoints; i++)
			{
				SqlGeometry point = pointList.STPointN(i);
				double x, y;
				_coordConverter.TransformPoint(point.STX.Value, point.STY.Value, out x, out y);
				if (i == 1 || !(lastX == x && lastY == y))
					list.Add(new double[] { x, y });

				lastX = x;
				lastY = y;
			}

			return list;

		}

		private GeoJson.Polygon GeoJsonPolygonFromSqlGeometry(Microsoft.SqlServer.Types.SqlGeometry exteriorRing, List<SqlGeometry> interiorRings)
		{
			List<double[][]> ringList = new List<double[][]>();

			try
			{

				List<double[]> extRingPoints = this.ConvertAndAccumulateDistinctPoints(exteriorRing);

				switch (extRingPoints.Count)
				{
					case 0: break;
					case 1:
						Trace.WriteLine("Warning: 1 point for polygon");
						//this.WritePoint_Internal(extRingPoints[0].X, extRingPoints[0].Y);
						break;
					case 2:
						Trace.WriteLine("Warning: 2 points for polygon");
						//_gpStroke.AddLines(extRingPoints.ToArray());
						break;
					default:
						ringList.Add(extRingPoints.ToArray());
						break;
				}

				// Polygones intérieurs
				if (interiorRings != null)
				{

					foreach (SqlGeometry interiorRing in interiorRings)
					{
						List<double[]> intRingPoints = this.ConvertAndAccumulateDistinctPoints(interiorRing);

						switch (intRingPoints.Count)
						{
							case 0: break;
							case 1:
								//bmp.SetPixel(extRingPointArray[0].X, extRingPointArray[0].Y, strokeColor.Color);
								//graphicsPath.AddLine(v_polyInterieurPoints[0], v_polyInterieurPoints[0]);
								break;
							case 2:
								Trace.WriteLine("Warning: 2 points for polygon interior ring");
								//_gpStroke.AddLine(intRingPoints[0], intRingPoints[1]);
								break;
							default:
								double[][] coords = intRingPoints.ToArray();
								ringList.Add(coords);
								break;
						}
					}
				}
			}
			catch (Exception v_ex)
			{
				throw;
			}
			return new GeoJson.Polygon() { coordinates = ringList.ToArray() };
		}

		private GeoJson.LineString GeoJsonLineStringFromSqlGeometry(SqlGeometry geom)
		{
			List<double[]> coords = new List<double[]>();
			try
			{
				for (int i = 1; i <= geom.STNumPoints().Value; i++)
					coords.Add(this.ConvertPoint(geom.STPointN(i)));
			}
			catch (Exception)
			{
				throw;
			}

			return new GeoJson.LineString() { coordinates = coords.ToArray() };
		}


		#endregion
	}


}
