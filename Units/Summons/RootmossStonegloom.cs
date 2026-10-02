using Microsoft.Xna.Framework;

namespace SagesOfOzvaram.Units.Summons
{
    /// <summary>
    /// Rootmoss Stonegloom summon unit - stats match its SummonCard (see Combat.SummonCatalog).
    /// </summary>
    public class RootmossStonegloom : BaseUnit
    {
        public RootmossStonegloom(Vector2 position = default)
            : base("Rootmoss Stonegloom", maxHP: 30, speed: 3, race: Race.RootmossStonegloom, heroClass: HeroClass.None, position)
        {
            // No sprite art exists for this summon yet.
            Strength = 9;      // "Attack" on the card
            Intelligence = 3;
            Defense = 12;
            Resistance = 7;
            Accuracy = 6;
            Evasion = 3;
            // No Inventory/EquippedWeapon - summons fight with their race's innate move only
            // (Stone Slam, via RaceAttacks), not weapons or cards.
        }
    }
}
