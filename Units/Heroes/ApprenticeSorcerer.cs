
using Microsoft.Xna.Framework;
using SagesOfOzvaram.Combat;

namespace SagesOfOzvaram.Units.Heroes
{
    /// <summary>
    /// Apprentice Sorcerer unit.
    /// </summary>
    public class ApprenticeSorcerer : BaseUnit
    {
        public ApprenticeSorcerer(Vector2 position = default)
            : base("Apprentice Sorcerer", maxHP: 45, speed: 6, race: Race.Vectium, heroClass: HeroClass.Sorcerer, position)
        {
            SpriteAssetPath = "imgs/sprites/units/ApprenticeSorcerer/Sorcerer_Sprite_1";
            Scale = 1.0f;
            Strength = 5;    // Vectium: nimble spellcaster, not a melee fighter
            Accuracy = 10;
            Defense = 5;     // fragile in melee
            Resistance = 15; // naturally magic-attuned
            Intelligence = 18; // primary caster stat - Staff specialist, scales Arcane Missile
            MaxMP = 20;       // primary spellcaster
            CurrentMP = 20;

            TryAddToInventory(WeaponCatalog.RhinewoodStaff);
            TryAddToInventory(WeaponCatalog.Crossbow);
            TryAddToInventory(WeaponCatalog.Dagger);
            TryAddToInventory(WeaponCatalog.GlassShield); // Glass Shield is the Sorcerer's shield for now
        }
    }
}
