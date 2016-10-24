using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

namespace MapBind.Data
{
	internal static class Utils
	{
		public static bool IsPNGBitmapEmpty(Bitmap bmp)
		{
			BitmapData bmData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
																									 ImageLockMode.ReadOnly,
																									 bmp.PixelFormat);

			// Get the average R,G,B values of pixels in this square
			long totals = 0;
			unsafe
			{
				// 24bit image so 3 bytes per pixel (PNG + transparency would be 4)
				int PixelSize = 4;
				for (int y = 0; y < bmp.Height; y++)
				{
					byte* p = (byte*)bmData.Scan0 + (y * bmData.Stride);
					for (int x = 0; x < bmp.Width; x++)
					{
						totals += p[x * PixelSize]; // Blue
						totals += p[x * PixelSize + 1]; // Green
						totals += p[x * PixelSize + 2]; // Red
						totals += p[x * PixelSize + 3]; // Red
					}
				}
			}
			bmp.UnlockBits(bmData);
			

			return totals == 0;
		}
	}
}
