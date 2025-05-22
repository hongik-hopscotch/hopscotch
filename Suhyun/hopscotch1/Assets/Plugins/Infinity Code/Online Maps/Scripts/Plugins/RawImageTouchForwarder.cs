/*         INFINITY CODE         */
/*   https://infinity-code.com   */

/* Special thanks to Brian Chasalow and V for help in developing this script. */

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OnlineMaps
{
    /// <summary>
    /// Forwards touch events from a RawImage to a map.
    /// </summary>
    [AddComponentMenu("Infinity Code/Online Maps/Plugins/Raw Image Touch Forwarder")]
    public class RawImageTouchForwarder : MonoBehaviour
    {
        private static Vector3[] corners = new Vector3[4];

        /// <summary>
        /// The RawImage component to forward touch events from.
        /// </summary>
        public RawImage image;

        /// <summary>
        /// The map to forward touch events to.
        /// </summary>
        public Map map;

        /// <summary>
        /// The target RenderTexture for the RawImage.
        /// </summary>
        public RenderTexture targetTexture;

        private TileSetControl control;
        private MouseController mouseController;

        private static Vector2 pointerPos = Vector2.zero;
        private static GameObject target;

        /// <summary>
        /// Gets the world camera associated with the RawImage's canvas.
        /// </summary>
        public Camera worldCamera
        {
            get
            {
                if (!image.canvas || image.canvas.renderMode == RenderMode.ScreenSpaceOverlay) return null;
                return image.canvas.worldCamera;
            }
        }

        /// <summary>
        /// Converts a position from the forwarder space to the map space.
        /// </summary>
        /// <param name="position">The position in the forwarder space.</param>
        /// <returns>The position in the map space.</returns>
        public Vector2 ForwarderToMapSpace(Vector2 position)
        {
            Vector2 pos;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(image.rectTransform, position, worldCamera, out pos))
            {
                return pos;
            }

            Vector2 sizeDelta = GetSizeDelta(image.rectTransform);
            pos += new Vector2(sizeDelta.x / 2.0f, sizeDelta.y / 2.0f);

            if (!targetTexture)
            {
                pos.x *= Screen.width / sizeDelta.x;
                pos.y *= Screen.height / sizeDelta.y;
            }
            else
            {
                pos.x *= targetTexture.width / sizeDelta.x;
                pos.y *= targetTexture.height / sizeDelta.y;
            }

            return pos;
        }
        
        private static Vector2 GetSizeDelta(RectTransform t)
        {
            t.GetWorldCorners(corners);
            return corners[2] - corners[0];
        }


        private GameObject GetTargetGameObject(Vector2 position)
        {
            PointerEventData pe = new PointerEventData(EventSystem.current);
            pe.position = position;

            List<RaycastResult> hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pe, hits);

            return hits.Any(h => h.gameObject == gameObject) ? gameObject : null;
        }

        private int GetTouchCountFromForwarder()
        {
            if (target != image.gameObject) return 0;

#if UNITY_EDITOR
            return InputManager.GetMouseButton(0) ? 1 : 0;
#else
            if (InputManager.touchSupported)
            {
                if (InputManager.touchCount > 0) return InputManager.touchCount;
            }
            return InputManager.GetMouseButton(0) ? 1 : 0;
#endif
        }

        /// <summary>
        /// Converts a position from the map space to the forwarder space.
        /// </summary>
        /// <param name="position">The position in the map space.</param>
        /// <returns>The position in the forwarder space.</returns>
        public Vector2 MapToForwarderSpace(Vector2 position)
        {
            float scale = image.canvas.scaleFactor;
            Vector2 sizeDelta = GetSizeDelta(image.rectTransform);

            if (!targetTexture)
            {
                position.x *= sizeDelta.x / Screen.width * scale;
                position.y *= sizeDelta.y / Screen.height * scale;
            }
            else
            {
                position.x *= sizeDelta.x / targetTexture.width * scale;
                position.y *= sizeDelta.y / targetTexture.height * scale;
            }

            position.x -= sizeDelta.x / 2 * scale;
            position.y -= sizeDelta.y / 2 * scale;

            Vector3 pos = (Vector3)position + image.transform.position;

            return RectTransformUtility.WorldToScreenPoint(worldCamera, pos);
        }

        protected void OnDisable()
        {
            control.OnUpdateBefore -= OnUpdateBefore;
            InputManager.OnGetTouchCount -= OnGetTouchCount;
            InputManager.OnGetInputPosition -= OnGetInputPosition;
            InputManager.OnGetMultitouchInputPositions -= OnGetMultitouchInputPositions;
            GUITooltipDrawer.OnDrawTooltip -= OnDrawTooltip;
        }

        private void OnDrawTooltip(GUIStyle style, string text, Vector2 position)
        {
            float scale = image.canvas.scaleFactor;

            Vector2 p = position;
            Vector2 sizeDelta = GetSizeDelta(image.rectTransform);

            if (!targetTexture)
            {
                p.x *= sizeDelta.x / Screen.width * scale;
                p.y *= sizeDelta.y / Screen.height * scale;
            }
            else
            {
                p.x *= sizeDelta.x / targetTexture.width * scale;
                p.y *= sizeDelta.y / targetTexture.height * scale;
            }

            p -= sizeDelta / 2 * scale;

            Vector3 pos = (Vector3)p + image.transform.position;

            p = RectTransformUtility.WorldToScreenPoint(worldCamera, pos);

            GUIContent tip = new GUIContent(text);
            Vector2 size = style.CalcSize(tip);
            GUI.Label(new Rect(p.x - size.x / 2 - 5, Screen.height - p.y - size.y - 20, size.x + 10, size.y + 5), text, style);
        }

        private void OnEnable()
        {
            if (!map) map = Map.instance;
            control = map?.control as TileSetControl;
            if (!control) return;
            
            mouseController = map.GetComponent<MouseController>();
            
            control.OnUpdateBefore += OnUpdateBefore;
            map.notInteractUnderGUI = false;
            mouseController.checkScreenSizeForWheelZoom = false;

            InputManager.OnGetTouchCount += OnGetTouchCount;
            InputManager.OnGetInputPosition += OnGetInputPosition;
            InputManager.OnGetMultitouchInputPositions += OnGetMultitouchInputPositions;
            GUITooltipDrawer.OnDrawTooltip += OnDrawTooltip;
        }

        private Vector2 OnGetInputPosition()
        {
            if (target != image.gameObject) return Vector2.zero;
            return ProcessTouch(pointerPos, out Vector2 pos) ? pos : Vector2.zero;
        }

        private Vector2[] OnGetMultitouchInputPositions()
        {
            Vector2[] touches = InputManager.touches.Select(t => t.position).ToArray();

            Vector2 p;
            for (int i = 0; i < touches.Length; i++)
            {
                ProcessTouch(touches[i], out p, false);
                touches[i] = p;
            }

            return touches;
        }

        private int OnGetTouchCount()
        {
            return target == image.gameObject ? GetTouchCountFromForwarder() : 0;
        }

        private void OnUpdateBefore()
        {
            pointerPos = InputManager.GetRawInputPosition();
            if (InputManager.touchSupported && InputManager.touchCount > 0)
            {
                pointerPos = Vector2.zero;
                for (int i = 0; i < InputManager.touchCount; i++)
                {
                    Touch touch = InputManager.touches[i];
                    pointerPos += touch.position;
                }

                pointerPos /= InputManager.touchCount;
            }

            target = GetTargetGameObject(pointerPos);
        }

        private bool ProcessTouch(Vector2 inputTouch, out Vector2 localPosition, bool checkRect = true)
        {
            localPosition = Vector2.zero;

            RectTransform t = image.rectTransform;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(image.rectTransform, inputTouch, worldCamera, out localPosition)) return false;
            if (checkRect && !t.rect.Contains(localPosition)) return false;

            Vector2 sizeDelta = GetSizeDelta(t);
            localPosition += sizeDelta / 2.0f;

            if (!targetTexture)
            {
                localPosition.x *= Screen.width / sizeDelta.x;
                localPosition.y *= Screen.height / sizeDelta.y;
            }
            else
            {
                localPosition.x *= targetTexture.width / sizeDelta.x;
                localPosition.y *= targetTexture.height / sizeDelta.y;
            }

            return true;
        }
    }
}