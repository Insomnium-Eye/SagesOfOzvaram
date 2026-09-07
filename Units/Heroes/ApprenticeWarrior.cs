
using Microsoft.Xna.Framework;
using SagesOfOzvaram.Combat;

namespace SagesOfOzvaram.Units.Heroes
{
    /// <summary>
    /// Apprentice Warrior unit.
    /// </summary>
    public class ApprenticeWarrior : BaseUnit
    {
        public ApprenticeWarrior(Vector2 position = default)
            : base("Apprentice Warrior", maxHP: 60, speed: 4, race: Race.Lethios, heroClass: HeroClass.Warrior, position)
        {
            SpriteAssetPath = "imgs/sprites/units/ApprenticeWarrior/Warrior_Sprite_1";
            Scale = 0.65f;
            Strength = 15;   // Lethios: physically powerful, per GDD flavor
            Accuracy = 5;
            Defense = 20;    // Lethios: durable tank
            Resistance = 5;
            Intelligence = 4; // brawn over brains

            Inventory.Add(WeaponCatalog.IronSword);
            Inventory.Add(WeaponCatalog.IronShield);
            Inventory.Add(WeaponCatalog.Dagger);
            EquippedWeapon = Inventory[0];
        }
    }
}
