
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
            Evasion = 4;
            Defense = 10;
            Resistance = 12;
            Intelligence = 12; // support caster stat - not currently tied to her Mace's flat +3 Light bonus, just her spellcasting
            MaxMP = 15;       // support caster
            CurrentMP = 15;

            TryAddToInventory(WeaponCatalog.LightMace);
            TryAddToInventory(WeaponCatalog.IronShield);
        }
    }
}
