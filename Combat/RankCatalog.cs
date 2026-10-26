using System.Collections.Generic;
using SagesOfOzvaram.Units;

namespace SagesOfOzvaram.Combat
{
    /// <summary>
    /// Starting Max AP per UnitRank - on request, "for now the only thing making them different."
    /// Weak (1)/Normal (3)/Elite (5) are the real, explicitly requested values. Hero/Boss/
    /// Legendary/Mythical aren't used by any current unit, so their numbers below just continue
    /// the stated +2-per-step pattern rather than being separately specified - unconfirmed
    /// placeholders, not real balance, until an actual unit of one of those ranks exists.
    /// </summary>
    public static class RankCatalog
    {
        private static readonly Dictionary<UnitRank, int> MaxAPByRank = new Dictionary<UnitRank, int>
        {
            [UnitRank.Weak] = 1,
            [UnitRank.Normal] = 3,
            [UnitRank.Elite] = 5,
            [UnitRank.Hero] = 7,
            [UnitRank.Boss] = 9,
            [UnitRank.Legendary] = 11,
            [UnitRank.Mythical] = 13,
        };

        public static int GetMaxAP(UnitRank rank) => MaxAPByRank[rank];
    }
}
