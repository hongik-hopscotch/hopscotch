/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using UnityEngine;

namespace OnlineMaps
{
    /// <summary>
    /// Represents a point in Mercator projection.
    /// </summary>
    public struct MercatorPoint
    {
        /// <summary>
        /// The X coordinate.
        /// </summary>
        public double x;

        /// <summary>
        /// The Y coordinate.
        /// </summary>
        public double y;

        /// <summary>
        /// Initializes a new instance of the MercatorPoint struct.
        /// </summary>
        /// <param name="x">The X coordinate.</param>
        /// <param name="y">The Y coordinate.</param>
        public MercatorPoint(double x, double y)
        {
            this.x = Mathd.Repeat01(x);
            this.y = Mathd.Clamp01(y);
        }

        /// <summary>
        /// Adds the specified values to the coordinates.
        /// </summary>
        /// <param name="x">The value to add to the X coordinate.</param>
        /// <param name="y">The value to add to the Y coordinate.</param>
        public void Add(double x, double y)
        {
            this.x = Mathd.Repeat01(this.x + x);
            this.y = Mathd.Clamp01(this.y + y);
        }
        
        /// <summary>
        /// Converts the specified meters to a Mercator point.
        /// </summary>
        /// <param name="meters">The meters to convert.</param>
        /// <returns>A MercatorPoint representing the converted meters.</returns>
        public static MercatorPoint FromMeters(Vector2d meters)
        {
            return new MercatorPoint(meters.x / Constants.EarthRadiusMeters, meters.y / Constants.EarthRadiusMeters);
        }
        
        /// <summary>
        /// Converts the specified meters to a Mercator point.
        /// </summary>
        /// <param name="x">The X coordinate in meters.</param>
        /// <param name="y">The Y coordinate in meters.</param>
        /// <returns>A MercatorPoint representing the converted meters.</returns>
        public static MercatorPoint FromMeters(double x, double y)
        {
            return new MercatorPoint(x / Constants.EarthRadiusMeters, y / Constants.EarthRadiusMeters);
        }

        /// <summary>
        /// Determines whether two rectangles intersect.
        /// </summary>
        /// <param name="topLeft1">The top-left corner of the first rectangle.</param>
        /// <param name="bottomRight1">The bottom-right corner of the first rectangle.</param>
        /// <param name="topLeft2">The top-left corner of the second rectangle.</param>
        /// <param name="bottomRight2">The bottom-right corner of the second rectangle.</param>
        /// <returns><c>true</c> if the rectangles intersect; otherwise, <c>false</c>.</returns>
        public static bool Intersects(MercatorPoint topLeft1, MercatorPoint bottomRight1, MercatorPoint topLeft2, MercatorPoint bottomRight2)
        {
            return !(topLeft1.x > bottomRight2.x || bottomRight1.x < topLeft2.x || topLeft1.y < bottomRight2.y || bottomRight1.y > topLeft2.y);
        }

        /// <summary>
        /// Subtracts the specified values from the coordinates.
        /// </summary>
        /// <param name="x">The value to subtract from the X coordinate.</param>
        /// <param name="y">The value to subtract from the Y coordinate.</param>
        public void Subtract(double x, double y)
        {
            this.x = Mathd.Repeat01(this.x - x);
            this.y = Mathd.Clamp01(this.y - y);
        }

        /// <summary>
        /// Converts the Mercator point to geographic coordinates.
        /// </summary>
        /// <param name="map">The map to use for the conversion.</param>
        /// <returns>A GeoPoint representing the geographic coordinates.</returns>
        public GeoPoint ToLocation(Map map)
        {
            return map.view.projection.MercatorToLocation(x, y);
        }

        /// <summary>
        /// Converts the Mercator point to meters.
        /// </summary>
        /// <returns>A meters in Web Mercator projection.</returns>
        public Vector2d ToMeters()
        {
            return new Vector2d(x * Constants.EarthRadiusMeters, y * Constants.EarthRadiusMeters);
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return $"x: {x.ToString(Culture.numberFormat)}, y: {y.ToString(Culture.numberFormat)}";
        }

        /// <summary>
        /// Converts the Mercator point to tile coordinates.
        /// </summary>
        /// <param name="zoom">The zoom level.</param>
        /// <returns>A TilePoint representing the tile coordinates.</returns>
        public TilePoint ToTile(int zoom)
        {
            return Projection.MercatorToTile(x, y, zoom);
        }

        /// <summary>
        /// Adds two Mercator points.
        /// </summary>
        /// <param name="a">The first Mercator point.</param>
        /// <param name="b">The second Mercator point.</param>
        /// <returns>A Vector2d representing the sum of the two points.</returns>
        public static Vector2d operator +(MercatorPoint a, MercatorPoint b)
        {
            return new Vector2d(a.x + b.x, a.y + b.y);
        }

        /// <summary>
        /// Subtracts one Mercator point from another.
        /// </summary>
        /// <param name="a">The first Mercator point.</param>
        /// <param name="b">The second Mercator point.</param>
        /// <returns>A Vector2d representing the difference between the two points.</returns>
        public static Vector2d operator -(MercatorPoint a, MercatorPoint b)
        {
            return new Vector2d(a.x - b.x, a.y - b.y);
        }

        /// <summary>
        /// Converts a MercatorPoint to a Vector2.
        /// </summary>
        /// <param name="point">The MercatorPoint to convert.</param>
        /// <returns>A Vector2 representing the converted MercatorPoint.</returns>
        public static implicit operator Vector2(MercatorPoint point)
        {
            return new Vector2((float)point.x, (float)point.y);
        }
    }
}