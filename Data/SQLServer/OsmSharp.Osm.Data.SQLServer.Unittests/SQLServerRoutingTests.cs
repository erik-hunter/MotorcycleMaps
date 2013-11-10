// OsmSharp - OpenStreetMap tools & library.
//
// Copyright (C) 2013 Abelshausen Ben
//                    Alexander Sinitsyn
//                    Simon Hughes
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
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using OsmSharp.Osm.Data.SQLServer.SimpleSchema;
using OsmSharp.Osm.Data.SQLServer.SimpleSchema.Processor;
using OsmSharp.Osm.Data.XML.Processor;
using OsmSharp.Routing;
using OsmSharp.Routing.Graph.DynamicGraph.PreProcessed;
using OsmSharp.Routing.Graph.Router.Dykstra;
using OsmSharp.Routing.Osm.Data;
using OsmSharp.Routing.Osm.Interpreter;
using OsmSharp.Routing.Route;
using OsmSharp.Tools.Math.Geo;

namespace OsmSharp.Osm.Data.SQLServer.Unittests
{
    /// <summary>
    /// Contains some simple SQLServer routing tests.
    /// </summary>
    [TestFixture]
    public class SQLServerRoutingTests
    {
        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            Tools.Output.OutputStreamHost.RegisterOutputStream(new Tools.Output.ConsoleOutputStream());
        }

        /// <summary>
        /// A simple SQLServer routing regression test.
        /// </summary>
        [Test]
        public void SQLServerRoutingRegressionTests1()
        {
            // the connectionstring.
            const string connectionString = @"Server=.;User Id=OsmSharp;Password=OsmSharp;Database=OsmData.Test;Application Name=SQLServerRoutingRegressionTests1;";

            // drop whatever data is there.
            var sqlConnection = new SqlConnection(connectionString);
            sqlConnection.Open();
            SimpleSchema.SchemaTools.SQLServerSimpleSchemaTools.Remove(sqlConnection);
            sqlConnection.Close();

            // create the source from the osm file.
            var xmlSource = new XmlDataProcessorSource(Assembly.GetExecutingAssembly().GetManifestResourceStream("OsmSharp.Osm.Data.SQLServer.Unittests.Data.ukraine1.osm"));

            // create the SQLServer processor target.
            var testTarget = new SQLServerSimpleSchemaDataProcessorTarget(connectionString, true);
            testTarget.RegisterSource(xmlSource); // register the source.
            testTarget.Pull(); // pull the data from source to target.

            var source = new SQLServerSimpleSchemaSource(connectionString);
            var tagsIndex = new OsmTagsIndex();
            var interpreter = new OsmRoutingInterpreter();
            var routingData = new OsmSourceRouterDataSource(interpreter, tagsIndex, source, VehicleEnum.Car);

            IRouter<RouterPoint> router = new Router<PreProcessedEdge>(routingData, interpreter, new DykstraRoutingPreProcessed(routingData.TagsIndex));
            RouterPoint point1 = router.Resolve(VehicleEnum.Car, new GeoCoordinate(50.3150034243338, 34.8784106812928));
            RouterPoint point2 = router.Resolve(VehicleEnum.Car, new GeoCoordinate(50.3092549484347, 34.8894929841615));
            OsmSharpRoute route = router.Calculate(VehicleEnum.Car, point1, point2);

            Assert.IsNotNull(route);
        }

        //[Test]
        [Ignore]
        public void UKTest()
        {
            const string connectionString = @"Server=.;User Id=OsmSharp;Password=OsmSharp;Database=OsmData;Application Name=UKTest;";
            var source = new SQLServerSimpleSchemaSource(connectionString);

            // keeps a memory-efficient version of the osm-tags.
            var tagsIndex = new OsmTagsIndex();

            // Creates a routing interpreter. (used to translate osm-tags into a routable network
            var interpreter = new OsmRoutingInterpreter();

            // Create OsmSourceRouterDataSource
            var routingData = new OsmSourceRouterDataSource(interpreter, tagsIndex, source, VehicleEnum.Car);

            // Create Router
            var router = new Router<PreProcessedEdge>(routingData, interpreter, new DykstraRoutingPreProcessed(routingData.TagsIndex));

            var a = new GeoCoordinate(52.77881, -2.38172);
            var b = new GeoCoordinate(52.80463, -2.12204);

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            // This section makes it work. Comment out to make it load data dynamically, which unfortunately does not work yet.
            //Tools.Output.OutputStreamHost.WriteLine("LoadMissingIfNeeded");
            //routingData.LoadMissingIfNeeded(new GeoCoordinateBox(a, b).Resize(.1));

            Tools.Output.OutputStreamHost.WriteLine("Resolve Point 1");
            var point1 = router.Resolve(VehicleEnum.Car, a);    // Newport
            Assert.NotNull(point1);

            Tools.Output.OutputStreamHost.WriteLine("Resolve Point 2");
            var point2 = router.Resolve(VehicleEnum.Car, b);    // Stafford - Google: 13.7 miles (25 mins)
            Assert.NotNull(point2);

            Tools.Output.OutputStreamHost.WriteLine("Calculating route");
            var route = router.Calculate(VehicleEnum.Car, point1, point2);

            stopwatch.Stop();
            Tools.Output.OutputStreamHost.WriteLine(string.Format("Time elapsed: {0}", stopwatch.Elapsed));

            Assert.NotNull(route);
            Tools.Output.OutputStreamHost.WriteLine("Route found");
            route.SaveAsGpx(new FileInfo("f:\\route.gpx"));
        }

    }
}
