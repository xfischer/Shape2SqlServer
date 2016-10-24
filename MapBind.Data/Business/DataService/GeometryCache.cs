using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Types;
using NetTopologySuite.Index.Strtree;
using GeoAPI.Geometries;
using System.Data.SqlClient;
using MapBind.Data.Models.SqlServer;
using System.Diagnostics;
using System.Threading;
using MapBind.Data.Models;


namespace MapBind.Data.Business
{
	internal static class GeometryCache
	{
		private static Dictionary<string, Dictionary<int, SqlGeometry>> _geomCacheByTableThenId = new Dictionary<string, Dictionary<int, SqlGeometry>>();
		private static Dictionary<string, Dictionary<int, double>> _geomEnvelopeAreaCacheByTableThenId = new Dictionary<string, Dictionary<int, double>>();
		private static Dictionary<string, Dictionary<int, MapBind.Data.Models.Geometry.Point>> _geomCentroidCacheByTableThenId = new Dictionary<string, Dictionary<int, MapBind.Data.Models.Geometry.Point>>();
		private static Dictionary<string, STRtree<int>> _spatialIndexSTR = new Dictionary<string, STRtree<int>>();

		private static Dictionary<string, bool> _loadedTables = new Dictionary<string, bool>();

		private static object _syncLock = new object();


		public static bool IsCacheLoaded(string tableName)
		{
			if (_loadedTables.ContainsKey(tableName))
				return _loadedTables[tableName];
			else
				return false;
		}

		public static void AddGeomToCache(string table, int id, SqlGeometry geom)
		{
			if (!_geomCacheByTableThenId.ContainsKey(table))
			{
				_geomCacheByTableThenId[table] = new Dictionary<int, SqlGeometry>();
				_geomEnvelopeAreaCacheByTableThenId[table] = new Dictionary<int, double>();
				_geomCentroidCacheByTableThenId[table] = new Dictionary<int, Models.Geometry.Point>();
				_spatialIndexSTR[table] = new STRtree<int>();
			}

			SqlGeometry envelope = geom.STEnvelope();
			Envelope env = new Envelope(envelope.STPointN(1).STX.Value, envelope.STPointN(3).STX.Value, envelope.STPointN(1).STY.Value, envelope.STPointN(3).STY.Value);

			_geomCacheByTableThenId[table][id] = geom;
			_geomEnvelopeAreaCacheByTableThenId[table][id] = envelope.STArea().Value;

			SqlGeometry envelopeCentroid = envelope.STCentroid().STPointN(1);
			_geomCentroidCacheByTableThenId[table][id] = new Models.Geometry.Point((float)envelopeCentroid.STX.Value, (float)envelopeCentroid.STY.Value);

			
			_spatialIndexSTR[table].Insert(env, id);
		}

		public static SqlGeometry GetGeometry(string table, int id)
		{
			return _geomCacheByTableThenId[table][id];
		}

		public static double GetGeometryArea(string table, int id)
		{
			return _geomEnvelopeAreaCacheByTableThenId[table][id];
		}

		public static Models.Geometry.Point GetEnvelopeCentroid(string table, int id)
		{
			return _geomCentroidCacheByTableThenId[table][id];
		}

		public static void LoadCache(string table, string connectionString)
		{
			lock (_syncLock)
			{
				try
				{
					if (!_loadedTables.ContainsKey(table))
					{
						_loadedTables[table] = false;

						string strQuery = SqlServerModel.GenerateGetAllGeomAndIdScript(table);
						using (SqlConnection con = new SqlConnection(connectionString))
						{
							con.Open();
							using (var cmd = new SqlCommand(strQuery, con))
							{
								cmd.CommandType = System.Data.CommandType.Text;

								DateTime start = DateTime.Now;

								using (var rdr = cmd.ExecuteReader())
								{
									while (rdr.Read())
									{
										int id = (int)rdr[0];
										SqlGeometry geom = (SqlGeometry)rdr[1];
										//SqlGeography geog = (SqlGeography)rdr[2];

										//AddGeomToCache(table, id, geom, geog);
										AddGeomToCache(table, id, geom);
									}

									Trace.WriteLine("cache loaded in: " + (DateTime.Now - start).TotalMilliseconds + " ms");
								}
							}
						}

						_loadedTables[table] = true;
					}
				}
				catch (Exception)
				{ throw; }
			}
		}

		public static IList<int> Query(string table, Envelope bbox)
		{
			return _spatialIndexSTR[table].Query(bbox);
		}

	}
}
