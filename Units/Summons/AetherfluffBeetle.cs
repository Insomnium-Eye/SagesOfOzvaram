using Microsoft.Xna.Framework;

namespace SagesOfOzvaram.Units.Summons
{
    /// <summary>
    /// Aetherfluff Beetle summon unit - stats match its SummonCard (see Combat.SummonCatalog).
    /// </summary>
    public class AetherfluffBeetle : BaseUnit
    {
        public AetherfluffBeetle(Vector2 position = default)
            : base("Aetherfluff Beetle", maxHP: 10, speed: 5, race: Race.AetherfluffBeetle, heroClass: HeroClass.None, position)
        {
            // No sprite art exists for this summon yet.
            Strength = 0;      // "Attack" on the card
            Intelligence = 2;
            Defense = 4;
            Resistance = 4;
            Accuracy = 5;
            Evasion = 8;
            // No Inventory/EquippedWeapon - summons fight with their race's innate move only
            // (Nudge, via RaceAttacks), not weapons or cards.
        }
    }
}
