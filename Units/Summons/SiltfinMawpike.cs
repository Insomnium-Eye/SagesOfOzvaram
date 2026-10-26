using Microsoft.Xna.Framework;

namespace SagesOfOzvaram.Units.Summons
{
    /// <summary>
    /// Siltfin Mawpike summon unit - stats match its SummonCard (see Combat.SummonCatalog).
    /// </summary>
    public class SiltfinMawpike : BaseUnit
    {
        public SiltfinMawpike(Vector2 position = default)
            : base("Siltfin Mawpike", maxHP: 16, speed: 8, race: Race.SiltfinMawpike, heroClass: HeroClass.None, rank: UnitRank.Normal, position)
        {
            Factions.Add(Faction.Wild);
            Diet = Diet.Carnivore;
            AngerRange = 3;
            ViewRange = 3;
            SpriteAssetPath = "imgs/sprites/units/Pawn_Sprite_1"; // placeholder - no unique art yet
            Strength = 9;      // "Attack" on the card
            Intelligence = 3;
            Defense = 6;
            Resistance = 5;
            Accuracy = 8;
            Evasion = 6;
            // No Inventory/EquippedWeapon - summons fight with their race's innate move only
            // (Snap Bite, via RaceAttacks), not weapons or cards.
        }
    }
}
