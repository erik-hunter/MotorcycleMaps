// OsmSharp - OpenStreetMap tools & library.
//
// Copyright (C) 2013 Abelshausen Ben
//                    Simon Hughes
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
using OsmSharp.Osm;
using OsmSharp.Tools.Math.Geo;
using OsmSharp.Osm.Tiles;

namespace OsmSharp.Osm
{
    /// <summary>
    ///     Represents a tile.
    /// </summary>
    public class Tile
    {
        ///// <summary>
        ///// Flag indicating where the y-tiles start.
        ///// </summary>
        //private readonly bool _y_is_top;

        ///// <summary>
        ///// Flag indicating where the x-tiles start.
        ///// </summary>
        //private readonly bool _x_is_left;

        /// <summary>
        ///     Creates a new tile.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="zoom"></param>
        public Tile(int x, int y, int zoom)
        {
            X = x;
            Y = y;

            Zoom = zoom;

            //_y_is_top = true;
            //_x_is_left = true;
        }

        ///// <summary>
        ///// Creates a new tile.
        ///// </summary>
        ///// <param name="x"></param>
        ///// <param name="y"></param>
        ///// <param name="zoom"></param>
        ///// <param name="y_is_top"></param>
        ///// <param name="x_is_left"></param>
        //private void Tile(int x, int y, int zoom, bool y_is_top, bool x_is_left)
        //{
        //    this.X = x;
        //    this.Y = y;

        //    this.Zoom = zoom;

        //    //_y_is_top = true;
        //    //_x_is_left = true;
        //}

        /// <summary>
        ///     The X position of the tile.
        /// </summary>
        public int X { get; private set; }

        /// <summary>
        ///     The Y position of the tile.
        /// </summary>
        public int Y { get; private set; }

        /// <summary>
        ///     The zoom level for this tile.
        /// </summary>
        public int Zoom { get; set; }

        /// <summary>
        ///     Returns the top left corner.
        /// </summary>
        public GeoCoordinate TopLeft
        {
            get
            {
                double powOfTwoToZoom = Math.Pow(2.0, Zoom);
                double n = Math.PI - ((TileDefaultsForRouting.TwoPi * Y) / powOfTwoToZoom);

                double longitude = ((X / powOfTwoToZoom * 360.0) - 180.0);
                double latitude = (180.0 / Math.PI * Math.Atan(Math.Sinh(n)));

                return new GeoCoordinate(latitude, longitude);
            }
        }

        /// <summary>
        ///     Returns the bottom right corner.
        /// </summary>
        public GeoCoordinate BottomRight
        {
            get
            {
                double powOfTwoToZoom = Math.Pow(2.0, Zoom);
                double n = Math.PI - ((TileDefaultsForRouting.TwoPi * (Y + 1)) / powOfTwoToZoom);

                double longitude = (((X + 1) / powOfTwoToZoom * 360.0) - 180.0);
                double latitude = (180.0 / Math.PI * Math.Atan(Math.Sinh(n)));

                return new GeoCoordinate(latitude, longitude);
            }
        }

        /// <summary>
        ///     Returns the bounding box for this tile.
        /// </summary>
        public GeoCoordinateBox Box
        {
            get
            {
                // calculate the tiles bounding box and set its properties.
                return new GeoCoordinateBox(TopLeft, BottomRight);
            }
        }

        #region Conversion Functions

        /// <summary>
        /// Returns the tile at the given location at the given zoom.
        /// </summary>
        /// <param name="location"></param>
        /// <param name="zoom"></param>
        /// <returns></returns>
        public static Tile CreateAroundLocation(GeoCoordinate location, int zoom)
        {
            return WorldToTilePos(location.Longitude, location.Latitude, zoom);
        }

        /// <summary>
        /// Returns the tile at the given location at the given zoom.
        /// </summary>
        /// <param name="lon"></param>
        /// <param name="lat"></param>
        /// <param name="zoom"></param>
        /// <returns></returns>
        public static Tile WorldToTilePos(double lon, double lat, int zoom)
        {
            var n = (int)Math.Floor(Math.Pow(2.0, zoom));
            double rad = lat * Math.PI / 180.0;
            var x = (int)((lon + 180.0) / 360.0 * n);
            var y = (int)((1.0 - Math.Log(Math.Tan(rad) + 1.0 / Math.Cos(rad)) / Math.PI) / 2.0 * n);

            return new Tile(x, y, zoom);
        }

        /// <summary>
        /// Returns the database tile ID number given the tile
        /// </summary>
        /// <returns></returns>
        public long DatabaseId()
        {
            long id = 0;
            var x = (uint) X;
            var y = (uint) Y;
            for(short i = 15; i >= 0; i--)
            {
                id = (id << 1) | ((x >> i) & 1);
                id = (id << 1) | ((y >> i) & 1);
            }

            return id;
        }

        /// <summary>
        /// Inverts the X-coordinate.
        /// </summary>
        /// <returns></returns>
        public Tile InvertX()
        {
            double powOfTwoToZoom = Math.Pow(2.0, Zoom);
            var n = (int)Math.Floor(powOfTwoToZoom);

            return new Tile(n - X - 1, Y, Zoom);
        }

        /// <summary>
        /// Inverts the Y-coordinate.
        /// </summary>
        /// <returns></returns>
        public Tile InvertY()
        {
            double powOfTwoToZoom = Math.Pow(2.0, Zoom);
            var n = (int)Math.Floor(powOfTwoToZoom);

            return new Tile(X, n - Y - 1, Zoom);
        }

        #endregion

        /// <summary>
        /// Returns a hashcode for this tile position.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode(); // ^
            //_x_is_left.GetHashCode() ^
            //_y_is_top.GetHashCode();
        }

        /// <summary>
        ///     Returns true if the given object represents the same tile.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj is Tile)
                return (obj as Tile).X == X && (obj as Tile).Y == Y;
            return false;
        }

        /// <summary>
        ///     Returns a description for this tile.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Format("{0},{1} {2}z Id: {3}", X, Y, Zoom, DatabaseId());
        }
    }
}