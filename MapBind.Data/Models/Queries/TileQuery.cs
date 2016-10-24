using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MapBind.Data.Models
{
	public class TileQuery : QueryBase
	{
		public int X { get; set; }
		public int Y { get; set; }
		public int Z { get; set; }
	}
}
