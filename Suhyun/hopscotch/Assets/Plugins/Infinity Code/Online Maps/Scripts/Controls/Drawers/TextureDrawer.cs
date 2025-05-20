/*         INFINITY CODE         */
/*   https://infinity-code.com   */

namespace OnlineMaps
{
    /// <summary>
    /// Draws the map on the texture.
    /// </summary>
    public class TextureDrawer : MapDrawer
    {
        private ITextureControl control;
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="control">The control to draw the texture on.</param>
        public TextureDrawer(ITextureControl control)
        {
            this.control = control;
        }

        public override void Dispose()
        {
            control = null;
        }

        public override void Draw()
        {
            
        }

        public override void DrawElements()
        {
            
        }

        public override void Initialize()
        {
            
        }
    }
}