using Microsoft.Xna.Framework;

namespace SagesOfOzvaram.Units.Summons
{
    /// <summary>
    /// Bearat summon unit - stats match its SummonCard (see Combat.SummonCatalog).
    /// </summary>
    public class Bearat : BaseUnit
    {
        public Bearat(Vector2 position = default)
            : base("Bearat", maxHP: 24, speed: 6, race: Race.Bearat, heroClass: HeroClass.None, position)
        {
            SpriteAssetPath = "imgs/sprites/units/Pawn_Sprite_1"; // placeholder - no unique art yet
            Strength = 12;     // "Attack" on the card
            Intelligence = 2;
            Defense = 9;
            Resistance = 4;
            Accuracy = 7;
            Evasion = 5;
            // No Inventory/EquippedWeapon - summons fight with their race's innate move only
            // (Maul, via RaceAttacks), not weapons or cards.
        }
    }
}
