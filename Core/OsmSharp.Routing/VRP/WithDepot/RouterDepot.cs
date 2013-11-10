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
using OsmSharp.Routing;
using OsmSharp.Routing.Route;
using OsmSharp.Tools.Math.Geo;

namespace OsmSharp.Routing.VRP.WithDepot
{
    /// <summary>
    /// Class to solve for a specific class of VRP problems: VRP problems with multi depot.
    /// </summary>
    public abstract class RouterDepot : RouterVRP
    {
        /// <summary>
        /// Creates a VRP router without a depot.
        /// </summary>
        public RouterDepot()
        {

        }

        /// <summary>
        /// Calculates this VRP No Depot.
        /// </summary>
        /// <param name="weights">The weights between all customer pairs incuding the depot.</param>
        /// <param name="locations">The location between of customers and the depot.</param>
        /// <returns></returns>
        public abstract int[][] CalculateDepot(double[][] weights, GeoCoordinate[] locations);
    }
}