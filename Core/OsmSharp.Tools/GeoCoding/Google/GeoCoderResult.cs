using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OsmSharp.Tools.GeoCoding.Google
{
    /// <summary>
    /// Geocoder result for Google API.
    /// </summary>
    public class GeoCoderResult : IGeoCoderResult
    {
        #region IGeoCoderResult Members

        /// <summary>
        /// Gets/sets the latitude.
        /// </summary>
        public double Latitude
        {
            get;
            set;
        }

        /// <summary>
        /// Gets/sets the longitude.
        /// </summary>
        public double Longitude
        {
            get;
            set;
        }

        /// <summary>
        /// Gets/sets the accuracy.
        /// </summary>
        public AccuracyEnum Accuracy
        {
            get;
            set;
        }

        #endregion

        /// <summary>
        /// Gets/sets the text.
        /// </summary>
        public string Text
        {
            get;
            set;
        }
    }
}
