using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MapBind.Data.Models;
using MapBind.Data.Models.Style;
using MapBind.Data.Models.GeometryWriter;
using MapBind.Data.Models.CoordinateConverters;
using System.Diagnostics;
using System.Threading;
using MapBind.Data.Models.BingMaps;
using System.Data.SqlClient;
using System.Configuration;
using Microsoft.SqlServer.Types;

namespace MapBind.Data.Business
{
	public sealed class SqlNativeDataService
	{
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

		#region UseInMemoryCache
		public bool UseInMemoryCache { get; set; }
		#endregion

		#endregion Properties

		public SqlNativeDataService()
		{
			this.GeometryClip = false;
			this.GeometryReduce = false;
			this.GeometryRemoveArtefacts = false;
			this.UseInMemoryCache = true;
			this.MetricsType = enMetricsType.None;
		}

		#region GetData High level

		#region BoundingBoxQuery
		/// <summary>
		/// For each table in the query, return a FeatureCollection.
		/// </summary>
		/// <param name="query">query with a bounding box</param>
		/// <returns>set of GeoJson Features</returns>
		public List<SqlGeometry> GetGeometries(BoundingBoxQuery query)
		{
			List<SqlGeometry> outGeom = new List<SqlGeometry>();
			Metrics metrics = new Metrics(this._metricsType);


			try
			{
				metrics.Start("Global");

				foreach (string table in query.Tables())
				{

					#region Preload cache if necessary

					if (UseInMemoryCache)
					{
						metrics.Start("Cache");
						if (!GeometryCache.IsCacheLoaded(table))
						{
							while (!GeometryCache.IsCacheLoaded(table))
							{
								GeometryCache.LoadCache(table, this.GetConnectionString());
								Trace.WriteLine(string.Format("Thread {0} waiting for cache...", Thread.CurrentThread.ManagedThreadId));
							}
						}
						metrics.Stop("Cache");
					}
					#endregion

					outGeom.AddRange(GetGeometries_BBox(query, metrics, table));

				}


				metrics.Stop("Global");

				#region Calculate metrics to return. (TODO)
				// Calculate metrics to return.
				switch (_metricsType)
				{
					case enMetricsType.OnlyTime:

						string msg = null;
						foreach (var kv in metrics.GetTaskTimes())
						{
							msg += string.Format("{0}: {1,6:###,###} ms{2}", kv.Key, kv.Value.TotalMilliseconds, Environment.NewLine);
						}
						//this.DrawMsgInImage(ref bmp, msg);



						break;

					default:

						break;
				}

				#endregion

			}
			catch (Exception ex)
			{
				throw;
			}

			return outGeom;
		}
		#endregion

		#region BingTileQuery
		public List<SqlGeometry> GetGeometries(BingTileQuery query)
		{

			int tileX, tileY, zoom;
			Metrics metrics = new Metrics(this._metricsType);

			metrics.Start("Global");


			BoundingBoxQuery bboxQuery = BoundingBoxQuery.FromBingTileQuery(query);

			// Get tile coordinates
			BingMapsTileSystem.QuadKeyToTileXY(query.quadKey, out tileX, out tileY, out zoom);


			List<SqlGeometry> outGeom = null;

			foreach (string table in query.Tables())
			{

				
				//bool foundInCache = false;

				//if (query.CacheMode != enDiskCacheMode.None)
				//{
				//  // If cacheMode is Read, see if image exists on disk
				//  bmp = this.LoadTileFromDisc(tileX, tileY, zoom, table);
				//  foundInCache = bmp != null;
				//}

				
					DateTime start = DateTime.Now;
					outGeom= GetGeometries_BBox(bboxQuery, metrics, table);


				//// Save to disc if image not empty and cacheMode is ReadWrite
				//if (query.CacheMode == enDiskCacheMode.ReadWrite
				//    && !foundInCache
				//    && bmp != null
				//    && bmp.Tag == null)
				//  this.SaveTileToDisc(tileX, tileY, zoom, table, bmp);



			}



			return outGeom;


		}



		#endregion

		#region WMS TileQuery
		public List<SqlGeometry> GetGeometries(TileQuery query)
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

			BoundingBox bbox = new BoundingBox(nwLon, seLat, seLon, nwLat);

			return GetGeometries(
				new BoundingBoxQuery()
				{
					BBox = bbox,
					_querytables = query._querytables,
					Width = 256,
					Height = 256,
					SRID = 4326,
					IsBench = query.IsBench
				}
			);
		}
		#endregion

		#endregion


		#region GetGeometries_BBox
		private List<SqlGeometry> GetGeometries_BBox(BoundingBoxQuery query, Metrics metrics, string tableName)
		{
			List<SqlGeometry>  outList = null;
			metrics.Start("Init");
			LayerStyle style = new LayerStyle(query.FillColor, query.StrokeColor, query.strokeThickness);
			SqlGeometryWriter writer = null;

			try
			{


				//bool useGeography = rdr.GetFieldType(0).Name == "SqlGeography";
				writer = new SqlGeometryWriter();
				

				DataService svc = new DataService(this.UseInMemoryCache, this.MetricsType, this.GeometryReduce, this.GeometryRemoveArtefacts, this.GeometryClip);
				outList = svc.GetObjectGeneric_FromCacheBBox(query, metrics, tableName, writer);

			}
			catch (Exception)
			{
				throw;
			}
			finally
			{
				if (writer != null)
				{
					writer.Dispose();
					writer = null;
				}
			}
			return outList;
		}
		#endregion


		#region Helpers


		/// <summary>
		/// Get an open connection using the configured connection string.
		/// </summary>
		/// <returns></returns>
		private SqlConnection GetOpenConnection()
		{
			SqlConnection conn = new SqlConnection(this.GetConnectionString());
			conn.Open();
			return conn;
		}

		private string GetConnectionString()
		{
			return ConfigurationManager.ConnectionStrings["MapBindData"].ConnectionString;
		}



		#endregion
	}
}
