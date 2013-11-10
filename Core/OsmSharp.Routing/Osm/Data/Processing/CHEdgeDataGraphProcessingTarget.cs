using System.Collections.Generic;
using OsmSharp.Routing.CH.PreProcessing;
using OsmSharp.Routing.Graph.Router;
using OsmSharp.Routing.Interpreter;
using OsmSharp.Routing.Interpreter.Roads;
using OsmSharp.Tools.Math;
using OsmSharp.Tools.Math.Geo;

namespace OsmSharp.Routing.Osm.Data.Processing
{
    /// <summary>
    ///     A pre-processing target for OSM-data to a contraction hierarchies data structure.
    /// </summary>
    public class CHEdgeDataGraphProcessingTarget : DynamicGraphDataProcessorTarget<CHEdgeData>
    {
        /// <summary>
        ///     Holds the data source.
        /// </summary>
        private readonly IDynamicGraphRouterDataSource<CHEdgeData> _dynamicDataSource;

        /// <summary>
        ///     Holds the vehicle profile this pre-processing target is for.
        /// </summary>
        private readonly VehicleEnum _vehicle;

        /// <summary>
        ///     Creates a CH data processing target.
        /// </summary>
        /// <param name="dynamicGraph"></param>
        /// <param name="interpreter"></param>
        /// <param name="tagsIndex"></param>
        /// <param name="vehicle"></param>
        public CHEdgeDataGraphProcessingTarget(IDynamicGraphRouterDataSource<CHEdgeData> dynamicGraph, IRoutingInterpreter interpreter, ITagsIndex tagsIndex,
                                               VehicleEnum vehicle) : base(dynamicGraph, interpreter, new CHEdgeDataComparer(), tagsIndex)
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
        /// <param name="edgeInterpreter"></param>
        /// <param name="tagsIndex"></param>
        /// <param name="tags"></param>
        /// <param name="directionForward"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="tagsId"></param>
        /// <returns></returns>
        protected override CHEdgeData CalculateEdgeData(IEdgeInterpreter edgeInterpreter, ITagsIndex tagsIndex, IDictionary<string, string> tags, bool directionForward,
                                                        GeoCoordinate from, GeoCoordinate to, uint? tagsId)
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

            // calculate tags id.
            if (!tagsId.HasValue)
                tagsId = tagsIndex.Add(tags);

            // initialize the edge data.
            return new CHEdgeData { Weight = (float) weight, Forward = forward, Backward = backward, Tags = tagsId.Value, ContractedVertexId = 0 };
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