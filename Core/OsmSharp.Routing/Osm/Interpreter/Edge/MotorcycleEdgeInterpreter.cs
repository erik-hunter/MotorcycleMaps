using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OsmSharp.Routing.Interpreter.Roads;
using OsmSharp.Routing;
using OsmSharp.Tools.Math.Geo;
using OsmSharp.Tools.Math.Units.Speed;

namespace OsmSharp.Routing.Osm.Interpreter.Edge
{
    /// <summary>
    /// Default edge interpreter.
    /// </summary>
    public class MotorcycleEdgeInterpreter : EdgeInterpreter
    {

        /// <summary>
        /// Returns the weight between two points on an edge with the given tags for the given vehicle.
        /// </summary>
        /// <param name="tags"></param>
        /// <param name="vehicle"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public new double Weight(IDictionary<string, string> tags, VehicleEnum vehicle, GeoCoordinate from, GeoCoordinate to)
        {
            throw new NotImplementedException("The Weight() function in MotorcycleEdgeInterpreter should only be called on three points!");
        }

        /// <summary>
        /// Returns the weight between two points on an edge with the given tags for the given vehicle.
        /// </summary>
        /// <param name="tags"></param>
        /// <param name="previous"></param>
        /// <param name="vehicle"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public double Weight(IDictionary<string, string> tags, VehicleEnum vehicle, GeoCoordinate previous, GeoCoordinate from, GeoCoordinate to)
        {
            if (previous.Equals(from) || from.Equals(to))
                return Weight(tags, vehicle, from, to);
            //  Get a cartesian equation for the previous-from points, using point slope form.
            //  This just ignores the difference in distance at different latitudes for now.
            //  lat - lat1 = m(long - long1)
            double firstSlope = (from.Latitude - previous.Latitude) / (from.Longitude - previous.Longitude);
            firstSlope = 1.0 / firstSlope;  //  Inverse slope for the perpendicular bisector
            double secondSlope = (to.Latitude - from.Latitude) / (to.Longitude - from.Longitude);
            secondSlope = 1.0 / secondSlope;
            GeoCoordinate firstMidpoint = midPoint(previous, from);
            GeoCoordinate secondMidpoint = midPoint(from, to);

            double xIntersect = ((firstSlope * firstMidpoint.Longitude) + firstMidpoint.Latitude - secondMidpoint.Latitude + (secondSlope * secondMidpoint.Longitude)) / (secondSlope - firstSlope);
            double yIntersect = (firstSlope * (xIntersect - firstMidpoint.Longitude)) + firstMidpoint.Latitude;
            GeoCoordinate circleCenter = new GeoCoordinate(yIntersect, xIntersect);

            double distance;
            if (xIntersect != (1.0 / 0) && xIntersect != (-1.0 / 0) && yIntersect != (1.0 / 0) && yIntersect != (-1.0 / 0))
                distance = circleCenter.DistanceEstimate(from).Value; //  Radius of the turn
            else
                distance = 1000;    //  Just say it's a pretty huge radius

            return distance;/* / (this.MaxSpeed(vehicle, tags).Value) * 3.6; */  // km/h to m/s
        }

        /// <summary>
        /// Gives the midpoint of two geocoordinates.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static GeoCoordinate midPoint(GeoCoordinate from, GeoCoordinate to)
        {
            return new GeoCoordinate(from.Latitude + (from.Latitude - to.Latitude), from.Longitude + (from.Longitude - to.Longitude));

            //  This is some haversine stuff that I don't understand
            /*
            double dLon = from.DistanceEstimate(to).Value;

            //convert to radians
            double fromLat = from.Latitude * (Math.PI/180.0);
            double fromLong = from.Longitude * (Math.PI/180.0);
            double toLat = to.Latitude * (Math.PI/180.0);
            double toLong = to.Longitude * (Math.PI / 180.0);

            double Bx = Math.Cos(toLat) * Math.Cos(dLon);
            double By = Math.Cos(toLat) * Math.Sin(dLon);
            double lat3 = Math.Atan2(Math.Sin(fromLat) + Math.Sin(toLat), Math.Sqrt(((Math.Cos(fromLat) + Bx) * (Math.Cos(fromLat) + Bx)) + (By * By)));
            double lon3 = fromLong + Math.Atan2(By, Math.Cos(fromLat) + Bx);

            GeoCoordinate toRet = new GeoCoordinate(lat3, lon3);
            return toRet;
             * */
        }

    }
}
