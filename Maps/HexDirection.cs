namespace SagesOfOzvaram.Maps
{
    /// <summary>
    /// The 6 directions a unit can face on a pointy-top hex grid, matching HexGrid's cube
    /// coordinate axes (see HexGrid.GetDirectionToNeighbor).
    /// </summary>
    public enum HexDirection
    {
        East,
        NorthEast,
        NorthWest,
        West,
        SouthWest,
        SouthEast
    }

    public static class HexDirectionExtensions
    {
        /// <summary>
        /// The direction directly opposite this one (e.g. East &lt;-&gt; West) - i.e. the
        /// direction "behind" a unit facing this way.
        /// </summary>
        public static HexDirection Opposite(this HexDirection direction) => (HexDirection)(((int)direction + 3) % 6);
    }
}
