using SagesOfOzvaram.Units;

namespace SagesOfOzvaram.Combat
{
    /// <summary>
    /// A spell drawn from a class's card deck (see GDD Card/Deck System). Wraps a Move for its
    /// damage/accuracy/range formulas, so it reuses the exact same stat-scaling math as weapon
    /// attacks, plus the presentation data (art, description) needed to render it as a card.
    /// </summary>
    public class SpellCard
    {
        /// <summary>Stable numeric ID for this card, e.g. for the dev console's "add card &lt;ID&gt;" - assigned once by SpellCatalog in declaration order, not set here.</summary>
        public int Id { get; internal set; }

        public string Name { get; }
        public string Description { get; }

        /// <summary>Null means Generic - any class can cast it (see GDD Generic Spells).</summary>
        public HeroClass? RequiredClass { get; }
        public Move Effect { get; }

        /// <summary>Path under Content/, no extension - e.g. "imgs/Cards/Spells/ArcaneBolt_CardArt".</summary>
        public string ArtAssetPath { get; }

        /// <summary>True once this card has actually been play-tested - see Combat.TestedCards (the real tracking lives there, by Name; this just reads it). Drives Test Mode's deck ordering (Untested cards first).</summary>
        public bool Tested => TestedCards.Names.Contains(Name);

        public SpellCard(string name, string description, HeroClass? requiredClass, Move effect, string artAssetPath)
        {
            Name = name;
            Description = description;
            RequiredClass = requiredClass;
            Effect = effect;
            ArtAssetPath = artAssetPath;
        }

        /// <summary>The card's type line, e.g. "Spell - Sorcerer", or "Spell - Generic" for a class-less spell.</summary>
        public string ClassLabel => $"Spell - {(RequiredClass.HasValue ? RequiredClass.Value.ToString() : "Generic")}";
    }
}
