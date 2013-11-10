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
using System.Linq;
using OsmSharp.Osm;
using OsmSharp.Osm.Data.Core.Processor;
using OsmSharp.Osm.Data.Processor.Filter.Sort;
using OsmSharp.Routing.Graph;
using OsmSharp.Routing.Graph.DynamicGraph;
using OsmSharp.Routing.Graph.DynamicGraph.PreProcessed;
using OsmSharp.Routing.Graph.DynamicGraph.SimpleWeighed;
using OsmSharp.Routing.Graph.Path;
using OsmSharp.Routing.Graph.Router;
using OsmSharp.Routing.Graph.Router.Dykstra;
using OsmSharp.Routing.Interpreter;
using OsmSharp.Routing.Metrics.Time;
using OsmSharp.Routing.Osm.Data.Processing;
using OsmSharp.Routing.Osm.Interpreter;
using OsmSharp.Routing.Route;
using OsmSharp.Routing.Router;
using OsmSharp.Tools.Math.Geo;
using OsmSharp.Tools.Output;

namespace OsmSharp.Routing
{
    /// <summary>
    ///     A class that implements common functionality for any routing algorithm.
    /// </summary>
    public class Router<TEdgeData> : IRouter<RouterPoint> where TEdgeData : IDynamicGraphEdgeData
    {
        /// <summary>
        ///     The default search delta.
        /// </summary>
        private const float DefaultSearchDelta = .01f;

        /// <summary>
        ///     Holds the graph object containing the routable network.
        /// </summary>
        private readonly IBasicRouterDataSource<TEdgeData> _dataGraph;

        /// <summary>
        ///     Interpreter for the routing network.
        /// </summary>
        private readonly IRoutingInterpreter _interpreter;

        /// <summary>
        ///     Holds the basic router that works on the dynamic graph.
        /// </summary>
        private readonly IBasicRouter<TEdgeData> _router;

        /// <summary>
        ///     Creates a new router.
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="interpreter"></param>
        /// <param name="router"></param>
        public Router(IBasicRouterDataSource<TEdgeData> graph, IRoutingInterpreter interpreter, IBasicRouter<TEdgeData> router)
        {
            _dataGraph = graph;
            _interpreter = interpreter;
            _router = router;

            _resolved_graph = new RouterResolvedGraph();
            _routerPoints = new Dictionary<GeoCoordinate, RouterPoint>();
        }

        /// <summary>
        ///     Returns true if the given vehicle is supported.
        /// </summary>
        /// <param name="vehicle"></param>
        /// <returns></returns>
        public bool SupportsVehicle(VehicleEnum vehicle)
        {
            return _dataGraph.SupportsProfile(vehicle);
        }

        /// <summary>
        ///     Calculates a route from source to target.
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public OsmSharpRoute Calculate(VehicleEnum vehicle, RouterPoint source, RouterPoint target)
        {
            return Calculate(vehicle, source, target, float.MaxValue);
        }

        /// <summary>
        ///     Calculates a route from source to target but does not search more than max around source or target location.
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public OsmSharpRoute Calculate(VehicleEnum vehicle, RouterPoint source, RouterPoint target, float max)
        {
            // check routing profiles.
            if (!SupportsVehicle(vehicle))
                throw new ArgumentOutOfRangeException("vehicle", string.Format("Routing profile {0} not supported by this router!", vehicle.ToString()));

            // calculate the route.
            PathSegment<long> route = _router.Calculate(_dataGraph, _interpreter, vehicle, RouteResolvedGraph(source), RouteResolvedGraph(target), max);

            // construct the route.
            OsmSharpRoute completeRoute = ConstructRoute(vehicle, route, source, target);
#if DEBUG
            if (route != null && completeRoute != null)
            {
                var tags = new List<RouteTags>();
                if (completeRoute.Tags != null)
                    tags.AddRange(completeRoute.Tags);
                tags.Add(new RouteTags { Key = "debug_route", Value = route.ToString() });
                completeRoute.Tags = tags.ToArray();
            }
#endif
            return completeRoute;
        }

        /// <summary>
        ///     Calculates a route from source to the closest target point.
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="source"></param>
        /// <param name="targets"></param>
        /// <returns></returns>
        public OsmSharpRoute CalculateToClosest(VehicleEnum vehicle, RouterPoint source, RouterPoint[] targets)
        {
            return CalculateToClosest(vehicle, source, targets, float.MaxValue);
        }

        /// <summary>
        ///     Calculates a route from source to the closest target point but does not search more than max around source location.
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="source"></param>
        /// <param name="targets"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public OsmSharpRoute CalculateToClosest(VehicleEnum vehicle, RouterPoint source, RouterPoint[] targets, float max)
        {
            // check routing profiles.
            if (!SupportsVehicle(vehicle))
                throw new ArgumentOutOfRangeException("vehicle", string.Format("Routing profile {0} not supported by this router!", vehicle.ToString()));

            // calculate the route.
            PathSegment<long> route = _router.CalculateToClosest(_dataGraph, _interpreter, vehicle, RouteResolvedGraph(source), RouteResolvedGraph(targets), max);

            // find the target.
            RouterPoint target = targets.First(x => x.Id == route.VertexId);

            // convert to an OsmSharpRoute.
            return ConstructRoute(vehicle, route, source, target);
        }

        /// <summary>
        ///     Calculates all the routes between the source and all given targets.
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="source"></param>
        /// <param name="targets"></param>
        /// <returns></returns>
        public OsmSharpRoute[] CalculateOneToMany(VehicleEnum vehicle, RouterPoint source, RouterPoint[] targets)
        {
            return CalculateManyToMany(vehicle, new[] { source }, targets)[0];
        }

        /// <summary>
        ///     Calculates all the routes between all the sources and all the targets.
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="sources"></param>
        /// <param name="targets"></param>
        /// <returns></returns>
        public OsmSharpRoute[][] CalculateManyToMany(VehicleEnum vehicle, RouterPoint[] sources, RouterPoint[] targets)
        {
            // check routing profiles.
            if (!SupportsVehicle(vehicle))
                throw new ArgumentOutOfRangeException("vehicle", string.Format("Routing profile {0} not supported by this router!", vehicle.ToString()));

            PathSegment<long>[][] routes = _router.CalculateManyToMany(_dataGraph, _interpreter, vehicle, RouteResolvedGraph(sources), RouteResolvedGraph(targets),
                                                                       double.MaxValue);

            var constructed_routes = new OsmSharpRoute[sources.Length][];
            for (int x = 0; x < sources.Length; x++)
            {
                constructed_routes[x] = new OsmSharpRoute[targets.Length];
                for (int y = 0; y < targets.Length; y++)
                    constructed_routes[x][y] = ConstructRoute(vehicle, routes[x][y], sources[x], targets[y]);
            }

            return constructed_routes;
        }


        /// <summary>
        ///     Calculates the weight from source to target.
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public double CalculateWeight(VehicleEnum vehicle, RouterPoint source, RouterPoint target)
        {
            // check routing profiles.
            if (!SupportsVehicle(vehicle))
                throw new ArgumentOutOfRangeException("vehicle", string.Format("Routing profile {0} not supported by this router!", vehicle.ToString()));

            // calculate the route.
            return _router.CalculateWeight(_dataGraph, _interpreter, vehicle, RouteResolvedGraph(source), RouteResolvedGraph(target), float.MaxValue);
        }

        /// <summary>
        ///     Calculates all the weights from source to all the targets.
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="source"></param>
        /// <param name="targets"></param>
        /// <returns></returns>
        public double[] CalculateOneToManyWeight(VehicleEnum vehicle, RouterPoint source, RouterPoint[] targets)
        {
            // check routing profiles.
            if (!SupportsVehicle(vehicle))
                throw new ArgumentOutOfRangeException("vehicle", string.Format("Routing profile {0} not supported by this router!", vehicle.ToString()));

            return _router.CalculateOneToManyWeight(_dataGraph, _interpreter, vehicle, RouteResolvedGraph(source), RouteResolvedGraph(targets), double.MaxValue);
        }

        /// <summary>
        ///     Calculates all the weights between all the sources and all the targets.
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="sources"></param>
        /// <param name="targets"></param>
        /// <returns></returns>
        public double[][] CalculateManyToManyWeight(VehicleEnum vehicle, RouterPoint[] sources, RouterPoint[] targets)
        {
            // check routing profiles.
            if (!SupportsVehicle(vehicle))
                throw new ArgumentOutOfRangeException("vehicle", string.Format("Routing profile {0} not supported by this router!", vehicle.ToString()));

            return _router.CalculateManyToManyWeight(_dataGraph, _interpreter, vehicle, RouteResolvedGraph(sources), RouteResolvedGraph(targets), double.MaxValue);
        }

        /// <summary>
        ///     Returns true if range calculation is supported.
        /// </summary>
        public bool IsCalculateRangeSupported
        {
            get { return _router.IsCalculateRangeSupported; }
        }

        /// <summary>
        ///     Calculates the locations around the origin that have a given weight.
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="orgin"></param>
        /// <param name="weight"></param>
        /// <returns></returns>
        public HashSet<GeoCoordinate> CalculateRange(VehicleEnum vehicle, RouterPoint orgin, float weight)
        {
            // check routing profiles.
            if (!SupportsVehicle(vehicle))
                throw new ArgumentOutOfRangeException("vehicle", string.Format("Routing profile {0} not supported by this router!", vehicle.ToString()));

            HashSet<long> objects_at_weight = _router.CalculateRange(_dataGraph, _interpreter, vehicle, RouteResolvedGraph(orgin), weight);

            var locations = new HashSet<GeoCoordinate>();
            foreach (long vertex in objects_at_weight)
            {
                GeoCoordinate coordinate = GetCoordinate(vertex);
                locations.Add(coordinate);
            }
            return locations;
        }

        /// <summary>
        ///     Returns true if the given source is at least connected to vertices with at least a given weight.
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="point"></param>
        /// <param name="weight"></param>
        /// <returns></returns>
        public bool CheckConnectivity(VehicleEnum vehicle, RouterPoint point, float weight)
        {
            // check routing profiles.
            if (!SupportsVehicle(vehicle))
                throw new ArgumentOutOfRangeException("vehicle", string.Format("Routing profile {0} not supported by this router!", vehicle.ToString()));

            return _router.CheckConnectivity(_dataGraph, _interpreter, vehicle, RouteResolvedGraph(point), weight);
        }

        /// <summary>
        ///     Returns an array of connectivity check results.
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="point"></param>
        /// <param name="weight"></param>
        /// <returns></returns>
        public bool[] CheckConnectivity(VehicleEnum vehicle, RouterPoint[] point, float weight)
        {
            // check routing profiles.
            if (!SupportsVehicle(vehicle))
                throw new ArgumentOutOfRangeException("vehicle", string.Format("Routing profile {0} not supported by this router!", vehicle.ToString()));

            var connectivity_array = new bool[point.Length];
            for (int idx = 0; idx < point.Length; idx++)
            {
                connectivity_array[idx] = CheckConnectivity(vehicle, point[idx], weight);

                // report progress.
                OutputStreamHost.ReportProgress(idx, point.Length, "Router.Core.CheckConnectivity", "Checking connectivity...");
            }
            return connectivity_array;
        }

        #region OsmSharpRoute Building

        /// <summary>
        ///     Converts a linked route to an OsmSharpRoute.
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="route"></param>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private OsmSharpRoute ConstructRoute(VehicleEnum vehicle, PathSegment<long> route, RouterPoint source, RouterPoint target)
        {
            if (route != null)
            {
                long[] vertices = route.ToArray();

                // construct the actual graph route.
                return Generate(vehicle, source, target, vertices);
            }
            return null; // calculation failed!
        }

        /// <summary>
        ///     Generates an osm sharp route from a graph route.
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="from_resolved"></param>
        /// <param name="to_resolved"></param>
        /// <param name="vertices"></param>
        /// <returns></returns>
        internal OsmSharpRoute Generate(VehicleEnum vehicle, RouterPoint from_resolved, RouterPoint to_resolved, long[] vertices)
        {
            // create the route.
            OsmSharpRoute route = null;

            if (vertices != null)
            {
                route = new OsmSharpRoute();

                // set the vehicle.
                route.Vehicle = vehicle;

                RoutePointEntry[] entries;
                if (vertices.Length > 0)
                    entries = GenerateEntries(vertices);
                else
                    entries = new RoutePointEntry[0];

                // create the from routing point.
                var from = new RoutePoint();
                //from.Name = from_point.Name;
                from.Latitude = (float) from_resolved.Location.Latitude;
                from.Longitude = (float) from_resolved.Location.Longitude;
                if (entries.Length > 0)
                {
                    entries[0].Points = new RoutePoint[1];
                    entries[0].Points[0] = from;
                    entries[0].Points[0].Tags = from_resolved.Tags.ConvertFrom();
                }

                // create the to routing point.
                var to = new RoutePoint();
                //to.Name = to_point.Name;
                to.Latitude = (float) to_resolved.Location.Latitude;
                to.Longitude = (float) to_resolved.Location.Longitude;
                if (entries.Length > 0)
                {
                    //to.Tags = ConvertTo(to_point.Tags);
                    entries[entries.Length - 1].Points = new RoutePoint[1];
                    entries[entries.Length - 1].Points[0] = to;
                    entries[entries.Length - 1].Points[0].Tags = to_resolved.Tags.ConvertFrom();
                }

                // set the routing points.
                route.Entries = entries;

                // calculate metrics.
                var calculator = new TimeCalculator(_interpreter);
                Dictionary<string, double> metrics = calculator.Calculate(route);
                route.TotalDistance = metrics[TimeCalculator.DISTANCE_KEY];
                route.TotalTime = metrics[TimeCalculator.TIME_KEY];
            }

            return route;
        }

        /// <summary>
        ///     Generates a list of entries.
        /// </summary>
        /// <param name="vertices"></param>
        /// <returns></returns>
        private RoutePointEntry[] GenerateEntries(long[] vertices)
        {
            // create an entries list.
            var entries = new List<RoutePointEntry>();

            // create the first entry.
            GeoCoordinate coordinate = GetCoordinate(vertices[0]);
            var first = new RoutePointEntry();
            first.Latitude = (float) coordinate.Latitude;
            first.Longitude = (float) coordinate.Longitude;
            first.Type = RoutePointEntryType.Start;
            first.WayFromName = null;
            first.WayFromNames = null;

            entries.Add(first);

            // create all the other entries except the last one.
            long nodePrevious = vertices[0];
            for (int idx = 1; idx < vertices.Length - 1; idx++)
            {
                // get all the data needed to calculate the next route entry.
                long nodeCurrent = vertices[idx];
                //long nodeNext = vertices[idx + 1];
                IDynamicGraphEdgeData edge = GetEdgeData(nodePrevious, nodeCurrent);

                // FIRST CALCULATE ALL THE ENTRY METRICS!

                // STEP1: Get the names.
                IDictionary<string, string> currentTags = _dataGraph.TagsIndex.Get(edge.Tags);
                string name = _interpreter.EdgeInterpreter.GetName(currentTags);
                Dictionary<string, string> names = _interpreter.EdgeInterpreter.GetNamesInAllLanguages(currentTags);

                // STEP2: Get the side streets
                IList<RoutePointEntrySideStreet> sideStreets = new List<RoutePointEntrySideStreet>();
                Dictionary<long, IDynamicGraphEdgeData> neighbours = GetNeighboursUndirectedWithEdges(nodeCurrent);
                if (neighbours.Count > 2)
                {
                    // construct neighbours list.
                    foreach (var neighbour in neighbours)
                    {
                        if (neighbour.Key != nodePrevious && neighbour.Key != vertices[idx + 1])
                        {
                            var sideStreet = new RoutePointEntrySideStreet();

                            GeoCoordinate neighbourCoordinate = GetCoordinate(neighbour.Key);
                            IDictionary<string, string> tags = _dataGraph.TagsIndex.Get(neighbour.Value.Tags);

                            sideStreet.Latitude = (float) neighbourCoordinate.Latitude;
                            sideStreet.Longitude = (float) neighbourCoordinate.Longitude;
                            sideStreet.Tags = tags.ConvertFrom();
                            sideStreet.WayName = _interpreter.EdgeInterpreter.GetName(tags);
                            sideStreet.WayNames = _interpreter.EdgeInterpreter.GetNamesInAllLanguages(tags).ConvertFrom();

                            sideStreets.Add(sideStreet);
                        }
                    }
                }

                // create the route entry.
                GeoCoordinate nextCoordinate = GetCoordinate(nodeCurrent);

                var routeEntry = new RoutePointEntry();
                routeEntry.Latitude = (float) nextCoordinate.Latitude;
                routeEntry.Longitude = (float) nextCoordinate.Longitude;
                routeEntry.SideStreets = sideStreets.ToArray();
                routeEntry.Tags = currentTags.ConvertFrom();
                routeEntry.Type = RoutePointEntryType.Along;
                routeEntry.WayFromName = name;
                routeEntry.WayFromNames = names.ConvertFrom();
                entries.Add(routeEntry);

                // set the previous node.
                nodePrevious = nodeCurrent;
            }

            // create the last entry.
            if (vertices.Length > 1)
            {
                int last_idx = vertices.Length - 1;
                IDynamicGraphEdgeData edge = GetEdgeData(vertices[last_idx - 1], vertices[last_idx]);
                IDictionary<string, string> tags = _dataGraph.TagsIndex.Get(edge.Tags);
                coordinate = GetCoordinate(vertices[last_idx]);
                var last = new RoutePointEntry();
                last.Latitude = (float) coordinate.Latitude;
                last.Longitude = (float) coordinate.Longitude;
                last.Type = RoutePointEntryType.Stop;
                last.Tags = tags.ConvertFrom();
                last.WayFromName = _interpreter.EdgeInterpreter.GetName(tags);
                last.WayFromNames = _interpreter.EdgeInterpreter.GetNamesInAllLanguages(tags).ConvertFrom();

                entries.Add(last);
            }

            // return the result.
            return entries.ToArray();
        }

        /// <summary>
        ///     Returns all the neighbours of the given vertex.
        /// </summary>
        /// <param name="vertex1"></param>
        /// <returns></returns>
        private Dictionary<long, IDynamicGraphEdgeData> GetNeighboursUndirectedWithEdges(long vertex1)
        {
            var neighbours = new Dictionary<long, IDynamicGraphEdgeData>();
            if (vertex1 > 0)
            {
                // the vertex is sure to be on the regular graph.
                KeyValuePair<uint, TEdgeData>[] arcs = _dataGraph.GetArcs(Convert.ToUInt32(vertex1));
                foreach (var arc in arcs)
                    neighbours[arc.Key] = arc.Value;

                // ... but it may have neighbours in the resolved graph.
                // replace maybe some neighbours by resolved neighbours.
                float longitude, latitude;
                if (_resolved_graph.GetVertex(vertex1, out latitude, out longitude))
                {
                    // the vertex is in the resolved graph, at least one if it's neighbours is incorrect.
                    foreach (long neigbourId in new List<long>(neighbours.Keys))
                    {
                        // find out if there exists a route between the given vertex and it's neighbour in the resolved graph.
                        if (_resolved_graph.GetVertex(neigbourId, out latitude, out longitude))
                        {
                            // this neighbour is incorrect.
                            long newNeighbour;
                            IDynamicGraphEdgeData newNeighbourData;
                            if (FindResolvedNeighbour(vertex1, neigbourId, out newNeighbour, out newNeighbourData))
                            {
                                neighbours.Remove(neigbourId);
                                neighbours.Add(newNeighbour, newNeighbourData);
                            }
                        }
                    }
                }
            }
            else
            {
                // this vertex is sure to be in the resolved graph and has all of it's neighbours there.
                KeyValuePair<long, RouterResolvedGraph.RouterResolvedGraphEdge>[] arcs = _resolved_graph.GetArcs(vertex1);
                foreach (var arc in arcs)
                    neighbours[arc.Key] = arc.Value;
            }
            return neighbours;
        }

        /// <summary>
        ///     Tries to find the resolved neighbour of the given vertex.
        /// </summary>
        /// <param name="vertex"></param>
        /// <param name="neighbour"></param>
        /// <param name="resolvedNeighbour"></param>
        /// <param name="resolvedNeighbourData"></param>
        /// <returns></returns>
        private bool FindResolvedNeighbour(long vertex, long neighbour, out long resolvedNeighbour, out IDynamicGraphEdgeData resolvedNeighbourData)
        {
            // get all resolved neighbours.
            KeyValuePair<long, RouterResolvedGraph.RouterResolvedGraphEdge>[] arcs = _resolved_graph.GetArcs(vertex);
            var newResolvedNeighbours = new List<KeyValuePair<long, long>>();
            foreach (var arc in arcs)
            {
                KeyValuePair<long, RouterResolvedGraph.RouterResolvedGraphEdge>[] nextNeighbours = _resolved_graph.GetArcs(arc.Key);
                if (nextNeighbours.Length == 2)
                {
                    // has to be exactly 2; the original neighbour and the next one.
                    if (nextNeighbours[0].Key == vertex)
                    {
                        // the second one is the next neighbour.
                        newResolvedNeighbours.Add(new KeyValuePair<long, long>(arc.Key, nextNeighbours[1].Key));
                    }
                    else
                    {
                        // first one is the next neighbour.
                        newResolvedNeighbours.Add(new KeyValuePair<long, long>(arc.Key, nextNeighbours[0].Key));
                    }
                }
            }

            // loop over all next neighbours and try to find the neighbour.
            foreach (var newResolvedNeighbour in newResolvedNeighbours)
            {
                long previousNeighbour = newResolvedNeighbour.Key;
                long currentNeighbour = newResolvedNeighbour.Value;
                while (currentNeighbour < 0)
                {
                    // keep looping when finding resolved nodes.
                    KeyValuePair<long, RouterResolvedGraph.RouterResolvedGraphEdge>[] nextNeighbours = _resolved_graph.GetArcs(currentNeighbour);
                    if (nextNeighbours.Length == 2)
                    {
                        // has to be exactly 2; the original neighbour and the next one.
                        if (nextNeighbours[0].Key == previousNeighbour)
                        {
                            // the second one is the next neighbour.
                            previousNeighbour = currentNeighbour;
                            currentNeighbour = nextNeighbours[1].Key;
                        }
                        else
                        {
                            // first one is the next neighbour.
                            previousNeighbour = currentNeighbour;
                            currentNeighbour = nextNeighbours[0].Key;
                        }
                    }
                    else
                    {
                        // this cannot be it.
                        break;
                    }
                }

                // check the neighbour.
                if (currentNeighbour == neighbour)
                {
                    // the neighbour was found.
                    resolvedNeighbour = newResolvedNeighbour.Key;
                    resolvedNeighbourData = arcs.First(x => x.Key == newResolvedNeighbour.Key).Value;
                    return true;
                }
            }
            resolvedNeighbourData = null;
            resolvedNeighbour = 0;
            return false;
        }

        /// <summary>
        ///     Returns the edge data between two neighbouring vertices.
        /// </summary>
        /// <param name="vertex1"></param>
        /// <param name="vertex2"></param>
        /// <returns></returns>
        private IDynamicGraphEdgeData GetEdgeData(long vertex1, long vertex2)
        {
            if (vertex1 > 0 && vertex2 > 0)
            {
                // none of the vertixes was a resolved vertex.
                KeyValuePair<uint, TEdgeData>[] arcs = _dataGraph.GetArcs(Convert.ToUInt32(vertex1));
                foreach (var arc in arcs)
                {
                    if (arc.Key == vertex2)
                        return arc.Value;
                }
                arcs = _dataGraph.GetArcs(Convert.ToUInt32(vertex2));
                foreach (var arc in arcs)
                {
                    if (arc.Key == vertex1)
                    {
                        var edge = new RouterResolvedGraph.RouterResolvedGraphEdge();
                        //edge.Backward = arc.Value.Forward;
                        //edge.Forward = arc.Value.Backward;
                        edge.Tags = arc.Value.Tags;
                        edge.Weight = arc.Value.Weight;
                        return edge;
                    }
                }
            }
            else
            {
                // one of the vertices was a resolved vertex.
                // edge should be in the resolved graph.
                KeyValuePair<long, RouterResolvedGraph.RouterResolvedGraphEdge>[] arcs = _resolved_graph.GetArcs(vertex1);
                foreach (var arc in arcs)
                {
                    if (arc.Key == vertex2)
                        return arc.Value;
                }
            }
            throw new Exception(string.Format("Edge {0}->{1} not found!", vertex1, vertex2));
        }

        /// <summary>
        ///     Returns the coordinate of the given vertex.
        /// </summary>
        /// <param name="vertex"></param>
        /// <returns></returns>
        private GeoCoordinate GetCoordinate(long vertex)
        {
            float latitude, longitude;
            if (vertex < 0)
            {
                // the vertex is resolved.
                if (!_resolved_graph.GetVertex(vertex, out latitude, out longitude))
                    throw new Exception(string.Format("Vertex with id {0} not found in resolved graph!", vertex));
            }
            else
            {
                // the vertex should be in the data graph.
                if (!_dataGraph.GetVertex(Convert.ToUInt32(vertex), out latitude, out longitude))
                    throw new Exception(string.Format("Vertex with id {0} not found in graph!", vertex));
            }
            return new GeoCoordinate(latitude, longitude);
        }

        #endregion

        #region Resolving Points

        /// <summary>
        ///     Holds all resolved points.
        /// </summary>
        private readonly Dictionary<GeoCoordinate, RouterPoint> _routerPoints;

        /// <summary>
        ///     Resolves the given coordinate to the closest routable point.
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="coordinate"></param>
        /// <returns></returns>
        public RouterPoint Resolve(VehicleEnum vehicle, GeoCoordinate coordinate)
        {
            return Resolve(vehicle, DefaultSearchDelta, coordinate, null, null);
        }

        /// <summary>
        ///     Resolves the given coordinate to the closest routable point.
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="delta"></param>
        /// <param name="coordinate"></param>
        /// <returns></returns>
        public RouterPoint Resolve(VehicleEnum vehicle, float delta, GeoCoordinate coordinate)
        {
            return Resolve(vehicle, delta, coordinate, null, null);
        }

        /// <summary>
        ///     Resolves the given coordinate to the closest routable point.
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="coordinate"></param>
        /// <param name="matcher"></param>
        /// <param name="matchingTags"></param>
        /// <returns></returns>
        public RouterPoint Resolve(VehicleEnum vehicle, GeoCoordinate coordinate, IEdgeMatcher matcher, IDictionary<string, string> matchingTags)
        {
            return Resolve(vehicle, DefaultSearchDelta, coordinate, matcher, matchingTags);
        }

        /// <summary>
        ///     Resolves the given coordinate to the closest routable point.
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="delta"></param>
        /// <param name="coordinate"></param>
        /// <param name="matcher"></param>
        /// <param name="matchingTags"></param>
        /// <returns></returns>
        public RouterPoint Resolve(VehicleEnum vehicle, float delta, GeoCoordinate coordinate, IEdgeMatcher matcher, IDictionary<string, string> matchingTags)
        {
            // check routing profiles.
            if (!SupportsVehicle(vehicle))
                throw new ArgumentOutOfRangeException("vehicle", string.Format("Routing profile {0} not supported by this router!", vehicle.ToString()));

            SearchClosestResult result = _router.SearchClosest(_dataGraph, _interpreter, vehicle, coordinate, delta, matcher, matchingTags);
                // search the closest routable object.
            if (result.Distance < double.MaxValue)
            {
                // a routable object was found.
                if (!result.Vertex2.HasValue)
                {
                    // the result was a single vertex.
                    float latitude, longitude;
                    if (!_dataGraph.GetVertex(result.Vertex1.Value, out latitude, out longitude))
                    {
                        // the vertex exists.
                        throw new Exception(string.Format("Vertex with id {0} not found!", result.Vertex1.Value));
                    }
                    return Normalize(new RouterPoint(result.Vertex1.Value, new GeoCoordinate(latitude, longitude), result.Vertex1.Value, result.Vertex1.Value));
                }
                else
                {
                    // the result is on an edge.
                    return Normalize(AddResolvedPoint(result.Vertex1.Value, result.Vertex2.Value, result.Position));
                }
            }
            return null; // no routable object was found closeby.
        }

        /// <summary>
        ///     Resolves the given coordinates to the closest routable points.
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="coordinate"></param>
        /// <returns></returns>
        public RouterPoint[] Resolve(VehicleEnum vehicle, GeoCoordinate[] coordinate)
        {
            return Resolve(vehicle, DefaultSearchDelta, coordinate);
        }

        /// <summary>
        ///     Resolves the given coordinates to the closest routable points.
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="delta"></param>
        /// <param name="coordinate"></param>
        /// <returns></returns>
        public RouterPoint[] Resolve(VehicleEnum vehicle, float delta, GeoCoordinate[] coordinate)
        {
            var points = new RouterPoint[coordinate.Length];
            for (int idx = 0; idx < coordinate.Length; idx++)
                points[idx] = Resolve(vehicle, delta, coordinate[idx]);
            return points;
        }

        /// <summary>
        ///     Resolves the given coordinates to the closest routable points.
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="coordinate"></param>
        /// <param name="matcher"></param>
        /// <param name="matchingTags"></param>
        /// <returns></returns>
        public RouterPoint[] Resolve(VehicleEnum vehicle, GeoCoordinate[] coordinate, IEdgeMatcher matcher, IDictionary<string, string>[] matchingTags)
        {
            return Resolve(vehicle, DefaultSearchDelta, coordinate, matcher, matchingTags);
        }

        /// <summary>
        ///     Resolves the given coordinates to the closest routable points.
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="delta"></param>
        /// <param name="coordinate"></param>
        /// <param name="matcher"></param>
        /// <param name="matchingTags"></param>
        /// <returns></returns>
        public RouterPoint[] Resolve(VehicleEnum vehicle, float delta, GeoCoordinate[] coordinate, IEdgeMatcher matcher, IDictionary<string, string>[] matchingTags)
        {
            var points = new RouterPoint[coordinate.Length];
            for (int idx = 0; idx < coordinate.Length; idx++)
                points[idx] = Resolve(vehicle, delta, coordinate[idx], matcher, matchingTags[idx]);
            return points;
        }

        /// <summary>
        ///     Find the coordinates of the closest routable point.
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="coordinate"></param>
        /// <returns></returns>
        public GeoCoordinate Search(VehicleEnum vehicle, GeoCoordinate coordinate)
        {
            // check routing profiles.
            if (!SupportsVehicle(vehicle))
                throw new ArgumentOutOfRangeException("vehicle", string.Format("Routing profile {0} not supported by this router!", vehicle.ToString()));

            return Search(vehicle, DefaultSearchDelta, coordinate);
        }

        /// <summary>
        ///     Find the coordinates of the closest routable point.
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="delta"></param>
        /// <param name="coordinate"></param>
        /// <returns></returns>
        public GeoCoordinate Search(VehicleEnum vehicle, float delta, GeoCoordinate coordinate)
        {
            // check routing profiles.
            if (!SupportsVehicle(vehicle))
                throw new ArgumentOutOfRangeException("vehicle", string.Format("Routing profile {0} not supported by this router!", vehicle.ToString()));

            SearchClosestResult result = _router.SearchClosest(_dataGraph, _interpreter, vehicle, coordinate, delta, null, null);
                // search the closest routable object.
            if (result.Distance < double.MaxValue)
            {
                // a routable object was found.
                if (!result.Vertex2.HasValue)
                {
                    // the result was a single vertex.
                    float latitude, longitude;
                    if (!_dataGraph.GetVertex(result.Vertex1.Value, out latitude, out longitude))
                    {
                        // the vertex exists.
                        throw new Exception(string.Format("Vertex with id {0} not found!", result.Vertex1.Value));
                    }
                    return new GeoCoordinate(latitude, longitude);
                }
                else
                {
                    // the result is on an edge.
                    throw new NotImplementedException();
                }
            }
            return null; // no routable object was found closeby.
        }

        #region Resolved Points Graph

        /// <summary>
        ///     Holds the resolved graph.
        /// </summary>
        private readonly RouterResolvedGraph _resolved_graph;

        /// <summary>
        ///     Holds the id of the next resolved point.
        /// </summary>
        private long _next_resolved_id = -1;

        /// <summary>
        ///     Returns the next resolved id.
        /// </summary>
        /// <returns></returns>
        private long GetNextResolvedId()
        {
            long next = _next_resolved_id;
            _next_resolved_id--;
            return next;
        }

        /// <summary>
        ///     Adds a resolved point to the graph.
        /// </summary>
        /// <param name="vertex1"></param>
        /// <param name="vertex2"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        private RouterPoint AddResolvedPoint(uint vertex1, uint vertex2, double position)
        {
            PathSegment<long> path = Shortest(vertex1, vertex2);
            float longitude1, latitude1, longitude2, latitude2;
            var vertices = new long[0];
            if (path != null)
            {
                // the vertices in this path.
                vertices = path.ToArray();
            }
            // should contain vertex1 and vertex2.
            if (vertices.Length == 0 || (vertices[0] == vertex1 && vertices[vertices.Length - 1] == vertex2))
            {
                // the vertices match.
                if (_dataGraph.GetVertex(vertex1, out latitude1, out longitude1) && _dataGraph.GetVertex(vertex2, out latitude2, out longitude2))
                {
                    // the two vertices are contained in the home graph.
                    var vertex1_coordinate = new GeoCoordinate(latitude1, longitude1);
                    var vertex2_coordinate = new GeoCoordinate(latitude2, longitude2);

                    var resolved_coordinate = new GeoCoordinate(latitude1 * (1.0 - position) + latitude2 * position,
                                                                longitude1 * (1.0 - position) + longitude2 * position);
                    if (vertices.Length == 0)
                    {
                        // the path has a length of 0; the vertices are not in the resolved graph yet!

                        // add the vertices in the resolved graph.
                        float latitude_dummy, longitude_dummy;
                        if (!_resolved_graph.GetVertex(vertex1, out latitude_dummy, out longitude_dummy))
                            _resolved_graph.AddVertex(vertex1, latitude1, longitude1);
                        if (!_resolved_graph.GetVertex(vertex2, out latitude_dummy, out longitude_dummy))
                            _resolved_graph.AddVertex(vertex2, latitude2, longitude2);

                        // find the arc(s).
                        KeyValuePair<uint, TEdgeData>? arc = null;
                        KeyValuePair<uint, TEdgeData>[] arcs = _dataGraph.GetArcs(vertex1);
                        for (int idx = 0; idx < arcs.Length; idx++)
                        {
                            if (arcs[idx].Key == vertex2)
                            {
                                // arc is found!
                                arc = arcs[idx];
                                break;
                            }
                        }
                        // find backward arc if needed.
                        if (!arc.HasValue)
                        {
                            arcs = _dataGraph.GetArcs(vertex2);
                            for (int idx = 0; idx < arcs.Length; idx++)
                            {
                                if (arcs[idx].Key == vertex1)
                                {
                                    // arc is found!
                                    arc = arcs[idx];
                                    break;
                                }
                            }
                        }

                        // check if an arc was found!
                        if (!arc.HasValue)
                            throw new Exception("A resolved position can only exist on an arc between two routable vertices.");

                        // add the arc (in both directions)
                        var resolved_edge_forward = new RouterResolvedGraph.RouterResolvedGraphEdge();
                        //if (forward)
                        //{
                        //    resolved_edge_forward.Backward = arc.Value.Value.Backward;
                        //    resolved_edge_forward.Forward = arc.Value.Value.Forward;
                        //}
                        //else
                        //{
                        //    resolved_edge_forward.Backward = !arc.Value.Value.Backward;
                        //    resolved_edge_forward.Forward = !arc.Value.Value.Forward;
                        //}
                        resolved_edge_forward.Tags = arc.Value.Value.Tags;
                        resolved_edge_forward.Weight = arc.Value.Value.Weight;
                        _resolved_graph.AddArc(vertex1, vertex2, resolved_edge_forward);
                        var resolved_edge_backward = new RouterResolvedGraph.RouterResolvedGraphEdge();
                        //if (!forward)
                        //{
                        //    resolved_edge_backward.Backward = arc.Value.Value.Backward;
                        //    resolved_edge_backward.Forward = arc.Value.Value.Forward;
                        //}
                        //else
                        //{
                        //    resolved_edge_backward.Backward = !arc.Value.Value.Backward;
                        //    resolved_edge_backward.Forward = !arc.Value.Value.Forward;
                        //}
                        resolved_edge_backward.Tags = arc.Value.Value.Tags;
                        resolved_edge_backward.Weight = arc.Value.Value.Weight;
                        _resolved_graph.AddArc(vertex2, vertex1, resolved_edge_backward);

                        // create the route manually.
                        vertices = new long[2];
                        vertices[0] = vertex1;
                        vertices[1] = vertex2;
                    }
                    else if (vertices.Length == 2)
                    {
                        // paths of length two are impossible!
                        throw new Exception("A resolved position can only exist on an arc between two routable vertices.");
                    }

                    // calculate positions.
                    int position_idx = 0;
                    double total = vertex1_coordinate.Distance(vertex2_coordinate);
                    for (int idx = 1; idx < vertices.Length; idx++)
                    {
                        // calculate positions.
                        float latitude_resolved, longitude_resolved;
                        if (_resolved_graph.GetVertex(vertices[idx], out latitude_resolved, out longitude_resolved))
                        {
                            // vertex found
                            double current_position = (vertex1_coordinate.Distance(new GeoCoordinate(latitude_resolved, longitude_resolved))) / total;
                            if (current_position > position)
                            {
                                // the position is found.
                                position_idx = idx - 1;
                                break;
                            }
                        }
                        else
                        {
                            // oeps; vertex was not found!
                            throw new Exception(string.Format("Vertex with id {0} not found!", vertices[idx]));
                        }
                    }

                    // get the vertices and the arc between them.
                    long vertex_from = vertices[position_idx];
                    long vertex_to = vertices[position_idx + 1];

                    KeyValuePair<long, RouterResolvedGraph.RouterResolvedGraphEdge>? from_to_arc = null;
                    KeyValuePair<long, RouterResolvedGraph.RouterResolvedGraphEdge>[] from_arcs = _resolved_graph.GetArcs(vertex_from);
                    for (int idx = 0; idx < from_arcs.Length; idx++)
                    {
                        if (from_arcs[idx].Key == vertex_to)
                        {
                            // arc is found!
                            from_to_arc = from_arcs[idx];
                            break;
                        }
                    }

                    // check if an arc was found!
                    if (!from_to_arc.HasValue)
                        throw new Exception("A resolved position can only exist on an arc between two vertices.");

                    // remove the arc.
                    _resolved_graph.DeleteArc(vertex_from, vertex_to);
                    _resolved_graph.DeleteArc(vertex_to, vertex_from);

                    // calculate the relative position.
                    double relative_position = 0;
                    GeoCoordinate vertex_from_coordinate = null;
                    GeoCoordinate vertex_to_coordinate = null;
                    float latitude_resolved1, longitude_resolved1, latitude_resolved2, longitude_resolved2;
                    if (_resolved_graph.GetVertex(vertex_from, out latitude_resolved1, out longitude_resolved1) &&
                        _resolved_graph.GetVertex(vertex_to, out latitude_resolved2, out longitude_resolved2))
                    {
                        vertex_from_coordinate = new GeoCoordinate(latitude_resolved1, longitude_resolved1);
                        vertex_to_coordinate = new GeoCoordinate(latitude_resolved2, longitude_resolved2);
                        relative_position = vertex_from_coordinate.Distance(resolved_coordinate) / vertex_from_coordinate.Distance(vertex_to_coordinate);
                    }
                    else
                    {
                        // oeps; vertices not found!
                        throw new Exception("A resolved position can only exist on an arc between two routable vertices.");
                    }

                    // add new vertex.
                    long resolved_vertex = GetNextResolvedId();
                    _resolved_graph.AddVertex(resolved_vertex, (float) resolved_coordinate.Latitude, (float) resolved_coordinate.Longitude);

                    // add the arcs.
                    var from_resolved_edge = new RouterResolvedGraph.RouterResolvedGraphEdge();
                    //from_resolved_edge.Backward = from_to_arc.Value.Value.Backward;
                    //from_resolved_edge.Forward = from_to_arc.Value.Value.Forward;
                    from_resolved_edge.Tags = from_to_arc.Value.Value.Tags;
                    from_resolved_edge.Weight = from_to_arc.Value.Value.Weight * relative_position;
                    _resolved_graph.AddArc(vertex_from, resolved_vertex, from_resolved_edge);
                    var resolved_from_edge = new RouterResolvedGraph.RouterResolvedGraphEdge();
                    //resolved_from_edge.Backward = !from_to_arc.Value.Value.Backward;
                    //resolved_from_edge.Forward = !from_to_arc.Value.Value.Forward;
                    resolved_from_edge.Tags = from_to_arc.Value.Value.Tags;
                    resolved_from_edge.Weight = from_to_arc.Value.Value.Weight * relative_position;
                    _resolved_graph.AddArc(resolved_vertex, vertex_from, resolved_from_edge);

                    // add the new arcs.
                    var resolved_to_edge = new RouterResolvedGraph.RouterResolvedGraphEdge();
                    //resolved_to_edge.Backward = from_to_arc.Value.Value.Backward;
                    //resolved_to_edge.Forward = from_to_arc.Value.Value.Forward;
                    resolved_to_edge.Tags = from_to_arc.Value.Value.Tags;
                    resolved_to_edge.Weight = from_to_arc.Value.Value.Weight * (1.0 - position);
                    _resolved_graph.AddArc(vertex_to, resolved_vertex, resolved_to_edge);
                    var to_resolved_edge = new RouterResolvedGraph.RouterResolvedGraphEdge();
                    //to_resolved_edge.Backward = !from_to_arc.Value.Value.Backward;
                    //to_resolved_edge.Forward = !from_to_arc.Value.Value.Forward;
                    to_resolved_edge.Tags = from_to_arc.Value.Value.Tags;
                    to_resolved_edge.Weight = from_to_arc.Value.Value.Weight * (1.0 - position);
                    _resolved_graph.AddArc(resolved_vertex, vertex_to, to_resolved_edge);

                    return new RouterPoint(resolved_vertex, resolved_coordinate, vertex1, vertex2);
                }
                else
                    throw new Exception("A resolved position can only exist on an arc between two routable vertices.");
            }
            else
                throw new Exception("A shortest path between two vertices has to contain at least the source and target!");
        }

        /// <summary>
        ///     Calculates all routes from a given resolved point to the routable graph.
        /// </summary>
        /// <param name="resolved_point"></param>
        /// <returns></returns>
        private PathSegmentVisitList RouteResolvedGraph(RouterPoint resolved_point)
        {
            // initialize the resulting visit list.
            var result = new PathSegmentVisitList(resolved_point.Neighbour1, resolved_point.Neighbour2);

            // do a simple dykstra search and add all found routable vertices to the visit list.
            var settled = new HashSet<long>();
            var current = new PathSegment<long>(resolved_point.Id);
            var visit_list = new PathSegmentVisitList(-1, -1);
            visit_list.UpdateVertex(current);

            while (true)
            {
                // return the vertex on top of the list.
                current = visit_list.GetFirst();
                // update the settled list.
                if (current != null)
                    settled.Add(current.VertexId);
                while (current != null && current.VertexId > 0)
                {
                    // add to the visit list.
                    result.UpdateVertex(current);

                    // choose a new current one.
                    current = visit_list.GetFirst();
                    // update the settled list.
                    if (current != null)
                        settled.Add(current.VertexId);
                }

                // check if it is the target.
                if (current == null)
                {
                    // current is empty; target not found!
                    return result;
                }

                // get the neighbours.
                KeyValuePair<long, RouterResolvedGraph.RouterResolvedGraphEdge>[] arcs = _resolved_graph.GetArcs(current.VertexId);
                for (int idx = 0; idx < arcs.Length; idx++)
                {
                    KeyValuePair<long, RouterResolvedGraph.RouterResolvedGraphEdge> arc = arcs[idx];
                    if (!settled.Contains(arc.Key))
                        visit_list.UpdateVertex(new PathSegment<long>(arc.Key, arc.Value.Weight + current.Weight, current));
                }
            }
        }

        /// <summary>
        ///     Calculates all routes from all the given resolved points to the routable graph.
        /// </summary>
        /// <param name="resolved_points"></param>
        /// <returns></returns>
        private PathSegmentVisitList[] RouteResolvedGraph(RouterPoint[] resolved_points)
        {
            var visit_lists = new PathSegmentVisitList[resolved_points.Length];
            for (int idx = 0; idx < resolved_points.Length; idx++)
                visit_lists[idx] = RouteResolvedGraph(resolved_points[idx]);
            return visit_lists;
        }

        #region Resolved Graph Routing

        /// <summary>
        ///     Calculates the shortest path between two points in the resolved vertex.
        /// </summary>
        /// <param name="vertex1"></param>
        /// <param name="vertex2"></param>
        /// <returns></returns>
        private PathSegment<long> Shortest(long vertex1, long vertex2)
        {
            var settled = new HashSet<long>();
            var current = new PathSegment<long>(vertex1);
            var visit_list = new PathSegmentVisitList(vertex1, vertex1);
            visit_list.UpdateVertex(current);

            while (true)
            {
                // return the vertex on top of the list.
                current = visit_list.GetFirst();

                // check if it is the target.
                if (current == null)
                {
                    // current is empty; target not found!
                    return null;
                }
                if (current.VertexId == vertex2)
                {
                    // current is the target.
                    return current;
                }

                // update the settled list.
                settled.Add(current.VertexId);

                // get the neighbours.
                KeyValuePair<long, RouterResolvedGraph.RouterResolvedGraphEdge>[] arcs = _resolved_graph.GetArcs(current.VertexId);
                for (int idx = 0; idx < arcs.Length; idx++)
                {
                    KeyValuePair<long, RouterResolvedGraph.RouterResolvedGraphEdge> arc = arcs[idx];
                    if (!settled.Contains(arc.Key) && (arc.Key < 0 || arc.Key == vertex2))
                        visit_list.UpdateVertex(new PathSegment<long>(arc.Key, arc.Value.Weight + current.Weight, current));
                }
            }
        }

        #endregion

        #endregion

        /// <summary>
        ///     Normalizes the router point.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        private RouterPoint Normalize(RouterPoint point)
        {
            RouterPoint normalize;
            if (!_routerPoints.TryGetValue(point.Location, out normalize))
            {
                _routerPoints.Add(point.Location, point);
                normalize = point;
            }
            return normalize;
        }

        /// <summary>
        ///     Resolves the given coordinate to the closest routable point.
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="coordinate"></param>
        /// <param name="pointTags"></param>
        /// <returns></returns>
        public RouterPoint Resolve(VehicleEnum vehicle, GeoCoordinate coordinate, IDictionary<string, string> pointTags)
        {
            return Resolve(vehicle, DefaultSearchDelta, coordinate, pointTags);
        }

        /// <summary>
        ///     Resolves the given coordinate to the closest routable point.
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="delta"></param>
        /// <param name="coordinate"></param>
        /// <param name="pointTags"></param>
        /// <returns></returns>
        public RouterPoint Resolve(VehicleEnum vehicle, float delta, GeoCoordinate coordinate, IDictionary<string, string> pointTags)
        {
            return Resolve(vehicle, delta, coordinate, null, pointTags);
        }

        #endregion

        #region Static Creation Methods

        /// <summary>
        ///     Creates a pre-processed edge router.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static Router<PreProcessedEdge> CreatePreProcessedEdgeRouter(DataProcessorSource source)
        {
            // create the default interpreter.
            var interpreter = new OsmRoutingInterpreter();

            return Router<PreProcessedEdge>.CreatePreProcessedEdgeRouter(source, interpreter);
        }

        /// <summary>
        ///     Creates a pre-processed edge router.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="interpreter"></param>
        /// <returns></returns>
        public static Router<PreProcessedEdge> CreatePreProcessedEdgeRouter(DataProcessorSource source, IRoutingInterpreter interpreter)
        {
            var tagsIndex = new OsmTagsIndex();
            var osmData = new DynamicGraphRouterDataSource<PreProcessedEdge>(tagsIndex);
            var targetData = new PreProcessedDataGraphProcessingTarget(osmData, interpreter, osmData.TagsIndex, VehicleEnum.Car);
            var sorter = new DataProcessorFilterSort();
            sorter.RegisterSource(source);
            targetData.RegisterSource(sorter);
            targetData.Pull();

            // create the router object.
            return new Router<PreProcessedEdge>(osmData, interpreter, new DykstraRoutingPreProcessed(osmData.TagsIndex));
        }

        /// <summary>
        ///     Creates a simple-weighed edge router.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static Router<SimpleWeighedEdge> CreateSimpleWeighedEdgeRouter(DataProcessorSource source)
        {
            // create the default interpreter.
            var interpreter = new OsmRoutingInterpreter();

            return Router<SimpleWeighedEdge>.CreateSimpleWeighedEdgeRouter(source, interpreter);
        }

        /// <summary>
        ///     Creates a simple-weighed edge router.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="interpreter"></param>
        /// <returns></returns>
        public static Router<SimpleWeighedEdge> CreateSimpleWeighedEdgeRouter(DataProcessorSource source, IRoutingInterpreter interpreter)
        {
            var tagsIndex = new OsmTagsIndex();
            var osmData = new DynamicGraphRouterDataSource<SimpleWeighedEdge>(tagsIndex);
            var targetData = new SimpleWeighedDataGraphProcessingTarget(osmData, interpreter, osmData.TagsIndex, VehicleEnum.Car);
            var sorter = new DataProcessorFilterSort();
            sorter.RegisterSource(source);
            targetData.RegisterSource(sorter);
            targetData.Pull();

            // create the router object.
            return new Router<SimpleWeighedEdge>(osmData, interpreter, new DykstraRoutingLive(osmData.TagsIndex));
        }

        #endregion
    }
}