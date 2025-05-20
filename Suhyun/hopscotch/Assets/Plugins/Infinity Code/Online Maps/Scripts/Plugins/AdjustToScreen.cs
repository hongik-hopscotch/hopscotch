/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using UnityEngine;
using UnityEngine.UI;

namespace OnlineMaps
{
    /// <summary>
    /// Adjusts map size to fit screen.
    /// </summary>
    [AddComponentMenu("Infinity Code/Online Maps/Plugins/Adjust to Screen")]
    [Plugin("Adjust to Screen")]
    public class AdjustToScreen : MonoBehaviour
    {
        /// <summary>
        /// Use half the size of the display source. Strongly improves performance.
        /// </summary>
        [Header("Recommended for 2D Controls")]
        public bool halfSize;

        /// <summary>
        /// Makes the map square with side size equal to the maximum value of the screen width and height.
        /// </summary>
        [Header("To not see the edges when rotating the map")]
        public bool useMaxSide;

        /// <summary>
        /// When set, the forwarder texture size will be used instead of the screen size.
        /// </summary>
        [Header("Optional")]
        public RawImageTouchForwarder forwarder;

        /// <summary>
        /// A camera reference that draws a map when you have multiple cameras in a scene.
        /// </summary>
        public Camera mapCamera;

        private int screenWidth;
        private int screenHeight;
        private Map map;
        private ControlBase control;
        private CameraOrbit cameraOrbit;

        private void GetScreenSize(out int width, out int height)
        {
            if (!forwarder)
            {
                width = Screen.width;
                height = Screen.height;
            }
            else
            {
                Vector3[] fourCorners = new Vector3[4];
                forwarder.image.rectTransform.GetWorldCorners(fourCorners);
                Vector3 size = fourCorners[2] - fourCorners[0];
                width = Mathf.RoundToInt(size.x);
                height = Mathf.RoundToInt(size.y);
            }
        }

        private void ResizeMap(int newWidth, int newHeight)
        {
            screenWidth = newWidth;
            screenHeight = newHeight;

            int width = newWidth / 256 * 256;
            int height = newHeight / 256 * 256;

            int zoom = map.view.intZoom;
        
            if (halfSize)
            {
                width = width / 512 * 256;
                height = height / 512 * 256;
            }

            if (newWidth % 256 != 0) width += 256;
            if (newHeight % 256 != 0) height += 256;

            if (useMaxSide) width = height = Mathf.Max(width, height);

            if (height > (1 << zoom) * Constants.TileSize)
            {
                zoom = Mathf.CeilToInt(Mathf.Log(height, 2) - 8);
            }

            if (width > (1 << zoom) * Constants.TileSize)
            {
                zoom = Mathf.CeilToInt(Mathf.Log(width, 2) - 8);
            }

            int viewWidth = width;
            int viewHeight = height;

            if (halfSize)
            {
                viewWidth *= 2;
                viewHeight *= 2;
            }

            if (map.view.intZoom != zoom) map.view.intZoom = zoom;

            ITextureControl textureControl = control as ITextureControl;
            Texture2D texture = textureControl?.texture;
            if (texture)
            {
                Utils.Destroy(texture);
                if (control is UIImageControl)
                {
                    Utils.Destroy(GetComponent<Image>().sprite);
                }
                else if (control is SpriteRendererControl)
                {
                    Utils.Destroy(GetComponent<SpriteRenderer>().sprite);
                }

                texture = new Texture2D(width, height, TextureFormat.RGB24, false);
                textureControl.SetTexture(texture);

                if (control is UIRawImageControl)
                {
                    RectTransform rt = transform as RectTransform;
                    rt.sizeDelta = new Vector2(viewWidth, viewHeight);
                }
                else if (control is UIImageControl)
                {
                    RectTransform rt = transform as RectTransform;
                    rt.sizeDelta = new Vector2(viewWidth, viewHeight);
                }
                else if (control is SpriteRendererControl)
                {
                    GetComponent<BoxCollider>().size = new Vector3(viewWidth / 100f, viewHeight / 100f, 0.2f);
                }

                map.RedrawImmediately();
            }
            else if (control is TileSetControl)
            {
                TileSetControl ts = control as TileSetControl;

                ts.Resize(width, height, viewWidth, viewHeight);
                if (ts.currentCamera.orthographic) ts.currentCamera.orthographicSize = newHeight / 2f;
                else if (cameraOrbit) cameraOrbit.distance = newHeight * 0.8f;

                if (forwarder)
                {
                    RenderTexture targetTexture = forwarder.targetTexture;
                    if (targetTexture.width != newWidth || targetTexture.height != newHeight)
                    {
                        targetTexture.Release();
                        targetTexture = new RenderTexture(newWidth, newHeight, 32);
                        forwarder.targetTexture = targetTexture;
                        forwarder.image.texture = targetTexture;
                        if (mapCamera) mapCamera.targetTexture = targetTexture;
                    }
                }
            }
        }

        private void Start()
        {
            map = GetComponent<Map>();
            control = map.control;
            cameraOrbit = GetComponent<CameraOrbit>();

            int width, height;
            GetScreenSize(out width, out height);
            ResizeMap(width, height);
        }

        private void Update()
        {
            int width, height;
            GetScreenSize(out width, out height);
            if (screenWidth == width && screenHeight == height) return;

            ResizeMap(width, height);
        }
    }
}