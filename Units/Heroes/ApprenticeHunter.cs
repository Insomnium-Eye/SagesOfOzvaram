
using Microsoft.Xna.Framework;
using SagesOfOzvaram.Combat;

namespace SagesOfOzvaram.Units.Heroes
{
    /// <summary>
    /// Apprentice Hunter unit.
    /// </summary>
    public class ApprenticeHunter : BaseUnit
    {
        public ApprenticeHunter(Vector2 position = default)
            : base("Apprentice Hunter", maxHP: 55, speed: 8, race: Race.Human, heroClass: HeroClass.Hunter, position)
        {
            SpriteAssetPath = "imgs/sprites/units/ApprenticeHunter/Hunter_Sprite_1";
            Scale = 0.65f;
            Strength = 8;    // Human: ranged marksman, precision over raw power
            Accuracy = 15;
            Defense = 8;
            Resistance = 8;
            Intelligence = 6;
            MaxMP = 8;       // skill-based, not a caster
            CurrentMP = 8;

            Inventory.Add(WeaponCatalog.Longbow);
            Inventory.Add(WeaponCatalog.FlintlockPistol);
            Inventory.Add(WeaponCatalog.Dagger);
            EquippedWeapon = Inventory[0];
            // Dagger specialization (unlocking Throw Knife) comes from HeroClass.Hunter via
            // ClassCatalog - shared by every Hunter, not set per-instance here.
        }
    }
}
