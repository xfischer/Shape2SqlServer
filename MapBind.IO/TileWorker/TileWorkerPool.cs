using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using MapBind.Data.Business;
using MapBind.Data.Models;
using MapBind.Data.Models.Raster;
using MapBind.Data.Models.BingMaps;

namespace MapBind.IO.TileWorker
{
	public sealed class TileWorkerPool
	{
		private BackgroundWorker _bgWorker;

		public event EventHandler<TileWorkerEventArgs> TileGenerated;

		private long _numTiles;
		private long _numTilesEmpty;
		private long _numTilesSkipped;

		private long _dbg;


		private string _tableName;
		private RasterFileSystem _rasterFS;
		private BitmapDataService _svc;


		private HashSet<string> _emptyQuadKeys;

		public event EventHandler Done;

		public TileWorkerPool(string databaseName, string tableName, BitmapDataService svc)
		{
			_rasterFS = new RasterFileSystem(databaseName, tableName);

			_tableName = tableName;
			_svc = svc;

		}


		public void Run(int startZoom, int endZoom)
		{
			startZoom = Math.Max(1, Math.Min(startZoom, endZoom));
			endZoom = Math.Max(startZoom, Math.Min(endZoom, 23));

			_bgWorker = new BackgroundWorker();
			_bgWorker.WorkerReportsProgress = true;
			_bgWorker.WorkerSupportsCancellation = true;
			_bgWorker.DoWork += new DoWorkEventHandler(bgWorker_DoWork);
			_bgWorker.ProgressChanged += new ProgressChangedEventHandler(bgWorker_ProgressChanged);
			_bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgWorker_RunWorkerCompleted);

			_bgWorker.RunWorkerAsync(new int[] { startZoom, endZoom });
		}

		void bgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			_rasterFS.SaveEmptyTiles(_emptyQuadKeys);

			if (this.Done != null) Done(this, new EventArgs());
		}

		void bgWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			_dbg++;
			if (TileGenerated != null) TileGenerated(this, (TileWorkerEventArgs)e.UserState);
		}

		void bgWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			BackgroundWorker worker = (BackgroundWorker)sender;
			int[] range = (int[])e.Argument;
			int minzoom = range[0];
			int maxzoom = range[1];

			_emptyQuadKeys = _rasterFS.LoadEmptyTilesFile();

			for (int z = minzoom; z <= maxzoom; z++)
			{
				if (worker.CancellationPending) return;
				GenerateTileSet4Zoom(worker, z);
			}
		}

		public void Cancel()
		{
			_bgWorker.CancelAsync();
		}


		private BingTileQuery CreateQuery(string quadKey, string tableName, enDiskCacheMode cacheMode)
		{
			BingTileQuery query = new BingTileQuery(quadKey, cacheMode, Color.OrangeRed, Color.Black, 1);
			query.AddTable(tableName);
			return query;
		}

		private void CreateDirIfNotExists(string dirPath)
		{
			if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);
		}

		private void GenerateTileSet4Zoom(BackgroundWorker worker, int zoomLevel)
		{
			double numTiles = BingMapsTileSystem.MapSize(zoomLevel) / 256;

			for (int x = 0; x < numTiles; x++)
			{
				for (int y = 0; y < numTiles; y++)
				{
					GenerateTile(worker, zoomLevel, x, y);
					if (worker.CancellationPending) return;
				}
				worker.ReportProgress(0, new TileWorkerEventArgs(_numTiles, _numTilesEmpty, _numTilesSkipped));
			}
		}

		private void GenerateTile(BackgroundWorker worker, int zoomLevel, int tileX, int tileY)
		{
			string quadKey = BingMapsTileSystem.TileXYToQuadKey(tileX, tileY, zoomLevel);

			if (this.IsParentTileEmpty(quadKey))
			{
				Interlocked.Increment(ref _numTilesSkipped);
			}
			else
			{

				BingTileQuery query = this.CreateQuery(quadKey, _tableName, enDiskCacheMode.ReadWrite);
				Bitmap bmp = _svc.GetImage(query);

				bool isEmpty = bmp.Tag != null;

				if (isEmpty)
				{
					_emptyQuadKeys.Add(quadKey);
					Interlocked.Increment(ref _numTilesEmpty);
				}
				else
				{
					//_rasterFS.SaveTileBitmap(bmp, ImageFormat.Png, zoomLevel, tileX, tileY);
					//bmp.Dispose();

					Interlocked.Increment(ref _numTiles);
				}
			}
		}

		private bool IsParentTileEmpty(string quadKey)
		{
			do
			{
				if (_emptyQuadKeys.Contains(quadKey))
					return true;

				quadKey = quadKey.Remove(quadKey.Length - 1);
			}
			while (quadKey != string.Empty);

			return false;
		}


	}
}
