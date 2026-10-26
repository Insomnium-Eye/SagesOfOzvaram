using System.Collections.Generic;

namespace SagesOfOzvaram.Combat
{
    /// <summary>
    /// Maps a Bleeding rank to the % of MAX HP it drains each turn (BaseUnit.ApplyBleedTick
    /// reads MaxHP, not current HP - this doc comment previously said "current", which was
    /// wrong). Weak raised 1%->5% on request; Minor/Moderate/Major/Severe are untouched, so Weak
    /// and Severe now happen to be equal (5%) - flagged here rather than silently rescaling the
    /// rest of the table, since only Weak was actually asked for.
    /// </summary>
    public static class BleedEffect
    {
        private static readonly Dictionary<StatusRank, float> PercentPerTurn = new Dictionary<StatusRank, float>
        {
            [StatusRank.Weak] = 0.05f,
            [StatusRank.Minor] = 0.02f,
            [StatusRank.Moderate] = 0.03f,
            [StatusRank.Major] = 0.04f,
            [StatusRank.Severe] = 0.05f,
        };

        public static float GetPercent(StatusRank rank) => PercentPerTurn[rank];
    }
}
