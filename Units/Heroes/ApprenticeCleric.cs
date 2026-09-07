
using Microsoft.Xna.Framework;
using SagesOfOzvaram.Combat;

namespace SagesOfOzvaram.Units.Heroes
{
    /// <summary>
    /// Apprentice Cleric unit.
    /// </summary>
    public class ApprenticeCleric : BaseUnit
    {
        public ApprenticeCleric(Vector2 position = default)
            : base("Apprentice Cleric", maxHP: 50, speed: 5, race: Race.Human, heroClass: HeroClass.Cleric, position)
        {
            SpriteAssetPath = "imgs/sprites/units/ApprenticeCleric/Cleric_Sprite_1";
            Scale = 0.65f;
            Strength = 6;    // Human: balanced support, per GDD's Cleric flavor
            Accuracy = 8;
            Defense = 10;
            Resistance = 12;
            Intelligence = 12; // Mace specialist - the extra INT-based damage on Mace Bash draws on this
            MaxMP = 15;       // support caster
            CurrentMP = 15;

            Inventory.Add(WeaponCatalog.LightMace);
            Inventory.Add(WeaponCatalog.IronShield);
            EquippedWeapon = Inventory[0];
        }
    }
}
