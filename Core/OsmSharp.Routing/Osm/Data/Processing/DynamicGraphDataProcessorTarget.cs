// OsmSharp - OpenStreetMap tools & library.
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
using OsmSharp.Osm;
using OsmSharp.Osm.Data.Core.Processor;
using OsmSharp.Osm.Simple;
using OsmSharp.Routing.Graph.DynamicGraph;
using OsmSharp.Routing.Interpreter;
using OsmSharp.Routing.Interpreter.Roads;
using OsmSharp.Tools.Collections.Huge;
using OsmSharp.Tools.Math;
using OsmSharp.Tools.Math.Geo;
using OsmSharp.Routing.Osm.Interpreter.Edge;

namespace OsmSharp.Routing.Osm.Data.Processing
{
    /// <summary>
    ///     Data Processor Target to fill a dynamic graph object.
    /// </summary>
    public abstract class DynamicGraphDataProcessorTarget<TEdgeData> : DataProcessorTarget where TEdgeData : IDynamicGraphEdgeData
    {
        /// <summary>
        ///     The bounding box to limit nodes if any.
        /// </summary>
        private readonly GeoCoordinateBox _box;

        /// <summary>
        ///     Holds the dynamic graph.
        /// </summary>
        private readonly IDynamicGraph<TEdgeData> _dynamicGraph;

        /// <summary>
        ///     Holds the edge comparer.
        /// </summary>
        private readonly IDynamicGraphEdgeComparer<TEdgeData> _edgeComparer;

        /// <summary>
        ///     Holds the id transformations.
        /// </summary>
        private readonly IDictionary<long, uint> _idTransformations;

        /// <summary>
        ///     The interpreter for osm data.
        /// </summary>
        private readonly IRoutingInterpreter _interpreter;

        /// <summary>
        ///     Holds the tags index.
        /// </summary>
        private readonly ITagsIndex _tagsIndex;

        /// <summary>
        ///     Holds a list of nodes used twice or more.
        /// </summary>
        private readonly HashSet<long> _usedTwiceOrMore;

        /// <summary>
        ///     Holds the coordinates.
        /// </summary>
        private HugeDictionary<long, float[]> _coordinates;

        /// <summary>
        ///     True when this target is in pre-index mode.
        /// </summary>
        private bool _preIndexMode;

        /// <summary>
        ///     Holds the index of all relevant nodes.
        /// </summary>
        private HashSet<long> _preIndex;

        /// <summary>
        ///     Creates a new processor target.
        /// </summary>
        /// <param name="dynamicGraph">The graph that will be filled.</param>
        /// <param name="interpreter">The interpreter to generate the edge data.</param>
        /// <param name="edgeComparer"></param>
        protected DynamicGraphDataProcessorTarget(IDynamicGraph<TEdgeData> dynamicGraph, IRoutingInterpreter interpreter, IDynamicGraphEdgeComparer<TEdgeData> edgeComparer)
            : this(dynamicGraph, interpreter, edgeComparer, new OsmTagsIndex(), new Dictionary<long, uint>())
        {
        }

        /// <summary>
        ///     Creates a new processor target.
        /// </summary>
        /// <param name="dynamicGraph">The graph that will be filled.</param>
        /// <param name="interpreter">The interpreter to generate the edge data.</param>
        /// <param name="edgeComparer"></param>
        /// <param name="tagsIndex"></param>
        protected DynamicGraphDataProcessorTarget(IDynamicGraph<TEdgeData> dynamicGraph, IRoutingInterpreter interpreter, IDynamicGraphEdgeComparer<TEdgeData> edgeComparer,
                                               ITagsIndex tagsIndex) : this(dynamicGraph, interpreter, edgeComparer, tagsIndex, new Dictionary<long, uint>())
        {
        }

        /// <summary>
        ///     Creates a new processor target.
        /// </summary>
        /// <param name="dynamicGraph">The graph that will be filled.</param>
        /// <param name="interpreter">The interpreter to generate the edge data.</param>
        /// <param name="edgeComparer"></param>
        /// <param name="tagsIndex"></param>
        /// <param name="idTransformations"></param>
        protected DynamicGraphDataProcessorTarget(IDynamicGraph<TEdgeData> dynamicGraph, IRoutingInterpreter interpreter, IDynamicGraphEdgeComparer<TEdgeData> edgeComparer,
                                               ITagsIndex tagsIndex, IDictionary<long, uint> idTransformations)
            : this(dynamicGraph, interpreter, edgeComparer, tagsIndex, idTransformations, null)
        {
        }

        /// <summary>
        ///     Creates a new processor target.
        /// </summary>
        /// <param name="dynamicGraph">The graph that will be filled.</param>
        /// <param name="interpreter">The interpreter to generate the edge data.</param>
        /// <param name="edgeComparer"></param>
        /// <param name="tagsIndex"></param>
        /// <param name="idTransformations"></param>
        /// <param name="box"></param>
        protected DynamicGraphDataProcessorTarget(IDynamicGraph<TEdgeData> dynamicGraph, IRoutingInterpreter interpreter, IDynamicGraphEdgeComparer<TEdgeData> edgeComparer,
                                               ITagsIndex tagsIndex, IDictionary<long, uint> idTransformations, GeoCoordinateBox box)
        {
            _dynamicGraph = dynamicGraph;
            _interpreter = interpreter;
            _edgeComparer = edgeComparer;
            _box = box;

            _tagsIndex = tagsIndex;
            _idTransformations = idTransformations;
            _preIndexMode = true;
            _preIndex = new HashSet<long>();
            _usedTwiceOrMore = new HashSet<long>();
        }

        /// <summary>
        ///     Returns the tags index.
        /// </summary>
        public ITagsIndex TagsIndex
        {
            get { return _tagsIndex; }
        }

        /// <summary>
        ///     Initializes the processing.
        /// </summary>
        public override void Initialize()
        {
            _coordinates = new HugeDictionary<long, float[]>();
        }

        /// <summary>
        ///     Applies the changes in the changeset.
        /// </summary>
        /// <param name="change"></param>
        public override void ApplyChange(SimpleChangeSet change)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///     Adds the given node.
        /// </summary>
        /// <param name="node"></param>
        public override void AddNode(SimpleNode node)
        {
            if(_preIndexMode)
                return;

            if(!node.Id.HasValue || !node.Latitude.HasValue || !node.Longitude.HasValue)
                return;

            if (_preIndex != null && _preIndex.Contains(node.Id.Value))
            {
                // only save the coordinates for relevant nodes.
                // save the node-coordinates.
                // add the relevant nodes.

                var lat = (float) node.Latitude.Value;
                var lon = (float) node.Longitude.Value;

                if (_box == null || _box.IsInside(new GeoCoordinate(lat, lon)))
                {
                    // the coordinate is acceptable.
                    _coordinates[node.Id.Value] = new[] { lat, lon };
                    if (_coordinates.Count == _preIndex.Count)
                    {
                        _preIndex.Clear();
                        _preIndex = null;
                    }
                }
            }
        }

        /// <summary>
        ///     Adds a given way.
        /// </summary>
        /// <param name="way"></param>
        public override void AddWay(SimpleWay way)
        {
            if (!_interpreter.EdgeInterpreter.IsRoutable(way.Tags))
                return;

            if (_preIndexMode)
            {
                // index only relevant nodes.
                if (way.Nodes != null)
                {
                    foreach (long node in way.Nodes)
                    {
                        if (_preIndex.Contains(node))
                            _usedTwiceOrMore.Add(node);
                        else
                            _preIndex.Add(node); // node is relevant.
                    }
                }
            }
            else
            {
                // add the forward edges.
                //if (!interpreter.IsOneWayReverse())
                if (true) // add backward edges too!
                {
                    // loop over all edges.
                    if (way.Nodes != null && way.Nodes.Count > 1)
                    {
                        // way has at least two nodes.
                        if (CalculateIsTraversable(_interpreter.EdgeInterpreter, _tagsIndex, way.Tags))
                        {
                            // add to the tags index.
                            uint tagsId = _tagsIndex.Add(way.Tags);

                            // the edge is traversable, add the edges.
                            uint? from = AddRoadNode(way.Nodes[0]);
                            uint? previous = null;
                            for (int idx = 1; idx < way.Nodes.Count; idx++)
                            {
                                // the to-node.
                                uint? to = AddRoadNode(way.Nodes[idx]);

                                // add the edge(s).
                                if (from.HasValue && to.HasValue)
                                {
                                    // ERIK! this is where I started changing things. Reverting this function back will 
                                    // revert the changes.
                                    if (_interpreter.EdgeInterpreter is MotorcycleEdgeInterpreter)
                                    {
                                        if (idx == 1)
                                        {
                                            if (!AddRoadEdge(way.Tags, true, from.Value, to.Value, tagsId))
                                                AddRoadEdge(way.Tags, false, to.Value, from.Value, tagsId);
                                            previous = from;
                                        }
                                        else
                                        {
                                            //  to revert, remove this code, as well as the if-else
                                            if (!AddRoadEdge(way.Tags, true, previous.Value, from.Value, to.Value, tagsId))
                                                AddRoadEdge(way.Tags, false, previous.Value, to.Value, from.Value, tagsId);
                                            previous = to;
                                        }
                                    }
                                    else
                                    {
                                        if (!AddRoadEdge(way.Tags, true, from.Value, to.Value, tagsId))
                                            AddRoadEdge(way.Tags, false, to.Value, from.Value, tagsId);
                                    }
                                }

                                from = to; // the to node becomes the from.
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Adds a node that is at least part of one road.
        /// </summary>
        /// <param name="nodeID"></param>
        /// <returns></returns>
        private uint? AddRoadNode(long nodeID)
        {
            uint id;
            // try and get existing node.
            if (!_idTransformations.TryGetValue(nodeID, out id))
            {
                // get coordinates.
                float[] coordinates;
                if (_coordinates.TryGetValue(nodeID, out coordinates))
                {
                    // the coordinate is present.
                    id = _dynamicGraph.AddVertex(coordinates[0], coordinates[1]);
                    _coordinates.Remove(nodeID); // free the memory again!

                    //if (_used_twice_or_more.Contains(node_id))
                    //{
                    _idTransformations[nodeID] = id;
                    //}
                    return id;
                }
                return null;
            }
            return id;
        }

        /// <summary>
        ///     Adds an edge.
        /// </summary>
        /// <param name="forward"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="tags"></param>
        /// <param name="tagsId"></param>
        private bool AddRoadEdge(IDictionary<string, string> tags, bool forward, uint from, uint to, uint? tagsId)
        {
            float latitude, longitude;
            GeoCoordinate fromCoordinate = null;
            if (_dynamicGraph.GetVertex(from, out latitude, out longitude))
            {
                fromCoordinate = new GeoCoordinate(latitude, longitude);
            }

            GeoCoordinate toCoordinate = null;
            if (_dynamicGraph.GetVertex(to, out latitude, out longitude))
            {
                toCoordinate = new GeoCoordinate(latitude, longitude);
            }

            if (fromCoordinate != null && toCoordinate != null)
            {
                // calculate the edge data.
                TEdgeData edgeData = CalculateEdgeData(_interpreter.EdgeInterpreter, _tagsIndex, tags, forward, fromCoordinate, toCoordinate, tagsId);

                _dynamicGraph.AddArc(from, to, edgeData, _edgeComparer);
            }
            return false;
        }

        /// <summary>
        ///     Adds an edge.
        /// </summary>
        /// <param name="forward"></param>
        /// <param name="previous"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="tags"></param>
        /// <param name="tagsId"></param>
        private bool AddRoadEdge(IDictionary<string, string> tags, bool forward, uint previous, uint from, uint to, uint? tagsId)
        {
            float latitude, longitude;
            GeoCoordinate fromCoordinate = null;
            if (_dynamicGraph.GetVertex(from, out latitude, out longitude))
            {
                fromCoordinate = new GeoCoordinate(latitude, longitude);
            }

            GeoCoordinate toCoordinate = null;
            if (_dynamicGraph.GetVertex(to, out latitude, out longitude))
            {
                toCoordinate = new GeoCoordinate(latitude, longitude);
            }

            GeoCoordinate prevCoordinate = null;
            if (_dynamicGraph.GetVertex(previous, out latitude, out longitude))
            {
                prevCoordinate = new GeoCoordinate(latitude, longitude);
            }

            if (fromCoordinate != null && toCoordinate != null)
            {
                // calculate the edge data.
                //TEdgeData edgeData = CalculateEdgeData(_interpreter.EdgeInterpreter, _tagsIndex, tags, forward, prevCoordinate, fromCoordinate, toCoordinate, tagsId);
                TEdgeData edgeData = CalculateEdgeData(_interpreter.EdgeInterpreter, _tagsIndex, tags, forward, fromCoordinate, toCoordinate, tagsId);

                _dynamicGraph.AddArc(from, to, edgeData, _edgeComparer);
            }
            return false;
        }

        /// <summary>
        ///     Calculates the edge data.
        /// </summary>
        /// <returns></returns>
        protected abstract TEdgeData CalculateEdgeData(IEdgeInterpreter edgeInterpreter, ITagsIndex tagsIndex, IDictionary<string, string> tags,
                                                       bool directionForward, GeoCoordinate from, GeoCoordinate to, uint? tagsId);

        /// <summary>
        ///     Returns true if the edge can be traversed.
        /// </summary>
        /// <param name="edgeInterpreter"></param>
        /// <param name="tagsIndex"></param>
        /// <param name="tags"></param>
        /// <returns></returns>
        protected abstract bool CalculateIsTraversable(IEdgeInterpreter edgeInterpreter, ITagsIndex tagsIndex, IDictionary<string, string> tags);

        /// <summary>
        ///     Adds a given relation.
        /// </summary>
        /// <param name="relation"></param>
        public override void AddRelation(SimpleRelation relation)
        {
        }

        /// <summary>
        ///     Closes this target.
        /// </summary>
        public override void Close()
        {
            if (_preIndexMode)
            {
                Source.Reset();
                _preIndexMode = false;
                Pull();
            }
        }
    }
}