using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MapBind.Data.Models.Style;
using Microsoft.SqlServer.Types;

namespace MapBind.Data.Models.GeometryWriter
{
	public abstract class GeometryWriterBase<TOutput, TPoint> : IDisposable
	{

		public void WriteGeometry(SqlGeometry geom)
		{
			try
			{

				if (!geom.STIsEmpty().Value)
				{
					switch (geom.STGeometryType().ToString())
					{
						#region GeometryCollection
						case "GeometryCollection":

							for (int i = 1; i <= geom.STNumGeometries().Value; i++)
								this.WriteGeometry(geom.STGeometryN(i));

							break;
						#endregion

						#region LineString
						case "LineString":

							this.WriteLineString(geom);

							break;
						#endregion

						#region MultiLineString
						case "MultiLineString":

							this.WriteMultiLineString(geom);
							break;

						#endregion

						#region MultiPoint
						case "MultiPoint":

							this.WriteMultiPoint(geom);

							break;
						#endregion

						#region Point
						case "Point":

							this.WritePoint(geom);

							break;
						#endregion

						#region MultiPolygon
						case "MultiPolygon":

							this.WriteMultiPolygon(geom);

							break;
						#endregion

						#region Polygon
						case "Polygon":

							this.WritePolygon(geom);


							break;
						#endregion

					}
				}
			}
			catch (Exception)
			{
				throw;
			}

		}

		public abstract void WritePolygon(SqlGeometry polygon);

		internal protected List<SqlGeometry> GetPolygonInteriorRings(SqlGeometry polygon)
		{
			List<SqlGeometry> interiorRings = null;
			int numInteriorRings = polygon.STNumInteriorRing().Value;
			if (numInteriorRings > 0)
			{
				interiorRings = new List<SqlGeometry>();
				for (int i = 1; i <= numInteriorRings; i++)
					interiorRings.Add(polygon.STInteriorRingN(i));
			}

			return interiorRings;

		}

		public abstract void WriteMultiPolygon(SqlGeometry geom);

		public abstract void WritePoint(SqlGeometry geom);

		public abstract void WriteMultiPoint(SqlGeometry geom);

		public abstract void WriteMultiLineString(SqlGeometry geom);

		public abstract void WriteLineString(SqlGeometry geom);

		public abstract TOutput GetOutput();

		public abstract TPoint ConvertPoint(SqlGeometry point);



		#region IDisposable Membres

		public abstract void Dispose();

		#endregion
	}
}
