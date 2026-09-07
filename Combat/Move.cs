using System;
using SagesOfOzvaram.Maps;
using SagesOfOzvaram.Units;

namespace SagesOfOzvaram.Combat
{
    /// <summary>
    /// A single attack a unit can perform - either an innate race-based move (every unit of a
    /// race has the same one, always available for free) or one granted later by an equipped
    /// weapon. Damage, hit chance and knockback scale off the attacker's stats via the Get*
    /// methods below rather than being fixed numbers.
    /// </summary>
    public class Move
    {
        public string Name { get; }
        public string Description { get; }
        public int APCost { get; }
        public int MPCost { get; }
        public int Range { get; }                    // hex distance
        public DamageType DamageType { get; }         // which stat (DEF/RES) mitigates this hit

        public int BaseDamage { get; }
        public float StrengthDivisor { get; }         // damage bonus = Strength / StrengthDivisor; 0 = no STR scaling
        public float IntelligenceDivisor { get; }     // damage bonus = Intelligence / IntelligenceDivisor; 0 = no INT scaling (magical weapons use this instead of STR)

        public bool HitsAllAdjacent { get; }          // true = hits every adjacent enemy instead of one chosen target (e.g. Sword Spin)

        public float BaseAccuracy { get; }            // 0-1 hit chance before the attacker's Accuracy stat is applied

        public int KnockbackBase { get; }             // tiles pushed back on hit, 0 = none
        public int KnockbackDamagePerTile { get; }    // +1 extra tile per this many damage dealt; 0 = no scaling

        public float CritChance { get; }              // 0-1 chance to critically hit
        public float CritMultiplier { get; }          // damage multiplier on a crit
        public bool GuaranteedCritOnBackstab { get; } // if true, a backstab always crits regardless of CritChance

        public string InflictsStatusEffect { get; }   // e.g. "Bleeding", null if none
        public float StatusEffectChance { get; }      // 0-1 chance the status effect is applied on a hit (default: always)
        public StatusRank? BleedRank { get; }          // severity if InflictsStatusEffect is "Bleeding"; see BleedEffect

        public float BonusDamageVsGuardingMultiplier { get; } // damage multiplier when the target is Guarding; 1 = no bonus
        public int AttackerAdvanceTiles { get; }              // tiles the ATTACKER moves toward the target on use (e.g. a thrust); 0 = none.
                                                               // Blocked if an enemy already occupies the tile - not enforced yet, no targeting/movement engine exists.
        public bool ConsumesWeapon { get; }                   // true for thrown/single-use attacks (e.g. Throw Knife) that remove the weapon from Inventory on use

        public Move(string name, string description, int apCost, int mpCost, int range,
                    float baseAccuracy, int baseDamage, float strengthDivisor = 0f, float intelligenceDivisor = 0f,
                    int knockbackBase = 0, int knockbackDamagePerTile = 0,
                    string inflictsStatusEffect = null, StatusRank? bleedRank = null,
                    DamageType damageType = DamageType.Physical,
                    float critChance = 0f, float critMultiplier = 2f, bool guaranteedCritOnBackstab = false,
                    float statusEffectChance = 1f,
                    float bonusDamageVsGuardingMultiplier = 1f, int attackerAdvanceTiles = 0,
                    bool consumesWeapon = false, bool hitsAllAdjacent = false)
        {
            Name = name;
            Description = description;
            APCost = apCost;
            MPCost = mpCost;
            Range = range;
            BaseAccuracy = baseAccuracy;
            BaseDamage = baseDamage;
            StrengthDivisor = strengthDivisor;
            IntelligenceDivisor = intelligenceDivisor;
            KnockbackBase = knockbackBase;
            KnockbackDamagePerTile = knockbackDamagePerTile;
            InflictsStatusEffect = inflictsStatusEffect;
            BleedRank = bleedRank;
            DamageType = damageType;
            CritChance = critChance;
            CritMultiplier = critMultiplier;
            GuaranteedCritOnBackstab = guaranteedCritOnBackstab;
            StatusEffectChance = statusEffectChance;
            BonusDamageVsGuardingMultiplier = bonusDamageVsGuardingMultiplier;
            AttackerAdvanceTiles = attackerAdvanceTiles;
            ConsumesWeapon = consumesWeapon;
            HitsAllAdjacent = hitsAllAdjacent;
        }

        /// <summary>
        /// Damage this move deals when used by the given attacker: BaseDamage + Strength/StrengthDivisor
        /// + Intelligence/IntelligenceDivisor, multiplied if the target is Guarding and this move has a
        /// bonus for that, plus a weapon specialist's bonus INT damage if sourceWeapon grants one (e.g.
        /// the Cleric's Mace bonus). Does NOT apply crits or the target's DEF/RES - see GetHitChance and
        /// BaseUnit.TakeDamage for those.
        /// </summary>
        public int GetDamage(BaseUnit attacker, BaseUnit target = null, Weapon sourceWeapon = null)
        {
            float bonus = 0f;
            if (StrengthDivisor > 0f) bonus += attacker.Strength / StrengthDivisor;
            if (IntelligenceDivisor > 0f) bonus += attacker.Intelligence / IntelligenceDivisor;

            float total = BaseDamage + bonus;

            if (target != null && target.IsGuarding && BonusDamageVsGuardingMultiplier > 1f)
                total *= BonusDamageVsGuardingMultiplier;

            if (IsSpecialist(attacker, sourceWeapon) && sourceWeapon.SpecialistIntDamageBonusDivisor > 0f)
                total += attacker.Intelligence / sourceWeapon.SpecialistIntDamageBonusDivisor;

            return (int)Math.Round(total);
        }

        /// <summary>
        /// Hit chance (0-1) for the given attacker: BaseAccuracy + Accuracy stat * 0.1%/point, plus a
        /// weapon specialist's flat accuracy bonus if sourceWeapon grants one (e.g. the Hunter's Bow
        /// bonus or the Cleric's Mace bonus).
        /// </summary>
        public float GetHitChance(BaseUnit attacker, Weapon sourceWeapon = null)
        {
            float chance = BaseAccuracy + attacker.Accuracy * 0.001f;

            if (IsSpecialist(attacker, sourceWeapon))
                chance += sourceWeapon.SpecialistAccuracyBonus;

            // No upper clamp - accuracy is allowed to exceed 100%, giving buffer room before
            // accuracy-lowering effects (e.g. Blind) actually bring a hit below guaranteed.
            return Math.Max(chance, 0f);
        }

        private static bool IsSpecialist(BaseUnit attacker, Weapon sourceWeapon)
        {
            return sourceWeapon != null && sourceWeapon.Type.HasValue
                && attacker.WeaponSpecializations.Contains(sourceWeapon.Type.Value);
        }

        /// <summary>Tiles a hit target is knocked back, given the damage actually dealt.</summary>
        public int GetKnockbackTiles(int damageDealt)
        {
            if (KnockbackBase <= 0 && KnockbackDamagePerTile <= 0)
                return 0;

            int bonus = KnockbackDamagePerTile > 0 ? damageDealt / KnockbackDamagePerTile : 0;
            return KnockbackBase + bonus;
        }

        /// <summary>Whether this hit is guaranteed to crit, given whether it's a backstab (see IsBackstab).</summary>
        public bool IsGuaranteedCrit(bool isBackstab) => isBackstab && GuaranteedCritOnBackstab;

        /// <summary>
        /// True if the attacker is standing directly behind the target - i.e. on the tile
        /// opposite the direction the target is Facing. Only meaningful at range 1 (adjacent
        /// tiles); returns false for anything else.
        /// </summary>
        public static bool IsBackstab(HexGrid grid, BaseUnit attacker, BaseUnit target)
        {
            var attackerHex = grid.WorldToHex(attacker.Position);
            var targetHex = grid.WorldToHex(target.Position);

            HexDirection? directionToAttacker = grid.GetDirectionToNeighbor(targetHex.col, targetHex.row, attackerHex.col, attackerHex.row);
            return directionToAttacker.HasValue && directionToAttacker.Value == target.Facing.Opposite();
        }
    }
}
