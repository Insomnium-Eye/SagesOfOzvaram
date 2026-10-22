namespace SagesOfOzvaram.Units
{
    /// <summary>
    /// What a Wild creature eats - drives its default Neutral-disposition behavior (see
    /// BaseUnit.Diet/AngerRange and Game1.RunAggroDrivenSummonAI). Herbivore and Omnivore both
    /// just wander for now (Omnivore's own distinct behavior - splitting time between foraging
    /// and opportunistic predation - is a later phase); Carnivore is the one that actively
    /// watches AngerRange and turns Hostile on its own.
    /// </summary>
    public enum Diet
    {
        Herbivore,
        Carnivore,
        Omnivore,
    }
}
