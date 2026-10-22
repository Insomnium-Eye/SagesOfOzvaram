namespace SagesOfOzvaram.Units
{
    /// <summary>
    /// A unit's broader allegiance, independent of the FFA's own per-match Owner/team system
    /// (BaseUnit.Owner/GetTeamRoot) - the three canon lore factions from the GDD (§2 World &amp;
    /// Lore), plus Wild for non-faction creatures (every current Units.Summons.* class). Unlike
    /// the canon three, Wild isn't a real allegiance in the lore sense - its actual stance toward
    /// everyone else varies per-unit and per-encounter (see BaseUnit.Disposition), which is why
    /// Tame Beast has to check Disposition on top of just "is this thing Wild" before it can ever
    /// succeed.
    /// </summary>
    public enum Faction
    {
        SunAndScaleAlliance,
        Draconics,
        HumanSupremacists,
        Wild,
    }
}
