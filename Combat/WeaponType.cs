namespace SagesOfOzvaram.Combat
{
    /// <summary>
    /// Broad weapon category, used to gate class/unit weapon specializations (e.g. only a
    /// Dagger specialist can Throw Knife). Not every weapon needs one - it's null unless a
    /// specialization actually cares about that weapon.
    /// </summary>
    public enum WeaponType
    {
        Dagger,
        OneHandedSword, // distinct from a future TwoHandedSword - a One-Handed specialist isn't automatically a Two-Handed one
        Shield,
        Staff,
        Bow,
        Pistol,
        Mace
    }
}
