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
using OsmSharp.Tools.Math.Units.Angle;

namespace OsmSharp.Tools.Math.Geo.Meta
{
    /// <summary>
    /// Relative direction calculator.
    /// </summary>
    public static class RelativeDirectionCalculator
    {
        /// <summary>
        /// Calculates the relative direction.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="along"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static RelativeDirection Calculate(GeoCoordinate from, GeoCoordinate along, GeoCoordinate to)
        {
            RelativeDirection direction = new RelativeDirection();

            double margin = 65;
            double straight_on = 10;
            double turn_back = 5;

            GeoCoordinateLine line_from = new GeoCoordinateLine(from, along);
            GeoCoordinateLine line_to = new GeoCoordinateLine(along, to);

            Degree angle = line_from.Direction.Angle(line_to.Direction);

            if (angle >= new Degree(360 - straight_on)
                || angle < new Degree(straight_on))
            {
                direction.Direction = RelativeDirectionEnum.StraightOn;
            }
            else if (angle >= new Degree(straight_on)
                && angle < new Degree(90 - margin))
            {
                direction.Direction = RelativeDirectionEnum.SlightlyLeft;
            }
            else if (angle >= new Degree(90 - margin)
                && angle < new Degree(90 + margin))
            {
                direction.Direction = RelativeDirectionEnum.Left;
            }
            else if (angle >= new Degree(90 + margin)
                && angle < new Degree(180 - turn_back))
            {
                direction.Direction = RelativeDirectionEnum.SharpLeft;
            }
            else if (angle >= new Degree(180 - turn_back)
                && angle < new Degree(180 + turn_back))
            {
                direction.Direction = RelativeDirectionEnum.TurnBack;
            }
            else if (angle >= new Degree(180 + turn_back)
                && angle < new Degree(270-margin))
            {
                direction.Direction = RelativeDirectionEnum.SharpRight;
            }
            else if (angle >= new Degree(270 - margin)
                && angle < new Degree(270 + margin))
            {
                direction.Direction = RelativeDirectionEnum.Right;
            }
            else if (angle >= new Degree(270 + margin)
                && angle < new Degree(360- straight_on))
            {
                direction.Direction = RelativeDirectionEnum.SlightlyRight;
            }
            //direction.Direction = RelativeDirectionEnum.StraightOn;
            direction.Angle = angle;

            return direction;
        }
    }
}
