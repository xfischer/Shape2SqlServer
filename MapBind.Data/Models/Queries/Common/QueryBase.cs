using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace MapBind.Data.Models
{
	public class QueryBase
	{
		public List<string> Tables()
		{
			if (_querytables == null || _querytables.Count == 0)
				return null;
			else
				return _querytables.Keys.ToList();
		}

		public int SRID { get; set; }

		public bool IsBench { get; set; }

		public Color FillColor { get; set; }
		public Color StrokeColor { get; set; }
		public int strokeThickness { get; set; }
		public enDiskCacheMode CacheMode { get; set; }

		#region struct
		protected internal struct QueryTableInfo
		{
			public string TableName { get; set; }
			public List<string> AttributeFields { get; set; }
			public string GeometryField { get; set; }
			public bool isGeography { get; set; }

			public bool IsEmpty
			{
				get
				{ return this.GeometryField == null; }
			}
		}
		#endregion

		protected internal Dictionary<string, QueryTableInfo> _querytables;

		internal QueryBase()
		{
			this._querytables = new Dictionary<string, QueryTableInfo>();
			this.FillColor = Color.FromArgb(255, 102, 0);
			this.StrokeColor = Color.White;
			this.strokeThickness = 1;
			this.CacheMode = enDiskCacheMode.None;
		}

		public void AddTable(string tableName)
		{
			if (_querytables.ContainsKey(tableName))
				throw new InvalidOperationException("Table is already part of the query");

			_querytables.Add(tableName, new QueryTableInfo());
		}

		public void AddFields(string tableName, IEnumerable<string> fields)
		{
			QueryTableInfo info = _querytables[tableName];
			info.TableName = tableName;
			info.AttributeFields = fields.ToList();
			_querytables[tableName] = info;
		}

		public void SetGeometryField(string tableName, string fieldName, bool isGeography)
		{
			QueryTableInfo info = _querytables[tableName];
			info.GeometryField = fieldName;
			info.isGeography = isGeography;
			_querytables[tableName] = info;
		}


	}

	
}
