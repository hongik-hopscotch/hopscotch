/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using UnityEngine;

namespace OnlineMaps
{
    /// <summary>
    /// Implements the display of 3D markers
    /// </summary>
    public class Marker3DDrawer : MarkerDrawerBase
    {
        private ControlBase3D control;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="control">Reference to 3D control</param>
        public Marker3DDrawer(ControlBase3D control)
        {
            this.control = control;
            map = control.map;
            control.OnUpdate3DMarkers += Update3DMarkers;
        }

        public override void Dispose()
        {
            base.Dispose();
            control.OnUpdate3DMarkers -= Update3DMarkers;
            control = null;
        }

        private void Update3DMarkers()
        {
            Marker3DManager manager = control.marker3DManager;
            if (!manager || !manager.enabled) return;
            if (!control.collider) return;

            int zoom = map.view.intZoom;

            GeoRect r = map.view.rect;
            TileRect tr = map.view.tileRect;

            long maxX = 1 << zoom;

            bool isEntireWorld = map.buffer.renderState.width == maxX * Constants.TileSize;
            if (isEntireWorld && Math.Abs(r.left - r.right) < 180)
            {
                if (r.left < 0)
                {
                    r.right += 360;
                    tr.right += maxX;
                }
                else
                {
                    r.left -= 360;
                    tr.left -= maxX;
                }
            }

            Bounds bounds = control.meshFilter.sharedMesh.bounds;
            float bestYScale = ElevationManagerBase.GetElevationScale(r, _elevationManager);

            for (int i = manager.count - 1; i >= 0; i--)
            {
                Marker3D marker = manager[i];
                if (marker.manager == null) marker.manager = manager;
                marker.Update(bounds, r, zoom, tr, bestYScale);
            }
        }
    }
}