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
using OsmSharp.Osm.Data.Core.Processor;
using OsmSharp.Osm.Simple;
using OsmSharp.Tools.Math.Geo;
using OsmSharp.Tools.Collections;

namespace OsmSharp.Osm.Data.Processor.Filter.BoundingBox
{
    /// <summary>
    ///     A data processing filter that filters on bounding box.
    /// </summary>
    public class DataProcessorFilterBoundingBox : DataProcessorFilter
    {
        /// <summary>
        ///     The box to filter against.
        /// </summary>
        private readonly GeoCoordinateBox _box;

        /// <summary>
        ///     An index of the actual nodes inside the bounding box.
        /// </summary>
        private readonly LongIndex.LongIndex _nodes_in = new LongIndex.LongIndex();

        /// <summary>
        ///     An index of extra nodes to include.
        /// </summary>
        private readonly LongIndex.LongIndex _nodes_to_include = new LongIndex.LongIndex();

        /// <summary>
        ///     An index of the actual relations inside the bounding box.
        /// </summary>
        private readonly HashSet<long> _relation_in = new HashSet<long>();

        /// <summary>
        ///     Holds the relations considered already.
        /// </summary>
        private readonly HashSet<long> _relations_considered = new HashSet<long>();

        /// <summary>
        ///     An index of extra ways to include.
        /// </summary>
        private readonly HashSet<long> _relations_to_include = new HashSet<long>();

        /// <summary>
        ///     An index of the actual ways inside the bounding box.
        /// </summary>
        private readonly HashSet<long> _ways_in = new HashSet<long>();

        /// <summary>
        ///     An index of extra relations to include.
        /// </summary>
        private readonly HashSet<long> _ways_to_include = new HashSet<long>();

        /// <summary>
        ///     Holds the current type.
        /// </summary>
        private SimpleOsmGeoType _current_type = SimpleOsmGeoType.Node;

        /// <summary>
        ///     Flag for extra mode.
        /// </summary>
        private bool _include_extra_mode;

        /// <summary>
        ///     Creates a new bounding box filter.
        /// </summary>
        /// <param name="box"></param>
        public DataProcessorFilterBoundingBox(GeoCoordinateBox box)
        {
            _box = box;
        }

        /// <summary>
        ///     Returns true if this filter can reset.
        /// </summary>
        public override bool CanReset
        {
            get { return Source.CanReset; }
        }

        /// <summary>
        ///     Initializes this filter.
        /// </summary>
        public override void Initialize()
        {
            Source.Initialize();
        }

        /// <summary>
        ///     Moves to the next object.
        /// </summary>
        /// <returns></returns>
        public override bool MoveNext()
        {
            if (!_include_extra_mode)
            {
                // just go over all nodes and ways.
                if (Source.MoveNext())
                {
                    bool finished = false;
                    bool is_in = false;

                    // move to the next object of the current type.
                    while (Current().Type != _current_type)
                    {
                        if (!Source.MoveNext())
                        {
                            finished = true;
                            break;
                        }
                    }

                    if (!finished)
                    {
                        while (Current().Type == _current_type && !is_in)
                        {
                            SimpleOsmGeo current = Source.Current();
                            is_in = IsInBB(current); // check and keep the extras.
                            if (is_in)
                            {
                                // add to the actual in-boundingbox indexes.
                                switch (current.Type)
                                {
                                    case SimpleOsmGeoType.Node:
                                        _nodes_in.Add(current.Id.Value);
                                        break;
                                    case SimpleOsmGeoType.Way:
                                        _ways_in.Add(current.Id.Value);
                                        break;
                                    case SimpleOsmGeoType.Relation:
                                        _relation_in.Add(current.Id.Value);
                                        break;
                                }
                                break;
                            }

                            // move to the next object of the current type.
                            if (!Source.MoveNext())
                            {
                                finished = true;
                                break;
                            }
                            while (Current().Type != _current_type)
                            {
                                if (!Source.MoveNext())
                                {
                                    finished = true;
                                    break;
                                }
                            }

                            // stop when finished.
                            if (finished)
                                break;
                        }
                    }

                    if (!finished && Current().Type == _current_type)
                    {
                        // nothing was finished and the types match.
                        return true;
                    }
                }

                // switch to the next mode.
                switch (_current_type)
                {
                    case SimpleOsmGeoType.Node:
                        Source.Reset();
                        _current_type = SimpleOsmGeoType.Way;
                        return MoveNext();
                    case SimpleOsmGeoType.Way:
                        Source.Reset();
                        _current_type = SimpleOsmGeoType.Relation;
                        return MoveNext();
                    case SimpleOsmGeoType.Relation:
                        Source.Reset();
                        _include_extra_mode = true;
                        return MoveNext();
                }
                throw new InvalidOperationException("Unkown SimpleOsmGeoType");
            }

            while (Source.MoveNext())
            {
                switch (Source.Current().Type)
                {
                    case SimpleOsmGeoType.Node:
                        if (_nodes_to_include.Contains(Source.Current().Id.Value))
                        {
                            if (!_nodes_in.Contains(Source.Current().Id.Value))
                                return true;
                        }
                        break;

                    case SimpleOsmGeoType.Way:
                        if (_ways_to_include.Contains(Source.Current().Id.Value))
                        {
                            if (!_ways_in.Contains(Source.Current().Id.Value))
                                return true;
                        }
                        break;

                    case SimpleOsmGeoType.Relation:
                        if (_relations_to_include.Contains(Source.Current().Id.Value))
                        {
                            if (!_relation_in.Contains(Source.Current().Id.Value))
                                return true;
                        }
                        break;
                }
            }
            return false;
        }

        /// <summary>
        ///     Returns the current object.
        /// </summary>
        /// <returns></returns>
        public override SimpleOsmGeo Current()
        {
            return Source.Current();
        }

        /// <summary>
        ///     Resets this filter.
        /// </summary>
        public override void Reset()
        {
            _ways_in.Clear();
            _nodes_in.Clear();
            _current_type = SimpleOsmGeoType.Node;
            _include_extra_mode = false;
            Source.Reset();
        }

        /// <summary>
        ///     Returns true if the given object is relevant in the bounding box.
        /// </summary>
        /// <param name="osm_geo"></param>
        /// <returns></returns>
        private bool IsInBB(SimpleOsmGeo osm_geo)
        {
            bool is_in = false;
            switch (osm_geo.Type)
            {
                case SimpleOsmGeoType.Node:
                    is_in = _box.IsInside(new GeoCoordinate((osm_geo as SimpleNode).Latitude.Value, (osm_geo as SimpleNode).Longitude.Value));
                    break;
                case SimpleOsmGeoType.Way:
                    foreach (long node_id in (osm_geo as SimpleWay).Nodes)
                    {
                        if (_nodes_in.Contains(node_id))
                        {
                            is_in = true;
                            break;
                        }
                    }
                    if (is_in)
                    {
                        foreach (long node_id in (osm_geo as SimpleWay).Nodes)
                            _nodes_to_include.Add(node_id);
                    }
                    break;
                case SimpleOsmGeoType.Relation:
                    if (!_relations_considered.Contains(osm_geo.Id.Value))
                    {
                        foreach (SimpleRelationMember member in (osm_geo as SimpleRelation).Members)
                        {
                            switch (member.MemberType.Value)
                            {
                                case SimpleRelationMemberType.Node:
                                    if (_nodes_in.Contains(member.MemberId.Value))
                                    {
                                        is_in = true;
                                        break;
                                    }
                                    break;
                                case SimpleRelationMemberType.Way:
                                    if (_ways_in.Contains(member.MemberId.Value))
                                    {
                                        is_in = true;
                                        break;
                                    }
                                    break;
                                case SimpleRelationMemberType.Relation:
                                    if (_relation_in.Contains(member.MemberId.Value))
                                    {
                                        is_in = true;
                                        break;
                                    }
                                    break;
                            }
                        }

                        if (is_in)
                        {
                            foreach (SimpleRelationMember member in (osm_geo as SimpleRelation).Members)
                            {
                                switch (member.MemberType.Value)
                                {
                                    case SimpleRelationMemberType.Node:
                                        _nodes_to_include.Add(member.MemberId.Value);
                                        break;
                                    case SimpleRelationMemberType.Way:
                                        _ways_to_include.Add(member.MemberId.Value);
                                        break;
                                    case SimpleRelationMemberType.Relation:
                                        _relations_to_include.Add(member.MemberId.Value);
                                        break;
                                }
                            }
                        }
                    }
                    break;
            }
            return is_in;
        }
    }
}