using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MapBind.Data.Models;
using MapBind.Data.Models.GeometryWriter;
using Microsoft.SqlServer.Types;
using MapBind.Data.Models.SqlServer;
using GeoAPI.Geometries;
using System.Diagnostics;
using System.Data.SqlClient;
using System.Configuration;

namespace MapBind.Data.Business
{
	public sealed class DataService
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
		private bool _useInMemoryCache = false;
		public bool UseInMemoryCache
		{
			get { return _useInMemoryCache; }
			set { _useInMemoryCache = value; }
		}
		#endregion

		#endregion Properties

		public DataService() : this(true, enMetricsType.None, false, false, false) { }

		public DataService(bool useInMemoryCache, enMetricsType metricsType, bool reduceGeometry, bool removeArtefacts, bool clipGeometry)
		{
			_useInMemoryCache = useInMemoryCache;
			_metricsType = metricsType;
			_geometryReduce = reduceGeometry;
			_geometryRemoveArtefacts = removeArtefacts;
			_geometryClip = clipGeometry;
		}



		#region GetObjectGeneric_FromCacheBBox
		public Toutput GetObjectGeneric_FromCacheBBox<Toutput, Tpoint>(BoundingBoxQuery query, Metrics metrics, string tableName, GeometryWriterBase<Toutput, Tpoint> writer)
		{
			Toutput objOut = default(Toutput);
			metrics.Start("Init");

			try
			{
				double reduceTolerance = Math.Min(query.BBox.Width / query.Width, query.BBox.Height / query.Height); // degrees per pixel / 2 
				double reduceToleranceMeters = reduceTolerance * 6378137;
				//double pixelArea = Math.Pow(BingMapsTileSystem.GroundResolution(query.BBox.maxY, query.ZoomLevel), 2); // mapResolution * mapResolution						
				double pixelRadiansAreaXY = ((query.BBox.maxX - query.BBox.minX) / query.Width) * ((query.BBox.maxY - query.BBox.minY) / query.Height);
				int numPixels = 0;

				#region Get data from cache or open DB

				if (UseInMemoryCache)
				{
					#region From Cache
					#region Preload cache

					metrics.Start("Cache");
					if (!GeometryCache.IsCacheLoaded(query.Tables()[0]))
					{
						while (!GeometryCache.IsCacheLoaded(query.Tables()[0]))
						{
							GeometryCache.LoadCache(query.Tables()[0], this.GetConnectionString());
							Trace.WriteLine(string.Format("Thread {0} waiting for cache...", System.Threading.Thread.CurrentThread.ManagedThreadId));
						}
					}
					metrics.Stop("Cache");

					#endregion

					SqlGeometry bboxGeom = SqlServerModel.GeometryFromBoundingBox(query.BBox);
					IList<int> resultsWithIndexSTR = GeometryCache.Query(tableName, new Envelope(query.BBox.minX, query.BBox.maxX, query.BBox.minY, query.BBox.maxY));

					if (resultsWithIndexSTR.Count > 0)
					{
						foreach (int id in resultsWithIndexSTR)
						{
							SqlGeometry geom = GeometryCache.GetGeometry(tableName, id);
							double geomArea = GeometryCache.GetGeometryArea(tableName, id);
							
							bool geomIsPixel = geomArea > 0 && geomArea <= pixelRadiansAreaXY;
							
							
							metrics.Start("Process");
							ProcessGeometry<Toutput, Tpoint>(writer, reduceTolerance, pixelRadiansAreaXY, ref numPixels, ref geom, geomArea);
							metrics.Stop("Process");

						}
						metrics.Start("GetOutput");
						objOut = writer.GetOutput();
						metrics.Stop("GetOutput");
					}

					#endregion
				}
				else
				{
					#region Get data from SQL DB
						
					using (SqlConnection conn = GetOpenConnection())
					{
						string strQuery = SqlServerModel.GenerateGetGeomInBBoxScript(tableName, query.BBox);

						using (var cmd = new SqlCommand(strQuery, conn))
						{
							cmd.CommandType = System.Data.CommandType.Text;

							using (var rdr = cmd.ExecuteReader(System.Data.CommandBehavior.SequentialAccess))
							{
								if (rdr.HasRows)
								{
									while (rdr.Read())
									{
										metrics.Start("Area");
										SqlGeometry geom = SqlGeometry.STGeomFromWKB(rdr.GetSqlBytes(0), 4326);
										double geomArea = geom.STArea().Value; // optimize this by having area field in DB
										metrics.Stop("Area");

										metrics.Start("Process");
										ProcessGeometry<Toutput, Tpoint>(writer, reduceTolerance, pixelRadiansAreaXY, ref numPixels, ref geom, geomArea);
										metrics.Stop("Process");
									}
									metrics.Start("GetOutput");
									objOut = writer.GetOutput();
									metrics.Stop("GetOutput");
								}


								rdr.Close();
							}
						}
					}

					#endregion
				}
				#endregion



			}
			catch (Exception)
			{
				throw;
			}


			return objOut;
		}

		private void ProcessGeometry<Toutput, Tpoint>(GeometryWriterBase<Toutput, Tpoint> writer, double reduceTolerance, double pixelRadiansAreaXY, ref int numPixels, ref SqlGeometry geom, double geomArea)
		{
			bool geomIsPixel = geomArea > 0 && geomArea <= pixelRadiansAreaXY;

			if (!geomIsPixel && _geometryReduce)
				geom = geom.Reduce(reduceTolerance);
			if (_geometryRemoveArtefacts)
				geom = SqlServerModel.DoRemoveArtefacts(geom);

			if (geomIsPixel)
			{
				numPixels++;
				writer.WritePoint(geom.STPointN(1));
			}
			else
			{
				writer.WriteGeometry(geom);
			}
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

		private void CloseAndDisposeConnection(SqlConnection connection)
		{
			try
			{
				if (connection != null && connection.State != System.Data.ConnectionState.Closed)
					connection.Close();
			}
			finally
			{
				try
				{
					if (connection != null)
						connection.Dispose();
				}
				finally
				{
					connection = null;
				}
			}
		}

		private string GetConnectionString()
		{
			return ConfigurationManager.ConnectionStrings["MapBindData"].ConnectionString;
		}

		//private void DrawMsgInImage(ref Bitmap bmp, string message)
		//{
		//  using (Graphics g = Graphics.FromImage(bmp))
		//  {
		//    g.DrawRectangle(Pens.Red, 0, 0, bmp.Width - 1, bmp.Height - 1);

		//    using (Font v_font = new Font(FontFamily.GenericMonospace, 11, FontStyle.Regular, GraphicsUnit.Pixel))
		//    {
		//      SizeF v_mesure = g.MeasureString(message, v_font);
		//      int width = (int)v_mesure.Width;
		//      int height = (int)v_mesure.Height;
		//      g.FillRectangle(Brushes.White, 1, 1, width + 1, height + 1);
		//      g.DrawString(message, v_font, Brushes.Black, new RectangleF(1, 1, bmp.Width - 2, bmp.Height - 2));
		//    }
		//  }
		//}

		#endregion
	}
}
