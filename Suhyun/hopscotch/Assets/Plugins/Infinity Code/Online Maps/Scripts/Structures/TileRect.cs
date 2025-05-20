/*         INFINITY CODE         */
/*   https://infinity-code.com   */

namespace OnlineMaps
{
    /// <summary>
    /// Represents a rectangular area in tile coordinates.
    /// </summary>
    public struct TileRect
    {
        /// <summary>
        /// Left boundary of the rectangle.
        /// </summary>
        public double left;

        /// <summary>
        /// Top boundary of the rectangle.
        /// </summary>
        public double top;

        /// <summary>
        /// Right boundary of the rectangle.
        /// </summary>
        public double right;

        /// <summary>
        /// Bottom boundary of the rectangle.
        /// </summary>
        public double bottom;

        /// <summary>
        /// Zoom level of the rectangle.
        /// </summary>
        public int zoom;

        /// <summary>
        /// Gets the bottom-left point of the rectangle.
        /// </summary>
        public TilePoint bottomLeft => new TilePoint(left, bottom, zoom);

        /// <summary>
        /// Gets the bottom-right point of the rectangle.
        /// </summary>
        public TilePoint bottomRight => new TilePoint(right, bottom, zoom);

        /// <summary>
        /// Gets the center point of the rectangle.
        /// </summary>
        public TilePoint center => new TilePoint((left + right) / 2, (top + bottom) / 2, zoom);

        /// <summary>
        /// Gets the height of the rectangle.
        /// </summary>
        public double height => bottom - top;

        /// <summary>
        /// Gets the size of the rectangle.
        /// </summary>
        public Vector2d size => new Vector2d(width, height);

        /// <summary>
        /// Gets the top-left point of the rectangle.
        /// </summary>
        public TilePoint topLeft => new TilePoint(left, top, zoom);

        /// <summary>
        /// Gets the top-right point of the rectangle.
        /// </summary>
        public TilePoint topRight => new TilePoint(right, top, zoom);

        /// <summary>
        /// Gets the width of the rectangle.
        /// </summary>
        public double width => right - left;

        /// <summary>
        /// Initializes a new instance of the TileRect struct.
        /// </summary>
        /// <param name="left">Left boundary of the rectangle.</param>
        /// <param name="top">Top boundary of the rectangle.</param>
        /// <param name="right">Right boundary of the rectangle.</param>
        /// <param name="bottom">Bottom boundary of the rectangle.</param>
        /// <param name="zoom">Zoom level of the rectangle.</param>
        public TileRect(double left, double top, double right, double bottom, int zoom)
        {
            this.left = left;
            this.top = top;
            this.right = right;
            this.bottom = bottom;
            this.zoom = zoom;
        }

        /// <summary>
        /// Initializes a new instance of the TileRect struct.
        /// </summary>
        /// <param name="topLeft">Top-left point of the rectangle.</param>
        /// <param name="bottomRight">Bottom-right point of the rectangle.</param>
        public TileRect(TilePoint topLeft, TilePoint bottomRight)
        {
            if (topLeft.zoom != bottomRight.zoom) bottomRight = bottomRight.ToZoom(topLeft.zoom);
            left = topLeft.x;
            top = topLeft.y;
            right = bottomRight.x;
            bottom = bottomRight.y;
            zoom = topLeft.zoom;
        }

        /// <summary>
        /// Determines whether the specified rectangle intersects with this rectangle.
        /// </summary>
        /// <param name="rect">The rectangle to check for intersection.</param>
        /// <returns>True if the rectangles intersect; otherwise, false.</returns>
        public bool Intersects(TileRect rect)
        {
            return !(rect.left > right || rect.right < left || rect.top > bottom || rect.bottom < top);
        }

        /// <summary>
        /// Linearly interpolates between the left and right boundaries and the top and bottom boundaries.
        /// </summary>
        /// <param name="x">The x-coordinate to interpolate.</param>
        /// <param name="y">The y-coordinate to interpolate.</param>
        /// <returns>A TilePoint representing the interpolated point.</returns>
        public TilePoint Lerp(double x, double y)
        {
            return new TilePoint((x - left) / width, (y - top) / height, zoom);
        }

        /// <summary>
        /// Linearly interpolates between the left and right boundaries and the top and bottom boundaries.
        /// </summary>
        /// <param name="point">The point to interpolate.</param>
        /// <returns>A TilePoint representing the interpolated point.</returns>
        public TilePoint Lerp(Vector2d point)
        {
            return Lerp(point.x, point.y);
        }
        
        public override string ToString()
        {
            return $"(left: {left.ToString(Culture.numberFormat)}, top: {top.ToString(Culture.numberFormat)}, right: {right}, bottom: {bottom.ToString(Culture.numberFormat)})";
        }
    }
}