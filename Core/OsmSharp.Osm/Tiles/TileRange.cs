using System.Collections;
using System.Collections.Generic;
using OsmSharp.Tools.Math.Geo;

namespace OsmSharp.Osm
{
    /// <summary>
    ///     Represents a range of tiles.
    /// </summary>
    public class TileRange : IEnumerable<Tile>
    {
        /// <summary>
        ///     Creates a new tile range.
        /// </summary>
        /// <param name="xMin"></param>
        /// <param name="yMin"></param>
        /// <param name="xMax"></param>
        /// <param name="yMax"></param>
        /// <param name="zoom"></param>
        public TileRange(int xMin, int yMin, int xMax, int yMax, int zoom)
        {
            // Check if wrong way round
            if (xMin <= xMax)
            {
                XMin = xMin;
                XMax = xMax;
            }
            else
            {
                XMin = xMax;
                XMax = xMin;
            }

            // Check if wrong way round
            if(yMin <= yMax)
            {
                YMin = yMin;
                YMax = yMax;
            }
            else
            {
                YMin = yMax;
                YMax = yMin;
            }

            Zoom = zoom;
        }

        /// <summary>
        ///     The minimum X of this range.
        /// </summary>
        public int XMin { get; private set; }

        /// <summary>
        ///     The minimum Y of this range.
        /// </summary>
        public int YMin { get; private set; }

        /// <summary>
        ///     The maximum X of this range.
        /// </summary>
        public int XMax { get; private set; }

        /// <summary>
        ///     The maximum Y of this range.
        /// </summary>
        public int YMax { get; private set; }

        /// <summary>
        ///     The zoom of this range.
        /// </summary>
        public int Zoom { get; private set; }

        #region Functions

        /// <summary>
        ///     Returns true if the given tile lies at the border of this range.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="zoom"></param>
        /// <returns></returns>
        public bool IsBorderAt(int x, int y, int zoom)
        {
            return ((x == XMin) || (x == XMax) || (y == YMin) || (y == YMin)) && Zoom == zoom;
        }

        /// <summary>
        /// Resizes this bounding box with the given delta.
        /// </summary>
        /// <param name="delta"></param>
        public TileRange Resize(int delta)
        {
            XMin -= delta;
            YMin -= delta;
            
            XMax += delta;
            YMax += delta;

            return this;
        }


        /// <summary>
        ///     Returns true if the given tile lies at the border of this range.
        /// </summary>
        /// <param name="tile"></param>
        /// <returns></returns>
        public bool IsBorderAt(Tile tile)
        {
            return IsBorderAt(tile.X, tile.Y, tile.Zoom);
        }

        #endregion

        #region Conversion Functions

        /// <summary>
        ///     Returns a tile range that encompasses the given bounding box at a given zoom level.
        /// </summary>
        /// <param name="box"></param>
        /// <param name="zoom"></param>
        /// <returns></returns>
        public static TileRange CreateAroundBoundingBox(GeoCoordinateBox box, int zoom)
        {
            var min = Tile.WorldToTilePos(box.MinLon, box.MinLat, zoom);
            var max = Tile.WorldToTilePos(box.MaxLon, box.MaxLat, zoom);

            return new TileRange(min.X, min.Y, max.X, max.Y, zoom);
        }

        #endregion

        /// <summary>
        ///     Returns en enumerator of tiles.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<Tile> GetEnumerator()
        {
            return new TileRangeEnumerator(this);
        }


        /// <summary>
        ///     Returns en enumerator of tiles.
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private class TileRangeEnumerator : IEnumerator<Tile>
        {
            private Tile _current;
            private TileRange _range;

            public TileRangeEnumerator(TileRange range)
            {
                _range = range;
            }

            public Tile Current
            {
                get { return _current; }
            }

            public void Dispose()
            {
                _range = null;
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            public bool MoveNext()
            {
                if (_current == null)
                {
                    _current = new Tile(_range.XMin, _range.YMin, _range.Zoom);
                    return true;
                }

                int x = _current.X;
                int y = _current.Y;

                if (x == _range.XMax)
                {
                    if (y == _range.YMax)
                        return false;
                    y++;
                    x = _range.XMin;
                }
                else
                    x++;
                _current = new Tile(x, y, _current.Zoom);
                return true;
            }

            public void Reset()
            {
                _current = null;
            }
        }
    }
}