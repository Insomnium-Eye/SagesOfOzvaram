using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
 
namespace SagesOfOzvaram.Maps
{
    /// <summary>
    /// Hex grid system using axial (odd-r offset) coordinates.
    /// Each hex is identified by (col, row) coordinates.
    /// </summary>
    public class HexGrid
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public float TileSize { get; private set; } // Radius of hex (pixels)
 
        // Hex dimensions
        private float _hexWidth;
        private float _hexHeight;
        private float _hexVerticalOffset;
        private float _hexHorizontalOffset;
 
        public HexGrid(int width, int height, float tileSize = 32f)
        {
            Width = width;
            Height = height;
            TileSize = tileSize;
 
            // Pointy-top hex dimensions (circumradius = TileSize)
            // Flat-to-flat width = sqrt(3) * radius
            // Point-to-point height = 2 * radius
            _hexWidth = (float)(Math.Sqrt(3f) * TileSize);
            _hexHeight = 2f * TileSize;
            _hexVerticalOffset = _hexHeight * 0.75f;  // 1.5x radius for row spacing (pointy-top odd-r)
            _hexHorizontalOffset = _hexWidth;  // Full width for column spacing
        }
 
        /// <summary>
        /// Convert axial hex coordinates to world position (pixels).
        /// </summary>
        public Vector2 HexToWorld(int col, int row)
        {
            // Pointy-top odd-r offset coordinates
            // Odd ROWS are offset RIGHT by half a column width
            float x = col * _hexHorizontalOffset;
            if (row % 2 == 1)
                x += _hexHorizontalOffset * 0.5f;
            
            float y = row * _hexVerticalOffset;
            
            return new Vector2(x, y);
        }
 
        /// <summary>
        /// Convert world position (pixels) to nearest hex coordinates.
        /// </summary>
        public (int col, int row) WorldToHex(Vector2 worldPos)
        {
            // Convert world position back to hex coordinates
            float q = (2f / 3f * worldPos.X) / TileSize;
            float r = (-1f / 3f * worldPos.X + (float)Math.Sqrt(3f) / 3f * worldPos.Y) / TileSize;
 
            return RoundToHex(q, r);
        }
 
        /// <summary>
        /// Round floating-point hex coordinates to nearest valid hex.
        /// </summary>
        private (int col, int row) RoundToHex(float q, float r)
        {
            float s = -q - r;
 
            float rq = (float)Math.Round(q);
            float rr = (float)Math.Round(r);
            float rs = (float)Math.Round(s);
 
            float qDiff = Math.Abs(rq - q);
            float rDiff = Math.Abs(rr - r);
            float sDiff = Math.Abs(rs - s);
 
            if (qDiff > rDiff && qDiff > sDiff)
                rq = -rr - rs;
            else if (rDiff > sDiff)
                rr = -rq - rs;
 
            int col = (int)rq;
            int row = (int)rr;
 
            // Convert cube to odd-r offset
            return CubeToOddR(col, row);
        }
 
        /// <summary>
        /// Convert cube coordinates to odd-r offset coordinates.
        /// </summary>
        private (int col, int row) CubeToOddR(int q, int r)
        {
            int col = q + (r - (r & 1)) / 2;
            int row = r;
            return (col, row);
        }

        /// <summary>
        /// Convert odd-r offset coordinates to cube coordinates.
        /// </summary>
        private (int q, int r) OddRToCube(int col, int row)
        {
            int q = col - (row - (row & 1)) / 2;
            int r = row;
            return (q, r);
        }

        // Cube-coordinate unit vectors for each HexDirection, in enum declaration order.
        private static readonly (int dq, int dr)[] DirectionVectors =
        {
            (1, 0),    // East
            (1, -1),   // NorthEast
            (0, -1),   // NorthWest
            (-1, 0),   // West
            (-1, 1),   // SouthWest
            (0, 1),    // SouthEast
        };

        /// <summary>
        /// Which HexDirection points from one tile to an ADJACENT one, or null if they aren't
        /// neighbors. Used for facing/backstab checks.
        /// </summary>
        public HexDirection? GetDirectionToNeighbor(int fromCol, int fromRow, int toCol, int toRow)
        {
            var (fq, fr) = OddRToCube(fromCol, fromRow);
            var (tq, tr) = OddRToCube(toCol, toRow);
            int dq = tq - fq;
            int dr = tr - fr;

            for (int i = 0; i < DirectionVectors.Length; i++)
            {
                if (DirectionVectors[i].dq == dq && DirectionVectors[i].dr == dr)
                    return (HexDirection)i;
            }
            return null;
        }

        /// <summary>
        /// The tile one step away from (col, row) in the given direction (not bounds-checked).
        /// </summary>
        public (int col, int row) GetNeighborCoords(int col, int row, HexDirection direction)
        {
            var (q, r) = OddRToCube(col, row);
            var (dq, dr) = DirectionVectors[(int)direction];
            return CubeToOddR(q + dq, r + dr);
        }
 
        /// <summary>
        /// Get all 6 neighboring hex tiles (cardinal + intercardinal directions).
        /// </summary>
        public List<(int col, int row)> GetNeighbors(int col, int row)
        {
            var neighbors = new List<(int, int)>();
 
            if (row % 2 == 0) // Even rows
            {
                // Neighbors for even row
                neighbors.Add((col + 1, row));     // Right
                neighbors.Add((col - 1, row));     // Left
                neighbors.Add((col, row - 1));     // Upper right
                neighbors.Add((col - 1, row - 1)); // Upper left
                neighbors.Add((col, row + 1));     // Lower right
                neighbors.Add((col - 1, row + 1)); // Lower left
            }
            else // Odd rows
            {
                // Neighbors for odd row
                neighbors.Add((col + 1, row));     // Right
                neighbors.Add((col - 1, row));     // Left
                neighbors.Add((col + 1, row - 1)); // Upper right
                neighbors.Add((col, row - 1));     // Upper left
                neighbors.Add((col + 1, row + 1)); // Lower right
                neighbors.Add((col, row + 1));     // Lower left
            }
 
            return neighbors;
        }
 
        /// <summary>
        /// Get all hexes within a certain distance (radius).
        /// </summary>
        public List<(int col, int row)> GetHexesInRadius(int centerCol, int centerRow, int radius)
        {
            var hexes = new List<(int, int)>();
 
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    if (GetDistance(centerCol, centerRow, x, y) <= radius)
                    {
                        hexes.Add((x, y));
                    }
                }
            }
 
            return hexes;
        }
 
        /// <summary>
        /// Calculate distance between two hex tiles.
        /// </summary>
        public int GetDistance(int col1, int row1, int col2, int row2)
        {
            var (q1, r1) = OddRToCube(col1, row1);
            var (q2, r2) = OddRToCube(col2, row2);
            int s1 = -q1 - r1;
            int s2 = -q2 - r2;
 
            return (Math.Abs(q1 - q2) + Math.Abs(r1 - r2) + Math.Abs(s1 - s2)) / 2;
        }
 
        /// <summary>
        /// Check if coordinates are within grid bounds.
        /// </summary>
        public bool IsInBounds(int col, int row)
        {
            return col >= 0 && col < Width && row >= 0 && row < Height;
        }
 
        /// <summary>
        /// Get the vertices of a hex polygon at world position (for rendering outline).
        /// </summary>
        public Vector2[] GetHexVertices(Vector2 center, float radius = -1f)
        {
            if (radius <= 0) radius = TileSize;
 
            var vertices = new Vector2[6];
            for (int i = 0; i < 6; i++)
            {
                // Pointy-top hex: start at 30 degrees (π/6)
                float angle = (float)(Math.PI / 3f * i + Math.PI / 6f);
                vertices[i] = center + new Vector2(
                    radius * (float)Math.Cos(angle),
                    radius * (float)Math.Sin(angle)
                );
            }
 
            return vertices;
        }
    }
}
