using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GeoAPI.Geometries;
using System.Text.RegularExpressions;
using System.Data;
using Microsoft.SqlServer.Types;
using System.Diagnostics;
using NetTopologySuite.IO;
using System.Data.SqlTypes;


namespace Shape2SqlServer.Core
{
	internal static class SqlServerHelper
	{

		private const double INVALIDGEOM_BUFFER = 0.000001d;
		private const double INVALIDGEOM_REDUCE = 0.00000025d;
		internal static bool? REVERSE_GEOMETRIES = null;

		internal static object ConvertToSqlType(IGeometry geom, int SRID, bool useGeography, int curRowIndex)
		{
			object v_ret = null;
			try
			{
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

						MsSql2008GeographyWriter geoWriter = new MsSql2008GeographyWriter();
						SqlGeography v_geog = null;
						if (REVERSE_GEOMETRIES.GetValueOrDefault(false))
							v_geog = geoWriter.WriteGeography(geom.Reverse());
						else
							v_geog = geoWriter.WriteGeography(geom);
						if (!v_geog.STIsValid().Value)
						{
							Trace.TraceWarning(string.Format("Invalid geometry. Must call make valid : {0}", v_geog.IsValidDetailed()));
							v_geog = v_geog.MakeValid();
						}
						v_ret = v_geog;
					}
					catch (OutOfMemoryException exMemory)
					{
						Trace.WriteLine(string.Format("OutOfMemory on geom #{0} ({1}: {2})", curRowIndex, exMemory.GetType().Name, exMemory.Message));
						throw;
					}
					catch (Exception exWriteGeom)
					{
						try
						{

							enRingOrientation ringOrientation = SqlServerHelper.GetRingOrientation(geom.Coordinates);

							Trace.WriteLine(string.Format("Invalid geom #{0} ({1}: {2}). Try to reverse", curRowIndex, exWriteGeom.GetType().Name, exWriteGeom.Message));
							// Maybe bad orientation
							MsSql2008GeographyWriter geogWriter = new MsSql2008GeographyWriter();
							v_ret = geogWriter.WriteGeography(geom.Reverse());

							REVERSE_GEOMETRIES = true;

						}
						catch (OutOfMemoryException exMemory)
						{
							Trace.WriteLine(string.Format("OutOfMemory on geom #{0} ({1}: {2})", curRowIndex, exMemory.GetType().Name, exMemory.Message));
							throw;
						}
						catch (Exception exReverse)
						{
							try
							{
								Trace.Write(string.Format("Bad reverse ({0}: {1})/ Converting to geometry", exReverse.GetType().Name, exReverse.Message));
								// Maybe a self intersecting polygon. Use the buffer trick with the geometry
								MsSql2008GeometryWriter geoWriter = new MsSql2008GeometryWriter();
								SqlGeometry sqlGeom = geoWriter.WriteGeometry(geom);
								if (!sqlGeom.STIsValid().Value)
								{
									Trace.Write(" / Make valid");
									v_ret = SqlGeography.STGeomFromText(new SqlChars(new SqlString(sqlGeom.MakeValid().ToString())), SRID);
									Trace.WriteLine(" / OK");
								}
								else
								{
									Trace.Write(" / Buffer");
									v_ret = SqlGeography.STGeomFromText(new SqlChars(new SqlString(sqlGeom.STBuffer(INVALIDGEOM_BUFFER).STBuffer(-INVALIDGEOM_BUFFER).Reduce(INVALIDGEOM_REDUCE).ToString())), SRID);
									Trace.WriteLine(" / OK");
								}
							}
							catch (OutOfMemoryException exMemory)
							{
								Trace.WriteLine(string.Format("OutOfMemory on geom #{0} ({1}: {2})", curRowIndex, exMemory.GetType().Name, exMemory.Message));
								throw;
							}
							catch (Exception exBuffer)
							{
								Trace.WriteLine(string.Format(" / KO ({0}: {1})", exBuffer.GetType().Name, exBuffer.Message));
								throw;
							}

						}
					}
				}
				else
				{
					MsSql2008GeometryWriter geoWriter = new MsSql2008GeometryWriter();
					SqlGeometry v_retGeom = geoWriter.WriteGeometry(geom);
					if (!v_retGeom.STIsValid().Value)
					{
						Trace.TraceWarning(string.Format("Invalid geometry. Must call make valid : {0}", v_retGeom.IsValidDetailed()));
						v_retGeom = v_retGeom.MakeValid();
					}
					v_ret = v_retGeom;
					//feature.geomWKT = geoWriter.WriteGeometry(geomOut).ToString();
				}
			}
			catch (Exception)
			{
				throw;
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
}
