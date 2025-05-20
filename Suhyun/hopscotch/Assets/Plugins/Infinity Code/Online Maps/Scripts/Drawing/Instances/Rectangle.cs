/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System.Collections.Generic;
using UnityEngine;

namespace OnlineMaps
{
    /// <summary>
    /// Class that draws a rectangle on the map.
    /// </summary>
    public class Rectangle : DrawingElement
    {
        private static List<Vector2> activePoints;
        private static List<int> backTriangles;

        private Color _backgroundColor = new Color(1, 1, 1, 0);
        private Color _borderColor = Color.black;
        private float _borderWidth = 1;
        
        private double _height = 1;
        private double _width = 1;
        private double _x;
        private double _y;
        private Texture2D _backgroundTexture;

        protected override bool createBackgroundMaterial => _backgroundTexture || _backgroundColor.a > 0;

        /// <summary>
        /// Center point of the rectangle.
        /// </summary>
        public override GeoPoint center => new(_x + _width / 2, _y + _height / 2);

        /// <summary>
        /// Background color of the rectangle.
        /// </summary>
        public Color backgroundColor
        {
            get => _backgroundColor;
            set
            {
                _backgroundColor = value;
                if (manager != null) manager.map.Redraw();
            }
        }

        /// <summary>
        /// Background texture of the rectangle. Currently, works only for Tileset. For this to work correctly, also set backgroundColor.
        /// </summary>
        public Texture2D backgroundTexture
        {
            get => _backgroundTexture;
            set
            {
                _backgroundTexture = value;
                if (manager != null) manager.map.Redraw();
            }
        }

        /// <summary>
        /// Border color of the rectangle.
        /// </summary>
        public Color borderColor
        {
            get => _borderColor;
            set
            {
                _borderColor = value;
                if (manager != null) manager.map.Redraw();
            }
        }

        /// <summary>
        /// Border width of the rectangle.
        /// </summary>
        public float borderWidth
        {
            get => _borderWidth;
            set
            {
                _borderWidth = value;
                if (manager != null) manager.map.Redraw();
            }
        }

        protected override string defaultName => "Rect";

        /// <summary>
        /// Gets or sets the width of the rectangle. Geographic coordinates.
        /// </summary>
        public double width
        {
            get => _width;
            set
            {
                _width = value;
                InitPoints();
                if (manager != null) manager.map.needRedraw = true;
            }
        }

        /// <summary>
        /// Gets or sets the height of the rectangle. Geographic coordinates.
        /// </summary>
        public double height
        {
            get => _height;
            set
            {
                _height = value;
                InitPoints();
                if (manager != null) manager.map.needRedraw = true;
            }
        }

        /// <summary>
        /// Gets or sets the x position of the rectangle. Geographic coordinates.
        /// </summary>
        public double x
        {
            get => _x;
            set
            {
                _x = value;
                InitPoints();
                if (manager != null) manager.map.needRedraw = true;
            }
        }

        /// <summary>
        /// Gets or sets the y position of the rectangle. Geographic coordinates.
        /// </summary>
        public double y
        {
            get => _y;
            set
            {
                _y = value;
                InitPoints();
                if (manager != null) manager.map.needRedraw = true;
            }
        }

        /// <summary>
        /// Coordinates of top-left corner.
        /// </summary>
        public GeoPoint topLeft
        {
            get => new GeoPoint(_x, _y);
            set
            {
                GeoPoint br = bottomRight;
                _x = value.x;
                _y = value.y;
                bottomRight = br;
            }
        }

        /// <summary>
        /// Coordinates of top-right corner.
        /// </summary>
        public GeoPoint topRight
        {
            get => new GeoPoint(_x + _width, _y);
            set
            {
                double b = _y + _height;
                _width = value.x - _x;
                _y = value.y;
                _height = b - _y;
                InitPoints();
                if (manager != null) manager.map.needRedraw = true;
            }
        }

        /// <summary>
        /// Coordinates of bottom-left corner.
        /// </summary>
        public GeoPoint bottomLeft
        {
            get => new GeoPoint(_x, _y + _height);
            set
            {
                double r = _x + _width;
                _x = value.x;
                _height = value.y - _y;
                _width = r - _x;
                InitPoints();
                if (manager != null) manager.map.needRedraw = true;
            }
        }

        /// <summary>
        /// Coordinates of bottom-right corner.
        /// </summary>
        public GeoPoint bottomRight
        {
            get => new GeoPoint(_x + _width, _y + _height);
            set
            {
                _width = value.x - _x;
                _height = value.y - _y;
                InitPoints();
                if (manager != null) manager.map.needRedraw = true;
            }
        }

        /// <summary>
        /// Creates a new rectangle.
        /// </summary>
        /// <param name="longitude">Longitude of top-left corner of the rectangle.</param>
        /// <param name="latitude">Latitude of top-left corner of the rectangle.</param>
        /// <param name="width">Width. Geographic coordinates.</param>
        /// <param name="height">Height. Geographic coordinates.</param>
        public Rectangle(double longitude, double latitude, double width, double height)
        {
            _y = 0;
            _x = longitude;
            _y = latitude;
            _width = width;
            _height = height;

            InitPoints();
        }

        /// <summary>
        /// Creates a new rectangle.
        /// </summary>
        /// <param name="location">The position of the rectangle. Geographic coordinates.</param>
        /// <param name="size">The size of the rectangle. Geographic coordinates.</param>
        public Rectangle(GeoPoint location, Vector2d size):this(location.x, location.y, size.x, size.y)
        {
        
        }

        /// <summary>
        /// Creates a new rectangle.
        /// </summary>
        /// <param name="rect">Rectangle. Geographic coordinates.</param>
        public Rectangle(Rect rect): this(rect.x, rect.y, rect.width, rect.height)
        {
        
        }

        /// <summary>
        /// Creates a new rectangle.
        /// </summary>
        /// <param name="x">Position X. Geographic coordinates.</param>
        /// <param name="y">Position Y. Geographic coordinates.</param>
        /// <param name="width">Width. Geographic coordinates.</param>
        /// <param name="height">Height. Geographic coordinates.</param>
        /// <param name="borderColor">Border color.</param>
        public Rectangle(double x, double y, double width, double height, Color borderColor)
            : this(x, y, width, height)
        {
            _borderColor = borderColor;
        }

        /// <summary>
        /// Creates a new rectangle.
        /// </summary>
        /// <param name="position">The location of the rectangle. Geographic coordinates.</param>
        /// <param name="size">The size of the rectangle. Geographic coordinates.</param>
        /// <param name="borderColor">Border color.</param>
        public Rectangle(GeoPoint position, Vector2d size, Color borderColor)
            : this(position.x, position.y, size.x, size.y)
        {
            _borderColor = borderColor;
        }

        /// <summary>
        /// Creates a new rectangle.
        /// </summary>
        /// <param name="rect">Rectangle. Geographic coordinates.</param>
        /// <param name="borderColor">Border color.</param>
        public Rectangle(Rect rect, Color borderColor)
            : this(rect)
        {
            _borderColor = borderColor;
        }

        /// <summary>
        /// Creates a new rectangle.
        /// </summary>
        /// <param name="x">Position X. Geographic coordinates.</param>
        /// <param name="y">Position Y. Geographic coordinates.</param>
        /// <param name="width">Width. Geographic coordinates.</param>
        /// <param name="height">Height. Geographic coordinates.</param>
        /// <param name="borderColor">Border color.</param>
        /// <param name="borderWidth">Border width.</param>
        public Rectangle(double x, double y, double width, double height, Color borderColor, float borderWidth)
            : this(x, y, width, height, borderColor)
        {
            _borderWidth = borderWidth;
        }

        /// <summary>
        /// Creates a new rectangle.
        /// </summary>
        /// <param name="location">The location of the rectangle. Geographic coordinates.</param>
        /// <param name="size">The size of the rectangle. Geographic coordinates.</param>
        /// <param name="borderColor">Border color.</param>
        /// <param name="borderWidth">Border width.</param>
        public Rectangle(GeoPoint location, Vector2d size, Color borderColor, float borderWidth)
            : this(location, size, borderColor)
        {
            _borderWidth = borderWidth;
        }

        /// <summary>
        /// Creates a new rectangle.
        /// </summary>
        /// <param name="rect">Rectangle. Geographic coordinates.</param>
        /// <param name="borderColor">Border color.</param>
        /// <param name="borderWidth">Border width.</param>
        public Rectangle(Rect rect, Color borderColor, float borderWidth)
            : this(rect, borderColor)
        {
            _borderWidth = borderWidth;
        }

        /// <summary>
        /// Creates a new rectangle.
        /// </summary>
        /// <param name="x">Position X. Geographic coordinates.</param>
        /// <param name="y">Position Y. Geographic coordinates.</param>
        /// <param name="width">Width. Geographic coordinates.</param>
        /// <param name="height">Height. Geographic coordinates.</param>
        /// <param name="borderColor">Border color.</param>
        /// <param name="borderWidth">Border width.</param>
        /// <param name="backgroundColor">Background color.</param>
        public Rectangle(double x, double y, double width, double height, Color borderColor, float borderWidth, Color backgroundColor)
            : this(x, y, width, height, borderColor, borderWidth)
        {
            _backgroundColor = backgroundColor;
        }

        /// <summary>
        /// Creates a new rectangle.
        /// </summary>
        /// <param name="location">The location of the rectangle. Geographic coordinates.</param>
        /// <param name="size">The size of the rectangle. Geographic coordinates.</param>
        /// <param name="borderColor">Border color.</param>
        /// <param name="borderWidth">Border width.</param>
        /// <param name="backgroundColor">Background color.</param>
        public Rectangle(GeoPoint location, Vector2d size, Color borderColor, float borderWidth, Color backgroundColor)
            : this(location, size, borderColor, borderWidth)
        {
            _backgroundColor = backgroundColor;
        }

        /// <summary>
        /// Creates a new rectangle.
        /// </summary>
        /// <param name="rect">Rectangle. Geographic coordinates.</param>
        /// <param name="borderColor">Border color.</param>
        /// <param name="borderWidth">Border width.</param>
        /// <param name="backgroundColor">Background color.</param>
        public Rectangle(Rect rect, Color borderColor, float borderWidth, Color backgroundColor)
            : this(rect, borderColor, borderWidth)
        {
            _backgroundColor = backgroundColor;
        }

        public override void Draw(Color32[] buffer, Vector2 bufferPosition, int bufferWidth, int bufferHeight, float zoom, bool invertY = false)
        {
            if (!visible) return;
            if (range != null && !range.InRange(manager.map.view.zoom)) return;

            FillPoly(buffer, bufferPosition, bufferWidth, bufferHeight, zoom, backgroundColor, invertY);
            DrawLineToBuffer(buffer, bufferPosition, bufferWidth, bufferHeight, zoom, borderColor, borderWidth, true, invertY);
        }

        public override void DrawOnTileSet(TileSetDrawer drawer, int index)
        {
            base.DrawOnTileSet(drawer, index);

            if (!visible)
            {
                active = false;
                return;
            }

            if (range != null && !range.InRange(drawer.map.view.zoom))
            {
                active = false;
                return;
            }

            InitMesh(drawer, borderColor, backgroundColor);
            if (materials.Length > 1 && materials[1].mainTexture != _backgroundTexture) materials[1].mainTexture = _backgroundTexture;
            
            mapRect = drawer.map.view.rect;

            CalculateLocalPoints(true, false);

            Rect rect1 = new Rect(localPoints[0].x, localPoints[2].y, localPoints[2].x - localPoints[0].x, localPoints[0].y - localPoints[2].y);
            Vector2 sizeInScene = drawer.control.sizeInScene;
            Rect rect2 = new Rect(0, 0, sizeInScene.x, sizeInScene.y);

            bool ignoreLeft = false;
            bool ignoreRight = false;
            bool ignoreTop = false;
            bool ignoreBottom = false;
            int countIgnore = 0;

            if (checkMapBoundaries)
            {
                if (!rect2.Overlaps(rect1))
                {
                    if (active) active = false;
                    return;
                }
                if (!active) active = true;

                for (int i = 0; i < localPoints.Count; i++)
                {
                    Vector2 point = localPoints[i];
                    if (point.x < 0)
                    {
                        point.x = 0;
                        if (!ignoreLeft) countIgnore++;
                        ignoreLeft = true;
                    }
                    if (point.y < 0)
                    {
                        point.y = 0;
                        if (!ignoreTop) countIgnore++;
                        ignoreTop = true;
                    }
                    if (point.x > sizeInScene.x)
                    {
                        point.x = sizeInScene.x;
                        if (!ignoreRight) countIgnore++;
                        ignoreRight = true;
                    }
                    if (point.y > sizeInScene.y)
                    {
                        point.y = sizeInScene.y;
                        if (!ignoreBottom) countIgnore++;
                        ignoreBottom = true;
                    }

                    localPoints[i] = point;
                }
            }
        
            ResetMeshLists(4);

            if (backTriangles == null) backTriangles = new List<int>(6);
            else backTriangles.Clear();

            if (!checkMapBoundaries || !_backgroundTexture)
            {
                uv.AddRange(new[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) });
            }
            else
            {
                float uvx1 = Mathf.Max(-rect1.x / rect1.width, 0);
                float uvy1 = 1 - Mathf.Max(-rect1.y / rect1.height, 0);
                float uvx2 = Mathf.Min((rect2.width - rect1.x) / rect1.width, 1);
                float uvy2 = 1 - Mathf.Min((rect2.height - rect1.y) / rect1.height, 1);
                uv.AddRange(new []{ new Vector2(uvx1, uvy2), new Vector2(uvx2, uvy2), new Vector2(uvx2, uvy1), new Vector2(uvx1, uvy1) });
            }

            vertices.Add(new Vector3(-localPoints[0].x, -0.05f, localPoints[0].y));
            vertices.Add(new Vector3(-localPoints[1].x, -0.05f, localPoints[1].y));
            vertices.Add(new Vector3(-localPoints[2].x, -0.05f, localPoints[2].y));
            vertices.Add(new Vector3(-localPoints[3].x, -0.05f, localPoints[3].y));

            if (!ignoreTop)
            {
                vertices[2] += new Vector3(0, 0, borderWidth);
                vertices[3] += new Vector3(0, 0, borderWidth);
            }

            if (!ignoreBottom)
            {
                vertices[0] -= new Vector3(0, 0, borderWidth);
                vertices[1] -= new Vector3(0, 0, borderWidth);
            }

            if (!ignoreLeft)
            {
                vertices[0] -= new Vector3(borderWidth, 0, 0);
                vertices[3] -= new Vector3(borderWidth, 0, 0);
            }

            if (!ignoreRight)
            {
                vertices[1] += new Vector3(borderWidth, 0, 0);
                vertices[2] += new Vector3(borderWidth, 0, 0);
            }

            normals.Add(Vector3.up);
            normals.Add(Vector3.up);
            normals.Add(Vector3.up);
            normals.Add(Vector3.up);

            backTriangles.Add(0);
            backTriangles.Add(2);
            backTriangles.Add(1);
            backTriangles.Add(0);
            backTriangles.Add(3);
            backTriangles.Add(2);

            if (activePoints == null) activePoints = new List<Vector2>();
            else activePoints.Clear();

            if (countIgnore == 0)
            {
                activePoints.Add(localPoints[0] + new Vector2(borderWidth, 0));
                activePoints.Add(localPoints[1]);
                activePoints.Add(localPoints[2]);
                activePoints.Add(localPoints[3]);
                activePoints.Add(localPoints[0] + new Vector2(0, borderWidth));
                DrawActivePoints(drawer, ref activePoints, ref vertices, ref normals, ref triangles, ref uv, borderWidth);
            }
            else if (countIgnore == 1)
            {
                int off = 0;
                if (ignoreTop) off = 3;
                else if (ignoreRight) off = 2;
                else if (ignoreBottom) off = 1;

                for (int i = 0; i < 4; i++)
                {
                    int ci = i + off;
                    if (ci > 3) ci -= 4;
                    activePoints.Add(localPoints[ci]);
                }
                DrawActivePoints(drawer, ref activePoints, ref vertices, ref normals, ref triangles, ref uv, borderWidth);
            }
            else if (countIgnore == 2)
            {
                if (ignoreBottom && ignoreTop)
                {
                    activePoints.Add(localPoints[1]);
                    activePoints.Add(localPoints[2]);
                    DrawActivePoints(drawer, ref activePoints, ref vertices, ref normals, ref triangles, ref uv, borderWidth);
                    activePoints.Add(localPoints[3]);
                    activePoints.Add(localPoints[0]);
                    DrawActivePoints(drawer, ref activePoints, ref vertices, ref normals, ref triangles, ref uv, borderWidth);
                }
                else if (ignoreLeft && ignoreRight)
                {
                    activePoints.Add(localPoints[0]);
                    activePoints.Add(localPoints[1]);
                    DrawActivePoints(drawer, ref activePoints, ref vertices, ref normals, ref triangles, ref uv, borderWidth);
                    activePoints.Add(localPoints[2]);
                    activePoints.Add(localPoints[3]);
                    DrawActivePoints(drawer, ref activePoints, ref vertices, ref normals, ref triangles, ref uv, borderWidth);
                }
                else
                {
                    DrawActivePointsCI3(drawer, ignoreTop, ignoreRight, ignoreBottom, activePoints, localPoints, ref vertices, ref normals, ref triangles, ref uv);
                }
            }
            else if (countIgnore == 3)
            {
                DrawActivePointsCI3(drawer, ignoreTop, ignoreRight, ignoreBottom, activePoints, localPoints, ref vertices, ref normals, ref triangles, ref uv);
            }
            else if (countIgnore == 4)
            {
                DrawActivePoints(drawer, ref activePoints, ref vertices, ref normals, ref triangles, ref uv, borderWidth);
            }

            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uv);
            mesh.subMeshCount = 2;

            active = true;

            mesh.SetTriangles(triangles.ToArray(), 0);
            mesh.SetTriangles(backTriangles.ToArray(), 1);

            UpdateMaterialsQueue(drawer.control, index);
        }

        private void DrawActivePointsCI3(TileSetDrawer drawer, bool ignoreTop, bool ignoreRight, bool ignoreBottom, List<Vector2> activePoints,
            List<Vector2> localPoints, ref List<Vector3> vertices, ref List<Vector3> normals, ref List<int> borderTriangles, ref List<Vector2> uv)
        {
            int off = 0;

            if (ignoreTop) off = 3;
            else if (ignoreRight) off = 2;
            else if (ignoreBottom) off = 1;

            for (int i = 0; i < 2; i++)
            {
                int ci = i + off;
                if (ci > 3) ci -= 4;
                activePoints.Add(localPoints[ci]);
            }
            DrawActivePoints(drawer, ref activePoints, ref vertices, ref normals, ref borderTriangles, ref uv, borderWidth);
        }

        public override bool HitTest(GeoPoint location, int zoom)
        {
            if (location.x < x || location.x > x + width) return false;
            if (location.y < y || location.y > y + height) return false;
            return true;
        }

        private void InitPoints()
        {
            SetPoints(new[]
            {
                _x, _y,
                _x + _width, _y,
                _x + _width, _y + _height,
                _x, _y + _height
            });
        }
    }
}