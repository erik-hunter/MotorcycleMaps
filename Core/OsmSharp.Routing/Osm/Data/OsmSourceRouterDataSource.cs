// OsmSharp - OpenStreetMap tools & library.
//
// Copyright (C) 2013 Abelshausen Ben
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
using OsmSharp.Osm;
using OsmSharp.Osm.Data;
using OsmSharp.Osm.Data.Core.Processor.ListSource;
using OsmSharp.Osm.Data.Processor.Filter.Sort;
using OsmSharp.Osm.Filters;
using OsmSharp.Osm.Tiles;
using OsmSharp.Routing.Graph;
using OsmSharp.Routing.Graph.DynamicGraph.PreProcessed;
using OsmSharp.Routing.Graph.Router;
using OsmSharp.Routing.Interpreter;
using OsmSharp.Routing.Osm.Data.Processing;
using OsmSharp.Tools.Math;
using OsmSharp.Tools.Math.Geo;

namespace OsmSharp.Routing.Osm.Data
{
    /// <summary>
    ///     A Dynamic graph with extended possibilities to allow resolving points.
    /// </summary>
    public class OsmSourceRouterDataSource : IBasicRouterDataSource<PreProcessedEdge>
    {
        /// <summary>
        ///     Holds a cache of data.
        /// </summary>
        private readonly DynamicGraphRouterDataSource<PreProcessedEdge> _dataCache;

        /// <summary>
        ///     Holds the edge interpreter.
        /// </summary>
        private readonly IRoutingInterpreter _interpreter;

        /// <summary>
        ///     Holds the data source.
        /// </summary>
        private readonly IDataSourceReadOnly _source;

        /// <summary>
        ///     Holds the tags index.
        /// </summary>
        private readonly ITagsIndex _tagsIndex;

        /// <summary>
        ///     Holds the supported vehicle profile.
        /// </summary>
        private readonly VehicleEnum _vehicle;

        /// <summary>
        ///     Creates a OSM dynamic graph.
        /// </summary>
        /// <param name="tagsIndex">The tags index.</param>
        /// <param name="source">The OSM data source.</param>
        /// <param name="interpreter">The routing interpreter.</param>
        /// <param name="vehicle">The vehicle profile being targetted.</param>
        public OsmSourceRouterDataSource(IRoutingInterpreter interpreter, ITagsIndex tagsIndex, IDataSourceReadOnly source, VehicleEnum vehicle)
        {
            _source = source;
            _vehicle = vehicle;
            _tagsIndex = tagsIndex;
            _interpreter = interpreter;

            _dataCache = new DynamicGraphRouterDataSource<PreProcessedEdge>(tagsIndex);
            _loadedTiles = new HashSet<long>();
            _loadedVertices = new HashSet<long>();
            _zoom = TileDefaultsForRouting.Zoom;
            _idTranformations = new Dictionary<long, uint>();
        }

        /// <summary>
        ///     Returns true if the given vehicle profile is supported.
        /// </summary>
        /// <param name="vehicle">The vehicle profile.</param>
        /// <returns></returns>
        public bool SupportsProfile(VehicleEnum vehicle)
        {
            return vehicle == _vehicle;
        }

        /// <summary>
        ///     Adds one more supported profile.
        /// </summary>
        /// <param name="vehicle"></param>
        public void AddSupportedProfile(VehicleEnum vehicle)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///     Returns all arcs with a given bounding box.
        /// </summary>
        /// <param name="box"></param>
        /// <returns></returns>
        public KeyValuePair<uint, KeyValuePair<uint, PreProcessedEdge>>[] GetArcs(GeoCoordinateBox box)
        {
            // load if needed.
            LoadMissingIfNeeded(box);

            return _dataCache.GetArcs(box);
        }

        /// <summary>
        ///     Returns true if the vertex is found.
        /// </summary>
        /// <param name="vertex"></param>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <returns></returns>
        public bool GetVertex(uint vertex, out float latitude, out float longitude)
        {
            return _dataCache.GetVertex(vertex, out latitude, out longitude);
        }

        /// <summary>
        ///     Not supported.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<uint> GetVertices()
        {
            return _dataCache.GetVertices();
        }

        /// <summary>
        ///     Returns all the arcs for a given vertex.
        /// </summary>
        /// <param name="vertex"></param>
        /// <returns></returns>
        public KeyValuePair<uint, PreProcessedEdge>[] GetArcs(uint vertex)
        {
            float latitude, longitude;
            if (_dataCache.GetVertex(vertex, out latitude, out longitude))
            {
                // load if needed.
                LoadMissingIfNeeded(vertex, latitude, longitude);

                // load the arcs.
                return _dataCache.GetArcs(vertex);
            }
            throw new Exception(string.Format("Vertex with id {0} not found!", vertex));
        }

        /// <summary>
        ///     Returns true if the given vertex has neighbour as a neighbour.
        /// </summary>
        /// <param name="vertex"></param>
        /// <param name="neighbour"></param>
        /// <returns></returns>
        public bool HasNeighbour(uint vertex, uint neighbour)
        {
            return GetArcs(vertex).Any(arc => arc.Key == neighbour);
        }


        /// <summary>
        ///     Returns the tags index.
        /// </summary>
        public ITagsIndex TagsIndex
        {
            get { return _tagsIndex; }
        }

        /// <summary>
        ///     Returns the number of vertices currently in this graph.
        /// </summary>
        public uint VertexCount
        {
            get { return _dataCache.VertexCount; }
        }

        #region Loading Strategy

        /// <summary>
        ///     Holds the id tranformations.
        /// </summary>
        private readonly IDictionary<long, uint> _idTranformations;

        /// <summary>
        ///     Holds an index of all loaded tiles.
        /// </summary>
        private readonly HashSet<long> _loadedTiles;

        /// <summary>
        ///     Holds an index of all vertices that have been validated with loaded tiles.
        /// </summary>
        private readonly HashSet<long> _loadedVertices;

        /// <summary>
        ///     The zoom level to cache at.
        /// </summary>
        private readonly int _zoom;

        /// <summary>
        ///     Load the missing tile this vertex is in if needed.
        /// </summary>
        /// <param name="vertex"></param>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        private void LoadMissingIfNeeded(long vertex, float latitude, float longitude)
        {
            if (_loadedVertices.Contains(vertex))
                return;

            // vertex not validated yet!
            Tile tile = Tile.CreateAroundLocation(new GeoCoordinate(latitude, longitude), _zoom);
            LoadTiles(new List<Tile> { tile });

            // add the vertex (if needed)
            _loadedVertices.Add(vertex);
        }

        /// <summary>
        /// Loads missing tiles for the bounding box if needed.
        /// </summary>
        /// <param name="box"></param>
        public void LoadMissingIfNeeded(GeoCoordinateBox box)
        {
            // calculate the tile range.
            TileRange tileRange = TileRange.CreateAroundBoundingBox(box, _zoom);
            LoadTiles(tileRange.ToList());
        }

        private void LoadTiles(IList<Tile> tiles)
        {
            if (tiles.Count == 0)
                return;

            var tilesNotAlreadyLoaded = tiles.Where(id => !_loadedTiles.Contains(id.DatabaseId())).ToList();
            if (tilesNotAlreadyLoaded.Count == 0)
                return;

            foreach(var tile in tilesNotAlreadyLoaded)
            {
                _loadedTiles.Add(tile.DatabaseId());
            }

            // load data.
            IList<OsmGeo> data = _source.Get(tilesNotAlreadyLoaded, Filter.Any()); // does not work, but it should!

            // original - works but very sloow because it repeats getting the same tiles from database
            //GeoCoordinateBox box = tile.Box.Resize(0.00001);
            //IList<OsmGeo> data = _source.Get(box, Filter.Any());

            // process the data just loaded.
            var targetData = new PreProcessedDataGraphProcessingTarget(_dataCache, _interpreter, _tagsIndex, _vehicle, _idTranformations);
            var dataProcessorSource = new OsmGeoListDataProcessorSource(data);
            var sorter = new DataProcessorFilterSort();
            sorter.RegisterSource(dataProcessorSource);
            targetData.RegisterSource(sorter);
            targetData.Pull();
        }

        #endregion
    }
}