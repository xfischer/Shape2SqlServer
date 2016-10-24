using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MapBind.Data.Models.SqlServer
{
	public struct SqlColumnDescriptor
	{
		public string Name;
		public string SqlType;
		public Type Type;

		public SqlColumnDescriptor(string name, string sqlType, Type type)
		{
			Name = name;
			SqlType = sqlType;
			Type = type;
		}
	}
}
