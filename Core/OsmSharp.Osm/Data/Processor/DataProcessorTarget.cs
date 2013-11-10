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

using OsmSharp.Osm.Simple;
using OsmSharp.Tools.Math.Geo;

namespace OsmSharp.Osm.Data.Core.Processor
{
    /// <summary>
    ///     Any target of osm data (Nodes, Ways and Relations).
    /// </summary>
    public abstract class DataProcessorTarget
    {
        /// <summary>
        ///     Holds the box.
        /// </summary>
        private GeoCoordinateBox _box;

        /// <summary>
        ///     Holds the source for this target.
        /// </summary>
        private DataProcessorSource _source;

        /// <summary>
        ///     Returns the bounding box of the data that was pulled into this target.
        /// </summary>
        public GeoCoordinateBox Box
        {
            get { return _box; }
        }

        /// <summary>
        ///     Returns the registered source.
        /// </summary>
        protected DataProcessorSource Source
        {
            get { return _source; }
        }

        /// <summary>
        ///     Initializes the target.
        /// </summary>
        public abstract void Initialize();

        /// <summary>
        ///     Applies a change to the target.
        /// </summary>
        /// <param name="change"></param>
        public abstract void ApplyChange(SimpleChangeSet change);

        /// <summary>
        ///     Adds a node to the target.
        /// </summary>
        /// <param name="node"></param>
        public abstract void AddNode(SimpleNode node);

        /// <summary>
        ///     Adds a way to the target.
        /// </summary>
        /// <param name="way"></param>
        public abstract void AddWay(SimpleWay way);

        /// <summary>
        ///     Adds a relation to the target.
        /// </summary>
        /// <param name="relation"></param>
        public abstract void AddRelation(SimpleRelation relation);

        /// <summary>
        ///     Registers a source on this target.
        /// </summary>
        /// <param name="source"></param>
        public void RegisterSource(DataProcessorSource source)
        {
            _source = source;
        }

        /// <summary>
        ///     Pulls the changes from the source to this target.
        /// </summary>
        public void Pull()
        {
            _source.Initialize();
            Initialize();

            while (_source.MoveNext())
            {
                object sourceObject = _source.Current();
                if (sourceObject is SimpleNode)
                {
                    var simpleNode = sourceObject as SimpleNode;

                    // expand the bounding box if needed.
                    if (simpleNode.Latitude.HasValue && simpleNode.Longitude.HasValue)
                    {
                        // location data is available.
                        var location = new GeoCoordinate(simpleNode.Latitude.Value, simpleNode.Longitude.Value);
                        if (_box == null)
                        {
                            // the box does not exist.
                            _box = new GeoCoordinateBox(location, location);
                        }
                        else if (!_box.IsInside(location))
                        {
                            // box exists but is too small.
                            var cornersPlus = new GeoCoordinate[5];
                            cornersPlus[0] = _box.Corners[0];
                            cornersPlus[1] = _box.Corners[1];
                            cornersPlus[2] = _box.Corners[2];
                            cornersPlus[3] = _box.Corners[3];
                            cornersPlus[4] = location;

                            _box = new GeoCoordinateBox(cornersPlus);
                        }
                    }
                    AddNode(simpleNode);
                }
                else if (sourceObject is SimpleWay)
                    AddWay(sourceObject as SimpleWay);
                else if (sourceObject is SimpleRelation)
                    AddRelation(sourceObject as SimpleRelation);
            }
            Close();
        }

        /// <summary>
        ///     Closes the current target.
        /// </summary>
        public abstract void Close();
    }
}