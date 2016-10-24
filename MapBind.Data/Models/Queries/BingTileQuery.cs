using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.SqlServer.Types;
using System.Drawing;

namespace MapBind.Data.Models
{
	public class BingTileQuery : QueryBase
	{
		public string quadKey { get; set; }
		
		public BingTileQuery(string quadKey, enDiskCacheMode cacheMode)
			: base()
		{
			this.quadKey = quadKey;
			this.CacheMode = cacheMode;
		}

		public BingTileQuery(string quadKey, enDiskCacheMode cacheMode, Color fillColor, Color strokeColor, int strokeThickness)
			: base()
		{
			this.quadKey = quadKey;
			base.FillColor = fillColor;
			base.strokeThickness = strokeThickness;
			base.StrokeColor = strokeColor;
			this.CacheMode = cacheMode;
		}
	}

}