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
using OsmSharp.Tools.Math;
using OsmSharp.Tools.Math.Geo;

namespace OsmSharp.Osm.Renderer
{
    /// <summary>
    /// A simple view class converting geocoordinates to screen coordinates.
    /// 
    /// This view is kept for backward compat. purposes but is not to be used anymore! The projection is incorrect and inconsistent.
    /// </summary>
    public class View
    {
        /// <summary>
        /// Holds the box this view is for.
        /// </summary>
        private GeoCoordinateBox _box;

        /// <summary>
        /// The zoom factor of this view.
        /// </summary>
        private float _zoom_factor;

        /// <summary>
        /// Creates a new view.
        /// </summary>
        /// <param name="box"></param>
        public View(GeoCoordinateBox box, float zoom_factor)
        {
            _box = box;
            _zoom_factor = zoom_factor;
        }

        /// <summary>
        /// Returns the current zoom factor.
        /// </summary>
        public float ZoomFactor
        {
            get
            {
                return _zoom_factor;
            }
        }

        /// <summary>
        /// Returns the box this view is showing.
        /// </summary>
        public GeoCoordinateBox Box
        {
            get
            {
                return _box;
            }
        }

        /// <summary>
        /// Converts a geo coordinate to projected two dimension versions on the target.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public PointF2D ConvertToTargetCoordinates(
            ITarget target,
            GeoCoordinate point)
        {
            double resolution_per_x_degree = ((double)target.XRes) / this.Box.DeltaLon;

            // TODO: explain the arbirtrairy parameter and the actual projection used by OsmSharp.Osm.
            double resolution_per_y_degree = ((double)target.YRes / this.Box.DeltaLat);

            double angle_div_x = (point.Longitude - _box.MinLon);
            double angle_div_y = (point.Latitude - _box.MaxLat);
            double x = angle_div_x * resolution_per_x_degree;
            double y = -(angle_div_y * resolution_per_y_degree); // x-axis and latitude-axis differ in direction.

            return new PointF2D((float)x,(float)y);
        }

        /// <summary>
        /// Converts the given geo coordinates to projected two dimension versions on the target.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="points"></param>
        /// <returns></returns>
        public PointF2D[] ConvertToTargetCoordinates(
            ITarget target,
            GeoCoordinate[] points)
        {
            PointF2D[] target_points = new PointF2D[points.Length];

            for (int idx = 0; idx < points.Length; idx++)
            {
                target_points[idx] = this.ConvertToTargetCoordinates(target, points[idx]);
            }

            return target_points;
        }

        public float ConvertToTargetXSize(ITarget target, double size)
        {
            double resolution_per_x_degree = ((double)target.XRes) / this.Box.DeltaLon;
            return (float)(size * resolution_per_x_degree);
        }

        public float ConvertToTargetYSize(ITarget target, double size)
        {
            double resolution_per_y_degree = ((double)target.YRes) / this.Box.DeltaLat;
            return (float)(size * resolution_per_y_degree);
        }

        public float ConvertFromTargetXSize(ITarget target, double delta_x)
        {
            double resolution_per_x_degree = ((double)target.XRes) / this.Box.DeltaLon;
            return (float)(delta_x / resolution_per_x_degree);
        }

        public float ConvertFromTargetYSize(ITarget target, double delta_y)
        {
            double resolution_per_y_degree = ((double)target.YRes) / this.Box.DeltaLat;
            return (float)(delta_y / resolution_per_y_degree);
        }

        public GeoCoordinate ConvertFromTargetCoordinates(ITarget target,
            double x,
            double y)
        {
            GeoCoordinate top_left = this.Box.Corners[1];

            double resolution_per_x_degree = (target.XRes) / this.Box.DeltaLon;
            double resolution_per_y_degree = (target.YRes / this.Box.DeltaLat);

            double angle_div_x = -x / resolution_per_x_degree;
            double angle_div_y = (y / resolution_per_y_degree); // x-axis and latitude-axis differ in direction.

            double longitude = (top_left.Longitude - angle_div_x);
            double latitude = (top_left.Latitude - angle_div_y);

            return new GeoCoordinate(latitude, longitude);
        }

        public static View CreateFrom(ITarget target, 
            float zoom_factor,
            GeoCoordinate center)
        {
            double pixels_per_zoom = 256;

            double degrees_per_zoom = (360f / System.Math.Pow(2, zoom_factor));
            double degrees_per_pixel = degrees_per_zoom / pixels_per_zoom;
            double degrees_x = (target.XRes * degrees_per_pixel);

            double degrees_y = (target.YRes * degrees_per_pixel / 1.5f);

            return new View(new GeoCoordinateBox(
                new GeoCoordinate(
                center.Latitude - degrees_y / 2.0f,
                center.Longitude - degrees_x / 2.0f),
                new GeoCoordinate(
                center.Latitude + degrees_y / 2.0f,
                center.Longitude + degrees_x / 2.0f)),
                zoom_factor);
        }

        public GeoCoordinateBox CreateBoxAround(ITarget target, GeoCoordinate coordinate, int pixels)
        {
            PointF2D center = this.ConvertToTargetCoordinates(target, coordinate);

            GeoCoordinate top_left = this.ConvertFromTargetCoordinates(target, (center[0] - pixels), (center[1] - pixels));
            GeoCoordinate bottom_right = this.ConvertFromTargetCoordinates(target, (center[0] + pixels),(center[1] + pixels));

            return new GeoCoordinateBox(top_left, bottom_right);
        }
    }
}