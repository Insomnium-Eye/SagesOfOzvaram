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
                apCost: 2, mpCost: 0, range: 1,
                baseAccuracy: 0.95f, baseDamage: 1, strengthDivisor: 5f,
                critChance: 0.01f, critMultiplier: 3f,
                knockbackBase: 1, knockbackDamagePerTile: 5,
                canInflictKnockdown: true, knockdownChanceIfStrongerStr: 0.80f, knockdownChanceOtherwise: 0.02f,
                knockdownStandUpApCost: 1),

            [Race.Lethios] = new Move(
                "Bite", "A vicious, hard-to-land bite - but when it connects, it's devastating and draws heavy blood.",
                apCost: 2, mpCost: 0, range: 1,
                baseAccuracy: 0.40f, baseDamage: 12, strengthDivisor: 2f,
                critChance: 0.25f, critMultiplier: 3f,
                inflictsStatusEffect: "Bleeding", bleedRank: StatusRank.Moderate),

            [Race.Vectium] = new Move(
                "Swipe", "A precise claw strike - far more reliable than a bite and quicker to find a vital spot, though it draws less blood.",
                apCost: 2, mpCost: 0, range: 1,
                baseAccuracy: 0.80f, baseDamage: 3, strengthDivisor: 1.5f,
                critChance: 0.40f, critMultiplier: 3f,
                inflictsStatusEffect: "Bleeding", bleedRank: StatusRank.Minor),

            // Summoned-creature races (not playable heroes) get their attack list the same way -
            // via their race's innate move - since summons don't have cards/weapons of their own.
            [Race.Roachlin] = new Move(
                "Roachlin Slash", "Slashes with claws.",
                apCost: 2, mpCost: 0, range: 1,
                baseAccuracy: 0.85f, baseDamage: 3, strengthDivisor: 5f),
        };

        public static Move GetInnateMove(Race race) => InnateMoves[race];
    }
}
