using System.Collections.Generic;
using System.Linq;
using SagesOfOzvaram.Maps;
using SagesOfOzvaram.Units;

namespace SagesOfOzvaram.Combat
{
    /// <summary>The context a turn menu can be opened under - just Combat today, but Build takes it explicitly so a future non-combat mode (exploration, a shop, ...) has somewhere to plug in without Game1 special-casing it.</summary>
    public enum GameMode
    {
        Combat
    }

    /// <summary>
    /// Builds a unit's turn menu fresh every time it's opened, by checking what's actually true
    /// right now, instead of Game1 showing one fixed, unconditional list of options regardless of
    /// whether they'd do anything: the unit itself (its Class, Speed, active StatusEffects,
    /// whether it's Knocked Down, ...), the current GameMode, the tile it's standing on and the
    /// ones adjacent to it (is there anywhere it could actually move?), and what it has
    /// equipped/carried (any weapons in Inventory?). Game1 calls Build once whenever the turn
    /// menu opens (OpenTurnMenu) and keeps the resulting list until it closes - nothing about a
    /// unit's own turn context changes without the player doing something that reopens the menu
    /// anyway.
    /// </summary>
    public static class TurnMenuBuilder
    {
        public static List<TurnMenuOption> Build(BaseUnit unit, GameMode mode, HexGrid grid, Map map, IEnumerable<BaseUnit> allUnits)
        {
            var options = new List<TurnMenuOption>();

            // Attack: shown only if the unit actually has a move it can afford right now - its
            // race's innate move is always in AvailableMoves (see RaceAttacks), but AP cost
            // varies per move/weapon-switch, so this is a real affordability check, not an
            // assumption that something's always usable.
            bool hasAffordableAttack = unit.AvailableMovesWithSource
                .Any(entry => unit.GetEffectiveAPCost(entry.Move, entry.SourceWeapon) <= unit.CurrentAP);
            if (hasAffordableAttack)
                options.Add(new TurnMenuOption { Id = "Attack", Label = "Attack" });

            // Cards: only a "Summoner" unit (a real HeroClass) actually has a deck/hand to draw
            // from - a summoned creature (HeroClass.None) doesn't, so it never sees this option.
            // Layered on top of that is a real check that the class has any spells to begin with
            // (SpellCatalog.GetSpellsForClass) rather than just assuming every HeroClass does.
            if (unit.Class != HeroClass.None && SpellCatalog.GetSpellsForClass(unit.Class).Count > 0)
                options.Add(new TurnMenuOption { Id = "Cards", Label = "Cards" });

            // Items: shown once the unit actually has something to manage - a weapon in
            // Inventory, a consumable (e.g. a potion from Conjure Potions), or (once it exists)
            // a piece of Equipment. See Game1.OpenItemsMenu for where these get combined into
            // one list.
            if (unit.Inventory.Count > 0 || unit.Consumables.Count > 0)
                options.Add(new TurnMenuOption { Id = "Items", Label = "Items" });

            // End always displays as Guard once it's affordable - same action either way
            // (ending the turn), just a more useful default label when there's AP to spare.
            bool canGuard = unit.CurrentAP >= unit.GuardAPCost;
            options.Add(new TurnMenuOption { Id = "End", Label = canGuard ? "Guard" : "End" });

            // Move: while Knocked Down this slot is "Stand Up" instead (standing up is itself
            // the fix for not being able to move, regardless of Speed/PreventsMovement below).
            // Otherwise it only appears at all if: the unit's Speed is actually enough to move at
            // all (> 1 - a Speed of 0 or 1 means immobile), no active StatusEffect specifically
            // blocks movement (PreventsMovement - e.g. a future Rooted, distinct from a
            // RestrictsActions effect like Stun/Sleep, which bypasses this whole menu already
            // rather than reaching here at all), the unit still has AP to spend, AND its current
            // tile actually has at least one adjacent, passable, unoccupied tile to step onto - a
            // unit fully boxed in has nothing to gain from opening the movement UI either way.
            if (unit.IsKnockedDown)
            {
                options.Add(new TurnMenuOption { Id = "Move", Label = "Stand Up" });
            }
            else if (unit.Speed > 1 && unit.CurrentAP > 0 && !unit.StatusEffects.Any(e => e.PreventsMovement))
            {
                var start = grid.WorldToHex(unit.Position);
                var occupied = allUnits.Where(u => u != unit).Select(u => grid.WorldToHex(u.Position)).ToHashSet();
                bool canMove = grid.GetNeighbors(start.col, start.row)
                    .Any(hex => Pathfinder.IsPassable(grid, map, hex.col, hex.row, occupied));
                if (canMove)
                    options.Add(new TurnMenuOption { Id = "Move", Label = "Move" });
            }

            // View Map: a free-look camera pan, not a real action - always available.
            options.Add(new TurnMenuOption { Id = "ViewMap", Label = "View Map" });

            return options;
        }
    }
}
