using System.Collections.Generic;
using SagesOfOzvaram.Units;

namespace SagesOfOzvaram.Combat
{
    /// <summary>
    /// What a HeroClass grants to ANY unit that is that class - weapon specializations today,
    /// spells later. Tied to Class rather than a specific tier/subclass (Apprentice, Journeyman,
    /// ...), so a unit keeps its class's specializations/spells as it evolves through tiers, and
    /// any future non-Apprentice unit sharing that class gets them too.
    /// </summary>
    public static class ClassCatalog
    {
        private static readonly Dictionary<HeroClass, HashSet<WeaponType>> Specializations = new Dictionary<HeroClass, HashSet<WeaponType>>
        {
            [HeroClass.Hunter] = new HashSet<WeaponType> { WeaponType.Dagger, WeaponType.Bow },
            [HeroClass.Warrior] = new HashSet<WeaponType> { WeaponType.OneHandedSword },
            [HeroClass.Sorcerer] = new HashSet<WeaponType> { WeaponType.Staff },
            [HeroClass.Cleric] = new HashSet<WeaponType> { WeaponType.Mace },
            [HeroClass.None] = new HashSet<WeaponType>(),
        };

        public static HashSet<WeaponType> GetWeaponSpecializations(HeroClass heroClass) => Specializations[heroClass];

        // Spells (per HeroClass) will live here too once a spell system exists.
    }
}
