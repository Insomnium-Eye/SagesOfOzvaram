using System.Collections.Generic;
using SagesOfOzvaram.Units;

namespace SagesOfOzvaram.Combat
{
    /// <summary>
    /// Every unit gets its race's innate attack(s) for free, always available regardless of
    /// equipment; weapons grant additional attacks on top of these. Generalized from one Move
    /// per Race to a list, on request - summoned creatures can now have a real kit of several
    /// moves (e.g. the Dusk Roachlin's Claw Crush/Leap/Syphon), not just a single attack, the
    /// same way a hero's weapon grants several. A summon still can't cast SpellCards from a
    /// hand (no deck/hand exists for one), but costing MP on top of AP - same as a hero's spell -
    /// is exactly what several of these new multi-move kits do, per the request that added them.
    /// </summary>
    public static class RaceAttacks
    {
        private static readonly Dictionary<Race, List<Move>> InnateMoves = new Dictionary<Race, List<Move>>
        {
            [Race.Human] = new List<Move> { new Move(
                "Punch", "A quick jab - very little behind it, but reliable, and a strong enough puncher can put the target flat on their back.",
                apCost: 1, mpCost: 0, range: 1,
                baseAccuracy: 0.95f, baseDamage: 1, damageType: DamageType.Blunt, strengthDivisor: 5f,
                critChance: 0.01f, critMultiplier: 3f,
                knockbackBase: 1, knockbackDamagePerTile: 5,
                canInflictKnockdown: true, knockdownChanceIfStrongerStr: 0.80f, knockdownChanceOtherwise: 0.02f,
                knockdownStandUpApCost: 1) },

            [Race.Lethios] = new List<Move> { new Move(
                "Bite", "A vicious, hard-to-land bite - but when it connects, it's devastating and draws heavy blood.",
                apCost: 1, mpCost: 0, range: 1,
                baseAccuracy: 0.40f, baseDamage: 12, damageType: DamageType.Sharp, strengthDivisor: 2f,
                critChance: 0.25f, critMultiplier: 3f,
                inflictsStatusEffect: "Bleeding", bleedRank: StatusRank.Moderate) },

            [Race.Vectium] = new List<Move> { new Move(
                "Swipe", "A precise claw strike - far more reliable than a bite and quicker to find a vital spot, though it draws less blood.",
                apCost: 1, mpCost: 0, range: 1,
                baseAccuracy: 0.80f, baseDamage: 3, damageType: DamageType.Sharp, strengthDivisor: 1.5f,
                critChance: 0.40f, critMultiplier: 3f,
                inflictsStatusEffect: "Bleeding", bleedRank: StatusRank.Minor) },

            // Summoned-creature races (not playable heroes) get their attack list the same way -
            // via their race's innate moves - since summons don't have cards/weapons of their
            // own. Dusk Roachlin is the first to get a real multi-move kit (Claw Crush/Leap) -
            // see Game1.CastLeap/ExecuteLeapOntoUnit/ExecuteLeapToTile for Leap's own dedicated
            // targeting, and RoachlinSyphon below (deliberately NOT in this list - see its own
            // doc comment) for the third move of the kit, Syphon.
            [Race.Roachlin] = new List<Move>
            {
                // 1:1 STR scaling, on request (see Units.Summons.DuskRoachlin's own Strength =
                // 9) - baseDamage 0, strengthDivisor 1f, so damage is simply EffectiveStrength
                // itself (9 at baseline). Previously a flat 5 + STR/2.5 (~8 at the old STR of 8) -
                // same rough output, just expressed as a direct 1:1 ratio instead of a
                // base-plus-divisor formula, same new convention applied to every other beast's
                // own main attack below (see each one's own comment).
                new Move("Claw Crush", "A crushing clawed strike aimed to wound.",
                    apCost: 1, mpCost: 0, range: 1,
                    baseAccuracy: 0.90f, baseDamage: 0, damageType: DamageType.Sharp, strengthDivisor: 1f,
                    inflictsStatusEffect: "Bleeding", bleedRank: StatusRank.Weak, statusEffectChance: 0.15f),
                new Move("Leap", "Wings hidden along its back unfurl - not for flight, just for distance - as it pounces toward its mark.",
                    apCost: 2, mpCost: 3, range: 3,
                    baseAccuracy: 1f, baseDamage: 0, isLeap: true),
            },

            [Race.DuskRoachlinPriest] = new List<Move> { new Move(
                "Shadow Weave", "Weaves a strand of dark magic into the target from range.",
                apCost: 1, mpCost: 0, range: 3,
                baseAccuracy: 0.75f, baseDamage: 3, damageType: DamageType.Magical, intelligenceDivisor: 3.5f) },

            [Race.AetherfluffBeetle] = new List<Move> { new Move(
                "Nudge", "A harmless little bump - this creature isn't built to fight.",
                apCost: 1, mpCost: 0, range: 1,
                baseAccuracy: 0.90f, baseDamage: 0, damageType: DamageType.Blunt) },

            // 1:1 STR scaling, on request: each of these beasts' main attack is now baseDamage 0,
            // strengthDivisor 1f - damage is simply EffectiveStrength itself - with that STR
            // value set (see each one's own Units.Summons.* constructor) to whatever the attack
            // used to deal at the beast's OLD Strength, so moving to the new flat-ratio
            // convention didn't change actual combat output, just how it's expressed. Cinder
            // Dust (LanternmothCinderwing) and Shadow Weave (DuskRoachlinPriest) are deliberately
            // excluded - both scale with Intelligence, not Strength, as actual caster-type
            // attacks, not beasts throwing their own weight around; Nudge (AetherfluffBeetle) is
            // excluded too - 0 damage either way, "this creature isn't built to fight."
            [Race.Bearat] = new List<Move> { new Move(
                "Maul", "A heavy, clubbing swipe from a thick-hided brute.",
                apCost: 1, mpCost: 0, range: 1,
                baseAccuracy: 0.75f, baseDamage: 0, damageType: DamageType.Blunt, strengthDivisor: 1f) },

            [Race.MirebackSlogger] = new List<Move> { new Move(
                "Shove", "Puts its full armored weight behind a crushing push.",
                apCost: 1, mpCost: 0, range: 1,
                baseAccuracy: 0.70f, baseDamage: 0, damageType: DamageType.Blunt, strengthDivisor: 1f,
                knockbackBase: 1, knockbackDamagePerTile: 3) },

            [Race.LanternmothCinderwing] = new List<Move> { new Move(
                "Cinder Dust", "Scatters a glowing, faintly searing dust at range.",
                apCost: 1, mpCost: 0, range: 3,
                baseAccuracy: 0.80f, baseDamage: 2, damageType: DamageType.Magical, intelligenceDivisor: 3.2f) },

            [Race.Brambleboar] = new List<Move> { new Move(
                "Gore", "A thorn-backed charge that tears flesh and leaves it bleeding.",
                apCost: 1, mpCost: 0, range: 1,
                baseAccuracy: 0.75f, baseDamage: 0, damageType: DamageType.Sharp, strengthDivisor: 1f,
                inflictsStatusEffect: "Bleeding", bleedRank: StatusRank.Minor, statusEffectChance: 0.35f) },

            [Race.SiltfinMawpike] = new List<Move> { new Move(
                "Snap Bite", "A fast, jagged-jawed bite from the shallows.",
                apCost: 1, mpCost: 0, range: 1,
                baseAccuracy: 0.80f, baseDamage: 0, damageType: DamageType.Sharp, strengthDivisor: 1f) },

            [Race.GloamravenOssuary] = new List<Move> { new Move(
                "Precision Strike", "A carefully placed strike aimed at a vital spot.",
                apCost: 1, mpCost: 0, range: 1,
                baseAccuracy: 0.85f, baseDamage: 0, damageType: DamageType.Sharp, strengthDivisor: 1f,
                critChance: 0.30f, critMultiplier: 3f) },

            [Race.RootmossStonegloom] = new List<Move> { new Move(
                "Stone Slam", "A slow, immense blow with the weight of old stone behind it.",
                apCost: 1, mpCost: 0, range: 1,
                baseAccuracy: 0.70f, baseDamage: 0, damageType: DamageType.Blunt, strengthDivisor: 1f) },
        };

        /// <summary>
        /// Syphon - the third move of the Dusk Roachlin's kit, only usable while "Pinning" (see
        /// Game1.CastSyphon) - deliberately kept OUT of InnateMoves/GetInnateMoves (so it never
        /// shows in the generic Attack Menu or AI's GetUsableAttacks): its real target is always
        /// the specific unit the Roachlin is currently pinning (StatusEffect.PinnedTarget), never
        /// a free choice from "every enemy in range" the generic attack-target list would offer,
        /// and its lifesteal + stacking self-buff (see CastSyphon) aren't expressible as plain
        /// Move/AttackResolver data anyway - this Move's BaseDamage/StrengthDivisor/DamageType
        /// are the only fields CastSyphon actually reads (via the normal GetDamage(roachlin,
        /// target) formula, same as any other move); BaseAccuracy is 1f (no roll, same as a trap
        /// trigger). Deliberately only a MILD +STR/5 modifier on top of its flat base - on
        /// request, NOT the full 1:1 ratio Claw Crush and every other beast's main attack now
        /// use (see InnateMoves above) - Syphon is the Roachlin's secondary, Pinning-only move,
        /// not its main attack.
        /// </summary>
        public static readonly Move RoachlinSyphon = new Move(
            "Syphon", "A sharp proboscis, driven deep - what it drains, the Roachlin keeps.",
            apCost: 1, mpCost: 2, range: 1,
            baseAccuracy: 1f, baseDamage: 5, damageType: DamageType.Sharp, strengthDivisor: 5f);

        public static List<Move> GetInnateMoves(Race race) => InnateMoves[race];
    }
}
