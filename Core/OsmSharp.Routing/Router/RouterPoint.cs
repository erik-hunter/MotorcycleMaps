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
using System.Linq;
using System.Text;
using OsmSharp.Tools.Math.Geo;

namespace OsmSharp.Routing
{
    /// <summary>
    /// Represents a resolved point. A hook for the router to route on.
    /// 
    /// The object represents a location and can be tagged.
    /// </summary>
    public class RouterPoint : IRouterPoint
    {
        /// <summary>
        /// Holds the id of this router point.
        /// </summary>
        private long _id;

        /// <summary>
        /// Creates a new router point.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="location"></param>
        /// <param name="neighbour1"></param>
        /// <param name="neighour2"></param>
        internal RouterPoint(long id, GeoCoordinate location, 
            long neighbour1, long neighour2)
        {
            _id = id;
            this.Location = location;
            this.Tags = new List<KeyValuePair<string, string>>();

            this.Neighbour1 = neighbour1;
            this.Neighbour2 = neighour2;
        }

        /// <summary>
        /// Returns the id of this router point.
        /// </summary>
        public long Id
        {
            get
            {
                return _id;
            }
        }

        /// <summary>
        /// Gets/sets neighbour1
        /// </summary>
        internal long Neighbour1
        {
            get;
            set;
        }

        /// <summary>
        /// Gets/sets neighbour2
        /// </summary>
        internal long Neighbour2
        {
            get;
            set;
        }

        /// <summary>
        /// Returns the location of this router point.
        /// </summary>
        public GeoCoordinate Location
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets/sets the tags.
        /// </summary>
        public List<KeyValuePair<string, string>> Tags
        {
            get;
            set;
        }

        /// <summary>
        /// Returns a description of this router point.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (this.Location == null)
            {
                return string.Format("{0}",
                                     this.Id);
            }
            return string.Format("{0} {1}",
                                 this.Id, this.Location.ToString());
        }
    }
}
