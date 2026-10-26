using SagesOfOzvaram.Units;

namespace SagesOfOzvaram.Combat
{
    /// <summary>Buff = positive (helps the affected unit); Affliction = negative (hurts it) - see BaseUnit.StatusEffects/Buffs/Afflictions.</summary>
    public enum StatusEffectType
    {
        Buff,
        Affliction
    }

    /// <summary>
    /// WHEN a StatusEffect's own effect actually applies - on request, so buffs/afflictions can
    /// behave differently by type rather than every one being an ad hoc one-off:
    /// - Active: always on for as long as the effect is active - stat modifiers (StrengthBonus
    ///   etc.), CC (Stunned/Deep Sleep's RestrictsActions, PreventsMovement, ZeroesEvasion). No
    ///   TurnTrigger involved at all - TurnSystem never dispatches an Active effect.
    /// - StartOfTurn / EndOfTurn: ticks once per matching turn boundary (see
    ///   StatusEffectTurnTrigger for which boundary) - currently just DamagePercentMaxHPPerTick,
    ///   dispatched by TurnSystem.DispatchTimedEffects (StartOfTurn from OnUnitTurnStart, EndOfTurn
    ///   from NextUnit, right before the turn actually changes). The existing per-turn ticks that
    ///   predate this enum (HealPercentMaxHPPerTurn, GrowsOverTime's stack growth, the generic
    ///   TurnsRemaining countdown - all in BaseUnit.TickStatusEffects) are hardcoded to "start of
    ///   the affected unit's own turn" and were NOT migrated onto this enum - they keep working
    ///   exactly as before. This enum/TurnTrigger pair only drives NEW effects going forward
    ///   (DamagePercentMaxHPPerTick), not a retrofit of everything that already existed.
    /// </summary>
    public enum StatusEffectTiming
    {
        Active,
        StartOfTurn,
        EndOfTurn,
    }

    /// <summary>Which turn boundary a StartOfTurn/EndOfTurn StatusEffect ticks on - meaningless for an Active effect. See StatusEffectTiming.</summary>
    public enum StatusEffectTurnTrigger
    {
        /// <summary>The affected unit's own turn (e.g. Weak Bleed's end-of-turn %maxHP tick).</summary>
        AffectedUnitsTurn,
        /// <summary>One specific OTHER unit's turn - see StatusEffect.TriggerUnit.</summary>
        SpecificUnitsTurn,
        /// <summary>Any unit's turn OTHER than the affected unit's own.</summary>
        AnyOtherUnitsTurn,
        /// <summary>The very next turn to start/end, whoever's it is - a one-shot: TurnSystem.DispatchTimedEffects removes the effect right after it fires once, rather than matching again every subsequent turn.</summary>
        NextTurn,
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

        /// <summary>See StatusEffectTiming. Defaults to Active, which every effect that predates this field (Stunned, Deep Sleep, Meditate, Slowed, Guarding, the temp stat buffs, generic Bleeding) still is - this default changes nothing for any of them.</summary>
        public StatusEffectTiming Timing = StatusEffectTiming.Active;

        /// <summary>See StatusEffectTurnTrigger. Only meaningful when Timing is StartOfTurn/EndOfTurn.</summary>
        public StatusEffectTurnTrigger TurnTrigger = StatusEffectTurnTrigger.AffectedUnitsTurn;

        /// <summary>The one specific unit TurnTrigger.SpecificUnitsTurn compares against - unused by every other TurnTrigger.</summary>
        public BaseUnit TriggerUnit;

        /// <summary>Pinning only (on request): true once Syphon has been used during THIS particular pin - at that point Combat.TurnMenuBuilder shows ONLY Release, no more Syphon/End, forcing the caster to let go right away rather than cashing in the Syphon benefit AND keeping the "cannot escape" lock going. Starts false on every fresh Pinning (a new Leap always creates a brand new StatusEffect instance), so it has no carry-over between separate pins.</summary>
        public bool SyphonedThisPin;

        /// <summary>True = this effect overrides EffectiveEvasion to exactly 0 for as long as it's active (e.g. Weak Root) - an absolute override, not a flat penalty, so it doesn't matter what the unit's base Evasion actually is.</summary>
        public bool ZeroesEvasion;

        /// <summary>% EffectiveAccuracy is multiplicatively reduced by while this effect is active (e.g. Weak Blind: 0.40 = -40%) - a proportional reduction, not a flat AccuracyBonus penalty, same "% of the stat itself" shape BaseUnit.EffectiveSpeed's own SpeedReductionPercent already uses for Slowed. The STRONGEST active one wins if more than one effect sets this, rather than compounding them together.</summary>
        public float AccuracyReductionPercent;

        /// <summary>% of max HP dealt as true damage (bypassing Defense/Resistance, see BaseUnit.ApplyTrueDamage) each time this effect's StartOfTurn/EndOfTurn TurnTrigger matches (e.g. Weak Bleed's 5%/turn). 0 = no per-tick damage. Meaningless for an Active effect.</summary>
        public float DamagePercentMaxHPPerTick;

        /// <summary>% of max HP dealt as true damage every time the AFFECTED unit spends AP on anything (see BaseUnit.SpendAP) - e.g. Weak Bleed's 1%-per-action. 0 = none. Independent of Timing/TurnTrigger - this checks on action, not on a turn boundary.</summary>
        public float DamagePercentMaxHPPerAction;

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

        /// <summary>Flat bonus to BaseUnit.EffectiveMaxHP while this effect is active (e.g. Syphon's own self-buff: +6) - summed the same way the stat bonuses above are. BaseUnit.TickStatusEffects clamps current HP down to EffectiveMaxHP right after removing any expired effect, so an overheal from a since-expired MaxHP buff doesn't linger.</summary>
        public int MaxHPBonus;

        /// <summary>The unit this effect's caster is currently pinning to the ground (Pinning only - see BaseUnit.TryStandUp/Game1's "Stand Up" handling) - unused by every other effect.</summary>
        public BaseUnit PinnedTarget;

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
