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
using System.Text;
using NUnit.Framework;
using OsmSharp.Osm.Simple;
using OsmSharp.Osm.Filters;
using OsmSharp.Osm.Data.Core.Processor.List;
using OsmSharp.Osm.Data.Processor.Filter.Tags;

namespace OsmSharp.UnitTests.Data.Processing
{
    /// <summary>
    /// Tests the data processor tags filter.
    /// </summary>
    [TestFixture]
    public class TagsDataProcessorFilterTests
    {
        /// <summary>
        /// Tests a simple filter.
        /// </summary>
        [Test]
        public void TestFilteringProcessor()
        {
            SimpleNode node1 = new SimpleNode();
            node1.Id = 1;
            node1.Latitude = 1;
            node1.Longitude = 1;
            node1.Tags = new Dictionary<string, string>();
            node1.Tags.Add("key", "node1");

            SimpleNode node2 = new SimpleNode();
            node2.Id = 2;
            node2.Longitude = 2;
            node2.Latitude = 2;
            node2.Tags = new Dictionary<string, string>();
            node2.Tags.Add("key", "node2");
            
            SimpleNode node3 = new SimpleNode();
            node3.Id = 3;
            node3.Longitude = 3;
            node3.Latitude = 3;
            node3.Tags = new Dictionary<string, string>();
            node3.Tags.Add("key", "node3");

            SimpleWay way1 = new SimpleWay();
            way1.Id = 1;
            way1.Nodes = new List<long>();
            way1.Nodes.Add(1);
            way1.Nodes.Add(2);
            way1.Tags = new Dictionary<string, string>();
            way1.Tags.Add("key", "way1");

            SimpleWay way2 = new SimpleWay();
            way2.Id = 2;
            way2.Nodes = new List<long>();
            way2.Nodes.Add(2);
            way2.Nodes.Add(3);
            way2.Tags = new Dictionary<string, string>();
            way2.Tags.Add("key", "way2");

            SimpleRelation relation1 = new SimpleRelation();
            relation1.Id = 1;
            relation1.Members = new List<SimpleRelationMember>();
            relation1.Members.Add(new SimpleRelationMember()
            {
                MemberId = 1,
                MemberRole = "node",
                MemberType = SimpleRelationMemberType.Node
            });
            relation1.Members.Add(new SimpleRelationMember()
            {
                MemberId = 1,
                MemberRole = "way",
                MemberType = SimpleRelationMemberType.Way
            });
            relation1.Tags = new Dictionary<string, string>();
            relation1.Tags.Add("key", "relation1");

            SimpleRelation relation2 = new SimpleRelation();
            relation2.Id = 2;
            relation2.Members = new List<SimpleRelationMember>();
            relation2.Members.Add(new SimpleRelationMember()
            {
                MemberId = 2,
                MemberRole = "node",
                MemberType = SimpleRelationMemberType.Node
            });
            relation2.Members.Add(new SimpleRelationMember()
            {
                MemberId = 2,
                MemberRole = "way",
                MemberType = SimpleRelationMemberType.Way
            });
            relation2.Tags = new Dictionary<string, string>();
            relation2.Tags.Add("key", "relation2");

            Filter node1Filter = Filter.Match("key", "node1");
            Filter node2Filter = Filter.Match("key", "node2");
            Filter node3Filter = Filter.Match("key", "node3");
            Filter way1Filter = Filter.Match("key", "way1");
            Filter way2Filter = Filter.Match("key", "way2");
            Filter relation1Filter = Filter.Match("key", "relation1");
            Filter relation2Filter = Filter.Match("key", "relation2");

            List<SimpleOsmGeo> sourceList = new List<SimpleOsmGeo>();
            sourceList.Add(node1);
            sourceList.Add(node2);
            sourceList.Add(node3);
            sourceList.Add(way1);
            sourceList.Add(way2);
            sourceList.Add(relation1);
            sourceList.Add(relation2);

            // test positive node filtering.
            List<SimpleOsmGeo> targetList = new List<SimpleOsmGeo>();
            CollectionDataProcessorSource source = new CollectionDataProcessorSource(
                sourceList);
            TagsDataProcessorFilter filter = new TagsDataProcessorFilter(node1Filter, Filter.None(), Filter.None());
            CollectionDataProcessorTarget target = new CollectionDataProcessorTarget(
                targetList);
            filter.RegisterSource(source);
            target.RegisterSource(filter);
            target.Pull();

            Assert.AreEqual(1, targetList.Count);
            Assert.AreEqual(1, targetList[0].Id.Value);
            Assert.AreEqual(SimpleOsmGeoType.Node, targetList[0].Type);

            // test positive way filtering
            targetList = new List<SimpleOsmGeo>();
            source = new CollectionDataProcessorSource(
                sourceList);
            filter = new TagsDataProcessorFilter(Filter.None(), way1Filter, Filter.None());
            target = new CollectionDataProcessorTarget(
                targetList);
            filter.RegisterSource(source);
            target.RegisterSource(filter);
            target.Pull();

            Assert.AreEqual(1, targetList.Count);
            Assert.AreEqual(1, targetList[0].Id.Value);
            Assert.AreEqual(SimpleOsmGeoType.Way, targetList[0].Type);

            // test positive relation filtering
            targetList = new List<SimpleOsmGeo>();
            source = new CollectionDataProcessorSource(
                sourceList);
            filter = new TagsDataProcessorFilter(Filter.None(), Filter.None(), relation1Filter);
            target = new CollectionDataProcessorTarget(
                targetList);
            filter.RegisterSource(source);
            target.RegisterSource(filter);
            target.Pull();

            Assert.AreEqual(1, targetList.Count);
            Assert.AreEqual(1, targetList[0].Id.Value);
            Assert.AreEqual(SimpleOsmGeoType.Relation, targetList[0].Type);

            // test positive way filtering and keep nodes.
            targetList = new List<SimpleOsmGeo>();
            source = new CollectionDataProcessorSource(
                sourceList);
            filter = new TagsDataProcessorFilter(Filter.None(), way1Filter, Filter.None(), true, false);
            target = new CollectionDataProcessorTarget(
                targetList);
            filter.RegisterSource(source);
            target.RegisterSource(filter);
            target.Pull();

            Assert.AreEqual(3, targetList.Count);
            Assert.AreEqual(1, targetList[0].Id.Value);
            Assert.AreEqual(2, targetList[1].Id.Value);
            Assert.AreEqual(1, targetList[2].Id.Value);
            Assert.AreEqual(SimpleOsmGeoType.Node, targetList[0].Type);
            Assert.AreEqual(SimpleOsmGeoType.Node, targetList[1].Type);
            Assert.AreEqual(SimpleOsmGeoType.Way, targetList[2].Type);

            // test positive relation filtering
            targetList = new List<SimpleOsmGeo>();
            source = new CollectionDataProcessorSource(
                sourceList);
            filter = new TagsDataProcessorFilter(Filter.None(), Filter.None(), relation1Filter, false, true);
            target = new CollectionDataProcessorTarget(
                targetList);
            filter.RegisterSource(source);
            target.RegisterSource(filter);
            target.Pull();

            Assert.AreEqual(3, targetList.Count);
            Assert.AreEqual(1, targetList[0].Id.Value);
            Assert.AreEqual(1, targetList[1].Id.Value);
            Assert.AreEqual(1, targetList[2].Id.Value);
            Assert.AreEqual(SimpleOsmGeoType.Node, targetList[0].Type);
            Assert.AreEqual(SimpleOsmGeoType.Way, targetList[1].Type);
            Assert.AreEqual(SimpleOsmGeoType.Relation, targetList[2].Type);

            // test positive relation filtering
            targetList = new List<SimpleOsmGeo>();
            source = new CollectionDataProcessorSource(
                sourceList);
            filter = new TagsDataProcessorFilter(Filter.None(), Filter.None(), relation1Filter, true, true);
            target = new CollectionDataProcessorTarget(
                targetList);
            filter.RegisterSource(source);
            target.RegisterSource(filter);
            target.Pull();

            Assert.AreEqual(4, targetList.Count);
            Assert.AreEqual(1, targetList[0].Id.Value);
            Assert.AreEqual(2, targetList[1].Id.Value);
            Assert.AreEqual(1, targetList[2].Id.Value);
            Assert.AreEqual(1, targetList[3].Id.Value);
            Assert.AreEqual(SimpleOsmGeoType.Node, targetList[0].Type);
            Assert.AreEqual(SimpleOsmGeoType.Node, targetList[1].Type);
            Assert.AreEqual(SimpleOsmGeoType.Way, targetList[2].Type);
            Assert.AreEqual(SimpleOsmGeoType.Relation, targetList[3].Type);

            // create a new relation that has a relation as a child.
            SimpleRelation relation3 = new SimpleRelation();
            relation3.Id = 3;
            relation3.Members = new List<SimpleRelationMember>();
            relation3.Members.Add(new SimpleRelationMember()
            {
                MemberId = 1,
                MemberRole = "relation",
                MemberType = SimpleRelationMemberType.Relation
            });
            relation3.Tags = new Dictionary<string, string>();
            relation3.Tags.Add("key", "relation3");
            sourceList.Add(relation3);
            Filter relation3Filter = Filter.Match("key", "relation3");

            // test positive relation filtering
            targetList = new List<SimpleOsmGeo>();
            source = new CollectionDataProcessorSource(
                sourceList);
            filter = new TagsDataProcessorFilter(Filter.None(), Filter.None(), relation3Filter, true, true);
            target = new CollectionDataProcessorTarget(
                targetList);
            filter.RegisterSource(source);
            target.RegisterSource(filter);
            target.Pull();

            Assert.AreEqual(5, targetList.Count);
            Assert.AreEqual(1, targetList[0].Id.Value);
            Assert.AreEqual(2, targetList[1].Id.Value);
            Assert.AreEqual(1, targetList[2].Id.Value);
            Assert.AreEqual(1, targetList[3].Id.Value);
            Assert.AreEqual(3, targetList[4].Id.Value);
            Assert.AreEqual(SimpleOsmGeoType.Node, targetList[0].Type);
            Assert.AreEqual(SimpleOsmGeoType.Node, targetList[1].Type);
            Assert.AreEqual(SimpleOsmGeoType.Way, targetList[2].Type);
            Assert.AreEqual(SimpleOsmGeoType.Relation, targetList[3].Type);
            Assert.AreEqual(SimpleOsmGeoType.Relation, targetList[4].Type);
        }
    }
}
