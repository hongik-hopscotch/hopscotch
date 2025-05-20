/*         INFINITY CODE         */
/*   https://infinity-code.com   */

namespace OnlineMaps
{
    /// <summary>
    /// Interface for map drawer functionality.
    /// </summary>
    public interface IMapDrawer
    {
        /// <summary>
        /// Disposes the map drawer.
        /// </summary>
        void Dispose();

        /// <summary>
        /// Draws the map.
        /// </summary>
        void Draw();

        /// <summary>
        /// Draws the map elements.
        /// </summary>
        void DrawElements();

        /// <summary>
        /// Initializes the map drawer.
        /// </summary>
        void Initialize();
    }
}