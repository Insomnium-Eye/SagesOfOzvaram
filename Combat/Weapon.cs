using System.Collections.Generic;

namespace SagesOfOzvaram.Combat
{
    /// <summary>
    /// An inventory item that grants its own attack(s) to whichever unit carries it, on top
    /// of that unit's innate racial move.
    /// </summary>
    public class Weapon
    {
        public string Name { get; }
        public List<Move> Attacks { get; }

        /// <summary>
        /// This specific weapon's flat power - e.g. a Rusty Dagger and a Steel Dagger both grant
        /// the same Stab move but hit for different amounts, because THEY (not the move) carry
        /// this number. Replaces the move's own BaseDamage for every attack this weapon grants
        /// (see Move.GetDamage); the wielder's Strength/Intelligence still adds on top via the
        /// move's own StrengthDivisor/IntelligenceDivisor, same as before. Null means this
        /// weapon hasn't been migrated to the per-weapon power system yet, so its moves fall
        /// back to their own BaseDamage in the meantime.
        /// </summary>
        public int? AttackPower { get; set; }

        /// <summary>
        /// Inventory space this weapon takes up (see BaseUnit.InventoryWeightCapacity). Small
        /// one-handed weapons (Dagger, Pistol) are 2; larger one-handed weapons (Sword, Mace,
        /// Staff, Bow, Shield) are 3.
        /// </summary>
        public int Weight { get; set; }

        /// <summary>
        /// This weapon's broad category, if a specialization cares about it (e.g. Dagger).
        /// Null for weapons no class specializes in yet.
        /// </summary>
        public WeaponType? Type { get; set; }

        /// <summary>
        /// Bonus attacks only available to a unit specialized in this weapon's Type (see
        /// BaseUnit.WeaponSpecializations) - e.g. only a Dagger specialist gets Throw Dagger,
        /// even though anyone can carry a Dagger and Stab with it.
        /// </summary>
        public List<Move> SpecialistAttacks { get; } = new List<Move>();

        /// <summary>
        /// If this is a shield, the passive DEF/RES bonus (0-1) it grants just by being carried -
        /// applies always, not just while Guarding. Null if this isn't a shield. See
        /// BaseUnit.GetPassiveShieldBonusPercent.
        /// </summary>
        public float? ShieldPassiveDefResBonusPercent { get; set; }

        /// <summary>
        /// If this is a shield, the ADDITIONAL DEF/RES bonus (0-1) Guard grants on top of
        /// ShieldPassiveDefResBonusPercent while actively Guarding - replaces the base 20%
        /// Guard bonus rather than stacking with it. Null if this isn't a shield.
        /// </summary>
        public float? ShieldGuardBonusPercent { get; set; }

        /// <summary>
        /// Flat hit-chance bonus (0-1) a specialist gets using THIS weapon's moves (e.g. the
        /// Hunter's +15% on bows, the Cleric's +20% on maces). 0 = no bonus. Only applies if the
        /// attacker's WeaponSpecializations contains this weapon's Type.
        /// </summary>
        public float SpecialistAccuracyBonus { get; set; } = 0f;

        /// <summary>
        /// Extra damage bonus (Intelligence / this divisor) a specialist gets on top of a move's
        /// normal damage using THIS weapon. 0 = no bonus. Only applies if the attacker's
        /// WeaponSpecializations contains this weapon's Type.
        /// </summary>
        public float SpecialistIntDamageBonusDivisor { get; set; } = 0f;

        /// <summary>Extra Range a specialist gets on THIS weapon's moves (e.g. the Cleric's mace head extending on a chain). 0 = no bonus. See Move.GetEffectiveRange.</summary>
        public int SpecialistRangeBonus { get; set; } = 0;

        /// <summary>Extra flat damage (of SpecialistFlatDamageBonusType, possibly different from the move's own DamageType) a specialist deals on top of a normal hit - e.g. the Cleric's +3 Light damage on Mace Bash. 0 = no bonus. See Move.GetSpecialistBonusDamage.</summary>
        public int SpecialistFlatDamageBonus { get; set; } = 0;

        /// <summary>The DamageType of SpecialistFlatDamageBonus - null if SpecialistFlatDamageBonus is 0.</summary>
        public DamageType? SpecialistFlatDamageBonusType { get; set; }

        public Weapon(string name, params Move[] attacks)
        {
            Name = name;
            Attacks = new List<Move>(attacks);
        }
    }
}
