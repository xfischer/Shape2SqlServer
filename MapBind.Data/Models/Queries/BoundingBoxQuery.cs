using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.SqlServer.Types;
using MapBind.Data.Models.BingMaps;

namespace MapBind.Data.Models
{
	public class BoundingBoxQuery : QueryBase
	{
		public BoundingBox BBox { get; set; }
		public int Width { get; set; }
		public int Height { get; set; }

		public BoundingBoxQuery() : base() { }
				
		#region Converters

		public static BoundingBoxQuery FromTileQuery(TileQuery query)
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

			BoundingBox bbox = new BoundingBox(nwLon, seLat, seLon, nwLat);

			return
				new BoundingBoxQuery()
				{
					BBox = bbox,
					_querytables = query._querytables,
					Width = 256,
					Height = 256,
					SRID = 4326,
					IsBench = query.IsBench
				};
		}

		public static BoundingBoxQuery FromBingTileQuery(BingTileQuery query)
		{
			int tileX, tileY, zoom, nwX, nwY;
			double nwLat, nwLon, seLat, seLon;

			BingMapsTileSystem.QuadKeyToTileXY(query.quadKey, out tileX, out tileY, out zoom);


			BingMapsTileSystem.TileXYToPixelXY(tileX, tileY, out nwX, out nwY);
			BingMapsTileSystem.PixelXYToLatLong(nwX, nwY, zoom, out nwLat, out nwLon);
			BingMapsTileSystem.PixelXYToLatLong(nwX + 256, nwY + 256, zoom, out seLat, out seLon);

			BoundingBox bbox = new BoundingBox(nwLon, seLat, seLon, nwLat);
			
			return new BoundingBoxQuery()
			{
				BBox = bbox,
				_querytables = query._querytables,
				Width = 256,
				Height = 256,
				SRID = 4326,
				IsBench = query.IsBench,
				FillColor = query.FillColor,
				StrokeColor = query.StrokeColor,
				strokeThickness = query.strokeThickness
			};

		}

		#endregion
	}
}