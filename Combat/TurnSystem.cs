using System;
using System.Collections.Generic;
using System.Linq;
using SagesOfOzvaram.Units;

namespace SagesOfOzvaram.Combat
{
    /// <summary>
    /// Manages turn order based on unit SPEED stat.
    /// A "Turn" (matches the GDD's terminology) begins when the previous one ends (or the match
    /// starts) and ends once every viable (alive) unit has acted; the order is then rebuilt from
    /// whoever's left. CurrentUnit/UnitTurnElapsed refer to whichever unit is acting right now
    /// within that Turn.
    /// </summary>
    public class TurnSystem
    {
        private List<BaseUnit> _allUnits;
        private List<BaseUnit> _turnOrder;
        private int _currentIndex;
        private int _currentTurn;

        private const float TURN_ANNOUNCEMENT_TIME = 2f;  // "Turn X - Go!" fade in/hold/fade out totals this
        private const float CAMERA_ZOOM_TIME = 1.5f;       // Smooth zoom to unit over 1.5 seconds

        public int CurrentTurn => _currentTurn;
        public BaseUnit CurrentUnit => _turnOrder[_currentIndex];

        /// <summary>Fires once a unit's turn has actually started (AP/MP reset and ready to act) - NOT fired for a Fainted unit's forced skip, which never really gets a turn. Game1 subscribes to this for turn-start logging (the Aggro System's per-turn console summary) without TurnSystem itself needing any UI/console dependency - this class stays pure game logic.</summary>
        public event Action<BaseUnit> UnitTurnStarted;

        /// <summary>Total individual unit actions taken across the whole match so far.</summary>
        public int TotalTurnsTaken { get; private set; }

        // Timer since the CURRENT TURN started - drives the "Turn X - Go!" banner only.
        public float TurnAnnouncementElapsed { get; private set; }
        public bool ShowingTurnAnnouncement => TurnAnnouncementElapsed < TURN_ANNOUNCEMENT_TIME;

        // Timer since the CURRENT UNIT started acting - drives auto-advance and camera pan.
        public float UnitTurnElapsed { get; private set; }
        public float CameraTransitionElapsed { get; private set; }
        public bool TransitioningCamera => CameraTransitionElapsed < CAMERA_ZOOM_TIME;

        /// <summary>
        /// Initialize the turn system and start Turn 1.
        /// </summary>
        public TurnSystem(List<BaseUnit> units)
        {
            _allUnits = units;
            _currentTurn = 1;
            StartNewTurn();
            OnUnitTurnStart();
        }

        /// <summary>
        /// Update turn timers.
        /// </summary>
        public void Update(float deltaTime)
        {
            TurnAnnouncementElapsed += deltaTime;
            UnitTurnElapsed += deltaTime;
            CameraTransitionElapsed += deltaTime;
        }

        /// <summary>
        /// Advance to the next viable unit. Starts a new Turn once everyone has acted.
        /// </summary>
        public void NextUnit()
        {
            // The turn that's ending, not the one about to start - captured before _currentIndex
            // moves, so DispatchTimedEffects(EndOfTurn, ...) below sees whoever just finished
            // acting (see Combat.StatusEffectTiming.EndOfTurn, e.g. Weak Bleed).
            DispatchTimedEffects(StatusEffectTiming.EndOfTurn, CurrentUnit);

            TotalTurnsTaken++;
            _currentIndex++;
            CameraTransitionElapsed = 0f;
            UnitTurnElapsed = 0f;

            // Skip anyone who died since the turn order was built
            while (_currentIndex < _turnOrder.Count && !_turnOrder[_currentIndex].IsAlive)
                _currentIndex++;

            if (_currentIndex >= _turnOrder.Count)
            {
                _currentTurn++;
                StartNewTurn();
            }

            OnUnitTurnStart();
        }

        /// <summary>
        /// Runs whenever a unit's turn starts (a normal advance or the first unit of a fresh
        /// Turn): ticks bleed, ticks every active StatusEffect (per-turn heals like Deep Sleep,
        /// stack growth like Meditate, and duration countdowns - see BaseUnit.TickStatusEffects),
        /// ticks the Break Stun cooldown, ticks Speed reduction, refills AP (plus any
        /// BonusAPNextTurn queued by Second Wind), and clears any Guard stance from their last
        /// turn. A Fainted unit skips its turn outright instead - no AP, no choices, forced every
        /// time until HP recovers (see BaseUnit.IsFainted). A unit with a restricting StatusEffect
        /// (Stunned, Deep Sleep, Meditating - see BaseUnit.GetRestrictingEffect) but not Fainted
        /// still gets AP like normal - its turn is limited to just attempting that effect's own
        /// "end it early" action or ending without acting, which Game1 enforces.
        /// </summary>
        private void OnUnitTurnStart()
        {
            // Summoning Curse (see Game1.ExecuteSummon) has already done its one job - sorting
            // this unit after every non-cursed unit in THIS Turn's StartNewTurn call below - the
            // moment its turn actually starts; clearing it now means the NEXT Turn's rebuild sorts
            // it by EffectiveSpeed normally, same as any other unit, exactly once.
            CurrentUnit.RemoveStatusEffect("Summoning Curse");

            CurrentUnit.ApplyBleedTick();
            CurrentUnit.TickStatusEffects();
            CurrentUnit.TickStunBreakCooldown();
            CurrentUnit.TickSpeedReduction();
            DispatchTimedEffects(StatusEffectTiming.StartOfTurn, CurrentUnit);

            if (CurrentUnit.IsFainted)
            {
                NextUnit();
                return;
            }

            UnitTurnStarted?.Invoke(CurrentUnit);

            CurrentUnit.ResetAP();
            CurrentUnit.RegenMana();
            CurrentUnit.ClearGuard();
            CurrentUnit.ResetCardDrawState();
        }

        /// <summary>
        /// Checks EVERY living unit's own StatusEffects (not just turnOwner's) for one matching
        /// `timing` whose TurnTrigger condition is satisfied against `turnOwner` (whoever's turn
        /// is starting - called from OnUnitTurnStart - or ending - called from NextUnit, before
        /// _currentIndex moves), and applies its DamagePercentMaxHPPerTick as true damage if so
        /// (see BaseUnit.ApplyTrueDamage - bypasses Defense/Resistance, same as a Bleed tick).
        /// Looping every unit rather than just turnOwner is what makes SpecificUnitsTurn/
        /// AnyOtherUnitsTurn actually work - an effect sitting on unit A that cares about unit B's
        /// turn has to be checked at the moment B's turn boundary happens, not A's.
        /// A NextTurn-triggered effect fires on the very first turn boundary (of either timing)
        /// after it was applied, then is removed immediately - a one-shot, not a recurring tick.
        /// </summary>
        private void DispatchTimedEffects(StatusEffectTiming timing, BaseUnit turnOwner)
        {
            foreach (var unit in _allUnits.Where(u => u.IsAlive))
            {
                foreach (var effect in unit.StatusEffects.Where(e => e.Timing == timing).ToList())
                {
                    bool matches = effect.TurnTrigger switch
                    {
                        StatusEffectTurnTrigger.AffectedUnitsTurn => turnOwner == unit,
                        StatusEffectTurnTrigger.SpecificUnitsTurn => turnOwner == effect.TriggerUnit,
                        StatusEffectTurnTrigger.AnyOtherUnitsTurn => turnOwner != unit,
                        StatusEffectTurnTrigger.NextTurn => true,
                        _ => false,
                    };
                    if (!matches)
                        continue;

                    if (effect.DamagePercentMaxHPPerTick > 0f)
                        unit.ApplyTrueDamage((int)Math.Round(unit.EffectiveMaxHP * effect.DamagePercentMaxHPPerTick));

                    if (effect.TurnTrigger == StatusEffectTurnTrigger.NextTurn)
                        unit.RemoveStatusEffect(effect.Name);
                }
            }
        }

        /// <summary>
        /// Rebuild the turn order from currently-alive, non-Relic units (a Relic - e.g. a
        /// Portal - is a stationary structure with no actions to take, so it's excluded
        /// entirely rather than needing a no-op turn skipped every cycle), sorted by effective
        /// SPEED (descending) - a Slowed unit (see BaseUnit.EffectiveSpeed) acts later starting
        /// next Turn. Every non-cursed unit sorts ahead of every "Summoning Curse"-afflicted one
        /// (a unit summoned last Turn getting its first turn this one - see Game1.ExecuteSummon)
        /// regardless of EffectiveSpeed; EffectiveSpeed still breaks ties WITHIN each of those two
        /// groups, so among several units all summoned the same Turn, the faster one still goes
        /// first - just always after everyone not newly summoned.
        /// </summary>
        private void StartNewTurn()
        {
            _turnOrder = _allUnits.Where(u => u.IsAlive && !u.IsRelic)
                .OrderByDescending(u => !u.HasStatusEffect("Summoning Curse"))
                .ThenByDescending(u => u.EffectiveSpeed)
                .ToList();
            _currentIndex = 0;
            TurnAnnouncementElapsed = 0f;
            UnitTurnElapsed = 0f;
        }

        /// <summary>
        /// Splice a unit that was JUST summoned mid-Turn into the live _turnOrder, so it still
        /// gets exactly one turn THIS Turn - at the very end, after every other unit without
        /// "Summoning Curse" (whether they've already acted or not) - rather than only joining
        /// starting next Turn. Game1.ExecuteSummon calls this right after applying the curse and
        /// adding the unit to _allUnits (Game1._units IS _allUnits, same list reference, so that
        /// part alone would already have it rebuilt-in correctly come next Turn regardless - this
        /// is what gets it INTO the current, already-built _turnOrder instead of waiting for that).
        /// Finds where the existing trailing "cursed" group starts (or the very end, if nobody
        /// else is cursed yet this Turn) and inserts by EffectiveSpeed within just that group, so
        /// several units summoned the same Turn still order fastest-of-them-first, same rule
        /// StartNewTurn's own sort already applies.
        /// </summary>
        public void AddUnitMidTurn(BaseUnit unit)
        {
            if (unit.IsRelic || !unit.IsAlive)
                return;

            int insertAt = _turnOrder.FindIndex(u => u.HasStatusEffect("Summoning Curse"));
            if (insertAt == -1)
                insertAt = _turnOrder.Count;

            while (insertAt < _turnOrder.Count && _turnOrder[insertAt].EffectiveSpeed >= unit.EffectiveSpeed)
                insertAt++;

            _turnOrder.Insert(insertAt, unit);

            // Defensive only - every cursed unit's curse clears the moment ITS turn starts
            // (OnUnitTurnStart), so nothing before _currentIndex can still be cursed, meaning
            // insertAt (found via HasStatusEffect("Summoning Curse")) can never actually land at
            // or before it. Keeps CurrentUnit correct if that invariant is ever broken anyway.
            if (insertAt <= _currentIndex)
                _currentIndex++;
        }

        /// <summary>
        /// Get this Turn's unit order (for debug display).
        /// </summary>
        public List<BaseUnit> GetTurnOrder() => _turnOrder;

        /// <summary>
        /// Get turn order text for display (e.g., "Hunter -> Sorcerer -> Cleric -> Warrior").
        /// </summary>
        public string GetTurnOrderText()
        {
            return string.Join(" -> ", _turnOrder.Select(u => u.Name));
        }
    }
}
