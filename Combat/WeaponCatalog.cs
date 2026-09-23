namespace SagesOfOzvaram.Combat
{
    /// <summary>
    /// Shared weapon definitions. Each property returns a fresh instance so units don't share
    /// mutable state through a common weapon object. Move numbers are first-pass placeholders
    /// pending a real balance pass, same as RaceAttacks. Physical weapons scale with Strength;
    /// magical weapons (the Sorcerer's Staff) scale with Intelligence instead. All attacks cost
    /// 2 AP for now, per design - except Sword Spin, a stronger AOE specialist move, at 3 AP.
    /// Weight (see BaseUnit.InventoryWeightCapacity) is 2 for small one-handed weapons (Dagger,
    /// Pistol) and 3 for everything larger (Sword, Mace, Staff, Bow, Shield).
    ///
    /// Flat damage is moving from each Move's own BaseDamage to a per-Weapon AttackPower (see
    /// Weapon.AttackPower) - the point being multiple weapons of the same kind (e.g. several
    /// daggers) can share identical moves but hit for different amounts. Dagger has been
    /// migrated (its moves' baseDamage is 0, unused); everything else here still carries its
    /// damage on the move itself until each one gets revisited.
    /// </summary>
    public static class WeaponCatalog
    {
        /// <summary>
        /// Sword Slash/Pierce are available to anyone carrying an Iron Sword; Sword Spin is a
        /// SpecialistAttack - only a WeaponType.OneHandedSword specialist (currently just the Warrior)
        /// can spin and hit everything adjacent.
        /// </summary>
        public static Weapon IronSword
        {
            get
            {
                var sword = new Weapon("One-Handed Iron Sword",
                    new Move("Sword Slash", "Slashes with the blade - a small chance to miss, an even smaller chance to crit for 3x damage, and a small chance to draw blood.",
                        apCost: 2, mpCost: 0, range: 1, baseAccuracy: 0.88f, baseDamage: 8, strengthDivisor: 4f,
                        critChance: 0.06f, critMultiplier: 3f,
                        inflictsStatusEffect: "Bleeding", bleedRank: StatusRank.Weak, statusEffectChance: 0.15f),
                    new Move("Sword Pierce", "A forward thrust that advances the attacker a tile - deals less damage than Sword Slash but punishes a Guarding target.",
                        apCost: 2, mpCost: 0, range: 2, baseAccuracy: 0.88f, baseDamage: 5, strengthDivisor: 5f,
                        inflictsStatusEffect: "Bleeding", bleedRank: StatusRank.Weak, statusEffectChance: 0.15f,
                        bonusDamageVsGuardingMultiplier: 1.5f, attackerAdvanceTiles: 1))
                { Type = WeaponType.OneHandedSword, Weight = 3 };

                sword.SpecialistAttacks.Add(new Move("Sword Spin",
                    "Spins the sword in a full circle, striking everything adjacent at once.",
                    apCost: 3, mpCost: 0, range: 1, baseAccuracy: 0.85f, baseDamage: 6, strengthDivisor: 3f,
                    hitsAllAdjacent: true));

                return sword;
            }
        }

        public static Weapon IronShield => new Weapon("Iron Shield",
            new Move("Shield Bash", "A shove with the shield's edge that can stagger the target.",
                apCost: 2, mpCost: 0, range: 1, baseAccuracy: 0.9f, baseDamage: 4, strengthDivisor: 6f,
                inflictsStatusEffect: "Stunned", statusEffectChance: 0.30f, statusDurationTurns: 1))
        { Type = WeaponType.Shield, ShieldGuardBonusPercent = 0.40f, Weight = 3 };

        // Sorcerer's shield - a Guard bonus only, no attack of its own.
        public static Weapon GlassShield => new Weapon("Glass Shield")
        { Type = WeaponType.Shield, ShieldGuardBonusPercent = 0.25f, Weight = 3 };

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
                    new Move("Stab", "A quick, precise thrust - short reach, but reliably finds its mark, and a hit from behind is a guaranteed critical.",
                        apCost: 2, mpCost: 0, range: 1, baseAccuracy: 1.0f, baseDamage: 0, strengthDivisor: 6f,
                        critChance: 0.05f, critMultiplier: 3f, guaranteedCritOnBackstab: true,
                        inflictsStatusEffect: "Bleeding", bleedRank: StatusRank.Weak, statusEffectChance: 0.35f))
                { Type = WeaponType.Dagger, Weight = 2, AttackPower = 7 };

                // Same dagger, same flat AttackPower as Stab - but a thrown strike doesn't
                // harness the wielder's Strength as efficiently as a direct thrust (weaker
                // StrengthDivisor), which is what keeps this "slightly less" than Stab overall.
                dagger.SpecialistAttacks.Add(new Move("Throw Dagger",
                    "Hurls the dagger at a target, consuming it - only a dagger specialist can make the throw count. Decent accuracy up close, but harder to land the further out it's thrown.",
                    apCost: 2, mpCost: 0, range: 8, baseAccuracy: 0.8f, baseDamage: 0, strengthDivisor: 10f,
                    accuracyFalloffPerTile: 0.08f, consumesWeapon: true));

                return dagger;
            }
        }

        /// <summary>
        /// Bonk is a base Attack anyone carrying a Staff can use (physical, STR-scaled). Arcane
        /// Missile is a SpecialistAttack - only a WeaponType.Staff specialist (currently just the
        /// Sorcerer) can channel it; everyone else is limited to Bonk.
        /// </summary>
        public static Weapon RhinewoodStaff
        {
            get
            {
                var staff = new Weapon("Rhinewood Staff",
                    new Move("Bonk", "A clumsy overhead whack with the staff - low accuracy, low damage, but can rattle the target.",
                        apCost: 2, mpCost: 0, range: 1, baseAccuracy: 0.55f, baseDamage: 2, strengthDivisor: 4f,
                        inflictsStatusEffect: "Stunned", statusEffectChance: 0.20f, statusDurationTurns: 1))
                { Type = WeaponType.Staff, Weight = 3 };

                staff.SpecialistAttacks.Add(new Move("Arcane Missile",
                    "A bolt of raw arcane energy that occasionally surges for extra damage - only a Staff specialist can channel it.",
                    apCost: 2, mpCost: 3, range: 3, baseAccuracy: 0.8f, baseDamage: 14, intelligenceDivisor: 3f,
                    damageType: DamageType.Magical, critChance: 0.08f, critMultiplier: 3f));

                return staff;
            }
        }

        // Bows are unreliable for non-specialists (very low base accuracy); a Bow specialist
        // (currently just the Hunter) gets +15% via SpecialistAccuracyBonus.
        public static Weapon Crossbow => new Weapon("Crossbow",
            new Move("Bolt Shot", "A mechanically-loosed bolt that can punch clean through, drawing blood - takes practice to land.",
                apCost: 2, mpCost: 0, range: 4, baseAccuracy: 0.40f, baseDamage: 10, strengthDivisor: 5f,
                inflictsStatusEffect: "Bleeding", bleedRank: StatusRank.Weak, statusEffectChance: 0.10f))
        { Type = WeaponType.Bow, SpecialistAccuracyBonus = 0.15f, Weight = 3 };

        public static Weapon Longbow => new Weapon("Longbow",
            new Move("Arrow Shot", "A well-aimed arrow from a distance, sharp enough to draw blood - takes practice to land.",
                apCost: 2, mpCost: 0, range: 5, baseAccuracy: 0.35f, baseDamage: 9, strengthDivisor: 5f,
                inflictsStatusEffect: "Bleeding", bleedRank: StatusRank.Weak, statusEffectChance: 0.10f))
        { Type = WeaponType.Bow, SpecialistAccuracyBonus = 0.15f, Weight = 3 };

        // No specialization - everyone has basic gun know-how, so damage doesn't scale with
        // STR or INT at all; it is what it is regardless of who's holding it.
        public static Weapon FlintlockPistol => new Weapon("Flintlock Pistol",
            new Move("Pistol Shot", "A loud, hard-hitting shot - not always reliable, but a solid hit can stagger and it has a real chance to critically wound.",
                apCost: 2, mpCost: 0, range: 3, baseAccuracy: 0.65f, baseDamage: 16,
                critChance: 0.12f, critMultiplier: 3f, knockbackBase: 1))
        { Type = WeaponType.Pistol, Weight = 2 };

        // A Mace specialist (currently just the Cleric) gets +20% accuracy and extra
        // INT-scaled damage on top of the normal STR-scaled hit.
        public static Weapon LightMace => new Weapon("Light Mace",
            new Move("Mace Bash", "A heavy, blunt strike more likely to stun than to cut.",
                apCost: 2, mpCost: 0, range: 1, baseAccuracy: 0.8f, baseDamage: 11, strengthDivisor: 4f,
                inflictsStatusEffect: "Stunned", statusEffectChance: 0.20f, statusDurationTurns: 1))
        { Type = WeaponType.Mace, SpecialistAccuracyBonus = 0.20f, SpecialistIntDamageBonusDivisor = 5f, Weight = 3 };
    }
}
