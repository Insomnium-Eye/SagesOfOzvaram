namespace SagesOfOzvaram.Combat
{
    /// <summary>
    /// A summonable creature card (see GDD Card/Deck System). Presentation data plus the raw
    /// stat block needed to render a Summon card - no actual summon-to-battlefield mechanics
    /// exist yet, this is just the card itself, the same stage the Spell cards started at.
    /// </summary>
    public class SummonCard
    {
        public string Name { get; }
        public int ManaCost { get; }
        public string UnitType { get; }
        public string Description { get; }

        public int Attack { get; }
        public int Intelligence { get; }
        public int Defense { get; }
        public int Resistance { get; }
        public int Accuracy { get; }
        public int Evasion { get; }
        public int Speed { get; }

        public int MinDamage { get; }
        public int MaxDamage { get; }
        public int HP { get; }

        /// <summary>Path under Content/, no extension - e.g. "imgs/Cards/Summons/DuskRoachlin_CardArt1".</summary>
        public string ArtAssetPath { get; }

        public SummonCard(string name, int manaCost, string unitType, string description,
            int attack, int intelligence, int defense, int resistance, int accuracy, int evasion, int speed,
            int minDamage, int maxDamage, int hp, string artAssetPath)
        {
            Name = name;
            ManaCost = manaCost;
            UnitType = unitType;
            Description = description;
            Attack = attack;
            Intelligence = intelligence;
            Defense = defense;
            Resistance = resistance;
            Accuracy = accuracy;
            Evasion = evasion;
            Speed = speed;
            MinDamage = minDamage;
            MaxDamage = maxDamage;
            HP = hp;
            ArtAssetPath = artAssetPath;
        }

        public string DamageRangeLabel => $"{MinDamage}-{MaxDamage}";
    }
}
