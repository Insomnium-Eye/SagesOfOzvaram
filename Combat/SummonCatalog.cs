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

        public static readonly SummonCard AetherfluffBeetle = new SummonCard(
            "Aetherfluff Beetle",
            manaCost: 1,
            unitType: "Beast",
            description: "A curious little burrow-beast that generates 5 mana for every allied unit within 4 tiles at the start of each turn.",
            attack: 0, intelligence: 2, defense: 4, resistance: 4, accuracy: 5, evasion: 8, speed: 5,
            minDamage: 0, maxDamage: 0, hp: 10,
            artAssetPath: "imgs/Cards/Summons/GenericSummon_CardArt");

        public static readonly SummonCard DuskRoachlinPriest = new SummonCard(
            "Dusk Roachlin Priest",
            manaCost: 15,
            unitType: "Roachlin",
            description: "A quiet spiritual leader of the roachlin, with low attack, defense, and speed but strong intellect and resistance. At the start of its turn it heals a random friendly target; roachlin targets are healed for double the amount. It also casts Shadow Weave, dealing dark damage to a target.",
            attack: 4, intelligence: 11, defense: 5, resistance: 10, accuracy: 7, evasion: 6, speed: 5,
            minDamage: 3, maxDamage: 9, hp: 18,
            artAssetPath: "imgs/Cards/Summons/GenericSummon_CardArt");

        public static readonly SummonCard Bearat = new SummonCard(
            "Bearat",
            manaCost: 8,
            unitType: "Beast",
            description: "A burly, rodent-like beast with heavy frame, thick hide, and relentless pressure in close combat.",
            attack: 12, intelligence: 2, defense: 9, resistance: 4, accuracy: 7, evasion: 5, speed: 6,
            minDamage: 6, maxDamage: 16, hp: 24,
            artAssetPath: "imgs/Cards/Summons/GenericSummon_CardArt");

        public static readonly SummonCard MirebackSlogger = new SummonCard(
            "Mireback Slogger",
            manaCost: 7,
            unitType: "Beast",
            description: "A swamp-born brute with a broad armored back and crushing weight, built to hold the line and shove enemies off balance.",
            attack: 10, intelligence: 2, defense: 11, resistance: 4, accuracy: 6, evasion: 3, speed: 4,
            minDamage: 5, maxDamage: 14, hp: 26,
            artAssetPath: "imgs/Cards/Summons/GenericSummon_CardArt");

        public static readonly SummonCard LanternmothCinderwing = new SummonCard(
            "Lanternmoth Cinderwing",
            manaCost: 6,
            unitType: "Beast",
            description: "A luminous cave moth that glows with old magic, spreading small bursts of mana and calm to nearby allies.",
            attack: 3, intelligence: 8, defense: 4, resistance: 8, accuracy: 7, evasion: 7, speed: 7,
            minDamage: 2, maxDamage: 7, hp: 12,
            artAssetPath: "imgs/Cards/Summons/GenericSummon_CardArt");

        public static readonly SummonCard Brambleboar = new SummonCard(
            "Brambleboar",
            manaCost: 6,
            unitType: "Beast",
            description: "A dense, thorn-backed boar that tears through enemy lines and leaves bleeding wounds behind its charge.",
            attack: 11, intelligence: 2, defense: 7, resistance: 4, accuracy: 7, evasion: 5, speed: 6,
            minDamage: 5, maxDamage: 15, hp: 20,
            artAssetPath: "imgs/Cards/Summons/GenericSummon_CardArt");

        public static readonly SummonCard SiltfinMawpike = new SummonCard(
            "Siltfin Mawpike",
            manaCost: 5,
            unitType: "Beast",
            description: "A river-dwelling predator with a heavy jaw and jagged fin-spines, built to strike through mud and shallows.",
            attack: 9, intelligence: 3, defense: 6, resistance: 5, accuracy: 8, evasion: 6, speed: 8,
            minDamage: 4, maxDamage: 12, hp: 16,
            artAssetPath: "imgs/Cards/Summons/GenericSummon_CardArt");

        public static readonly SummonCard GloamravenOssuary = new SummonCard(
            "Gloamraven Ossuary",
            manaCost: 7,
            unitType: "Beast",
            description: "A skeletal-winged scavenger that stalks the edges of battle, marking enemies and cutting through the dark with precise attacks.",
            attack: 8, intelligence: 6, defense: 5, resistance: 6, accuracy: 10, evasion: 8, speed: 9,
            minDamage: 3, maxDamage: 11, hp: 14,
            artAssetPath: "imgs/Cards/Summons/GenericSummon_CardArt");

        public static readonly SummonCard RootmossStonegloom = new SummonCard(
            "Rootmoss Stonegloom",
            manaCost: 9,
            unitType: "Beast",
            description: "A moss-veiled guardian grown from ancient petrified roots and old stone, slow but impossible to move once it locks in.",
            attack: 9, intelligence: 3, defense: 12, resistance: 7, accuracy: 6, evasion: 3, speed: 3,
            minDamage: 5, maxDamage: 13, hp: 30,
            artAssetPath: "imgs/Cards/Summons/GenericSummon_CardArt");

        private static readonly List<SummonCard> AllCards = new List<SummonCard>
        {
            DuskRoachlin,
            AetherfluffBeetle,
            DuskRoachlinPriest,
            Bearat,
            MirebackSlogger,
            LanternmothCinderwing,
            Brambleboar,
            SiltfinMawpike,
            GloamravenOssuary,
            RootmossStonegloom
        };

        public static List<SummonCard> AllSummons => AllCards;
    }
}
