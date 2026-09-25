using System;
using System.Collections.Generic;
using System.Linq;
using SagesOfOzvaram.Maps;
using SagesOfOzvaram.Units;

namespace SagesOfOzvaram.Combat
{
    /// <summary>
    /// Outcome of a Move hitting (or missing) one target - enough detail for the caller to show
    /// a combat log line without re-deriving what happened.
    /// </summary>
    public class AttackOutcome
    {
        public BaseUnit Target { get; set; }
        public bool Hit { get; set; }
        public bool Crit { get; set; }
        public bool Backstab { get; set; }
        public int Damage { get; set; }
        public string StatusApplied { get; set; } // e.g. "Bleeding"/"Stunned", null if none landed
        public int KnockbackTiles { get; set; }    // tiles actually travelled (may be less than the move's max if blocked)
        public bool TargetFainted { get; set; }
    }

    /// <summary>
    /// Resolves a chosen Move against its target(s): spends AP/MP, rolls hit/crit, applies
    /// damage/status/knockback/advance, and handles weapon-switch/consumption. Pure game logic -
    /// no rendering or input - callers (e.g. Game1's attack submenu) drive it once a target (or,
    /// for a HitsAllAdjacent move, no target at all) has been confirmed.
    /// </summary>
    public static class AttackResolver
    {
        private static readonly Random Rng = new Random();

        /// <summary>
        /// Resolve `move` (sourced from `sourceWeapon`, null for a racial move) used by
        /// `attacker`. For a single-target move, `primaryTarget` is who it hits; for a
        /// HitsAllAdjacent move (e.g. Sword Spin), primaryTarget is ignored and every living
        /// unit adjacent to the attacker is hit instead; for a HitsCone move (e.g. Frost Blast),
        /// both primaryTarget and aimDirection matter differently - aimDirection is required and
        /// every living unit in that 60-degree cone out to Range is hit (see HexGrid.GetHexesInCone).
        /// Callers should already have confirmed attacker.GetEffectiveAPCost(move, sourceWeapon)
        /// &lt;= attacker.CurrentAP (and MP cost) before calling - this clamps rather than
        /// validates, so it never goes negative.
        /// </summary>
        public static List<AttackOutcome> Resolve(BaseUnit attacker, BaseUnit primaryTarget, Move move,
            Weapon sourceWeapon, HexGrid grid, Map map, IEnumerable<BaseUnit> allUnits, HexDirection? aimDirection = null)
        {
            attacker.CurrentAP = Math.Max(0, attacker.CurrentAP - attacker.GetEffectiveAPCost(move, sourceWeapon));
            attacker.CurrentMP = Math.Max(0, attacker.CurrentMP - move.MPCost);

            // Callers should already have confirmed there's enough ammo before getting here
            // (see Game1.ConfirmAttackMenuSelection) - this just spends it, 1 per use regardless
            // of how many targets a HitsAllAdjacent/HitsCone move ends up hitting.
            if (move.RequiredAmmoType.HasValue)
                attacker.TryConsumeAmmo(move.RequiredAmmoType.Value);

            // Using a weapon other than the equipped one already paid the +1 AP switch cost
            // (GetEffectiveAPCost) - so drawing it now is "free" and becomes the equipped weapon.
            if (sourceWeapon != null)
                attacker.EquippedWeapon = sourceWeapon;

            var unitsSnapshot = allUnits as IList<BaseUnit> ?? allUnits.ToList();

            List<BaseUnit> targets;
            if (move.HitsCone && aimDirection.HasValue)
                targets = GetConeTargets(attacker, move, sourceWeapon, aimDirection.Value, grid, unitsSnapshot);
            else if (move.HitsAllAdjacent)
                targets = GetAdjacentTargets(attacker, grid, unitsSnapshot);
            else
                targets = primaryTarget != null ? new List<BaseUnit> { primaryTarget } : new List<BaseUnit>();

            var outcomes = new List<AttackOutcome>();
            foreach (var target in targets)
                outcomes.Add(ResolveAgainst(attacker, target, move, sourceWeapon, grid, map, unitsSnapshot));

            // Throw Dagger etc. - consume the weapon AFTER it's done its job this use.
            if (move.ConsumesWeapon && sourceWeapon != null)
                attacker.RemoveFromInventory(sourceWeapon);

            return outcomes;
        }

        private static List<BaseUnit> GetAdjacentTargets(BaseUnit attacker, HexGrid grid, IList<BaseUnit> allUnits)
        {
            var attackerHex = grid.WorldToHex(attacker.Position);
            var adjacentHexes = grid.GetNeighbors(attackerHex.col, attackerHex.row).ToHashSet();
            return allUnits.Where(u => u != attacker && u.IsAlive && adjacentHexes.Contains(grid.WorldToHex(u.Position))).ToList();
        }

        private static List<BaseUnit> GetConeTargets(BaseUnit attacker, Move move, Weapon sourceWeapon, HexDirection direction, HexGrid grid, IList<BaseUnit> allUnits)
        {
            var attackerHex = grid.WorldToHex(attacker.Position);
            int range = move.GetEffectiveRange(attacker, sourceWeapon);
            var coneHexes = grid.GetHexesInCone(attackerHex.col, attackerHex.row, direction, range).ToHashSet();
            return allUnits.Where(u => u != attacker && u.IsAlive && coneHexes.Contains(grid.WorldToHex(u.Position))).ToList();
        }

        private static AttackOutcome ResolveAgainst(BaseUnit attacker, BaseUnit target, Move move, Weapon sourceWeapon,
            HexGrid grid, Map map, IList<BaseUnit> allUnits)
        {
            var outcome = new AttackOutcome { Target = target };
            if (target == null || !target.IsAlive)
                return outcome;

            bool isBackstab = Move.IsBackstab(grid, attacker, target);
            outcome.Backstab = isBackstab;

            var attackerHex = grid.WorldToHex(attacker.Position);
            var targetHex = grid.WorldToHex(target.Position);
            int distanceTiles = grid.GetDistance(attackerHex.col, attackerHex.row, targetHex.col, targetHex.row);

            float hitChance = move.GetHitChance(attacker, target, sourceWeapon, distanceTiles);
            if (Rng.NextDouble() > hitChance)
                return outcome; // miss - nothing else happens

            outcome.Hit = true;

            bool crit = move.IsGuaranteedCrit(isBackstab) || Rng.NextDouble() < move.CritChance;
            outcome.Crit = crit;

            int damage = move.GetDamage(attacker, target, sourceWeapon);
            if (crit)
                damage = (int)Math.Round(damage * move.CritMultiplier);
            outcome.Damage = damage;

            target.TakeDamage(damage, move.DamageType);

            // A specialist's bonus flat damage (possibly a different DamageType, e.g. the
            // Cleric's +3 Light on Mace Bash) is mitigated separately from the main hit.
            var (bonusDamage, bonusDamageType) = move.GetSpecialistBonusDamage(attacker, sourceWeapon);
            if (bonusDamage > 0)
            {
                target.TakeDamage(bonusDamage, bonusDamageType);
                outcome.Damage += bonusDamage;
            }

            outcome.TargetFainted = target.IsFainted;

            if (move.InflictsStatusEffect != null && Rng.NextDouble() < move.GetStatusEffectChance(attacker, target))
            {
                ApplyStatusEffect(target, move);
                outcome.StatusApplied = move.InflictsStatusEffect;
            }

            if (Rng.NextDouble() < move.GetKnockdownChance(attacker, target))
            {
                target.ApplyKnockdown(move.KnockdownStandUpAPCost);
                outcome.StatusApplied = outcome.StatusApplied != null ? outcome.StatusApplied + ", Knocked Down" : "Knocked Down";
            }

            outcome.KnockbackTiles = ApplyKnockback(attacker, target, move, damage, grid, map, allUnits);

            if (move.AttackerAdvanceTiles > 0)
                ApplyAdvance(attacker, target, move, grid, map, allUnits);

            return outcome;
        }

        private static void ApplyStatusEffect(BaseUnit target, Move move)
        {
            switch (move.InflictsStatusEffect)
            {
                case "Bleeding":
                    // Take the stronger of any existing bleed and this hit's rank - a weaker
                    // follow-up hit shouldn't downgrade a bleed a harder hit already applied.
                    target.BleedPercentPerTurn = Math.Max(target.BleedPercentPerTurn, BleedEffect.GetPercent(move.BleedRank ?? StatusRank.Weak));
                    break;
                case "Stunned":
                    target.ApplyStun(move.StatusDurationTurns);
                    break;
                case "Slowed":
                    target.ApplySpeedReduction(move.SpeedReductionPercent, move.StatusDurationTurns);
                    break;
            }
        }

        /// <summary>Push the target away from the attacker in a straight line, stopping early if it'd leave the map or land on an obstacle/another unit.</summary>
        private static int ApplyKnockback(BaseUnit attacker, BaseUnit target, Move move, int damageDealt,
            HexGrid grid, Map map, IList<BaseUnit> allUnits)
        {
            int tiles = move.GetKnockbackTiles(damageDealt);
            if (tiles <= 0)
                return 0;

            var attackerHex = grid.WorldToHex(attacker.Position);
            var targetHex = grid.WorldToHex(target.Position);
            var direction = grid.GetDirectionTo(attackerHex.col, attackerHex.row, targetHex.col, targetHex.row);
            var occupied = allUnits.Where(u => u != target).Select(u => grid.WorldToHex(u.Position)).ToHashSet();

            var current = targetHex;
            int actualTiles = 0;
            for (int i = 0; i < tiles; i++)
            {
                var next = grid.GetNeighborCoords(current.col, current.row, direction);
                if (!Pathfinder.IsPassable(grid, map, next.col, next.row, occupied))
                    break;

                current = next;
                actualTiles++;
            }

            if (actualTiles > 0)
                target.Position = grid.HexToWorld(current.col, current.row);
            return actualTiles;
        }

        /// <summary>Move the attacker toward the target (e.g. Sword Pierce's thrust), stopping before the target's own tile or at the first obstacle.</summary>
        private static void ApplyAdvance(BaseUnit attacker, BaseUnit target, Move move, HexGrid grid, Map map, IList<BaseUnit> allUnits)
        {
            var attackerHex = grid.WorldToHex(attacker.Position);
            var targetHex = grid.WorldToHex(target.Position);
            var direction = grid.GetDirectionTo(attackerHex.col, attackerHex.row, targetHex.col, targetHex.row);
            var occupied = allUnits.Where(u => u != attacker).Select(u => grid.WorldToHex(u.Position)).ToHashSet();

            var current = attackerHex;
            for (int i = 0; i < move.AttackerAdvanceTiles; i++)
            {
                var next = grid.GetNeighborCoords(current.col, current.row, direction);
                if (!Pathfinder.IsPassable(grid, map, next.col, next.row, occupied))
                    break;

                current = next;
            }

            attacker.Position = grid.HexToWorld(current.col, current.row);
        }
    }
}
