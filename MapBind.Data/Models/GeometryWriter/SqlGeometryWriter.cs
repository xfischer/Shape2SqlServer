using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Types;

namespace MapBind.Data.Models.GeometryWriter
{
	public sealed class SqlGeometryWriter : GeometryWriterBase<List<SqlGeometry>, SqlGeometry>
	{

		List<SqlGeometry> _output;

		public SqlGeometryWriter()
		{
			_output = new List<SqlGeometry>();
		}

		public override void WritePolygon(SqlGeometry polygon)
		{
			_output.Add(polygon);
		}

		public override void WriteMultiPolygon(SqlGeometry geom)
		{
			_output.Add(geom);
		}

		public override void WritePoint(SqlGeometry geom)
		{
			_output.Add(geom);
		}

		public override void WriteMultiPoint(SqlGeometry geom)
		{
			_output.Add(geom);
		}

		public override void WriteMultiLineString(SqlGeometry geom)
		{
			_output.Add(geom);
		}

		public override void WriteLineString(SqlGeometry geom)
		{
			_output.Add(geom);
		}

		public override List<SqlGeometry> GetOutput()
		{
			return _output;
		}

		public override SqlGeometry ConvertPoint(SqlGeometry point)
		{
			return point;
		}

		public override void Dispose()
		{
			
		}
	}
}
