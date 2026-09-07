using Microsoft.Xna.Framework;

namespace SagesOfOzvaram.Units.Summons
{
    /// <summary>
    /// Dusk Roachlin summon unit - stats match its SummonCard (see Combat.SummonCatalog).
    /// </summary>
    public class DuskRoachlin : BaseUnit
    {
        public DuskRoachlin(Vector2 position = default)
            : base("Dusk Roachlin", maxHP: 12, speed: 9, race: Race.Roachlin, heroClass: HeroClass.None, position)
        {
            SpriteAssetPath = "imgs/sprites/units/Roachlin_Sprite_1";
            Strength = 8;      // "Attack" on the card
            Intelligence = 3;
            Defense = 6;
            Resistance = 4;
            Accuracy = 7;
            Evasion = 6;
            // No Inventory/EquippedWeapon - summons fight with their race's innate move only
            // (Roachlin Slash, via RaceAttacks), not weapons or cards.
        }
    }
}
