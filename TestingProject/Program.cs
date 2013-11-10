using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using OsmSharp;
using OsmSharp.Routing.Graph.DynamicGraph.SimpleWeighed;
using OsmSharp.Osm;
using OsmSharp.Routing;
using OsmSharp.Routing.Osm.Interpreter;
using OsmSharp.Routing.Osm.Interpreter.Edge;
using OsmSharp.Routing.Graph.Memory;
using OsmSharp.Routing.Osm.Data.Processing;
using OsmSharp.Osm.Data.XML.Processor;
using OsmSharp.Osm.Data.Processor.Filter.Sort;
using OsmSharp.Routing.Graph.Router.Dykstra;
using OsmSharp.Tools.Math.Geo;
using OsmSharp.Routing.TSP;
using OsmSharp.Routing.TSP.Genetic;
using OsmSharp.Routing.Route;

namespace TestingOsmSharp
{
    class Program
    {
        static void Main(string[] args)
        {
            // keeps a memory-efficient version of the osm-tags.
            OsmTagsIndex tags_index = new OsmTagsIndex();

            // creates a routing interpreter. (used to translate osm-tags into a routable network)
            OsmRoutingInterpreter interpreter = new OsmRoutingInterpreter(new MotorcycleEdgeInterpreter());

            // create a routing datasource, keeps all processed osm routing data.
            MemoryRouterDataSource<SimpleWeighedEdge> osm_data =
                new MemoryRouterDataSource<SimpleWeighedEdge>(tags_index);

            // load data into this routing datasource.
            Stream osm_xml_data = new FileInfo(@"C:\Users\Erik\Documents\MotorcycleMaps\boulder.osm").OpenRead(); // for example moscow!
            using (osm_xml_data)
            {
                SimpleWeighedDataGraphProcessingTarget target_data = new SimpleWeighedDataGraphProcessingTarget(
                    osm_data, interpreter, osm_data.TagsIndex, VehicleEnum.Car);
                XmlDataProcessorSource data_processor_source = new XmlDataProcessorSource(osm_xml_data);
                DataProcessorFilterSort sorter = new DataProcessorFilterSort();
                sorter.RegisterSource(data_processor_source);
                target_data.RegisterSource(sorter);
                target_data.Pull();
            }

            // create the router object.
            IRouter<RouterPoint> router = new Router<SimpleWeighedEdge>(osm_data, interpreter,
                new DykstraRoutingLive(osm_data.TagsIndex));

            // build a list of points to route from/to. (some coordinates in moscow)
            IList<GeoCoordinate> coordinates = new List<GeoCoordinate>();
            coordinates.Add(new GeoCoordinate(40.017427, -105.290567));
            coordinates.Add(new GeoCoordinate(40.017207, -105.288965));

            // find the closest road for each of these points.
            RouterPoint[] points = router.Resolve(VehicleEnum.Car, coordinates.ToArray());

            // calculate all travel times between each of these points.
            double[][] times = router.CalculateManyToManyWeight(VehicleEnum.Car,
                points, points);

            // solve the TSP.
            RouterTSP tsp_solver = new RouterTSPAEXGenetic(); // use a genetic algorithm to solve the TSP. 
            OsmSharp.Routing.TSP.RouterTSPWrapper<RouterPoint, RouterTSP> tsp_router =
                new RouterTSPWrapper<RouterPoint, RouterTSP>(tsp_solver, router);
            OsmSharpRoute route = tsp_router.CalculateTSP(VehicleEnum.Car, points, true);

            // save this route to gpx for example.
            route.SaveAsGpx(new FileInfo(@"C:\Users\Erik\Documents\MotorcycleMaps\boulder.gpx"));

        }
    }
}
