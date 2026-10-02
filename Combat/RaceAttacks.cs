using System.Collections.Generic;
using SagesOfOzvaram.Units;

namespace SagesOfOzvaram.Combat
{
    /// <summary>
    /// Every unit gets its race's innate attack for free, always available regardless of
    /// equipment; weapons grant additional attacks on top of this one.
    /// </summary>
    public static class RaceAttacks
    {
        private static readonly Dictionary<Race, Move> InnateMoves = new Dictionary<Race, Move>
        {
            [Race.Human] = new Move(
                "Punch", "A quick jab - very little behind it, but reliable, and a strong enough puncher can put the target flat on their back.",
                apCost: 1, mpCost: 0, range: 1,
                baseAccuracy: 0.95f, baseDamage: 1, damageType: DamageType.Blunt, strengthDivisor: 5f,
                critChance: 0.01f, critMultiplier: 3f,
                knockbackBase: 1, knockbackDamagePerTile: 5,
                canInflictKnockdown: true, knockdownChanceIfStrongerStr: 0.80f, knockdownChanceOtherwise: 0.02f,
                knockdownStandUpApCost: 1),

            [Race.Lethios] = new Move(
                "Bite", "A vicious, hard-to-land bite - but when it connects, it's devastating and draws heavy blood.",
                apCost: 1, mpCost: 0, range: 1,
                baseAccuracy: 0.40f, baseDamage: 12, damageType: DamageType.Sharp, strengthDivisor: 2f,
                critChance: 0.25f, critMultiplier: 3f,
                inflictsStatusEffect: "Bleeding", bleedRank: StatusRank.Moderate),

            [Race.Vectium] = new Move(
                "Swipe", "A precise claw strike - far more reliable than a bite and quicker to find a vital spot, though it draws less blood.",
                apCost: 1, mpCost: 0, range: 1,
                baseAccuracy: 0.80f, baseDamage: 3, damageType: DamageType.Sharp, strengthDivisor: 1.5f,
                critChance: 0.40f, critMultiplier: 3f,
                inflictsStatusEffect: "Bleeding", bleedRank: StatusRank.Minor),

            // Summoned-creature races (not playable heroes) get their attack list the same way -
            // via their race's innate move - since summons don't have cards/weapons of their own.
            [Race.Roachlin] = new Move(
                "Roachlin Slash", "Slashes with claws.",
                apCost: 1, mpCost: 0, range: 1,
                baseAccuracy: 0.85f, baseDamage: 3, damageType: DamageType.Sharp, strengthDivisor: 5f),

            [Race.DuskRoachlinPriest] = new Move(
                "Shadow Weave", "Weaves a strand of dark magic into the target from range.",
                apCost: 1, mpCost: 0, range: 3,
                baseAccuracy: 0.75f, baseDamage: 3, damageType: DamageType.Magical, intelligenceDivisor: 3.5f),

            [Race.AetherfluffBeetle] = new Move(
                "Nudge", "A harmless little bump - this creature isn't built to fight.",
                apCost: 1, mpCost: 0, range: 1,
                baseAccuracy: 0.90f, baseDamage: 0, damageType: DamageType.Blunt),

            [Race.Bearat] = new Move(
                "Maul", "A heavy, clubbing swipe from a thick-hided brute.",
                apCost: 1, mpCost: 0, range: 1,
                baseAccuracy: 0.75f, baseDamage: 6, damageType: DamageType.Blunt, strengthDivisor: 2.4f),

            [Race.MirebackSlogger] = new Move(
                "Shove", "Puts its full armored weight behind a crushing push.",
                apCost: 1, mpCost: 0, range: 1,
                baseAccuracy: 0.70f, baseDamage: 5, damageType: DamageType.Blunt, strengthDivisor: 2.2f,
                knockbackBase: 1, knockbackDamagePerTile: 3),

            [Race.LanternmothCinderwing] = new Move(
                "Cinder Dust", "Scatters a glowing, faintly searing dust at range.",
                apCost: 1, mpCost: 0, range: 3,
                baseAccuracy: 0.80f, baseDamage: 2, damageType: DamageType.Magical, intelligenceDivisor: 3.2f),

            [Race.Brambleboar] = new Move(
                "Gore", "A thorn-backed charge that tears flesh and leaves it bleeding.",
                apCost: 1, mpCost: 0, range: 1,
                baseAccuracy: 0.75f, baseDamage: 5, damageType: DamageType.Sharp, strengthDivisor: 2.2f,
                inflictsStatusEffect: "Bleeding", bleedRank: StatusRank.Minor, statusEffectChance: 0.35f),

            [Race.SiltfinMawpike] = new Move(
                "Snap Bite", "A fast, jagged-jawed bite from the shallows.",
                apCost: 1, mpCost: 0, range: 1,
                baseAccuracy: 0.80f, baseDamage: 4, damageType: DamageType.Sharp, strengthDivisor: 2.25f),

            [Race.GloamravenOssuary] = new Move(
                "Precision Strike", "A carefully placed strike aimed at a vital spot.",
                apCost: 1, mpCost: 0, range: 1,
                baseAccuracy: 0.85f, baseDamage: 3, damageType: DamageType.Sharp, strengthDivisor: 2f,
                critChance: 0.30f, critMultiplier: 3f),

            [Race.RootmossStonegloom] = new Move(
                "Stone Slam", "A slow, immense blow with the weight of old stone behind it.",
                apCost: 1, mpCost: 0, range: 1,
                baseAccuracy: 0.70f, baseDamage: 5, damageType: DamageType.Blunt, strengthDivisor: 2.25f),
        };

        public static Move GetInnateMove(Race race) => InnateMoves[race];
    }
}
