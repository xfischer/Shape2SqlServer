using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MapBind.Data.Models.BingMaps;
using System.Drawing;
using System.Drawing.Imaging;
using System.Configuration;

namespace MapBind.Data.Models.Raster
{
	public sealed class RasterFileSystem
	{
		private const string EMPTYTILES_FILENAME = "emptyTiles.txt";

		private string _databaseName;
		private string _tableName;
		private string _outputDir;
		private string _emptyTilesFilePath;

		public RasterFileSystem(string databaseName, string tableName)
		{
			_databaseName = databaseName;
			_tableName = tableName;
			_outputDir = GetOutputDirFromAppSettings();

			CreateDirIfNotExists(_outputDir);
			_outputDir = Path.Combine(_outputDir, _databaseName);
			CreateDirIfNotExists(_outputDir);
			_outputDir = Path.Combine(_outputDir, _tableName);
			CreateDirIfNotExists(_outputDir);

			_emptyTilesFilePath = Path.Combine(_outputDir, EMPTYTILES_FILENAME);

		}

		public static string GetOutputDirFromAppSettings()
		{
			string ret = ConfigurationManager.AppSettings["PyramidBaseDir"];
			if (ret == null)
				throw new ConfigurationErrorsException("PyramidBaseDir must be set in .config file for raster tile disk cache operation");
			else
				return ret;

		}

		#region Empty tiles file
		public void SaveEmptyTiles(HashSet<string> emptyQuadKeys)
		{
			// Save empty tiles hashTable as text file
			using (StreamWriter writer = new StreamWriter(_emptyTilesFilePath))
			{
				foreach (string quadKey in emptyQuadKeys)
					writer.WriteLine(quadKey);
			}
		}

		public HashSet<string> LoadEmptyTilesFile()
		{
			HashSet<string> emptyQuadKeys = new HashSet<string>();

			if (File.Exists(_emptyTilesFilePath))
			{

				// Save empty tiles hashTable as text file
				using (StreamReader reader = new StreamReader(_emptyTilesFilePath))
				{
					string quadKey;
					do
					{
						quadKey = reader.ReadLine();
						if (quadKey != null) emptyQuadKeys.Add(quadKey);
					}
					while (quadKey != null);
				}
			}

			return emptyQuadKeys;
		}
		#endregion

		#region Tiles

		#region GetTileFilePath

		public string GetTileFilePath(string quadKey)
		{
			int tileX, tileY, zoom;
			BingMapsTileSystem.QuadKeyToTileXY(quadKey, out tileX, out tileY, out zoom);
			return GetTileFilePath(zoom, tileX, tileY);
		}

		public string GetTileFilePath(int zoomLevel, int tileX, int tileY)
		{
			string quadKey = BingMapsTileSystem.TileXYToQuadKey(tileX, tileY, zoomLevel);

			return Path.Combine(_outputDir, _tableName + "-" + zoomLevel.ToString(), tileX.ToString(), tileY.ToString() + ".png");
		}

		public static string GetTileFilePath(string quadKey, string databaseName, string tableName)
		{
			int tileX, tileY, zoom;
			BingMapsTileSystem.QuadKeyToTileXY(quadKey, out tileX, out tileY, out zoom);
			return Path.Combine(RasterFileSystem.GetOutputDirFromAppSettings(), databaseName, tableName, tableName + "-" + zoom.ToString(), tileX.ToString(), tileY.ToString() + ".png");
		}

		#endregion

		public static bool ZoomDirectoryExists(string quadKey, string databaseName, string tableName)
		{
			int tileX, tileY, zoom;
			BingMapsTileSystem.QuadKeyToTileXY(quadKey, out tileX, out tileY, out zoom);
			return Directory.Exists(Path.Combine(RasterFileSystem.GetOutputDirFromAppSettings(), databaseName, tableName, tableName + "-" + zoom.ToString()));
		}

		private void EnsureTilePathExists(int zoomLevel, int tileX)
		{
			string zoomDir = Path.Combine(_outputDir, _tableName + "-" + zoomLevel.ToString());
			CreateDirIfNotExists(zoomDir);
			CreateDirIfNotExists(Path.Combine(zoomDir, tileX.ToString()));
		}

		public void SaveTileBitmap(Bitmap bmp, ImageFormat format, int zoomLevel, int tileX, int tileY)
		{
			this.EnsureTilePathExists(zoomLevel, tileX);
			bmp.Save(GetTileFilePath(zoomLevel, tileX, tileY), format);
		}

		public Bitmap GetTileBitmap(int zoomLevel, int tileX, int tileY)
		{
			Bitmap bmp = null;
			string tilePath = this.GetTileFilePath(zoomLevel, tileX, tileY);
			if (File.Exists(tilePath))
			{
				bmp = (Bitmap)Bitmap.FromFile(tilePath);
			}

			return bmp;
		}
		#endregion

		#region Helpers
		private void CreateDirIfNotExists(string dirPath)
		{
			if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);
		}
		#endregion
	}
}
