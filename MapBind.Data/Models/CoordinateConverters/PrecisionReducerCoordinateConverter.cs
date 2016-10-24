using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MapBind.Data.Models.CoordinateConverters
{
	public sealed class PrecisionReducerCoordinateConverter : CoordinateConverterBase
	{
		#region Private Members

		private int _globalMapHeightPixels;
		private int _usefulDigitsX;
		private int _usefulDigitsY;
		private bool _digitsYset;

		#endregion
		
		#region Constructor
		public PrecisionReducerCoordinateConverter(BoundingBox query, int mapWidthPixels, int mapHeightPixels)
		{
			
			// Calculate what would be the mapSize if bbox was the whole world
			double globalMapWidthPixels = (360d * mapWidthPixels) / query.Width;
			_globalMapHeightPixels = (int)((180d * mapHeightPixels) / query.Height);

			_usefulDigitsX = GetUsefulDigits(2 * Math.PI * BingMaps.BingMapsTileSystem.EarthRadius / globalMapWidthPixels);
			int digitsYMax = this.GetUsefulDigits(this.GetGroundResolution(query.maxY, _globalMapHeightPixels));
			int digitsYMin = this.GetUsefulDigits(this.GetGroundResolution(query.minY, _globalMapHeightPixels));
			if (digitsYMax == digitsYMin)
			{
				_usefulDigitsY = digitsYMin;
				_digitsYset = true;
			}
			else
				_digitsYset = false;
		} 
		#endregion	
		

		#region Precision Helpers
		private int GetUsefulDigits(double resolution)
		{
			return 1 + (int)Math.Floor(Math.Abs(Math.Log10(resolution * 360d / BingMaps.BingMapsTileSystem.EarthCircumference)));
		}

		private double GetGroundResolution(double latitude, double mapSize)
		{
			return Math.Cos(latitude * Math.PI / 180) * 2 * Math.PI * BingMaps.BingMapsTileSystem.EarthRadius / mapSize;
		}

		#endregion


		public override void TransformPoint(double x, double y, out double outX, out double outY)
		{
			int digitsY = _digitsYset ? _usefulDigitsY : this.GetUsefulDigits(this.GetGroundResolution(y, _globalMapHeightPixels));
			outY = (double)Math.Round((decimal)y, digitsY);
			outX = (double)Math.Round((decimal)x, _usefulDigitsX);
		}
	}
}
