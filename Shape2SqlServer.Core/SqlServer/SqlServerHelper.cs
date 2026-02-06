#nullable enable

using System;
using System.Data.SqlTypes;
using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.Types;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace Shape2SqlServer.Core;

internal static class SqlServerHelper
{
	private const double INVALIDGEOM_BUFFER = 0.000001d;
		private const double INVALIDGEOM_REDUCE = 0.00000025d;
		internal static bool? REVERSE_GEOMETRIES = null;

		internal static object? ConvertToSqlType(Geometry geom, int SRID, bool useGeography, int curRowIndex, ILogger logger)
		{
			object? v_ret = null;

			// Set geom SRID
			geom.SRID = SRID;

				if (useGeography)
				{
					try
					{

						// SQL server needs anticlockwise rings
						if (!REVERSE_GEOMETRIES.HasValue)
						{
							enRingOrientation ringOrientation = SqlServerHelper.GetRingOrientation(geom.Coordinates);
							if (ringOrientation == enRingOrientation.Clockwise)
								REVERSE_GEOMETRIES = true;
						}

						SqlServerBytesWriter geoWriter = new();
						SqlGeography? v_geog = null;
						if (REVERSE_GEOMETRIES.GetValueOrDefault(false))
							v_geog = SqlGeography.Deserialize(new SqlBytes(geoWriter.Write(geom.Reverse())));
						else
							v_geog = SqlGeography.Deserialize(new SqlBytes(geoWriter.Write(geom)));
						if (!v_geog.STIsValid().Value)
						{
							logger.LogWarning("Invalid geometry. Must call make valid: {Details}", v_geog.IsValidDetailed());
							v_geog = v_geog.MakeValid();
						}
						v_ret = v_geog;
					}
					catch (OutOfMemoryException exMemory)
					{
						logger.LogError(exMemory, "OutOfMemory on geom #{RowIndex}", curRowIndex);
						throw;
					}
					catch (Exception exWriteGeom)
					{
						try
						{

							enRingOrientation ringOrientation = SqlServerHelper.GetRingOrientation(geom.Coordinates);

							logger.LogWarning(exWriteGeom, "Invalid geom #{RowIndex}. Try to reverse", curRowIndex);
							// Maybe bad orientation
							SqlServerBytesWriter geogWriter = new();
							v_ret = SqlGeography.Deserialize(new SqlBytes(geogWriter.Write(geom.Reverse())));

							REVERSE_GEOMETRIES = true;

						}
						catch (OutOfMemoryException exMemory)
						{
							logger.LogError(exMemory, "OutOfMemory on geom #{RowIndex}", curRowIndex);
							throw;
						}
						catch (Exception exReverse)
						{
							try
							{
								logger.LogWarning(exReverse, "Bad reverse, converting to geometry");
								// Maybe a self intersecting polygon. Use the buffer trick with the geometry
								SqlServerBytesWriter geoWriter = new();
								SqlGeometry sqlGeom = SqlGeometry.Deserialize(new SqlBytes(geoWriter.Write(geom)));
								if (!sqlGeom.STIsValid().Value)
								{
									logger.LogInformation("Make valid - OK");
									v_ret = SqlGeography.STGeomFromText(new SqlChars(new SqlString(sqlGeom.MakeValid().ToString())), SRID);
								}
								else
								{
									logger.LogInformation("Buffer - OK");
									v_ret = SqlGeography.STGeomFromText(new SqlChars(new SqlString(sqlGeom.STBuffer(INVALIDGEOM_BUFFER).STBuffer(-INVALIDGEOM_BUFFER).Reduce(INVALIDGEOM_REDUCE).ToString())), SRID);
								}
							}
							catch (OutOfMemoryException exMemory)
							{
								logger.LogError(exMemory, "OutOfMemory on geom #{RowIndex}", curRowIndex);
								throw;
							}
							catch (Exception exBuffer)
							{
								logger.LogError(exBuffer, "Failed to fix geometry");
								throw;
							}

						}
					}
				}
				else
				{
					SqlServerBytesWriter geoWriter = new();
					SqlGeometry v_retGeom = SqlGeometry.Deserialize(new SqlBytes(geoWriter.Write(geom)));
					if (!v_retGeom.STIsValid().Value)
					{
						logger.LogWarning("Invalid geometry. Must call make valid: {Details}", v_retGeom.IsValidDetailed());
						v_retGeom = v_retGeom.MakeValid();
					}
					v_ret = v_retGeom;
				}

			return v_ret;
		}

		internal static enRingOrientation GetRingOrientation(Coordinate[] coordinates)
		{
			// Inspired by http://www.engr.colostate.edu/~dga/dga/papers/point_in_polygon.pdf

			// This algorithm is to simply determine the Ring Orientation, so to do so, find the
			// extreme left and right points, and then check orientation

			if (coordinates.Length < 4)
			{
				return enRingOrientation.Unknown;
				//throw new ArgumentException("A polygon requires at least 4 points.");
			}

			if (coordinates[0].X != coordinates[coordinates.Length - 1].X || coordinates[0].Y != coordinates[coordinates.Length - 1].Y)
			{
				return enRingOrientation.Unknown;
			}

			int rightmostIndex = 0;
			int leftmostIndex = 0;

			for (int i = 1; i < coordinates.Length; i++)
			{
				if (coordinates[i].X < coordinates[leftmostIndex].X)
				{
					leftmostIndex = i;
				}
				if (coordinates[i].X > coordinates[rightmostIndex].X)
				{
					rightmostIndex = i;
				}
			}


			Coordinate p0; // Point before the extreme
			Coordinate p1; // The extreme point
			Coordinate p2; // Point after the extreme

			double m; // Holds line slope

			double lenP2x;  // Length of the P1-P2 line segment's delta X
			double newP0y;  // The Y value of the P1-P0 line segment adjusted for X=lenP2x

			enRingOrientation left_orientation;
			enRingOrientation right_orientation;

			// Determine the orientation at the Left Point
			if (leftmostIndex == 0)
				p0 = coordinates[coordinates.Length - 2];
			else
				p0 = coordinates[leftmostIndex - 1];

			p1 = coordinates[leftmostIndex];

			if (leftmostIndex == coordinates.Length - 1)
				p2 = coordinates[1];
			else
				p2 = coordinates[leftmostIndex + 1];

			m = (p1.Y - p0.Y) / (p1.X - p0.X);

			if (double.IsInfinity(m))
			{
				// This is a vertical line segment, so just calculate the dY to
				// determine orientation

				left_orientation = (enRingOrientation)Math.Sign(p0.Y - p1.Y);
			}
			else if (double.IsNaN(m))
			{
				lenP2x = p2.X - p1.X;
				newP0y = p1.Y;


				left_orientation = (enRingOrientation)Math.Sign(newP0y - p2.Y);
			}
			else
			{
				lenP2x = p2.X - p1.X;
				newP0y = p1.Y + (m * lenP2x);


				left_orientation = (enRingOrientation)Math.Sign(newP0y - p2.Y);
			}



			// Determine the orientation at the Right Point
			if (rightmostIndex == 0)
				p0 = coordinates[coordinates.Length - 2];
			else
				p0 = coordinates[rightmostIndex - 1];

			p1 = coordinates[rightmostIndex];

			if (rightmostIndex == coordinates.Length - 1)
				p2 = coordinates[1];
			else
				p2 = coordinates[rightmostIndex + 1];

			m = (p1.Y - p0.Y) / (p1.X - p0.X);

			if (double.IsInfinity(m))
			{
				// This is a vertical line segment, so just calculate the dY to
				// determine orientation

				right_orientation = (enRingOrientation)Math.Sign(p1.Y - p0.Y);
			}
			else if (double.IsNaN(m))
			{
				lenP2x = p2.X - p1.X;
				newP0y = p1.Y;

				right_orientation = (enRingOrientation)Math.Sign(p2.Y - newP0y);
			}
			else
			{
				lenP2x = p2.X - p1.X;
				newP0y = p1.Y + (m * lenP2x);

				right_orientation = (enRingOrientation)Math.Sign(p2.Y - newP0y);
			}


			if (left_orientation == enRingOrientation.Unknown)
			{
				return right_orientation;
			}
			else
			{
				return left_orientation;
			}
		}

		internal static BoundingBox GetBoundingBox(Envelope bounds)
		{
			return new BoundingBox(bounds.MinX, bounds.MinY, bounds.MaxX, bounds.MaxY);
		}
	}
