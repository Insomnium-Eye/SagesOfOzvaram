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

        /// <summary>True = this effect blocks movement specifically WITHOUT restricting the rest of the turn menu (e.g. a future Rooted) - unlike RestrictsActions, the unit can still Attack/Cards/Items/End normally, it just never sees a Move option. See Combat.TurnMenuBuilder.</summary>
        public bool PreventsMovement;

        /// <summary>% of max HP healed at the start of every one of the affected unit's own turns while this effect is active (e.g. Deep Sleep). 0 = no per-turn heal.</summary>
        public float HealPercentMaxHPPerTurn;

        /// <summary>True = this effect gains a stack (see Stacks/MaxStacks) at the start of every one of the affected unit's own turns, each stack re-deriving the stat bonus fields above from StackBonusTable (e.g. Meditate). See StackBonusTable.</summary>
        public bool GrowsOverTime;
        public int Stacks;
        public int MaxStacks = 1;

        /// <summary>For a GrowsOverTime effect: the bonus to apply (to every one of StrengthBonus/AccuracyBonus/DefenseBonus/ResistanceBonus/IntelligenceBonus that's actually in use - a GrowsOverTime effect moves them together, not independently) once Stacks reaches a given count - index 0 = the value for 1 stack, etc. Not necessarily linear (e.g. Meditate: +2/+5/+8, not a flat per-stack multiple), so this is an explicit table rather than a multiplier. BaseUnit.TickStatusEffects re-applies the entry for the new Stacks count to the bonus fields every time this effect grows; the fields above already hold the stack-1 value from whoever first applies the effect (see Combat.SpellCaster.CastMeditateSpell).</summary>
        public int[] StackBonusTable;

        /// <summary>Ending this effect via EndEffectLabel converts its CURRENT bonus (whatever stack it's built up to) into a new fixed-duration Buff with this many turns, instead of just vanishing (e.g. Meditate). 0 = just vanishes (e.g. Stun, Deep Sleep). See BaseUnit.TryEndRestrictingEffect.</summary>
        public int LockInDurationTurns;

        /// <summary>A single hit for this % of max HP or more clears this effect immediately with NO lock-in, even if LockInDurationTurns is set (e.g. Meditate). 0 = can't be interrupted by damage. See BaseUnit.ApplyRawDamage.</summary>
        public float InterruptDamagePercentMaxHP;
    }
}
