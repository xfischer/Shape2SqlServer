using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Types;
using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;

namespace MapBind.Data.Models.SqlServer
{
	public sealed class SqlServerModel
	{

		public const string PK_FIELD_NAME_DEFAULT = "ID";
		public const string GEOMETRY_FIELD_NAME_DEFAULT = "geom_geom";
		public const string GEOGRAPHY_FIELD_NAME_DEFAULT = "geom_geog";
		public const string GEOMETRY_INDEX_NAME_DEFAULT = "IDX_" + GEOMETRY_FIELD_NAME_DEFAULT;
		public const string GEOGRAPHY_INDEX_NAME_DEFAULT = "IDX_" + GEOGRAPHY_FIELD_NAME_DEFAULT;

		#region Info

		public static string GetSqlDatabaseName(string connectionString)
		{
			return GetConnectionStringPart(connectionString, "INITIAL CATALOG");
		}
		public static string GetSqlServerName(string connectionString)
		{
			return GetConnectionStringPart(connectionString, "Data Source");
		}
		private static string GetConnectionStringPart(string connectionString, string NamedPart)
		{
			string regexDataSource = NamedPart.Replace(" ", "\\s") + "(\\s)*=(\\s)*(?<Value>([^;]*))";
			RegexOptions options = RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.Compiled;
			Regex reg = new Regex(regexDataSource, options);
			MatchCollection v_matchCol = reg.Matches(connectionString);
			if (v_matchCol.Count > 0)
				return v_matchCol[0].Groups["Value"].Value;
			else
				return null;
		}

		#endregion

		#region BoundingBox

		/// <summary>
		/// Build an SqlGeography Polygon from a bounding box.
		/// </summary>
		/// <param name="box">Bounding Box</param>
		/// <returns>SqlGeography Polygon</returns>
		public static SqlGeography GeographyFromBoundingBox(Models.BoundingBox box, int srid = 4326)
		{
			var geob = new SqlGeographyBuilder();
			geob.SetSrid(srid);
			geob.BeginGeography(OpenGisGeographyType.Polygon);
			geob.BeginFigure(box.maxY, box.minX);
			geob.AddLine(box.minY, box.minX);
			geob.AddLine(box.minY, box.maxX);
			geob.AddLine(box.maxY, box.maxX);
			geob.AddLine(box.maxY, box.minX);
			geob.EndFigure();
			geob.EndGeography();
			var geog = geob.ConstructedGeography;
			//Debug.WriteLine(geog.AsGml().Value);
			return geog;
		}

		/// <summary>
		/// Build an SqlGeometry Polygon from a bounding box.
		/// </summary>
		/// <param name="box">Bounding Box</param>
		/// <returns>SqlGeometry Polygon</returns>
		public static SqlGeometry GeometryFromBoundingBox(Models.BoundingBox box, int srid = 4326)
		{
			var geob = new SqlGeometryBuilder();
			geob.SetSrid(srid);
			geob.BeginGeometry(OpenGisGeometryType.Polygon);
			geob.BeginFigure(box.minX, box.maxY);
			geob.AddLine(box.minX, box.minY);
			geob.AddLine(box.maxX, box.minY);
			geob.AddLine(box.maxX, box.maxY);
			geob.AddLine(box.minX, box.maxY);
			geob.EndFigure();
			geob.EndGeometry();
			var geom = geob.ConstructedGeometry;
			//Debug.WriteLine(geog.AsGml().Value);
			return geom;
		}

		/// <summary>
		/// Build an SqlGeography Polygon from a bounding box given in discrete coordinates.
		/// </summary>
		/// <param name="nwLat">Northwest Latitude</param>
		/// <param name="nwLong">Northwest Longitude</param>
		/// <param name="seLat">Southeast Latitude</param>
		/// <param name="seLong">Southeast Longitude</param>
		/// <returns>SqlGeography Polygon</returns>
		public static SqlGeography GeograhyFromBoundingBoxNwSe(double nwLat, double nwLong, double seLat, double seLong, int srid)
		{
			return GeographyFromBoundingBox(new Models.BoundingBox(nwLong, seLat, seLong, nwLat), srid);
		}

		/// <summary>
		/// Build an SqlGeometry Polygon from a bounding box given in discrete coordinates.
		/// </summary>
		/// <param name="nwLat">Northwest Latitude</param>
		/// <param name="nwLong">Northwest Longitude</param>
		/// <param name="seLat">Southeast Latitude</param>
		/// <param name="seLong">Southeast Longitude</param>
		/// <returns>SqlGeography Polygon</returns>
		public static SqlGeometry GeometryFromBoundingBoxNwSe(double nwLat, double nwLong, double seLat, double seLong, int srid)
		{
			return GeometryFromBoundingBox(new Models.BoundingBox(nwLong, seLat, seLong, nwLat), srid);
		}

		#endregion

		#region Scripts

		public static string GenerateCreateTableScript(string tableName, List<SqlColumnDescriptor> columns, enSpatialType spatialType, bool dropTableIfExists, string geomColName, string idColName)
		{

			StringBuilder builder = new StringBuilder();

			if (dropTableIfExists)
				builder.AppendLine(SqlServerModel.GenerateDropTableIfExistsScript(tableName));

			builder.AppendLine("CREATE TABLE [" + tableName + "](");

			// id
			builder.AppendFormat("[{0}] [int] IDENTITY(1,1) NOT NULL,", idColName);
			builder.AppendLine();

			// columns
			foreach (SqlColumnDescriptor desc in columns)
			{
				builder.AppendFormat("\t[{0}] {1} NULL,", desc.Name, desc.SqlType);
				builder.AppendLine();
			}

			// geom
			switch (spatialType)
			{
				case enSpatialType.geometry:
					builder.AppendFormat("[{0}] [{1}] NULL,", geomColName, "geometry");
					builder.AppendLine();
					break;
				case enSpatialType.geography:
					builder.AppendFormat("[{0}] [{1}] NULL,", geomColName, "geography");
					builder.AppendLine();
					break;
				case enSpatialType.both:
					builder.AppendFormat("[{0}_geom] [{1}] NULL,", geomColName, "geometry");
					builder.AppendLine();
					builder.AppendFormat("[{0}_geog] [{1}] NULL,", geomColName, "geography");
					builder.AppendLine();
					break;
			}

			// primary key
			builder.AppendFormat("PRIMARY KEY CLUSTERED ( [{0}] ASC) WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, "
																	+ "IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]) ON [PRIMARY]"
																	, idColName);

			return builder.ToString();
		}

		public static DataTable GenerateDataTable(string tableName, List<SqlColumnDescriptor> columnTypes, enSpatialType spatialType, bool dropTableIfExists, string geomColName, string idColName)
		{
			DataTable dt = new DataTable(tableName);

			// id column
			DataColumn idColumn = new DataColumn(idColName, typeof(int));
			idColumn.AutoIncrement = true;
			idColumn.AutoIncrementSeed = 1;
			idColumn.AutoIncrementStep = 1;
			dt.Columns.Add(idColumn);

			// columns
			foreach (var field in columnTypes)
			{
				dt.Columns.Add(new DataColumn(field.Name, field.Type));
			}

			// geom
			switch (spatialType)
			{
				case enSpatialType.geometry:
					dt.Columns.Add(new DataColumn(geomColName, typeof(SqlGeometry)));
					break;
				case enSpatialType.geography:
					dt.Columns.Add(new DataColumn(geomColName, typeof(SqlGeography)));
					break;
				case enSpatialType.both:
					dt.Columns.Add(new DataColumn(geomColName + "_geom", typeof(SqlGeometry)));
					dt.Columns.Add(new DataColumn(geomColName + "_geog", typeof(SqlGeography)));
					break;
			}


			return dt;
		}

		public static string GenerateDropTableIfExistsScript(string tableName)
		{
			return string.Format("IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[{0}]') AND type in ('U')) DROP TABLE [{0}]", tableName);
		}

		public static string GenerateUniqueColName(string colDesiredName, List<SqlColumnDescriptor> columns, string tableName)
		{
			List<string> colNames = (from col in columns
															 select col.Name).ToList();


			if (!colNames.Contains(colDesiredName))
				return colDesiredName;
			if (!colNames.Contains(CleanSQLName(tableName) + colDesiredName))
				return CleanSQLName(tableName) + colDesiredName;
			if (!colNames.Contains(colDesiredName + CleanSQLName(tableName)))
				return colDesiredName + CleanSQLName(tableName);

			string colName = null;
			do
			{
				string hash = Guid.NewGuid().ToString().Substring(1, 2);
				colName = colDesiredName + hash;
			}
			while (!colNames.Contains(colName));

			return colName;
		}

		public static string CleanSQLName(string name)
		{
			return System.Text.RegularExpressions.Regex.Replace(name, @"[^\w\.-_@]", "");
		}

		public static DataRow GetNewDataTableRow(DataTable dataTable, string tableName, List<object> sqlNativeGeomList, List<object> attributes)
		{
			// TODO use SMO to avoid SQL injection attacks
			DataRow row = dataTable.NewRow();

			int i = 1;
			foreach (var obj in attributes)
			{
				row[i] = obj;
				i++;
			}

			for (int j = 0; j < sqlNativeGeomList.Count; j++)
			{
				row[i] = sqlNativeGeomList[j];
				i++;
			}

			return row;
		}

		public static string GenerateCreateSpatialIndexScript(string tableName, string geomColumnName, BoundingBox geoBounds, enSpatialType spatialType, enSpatialIndexGridDensity gridDensity)
		{

			string defaultGridDensity = gridDensity.ToString();

			// spatial index (geom)
			switch (spatialType)
			{
				case enSpatialType.geography:
					return string.Format("CREATE SPATIAL INDEX [IDX_{0}] ON [{1}] ( [{0}] ) USING  GEOGRAPHY_GRID WITH "
																				+ " ( GRIDS =(LEVEL_1 = {2},LEVEL_2 = {2},LEVEL_3 = {2},LEVEL_4 = {2}), "
																				+ "CELLS_PER_OBJECT = 16, PAD_INDEX  = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]"
																				, geomColumnName
																				, tableName
																				, defaultGridDensity);
				case enSpatialType.geometry:
					return string.Format("CREATE SPATIAL INDEX [IDX_{0}] ON [{1}] ( [{0}] ) USING  GEOMETRY_GRID WITH "
																			+ " ( BOUNDING_BOX =({2}, {3}, {4}, {5}), GRIDS =(LEVEL_1 = {6},LEVEL_2 = {6},LEVEL_3 = {6},LEVEL_4 = {6}), "
																			+ "CELLS_PER_OBJECT = 16, PAD_INDEX  = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]"
																			, geomColumnName
																			, tableName
																			, Convert.ToString(geoBounds.minX, CultureInfo.InvariantCulture)
																			, Convert.ToString(geoBounds.minY, CultureInfo.InvariantCulture)
																			, Convert.ToString(geoBounds.maxX, CultureInfo.InvariantCulture)
																			, Convert.ToString(geoBounds.maxY, CultureInfo.InvariantCulture)
																			, defaultGridDensity);

				case enSpatialType.both:
					return string.Format("CREATE SPATIAL INDEX [IDX_{0}] ON [{1}] ( [{0}] ) USING  GEOGRAPHY_GRID WITH "
																				+ " ( GRIDS =(LEVEL_1 = {2},LEVEL_2 = {2},LEVEL_3 = {2},LEVEL_4 = {2}), "
																				+ "CELLS_PER_OBJECT = 16, PAD_INDEX  = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]"
																				, geomColumnName + "_geog"
																				, tableName
																				, defaultGridDensity)
									+
									string.Format("CREATE SPATIAL INDEX [IDX_{0}] ON [{1}] ( [{0}] ) USING  GEOMETRY_GRID WITH "
																			+ " ( BOUNDING_BOX =({2}, {3}, {4}, {5}), GRIDS =(LEVEL_1 = {6},LEVEL_2 = {6},LEVEL_3 = {6},LEVEL_4 = {6}), "
																			+ "CELLS_PER_OBJECT = 16, PAD_INDEX  = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]"
																			, geomColumnName + "_geom"
																			, tableName
																			, Convert.ToString(geoBounds.minX, CultureInfo.InvariantCulture)
																			, Convert.ToString(geoBounds.minY, CultureInfo.InvariantCulture)
																			, Convert.ToString(geoBounds.maxX, CultureInfo.InvariantCulture)
																			, Convert.ToString(geoBounds.maxY, CultureInfo.InvariantCulture)
																			, defaultGridDensity);
				default: return null;
			}



		}

		internal static string GenerateGetGeomIdInBBoxScript(string tableName, BoundingBox bbox, string idFieldName = PK_FIELD_NAME_DEFAULT, string geomFieldName = GEOMETRY_FIELD_NAME_DEFAULT, string indexName = GEOMETRY_INDEX_NAME_DEFAULT)
		{
			string strQuery =
@"DECLARE @bbox geometry

SET @bbox = geometry::STGeomFromText('{0}',4326)
	
SELECT {4}
FROM {2} WITH(INDEX({3}))
WHERE {1}.STIntersects(@bbox) = 1";

			return string.Format(strQuery, SqlServerModel.GeometryFromBoundingBox(bbox).ToString()
																		, geomFieldName
																		, tableName
																		, indexName
																		, idFieldName);

		}


		internal static string GenerateGetAllGeomAndIdScript(string tableName, string geomFieldName = GEOMETRY_FIELD_NAME_DEFAULT, string geogFieldName = GEOGRAPHY_FIELD_NAME_DEFAULT, string idFieldName = PK_FIELD_NAME_DEFAULT)
		{
			string strQuery = @"SELECT {0}, {1}, {2} FROM {3}";

			return string.Format(strQuery, idFieldName, geomFieldName, geogFieldName, tableName);
		}
		internal static string GenerateGetGeomInBBoxScript(string tableName, BoundingBox bbox, string geomFieldName = GEOMETRY_FIELD_NAME_DEFAULT, string indexName = GEOMETRY_INDEX_NAME_DEFAULT)
		{
			string strQuery =
@"DECLARE @bbox geometry DECLARE @res float

SET @bbox = geometry::STGeomFromText('{0}',4326)
--SET @res = (@bbox.STPointN(1).STY - @bbox.STPointN(2).STY) / 256
	
--SELECT {1}.Reduce(@res).STAsBinary() AS geom
SELECT {1}.STAsBinary() AS geom,[ID]
      ,[ID_GEOFLA]
      ,[CODE_DEPT]
      ,[NOM_DEPT]
      ,[CODE_CHF]
      ,[NOM_CHF]
      ,[X_CHF_LIEU]
      ,[Y_CHF_LIEU]
      ,[X_CENTROID]
      ,[Y_CENTROID]
      ,[CODE_REG]
      ,[NOM_REGION]
      ,[geom_geom]
      ,[geom_geog]
FROM {2} WITH(INDEX({3}))
WHERE {1}.STIntersects(@bbox) = 1";

			//      string strQuery =
			//@"DECLARE @bbox geometry DECLARE @res float
			//
			//SET @bbox = geometry::STGeomFromText('{0}',4326)
			//SET @res = (@bbox.STPointN(1).STY - @bbox.STPointN(2).STY) / 256
			//	
			//SELECT {1}.Reduce(@res).STAsBinary() AS geom
			//FROM {2} WITH(INDEX({3}))
			//WHERE {1}.STIntersects(@bbox) = 1";

			return string.Format(strQuery, SqlServerModel.GeometryFromBoundingBox(bbox).ToString()
																		, geomFieldName
																		, tableName
																		, indexName);

		}

		internal static string GenerateGetGeomEnvelopeInBBoxScript(string tableName, BoundingBox bbox, string geomWhereClause, string geomFieldName = GEOMETRY_FIELD_NAME_DEFAULT, string indexName = GEOMETRY_INDEX_NAME_DEFAULT)
		{
			string strQuery =
@"DECLARE @bbox geometry DECLARE @res float

SET @bbox = geometry::STGeomFromText('{0}',4326)

SELECT {1} AS geom
FROM {2} WITH(INDEX({3}))
WHERE {4}.STIntersects(@bbox) = 1";

			//      string strQuery =
			//@"DECLARE @bbox geometry DECLARE @res float
			//
			//SET @bbox = geometry::STGeomFromText('{0}',4326)
			//SET @res = (@bbox.STPointN(1).STY - @bbox.STPointN(2).STY) / 256
			//	
			//SELECT {1}.Reduce(@res).STAsBinary() AS geom
			//FROM {2} WITH(INDEX({3}))
			//WHERE {1}.STIntersects(@bbox) = 1";

			return string.Format(strQuery, SqlServerModel.GeometryFromBoundingBox(bbox).ToString()
																		, geomFieldName
																		, tableName
																		, indexName
																		, geomWhereClause);

		}

		internal static string GenerateGetGeomAndIdInBBoxScript(string table, BoundingBox bbox)
		{
			string strQuery =
@"DECLARE @bbox geometry DECLARE @res float

SET @bbox = geometry::STGeomFromText('{0}',4326)
	
SELECT ID,geom_geom
FROM COMMUNE WITH(INDEX(IDX_geom_geom))
WHERE geom_geom.STIntersects(@bbox) = 1";

			return string.Format(strQuery, SqlServerModel.GeometryFromBoundingBox(bbox).ToString());
		}

		#endregion

		#region Optimization

		internal static SqlGeography DoRemoveArtifacts(SqlGeography geography)
		{
			/*
			 *   DECLARE @h geography = null
  
  IF @g.STIsEmpty() = 0
  BEGIN	
  	  SET @h = geography::STGeomFromText('POINT EMPTY', @g.STSrid);
	  DECLARE @i int = 1;
	  WHILE @i <= @g.STNumGeometries() BEGIN
		IF(@g.STGeometryN(@i).STDimension() = 2) BEGIN
		  SELECT @h = @h.STUnion(@g.STGeometryN(@i));
		END
		SET @i = @i + 1;
	  END
  END
  RETURN @h;
			 * */

			SqlGeography h = null;
			try
			{


				if (!geography.STIsEmpty())
				{
					h = SqlGeography.Parse(new System.Data.SqlTypes.SqlString("POINT EMPTY"));
					h.STSrid = 4326;
					int i = 1;
					while (i <= geography.STNumGeometries())
					{
						if (geography.STGeometryN(i).STDimension() == 2)
							h = h.STUnion(geography.STGeometryN(i));

						i++;
					}

				}
			}
			catch (Exception)
			{
				throw;
			}
			return h;

		}

		internal static SqlGeometry DoRemoveArtefacts(SqlGeometry geometry)
		{
			/*
			 *   DECLARE @h geography = null
  
  IF @g.STIsEmpty() = 0
  BEGIN	
  	  SET @h = geography::STGeomFromText('POINT EMPTY', @g.STSrid);
	  DECLARE @i int = 1;
	  WHILE @i <= @g.STNumGeometries() BEGIN
		IF(@g.STGeometryN(@i).STDimension() = 2) BEGIN
		  SELECT @h = @h.STUnion(@g.STGeometryN(@i));
		END
		SET @i = @i + 1;
	  END
  END
  RETURN @h;
			 * */

			SqlGeometry h = null;
			try
			{


				if (!geometry.STIsEmpty())
				{
					h = SqlGeometry.Parse(new System.Data.SqlTypes.SqlString("POINT EMPTY"));
					h.STSrid = 4326;
					int i = 1;
					while (i <= geometry.STNumGeometries())
					{
						if (geometry.STGeometryN(i).STDimension() == 2)
							h = h.STUnion(geometry.STGeometryN(i));

						i++;
					}

				}
			}
			catch (Exception)
			{
				throw;
			}
			return h;

		}

		#endregion

	}
}
