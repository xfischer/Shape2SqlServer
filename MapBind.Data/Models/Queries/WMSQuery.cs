using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace MapBind.Data.Models
{
	public class WMSQuery
	{
		public string VERSION { get; set; }
		public enWMSRequest REQUEST { get; set; }
		public string[] LAYERS { get; set; }
		public string[] STYLES { get; set; }
		public string CRS { get; set; }
		public BoundingBox BBOX { get; set; }
		public int WIDTH { get; set; }
		public int HEIGHT { get; set; }
		public string FORMAT { get; set; }
		public bool TRANSPARENT { get; set; }
		public Color BGCOLOR { get; set; }
		public enWMSException EXCEPTIONS { get; set; }
		public string TIME { get; set; }
		public string ELEVATION { get; set; }
		/*
		Request parameter				Mandatory/optional	Description
		VERSION=1.3.0					M					Request version.
		REQUEST=GetMap					M					Request name.
		LAYERS=layer_list				M					Comma-separated list of one or more map layers.
		STYLES=style_list				M					Comma-separated list of one rendering style per requested layer.
		CRS=namespace:identifier		M					Coordinate reference system.
		BBOX=minx,miny,maxx,maxy		M					Bounding box corners (lower left, upper right) in CRS units.
		WIDTH=output_width				M					Width in pixels of map picture.
		HEIGHT=output_height			M					Height in pixels of map picture.
		FORMAT=output_format			M					Output format of map.
		TRANSPARENT=TRUE|FALSE			O					Background transparency of map (default=FALSE).
		BGCOLOR=color_value				O					Hexadecimal red-green-blue colour value for the background color (default=0xFFFFFF).
		EXCEPTIONS=exception_format		O					The format in which exceptions are to be reported by the WMS (default=XML).
		TIME=time						O					Time value of layer desired.
		ELEVATION=elevation				O					Elevation of layer desired.
		
		 */
	}

	public enum enWMSRequest
	{
		GetCapabilities
		,
		GetMap
			, GetFeatureInfo
	}

	public enum enWMSException
	{ 
		XML,INIMAGE,BLANK
	}
}
