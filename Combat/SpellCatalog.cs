using System.Collections.Generic;
using SagesOfOzvaram.Units;

namespace SagesOfOzvaram.Combat
{
    /// <summary>
    /// Spell cards available: the full 15-card Sorcerer and Warrior rosters, plus the Generic
    /// Spells roster (GDD §4.1) - the kind of thing anyone could pick up without studying magic,
    /// so every one of those has RequiredClass null and shows up for every class's hand. Numbers
    /// are first-pass placeholders pending a balance pass, same as everywhere else. Several of
    /// these (Meditate's stacking/interrupt, Forced Sleep's wake condition, Conjure Potions'
    /// item creation, Execute's low-HP bonus, Summon Blade/Shield's granted equipment, ...)
    /// describe mechanics with no execution system behind them yet - see Move.cs's non-attack
    /// fields for what IS captured as real data (heal amounts, granted AP, status duration)
    /// versus what's still prose-only.
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

        // --- Warrior Spells: buffs (self/allies), summoned weapons/shields/armor, weapon techniques ---

        public static readonly SpellCard Taunt = new SpellCard(
            "Taunt",
            "Taunts every enemy within 3 tiles, sharply increasing their aggression toward the warrior.",
            HeroClass.Warrior,
            new Move("Taunt", "Make yourself impossible to ignore.",
                apCost: 1, mpCost: 2, range: 3, baseAccuracy: 1f, baseDamage: 0,
                inflictsStatusEffect: "Taunting", statusDurationTurns: 2),
            "imgs/Cards/Spells/Taunt_CardArt");

        public static readonly SpellCard Bodyguard = new SpellCard(
            "Bodyguard",
            "For 1 turn, redirects damage aimed at any ally within 3 tiles onto the warrior instead.",
            HeroClass.Warrior,
            new Move("Bodyguard", "Step between your allies and harm.",
                apCost: 2, mpCost: 3, range: 3, baseAccuracy: 1f, baseDamage: 0,
                targetsAllies: true, inflictsStatusEffect: "Bodyguarding", statusDurationTurns: 1),
            "imgs/Cards/Spells/Bodyguard_CardArt");

        public static readonly SpellCard Rage = new SpellCard(
            "Rage",
            "For 3 turns, every hit you take grants +2 damage and +2 DEF, stacking for the rest of the fight.",
            HeroClass.Warrior,
            new Move("Rage", "Let every wound feed your fury instead of slowing you down.",
                apCost: 2, mpCost: 3, range: 0, baseAccuracy: 1f, baseDamage: 0,
                inflictsStatusEffect: "Enraged", statusDurationTurns: 3),
            "imgs/Cards/Spells/Rage_CardArt");

        public static readonly SpellCard MightySlash = new SpellCard(
            "Mighty Slash",
            "Channel extra force into whatever weapon is equipped, dealing 150% of its normal damage.",
            HeroClass.Warrior,
            new Move("Mighty Slash", "One devastating swing, whatever's in hand.",
                apCost: 2, mpCost: 3, range: 1, baseAccuracy: 0.90f, baseDamage: 4,
                strengthDivisor: 2f, damageType: DamageType.Physical),
            "imgs/Cards/Spells/MightySlash_CardArt");

        public static readonly SpellCard SummonBlade = new SpellCard(
            "Summon Blade",
            "Conjures a spectral blade into the caster's hand for the rest of the encounter, granting a bonus STR-scaled attack even if unarmed.",
            HeroClass.Warrior,
            new Move("Summon Blade", "Will a weapon into being from raw discipline.",
                apCost: 2, mpCost: 4, range: 0, baseAccuracy: 1f, baseDamage: 0),
            "imgs/Cards/Spells/SummonBlade_CardArt");

        public static readonly SpellCard SummonShield = new SpellCard(
            "Summon Shield",
            "Conjures a spectral shield, granting the caster's Guard stance a bonus as if wielding a real shield.",
            HeroClass.Warrior,
            new Move("Summon Shield", "A shield of pure discipline, held in an empty hand.",
                apCost: 2, mpCost: 4, range: 0, baseAccuracy: 1f, baseDamage: 0),
            "imgs/Cards/Spells/SummonShield_CardArt");

        public static readonly SpellCard ConjureArmor = new SpellCard(
            "Conjure Armor",
            "Conjures a temporary set of magical armor: +DEF/+RES for 3 turns. A placeholder for the real armor system planned later.",
            HeroClass.Warrior,
            new Move("Conjure Armor", "Plate yourself in force made solid.",
                apCost: 3, mpCost: 5, range: 0, baseAccuracy: 1f, baseDamage: 0,
                inflictsStatusEffect: "Conjured Armor", statusDurationTurns: 3),
            "imgs/Cards/Spells/ConjureArmor_CardArt");

        public static readonly SpellCard ShieldWall = new SpellCard(
            "Shield Wall",
            "Every adjacent ally gains +DEF for 2 turns.",
            HeroClass.Warrior,
            new Move("Shield Wall", "Form a wall nothing gets through.",
                apCost: 2, mpCost: 3, range: 1, baseAccuracy: 1f, baseDamage: 0,
                hitsAllAdjacent: true, targetsAllies: true,
                inflictsStatusEffect: "Shield Walled", statusDurationTurns: 2),
            "imgs/Cards/Spells/ShieldWall_CardArt");

        public static readonly SpellCard Whirlwind = new SpellCard(
            "Whirlwind",
            "Spin with weapon extended, striking every adjacent enemy at once.",
            HeroClass.Warrior,
            new Move("Whirlwind", "A spinning arc of steel.",
                apCost: 3, mpCost: 3, range: 1, baseAccuracy: 0.85f, baseDamage: 6,
                strengthDivisor: 3f, damageType: DamageType.Physical, hitsAllAdjacent: true),
            "imgs/Cards/Spells/Whirlwind_CardArt");

        public static readonly SpellCard Execute = new SpellCard(
            "Execute",
            "A finishing blow - deals double damage against targets below 25% HP.",
            HeroClass.Warrior,
            new Move("Execute", "End it.",
                apCost: 2, mpCost: 3, range: 1, baseAccuracy: 0.85f, baseDamage: 5,
                strengthDivisor: 3f, damageType: DamageType.Physical),
            "imgs/Cards/Spells/Execute_CardArt");

        public static readonly SpellCard Charge = new SpellCard(
            "Charge",
            "Charge across the battlefield and slam into a distant target, closing the gap instantly.",
            HeroClass.Warrior,
            new Move("Charge", "Close the distance before they can react.",
                apCost: 2, mpCost: 3, range: 3, baseAccuracy: 0.85f, baseDamage: 5,
                strengthDivisor: 3f, damageType: DamageType.Physical, attackerAdvanceTiles: 3),
            "imgs/Cards/Spells/Charge_CardArt");

        public static readonly SpellCard BattleCry = new SpellCard(
            "Battle Cry",
            "A roar that hardens resolve: +STR for 2 turns.",
            HeroClass.Warrior,
            new Move("Battle Cry", "A roar that steels every muscle for what's next.",
                apCost: 1, mpCost: 2, range: 0, baseAccuracy: 1f, baseDamage: 0,
                inflictsStatusEffect: "Battle Cry", statusDurationTurns: 2),
            "imgs/Cards/Spells/BattleCry_CardArt");

        public static readonly SpellCard CounterStance = new SpellCard(
            "Counter Stance",
            "Brace for the next attack: the first hit you take this turn is reflected back at the attacker for a portion of the damage dealt.",
            HeroClass.Warrior,
            new Move("Counter Stance", "Let them swing first - it'll be their last mistake.",
                apCost: 1, mpCost: 3, range: 0, baseAccuracy: 1f, baseDamage: 0,
                inflictsStatusEffect: "Countering", statusDurationTurns: 1),
            "imgs/Cards/Spells/CounterStance_CardArt");

        public static readonly SpellCard Disarm = new SpellCard(
            "Disarm",
            "Knocks a target's weapon off-balance, lowering their damage and Accuracy for 2 turns.",
            HeroClass.Warrior,
            new Move("Disarm", "A precise strike aimed at the grip, not the target.",
                apCost: 2, mpCost: 2, range: 1, baseAccuracy: 0.75f, baseDamage: 0,
                inflictsStatusEffect: "Disarmed", statusEffectChance: 0.70f, statusDurationTurns: 2),
            "imgs/Cards/Spells/Disarm_CardArt");

        public static readonly SpellCard IronWill = new SpellCard(
            "Iron Will",
            "Steels the mind against fear: +RES for 3 turns, and resists Stun while active.",
            HeroClass.Warrior,
            new Move("Iron Will", "Nothing left that can shake you.",
                apCost: 1, mpCost: 2, range: 0, baseAccuracy: 1f, baseDamage: 0,
                inflictsStatusEffect: "Iron Will", statusDurationTurns: 3),
            "imgs/Cards/Spells/IronWill_CardArt");

        // --- Hunter Spells: ranged damage, traps, and beast-taming ---

        public static readonly SpellCard PlaceTrap = new SpellCard(
            "Place Trap",
            "Places an invisible trap on a tile within 3 tiles. Triggers when any enemy or neutral unit passes "
                + "through or lands on it, snaring and damaging them. Flying units are immune.",
            HeroClass.Hunter,
            new Move("Place Trap", "A trap only the one who set it will ever see.",
                apCost: 2, mpCost: 3, range: 3, baseAccuracy: 1f, baseDamage: 6,
                strengthDivisor: 4f, damageType: DamageType.Physical,
                inflictsStatusEffect: "Snared", statusEffectChance: 0.85f, statusDurationTurns: 2),
            "imgs/Cards/Spells/PlaceTrap_CardArt");

        public static readonly SpellCard TameBeast = new SpellCard(
            "Tame Beast",
            "Attempts to tame a neutral beast of the hunter's level or lower, placing it under the hunter's control.",
            HeroClass.Hunter,
            new Move("Tame Beast", "Not every bond needs words.",
                apCost: 3, mpCost: 5, range: 3, baseAccuracy: 0.70f, baseDamage: 0),
            "imgs/Cards/Spells/TameBeast_CardArt");

        public static readonly SpellCard EagleEye = new SpellCard(
            "Eagle Eye",
            "Sharpens focus to a hawk's precision: +10 Accuracy for 3 turns.",
            HeroClass.Hunter,
            new Move("Eagle Eye", "See the shot before you take it.",
                apCost: 1, mpCost: 2, range: 0, baseAccuracy: 1f, baseDamage: 0,
                inflictsStatusEffect: "Eagle Eye", statusDurationTurns: 3),
            "imgs/Cards/Spells/EagleEye_CardArt");

        public static readonly SpellCard MultiShot = new SpellCard(
            "Multi-Shot",
            "Fires three arrows at once, striking the primary target plus up to 2 more enemies within range.",
            HeroClass.Hunter,
            new Move("Multi-Shot", "Why loose one arrow when you can loose three.",
                apCost: 2, mpCost: 3, range: 5, baseAccuracy: 0.80f, baseDamage: 5,
                strengthDivisor: 4f, damageType: DamageType.Physical),
            "imgs/Cards/Spells/MultiShot_CardArt");

        public static readonly SpellCard PiercingShot = new SpellCard(
            "Piercing Shot",
            "A single arrow shot hard enough to punch through the target and anyone standing behind them in a line.",
            HeroClass.Hunter,
            new Move("Piercing Shot", "One line, drawn straight through everything in it.",
                apCost: 2, mpCost: 3, range: 5, baseAccuracy: 0.85f, baseDamage: 8,
                strengthDivisor: 3.5f, damageType: DamageType.Physical),
            "imgs/Cards/Spells/PiercingShot_CardArt");

        public static readonly SpellCard ExplosiveTrap = new SpellCard(
            "Explosive Trap",
            "Places a hidden trap within 3 tiles that detonates when triggered, dealing heavy damage to the "
                + "triggering unit and anyone adjacent to it. No snare - it's built to hurt, not hold.",
            HeroClass.Hunter,
            new Move("Explosive Trap", "Not every trap needs to be subtle.",
                apCost: 3, mpCost: 4, range: 3, baseAccuracy: 1f, baseDamage: 10,
                strengthDivisor: 3f, damageType: DamageType.Physical),
            "imgs/Cards/Spells/ExplosiveTrap_CardArt");

        public static readonly SpellCard Camouflage = new SpellCard(
            "Camouflage",
            "Blend into the surroundings: +Evasion for 3 turns.",
            HeroClass.Hunter,
            new Move("Camouflage", "Become part of the terrain.",
                apCost: 1, mpCost: 2, range: 0, baseAccuracy: 1f, baseDamage: 0,
                inflictsStatusEffect: "Camouflaged", statusDurationTurns: 3),
            "imgs/Cards/Spells/Camouflage_CardArt");

        public static readonly SpellCard HuntersMark = new SpellCard(
            "Hunter's Mark",
            "Marks a distant target - they take bonus damage from all sources for 3 turns.",
            HeroClass.Hunter,
            new Move("Hunter's Mark", "Once marked, there's nowhere left to hide.",
                apCost: 1, mpCost: 2, range: 5, baseAccuracy: 0.90f, baseDamage: 0,
                inflictsStatusEffect: "Marked", statusEffectChance: 0.90f, statusDurationTurns: 3),
            "imgs/Cards/Spells/HuntersMark_CardArt");

        public static readonly SpellCard Disengage = new SpellCard(
            "Disengage",
            "Leap backward out of melee range without provoking an opportunity attack.",
            HeroClass.Hunter,
            new Move("Disengage", "The best shot is the one you live to take.",
                apCost: 1, mpCost: 2, range: 3, baseAccuracy: 1f, baseDamage: 0,
                targetsAllies: true),
            "imgs/Cards/Spells/Disengage_CardArt");

        public static readonly SpellCard BeastBond = new SpellCard(
            "Beast Bond",
            "Strengthens the bond with a tamed beast companion: +STR/+Speed for 3 turns. Requires an active Tame Beast.",
            HeroClass.Hunter,
            new Move("Beast Bond", "A word and a gesture your beast already knows.",
                apCost: 1, mpCost: 3, range: 3, baseAccuracy: 1f, baseDamage: 0,
                targetsAllies: true, inflictsStatusEffect: "Bonded", statusDurationTurns: 3),
            "imgs/Cards/Spells/BeastBond_CardArt");

        public static readonly SpellCard RainOfArrows = new SpellCard(
            "Rain of Arrows",
            "A volley of arrows arcs down over a target area, striking everyone caught beneath it.",
            HeroClass.Hunter,
            new Move("Rain of Arrows", "The sky itself turns hostile.",
                apCost: 3, mpCost: 4, range: 5, baseAccuracy: 0.75f, baseDamage: 6,
                strengthDivisor: 4f, damageType: DamageType.Physical),
            "imgs/Cards/Spells/RainOfArrows_CardArt");

        public static readonly SpellCard PoisonShot = new SpellCard(
            "Poison Shot",
            "A coated arrowhead - light damage on its own, but leaves a lingering Poison.",
            HeroClass.Hunter,
            new Move("Poison Shot", "The wound is the least of their worries.",
                apCost: 2, mpCost: 2, range: 5, baseAccuracy: 0.85f, baseDamage: 4,
                strengthDivisor: 5f, damageType: DamageType.Physical,
                inflictsStatusEffect: "Poisoned", statusEffectChance: 0.65f, statusDurationTurns: 3),
            "imgs/Cards/Spells/PoisonShot_CardArt");

        public static readonly SpellCard NetShot = new SpellCard(
            "Net Shot",
            "Fires a weighted net instead of an arrowhead - minimal damage, but a strong chance to root the target in place.",
            HeroClass.Hunter,
            new Move("Net Shot", "Sometimes the point isn't to hurt them.",
                apCost: 2, mpCost: 2, range: 4, baseAccuracy: 0.85f, baseDamage: 2,
                strengthDivisor: 6f, damageType: DamageType.Physical,
                inflictsStatusEffect: "Snared", statusEffectChance: 0.80f, statusDurationTurns: 2),
            "imgs/Cards/Spells/NetShot_CardArt");

        public static readonly SpellCard Track = new SpellCard(
            "Track",
            "Reveals every enemy within a wide radius for 3 turns, cutting through fog of war and camouflage alike.",
            HeroClass.Hunter,
            new Move("Track", "Every trail tells a story, if you know how to read it.",
                apCost: 1, mpCost: 2, range: 0, baseAccuracy: 1f, baseDamage: 0,
                inflictsStatusEffect: "Tracking", statusDurationTurns: 3),
            "imgs/Cards/Spells/Track_CardArt");

        public static readonly SpellCard VitalShot = new SpellCard(
            "Vital Shot",
            "A called shot aimed at a vital point - unremarkable on a graze, brutal on a solid hit.",
            HeroClass.Hunter,
            new Move("Vital Shot", "One shot, exactly where it counts.",
                apCost: 2, mpCost: 3, range: 5, baseAccuracy: 0.80f, baseDamage: 6,
                strengthDivisor: 3f, damageType: DamageType.Physical,
                critChance: 0.30f, critMultiplier: 2.5f),
            "imgs/Cards/Spells/VitalShot_CardArt");

        private static readonly List<SpellCard> AllCards = new List<SpellCard>
        {
            ArcaneBolt,
            Meditate, ForcedSleep, ConjurePotions, BandageWound, SecondWind,
            ThrowGrit, Brace, RallyCry, Trip, SteadyHands,
            ManaShield, Blink, Flames, IceSpike, ChainLightning, EnchantWeapon,
            Fireball, FrostNova, ArcaneShield, Curse, TeleportStrike, ManaBurn, MagicMissile, ArcaneOrb,
            Taunt, Bodyguard, Rage, MightySlash, SummonBlade, SummonShield, ConjureArmor,
            ShieldWall, Whirlwind, Execute, Charge, BattleCry, CounterStance, Disarm, IronWill,
            PlaceTrap, TameBeast, EagleEye, MultiShot, PiercingShot, ExplosiveTrap, Camouflage,
            HuntersMark, Disengage, BeastBond, RainOfArrows, PoisonShot, NetShot, Track, VitalShot,
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
