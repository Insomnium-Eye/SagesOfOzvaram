using System.Collections.Generic;
using SagesOfOzvaram.Units;

namespace SagesOfOzvaram.Combat
{
    /// <summary>
    /// Racial perks - a stat bonus (or two) paired with a specific elemental vulnerability,
    /// applied automatically to EVERY unit of that race regardless of class. Mirrors
    /// ClassCatalog's per-class weapon specializations, but keyed by Race instead of HeroClass.
    /// Stat bonuses feed into BaseUnit's Effective*/MitigationPercent properties - nothing reads
    /// Strength/Defense/Resistance/Evasion directly in combat math, only the Effective* versions.
    /// </summary>
    public static class RaceCatalog
    {
        public class RacialModifiers
        {
            public int StrengthBonus { get; init; }
            public int DefenseBonus { get; init; }
            public int ResistanceBonus { get; init; }
            public int EvasionBonus { get; init; }

            /// <summary>The one DamageType this race takes extra damage from. Null = no vulnerability.</summary>
            public DamageType? VulnerableTo { get; init; }

            /// <summary>Extra damage taken from VulnerableTo, as a fraction (0.02 = +2%) - applied AFTER normal DEF/RES mitigation, in BaseUnit.TakeDamage.</summary>
            public float VulnerabilityBonusPercent { get; init; }
        }

        private static readonly RacialModifiers None = new RacialModifiers();

        private static readonly Dictionary<Race, RacialModifiers> Modifiers = new Dictionary<Race, RacialModifiers>
        {
            [Race.Vectium] = new RacialModifiers { EvasionBonus = 5, VulnerableTo = DamageType.Electric, VulnerabilityBonusPercent = 0.02f },
            [Race.Human] = new RacialModifiers { ResistanceBonus = 7, VulnerableTo = DamageType.Poison, VulnerabilityBonusPercent = 0.04f },
            [Race.Lethios] = new RacialModifiers { StrengthBonus = 2, DefenseBonus = 2, VulnerableTo = DamageType.Frost, VulnerabilityBonusPercent = 0.10f },
            // Roachlin (summoned creatures) has no perks defined yet - falls back to None below.
        };

        public static RacialModifiers GetModifiers(Race race) => Modifiers.TryGetValue(race, out var mods) ? mods : None;
    }
}
