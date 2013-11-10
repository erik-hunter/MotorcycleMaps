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
using System.IO;
using System.Reflection;
using NUnit.Framework;
using OsmSharp.Osm.Data.PBF.Raw.Processor;
using OsmSharp.Osm.Data.SQLServer.SimpleSchema.SchemaTools;
using OsmSharp.Osm.Data.XML.Processor;
using OsmSharp.Osm.Simple;


namespace OsmSharp.Osm.Data.SQLServer.Unittests
{
    [TestFixture]
    public class SQLServerDDLChecks
    {
        private SQLServerDDLChecksProcessorTarget _testTarget;

        [TestFixtureSetUp]
        public void FixtureSetUp()
        {
            // Arrange
            const string connectionString = @"Server=.;User Id=OsmSharp;Password=OsmSharp;Database=OsmData;";

            var sqlConnection = new SqlConnection(connectionString);
            sqlConnection.Open();

            var source = new XmlDataProcessorSource(Assembly.GetExecutingAssembly().GetManifestResourceStream("OsmSharp.Osm.Data.SQLServer.Unittests.Data.ukraine1.osm"));
            //var source = new PBFDataProcessorSource(new FileInfo("C:\\great-britain-latest.osm.pbf").OpenRead());

            // Act
            _testTarget = new SQLServerDDLChecksProcessorTarget(connectionString);
            _testTarget.RegisterSource(source);
            _testTarget.Pull();
        }

        [Test]
        public void NodeUsr()
        {
            Console.WriteLine(_testTarget.NodeUsr);
            Assert.LessOrEqual(_testTarget.NodeUsr, SQLServerSimpleSchemaConstants.NodeUsr);
        }

        [Test]
        public void NodeTagsKey()
        {
            Console.WriteLine(_testTarget.NodeTagsKey);
            Assert.LessOrEqual(_testTarget.NodeTagsKey, SQLServerSimpleSchemaConstants.NodeTagsKey);
        }

        [Test]
        public void NodeTagsValue()
        {
            Console.WriteLine(_testTarget.NodeTagsValue);
            Assert.LessOrEqual(_testTarget.NodeTagsValue, SQLServerSimpleSchemaConstants.NodeTagsValue);
        }

        [Test]
        public void WayUsr()
        {
            Console.WriteLine(_testTarget.WayUsr);
            Assert.LessOrEqual(_testTarget.WayUsr, SQLServerSimpleSchemaConstants.WayUsr);
        }

        [Test]
        public void WayTagsKey()
        {
            Console.WriteLine(_testTarget.WayTagsKey);
            Assert.LessOrEqual(_testTarget.WayTagsKey, SQLServerSimpleSchemaConstants.WayTagsKey);
        }

        [Test]
        public void WayTagsValue()
        {
            Console.WriteLine(_testTarget.WayTagsValue);
            Assert.LessOrEqual(_testTarget.WayTagsValue, SQLServerSimpleSchemaConstants.WayTagsValue);
        }

        [Test]
        public void RelationUsr()
        {
            Console.WriteLine(_testTarget.RelationUsr);
            Assert.LessOrEqual(_testTarget.RelationUsr, SQLServerSimpleSchemaConstants.RelationUsr);
        }

        [Test]
        public void RelationTagsKey()
        {
            Console.WriteLine(_testTarget.RelationTagsKey);
            Assert.LessOrEqual(_testTarget.RelationTagsKey, SQLServerSimpleSchemaConstants.RelationTagsKey);
        }

        [Test]
        public void RelationTagsValue()
        {
            Console.WriteLine(_testTarget.RelationTagsValue);
            Assert.LessOrEqual(_testTarget.RelationTagsValue, SQLServerSimpleSchemaConstants.RelationTagsValue);
        }

        [Test]
        public void RelationMemberRole()
        {
            Console.WriteLine(_testTarget.RelationMemberRole);
            Assert.LessOrEqual(_testTarget.RelationMemberRole, SQLServerSimpleSchemaConstants.RelationMemberRole);
        }

        [Test]
        public void RelationMemberTypeStringLengths()
        {
            Console.WriteLine(SimpleRelationMemberType.Node.ToString().Length);
            Assert.LessOrEqual(SimpleRelationMemberType.Node.ToString().Length, SQLServerSimpleSchemaConstants.RelationMemberType);

            Console.WriteLine(SimpleRelationMemberType.Relation.ToString().Length);
            Assert.LessOrEqual(SimpleRelationMemberType.Relation.ToString().Length, SQLServerSimpleSchemaConstants.RelationMemberType);

            Console.WriteLine(SimpleRelationMemberType.Way.ToString().Length);
            Assert.LessOrEqual(SimpleRelationMemberType.Way.ToString().Length, SQLServerSimpleSchemaConstants.RelationMemberType);
        }
    }
}
