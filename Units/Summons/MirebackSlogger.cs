using Microsoft.Xna.Framework;

namespace SagesOfOzvaram.Units.Summons
{
    /// <summary>
    /// Mireback Slogger summon unit - stats match its SummonCard (see Combat.SummonCatalog).
    /// </summary>
    public class MirebackSlogger : BaseUnit
    {
        public MirebackSlogger(Vector2 position = default)
            : base("Mireback Slogger", maxHP: 26, speed: 4, race: Race.MirebackSlogger, heroClass: HeroClass.None, rank: UnitRank.Normal, position)
        {
            Factions.Add(Faction.Wild);
            Diet = Diet.Herbivore;
            ViewRange = 3;
            SpriteAssetPath = "imgs/sprites/units/Pawn_Sprite_1"; // placeholder - no unique art yet
            Strength = 10;     // "Attack" on the card
            Intelligence = 2;
            Defense = 11;
            Resistance = 4;
            Accuracy = 6;
            Evasion = 3;
            // No Inventory/EquippedWeapon - summons fight with their race's innate move only
            // (Shove, via RaceAttacks), not weapons or cards.
        }
    }
}
