#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Types;
using System.Data;
using System.Globalization;

namespace Shape2SqlServer.Core;

internal sealed class SqlServerModel
{
	public const string PK_FIELD_NAME_DEFAULT = "ID";
	public const string GEOMETRY_FIELD_NAME_DEFAULT = "geom_geom";
	public const string GEOGRAPHY_FIELD_NAME_DEFAULT = "geom_geog";
	public const string GEOMETRY_INDEX_NAME_DEFAULT = "IDX_" + GEOMETRY_FIELD_NAME_DEFAULT;
	public const string GEOGRAPHY_INDEX_NAME_DEFAULT = "IDX_" + GEOGRAPHY_FIELD_NAME_DEFAULT;

	internal static string GenerateCreateTableScript(string tableName, string schema, List<SqlColumnDescriptor> columns, enSpatialType spatialType, bool dropTableIfExists, string geomColName, string idColName)
	{
		StringBuilder builder = new();

		if (dropTableIfExists)
			builder.AppendLine(GenerateDropTableIfExistsScript(tableName, schema));

		builder.AppendLine(string.Concat("CREATE TABLE ", GenerateFullTableName(tableName, schema), " ("));

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

	internal static DataTable GenerateDataTable(string tableName, List<SqlColumnDescriptor> columnTypes, enSpatialType spatialType, bool dropTableIfExists, string geomColName, string idColName)
	{
		DataTable dt = new(tableName);

		// id column
		DataColumn idColumn = new(idColName, typeof(int))
		{
			AutoIncrement = true,
			AutoIncrementSeed = 1,
			AutoIncrementStep = 1
		};
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

	public static string GenerateDropTableIfExistsScript(string tableName, string schema)
	{
		string tableFullName = GenerateFullTableName(tableName, schema);
		return string.Format("IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('{0}') AND type in ('U')) DROP TABLE {0}", tableFullName);
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

		string? colName = null;
		do
		{
			string hash = Guid.NewGuid().ToString().Substring(1, 2);
			colName = colDesiredName + hash;
		}
		while (!colNames.Contains(colName));

		return colName;
	}

	public static string CleanSQLName(string name) =>
		System.Text.RegularExpressions.Regex.Replace(name, @"[^\w\.-_@]", "");

	public static DataRow GetNewDataTableRow(DataTable dataTable, string tableName, List<object> sqlNativeGeomList, List<object> attributes)
	{
		// TODO use SMO to avoid SQL injection attacks
		DataRow row = dataTable.NewRow();

		int i = 1;
		foreach (var obj in attributes)
		{
			try
			{
				row[i] = obj;
				i++;
			}
			catch (Exception)
			{
				//Trace.TraceWarning(ex.Message);
				row[i] = DBNull.Value;
				i++;
			}
		}

		for (int j = 0; j < sqlNativeGeomList.Count; j++)
		{
			row[i] = sqlNativeGeomList[j];
			i++;
		}

		return row;
	}

	public static string GenerateFullTableName(string tableName, string schema) =>
		string.IsNullOrWhiteSpace(schema)
			? string.Format("[{0}]", tableName)
			: string.Format("[{0}].[{1}]", schema, tableName);

	public static string? GenerateCreateSpatialIndexScript(string shortTableName, string schema, string geomColumnName, BoundingBox geoBounds, enSpatialType spatialType, enSpatialIndexGridDensity gridDensity)
	{
		string defaultGridDensity = gridDensity.ToString();
		string tableName = GenerateFullTableName(shortTableName, schema);
		// spatial index (geom)
		switch (spatialType)
		{
			case enSpatialType.geography:
				return string.Format("CREATE SPATIAL INDEX [IDX_{0}] ON {1} ( [{0}] ) USING  GEOGRAPHY_GRID WITH "
																																	+ " ( GRIDS =(LEVEL_1 = {2},LEVEL_2 = {2},LEVEL_3 = {2},LEVEL_4 = {2}), "
																																	+ "CELLS_PER_OBJECT = 16, PAD_INDEX  = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]"
																																	, geomColumnName
																																	, tableName
																																	, defaultGridDensity);
			case enSpatialType.geometry:
				return string.Format("CREATE SPATIAL INDEX [IDX_{0}] ON {1} ( [{0}] ) USING  GEOMETRY_GRID WITH "
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
				return string.Format("CREATE SPATIAL INDEX [IDX_{0}] ON {1} ( [{0}] ) USING  GEOGRAPHY_GRID WITH "
																																	+ " ( GRIDS =(LEVEL_1 = {2},LEVEL_2 = {2},LEVEL_3 = {2},LEVEL_4 = {2}), "
																																	+ "CELLS_PER_OBJECT = 16, PAD_INDEX  = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]"
																																	, geomColumnName + "_geog"
																																	, tableName
																																	, defaultGridDensity)
												+
												string.Format("CREATE SPATIAL INDEX [IDX_{0}] ON {1} ( [{0}] ) USING  GEOMETRY_GRID WITH "
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
}
