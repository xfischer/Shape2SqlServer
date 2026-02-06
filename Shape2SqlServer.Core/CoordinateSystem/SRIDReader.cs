#nullable enable
using System.Collections.Generic;
using System.IO;
using ProjNet;
using ProjNet.CoordinateSystems;
using Shape2SqlServer.Core.Properties;

namespace Shape2SqlServer.Core;

/// <summary>
/// Helper to read SRID.csv and get coordinate systems.
/// </summary>
public class SRIDReader
{
    /// <summary>
    /// Well-known Text string with its SRID
    /// </summary>
    public class WKTstring
    {
        /// <summary>
        /// Well-known ID
        /// </summary>
        public int WKID;

        /// <summary>
        /// Well-known Text
        /// </summary>
        public string WKT = string.Empty;

        /// <summary>
        /// String representation
        /// </summary>
        /// <returns></returns>
        public override string ToString() => $"{WKID}: {WKT}";
    }

    /// <summary>
    /// Enumerates all SRID's in the SRID.csv file.
    /// </summary>
    /// <returns>Enumerator</returns>
    public static IEnumerable<WKTstring> GetSRIDs()
    {
        using MemoryStream memStream = new(Resources.SRID);
        using StreamReader sr = new(memStream);
        while (!sr.EndOfStream)
        {
            string? line = sr.ReadLine();
            if (line == null)
                continue;

            int split = line.IndexOf(';');
            if (split > -1)
            {
                WKTstring wkt = new()
                {
                    WKID = int.Parse(line.Substring(0, split)),
                    WKT = line.Substring(split + 1)
                };
                yield return wkt;
            }
        }
    }

    /// <summary>
    /// Gets a coordinate system from the SRID.csv file
    /// </summary>
    /// <param name="id">EPSG ID</param>
    /// <returns>Coordinate system, or null if SRID was not found.</returns>
    public static CoordinateSystem? GetCSbyID(int id)
    {
        CoordinateSystemFactory fac = new();
        foreach (WKTstring wkt in GetSRIDs())
        {
            if (wkt.WKID == id)
            {
                return fac.CreateFromWkt(wkt.WKT);
            }
        }
        return null;
    }
}
