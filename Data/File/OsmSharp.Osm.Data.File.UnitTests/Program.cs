// OsmSharp - OpenStreetMap tools & library.
//
// Copyright (C) 2013 Simon Hughes
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
using System.Diagnostics;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using OsmSharp.Osm.Data.XML.Processor;
using OsmSharp.Routing;
using OsmSharp.Routing.Graph;
using OsmSharp.Routing.Graph.DynamicGraph.PreProcessed;
using OsmSharp.Routing.Graph.Router;
using OsmSharp.Routing.Graph.Router.Dykstra;
using OsmSharp.Routing.Graph.Serialization.v1;
using OsmSharp.Routing.Osm.Data.Processing;
using OsmSharp.Routing.Osm.Interpreter;
using OsmSharp.Tools.Math.Geo;

// ReSharper disable EmptyGeneralCatchClause

namespace OsmSharp.Osm.Data.File.UnitTests
{
    [TestFixture]
    public class Osm2OsmSharp
    {
        private readonly MemoryStream _stream = new MemoryStream();

        [Test]
        public void Save_Load_Route()
        {
            var data = Convert();
            Save(data);
            var loaded = Load();
            Route(loaded);
        }

        private DynamicGraphRouterDataSource<PreProcessedEdge> Convert()
        {
            var assemblyWithUkraineOsm = Assembly.LoadFrom("OsmSharp.Osm.Data.SQLServer.Unittests.DLL");
            var source = new XmlDataProcessorSource(assemblyWithUkraineOsm.GetManifestResourceStream("OsmSharp.Osm.Data.SQLServer.Unittests.Data.ukraine1.osm"));
            var interpreter = new OsmRoutingInterpreter();
            var data = new DynamicGraphRouterDataSource<PreProcessedEdge>(new OsmTagsIndex());
            var targetData = new PreProcessedDataGraphProcessingTarget(data, interpreter, data.TagsIndex, VehicleEnum.Car);
            targetData.RegisterSource(source);
            targetData.Pull();

            return data;
        }

        private void Save(DynamicGraphRouterDataSource<PreProcessedEdge> data)
        {
            var serializer = new V1RoutingSerializer();
            _stream.Seek(0, SeekOrigin.Begin);
            serializer.Serialize(_stream, data);
        }

        private IBasicRouterDataSource<PreProcessedEdge> Load()
        {
            var serializer = new V1RoutingSerializer();
            _stream.Seek(0, SeekOrigin.Begin);
            return serializer.Deserialize(_stream, false);
        }

        private void Route(IBasicRouterDataSource<PreProcessedEdge> data)
        {
            var interpreter = new OsmRoutingInterpreter();
            var router = new Router<PreProcessedEdge>(data, interpreter, new DykstraRoutingPreProcessed(data.TagsIndex));

            Console.WriteLine("Resolve Point 1");
            RouterPoint point1 = router.Resolve(VehicleEnum.Car, new GeoCoordinate(50.3150034243338, 34.8784106812928));
            Assert.NotNull(point1);

            Console.WriteLine("Resolve Point 2");
            RouterPoint point2 = router.Resolve(VehicleEnum.Car, new GeoCoordinate(50.3092549484347, 34.8894929841615));
            Assert.NotNull(point2);

            Console.WriteLine("Calculating route");
            var route = router.Calculate(VehicleEnum.Car, point1, point2);

            Assert.NotNull(route);
        }

        //[Test]
        [Ignore]
        public void UKTest()
        {
            var stopwatch = new Stopwatch();

            stopwatch.Start();
            
            var serializer = new V1RoutingSerializer();
            IBasicRouterDataSource<PreProcessedEdge> data = null;
            using(var stream = new FileStream(@"f:\great-britain.oss", FileMode.Open, FileAccess.Read))
                data = serializer.Deserialize(stream, false);

            var interpreter = new OsmRoutingInterpreter();
            var router = new Router<PreProcessedEdge>(data, interpreter, new DykstraRoutingPreProcessed(data.TagsIndex));
            var a = new GeoCoordinate(52.77881, -2.38172);
            var b = new GeoCoordinate(52.80463, -2.12204);

            stopwatch.Stop();
            Console.WriteLine(string.Format("Time to load UK into memory: {0}", stopwatch.Elapsed));

            stopwatch.Restart();

            Console.WriteLine("Resolve Point 1");
            var point1 = router.Resolve(VehicleEnum.Car, a);    // Newport
            Assert.NotNull(point1);

            Console.WriteLine("Resolve Point 2");
            var point2 = router.Resolve(VehicleEnum.Car, b);    // Stafford - Google: 13.7 miles (25 mins)
            Assert.NotNull(point2);

            Console.WriteLine("Calculating route");
            var route = router.Calculate(VehicleEnum.Car, point1, point2);

            stopwatch.Stop();
            Console.WriteLine(string.Format("Time elapsed: {0}", stopwatch.Elapsed));

            Assert.NotNull(route);
            Tools.Output.OutputStreamHost.WriteLine("Route found");
            route.SaveAsGpx(new FileInfo("f:\\route.gpx"));
        }
    }
}
