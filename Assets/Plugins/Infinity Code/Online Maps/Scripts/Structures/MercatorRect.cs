/*         INFINITY CODE         */
/*   https://infinity-code.com   */

namespace OnlineMaps
{
    /// <summary>
    /// Represents a rectangular area in Mercator projection coordinates.
    /// </summary>
    public struct MercatorRect
    {
        /// <summary>
        /// The left boundary of the rectangle.
        /// </summary>
        public double left;

        /// <summary>
        /// The top boundary of the rectangle.
        /// </summary>
        public double top;

        /// <summary>
        /// The right boundary of the rectangle.
        /// </summary>
        public double right;

        /// <summary>
        /// The bottom boundary of the rectangle.
        /// </summary>
        public double bottom;

        /// <summary>
        /// Gets the bottom-left corner of the rectangle.
        /// </summary>
        public MercatorPoint bottomLeft => new MercatorPoint(left, bottom);

        /// <summary>
        /// Gets the bottom-right corner of the rectangle.
        /// </summary>
        public MercatorPoint bottomRight => new MercatorPoint(right, bottom);

        /// <summary>
        /// Gets the center point of the rectangle.
        /// </summary>
        public MercatorPoint center => new MercatorPoint((left + right) / 2, (top + bottom) / 2);

        /// <summary>
        /// Gets the height of the rectangle.
        /// </summary>
        public double height => bottom - top;

        /// <summary>
        /// Gets the size of the rectangle as a vector.
        /// </summary>
        public Vector2d size => new Vector2d(width, height);

        /// <summary>
        /// Gets the top-left corner of the rectangle.
        /// </summary>
        public MercatorPoint topLeft => new MercatorPoint(left, top);

        /// <summary>
        /// Gets the top-right corner of the rectangle.
        /// </summary>
        public MercatorPoint topRight => new MercatorPoint(right, top);

        /// <summary>
        /// Gets the width of the rectangle.
        /// </summary>
        public double width => right - left;

        /// <summary>
        /// Initializes a new instance of the MercatorRect struct with the specified boundaries.
        /// </summary>
        /// <param name="left">The left boundary.</param>
        /// <param name="top">The top boundary.</param>
        /// <param name="right">The right boundary.</param>
        /// <param name="bottom">The bottom boundary.</param>
        public MercatorRect(double left, double top, double right, double bottom)
        {
            this.left = left;
            this.top = top;
            this.right = right;
            this.bottom = bottom;
        }

        /// <summary>
        /// Initializes a new instance of the MercatorRect struct with the specified top-left and bottom-right points.
        /// </summary>
        /// <param name="topLeft">The top-left point.</param>
        /// <param name="bottomRight">The bottom-right point.</param>
        public MercatorRect(MercatorPoint topLeft, MercatorPoint bottomRight)
        {
            left = topLeft.x;
            top = topLeft.y;
            right = bottomRight.x;
            bottom = bottomRight.y;
        }

        /// <summary>
        /// Determines whether this rectangle intersects with another rectangle.
        /// </summary>
        /// <param name="rect">The other rectangle.</param>
        /// <returns><c>true</c> if the rectangles intersect; otherwise, <c>false</c>.</returns>
        public bool Intersects(MercatorRect rect)
        {
            return !(rect.left > right || rect.right < left || rect.top > bottom || rect.bottom < top);
        }

        public override string ToString()
        {
            return $"(left: {left.ToString(Culture.numberFormat)}, top: {top.ToString(Culture.numberFormat)}, right: {right}, bottom: {bottom.ToString(Culture.numberFormat)})";
        }
    }
}