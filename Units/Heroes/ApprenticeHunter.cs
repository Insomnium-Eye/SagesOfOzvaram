
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
            Evasion = 6;
            Defense = 8;
            Resistance = 8;
            Intelligence = 6;
            // Was 8 - too low to ever afford the Portal System's bootstrap (its own card cost
            // plus the +5 surcharge for a first Generic Portal) even for the cheapest summon,
            // and Tame Beast/the generic utility spells in the Hunter's own kit need headroom
            // too. Still clearly the lowest of the "real" spenders (Sorcerer 20, Cleric 15) -
            // skill-based, not a caster - just no longer functionally locked out of the
            // beast-taming/summoning mechanics that are the class's own stated identity.
            MaxMP = 12;
            CurrentMP = 12;

            TryAddToInventory(WeaponCatalog.Longbow);
            TryAddToInventory(WeaponCatalog.FlintlockPistol);
            TryAddToInventory(WeaponCatalog.Dagger);
            // Dagger specialization (unlocking Throw Dagger) comes from HeroClass.Hunter via
            // ClassCatalog - shared by every Hunter, not set per-instance here.

            AddAmmo(AmmoType.Arrow, 10);
            AddAmmo(AmmoType.Bullet, 10);
        }
    }
}
