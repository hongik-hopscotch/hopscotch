/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using UnityEngine;

namespace OnlineMaps
{
    /// <summary>
    /// Implements the use of elevation data from Mapbox
    /// </summary>
    [Plugin("Mapbox Elevations", typeof(ControlBaseDynamicMesh), "Elevations")]
    [AddComponentMenu("Infinity Code/Online Maps/Elevations/Mapbox Elevation Manager")]
    public class MapboxElevationManager : TiledElevationManager<MapboxElevationManager>
    {
        protected override string cachePrefix => "mapbox_elevation_";
        protected override int tileWidth => 256;

        protected override int tileHeight => 256;


        public override void CancelCurrentElevationRequest()
        {
            // TODO Implement this
        }

        private void OnTileDownloaded(Tile tile, WebRequest www)
        {
            if (www.hasError)
            {
                if (OnElevationFails != null) OnElevationFails(www.error);
                Debug.Log("Download error");
                return;
            }

            if (OnDownloadSuccess != null) OnDownloadSuccess(tile, www);
        
            Texture2D texture = new Texture2D(256, 256, TextureFormat.RGB24, false);
            texture.LoadImage(www.bytes);

            SetElevationTexture(tile, texture);
            Utils.Destroy(texture);

            if (OnElevationUpdated != null) OnElevationUpdated();
        }

        /// <summary>
        /// Sets the elevation texture for the tile.
        /// </summary>
        /// <param name="tile">Tile</param>
        /// <param name="texture">Texture</param>
        public void SetElevationTexture(Tile tile, Texture2D texture)
        {
            const int res = 256;

            if (texture.width != res || texture.height != res)
            {
                Debug.Log("Texture size != res");
                return;
            }

            Color[] colors = texture.GetPixels();

            tile.elevations = new short[tile.width, tile.height];

            short max = short.MinValue;
            short min = short.MaxValue;

            for (int y = 0; y < res; y++)
            {
                int py = (255 - y) * res;

                for (int x = 0; x < res; x++)
                {
                    Color c = colors[py + x];

                    double height = -10000 + (c.r * 255 * 256 * 256 + c.g * 255 * 256 + c.b * 255) * 0.1;
                    short v = (short) Math.Round(height);
                    tile.elevations[x, y] = v;
                    if (v < min) min = v;
                    if (v > max) max = v;
                }
            }

            SetElevationToCache(tile, tile.elevations);

            tile.minValue = min;
            tile.maxValue = max;

            tile.loaded = true;
            needUpdateMinMax = true;

            CheckAllTilesLoaded();

            map.Redraw();
        }

        protected override void Start()
        {
            base.Start();

            if (!KeyManager.hasMapbox)
            {
                Debug.LogWarning("Missing Key Manager / Mapbox Access Token.");
            }
        }

        public override void StartDownloadElevationTile(Tile tile)
        {
            string token = KeyManager.Mapbox();
            if (string.IsNullOrEmpty(token)) return;
            
            string url = "https://api.mapbox.com/v4/mapbox.terrain-rgb/" + tile.zoom + "/" + tile.x + "/" + tile.y + ".pngraw?access_token=" + token;
            WebRequest www = new WebRequest(url);
            www.OnComplete += delegate { OnTileDownloaded(tile, www); };

            if (OnElevationRequested != null) OnElevationRequested();
        }
    }
}