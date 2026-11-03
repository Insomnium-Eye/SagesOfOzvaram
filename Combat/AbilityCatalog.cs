using System.Collections.Generic;
using SagesOfOzvaram.Units;

namespace SagesOfOzvaram.Combat
{
    /// <summary>
    /// Abilities: a new category of action, distinct from a unit's weapon/racial Attack Menu
    /// moves, its class's Cards, and the generic Actions ("Pick Up") slot - something a unit just
    /// HAS, granted innately by its Rank, Race, or (once a stable per-unit identity exists beyond
    /// its C# type) its own individual Id, rather than drawn from a hand or equipped. Reuses
    /// Combat.Move for the actual cost/range/effect data (same as a racial move or a specially-
    /// flagged SpellCard already does for Blink/Tame Beast/Place Trap/Reshuffle) rather than
    /// inventing a parallel data shape - an ability's "kind" is still just a Move.Is* flag, kept
    /// OUT of AvailableMovesWithSource/the Attack Menu/AttackResolver the same way those are,
    /// handled directly by Game1 (CastSummonPortal) since it needs targeting/world-state access
    /// Move alone can't provide. GetAbilitiesFor(unit) is the one place every granting rule lives,
    /// so a future Race- or unit-specific ability just adds another check there, not a new system.
    /// </summary>
    public static class AbilityCatalog
    {
        /// <summary>
        /// First ability, on request: granted to every Elite- or Hero-rank unit (the 4 Apprentice
        /// heroes are Elite - see Units.Heroes.* constructors/UnitRank - plus any future named
        /// Hero-rank character). Conjures a Generic Portal nearby - same mechanics, cost, and
        /// range as Phase 1 of CastSummon (2 AP/5 MP, Game1.GenericPortalPlacementRange tiles) -
        /// but as its OWN standalone action, not tied to summoning a creature in the same breath.
        /// Game1.PlayerHasActiveGenericPortal/GetGenericPortalSurcharge both key off ANY living
        /// Portal owned by the caster, regardless of which path created it, so a portal placed
        /// via this ability already waives the normal summon-a-creature surcharge for free - no
        /// extra plumbing needed for that half of the request.
        /// </summary>
        public static readonly Move SummonPortal = new Move(
            "Summon Portal", "Conjures a Generic Portal nearby, so creatures can be summoned without paying the usual portal surcharge.",
            apCost: 2, mpCost: 5, range: 0, baseAccuracy: 1f, baseDamage: 0, isSummonPortal: true);

        /// <summary>Every ability `unit` currently has access to - by Rank for now (Elite/Hero get Summon Portal); a future pass can add Race- or unit-specific grants here too without changing how any of this is consumed elsewhere.</summary>
        public static List<Move> GetAbilitiesFor(BaseUnit unit)
        {
            var abilities = new List<Move>();

            if (unit.Rank == UnitRank.Elite || unit.Rank == UnitRank.Hero)
                abilities.Add(SummonPortal);

            return abilities;
        }
    }
}
