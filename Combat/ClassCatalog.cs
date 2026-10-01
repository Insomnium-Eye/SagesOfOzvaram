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
            [HeroClass.Warrior] = new HashSet<WeaponType> { WeaponType.OneHandedSword, WeaponType.Shield },
            [HeroClass.Sorcerer] = new HashSet<WeaponType> { WeaponType.Staff, WeaponType.Wand },
            [HeroClass.Cleric] = new HashSet<WeaponType> { WeaponType.Mace },
            [HeroClass.None] = new HashSet<WeaponType>(),
        };

        public static HashSet<WeaponType> GetWeaponSpecializations(HeroClass heroClass) => Specializations[heroClass];

        // Flat MP regenerated at the start of every turn (see BaseUnit.RegenMana), tiered by
        // class rather than set per-instance - same "shared by Class" rule as specializations
        // above. Sorcerer highest (primary spellcaster) > Cleric (support caster) > Warrior/Hunter
        // tied lowest (not casters) > None (summoned creatures - no mana pool to speak of).
        // First-pass placeholder numbers, pending a balance pass like everywhere else.
        private static readonly Dictionary<HeroClass, int> ManaRegenPerTurn = new Dictionary<HeroClass, int>
        {
            [HeroClass.Sorcerer] = 4,
            [HeroClass.Cleric] = 3,
            [HeroClass.Hunter] = 1,
            [HeroClass.Warrior] = 1,
            [HeroClass.None] = 0,
        };

        public static int GetManaRegenPerTurn(HeroClass heroClass) => ManaRegenPerTurn[heroClass];

        // Spells (per HeroClass) will live here too once a spell system exists.
    }
}
