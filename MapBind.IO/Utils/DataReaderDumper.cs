using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace Shape2SqlServer.Core
{
	internal static class DataReaderDumper
	{
		public static string DumpCurrentRecord(this IDataReader reader)
		{
			string dump = null;
			try
			{
				if (reader.IsClosed)
					dump = "Reader is closed";
				else
					for (int i = 0; i < reader.FieldCount - 1; i++)
					{
						dump += reader.GetName(i + 1) + ": ";
						object val = reader.GetValue(i);
						if (val == null)
							dump += "<null>";
						else
							dump += val.ToString();

						dump += "\n";
					}
			}
			catch (Exception e)
			{
				dump = "Cannot dump: " + e.Message;
			}
			return dump;
		}
	}
}
