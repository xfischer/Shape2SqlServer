#nullable enable
using System;

namespace Shape2SqlServer.Core;

/// <summary>
/// Represents a geographic bounding box with immutable semantics.
/// </summary>
internal record BoundingBox(double MinX, double MinY, double MaxX, double MaxY)
{
	/// <summary>
	/// Gets the width of the bounding box (MaxX - MinX).
	/// </summary>
	public double Width => MaxX - MinX;

	/// <summary>
	/// Gets the height of the bounding box (MaxY - MinY).
	/// </summary>
	public double Height => MaxY - MinY;

	/// <summary>
	/// Gets the aspect ratio (Width / Height).
	/// </summary>
	public double AspectRatio => Width / Height;

	/// <summary>
	/// Returns a string representation of the bounding box.
	/// </summary>
	public override string ToString() => $"[{MinX}, {MinY}, {MaxX}, {MaxY}]";
}
