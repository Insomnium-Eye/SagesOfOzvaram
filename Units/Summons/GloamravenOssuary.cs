using Microsoft.Xna.Framework;

namespace SagesOfOzvaram.Units.Summons
{
    /// <summary>
    /// Gloamraven Ossuary summon unit - stats match its SummonCard (see Combat.SummonCatalog).
    /// </summary>
    public class GloamravenOssuary : BaseUnit
    {
        public GloamravenOssuary(Vector2 position = default)
            : base("Gloamraven Ossuary", maxHP: 14, speed: 9, race: Race.GloamravenOssuary, heroClass: HeroClass.None, position)
        {
            SpriteAssetPath = "imgs/sprites/units/Pawn_Sprite_1"; // placeholder - no unique art yet
            Strength = 8;      // "Attack" on the card
            Intelligence = 6;
            Defense = 5;
            Resistance = 6;
            Accuracy = 10;
            Evasion = 8;
            // No Inventory/EquippedWeapon - summons fight with their race's innate move only
            // (Precision Strike, via RaceAttacks), not weapons or cards.
        }
    }
}
