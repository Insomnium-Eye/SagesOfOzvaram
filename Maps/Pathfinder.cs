using System.Collections.Generic;

namespace SagesOfOzvaram.Maps
{
    /// <summary>
    /// BFS-based pathfinding over the hex grid, respecting tile passability, blocking map
    /// objects (e.g. trees), and tiles occupied by units. Every step costs 1 tile (no terrain
    /// movement-cost differences yet), so BFS gives the shortest path.
    /// </summary>
    public static class Pathfinder
    {
        /// <summary>
        /// Whether a unit could stand on/move through this tile: in bounds, marked Passable,
        /// no Blocking map object on it (e.g. a tree), and not already occupied.
        /// </summary>
        public static bool IsPassable(HexGrid grid, Map map, int col, int row, HashSet<(int col, int row)> occupiedTiles)
        {
            if (!grid.IsInBounds(col, row))
                return false;

            Tile tile = map.GetTile(col, row);
            if (tile == null || !tile.Passable)
                return false;

            foreach (var obj in tile.Objects)
            {
                if (obj.Blocking)
                    return false;
            }

            return !occupiedTiles.Contains((col, row));
        }

        /// <summary>
        /// Every tile reachable from start within maxTiles steps, mapped to how many tiles it
        /// takes to reach it. Used to compute a unit's movement range for highlighting.
        /// </summary>
        public static Dictionary<(int col, int row), int> GetReachableTiles(
            HexGrid grid, Map map, (int col, int row) start, int maxTiles, HashSet<(int col, int row)> occupiedTiles)
        {
            var distances = new Dictionary<(int, int), int> { [start] = 0 };
            var queue = new Queue<(int, int)>();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                int dist = distances[current];
                if (dist >= maxTiles)
                    continue;

                foreach (var neighbor in grid.GetNeighbors(current.Item1, current.Item2))
                {
                    if (distances.ContainsKey(neighbor))
                        continue;
                    if (!IsPassable(grid, map, neighbor.Item1, neighbor.Item2, occupiedTiles))
                        continue;

                    distances[neighbor] = dist + 1;
                    queue.Enqueue(neighbor);
                }
            }

            return distances;
        }

        /// <summary>
        /// Shortest path from start to target (start excluded, target last), or null if the
        /// target is unreachable/impassable/occupied.
        /// </summary>
        public static List<(int col, int row)> FindPath(
            HexGrid grid, Map map, (int col, int row) start, (int col, int row) target, HashSet<(int col, int row)> occupiedTiles)
        {
            if (start == target)
                return new List<(int, int)>();

            if (!IsPassable(grid, map, target.col, target.row, occupiedTiles))
                return null;

            var cameFrom = new Dictionary<(int, int), (int, int)>();
            var visited = new HashSet<(int, int)> { start };
            var queue = new Queue<(int, int)>();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == target)
                {
                    var path = new List<(int, int)>();
                    var step = target;
                    while (step != start)
                    {
                        path.Add(step);
                        step = cameFrom[step];
                    }
                    path.Reverse();
                    return path;
                }

                foreach (var neighbor in grid.GetNeighbors(current.Item1, current.Item2))
                {
                    if (visited.Contains(neighbor))
                        continue;
                    if (!IsPassable(grid, map, neighbor.Item1, neighbor.Item2, occupiedTiles))
                        continue;

                    visited.Add(neighbor);
                    cameFrom[neighbor] = current;
                    queue.Enqueue(neighbor);
                }
            }

            return null;
        }
    }
}
