using System;
using System.Collections.Generic;
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
        public bool HitsCone { get; }                 // true = aimed at a direction (not a single target) and hits every unit in that 60-degree cone out to Range (e.g. Frost Blast) - see HexGrid.GetHexesInCone

        public float BaseAccuracy { get; }            // 0-1 hit chance before the attacker's Accuracy stat is applied
        public float AccuracyFalloffPerTile { get; }  // accuracy lost per tile of distance beyond range 1 (e.g. Throw Dagger); 0 = no falloff, accuracy is flat regardless of how far within Range the target is

        /// <summary>
        /// Distance-banded accuracy override, for a non-monotonic curve a flat falloff can't
        /// express - e.g. a Longbow that's unreliable up close, best at mid-range, and falls off
        /// (but less severely) at long range. Each entry is (minDistance, maxDistance, accuracy);
        /// if the target's actual distance falls in a band, that band's accuracy REPLACES
        /// BaseAccuracy entirely (the attacker's Accuracy stat and any specialist bonus still
        /// apply on top, same as normal). Null/empty = no bands, just use BaseAccuracy.
        /// </summary>
        public List<(int MinDistance, int MaxDistance, float Accuracy)> AccuracyBands { get; }

        public int KnockbackBase { get; }             // tiles pushed back on hit, 0 = none
        public int KnockbackDamagePerTile { get; }    // +1 extra tile per this many damage dealt; 0 = no scaling

        public float CritChance { get; }              // 0-1 chance to critically hit
        public float CritMultiplier { get; }          // damage multiplier on a crit
        public bool GuaranteedCritOnBackstab { get; } // if true, a backstab always crits regardless of CritChance

        public string InflictsStatusEffect { get; }   // e.g. "Bleeding", null if none
        public float StatusEffectChance { get; }      // 0-1 chance the status effect is applied on a hit (default: always)
        public StatusRank? BleedRank { get; }          // severity if InflictsStatusEffect is "Bleeding"; see BleedEffect

        public AmmoType? RequiredAmmoType { get; }     // e.g. Arrow for Arrow Shot - null means no ammo needed (melee/magic/Bow Whack). Consumed 1 per use - see AttackResolver.Resolve and BaseUnit.TryConsumeAmmo

        public float BonusDamageVsGuardingMultiplier { get; } // damage multiplier when the target is Guarding; 1 = no bonus
        public int AttackerAdvanceTiles { get; }              // tiles the ATTACKER moves toward the target on use (e.g. a thrust); 0 = none.
                                                               // Blocked if an enemy already occupies the tile - not enforced yet, no targeting/movement engine exists.
        public bool ConsumesWeapon { get; }                   // true for thrown/single-use attacks (e.g. Throw Dagger) that remove the weapon from Inventory on use

        public bool CanInflictKnockdown { get; }               // true if this move can inflict Knocked Down (see GetKnockdownChance)
        public float KnockdownChanceIfStrongerSTR { get; }     // chance (0-1) when attacker.EffectiveStrength > target.EffectiveStrength + target.TotalDefense (Defense stands in for Armor - no separate Armor stat exists yet)
        public float KnockdownChanceOtherwise { get; }         // chance (0-1) otherwise
        public int KnockdownStandUpAPCost { get; }              // AP the target must spend to stand back up, if this move knocks it down

        // Non-attack effect fields, added for the generic support/utility spells (GDD §4.1
        // Generic Spells) - a Move's damage-formula fields above stay 0/unset for these, since
        // they aren't attacks. No spell-casting execution exists yet, so these are read by
        // nothing today; they exist so each spell's headline number (heal amount, AP granted,
        // status duration) is real data on the card rather than only prose. The more elaborate
        // per-spell mechanics (Meditate's stacking/interrupt, Forced Sleep's wake condition,
        // etc.) are still too state-machine-y to reduce to a flat field, so those stay prose-only
        // in each SpellCard's Description until a real status-effect system exists to run them.
        public int HealFlat { get; }               // flat HP restored on use, 0 = none
        public float HealPercentMaxHP { get; }      // % of max HP restored (e.g. per-turn regen while a state is active), 0 = none
        public int GrantedAP { get; }               // bonus AP granted to the caster, 0 = none
        public int StatusDurationTurns { get; }     // how many turns InflictsStatusEffect lasts, 0 = instant/not duration-based
        public bool TargetsAllies { get; }          // true = affects allies (self and/or adjacent allies) rather than an enemy target
        public float SpeedReductionPercent { get; } // % Speed reduction when InflictsStatusEffect is "Slowed" (e.g. Frost Blast), 0-1; lasts StatusDurationTurns - see BaseUnit.ApplySpeedReduction

        public Move(string name, string description, int apCost, int mpCost, int range,
                    float baseAccuracy, int baseDamage, DamageType damageType = DamageType.Magical,
                    float strengthDivisor = 0f, float intelligenceDivisor = 0f,
                    int knockbackBase = 0, int knockbackDamagePerTile = 0,
                    string inflictsStatusEffect = null, StatusRank? bleedRank = null,
                    float critChance = 0f, float critMultiplier = 2f, bool guaranteedCritOnBackstab = false,
                    float statusEffectChance = 1f,
                    float bonusDamageVsGuardingMultiplier = 1f, int attackerAdvanceTiles = 0,
                    bool consumesWeapon = false, bool hitsAllAdjacent = false, bool hitsCone = false,
                    int healFlat = 0, float healPercentMaxHP = 0f, int grantedAP = 0,
                    int statusDurationTurns = 0, bool targetsAllies = false, float speedReductionPercent = 0f,
                    bool canInflictKnockdown = false, float knockdownChanceIfStrongerStr = 0f, float knockdownChanceOtherwise = 0f,
                    int knockdownStandUpApCost = 1, float accuracyFalloffPerTile = 0f,
                    List<(int MinDistance, int MaxDistance, float Accuracy)> accuracyBands = null,
                    AmmoType? requiredAmmoType = null)
        {
            Name = name;
            Description = description;
            APCost = apCost;
            MPCost = mpCost;
            Range = range;
            BaseAccuracy = baseAccuracy;
            AccuracyFalloffPerTile = accuracyFalloffPerTile;
            AccuracyBands = accuracyBands;
            BaseDamage = baseDamage;
            StrengthDivisor = strengthDivisor;
            IntelligenceDivisor = intelligenceDivisor;
            KnockbackBase = knockbackBase;
            KnockbackDamagePerTile = knockbackDamagePerTile;
            InflictsStatusEffect = inflictsStatusEffect;
            BleedRank = bleedRank;
            RequiredAmmoType = requiredAmmoType;
            DamageType = damageType;
            CritChance = critChance;
            CritMultiplier = critMultiplier;
            GuaranteedCritOnBackstab = guaranteedCritOnBackstab;
            StatusEffectChance = statusEffectChance;
            BonusDamageVsGuardingMultiplier = bonusDamageVsGuardingMultiplier;
            AttackerAdvanceTiles = attackerAdvanceTiles;
            ConsumesWeapon = consumesWeapon;
            HitsAllAdjacent = hitsAllAdjacent;
            HitsCone = hitsCone;
            HealFlat = healFlat;
            HealPercentMaxHP = healPercentMaxHP;
            GrantedAP = grantedAP;
            StatusDurationTurns = statusDurationTurns;
            TargetsAllies = targetsAllies;
            SpeedReductionPercent = speedReductionPercent;
            CanInflictKnockdown = canInflictKnockdown;
            KnockdownChanceIfStrongerSTR = knockdownChanceIfStrongerStr;
            KnockdownChanceOtherwise = knockdownChanceOtherwise;
            KnockdownStandUpAPCost = knockdownStandUpApCost;
        }

        /// <summary>
        /// Damage this move deals when used by the given attacker: a flat base + Strength/StrengthDivisor
        /// + Intelligence/IntelligenceDivisor, multiplied if the target is Guarding and this move has a
        /// bonus for that, plus a weapon specialist's bonus INT damage if sourceWeapon grants one (e.g.
        /// the Cleric's Mace bonus). The flat base is sourceWeapon.AttackPower if that weapon has been
        /// migrated to the per-weapon power system (see Weapon.AttackPower) - different weapons of the
        /// same kind (a Rusty vs. a Steel Dagger) hit differently despite sharing the same move - and
        /// falls back to this move's own BaseDamage otherwise (racial moves always use BaseDamage, since
        /// they have no weapon). Does NOT apply crits or the target's DEF/RES - see GetHitChance and
        /// BaseUnit.TakeDamage for those.
        /// </summary>
        public int GetDamage(BaseUnit attacker, BaseUnit target = null, Weapon sourceWeapon = null)
        {
            float bonus = 0f;
            if (StrengthDivisor > 0f) bonus += attacker.EffectiveStrength / StrengthDivisor;
            if (IntelligenceDivisor > 0f) bonus += attacker.Intelligence / IntelligenceDivisor;

            float flatBase = sourceWeapon?.AttackPower ?? BaseDamage;
            float total = flatBase + bonus;

            if (target != null && target.IsGuarding && BonusDamageVsGuardingMultiplier > 1f)
                total *= BonusDamageVsGuardingMultiplier;

            if (IsSpecialist(attacker, sourceWeapon) && sourceWeapon.SpecialistIntDamageBonusDivisor > 0f)
                total += attacker.Intelligence / sourceWeapon.SpecialistIntDamageBonusDivisor;

            return (int)Math.Round(total);
        }

        /// <summary>
        /// Hit chance (0-1) for the given attacker against target: BaseAccuracy (or, if
        /// AccuracyBands is set and covers the actual distance, that band's accuracy instead -
        /// e.g. a Longbow's close/mid/far curve) + Accuracy stat * 0.1%/point, plus a weapon
        /// specialist's flat accuracy bonus if sourceWeapon grants one (e.g. the Hunter's Bow
        /// bonus or the Cleric's Mace bonus), minus target's Evasion * 0.1%/point (same rate as
        /// Accuracy, just working the other way - null target = no Evasion applied, e.g. a
        /// preview with nothing selected yet). distanceTiles is the actual hex distance to the
        /// target this use (0/unknown = no bands/falloff applied) - only matters for a move with
        /// AccuracyBands or AccuracyFalloffPerTile set.
        /// </summary>
        public float GetHitChance(BaseUnit attacker, BaseUnit target = null, Weapon sourceWeapon = null, int distanceTiles = 0)
        {
            float chance = BaseAccuracy;

            if (AccuracyBands != null)
            {
                foreach (var band in AccuracyBands)
                {
                    if (distanceTiles >= band.MinDistance && distanceTiles <= band.MaxDistance)
                    {
                        chance = band.Accuracy;
                        break;
                    }
                }
            }

            chance += attacker.Accuracy * 0.001f;

            if (target != null)
                chance -= target.EffectiveEvasion * 0.001f;

            if (IsSpecialist(attacker, sourceWeapon))
                chance += sourceWeapon.SpecialistAccuracyBonus;

            // Falls off the further the target actually is, beyond the first (adjacent) tile.
            if (AccuracyFalloffPerTile > 0f && distanceTiles > 1)
                chance -= AccuracyFalloffPerTile * (distanceTiles - 1);

            // Knocked Down: -75% accuracy on anything the attacker attempts while prone.
            if (attacker.IsKnockedDown)
                chance *= 0.25f;

            // No upper clamp - accuracy is allowed to exceed 100%, giving buffer room before
            // accuracy-lowering effects (e.g. Blind) actually bring a hit below guaranteed.
            return Math.Max(chance, 0f);
        }

        /// <summary>
        /// This move's actual range for the given attacker: Range, plus sourceWeapon's
        /// SpecialistRangeBonus if the attacker specializes in it (e.g. a Cleric's mace head
        /// detaching on a chain to reach an extra tile). Unchanged for a racial move (sourceWeapon null).
        /// </summary>
        public int GetEffectiveRange(BaseUnit attacker, Weapon sourceWeapon)
        {
            return IsSpecialist(attacker, sourceWeapon) ? Range + sourceWeapon.SpecialistRangeBonus : Range;
        }

        /// <summary>
        /// Extra flat damage (of a possibly different DamageType than this move's own) a
        /// specialist deals on top of a normal hit - e.g. the Cleric's +3 Light damage on Mace
        /// Bash. (0, Sharp) if not a specialist or sourceWeapon doesn't grant one - check Amount
        /// before using Type.
        /// </summary>
        public (int Amount, DamageType Type) GetSpecialistBonusDamage(BaseUnit attacker, Weapon sourceWeapon)
        {
            if (!IsSpecialist(attacker, sourceWeapon) || sourceWeapon.SpecialistFlatDamageBonus <= 0 || !sourceWeapon.SpecialistFlatDamageBonusType.HasValue)
                return (0, DamageType.Sharp);

            return (sourceWeapon.SpecialistFlatDamageBonus, sourceWeapon.SpecialistFlatDamageBonusType.Value);
        }

        /// <summary>
        /// Chance (0-1) this hit's InflictsStatusEffect actually lands: StatusEffectChance
        /// normally, but Bleeding specifically is blocked outright (0%) if the target's Defense
        /// beats the attacker's Strength - tough enough armor/hide shrugs off a hit that would
        /// otherwise draw blood. Doesn't affect other statuses (e.g. Stunned).
        /// </summary>
        public float GetStatusEffectChance(BaseUnit attacker, BaseUnit target)
        {
            if (InflictsStatusEffect == "Bleeding" && target.TotalDefense > attacker.EffectiveStrength)
                return 0f;

            return StatusEffectChance;
        }

        /// <summary>
        /// Chance (0-1) this hit knocks the target down: KnockdownChanceIfStrongerSTR if the
        /// attacker's Strength beats the target's Strength + Defense (Defense standing in for
        /// Armor), KnockdownChanceOtherwise if not. 0 if this move can't inflict Knockdown at all.
        /// </summary>
        public float GetKnockdownChance(BaseUnit attacker, BaseUnit target)
        {
            if (!CanInflictKnockdown)
                return 0f;

            return attacker.EffectiveStrength > target.EffectiveStrength + target.TotalDefense
                ? KnockdownChanceIfStrongerSTR
                : KnockdownChanceOtherwise;
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
