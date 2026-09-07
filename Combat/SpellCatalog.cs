using System.Collections.Generic;
using SagesOfOzvaram.Units;

namespace SagesOfOzvaram.Combat
{
    /// <summary>
    /// Spell cards available per class. First-pass content - just Arcane Bolt for the Sorcerer
    /// so far, pending the actual deck/draw system described in the GDD (§4.1 Card/Deck System).
    /// Numbers are first-pass placeholders pending a balance pass, same as everywhere else.
    /// </summary>
    public static class SpellCatalog
    {
        public static readonly SpellCard ArcaneBolt = new SpellCard(
            "Arcane Bolt",
            "Deals 1 + INT/2 arcane magic damage to a target within 5 tiles.",
            HeroClass.Sorcerer,
            new Move("Arcane Bolt", "A bolt of raw arcane force.",
                apCost: 2, mpCost: 2, range: 5, baseAccuracy: 0.85f, baseDamage: 1,
                intelligenceDivisor: 2f, damageType: DamageType.Magical),
            "imgs/Cards/Spells/ArcaneBolt_CardArt");

        private static readonly List<SpellCard> AllCards = new List<SpellCard> { ArcaneBolt };

        /// <summary>Every spell card a given class currently has access to.</summary>
        public static List<SpellCard> GetSpellsForClass(HeroClass heroClass)
        {
            var result = new List<SpellCard>();
            foreach (var card in AllCards)
            {
                if (card.RequiredClass == heroClass)
                    result.Add(card);
            }
            return result;
        }
    }
}
