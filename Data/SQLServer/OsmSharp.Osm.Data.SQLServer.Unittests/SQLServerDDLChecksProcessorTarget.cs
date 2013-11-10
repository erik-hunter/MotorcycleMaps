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
using System.Collections.Generic;
using System.Data.SqlClient;
using OsmSharp.Osm.Data.Core.Processor;
using OsmSharp.Osm.Simple;

namespace OsmSharp.Osm.Data.SQLServer.Unittests
{
    public class SQLServerDDLChecksProcessorTarget : DataProcessorTarget
    {
        private SqlConnection _connection;
        private readonly string _connectionString;

        public int NodeUsr;
        public int NodeTagsKey;
        public int NodeTagsValue;
        
        public int WayUsr;
        public int WayTagsKey;
        public int WayTagsValue;

        public int RelationUsr;
        public int RelationTagsKey;
        public int RelationTagsValue;
        public int RelationMemberRole;

        public SQLServerDDLChecksProcessorTarget(string connectionString)
        {
            _connectionString = connectionString;
        }
        public override void Initialize()
        {
            _connection = new SqlConnection(_connectionString);
            _connection.Open();
        }

        public override void ApplyChange(SimpleChangeSet change)
        {
            throw new NotSupportedException();
        }

        public override void AddNode(SimpleNode node)
        {
            if(!node.Id.HasValue)
                return;

            MaxStringLength(node.UserName, ref NodeUsr);

            if (node.Tags == null)
                return;

            foreach(KeyValuePair<string, string> tag in node.Tags)
            {
                MaxStringLength(tag.Key, ref NodeTagsKey);
                MaxStringLength(tag.Value, ref NodeTagsValue);
            }
        }

        public override void AddWay(SimpleWay way)
        {
            if(!way.Id.HasValue)
                return;

            MaxStringLength(way.UserName, ref WayUsr);

            if (way.Tags == null)
                return;

            foreach(KeyValuePair<string, string> tag in way.Tags)
            {
                MaxStringLength(tag.Key, ref WayTagsKey);
                MaxStringLength(tag.Value, ref WayTagsValue);
            }
        }

        public override void AddRelation(SimpleRelation relation)
        {
            if(!relation.Id.HasValue)
                return;

            MaxStringLength(relation.UserName, ref RelationUsr);

            if(relation.Tags != null)
            {
                foreach(KeyValuePair<string, string> tag in relation.Tags)
                {
                    MaxStringLength(tag.Key, ref RelationTagsKey);
                    MaxStringLength(tag.Value, ref RelationTagsValue);
                }
            }

            if(relation.Members != null)
            {
                foreach (SimpleRelationMember member in relation.Members)
                {
                    MaxStringLength(member.MemberRole, ref RelationMemberRole);
                }
            }
        }

        private static void MaxStringLength(string value, ref int length)
        {
            if(value == null)
                return;

            int len = value.Length;
            if(length < len)
                length = len;
        }
   
        public override void Close()
        {
            if(_connection != null)
            {
                _connection.Close();
                _connection.Dispose();
            }
            _connection = null;
        }
    }
}