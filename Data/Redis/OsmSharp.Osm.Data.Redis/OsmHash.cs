﻿// OsmSharp - OpenStreetMap tools & library.
// Copyright (C) 2012 Abelshausen Ben
// 
// This file is part of OsmSharp.
// 
// OsmSharp is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// OsmSharp is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with OsmSharp. If not, see <http://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OsmSharp.Tools.Math.Geo;

namespace OsmSharp.Osm.Data.Redis
{
    /// <summary>
    /// Converts OSM objects to a hashstring.
    /// </summary>
    public static class OsmHash
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="coordinate"></param>
        /// <returns></returns>
        public static string GetOsmHashAsString(GeoCoordinate coordinate)
        {
            return OsmHash.GetOsmHashAsString(coordinate.Latitude, coordinate.Longitude);
        }

        public static string GetOsmHashAsString(double latitude, double longitude)
        {
            return OsmHash.GetOsmHashAsString(
                OsmHash.lon2x(longitude),
                OsmHash.lat2y(latitude));
        }

        public static string GetOsmHashAsString(uint x, uint y)
        {
            return "oh:" + x + ":" + y;
        }

        #region Tile Calculations

        public static uint xy2tile(uint x, uint y)
        {
            uint tile = 0;
            int i;

            for (i = 15; i >= 0; i--)
            {
                tile = (tile << 1) | ((x >> i) & 1);
                tile = (tile << 1) | ((y >> i) & 1);
            }

            return tile;
        }

        public static uint lon2x(double lon)
        {
            return (uint)Math.Floor(((lon + 180.0) * 65536.0 / 360.0));
        }

        public static uint lat2y(double lat)
        {
            return (uint)Math.Floor(((lat + 90.0) * 65536.0 / 180.0));
        }

        #endregion
    }
}
