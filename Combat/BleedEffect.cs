using System.Collections.Generic;

namespace SagesOfOzvaram.Combat
{
    /// <summary>
    /// Maps a Bleeding rank to the % of CURRENT HP it drains each turn. First-pass linear
    /// scale (1% per rank step), pending a real balance pass.
    /// </summary>
    public static class BleedEffect
    {
        private static readonly Dictionary<StatusRank, float> PercentPerTurn = new Dictionary<StatusRank, float>
        {
            [StatusRank.Weak] = 0.01f,
            [StatusRank.Minor] = 0.02f,
            [StatusRank.Moderate] = 0.03f,
            [StatusRank.Major] = 0.04f,
            [StatusRank.Severe] = 0.05f,
        };

        public static float GetPercent(StatusRank rank) => PercentPerTurn[rank];
    }
}
