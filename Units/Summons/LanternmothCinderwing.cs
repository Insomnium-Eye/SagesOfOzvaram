using Microsoft.Xna.Framework;

namespace SagesOfOzvaram.Units.Summons
{
    /// <summary>
    /// Lanternmoth Cinderwing summon unit - stats match its SummonCard (see Combat.SummonCatalog).
    /// </summary>
    public class LanternmothCinderwing : BaseUnit
    {
        public LanternmothCinderwing(Vector2 position = default)
            : base("Lanternmoth Cinderwing", maxHP: 12, speed: 7, race: Race.LanternmothCinderwing, heroClass: HeroClass.None, position)
        {
            // No sprite art exists for this summon yet.
            Strength = 3;      // "Attack" on the card
            Intelligence = 8;
            Defense = 4;
            Resistance = 8;
            Accuracy = 7;
            Evasion = 7;
            // No Inventory/EquippedWeapon - summons fight with their race's innate move only
            // (Cinder Dust, via RaceAttacks), not weapons or cards.
        }
    }
}
