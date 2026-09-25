namespace SagesOfOzvaram.Combat
{
    /// <summary>
    /// A consumable ammo type a ranged Move can require (see Move.RequiredAmmoType) - tracked
    /// separately per type on BaseUnit (an Arrow can't feed a Crossbow), not shared as one
    /// generic pool.
    /// </summary>
    public enum AmmoType
    {
        Arrow,  // Longbow
        Bullet, // Flintlock Pistol
        Bolt,   // Crossbow
    }
}
