using System.Collections.Generic;
using OsmSharp.Routing.Graph.DynamicGraph.PreProcessed;
using OsmSharp.Routing.Graph.Router;
using OsmSharp.Routing.Interpreter;
using OsmSharp.Routing.Interpreter.Roads;
using OsmSharp.Tools.Math;
using OsmSharp.Tools.Math.Geo;

namespace OsmSharp.Routing.Osm.Data.Processing
{
    /// <summary>
    ///     A pre-processed data graph processing target.
    /// </summary>
    public class PreProcessedDataGraphProcessingTarget : DynamicGraphDataProcessorTarget<PreProcessedEdge>
    {
        /// <summary>
        ///     Holds the data source.
        /// </summary>
        private readonly IDynamicGraphRouterDataSource<PreProcessedEdge> _dynamicDataSource;

        /// <summary>
        ///     Holds the vehicle profile this pre-processing target is for.
        /// </summary>
        private readonly VehicleEnum _vehicle;

        /// <summary>
        ///     Creates a new osm edge data processing target.
        /// </summary>
        /// <param name="dynamicGraph"></param>
        /// <param name="interpreter"></param>
        /// <param name="tagsIndex"></param>
        /// <param name="vehicle"></param>
        public PreProcessedDataGraphProcessingTarget(IDynamicGraphRouterDataSource<PreProcessedEdge> dynamicGraph, IRoutingInterpreter interpreter,
                                                     ITagsIndex tagsIndex, VehicleEnum vehicle)
            : this(dynamicGraph, interpreter, tagsIndex, vehicle, new Dictionary<long, uint>())
        {
        }

        /// <summary>
        ///     Creates a new osm edge data processing target.
        /// </summary>
        /// <param name="dynamicGraph"></param>
        /// <param name="interpreter"></param>
        /// <param name="tagsIndex"></param>
        /// <param name="vehicle"></param>
        /// <param name="idTransformations"></param>
        public PreProcessedDataGraphProcessingTarget(IDynamicGraphRouterDataSource<PreProcessedEdge> dynamicGraph, IRoutingInterpreter interpreter,
                                                     ITagsIndex tagsIndex, VehicleEnum vehicle, IDictionary<long, uint> idTransformations)
            : this(dynamicGraph, interpreter, tagsIndex, vehicle, idTransformations, null)
        {
        }

        /// <summary>
        ///     Creates a new osm edge data processing target.
        /// </summary>
        /// <param name="dynamicGraph"></param>
        /// <param name="interpreter"></param>
        /// <param name="tagsIndex"></param>
        /// <param name="vehicle"></param>
        /// <param name="box"></param>
        public PreProcessedDataGraphProcessingTarget(IDynamicGraphRouterDataSource<PreProcessedEdge> dynamicGraph, IRoutingInterpreter interpreter,
                                                     ITagsIndex tagsIndex, VehicleEnum vehicle, GeoCoordinateBox box)
            : this(dynamicGraph, interpreter, tagsIndex, vehicle, new Dictionary<long, uint>(), box)
        {
        }

        /// <summary>
        ///     Creates a new osm edge data processing target.
        /// </summary>
        /// <param name="dynamicGraph"></param>
        /// <param name="interpreter"></param>
        /// <param name="tagsIndex"></param>
        /// <param name="vehicle"></param>
        /// <param name="idTransformations"></param>
        /// <param name="box"></param>
        public PreProcessedDataGraphProcessingTarget(IDynamicGraphRouterDataSource<PreProcessedEdge> dynamicGraph, IRoutingInterpreter interpreter,
                                                     ITagsIndex tagsIndex, VehicleEnum vehicle, IDictionary<long, uint> idTransformations, GeoCoordinateBox box)
            : base(dynamicGraph, interpreter, null, tagsIndex, idTransformations, box)
        {
            _vehicle = vehicle;
            _dynamicDataSource = dynamicGraph;
        }

        /// <summary>
        ///     Initializes this target.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();

            _dynamicDataSource.AddSupportedProfile(_vehicle);
        }

        /// <summary>
        ///     Calculates edge data.
        /// </summary>
        /// <param name="tagsIndex"></param>
        /// <param name="tags"></param>
        /// <param name="directionForward"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="edgeInterpreter"></param>
        /// <param name="tagsId"></param>
        /// <returns></returns>
        protected override PreProcessedEdge CalculateEdgeData(IEdgeInterpreter edgeInterpreter, ITagsIndex tagsIndex, IDictionary<string, string> tags,
                                                              bool directionForward, GeoCoordinate from, GeoCoordinate to, uint? tagsId)
        {
            double weight = edgeInterpreter.Weight(tags, _vehicle, from, to);
            bool? direction = edgeInterpreter.IsOneWay(tags, _vehicle);
            bool forward, backward;
            if (!direction.HasValue)
            {
                // both directions.
                forward = true;
                backward = true;
            }
            else
            {
                // define back/forward.
                forward = (directionForward && direction.Value) || (!directionForward && !direction.Value);
                backward = (directionForward && !direction.Value) || (!directionForward && direction.Value);
            }

            if (!tagsId.HasValue)
                tagsId = tagsIndex.Add(tags);

            // initialize the edge data.
            return new PreProcessedEdge((float) weight, forward, backward, tagsId.Value);
        }

        /// <summary>
        ///     Calculates edge data. DOES NOT DO ANYTHING WITH previous.
        /// </summary>
        /// <param name="edgeInterpreter"></param>
        /// <param name="tagsIndex"></param>
        /// <param name="tags"></param>
        /// <param name="directionForward"></param>
        /// <param name="previous"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="tagsId"></param>
        /// <returns></returns>
        protected PreProcessedEdge CalculateEdgeData(IEdgeInterpreter edgeInterpreter, ITagsIndex tagsIndex, IDictionary<string, string> tags,
                                                              bool directionForward, GeoCoordinate previous, GeoCoordinate from, GeoCoordinate to, uint? tagsId)
        {
            double weight = edgeInterpreter.Weight(tags, _vehicle, from, to);
            bool? direction = edgeInterpreter.IsOneWay(tags, _vehicle);
            bool forward, backward;
            if (!direction.HasValue)
            {
                // both directions.
                forward = true;
                backward = true;
            }
            else
            {
                // define back/forward.
                forward = (directionForward && direction.Value) || (!directionForward && !direction.Value);
                backward = (directionForward && !direction.Value) || (!directionForward && direction.Value);
            }

            if (!tagsId.HasValue)
                tagsId = tagsIndex.Add(tags);

            // initialize the edge data.
            return new PreProcessedEdge((float)weight, forward, backward, tagsId.Value);
        }

        /// <summary>
        ///     Returns true if the edge is traversable.
        /// </summary>
        /// <param name="edgeInterpreter"></param>
        /// <param name="tagsIndex"></param>
        /// <param name="tags"></param>
        /// <returns></returns>
        protected override bool CalculateIsTraversable(IEdgeInterpreter edgeInterpreter, ITagsIndex tagsIndex, IDictionary<string, string> tags)
        {
            return edgeInterpreter.CanBeTraversedBy(tags, _vehicle);
        }
    }
}