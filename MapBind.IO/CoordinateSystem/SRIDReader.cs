using System.Collections.Generic;
using System.IO;
using ProjNet.Converters.WellKnownText;
using ProjNet.CoordinateSystems;
using GeoAPI.CoordinateSystems;
using Shape2SqlServer.Core.Properties;

namespace Shape2SqlServer.Core
{
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
			public string WKT;

            /// <summary>
            /// String representation
            /// </summary>
            /// <returns></returns>
            public override string ToString()
			{
				return WKID.ToString() + ": " + WKT;
			}

		}

		/// <summary>
		/// Enumerates all SRID's in the SRID.csv file.
		/// </summary>
		/// <returns>Enumerator</returns>
		public static IEnumerable<WKTstring> GetSRIDs()
		{
			using (MemoryStream memStream = new MemoryStream(Resources.SRID))
			{
				using (StreamReader sr = new StreamReader(memStream))
				{
					while (!sr.EndOfStream)
					{
						string line = sr.ReadLine();
						int split = line.IndexOf(';');
						if (split > -1)
						{
							WKTstring wkt = new WKTstring();
							wkt.WKID = int.Parse(line.Substring(0, split));
							wkt.WKT = line.Substring(split + 1);
							yield return wkt;
						}
					}
					sr.Close();
				}
			}
		}
		/// <summary>
		/// Gets a coordinate system from the SRID.csv file
		/// </summary>
		/// <param name="id">EPSG ID</param>
		/// <returns>Coordinate system, or null if SRID was not found.</returns>
		public static ICoordinateSystem GetCSbyID(int id)
		{
			CoordinateSystemFactory fac = new CoordinateSystemFactory();
			foreach (WKTstring wkt in GetSRIDs())
			{
				if (wkt.WKID == id)
				{
					return CoordinateSystemWktReader.Parse(wkt.WKT) as ICoordinateSystem;
				}
			}
			return null;
		}
	}
}
