using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GeoAPI.Geometries;

namespace MapBind.IO.ShapeFile
{
	public sealed class ShapeImportExceptionEventArgs : UnhandledExceptionEventArgs
	{
		private string _shapeInfo;
		public string ShapeInfo { get { return _shapeInfo; } }

		private IGeometry _shapeGeom;
		public IGeometry ShapeGeom { get { return _shapeGeom; } }

		private int _shapeIndex;
		public int ShapeIndex { get { return _shapeIndex; } }

		public bool Ignore { get; set; }

		public ShapeImportExceptionEventArgs(Exception exception, bool isTerminating, string shapeInfo, IGeometry shapeGeom, int recordIndex)
			: base(exception, isTerminating)
		{
			_shapeInfo = shapeInfo;
			Ignore = false;
			_shapeIndex = recordIndex;
			_shapeGeom = shapeGeom;

		}

		public ShapeImportExceptionEventArgs(Exception exception, bool isTerminating)
			: base(exception, isTerminating)
		{
			_shapeInfo = null;
			Ignore = false;
			_shapeIndex = 0;
			_shapeGeom = null;

		}
	}
}
