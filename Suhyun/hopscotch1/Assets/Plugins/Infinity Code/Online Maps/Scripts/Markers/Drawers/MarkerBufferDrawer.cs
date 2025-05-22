/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OnlineMaps
{
    /// <summary>
    /// Implements drawing markers in the buffer
    /// </summary>
    public class MarkerBufferDrawer : Marker2DDrawer
    {
        /// <summary>
        /// Allows you to change the order of drawing markers.
        /// </summary>
        public Func<IEnumerable<Marker2D>, IEnumerable<Marker2D>> OnSortMarker;

        private ControlBase control;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="control">Reference to control</param>
        public MarkerBufferDrawer(ControlBase control)
        {
            this.control = control;
            map = control.map;
            control.OnDrawMarkers += Draw;
        }

        /// <summary>
        /// Dispose the current drawer
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
            control.OnDrawMarkers -= Draw;
            control = null;
            OnSortMarker = null;
        }

        private void Draw()
        {
            if (!control.marker2DManager) return;

            const float tileSize = Constants.TileSize;

            Vector2Int bufferPosition = map.buffer.bufferPosition;
            Vector2Int frontBufferPosition = map.buffer.frontBufferPosition;
            int bufferZoom = map.buffer.renderState.intZoom;

            GeoPoint tl = map.view.projection.TileToLocation(bufferPosition.x + frontBufferPosition.x / tileSize, bufferPosition.y + frontBufferPosition.y / tileSize, bufferZoom);
            GeoPoint br = map.view.projection.TileToLocation(bufferPosition.x + (frontBufferPosition.x + map.buffer.renderState.width) / tileSize, bufferPosition.y + (frontBufferPosition.y + control.height) / tileSize, bufferZoom);
            GeoRect r = new GeoRect(tl, br).rightFixed;

            IEnumerable<Marker2D> usedMarkers = control.marker2DManager.Where(m => m.enabled && m.range.InRange(bufferZoom));
            usedMarkers = OnSortMarker != null ? OnSortMarker(usedMarkers) : usedMarkers.OrderByDescending(m => m, new MarkerComparer());

            foreach (Marker2D marker in usedMarkers.ToArray()) SetMarkerToBuffer(marker, bufferPosition, frontBufferPosition, r);
        }

        private void SetMarkerToBuffer(Marker2D marker, Vector2Int bufferPosition, Vector2Int frontBufferPosition, GeoRect rect)
        {
            const int s = Constants.TileSize;
            
            GeoPoint p = marker.location;

            Buffer buffer = map.buffer;
            StateProps state = buffer.renderState;
            long countTiles = state.countTiles;

            bool isEntireWorld = state.width == countTiles * s;
            bool isBiggestThatBuffer = state.width + 512 == countTiles * s;

            if (isEntireWorld || isBiggestThatBuffer)
            {

            }
            else if (!rect.ContainsWrapped(p)) return;

#if !UNITY_WEBGL
            int maxCount = 20;
            while (marker.locked && maxCount > 0)
            {
                Compatibility.ThreadSleep(1);
                maxCount--;
            }
#endif

            marker.locked = true;

            TilePoint tm = p.ToTile(map) - bufferPosition;

            if (isEntireWorld)
            {
                TilePoint t = state.center.ToTile(map, state.intZoom);
                t.x -= state.width / s / 2;

                if (tm.x < t.x) tm.x += countTiles;
            }
            else
            {
                if (tm.x < 0) tm.x += countTiles;
                else if (tm.x > countTiles) tm.x -= countTiles;
            }

            float zoomFactor = state.zoomFactor;

            tm *= s;

            int ipx = (int)((tm.x - frontBufferPosition.x) / zoomFactor);
            int ipy = (int)((tm.y - frontBufferPosition.y) / zoomFactor);

            Vector2Int ip = marker.GetAlignedPosition(ipx, ipy);

            Color32[] markerColors = marker.colors;
            if (markerColors == null || markerColors.Length == 0) return;

            int markerWidth = marker.width;
            int markerHeight = marker.height;

            for (int y = 0; y < marker.height; y++)
            {
                if (ip.y + y < 0 || ip.y + y >= control.height) continue;

                int cy = (markerHeight - y - 1) * markerWidth;

                for (int x = 0; x < marker.width; x++)
                {
                    if (ip.x + x < 0 || ip.x + x >= state.width) continue;

                    try
                    {
                        buffer.SetColorToBuffer(markerColors[cy + x], ip, x, y);
                    }
                    catch
                    {
                    }
                }
            }

            if (isEntireWorld)
            {
                ip.x -= (int)(state.width / zoomFactor);
                for (int y = 0; y < marker.height; y++)
                {
                    if (ip.y + y < 0 || ip.y + y >= control.height) continue;

                    int cy = (markerHeight - y - 1) * markerWidth;

                    for (int x = 0; x < marker.width; x++)
                    {
                        if (ip.x + x < 0 || ip.x + x >= state.width) continue;

                        try
                        {
                            buffer.SetColorToBuffer(markerColors[cy + x], ip, x, y);
                        }
                        catch
                        {
                        }
                    }
                }

                ip.x += (int)(state.width * 2 / zoomFactor);
                for (int y = 0; y < marker.height; y++)
                {
                    if (ip.y + y < 0 || ip.y + y >= control.height) continue;

                    int cy = (markerHeight - y - 1) * markerWidth;

                    for (int x = 0; x < marker.width; x++)
                    {
                        if (ip.x + x < 0 || ip.x + x >= state.width) continue;

                        try
                        {
                            buffer.SetColorToBuffer(markerColors[cy + x], ip, x, y);
                        }
                        catch
                        {
                        }
                    }
                }
            }

            marker.locked = false;
        }

        internal class MarkerComparer : IComparer<Marker>
        {
            public int Compare(Marker m1, Marker m2)
            {
                GeoPoint p1 = m1.location;
                GeoPoint p2 = m2.location;

                double oy = p1.y - p2.y;
                return Math.Abs(oy) < double.Epsilon ? Math.Sign(p2.x - p1.x) : Math.Sign(oy);
            }
        }
    }
}