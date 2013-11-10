using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using ServiceStack.Text;

namespace OsmSharp.Tools.GeoCoding.Google
{
    /// <summary>
    /// A geocoder class using the Google Geocoding API.
    /// </summary>
    /// <remarks>Make sure you read the Google Maps TOS: http://developers.google.com/maps/terms#section_10_12 </remarks>
    public class GeoCoder : IGeoCoder
    {
        /// <summary>
        /// The url of the google maps static api.
        /// </summary>
        private static string _GOOGLE_GEOCODER_URL =
            "http://maps.googleapis.com/maps/api/geocode/json?address={0}&sensor=false";

        /// <summary>
        /// Creates a new geocoder.
        /// </summary>
        public GeoCoder()
        {

        }

        #region IGeoCoder Members

        /// <summary>
        /// Geocodes and returns the result.
        /// </summary>
        /// <param name="country"></param>
        /// <param name="postal_code"></param>
        /// <param name="commune"></param>
        /// <param name="street"></param>
        /// <param name="house_number"></param>
        /// <returns></returns>
        public IGeoCoderResult Code(string country,
            string postal_code,
            string commune,
            string street,
            string house_number)
        {
            // build the address string.
            StringBuilder builder = new StringBuilder();
            builder.Append(street);
            builder.Append(" ");
            builder.Append(house_number);
            builder.Append(" ");
            builder.Append(postal_code);
            builder.Append(" ");
            builder.Append(commune);
            builder.Append(" ");
            builder.Append(country);
            builder.Append(" ");

            // the request url.
            string url = string.Format(_GOOGLE_GEOCODER_URL, builder.ToString());
            System.Diagnostics.Debug.WriteLine(url);

            // check if there are responses.
            GeoCoderResult res = new GeoCoderResult();
            res.Accuracy = AccuracyEnum.UnkownLocationLevel;

            // create the source and get the json object.
            JsonObject source = this.DownloadJson(url);

            int count = source.Count - 1;
            var test = source["status"];
            if (test == "OK")
            {
                // get the geometry object from the result.
                var result = source.ArrayObjects("results");
                var geometry = result[0].Object("geometry");

                // determine the accuracy.
                var locationType = geometry["location_type"];
                if (locationType != null)
                {
                    locationType = locationType.ToUpper();
                    switch (locationType)
                    {
                        case "ROOFTOP":
                            res.Accuracy = AccuracyEnum.AddressLevel;
                            break;
                        case "RANGE_INTERPOLATED":
                            res.Accuracy = AccuracyEnum.IntersectionLevel;
                            break;
                        case "GEOMETRIC_CENTER":
                            res.Accuracy = AccuracyEnum.TownLevel;
                            break;
                        case "APPROXIMATE":
                            res.Accuracy = AccuracyEnum.RegionLevel;
                            break;
                        default:
                            res.Accuracy = AccuracyEnum.UnkownLocationLevel;
                            break;
                    }
                }

                // get the location.
                var location = geometry.Object("location");
                var latString = location["lat"];
                var lonString = location["lng"];
                if (latString != null && lonString != null)
                {
                    float latitude = float.Parse(latString, System.Globalization.CultureInfo.InvariantCulture);
                    float longitude = float.Parse(lonString, System.Globalization.CultureInfo.InvariantCulture);

                    res.Latitude = latitude;
                    res.Longitude = longitude;
                }

                // set the geocoded text
                res.Text = builder.ToString();
            }
            return res;
        }

        #endregion

        #region Base-Google-Api-Functions

        /// <summary>
        /// Downloads an json from an url.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private JsonObject DownloadJson(string url)
        {
            // download the json string.
            string json = this.DownloadString(url);

            // parse the xml if it exists.
            JsonObject source = null;
            if (json != null && json.Length > 0)
            {
                source = JsonObject.Parse(json);
            }
            return source;
        }

        /// <summary>
        /// Downloads a string from an url.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private string DownloadString(string url)
        {
            // create the webclient if needed.
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(
                     url);
            request.Timeout = 10000;

            try
            { // try to download the string.
                WebResponse myResp = request.GetResponse();
                StreamReader stream = new StreamReader(myResp.GetResponseStream());
                return stream.ReadToEnd();
            }
            catch (WebException)
            {
                return string.Empty;
            }
        }

        #endregion
    }
}
