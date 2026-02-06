#nullable enable
using System;
using System.Data;
using System.Text;

namespace Shape2SqlServer.Core;

internal static class DataReaderDumper
{
    public static string DumpCurrentRecord(this IDataReader reader)
    {
        string? dump = null;
        try
        {
            if (reader.IsClosed)
            {
                dump = "Reader is closed";
            }
            else
            {
                var sb = new StringBuilder();
                for (int i = 0; i < reader.FieldCount - 1; i++)
                {
                    sb.Append($"{reader.GetName(i + 1)}: ");
                    object? val = reader.GetValue(i);
                    if (val == null)
                        sb.Append("<null>");
                    else
                        sb.Append(val.ToString());

                    sb.Append('\n');
                }
                dump = sb.ToString();
            }
        }
        catch (Exception e)
        {
            dump = $"Cannot dump: {e.Message}";
        }
        return dump;
    }
}
