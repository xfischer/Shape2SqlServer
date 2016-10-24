using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Threading;
using GeoAPI.Geometries;
using MapBind.Data.Models;
using MapBind.Data.Models.BingMaps;
using MapBind.Data.Models.CoordinateConverters;
using MapBind.Data.Models.Raster;
using MapBind.Data.Models.SqlServer;
using Microsoft.SqlServer.Types;
using MapBind.Data.Models.Geometry;
using MapBind.Data.Models.GeometryWriter;
using MapBind.Data.Models.Style;

namespace MapBind.Data.Business
{
	/// <summary>
	/// This is a lightweight class that handles on the fly raster images generation
	/// </summary>
	public sealed class BitmapDataService
	{
		TraceSource _trace = new TraceSource("MapBind.Data");

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

		#region GetData High level

		#region BoundingBoxQuery
		/// <summary>
		/// For each table in the query, return a FeatureCollection.
		/// </summary>
		/// <param name="query">query with a bounding box</param>
		/// <returns>set of GeoJson Features</returns>
		public Bitmap GetImage(BoundingBoxQuery query)
		{

			Bitmap bmp = new Bitmap(query.Width, query.Height);
			Metrics metrics = new Metrics(this._metricsType);


			try
			{
				metrics.Start("Global");

				using (Graphics g = Graphics.FromImage(bmp))
				{
					foreach (string table in query.Tables())
					{
						Bitmap bmpTable = null;

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

						if (query.IsBench)
							bmpTable = GetBenchImageGeneric(query, metrics, table, UseInMemoryCache);
						else if (UseInMemoryCache)
							bmpTable = GetImage_BBox(query, metrics, table);
						//else
						//  bmpTable = GetImageGeneric_FromDB(query, metrics, table, false);

						g.DrawImageUnscaled(bmpTable, 0, 0);
						bmp.Tag = bmpTable.Tag;
						bmpTable.Dispose();

					}
				}

				metrics.Stop("Global");

				#region Calculate metrics to return.
				// Calculate metrics to return.
				switch (_metricsType)
				{
					case enMetricsType.OnlyTime:

						string msg = null;
						foreach (var kv in metrics.GetTaskTimes())
						{
							msg += string.Format("{0}: {1,6:###,###} ms{2}", kv.Key, kv.Value.TotalMilliseconds, Environment.NewLine);
						}
						this.DrawMsgInImage(ref bmp, msg);



						break;

					default:

						break;
				}

				#endregion

			}
			catch (Exception ex)
			{
				#region Write exception in image
				// Write exception in image
				using (Graphics g = Graphics.FromImage(bmp))
				{
					g.Clear(Color.FromArgb(128, Color.Red));

					using (Font font = new Font(FontFamily.GenericMonospace, 11, FontStyle.Regular, GraphicsUnit.Pixel))
						g.DrawString(ex.ToString(), font, Brushes.Black, new RectangleF(0, 0, 256, 256));
				}
				#endregion
			}

			return bmp;
		}
		#endregion

		#region BingTileQuery
		public Bitmap GetImage(BingTileQuery query)
		{

			int tileX, tileY, zoom;
			Metrics metrics = new Metrics(this._metricsType);

			metrics.Start("Global");


			BoundingBoxQuery bboxQuery = BoundingBoxQuery.FromBingTileQuery(query);

			// Get tile coordinates
			BingMapsTileSystem.QuadKeyToTileXY(query.quadKey, out tileX, out tileY, out zoom);


			Queue<Bitmap> bmpQueue = new Queue<Bitmap>();

			foreach (string table in query.Tables())
			{

				Bitmap bmp = null;
				bool foundInCache = false;

				if (query.CacheMode != enDiskCacheMode.None)
				{
					// If cacheMode is Read, see if image exists on disk
					bmp = this.LoadTileFromDisc(tileX, tileY, zoom, table);
					foundInCache = bmp != null;
				}

				if (bmp == null)
				{

					DateTime start = DateTime.Now;
					if (bboxQuery.IsBench)
						bmp = GetBenchImageGeneric(bboxQuery, metrics, table, UseInMemoryCache);
					else 
					{
						//string test = GetGeoJson_BBox(bboxQuery, metrics, table);
						bmp = GetImage_BBox(bboxQuery, metrics, table);
					}
					//else
					//  bmp = GetImageGeneric_FromDB(bboxQuery, metrics, table, false);

				}

				// Save to disc if image not empty and cacheMode is ReadWrite
				if (query.CacheMode == enDiskCacheMode.ReadWrite
						&& !foundInCache
						&& bmp != null
						&& bmp.Tag == null)
					this.SaveTileToDisc(tileX, tileY, zoom, table, bmp);


				bmpQueue.Enqueue(bmp);
			}

			metrics.Stop("Global");

			if (bmpQueue.Count <= 1)
			{
				Bitmap bmpOut = bmpQueue.Dequeue();
				#region Calculate metrics to return.
				// Calculate metrics to return.


				switch (_metricsType)
				{
					case enMetricsType.OnlyTime:

						string msg = string.Empty;
						foreach (var kv in metrics.GetTaskTimes())
						{
							msg += string.Format("{0}: {1,6:###,###} ms{2}", kv.Key, kv.Value.TotalMilliseconds, Environment.NewLine);
						}
						this.DrawMsgInImage(ref bmpOut, msg);



						break;

					default:

						break;
				}

				#endregion

				return bmpOut;

			}
			else
			{
				Bitmap outBmp = bmpQueue.Dequeue();
				using (Graphics g = Graphics.FromImage(outBmp))
				{
					do
					{

						Bitmap curBmp = bmpQueue.Dequeue();
						if (curBmp.Tag != null)
							outBmp.Tag = curBmp.Tag;

						g.DrawImageUnscaled(curBmp, 0, 0);

						curBmp.Dispose();
					}
					while (bmpQueue.Count > 0);
				}

				return outBmp;
			}


		}

		#region Tile Load/Save from Disc
		private Bitmap LoadTileFromDisc(int tileX, int tileY, int zoom, string tableName)
		{
			RasterFileSystem rasterFS = new RasterFileSystem(SqlServerModel.GetSqlDatabaseName(this.GetConnectionString()), tableName);
			return rasterFS.GetTileBitmap(zoom, tileX, tileY);
		}

		private void SaveTileToDisc(int tileX, int tileY, int zoom, string tableName, Bitmap bmp)
		{
			if (bmp != null && bmp.Tag == null)
			{
				RasterFileSystem rasterFS = new RasterFileSystem(SqlServerModel.GetSqlDatabaseName(this.GetConnectionString()), tableName);
				rasterFS.SaveTileBitmap(bmp, ImageFormat.Png, zoom, tileX, tileY);
			}
		}
		#endregion

		#endregion

		#region WMS TileQuery
		public Bitmap GetImage(TileQuery query)
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

			return GetImage(
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

		#region Main Drawing

		#region GetBenchImageGeneric
		private Bitmap GetBenchImageGeneric(BoundingBoxQuery query, Metrics metrics, string tableName, bool useCache)
		{
			Bitmap bmp = new Bitmap(query.Width, query.Height);

			try
			{

				string strQuery = null;
				if (useCache)
					strQuery = SqlServerModel.GenerateGetGeomIdInBBoxScript(tableName, query.BBox);
				else
					strQuery = SqlServerModel.GenerateGetGeomInBBoxScript(tableName, query.BBox);

				SqlGeometry bboxGeom = SqlServerModel.GeometryFromBoundingBox(query.BBox);


				//start = DateTime.Now;

				//IList<int> resultsWithIndex = _spatialIndex.Query(new Envelope(query.BBox.minX, query.BBox.maxX, query.BBox.minY, query.BBox.maxY));
				//foreach (int id in resultsWithIndex)
				//{
				//  SqlGeometry geom = GetGeomFromCache(tableName, id);
				//}

				//metrics.TaskTimesMilliseconds.Add(string.Format("quadTree {0} items", resultsWithIndex.Count), (int)(DateTime.Now - start).TotalMilliseconds);


				metrics.Start("STR");

				IList<int> resultsWithIndexSTR = GeometryCache.Query(tableName, new Envelope(query.BBox.minX, query.BBox.maxX, query.BBox.minY, query.BBox.maxY));
				foreach (int id in resultsWithIndexSTR)
				{
					SqlGeometry geom = GeometryCache.GetGeometry(tableName, id);
				}

				metrics.Stop("STR");
				Trace.WriteLine(string.Format("STR {0} items", resultsWithIndexSTR.Count));

				//List<SqlGeometry> vlist =  ImageryDataService._geomCacheByTableThenId[tableName].Values.Where(g => g.STIntersects(bboxGeom).Value).ToList();

				metrics.Start("DB");

				int nbRecords = 0;
				using (var conn = GetOpenConnection())
				{

					using (var cmd = new SqlCommand(strQuery, conn))
					{
						cmd.CommandType = System.Data.CommandType.Text;

						using (var rdr = cmd.ExecuteReader(System.Data.CommandBehavior.SequentialAccess))
						{
							while (rdr.Read())
							{
								int id = rdr.GetSqlInt32(0).Value;
								SqlGeometry geom = GeometryCache.GetGeometry(tableName, id);
								nbRecords++;
							}
							rdr.Close();
						}
					}
				}

				metrics.Stop("DB");
				Trace.WriteLine(string.Format("DB {0} items", nbRecords));



			}
			catch (Exception)
			{
				throw;
			}
			return bmp;
		}
		#endregion
			
		#region GetImage_FromCacheBBox
		private Bitmap GetImage_BBox(BoundingBoxQuery query, Metrics metrics, string tableName)
		{
			Bitmap bmpOut = null;
			metrics.Start("Init");
			LayerStyle style = new LayerStyle(query.FillColor, query.StrokeColor, query.strokeThickness);
			GDIBitmapGeometryWriter writer = null;

			try
			{

				//bool useGeography = rdr.GetFieldType(0).Name == "SqlGeography";
				BitmapCoordConverter coordConverter = new BitmapCoordConverter(query.Width, query.Height, query.BBox);
				writer = new GDIBitmapGeometryWriter(SmoothingMode.AntiAlias, coordConverter);
				writer.Init(query.Width, query.Height, style, metrics);

				DataService svc = new DataService(this.UseInMemoryCache, this.MetricsType, this.GeometryReduce, this.GeometryRemoveArtefacts, this.GeometryClip);
				bmpOut = svc.GetObjectGeneric_FromCacheBBox<Bitmap, System.Drawing.Point>(query, metrics, tableName, writer);
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
			return bmpOut;
		}
		#endregion
		
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

		private void DrawMsgInImage(ref Bitmap bmp, string message)
		{
			using (Graphics g = Graphics.FromImage(bmp))
			{
				g.DrawRectangle(Pens.Red, 0, 0, bmp.Width - 1, bmp.Height - 1);

				using (Font v_font = new Font(FontFamily.GenericMonospace, 11, FontStyle.Regular, GraphicsUnit.Pixel))
				{
					SizeF v_mesure = g.MeasureString(message, v_font);
					int width = (int)v_mesure.Width;
					int height = (int)v_mesure.Height;
					g.FillRectangle(Brushes.White, 1, 1, width + 1, height + 1);
					g.DrawString(message, v_font, Brushes.Black, new RectangleF(1, 1, bmp.Width - 2, bmp.Height - 2));
				}
			}
		}

		#endregion
	}
}