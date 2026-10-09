using System.Collections.Generic;

namespace SagesOfOzvaram.Combat
{
    /// <summary>
    /// Shared weapon definitions. Each property returns a fresh instance so units don't share
    /// mutable state through a common weapon object. Move numbers are first-pass placeholders
    /// pending a real balance pass, same as RaceAttacks. Physical weapons scale with Strength;
    /// magical weapons (the Sorcerer's Staff/Wand) scale with Intelligence instead. Every attack
    /// costs 1 AP, per design; only the actual MAGIC attacks cost MP - that's Frost Blast and
    /// Arcane Missile specifically (the Staff/Wand SpecialistAttacks, Sorcerer-only), not their
    /// base Bonk/Whack, which anyone can swing for free. Nothing else in the game costs MP to use.
    /// Weight (see BaseUnit.InventoryWeightCapacity) is 2 for small one-handed weapons (Dagger,
    /// Pistol) and 3 for everything larger (Sword, Mace, Staff, Bow, Shield).
    ///
    /// Flat damage is moving from each Move's own BaseDamage to a per-Weapon AttackPower (see
    /// Weapon.AttackPower) - the point being multiple weapons of the same kind (e.g. several
    /// daggers, or several swords) can share identical moves but hit for different amounts.
    /// Dagger, Iron Sword, Iron Shield and Light Mace have been migrated (their moves'
    /// baseDamage is 0, unused); Staff, Wand, Bow and Pistol still carry their damage on the
    /// move itself until each one gets revisited. Dagger vs. Sword is intentionally asymmetric - Dagger's AttackPower (7)
    /// and every one of its StrengthDivisors are lower/weaker than Sword's (AttackPower 9,
    /// divisors 4-5) - daggers should always hit for less and benefit less from Strength than
    /// swords do, as a rule for every future weapon added to either classification.
    /// </summary>
    public static class WeaponCatalog
    {
        /// <summary>
        /// Sword Slash/Pierce are available to anyone carrying an Iron Sword; Sword Spin is a
        /// SpecialistAttack - only a WeaponType.OneHandedSword specialist (currently just the Warrior)
        /// can spin and hit everything adjacent. Migrated to Weapon.AttackPower (9 - clearly above
        /// Dagger's 7) so a future better/worse sword can share these same moves. Slash keeps the
        /// best StrengthDivisor (the "optimized" single-target technique); Pierce and Spin are both
        /// a notch behind on raw scaling (their value is in the guard-punish/advance and AOE
        /// utility respectively) - all three are still clearly stronger STR scaling than any Dagger
        /// move, per the "dagger < sword in both base damage and STR scaling" rule.
        /// </summary>
        public static Weapon IronSword
        {
            get
            {
                var sword = new Weapon("One-Handed Iron Sword",
                    "A sturdy one-handed blade - reliable slashes and thrusts, plus a wide spinning strike for a specialist.",
                    new Move("Sword Slash", "Slashes with the blade - a small chance to miss, an even smaller chance to crit for 3x damage, and a small chance to draw blood.",
                        apCost: 1, mpCost: 0, range: 1, baseAccuracy: 0.88f, baseDamage: 0, damageType: DamageType.Sharp, strengthDivisor: 4f,
                        critChance: 0.06f, critMultiplier: 3f,
                        inflictsStatusEffect: "Bleeding", bleedRank: StatusRank.Weak, statusEffectChance: 0.15f),
                    new Move("Sword Pierce", "A forward thrust that advances the attacker a tile - hits a little less hard than Sword Slash but punishes a Guarding target.",
                        apCost: 1, mpCost: 0, range: 2, baseAccuracy: 0.88f, baseDamage: 0, damageType: DamageType.Sharp, strengthDivisor: 5f,
                        inflictsStatusEffect: "Bleeding", bleedRank: StatusRank.Weak, statusEffectChance: 0.15f,
                        bonusDamageVsGuardingMultiplier: 1.5f, attackerAdvanceTiles: 1))
                { Type = WeaponType.OneHandedSword, Weight = 3, AttackPower = 9 };

                sword.SpecialistAttacks.Add(new Move("Sword Spin",
                    "Spins the sword in a full circle, striking everything adjacent at once - hits a little less hard per target than a focused Slash, but that's the price of hitting everyone.",
                    apCost: 1, mpCost: 0, range: 1, baseAccuracy: 0.85f, baseDamage: 0, damageType: DamageType.Sharp, strengthDivisor: 5f,
                    hitsAllAdjacent: true));

                return sword;
            }
        }

        /// <summary>
        /// No base Attack - anyone carrying an Iron Shield just gets its passive DEF/RES bonus
        /// (15%, plus another 25% on top while Guarding, for 40% total). Shield Bash is a
        /// SpecialistAttack - only a WeaponType.Shield specialist (currently just the Warrior)
        /// can bash with it; everyone else carrying one is purely a wall, no attack.
        /// </summary>
        public static Weapon IronShield
        {
            get
            {
                var shield = new Weapon("Iron Shield", "A heavy plated shield - a solid passive defense boost, even stronger while Guarding, with a staggering Bash for a specialist.")
                { Type = WeaponType.Shield, Weight = 3, ShieldPassiveDefResBonusPercent = 0.15f, ShieldGuardBonusPercent = 0.25f, AttackPower = 4 };

                shield.SpecialistAttacks.Add(new Move("Shield Bash", "A shove with the shield's edge - decent accuracy, low damage, but it reliably staggers the target for a full turn.",
                    apCost: 1, mpCost: 0, range: 1, baseAccuracy: 0.9f, baseDamage: 0, damageType: DamageType.Blunt, strengthDivisor: 6f,
                    inflictsStatusEffect: "Stunned", statusEffectChance: 1.0f, statusDurationTurns: 1));

                return shield;
            }
        }

        // Sorcerer's shield - passive Guard-only bonus, no attack of its own (nobody specializes in it).
        public static Weapon GlassShield => new Weapon("Glass Shield", "A light, half-translucent ward - a modest passive defense boost and no attack of its own, favored by casters who'd rather not carry the weight of steel.")
        { Type = WeaponType.Shield, ShieldPassiveDefResBonusPercent = 0.10f, ShieldGuardBonusPercent = 0.15f, Weight = 3 };

        /// <summary>
        /// Dagger's base Stab is available to anyone carrying one; Throw Dagger is a
        /// SpecialistAttack - only a unit with WeaponType.Dagger in its WeaponSpecializations
        /// (currently just the Hunter) can use it, even though other units also carry a Dagger.
        /// </summary>
        public static Weapon Dagger
        {
            get
            {
                var dagger = new Weapon("Dagger",
                    "A short, light blade - quick and precise up close, and a specialist can throw it for ranged damage at the cost of the dagger itself.",
                    new Move("Stab", "A quick, precise thrust - short reach, but reliably finds its mark, and a hit from behind is a guaranteed critical.",
                        apCost: 1, mpCost: 0, range: 1, baseAccuracy: 1.0f, baseDamage: 0, damageType: DamageType.Sharp, strengthDivisor: 6f,
                        critChance: 0.05f, critMultiplier: 3f, guaranteedCritOnBackstab: true,
                        inflictsStatusEffect: "Bleeding", bleedRank: StatusRank.Weak, statusEffectChance: 0.35f))
                { Type = WeaponType.Dagger, Weight = 2, AttackPower = 7 };

                // Same dagger, same flat AttackPower as Stab - but a thrown strike doesn't
                // harness the wielder's Strength as efficiently as a direct thrust (weaker
                // StrengthDivisor), which is what keeps this "slightly less" than Stab overall.
                dagger.SpecialistAttacks.Add(new Move("Throw Dagger",
                    "Hurls the dagger at a target, consuming it - only a dagger specialist can make the throw count. Decent accuracy up close, but harder to land the further out it's thrown.",
                    apCost: 1, mpCost: 0, range: 8, baseAccuracy: 0.8f, baseDamage: 0, damageType: DamageType.Sharp, strengthDivisor: 10f,
                    accuracyFalloffPerTile: 0.08f, consumesWeapon: true, animationType: AttackAnimationType.Shooting));

                return dagger;
            }
        }

        /// <summary>
        /// Bonk is a base Attack anyone carrying a Staff can use (physical, STR-scaled, no MP
        /// cost - it's just whacking someone with a stick). Frost Blast is a SpecialistAttack -
        /// only a WeaponType.Staff specialist (currently just the Sorcerer) can channel it, and
        /// it's the one that actually costs MP; everyone else is limited to Bonk. Same
        /// base+specialist pattern as every other weapon (e.g. Sword's Slash/Pierce + Spin) -
        /// the Wand (see below) mirrors it too, with Whack + Arcane Missile.
        /// </summary>
        public static Weapon RhinewoodStaff
        {
            get
            {
                var staff = new Weapon("Rhinewood Staff",
                    "A stout wooden staff - a clumsy free whack for anyone, or a chilling cone of frost for a Staff specialist.",
                    new Move("Bonk", "A clumsy overhead whack with the staff - low accuracy, low damage, but can rattle the target. No different from swinging any other blunt object - costs no mana.",
                        apCost: 1, mpCost: 0, range: 1, baseAccuracy: 0.55f, baseDamage: 2, damageType: DamageType.Blunt, strengthDivisor: 4f,
                        inflictsStatusEffect: "Stunned", statusEffectChance: 0.20f, statusDurationTurns: 1))
                { Type = WeaponType.Staff, Weight = 3 };

                staff.SpecialistAttacks.Add(new Move("Frost Blast",
                    "A cone of biting frost, low on raw damage but enough to chill anything it touches, slowing them down - only a Staff specialist can channel it.",
                    apCost: 1, mpCost: 10, range: 4, baseAccuracy: 0.75f, baseDamage: 5, intelligenceDivisor: 5f,
                    damageType: DamageType.Magical, hitsCone: true, animationType: AttackAnimationType.Shooting,
                    inflictsStatusEffect: "Slowed", statusDurationTurns: 2, speedReductionPercent: 0.4f));

                return staff;
            }
        }

        /// <summary>
        /// Small, light, one-handed - Whack is a base Attack anyone carrying a Wand can use
        /// (weak, but reliable, and free - no mana in a tap with a stick). Arcane Missile is a
        /// SpecialistAttack - only a WeaponType.Wand specialist (currently just the Sorcerer)
        /// can channel it, and it's the one that actually costs MP; everyone else is limited to
        /// Whack. This is the Sorcerer's actual spellcasting implement, not the Staff.
        /// </summary>
        public static Weapon Wand
        {
            get
            {
                var wand = new Weapon("Willow Wand",
                    "A slender spellcasting wand - a light free tap for anyone, or a bolt of raw arcane energy for a Wand specialist. The Sorcerer's actual spellcasting implement.",
                    new Move("Whack", "A quick rap with the wand's tip - barely stings, but reliably lands. No mana in a tap with a stick.",
                        apCost: 1, mpCost: 0, range: 1, baseAccuracy: 0.95f, baseDamage: 1, damageType: DamageType.Blunt, strengthDivisor: 6f))
                { Type = WeaponType.Wand, Weight = 2 };

                wand.SpecialistAttacks.Add(new Move("Arcane Missile",
                    "A bolt of raw arcane energy that occasionally surges for extra damage - only a Wand specialist can channel it.",
                    apCost: 1, mpCost: 10, range: 3, baseAccuracy: 0.8f, baseDamage: 14, intelligenceDivisor: 3f,
                    damageType: DamageType.Magical, critChance: 0.08f, critMultiplier: 3f, animationType: AttackAnimationType.Shooting));

                return wand;
            }
        }

        /// <summary>
        /// A generalist weapon - WeaponType.Crossbow (distinct from Bow), nobody specializes in
        /// it, no accuracy bonus for anyone. Low accuracy jammed up close, then flat/consistent
        /// from range 2 on (unlike the Longbow's three-band curve). Weaker accuracy than the
        /// Pistol, but more damage and a higher crit chance - the trade-off. Requires Bolts (see
        /// AmmoType.Bolt / BaseUnit.TryConsumeAmmo) - 1 consumed per shot.
        /// </summary>
        public static Weapon Crossbow => new Weapon("Crossbow",
            "A mechanically-loosed ranged weapon - weaker accuracy than a Pistol, but hits harder with a better chance to crit. Requires Bolts.",
            new Move("Bolt Shot", "A mechanically-loosed bolt that can punch clean through, drawing blood - clumsy at point-blank range, but reliably lands from there on out.",
                apCost: 1, mpCost: 0, range: 4, baseAccuracy: 0.55f, baseDamage: 13, damageType: DamageType.Sharp, strengthDivisor: 5f,
                critChance: 0.20f, critMultiplier: 3f,
                inflictsStatusEffect: "Bleeding", bleedRank: StatusRank.Weak, statusEffectChance: 0.10f,
                accuracyBands: new List<(int, int, float)> { (1, 1, 0.30f), (2, 4, 0.55f) },
                requiredAmmoType: AmmoType.Bolt, animationType: AttackAnimationType.Shooting))
        { Type = WeaponType.Crossbow, Weight = 3 };

        /// <summary>
        /// Arrow Shot isn't a simple falloff - it's worst jammed up close (1-2 tiles, too near
        /// to draw properly), best at its intended mid-range (3-4), and drops off again at long
        /// range (5-6) but less severely than up close (see Move.AccuracyBands). Bow Whack is a
        /// second base Attack anyone carrying a Longbow can use - a melee fallback with the bow
        /// itself, very little damage. The Hunter (Bow specialist) gets +15% accuracy on Arrow
        /// Shot on top of whichever band applies.
        /// </summary>
        public static Weapon Longbow => new Weapon("Longbow",
            "A tall hunting bow - best at mid-range, with a weak melee fallback that costs no arrow. A Hunter (Bow specialist) gets a real accuracy edge with it. Requires Arrows.",
            new Move("Arrow Shot", "A well-aimed arrow, sharp enough to draw blood - hard to loose properly at point-blank range, most accurate at its intended mid-range, and only somewhat less reliable at long range.",
                apCost: 1, mpCost: 0, range: 6, baseAccuracy: 0.65f, baseDamage: 9, damageType: DamageType.Sharp, strengthDivisor: 5f,
                inflictsStatusEffect: "Bleeding", bleedRank: StatusRank.Weak, statusEffectChance: 0.10f,
                accuracyBands: new List<(int, int, float)> { (1, 2, 0.20f), (3, 4, 0.65f), (5, 6, 0.45f) },
                requiredAmmoType: AmmoType.Arrow, animationType: AttackAnimationType.Shooting),
            new Move("Bow Whack", "A quick smack with the bow's limb at point-blank range - barely hurts, but doesn't require an arrow.",
                apCost: 1, mpCost: 0, range: 1, baseAccuracy: 0.85f, baseDamage: 1, damageType: DamageType.Blunt, strengthDivisor: 6f))
        { Type = WeaponType.Bow, SpecialistAccuracyBonus = 0.15f, Weight = 3 };

        // No specialization - everyone has basic gun know-how, so damage doesn't scale with
        // STR or INT at all; it is what it is regardless of who's holding it. Classified Sharp -
        // a bullet punches/pierces rather than bludgeoning; flag if you'd rather it get its own
        // Ballistic category down the line. More accurate than the Crossbow but hits softer and
        // crits less - the trade-off. Low accuracy jammed up close, flat/consistent from range 2
        // on. Requires Bullets (AmmoType.Bullet) - 1 consumed per shot.
        public static Weapon FlintlockPistol => new Weapon("Flintlock Pistol",
            "A single-shot black-powder pistol - no Strength or Intelligence scaling, it hits the same regardless of who's holding it. Requires Bullets.",
            new Move("Pistol Shot", "A loud shot - unreliable at point-blank range, but a clean, practiced draw from there on out; a solid hit can stagger, and it has a real chance to critically wound.",
                apCost: 1, mpCost: 0, range: 3, baseAccuracy: 0.75f, baseDamage: 12, damageType: DamageType.Sharp,
                critChance: 0.08f, critMultiplier: 3f, knockbackBase: 1,
                inflictsStatusEffect: "Bleeding", bleedRank: StatusRank.Weak, statusEffectChance: 0.10f,
                accuracyBands: new List<(int, int, float)> { (1, 1, 0.35f), (2, 3, 0.75f) },
                requiredAmmoType: AmmoType.Bullet, animationType: AttackAnimationType.Shooting))
        { Type = WeaponType.Pistol, Weight = 2 };

        /// <summary>
        /// Average STR-scaled damage, mediocre accuracy, decent crit chance - Blunt. A Mace
        /// specialist (currently just the Cleric) gets +20% accuracy, +1 Range (the head
        /// detaches on a chain), and an extra +3 Light damage on every hit (see
        /// Weapon.SpecialistFlatDamageBonus) - mitigated separately by RES, not DEF, since it's
        /// a different damage type than the mace's own Blunt hit.
        /// </summary>
        public static Weapon LightMace => new Weapon("Light Mace",
            "A blunt, mediocre-in-every-way weapon on its own - a Mace specialist transforms it into something far more precise and dangerous.",
            new Move("Mace Bash", "A heavy strike, middling in every way on its own - a Mace specialist's weapon transforms it into something much more precise and dangerous.",
                apCost: 1, mpCost: 0, range: 1, baseAccuracy: 0.70f, baseDamage: 0, damageType: DamageType.Blunt, strengthDivisor: 4f,
                critChance: 0.15f, critMultiplier: 3f,
                inflictsStatusEffect: "Stunned", statusEffectChance: 0.20f, statusDurationTurns: 1))
        {
            Type = WeaponType.Mace, Weight = 3, AttackPower = 8,
            SpecialistAccuracyBonus = 0.20f, SpecialistRangeBonus = 1,
            SpecialistFlatDamageBonus = 3, SpecialistFlatDamageBonusType = DamageType.Light,
        };
    }
}
