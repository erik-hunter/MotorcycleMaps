﻿// OsmSharp - OpenStreetMap tools & library.
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
using System.Text;
using OsmSharp.Osm.Data.Core.Processor;
using System.IO;
using OsmSharp.Osm.Data.PBF.Dense;
using OsmSharp.Osm.Simple;

namespace OsmSharp.Osm.Data.PBF.Raw.Processor
{
    /// <summary>
    /// A source of PBF formatted OSM data.
    /// </summary>
    public class PBFDataProcessorSource : DataProcessorSource, IPBFOsmPrimitiveConsumer
    {
        /// <summary>
        /// Holds the source of the data.
        /// </summary>
        private readonly Stream _stream;

        /// <summary>
        /// Creates a new source of PBF formated OSM data.
        /// </summary>
        /// <param name="stream"></param>
        public PBFDataProcessorSource(Stream stream)
        {
            _stream = stream;
        }

        /// <summary>
        /// Initializes the current source.
        /// </summary>
        public override void Initialize()
        {
            _stream.Seek(0, SeekOrigin.Begin);

            InitializePBFReader();
        }

        /// <summary>
        /// Moves to the next object.
        /// </summary>
        /// <returns></returns>
        public override bool MoveNext()
        {
            var nextPBFPrimitive = MoveToNextPrimitive();

            if (nextPBFPrimitive.Value != null)
            { // there is a primitive.
                _current = Convert(nextPBFPrimitive);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Holds the current object.
        /// </summary>
        private SimpleOsmGeo _current;

        /// <summary>
        /// Returns the current geometry.
        /// </summary>
        /// <returns></returns>
        public override SimpleOsmGeo Current()
        {
            return _current;
        }

        /// <summary>
        /// Resetting this data source 
        /// </summary>
        public override void Reset()
        {
            _current = null;
            _stream.Seek(0, SeekOrigin.Begin);
        }

        /// <summary>
        /// Returns true if this source can be reset.
        /// </summary>
        public override bool CanReset
        {
            get
            {
                return _stream.CanSeek;
            }
        }

        #region Primitive Conversion

        /// <summary>
        /// Converts simple primitives.
        /// </summary>
        /// <param name="pbfPrimitive"></param>
        /// <returns></returns>
        internal SimpleOsmGeo Convert(KeyValuePair<PrimitiveBlock, object> pbfPrimitive)
        {
            if (pbfPrimitive.Value == null || pbfPrimitive.Key == null)
            {
                throw new ArgumentNullException("pbfPrimitive");
            }

            PrimitiveBlock block = pbfPrimitive.Key; // get the block properties this object comes from.
            if (pbfPrimitive.Value is Node)
            {
                var node = (pbfPrimitive.Value as Node);
                var simpleNode = new SimpleNode
                {
                    ChangeSetId = node.info.changeset,
                    Id = node.id,
                    Latitude = .000000001 * (block.lat_offset + (block.granularity * (double)node.lat)),
                    Longitude = .000000001 * (block.lon_offset + (block.granularity * (double)node.lon)),
                    Tags = new Dictionary<string, string>()
                };
                int count = node.keys.Count;
                for (int tagIdx = 0; tagIdx < count; tagIdx++)
                {
                    byte[] keyBytes = block.stringtable.s[(int)node.keys[tagIdx]];
                    string key = UTF8Encoding.UTF8.GetString(keyBytes, 0, keyBytes.Length);

                    if (!simpleNode.Tags.ContainsKey(key))
                    {
                        byte[] valueBytes = block.stringtable.s[(int)node.vals[tagIdx]];
                        string value = UTF8Encoding.UTF8.GetString(valueBytes, 0, valueBytes.Length);
                        simpleNode.Tags.Add(new KeyValuePair<string, string>(key, value));
                    }
                }
                simpleNode.TimeStamp = Tools.Utilities.FromUnixTime(node.info.timestamp * (long)block.date_granularity);
                simpleNode.Visible = true;
                simpleNode.Version = (uint)node.info.version;
                simpleNode.UserId = node.info.uid;
                byte[] userByte = block.stringtable.s[node.info.user_sid];
                simpleNode.UserName = UTF8Encoding.UTF8.GetString(userByte, 0, userByte.Length);
                simpleNode.Version = (ulong)node.info.version;
                simpleNode.Visible = true;

                return simpleNode;
            }

            if (pbfPrimitive.Value is Way)
            {
                var way = (pbfPrimitive.Value as Way);

                var simpleWay = new SimpleWay { Id = way.id, Nodes = new List<long>(way.refs.Count) };
                long nodeID = 0;

                int count = way.refs.Count;
                for (int nodeIdx = 0; nodeIdx < count; nodeIdx++)
                {
                    nodeID = nodeID + way.refs[nodeIdx];
                    simpleWay.Nodes.Add(nodeID);
                }
                simpleWay.Tags = new Dictionary<string, string>();

                count = way.keys.Count;
                for (int tagIdx = 0; tagIdx < count; tagIdx++)
                {
                    byte[] keyBytes = block.stringtable.s[(int)way.keys[tagIdx]];
                    string key = UTF8Encoding.UTF8.GetString(keyBytes, 0, keyBytes.Length);

                    if (!simpleWay.Tags.ContainsKey(key))
                    {
                        byte[] valueBytes = block.stringtable.s[(int)way.vals[tagIdx]];
                        string value = UTF8Encoding.UTF8.GetString(valueBytes, 0, valueBytes.Length);
                        simpleWay.Tags.Add(new KeyValuePair<string, string>(key, value));
                    }
                }
                if (way.info != null)
                { // add the metadata if any.
                    simpleWay.ChangeSetId = way.info.changeset;
                    simpleWay.TimeStamp = Tools.Utilities.FromUnixTime(way.info.timestamp * (long)block.date_granularity);
                    simpleWay.UserId = way.info.uid;
                    byte[] userBytes = block.stringtable.s[way.info.user_sid];
                    simpleWay.UserName = UTF8Encoding.UTF8.GetString(userBytes, 0, userBytes.Length);
                    simpleWay.Version = (ulong)way.info.version;
                }
                simpleWay.Visible = true;

                return simpleWay;
            }

            if (pbfPrimitive.Value is Relation)
            {
                var relation = (pbfPrimitive.Value as Relation);
                var simpleRelation = new SimpleRelation { Id = relation.id, Members = new List<SimpleRelationMember>() };
                long memberID = 0;

                int count = relation.types.Count;
                for (int memberIdx = 0; memberIdx < count; memberIdx++)
                {
                    memberID = memberID + relation.memids[memberIdx];
                    byte[] roleBytes = block.stringtable.s[relation.roles_sid[memberIdx]];
                    string role = UTF8Encoding.UTF8.GetString(roleBytes, 0, roleBytes.Length);
                    var member = new SimpleRelationMember { MemberId = memberID, MemberRole = role };
                    switch (relation.types[memberIdx])
                    {
                        case Relation.MemberType.NODE:
                            member.MemberType = SimpleRelationMemberType.Node;
                            break;
                        case Relation.MemberType.WAY:
                            member.MemberType = SimpleRelationMemberType.Way;
                            break;
                        case Relation.MemberType.RELATION:
                            member.MemberType = SimpleRelationMemberType.Relation;
                            break;
                    }

                    simpleRelation.Members.Add(member);
                }

                simpleRelation.Tags = new Dictionary<string, string>();

                count = relation.keys.Count;
                for (int tagIdx = 0; tagIdx < count; tagIdx++)
                {
                    byte[] keyBytes = block.stringtable.s[(int)relation.keys[tagIdx]];
                    string key = UTF8Encoding.UTF8.GetString(keyBytes, 0, keyBytes.Length);

                    if (!simpleRelation.Tags.ContainsKey(key))
                    {
                        byte[] valueBytes = block.stringtable.s[(int)relation.vals[tagIdx]];
                        string value = UTF8Encoding.UTF8.GetString(valueBytes, 0, valueBytes.Length);
                        simpleRelation.Tags.Add(new KeyValuePair<string, string>(key, value));
                    }
                }
                if (relation.info != null)
                { // read metadata if any.
                    simpleRelation.ChangeSetId = relation.info.changeset;
                    simpleRelation.TimeStamp = Tools.Utilities.FromUnixTime(relation.info.timestamp * (long)block.date_granularity);
                    simpleRelation.UserId = relation.info.uid;
                    byte[] userBytes = block.stringtable.s[relation.info.user_sid];
                    simpleRelation.UserName = UTF8Encoding.UTF8.GetString(userBytes, 0, userBytes.Length);
                    simpleRelation.Version = (ulong)relation.info.version;
                }
                simpleRelation.Visible = true;

                return simpleRelation;
            }
            throw new Exception(string.Format("PBF primitive with type {0} not supported!", pbfPrimitive.GetType()));
        }

        #endregion

        #region PBF Blocks Reader

        /// <summary>
        /// Holds the PBF reader.
        /// </summary>
        private PBFReader _reader;

        /// <summary>
        /// Holds the primitives decompressor.
        /// </summary>
        private Decompressor _decompressor;

        /// <summary>
        /// Initializes the PBF reader.
        /// </summary>
        private void InitializePBFReader()
        {
            _reader = new PBFReader(_stream);
            _decompressor = new Decompressor(this);

            InitializeBlockCache();
        }

        /// <summary>
        /// Moves the PBF reader to the next primitive or returns one of the cached ones.
        /// </summary>
        /// <returns></returns>
        private KeyValuePair<PrimitiveBlock, object> MoveToNextPrimitive()
        {
            KeyValuePair<PrimitiveBlock, object> next = DeQueuePrimitive();
            if (next.Value == null)
            {
                PrimitiveBlock block = _reader.MoveNext();
                if (block != null)
                {
                    _decompressor.ProcessPrimitiveBlock(block);
                    next = DeQueuePrimitive();
                }
            }
            return next;
        }

        #region Block Cache

        /// <summary>
        /// Holds the cached primitives.
        /// </summary>
        private Queue<KeyValuePair<PrimitiveBlock, object>> _cachedPrimitives;

        /// <summary>
        /// Initializes the block cache.
        /// </summary>
        private void InitializeBlockCache()
        {
            _cachedPrimitives = new Queue<KeyValuePair<PrimitiveBlock, object>>();
        }

        /// <summary>
        /// Queues the primitives.
        /// </summary>
        /// <param name="block"></param>
        /// <param name="primitive"></param>
        private void QueuePrimitive(PrimitiveBlock block, object primitive)
        {
            _cachedPrimitives.Enqueue(new KeyValuePair<PrimitiveBlock, object>(block, primitive));
        }

        /// <summary>
        /// DeQueues a primitive.
        /// </summary>
        /// <returns></returns>
        private KeyValuePair<PrimitiveBlock, object> DeQueuePrimitive()
        {
            if (_cachedPrimitives.Count > 0)
            {
                return _cachedPrimitives.Dequeue();
            }
            return new KeyValuePair<PrimitiveBlock, object>();
        }

        #endregion

        #endregion

        /// <summary>
        /// Processes a node.
        /// </summary>
        /// <param name="block"></param>
        /// <param name="node"></param>
        void IPBFOsmPrimitiveConsumer.ProcessNode(PrimitiveBlock block, Node node)
        {
            QueuePrimitive(block, node);
        }

        /// <summary>
        /// Processes a way.
        /// </summary>
        /// <param name="block"></param>
        /// <param name="way"></param>
        void IPBFOsmPrimitiveConsumer.ProcessWay(PrimitiveBlock block, Way way)
        {
            QueuePrimitive(block, way);
        }

        /// <summary>
        /// Processes a relation.
        /// </summary>
        /// <param name="block"></param>
        /// <param name="relation"></param>
        void IPBFOsmPrimitiveConsumer.ProcessRelation(PrimitiveBlock block, Relation relation)
        {
            QueuePrimitive(block, relation);
        }
    }
}