namespace SagesOfOzvaram.Combat
{
    /// <summary>
    /// Which simple visual effect Game1 plays when a Move resolves - a melee swing near the
    /// target (Slash) or a projectile traveling from attacker to target (Shooting). Purely
    /// presentational; doesn't affect resolution at all.
    /// </summary>
    public enum AttackAnimationType
    {
        Slash,
        Shooting,
    }
}
