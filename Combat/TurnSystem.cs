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
        /// Turn): ticks bleed, refills AP, and clears any Guard stance from their last turn.
        /// </summary>
        private void OnUnitTurnStart()
        {
            CurrentUnit.ApplyBleedTick();
            CurrentUnit.ResetAP();
            CurrentUnit.ClearGuard();
        }

        /// <summary>
        /// Rebuild the turn order from currently-alive units, sorted by SPEED (descending).
        /// </summary>
        private void StartNewTurn()
        {
            _turnOrder = _allUnits.Where(u => u.IsAlive).OrderByDescending(u => u.Speed).ToList();
            _currentIndex = 0;
            TurnAnnouncementElapsed = 0f;
            UnitTurnElapsed = 0f;
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
