/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using UnityEngine;

namespace OnlineMaps
{
    /// <summary>
    /// The main properties of the map
    /// </summary>
    public struct StateProps
    {
        /// <summary>
        /// Coordinates of the center point
        /// </summary>
        public GeoPoint center;

        /// <summary>
        /// Rect of the view
        /// </summary>
        public GeoRect rect;

        /// <summary>
        /// Width of the map
        /// </summary>
        public int width;

        /// <summary>
        /// Height of the map
        /// </summary>
        public int height;

        /// <summary>
        /// The scaling factor for zoom
        /// </summary>
        public float zoomFactor;

        /// <summary>
        /// The fractional part of zoom
        /// </summary>
        public float zoomFractional;

        private float _zoom;

        /// <summary>
        /// The number of tiles in the current zoom level
        /// </summary>
        public int countTiles => 1 << intZoom;

        /// <summary>
        /// The integer part of zoom
        /// </summary>
        public int intZoom => (int) _zoom;

        /// <summary>
        /// Float zoom
        /// </summary>
        public float zoom
        {
            get => _zoom;
            set
            {
                _zoom = value;
                zoomFractional = _zoom - intZoom;
                zoomFactor = Mathf.Pow(2, -zoomFractional);
            }
        }
    }
}