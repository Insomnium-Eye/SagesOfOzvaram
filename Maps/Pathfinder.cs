using System.Collections.Generic;

namespace SagesOfOzvaram.Maps
{
    /// <summary>
    /// Dijkstra-based pathfinding over the hex grid, respecting tile passability, blocking map
    /// objects (e.g. trees), and tiles occupied by units. Most tiles cost a normal step, but
    /// water costs an extra full AP on top - so this has to be cost-weighted, not plain BFS,
    /// to correctly route around water when a cheaper land path exists.
    /// </summary>
    public static class Pathfinder
    {
        private const float Epsilon = 0.0001f;

        /// <summary>
        /// Whether a unit could stand on/move through this tile: in bounds, marked Passable
        /// (currently only mountain tiles aren't), no Blocking map object on it (e.g. a tree),
        /// and not already occupied.
        /// </summary>
        public static bool IsPassable(HexGrid grid, Map map, int col, int row, HashSet<(int col, int row)> occupiedTiles)
        {
            if (!grid.IsInBounds(col, row))
                return false;

            Tile tile = map.GetTile(col, row);
            if (tile == null || !tile.Passable)
                return false;

            // Belt-and-suspenders: Type and Passable are independent properties that can drift
            // out of sync (e.g. DevConsole's "tile set ... type ..." changes Type without
            // touching Passable), so check the terrain rule directly rather than trusting the
            // flag alone. Mountain is the only impassable terrain for now.
            if (tile.Type == "mountain")
                return false;

            foreach (var obj in tile.Objects)
            {
                if (obj.Blocking)
                    return false;
            }

            return !occupiedTiles.Contains((col, row));
        }

        /// <summary>
        /// AP cost to move onto this tile: 1/tilesPerAP normally, plus a full extra AP if it's
        /// water. Assumes the tile is already known to be passable.
        /// </summary>
        private static float GetStepCost(Map map, int col, int row, int tilesPerAP)
        {
            float cost = 1f / tilesPerAP;
            if (map.GetTile(col, row)?.Type == "water")
                cost += 1f;
            return cost;
        }

        /// <summary>
        /// Every tile reachable from start within apBudget AP (Dijkstra, weighted by
        /// GetStepCost), each mapped to (tiles, waterTiles) needed to reach it - use these with
        /// the same ceil(tiles/tilesPerAP) + waterTiles formula ExecuteMove uses, to show the
        /// exact AP cost per tile for movement-range highlighting. `passageBlockingTiles` governs
        /// what the search may ROUTE THROUGH at all (e.g. a living enemy - never an ally, which is
        /// always passable - see Game1.GetPassageBlockingTiles); `destinationBlockingTiles` is the
        /// separate, usually larger set (every living unit, ally included) of tiles that may be
        /// routed through anyway but can't actually be the final result - a tile this search could
        /// only reach by routing through a blocked tile never reaches `bestSteps` in the first
        /// place, so filtering the destination set from the final result is enough; no separate
        /// pass is needed to also keep it out of further expansion.
        /// </summary>
        public static Dictionary<(int col, int row), (int tiles, int waterTiles)> GetReachableTiles(
            HexGrid grid, Map map, (int col, int row) start, int tilesPerAP, float apBudget,
            HashSet<(int col, int row)> passageBlockingTiles, HashSet<(int col, int row)> destinationBlockingTiles)
        {
            var bestCost = new Dictionary<(int, int), float> { [start] = 0f };
            var bestSteps = new Dictionary<(int, int), (int tiles, int waterTiles)> { [start] = (0, 0) };

            var queue = new PriorityQueue<(int, int), float>();
            queue.Enqueue(start, 0f);

            while (queue.TryDequeue(out var current, out float currentCost))
            {
                if (currentCost > bestCost[current] + Epsilon)
                    continue; // stale entry - a cheaper path to this tile was already processed

                foreach (var neighbor in grid.GetNeighbors(current.Item1, current.Item2))
                {
                    if (!IsPassable(grid, map, neighbor.Item1, neighbor.Item2, passageBlockingTiles))
                        continue;

                    bool isWater = map.GetTile(neighbor.Item1, neighbor.Item2)?.Type == "water";
                    float newCost = currentCost + GetStepCost(map, neighbor.Item1, neighbor.Item2, tilesPerAP);
                    if (newCost > apBudget + Epsilon)
                        continue;

                    if (!bestCost.TryGetValue(neighbor, out float existingCost) || newCost < existingCost - Epsilon)
                    {
                        bestCost[neighbor] = newCost;
                        var (prevTiles, prevWater) = bestSteps[current];
                        bestSteps[neighbor] = (prevTiles + 1, prevWater + (isWater ? 1 : 0));
                        queue.Enqueue(neighbor, newCost);
                    }
                }
            }

            bestSteps.Remove(start);
            foreach (var tile in destinationBlockingTiles)
                bestSteps.Remove(tile);
            return bestSteps;
        }

        /// <summary>
        /// Cheapest path (by AP cost, via Dijkstra) from start to target (start excluded,
        /// target last), or null if the target is unreachable/impassable, or occupied in a way
        /// that blocks it as a final stop. `passageBlockingTiles` governs what the route may pass
        /// THROUGH at all (e.g. a living enemy - never an ally, which is always passable for
        /// routing purposes - see Game1.GetPassageBlockingTiles); `destinationBlockingTiles` is
        /// the separate, usually larger set (every living unit, ally included - see
        /// Game1.GetDestinationBlockingTiles) that additionally rules out `target` itself as a
        /// place to actually end up, even if the route could otherwise pass through or near it -
        /// this only needs checking against `target`, since passing through en route is fine.
        /// </summary>
        public static List<(int col, int row)> FindPath(
            HexGrid grid, Map map, (int col, int row) start, (int col, int row) target, int tilesPerAP,
            HashSet<(int col, int row)> passageBlockingTiles, HashSet<(int col, int row)> destinationBlockingTiles)
        {
            if (start == target)
                return new List<(int, int)>();

            if (!IsPassable(grid, map, target.col, target.row, passageBlockingTiles) || destinationBlockingTiles.Contains(target))
                return null;

            var bestCost = new Dictionary<(int, int), float> { [start] = 0f };
            var cameFrom = new Dictionary<(int, int), (int, int)>();

            var queue = new PriorityQueue<(int, int), float>();
            queue.Enqueue(start, 0f);

            while (queue.TryDequeue(out var current, out float currentCost))
            {
                if (currentCost > bestCost[current] + Epsilon)
                    continue;

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
                    if (!IsPassable(grid, map, neighbor.Item1, neighbor.Item2, passageBlockingTiles))
                        continue;

                    float newCost = currentCost + GetStepCost(map, neighbor.Item1, neighbor.Item2, tilesPerAP);

                    if (!bestCost.TryGetValue(neighbor, out float existingCost) || newCost < existingCost - Epsilon)
                    {
                        bestCost[neighbor] = newCost;
                        cameFrom[neighbor] = current;
                        queue.Enqueue(neighbor, newCost);
                    }
                }
            }

            return null;
        }
    }
}
