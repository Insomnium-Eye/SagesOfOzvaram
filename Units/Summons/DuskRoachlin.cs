using Microsoft.Xna.Framework;

namespace SagesOfOzvaram.Units.Summons
{
    /// <summary>
    /// Dusk Roachlin summon unit - stats match its SummonCard (see Combat.SummonCatalog).
    /// </summary>
    public class DuskRoachlin : BaseUnit
    {
        public DuskRoachlin(Vector2 position = default)
            : base("Dusk Roachlin", maxHP: 12, speed: 9, race: Race.Roachlin, heroClass: HeroClass.None, rank: UnitRank.Normal, position)
        {
            Factions.Add(Faction.Wild);
            SpriteAssetPath = "imgs/sprites/units/Roachlin_Sprite_1";

            // The source image (662x657) is actually close to the Warrior's (736x769) but with
            // no per-unit Scale set it defaulted to 1.0 - the SAME multiplier as the Sorcerer's
            // much smaller 396x335 art - so it rendered far larger than any hero instead of
            // fitting its hex tile. 0.4 lands it around the Hunter's on-screen size, fitting
            // for a small swarm creature rather than the biggest thing on the field.
            Scale = 0.4f;

            Strength = 9;      // "Attack" on the card - Claw Crush now scales 1:1 with this (on request)
            Intelligence = 3;
            Defense = 6;
            Resistance = 4;
            Accuracy = 7;
            Evasion = 6;
            ViewRange = 3;
            // No Inventory/EquippedWeapon - summons fight with their race's innate move only
            // (Roachlin Slash, via RaceAttacks), not weapons or cards.
        }
    }
}
