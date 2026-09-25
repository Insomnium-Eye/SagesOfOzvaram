namespace SagesOfOzvaram.Combat
{
    /// <summary>
    /// Broad weapon category, used to gate class/unit weapon specializations (e.g. only a
    /// Dagger specialist can Throw Dagger). Not every weapon needs one - it's null unless a
    /// specialization actually cares about that weapon.
    /// </summary>
    public enum WeaponType
    {
        Dagger,
        OneHandedSword, // distinct from a future TwoHandedSword - a One-Handed specialist isn't automatically a Two-Handed one
        Shield,
        Staff,
        Wand, // distinct from Staff - a Staff specialist isn't automatically a Wand specialist (the Sorcerer happens to be both)
        Bow,
        Crossbow, // distinct from Bow - a generalist weapon (see WeaponCatalog.Crossbow), nobody specializes in it, and a Bow specialist gets no bonus using one
        Pistol,
        Mace
    }
}
