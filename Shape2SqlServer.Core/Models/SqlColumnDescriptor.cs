#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shape2SqlServer.Core;

internal struct SqlColumnDescriptor(string name, string sqlType, Type type)
{
	public string Name = name;
	public string SqlType = sqlType;
	public Type Type = type;
}
