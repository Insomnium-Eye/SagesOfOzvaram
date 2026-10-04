using System.Collections.Generic;

namespace SagesOfOzvaram.Combat
{
    /// <summary>
    /// Which cards have actually been play-tested, by Name (stable even when a card's numeric Id
    /// shifts due to the catalog changing - see SpellCard/SummonCard.Id's own doc comment on that
    /// instability). This is plain source data, not runtime state - when the user reports a card
    /// as tested, its name gets added here and the game rebuilt, so "tested" status survives
    /// every relaunch across a long testing session instead of resetting each time. Read via
    /// SpellCard.Tested/SummonCard.Tested; only Combat.TestMode (the deck-ordering logic) and the
    /// card UI (an indicator) should ever need to check it directly.
    /// </summary>
    public static class TestedCards
    {
        public static readonly HashSet<string> Names = new HashSet<string>
        {
            // Add a card's exact Name here once the user confirms they've tested it.
        };
    }
}
