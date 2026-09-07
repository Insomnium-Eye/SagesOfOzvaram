namespace SagesOfOzvaram.Units
{
    /// <summary>
    /// The 4 base classes (GDD §4.2). Distinct from Race - any race can theoretically be any
    /// class. Drives what a unit has access to via ClassCatalog (weapon specializations, and
    /// eventually spells), independent of which specific tier/subclass instance it is.
    /// </summary>
    public enum HeroClass
    {
        Sorcerer,
        Warrior,
        Cleric,
        Hunter
    }
}
