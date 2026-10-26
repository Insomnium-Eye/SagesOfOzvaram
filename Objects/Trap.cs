using System.Collections.Generic;
using SagesOfOzvaram.Combat;
using SagesOfOzvaram.Units;

namespace SagesOfOzvaram.Objects
{
    /// <summary>
    /// A stationary battlefield hazard placed by a Move.IsPlaceTrap spell (e.g. Place Trap sets a
    /// "Bearat Trap" - see Game1.CastPlaceTrap/ExecutePlaceTrap) instead of attacking immediately.
    /// Triggers and is consumed the instant ANY unit - any team, any Disposition, even Owner's own
    /// team - steps onto or passes through a tile within Range of Tile (see Game1.MoveUnitTowards/
    /// TriggerTrap); nothing is immune by team the way the original single-target version of this
    /// was. Visible only to Owner's own team (GetTeamRoot) regardless of Fog of War's own
    /// vision-range check - see Draw's trap-rendering pass - and (once Flying exists as a real
    /// mechanic elsewhere in the game) will need to exempt a flying mover from triggering it at all.
    ///
    /// Deliberately NOT a BaseUnit, despite having a Position-like Tile/Owner: a Trap has no turn
    /// and no HP, and critically must never appear in any of the "every living unit, by team"
    /// collections Game1 builds from _units for pathfinding/targeting (GetPassageBlockingTiles,
    /// GetDestinationBlockingTiles, every AI/player attack-target list) - if it did, those would
    /// treat its own tile as impassable/un-landable for every other team, making it physically
    /// impossible for anything to ever walk onto it and trigger it. Tracked in its own list
    /// (Game1._placedTraps) instead, sidestepping all of that entirely.
    /// </summary>
    public class Trap
    {
        public string Name;
        public (int col, int row) Tile;
        public BaseUnit Owner;

        /// <summary>How far (in tiles) from Tile a unit still triggers this trap - 0 (every trap so far) means only the exact tile counts; a future, larger-area trap (e.g. Explosive Trap) could set this higher.</summary>
        public int Range;

        // The trap's own trigger-time effect, independent of any caster stat - a trap resolves on
        // its own, with no attacker present at the moment it actually goes off, so (unlike a
        // SpellCard's Move) there's nothing here to scale off Strength/Intelligence.
        public int FlatDamage;
        public DamageType DamageType;

        /// <summary>Every StatusEffect this trap applies to whoever triggers it, unconditionally (no chance roll, no per-effect condition) - e.g. the Bearat Trap's Weak Root + Weak Bleed (see Game1.CreateBearatTrap). Built fresh per Trap instance, not shared/cloned, since a Trap only ever triggers once (it's removed from Game1._placedTraps the moment it does) - there's no second victim to worry about reusing these same StatusEffect objects against.</summary>
        public List<StatusEffect> AppliedEffects = new List<StatusEffect>();
    }
}
