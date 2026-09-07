using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
 
namespace SagesOfOzvaram.Maps
{
    /// <summary>
    /// Renders hex grid and handles visualization in MonoGame.
    /// </summary>
    public class HexGridRenderer
    {
        private HexGrid _grid;
        private SpriteBatch _spriteBatch;
        private Texture2D _whitePixel; // 1x1 white texture for drawing lines
 
        // Camera
        public Vector2 CameraPosition { get; set; } = Vector2.Zero;
        public float ZoomLevel { get; set; } = 1f;
        public const float MIN_ZOOM = 0.5f;
        public const float MAX_ZOOM = 3f;
 
        // Grid colors
        private Color _gridLineColor = new Color(100, 100, 100, 255);
        private Color _selectedHexColor = new Color(255, 200, 0, 100);
        private Color _hoverHexColor = new Color(200, 200, 255, 50);
 
        public HexGridRenderer(HexGrid grid, SpriteBatch spriteBatch, GraphicsDevice graphicsDevice)
        {
            _grid = grid;
            _spriteBatch = spriteBatch;
 
            // Create 1x1 white pixel texture for line drawing
            _whitePixel = new Texture2D(graphicsDevice, 1, 1);
            _whitePixel.SetData(new[] { Color.White });
        }
 
        /// <summary>
        /// Draw the entire hex grid with outlines.
        /// </summary>
        public void DrawGrid(Vector2 viewportSize)
        {
            _spriteBatch.Begin(transformMatrix: GetCameraMatrix(viewportSize));
 
            // Draw all hex tiles
            for (int col = 0; col < _grid.Width; col++)
            {
                for (int row = 0; row < _grid.Height; row++)
                {
                    Vector2 worldPos = _grid.HexToWorld(col, row);
                    DrawHexOutline(worldPos, _gridLineColor, 2f);
                }
            }
 
            _spriteBatch.End();
        }
 
        /// <summary>
        /// Draw a single hex outline at world position.
        /// </summary>
        private void DrawHexOutline(Vector2 center, Color color, float lineWidth)
        {
            var vertices = _grid.GetHexVertices(center);
 
            // Draw lines between vertices
            for (int i = 0; i < 6; i++)
            {
                int nextI = (i + 1) % 6;
                DrawLine(vertices[i], vertices[nextI], color, lineWidth);
            }
        }
 
        /// <summary>
        /// Draw a filled hex at world position (for highlighting).
        /// </summary>
        private void DrawHexFilled(Vector2 center, Color color, float alpha = 0.5f)
        {
            var vertices = _grid.GetHexVertices(center);
            color = new Color(color.R, color.G, color.B, (int)(255 * alpha));
 
            // Simple triangle fan fill
            Vector2 centerPoint = center;
            for (int i = 0; i < 6; i++)
            {
                int nextI = (i + 1) % 6;
                DrawTriangle(centerPoint, vertices[i], vertices[nextI], color);
            }
        }
 
        /// <summary>
        /// Draw line between two points using stretched pixel.
        /// </summary>
        private void DrawLine(Vector2 start, Vector2 end, Color color, float thickness)
        {
            float angle = (float)Math.Atan2(end.Y - start.Y, end.X - start.X);
            float length = Vector2.Distance(start, end);
 
            _spriteBatch.Draw(
                _whitePixel,
                start,
                null,
                color,
                angle,
                Vector2.Zero,
                new Vector2(length, thickness),
                SpriteEffects.None,
                0f
            );
        }
 
        /// <summary>
        /// Draw filled triangle.
        /// </summary>
        private void DrawTriangle(Vector2 p1, Vector2 p2, Vector2 p3, Color color)
        {
            // Simple implementation using three lines (not a true fill, but works for visualization)
            DrawLine(p1, p2, color, 1f);
            DrawLine(p2, p3, color, 1f);
            DrawLine(p3, p1, color, 1f);
        }
 
        /// <summary>
        /// Highlight a selected hex.
        /// </summary>
        public void DrawHighlightedHex(int col, int row, Color highlightColor)
        {
            if (!_grid.IsInBounds(col, row)) return;
 
            Vector2 worldPos = _grid.HexToWorld(col, row);
            DrawHexFilled(worldPos, highlightColor, 0.3f);
        }
 
        /// <summary>
        /// Draw hex at cursor position (hover effect).
        /// </summary>
        public void DrawHoverHex(Vector2 cursorWorldPos)
        {
            DrawHexFilled(cursorWorldPos, _hoverHexColor, 0.2f);
        }
 
        /// <summary>
        /// Get camera transformation matrix for viewport.
        /// </summary>
        private Matrix GetCameraMatrix(Vector2 viewportSize)
        {
            return Matrix.CreateTranslation(-CameraPosition.X, -CameraPosition.Y, 0f) *
                   Matrix.CreateScale(ZoomLevel, ZoomLevel, 1f) *
                   Matrix.CreateTranslation(viewportSize.X / 2f, viewportSize.Y / 2f, 0f);
        }
 
        /// <summary>
        /// Pan camera by delta.
        /// </summary>
        public void PanCamera(Vector2 delta)
        {
            CameraPosition += delta / ZoomLevel;
        }
 
        /// <summary>
        /// Zoom camera in/out.
        /// </summary>
        public void Zoom(float delta, Vector2 cursorPos)
        {
            float oldZoom = ZoomLevel;
            ZoomLevel = MathHelper.Clamp(ZoomLevel + delta, MIN_ZOOM, MAX_ZOOM);
 
            // Adjust camera to zoom toward cursor
            float zoomDelta = ZoomLevel - oldZoom;
            CameraPosition += (cursorPos - CameraPosition) * (zoomDelta / oldZoom);
        }
 
        /// <summary>
        /// Convert screen coordinates to world coordinates (accounting for camera/zoom).
        /// </summary>
        public Vector2 ScreenToWorld(Vector2 screenPos, Vector2 viewportSize)
        {
            return (screenPos - viewportSize / 2f) / ZoomLevel + CameraPosition;
        }
 
        /// <summary>
        /// Convert world coordinates to screen coordinates.
        /// </summary>
        public Vector2 WorldToScreen(Vector2 worldPos, Vector2 viewportSize)
        {
            return (worldPos - CameraPosition) * ZoomLevel + viewportSize / 2f;
        }
 
        /// <summary>
        /// Get hex coordinates at screen position (useful for clicking).
        /// </summary>
        public (int col, int row) GetHexAtScreenPos(float screenX, float screenY, Vector2 viewportSize)
        {
            Vector2 worldPos = ScreenToWorld(new Vector2(screenX, screenY), viewportSize);
            return _grid.WorldToHex(worldPos);
        }
 
        /// <summary>
        /// Get camera transformation matrix (public for external use).
        /// </summary>
        public Matrix GetCameraMatrixPublic(Vector2 viewportSize)
        {
            return Matrix.CreateTranslation(-CameraPosition.X, -CameraPosition.Y, 0f) *
                   Matrix.CreateScale(ZoomLevel, ZoomLevel, 1f) *
                   Matrix.CreateTranslation(viewportSize.X / 2f, viewportSize.Y / 2f, 0f);
        }
 
        public void Dispose()
        {
            _whitePixel?.Dispose();
        }
    }
}
