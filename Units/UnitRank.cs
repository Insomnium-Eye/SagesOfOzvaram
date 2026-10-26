namespace SagesOfOzvaram.Units
{
    /// <summary>
    /// A unit's power tier, on request. For now the only thing this drives is starting MaxAP
    /// (see Combat.RankCatalog.GetMaxAP) - a later pass could use it for other scaling (XP
    /// reward, loot, AI difficulty, ...) once there's a reason to.
    /// </summary>
    public enum UnitRank
    {
        Weak,
        Normal,
        Elite,
        Hero,
        Boss,
        Legendary,
        Mythical,
    }
}
