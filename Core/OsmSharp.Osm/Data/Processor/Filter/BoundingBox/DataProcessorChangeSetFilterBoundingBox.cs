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

using System.Collections.Generic;
using OsmSharp.Osm.Data.Core.Processor.ChangeSets;
using OsmSharp.Osm.Simple;
using OsmSharp.Tools.Math;
using OsmSharp.Tools.Math.Geo;
using OsmSharp.Tools.Output;
using OsmSharp.Tools.Collections;

namespace OsmSharp.Osm.Data.Processor.Filter.BoundingBox
{
    /// <summary>
    ///     Filter processing changesets and filtering them on bounding box based on a database
    ///     that can be queried.
    /// </summary>
    public class DataProcessorChangeSetFilterBoundingBox : DataProcessorChangeSetFilter
    {
        /// <summary>
        ///     Holds the bounding box.
        /// </summary>
        private readonly GeoCoordinateBox _box;

        /// <summary>
        ///     Holds the data source of the reference data.
        /// </summary>
        private readonly IDataSourceReadOnly _data_source;

        /// <summary>
        ///     Holds the changeset listener.
        /// </summary>
        private readonly IChangeSetFilterListener _listener;

        /// <summary>
        ///     Holds the current changeset.
        /// </summary>
        private SimpleChangeSet _current;

        /// <summary>
        ///     Holds the already tested relations.
        /// </summary>
        private List<long> _tested_relations;

        /// <summary>
        ///     Creates a new changeset filter.
        /// </summary>
        /// <param name="data_source"></param>
        /// <param name="box"></param>
        public DataProcessorChangeSetFilterBoundingBox(IDataSourceReadOnly data_source, GeoCoordinateBox box)
        {
            _box = box;
            _data_source = data_source;
        }

        /// <summary>
        ///     Creates a new changeset filter.
        /// </summary>
        /// <param name="data_source"></param>
        /// <param name="box"></param>
        /// <param name="listener"></param>
        public DataProcessorChangeSetFilterBoundingBox(IDataSourceReadOnly data_source, GeoCoordinateBox box, IChangeSetFilterListener listener)
        {
            _box = box;
            _data_source = data_source;
            _listener = listener;
        }

        /// <summary>
        ///     Initializes this filter.
        /// </summary>
        public override void Initialize()
        {
            Source.Initialize();
        }

        /// <summary>
        ///     Move to the next object.
        /// </summary>
        /// <returns></returns>
        public override bool MoveNext()
        {
            // move to the next change available.
            while (Source.MoveNext())
            {
                // convert the changeset to only data withing this bb!
                KeyValuePair<SimpleChangeSet, GeoCoordinateBox> changes_inside = FilterChanges(Source.Current());
                if (changes_inside.Key.Changes.Count > 0)
                {
                    // some data in this changeset is ok!
                    OutputStreamHost.WriteLine(string.Empty);
                    OutputStreamHost.Write("Changeset accepted:");

                    // set the converted changeset as the current one!
                    _current = changes_inside.Key;

                    // report to the listener.
                    if (_listener != null && changes_inside.Value != null)
                        _listener.ReportChangeDetected(changes_inside.Value);

                    return true;
                }
            }
            _current = null;
            return false;
        }

        /// <summary>
        ///     Filters the geo objects in the changeset and omits the ones not inside the bb.
        /// </summary>
        /// <param name="simpleChangeSet"></param>
        /// <returns></returns>
        private KeyValuePair<SimpleChangeSet, GeoCoordinateBox> FilterChanges(SimpleChangeSet simpleChangeSet)
        {
            var changes_coordinates = new List<GeoCoordinate>();

            var filtered_changeset = new SimpleChangeSet();
            filtered_changeset.Changes = new List<SimpleChange>();

            // keep a list of tested relation to prevent circular references from hanging this process.
            _tested_relations = new List<long>();

            // keep all the objects that could not be checked and check them later.
            var objects_not_checked = new List<KeyValuePair<SimpleOsmGeo, SimpleChange>>();

            // loop over all objects inside this box and if they are nodes they can be checked.
            var nodes_inside = new HashSet<long>(); // keep track of all the nodes inside for quick checks later!
            foreach (SimpleChange change in simpleChangeSet.Changes)
            {
                // keep the changed nodes that are ok for the filter.
                var nodes_changed = new List<SimpleOsmGeo>();

                // loop over all objects in this set and keep the nodes.
                foreach (SimpleOsmGeo geo in change.OsmGeo)
                {
                    if (geo is SimpleNode)
                    {
                        var node = geo as SimpleNode;
                        var coord = new GeoCoordinate(node.Latitude.Value, node.Longitude.Value);

                        if (_box.IsInsideAny(new PointF2D[] { coord }))
                        {
                            nodes_changed.Add(node);
                            nodes_inside.Add(node.Id.Value);
                            changes_coordinates.Add(coord);
                        }
                    }
                    else
                        objects_not_checked.Add(new KeyValuePair<SimpleOsmGeo, SimpleChange>(geo, change));
                }

                // are there changes in the nodes?
                if (nodes_changed.Count > 0)
                {
                    var nodes_change = new SimpleChange();
                    nodes_change.OsmGeo = nodes_changed;
                    nodes_change.Type = change.Type;

                    filtered_changeset.Changes.Add(nodes_change);
                }
            }

            // try all ways.
            var ways_inside = new HashSet<long>(); // keep the ways inside for quick checks later!
            foreach (var change_pair in objects_not_checked)
            {
                SimpleOsmGeo geo = change_pair.Key;
                SimpleChange change = change_pair.Value;

                // keep the changed ways that are ok for the filter.
                var ways_changed = new List<SimpleOsmGeo>();

                // detect if the way is part of the filter!
                if (geo is SimpleWay && (geo as SimpleWay).Nodes != null)
                {
                    // try the cached nodes.
                    foreach (long node_id in (geo as SimpleWay).Nodes)
                    {
                        if (nodes_inside.Contains(node_id))
                        {
                            ways_changed.Add(geo);
                            ways_inside.Add(geo.Id.Value);
                            break;
                        }
                    }

                    // first try to load the complete way.
                    Way way = _data_source.GetWay(geo.Id.Value);
                    if (way != null && way.Nodes != null)
                    {
                        foreach (Node node in way.Nodes)
                        {
                            if (node != null)
                            {
                                // only if the node is found
                                changes_coordinates.Add(node.Coordinate);
                                if (_box.IsInsideAny(new PointF2D[] { node.Coordinate }))
                                {
                                    changes_coordinates.Add(node.Coordinate);
                                    ways_changed.Add(geo);
                                    ways_inside.Add(geo.Id.Value);
                                    break;
                                }
                            }
                        }
                    }
                    if (change.Type == SimpleChangeType.Delete && way != null && (way.Nodes == null || way.Nodes.Count == 0))
                    {
                        // alway delete empty ways!
                        ways_changed.Add(geo);
                        ways_inside.Add(geo.Id.Value);
                    }

                    // second try to load the nodes individually.
                    foreach (long node_id in (geo as SimpleWay).Nodes)
                    {
                        Node node = _data_source.GetNode(node_id);
                        if (node != null)
                        {
                            // only if the node is found
                            if (_box.IsInsideAny(new PointF2D[] { node.Coordinate }))
                            {
                                changes_coordinates.Add(node.Coordinate);
                                ways_changed.Add(geo);
                                ways_inside.Add(geo.Id.Value);
                                break;
                            }
                        }
                    }
                }

                // are there changes in the nodes?
                if (ways_changed.Count > 0)
                {
                    var ways_change = new SimpleChange();
                    ways_change.OsmGeo = ways_changed;
                    ways_change.Type = change.Type;

                    filtered_changeset.Changes.Add(ways_change);
                }
            }

            // try all relations.            
            foreach (var change_pair in objects_not_checked)
            {
                SimpleOsmGeo geo = change_pair.Key;
                SimpleChange change = change_pair.Value;

                // keep the changed ways that are ok for the filter.
                var relations_changed = new List<SimpleOsmGeo>();

                // test all relations.
                if (geo is SimpleRelation)
                {
                    if (!_tested_relations.Contains(geo.Id.Value))
                    {
                        _tested_relations.Add(geo.Id.Value);
                        if (IsInsideBoxRelation((geo as SimpleRelation).Members, nodes_inside, ways_inside))
                            relations_changed.Add(geo);
                    }
                }

                // are there changes in the nodes?
                if (relations_changed.Count > 0)
                {
                    var relations_change = new SimpleChange();
                    relations_change.OsmGeo = relations_changed;
                    relations_change.Type = change.Type;

                    filtered_changeset.Changes.Add(relations_change);
                }
            }

            // create bounding box of the found changes!
            GeoCoordinateBox box = null;
            if (changes_coordinates.Count > 0)
                box = new GeoCoordinateBox(changes_coordinates);
            return new KeyValuePair<SimpleChangeSet, GeoCoordinateBox>(filtered_changeset, box);
        }

        /// <summary>
        ///     Returns true if any part of this changeset exists inside the bounding box.
        /// </summary>
        /// <param name="simpleChangeSet"></param>
        /// <returns></returns>
        private bool IsInsideBox(SimpleChangeSet simpleChangeSet)
        {
            // keep a list of tested relation to prevent circular references from hanging this process.
            _tested_relations = new List<long>();

            // keep all the objects that could not be checked and check them later.
            var objects_not_checked = new List<SimpleOsmGeo>();

            // loop over all objects inside this box and if they are nodes they can be checked.
            foreach (SimpleChange change in simpleChangeSet.Changes)
            {
                foreach (SimpleOsmGeo geo in change.OsmGeo)
                {
                    if (geo is SimpleNode)
                    {
                        var node = geo as SimpleNode;
                        var coord = new GeoCoordinate(node.Latitude.Value, node.Longitude.Value);

                        if (_box.IsInsideAny(new PointF2D[] { coord }))
                            return true;
                    }
                    else
                        objects_not_checked.Add(geo);
                }
            }

            // try all ways.
            foreach (SimpleOsmGeo geo in objects_not_checked)
            {
                if (geo is SimpleWay && (geo as SimpleWay).Nodes != null)
                {
                    Way way = _data_source.GetWay(geo.Id.Value);
                    if (way != null && way.Nodes != null)
                    {
                        foreach (Node node in way.Nodes)
                        {
                            if (node != null)
                            {
                                // only if the node is found
                                if (_box.IsInsideAny(new PointF2D[] { node.Coordinate }))
                                    return true;
                            }
                        }
                    }
                }
            }

            // try all relations.
            foreach (SimpleOsmGeo geo in objects_not_checked)
            {
                if (geo is SimpleRelation)
                {
                    if (!_tested_relations.Contains(geo.Id.Value))
                    {
                        _tested_relations.Add(geo.Id.Value);
                        if (IsInsideBoxRelation((geo as SimpleRelation).Members))
                            return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        ///     Returns true if any member of the relation is inside the filter.
        /// </summary>
        /// <param name="members"></param>
        /// <returns></returns>
        private bool IsInsideBoxRelation(IList<SimpleRelationMember> members)
        {
            return IsInsideBoxRelation(members, new HashSet<long>(), new HashSet<long>());
        }

        /// <summary>
        ///     Returns true if any member of the relation is inside the filter.
        /// </summary>
        /// <param name="members"></param>
        /// <param name="nodes_inside"></param>
        /// <param name="ways_inside"></param>
        /// <returns></returns>
        private bool IsInsideBoxRelation(IList<SimpleRelationMember> members, HashSet<long> nodes_inside, HashSet<long> ways_inside)
        {
            // first do nodes.
            if (members != null)
            {
                foreach (SimpleRelationMember member in members)
                {
                    if (member.MemberType.HasValue)
                    {
                        if (member.MemberType.Value == SimpleRelationMemberType.Node)
                        {
                            // check known nodes.
                            if (nodes_inside.Contains(member.MemberId.Value))
                                return true;

                            // check unknown nodes.
                            Node node = _data_source.GetNode(member.MemberId.Value);

                            if (node != null)
                            {
                                // only if the node is found
                                if (_box.IsInsideAny(new PointF2D[] { node.Coordinate }))
                                {
                                    nodes_inside.Add(node.Id);
                                    return true;
                                }
                            }
                        }
                    }
                }

                // then do ways
                foreach (SimpleRelationMember member in members)
                {
                    if (member.MemberType.HasValue)
                    {
                        if (member.MemberType.Value == SimpleRelationMemberType.Way)
                        {
                            // check known ways.
                            if (ways_inside.Contains(member.MemberId.Value))
                                return true;

                            // check unknown ways.
                            Way way = _data_source.GetWay(member.MemberId.Value);

                            if (way != null)
                            {
                                // only if the node is found
                                foreach (Node node in way.Nodes)
                                {
                                    if (_box.IsInsideAny(new PointF2D[] { node.Coordinate }))
                                    {
                                        ways_inside.Add(way.Id);
                                        nodes_inside.Add(node.Id);
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }

                // then do relations.            
                foreach (SimpleRelationMember member in members)
                {
                    if (member.MemberType.HasValue)
                    {
                        if (member.MemberType.Value == SimpleRelationMemberType.Node)
                        {
                            if (!_tested_relations.Contains(member.MemberId.Value))
                            {
                                _tested_relations.Add(member.MemberId.Value);

                                // get the relation.
                                Relation relation = _data_source.GetRelation(member.MemberId.Value);
                                if (relation != null)
                                {
                                    var simple_members = new List<SimpleRelationMember>();
                                    foreach (RelationMember loaded_member in relation.Members)
                                    {
                                        var simple_member = new SimpleRelationMember();
                                        simple_member.MemberId = loaded_member.Member.Id;
                                        simple_member.MemberRole = loaded_member.Role;
                                        switch (loaded_member.Member.Type)
                                        {
                                            case OsmType.Node:
                                                simple_member.MemberType = SimpleRelationMemberType.Node;
                                                break;
                                            case OsmType.Way:
                                                simple_member.MemberType = SimpleRelationMemberType.Way;
                                                break;
                                            case OsmType.Relation:
                                                simple_member.MemberType = SimpleRelationMemberType.Relation;
                                                break;
                                        }
                                        simple_members.Add(simple_member);
                                    }

                                    if (IsInsideBoxRelation(simple_members, nodes_inside, ways_inside))
                                        return true;
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        ///     Returns the current changeset.
        /// </summary>
        /// <returns></returns>
        public override SimpleChangeSet Current()
        {
            return _current;
        }

        /// <summary>
        ///     Resets this filter.
        /// </summary>
        public override void Reset()
        {
            Source.Reset();
        }

        /// <summary>
        ///     Closes this filter.
        /// </summary>
        public override void Close()
        {
            Source.Close();
        }
    }
}