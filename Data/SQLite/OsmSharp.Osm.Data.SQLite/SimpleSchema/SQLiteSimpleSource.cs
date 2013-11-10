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
using System.Data.SQLite;
using System.Linq;
using OsmSharp.Osm.Data.SQLite.Raw.Processor;
using OsmSharp.Osm.Factory;
using OsmSharp.Osm.Filters;
using OsmSharp.Tools.Math.Geo;

namespace OsmSharp.Osm.Data.SQLite.SimpleSchema
{
    /// <summary>
    /// A SQLite simple data source.
    /// </summary>
    public class SQLiteSimpleSource : IDataSourceReadOnly, IDisposable
    {
        private readonly string _connectionString;
        private readonly Guid _id;
        private const int MaxCacheNodes = 1000;
        private readonly Dictionary<long, CachedNode> _cacheNodes = new Dictionary<long, CachedNode>(MaxCacheNodes);

        /// <summary>
        /// Creates a new SQLite simple data source.
        /// </summary>
        /// <param name="connectionString"></param>
        public SQLiteSimpleSource(string connectionString)
        {
            _connectionString = connectionString;
            _id = Guid.NewGuid();
        }

        /// <summary>
        /// Holds the connection.
        /// </summary>
        private SQLiteConnection _connection;

        /// <summary>
        /// Creates/get the connection.
        /// </summary>
        /// <returns></returns>
        private SQLiteConnection CreateConnection()
        {
            if (_connection == null)
            {
                _connection = new SQLiteConnection(_connectionString);
                _connection.Open();
            }
            return _connection;
        }

        #region IDataSourceReadOnly Members

        /// <summary>
        /// Returns the boundingbox of this data if any.
        /// </summary>
        public GeoCoordinateBox BoundingBox
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Returns the name of this data source.
        /// </summary>
        public string Name
        {
            get
            {
                return "SQLite API Data Source";
            }
        }

        /// <summary>
        /// Returns the id of this data source.
        /// </summary>
        public Guid Id
        {
            get
            {
                return _id;
            }
        }

        /// <summary>
        /// Returns a value that indicates if the boundingbox is available or not.
        /// </summary>
        public bool HasBoundinBox
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Returns a value that indicates if this data is readonly or not.
        /// </summary>
        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Returns the node for the given id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Node GetNode(long id)
        {
            IList<Node> nodes = GetNodes(new List<long>(new long[] { id }));
            if (nodes.Count > 0)
                return nodes[0];

            return null;
        }

        /// <summary>
        /// Returns all the nodes for the given ids.
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public IList<Node> GetNodes(IList<long> ids)
        {
            if (ids.Count <= 0)
                return new List<Node>();

            // initialize connection.
            SQLiteConnection con = CreateConnection();
            string sql;
            SQLiteCommand com;
            SQLiteDataReader reader;

            // STEP 1: query nodes table.
            //id	latitude	longitude	changeset_id	visible	timestamp	tile	version

            Dictionary<long, Node> nodes = GetCachedNodes(ids);
            if (nodes.Count > 0)
                if (nodes.Count < ids.Count)
                    ids = new List<long>(ids.Where(x => !_cacheNodes.ContainsKey(x)));
                else
                    return nodes.Values.ToList();

            for (int idx_1000 = 0; idx_1000 <= ids.Count / 1000; idx_1000++)
            {
                int start_idx = idx_1000 * 1000;
                int stop_idx = Math.Min((idx_1000 + 1) * 1000, ids.Count);
                string ids_string = ConstructIdList(ids, start_idx, stop_idx);
                if (ids_string.Length <= 0)
                    continue;

                sql = "SELECT * FROM node WHERE (id IN ({0})) ";
                sql = string.Format(sql, ids_string);
                com = new SQLiteCommand(sql, con);
                reader = ExecuteReader(com);

                while (reader.Read())
                {
                    // load/parse data.
                    // id (0)
                    long id = reader.GetInt64(0);

                    Node node;
                    if (!nodes.TryGetValue(id, out node))
                    {
                        // create node.
                        node = OsmBaseFactory.CreateNode(id);

                        // latitude (1)
                        int latitude_int = reader.GetInt32(1);

                        // longitude (2)
                        int longitude_int = reader.GetInt32(2);

                        // changeset_id (3)
                        if (!reader.IsDBNull(3))
                            node.ChangeSetId = reader.GetInt64(3);

                        // visible (4)
                        if (!reader.IsDBNull(4))
                            node.Visible = reader.GetInt32(4) == 1;

                        // timestamp (5)
                        if (!reader.IsDBNull(5))
                            node.TimeStamp = reader.GetDateTime(5);

                        // tile (6)
                        //if (!reader.IsDBNull(6))
                        //  long tile = reader.GetInt64(6);

                        // usr (7)
                        //if (!reader.IsDBNull(7))
                        //  node.User = reader.GetString(7);

                        // usr_id (8)
                        //if (!reader.IsDBNull(8))
                        //  node.UserId = reader.GetInt64(8);

                        node.Coordinate = new GeoCoordinate(latitude_int / 10000000.0, longitude_int / 10000000.0);
                        nodes.Add(node.Id, node);

                        AddCachedNode(node);
                    }
                }

                reader.Close();
            }

            // STEP2: Load all tags.
            LoadTags(nodes, "node");

            return nodes.Values.ToList();
        }

        private CachedNode AddCachedNode(Node node)
        {
            CachedNode cachedNode;
            if (_cacheNodes.TryGetValue(node.Id, out cachedNode))
                return cachedNode; //exists

            if (_cacheNodes.Count > MaxCacheNodes)
                _cacheNodes.Remove(_cacheNodes.First().Key);

            cachedNode = new CachedNode(node);
            _cacheNodes.Add(node.Id, cachedNode);
            return cachedNode;
        }

        private void AddCachedWay(Node node, Way way)
        {
            CachedNode cached;
            if (!_cacheNodes.TryGetValue(node.Id, out cached))
            {
                AddCachedNode(node);
                return;
            }
            if (cached.Ways == null)
                cached.Ways = new List<Way>();
            cached.Ways.Add(way);
        }

        private Dictionary<long, Node> GetCachedNodes(IList<long> ids)
        {
            return _cacheNodes.Where(x => ids.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value.Node);
        }

        private Dictionary<long, Way> GetCachedWays(Dictionary<long, Node> nodes)
        {
            return _cacheNodes.Where(n => nodes.ContainsKey(n.Key) && n.Value.Ways != null && n.Value.Ways.Count > 0).SelectMany(cn => cn.Value.Ways).Distinct().ToDictionary(way => way.Id, way => way);
        }

        /// <summary>
        /// Returns the relation with the given id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Relation GetRelation(long id)
        {
            IList<Relation> relations = GetRelations(new List<long>(new long[] { id }));
            if (relations.Count > 0)
                return relations[0];

            return null;
        }

        /// <summary>
        /// Returns all relations with the given ids.
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public IList<Relation> GetRelations(IList<long> ids)
        {
            if (ids.Count <= 0)
                return new List<Relation>();

            // initialize connection.
            SQLiteConnection con = CreateConnection();
            string sql;
            SQLiteCommand com;
            SQLiteDataReader reader;

            // STEP2: Load relations.
            Dictionary<long, Relation> relations = new Dictionary<long, Relation>();
            for (int idx_1000 = 0; idx_1000 <= ids.Count / 1000; idx_1000++)
            {
                int start_idx = idx_1000 * 1000;
                int stop_idx = Math.Min((idx_1000 + 1) * 1000, ids.Count);
                string ids_string = ConstructIdList(ids, start_idx, stop_idx);
                if (ids_string.Length <= 0)
                    continue;

                sql = "SELECT * FROM relation WHERE (id IN ({0})) ";
                sql = string.Format(sql, ids_string);
                com = new SQLiteCommand(sql, con);
                reader = ExecuteReader(com);

                Relation relation;
                while (reader.Read())
                {
                    // load/parse data.
                    // id (0)
                    long id = reader.GetInt64(0);
                    // create node.
                    relation = OsmBaseFactory.CreateRelation(id);

                    // changeset_id (1)
                    if (!reader.IsDBNull(1))
                        relation.ChangeSetId = reader.GetInt64(1);

                    // visible (2)
                    if (!reader.IsDBNull(2))
                        relation.Visible = reader.GetInt32(2) == 1;

                    // timestamp (3)
                    if (!reader.IsDBNull(3))
                        relation.TimeStamp = reader.GetDateTime(3);

                    // version (4)
                    if (!reader.IsDBNull(4))
                        relation.Version = reader.GetInt64(4);

                    // usr (5)
                    //if (!reader.IsDBNull(5))
                    //  relation.User = reader.GetString(5);

                    // usr_id (6)
                    //if (!reader.IsDBNull(6))
                    //  relation.UserId = reader.GetInt64(6);

                    relations.Add(relation.Id, relation);
                }
                reader.Close();
            }

            // STEP3: Load Members
            // TODO: Load members incl. roles, nodes, ways and relations

            // STEP4: Load all tags.
            LoadTags(relations, "relation");

            return relations.Values.ToList();
        }

        /// <summary>
        /// Returns all relations that contain the given object.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public IList<Relation> GetRelationsFor(Osm.OsmBase obj)
        {
            // TODO: implement this
            return new List<Relation>();
        }

        /// <summary>
        /// Returns the way for the given id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Way GetWay(long id)
        {
            IList<Way> ways = GetWays(new List<long>(new long[] { id }));
            if (ways.Count > 0)
                return ways[0];

            return null;
        }

        /// <summary>
        /// Returns all the ways with the given ids.
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public IList<Way> GetWays(IList<long> ids)
        {
            return GetWays(ids, null);
        }

        /// <summary>
        /// Returns all ways containing the given nodes.
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="nodes"></param>
        /// <returns></returns>
        private IList<Way> GetWays(IList<long> ids, Dictionary<long, Node> nodes)
        {
            if (ids.Count <= 0)
                return new List<Way>();

            // initialize connection.
            SQLiteConnection con = CreateConnection();
            string sql;
            SQLiteCommand com;
            SQLiteDataReader reader;

            // STEP2: Load ways.
            // TODO: Recheck for using cached ways
            Dictionary<long, Way> ways = new Dictionary<long, Way>();
            for (int idx_1000 = 0; idx_1000 <= ids.Count / 1000; idx_1000++)
            {
                int start_idx = idx_1000 * 1000;
                int stop_idx = Math.Min((idx_1000 + 1) * 1000, ids.Count);
                string ids_string = ConstructIdList(ids, start_idx, stop_idx);
                if (ids_string.Length <= 0)
                    continue;

                sql = "SELECT * FROM way WHERE (id IN ({0})) ";
                sql = string.Format(sql, ids_string);
                com = new SQLiteCommand(sql, con);
                reader = ExecuteReader(com);

                Way way;
                while (reader.Read())
                {
                    // load/parse data.
                    // id (0)
                    long id = reader.GetInt64(0);
                    // create node.
                    way = OsmBaseFactory.CreateWay(id);

                    // changeset_id (1)
                    if (!reader.IsDBNull(1))
                        way.ChangeSetId = reader.GetInt64(1);

                    // visible (2)
                    if (!reader.IsDBNull(2))
                        way.Visible = reader.GetInt32(2) == 1;

                    // timestamp (3)
                    if (!reader.IsDBNull(3))
                        way.TimeStamp = reader.GetDateTime(3);

                    // version (4)
                    if (!reader.IsDBNull(4))
                        way.Version = reader.GetInt64(4);

                    // usr (5)
                    if (!reader.IsDBNull(5))
                        way.User = reader.GetString(5);

                    // usr_id (6)
                    if (!reader.IsDBNull(6))
                        way.UserId = reader.GetInt64(6);

                    ways.Add(way.Id, way);
                }
                reader.Close();
            }

            //STEP3: Load all node-way relations
            List<long> missing_node_ids = new List<long>();
            for (int idx_1000 = 0; idx_1000 <= ids.Count / 1000; idx_1000++)
            {
                int start_idx = idx_1000 * 1000;
                int stop_idx = Math.Min((idx_1000 + 1) * 1000, ids.Count);
                string ids_string = ConstructIdList(ids, start_idx, stop_idx);
                if (ids_string.Length <= 0)
                    continue;

                sql = "SELECT * FROM way_nodes WHERE (way_id IN ({0})) ORDER BY sequence_id";
                sql = string.Format(sql, ids_string);
                com = new SQLiteCommand(sql, con);
                reader = ExecuteReader(com);

                while (reader.Read())
                {
                    long id = reader.GetInt64(0);
                    long node_id = reader.GetInt64(1);
                    long sequence_id = reader.GetInt64(2);

                    if (nodes == null || !nodes.ContainsKey(node_id))
                    {
                        missing_node_ids.Add(node_id);
                    }
                }

                reader.Close();
            }

            //STEP4: Load all missing nodes.
            IList<Node> missing_nodes = GetNodes(missing_node_ids);
            Dictionary<long, Node> way_nodes = new Dictionary<long, Node>();
            if (nodes != null)
            {
                way_nodes = new Dictionary<long, Node>(nodes);
            }
            foreach (Node node in missing_nodes)
            {
                way_nodes.Add(node.Id, node);
            }

            //STEP5: assign nodes to way.
            for (int idx_1000 = 0; idx_1000 <= ids.Count / 1000; idx_1000++)
            {
                int start_idx = idx_1000 * 1000;
                int stop_idx = Math.Min((idx_1000 + 1) * 1000, ids.Count);
                string ids_string = ConstructIdList(ids, start_idx, stop_idx);
                if (ids_string.Length <= 0)
                    continue;

                sql = "SELECT * FROM way_nodes WHERE (way_id IN ({0})) ORDER BY sequence_id";
                sql = string.Format(sql, ids_string);
                com = new SQLiteCommand(sql, con);
                reader = ExecuteReader(com);

                while (reader.Read())
                {
                    long id = reader.GetInt64(0);
                    long node_id = reader.GetInt64(1);
                    long sequence_id = reader.GetInt64(2);

                    Node way_node;
                    if (way_nodes.TryGetValue(node_id, out way_node))
                    {
                        Way way;
                        if (ways.TryGetValue(id, out way))
                        {
                            way.Nodes.Add(way_node);
                        }
                    }
                }
                reader.Close();
            }

            // STEP4: Load all tags.
            LoadTags(ways, "way");

            return ways.Values.ToList();
        }

        /// <summary>
        /// Returns all the ways that contain the given node.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public IList<Way> GetWaysFor(Node node)
        {
            Dictionary<long, Node> nodes = new Dictionary<long, Node>();
            nodes.Add(node.Id, node);
            return GetWaysForNodes(nodes);
        }

        /// <summary>
        /// Returns all the ways that contain any of the given nodes.
        /// </summary>
        /// <param name="nodes"></param>
        /// <returns></returns>
        public IList<Way> GetWaysForNodes(Dictionary<long, Node> nodes)
        {
            if (nodes.Count <= 0)
                return new List<Way>();

            // initialize connection.
            SQLiteConnection con = CreateConnection();
            string sql;
            SQLiteCommand com;
            SQLiteDataReader reader;

            List<long> way_ids = new List<long>();
            for (int idx_1000 = 0; idx_1000 <= nodes.Count / 1000; idx_1000++)
            {
                // STEP1: Load ways that exist for the given nodes.
                int start_idx = idx_1000 * 1000;
                int stop_idx = Math.Min((idx_1000 + 1) * 1000, nodes.Count);
                string ids_string = ConstructIdList(nodes.Keys.ToList(), start_idx, stop_idx);
                if (ids_string.Length <= 0)
                    continue;

                sql = "SELECT * FROM way_nodes WHERE (node_id IN ({0})) ";
                sql = string.Format(sql, ids_string);
                com = new SQLiteCommand(sql, con);
                reader = ExecuteReader(com);

                while (reader.Read())
                {
                    long id = reader.GetInt64(0);
                    if (!way_ids.Contains(id))
                    {
                        way_ids.Add(id);
                    }
                }

                reader.Close();
            }

            return GetWays(way_ids, nodes);
        }

        private static SQLiteDataReader ExecuteReader(SQLiteCommand com)
        {
            return com.ExecuteReader();
        }

        /// <summary>
        /// Returns all objects with the given bounding box and valid for the given filter;
        /// </summary>
        /// <param name="box"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public IList<OsmGeo> Get(GeoCoordinateBox box, Filter filter)
        {
            // initialize connection.
            SQLiteConnection con = CreateConnection();
            List<OsmGeo> base_list = new List<OsmGeo>();

            // calculate bounding box parameters to query db.
            long latitude_min = (long)(box.MinLat * 10000000.0);
            long longitude_min = (long)(box.MinLon * 10000000.0);
            long latitude_max = (long)(box.MaxLat * 10000000.0);
            long longitude_max = (long)(box.MaxLon * 10000000.0);

            // TODO: improve this to allow loading of bigger bb's.
            uint x_min = TileCalculations.lon2x(box.MinLon);
            uint x_max = TileCalculations.lon2x(box.MaxLon);
            uint y_min = TileCalculations.lat2y(box.MinLat);
            uint y_max = TileCalculations.lat2y(box.MaxLat);

            IList<long> boxes = new List<long>();

            for (uint x = x_min; x <= x_max; x++)
            {
                for (uint y = y_min; y <= y_max; y++)
                {
                    boxes.Add(TileCalculations.xy2tile(x, y));
                }
            }

            // STEP 1: query nodes table.
            //id	latitude	longitude	changeset_id	visible	timestamp	tile	version
            string sql = "SELECT * FROM node WHERE  (tile IN ({4})) AND (visible = 1) AND (latitude BETWEEN {0} AND {1} AND longitude BETWEEN {2} AND {3})";
            sql = string.Format(sql,
                latitude_min.ToString(),
                latitude_max.ToString(),
                longitude_min.ToString(),
                longitude_max.ToString(),
                this.ConstructIdList(boxes));

            // TODO: parameters.
            var com = new SQLiteCommand(sql);
            com.Connection = con;
            SQLiteDataReader reader = ExecuteReader(com);
            Node node = null;
            var nodes = new Dictionary<long, Node>();
            while (reader.Read())
            {
                // load/parse data.
                long returnedId = reader.GetInt64(0);
                int latitudeInt = reader.GetInt32(1);
                int longitudeInt = reader.GetInt32(2);
                long changesetId = reader.GetInt64(3);
                //bool visible = reader.GetInt64(4) == 1;
                DateTime timestamp = reader.GetDateTime(5);
                //long tile = reader.GetInt64(6);
                long version = reader.GetInt64(7);

                // create node.
                node = OsmBaseFactory.CreateNode(returnedId);
                node.Version = version;
                //node.UserId = user_id;
                node.TimeStamp = timestamp;
                node.ChangeSetId = changesetId;
                node.Coordinate = new GeoCoordinate(latitudeInt / 10000000.0, longitudeInt / 10000000.0);

                nodes.Add(node.Id, node);
            }
            reader.Close();

            // STEP2: Load all tags.
            LoadTags(nodes, "node");

            // STEP3: Load all ways for the given nodes.
            IList<Way> ways = GetWaysForNodes(nodes);

            // Add all objects to the base list.
            foreach (Node nodeResult in nodes.Values.ToList())
            {
                base_list.Add(nodeResult);
            }
            foreach (Way way in ways)
            {
                base_list.Add(way);
            }
            return base_list;
        }

        /// <summary>
        /// Constructs a list of ids.
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        private string ConstructIdList(IList<long> ids)
        {
            return ConstructIdList(ids, 0, ids.Count);
        }

        /// <summary>
        /// Constructs a list of ids.
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="startIdx"></param>
        /// <param name="endIdx"></param>
        /// <returns></returns>
        private string ConstructIdList(IList<long> ids, int startIdx, int endIdx)
        {
            string return_string = string.Empty;
            if (ids.Count > 0 && ids.Count > startIdx)
            {
                return_string = return_string + ids[startIdx].ToString();
                for (int i = startIdx + 1; i < endIdx; i++)
                {
                    return_string = return_string + "," + ids[i].ToString();
                }
            }
            return return_string;
        }

        /// <summary>
        /// Loads all tags for all given <see cref="OsmGeo"/>-items in <paramref name="geoCollection"/>.
        /// </summary>
        /// <typeparam name="TE"></typeparam>
        /// <param name="geoCollection"></param>
        /// <param name="geoType">Valid values for <paramref name="geoType"/> are: <c>node</c>, <c>way</c> or <c>relation</c></param>
        private void LoadTags<TE>(Dictionary<long, TE> geoCollection, string geoType) where TE : OsmGeo
        {
            if (geoType != "node" && geoType != "way" && geoType != "relation")
                throw new ArgumentOutOfRangeException("geoType", geoType, "Only 'node', 'way' and 'relation' are allowed for geoType.");

            if (geoCollection.Count <= 0)
                return;

            for (int idx_1000 = 0; idx_1000 <= geoCollection.Count / 1000; idx_1000++)
            {
                int start_idx = idx_1000 * 1000;
                int stop_idx = Math.Min((idx_1000 + 1) * 1000, geoCollection.Count);
                string ids_string = ConstructIdList(geoCollection.Keys.ToList(), start_idx, stop_idx);
                if (ids_string.Length <= 0)
                    continue;

                string sql = "SELECT * FROM {1}_tags WHERE ({1}_id IN ({0})) ";
                sql = string.Format(sql, ids_string, geoType);
                SQLiteConnection con = CreateConnection();
                SQLiteCommand com = new SQLiteCommand(sql, con);
                SQLiteDataReader reader = ExecuteReader(com);

                while (reader.Read())
                {
                    long id = reader.GetInt64(0);
                    string key = reader.GetString(1);
                    object value_object = reader[2];
                    string value = string.Empty;
                    if (value_object != null && value_object != DBNull.Value)
                    {
                        value = (string)value_object;
                    }

                    TE geoItem;
                    if (geoCollection.TryGetValue(id, out geoItem))
                    {
                        geoItem.Tags.Add(key, value);
                    }
                }
                reader.Close();
            }
        }

        #endregion

        /// <summary>
        /// Closes this source.
        /// </summary>
        public void Close()
        {
            if (_connection != null)
            {
                _connection.Close();
                _connection = null;
            }
        }

        #region IDisposable Members

        /// <summary>
        /// Disposes all resources.
        /// </summary>
        public void Dispose()
        {
            _connection.Close();
            _connection.Dispose();
            _connection = null;
        }

        #endregion

        /// <summary>
        /// Represents a cached node.
        /// </summary>
        private class CachedNode
        {
            /// <summary>
            /// Creates a new cached node.
            /// </summary>
            /// <param name="node"></param>
            public CachedNode(Node node)
            {
                Node = node;
            }

            /// <summary>
            /// Gets/sets the node.
            /// </summary>
            public Node Node
            {
                get;
                set;
            }

            /// <summary>
            /// Gets/sets the ways.
            /// </summary>
            public List<Way> Ways
            {
                get;
                set;
            }
        }
    }
}