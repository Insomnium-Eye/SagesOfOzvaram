using Microsoft.Xna.Framework;

namespace SagesOfOzvaram.Units.Summons
{
    /// <summary>
    /// Brambleboar summon unit - stats match its SummonCard (see Combat.SummonCatalog).
    /// </summary>
    public class Brambleboar : BaseUnit
    {
        public Brambleboar(Vector2 position = default)
            : base("Brambleboar", maxHP: 20, speed: 6, race: Race.Brambleboar, heroClass: HeroClass.None, rank: UnitRank.Normal, position)
        {
            Factions.Add(Faction.Wild);
            Diet = Diet.Omnivore;
            ViewRange = 3;
            SpriteAssetPath = "imgs/sprites/units/Pawn_Sprite_1"; // placeholder - no unique art yet
            Strength = 10;     // "Attack" on the card - Gore now scales 1:1 with this (on request)
            Intelligence = 2;
            Defense = 7;
            Resistance = 4;
            Accuracy = 7;
            Evasion = 5;
            // No Inventory/EquippedWeapon - summons fight with their race's innate move only
            // (Gore, via RaceAttacks), not weapons or cards.
        }
    }
}
