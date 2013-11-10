﻿
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using OsmSharp.Osm;
using OsmSharp.Osm.Data.Processor.Filter.Sort;
using OsmSharp.Routing.Graph;
using OsmSharp.Routing.Graph.DynamicGraph.SimpleWeighed;
using OsmSharp.Routing.Osm.Data.Processing;
using OsmSharp.Routing;
using OsmSharp.Osm.Data.XML.Processor;
using System.Reflection;
using OsmSharp.Routing.Osm.Interpreter;
using OsmSharp.Routing.Graph.Router;
using OsmSharp.Routing.Graph.Router.Dykstra;
using OsmSharp.Tools.Math.Geo;
using OsmSharp.Routing.Route;
using OsmSharp.Routing.Instructions;
using OsmSharp.UnitTests.Routing.Instructions;
using OsmSharp.Tools.Enumerations;

namespace OsmSharp.UnitTests.Routing
{
    /// <summary>
    /// Holds some regression tests of previously detected routing issues.
    /// </summary>
    [TestFixture]
    public class RoutingRegressionTests
    {
        /// <summary>
        /// Issue with resolved points and incorrect routing.
        /// Some routes will pass through the same point twice.
        /// </summary>
        [Test]
        public void RoutingRegressionTest1()
        {
            OsmRoutingInterpreter interpreter = new OsmRoutingInterpreter();

            OsmTagsIndex tags_index = new OsmTagsIndex();

            // do the data processing.
            DynamicGraphRouterDataSource<SimpleWeighedEdge> memory_data =
                new DynamicGraphRouterDataSource<SimpleWeighedEdge>(tags_index);
            SimpleWeighedDataGraphProcessingTarget target_data = new SimpleWeighedDataGraphProcessingTarget(
                memory_data, interpreter, memory_data.TagsIndex, VehicleEnum.Car);
            XmlDataProcessorSource data_processor_source = new XmlDataProcessorSource(
                Assembly.GetExecutingAssembly().GetManifestResourceStream("OsmSharp.UnitTests.test_routing_regression1.osm"));
            DataProcessorFilterSort sorter = new DataProcessorFilterSort();
            sorter.RegisterSource(data_processor_source);
            target_data.RegisterSource(sorter);
            target_data.Pull();

            IBasicRouter<SimpleWeighedEdge> basic_router = new DykstraRoutingLive(memory_data.TagsIndex);
            IRouter<RouterPoint> router = new Router<SimpleWeighedEdge>(
                memory_data, interpreter, basic_router);

            // resolve the three points in question.
            GeoCoordinate point35 = new GeoCoordinate(51.01257, 4.000753);
            RouterPoint point35resolved = router.Resolve(VehicleEnum.Car, point35);
            GeoCoordinate point40 = new GeoCoordinate(51.01250, 4.000013);
            RouterPoint point40resolved = router.Resolve(VehicleEnum.Car, point40);
            GeoCoordinate point45 = new GeoCoordinate(51.01315, 3.999588);
            RouterPoint point45resolved = router.Resolve(VehicleEnum.Car, point45);

            // route between 35 and 45.
            OsmSharpRoute routebefore = router.Calculate(VehicleEnum.Car, point35resolved, point45resolved);

            GeoCoordinate point129 = new GeoCoordinate(51.01239, 3.999573);
            RouterPoint point129resolved = router.Resolve(VehicleEnum.Car, point129);

            // route between 35 and 45.
            OsmSharpRoute routeafter = router.Calculate(VehicleEnum.Car, point35resolved, point45resolved);
            Assert.AreEqual(routebefore.TotalDistance, routeafter.TotalDistance);
        }

        /// <summary>
        /// Issue with generation instructions but where streetnames seem to be stripped.
        /// Some streetnames are missing from the instructions.
        /// </summary>
        [Test]
        public void RoutingRegressionTest2()
        {
            OsmRoutingInterpreter interpreter = new OsmRoutingInterpreter();

            OsmTagsIndex tags_index = new OsmTagsIndex();

            // do the data processing.
            DynamicGraphRouterDataSource<SimpleWeighedEdge> memory_data =
                new DynamicGraphRouterDataSource<SimpleWeighedEdge>(tags_index);
            SimpleWeighedDataGraphProcessingTarget target_data = new SimpleWeighedDataGraphProcessingTarget(
                memory_data, interpreter, memory_data.TagsIndex, VehicleEnum.Car);
            XmlDataProcessorSource data_processor_source = new XmlDataProcessorSource(
                Assembly.GetExecutingAssembly().GetManifestResourceStream("OsmSharp.UnitTests.test_routing_regression1.osm"));
            DataProcessorFilterSort sorter = new DataProcessorFilterSort();
            sorter.RegisterSource(data_processor_source);
            target_data.RegisterSource(sorter);
            target_data.Pull();

            IBasicRouter<SimpleWeighedEdge> basic_router = new DykstraRoutingLive(memory_data.TagsIndex);
            IRouter<RouterPoint> router = new Router<SimpleWeighedEdge>(
                memory_data, interpreter, basic_router);

            // resolve the three points in question.
            GeoCoordinate point35 = new GeoCoordinate(51.01257, 4.000753);
            RouterPoint point35resolved = router.Resolve(VehicleEnum.Car, point35);
            GeoCoordinate point45 = new GeoCoordinate(51.01315, 3.999588);
            RouterPoint point45resolved = router.Resolve(VehicleEnum.Car, point45);
            GeoCoordinate point40 = new GeoCoordinate(51.01250, 4.000013);
            RouterPoint point40resolved = router.Resolve(VehicleEnum.Car, point40);

            // calculate two smaller routes.
            OsmSharpRoute route3545 = router.Calculate(VehicleEnum.Car, point35resolved, point45resolved);
            OsmSharpRoute route4540 = router.Calculate(VehicleEnum.Car, point45resolved, point40resolved);
            OsmSharpRoute route3540concatenated = OsmSharpRoute.Concatenate(route3545, route4540);

            OsmSharpRoute route3540 = router.Calculate(VehicleEnum.Car, point35resolved, point40resolved);

            // check if both routes are equal.
            Assert.AreEqual(route3540.Entries.Length, route3540concatenated.Entries.Length);
            for(int idx = 0; idx < route3540.Entries.Length; idx++)
            {
                Assert.AreEqual(route3540.Entries[idx].Distance, route3540concatenated.Entries[idx].Distance);
                Assert.AreEqual(route3540.Entries[idx].Latitude, route3540concatenated.Entries[idx].Latitude);
                Assert.AreEqual(route3540.Entries[idx].Longitude, route3540concatenated.Entries[idx].Longitude);
                Assert.AreEqual(route3540.Entries[idx].Time, route3540concatenated.Entries[idx].Time);
                Assert.AreEqual(route3540.Entries[idx].Type, route3540concatenated.Entries[idx].Type);
                Assert.AreEqual(route3540.Entries[idx].WayFromName, route3540concatenated.Entries[idx].WayFromName);

                // something that is allowed to be different in this case!
                // route3540.Entries[idx].Points != null

                // check sidestreets.
                if (route3540.Entries[idx].SideStreets != null && 
                    route3540.Entries[idx].SideStreets.Length > 0)
                { // check if the sidestreets represent the same information.
                    for (int metricIdx = 0; metricIdx < route3540concatenated.Entries[idx].SideStreets.Length; metricIdx++)
                    {
                        Assert.AreEqual(route3540.Entries[idx].SideStreets[metricIdx].WayName, 
                            route3540concatenated.Entries[idx].SideStreets[metricIdx].WayName);
                        Assert.AreEqual(route3540.Entries[idx].SideStreets[metricIdx].Latitude, 
                            route3540concatenated.Entries[idx].SideStreets[metricIdx].Latitude);
                        Assert.AreEqual(route3540.Entries[idx].SideStreets[metricIdx].Longitude, 
                            route3540concatenated.Entries[idx].SideStreets[metricIdx].Longitude);
                    }
                }
                else
                {
                    Assert.IsTrue(route3540concatenated.Entries[idx].SideStreets == null ||
                        route3540concatenated.Entries[idx].SideStreets.Length == 0);
                }


                if (route3540.Entries[idx].Tags != null &&
                    route3540.Entries[idx].Tags.Length > 0)
                { // check if the Tags represent the same information.
                    for (int metricIdx = 0; metricIdx < route3540concatenated.Entries[idx].Tags.Length; metricIdx++)
                    {
                        Assert.AreEqual(route3540.Entries[idx].Tags[metricIdx].Key,
                            route3540concatenated.Entries[idx].Tags[metricIdx].Key);
                        Assert.AreEqual(route3540.Entries[idx].Tags[metricIdx].Value,
                            route3540concatenated.Entries[idx].Tags[metricIdx].Value);
                    }
                }
                else
                {
                    Assert.IsTrue(route3540concatenated.Entries[idx].Tags == null ||
                        route3540concatenated.Entries[idx].Tags.Length == 0);
                }

                Assert.AreEqual(route3540.Entries[idx].Distance, route3540concatenated.Entries[idx].Distance);
            }
            if (route3540.Tags != null &&
                route3540.Tags.Length > 0)
            {
                for (int tagIdx = 0; tagIdx < route3540.Tags.Length; tagIdx++)
                {
                    if (route3540.Tags[tagIdx].Key != "debug_route")
                    {
                        Assert.AreEqual(route3540.Tags[tagIdx].Key, route3540concatenated.Tags[tagIdx].Key);
                        Assert.AreEqual(route3540.Tags[tagIdx].Value, route3540concatenated.Tags[tagIdx].Value);
                    }
                }
            }
            else
            {
                Assert.IsTrue(route3540concatenated.Tags == null ||
                    route3540concatenated.Tags.Length == 0);
            }          
            if (route3540.Metrics != null)
            {
                for (int metricIdx = 0; metricIdx < route3540concatenated.Entries.Length; metricIdx++)
                {
                    Assert.AreEqual(route3540.Metrics[metricIdx].Key, route3540concatenated.Metrics[metricIdx].Key);
                    Assert.AreEqual(route3540.Metrics[metricIdx].Value, route3540concatenated.Metrics[metricIdx].Value);
                }
            }
            else
            {
                Assert.IsNull(route3540concatenated.Metrics);
            }

            // remove the point in between, the only difference between the regular and the concatenated route.
            route3540concatenated.Entries[7].Points = null;

            // create the language generator.
            var languageGenerator = new LanguageTestGenerator();

            // generate the instructions.
            var instructionGenerator = new InstructionGenerator();
            List<Instruction> instructions =
                instructionGenerator.Generate(route3540, interpreter, languageGenerator);
            List<Instruction> instructionsConcatenated =
                instructionGenerator.Generate(route3540concatenated, interpreter, languageGenerator);

            Assert.AreEqual(instructions.Count, instructionsConcatenated.Count);
            for (int idx = 0; idx < instructions.Count; idx++)
            {
                Assert.AreEqual(instructions[idx].Location.Center,
                    instructionsConcatenated[idx].Location.Center);
                Assert.AreEqual(instructions[idx].Text,
                    instructionsConcatenated[idx].Text);
            }
        }

        /// <summary>
        /// Issue with high-density resolved points. Routes are incorrectly skipping nodes.
        /// Some routes are not on known roads as a result.
        /// </summary>
        [Test]
        public void RoutingRegressionTest3()
        {
            OsmRoutingInterpreter interpreter = new OsmRoutingInterpreter();

            OsmTagsIndex tags_index = new OsmTagsIndex();

            // do the data processing.
            DynamicGraphRouterDataSource<SimpleWeighedEdge> memory_data =
                new DynamicGraphRouterDataSource<SimpleWeighedEdge>(tags_index);
            SimpleWeighedDataGraphProcessingTarget target_data = new SimpleWeighedDataGraphProcessingTarget(
                memory_data, interpreter, memory_data.TagsIndex, VehicleEnum.Car);
            XmlDataProcessorSource data_processor_source = new XmlDataProcessorSource(
                Assembly.GetExecutingAssembly().GetManifestResourceStream("OsmSharp.UnitTests.test_network.osm"));
            DataProcessorFilterSort sorter = new DataProcessorFilterSort();
            sorter.RegisterSource(data_processor_source);
            target_data.RegisterSource(sorter);
            target_data.Pull();

            IBasicRouter<SimpleWeighedEdge> basic_router = new DykstraRoutingLive(memory_data.TagsIndex);
            IRouter<RouterPoint> router = null; // create later over and over again.

            // build coordinates list of resolved points.
            List<GeoCoordinate> testPoints = new List<GeoCoordinate>();
            testPoints.Add(new GeoCoordinate(51.0582204, 3.7193524));
            testPoints.Add(new GeoCoordinate(51.0582199, 3.7194002));
            //testPoints.Add(new GeoCoordinate(51.0582178, 3.7195025));

            testPoints.Add(new GeoCoordinate(51.0581727, 3.7195833));
            testPoints.Add(new GeoCoordinate(51.0581483, 3.7195553));
            //testPoints.Add(new GeoCoordinate(51.0581219, 3.7195231));
            //testPoints.Add(new GeoCoordinate(51.0580965, 3.7194918));

            testPoints.Add(new GeoCoordinate(51.0581883, 3.7196617));
            testPoints.Add(new GeoCoordinate(51.0581628, 3.7196889));

            // build a matrix of routes between all points.
            OsmSharpRoute[][] referenceRoutes = new OsmSharpRoute[testPoints.Count][];
            int[] permuationArray = new int[testPoints.Count];
            for (int fromIdx = 0; fromIdx < testPoints.Count; fromIdx++)
            {
                permuationArray[fromIdx] = fromIdx;
                referenceRoutes[fromIdx] = new OsmSharpRoute[testPoints.Count];
                for (int toIdx = 0; toIdx < testPoints.Count; toIdx++)
                {
                    // create router from scratch.
                    router = new Router<SimpleWeighedEdge>(
                        memory_data, interpreter, basic_router);

                    // resolve points.
                    RouterPoint from = router.Resolve(VehicleEnum.Car, testPoints[fromIdx]);
                    RouterPoint to = router.Resolve(VehicleEnum.Car, testPoints[toIdx]);

                    // calculate route.
                    referenceRoutes[fromIdx][toIdx] = router.Calculate(VehicleEnum.Car, from, to);
                }
            }

            // resolve points in some order and compare the resulting routes.
            // they should be identical in length except for some numerical rounding errors.
            PermutationEnumerable<int> enumerator = new PermutationEnumerable<int>(
                permuationArray);
            foreach (int[] permutation in enumerator)
            {
                //int[] permutation = new int[] { 5, 0, 1, 2, 4, 3, 6, 7, 8 };

                //// incrementally resolve points.
                //// create router from scratch.
                //router = new Router<SimpleWeighedEdge>(memory_data, interpreter, basic_router);

                //// resolve in the order of the permutation.
                //RouterPoint[] resolvedPoints = new RouterPoint[permutation.Length];
                //resolvedPoints[permutation[0]] = router.Resolve(VehicleEnum.Car, testPoints[permutation[0]]);
                //for (int idx = 1; idx < permutation.Length; idx++)
                //{
                //    resolvedPoints[permutation[idx]] = router.Resolve(VehicleEnum.Car, testPoints[permutation[idx]]);

                //    for (int fromIdx = 0; fromIdx <= idx; fromIdx++)
                //    {
                //        for (int toIdx = 0; toIdx <= idx; toIdx++)
                //        {
                //            // calculate route.
                //            OsmSharpRoute route = router.Calculate(VehicleEnum.Car, 
                //                resolvedPoints[permutation[fromIdx]], resolvedPoints[permutation[toIdx]]);

                //            Assert.AreEqual(referenceRoutes[permutation[fromIdx]][permutation[toIdx]].TotalDistance, 
                //                route.TotalDistance, 0.1);
                //        }
                //    }
                //}

                // create router from scratch.
                router = new Router<SimpleWeighedEdge>(memory_data, interpreter, basic_router);

                // resolve in the order of the permutation.
                RouterPoint[] resolvedPoints = new RouterPoint[permutation.Length];
                for (int idx = 0; idx < permutation.Length; idx++)
                {
                    resolvedPoints[permutation[idx]] = router.Resolve(VehicleEnum.Car, testPoints[permutation[idx]]);
                }

                for (int fromIdx = 0; fromIdx < testPoints.Count; fromIdx++)
                {
                    for (int toIdx = 0; toIdx < testPoints.Count; toIdx++)
                    {
                        // calculate route.
                        OsmSharpRoute route = router.Calculate(VehicleEnum.Car, resolvedPoints[fromIdx], resolvedPoints[toIdx]);

                        Assert.AreEqual(referenceRoutes[fromIdx][toIdx].TotalDistance, route.TotalDistance, 0.1);
                    }
                }
            }
        }

        /// <summary>
        /// Issue with high-density resolved points. Routes are incorrectly skipping nodes.
        /// Some routes are not on known roads as a result.
        /// </summary>
        [Test]
        public void RoutingRegressionTest4()
        {
            OsmRoutingInterpreter interpreter = new OsmRoutingInterpreter();

            OsmTagsIndex tags_index = new OsmTagsIndex();

            // do the data processing.
            DynamicGraphRouterDataSource<SimpleWeighedEdge> memory_data =
                new DynamicGraphRouterDataSource<SimpleWeighedEdge>(tags_index);
            SimpleWeighedDataGraphProcessingTarget target_data = new SimpleWeighedDataGraphProcessingTarget(
                memory_data, interpreter, memory_data.TagsIndex, VehicleEnum.Car);
            XmlDataProcessorSource data_processor_source = new XmlDataProcessorSource(
                Assembly.GetExecutingAssembly().GetManifestResourceStream("OsmSharp.UnitTests.test_network.osm"));
            DataProcessorFilterSort sorter = new DataProcessorFilterSort();
            sorter.RegisterSource(data_processor_source);
            target_data.RegisterSource(sorter);
            target_data.Pull();

            IBasicRouter<SimpleWeighedEdge> basic_router = new DykstraRoutingLive(memory_data.TagsIndex);
            IRouter<RouterPoint> router = null; // create later over and over again.

            // build coordinates list of resolved points.
            List<GeoCoordinate> testPoints = new List<GeoCoordinate>();
            testPoints.Add(new GeoCoordinate(51.0581719, 3.7201622));
            testPoints.Add(new GeoCoordinate(51.0580439, 3.7202134));
            testPoints.Add(new GeoCoordinate(51.0580573, 3.7204378));
            testPoints.Add(new GeoCoordinate(51.0581862, 3.7203758));

            // build a matrix of routes between all points.
            OsmSharpRoute[][] referenceRoutes = new OsmSharpRoute[testPoints.Count][];
            int[] permuationArray = new int[testPoints.Count];
            for (int fromIdx = 0; fromIdx < testPoints.Count; fromIdx++)
            {
                permuationArray[fromIdx] = fromIdx;
                referenceRoutes[fromIdx] = new OsmSharpRoute[testPoints.Count];
                for (int toIdx = 0; toIdx < testPoints.Count; toIdx++)
                {
                    // create router from scratch.
                    router = new Router<SimpleWeighedEdge>(
                        memory_data, interpreter, basic_router);

                    // resolve points.
                    RouterPoint from = router.Resolve(VehicleEnum.Car, testPoints[fromIdx]);
                    RouterPoint to = router.Resolve(VehicleEnum.Car, testPoints[toIdx]);

                    // calculate route.
                    referenceRoutes[fromIdx][toIdx] = router.Calculate(VehicleEnum.Car, from, to);
                }
            }

            // resolve points in some order and compare the resulting routes.
            // they should be identical in length except for some numerical rounding errors.
            PermutationEnumerable<int> enumerator = new PermutationEnumerable<int>(
                permuationArray);
            foreach (int[] permutation in enumerator)
            {
                // create router from scratch.
                router = new Router<SimpleWeighedEdge>(memory_data, interpreter, basic_router);

                // resolve in the order of the permutation.
                RouterPoint[] resolvedPoints = new RouterPoint[permutation.Length];
                for (int idx = 0; idx < permutation.Length; idx++)
                {
                    resolvedPoints[permutation[idx]] = router.Resolve(VehicleEnum.Car, testPoints[permutation[idx]]);
                }

                for (int fromIdx = 0; fromIdx < testPoints.Count; fromIdx++)
                {
                    for (int toIdx = 0; toIdx < testPoints.Count; toIdx++)
                    {
                        // calculate route.
                        OsmSharpRoute route = router.Calculate(VehicleEnum.Car, resolvedPoints[fromIdx], resolvedPoints[toIdx]);

                        Assert.AreEqual(referenceRoutes[fromIdx][toIdx].TotalDistance, route.TotalDistance, 0.1);
                    }
                }
            }
        }
    }
}