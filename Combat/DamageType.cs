namespace SagesOfOzvaram.Combat
{
    /// <summary>
    /// A hit's damage type - Sharp and Blunt are the two physical flavors (mitigated by DEF);
    /// everything else is magical/elemental (mitigated by RES). Electric/Poison/Frost/Fire exist
    /// specifically as racial vulnerability targets (see RaceCatalog) - nothing currently deals
    /// them (no move uses these yet), but a future spell/weapon can. Light is both a real,
    /// already-dealt damage type (a Cleric's blessed mace) AND a racial vulnerability target
    /// (Roachlin - always inflicts Weak Blind on hit, see RaceCatalog.GuaranteedWeakBlindFrom).
    /// Otherwise pure label for now (see IsPhysical) - nothing gates which status effects a type
    /// can inflict, though every move happens to already follow a natural pattern (Sharp things
    /// draw blood, Blunt things stun/knock down) that could become a real rule later.
    /// </summary>
    public enum DamageType
    {
        Sharp,    // blades, points, claws, bites, arrows/bolts - swords, daggers, bows
        Blunt,    // bludgeoning - maces, staves, shields, fists
        Magical,  // arcane spell damage
        Light,    // holy/radiant magic - e.g. a Cleric's blessed mace
        Electric, // elemental - a Vectium racial vulnerability
        Poison,   // elemental - a Human racial vulnerability
        Frost,    // elemental - a Lethios racial vulnerability
        Fire,     // elemental - a Roachlin racial vulnerability (+50%, see RaceCatalog)
    }

    public static class DamageTypeExtensions
    {
        /// <summary>True for Sharp/Blunt (mitigated by DEF); false for anything magical/elemental (mitigated by RES) - see BaseUnit.TakeDamage.</summary>
        public static bool IsPhysical(this DamageType type) => type == DamageType.Sharp || type == DamageType.Blunt;
    }
}
