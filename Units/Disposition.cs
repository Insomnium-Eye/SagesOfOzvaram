namespace SagesOfOzvaram.Units
{
    /// <summary>
    /// A unit's current stance toward everyone else - only meaningful for a Wild-faction unit
    /// with no Owner yet (see BaseUnit.Disposition/Factions); a real Faction's units are just
    /// hostile to anyone outside it, no per-unit variance needed. Decided per-unit and
    /// per-encounter, not fixed by Faction membership alone - the same species could be Neutral
    /// in one placement and Hostile in another (see Game1's "summon" dev command/Tame Beast).
    /// </summary>
    public enum Disposition
    {
        /// <summary>Won't act unless provoked (see RunAggroDrivenSummonAI's forced-targeting check) - the only disposition Tame Beast can ever succeed against.</summary>
        Neutral,

        /// <summary>Same passive stand-down behavior as Neutral, but never a valid Tame Beast target - already bonded to someone/something, not a free agent.</summary>
        Friendly,

        /// <summary>Always aggressive toward everyone it can reach, same as SummonBehavior.HostileTowardsEnemies but with no Owner to spare.</summary>
        Hostile,
    }
}
