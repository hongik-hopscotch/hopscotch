/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.Collections;
using UnityEngine;

namespace OnlineMaps
{
    /// <summary>
    /// Class that draws a line on the map.
    /// </summary>
    public class Line : DrawingElement
    {
        /// <summary>
        /// Forces the line to follow the relief.
        /// </summary>
        public bool followRelief = false;

        /// <summary>
        /// Sets the line width used for HitTest.
        /// </summary>
        public float? hitTestWidth;

        private Color _color = Color.black;
        private Texture2D _texture;
        private float _width = 1;

        /// <summary>
        /// Color of the line.
        /// </summary>
        public Color color
        {
            get => _color;
            set
            {
                _color = value;
                manager.map.Redraw();
            }
        }

        protected override bool createBackgroundMaterial => false;

        protected override string defaultName => "Line";

        /// <summary>
        /// Texture of line. Uses only in tileset.
        /// </summary>
        public Texture2D texture
        {
            get => _texture;
            set
            {
                _texture = value;
                if (manager != null) manager.map.Redraw();
            }
        }

        /// <summary>
        /// IEnumerable of points of the line. Geographic coordinates.
        /// Can be:
        /// IEnumerable of Vector2, Vector2d, GeoPoint, TilePoint, MercatorPoint, where X - longitude, Y - latitude, 
        /// IEnumerable of float or IEnumerable of double, where values (longitude, latitude, longitude, latitude... etc).
        /// </summary>
        public GeoPoint[] points
        {
            get { return _points; }
            set
            {
                if (value == null) throw new Exception("Points can not be null.");
                SetPoints(value);
            }
        }

        protected override bool splitToPieces => followRelief && hasElevation;

        /// <summary>
        /// Width of the line.
        /// </summary>
        public float width
        {
            get => _width;
            set
            {
                _width = value;
                if (manager != null) manager.map.Redraw();
            }
        }

        /// <summary>
        /// Creates a new line.
        /// </summary>
        public Line()
        {
            _points = Array.Empty<GeoPoint>();
            mercatorPoints = Array.Empty<MercatorPoint>();
        }

        /// <summary>
        /// Creates a new line.
        /// </summary>
        /// <param name="points">
        /// IEnumerable of points of the line. Geographic coordinates.
        /// The values can be of type: Vector2, Vector2d, GeoPoint, TilePoint, MercatorPoint, float, double.
        /// If values float or double, the value should go in pairs(longitude, latitude).
        /// </param>
        public Line(IEnumerable points):this()
        {
            if (_points == null) throw new Exception("Points can not be null.");
            SetPoints(points);
        }

        /// <summary>
        /// Creates a new line.
        /// </summary>
        /// <param name="points">
        /// IEnumerable of points of the line. Geographic coordinates.
        /// The values can be of type: Vector2, Vector2d, GeoPoint, TilePoint, MercatorPoint, float, double.
        /// If values float or double, the value should go in pairs(longitude, latitude).
        /// </param>
        /// <param name="color">Color of the line.</param>
        public Line(IEnumerable points, Color color):this(points)
        {
            _color = color;
        }

        /// <summary>
        /// Creates a new line.
        /// </summary>
        /// <param name="points">
        /// IEnumerable of points of the line. Geographic coordinates.
        /// The values can be of type: Vector2, Vector2d, GeoPoint, TilePoint, MercatorPoint, float, double.
        /// If values float or double, the value should go in pairs(longitude, latitude).
        /// </param>
        /// <param name="color">Color of the line.</param>
        /// <param name="width">Width of the line.</param>
        public Line(IEnumerable points, Color color, float width) : this(points, color)
        {
            _width = width;
        }

        public override void Draw(Color32[] buffer, Vector2 bufferPosition, int bufferWidth, int bufferHeight, float zoom, bool invertY = false)
        {
            if (!visible) return;
            if (range != null && !range.InRange(manager.map.view.zoom)) return;

            DrawLineToBuffer(buffer, bufferPosition, bufferWidth, bufferHeight, zoom, color, width, false, invertY);
        }

        public override void DrawOnTileSet(TileSetDrawer drawer, int index)
        {
            if (points == null) return;

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

            active = true;

            InitMesh(drawer, color, default, texture);
            InitLineMesh(drawer, width);

            mesh.Clear();

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uv);

            mesh.SetTriangles(triangles.ToArray(), 0);

            UpdateMaterialsQueue(drawer.control, index);
        }

        protected override void DisposeLate()
        {
            base.DisposeLate();
            
            texture = null;
        }

        public override bool HitTest(GeoPoint location, int zoom)
        {
            if (points == null) return false;

            TilePoint c = location.ToTile(manager.map, zoom) * Constants.TileSize;
            TilePoint prev = default;
            int index = 0;
            
            float w = hitTestWidth ?? width;
            float sqrW = w * w;
            
            foreach (MercatorPoint mp in mercatorPoints)
            {
                TilePoint p = mp.ToTile(zoom);
                if (index++ > 0)
                {
                    Vector2d np = Geometry.NearestPointStrict(c, p, prev) * Constants.TileSize;
                    if ((c - np).sqrMagnitude < sqrW) return true;
                }
                prev = p;
            }
            return false;
        }
    }
}