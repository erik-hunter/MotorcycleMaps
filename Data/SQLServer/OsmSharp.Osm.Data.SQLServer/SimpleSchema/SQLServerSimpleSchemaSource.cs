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
using System.Collections.Generic;
using System.Linq;
using OsmSharp.Osm.Data.SQLServer.SimpleSchema.SchemaTools;
using OsmSharp.Osm.Factory;
using OsmSharp.Osm.Filters;
using OsmSharp.Osm.Tiles;
using OsmSharp.Tools.Math.Geo;
using System.Data.SqlClient;

namespace OsmSharp.Osm.Data.SQLServer.SimpleSchema
{
    /// <summary>
    /// Allows a version of the OsmSharp simple schema to be queried in SQLServer.
    /// 
    /// http://www.osmsharp.com/wiki/simpleschema
    /// </summary>
    public class SQLServerSimpleSchemaSource : IDataSourceReadOnly, IDisposable
    {
        private const int Batch = 1000;

        /// <summary>
        /// Holds the connection string.
        /// </summary>
        private readonly string _connectionString;

        /// <summary>
        /// The id of this datasource.
        /// </summary>
        private readonly Guid _id;

        /// <summary>
        /// Flag that indicates if the schema needs to be created if not present.
        /// </summary>
        private readonly bool _createAndDetectSchema;

        /// <summary>
        /// Creates a new simple schema datasource.
        /// </summary>
        /// <param name="connectionString"></param>
        public SQLServerSimpleSchemaSource(string connectionString)
        {
            _connectionString = connectionString;
            _id = Guid.NewGuid();
            _createAndDetectSchema = false;
        }

        /// <summary>
        /// Creates a new simple schema datasource.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="createSchema">Creates all the needed tables if true.</param>
        public SQLServerSimpleSchemaSource(string connectionString, bool createSchema)
        {
            _connectionString = connectionString;
            _id = Guid.NewGuid();
            _createAndDetectSchema = createSchema;
        }

        /// <summary>
        /// Holds the connection to the SQLServer db.
        /// </summary>
        private SqlConnection _connection;

        /// <summary>
        /// Creates a new/gets the existing connection.
        /// </summary>
        /// <returns></returns>
        private SqlConnection CreateConnection()
        {
            if (_connection == null)
            {
                _connection = new SqlConnection(_connectionString);
                _connection.Open();

                if (_createAndDetectSchema)
                { // creates or detects the tables.
                    SQLServerSimpleSchemaTools.CreateAndDetect(_connection);
                }
            }
            return _connection;
        }

        #region IDataSourceReadOnly Members

        /// <summary>
        /// Not supported.
        /// </summary>
        public GeoCoordinateBox BoundingBox
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Returns the name.
        /// </summary>
        public string Name
        {
            get
            {
                return "SQLServer Simple Schema Source";
            }
        }

        /// <summary>
        /// Returns the id.
        /// </summary>
        public Guid Id
        {
            get
            {
                return _id;
            }
        }

        /// <summary>
        /// Returns false; database sources have no bounding box.
        /// </summary>
        public bool HasBoundinBox
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Return true; source is readonly.
        /// </summary>
        public bool IsReadOnly
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Returns the node with the given id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Node GetNode(long id)
        {
            var nodes = GetNodes(new List<long>(new[] { id }));
            return nodes.Count > 0 ? nodes[0] : null;
        }

        /// <summary>
        /// Returns all the nodes with the given ids.
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public IList<Node> GetNodes(IList<long> ids)
        {
            if (ids.Count <= 0)
                return new List<Node>();

            // initialize connection.
            SqlConnection con = CreateConnection();
            // STEP 1: query nodes table.

            var nodes = new Dictionary<long, Node>();
            for (int idx1000 = 0; idx1000 <= ids.Count / Batch; idx1000++)
            {
                int startIdx = idx1000 * Batch;
                int stopIdx = Math.Min((idx1000 + 1) * Batch, ids.Count);
                string idsString = ConstructIdList(ids,startIdx,stopIdx);
                if (idsString.Length <= 0)
                    continue;

                var sql = string.Format("SELECT id,latitude,longitude,changeset_id,[timestamp],[version] FROM node WHERE id IN ({0})", idsString);
                        
                var com = new SqlCommand(sql) { Connection = con };
                var reader = com.ExecuteReader();
                var nodeIds = new List<long>();
                while (reader.Read())
                {
                    // load/parse data.
                    long id = reader.GetInt64(0);

                    if (nodes.ContainsKey(id))
                        continue;

                    // create node.
                    Node node = OsmBaseFactory.CreateNode(id);
                    node.Version = reader.GetInt32(5);
                    node.TimeStamp = reader.GetDateTime(4);
                    node.ChangeSetId = reader.GetInt64(3);
                    node.Coordinate = new GeoCoordinate(reader.GetInt32(1) / 10000000.0, reader.GetInt32(2) / 10000000.0);

                    nodes.Add(node.Id, node);
                    nodeIds.Add(node.Id);
                }
                reader.Close();
            }

            // STEP2: Load all node tags.
            LoadNodeTags(nodes);

            return nodes.Values.ToList();
        }

        /// <summary>
        /// Returns the relation with the given id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Relation GetRelation(long id)
        {
            // TODO: implement this
            return null;
        }

        /// <summary>
        /// Returns all the relations with the given ids.
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public IList<Relation> GetRelations(IList<long> ids)
        {
            // TODO: implement this
            return new List<Relation>();
        }

        /// <summary>
        /// Returns all relations for the given objects.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public IList<Relation> GetRelationsFor(OsmBase obj)
        {
            // TODO: implement this
            return new List<Relation>();
        }

        /// <summary>
        /// Returns the way with the given id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Way GetWay(long id)
        {
            IList<Way> ways = GetWays(new List<long>(new[] { id }));
            if (ways.Count > 0)
            {
                return ways[0];
            }
            return null;
        }

        /// <summary>
        /// Returns all ways with the given ids.
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public IList<Way> GetWays(IList<long> ids)
        {
            return GetWays(ids, null);
        }

        /// <summary>
        /// Returns all ways but use the existing nodes to fill the Nodes-lists.
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="nodes"></param>
        /// <returns></returns>
        private IList<Way> GetWays(IList<long> ids, Dictionary<long,Node> nodes)
        {
            if (ids.Count > 0)
            {
                var con = CreateConnection();

                // STEP2: Load ways.
                var ways = new Dictionary<long, Way>();
                SqlDataReader reader;
                for (int idx1000 = 0; idx1000 <= ids.Count / Batch; idx1000++)
                {
                    int startIdx = idx1000 * Batch;
                    int stopIdx = Math.Min((idx1000 + 1) * Batch, ids.Count);

                    string idsString = ConstructIdList(ids,startIdx,stopIdx);
                    if (idsString.Length <= 0)
                        continue;

                    var sql = string.Format("SELECT id,changeset_id,[timestamp],[version] FROM way WHERE id IN ({0})", idsString);
                    var com = new SqlCommand(sql) { Connection = con };
                    reader = com.ExecuteReader();
                    while (reader.Read())
                    {
                        // create way.
                        Way way = OsmBaseFactory.CreateWay(reader.GetInt64(0));
                        way.Version = reader.GetInt32(3);
                        way.TimeStamp = reader.GetDateTime(2);
                        way.ChangeSetId = reader.GetInt64(1);

                        ways.Add(way.Id, way);
                    }
                    reader.Close();
                }

                //STEP3: Load all node-way relations
                var missingNodeIds = new List<long>();
                for (int idx1000 = 0; idx1000 <= ids.Count / Batch; idx1000++)
                {
                    int startIdx = idx1000 * Batch;
                    int stopIdx = Math.Min((idx1000 + 1) * Batch, ids.Count);

                    string idsString = ConstructIdList(ids, startIdx, stopIdx);
                    if (idsString.Length <= 0)
                        continue;

                    var sql = string.Format("SELECT node_id FROM way_nodes WHERE (way_id IN ({0})) ORDER BY sequence_id", idsString);
                    var com = new SqlCommand(sql) { Connection = con };
                    reader = com.ExecuteReader();
                    while (reader.Read())
                    {
                        long nodeID = reader.GetInt64(0);
                        if (nodes == null || !nodes.ContainsKey(nodeID))
                        {
                            missingNodeIds.Add(nodeID);
                        }
                    }
                    reader.Close();
                }

                //STEP4: Load all missing nodes.
                IList<Node> missingNodes = GetNodes(missingNodeIds);
                var wayNodes = new Dictionary<long, Node>();
                if (nodes != null)
                {
                    wayNodes = new Dictionary<long, Node>(nodes);
                }
                foreach (Node node in missingNodes)
                {
                    wayNodes.Add(node.Id, node);
                }

                //STEP5: assign nodes to way.
                for (int idx1000 = 0; idx1000 <= ids.Count / Batch; idx1000++)
                {
                    int startIdx = idx1000 * Batch;
                    int stopIdx = Math.Min((idx1000 + 1) * Batch, ids.Count);

                    string idsString = ConstructIdList(ids, startIdx, stopIdx);
                    if (idsString.Length <= 0)
                        continue;

                    var sql = string.Format("SELECT way_id,node_id FROM way_nodes WHERE (way_id IN ({0})) ORDER BY sequence_id", idsString);
                    var com = new SqlCommand(sql) { Connection = con };
                    reader = com.ExecuteReader();
                    while (reader.Read())
                    {
                        long id = reader.GetInt64(0);
                        long nodeID = reader.GetInt64(1);

                        Node wayNode;
                        if (wayNodes.TryGetValue(nodeID, out wayNode))
                        {
                            Way way;
                            if (ways.TryGetValue(id, out way))
                            {
                                way.Nodes.Add(wayNode);
                            }
                        }
                    }
                    reader.Close();
                }


                //STEP4: Load all tags.
                for (int idx1000 = 0; idx1000 <= ids.Count / Batch; idx1000++)
                {
                    int startIdx = idx1000 * Batch;
                    int stopIdx = Math.Min((idx1000 + 1) * Batch, ids.Count);

                    string idsString = ConstructIdList(ids, startIdx, stopIdx);
                    if (idsString.Length <= 0)
                        continue;

                    var sql = string.Format("SELECT way_id,[key],value FROM way_tags WHERE way_id IN ({0})", idsString);
                    var com = new SqlCommand(sql) { Connection = con };
                    reader = com.ExecuteReader();
                    while (reader.Read())
                    {
                        long id = reader.GetInt64(0);
                        string key = reader.GetString(1);
                        object valueObject = reader[2];
                        string value = string.Empty;
                        if (valueObject != null && valueObject != DBNull.Value)
                        {
                            value = (string)valueObject;
                        }

                        Way way;
                        if (ways.TryGetValue(id, out way))
                        {
                            way.Tags.Add(key, value);
                        }
                    }
                    reader.Close();
                }

                return ways.Values.ToList();
            }
            return new List<Way>();
        }

        private IList<Way> GetWaysFromTiles(Dictionary<long, Node> nodes, string nodeIdSql)
        {
            if(nodes.Count <= 0)
                return new List<Way>();

            var con = CreateConnection();

            // Load ways
            var ways = new Dictionary<long, Way>();
            var wayIdSql = string.Format("SELECT way_id FROM way_nodes WHERE node_id IN ({0})", nodeIdSql);
            var sql = string.Format("SELECT id,changeset_id,[timestamp],[version] FROM way WHERE id IN ({0})", wayIdSql);
            var com = new SqlCommand(sql) { Connection = con };
            SqlDataReader reader = com.ExecuteReader();
            while(reader.Read())
            {
                // create way.
                Way way = OsmBaseFactory.CreateWay(reader.GetInt64(0));
                way.Version = reader.GetInt32(3);
                way.TimeStamp = reader.GetDateTime(2);
                way.ChangeSetId = reader.GetInt64(1);

                ways.Add(way.Id, way);
            }
            reader.Close();

            // Load way tags
            sql = string.Format("SELECT way_id,[key],value FROM way_tags WHERE way_id IN ({0})", wayIdSql);
            com = new SqlCommand(sql) { Connection = con };
            reader = com.ExecuteReader();
            while(reader.Read())
            {
                long id = reader.GetInt64(0);
                string key = reader.GetString(1);
                object valueObject = reader[2];
                string value = string.Empty;
                if(valueObject != null && valueObject != DBNull.Value)
                {
                    value = (string)valueObject;
                }

                Way way;
                if(ways.TryGetValue(id, out way))
                {
                    way.Tags.Add(key, value);
                }
            }
            reader.Close();

            // Assign nodes to way.
            sql = string.Format("SELECT way_id,node_id FROM way_nodes WHERE way_id IN ({0}) ORDER BY sequence_id", wayIdSql);
            com = new SqlCommand(sql) { Connection = con };
            reader = com.ExecuteReader();
            while(reader.Read())
            {
                long id = reader.GetInt64(0);
                long nodeID = reader.GetInt64(1);

                Node wayNode;
                if(nodes.TryGetValue(nodeID, out wayNode))
                {
                    Way way;
                    if(ways.TryGetValue(id, out way))
                    {
                        way.Nodes.Add(wayNode);
                    }
                }
            }
            reader.Close();

            return ways.Values.ToList();
        }

        /// <summary>
        /// Returns all ways using the given node.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public IList<Way> GetWaysFor(Node node)
        {
            var nodes = new Dictionary<long,Node> { { node.Id, node } };
            return GetWaysForNodes(nodes);
        }

        /// <summary>
        /// Returns all ways using any of the given nodes.
        /// </summary>
        /// <param name="nodes"></param>
        /// <returns></returns>
        public IList<Way> GetWaysForNodes(Dictionary<long,Node> nodes)
        {
            if (nodes.Count > 0)
            {
                SqlConnection con = CreateConnection();

                var wayIds = new List<long>();
                for (int idx100 = 0; idx100 <= nodes.Count / 100; idx100++)
                {
                    // STEP1: Load ways that exist for the given nodes.
                    int startIdx = idx100 * 100;
                    int stopIdx = Math.Min((idx100 + 1) * 100, nodes.Count);
                    string idsString = ConstructIdList(nodes.Keys.ToList(), startIdx, stopIdx);
                    if (idsString.Length > 0)
                    {
                        var sql = string.Format("SELECT way_id FROM way_nodes WHERE node_id IN ({0}) ", idsString);
                        var com = new SqlCommand(sql) { Connection = con };
                        SqlDataReader reader = com.ExecuteReader();

                        while (reader.Read())
                        {
                            long id = reader.GetInt64(0);
                            if (!wayIds.Contains(id))
                            {
                                wayIds.Add(id);
                            }
                        }
                        reader.Close();
                        com.Dispose();
                    }
                }

                return GetWays(wayIds, nodes);
            }
            return new List<Way>();
        }

        /// <summary>
        /// Returns all data within the given bounding box and filtered by the given filter.
        /// Note: Filter is not yet used.
        /// </summary>
        /// <param name="box"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public IList<OsmGeo> Get(GeoCoordinateBox box, Filter filter)
        {
            // calculate bounding box parameters to query db.
            var tileRange = TileRange.CreateAroundBoundingBox(box, TileDefaultsForRouting.Zoom);
            var boxes = tileRange.Select(tile => tile.DatabaseId()).ToList();
            return Get(boxes, filter);
        }

        /// <summary>
        /// Returns all data for the given tile
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public IList<OsmGeo> Get(Tile tile, Filter filter)
        {
            return Get(new List<long> { tile.DatabaseId() }, filter);
        } 
        
        /// <summary>
        /// Returns all data for the given tiles
        /// </summary>
        /// <param name="tiles"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public IList<OsmGeo> Get(IList<Tile> tiles, Filter filter)
        {
            return Get(tiles.Select(tile => tile.DatabaseId()).ToList(), filter);
        }

        /// <summary>
        /// Returns all data for the given tile IDs
        /// Note: Filter is not yet used.
        /// </summary>
        /// <param name="tileDatabaseIDs">The tile database ID's</param>
        /// <param name="filter"></param>
        /// <returns></returns>
        private IList<OsmGeo> Get(IList<long> tileDatabaseIDs, Filter filter)
        {
            var baseList = new List<OsmGeo>();
            if (tileDatabaseIDs.Count == 0)
                return baseList;

            // STEP 1: query nodes table.
            var from = string.Format("FROM node WHERE (visible = 1) AND tile IN ({0})", ConstructIdList(tileDatabaseIDs));
            var sql = string.Format("SELECT id,latitude,longitude,changeset_id,[timestamp],[version] {0}", from);
            var nodeIdSql = string.Format("SELECT id {0}", from);

            var conn = CreateConnection();
            var cmd = new SqlCommand(sql) { Connection = conn };
            SqlDataReader reader = cmd.ExecuteReader();
            var nodes = new Dictionary<long, Node>();
            var nodeIds = new List<long>();
            while (reader.Read())
            {
                // create node.
                Node node = OsmBaseFactory.CreateNode(reader.GetInt64(0));
                node.ChangeSetId = reader.GetInt64(3);
                node.TimeStamp = reader.GetDateTime(4);
                node.Version = reader.GetInt32(5);
                node.Coordinate = new GeoCoordinate(reader.GetInt32(1) / 10000000.0, reader.GetInt32(2) / 10000000.0);

                nodes.Add(node.Id,node);
                nodeIds.Add(node.Id);
            }
            reader.Close();

            // STEP2: Load all node tags.
            LoadNodeTagsFromTiles(nodes, nodeIdSql);            

            // STEP3: Load all ways for the given nodes.
            IList<Way> ways = GetWaysFromTiles(nodes, nodeIdSql);

            // Add all objects to the base list.
            baseList.AddRange(nodes.Values);
            baseList.AddRange(ways);
            return baseList;
        }

        /// <summary>
        /// Constructs an id list for SQL.
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        private static string ConstructIdList(IList<long> ids)
        {
            return ConstructIdList(ids, 0, ids.Count);
        }

        /// <summary>
        /// Constructs an id list for SQL for only the specified section of ids.
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="startIdx"></param>
        /// <param name="endIdx"></param>
        /// <returns></returns>
        private static string ConstructIdList(IList<long> ids,int startIdx,int endIdx)
        {
            string returnString = string.Empty;
            if (ids.Count > 0 && ids.Count > startIdx)
            {
                returnString = returnString + ids[startIdx];
                for (int i = startIdx + 1; i < endIdx; i++)
                {
                    returnString = returnString + "," + ids[i];
                }
            }
            return returnString;
        }

        /// <summary>
        /// Loads all tags for the given nodes.
        /// </summary>
        /// <param name="nodes"></param>
        private void LoadNodeTags(Dictionary<long,Node> nodes)
        {
            if (nodes.Count <= 0)
                return;

            for (int idx1000 = 0; idx1000 <= nodes.Count / Batch; idx1000++)
            {
                int startIdx = idx1000 * Batch;
                int stopIdx = Math.Min((idx1000 + 1) * Batch,nodes.Count);
                string ids = ConstructIdList(nodes.Keys.ToList(), startIdx,stopIdx);
                if (ids.Length <= 0)
                    continue;

                var sql = string.Format("SELECT node_id,[key],value FROM node_tags WHERE node_id IN ({0})", ids);
                var con = CreateConnection();
                var com = new SqlCommand(sql) { Connection = con };
                var reader = com.ExecuteReader();
                while (reader.Read())
                {
                    long nodeID = reader.GetInt64(0);
                    string key = reader.GetString(1);
                    object val = reader.GetValue(2);

                    string value = string.Empty;
                    if (val is string)
                    {
                        value = val as string;
                    }

                    nodes[nodeID].Tags.Add(key, value);

                }
                reader.Close();
            }
        }

        private void LoadNodeTagsFromTiles(Dictionary<long, Node> nodes, string nodeIdSql)
        {
            if (nodes.Count <= 0)
                return;

            var sql = string.Format("SELECT node_id,[key],value FROM node_tags WHERE node_id IN ({0})", nodeIdSql);
            var con = CreateConnection();
            var com = new SqlCommand(sql) { Connection = con };
            var reader = com.ExecuteReader();
            while (reader.Read())
            {
                long nodeID = reader.GetInt64(0);
                string key = reader.GetString(1);
                object val = reader.GetValue(2);

                string value = string.Empty;
                if (val is string)
                {
                    value = val as string;
                }

                nodes[nodeID].Tags.Add(key, value);

            }
            reader.Close();
        }

        #endregion

        /// <summary>
        /// Closes this datasource.
        /// </summary>
        public void Close()
        {
            if (_connection == null)
                return;

            _connection.Close();
            _connection = null;
        }

        #region IDisposable Members

        /// <summary>
        /// Diposes the resources used in this datasource.
        /// </summary>
        public void Dispose()
        {
            _connection.Close();
            _connection.Dispose();
            _connection = null;
        }

        #endregion
    }
}
