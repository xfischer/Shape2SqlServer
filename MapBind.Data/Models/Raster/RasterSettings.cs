using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MapBind.Data.Models;
using System.Drawing;

namespace MapBind.Data.Models.Raster
{
	[Serializable()]
	public class RasterSettings
	{
		public string TableName { get; set; }
		public bool Clip { get; set; }
		public bool Reduce { get; set; }
		public bool RemoveArtefacts { get; set; }
		public bool Geomcache { get; set; }
		public enMetricsType MetricsType { get; set; }
		public Color FillColor { get; set; }
		public Color StrokeColor { get; set; }

		public RasterSettings(string tableName, bool clip, bool reduce, bool removeArtefacts, bool geomcache, enMetricsType metrics, Color fillColor, Color strokeColor)
		{
			this.TableName = tableName;
			this.Clip = clip;
			this.Reduce = reduce;
			this.RemoveArtefacts = removeArtefacts;
			this.Geomcache = geomcache;
			this.MetricsType = metrics;
			this.FillColor = fillColor;
			this.StrokeColor = strokeColor;
		}


	}


}
