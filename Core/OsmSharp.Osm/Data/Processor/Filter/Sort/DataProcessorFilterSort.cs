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
using OsmSharp.Osm.Data.Core.Processor;
using OsmSharp.Osm.Simple;

namespace OsmSharp.Osm.Data.Processor.Filter.Sort
{
    /// <summary>
    ///     A data processing filter that sorts the OSM data: Node -> Way -> Relation.
    /// </summary>
    public class DataProcessorFilterSort : DataProcessorFilter
    {
        /// <summary>
        ///     The current type.
        /// </summary>
        private SimpleOsmGeoType _currentType = SimpleOsmGeoType.Node;

        /// <summary>
        ///     Returns true if this source can be reset.
        /// </summary>
        public override bool CanReset
        {
            get { return Source.CanReset; }
        }

        /// <summary>
        ///     Initializes this filter.
        /// </summary>
        public override void Initialize()
        {
            if (Source == null)
                throw new Exception("No source registered!");
            Source.Initialize();
        }

        /// <summary>
        ///     Move to the next object.
        /// </summary>
        /// <returns></returns>
        public override bool MoveNext()
        {
            if (Source.MoveNext())
            {
                bool finished = false;
                while (Current().Type != _currentType)
                {
                    if (!Source.MoveNext())
                    {
                        finished = true;
                        break;
                    }
                }

                if (!finished && Current().Type == _currentType)
                    return true;
            }

            switch (_currentType)
            {
                case SimpleOsmGeoType.Node:
                    Source.Reset();
                    _currentType = SimpleOsmGeoType.Way;
                    return MoveNext();
                case SimpleOsmGeoType.Way:
                    Source.Reset();
                    _currentType = SimpleOsmGeoType.Relation;
                    return MoveNext();
                case SimpleOsmGeoType.Relation:
                    return false;
            }
            throw new InvalidOperationException("Unkown SimpleOsmGeoType");
        }

        /// <summary>
        ///     Returns the current object.
        /// </summary>
        /// <returns></returns>
        public override SimpleOsmGeo Current()
        {
            return Source.Current();
        }

        /// <summary>
        ///     Resets this filter.
        /// </summary>
        public override void Reset()
        {
            _currentType = SimpleOsmGeoType.Node;
            Source.Reset();
        }
    }
}