namespace SagesOfOzvaram.Units
{
    /// <summary>
    /// Playable/NPC races, per the GDD's race roster (§2).
    /// </summary>
    public enum Race
    {
        Human,
        Lethios,
        Vectium,

        /// <summary>A summoned-creature race, not a playable hero race - see RaceAttacks for its innate move.</summary>
        Roachlin
    }
}
