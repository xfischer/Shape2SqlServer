using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MapBind.Data.Models.Geometry;
using Microsoft.SqlServer.Types;

namespace MapBind.Data.Models.CoordinateConverters
{
	internal sealed class BitmapCoordConverter : CoordinateConverterBase
	{

		private double _4Pi = 4 * Math.PI;
		private double _PiDiv180 = Math.PI / 180;
		private int _mapWidth, _mapHeight;
		private double _offsetX, _offsetY;
		private Metrics _metrics;
		private BoundingBox _targetViewPortBBox;

		public BitmapCoordConverter(int mapWidth, int mapHeight, BoundingBox viewport)
			: this(mapWidth, mapHeight, viewport, null)
		{
		}

		public BitmapCoordConverter(int mapWidth, int mapHeight, BoundingBox viewport, Metrics metrics)
		{
			_mapWidth = mapWidth;
			_mapHeight = mapHeight;
			_metrics = metrics;

			// clip viewPort (THIS IS NOT SCALE TO FIT)
			_targetViewPortBBox = this.convertBBox(viewport);
			double aspectRatio = _targetViewPortBBox.AspectRatio;
			if (aspectRatio > mapWidth / mapHeight)
			{
				_targetViewPortBBox.maxX = _targetViewPortBBox.Height * ((double)mapWidth / (double)mapHeight) + _targetViewPortBBox.minX;
			}
			else if (aspectRatio < mapWidth / mapHeight)
			{
				_targetViewPortBBox.maxY = _targetViewPortBBox.Width * ((double)mapHeight / (double)mapWidth) + _targetViewPortBBox.minY;
			}


			// calculate offset from TopLeft viewport corner
			this.LatLongToXY(viewport.maxY, viewport.minX, out _offsetX, out _offsetY);

		}

		private BoundingBox convertBBox(BoundingBox bbox)
		{
			double xWest, xEast, ySouth, yNorth;
			this.LatLongToXY(bbox.maxY, bbox.minX, out xWest, out yNorth); // NorthWest
			this.LatLongToXY(bbox.minY, bbox.maxX, out xEast, out ySouth); // SouthEast

			return new BoundingBox(xWest, yNorth, xEast, ySouth);
		}

		private void LatLongToXY(double latitude, double longitude, out double coordX, out double coordY)
		{
			coordX = (longitude + 180) / 360;
			double sinLatitude = Math.Sin(latitude * _PiDiv180);
			coordY = 0.5 - Math.Log((1 + sinLatitude) / (1 - sinLatitude)) / _4Pi;
		}

		public override void TransformPoint(double x, double y, out double outX, out double outY)
		{
			this.LatLongToXY(y, x, out outX, out outY);

			outX = (int)(_mapWidth * (outX -  _targetViewPortBBox.minX) / _targetViewPortBBox.Width);
			outY = (int)(_mapHeight * (outY- _targetViewPortBBox.minY) / _targetViewPortBBox.Height);
		}
	}
}


