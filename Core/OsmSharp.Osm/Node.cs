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
using OsmSharp.Osm.Simple;
using OsmSharp.Tools.Collections;
using OsmSharp.Tools.Math.Geo;

namespace OsmSharp.Osm
{
    /// <summary>
    ///     Node class.
    /// </summary>
    public class Node : OsmGeo, IEquatable<Node>
    {
        /// <summary>
        ///     Creates a new node.
        /// </summary>
        /// <param name="id"></param>
        protected internal Node(long id) : base(id)
        {
        }

        /// <summary>
        ///     Creates a new node using a string table.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="string_table"></param>
        protected internal Node(ObjectTable<string> string_table, long id) : base(string_table, id)
        {
        }

        /// <summary>
        ///     Returns the node type.
        /// </summary>
        public override OsmType Type
        {
            get { return OsmType.Node; }
        }

        /// <summary>
        ///     The coordinates of this node.
        /// </summary>
        public GeoCoordinate Coordinate { get; set; }

        /// <summary>
        ///     Converts this node to it's simple counterpart.
        /// </summary>
        /// <returns></returns>
        public override SimpleOsmGeo ToSimple()
        {
            return new SimpleNode
            {
                Id          = Id,
                ChangeSetId = ChangeSetId,
                Latitude    = Coordinate.Latitude,
                Longitude   = Coordinate.Longitude,
                Tags        = Tags,
                TimeStamp   = TimeStamp,
                UserId      = UserId,
                UserName    = User,
                Version     = (ulong?) Version,
                Visible     = Visible
            };
        }

        /// <summary>
        ///     Returns a description of this node.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (Coordinate != null)
                return string.Format("http://www.openstreetmap.org/?node={0}:[{1};{2}]", Id, Coordinate.Longitude, Coordinate.Latitude);
            return string.Format("http://www.openstreetmap.org/?node={0}", Id);
        }

        /// <summary>
        ///     Copies all properties of this node onto the given node without the id.
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public void CopyTo(Node n)
        {
            foreach (var tag in Tags)
                n.Tags.Add(tag.Key, tag.Value);

            n.TimeStamp = TimeStamp;
            n.User = User;
            n.UserId = UserId;
            n.Version = Version;
            n.Visible = Visible;
        }

        /// <summary>
        ///     Returns an exact copy of this way.
        ///     WARNING: even the id is copied!
        /// </summary>
        /// <returns></returns>
        public Node Copy()
        {
            var n = new Node(Id);
            CopyTo(n);
            return n;
        }

        #region IEquatable<Node> Members

        /// <summary>
        ///     Returns true if the given object equals the other in content.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(Node other)
        {
            if (other != null)
                return other.Id == Id;
            return false;
        }

        #endregion
    }
}