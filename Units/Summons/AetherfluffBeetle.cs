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
            Factions.Add(Faction.Wild);
            Diet = Diet.Herbivore;
            ViewRange = 3;
            SpriteAssetPath = "imgs/sprites/units/Pawn_Sprite_1"; // placeholder - no unique art yet
            Strength = 0;      // "Attack" on the card
            Intelligence = 2;
            Defense = 4;
            Resistance = 4;
            Accuracy = 5;
            Evasion = 8;
            // Mana aura - see BaseUnit.ManaAuraRange/Game1.ApplyManaAuras: at the start of every
            // OTHER unit's turn, everything within 4 tiles of this beetle restores 5 MP,
            // regardless of team.
            ManaAuraRange = 4;
            ManaAuraAmount = 5;
            // No Inventory/EquippedWeapon - summons fight with their race's innate move only
            // (Nudge, via RaceAttacks), not weapons or cards.
        }
    }
}
