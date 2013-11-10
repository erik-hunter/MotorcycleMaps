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
using OsmSharp.Routing.Graph;
using OsmSharp.Routing.Graph.Path;
using OsmSharp.Routing.Interpreter;
using OsmSharp.Routing.Constraints;
using OsmSharp.Tools.Math;
using OsmSharp.Routing.Graph.DynamicGraph;
using OsmSharp.Tools.Math.Geo;
using OsmSharp.Routing.Router;
using OsmSharp.Tools.Collections.PriorityQueues;
using OsmSharp.Routing.Graph.DynamicGraph.SimpleWeighed;
using OsmSharp.Tools.Collections;

namespace OsmSharp.Routing.Graph.Router.Dykstra
{
    /// <summary>
    /// A class containing a dykstra implementation suitable for a simple graph.
    /// </summary>
    public class DykstraRoutingLive : DykstraRoutingBase<SimpleWeighedEdge>, IBasicRouter<SimpleWeighedEdge>
    {
        /// <summary>
        /// Creates a new dykstra routing object.
        /// </summary>
        /// <param name="tags_index"></param>
        public DykstraRoutingLive(ITagsIndex tags_index)
            : base(tags_index)
        {

        }

        /// <summary>
        /// Calculates the shortest path from the given vertex to the given vertex given the weights in the graph.
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="graph"></param>
        /// <param name="interpreter"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public PathSegment<long> Calculate(IDynamicGraphReadOnly<SimpleWeighedEdge> graph, IRoutingInterpreter interpreter, VehicleEnum vehicle,
            PathSegmentVisitList from, PathSegmentVisitList to, double max)
        {
            return this.CalculateToClosest(graph, interpreter, vehicle, from,
                new PathSegmentVisitList[] { to }, max);
        }

        /// <summary>
        /// Calculates the shortest path from all sources to all targets.
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="interpreter"></param>
        /// <param name="vehicle"></param>
        /// <param name="sources"></param>
        /// <param name="targets"></param>
        /// <param name="max_search"></param>
        /// <returns></returns>
        public PathSegment<long>[][] CalculateManyToMany(IBasicRouterDataSource<SimpleWeighedEdge> graph, IRoutingInterpreter interpreter, VehicleEnum vehicle, 
            PathSegmentVisitList[] sources, PathSegmentVisitList[] targets, double max_search)
        {
            PathSegment<long>[][] results = new PathSegment<long>[sources.Length][];
            for (int source_idx = 0; source_idx < sources.Length; source_idx++)
            {
                results[source_idx] = this.DoCalculation(graph, interpreter, vehicle,
                   sources[source_idx], targets, max_search, false, false);
            }
            return results;
        }

        /// <summary>
        /// Calculates the shortest path from the given vertex to the given vertex given the weights in the graph.
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="graph"></param>
        /// <param name="interpreter"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public double CalculateWeight(IDynamicGraphReadOnly<SimpleWeighedEdge> graph, IRoutingInterpreter interpreter, VehicleEnum vehicle,
            PathSegmentVisitList from, PathSegmentVisitList to, double max)
        {
            PathSegment<long> closest = this.CalculateToClosest(graph, interpreter, vehicle, from,
                new PathSegmentVisitList[] { to }, max);
            if (closest != null)
            {
                return closest.Weight;
            }
            return double.MaxValue;
        }

        /// <summary>
        /// Calculates a shortest path between the source vertex and any of the targets and returns the shortest.
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="interpreter"></param>
        /// <param name="vehicle"></param>
        /// <param name="from"></param>
        /// <param name="targets"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public PathSegment<long> CalculateToClosest(IDynamicGraphReadOnly<SimpleWeighedEdge> graph, IRoutingInterpreter interpreter, 
            VehicleEnum vehicle, PathSegmentVisitList from, PathSegmentVisitList[] targets, double max)
        {
            PathSegment<long>[] result = this.DoCalculation(graph, interpreter, vehicle,
                from, targets, max, true, false);
            if (result != null && result.Length == 1)
            {
                return result[0];
            }
            return null;
        }

        /// <summary>
        /// Calculates all routes from a given source to all given targets.
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="interpreter"></param>
        /// <param name="vehicle"></param>
        /// <param name="source"></param>
        /// <param name="targets"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public double[] CalculateOneToManyWeight(IDynamicGraphReadOnly<SimpleWeighedEdge> graph, IRoutingInterpreter interpreter, VehicleEnum vehicle,
            PathSegmentVisitList source, PathSegmentVisitList[] targets, double max)
        {
            PathSegment<long>[] many = this.DoCalculation(graph, interpreter, vehicle,
                   source, targets, max, false, false);

            double[] weights = new double[many.Length];
            for (int idx = 0; idx < many.Length; idx++)
            {
                if (many[idx] != null)
                {
                    weights[idx] = many[idx].Weight;
                }
                else
                {
                    weights[idx] = double.MaxValue;
                }
            }
            return weights;
        }

        /// <summary>
        /// Calculates all routes from a given sources to all given targets.
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="interpreter"></param>
        /// <param name="vehicle"></param>
        /// <param name="sources"></param>
        /// <param name="targets"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public double[][] CalculateManyToManyWeight(IDynamicGraphReadOnly<SimpleWeighedEdge> graph, IRoutingInterpreter interpreter, VehicleEnum vehicle,
            PathSegmentVisitList[] sources, PathSegmentVisitList[] targets, double max)
        {
            double[][] results = new double[sources.Length][];
            for (int idx = 0; idx < sources.Length; idx++)
            {
                results[idx] = this.CalculateOneToManyWeight(graph, interpreter, vehicle, sources[idx], targets, max);

                // report progress.
                OsmSharp.Tools.Output.OutputStreamHost.ReportProgress(idx, sources.Length, "Router.Core.Graph.Router.Dykstra.DykstraRouting<EdgeData>.CalculateManyToManyWeight",
                    "Calculating weights...");
            }
            return results;
        }

        /// <summary>
        /// Returns true, range calculation is supported.
        /// </summary>
        public bool IsCalculateRangeSupported
        {
            get
            {
                return true;
            }
        }


        /// <summary>
        /// Calculates all points that are at or close to the given weight.
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="interpreter"></param>
        /// <param name="vehicle"></param>
        /// <param name="source"></param>
        /// <param name="weight"></param>
        /// <returns></returns>
        public HashSet<long> CalculateRange(IDynamicGraphReadOnly<SimpleWeighedEdge> graph, IRoutingInterpreter interpreter, VehicleEnum vehicle,
            PathSegmentVisitList source, double weight)
        {
            return this.CalculateRange(graph, interpreter, vehicle, source, weight, true);
        }

        /// <summary>
        /// Calculates all points that are at or close to the given weight.
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="interpreter"></param>
        /// <param name="vehicle"></param>
        /// <param name="source"></param>
        /// <param name="weight"></param>
        /// <param name="forward"></param>
        /// <returns></returns>
        public HashSet<long> CalculateRange(IDynamicGraphReadOnly<SimpleWeighedEdge> graph, IRoutingInterpreter interpreter, VehicleEnum vehicle,
            PathSegmentVisitList source, double weight, bool forward)
        {
            PathSegment<long>[] result = this.DoCalculation(graph, interpreter, vehicle,
                   source, new PathSegmentVisitList[0], weight, false, true, forward);

            HashSet<long> result_vertices = new HashSet<long>();
            for (int idx = 0; idx < result.Length; idx++)
            {
                result_vertices.Add(result[idx].VertexId);
            }
            return result_vertices;
        }

        /// <summary>
        /// Returns true if the search can move beyond the given weight.
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="interpreter"></param>
        /// <param name="vehicle"></param>
        /// <param name="source"></param>
        /// <param name="weight"></param>
        /// <returns></returns>
        public bool CheckConnectivity(IDynamicGraphReadOnly<SimpleWeighedEdge> graph, IRoutingInterpreter interpreter, VehicleEnum vehicle,
            PathSegmentVisitList source, double weight)
        {
            HashSet<long> range = this.CalculateRange(graph, interpreter, vehicle, source, weight, true);

            if (range.Count > 0)
            {
                range = this.CalculateRange(graph, interpreter, vehicle, source, weight, false);
                if (range.Count > 0)
                {
                    return true;
                }
            }
            return false;
        }

        #region Implementation

        /// <summary>
        /// Does forward dykstra calculation(s) with several options.
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="interpreter"></param>
        /// <param name="vehicle"></param>
        /// <param name="source"></param>
        /// <param name="targets"></param>
        /// <param name="weight"></param>
        /// <param name="stop_at_first"></param>
        /// <param name="return_at_weight"></param>
        /// <returns></returns>
        private PathSegment<long>[] DoCalculation(IDynamicGraphReadOnly<SimpleWeighedEdge> graph, IRoutingInterpreter interpreter, VehicleEnum vehicle,
            PathSegmentVisitList source, PathSegmentVisitList[] targets, double weight,
            bool stop_at_first, bool return_at_weight)
        {
            return this.DoCalculation(graph, interpreter, vehicle, source, targets, weight, stop_at_first, return_at_weight, true);
        }

        /// <summary>
        /// Does dykstra calculation(s) with several options.
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="interpreter"></param>
        /// <param name="vehicle"></param>
        /// <param name="source"></param>
        /// <param name="targets"></param>
        /// <param name="weight"></param>
        /// <param name="stop_at_first"></param>
        /// <param name="return_at_weight"></param>
        /// <param name="forward"></param>
        /// <returns></returns>
        private PathSegment<long>[] DoCalculation(IDynamicGraphReadOnly<SimpleWeighedEdge> graph, IRoutingInterpreter interpreter, 
            VehicleEnum vehicle, PathSegmentVisitList source, PathSegmentVisitList[] targets, 
            double weight, bool stop_at_first, bool return_at_weight, bool forward)
        {
            float latitude, longitude;
            float fromLatitude, fromLongitude;
            this.InvokeRoutingStarted();

            //  initialize the result data structures.
            List<PathSegment<long>> segments_at_weight = new List<PathSegment<long>>();
            // the resulting target segments.
            PathSegment<long>[] segments_to_target = new PathSegment<long>[targets.Length];
            long found_targets = 0;

            // initialize the list of arcs that are prohibited.
            VertexPair sourcePair = null;
            if (source.Neighbour1 != source.Neighbour2)
            {
                sourcePair = new VertexPair(source.Neighbour1, source.Neighbour2);
            }

            // intialize dykstra data structures.
            IPriorityQueue<PathSegment<long>> heap = new BinairyHeap<PathSegment<long>>();
            HashSet<long> chosen_vertices = new HashSet<long>();
            Dictionary<long, IList<RoutingLabel>> labels = new Dictionary<long, IList<RoutingLabel>>();
            foreach (long vertex in source.GetVertices())
            {
                labels[vertex] = new List<RoutingLabel>();

                PathSegment<long> path = source.GetPathTo(vertex);
                heap.Push(path, (float)path.Weight);
            }

            // set the from node as the current node and put it in the correct data structures.
            // intialize the source's neighbours.
            PathSegment<long> current = heap.Pop();
            while (current != null &&
                chosen_vertices.Contains(current.VertexId))
            { // keep dequeuing.
                current = heap.Pop();
            }

            // test each target for the source.
            // test each source for any of the targets.
            Dictionary<long, PathSegment<long>> paths_from_source = new Dictionary<long, PathSegment<long>>();
            foreach (long source_vertex in source.GetVertices())
            { // get the path to the vertex.
                PathSegment<long> source_path = source.GetPathTo(source_vertex); // get the source path.
                source_path = source_path.From;
                while (source_path != null)
                { // add the path to the paths from source.
                    paths_from_source[source_path.VertexId] = source_path;
                    source_path = source_path.From;
                }
            }
            // loop over all targets
            for (int idx = 0; idx < targets.Length; idx++)
            { // check for each target if there are paths to the source.
                foreach (long target_vertex in targets[idx].GetVertices())
                {
                    PathSegment<long> target_path = targets[idx].GetPathTo(target_vertex); // get the target path.
                    target_path = target_path.From;
                    while (target_path != null)
                    { // add the path to the paths from source.
                        PathSegment<long> path_from_source;
                        if (paths_from_source.TryGetValue(target_path.VertexId, out path_from_source))
                        { // a path is found.
                            // get the existing path if any.
                            PathSegment<long> existing = segments_to_target[idx];
                            if (existing == null)
                            { // a path did not exist yet!
                                segments_to_target[idx] = target_path.Reverse().ConcatenateAfter(path_from_source);
                                found_targets++;
                            }
                            else if (existing.Weight > target_path.Weight + path_from_source.Weight)
                            { // a new path is found with a lower weight.
                                segments_to_target[idx] = target_path.Reverse().ConcatenateAfter(path_from_source);
                            }
                        }
                        target_path = target_path.From;
                    }
                }
            }
            if (found_targets == targets.Length && targets.Length > 0)
            { // routing is finished!
                return segments_to_target.ToArray();
            }

            if (stop_at_first)
            { // only one entry is needed.
                if (found_targets > 0)
                { // targets found, return the shortest!
                    PathSegment<long> shortest = null;
                    foreach (PathSegment<long> found_target in segments_to_target)
                    {
                        if (shortest == null)
                        {
                            shortest = found_target;
                        }
                        else if (found_target != null &&
                            shortest.Weight > found_target.Weight)
                        {
                            shortest = found_target;
                        }
                    }
                    segments_to_target = new PathSegment<long>[1];
                    segments_to_target[0] = shortest;
                    return segments_to_target;
                }
                else
                { // not targets found yet!
                    segments_to_target = new PathSegment<long>[1];
                }
            }

            // test for identical start/end point.
            for (int idx = 0; idx < targets.Length; idx++)
            {
                PathSegmentVisitList target = targets[idx];
                if (return_at_weight)
                { // add all the reached vertices larger than weight to the results.
                    if (current.Weight > weight)
                    {
                        PathSegment<long> to_path = target.GetPathTo(current.VertexId);
                        to_path.Reverse();
                        to_path = to_path.ConcatenateAfter(current);
                        segments_at_weight.Add(to_path);
                    }
                }
                else if (target.Contains(current.VertexId))
                { // the current is a target!
                    PathSegment<long> to_path = target.GetPathTo(current.VertexId);
                    to_path = to_path.Reverse();
                    to_path = to_path.ConcatenateAfter(current);

                    if (stop_at_first)
                    { // stop at the first occurance.
                        segments_to_target[0] = to_path;
                        return segments_to_target;
                    }
                    else
                    { // normal one-to-many; add to the result.
                        // check if routing is finished.
                        if (segments_to_target[idx] == null)
                        { // make sure only the first route is set.
                            found_targets++;
                            segments_to_target[idx] = to_path;
                            if (found_targets == targets.Length)
                            { // routing is finished!
                                this.InvokeRoutingStopped();

                                return segments_to_target.ToArray();
                            }
                        }
                        else if (segments_to_target[idx].Weight > to_path.Weight)
                        { // check if the second, third or later is shorter.
                            segments_to_target[idx] = to_path;
                        }
                    }
                }
            }

            // start OsmSharp.Routing.
            KeyValuePair<uint, SimpleWeighedEdge>[] arcs = graph.GetArcs(
                Convert.ToUInt32(current.VertexId));
            chosen_vertices.Add(current.VertexId);

            if (this.InvokeRoutingVertexSelectedRequired() &&
                current.VertexId > 0 &&
                current.From != null &&
                current.From.VertexId > 0)
            {
                graph.GetVertex(Convert.ToUInt32(current.VertexId), out latitude, out longitude);
                if (current.From != null)
                {
                    graph.GetVertex(Convert.ToUInt32(current.From.VertexId), out fromLatitude, out fromLongitude);
                    this.InvokeRoutingVertexSelected(current.From.VertexId, fromLatitude, fromLongitude,
                        current.VertexId, latitude, longitude);
                }
                else
                {
                    this.InvokeRoutingVertexSelected(null, 0, 0,
                        current.VertexId, latitude, longitude);
                }
            }

            // loop until target is found and the route is the shortest!
            while (true)
            {
                // get the current labels list (if needed).
                IList<RoutingLabel> current_labels = null;
                if (interpreter.Constraints != null)
                { // there are constraints, get the labels.
                    current_labels = labels[current.VertexId];
                    labels.Remove(current.VertexId);
                }

                // update the visited nodes.
                foreach (KeyValuePair<uint, SimpleWeighedEdge> neighbour in arcs)
                {
                    // check for prohibited edges.
                    if (sourcePair != null &&
                        ((source.Neighbour1 == current.VertexId && source.Neighbour2 == neighbour.Key) ||
                        (source.Neighbour2 == current.VertexId && source.Neighbour1 == neighbour.Key)))
                    { // the edge matches the source pair or the edge the source-vertex was resolved on.
                        continue;
                    }

                    // check the tags against the interpreter.
                    IDictionary<string, string> tags = this.TagsIndex.Get(neighbour.Value.Tags);
                    if (interpreter.EdgeInterpreter.CanBeTraversedBy(tags, vehicle))
                    { // it's ok; the edge can be traversed by the given vehicle.
                        bool? one_way = interpreter.EdgeInterpreter.IsOneWay(tags, vehicle);
                        bool can_be_traversed_one_way = (!one_way.HasValue) || // bidirectional edge. 
                            (forward && (one_way.Value == neighbour.Value.IsForward)) ||
                            (!forward && (one_way.Value != neighbour.Value.IsForward)); // backward edge has backward restruction, forward edge forward restriction.
                        if ((current.From == null || 
                            interpreter.CanBeTraversed(current.From.VertexId, current.VertexId, neighbour.Key)) && // test for turning restrictions.
                            can_be_traversed_one_way &&
                            !chosen_vertices.Contains(neighbour.Key))
                        { // the neigbour is forward and is not settled yet!
                            // check the labels (if needed).
                            bool constraints_ok = true;
                            if (interpreter.Constraints != null)
                            { // check if the label is ok.
                                RoutingLabel neighbour_label = interpreter.Constraints.GetLabelFor(
                                    this.TagsIndex.Get(neighbour.Value.Tags));

                                // only test labels if there is a change.
                                if (current_labels.Count == 0 || !neighbour_label.Equals(current_labels[current_labels.Count - 1]))
                                { // labels are different, test them!
                                    constraints_ok = interpreter.Constraints.ForwardSequenceAllowed(current_labels,
                                        neighbour_label);

                                    if (constraints_ok)
                                    { // update the labels.
                                        List<RoutingLabel> neighbour_labels = new List<RoutingLabel>(current_labels);
                                        neighbour_labels.Add(neighbour_label);

                                        labels[neighbour.Key] = neighbour_labels;
                                    }
                                }
                                else
                                { // set the same label(s).
                                    labels[neighbour.Key] = current_labels;
                                }
                            }

                            if (constraints_ok)
                            { // all constraints are validated or there are none.
                                // calculate neighbours weight.
                                double total_weight = current.Weight + neighbour.Value.Weight;

                                // update the visit list;
                                PathSegment<long> neighbour_route = new PathSegment<long>(neighbour.Key, total_weight, current);
                                heap.Push(neighbour_route, (float)neighbour_route.Weight);
                            }
                        }
                    }
                }

                // while the visit list is not empty.
                current = null;
                if (heap.Count > 0)
                {
                    // choose the next vertex.
                    current = heap.Pop();
                    while (current != null &&
                        chosen_vertices.Contains(current.VertexId))
                    { // keep dequeuing.
                        current = heap.Pop();
                    }
                    if (current != null)
                    {
                        chosen_vertices.Add(current.VertexId);
                    }
                }
                while (current != null && current.Weight > weight)
                {
                    if (return_at_weight)
                    { // add all the reached vertices larger than weight to the results.
                        segments_at_weight.Add(current);
                    }

                    // choose the next vertex.
                    current = heap.Pop();
                    while (current != null &&
                        chosen_vertices.Contains(current.VertexId))
                    { // keep dequeuing.
                        current = heap.Pop();
                    }
                }

                if (current == null)
                { // route is not found, there are no vertices left
                    // or the search whent outside of the max bounds.
                    break;
                }

                if (this.InvokeRoutingVertexSelectedRequired() &&
                    current.VertexId > 0 &&
                    current.From.VertexId > 0)
                {
                    graph.GetVertex(Convert.ToUInt32(current.VertexId), out latitude, out longitude);
                    graph.GetVertex(Convert.ToUInt32(current.From.VertexId), out fromLatitude, out fromLongitude);
                    this.InvokeRoutingVertexSelected(current.From.VertexId, fromLatitude, fromLongitude,
                        current.VertexId, latitude, longitude);
                }

                // check target.
                for (int idx = 0; idx < targets.Length; idx++)
                {
                    PathSegmentVisitList target = targets[idx];
                    if (target.Contains(current.VertexId))
                    { // the current is a target!
                        PathSegment<long> to_path = target.GetPathTo(current.VertexId);
                        to_path = to_path.Reverse();
                        to_path = to_path.ConcatenateAfter(current);

                        if (stop_at_first)
                        { // stop at the first occurance.
                            segments_to_target[0] = to_path;
                            return segments_to_target;
                        }
                        else
                        { // normal one-to-many; add to the result.
                            // check if routing is finished.
                            if (segments_to_target[idx] == null)
                            { // make sure only the first route is set.
                                found_targets++;
                                segments_to_target[idx] = to_path;
                                if (found_targets == targets.Length)
                                { // routing is finished!
                                    this.InvokeRoutingStopped();

                                    return segments_to_target.ToArray();
                                }
                            }
                            else if (segments_to_target[idx].Weight > to_path.Weight)
                            { // check if the second, third or later is shorter.
                                segments_to_target[idx] = to_path;
                            }
                        }
                    }
                }

                // get the neigbours of the current node.
                arcs = graph.GetArcs(Convert.ToUInt32(current.VertexId));
            }

            this.InvokeRoutingStopped();

            // return the result.
            if (!return_at_weight)
            {
                return segments_to_target.ToArray();
            }
            return segments_at_weight.ToArray();
        }

        private class VertexPair
        {
            public VertexPair(long vertex1, long vertex2)
            {
                this.Vertex1 = vertex1;
                this.Vertex2 = vertex2;
            }

            public long Vertex1 { get; private set; }

            public long Vertex2 { get; private set; }
        }

        #endregion
    }
}