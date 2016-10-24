using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;


namespace MapBind.Data.Models.GeoJson
{
    /// <summary>
    /// This class is returned to the client. It contains all of the data to be displayed.
    /// </summary>
    public class GeoJsonResult
    {
        public string error { get; set; }

        public Dictionary<string, FeatureCollection> featureSet { get; set; }

        public Metrics metrics { get; set; }

        public GeoJsonResult()
        {
            featureSet = new Dictionary<string, FeatureCollection>();
            metrics = new Metrics(enMetricsType.None);
        }
    }
}