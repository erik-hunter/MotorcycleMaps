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

using OsmSharp.Routing.Route;
using OsmSharp.Routing.ArcAggregation.Output;
using OsmSharp.Routing;
using OsmSharp.Routing.Interpreter;

namespace OsmSharp.Routing.Metrics
{
    /// <summary>
    /// Calculates route metrics.
    /// </summary>
    public abstract class OsmSharpRouteMetricCalculator
    {
        /// <summary>
        /// Holds a routing interpreter.
        /// </summary>
        private IRoutingInterpreter _interpreter;

        /// <summary>
        /// Creates a new metrics calculator.
        /// </summary>
        /// <param name="interpreter"></param>
        protected OsmSharpRouteMetricCalculator(IRoutingInterpreter interpreter)
        {
            _interpreter = interpreter;
        }

        /// <summary>
        /// Calculates metrics for the given route.
        /// </summary>
        /// <param name="route"></param>
        /// <returns></returns>
        public Dictionary<string, double> Calculate(OsmSharpRoute route)
        {
            OsmSharp.Routing.ArcAggregation.ArcAggregator aggregator = 
                new OsmSharp.Routing.ArcAggregation.ArcAggregator(_interpreter);
            AggregatedPoint p = aggregator.Aggregate(route);
            return this.Calculate(route.Vehicle, p);
        }

        /// <summary>
        /// Returns the routing interpreter.
        /// </summary>
        protected IRoutingInterpreter Interpreter
        {
            get
            {
                return _interpreter;
            }
        }

        /// <summary>
        /// Does the metric calculations.
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public abstract Dictionary<string, double> Calculate(VehicleEnum vehicle, AggregatedPoint p);
    }
}
