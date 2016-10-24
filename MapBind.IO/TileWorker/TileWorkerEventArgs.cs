using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using MapBind.Data.Business;
using System.Threading;
using System.Drawing;
using MapBind.Data.Models;
using MapBind.Data.Models.BingMaps;
using System.Drawing.Imaging;
using System.IO;

namespace MapBind.IO.TileWorker
{
	public sealed class TileWorkerEventArgs : EventArgs
	{
		public long NumTiles { get; private set; }
		public long NumEmptyTiles { get; private set; }
		public long NumSkippedTiles { get; private set; }

		public TileWorkerEventArgs(long progress)
			: base()
		{
			this.NumTiles = progress;
		}

		public TileWorkerEventArgs(long numTiles, long numEmptyTiles, long numSkipped)
			: base()
		{
			this.NumTiles = numTiles;
			this.NumEmptyTiles= numEmptyTiles;
			this.NumSkippedTiles = numSkipped;
		}
	}
}
