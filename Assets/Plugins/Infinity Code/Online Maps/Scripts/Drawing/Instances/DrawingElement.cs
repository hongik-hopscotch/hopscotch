/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace OnlineMaps
{
    /// <summary>
    /// Class implements the basic functionality of drawing on the map.
    /// </summary>
    public abstract class DrawingElement: IInteractiveElement, IDataContainer
    {
        /// <summary>
        /// Default event caused to draw tooltip.
        /// </summary>
        public static Action<DrawingElement> OnElementDrawTooltip;

        /// <summary>
        /// Events that occur when user click on the drawing element.
        /// </summary>
        public Action<DrawingElement> OnClick;

        /// <summary>
        /// Events that occur when user double-click on the drawing element.
        /// </summary>
        public Action<DrawingElement> OnDoubleClick;

        /// <summary>
        /// Event caused to draw tooltip.
        /// </summary>
        public Action<DrawingElement> OnDrawTooltip;

        /// <summary>
        /// Event that occur when tileset initializes a mesh.
        /// </summary>
        public Action<DrawingElement, Renderer> OnInitMesh;

        /// <summary>
        /// Events that occur when user long press on the drawing element.
        /// </summary>
        public Action<DrawingElement> OnLongPress;

        /// <summary>
        /// Events that occur when user press on the drawing element.
        /// </summary>
        public Action<DrawingElement> OnPress;

        /// <summary>
        /// Events that occur when user release on the drawing element.
        /// </summary>
        public Action<DrawingElement> OnRelease;

        protected static List<Vector3> vertices;
        protected static List<Vector3> normals;
        protected static List<int> triangles;
        protected static List<Vector2> uv;
        protected static List<Vector2> localPoints;

        /// <summary>
        /// Need to check the map boundaries? It allows you to make drawing element, which are active outside the map.
        /// </summary>
        public bool checkMapBoundaries = true;

        /// <summary>
        /// Zoom range, in which the drawing element will be displayed.
        /// </summary>
        public LimitedRange range;

        /// <summary>
        /// Tooltip that is displayed when user hover on the drawing element.
        /// </summary>
        public string tooltip;

        /// <summary>
        /// The local Y position for the GameObject on Tileset.
        /// </summary>
        public float yOffset = 0;

        protected bool _visible = true;
        protected float bestElevationYScale;
        protected GameObject gameObject;
        protected Mesh mesh;
        protected GeoRect mapRect;
        protected Material[] materials;

        private string _name;
        private int _renderQueueOffset;
        private IInteractiveElementManager _manager;
        private ElevationManagerBase _elevationManager;
        private bool elevationManagerInited;

        protected GeoPoint[] _points;
        protected MercatorPoint[] mercatorPoints;

        /// <summary>
        /// Gets or sets custom data by key.
        /// </summary>
        /// <param name="key">Key</param>
        public object this[string key]
        {
            get
            {
                object val;
                return customData.TryGetValue(key, out val) ? val : null;
            }
            set => customData[key] = value;
        }

        /// <summary>
        /// Gets or sets the active state of the drawing element GameObject.
        /// </summary>
        protected virtual bool active
        {
            get
            {
                if (gameObject == null) return false;
                return gameObject.activeSelf;
            }
            set
            {
                if (gameObject != null) gameObject.SetActive(value);
            }
        }

        /// <summary>
        /// Creates a background material for the drawing element.
        /// </summary>
        protected abstract bool createBackgroundMaterial { get; }

        /// <summary>
        /// Gets custom fields.
        /// </summary>
        public Dictionary<string, object> customData { get; private set; } = new Dictionary<string, object>();

        /// <summary>
        /// Center point of the drawing element.
        /// </summary>
        public virtual GeoPoint center => GeoPoint.zero;

        /// <summary>
        /// Default name of the drawing element.
        /// </summary>
        protected virtual string defaultName
        {
            get { return "Drawing Element"; }
        }

        /// <summary>
        /// Gets the elevation manager.
        /// </summary>
        protected ElevationManagerBase elevationManager
        {
            get
            {
                if (!elevationManagerInited)
                {
                    elevationManagerInited = true;

                    ControlBaseDynamicMesh control = manager.map.control as ControlBaseDynamicMesh;
                    if (control != null) _elevationManager = control.elevationManager;
                }

                return _elevationManager;
            }
        }

        /// <summary>
        /// Checks if the elevation is used.
        /// </summary>
        protected bool hasElevation
        {
            get { return elevationManager != null && elevationManager.enabled; }
        }

        /// <summary>
        /// Instance of the drawing element.
        /// </summary>
        public GameObject instance
        {
            get { return gameObject; }
        }

        /// <summary>
        /// Reference to DrawingElementManager.
        /// </summary>
        public IInteractiveElementManager manager
        {
            get { return _manager != null? _manager: DrawingElementManager.instance; }
            set { _manager = value; }
        }

        /// <summary>
        /// Gets or sets the name of the drawing element.
        /// </summary>
        public string name
        {
            get
            {
                if (!string.IsNullOrEmpty(_name)) return _name;
                return defaultName;
            }
            set
            {
                _name = value;
                if (gameObject != null) gameObject.name = name;
                if (mesh != null) mesh.name = name;
            }
        }

        /// <summary>
        /// Gets or sets the render queue offset.
        /// </summary>
        public int renderQueueOffset
        {
            get { return _renderQueueOffset; }
            set
            {
                _renderQueueOffset = value;
                if (materials != null)
                {
                    Shader shader = (manager.map.control as TileSetControl).drawingShader;

                    for (int i = 0; i < materials.Length; i++)
                    {
                        Material m = materials[i];
                        if (m != null) m.renderQueue = shader.renderQueue + value;
                    }
                }
            }
        }

        /// <summary>
        /// Should the drawing element be split into pieces?
        /// </summary>
        protected virtual bool splitToPieces
        {
            get { return false; }
        }

        /// <summary>
        /// Gets or sets the visibility of the drawing element.
        /// </summary>
        public virtual bool visible
        {
            get { return _visible; }
            set
            {
                if (_visible == value) return;

                _visible = value;
                manager.map.Redraw();
            }
        }

        protected DrawingElement()
        {
        
        }

        private static void AddLineSegment(List<Vector3> vertices, List<Vector3> normals, List<int> triangles, List<Vector2> uv, Vector3 s1, Vector3 s2, Vector3 prevS1, Vector3 prevS2)
        {
            int ti = vertices.Count;
            vertices.AddRange(new[] { prevS1, s1, s2, prevS2 });
            normals.AddRange(new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up });
            uv.AddRange(new[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) });
            triangles.AddRange(new[] { ti, ti + 1, ti + 2, ti, ti + 2, ti + 3 });
        }

        protected void CalculateLocalPoints(bool closed = false, bool optimize = true)
        {
            if (localPoints == null) localPoints = new List<Vector2>();
            else localPoints.Clear();
            
            if (mercatorPoints.Length < 2) return; 
            
            Map map = manager.map;
            
            int zoom = map.view.intZoom;
            float zoomFactor = map.view.zoomFactor;

            TilePoint st = mapRect.topLeft.ToTile(map);

            int max = 1 << zoom;
            int halfMax = max / 2;

            double ppx = 0;
            Vector2 sizeInScene = (map.control as ControlBaseDynamicMesh).sizeInScene;
            double scaleX = Constants.TileSize * sizeInScene.x / map.buffer.renderState.width / zoomFactor;
            double scaleY = Constants.TileSize * sizeInScene.y / map.buffer.renderState.height / zoomFactor;
            
            bool isOptimized = false;
            
            TilePoint tp = new TilePoint();
            TilePoint tpr = new TilePoint();
            
            int mapTileWidth = map.control.width / Constants.TileSize / 2;

            for (int i = 0; i < mercatorPoints.Length; i++)
            {
                MercatorPoint p = mercatorPoints[i];
                tp = p.ToTile(zoom);
                isOptimized = false;

                if (optimize && i > 0)
                {
                    if ((tpr - tp).sqrMagnitude < 0.0001)
                    {
                        isOptimized = true;
                        continue;
                    }
                }

                tpr = tp;
                tp -= st;

                if (i == 0)
                {
                    double ox = tp.x - mapTileWidth;
                    if (ox < -halfMax) tp.x += max;
                    else if (ox > halfMax) tp.x -= max;
                }
                else
                {
                    double ox = tp.x - ppx;
                    int maxIt = 3;
                    while (maxIt-- > 0)
                    {
                        if (ox < -halfMax)
                        {
                            tp.x += max;
                            ox += max;
                        }
                        else if (ox > halfMax)
                        {
                            tp.x -= max;
                            ox -= max;
                        }
                        else break;
                    }
                }

                ppx = tp.x;

                double rx1 = tp.x * scaleX;
                double ry1 = tp.y * scaleY;

                localPoints.Add(new Vector2((float)rx1, (float)ry1));
            }

            if (isOptimized)
            {
                tp -= st;

                if (mercatorPoints.Length == 0)
                {
                    double ox = tp.x - mapTileWidth;
                    if (ox < -halfMax) tp.x += max;
                    else if (ox > halfMax) tp.x -= max;
                }
                else
                {
                    double ox = tp.x - ppx;
                    int maxIt = 3;
                    while (maxIt-- > 0)
                    {
                        if (ox < -halfMax)
                        {
                            tp.x += max;
                            ox += max;
                        }
                        else if (ox > halfMax)
                        {
                            tp.x -= max;
                            ox -= max;
                        }
                        else break;
                    }
                }

                double rx1 = tp.x * scaleX;
                double ry1 = tp.y * scaleY;

                Vector2 np = new Vector2((float)rx1, (float)ry1);
                localPoints.Add(np);
            }

            if (closed && (localPoints[0] - localPoints[localPoints.Count - 1]).magnitude > sizeInScene.x / 256) localPoints.Add(localPoints[0]);
        }

        /// <summary>
        /// Destroys the instance of the drawing element.
        /// </summary>
        public virtual void DestroyInstance()
        {
            if (gameObject != null)
            {
                Utils.Destroy(gameObject);
                gameObject = null;
            }

            if (mesh != null)
            {
                Utils.Destroy(mesh);
                mesh = null;
            }

            if (materials != null)
            {
                for (int i = 0; i < materials.Length; i++)
                {
                    Material material = materials[i];
                    if (material != null) Utils.Destroy(material);
                }

                materials = null;
            }
        }

        /// <summary>
        /// Dispose drawing element.
        /// </summary>
        public void Dispose()
        {
            _manager = null;
            customData = null;
            OnClick = null;
            OnDoubleClick = null;
            OnDrawTooltip = null;
            OnPress = null;
            OnRelease = null;

            DestroyInstance();
            tooltip = null;

            DisposeLate();
        }

        protected virtual void DisposeLate()
        {
        
        }

        /// <summary>
        /// Draw element on the map.
        /// </summary>
        /// <param name="buffer">Backbuffer</param>
        /// <param name="bufferPosition">Backbuffer position</param>
        /// <param name="bufferWidth">Backbuffer width</param>
        /// <param name="bufferHeight">Backbuffer height</param>
        /// <param name="zoom">Zoom of the map</param>
        /// <param name="invertY">Invert Y direction</param>
        public virtual void Draw(Color32[] buffer, Vector2 bufferPosition, int bufferWidth, int bufferHeight, float zoom, bool invertY = false)
        {
        
        }

        protected void DrawActivePoints(TileSetDrawer drawer, ref List<Vector2> activePoints, ref List<Vector3> vertices, ref List<Vector3> normals, ref List<int> triangles, ref List<Vector2> uv, float width)
        {
            if (activePoints.Count < 2)
            {
                activePoints.Clear();
                return;
            }

            List<Vector2> points = activePoints;

            if (splitToPieces) points = SplitToPieces(drawer, points);

            float w2 = width * 2;

            Vector3 prevS1, prevS2;

            int c = points.Count - 1;
            bool extraPointAdded = false;
            bool elevationActive = hasElevation;

            DrawFirstAndLastActivePoints(points, 0, -points[0].x, points[0].y, width, elevationActive, out prevS1, out prevS2);
            for (int i = 1; i < c; i++)
            {
                DrawIntermediateActicePoints(vertices, normals, triangles, uv, width, points, ref i, w2, elevationActive, ref extraPointAdded, ref c, ref prevS1, ref prevS2);
            }
            
            DrawFirstAndLastActivePoints(points, c, -points[c].x, points[c].y, width, elevationActive, out Vector3 s1, out Vector3 s2);
            AddLineSegment(vertices, normals, triangles, uv, s1, s2, prevS1, prevS2);

            activePoints.Clear();
        }

        protected void DrawFirstAndLastActivePoints(List<Vector2> points, int i, float px, float pz, float width, bool elevationActive, out Vector3 s1, out Vector3 s2)
        {
            float p1x, p1z, p2x, p2z;

            if (i == 0)
            {
                p1x = px;
                p1z = pz;
                p2x = -points[1].x;
                p2z = points[1].y;
            }
            else
            {
                p1x = -points[i - 1].x;
                p1z = points[i - 1].y;
                p2x = px;
                p2z = pz;
            }

            float a = Geometry.Angle2DRad(p1x, p1z, p2x, p2z, 90);

            float offX = Mathf.Cos(a) * width;
            float offZ = Mathf.Sin(a) * width;

            float s1x = px + offX;
            float s1z = pz + offZ;
            float s2x = px - offX;
            float s2z = pz - offZ;

            float s1y = 0;
            float s2y = 0; 

            if (elevationActive)
            {
                s1y = elevationManager.GetElevationValue(s1x, s1z, bestElevationYScale, mapRect);
                s2y = elevationManager.GetElevationValue(s2x, s2z, bestElevationYScale, mapRect);
            }

            s1 = new Vector3(s1x, s1y, s1z);
            s2 = new Vector3(s2x, s2y, s2z);
        }

        private void DrawIntermediateActicePoints(List<Vector3> vertices, List<Vector3> normals, List<int> triangles, List<Vector2> uv, float width, List<Vector2> points, ref int i, float w2, bool elevationActive, ref bool extraPointAdded, ref int c, ref Vector3 prevS1, ref Vector3 prevS2)
        {
            Vector3 s1, s2;

            Vector2 p = points[i];
            Vector2 pp = points[i - 1];
            Vector2 np = points[i + 1];
            
            float px = -p.x;
            float pz = p.y;
            
            float p1x = -pp.x;
            float p1z = pp.y;
            float p2x = -np.x;
            float p2z = np.y;

            float a1 = Geometry.Angle2DRad(p1x, p1z, px, pz, 90);
            float a3 = Geometry.AngleOfTriangle(pp, np, p) * Mathf.Rad2Deg;
            if (a3 < 60 && !extraPointAdded)
            {
                points.Insert(i + 1, Vector2.Lerp(p, np, 0.001f));
                points[i] = Vector2.Lerp(p, pp, 0.001f);
                c++;
                i--;
                extraPointAdded = true;
                return;
            }

            extraPointAdded = false;
            float a2 = Geometry.Angle2DRad(px, pz, p2x, p2z, 90);

            float off1x = Mathf.Cos(a1) * width;
            float off1z = Mathf.Sin(a1) * width;
            float off2x = Mathf.Cos(a2) * width;
            float off2z = Mathf.Sin(a2) * width;

            float p21x = px + off1x;
            float p21z = pz + off1z;
            float p22x = px - off1x;
            float p22z = pz - off1z;
            float p31x = px + off2x;
            float p31z = pz + off2z;
            float p32x = px - off2x;
            float p32z = pz - off2z;

            float is1x, is1z, is2x, is2z;
                
            int state1 = Geometry.GetIntersectionPointOfTwoLines(p1x + off1x, p1z + off1z, p21x, p21z, p31x, p31z, p2x + off2x, p2z + off2z, out is1x, out is1z);
            int state2 = Geometry.GetIntersectionPointOfTwoLines(p1x - off1x, p1z - off1z, p22x, p22z, p32x, p32z, p2x - off2x, p2z - off2z, out is2x, out is2z);

            if (state1 == 1 && state2 == 1)
            {
                float o1x = is1x - px;
                float o1z = is1z - pz;
                float o2x = is2x - px;
                float o2z = is2z - pz;

                float m1 = Mathf.Sqrt(o1x * o1x + o1z * o1z);
                float m2 = Mathf.Sqrt(o2x * o2x + o2z * o2z);

                if (m1 > w2)
                {
                    is1x = o1x / m1 * w2 + px;
                    is1z = o1z / m1 * w2 + pz;
                }
                if (m2 > w2)
                {
                    is2x = o2x / m2 * w2 + px;
                    is2z = o2z / m2 * w2 + pz;
                }

                float s1y = 0;
                float s2y = 0;

                if (elevationActive)
                {
                    s1y = elevationManager.GetElevationValue(is1x, is1z, bestElevationYScale, mapRect);
                    s2y = elevationManager.GetElevationValue(is2x, is2z, bestElevationYScale, mapRect);
                }

                s1 = new Vector3(is1x, s1y, is1z);
                s2 = new Vector3(is2x, s2y, is2z);
            }
            else
            {
                float po1x = p31x;
                float po1z = p31z;
                float po2x = p32x;
                float po2z = p32z;

                float s1y = 0;
                float s2y = 0;

                if (elevationActive)
                {
                    s1y = elevationManager.GetElevationValue(po1x, po1z, bestElevationYScale, mapRect);
                    s2y = elevationManager.GetElevationValue(po2x, po2z, bestElevationYScale, mapRect);
                }

                s1 = new Vector3(po1x, s1y, po1z);
                s2 = new Vector3(po2x, s2y, po2z);
            }
                
            AddLineSegment(vertices, normals, triangles, uv, s1, s2, prevS1, prevS2);

            prevS1 = s1;
            prevS2 = s2;
        }

        protected void DrawLineToBuffer(Color32[] buffer, Vector2 bufferPosition, int bufferWidth, int bufferHeight, float zoom, Color32 color, float width, bool closed, bool invertY)
        {
            if (color.a == 0) return;

            int izoom = (int) zoom;
            float zoomScale = Mathf.Pow(2, izoom - zoom);

            TilePoint t1, t2, s = Projection.MercatorToTile(0.5, 0.5, izoom);

            int max = 1 << izoom;

            int w = Mathf.RoundToInt(width);

            double ppx1 = 0;

            float bx1 = bufferPosition.x;
            float bx2 = bx1 + zoomScale * bufferWidth / Constants.TileSize;
            float by1 = bufferPosition.y;
            float by2 = by1 + zoomScale * bufferHeight / Constants.TileSize;

            for (int i = 1; i < mercatorPoints.Length; i++)
            {
                t1 = mercatorPoints[i - 1].ToTile(izoom);
                t2 = mercatorPoints[i].ToTile(izoom);
                
                if ((t1.x < bx1 && t2.x < bx1) || (t1.x > bx2 && t2.x > bx2))
                {

                }
                else if ((t1.y < by1 && t2.y < by1) || (t1.y > by2 && t2.y > by2))
                {

                }
                else DrawLinePartToBuffer(buffer, bufferPosition, bufferWidth, bufferHeight, color, s.x, s.y, t1.x, t1.y, t2.x, t2.y, max, ref ppx1, w, invertY, zoomScale);
            }

            if (!closed) return;
            
            t1 = mercatorPoints[mercatorPoints.Length - 1].ToTile(izoom);
            t2 = mercatorPoints[0].ToTile(izoom);
                
            if ((t1.x < bx1 && t2.x < bx1) || (t1.x > bx2 && t2.x > bx2))
            {

            }
            else if ((t1.y < by1 && t2.y < by1) || (t1.y > by2 && t2.y > by2))
            {

            }
            else DrawLinePartToBuffer(buffer, bufferPosition, bufferWidth, bufferHeight, color, s.x, s.y, t1.x, t1.y, t2.x, t2.y, max, ref ppx1, w, invertY, zoomScale);
        }

        private static void DrawLinePartToBuffer(Color32[] buffer, Vector2 bufferPosition, int bufferWidth, int bufferHeight, Color32 color, double sx, double sy, double p1tx, double p1ty, double p2tx, double p2ty, int maxX, ref double ppx1, int w, bool invertY, float zoomScale)
        {
            if ((p1tx < bufferPosition.x && p2tx < bufferPosition.x) || (p1tx > bufferPosition.x + (bufferWidth >> 8) / zoomScale && p2tx > bufferPosition.x + (bufferWidth >> 8) / zoomScale)) return;
            if ((p1ty < bufferPosition.y && p2ty < bufferPosition.y) || (p1ty > bufferPosition.y + (bufferHeight >> 8) / zoomScale && p2ty > bufferPosition.y + (bufferHeight >> 8) / zoomScale)) return;

            if ((p1tx - p2tx) * (p1tx - p2tx) + (p1ty - p2ty) * (p1ty - p2ty) > 0.04)
            {
                double p3tx = (p1tx + p2tx) / 2;
                double p3ty = (p1ty + p2ty) / 2;
                DrawLinePartToBuffer(buffer, bufferPosition, bufferWidth, bufferHeight, color, sx, sy, p1tx, p1ty, p3tx, p3ty, maxX, ref ppx1, w, invertY, zoomScale);
                DrawLinePartToBuffer(buffer, bufferPosition, bufferWidth, bufferHeight, color, sx, sy, p3tx, p3ty, p2tx, p2ty, maxX, ref ppx1, w, invertY, zoomScale);
                return;
            }

            p1tx -= sx;
            p2tx -= sx;
            p1ty -= sy;
            p2ty -= sy;
            
            double gpx1 = p1tx + maxX;
            double lpx1 = p1tx - maxX;

            if (Math.Abs(ppx1 - gpx1) < Math.Abs(ppx1 - p1tx)) p1tx = gpx1;
            else if (Math.Abs(ppx1 - lpx1) < Math.Abs(ppx1 - p1tx)) p1tx = lpx1;

            ppx1 = p1tx;

            double gpx2 = p2tx + maxX;
            double lpx2 = p2tx - maxX;

            if (Math.Abs(ppx1 - gpx2) < Math.Abs(ppx1 - p2tx)) p2tx = gpx2;
            else if (Math.Abs(ppx1 - lpx2) < Math.Abs(ppx1 - p2tx)) p2tx = lpx2;

            double p1x = (p1tx + sx - bufferPosition.x) / zoomScale;
            double p1y = (p1ty + sy - bufferPosition.y) / zoomScale;
            double p2x = (p2tx + sx - bufferPosition.x) / zoomScale;
            double p2y = (p2ty + sy - bufferPosition.y) / zoomScale;

            if (p1x > maxX && p2x > maxX)
            {
                p1x -= maxX;
                p2x -= maxX;
            }

            double fromX = p1x * Constants.TileSize;
            double fromY = p1y * Constants.TileSize;
            double toX = p2x * Constants.TileSize;
            double toY = p2y * Constants.TileSize;

            double stX = (fromX < toX ? fromX : toX) - w;
            if (stX < 0) stX = 0;
            else if (stX > bufferWidth) stX = bufferWidth;

            double stY = (fromY < toY ? fromY : toY) - w;
            if (stY < 0) stY = 0;
            else if (stY > bufferHeight) stY = bufferHeight;

            double endX = (fromX > toX ? fromX : toX) + w;
            if (endX < 0) endX = 0;
            else if (endX > bufferWidth) endX = bufferWidth;

            double endY = (fromY > toY ? fromY : toY) + w;
            if (endY < 0) endY = 0;
            else if (endY > bufferHeight) endY = bufferHeight;

            int istx = (int) Math.Round(stX);
            int isty = (int) Math.Round(stY);

            int sqrW = w * w;

            int lengthX = (int) Math.Round(endX - stX);
            int lengthY = (int) Math.Round(endY - stY);

            byte clrR = color.r;
            byte clrG = color.g;
            byte clrB = color.b;
            byte clrA = color.a;
            float alpha = clrA / 256f;
            if (alpha > 1) alpha = 1;

            for (int y = 0; y < lengthY; y++)
            {
                double py = y + stY;
                int ipy = y + isty;
                double centerY = py + 0.5;
                if (!invertY) ipy = bufferHeight - ipy - 1;
                ipy *= bufferWidth;

                for (int x = 0; x < lengthX; x++)
                {
                    double px = x + stX;
                    int ipx = x + istx;
                    double centerX = px + 0.5;

                    double npx, npy;

                    Geometry.NearestPointStrict(centerX, centerY, fromX, fromY, toX, toY, out npx, out npy);
                    double onpx = centerX - npx;
                    double onpy = centerY - npy;

                    double dist = onpx * onpx + onpy * onpy;

                    if (dist <= sqrW)
                    {
                        int bufferIndex = ipy + ipx;
                        Color32 pc = buffer[bufferIndex];
                        pc.r = (byte)((clrR - pc.r) * alpha + pc.r);
                        pc.g = (byte)((clrG - pc.g) * alpha + pc.g);
                        pc.b = (byte)((clrB - pc.b) * alpha + pc.b);
                        pc.a = (byte)((clrA - pc.a) * alpha + pc.a);
                        buffer[bufferIndex] = pc;
                    }
                }
            }
        }

        /// <summary>
        /// Draws element on the tileset.
        /// </summary>
        /// <param name="drawer">Tileset drawer</param>
        /// <param name="index">Index of drawing element</param>
        public virtual void DrawOnTileSet(TileSetDrawer drawer, int index)
        {
        
        }

        protected void FillPoly(Color32[] buffer, Vector2 bufferPosition, int bufferWidth, int bufferHeight, float zoom, Color32 color, bool invertY)
        {
            if (color.a == 0) return;
            float alpha = color.a / 255f;

            double minX, maxX, minY, maxY;
            double[] bufferPoints = GetBufferPoints(bufferPosition, zoom, out minX, out maxX, out minY, out maxY);

            if (maxX < 0 || minX > bufferWidth || maxY < 0 || minY > bufferHeight) return;

            double stX = minX;
            if (stX < 0) stX = 0;
            else if (stX > bufferWidth) stX = bufferWidth;

            double stY = minY;
            if (stY < 0) stY = 0;
            else if (stY > bufferHeight) stY = bufferHeight;

            double endX = maxX;
            if (endX < 0) stX = 0;
            else if (endX > bufferWidth) endX = bufferWidth;

            double endY = maxY;
            if (endY < 0) endY = 0;
            else if (endY > bufferHeight) endY = bufferHeight;

            int lengthX = (int)Math.Round(endX - stX);
            int lengthY = (int)Math.Round(endY - stY);

            Color32 clr = new Color32(color.r, color.g, color.b, 255);

            const int blockSize = 5;
            int blockCountX = lengthX / blockSize + (lengthX % blockSize == 0 ? 0 : 1);
            int blockCountY = lengthY / blockSize + (lengthY % blockSize == 0 ? 0 : 1);

            byte clrR = clr.r;
            byte clrG = clr.g;
            byte clrB = clr.b;

            int istx = (int) Math.Round(stX);
            int isty = (int) Math.Round(stY);

            for (int by = 0; by < blockCountY; by++)
            {
                int byp = by * blockSize;
                double bufferY = byp + stY;
                int iby = byp + isty;

                for (int bx = 0; bx < blockCountX; bx++)
                {
                    int bxp = bx * blockSize;
                    double bufferX = bxp + stX;
                    int ibx = bxp + istx;

                    bool p1 = Geometry.IsPointInPolygon(bufferPoints, bufferX, bufferY);
                    bool p2 = Geometry.IsPointInPolygon(bufferPoints, bufferX + blockSize - 1, bufferY);
                    bool p3 = Geometry.IsPointInPolygon(bufferPoints, bufferX + blockSize - 1, bufferY + blockSize - 1);
                    bool p4 = Geometry.IsPointInPolygon(bufferPoints, bufferX, bufferY + blockSize - 1);

                    if (p1 && p2 && p3 && p4)
                    {
                        for (int y = 0; y < blockSize; y++)
                        {
                            if (byp + y >= lengthY) break;
                            int cby = iby + y;
                            if (!invertY) cby = bufferHeight - cby - 1;
                            int byi = cby * bufferWidth + ibx;

                            for (int x = 0; x < blockSize; x++)
                            {
                                if (bxp + x >= lengthX) break;

                                int bufferIndex = byi + x;
                            
                                Color32 a = buffer[bufferIndex];
                                a.r = (byte) (a.r + (clrR - a.r) * alpha);
                                a.g = (byte) (a.g + (clrG - a.g) * alpha);
                                a.b = (byte) (a.b + (clrB - a.b) * alpha);
                                a.a = (byte) (a.a + (255 - a.a) * alpha);
                                buffer[bufferIndex] = a;
                            }
                        }
                    }
                    else if (p1 || p2 || p3 || p4)
                    {
                        for (int y = 0; y < blockSize; y++)
                        {
                            if (byp + y >= lengthY) break;
                            int cby = iby + y;
                            if (!invertY) cby = bufferHeight - cby - 1;
                            int byi = cby * bufferWidth + ibx;

                            for (int x = 0; x < blockSize; x++)
                            {
                                if (bxp + x >= lengthX) break;

                                if (!Geometry.IsPointInPolygon(bufferPoints, bufferX + x, bufferY + y)) continue;
                                
                                int bufferIndex = byi + x;
                                Color32 a = buffer[bufferIndex];
                                a.r = (byte)(a.r + (clrR - a.r) * alpha);
                                a.g = (byte)(a.g + (clrG - a.g) * alpha);
                                a.b = (byte)(a.b + (clrB - a.b) * alpha);
                                a.a = (byte)(a.a + (255 - a.a) * alpha);
                                buffer[bufferIndex] = a;
                            }
                        }
                    }
                }
            }
        }

        private double[] GetBufferPoints(Vector2 bufferPosition, float zoom, out double minX, out double maxX, out double minY, out double maxY)
        {
            int izoom = (int) zoom;
            float zoomScale = Mathf.Pow(2, izoom - zoom);
            float scaledTileSize = Constants.TileSize / zoomScale;

            double[] bufferPoints = new double[mercatorPoints.Length * 2];

            minX = double.MaxValue;
            maxX = double.MinValue;
            minY = double.MaxValue;
            maxY = double.MinValue;

            for (int i = 0; i < mercatorPoints.Length; i++)
            {
                TilePoint t = mercatorPoints[i].ToTile(izoom);
                t -= bufferPosition;
                t *= scaledTileSize;

                if (t.x < minX) minX = t.x;
                if (t.x > maxX) maxX = t.x;
                if (t.y < minY) minY = t.y;
                if (t.y > maxY) maxY = t.y;

                bufferPoints[i * 2] = t.x;
                bufferPoints[i * 2 + 1] = t.y;
            }
            
            return bufferPoints;
        }
        
        /// <summary>
        /// Gets the data by key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <typeparam name="T">The type of the data.</typeparam>
        /// <returns>The data.</returns>
        public T GetData<T>(string key)
        {
            object val;
            customData.TryGetValue(key, out val);
            return val != null? (T)val : default;
        }

        /// <summary>
        /// Determines if the drawing element at the specified coordinates.
        /// </summary>
        /// <param name="location">Location</param>
        /// <param name="zoom">Zoom</param>
        /// <returns>True if the drawing element in position, false if not.</returns>
        public virtual bool HitTest(GeoPoint location, int zoom)
        {
            return false;
        }

        protected void InitLineMesh(TileSetDrawer drawer, float width, bool closed = false, bool optimize = true)
        {
            ResetMeshLists(32);
            if (mercatorPoints == null) return;

            mapRect = manager.map.buffer.GetCorners().rightFixed;

            CalculateLocalPoints(closed, optimize);
            List<Vector2> activePoints = new List<Vector2>(localPoints.Count);

            long maxX = 1L << manager.map.view.intZoom;
            float maxSize = maxX * Constants.TileSize * drawer.control.sizeInScene.x / drawer.control.width / manager.map.view.zoomFactor;
            float halfSize = maxSize / 2;

            float lastPointX = 0;
            float lastPointY = 0;

            float sizeX = drawer.control.sizeInScene.x;
            float sizeY = drawer.control.sizeInScene.y;

            bestElevationYScale = ElevationManagerBase.GetElevationScale(mapRect, _elevationManager);

            Vector2[] intersections = new Vector2[4];
            bool needExtraPoint = false;
            float extraX = 0, extraY = 0;

            bool isEntireWorld = manager.map.buffer.renderState.width == maxX * Constants.TileSize;

            for (int i = 0; i < localPoints.Count; i++)
            {
                Vector2 p = localPoints[i];
                float px = p.x;
                float py = p.y;

                if (needExtraPoint)
                {
                    activePoints.Add(new Vector2(extraX, extraY));

                    float ox = extraX - lastPointX;
                    if (ox > halfSize) lastPointX += maxSize;
                    else if (ox < -halfSize) lastPointX -= maxSize;

                    activePoints.Add(new Vector2(lastPointX, lastPointY));

                    needExtraPoint = false;
                }

                if (i > 0 && checkMapBoundaries)
                {
                    int countIntersections = 0;

                    float ox = px - lastPointX;
                    while (Math.Abs(ox) > halfSize)
                    {
                        if (ox < 0)
                        {
                            px += maxSize;
                            ox += maxSize;
                        }
                        else if (ox > 0)
                        {
                            px -= maxSize;
                            ox -= maxSize;
                        }
                    }

                    float crossTopX, crossTopY, crossLeftX, crossLeftY, crossBottomX, crossBottomY, crossRightX, crossRightY;

                    bool hasCrossTop =      Geometry.LineIntersection(lastPointX, lastPointY, px, py, 0,     0,     sizeX, 0,     out crossTopX,    out crossTopY);
                    bool hasCrossBottom =   Geometry.LineIntersection(lastPointX, lastPointY, px, py, 0,     sizeY, sizeX, sizeY, out crossBottomX, out crossBottomY);
                    bool hasCrossLeft =     Geometry.LineIntersection(lastPointX, lastPointY, px, py, 0,     0,     0,     sizeY, out crossLeftX,   out crossLeftY);
                    bool hasCrossRight =    Geometry.LineIntersection(lastPointX, lastPointY, px, py, sizeX, 0,     sizeX, sizeY, out crossRightX,  out crossRightY);

                    if (hasCrossTop)
                    {
                        intersections[0] = new Vector2(crossTopX, crossTopY);
                        countIntersections++;
                    }
                    if (hasCrossBottom)
                    {
                        intersections[countIntersections] = new Vector2(crossBottomX, crossBottomY);
                        countIntersections++;
                    }
                    if (hasCrossLeft)
                    {
                        intersections[countIntersections] = new Vector2(crossLeftX, crossLeftY);
                        countIntersections++;
                    }
                    if (hasCrossRight)
                    {
                        intersections[countIntersections] = new Vector2(crossRightX, crossRightY);
                        countIntersections++;
                    }

                    if (countIntersections == 1) activePoints.Add(intersections[0]);
                    else if (countIntersections == 2)
                    {
                        Vector2 lastPoint = new Vector2(lastPointX, lastPointY);
                        int minIndex = (lastPoint - intersections[0]).sqrMagnitude < (lastPoint - intersections[1]).sqrMagnitude? 0: 1;
                        activePoints.Add(intersections[minIndex]);
                        activePoints.Add(intersections[1 - minIndex]);
                    }

                    if (hasCrossLeft)
                    {
                        needExtraPoint = Geometry.LineIntersection(lastPointX + maxSize, lastPointY, px + maxSize, py, sizeX, 0, sizeX, sizeY, out extraX, out extraY);
                    }
                    else if (hasCrossRight)
                    {
                        needExtraPoint = Geometry.LineIntersection(lastPointX - maxSize, lastPointY, px - maxSize, py, 0, 0, 0, sizeY, out extraX, out extraY);
                    }
                    else if (isEntireWorld)
                    {
                        if (px < 0)
                        {
                            DrawActivePoints(drawer, ref activePoints, ref vertices, ref normals, ref triangles, ref uv, width);
                            px += maxSize;

                        }
                        else if (px > sizeX)
                        {
                            DrawActivePoints(drawer, ref activePoints, ref vertices, ref normals, ref triangles, ref uv, width);
                            px -= maxSize;
                        }
                    }
                }

                if (!checkMapBoundaries || px >= 0 && py >= 0 && px <= sizeX && py <= sizeY) activePoints.Add(new Vector2(px, py));
                else if (activePoints.Count > 0) DrawActivePoints(drawer, ref activePoints, ref vertices, ref normals, ref triangles, ref uv, width);

                lastPointX = px;
                lastPointY = py;
            }

            if (needExtraPoint)
            {
                activePoints.Add(new Vector2(extraX, extraY));

                float ox = extraX - lastPointX;
                if (ox > halfSize) lastPointX += maxSize;
                else if (ox < -halfSize) lastPointX -= maxSize;

                activePoints.Add(new Vector2(lastPointX, lastPointY));
            }
            if (activePoints.Count > 0) DrawActivePoints(drawer, ref activePoints, ref vertices, ref normals, ref triangles, ref uv, width);
        }

        protected bool InitMesh(TileSetDrawer drawer, Color borderColor, Color backgroundColor = default, Texture borderTexture = null, Texture backgroundTexture = null)
        {
            if (mesh != null)
            {
                materials[0].color = borderColor;
                if (backgroundColor != default(Color)) materials[1].color = backgroundColor;
                return false;
            }

            gameObject = new GameObject(name);
            gameObject.transform.parent = drawer.drawingsGameObject.transform;
            gameObject.transform.localPosition = new Vector3(0, yOffset, 0);
            gameObject.transform.localRotation = Quaternion.Euler(Vector3.zero);
            gameObject.transform.localScale = Vector3.one;
            gameObject.layer = drawer.drawingsGameObject.layer;

            MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
            MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            mesh = new Mesh {name = name};
            meshFilter.mesh = mesh;
            materials = new Material[createBackgroundMaterial?2: 1];
            Shader shader = drawer.control.drawingShader;
            Material borderMaterial = materials[0] = new Material(shader);
            borderMaterial.shader = shader;
            borderMaterial.color = borderColor;
            borderMaterial.mainTexture = borderTexture;

            if (createBackgroundMaterial)
            {
                Material backgroundMaterial = materials[1] = new Material(shader);
                backgroundMaterial.shader = shader;
                if (backgroundColor != default(Color)) backgroundMaterial.color = backgroundColor;
                backgroundMaterial.mainTexture = backgroundTexture;
            }

            renderer.materials = materials;
            for (int i = 0; i < materials.Length; i++) materials[i].renderQueue = shader.renderQueue + renderQueueOffset;

            if (OnInitMesh != null) OnInitMesh(this, renderer);

            return true;
        }

        /// <summary>
        /// It marks the elements changed. It is used for the Drawing API as an overlay.
        /// </summary>
        public static void MarkChanged()
        {
            lock (Tile.lockTiles)
            {
                foreach (Tile tile in Map.instance.tileManager.tiles) tile.drawingChanged = true;
            }
        }

        protected static void ResetMeshLists(int countPoints)
        {
            if (vertices == null) vertices = new List<Vector3>(Mathf.Max(Mathf.NextPowerOfTwo(countPoints * 4), 32));
            else vertices.Clear();

            if (normals == null) normals = new List<Vector3>(vertices.Capacity);
            else normals.Clear();

            if (triangles == null) triangles = new List<int>(Mathf.Max(Mathf.NextPowerOfTwo(countPoints * 6), 32));
            else triangles.Clear();

            if (uv == null) uv = new List<Vector2>(vertices.Capacity);
            else uv.Clear();
        }

        /// <summary>
        /// Sets the points of the drawing element.
        /// </summary>
        /// <param name="newPoints">The collection of new points (array or list of types: Vector2, Vector2d, GeoPoint, TilePoint, MercatorPoint, float, double).</param>
        public virtual void SetPoints(IEnumerable newPoints)
        {
            if (newPoints == null)
            {
                _points = Array.Empty<GeoPoint>();
                manager.map.Redraw();
                return;
            }

            Map map = manager.map;

            _points = GeoPoint.FromEnumerable(map, newPoints);
            mercatorPoints = _points.Select(p => p.ToMercator(map)).ToArray();
            
            manager.map.Redraw();
        }

        private List<Vector2> SplitToPieces(TileSetDrawer drawer, List<Vector2> activePoints)
        {
            List<Vector2> newPoints = new List<Vector2>(activePoints.Count);
            float d = drawer.control.sizeInScene.x / 4;
            Vector2 p1 = activePoints[0];
            newPoints.Add(p1);

            for (int i = 1; i < activePoints.Count; i++)
            {
                Vector2 p2 = activePoints[i];
                if ((p2 - p1).sqrMagnitude < d) newPoints.Add(p2);
                else SplitToPieces(newPoints, p1, p2, d);

                p1 = p2;
            }

            return newPoints;
        }

        private static void SplitToPieces(List<Vector2> points, Vector2 p1, Vector2 p2, float d)
        {
            Vector2 c = (p1 + p2) / 2;
            if ((p1 - c).sqrMagnitude < d) points.Add(c);
            else SplitToPieces(points, p1, c, d);

            if ((c - p2).sqrMagnitude < d) points.Add(p2);
            else SplitToPieces(points, c, p2, d);
        }

        protected void UpdateMaterialsQueue(TileSetControl control, int index)
        {
            foreach (Material material in materials) material.renderQueue = control.drawingShader.renderQueue + renderQueueOffset + index;
        }
    }
}