using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Diagnostics;

using System.Data.SqlClient;
using System.Configuration;
using System.Xml.Linq;
using System.IO;
using Microsoft.SqlServer.Types;
using MapBind.Data.Models;
using MapBind.Data.Models.GeoJson;
using MapBind.Data.Models.SqlServer;

namespace MapBind.Data.Business
{
	/// <summary>
	/// This is a lightweight class that handles all data retrieval.
	/// </summary>
	public class GeoJSONDataService
	{
		private bool useGeography = false;

		#region Properties

		#region GeometryClip
		private bool _geometryClip = false;
		public bool GeometryClip
		{
			get { return _geometryClip; }
			set { _geometryClip = value; }
		}
		#endregion

		#region GeometryRemoveArtefacts
		private bool _geometryRemoveArtefacts = true;
		public bool GeometryRemoveArtefacts
		{
			get { return _geometryRemoveArtefacts; }
			set { _geometryRemoveArtefacts = value; }
		}
		#endregion

		#region GeometryReduce
		private bool _geometryReduce = true;
		public bool GeometryReduce
		{
			get { return _geometryReduce; }
			set { _geometryReduce = value; }
		}
		#endregion

		#region MetricsType
		private enMetricsType _metricsType = enMetricsType.OnlyTime;
		public enMetricsType MetricsType
		{
			get { return _metricsType; }
			set { _metricsType = value; }
		}
		#endregion

		public static class TableName
		{
			public const string ne_10m_admin_0_countries = "ne_10m_admin_0_countries";
			public const string ne_10m_admin_1_states_provinces_shp = "ne_10m_admin_1_states_provinces_shp";
			public const string COMMUNE = "COMMUNE";
			public const string COMMUNE_geom = "COMMUNE_geom";
		}
		#endregion Properties

		#region GetData High level

		/// <summary>
		/// For each table in the query, return a FeatureCollection.
		/// </summary>
		/// <param name="query">query with a bounding box</param>
		/// <returns>set of GeoJson Features</returns>
		public GeoJsonResult GetGeoJsonData(BoundingBoxQuery query)
		{
			var data = new GeoJsonResult();
			var start = DateTime.Now;
			try
			{
				foreach (string table in query.Tables)
				{
					if (query.useGeography)
						data.featureSet.Add(table, GetGeoJsonGeneric((SqlGeography)query.Box, query.Resolution, query.NumDigits, table));
					else
						data.featureSet.Add(table, GetGeoJsonGeneric((SqlGeometry)query.Box, query.Resolution, query.NumDigits, table));
				}
			}
			catch (Exception ex)
			{
				data = new GeoJsonResult() { error = "Something went wrong : " + ex.Message };
				Debug.WriteLine(data.error);
			}

			#region Calculate metrics to return.
			// Calculate metrics to return.
			var elapsed = DateTime.Now - start;

			// This counting of the number of points is based on the simplified geometries being returned.
			// For example, only the points in the first ring of polygons is counted since we only use the first ring
			// (which is another name for the outside border).
			// The number of points is not needed for drawing, it is just being returned in case you are interested.
			// If you like, you can extend the counting functions below.

			switch (_metricsType)
			{
				case enMetricsType.None:
					break;
				default:
					data.metrics.QueryTimeMilliseconds = (int)elapsed.TotalMilliseconds;
					break;
			}


			switch (_metricsType)
			{
				case enMetricsType.TimeAndFeatureCount:

					data.metrics.NumberOfGemetryCollection = data.featureSet.Values.SelectMany(fc => fc.features).Select(f => f.geometry).OfType<Models.GeoJson.GeometryCollection>().Count();
					data.metrics.NumberOfLineString = data.featureSet.Values.SelectMany(fc => fc.features).Select(f => f.geometry).OfType<Models.GeoJson.LineString>().Count();
					data.metrics.NumberOfMultiLineString = data.featureSet.Values.SelectMany(fc => fc.features).Select(f => f.geometry).OfType<Models.GeoJson.MultiLineString>().Count();
					data.metrics.NumberOfMultiPoint = data.featureSet.Values.SelectMany(fc => fc.features).Select(f => f.geometry).OfType<Models.GeoJson.MultiPoint>().Count();
					data.metrics.NumberOfMultiPolygon = data.featureSet.Values.SelectMany(fc => fc.features).Select(f => f.geometry).OfType<Models.GeoJson.MultiPolygon>().Count();
					data.metrics.NumberOfPoint = data.featureSet.Values.SelectMany(fc => fc.features).Select(f => f.geometry).OfType<Models.GeoJson.Point>().Count();
					data.metrics.NumberOfPolygon = data.featureSet.Values.SelectMany(fc => fc.features).Select(f => f.geometry).OfType<Models.GeoJson.Polygon>().Count();


					//data.metrics.NumberOfPoints += data.featureSet.Values.SelectMany(fc => fc.features).Select(f => f.geometry).OfType<Models.GeoJson.Point>().Count();
					//data.metrics.NumberOfPoints += data.featureSet.Values.SelectMany(fc => fc.features).Select(f => f.geometry).OfType<Models.GeoJson.MultiPoint>().Select(mp => mp.coordinates.Length).Sum();
					//data.metrics.NumberOfPoints += data.featureSet.Values.SelectMany(fc => fc.features).Select(f => f.geometry).OfType<Models.GeoJson.LineString>().Select(ls => ls.coordinates.Length).Sum();
					//data.metrics.NumberOfPoints += data.featureSet.Values.SelectMany(fc => fc.features).Select(f => f.geometry).OfType<Models.GeoJson.MultiLineString>().SelectMany(mls => mls.coordinates).Select(a => a.Length).Sum();
					//data.metrics.NumberOfPoints += data.featureSet.Values.SelectMany(fc => fc.features).Select(f => f.geometry).OfType<Models.GeoJson.Polygon>().Select(p => p.coordinates[0].Length).Sum();
					//data.metrics.NumberOfPoints += data.featureSet.Values.SelectMany(fc => fc.features).Select(f => f.geometry).OfType<Models.GeoJson.MultiPolygon>().SelectMany(p => p.coordinates).Select(pa => pa[0].Length).Sum();


					break;
				default:
					break;
			}
			#endregion

			return data;
		}

		public GeoJsonResult GetGeoJsonData(BingTileQuery query)
		{
			int tileX;
			int tileY;
			int zoom;
			int nwX;
			int nwY;
			double nwLat;
			double nwLon;
			double seLat;
			double seLon;


			BingMapsTileSystem.QuadKeyToTileXY(query.quadKey, out tileX, out tileY, out zoom);
			BingMapsTileSystem.TileXYToPixelXY(tileX, tileY, out nwX, out nwY);
			BingMapsTileSystem.PixelXYToLatLong(nwX, nwY, zoom, out nwLat, out nwLon);
			BingMapsTileSystem.PixelXYToLatLong(nwX + 256, nwY + 256, zoom, out seLat, out seLon);
			double res = BingMapsTileSystem.GroundResolution(seLat + (nwLat - seLat) / 2d, zoom);
			int numDigits = BingMapsTileSystem.UsefulDigits(res);

			object bbox = null;
			if (useGeography)
				bbox = SqlServerModel.GeograhyFromBoundingBoxNwSe(nwLat, nwLon, seLat, seLon, 4326);
			else
				bbox = SqlServerModel.GeometryFromBoundingBoxNwSe(nwLat, nwLon, seLat, seLon, 4326);

			return GetGeoJsonData(
					new BoundingBoxQuery()
					{
						useGeography = useGeography,
						Box = bbox,
						Resolution = res,
						NumDigits = numDigits,
						Tables = query.Tables
					}
				);


		}

		public GeoJsonResult GetGeoJsonData(TileQuery query)
		{
			int tileX = query.X;
			int tileY = query.Y;
			int zoom = query.Z;
			int nwX;
			int nwY;
			double nwLat;
			double nwLon;
			double seLat;
			double seLon;

			BingMapsTileSystem.TileXYToPixelXY(tileX, tileY, out nwX, out nwY);
			BingMapsTileSystem.PixelXYToLatLong(nwX, nwY, zoom, out nwLat, out nwLon);
			BingMapsTileSystem.PixelXYToLatLong(nwX + 256, nwY + 256, zoom, out seLat, out seLon);
			double res = BingMapsTileSystem.GroundResolution(seLat + (nwLat - seLat) / 2d, zoom);
			int numDigits = BingMapsTileSystem.UsefulDigits(res);

			object box = null;
			if (useGeography)
				box = SqlServerModel.GeograhyFromBoundingBoxNwSe(nwLat, nwLon, seLat, seLon, query.SRID);
			else
				box = SqlServerModel.GeometryFromBoundingBoxNwSe(nwLat, nwLon, seLat, seLon, query.SRID);

			return GetGeoJsonData(
				new BoundingBoxQuery()
				{
					useGeography = useGeography,
					Box = box,
					Resolution = res,
					NumDigits = numDigits,
					Tables = query.Tables
				}
			);
		}

		#endregion

		#region Data Access and transform

		private Models.GeoJson.FeatureCollection GetGeoJsonGeneric(SqlGeography box, double resolution, int numDigits, string tableName)
		{
			if (resolution == 0) resolution = 1500d;
			if (numDigits == 0) numDigits = 5;

			var features = new List<Models.GeoJson.Feature>();

			try
			{

				using (var conn = GetOpenConnection())
				{
					using (var cmd = new SqlCommand("SL_" + tableName + "_InBounds", conn))
					{
						cmd.CommandType = System.Data.CommandType.StoredProcedure;

						cmd.Parameters.Add(new SqlParameter("@bbox", System.Data.SqlDbType.Udt)
						{
							UdtTypeName = "geography",
							Value = box
						});
						if (_geometryReduce)
							cmd.Parameters.AddWithValue("@res", (float)resolution / 2);
						else
							cmd.Parameters.AddWithValue("@res", DBNull.Value);


						using (var rdr = cmd.ExecuteReader())
						{
							while (rdr.Read())
							{

								// get fields
								object geometry = null;
								Dictionary<string, string> properties = null;

								for (int i = 0; i < rdr.FieldCount; i++)
								{

									var fieldName = rdr.GetName(i);
									switch (fieldName)
									{
										case "geom":

											try
											{
												// get geography and translate to GeoJson
												// Reserved field names for geom are : geom, geomClip, geomWKB
												if (!rdr.IsDBNull(i))
												{
													var geography = (SqlGeography)rdr["geom"];

													if (this.GeometryClip)
														geography = geography.STIntersection(box);


													if (this.GeometryRemoveArtefacts)
														geography = SqlServerModel.DoRemoveArtifacts(geography);

													if (geography != null && !geography.STIsEmpty())
														geometry = geography.ToGeoJson(numDigits);
												}
											}
											catch (Exception)
											{
												throw;
											}

											break;

										default:

											//if (!rdr.IsDBNull(i))
											//{
											//  if (properties == null)
											//    properties = new Dictionary<string, string>();

											//  properties.Add(fieldName, rdr[i].ToString());
											//}

											break;
									}
								}




								if (geometry != null)
								{
									var feature = new Models.GeoJson.Feature()
									{
										geometry = geometry,
										properties = properties
									};
									features.Add(feature);
								}
							}
						}
					}
				}
			}
			catch (Exception)
			{
				throw;
			}


			var fc = new Models.GeoJson.FeatureCollection()
			{
				features = features.ToArray()
			};
			return fc;
		}
		private Models.GeoJson.FeatureCollection GetGeoJsonGeneric(SqlGeometry box, double resolution, int numDigits, string tableName)
		{
			if (resolution == 0) resolution = 1500d;
			if (numDigits == 0) numDigits = 5;

			var features = new List<Models.GeoJson.Feature>();

			try
			{

				using (var conn = GetOpenConnection())
				{
					using (var cmd = new SqlCommand("SL_" + tableName + "_InBounds_geom", conn))
					{
						cmd.CommandType = System.Data.CommandType.StoredProcedure;

						cmd.Parameters.Add(new SqlParameter("@bbox", System.Data.SqlDbType.Udt)
						{
							UdtTypeName = "geometry",
							Value = box
						});
						if (_geometryReduce)
							cmd.Parameters.AddWithValue("@res", (float)resolution / 2);
						else
							cmd.Parameters.AddWithValue("@res", DBNull.Value);


						using (var rdr = cmd.ExecuteReader())
						{
							while (rdr.Read())
							{

								// get fields
								object outGeometry = null;
								Dictionary<string, string> properties = null;

								for (int i = 0; i < rdr.FieldCount; i++)
								{

									var fieldName = rdr.GetName(i);
									switch (fieldName)
									{
										case "geom":

											try
											{
												// get geography and translate to GeoJson
												// Reserved field names for geom are : geom, geomClip, geomWKB
												if (!rdr.IsDBNull(i))
												{
													var geometry = (SqlGeometry)rdr["geom"];

													if (this.GeometryClip)
														geometry = geometry.STIntersection(box);


													if (this.GeometryRemoveArtefacts)
														geometry = SqlServerModel.DoRemoveArtifacts(geometry);

													if (geometry != null && !geometry.STIsEmpty())
														outGeometry = geometry.ToGeoJson(numDigits);
												}
											}
											catch (Exception)
											{
												throw;
											}

											break;

										default:

											//if (!rdr.IsDBNull(i))
											//{
											//  if (properties == null)
											//    properties = new Dictionary<string, string>();

											//  properties.Add(fieldName, rdr[i].ToString());
											//}

											break;
									}
								}




								if (outGeometry != null)
								{
									var feature = new Models.GeoJson.Feature()
									{
										geometry = outGeometry,
										properties = properties
									};
									features.Add(feature);
								}
							}
						}
					}
				}
			}
			catch (Exception)
			{
				throw;
			}


			var fc = new Models.GeoJson.FeatureCollection()
			{
				features = features.ToArray()
			};
			return fc;
		}

		/// <summary>
		/// Get an open connection using the configured connection string.
		/// </summary>
		/// <returns></returns>
		private SqlConnection GetOpenConnection()
		{
			string connStr = ConfigurationManager.ConnectionStrings["MapBindData"].ConnectionString;
			SqlConnection conn = new SqlConnection(connStr);
			conn.Open();
			return conn;
		}

		#endregion

	}
}