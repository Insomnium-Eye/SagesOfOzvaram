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

            /// <summary>
            /// Every DamageType this race takes extra damage from, and by how much (0.02 = +2%),
            /// applied in BaseUnit.TakeDamage BEFORE normal DEF/RES mitigation (so DEF/RES still
            /// mitigates part of the inflated amount, rather than being tacked on after - this
            /// doc previously said "after", which was wrong; the code was always "before").
            /// Generalized from a single nullable VulnerableTo+VulnerabilityBonusPercent pair to
            /// a dictionary so a race can have more than one (e.g. Roachlin: Fire +50%). A
            /// DamageType present here with 0% (e.g. Roachlin: Light) is still "vulnerable" for
            /// GuaranteedWeakBlindFrom below, just with no extra damage.
            /// </summary>
            public Dictionary<DamageType, float> DamageVulnerabilities { get; init; } = new Dictionary<DamageType, float>();

            /// <summary>
            /// If set, being hit by THIS DamageType always inflicts Weak Blind (100% - no roll,
            /// regardless of whatever InflictsStatusEffect the move itself carries) - e.g.
            /// Roachlin + Light. Deliberately narrow (just this one status, not a generic "any
            /// status on any damage type" reaction system) since it's the only current case that
            /// needs it - generalize later if a second one ever comes up.
            /// </summary>
            public DamageType? GuaranteedWeakBlindFrom { get; init; }
        }

        private static readonly RacialModifiers None = new RacialModifiers();

        private static readonly Dictionary<Race, RacialModifiers> Modifiers = new Dictionary<Race, RacialModifiers>
        {
            [Race.Vectium] = new RacialModifiers { EvasionBonus = 5, DamageVulnerabilities = new Dictionary<DamageType, float> { [DamageType.Electric] = 0.02f } },
            [Race.Human] = new RacialModifiers { ResistanceBonus = 7, DamageVulnerabilities = new Dictionary<DamageType, float> { [DamageType.Poison] = 0.04f } },
            [Race.Lethios] = new RacialModifiers { StrengthBonus = 2, DefenseBonus = 2, DamageVulnerabilities = new Dictionary<DamageType, float> { [DamageType.Frost] = 0.10f } },
            [Race.Roachlin] = new RacialModifiers
            {
                DamageVulnerabilities = new Dictionary<DamageType, float> { [DamageType.Fire] = 0.50f, [DamageType.Light] = 0f },
                GuaranteedWeakBlindFrom = DamageType.Light,
            },
        };

        public static RacialModifiers GetModifiers(Race race) => Modifiers.TryGetValue(race, out var mods) ? mods : None;
    }
}
