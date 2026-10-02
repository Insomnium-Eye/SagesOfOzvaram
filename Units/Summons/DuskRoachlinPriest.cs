using Microsoft.Xna.Framework;

namespace SagesOfOzvaram.Units.Summons
{
    /// <summary>
    /// Dusk Roachlin Priest summon unit - stats match its SummonCard (see Combat.SummonCatalog).
    /// </summary>
    public class DuskRoachlinPriest : BaseUnit
    {
        public DuskRoachlinPriest(Vector2 position = default)
            : base("Dusk Roachlin Priest", maxHP: 18, speed: 5, race: Race.DuskRoachlinPriest, heroClass: HeroClass.None, position)
        {
            // No sprite art exists for this summon yet.
            Strength = 4;      // "Attack" on the card
            Intelligence = 11;
            Defense = 5;
            Resistance = 10;
            Accuracy = 7;
            Evasion = 6;
            // No Inventory/EquippedWeapon - summons fight with their race's innate move only
            // (Shadow Weave, via RaceAttacks), not weapons or cards.
        }
    }
}
