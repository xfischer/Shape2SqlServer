using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GeoAPI.Geometries;

namespace Shape2SqlServer.Core
{
    /// <summary>
    /// Specialized exception args for shape import exceptions.
    /// </summary>
    public sealed class ShapeImportExceptionEventArgs : UnhandledExceptionEventArgs
	{
		private string _shapeInfo;
        /// <summary>
        /// Information about the shape that caused the exception.
        /// </summary>
        public string ShapeInfo { get { return _shapeInfo; } }

		private IGeometry _shapeGeom;
        /// <summary>
        /// Geometry of the shape that caused the exception.
        /// </summary>
        public IGeometry ShapeGeom { get { return _shapeGeom; } }

		private int _shapeIndex;
        /// <summary>
        /// Index in the input file of the shape that caused the exception.
        /// </summary>
        public int ShapeIndex { get { return _shapeIndex; } }

        /// <summary>
        /// Flag to ignore the error and continue processing.
        /// </summary>
        public bool Ignore { get; set; }

        /// <summary>
        /// Creates a new instance of ShapeImportExceptionEventArgs.
        /// </summary>
        /// <param name="exception">Root exception that caused the issue</param>
        /// <param name="isTerminating">Indicates that this is error cannot be recovered</param>
        /// <param name="shapeInfo">Information about the shape that caused the exception</param>
        /// <param name="shapeGeom">Geometry of the shape that caused the exception</param>
        /// <param name="recordIndex">Index in the input file of the shape that caused the exception</param>
        public ShapeImportExceptionEventArgs(Exception exception, bool isTerminating, string shapeInfo, IGeometry shapeGeom, int recordIndex)
			: base(exception, isTerminating)
		{
			_shapeInfo = shapeInfo;
			Ignore = false;
			_shapeIndex = recordIndex;
			_shapeGeom = shapeGeom;

		}

        /// <summary>
        /// Creates a new instance of ShapeImportExceptionEventArgs.
        /// </summary>
        /// <param name="exception">Root exception</param>
        /// <param name="isTerminating">Indicates that this is error cannot be recovered</param>
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
