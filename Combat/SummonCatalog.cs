using System.Collections.Generic;

namespace SagesOfOzvaram.Combat
{
    /// <summary>
    /// Summon cards available. First-pass content - just Dusk Roachlin so far, pending the
    /// actual deck/draw and summon-to-battlefield mechanics described in the GDD (§4.1
    /// Card/Deck System). Numbers are first-pass placeholders pending a balance pass, same as
    /// everywhere else.
    /// </summary>
    public static class SummonCatalog
    {
        public static readonly SummonCard DuskRoachlin = new SummonCard(
            "Dusk Roachlin",
            manaCost: 5,
            unitType: "Roachlin",
            description: "A light-blind cave dweller that swarms prey in numbers, striking from darkness.",
            attack: 8, intelligence: 3, defense: 6, resistance: 4, accuracy: 7, evasion: 6, speed: 9,
            minDamage: 3, maxDamage: 12, hp: 12,
            artAssetPath: "imgs/Cards/Summons/DuskRoachlin_CardArt1");

        private static readonly List<SummonCard> AllCards = new List<SummonCard> { DuskRoachlin };

        public static List<SummonCard> AllSummons => AllCards;
    }
}
