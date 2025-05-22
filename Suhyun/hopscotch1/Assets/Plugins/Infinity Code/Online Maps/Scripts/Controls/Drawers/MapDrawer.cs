/*         INFINITY CODE         */
/*   https://infinity-code.com   */

namespace OnlineMaps
{
    /// <summary>
    /// Abstract class for map drawer functionality.
    /// </summary>
    public abstract class MapDrawer: IMapDrawer
    {
        /// <summary>
        /// Disposes the map drawer.
        /// </summary>
        public abstract void Dispose();
        
        /// <summary>
        /// Draws the map.
        /// </summary>
        public abstract void Draw();
        
        /// <summary>
        /// Draws the drawer elements on the map.
        /// </summary>
        public abstract void DrawElements();
        
        /// <summary>
        /// Initializes the map drawer.
        /// </summary>
        public abstract void Initialize();
    }
}