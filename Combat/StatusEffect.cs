namespace SagesOfOzvaram.Combat
{
    /// <summary>Buff = positive (helps the affected unit); Affliction = negative (hurts it) - see BaseUnit.StatusEffects/Buffs/Afflictions.</summary>
    public enum StatusEffectType
    {
        Buff,
        Affliction
    }

    /// <summary>
    /// A single active status effect on a unit - the one place stat bonuses, per-turn ticks, and
    /// "this unit can't act normally" menu restrictions all live, instead of being a separate ad
    /// hoc field/flag pair per effect scattered across BaseUnit (how Stun, Deep Sleep, Meditate,
    /// and the temporary stat buffs each used to be handled). A new effect just needs values for
    /// whichever fields it actually uses - see BaseUnit.ApplyStatusEffect/StatusEffects and
    /// Combat.SpellCaster for how these get created and applied.
    /// </summary>
    public class StatusEffect
    {
        public string Name;
        public StatusEffectType Type;

        /// <summary>How many of the affected unit's own turns remain. -1 (the default) = indefinite - persists until something explicitly removes it (Wake Up, End Meditation, Break Stun, an interrupt, ...), not a countdown.</summary>
        public int TurnsRemaining = -1;

        // Flat stat bonuses (negative for a weakening Affliction, once one exists) - see
        // BaseUnit's Effective*/Total* properties, which sum Magnitude() across every active
        // StatusEffect on a unit.
        public int StrengthBonus;
        public int AccuracyBonus;
        public int DefenseBonus;
        public int ResistanceBonus;
        public int IntelligenceBonus;

        /// <summary>True = while active, the affected unit's turn menu is replaced entirely by just EndEffectLabel (if affordable/allowed) plus End Turn - see BaseUnit.GetRestrictingEffect/TryEndRestrictingEffect.</summary>
        public bool RestrictsActions;
        public string EndEffectLabel;
        public int EndEffectAPCost;

        /// <summary>% of max HP healed at the start of every one of the affected unit's own turns while this effect is active (e.g. Deep Sleep). 0 = no per-turn heal.</summary>
        public float HealPercentMaxHPPerTurn;

        /// <summary>True = this effect gains a stack (see Stacks/MaxStacks) at the start of every one of the affected unit's own turns instead of applying its bonus all at once - each stack multiplies the stat bonuses above (e.g. Meditate). See Magnitude.</summary>
        public bool GrowsOverTime;
        public int Stacks;
        public int MaxStacks = 1;

        /// <summary>Ending this effect via EndEffectLabel converts its CURRENT bonus (stacks included) into a new fixed-duration Buff with this many turns, instead of just vanishing (e.g. Meditate). 0 = just vanishes (e.g. Stun, Deep Sleep). See BaseUnit.TryEndRestrictingEffect.</summary>
        public int LockInDurationTurns;

        /// <summary>A single hit for this % of max HP or more clears this effect immediately with NO lock-in, even if LockInDurationTurns is set (e.g. Meditate). 0 = can't be interrupted by damage. See BaseUnit.ApplyRawDamage.</summary>
        public float InterruptDamagePercentMaxHP;

        /// <summary>This effect's current live multiplier on its stat bonus fields - Stacks if GrowsOverTime (0 until it's ticked at least once), otherwise always 1 (a flat, non-growing effect).</summary>
        public int Magnitude => GrowsOverTime ? Stacks : 1;
    }
}
