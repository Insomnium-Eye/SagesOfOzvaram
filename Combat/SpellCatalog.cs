using System.Collections.Generic;
using SagesOfOzvaram.Units;

namespace SagesOfOzvaram.Combat
{
    /// <summary>
    /// Spell cards available. First-pass content: Arcane Bolt (Sorcerer-only) plus the full
    /// Generic Spells roster (GDD §4.1) - the kind of thing anyone could pick up without
    /// studying magic, so every one of these has RequiredClass null and shows up for every
    /// class's hand. Numbers are first-pass placeholders pending a balance pass, same as
    /// everywhere else. Several of these (Meditate's stacking/interrupt, Forced Sleep's wake
    /// condition, Conjure Potions' item creation, ...) describe mechanics with no execution
    /// system behind them yet - see Move.cs's non-attack fields for what IS captured as real
    /// data (heal amounts, granted AP, status duration) versus what's still prose-only.
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

        // --- Generic Spells: RequiredClass null, available to any class ---

        public static readonly SpellCard Meditate = new SpellCard(
            "Meditate",
            "Enter a meditative state: each turn, gain a stack of RES/DEF/INT/ACC/STR (up to 3 stacks). "
                + "Taking a hit for 10%+ of max HP interrupts it and clears all stacks. Choosing to end it "
                + "voluntarily at the start of your turn locks in the current buff for 3 more turns.",
            requiredClass: null,
            new Move("Meditate", "Center yourself and let your focus build.",
                apCost: 2, mpCost: 3, range: 0, baseAccuracy: 1f, baseDamage: 0,
                inflictsStatusEffect: "Meditating", statusDurationTurns: 3),
            "imgs/Cards/Spells/Meditate_CardArt");

        public static readonly SpellCard ForcedSleep = new SpellCard(
            "Forced Sleep",
            "Fall into a light sleep: immobilized, but heal 20% of max HP each turn while asleep. "
                + "Any single hit of 10+ damage wakes you immediately.",
            requiredClass: null,
            new Move("Forced Sleep", "Force yourself into a shallow, restorative sleep.",
                apCost: 2, mpCost: 4, range: 0, baseAccuracy: 1f, baseDamage: 0,
                healPercentMaxHP: 0.20f, inflictsStatusEffect: "Asleep"),
            "imgs/Cards/Spells/ForcedSleep_CardArt");

        public static readonly SpellCard ConjurePotions = new SpellCard(
            "Conjure Potions",
            "Places 1 minor HP potion, 1 minor MP potion, and 1 minor AP potion directly into the caster's inventory.",
            requiredClass: null,
            new Move("Conjure Potions", "Scrounge together a few minor potions from what's on hand.",
                apCost: 3, mpCost: 5, range: 0, baseAccuracy: 1f, baseDamage: 0),
            "imgs/Cards/Spells/ConjurePotions_CardArt");

        public static readonly SpellCard BandageWound = new SpellCard(
            "Bandage Wound",
            "Simple field first-aid: heals 8 HP and cures Bleeding. No training required - just steady hands and clean cloth.",
            requiredClass: null,
            new Move("Bandage Wound", "Wrap a wound tight to stop the bleeding.",
                apCost: 2, mpCost: 2, range: 0, baseAccuracy: 1f, baseDamage: 0,
                healFlat: 8),
            "imgs/Cards/Spells/BandageWound_CardArt");

        public static readonly SpellCard SecondWind = new SpellCard(
            "Second Wind",
            "A burst of adrenaline: gain 2 AP on your next turn.",
            requiredClass: null,
            new Move("Second Wind", "Push through the fatigue for one more push.",
                apCost: 1, mpCost: 3, range: 0, baseAccuracy: 1f, baseDamage: 0,
                grantedAP: 2),
            "imgs/Cards/Spells/SecondWind_CardArt");

        public static readonly SpellCard ThrowGrit = new SpellCard(
            "Throw Grit",
            "Flings a handful of dirt and sand into a target's eyes within 3 tiles, lowering their Accuracy for 2 turns.",
            requiredClass: null,
            new Move("Throw Grit", "A cheap trick anyone can pull off - a fistful of grit to the eyes.",
                apCost: 1, mpCost: 1, range: 3, baseAccuracy: 0.85f, baseDamage: 0,
                inflictsStatusEffect: "Blinded", statusEffectChance: 0.75f, statusDurationTurns: 2),
            "imgs/Cards/Spells/ThrowGrit_CardArt");

        public static readonly SpellCard Brace = new SpellCard(
            "Brace",
            "Plant your feet and tense every muscle: +DEF/+RES for 1 turn. No shield required, just instinct.",
            requiredClass: null,
            new Move("Brace", "Brace for the incoming blow.",
                apCost: 1, mpCost: 2, range: 0, baseAccuracy: 1f, baseDamage: 0,
                inflictsStatusEffect: "Braced", statusDurationTurns: 1),
            "imgs/Cards/Spells/Brace_CardArt");

        public static readonly SpellCard RallyCry = new SpellCard(
            "Rally Cry",
            "Shout encouragement to nearby allies: every adjacent ally gains +STR/+ACC for 2 turns.",
            requiredClass: null,
            new Move("Rally Cry", "A shout that puts steel back in the spine.",
                apCost: 2, mpCost: 3, range: 1, baseAccuracy: 1f, baseDamage: 0,
                hitsAllAdjacent: true, targetsAllies: true,
                inflictsStatusEffect: "Rallied", statusDurationTurns: 2),
            "imgs/Cards/Spells/RallyCry_CardArt");

        public static readonly SpellCard Trip = new SpellCard(
            "Trip",
            "A well-placed shove or hooked foot on an adjacent target - little damage, but a real chance to knock them down and Stun them.",
            requiredClass: null,
            new Move("Trip", "Simple physical trickery - anyone can throw someone off balance.",
                apCost: 1, mpCost: 1, range: 1, baseAccuracy: 0.75f, baseDamage: 0,
                inflictsStatusEffect: "Stunned", statusEffectChance: 0.6f),
            "imgs/Cards/Spells/Trip_CardArt");

        public static readonly SpellCard SteadyHands = new SpellCard(
            "Steady Hands",
            "A moment of focused concentration: +Accuracy for 2 turns. Safer than Meditate, but far less potent.",
            requiredClass: null,
            new Move("Steady Hands", "Breathe. Focus. Steady your hands.",
                apCost: 1, mpCost: 2, range: 0, baseAccuracy: 1f, baseDamage: 0,
                inflictsStatusEffect: "SteadyHands", statusDurationTurns: 2),
            "imgs/Cards/Spells/SteadyHands_CardArt");

        // --- Sorcerer Spells: burst arcane/elemental damage plus magical utility ---

        public static readonly SpellCard ManaShield = new SpellCard(
            "Mana Shield",
            "Grants a shield that redirects incoming damage to MP instead of HP. Toggle it off on your own "
                + "turn at will; it breaks automatically once your MP is depleted. Costs 5 MP to cast.",
            HeroClass.Sorcerer,
            new Move("Mana Shield", "Wrap yourself in a ward of raw mana.",
                apCost: 1, mpCost: 5, range: 0, baseAccuracy: 1f, baseDamage: 0,
                inflictsStatusEffect: "Mana Shielded"),
            "imgs/Cards/Spells/ManaShield_CardArt");

        public static readonly SpellCard Blink = new SpellCard(
            "Blink",
            "Teleport up to 3 tiles in an instant - a burst of mobility with no attack attached.",
            HeroClass.Sorcerer,
            new Move("Blink", "Fold the space between here and there.",
                apCost: 1, mpCost: 3, range: 3, baseAccuracy: 1f, baseDamage: 0,
                targetsAllies: true),
            "imgs/Cards/Spells/Blink_CardArt");

        public static readonly SpellCard Flames = new SpellCard(
            "Flames",
            "Spews flames in an arc, dealing moderate magic damage to every adjacent enemy, with a small chance to set them Burning.",
            HeroClass.Sorcerer,
            new Move("Flames", "A gout of fire sweeps across everything nearby.",
                apCost: 3, mpCost: 4, range: 1, baseAccuracy: 0.85f, baseDamage: 6,
                intelligenceDivisor: 3f, damageType: DamageType.Magical,
                hitsAllAdjacent: true, inflictsStatusEffect: "Burning", statusEffectChance: 0.20f),
            "imgs/Cards/Spells/Flames_CardArt");

        public static readonly SpellCard IceSpike = new SpellCard(
            "Ice Spike",
            "Shoots a spike of ice that impales the first unit in its path, with a chance to inflict Frostbite.",
            HeroClass.Sorcerer,
            new Move("Ice Spike", "A shard of ice, hard and fast enough to punch straight through.",
                apCost: 2, mpCost: 3, range: 4, baseAccuracy: 0.85f, baseDamage: 7,
                intelligenceDivisor: 2.5f, damageType: DamageType.Magical,
                inflictsStatusEffect: "Frostbite", statusEffectChance: 0.25f),
            "imgs/Cards/Spells/IceSpike_CardArt");

        public static readonly SpellCard ChainLightning = new SpellCard(
            "Chain Lightning",
            "Lightning arcs to the target, then jumps to up to 2 more nearby enemies at reduced range, each with a small chance to Shock.",
            HeroClass.Sorcerer,
            new Move("Chain Lightning", "Lightning that doesn't stop at just one target.",
                apCost: 3, mpCost: 5, range: 4, baseAccuracy: 0.80f, baseDamage: 6,
                intelligenceDivisor: 3f, damageType: DamageType.Magical,
                inflictsStatusEffect: "Shocked", statusEffectChance: 0.15f),
            "imgs/Cards/Spells/ChainLightning_CardArt");

        public static readonly SpellCard EnchantWeapon = new SpellCard(
            "Enchant Weapon",
            "Enchants the caster's or an adjacent ally's weapon, adding bonus magic damage to its hits based on the wielder's Intelligence, until the encounter ends.",
            HeroClass.Sorcerer,
            new Move("Enchant Weapon", "Weave arcane force into cold steel.",
                apCost: 2, mpCost: 4, range: 1, baseAccuracy: 1f, baseDamage: 0,
                targetsAllies: true),
            "imgs/Cards/Spells/EnchantWeapon_CardArt");

        public static readonly SpellCard Fireball = new SpellCard(
            "Fireball",
            "A devastating orb of fire hurled at a distant target, exploding on impact - the sorcerer's signature high-damage nuke.",
            HeroClass.Sorcerer,
            new Move("Fireball", "Fire given form and hurled with intent.",
                apCost: 4, mpCost: 6, range: 5, baseAccuracy: 0.80f, baseDamage: 12,
                intelligenceDivisor: 2f, damageType: DamageType.Magical,
                inflictsStatusEffect: "Burning", statusEffectChance: 0.25f),
            "imgs/Cards/Spells/Fireball_CardArt");

        public static readonly SpellCard FrostNova = new SpellCard(
            "Frost Nova",
            "A burst of frost erupts from the caster, damaging and chilling every adjacent enemy.",
            HeroClass.Sorcerer,
            new Move("Frost Nova", "The air itself freezes outward in a ring.",
                apCost: 3, mpCost: 4, range: 0, baseAccuracy: 0.90f, baseDamage: 5,
                intelligenceDivisor: 3f, damageType: DamageType.Magical,
                hitsAllAdjacent: true, inflictsStatusEffect: "Frostbite", statusEffectChance: 0.30f),
            "imgs/Cards/Spells/FrostNova_CardArt");

        public static readonly SpellCard ArcaneShield = new SpellCard(
            "Arcane Shield",
            "A shimmering ward of arcane force: +DEF/+RES for 2 turns. Stronger than Brace, but costs Mana to conjure.",
            HeroClass.Sorcerer,
            new Move("Arcane Shield", "Bend light and force into a barrier.",
                apCost: 1, mpCost: 3, range: 0, baseAccuracy: 1f, baseDamage: 0,
                targetsAllies: true, inflictsStatusEffect: "Arcane Shielded", statusDurationTurns: 2),
            "imgs/Cards/Spells/ArcaneShield_CardArt");

        public static readonly SpellCard Curse = new SpellCard(
            "Curse",
            "Lays a curse on a distant target, lowering their Strength and Accuracy for 3 turns.",
            HeroClass.Sorcerer,
            new Move("Curse", "Whisper a hex that saps strength and steadiness alike.",
                apCost: 2, mpCost: 3, range: 4, baseAccuracy: 0.85f, baseDamage: 0,
                inflictsStatusEffect: "Cursed", statusEffectChance: 0.80f, statusDurationTurns: 3),
            "imgs/Cards/Spells/Curse_CardArt");

        public static readonly SpellCard TeleportStrike = new SpellCard(
            "Teleport Strike",
            "Blink directly beside a distant target and strike them with arcane force, closing the gap instantly.",
            HeroClass.Sorcerer,
            new Move("Teleport Strike", "Here, then there, then already swinging.",
                apCost: 2, mpCost: 4, range: 3, baseAccuracy: 0.85f, baseDamage: 5,
                intelligenceDivisor: 3f, damageType: DamageType.Magical,
                attackerAdvanceTiles: 3),
            "imgs/Cards/Spells/TeleportStrike_CardArt");

        public static readonly SpellCard ManaBurn = new SpellCard(
            "Mana Burn",
            "Sears a target's mana reserves, dealing bonus magic damage - especially effective against other casters.",
            HeroClass.Sorcerer,
            new Move("Mana Burn", "Set a target's own reserves alight.",
                apCost: 2, mpCost: 3, range: 4, baseAccuracy: 0.85f, baseDamage: 4,
                intelligenceDivisor: 4f, damageType: DamageType.Magical),
            "imgs/Cards/Spells/ManaBurn_CardArt");

        public static readonly SpellCard MagicMissile = new SpellCard(
            "Magic Missile",
            "A cheap, reliable bolt of raw magic - low cost, low damage, but rarely misses.",
            HeroClass.Sorcerer,
            new Move("Magic Missile", "The first spell every apprentice learns to never miss.",
                apCost: 1, mpCost: 1, range: 4, baseAccuracy: 0.95f, baseDamage: 3,
                intelligenceDivisor: 4f, damageType: DamageType.Magical),
            "imgs/Cards/Spells/MagicMissile_CardArt");

        public static readonly SpellCard ArcaneOrb = new SpellCard(
            "Arcane Orb",
            "A slow-forming orb of pure arcane energy - expensive and telegraphed, but hits devastatingly hard, with a real chance to crit.",
            HeroClass.Sorcerer,
            new Move("Arcane Orb", "Raw arcane power, given time to gather before it's unleashed.",
                apCost: 4, mpCost: 7, range: 5, baseAccuracy: 0.75f, baseDamage: 16,
                intelligenceDivisor: 1.5f, damageType: DamageType.Magical,
                critChance: 0.15f, critMultiplier: 2.5f),
            "imgs/Cards/Spells/ArcaneOrb_CardArt");

        private static readonly List<SpellCard> AllCards = new List<SpellCard>
        {
            ArcaneBolt,
            Meditate, ForcedSleep, ConjurePotions, BandageWound, SecondWind,
            ThrowGrit, Brace, RallyCry, Trip, SteadyHands,
            ManaShield, Blink, Flames, IceSpike, ChainLightning, EnchantWeapon,
            Fireball, FrostNova, ArcaneShield, Curse, TeleportStrike, ManaBurn, MagicMissile, ArcaneOrb,
        };

        /// <summary>Every spell card a given class currently has access to: its class-specific cards plus every Generic (RequiredClass null) card.</summary>
        public static List<SpellCard> GetSpellsForClass(HeroClass heroClass)
        {
            var result = new List<SpellCard>();
            foreach (var card in AllCards)
            {
                if (card.RequiredClass == null || card.RequiredClass == heroClass)
                    result.Add(card);
            }
            return result;
        }
    }
}
