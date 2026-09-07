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
        public string Name { get; }
        public string Description { get; }
        public HeroClass RequiredClass { get; }
        public Move Effect { get; }

        /// <summary>Path under Content/, no extension - e.g. "imgs/Cards/Spells/ArcaneBolt_CardArt".</summary>
        public string ArtAssetPath { get; }

        public SpellCard(string name, string description, HeroClass requiredClass, Move effect, string artAssetPath)
        {
            Name = name;
            Description = description;
            RequiredClass = requiredClass;
            Effect = effect;
            ArtAssetPath = artAssetPath;
        }

        /// <summary>The card's type line, e.g. "Spell - Sorcerer".</summary>
        public string ClassLabel => $"Spell - {RequiredClass}";
    }
}
