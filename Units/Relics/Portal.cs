using Microsoft.Xna.Framework;

namespace SagesOfOzvaram.Units.Relics
{
    /// <summary>
    /// A stationary battlefield structure (see BaseUnit.IsRelic) that anchors where its owner's
    /// Generic summons get placed - see Game1's portal-placement flow (CastSummon/
    /// ExecutePortalPlacement). Fully attackable like any other BaseUnit (HP/DEF/RES, Owner/team
    /// membership all work the same way) - it just never takes a turn of its own. No sprite art
    /// exists for it yet, so Game1 draws a simple procedural placeholder instead (see
    /// DrawPortalPlaceholder) rather than loading a texture.
    /// </summary>
    public class Portal : BaseUnit
    {
        public override bool IsRelic => true;

        public Portal(string name, int hp, int defense, int resistance, Vector2 position)
            : base(name, maxHP: hp, speed: 0, race: Race.GenericPortal, heroClass: HeroClass.None, position)
        {
            Defense = defense;
            Resistance = resistance;

            // A stationary structure - these never factor into anything for a Relic (it never
            // attacks and IsRelic keeps it out of turn order entirely), zeroed out so it doesn't
            // read like forgotten hero placeholder stats if ever inspected.
            Strength = 0;
            Intelligence = 0;
            Accuracy = 0;
            Evasion = 0;
        }
    }
}
