#nullable enable
using System;
using NetTopologySuite.Geometries;

namespace Shape2SqlServer.Core;

/// <summary>
/// Specialized exception args for shape import exceptions.
/// </summary>
public sealed class ShapeImportExceptionEventArgs : UnhandledExceptionEventArgs
{
    /// <summary>
    /// Information about the shape that caused the exception.
    /// </summary>
    public string? ShapeInfo { get; }

    /// <summary>
    /// Geometry of the shape that caused the exception.
    /// </summary>
    public Geometry? ShapeGeom { get; }

    /// <summary>
    /// Index in the input file of the shape that caused the exception.
    /// </summary>
    public int ShapeIndex { get; }

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
    public ShapeImportExceptionEventArgs(Exception exception, bool isTerminating, string? shapeInfo, Geometry? shapeGeom, int recordIndex)
        : base(exception, isTerminating)
    {
        ShapeInfo = shapeInfo;
        Ignore = false;
        ShapeIndex = recordIndex;
        ShapeGeom = shapeGeom;
    }

    /// <summary>
    /// Creates a new instance of ShapeImportExceptionEventArgs.
    /// </summary>
    /// <param name="exception">Root exception</param>
    /// <param name="isTerminating">Indicates that this is error cannot be recovered</param>
    public ShapeImportExceptionEventArgs(Exception exception, bool isTerminating)
        : base(exception, isTerminating)
    {
        ShapeInfo = null;
        Ignore = false;
        ShapeIndex = 0;
        ShapeGeom = null;
    }
}
