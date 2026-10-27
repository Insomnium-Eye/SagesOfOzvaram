using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SagesOfOzvaram.Maps;
using SagesOfOzvaram.Units;
using SagesOfOzvaram.Units.Heroes;
using SagesOfOzvaram.Units.Summons;
using SagesOfOzvaram.Units.Relics;
using SagesOfOzvaram.Combat;
using SagesOfOzvaram.Objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SagesOfOzvaram
{
    public class Game1 : Game
    {
        private enum GameState
        {
            StartMenu,
            CharacterSelect,
            Playing,
            MatchOver
        }

        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        // Map systems
        private HexGrid _hexGrid;
        private HexGridRenderer _renderer;
        private Map _map;
        private AssetRegistry _assetRegistry;
        private DevConsole _console;

        // Units
        private List<BaseUnit> _units;
        private TurnSystem _turnSystem;
        private Vector2 _cameraTarget;
        private float _cameraZoomTarget = 1f;
        private bool _hasAutoAdvancedThisTurn = false;
        private bool _hasAiActedThisTurn = false;

        /// <summary>Which unit we last ran the once-per-turn setup for (control handoff, resetting _hasAiActedThisTurn/_hasAutoAdvancedThisTurn, deciding _currentUnitTurnBatched) - compared against _turnSystem.CurrentUnit each frame so that setup fires exactly once per turn (see the camera-tracking block in Update), not every single frame once the camera happens to already be settled (the previous, buggier trigger condition).</summary>
        private BaseUnit _lastTurnStartUnit;

        /// <summary>
        /// True while the CURRENT unit's turn is being fast-forwarded as part of the AI Turn
        /// Batching system (see ShouldBatchAiTurn) rather than individually presented with a
        /// camera pan - always false for the player's own turn. Decided once per turn, at the
        /// same moment _hasAiActedThisTurn/_hasAutoAdvancedThisTurn reset (see Update), and read
        /// both there (to skip the camera pan/3s dwell) and in Draw (to show the "Other units are
        /// taking their turns..." message).
        /// </summary>
        private bool _currentUnitTurnBatched;

        /// <summary>True once LogUnitTurnDivider has printed at least one unit's turn divider - false right after a fresh match starts (EnterTestMode/EnterOriginalMode) so the very first unit's turn doesn't print a leading separator with nothing above it to separate from.</summary>
        private bool _hasLoggedAnyUnitTurn = false;

        /// <summary>Last TurnSystem.CurrentTurn CheckWildAnimalSpawn already rolled against - 0 on a fresh EnterOriginalMode (not subscribed at all in Test Mode) so the spawn roll only happens once per Turn (full round), not once per individual unit's turn.</summary>
        private int _lastWildSpawnCheckTurn = 0;

        // UI
        private SpriteFont _font;
        private Texture2D _whitePixel;

        // Input
        private KeyboardState _previousKeyboardState;
        private MouseState _previousMouseState;
        private string _consoleInput = "";

        /// <summary>
        /// Lines scrolled up from the bottom of the console's output - 0 shows the latest lines
        /// (the normal/live view). Clamped against the actual output length every time it's used
        /// (DrawConsole), so it's safe to let it grow past what's currently valid (e.g. right
        /// after a "clear") without needing to proactively re-clamp it everywhere it changes.
        /// </summary>
        private int _consoleScrollOffset = 0;

        /// <summary>Lines of console output shown at once - shared by DrawConsole (what it slices/shows) and HandleConsoleInput (how far a PageUp/PageDown press scrolls).</summary>
        private const int ConsoleVisibleLines = 12;
        private bool _consoleOpen = false;

        // Selected hex
        private (int col, int row) _selectedHex = (-1, -1);

        // Start menu (the game's actual boot screen - see Initialize) - "Arena" (EnterOriginalMode,
        // the full 20x15 map + character select, controlling only the chosen character) or
        // "Debug" (EnterTestMode, the small 3x3 map, controlling every unit) - see
        // HandleStartMenuInput/ConfirmStartMenuSelection/DrawStartMenu.
        private int _startMenuIndex = 0;
        private static readonly string[] StartMenuLabels = { "Arena", "Debug" };

        // Current screen - Initialize always sets this explicitly to StartMenu; the field default
        // here only matters if something somehow read it before Initialize runs.
        private GameState _gameState = GameState.StartMenu;

        /// <summary>
        /// True once the dev console's "test mode" has been used (see EnterTestMode) - pre-
        /// release dev tooling, meant to be removed/hidden before an official release. Suppresses
        /// the FFA win check (so a stray AI hit between the 4 spawned classes can't end the
        /// "match" early) and the placeholder AI's own movement step (RunSimpleAI) - it still
        /// attacks if already in range, just never paths toward the player - while active.
        /// </summary>
        private bool _testModeActive = false;
        private static readonly string[] AvatarFileNames =
        {
            "ApprenticeSorc_Avatar",     // matches _units[0] (Sorcerer)
            "ApprenticeWarrior_Avatar",  // matches _units[1]
            "ApprenticeCleric_Avatar",   // matches _units[2]
            "ApprenticeHunter_Avatar"    // matches _units[3]
        };
        private Texture2D[] _avatarTextures;

        /// <summary>One small icon per stat code ("STR"/"ACC"/"DEF"/"RES"/"INT") used by the on-screen stat-change display (see DrawUnitStatBonuses) - loaded once in LoadContent from Content/imgs/UI/StatIcons/{code}.png.</summary>
        private Dictionary<string, Texture2D> _statIcons = new Dictionary<string, Texture2D>();

        // Spell cards
        private Texture2D _spellCardTemplate;
        private Dictionary<string, Texture2D> _cardArtCache = new Dictionary<string, Texture2D>();
        private Dictionary<string, Texture2D> _cardCompositeCache = new Dictionary<string, Texture2D>();

        // Summon cards
        private Texture2D _summonCardTemplate;
        private Dictionary<string, Texture2D> _summonArtCache = new Dictionary<string, Texture2D>();
        private Dictionary<string, Texture2D> _summonCompositeCache = new Dictionary<string, Texture2D>();

        // Card hand (spell + summon cards combined, browsable as a hand of playing cards)
        private bool _cardMenuActive = false;
        private List<object> _availableHandCards = new List<object>();
        private int _handCardIndex = 0;
        private int? _highlightedHandCardIndex;
        private readonly Random _deckRandom = new Random();
        private readonly Dictionary<HeroClass, List<object>> _classDecks = new Dictionary<HeroClass, List<object>>();
        private readonly Dictionary<HeroClass, List<object>> _classHands = new Dictionary<HeroClass, List<object>>();

        /// <summary>Every card a class has actually USED (cast, successfully or not - see DiscardCard), one pile per class, same shape as _classHands/_classDecks. Nothing currently reads from these piles (no reshuffle-on-empty-deck mechanic exists yet - see Card/Deck System's "Depletion" note) - this exists so a used card has somewhere real to go instead of just vanishing, which is what this whole discard-pile system was added to fix.</summary>
        private readonly Dictionary<HeroClass, List<object>> _classDiscardPiles = new Dictionary<HeroClass, List<object>>();

        private int _selectedCharacterIndex = 0;  // Sorcerer is the default selection

        /// <summary>Whichever unit the player currently has turn-menu control of - the real player hero on its own turn, or (see the per-turn handoff in Update) one of that hero's own ControllableBySummoner summons on ITS turn instead. Everywhere else in Game1 that reads "the player's unit" (AP/MP display, attack/move/cast execution, ...) means THIS, not necessarily the hero.</summary>
        private BaseUnit _playerUnit;

        /// <summary>The actual player-chosen hero (Arena: whoever was picked at character select; Debug/Test Mode: whichever unit currently has its turn, same "control everyone" rule Test Mode already had) - stays fixed across a controllable summon's turn so Update knows who to hand control BACK to once that summon's turn ends, and whose summons even count as "the player's own" for the handoff in the first place (see IsPlayerControllableSummon). Null before character select resolves it (Arena) or before EnterTestMode picks a first unit (Debug).</summary>
        private BaseUnit _playerHero;

        // Turn menu (shown, next to the player's unit, when it's their turn) - its actual
        // contents are built fresh on demand by Combat.TurnMenuBuilder (see GetTurnMenuOptions)
        // rather than being one fixed list, so e.g. "Cards" never appears for a non-Summoner unit
        // and "Move" disappears entirely once there's nowhere left to go.
        private int _turnMenuIndex = 0;
        private bool _turnMenuActive = false;
        private bool _viewingMap = false;

        // Restricted turn (replaces the normal turn menu entirely while the player's unit has a
        // restricting StatusEffect active - Stunned, Deep Sleep, Meditating, ... - see
        // HandleRestrictedTurnInput). Not shown at all while Fainted - that's a forced, no-choice
        // skip. The first option's label/cost comes straight from that effect's own
        // EndEffectLabel/EndEffectAPCost (see StatusEffect/BaseUnit.GetRestrictingEffect); "End
        // Turn" is always the second - this menu works for ANY current or future restricting
        // effect with no Game1 changes needed, since it reads the effect's own data rather than
        // hardcoding which effect is active.
        private int _restrictedMenuIndex = 0;

        /// <summary>The two options shown while the player's unit can't act normally - the active restricting effect's own EndEffectLabel (with its AP cost), then "End Turn".</summary>
        private static string[] GetRestrictedMenuOptions(BaseUnit unit)
        {
            var effect = unit.GetRestrictingEffect();
            return new[] { effect?.EndEffectLabel ?? "Wait", "End Turn" };
        }

        // Match end (FFA - each player is a "team" of one; last unit not Fainted wins)
        private BaseUnit _matchWinner;

        // Attack submenu (opened from the "Attack" turn-menu option)
        private List<string> _attackMenuLabels = new List<string>();
        private List<(Move Move, Weapon SourceWeapon)> _attackMenuMoves = new List<(Move, Weapon)>();
        private int _attackMenuIndex = 0;
        private bool _attackMenuActive = false;

        // Target selection (opened after confirming a single-target move in the attack submenu -
        // a HitsAllAdjacent move like Sword Spin skips this and resolves immediately)
        private bool _targetingModeActive = false;
        private Move _pendingMove;
        private Weapon _pendingSourceWeapon;
        private List<BaseUnit> _targetCandidates = new List<BaseUnit>();

        // Set only when _pendingMove came from casting a SpellCard (via the Cards menu) rather
        // than a weapon/racial attack - lets ExecutePendingAttack/CancelTargeting know to remove
        // the card from hand on a successful cast, and to return to the Cards menu (not the
        // Attack menu) on cancel.
        private SpellCard _pendingSpellCard;
        private bool _targetingFromCardMenu = false;

        // Teleport targeting (opened for an IsBlink spell, e.g. Blink/Disengage) - click any
        // highlighted tile to teleport the caster there instantly, no pathfinding involved.
        private bool _teleportModeActive = false;
        private HashSet<(int col, int row)> _teleportValidTiles = new HashSet<(int col, int row)>();

        // Ally-shield targeting (opened for an IsAllyShield spell, e.g. Arcane Shield) - unlike
        // _targetCandidates (attack targeting), this list includes the caster itself, since
        // shielding yourself is a valid choice.
        private bool _allyTargetModeActive = false;
        private List<BaseUnit> _allyTargetCandidates = new List<BaseUnit>();

        // Tame Beast targeting (opened for Move.IsTameBeast) - candidates are pre-filtered to
        // only genuinely tameable units (BaseUnit.IsTameable - Wild, unowned, Neutral) at or
        // below the caster's own Level, so anything actually clickable here is guaranteed to be
        // a legal target; the only thing left to chance on confirm is the accuracy roll itself.
        private bool _tameBeastModeActive = false;
        private List<BaseUnit> _tameBeastCandidates = new List<BaseUnit>();

        // Trap placement targeting (opened for Move.IsPlaceTrap, e.g. Place Trap) - click any
        // highlighted empty tile within Range to plant a Trap (see Objects.Trap) there instead of
        // attacking immediately. See CastPlaceTrap/HandleTrapPlacementInput/ExecutePlaceTrap.
        private bool _trapPlacementModeActive = false;
        private HashSet<(int col, int row)> _trapPlacementValidTiles = new HashSet<(int col, int row)>();

        /// <summary>Every armed Trap currently on the battlefield - see Objects.Trap for why this is its own list rather than living in _units.</summary>
        private List<Trap> _placedTraps = new List<Trap>();

        /// <summary>Cached placeholder sprite for every Trap (loaded once in LoadContent) - reuses the same generic Pawn placeholder several summons without unique art already use, just drawn smaller and tinted (see Draw's trap-rendering pass) rather than loaded per-instance.</summary>
        private Texture2D _trapSpriteTexture;

        // Leap targeting (opened for Move.IsLeap, e.g. the Dusk Roachlin's Leap) - a dual mode no
        // other move needs: click either an empty, passable tile within Range (a plain
        // reposition) OR a living unit within Range (lands adjacent to it instead, then resolves
        // Leap's own landing effect - see ExecuteLeapToTile/ExecuteLeapOntoUnit). _leapValidTargets
        // is pre-filtered to units that actually HAVE a free adjacent tile to land on - clicking
        // one is guaranteed to succeed, same "only show what can work" rule every other targeting
        // mode already follows.
        private bool _leapModeActive = false;
        private HashSet<(int col, int row)> _leapValidTiles = new HashSet<(int col, int row)>();
        private List<BaseUnit> _leapValidTargets = new List<BaseUnit>();

        // Release targeting (the "Release" turn-menu option, shown only while Pinning - see
        // TurnMenuBuilder) - on request, releasing doesn't just instantly let go: it prompts for
        // one of the tiles adjacent to the caster's CURRENT position (which, while Pinning, is
        // the same tile its pinned target occupies - see ExecuteLeapOntoUnit) to step off onto,
        // since the caster has to physically vacate that shared tile once it's no longer pinning
        // anything there.
        private bool _releaseModeActive = false;
        private HashSet<(int col, int row)> _releaseValidTiles = new HashSet<(int col, int row)>();

        /// <summary>Items currently lying on the ground (e.g. Meat dropped by a slain Wild beast - see Update's death sweep), waiting to be picked up via the "Pick Up" Action - see TurnMenuBuilder/ExecutePickUp.</summary>
        private List<DroppedItem> _droppedItems = new List<DroppedItem>();

        // Summon placement (opened for a SummonCard) - click any highlighted tile to spawn the
        // summoned creature there (within SummonPlacementRange of its owner's Generic Portal,
        // or of the caster directly if it somehow has none - see OpenSummonCreaturePlacement);
        // see CastSummon/ExecuteSummon.
        private bool _summonPlacementModeActive = false;
        private HashSet<(int col, int row)> _summonPlacementValidTiles = new HashSet<(int col, int row)>();
        private SummonCard _pendingSummonCard;

        // Generic Portal placement - Phase 1 of CastSummon for a caster with no Generic Portal
        // of their own standing yet (see PlayerHasActiveGenericPortal); click any highlighted
        // tile within GenericPortalPlacementRange of the caster to conjure the portal there,
        // which then opens Phase 2 (the normal summon-placement mode above) for the actual
        // creature - see ExecutePortalPlacement/OpenSummonCreaturePlacement.
        private bool _portalPlacementModeActive = false;
        private HashSet<(int col, int row)> _portalPlacementValidTiles = new HashSet<(int col, int row)>();

        // Dev-console "summon <UnitID> <Team>" placement (see DevSummonCommand/
        // HandleDevSummonPlacementInput) - click any passable, unoccupied tile anywhere on the
        // map to spawn the creature there, bypassing AP/MP/hand entirely. Suppresses the entire
        // normal turn-system update (same treatment _consoleOpen already gets) while active, so
        // nothing else can act mid-placement.
        private bool _devSummonModeActive = false;
        private SummonCard _pendingDevSummonCard;
        private BaseUnit _pendingDevSummonOwner;

        /// <summary>Recomputed every Draw frame while _targetingModeActive - whichever _targetCandidates entry the mouse is currently over, or null. Drives the damage/hit%/crit/affliction preview panel.</summary>
        private BaseUnit _hoveredAttackTarget;

        /// <summary>Whichever unit's buff/debuff source breakdown is currently open (see DrawUnitDetailBox/HandleMapControls) - null means the box is closed. Set by clicking a unit, cleared by clicking it again or clicking empty space; purely an inspection tool, doesn't belong to anyone's turn.</summary>
        private BaseUnit _unitDetailBoxUnit;

        // Cone aiming (opened instead of _targetingModeActive for a HitsCone move, e.g. Frost
        // Blast) - aimed by clicking any hex to set a direction, not by picking a unit.
        private bool _coneAimingModeActive = false;

        // Items menu (opened from the "Items" turn-menu option) - a flat list of every Item the
        // unit is carrying: equipped/carried weapons (Inventory), equipment (not yet implemented
        // - Items.Category reserves a slot for it, but nothing ever populates one), and
        // consumables (Consumables). See OpenItemsMenu/HandleItemsMenuInput/DrawItemsMenu.
        private bool _itemsMenuActive = false;
        private List<Item> _itemsMenuEntries = new List<Item>();
        private List<string> _itemsMenuLabels = new List<string>();
        private int _itemsMenuIndex = 0;

        // Item detail view (opened by selecting an entry from the items list above) - shows that
        // item's stats/granted-attacks/description (Weapon) or description (ConsumableItem),
        // plus whichever actions BuildItemDetailActions decides apply to it. See
        // HandleItemDetailInput/ConfirmItemDetailSelection/DrawItemDetail.
        private bool _itemDetailActive = false;
        private Item _selectedItem;
        private List<string> _itemDetailActions = new List<string>();
        private int _itemDetailIndex = 0;

        /// <summary>How many lines of the item-detail info panel are scrolled past (0 = showing from the top) - a long weapon's full move list/descriptions can run taller than the screen, so DrawItemDetail only ever renders a window of its wrapped lines starting here. Scrolled with the mouse wheel while the panel is open (HandleItemDetailInput) and clamped against the real wrapped-line count in DrawItemDetail, the same split DevConsole's own scroll uses.</summary>
        private int _itemDetailScrollOffset = 0;

        // Combat log - a short-lived line summarizing the last attack's outcome
        private string _combatLogMessage = "";
        private float _combatLogTimer = 0f;

        /// <summary>A simple, presentation-only "swing near the target" (Slash) or "projectile from attacker to target" (Shooting) effect, spawned per-target whenever an attack resolves - see SpawnAttackAnimations.</summary>
        private class ActiveAttackEffect
        {
            public AttackAnimationType Type;
            public Vector2 From;
            public Vector2 To;
            public float Elapsed;
            public float Duration;        // total lifetime - the effect is removed once Elapsed reaches this
            public float TravelDuration;  // Shooting only: time to actually cross from From to To: may be less than Duration, so the projectile then holds steady at To for the remainder (Duration - TravelDuration) instead of vanishing the instant it lands - a clearer "impact" beat. Equals Duration (no hold) for Slash.
            public Color ProjectileColor; // Shooting only - see SpawnAttackAnimations
        }

        private readonly List<ActiveAttackEffect> _activeAttackEffects = new List<ActiveAttackEffect>();

        /// <summary>A brief "took a hit" reaction on whichever unit actually got hit - a nudge away from the attacker plus a red flash on the sprite, both fading out over Duration. Spawned per-hit alongside the attack's own Slash/Shooting effect (see SpawnAttackAnimations), but tracked separately since it's keyed to the TARGET rather than the attack itself. StartDelay holds it off until the attack actually ARRIVES - zero for a Slash (already at the target), but a Shooting projectile's travel time for anything ranged, so a cast target doesn't flinch before the bolt/missile has even reached it.</summary>
        private class ActiveHitReaction
        {
            public BaseUnit Target;
            public float Elapsed;
            public float StartDelay;
            public float Duration;
            public Vector2 NudgeDirection; // unit vector pointing away from the attacker
        }

        private readonly List<ActiveHitReaction> _activeHitReactions = new List<ActiveHitReaction>();

        /// <summary>A floating "-N" damage number that rises and fades above Target - on request, specifically for the moment a unit triggers a placed Trap (see Game1.TriggerTrap/SpawnDamageNumber), since a trap trigger has no attack animation of its own to carry that feedback the way a normal weapon/spell hit's Slash/Shooting effect does.</summary>
        private class ActiveDamageNumber
        {
            public BaseUnit Target;
            public int Amount;
            public float Elapsed;
            public float Duration;
        }

        private readonly List<ActiveDamageNumber> _activeDamageNumbers = new List<ActiveDamageNumber>();
        // Long enough to actually read: a sin(pi*t) intensity curve only spends a sliver of its
        // total duration near full strength, so a short duration (the original 0.25s, matched to
        // a melee Slash's own tiny window) made the flash/nudge nearly subliminal - same lesson
        // as the missile animation needing a longer window to actually be seen.
        private const float HitReactionDuration = 0.4f;
        private const float HitReactionNudgeDistance = 14f; // pixels, world space

        // Movement mode (opened from the "Move" turn-menu option)
        private bool _movementModeActive = false;
        private Dictionary<(int col, int row), (int tiles, int waterTiles)> _reachableTiles = new Dictionary<(int, int), (int, int)>();

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            // Set window size
            _graphics.PreferredBackBufferWidth = 1280;
            _graphics.PreferredBackBufferHeight = 720;
            _graphics.ApplyChanges();
        }

        protected override void Initialize()
        {
            // Asset registry
            _assetRegistry = new AssetRegistry();
            RegisterDefaultAssets();

            // Initialize units list
            _units = new List<BaseUnit>();

            // Hex grid/map need SOME initial values to construct the console/renderer against -
            // EnterTestMode (called below) immediately replaces both with the 3x3 test setup, so
            // what's built here doesn't matter beyond being valid.
            _hexGrid = new HexGrid(3, 3, tileSize: 32f);
            _map = new Map(3, 3);

            // Dev console
            _console = new DevConsole(_map, _assetRegistry);
            _console.AddCardCallback = AddCardToHandCommand;
            _console.ListCardsCallback = ListCardsCommand;
            _console.TestModeCallback = EnterTestMode;
            _console.OriginalModeCallback = EnterOriginalMode;
            _console.SummonCallback = DevSummonCommand;

            // Actual typed characters for the console - KeyboardState (polled in
            // HandleConsoleInput) only reports WHICH keys are held, not what character a key
            // produces (layout/shift-dependent), so without this nothing ever appeared when
            // typing. TextInput is keyboard-layout-aware and fires once per keystroke.
            Window.TextInput += OnConsoleTextInput;

            // Renderer
            _renderer = new HexGridRenderer(_hexGrid, _spriteBatch, GraphicsDevice);

            // The game boots into a Start Menu (Arena / Debug - see DrawStartMenu) rather than
            // straight into either mode - EnterTestMode/EnterOriginalMode (and everything that
            // depends on one of them having run, e.g. _turnSystem/_units/_playerUnit) only fire
            // once the player actually picks one (ConfirmStartMenuSelection), so nothing past
            // this point may assume either has happened yet while _gameState is StartMenu.
            _gameState = GameState.StartMenu;
            _startMenuIndex = 0;

            base.Initialize();
        }

        /// <summary>
        /// Spawn units in random corners of the map (avoiding water and mountains).
        /// </summary>
        private void SpawnUnits()
        {
            Random rand = new Random();

            // Define the 4 corners (col, row)
            var corners = new[]
            {
                (0, 0),                                    // Top-left
                (_map.Width - 1, 0),                       // Top-right
                (0, _map.Height - 1),                      // Bottom-left
                (_map.Width - 1, _map.Height - 1)          // Bottom-right
            };

            // Create unit types
            var unitTypes = new BaseUnit[]
            {
                new ApprenticeSorcerer(),
                new ApprenticeWarrior(),
                new ApprenticeCleric(),
                new ApprenticeHunter()
            };

            // Spawn each unit in a corner
            for (int i = 0; i < unitTypes.Length && i < corners.Length; i++)
            {
                var (cornerCol, cornerRow) = corners[i];
                Vector2 spawnPos = FindValidSpawnPosition(cornerCol, cornerRow, rand);
                unitTypes[i].Position = spawnPos;
                _units.Add(unitTypes[i]);
            }
        }

        /// <summary>
        /// Find a valid spawn position near a corner (not water or mountain).
        /// </summary>
        private Vector2 FindValidSpawnPosition(int cornerCol, int cornerRow, Random rand)
        {
            int searchRadius = 5; // How far from corner to search
            int col = cornerCol;
            int row = cornerRow;

            // Search in expanding radius around corner
            for (int radius = 0; radius < searchRadius; radius++)
            {
                // Get all tiles within this radius
                var candidateTiles = new List<(int, int)>();

                for (int c = Math.Max(0, cornerCol - radius); c <= Math.Min(_map.Width - 1, cornerCol + radius); c++)
                {
                    for (int r = Math.Max(0, cornerRow - radius); r <= Math.Min(_map.Height - 1, cornerRow + radius); r++)
                    {
                        // Check if tile is valid (not water, not mountain)
                        Tile tile = _map.GetTile(c, r);
                        if (tile.Type != "water" && tile.Type != "mountain")
                        {
                            candidateTiles.Add((c, r));
                        }
                    }
                }

                // If we found valid tiles, pick one randomly
                if (candidateTiles.Count > 0)
                {
                    var (finalCol, finalRow) = candidateTiles[rand.Next(candidateTiles.Count)];
                    return _hexGrid.HexToWorld(finalCol, finalRow);
                }
            }

            // Fallback (shouldn't happen with reasonable map)
            return _hexGrid.HexToWorld(0, 0);
        }

        /// <summary>
        /// Dev-console "test mode" handler (see DevConsole.TestModeCallback). Pre-release dev
        /// tooling - meant to be removed/hidden entirely before an official release, per the
        /// request that introduced it - kept as deliberately separate, self-contained state
        /// (_testModeActive, this method, BuildTestDeck) rather than woven into the normal
        /// match-setup path, so stripping it later only means deleting this and its few
        /// _testModeActive checks elsewhere, not untangling it from real game logic. Swaps to a
        /// small 3x3 all-grass map (the real procedural MapGenerator - see Initialize - is left
        /// completely alone; this builds a Map directly, whose tiles already default to
        /// grass/passable with no generator needed), spawns all 4 hero classes at its 4 corners
        /// (reuses SpawnUnits/FindValidSpawnPosition exactly as a normal match does, just against
        /// the swapped-in 3x3 grid), puts the player in control of the first one (Sorcerer - no
        /// character select in Test Mode), gives every unit effectively unlimited AP/MP (999 -
        /// "free" in every practical sense, since TurnSystem's normal ResetAP/RegenMana top back
        /// up to that same inflated Max every turn with no extra code needed, rather than
        /// threading a true zero-cost bypass through every cast path), and replaces the normal
        /// ~30-card shuffled class deck with BuildTestDeck's "every card that exists, Untested
        /// ones first" deck. Re-entering Test Mode (e.g. after a relaunch, once more cards have
        /// been marked tested in Combat.TestedCards) resets everything fresh again.
        /// </summary>
        private string EnterTestMode()
        {
            GameLogger.ResetForNewMatch();
            _hasLoggedAnyUnitTurn = false;

            _hexGrid = new HexGrid(3, 3, tileSize: 32f);
            _map = new Map(3, 3);
            _console.SetMap(_map);

            _units.Clear();
            SpawnUnits();
            foreach (var unit in _units)
                unit.LoadContent(Content, GraphicsDevice);

            const int unlimited = 999;
            foreach (var unit in _units)
            {
                unit.MaxAP = unlimited;
                unit.CurrentAP = unlimited;
                unit.MaxMP = unlimited;
                unit.CurrentMP = unlimited;
            }

            _testModeActive = true;

            // Every class gets its own independent test deck/hand (not just whichever unit
            // starts in control) - the player takes control of EACH unit on its own turn (see
            // the _testModeActive follow in Update), so testing something that requires
            // controlling more than one side - taking damage, breaking Meditate on the unit
            // that's actually meditating, ... - needs every one of them to have cards to draw
            // the moment control passes to it, not just the Sorcerer.
            foreach (var unit in _units)
            {
                if (unit.Class == HeroClass.None)
                    continue; // a summoned creature, not a "Summoner" - no deck/hand of its own
                _classDecks[unit.Class] = BuildTestDeck(unit.Class);
                _classHands[unit.Class] = new List<object>();
                _classDiscardPiles[unit.Class] = new List<object>();
            }

            _playerUnit = _units[0];
            _playerHero = _playerUnit; // unused while _testModeActive (that branch always wins first - see Update), kept in sync anyway
            EnsurePlayerHandInitialized();

            _turnSystem = new TurnSystem(_units);
            _turnSystem.UnitTurnStarted += LogUnitTurnDivider;
            _turnSystem.UnitTurnStarted += LogUnitAggro;
            _turnSystem.UnitTurnStarted += ApplyManaAuras;
            _turnSystem.UnitTurnStarted += ResetCombatMessageCount;
            LogUnitTurnDivider(_turnSystem.CurrentUnit); // the constructor already starts the first unit's turn before this subscription could catch it
            LogUnitAggro(_turnSystem.CurrentUnit);
            ApplyManaAuras(_turnSystem.CurrentUnit);
            ResetCombatMessageCount(_turnSystem.CurrentUnit);
            _renderer.CameraPosition = _hexGrid.HexToWorld(1, 1);

            // Reset every input-mode flag - whatever was on screen before "test mode" was typed
            // (a targeting mode, an open menu, ...) no longer refers to anything valid.
            _gameState = GameState.Playing;
            _turnMenuActive = false;
            _cardMenuActive = false;
            _itemsMenuActive = false;
            _itemDetailActive = false;
            _attackMenuActive = false;
            _targetingModeActive = false;
            _coneAimingModeActive = false;
            _teleportModeActive = false;
            _allyTargetModeActive = false;
            _tameBeastModeActive = false;
            _trapPlacementModeActive = false;
            _leapModeActive = false;
            _releaseModeActive = false;
            _placedTraps.Clear();
            _droppedItems.Clear();
            _summonPlacementModeActive = false;
            _portalPlacementModeActive = false;
            _movementModeActive = false;
            _viewingMap = false;

            var playerDeck = _classDecks[_playerUnit.Class];
            int untestedCount = playerDeck.Count(c => !IsCardTested(c));
            return $"Test Mode active - 3x3 grass map, {_units.Count} classes spawned, every unit player-controlled on its own turn (starting with {_playerUnit.Name}). "
                + $"Deck: {playerDeck.Count} cards each ({untestedCount} untested, at the top). AP/MP effectively unlimited.";
        }

        /// <summary>Every SpellCard `heroClass` actually has access to (its own cards + every Generic one - same rule the real Cards menu uses, SpellCatalog.GetSpellsForClass) plus every SummonCard that exists (summons aren't class-gated - any Summoner can use any of them), Untested ones first (stable within each group - catalog declaration order) - see Combat.TestedCards for what "tested" means and how a card gets marked. Deliberately ignores the normal ~30-card deck cap, but NOT class restrictions - those stayed accurate even in Test Mode once every class became player-controlled on its own turn (see EnterTestMode); a per-class deck built from GetAllCards() instead would hand e.g. the Cleric Sorcerer-only spells like Mana Shield, which it could never actually draw in a real match.</summary>
        private List<object> BuildTestDeck(HeroClass heroClass)
        {
            var all = new List<object>();
            all.AddRange(SpellCatalog.GetSpellsForClass(heroClass));
            all.AddRange(SummonCatalog.AllSummons);
            var ordered = all.OrderBy(IsCardTested).ToList();

            return ordered;
        }

        private static bool IsCardTested(object card) => card switch
        {
            SpellCard spell => spell.Tested,
            SummonCard summon => summon.Tested,
            _ => false,
        };

        /// <summary>The original 20x15 procedural map this game shipped with, kept fully intact and callable - EnterOriginalMode uses it to switch back - even while Test Mode is the default boot configuration for now (see EnterTestMode/Initialize).</summary>
        private Map BuildOriginalMap()
        {
            int mapSeed = Environment.TickCount; // Or use a fixed seed like 12345 for reproducibility
            var generator = new MapGenerator(mapSeed);
            return generator.GenerateMap(20, 15, scale: 0.08f, octaves: 4);
        }

        /// <summary>
        /// Dev-console "original mode" handler (see DevConsole.OriginalModeCallback) - the
        /// reverse of EnterTestMode: switches back to the normal 20x15 procedural map
        /// (BuildOriginalMap), normal character select (rather than auto-picking the Sorcerer),
        /// and standard ~30-card shuffled decks/AP/MP for all 4 classes.
        /// </summary>
        private string EnterOriginalMode()
        {
            GameLogger.ResetForNewMatch();
            _hasLoggedAnyUnitTurn = false;

            _hexGrid = new HexGrid(20, 15, tileSize: 32f);
            _map = BuildOriginalMap();
            _console.SetMap(_map);

            _units.Clear();
            SpawnUnits();
            foreach (var unit in _units)
                unit.LoadContent(Content, GraphicsDevice);

            SpawnInitialWildAnimals();

            _testModeActive = false;
            _playerUnit = null; // back through character select
            _playerHero = null;
            GenerateClassDecks();

            _turnSystem = new TurnSystem(_units);
            _turnSystem.UnitTurnStarted += LogUnitTurnDivider;
            _turnSystem.UnitTurnStarted += LogUnitAggro;
            _turnSystem.UnitTurnStarted += CheckWildAnimalSpawn;
            _turnSystem.UnitTurnStarted += ApplyManaAuras;
            _turnSystem.UnitTurnStarted += ResetCombatMessageCount;
            LogUnitTurnDivider(_turnSystem.CurrentUnit); // the constructor already starts the first unit's turn before this subscription could catch it
            LogUnitAggro(_turnSystem.CurrentUnit);
            ApplyManaAuras(_turnSystem.CurrentUnit);
            ResetCombatMessageCount(_turnSystem.CurrentUnit);
            _lastWildSpawnCheckTurn = 0;
            _renderer.CameraPosition = _hexGrid.HexToWorld(10, 7);
            _cameraZoomTarget = 2.5f;

            _gameState = GameState.CharacterSelect;
            _selectedCharacterIndex = 0;

            // Reset every input-mode flag, same reasoning as EnterTestMode.
            _turnMenuActive = false;
            _cardMenuActive = false;
            _itemsMenuActive = false;
            _itemDetailActive = false;
            _attackMenuActive = false;
            _targetingModeActive = false;
            _coneAimingModeActive = false;
            _teleportModeActive = false;
            _allyTargetModeActive = false;
            _tameBeastModeActive = false;
            _trapPlacementModeActive = false;
            _leapModeActive = false;
            _releaseModeActive = false;
            _placedTraps.Clear();
            _droppedItems.Clear();
            _summonPlacementModeActive = false;
            _portalPlacementModeActive = false;
            _movementModeActive = false;
            _viewingMap = false;

            return "Original Mode restored - normal 20x15 procedural map, character select, standard decks/AP/MP.";
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // Initialize renderer HERE (after SpriteBatch exists)
            _renderer = new HexGridRenderer(_hexGrid, _spriteBatch, GraphicsDevice);

            // Center on whichever unit's turn it is rather than a hardcoded hex (used to be
            // HexToWorld(10, 7), a fixed point on the original 20x15 map specifically - once
            // Test Mode's 3x3 map could also be the active one at this point, that fixed point
            // would aim the camera at empty space off the edge of it). MonoGame calls LoadContent
            // automatically right after Initialize() returns - before the Start Menu has had a
            // chance to show, let alone before the player has picked Arena/Debug - so _turnSystem
            // doesn't exist yet at this point any more (EnterTestMode/EnterOriginalMode haven't
            // run). Skipping this is safe either way: whichever mode the player picks sets
            // _renderer.CameraPosition itself once it actually runs.
            if (_turnSystem != null)
                _renderer.CameraPosition = _turnSystem.CurrentUnit.Position;

            // Load unit sprites
            foreach (var unit in _units)
            {
                unit.LoadContent(Content, GraphicsDevice);
            }

            // Load character-select avatar portraits (direct disk load, same fallback BaseUnit uses)
            _avatarTextures = new Texture2D[AvatarFileNames.Length];
            for (int i = 0; i < AvatarFileNames.Length; i++)
            {
                string filePath = Path.Combine("Content", "imgs", "Avatar", AvatarFileNames[i] + ".png");
                if (File.Exists(filePath))
                {
                    using var stream = File.OpenRead(filePath);
                    _avatarTextures[i] = Texture2D.FromStream(GraphicsDevice, stream);
                }
            }

            // Load the spell card template (per-card art and the finished art+template composite
            // are both built lazily, see GetCardArt/GetCardComposite).
            _spellCardTemplate = LoadTextureFromDisk(Path.Combine("Content", "imgs", "Cards", "Spells", "SpellCard.png"));

            // Same deal for the summon card template (see GetSummonCardArt/GetSummonCardComposite).
            _summonCardTemplate = LoadTextureFromDisk(Path.Combine("Content", "imgs", "Cards", "Summons", "SummonCard.png"));

            // Placeholder world sprite for every Objects.Trap (e.g. Place Trap's "Bearat Trap") -
            // no dedicated trap art exists yet, so this reuses the same generic Pawn placeholder
            // several summons without unique art already use (see Draw's trap-rendering pass).
            _trapSpriteTexture = LoadTextureFromDisk(Path.Combine("Content", "imgs", "sprites", "units", "Pawn_Sprite_1.png"));

            // Load the per-stat icons used by the on-screen stat-change display.
            foreach (string statCode in new[] { "STR", "ACC", "DEF", "RES", "INT" })
            {
                var texture = LoadTextureFromDisk(Path.Combine("Content", "imgs", "UI", "StatIcons", statCode + ".png"));
                if (texture != null)
                    _statIcons[statCode] = texture;
            }

            // Load or create font (monospace for console)
            try
            {
                _font = Content.Load<SpriteFont>("Fonts/DefaultFont");
            }
            catch
            {
                // If no font exists, we'll skip text rendering
                _font = null;
            }

            // Create 1x1 white pixel for UI drawing
            _whitePixel = new Texture2D(GraphicsDevice, 1, 1);
            _whitePixel.SetData(new[] { Color.White });
        }

        /// <summary>Load a texture directly from disk (bypassing the Content pipeline), or null if the file doesn't exist.</summary>
        private Texture2D LoadTextureFromDisk(string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            using var stream = File.OpenRead(filePath);
            return Texture2D.FromStream(GraphicsDevice, stream);
        }

        /// <summary>Get (loading and caching on first use) a spell card's art texture.</summary>
        private Texture2D GetCardArt(SpellCard card)
        {
            if (!_cardArtCache.TryGetValue(card.ArtAssetPath, out var texture))
            {
                texture = LoadTextureFromDisk(Path.Combine("Content", card.ArtAssetPath.Replace('/', Path.DirectorySeparatorChar) + ".png"));
                _cardArtCache[card.ArtAssetPath] = texture;
            }
            return texture;
        }

        // A card template's art window is never guaranteed to be a plain axis-aligned rectangle
        // (the Spell template has a curved notch in its top-right corner clearing the cost
        // circle; the Summon template has seven notches down its left edge clearing the stat
        // bars) - a fractional-region rectangle either misses those notches or leaves a seam
        // where the assumed edge doesn't match the real one. FloodFillMask instead flood-fills
        // the template's actual pixels from a seed point to get the exact shape, computed once
        // and cached; CompositeArtIntoMask then resamples art directly into the template's own
        // pixel buffer wherever that mask is true, as a single finished texture per card - no
        // runtime draw-order or blend-state step that could reintroduce a gap.
        private static bool[] FloodFillMask(Color[] pixels, int w, int h, int seedX, int seedY, Func<Color, bool> isMatch, out Rectangle bounds)
        {
            bounds = Rectangle.Empty;
            int seedIndex = seedY * w + seedX;
            if (!isMatch(pixels[seedIndex]))
                return null;

            var visited = new bool[w * h];
            var queue = new Queue<int>();
            visited[seedIndex] = true;
            queue.Enqueue(seedIndex);

            int minX = w, maxX = 0, minY = h, maxY = 0;
            while (queue.Count > 0)
            {
                int idx = queue.Dequeue();
                int x = idx % w;
                int y = idx / w;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;

                if (x > 0) { int n = idx - 1; if (!visited[n] && isMatch(pixels[n])) { visited[n] = true; queue.Enqueue(n); } }
                if (x < w - 1) { int n = idx + 1; if (!visited[n] && isMatch(pixels[n])) { visited[n] = true; queue.Enqueue(n); } }
                if (y > 0) { int n = idx - w; if (!visited[n] && isMatch(pixels[n])) { visited[n] = true; queue.Enqueue(n); } }
                if (y < h - 1) { int n = idx + w; if (!visited[n] && isMatch(pixels[n])) { visited[n] = true; queue.Enqueue(n); } }
            }

            bounds = new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
            return visited;
        }

        /// <summary>Resample art into a template's pixel buffer wherever mask is true, within bounds (art is scaled to fill bounds; only masked pixels are overwritten).</summary>
        private static void CompositeArtIntoMask(Color[] templatePixels, int w, bool[] mask, Rectangle bounds, Color[] artPixels, int artW, int artH)
        {
            int x0 = bounds.X, y0 = bounds.Y, rectW = bounds.Width, rectH = bounds.Height;
            for (int y = 0; y < rectH; y++)
            {
                int ty = y0 + y;
                int sy = Math.Min(artH - 1, y * artH / rectH);
                int rowBase = ty * w;
                int artRowBase = sy * artW;
                for (int x = 0; x < rectW; x++)
                {
                    int tx = x0 + x;
                    if (!mask[rowBase + tx])
                        continue;
                    int sx = Math.Min(artW - 1, x * artW / rectW);
                    templatePixels[rowBase + tx] = artPixels[artRowBase + sx];
                }
            }
        }

        private bool[] _spellCardArtMask;
        private Rectangle _spellCardArtBounds;

        private static bool IsNearWhite(Color c) => c.R > 230 && c.G > 230 && c.B > 230;

        /// <summary>Flood-fill the spell template's art window from a seed point at CardArtRegion's center.</summary>
        private void BuildSpellCardArtMask()
        {
            int w = _spellCardTemplate.Width;
            int h = _spellCardTemplate.Height;
            var pixels = new Color[w * h];
            _spellCardTemplate.GetData(pixels);

            int seedX = (int)((CardArtRegion.x0 + CardArtRegion.x1) / 2f * w);
            int seedY = (int)((CardArtRegion.y0 + CardArtRegion.y1) / 2f * h);
            _spellCardArtMask = FloodFillMask(pixels, w, h, seedX, seedY, IsNearWhite, out _spellCardArtBounds);
        }

        /// <summary>Get (building and caching on first use) a spell card's finished art+template texture. See BuildSpellCardArtMask/CompositeArtIntoMask.</summary>
        private Texture2D GetCardComposite(SpellCard card)
        {
            if (_cardCompositeCache.TryGetValue(card.Name, out var cached))
                return cached;

            Texture2D composite = null;
            if (_spellCardTemplate != null)
            {
                if (_spellCardArtMask == null)
                    BuildSpellCardArtMask();

                int w = _spellCardTemplate.Width;
                int h = _spellCardTemplate.Height;
                var pixels = new Color[w * h];
                _spellCardTemplate.GetData(pixels);

                Texture2D art = GetCardArt(card);
                if (art != null && _spellCardArtMask != null)
                {
                    var artPixels = new Color[art.Width * art.Height];
                    art.GetData(artPixels);
                    CompositeArtIntoMask(pixels, w, _spellCardArtMask, _spellCardArtBounds, artPixels, art.Width, art.Height);
                }

                composite = new Texture2D(GraphicsDevice, w, h);
                composite.SetData(pixels);
            }

            _cardCompositeCache[card.Name] = composite;
            return composite;
        }

        /// <summary>Get (loading and caching on first use) a summon card's art texture.</summary>
        private Texture2D GetSummonCardArt(SummonCard card)
        {
            if (!_summonArtCache.TryGetValue(card.ArtAssetPath, out var texture))
            {
                texture = LoadTextureFromDisk(Path.Combine("Content", card.ArtAssetPath.Replace('/', Path.DirectorySeparatorChar) + ".png"));
                _summonArtCache[card.ArtAssetPath] = texture;
            }
            return texture;
        }

        private bool[] _summonCardArtMask;
        private Rectangle _summonCardArtBounds;

        /// <summary>
        /// Flood-fill the summon template's art window from a seed point at SummonArtRegion's
        /// center. Unlike the Spell template's near-white window, the Summon template's window
        /// is a flat maroon/red fill, so the match is by color distance from the seed itself
        /// rather than a fixed "near white" test.
        /// </summary>
        private void BuildSummonCardArtMask()
        {
            int w = _summonCardTemplate.Width;
            int h = _summonCardTemplate.Height;
            var pixels = new Color[w * h];
            _summonCardTemplate.GetData(pixels);

            int seedX = (int)((SummonArtRegion.x0 + SummonArtRegion.x1) / 2f * w);
            int seedY = (int)((SummonArtRegion.y0 + SummonArtRegion.y1) / 2f * h);
            Color seedColor = pixels[seedY * w + seedX];
            const int tolerance = 20;
            bool IsCloseToSeed(Color c) =>
                Math.Abs(c.R - seedColor.R) <= tolerance &&
                Math.Abs(c.G - seedColor.G) <= tolerance &&
                Math.Abs(c.B - seedColor.B) <= tolerance;

            _summonCardArtMask = FloodFillMask(pixels, w, h, seedX, seedY, IsCloseToSeed, out _summonCardArtBounds);
        }

        /// <summary>Get (building and caching on first use) a summon card's finished art+template texture. See BuildSummonCardArtMask/CompositeArtIntoMask.</summary>
        private Texture2D GetSummonCardComposite(SummonCard card)
        {
            if (_summonCompositeCache.TryGetValue(card.Name, out var cached))
                return cached;

            Texture2D composite = null;
            if (_summonCardTemplate != null)
            {
                if (_summonCardArtMask == null)
                    BuildSummonCardArtMask();

                int w = _summonCardTemplate.Width;
                int h = _summonCardTemplate.Height;
                var pixels = new Color[w * h];
                _summonCardTemplate.GetData(pixels);

                Texture2D art = GetSummonCardArt(card);
                if (art != null && _summonCardArtMask != null)
                {
                    var artPixels = new Color[art.Width * art.Height];
                    art.GetData(artPixels);
                    CompositeArtIntoMask(pixels, w, _summonCardArtMask, _summonCardArtBounds, artPixels, art.Width, art.Height);
                }

                composite = new Texture2D(GraphicsDevice, w, h);
                composite.SetData(pixels);
            }

            _summonCompositeCache[card.Name] = composite;
            return composite;
        }

        private void RegisterDefaultAssets()
        {
            // Register some default assets (these are placeholders—sprites/models would be loaded from files)
            _assetRegistry.Register("mountain_standard", new AssetReference
            {
                Key = "mountain_standard",
                Type = AssetReference.AssetType.Model3D,
                Path = "models/mountain_01.fbx",
                Scale = 1.5f
            });

            _assetRegistry.Register("tree_oak", new AssetReference
            {
                Key = "tree_oak",
                Type = AssetReference.AssetType.Sprite,
                Path = "sprites/tree_oak.png",
                Scale = 2.0f
            });

            _assetRegistry.Register("unit_warrior", new AssetReference
            {
                Key = "unit_warrior",
                Type = AssetReference.AssetType.Sprite,
                Path = "sprites/units/warrior.png",
                Scale = 1.0f
            });

            _assetRegistry.Register("furniture_table", new AssetReference
            {
                Key = "furniture_table",
                Type = AssetReference.AssetType.Sprite,
                Path = "sprites/furniture/table.png",
                Scale = 1.0f
            });
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            var keyboardState = Keyboard.GetState();
            var mouseState = Mouse.GetState();

            if (_gameState == GameState.StartMenu)
            {
                HandleStartMenuInput(keyboardState, mouseState);
            }
            else if (_gameState == GameState.CharacterSelect)
            {
                HandleCharacterSelectInput(keyboardState, mouseState);
            }
            else if (_gameState == GameState.MatchOver)
            {
                // Nothing to update - just showing the result screen (see Draw).
            }
            else
            {
                // Leveling System incentive: any beast (Wild-faction, tamed or not) that just
                // died drops something on the ground at its own position, BEFORE the sweep
                // below removes it - same per-frame catch-all reasoning as that sweep (every
                // death cause, not just a direct attack), so a beast that bleeds out still drops
                // its loot even though AttackResolver's own kill check (no attacker to credit)
                // wouldn't have granted a level for it. Meat is the default drop for every beast
                // EXCEPT the Aetherfluff Beetle, a mana-aura creature (see ManaAuraRange/
                // ApplyManaAuras) that drops 2 Minor MP Potions instead - thematically fitting
                // (it IS the mana critter) and, practically, the only species-specific death
                // drop that exists right now, so a direct type check is simplest rather than
                // adding a generic "what does this species drop" field nothing else needs yet.
                foreach (var deadBeast in _units.Where(u => !u.IsAlive && u.Factions.Contains(Faction.Wild)))
                {
                    if (deadBeast is AetherfluffBeetle)
                    {
                        _droppedItems.Add(new DroppedItem(ConsumableCatalog.MinorMPPotion, deadBeast.Position));
                        _droppedItems.Add(new DroppedItem(ConsumableCatalog.MinorMPPotion, deadBeast.Position));
                    }
                    else
                    {
                        _droppedItems.Add(new DroppedItem(ConsumableCatalog.Meat, deadBeast.Position));
                    }
                }

                // Neither a summoned creature NOR an untamed Wild beast leaves a corpse the way a
                // hero (one of the 4 summoners) does - the instant one's HP reaches 0 (a direct
                // hit, a Bleed tick, anything else that can zero out HP), it's removed from the
                // battlefield outright instead of lingering with the gray "DEAD" tint/label a
                // hero gets (see Draw's unit-rendering loop). A per-frame sweep catches every
                // death cause uniformly rather than hunting down each individual path that can
                // reduce HP to 0. A hero (Owner == null, not Faction.Wild) is deliberately left
                // alone here - its corpse stays exactly as it already did. A TAMED beast (Owner
                // set) is already covered by the Owner != null half, same as any other summon.
                _units.RemoveAll(u => !u.IsAlive && (u.Owner != null || u.Factions.Contains(Faction.Wild)));

                // FFA win check - a unit's team is itself, or whoever summoned it (see
                // GetTeamRoot/BaseUnit.Owner); the moment at most one team still has a living
                // member, the match is over. A lone surviving summon still counts as its owner's
                // team winning, even if the owner itself has already Fainted. An untamed Wild
                // beast (Owner == null) is excluded entirely from this count - GetTeamRoot would
                // otherwise treat it as its OWN one-unit "team" (nobody owns it, so it roots to
                // itself), which let a wild animal surviving on the field keep the match alive
                // forever after every real summoner but one had already been wiped out. Suppressed
                // entirely in Test Mode - it spawns all 4 classes as separate "teams" with AI that
                // can still attack if already in range, and a stray kill ending the "match" would
                // cut a testing session short for no reason.
                var stillStandingTeams = _units
                    .Where(u => !u.IsFainted && !(u.Owner == null && u.Factions.Contains(Faction.Wild)))
                    .Select(GetTeamRoot).Distinct().ToList();
                if (!_testModeActive && stillStandingTeams.Count <= 1)
                {
                    _matchWinner = stillStandingTeams.Count == 1 ? stillStandingTeams[0] : null;
                    _gameState = GameState.MatchOver;
                    _previousKeyboardState = keyboardState;
                    _previousMouseState = mouseState;
                    base.Update(gameTime);
                    return;
                }

                // Toggle console with grave key (~)
                if (keyboardState.IsKeyDown(Keys.OemTilde) && !_previousKeyboardState.IsKeyDown(Keys.OemTilde))
                {
                    _consoleOpen = !_consoleOpen;
                }

                if (_consoleOpen)
                {
                    // Console input handling
                    HandleConsoleInput(keyboardState, mouseState);
                }
                else if (_devSummonModeActive)
                {
                    // Dev-console "summon" placement - same full freeze of the normal turn
                    // system _consoleOpen gets above, so nothing else can act mid-placement.
                    HandleDevSummonPlacementInput(keyboardState, mouseState);
                }
                else
                {
                    // Update turn system
                    float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
                    _turnSystem.Update(deltaTime);

                    // Advance every unit's walk animation (see BaseUnit.SetMovementPath) - a
                    // no-op for anyone not currently mid-move.
                    foreach (var unit in _units)
                        unit.UpdateMovementAnimation(deltaTime);

                    // Advance (and drop once finished) every active slash/shooting attack effect.
                    for (int i = _activeAttackEffects.Count - 1; i >= 0; i--)
                    {
                        _activeAttackEffects[i].Elapsed += deltaTime;
                        if (_activeAttackEffects[i].Elapsed >= _activeAttackEffects[i].Duration)
                            _activeAttackEffects.RemoveAt(i);
                    }

                    // Advance (and drop once finished) every active hit-reaction (see SpawnAttackAnimations).
                    // Elapsed counts from the moment the attack was spawned, not from when the
                    // reaction itself starts playing - StartDelay (0 for Slash, the projectile's
                    // travel time for Shooting) is what the reaction waits out before it begins.
                    for (int i = _activeHitReactions.Count - 1; i >= 0; i--)
                    {
                        _activeHitReactions[i].Elapsed += deltaTime;
                        if (_activeHitReactions[i].Elapsed >= _activeHitReactions[i].StartDelay + _activeHitReactions[i].Duration)
                            _activeHitReactions.RemoveAt(i);
                    }

                    // Advance (and drop once finished) every active floating damage number (see SpawnDamageNumber).
                    for (int i = _activeDamageNumbers.Count - 1; i >= 0; i--)
                    {
                        _activeDamageNumbers[i].Elapsed += deltaTime;
                        if (_activeDamageNumbers[i].Elapsed >= _activeDamageNumbers[i].Duration)
                            _activeDamageNumbers.RemoveAt(i);
                    }

                    if (_combatLogTimer > 0f)
                        _combatLogTimer -= deltaTime;

                    // Camera continuously tracks whichever unit is acting, so it follows
                    // smoothly even as that unit walks tile-by-tile mid-turn (its Position
                    // animates via UpdateMovementAnimation above) instead of only snapping once
                    // when the turn starts. Fully suppressed (no auto-recenter at all) while
                    // free-looking the map (_viewingMap) or picking a Move destination
                    // (_movementModeActive) - both hand the camera entirely to WASD/scroll
                    // panning instead (see HandleMapControls); ResumeFromMapView and
                    // OpenMovementMode each do their own one-time snap on entry.
                    if (_viewingMap || _movementModeActive)
                    {
                        // Manual panning owns the camera here - nothing to do.
                    }
                    else
                    {
                        // Once-per-turn setup, keyed off the unit actually changing rather than
                        // "the camera happens to be settled" (the previous trigger condition) -
                        // that used to re-fire every single frame once the pan finished, resetting
                        // _hasAiActedThisTurn back to false and re-invoking RunSimpleAI repeatedly
                        // for the rest of a slow unit's dwell time (harmless in practice, since AP
                        // was already spent and a repeat call found nothing left to do, but pure
                        // waste). Doing it here also means _playerUnit reassignment and AI Turn
                        // Batching's decision (ShouldBatchAiTurn, right below) are both already
                        // correct from this turn's very first frame, not just once the pan settles.
                        bool isNewUnitTurn = _lastTurnStartUnit != _turnSystem.CurrentUnit;
                        if (isNewUnitTurn)
                        {
                            _lastTurnStartUnit = _turnSystem.CurrentUnit;
                            _hasAutoAdvancedThisTurn = false;
                            _hasAiActedThisTurn = false;

                            // Test Mode: the player controls EVERY unit on its own turn, not just
                            // whichever one EnterTestMode started with - needed to test taking
                            // damage (attack a unit on someone ELSE's controlled turn) and
                            // breaking Meditate (control the meditating unit directly instead of
                            // watching a no-op AI sit there). EnterTestMode already gave every
                            // class its own deck/hand for exactly this. Closing whatever UI
                            // sub-mode the previous unit's turn left open (it shouldn't have left
                            // one, but this is dev tooling - better safe) so the new unit starts
                            // clean and its turn menu auto-opens via the normal path below.
                            if (_testModeActive && _turnSystem.CurrentUnit != _playerUnit)
                            {
                                _playerUnit = _turnSystem.CurrentUnit;
                                EnsurePlayerHandInitialized();

                                _turnMenuActive = false;
                                _cardMenuActive = false;
                                _itemsMenuActive = false;
                                _itemDetailActive = false;
                                _attackMenuActive = false;
                                _targetingModeActive = false;
                                _coneAimingModeActive = false;
                                _teleportModeActive = false;
                                _allyTargetModeActive = false;
                                _tameBeastModeActive = false;
                                _trapPlacementModeActive = false;
                                _leapModeActive = false;
                                _releaseModeActive = false;
                                _summonPlacementModeActive = false;
                                _portalPlacementModeActive = false;
                                _movementModeActive = false;
                            }
                            // Normal (Arena) play: the player's own hero controls its own turn as
                            // always, but a ControllableBySummoner summon of THEIRS (see
                            // BaseUnit.Behavior/SummonBehavior) also gets handed control for its
                            // own turn, instead of falling to the placeholder AI (RunSimpleAI),
                            // which has no concept of teams and would simply attack whoever
                            // _playerUnit happens to be - exactly the "summons attack their own
                            // summoner" bug this fixes. Every OTHER unit's turn (an enemy hero, or
                            // some future non-ControllableBySummoner summon) hands control back to
                            // the real hero, so isPlayerTurn below is false for it and it falls
                            // through to RunSimpleAI as it always has.
                            else if (!_testModeActive)
                            {
                                BaseUnit desiredController = IsPlayerControllableSummon(_turnSystem.CurrentUnit)
                                    ? _turnSystem.CurrentUnit
                                    : _playerHero;

                                if (desiredController != null && desiredController != _playerUnit)
                                {
                                    _playerUnit = desiredController;
                                    if (desiredController.Class != HeroClass.None)
                                        EnsurePlayerHandInitialized();

                                    _turnMenuActive = false;
                                    _cardMenuActive = false;
                                    _itemsMenuActive = false;
                                    _itemDetailActive = false;
                                    _attackMenuActive = false;
                                    _targetingModeActive = false;
                                    _coneAimingModeActive = false;
                                    _teleportModeActive = false;
                                    _allyTargetModeActive = false;
                                    _tameBeastModeActive = false;
                                    _trapPlacementModeActive = false;
                                    _leapModeActive = false;
                                    _releaseModeActive = false;
                                    _summonPlacementModeActive = false;
                                    _portalPlacementModeActive = false;
                                    _movementModeActive = false;
                                }
                            }

                            // AI Turn Batching - decided fresh every turn, right after the control
                            // handoff above so _playerUnit already reflects whoever controls THIS
                            // turn. Always false for the player's own turn.
                            _currentUnitTurnBatched = _turnSystem.CurrentUnit != _playerUnit && ShouldBatchAiTurn(_turnSystem.CurrentUnit);
                        }

                        // A batched unit's turn (see ShouldBatchAiTurn/the AI-driving branch
                        // below, which fast-forwards it with no dwell either) gets no camera
                        // attention at all - left exactly wherever it already was, rather than
                        // panning to every wild animal/unseen enemy's turn one at a time.
                        if (!_currentUnitTurnBatched && _targetingModeActive)
                        {
                            // Picking a target for an attack or a damage-dealing spell cast (both
                            // go through _targetingModeActive - see HandleTargetingInput): ease
                            // toward whichever candidate the mouse is CURRENTLY over
                            // (_hoveredAttackTarget, recomputed every Draw frame) instead of the
                            // acting unit's own position - a target "a bit far" away used to need
                            // manual panning first before it could even be clicked precisely.
                            //
                            // Bug fix: this used to be an if/else-if alongside the normal per-turn
                            // lock below, so the INSTANT hover was lost (e.g. the candidate's own
                            // approach moved it out from under a stationary mouse, a near-certainty
                            // with a gradual ease) it fell through to that else-if and hard-SNAPPED
                            // the camera back onto the caster - which could then put the candidate
                            // back under the mouse, re-hovering it, easing toward it again, losing
                            // hover again, snapping back again... a genuine jitter loop, not the
                            // intended "just stay put" behavior the comment below already described
                            // but the code never actually implemented. Now entirely self-contained:
                            // while actually hovering something, ease toward it (gradual, not an
                            // instant snap, for the same reason - closing the gap in one frame would
                            // risk the exact same un-hover-then-snap-back cycle); the instant nothing
                            // IS hovered, this whole block simply does nothing at all, leaving the
                            // camera exactly where it last was - it can no longer fall through to the
                            // caster-lock branch while targeting mode is still open, only once it
                            // actually closes (cast or cancel) does the normal per-turn lock resume.
                            if (_hoveredAttackTarget != null)
                            {
                                _cameraTarget = _hoveredAttackTarget.Position;
                                float followAmount = 1f - MathF.Pow(0.0001f, deltaTime);
                                _renderer.CameraPosition = Vector2.Lerp(_renderer.CameraPosition, _cameraTarget, MathHelper.Clamp(followAmount, 0f, 1f));
                            }
                        }
                        else if (!_currentUnitTurnBatched)
                        {
                            _cameraTarget = _turnSystem.CurrentUnit.Position;

                            if (_turnSystem.TransitioningCamera)
                            {
                                float progress = _turnSystem.CameraTransitionElapsed / 1.5f;  // Normalize to 0-1
                                progress = MathHelper.Clamp(progress, 0f, 1f);

                                // Smooth interpolation (easing)
                                float easeProgress = progress * progress * (3f - 2f * progress);  // Smoothstep

                                _renderer.CameraPosition = Vector2.Lerp(_renderer.CameraPosition, _cameraTarget, easeProgress);
                                _renderer.ZoomLevel = MathHelper.Lerp(_renderer.ZoomLevel, _cameraZoomTarget, easeProgress);
                            }
                            else
                            {
                                // Initial swoop-in is done - lock on exactly from here so the
                                // camera can never lag behind a unit mid-walk.
                                _renderer.CameraPosition = _cameraTarget;
                                _renderer.ZoomLevel = _cameraZoomTarget;
                            }
                        }
                    }

                    bool isPlayerTurn = _turnSystem.CurrentUnit == _playerUnit;

                    // Nothing acts - neither the player nor AI - until the "Turn X - Go!" banner
                    // finishes (2s). The camera still swoops in underneath it (that lerp is
                    // above, ungated), but this is what actually stops an AI unit from moving
                    // the instant a match/Turn starts, before the player's even had a chance to
                    // register whose turn it is. Same freeze while any attack animation
                    // (SpawnAttackAnimations - a swing or a traveling projectile like Arcane
                    // Missile) is still playing, so the menu doesn't instantly reopen over it and
                    // the game visibly pauses on the shot instead - see Draw's turn-menu check,
                    // which uses the same _activeAttackEffects.Count condition to hide it.
                    if (_turnSystem.ShowingTurnAnnouncement || _activeAttackEffects.Count > 0)
                    {
                        // Waiting out the announcement/animation - no input handling, no AI, no auto-advance.
                    }
                    else if (isPlayerTurn && !_turnSystem.TransitioningCamera)
                    {
                        if (_playerUnit.GetRestrictingEffect() != null)
                        {
                            // A restricting StatusEffect (Stunned, Deep Sleep, Meditating, ...)
                            // overrides everything else - the only choices are trying its own
                            // end-it-early action or ending the turn without acting (see
                            // HandleRestrictedTurnInput).
                            HandleRestrictedTurnInput(keyboardState, mouseState);
                        }
                        else if (_viewingMap)
                        {
                            // Free-look mode: WASD panning happens below in HandleMapControls.
                            // E snaps the camera back and reopens the menu.
                            if (keyboardState.IsKeyDown(Keys.E) && !_previousKeyboardState.IsKeyDown(Keys.E))
                                ResumeFromMapView();
                        }
                        else if (_attackMenuActive)
                        {
                            HandleAttackMenuInput(keyboardState, mouseState);
                        }
                        else if (_targetingModeActive)
                        {
                            HandleTargetingInput(keyboardState, mouseState);
                        }
                        else if (_coneAimingModeActive)
                        {
                            HandleConeAimingInput(keyboardState, mouseState);
                        }
                        else if (_teleportModeActive)
                        {
                            HandleTeleportInput(keyboardState, mouseState);
                        }
                        else if (_allyTargetModeActive)
                        {
                            HandleAllyTargetInput(keyboardState, mouseState);
                        }
                        else if (_tameBeastModeActive)
                        {
                            HandleTameBeastInput(keyboardState, mouseState);
                        }
                        else if (_trapPlacementModeActive)
                        {
                            HandleTrapPlacementInput(keyboardState, mouseState);
                        }
                        else if (_leapModeActive)
                        {
                            HandleLeapInput(keyboardState, mouseState);
                        }
                        else if (_releaseModeActive)
                        {
                            HandleReleaseInput(keyboardState, mouseState);
                        }
                        else if (_summonPlacementModeActive)
                        {
                            HandleSummonPlacementInput(keyboardState, mouseState);
                        }
                        else if (_portalPlacementModeActive)
                        {
                            HandlePortalPlacementInput(keyboardState, mouseState);
                        }
                        else if (_movementModeActive)
                        {
                            HandleMovementInput(keyboardState, mouseState);
                        }
                        else if (_cardMenuActive)
                        {
                            HandleCardMenuInput(keyboardState, mouseState);
                        }
                        else if (_itemsMenuActive)
                        {
                            HandleItemsMenuInput(keyboardState, mouseState);
                        }
                        else if (_itemDetailActive)
                        {
                            HandleItemDetailInput(keyboardState, mouseState);
                        }
                        else
                        {
                            // Pause auto-advance and let the player choose an action from the menu
                            if (!_turnMenuActive)
                            {
                                _turnMenuIndex = 0;
                                _turnMenuActive = true;
                            }
                            HandleTurnMenuInput(keyboardState, mouseState);
                        }
                    }
                    else
                    {
                        _turnMenuActive = false;
                        _viewingMap = false;
                        _attackMenuActive = false;
                        _targetingModeActive = false;
                        _coneAimingModeActive = false;
                        _teleportModeActive = false;
                        _allyTargetModeActive = false;
                        _tameBeastModeActive = false;
                        _trapPlacementModeActive = false;
                        _leapModeActive = false;
                        _releaseModeActive = false;
                        _summonPlacementModeActive = false;
                        _portalPlacementModeActive = false;
                        _movementModeActive = false;
                        _cardMenuActive = false;
                        _itemsMenuActive = false;
                        _itemDetailActive = false;

                        // Simple placeholder AI (GDD-pending): walk toward the player once per
                        // turn, so there's something in range to test attacks/spells against.
                        if (_currentUnitTurnBatched)
                        {
                            // AI Turn Batching (see ShouldBatchAiTurn): no camera pan to wait for
                            // and no fixed dwell afterward either - act the instant the turn
                            // starts, then advance the instant it's done acting AND nothing is
                            // still animating (the same _activeAttackEffects gate this whole outer
                            // if/else chain already waits on, so an attack's own swing/projectile
                            // still finishes playing before the next unit's turn begins - this
                            // just skips the ADDITIONAL fixed 3-second hold non-batched turns get
                            // on top of that). The "Other units are taking their turns..." message
                            // (see DrawBottomHint in Draw) covers the whole stretch.
                            if (!_hasAiActedThisTurn)
                            {
                                _hasAiActedThisTurn = true;
                                var batchedUnit = _turnSystem.CurrentUnit;
                                RunSimpleAI(batchedUnit);
                                LogIfAiTurnWasSilent(batchedUnit);
                            }
                            else if (!_hasAutoAdvancedThisTurn)
                            {
                                _hasAutoAdvancedThisTurn = true;
                                _turnSystem.NextUnit();
                            }
                        }
                        else
                        {
                            // Individually-presented AI turn (a currently-visible team unit, or
                            // AI Turn Batching disabled e.g. in Test Mode) - unchanged pacing:
                            // wait for the camera's own swoop-in to finish, THEN act, THEN hold
                            // for a fixed 3-second dwell (measured from turn start, so it overlaps
                            // the swoop) before moving on, so the player has time to actually see
                            // what a visible enemy just did.
                            if (!isPlayerTurn && !_turnSystem.TransitioningCamera && !_hasAiActedThisTurn)
                            {
                                _hasAiActedThisTurn = true;
                                var actingUnit = _turnSystem.CurrentUnit;
                                RunSimpleAI(actingUnit);
                                LogIfAiTurnWasSilent(actingUnit);
                            }

                            // Auto-advance to next unit after 3 seconds (once per unit, non-player units only)
                            if (!isPlayerTurn && _turnSystem.UnitTurnElapsed > 3f && !_hasAutoAdvancedThisTurn)
                            {
                                _hasAutoAdvancedThisTurn = true;
                                _turnSystem.NextUnit();
                            }
                        }
                    }

                    // Map editor controls
                    HandleMapControls(keyboardState, mouseState, deltaTime);
                }
            }

            _previousKeyboardState = keyboardState;
            _previousMouseState = mouseState;

            base.Update(gameTime);
        }

        /// <summary>
        /// Appends actual typed characters to _consoleInput while the console is open - see the
        /// Window.TextInput subscription in Initialize(). Filters out '`'/'~' (toggles the
        /// console, see Update - never typed) and Enter/Backspace (handled via KeyboardState
        /// polling in HandleConsoleInput instead, so they don't double up as control characters).
        /// </summary>
        private void OnConsoleTextInput(object sender, TextInputEventArgs e)
        {
            if (!_consoleOpen)
                return;

            char c = e.Character;
            if (c == '`' || c == '~' || c == '\r' || c == '\n' || c == '\b')
                return;

            _consoleInput += c;
        }

        private void HandleConsoleInput(KeyboardState keyboardState, MouseState mouseState)
        {
            // Backspace
            if (keyboardState.IsKeyDown(Keys.Back) && !_previousKeyboardState.IsKeyDown(Keys.Back) && _consoleInput.Length > 0)
            {
                _consoleInput = _consoleInput.Substring(0, _consoleInput.Length - 1);
            }

            // Paste (Ctrl+V) - Window.TextInput (see OnConsoleTextInput) only ever fires for an
            // actual keystroke, never for a clipboard paste, so without this there was no way to
            // paste a command at all; every character had to be typed by hand, typos included.
            // Collapses a multi-line clipboard payload to a single line (a console command is
            // always one line) rather than embedding literal newlines into _consoleInput.
            bool ctrlHeld = keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl);
            if (ctrlHeld && keyboardState.IsKeyDown(Keys.V) && !_previousKeyboardState.IsKeyDown(Keys.V))
            {
                string clipboardText = WindowsClipboard.GetText();
                if (!string.IsNullOrEmpty(clipboardText))
                    _consoleInput += clipboardText.Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ');
            }

            // Submit command - also snaps the scrollback back to the live/bottom view, same as
            // sending a message in a chat log.
            if (keyboardState.IsKeyDown(Keys.Enter) && !_previousKeyboardState.IsKeyDown(Keys.Enter))
            {
                _console.ExecuteCommand(_consoleInput);
                _consoleInput = "";
                _consoleScrollOffset = 0;
            }

            // Command history - kept on Up/Down (the conventional binding for a command-line
            // prompt, same as a shell); PageUp/PageDown and the mouse wheel scroll the output
            // log instead, below, so there's no ambiguity between the two.
            if (keyboardState.IsKeyDown(Keys.Up) && !_previousKeyboardState.IsKeyDown(Keys.Up))
            {
                _consoleInput = _console.GetPreviousCommand();
            }
            if (keyboardState.IsKeyDown(Keys.Down) && !_previousKeyboardState.IsKeyDown(Keys.Down))
            {
                _consoleInput = _console.GetNextCommand();
            }

            // Scroll the output log: PageUp/PageDown by a full page, mouse wheel a few lines per
            // notch. Clamping against the actual output length happens in DrawConsole.
            if (keyboardState.IsKeyDown(Keys.PageUp) && !_previousKeyboardState.IsKeyDown(Keys.PageUp))
                _consoleScrollOffset += ConsoleVisibleLines;
            if (keyboardState.IsKeyDown(Keys.PageDown) && !_previousKeyboardState.IsKeyDown(Keys.PageDown))
                _consoleScrollOffset = Math.Max(0, _consoleScrollOffset - ConsoleVisibleLines);

            int scrollDelta = mouseState.ScrollWheelValue - _previousMouseState.ScrollWheelValue;
            if (scrollDelta != 0)
            {
                // MonoGame reports 120 units per standard wheel notch - dividing it down gives a
                // few lines per notch rather than a single line, which feels too slow to scan output with.
                _consoleScrollOffset = Math.Max(0, _consoleScrollOffset + scrollDelta / 40);
            }
        }

        /// <summary>Handle input on the boot-time Start Menu (see DrawStartMenu): W/S or mouse-hover to highlight "Arena" or "Debug", click or E to confirm.</summary>
        private void HandleStartMenuInput(KeyboardState keyboardState, MouseState mouseState)
        {
            if (keyboardState.IsKeyDown(Keys.S) && !_previousKeyboardState.IsKeyDown(Keys.S))
                _startMenuIndex = (_startMenuIndex + 1) % StartMenuLabels.Length;
            if (keyboardState.IsKeyDown(Keys.W) && !_previousKeyboardState.IsKeyDown(Keys.W))
                _startMenuIndex = (_startMenuIndex - 1 + StartMenuLabels.Length) % StartMenuLabels.Length;

            Vector2 viewportSize = new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            Rectangle[] optionRects = GetStartMenuOptionRects(viewportSize);

            for (int i = 0; i < optionRects.Length; i++)
            {
                if (optionRects[i].Contains(mouseState.X, mouseState.Y))
                {
                    _startMenuIndex = i;
                    if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
                        ConfirmStartMenuSelection();
                }
            }

            if (keyboardState.IsKeyDown(Keys.E) && !_previousKeyboardState.IsKeyDown(Keys.E))
                ConfirmStartMenuSelection();
        }

        /// <summary>
        /// "Arena" (index 0) routes to EnterOriginalMode - the full 20x15 procedural map, which
        /// itself opens character select (GameState.CharacterSelect), so the player ends up
        /// controlling only whichever single character they then pick, same as the game's
        /// original pre-Test-Mode flow. "Debug" (index 1) routes straight to EnterTestMode - the
        /// small 3x3 map with every unit player-controlled on its own turn - mirroring exactly
        /// what Initialize() used to do unconditionally before this menu existed, including the
        /// one-time camera/auto-advance setup EnterTestMode's caller is responsible for.
        /// </summary>
        private void ConfirmStartMenuSelection()
        {
            if (_startMenuIndex == 0) // Arena
            {
                EnterOriginalMode();
            }
            else // Debug
            {
                EnterTestMode();
                _cameraTarget = _turnSystem.CurrentUnit.Position;
                _hasAutoAdvancedThisTurn = false;
                _hasAiActedThisTurn = false;
            }
        }

        /// <summary>
        /// Handle input on the character-select screen: WASD/mouse-hover to highlight a
        /// portrait, click or E to confirm and start the game with that unit as the player's.
        /// </summary>
        private void HandleCharacterSelectInput(KeyboardState keyboardState, MouseState mouseState)
        {
            // Keyboard navigation across the 2x2 grid (each key sets, rather than toggles,
            // its axis so holding/re-pressing a direction at the edge is a no-op, not a bounce-back)
            if (keyboardState.IsKeyDown(Keys.D) && !_previousKeyboardState.IsKeyDown(Keys.D))
                _selectedCharacterIndex |= 1;
            if (keyboardState.IsKeyDown(Keys.A) && !_previousKeyboardState.IsKeyDown(Keys.A))
                _selectedCharacterIndex &= ~1;
            if (keyboardState.IsKeyDown(Keys.S) && !_previousKeyboardState.IsKeyDown(Keys.S))
                _selectedCharacterIndex |= 2;
            if (keyboardState.IsKeyDown(Keys.W) && !_previousKeyboardState.IsKeyDown(Keys.W))
                _selectedCharacterIndex &= ~2;

            Vector2 viewportSize = new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            Rectangle[] portraitRects = GetCharacterSelectRects(viewportSize);

            for (int i = 0; i < portraitRects.Length; i++)
            {
                if (portraitRects[i].Contains(mouseState.X, mouseState.Y))
                {
                    _selectedCharacterIndex = i;
                    if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
                        ConfirmCharacterSelection();
                }
            }

            if (keyboardState.IsKeyDown(Keys.E) && !_previousKeyboardState.IsKeyDown(Keys.E))
                ConfirmCharacterSelection();
        }

        private void ConfirmCharacterSelection()
        {
            _playerUnit = _units[_selectedCharacterIndex];
            _playerHero = _playerUnit;
            EnsurePlayerHandInitialized();
            _gameState = GameState.Playing;
        }

        /// <summary>Ensure `heroClass` has an initialized hand (drawing a starting 3-card hand from its own deck - see GenerateClassDecks - if it's currently empty). The class-keyed `_classHands`/`_classDecks` dictionaries work identically whether that class happens to be the player's or an AI-controlled hero's (each of the 4 classes belongs to exactly one unit at a time) - this just guarantees one actually exists before anything (the player's own hand UI, or now AI decision-making - see Game1.TryAiSummon) reads it.</summary>
        private List<object> EnsureHandInitialized(HeroClass heroClass)
        {
            if (!_classHands.TryGetValue(heroClass, out var hand) || hand == null)
            {
                hand = new List<object>();
                _classHands[heroClass] = hand;
            }

            if (hand.Count == 0)
            {
                hand.AddRange(DrawCardsFromDeck(heroClass, 3));
                _classHands[heroClass] = hand;
            }

            // Covers the one path DiscardCard's own hook (see EnsureReshuffleCardIfNeeded) can't:
            // a class whose deck was ALREADY empty the very first time its hand is initialized
            // (the draw above found nothing, so hand is still empty too) - no discard ever
            // happened, so that hook never ran.
            EnsureReshuffleCardIfNeeded(heroClass);

            return hand;
        }

        /// <summary>
        /// Reshuffle mechanic (on request): grants `heroClass` a single "Reshuffle" card
        /// (`SpellCatalog.Reshuffle`) the moment its deck AND hand are BOTH completely empty -
        /// the deck ran dry and every card drawn from it has since been used. No-op if either
        /// still has anything in it, or if a Reshuffle card is already sitting in hand (never
        /// grants a second one). Called from DiscardCard (every point a card actually leaves a
        /// hand, player or AI) and EnsureHandInitialized (the one empty-from-the-start edge case
        /// DiscardCard can't see, see its own comment there).
        /// </summary>
        private void EnsureReshuffleCardIfNeeded(HeroClass heroClass)
        {
            if (heroClass == HeroClass.None)
                return;

            bool deckEmpty = !_classDecks.TryGetValue(heroClass, out var deck) || deck == null || deck.Count == 0;
            bool handEmpty = !_classHands.TryGetValue(heroClass, out var hand) || hand == null || hand.Count == 0;
            if (!deckEmpty || !handEmpty)
                return;

            if (hand == null)
            {
                hand = new List<object>();
                _classHands[heroClass] = hand;
            }

            hand.Add(SpellCatalog.Reshuffle);

            if (_playerUnit != null && _playerUnit.Class == heroClass)
                _availableHandCards = new List<object>(hand);
        }

        private void EnsurePlayerHandInitialized()
        {
            if (_playerUnit == null)
                return;

            var hand = EnsureHandInitialized(_playerUnit.Class);

            _availableHandCards = new List<object>(hand);
            if (_availableHandCards.Count > 0)
                _handCardIndex = Math.Clamp(_handCardIndex, 0, _availableHandCards.Count - 1);
        }

        private int GetDrawAPCost(BaseUnit unit)
        {
            if (unit == null)
                return 1;

            int drawCount = unit.CardsDrawnThisTurn;
            return drawCount == 0 ? 1 : 1 << drawCount;
        }

        private List<object> DrawCardsFromDeck(HeroClass heroClass, int count)
        {
            if (!_classDecks.TryGetValue(heroClass, out var deck) || deck == null)
                return new List<object>();

            var drawn = new List<object>();
            for (int i = 0; i < count && deck.Count > 0; i++)
            {
                // Test Mode draws sequentially from the FRONT of the deck instead of randomly -
                // BuildTestDeck sorts Untested cards there, so "Untested cards always come up
                // first" actually holds; a random draw would make that ordering meaningless.
                int cardIndex = _testModeActive ? 0 : _deckRandom.Next(deck.Count);
                drawn.Add(deck[cardIndex]);
                deck.RemoveAt(cardIndex);
            }

            _classDecks[heroClass] = deck;
            return drawn;
        }

        private void TryDrawCardFromDeck()
        {
            if (_playerUnit == null)
                return;

            if (_classHands.TryGetValue(_playerUnit.Class, out var currentHand) && currentHand != null && currentHand.Count >= MaxHandSize)
            {
                ShowCombatMessage($"{_playerUnit.Name}'s hand is full ({MaxHandSize}/{MaxHandSize}) - can't draw another card.");
                return;
            }

            int drawCost = GetDrawAPCost(_playerUnit);
            if (_playerUnit.CurrentAP < drawCost)
                return;

            if (!_classDecks.TryGetValue(_playerUnit.Class, out var deck) || deck == null || deck.Count == 0)
                return;

            var drawnCard = DrawCardsFromDeck(_playerUnit.Class, 1);
            if (drawnCard.Count == 0)
                return;

            if (!_classHands.TryGetValue(_playerUnit.Class, out var hand) || hand == null)
                hand = new List<object>();

            hand.Add(drawnCard[0]);
            _classHands[_playerUnit.Class] = hand;
            _playerUnit.SpendAP(drawCost);
            _playerUnit.RecordCardDraw();

            _availableHandCards = new List<object>(hand);
            _handCardIndex = _availableHandCards.Count - 1;
            _highlightedHandCardIndex = _handCardIndex;
        }

        /// <summary>
        /// Moves `card` from `hand` into that class's discard pile (see _classDiscardPiles) - a
        /// used card now goes SOMEWHERE real instead of just vanishing. Shared by the player's own
        /// RemoveCardFromHand (which also refreshes the player-only hand-UI state on top) and AI's
        /// TryAiSummon (which doesn't need any UI refresh at all, just the hand/discard-pile
        /// bookkeeping) - the one place either path actually removes a card, so the two can never
        /// drift into handling discard differently.
        /// </summary>
        private void DiscardCard(HeroClass heroClass, List<object> hand, object card)
        {
            if (!hand.Remove(card))
                return;

            if (!_classDiscardPiles.TryGetValue(heroClass, out var pile) || pile == null)
            {
                pile = new List<object>();
                _classDiscardPiles[heroClass] = pile;
            }
            pile.Add(card);

            // Reshuffle mechanic: this discard may have just emptied the hand while the deck was
            // ALREADY empty (or vice versa isn't possible - a draw can't happen mid-discard) -
            // check fresh every time a card actually leaves a hand, rather than hunting down
            // every individual path that could cause it.
            EnsureReshuffleCardIfNeeded(heroClass);
        }

        /// <summary>Discard a played card from the player's hand (consumed on a successful cast) and refresh the hand view.</summary>
        private void RemoveCardFromHand(object card)
        {
            if (_classHands.TryGetValue(_playerUnit.Class, out var hand) && hand != null)
                DiscardCard(_playerUnit.Class, hand, card);

            _availableHandCards = new List<object>(hand ?? new List<object>());
            if (_availableHandCards.Count > 0)
            {
                _handCardIndex = Math.Clamp(_handCardIndex, 0, _availableHandCards.Count - 1);
                _highlightedHandCardIndex = _handCardIndex;
            }
            else
            {
                _handCardIndex = 0;
                _highlightedHandCardIndex = null;
            }
        }

        /// <summary>
        /// Dev-console "add card &lt;CardID&gt;" handler (see DevConsole.AddCardCallback): looks the
        /// integer ID up against every SpellCard/SummonCard's stable Id (see "list cards" for the
        /// full table) and adds it straight into the player's current hand, bypassing the deck
        /// entirely - a pure testing shortcut, not a real draw.
        /// </summary>
        private string AddCardToHandCommand(int cardId)
        {
            if (_playerUnit == null)
                return "No player unit active - pick a character first.";

            object card = FindCardById(cardId);
            if (card == null)
                return $"Unknown card ID: {cardId}. Try 'list cards'.";

            if (!_classHands.TryGetValue(_playerUnit.Class, out var hand) || hand == null)
            {
                hand = new List<object>();
                _classHands[_playerUnit.Class] = hand;
            }
            hand.Add(card);

            _availableHandCards = new List<object>(hand);
            _handCardIndex = _availableHandCards.Count - 1;
            _highlightedHandCardIndex = _handCardIndex;

            string name = card is SpellCard spellCard ? spellCard.Name : card is SummonCard summonCard ? summonCard.Name : cardId.ToString();
            return $"Added '{name}' (ID {cardId}) to {_playerUnit.Name}'s hand.";
        }

        /// <summary>
        /// Dev-console "list cards all"/"list cards &lt;ClassID&gt;" handler (see
        /// DevConsole.ListCardsCallback): one "ID: Name" line per card, in catalog order.
        /// classId null (bare "list cards" or "list cards all") lists every card; 0-3
        /// (Sorcerer/Warrior/Cleric/Hunter) restricts the Spell Cards section to that class's
        /// own cards plus the Generic ones everyone gets (same set `GetSpellsForClass` returns
        /// for the real Cards menu) - Summon Cards are always listed in full either way, since
        /// they aren't class-gated.
        /// </summary>
        private List<string> ListCardsCommand(int? classId)
        {
            var lines = new List<string>();

            if (classId.HasValue)
            {
                // 0 = Generic (RequiredClass null - every class gets these, but they're their
                // own category here, not folded into any one class's list); 1-4 = an actual
                // HeroClass, showing ONLY that class's own cards - unlike the real Cards menu
                // (GetSpellsForClass), Generic spells are deliberately excluded here, since the
                // point of asking for "Sorcerer cards" is the Sorcerer-specific roster.
                if (classId.Value == 0)
                {
                    lines.Add("Spell Cards (Generic):");
                    foreach (var card in SpellCatalog.GetAllCards())
                    {
                        if (card.RequiredClass == null)
                            lines.Add($"  {card.Id}: {card.Name}");
                    }
                }
                else
                {
                    HeroClass? heroClass = classId.Value switch
                    {
                        1 => HeroClass.Sorcerer,
                        2 => HeroClass.Warrior,
                        3 => HeroClass.Cleric,
                        4 => HeroClass.Hunter,
                        _ => null,
                    };

                    if (heroClass == null)
                        return new List<string> { $"Unknown class ID: {classId.Value}. Use 0=Generic, 1=Sorcerer, 2=Warrior, 3=Cleric, 4=Hunter." };

                    lines.Add($"Spell Cards ({heroClass}):");
                    foreach (var card in SpellCatalog.GetAllCards())
                    {
                        if (card.RequiredClass == heroClass)
                            lines.Add($"  {card.Id}: {card.Name}");
                    }
                }
            }
            else
            {
                lines.Add("Spell Cards:");
                foreach (var card in SpellCatalog.GetAllCards())
                    lines.Add($"  {card.Id}: {card.Name}");
            }

            lines.Add("Summon Cards:");
            foreach (var card in SummonCatalog.AllSummons)
                lines.Add($"  {card.Id}: {card.Name}");

            return lines;
        }

        /// <summary>Find a SpellCard or SummonCard by its stable integer Id - see AddCardToHandCommand.</summary>
        private static object FindCardById(int cardId)
        {
            foreach (var card in SpellCatalog.GetAllCards())
            {
                if (card.Id == cardId)
                    return card;
            }

            foreach (var card in SummonCatalog.AllSummons)
            {
                if (card.Id == cardId)
                    return card;
            }

            return null;
        }

        /// <summary>
        /// Compute the 4 portrait rectangles for the character-select 2x2 grid, centered onscreen.
        /// </summary>
        private Rectangle[] GetCharacterSelectRects(Vector2 viewportSize)
        {
            const int portraitSize = 128;
            const int spacing = 40;
            int gridWidth = portraitSize * 2 + spacing;
            int gridHeight = portraitSize * 2 + spacing;
            Vector2 origin = new Vector2((viewportSize.X - gridWidth) / 2f, (viewportSize.Y - gridHeight) / 2f);

            var rects = new Rectangle[4];
            for (int i = 0; i < 4; i++)
            {
                int col = i & 1;
                int row = (i >> 1) & 1;
                rects[i] = new Rectangle(
                    (int)(origin.X + col * (portraitSize + spacing)),
                    (int)(origin.Y + row * (portraitSize + spacing)),
                    portraitSize, portraitSize);
            }
            return rects;
        }

        /// <summary>A centered vertical list of StartMenuLabels.Length option boxes - unlike GetMenuOptionRects/GetFixedBottomRightMenuOptionRects, this can't anchor off the player's position (none exists yet at the Start Menu) or a fixed screen corner (this is a full-screen title-style menu, not an in-game submenu).</summary>
        private Rectangle[] GetStartMenuOptionRects(Vector2 viewportSize)
        {
            const int optionWidth = 220;
            const int optionHeight = 44;
            const int spacing = 16;

            float totalHeight = StartMenuLabels.Length * optionHeight + (StartMenuLabels.Length - 1) * spacing;
            Vector2 origin = new Vector2((viewportSize.X - optionWidth) / 2f, (viewportSize.Y - totalHeight) / 2f);

            var rects = new Rectangle[StartMenuLabels.Length];
            for (int i = 0; i < rects.Length; i++)
                rects[i] = new Rectangle((int)origin.X, (int)(origin.Y + i * (optionHeight + spacing)), optionWidth, optionHeight);
            return rects;
        }

        /// <summary>
        /// This turn's actual menu options for _playerUnit, built fresh every call by
        /// Combat.TurnMenuBuilder off its current Class/Inventory/Knocked-Down state and the
        /// tiles around it - see TurnMenuBuilder for exactly what each option checks. Called from
        /// every place the turn menu reads or draws its options, rather than cached, so it always
        /// reflects whatever just changed (AP spent, a neighboring tile that opened up, ...).
        /// </summary>
        private List<TurnMenuOption> GetTurnMenuOptions() =>
            TurnMenuBuilder.Build(_playerUnit, GameMode.Combat, _hexGrid, _map, _units, _droppedItems);

        /// <summary>
        /// Handle input on the player unit's turn menu: W/S or mouse-hover to highlight an
        /// option, click or E to confirm.
        /// </summary>
        private void HandleTurnMenuInput(KeyboardState keyboardState, MouseState mouseState)
        {
            var options = GetTurnMenuOptions();
            _turnMenuIndex = Math.Clamp(_turnMenuIndex, 0, options.Count - 1);

            if (keyboardState.IsKeyDown(Keys.S) && !_previousKeyboardState.IsKeyDown(Keys.S))
                _turnMenuIndex = (_turnMenuIndex + 1) % options.Count;
            if (keyboardState.IsKeyDown(Keys.W) && !_previousKeyboardState.IsKeyDown(Keys.W))
                _turnMenuIndex = (_turnMenuIndex - 1 + options.Count) % options.Count;

            Vector2 viewportSize = new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            Rectangle[] optionRects = GetTurnMenuOptionRects(viewportSize);

            for (int i = 0; i < optionRects.Length; i++)
            {
                if (optionRects[i].Contains(mouseState.X, mouseState.Y))
                {
                    _turnMenuIndex = i;
                    if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
                        ConfirmTurnMenuSelection();
                }
            }

            if (keyboardState.IsKeyDown(Keys.E) && !_previousKeyboardState.IsKeyDown(Keys.E))
                ConfirmTurnMenuSelection();
        }

        private void ConfirmTurnMenuSelection()
        {
            var options = GetTurnMenuOptions();
            _turnMenuIndex = Math.Clamp(_turnMenuIndex, 0, options.Count - 1);
            string optionId = options[_turnMenuIndex].Id;

            if (optionId == "End")
            {
                _turnMenuActive = false;

                // The slot displays as "Guard" (see TurnMenuBuilder) whenever the unit can afford
                // it AND isn't Knocked Down (bracing while flat on your back makes no sense, and
                // the label already reflects that - see TurnMenuBuilder's own canGuard check);
                // selecting it then spends the AP and applies the Guard stance on top of ending
                // the turn. Mirrors that same condition here rather than just reading CurrentAP
                // again, so the actual behavior can never drift from what the label promised.
                if (!_playerUnit.IsKnockedDown && _playerUnit.CurrentAP >= _playerUnit.GuardAPCost)
                {
                    _playerUnit.SpendAP(_playerUnit.GuardAPCost);
                    _playerUnit.ApplyGuard();
                }

                _turnSystem.NextUnit();
            }
            else if (optionId == "ViewMap")
            {
                _turnMenuActive = false;
                _viewingMap = true;
            }
            else if (optionId == "Attack")
            {
                _turnMenuActive = false;
                OpenAttackMenu();
            }
            else if (optionId == "Move")
            {
                _turnMenuActive = false;

                // The slot displays as "Stand Up" (see TurnMenuBuilder) while Knocked Down, since
                // movement is disabled until the unit spends the AP to get back up.
                if (_playerUnit.IsKnockedDown)
                {
                    if (IsPinnedAndCannotEscape(_playerUnit, out var pinner))
                        ShowCombatMessage($"{_playerUnit.Name} cannot escape {pinner.Name}'s grip!");
                    else
                        _playerUnit.TryStandUp();
                    _turnMenuIndex = 0;
                    _turnMenuActive = true;
                }
                else
                {
                    OpenMovementMode();
                }
            }
            else if (optionId == "Cards")
            {
                _turnMenuActive = false;
                OpenCardMenu();
            }
            else if (optionId == "Items")
            {
                _turnMenuActive = false;
                OpenItemsMenu();
            }
            else if (optionId == "PickUp")
            {
                ExecutePickUp(_playerUnit);
                // Stays on the turn menu (TurnMenuBuilder re-evaluates "Pick Up" fresh - it'll
                // disappear once nothing reachable is left to grab, same as every other option).
                _turnMenuIndex = 0;
            }
            else if (optionId == "Syphon")
            {
                CastSyphon(_playerUnit);
                _turnMenuIndex = 0;
            }
            else if (optionId == "Release")
            {
                OpenReleaseTargeting();
            }
        }

        /// <summary>
        /// The "Pick Up" Action (see TurnMenuBuilder): spends BaseUnit.PickUpAPCost (1) to grab
        /// whichever dropped ConsumableItem on `unit`'s own tile or one tile away is reachable
        /// AND has item-box room (BaseUnit.CanCarryConsumable) - the menu option itself already
        /// guarantees at least one such item exists, so this just finds and takes the first one
        /// (nearest tile first, i.e. the unit's own tile before any neighbor). Shared by the
        /// player (ConfirmTurnMenuSelection) - AI doesn't use this yet, same documented scope cut
        /// as spell-card casting in RunHeroAI.
        /// </summary>
        private void ExecutePickUp(BaseUnit unit)
        {
            var unitHex = _hexGrid.WorldToHex(unit.Position);
            var pickup = _droppedItems
                .Where(d => d.Item is ConsumableItem ci && unit.CanCarryConsumable(ci))
                .OrderBy(d => _hexGrid.GetDistance(unitHex.col, unitHex.row, _hexGrid.WorldToHex(d.Position).col, _hexGrid.WorldToHex(d.Position).row))
                .FirstOrDefault(d => _hexGrid.GetDistance(unitHex.col, unitHex.row, _hexGrid.WorldToHex(d.Position).col, _hexGrid.WorldToHex(d.Position).row) <= 1);
            if (pickup == null)
                return;

            var consumable = (ConsumableItem)pickup.Item;
            unit.SpendAP(BaseUnit.PickUpAPCost);
            unit.AddConsumable(consumable);
            _droppedItems.Remove(pickup);
            ShowCombatMessage($"{unit.Name} picks up {consumable.Name}.");
        }

        /// <summary>
        /// The "Syphon" turn-menu option (see TurnMenuBuilder/Combat.RaceAttacks.RoachlinSyphon) -
        /// only ever shown while `roachlin` HasStatusEffect("Pinning") and hasn't already Syphoned
        /// this same pin (TurnMenuBuilder's own SyphonedThisPin check), so PinnedTarget is
        /// guaranteed non-null (and AP/MP affordable too) - the null/dead check below is
        /// defensive only. Deals RoachlinSyphon's damage (no hit roll - BaseAccuracy 1f; a flat
        /// base PLUS a mild +STR/5 modifier, deliberately gentler than Claw Crush's full 1:1
        /// scaling - see RoachlinSyphon's own doc comment), heals the Roachlin by that same damage
        /// value, then grants - or, if "Syphon Vigor" is already active, STACKS on top of and
        /// refreshes - a 2-turn self-buff: +3 STR, +6 Max HP, +3 DEF. Stacking/refreshing is done
        /// by mutating the existing StatusEffect instance directly rather than through
        /// ApplyStatusEffect (which would just replace it at the base values, not add on top),
        /// since "stacks and refreshes each time" needs the SECOND use to add a further +3/+6/+3
        /// on top of the first, not reset back down to it. Finally marks the Pinning itself as
        /// SyphonedThisPin - on request, cashing in the benefit forces an immediate Release right
        /// after (TurnMenuBuilder then shows ONLY Release, nothing else), the strategic trade-off
        /// against just ending the turn still Pinning instead.
        /// </summary>
        private void CastSyphon(BaseUnit roachlin)
        {
            var pinning = roachlin.StatusEffects.FirstOrDefault(e => e.Name == "Pinning");
            var target = pinning?.PinnedTarget;
            if (target == null || !target.IsAlive)
            {
                ShowCombatMessage("Nothing to Syphon.");
                return;
            }

            var move = RaceAttacks.RoachlinSyphon;
            roachlin.SpendAP(move.APCost);
            roachlin.CurrentMP = Math.Max(0, roachlin.CurrentMP - move.MPCost);

            int damage = move.GetDamage(roachlin, target);
            target.TakeDamage(damage, move.DamageType);
            roachlin.Heal(damage);

            var vigor = roachlin.StatusEffects.FirstOrDefault(e => e.Name == "Syphon Vigor");
            if (vigor != null)
            {
                vigor.StrengthBonus += 3;
                vigor.MaxHPBonus += 6;
                vigor.DefenseBonus += 3;
                vigor.TurnsRemaining = 2;
            }
            else
            {
                roachlin.ApplyStatusEffect(new StatusEffect
                {
                    Name = "Syphon Vigor",
                    Type = StatusEffectType.Buff,
                    TurnsRemaining = 2,
                    StrengthBonus = 3,
                    MaxHPBonus = 6,
                    DefenseBonus = 3,
                });
            }

            pinning.SyphonedThisPin = true;
            ShowCombatMessage($"{roachlin.Name} syphons {target.Name} for {damage} damage, healing the same amount!");
        }

        /// <summary>
        /// Handle input while the player's unit has a restricting StatusEffect active (Stunned,
        /// Deep Sleep, Meditating, but not Fainted): W/S or mouse-hover to highlight that
        /// effect's own end-it-early action vs "End Turn" (see GetRestrictedMenuOptions), click
        /// or E to confirm. No other menu is reachable from here - see the isPlayerTurn dispatch
        /// in Update.
        /// </summary>
        private void HandleRestrictedTurnInput(KeyboardState keyboardState, MouseState mouseState)
        {
            var options = GetRestrictedMenuOptions(_playerUnit);

            if (keyboardState.IsKeyDown(Keys.S) && !_previousKeyboardState.IsKeyDown(Keys.S))
                _restrictedMenuIndex = (_restrictedMenuIndex + 1) % options.Length;
            if (keyboardState.IsKeyDown(Keys.W) && !_previousKeyboardState.IsKeyDown(Keys.W))
                _restrictedMenuIndex = (_restrictedMenuIndex - 1 + options.Length) % options.Length;

            Vector2 viewportSize = new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            Rectangle[] optionRects = GetMenuOptionRects(viewportSize, options.Length);

            for (int i = 0; i < optionRects.Length; i++)
            {
                if (optionRects[i].Contains(mouseState.X, mouseState.Y))
                {
                    _restrictedMenuIndex = i;
                    if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
                        ConfirmRestrictedMenuSelection();
                }
            }

            if (keyboardState.IsKeyDown(Keys.E) && !_previousKeyboardState.IsKeyDown(Keys.E))
                ConfirmRestrictedMenuSelection();
        }

        private void ConfirmRestrictedMenuSelection()
        {
            string option = GetRestrictedMenuOptions(_playerUnit)[_restrictedMenuIndex];

            if (option == "End Turn")
            {
                // Whatever's restricting this unit consumes one of its own turns here (Stun's
                // fixed duration counts down; an indefinite effect like Deep Sleep/Meditate is
                // untouched - see ConsumeRestrictingEffectTurn) until something actually clears it.
                _playerUnit.ConsumeRestrictingEffectTurn();
                _restrictedMenuIndex = 0;
                _turnSystem.NextUnit();
                return;
            }

            // Anything else is the restricting effect's own "end it early" action (Break Stun,
            // Wake Up, End Meditation, ...) - TryEndRestrictingEffect reports success/failure via
            // its own bool (unaffordable, on cooldown, ...), nothing more to show here yet.
            if (_playerUnit.TryEndRestrictingEffect())
            {
                _restrictedMenuIndex = 0;
                _turnMenuIndex = 0;
                _turnMenuActive = true; // free to act normally with whatever AP remains
            }
        }

        private void GenerateClassDecks()
        {
            foreach (HeroClass heroClass in Enum.GetValues(typeof(HeroClass)))
            {
                if (heroClass == HeroClass.None)
                    continue;

                var classSpells = SpellCatalog.GetSpellsForClass(heroClass);
                var summonPool = SummonCatalog.AllSummons;
                var deckPool = new List<object>();
                deckPool.AddRange(classSpells);
                deckPool.AddRange(summonPool);

                if (deckPool.Count == 0)
                    continue;

                int totalCards = _deckRandom.Next(15, 31);
                totalCards = Math.Min(totalCards, deckPool.Count);

                // Keep the deck class-appropriate by always taking at least a few of that class's
                // own spell cards first, then fill the rest from generic summons and any remaining
                // class-eligible spells.
                var shuffledClassSpells = classSpells.OrderBy(_ => _deckRandom.Next()).ToList();
                var shuffledSummons = summonPool.OrderBy(_ => _deckRandom.Next()).ToList();

                var classDeck = new List<object>();
                int classSpellCount = Math.Min(shuffledClassSpells.Count, Math.Max(8, totalCards - 4));
                classDeck.AddRange(shuffledClassSpells.Take(classSpellCount));

                var remainingSlots = totalCards - classDeck.Count;
                if (remainingSlots > 0)
                {
                    var filler = new List<object>();
                    filler.AddRange(shuffledSummons);
                    filler.AddRange(shuffledClassSpells.Skip(classSpellCount));
                    filler = filler.OrderBy(_ => _deckRandom.Next()).ToList();
                    classDeck.AddRange(filler.Take(remainingSlots));
                }

                classDeck = classDeck.OrderBy(_ => _deckRandom.Next()).ToList();
                _classDecks[heroClass] = classDeck;
            }
        }

        /// <summary>
        /// Open the Items menu: every Weapon in Inventory plus every ConsumableItem in
        /// Consumables, combined into one list (equipment has no concrete type yet - see
        /// ItemCategory.Equipment - so it never contributes any entries). Selecting one opens its
        /// detail view (OpenItemDetail); "Back" returns to the turn menu.
        /// </summary>
        private void OpenItemsMenu()
        {
            _itemsMenuEntries = new List<Item>();
            _itemsMenuEntries.AddRange(_playerUnit.Inventory);
            _itemsMenuEntries.AddRange(_playerUnit.Consumables);

            _itemsMenuLabels = _itemsMenuEntries
                .Select(item => item == _playerUnit.EquippedWeapon ? $"{item.Name} [Equipped]" : item.Name)
                .ToList();
            _itemsMenuLabels.Add("Back");

            _itemsMenuIndex = 0;
            _itemsMenuActive = true;
        }

        /// <summary>Handle input on the items list: W/S or mouse-hover to highlight an entry, click or E to confirm.</summary>
        private void HandleItemsMenuInput(KeyboardState keyboardState, MouseState mouseState)
        {
            if (keyboardState.IsKeyDown(Keys.S) && !_previousKeyboardState.IsKeyDown(Keys.S))
                _itemsMenuIndex = (_itemsMenuIndex + 1) % _itemsMenuLabels.Count;
            if (keyboardState.IsKeyDown(Keys.W) && !_previousKeyboardState.IsKeyDown(Keys.W))
                _itemsMenuIndex = (_itemsMenuIndex - 1 + _itemsMenuLabels.Count) % _itemsMenuLabels.Count;

            Vector2 viewportSize = new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            Rectangle[] optionRects = GetMenuOptionRects(viewportSize, _itemsMenuLabels.Count);

            for (int i = 0; i < optionRects.Length; i++)
            {
                if (optionRects[i].Contains(mouseState.X, mouseState.Y))
                {
                    _itemsMenuIndex = i;
                    if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
                        ConfirmItemsMenuSelection();
                }
            }

            if (keyboardState.IsKeyDown(Keys.E) && !_previousKeyboardState.IsKeyDown(Keys.E))
                ConfirmItemsMenuSelection();
        }

        private void ConfirmItemsMenuSelection()
        {
            if (_itemsMenuLabels[_itemsMenuIndex] == "Back")
            {
                _itemsMenuActive = false;
                _turnMenuIndex = 0;
                _turnMenuActive = true;
                return;
            }

            OpenItemDetail(_itemsMenuEntries[_itemsMenuIndex]);
        }

        /// <summary>Open the detail view for a single item - its stats/attacks/description (see DrawItemDetail) plus whichever actions BuildItemDetailActions decides apply to it.</summary>
        private void OpenItemDetail(Item item)
        {
            _selectedItem = item;
            _itemDetailActions = BuildItemDetailActions(item);
            _itemDetailIndex = 0;
            _itemDetailScrollOffset = 0;
            _itemsMenuActive = false;
            _itemDetailActive = true;
        }

        /// <summary>A Weapon toggles between "Equip (1 AP)" and "Unequip (1 AP)" depending on whether it's the unit's current EquippedWeapon; a ConsumableItem gets "Use (1 AP)". Every item gets "Discard (1 AP)" (a stub for now - see ConfirmItemDetailSelection) and "Back".</summary>
        private List<string> BuildItemDetailActions(Item item)
        {
            var actions = new List<string>();

            if (item is Weapon weapon)
                actions.Add(weapon == _playerUnit.EquippedWeapon ? "Unequip (1 AP)" : "Equip (1 AP)");
            else if (item is ConsumableItem)
                actions.Add("Use (1 AP)");

            actions.Add("Discard (1 AP)");
            actions.Add("Back");
            return actions;
        }

        /// <summary>Handle input on an item's detail/action view: W/S or mouse-hover to highlight an action, click or E to confirm, mouse wheel to scroll the info panel (it claims the wheel entirely while open - see HandleMapControls' zoom suppression).</summary>
        private void HandleItemDetailInput(KeyboardState keyboardState, MouseState mouseState)
        {
            if (keyboardState.IsKeyDown(Keys.S) && !_previousKeyboardState.IsKeyDown(Keys.S))
                _itemDetailIndex = (_itemDetailIndex + 1) % _itemDetailActions.Count;
            if (keyboardState.IsKeyDown(Keys.W) && !_previousKeyboardState.IsKeyDown(Keys.W))
                _itemDetailIndex = (_itemDetailIndex - 1 + _itemDetailActions.Count) % _itemDetailActions.Count;

            // A few lines per notch - final clamping against the real wrapped-line count happens
            // in DrawItemDetail, the same split DevConsole's own scroll uses.
            int scrollDelta = mouseState.ScrollWheelValue - _previousMouseState.ScrollWheelValue;
            if (scrollDelta != 0)
                _itemDetailScrollOffset = Math.Max(0, _itemDetailScrollOffset - scrollDelta / 40);

            Vector2 viewportSize = new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            Rectangle[] optionRects = GetFixedBottomRightMenuOptionRects(viewportSize, _itemDetailActions.Count);

            for (int i = 0; i < optionRects.Length; i++)
            {
                if (optionRects[i].Contains(mouseState.X, mouseState.Y))
                {
                    _itemDetailIndex = i;
                    if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
                        ConfirmItemDetailSelection();
                }
            }

            if (keyboardState.IsKeyDown(Keys.E) && !_previousKeyboardState.IsKeyDown(Keys.E))
                ConfirmItemDetailSelection();
        }

        private void ConfirmItemDetailSelection()
        {
            string action = _itemDetailActions[_itemDetailIndex];

            if (action == "Back")
            {
                _itemDetailActive = false;
                OpenItemsMenu(); // rebuild - an Equip/Unequip just done elsewhere could have changed labels
                return;
            }

            if (action == "Discard (1 AP)")
            {
                // Stub, as requested - no tile-drop flow exists yet (will be: prompt the unit to
                // pick an adjacent tile to drop the item on). No AP spent, item not removed -
                // same honest "nothing happens yet" treatment Items itself had before this
                // system existed.
                ShowCombatMessage("Discarding isn't implemented yet.");
                return;
            }

            if (_playerUnit.CurrentAP < 1)
            {
                ShowCombatMessage("Not enough AP.");
                return;
            }

            if (action == "Equip (1 AP)")
            {
                _playerUnit.SpendAP(1);
                _playerUnit.EquippedWeapon = (Weapon)_selectedItem;
                ShowCombatMessage($"{_playerUnit.Name} equips {_selectedItem.Name}.");
                CloseItemsFlowToTurnMenu();
            }
            else if (action == "Unequip (1 AP)")
            {
                _playerUnit.SpendAP(1);
                _playerUnit.EquippedWeapon = null;
                ShowCombatMessage($"{_playerUnit.Name} unequips {_selectedItem.Name}.");
                CloseItemsFlowToTurnMenu();
            }
            else if (action == "Use (1 AP)")
            {
                var consumable = (ConsumableItem)_selectedItem;
                _playerUnit.SpendAP(1);
                string message = consumable.Use(_playerUnit);
                _playerUnit.RemoveConsumable(consumable);
                ShowCombatMessage(message);
                CloseItemsFlowToTurnMenu();
            }
        }

        private void CloseItemsFlowToTurnMenu()
        {
            _itemDetailActive = false;
            _itemsMenuActive = false;
            _turnMenuIndex = 0;
            _turnMenuActive = true;
        }

        /// <summary>
        /// Open the card hand using a random deck for the current class. The deck is built once
        /// per new game and is then shuffled by class, with each class drawing from its own spell
        /// pool plus generic summon cards while keeping non-matching class spells out entirely.
        /// </summary>
        private void OpenCardMenu()
        {
            EnsurePlayerHandInitialized();
            _availableHandCards = _classHands.TryGetValue(_playerUnit.Class, out var hand) && hand != null
                ? new List<object>(hand)
                : new List<object>();

            _handCardIndex = 0;
            _highlightedHandCardIndex = null;
            _cardMenuActive = true;
        }

        private void HandleCardMenuInput(KeyboardState keyboardState, MouseState mouseState)
        {
            if (_availableHandCards.Count > 1)
            {
                if (keyboardState.IsKeyDown(Keys.D) && !_previousKeyboardState.IsKeyDown(Keys.D))
                {
                    _handCardIndex = (_handCardIndex + 1) % _availableHandCards.Count;
                    _highlightedHandCardIndex = _handCardIndex;
                }
                if (keyboardState.IsKeyDown(Keys.A) && !_previousKeyboardState.IsKeyDown(Keys.A))
                {
                    _handCardIndex = (_handCardIndex - 1 + _availableHandCards.Count) % _availableHandCards.Count;
                    _highlightedHandCardIndex = _handCardIndex;
                }
            }

            // A click selects/focuses a card, same as A/D - EXCEPT when it lands on the card
            // that's already focused/highlighted (i.e. clicking it again), which casts it
            // instead. No time window: this used to require two clicks within a fixed interval
            // (a real "double-click"), but that was unreliable in practice - two deliberate
            // clicks a bit further apart than the window missed each other, forcing repeated
            // clicking before two ever happened to land close enough together. Requiring the
            // SECOND click to land on the already-selected card (any time later) needs no timing
            // at all and is just as safe against an accidental single click casting something.
            // The click has to land on an actual card - either a smaller peeking side card's own
            // slot, or the current big focused card's larger on-screen silhouette (ScaleCardRect)
            // - clicking empty space does nothing either way.
            if (_availableHandCards.Count > 0 && mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
            {
                Vector2 viewportSize = new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
                var layout = GetHandCardLayout(viewportSize);
                Point clickPos = new Point(mouseState.X, mouseState.Y);

                if (_highlightedHandCardIndex.HasValue
                    && ScaleCardRect(layout[_highlightedHandCardIndex.Value], 2.10f).Contains(clickPos))
                {
                    _handCardIndex = _highlightedHandCardIndex.Value;
                    TryCastSelectedCard();
                    return; // casting may close the card menu - don't also process E/F below against now-stale state
                }

                for (int i = layout.Count - 1; i >= 0; i--)
                {
                    if (layout[i].Contains(clickPos))
                    {
                        _handCardIndex = i;
                        _highlightedHandCardIndex = i;
                        break;
                    }
                }
            }

            if (keyboardState.IsKeyDown(Keys.Space) && !_previousKeyboardState.IsKeyDown(Keys.Space))
            {
                TryCastSelectedCard();
                return; // casting may close the card menu (targeting mode, or a HitsAllAdjacent resolve) - don't also process E/F below against now-stale state
            }

            if (keyboardState.IsKeyDown(Keys.E) && !_previousKeyboardState.IsKeyDown(Keys.E))
            {
                _cardMenuActive = false;
                _turnMenuIndex = 0;
                _turnMenuActive = true;
            }

            if (keyboardState.IsKeyDown(Keys.F) && !_previousKeyboardState.IsKeyDown(Keys.F))
            {
                TryDrawCardFromDeck();
                _availableHandCards = _classHands.TryGetValue(_playerUnit.Class, out var hand) && hand != null
                    ? new List<object>(hand)
                    : new List<object>();
                if (_availableHandCards.Count > 0)
                    _handCardIndex = _availableHandCards.Count - 1;
            }
        }

        /// <summary>
        /// Enter movement mode: computes _reachableTiles out to a bit beyond the unit's current
        /// AP budget (so out-of-range tiles nearby still show, tinted red - see DrawMovementRange).
        /// Also snaps the camera once onto whatever tile the cursor happens to be over already,
        /// so opening the menu doesn't leave the view centered somewhere unrelated - WASD then
        /// pans freely from there (Update's camera-follow is suppressed for the whole mode, not
        /// re-triggered every frame - a per-frame version of this chased the mouse in a runaway
        /// feedback loop, since moving the camera changes which tile a STATIONARY cursor points
        /// at, which moved the camera further, forever).
        /// </summary>
        private void OpenMovementMode()
        {
            var start = _hexGrid.WorldToHex(_playerUnit.Position);
            float displayApBudget = _playerUnit.CurrentAP + 2f; // show a bit beyond current reach too, in red

            _reachableTiles = Pathfinder.GetReachableTiles(_hexGrid, _map, start, _playerUnit.TilesPerAP, displayApBudget,
                GetPassageBlockingTiles(_playerUnit), GetDestinationBlockingTiles(_playerUnit));
            _reachableTiles.Remove(start);

            var mouseState = Mouse.GetState();
            var viewportSize = new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            var cursorHex = _renderer.GetHexAtScreenPos(mouseState.X, mouseState.Y, viewportSize);
            _renderer.CameraPosition = _hexGrid.HexToWorld(cursorHex.col, cursorHex.row);

            _movementModeActive = true;
        }

        /// <summary>
        /// Tiles `mover` may not ROUTE THROUGH at all - a living unit on a DIFFERENT team from
        /// `mover` (an enemy or a neutral). Allied units (same team, via GetTeamRoot - this
        /// includes mover's own summons, and a summon's own summoner) are deliberately NOT
        /// included: they're always passable for pathfinding purposes, so a path can freely route
        /// through an ally's tile on the way to somewhere else, even though (see
        /// GetDestinationBlockingTiles) it still can't actually be where the route ENDS. A dead
        /// unit (either team) was never included here either way - it stops blocking movement the
        /// instant it dies (see Pathfinder.FindPath/GetReachableTiles for how the two sets are used).
        /// </summary>
        private HashSet<(int col, int row)> GetPassageBlockingTiles(BaseUnit mover)
        {
            var moverTeam = GetTeamRoot(mover);
            return _units.Where(u => u != mover && u.IsAlive && GetTeamRoot(u) != moverTeam)
                          .Select(u => _hexGrid.WorldToHex(u.Position))
                          .ToHashSet();
        }

        /// <summary>
        /// Tiles nothing may actually END its move on (or be placed/teleported onto) - every
        /// LIVING unit regardless of team, so two units (allied or not) can never occupy the same
        /// tile even though an ally doesn't block simply passing through (see
        /// GetPassageBlockingTiles). A dead unit never blocks a destination either - once it dies,
        /// its tile is free to land on too (a hero's corpse remains on screen, but a summon is
        /// removed from the battlefield outright - see the death-cleanup sweep in Update - either
        /// way the tile itself was never a destination blocker here).
        /// </summary>
        private HashSet<(int col, int row)> GetDestinationBlockingTiles(BaseUnit mover)
        {
            return _units.Where(u => u != mover && u.IsAlive)
                          .Select(u => _hexGrid.WorldToHex(u.Position))
                          .ToHashSet();
        }

        /// <summary>
        /// Handle input in movement mode: click any tile to move toward it (as far as current
        /// AP allows, even if that's short of the clicked tile), E to cancel back to the turn
        /// menu without moving.
        /// </summary>
        private void HandleMovementInput(KeyboardState keyboardState, MouseState mouseState)
        {
            if (keyboardState.IsKeyDown(Keys.E) && !_previousKeyboardState.IsKeyDown(Keys.E))
            {
                CancelMovementMode();
                return;
            }

            if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
            {
                Vector2 viewportSize = new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
                var clickedHex = _renderer.GetHexAtScreenPos(mouseState.X, mouseState.Y, viewportSize);
                TryMoveTowards(clickedHex);
            }
        }

        /// <summary>
        /// Move the player's unit as far as it can afford along the shortest path toward
        /// destination. If destination is fully affordable, it arrives there; otherwise it
        /// stops as far along the path as its remaining AP covers. A destination with no path
        /// at all (impassable/occupied/unreachable) is simply ignored - not clickable.
        /// </summary>
        private void TryMoveTowards((int col, int row) destination)
        {
            MoveUnitTowards(_playerUnit, destination);

            _movementModeActive = false;
            _turnMenuIndex = 0;
            _turnMenuActive = true;
        }

        /// <summary>
        /// Move `unit` toward `destination` as far as its current AP affords along the
        /// shortest path (see Pathfinder.FindPath) - stops as far along the path as it can
        /// still pay for, using the exact integer charge formula rather than Dijkstra's float
        /// approximation. No-op if there's no path at all (impassable/occupied/unreachable) or
        /// the unit can't afford even the first step. Shared by the player's click-to-move
        /// (TryMoveTowards) and the placeholder AI (RunSimpleAI). AP is spent immediately, but
        /// Position animates tile-by-tile over the next several frames (see
        /// BaseUnit.SetMovementPath) rather than jumping straight to the destination.
        /// </summary>
        private void MoveUnitTowards(BaseUnit unit, (int col, int row) destination)
        {
            // Snared (or any future PreventsMovement effect) blocks movement here directly rather
            // than at each caller - the player already never sees a Move option while snared (see
            // Combat.TurnMenuBuilder), but AI movement (WanderUnit/MoveAiUnitToward) has no menu to
            // gate it, so this is the one chokepoint both paths actually share. Knocked Down
            // (IsKnockedDown, not a PreventsMovement StatusEffect - see ApplyKnockdown) needs the
            // same guard for the same reason: the player's own menu already swaps Move for Stand
            // Up while knocked down, but nothing stopped an AI-controlled knocked-down unit from
            // still wandering/approaching via this same entrypoint until now - a real, if minor,
            // pre-existing gap noticed while making Knocked Down a real, visible StatusEffect.
            if (unit.StatusEffects.Any(e => e.PreventsMovement) || unit.IsKnockedDown)
                return;

            var start = _hexGrid.WorldToHex(unit.Position);
            var path = Pathfinder.FindPath(_hexGrid, _map, start, destination, unit.TilesPerAP,
                GetPassageBlockingTiles(unit), GetDestinationBlockingTiles(unit));
            if (path == null || path.Count == 0)
                return;

            int tilesPerAP = unit.TilesPerAP;
            int tiles = 0, water = 0, lastAffordableIndex = -1;
            for (int i = 0; i < path.Count; i++)
            {
                var (col, row) = path[i];
                bool isWater = _map.GetTile(col, row)?.Type == "water";
                int candidateTiles = tiles + 1;
                int candidateWater = water + (isWater ? 1 : 0);
                int candidateApCost = (int)Math.Ceiling(candidateTiles / (float)tilesPerAP) + candidateWater;
                if (candidateApCost > unit.CurrentAP)
                    break;

                tiles = candidateTiles;
                water = candidateWater;
                lastAffordableIndex = i;
            }

            if (lastAffordableIndex < 0)
                return; // can't afford even the first step

            var affordablePath = path.Take(lastAffordableIndex + 1).ToList();

            // A placed Trap (see Objects.Trap/CastPlaceTrap) triggers the instant it's reached -
            // by ANY unit, any team, even Owner's own (nothing is immune by team) - cutting the
            // move short right there - unit never walks the rest of an already-affordable path
            // through it, same as a real trap would stop someone mid-stride rather than let them
            // walk clean through. Range (0 for every current trap) is checked as a hex distance
            // rather than exact-tile equality, so a future larger-area trap works the same way.
            int triggerIndex = -1;
            Trap triggeredTrap = null;
            for (int i = 0; i < affordablePath.Count; i++)
            {
                var (col, row) = affordablePath[i];
                var trap = _placedTraps.FirstOrDefault(t => _hexGrid.GetDistance(t.Tile.col, t.Tile.row, col, row) <= t.Range);
                if (trap != null)
                {
                    triggeredTrap = trap;
                    triggerIndex = i;
                    break;
                }
            }

            int apCost;
            if (triggeredTrap != null)
            {
                affordablePath = affordablePath.Take(triggerIndex + 1).ToList();
                tiles = 0;
                water = 0;
                foreach (var (col, row) in affordablePath)
                {
                    tiles++;
                    if (_map.GetTile(col, row)?.Type == "water")
                        water++;
                }
                apCost = (int)Math.Ceiling(tiles / (float)tilesPerAP) + water;
            }
            else
            {
                apCost = (int)Math.Ceiling(tiles / (float)tilesPerAP) + water;
            }

            unit.SetMovementPath(affordablePath.Select(hex => _hexGrid.HexToWorld(hex.col, hex.row)));
            unit.SpendAP(apCost);

            var finalTile = affordablePath[affordablePath.Count - 1];
            string moveMessage = $"{unit.Name} moved from ({start.col}, {start.row}) to ({finalTile.col}, {finalTile.row})";
            GameLogger.Log(moveMessage);
            _console.Log(moveMessage);
            // Logged directly rather than through ShowCombatMessage (deliberately - a move
            // shouldn't flash an on-screen message for every single step the player takes), but
            // it still needs to count toward _combatMessagesThisUnitTurn - otherwise a unit that
            // only moved this turn (no attack, no cast) would be wrongly flagged as having done
            // "nothing" by LogIfAiTurnWasSilent right after a log line proving it actually moved.
            _combatMessagesThisUnitTurn++;

            if (triggeredTrap != null)
                TriggerTrap(triggeredTrap, unit);
        }

        /// <summary>Minimum aggro (of the Aggro System's 0-20 range) before a unit is forced to target whoever it's held against - see Aggro System.</summary>
        private const int AggroForcedTargetThreshold = 10;

        /// <summary>Aggro gained by every unit a Wild Carnivore finds within its own AngerRange when it turns Hostile (see RunAggroDrivenSummonAI) - the user's own example number.</summary>
        private const int WildAngerAggroGain = 5;

        /// <summary>Chance, each turn, that a Neutral/Friendly Wild animal with nothing to react to actually wanders instead of standing still (see WanderUnit) - first-pass placeholder, pending a balance pass.</summary>
        private const float WanderChancePerTurn = 0.5f;

        /// <summary>
        /// Simple wandering for a Neutral/Friendly Wild animal with nothing to react to this
        /// turn - a WanderChancePerTurn chance to move onto one random passable, unoccupied tile
        /// adjacent to it, reusing MoveUnitTowards (the same shared entrypoint player/AI movement
        /// already goes through), so it's AP-costed, animated, and logged exactly like any other
        /// move. Does nothing the rest of the time, or if every adjacent tile is already blocked.
        /// </summary>
        private void WanderUnit(BaseUnit unit)
        {
            if (_deckRandom.NextDouble() >= WanderChancePerTurn)
                return;

            var unitHex = _hexGrid.WorldToHex(unit.Position);
            var occupied = GetDestinationBlockingTiles(unit);
            var openNeighbors = _hexGrid.GetNeighbors(unitHex.col, unitHex.row)
                .Where(hex => Pathfinder.IsPassable(_hexGrid, _map, hex.col, hex.row, occupied))
                .ToList();

            if (openNeighbors.Count == 0)
                return;

            var destination = openNeighbors[_deckRandom.Next(openNeighbors.Count)];
            MoveUnitTowards(unit, destination);
        }

        /// <summary>
        /// Placeholder AI (GDD-pending - no real tactical depth yet): always targets the
        /// enemy hero - now genuinely aggro/multi-target aware, see RunHeroAI. A summon using
        /// SummonBehavior.NeutralAggressionBased or HostileTowardsEnemies gets its own
        /// aggro-driven targeting instead (RunAggroDrivenSummonAI) - every other summon behavior
        /// (including ControllableBySummoner, which never reaches RunSimpleAI at all, see
        /// Update's per-turn control handoff) falls through to RunHeroAI same as an enemy hero.
        /// Also handles any restricting StatusEffect (Stun, Deep Sleep, Meditate, ...: always
        /// attempts that effect's own end-it-early action) and Knockdown (always stands back up).
        /// In Test Mode, this entire method is a no-op for every AI unit (no movement, no
        /// attacking either) - see _testModeActive.
        /// </summary>
        private void RunSimpleAI(BaseUnit aiUnit)
        {
            if (aiUnit == _playerUnit || !aiUnit.IsAlive)
                return;

            // Test Mode: the other 3 spawned classes don't act at all - no movement (per the
            // original request) AND no attacking either. Discovered why the attack half matters
            // too by actually looking at a live screenshot: on a 3x3 map every corner is close
            // enough to every other that the "attack if already in range" behavior alone let the
            // 3 AI units kill each other, and the player's OWN unit, by turn 3 with the player
            // never getting a turn - the opposite of a safe card-testing sandbox. Movement-only
            // removal assumed units wouldn't already be in range of each other, which doesn't
            // hold on a map this small.
            if (_testModeActive)
                return;

            // Restricted (Stunned, Deep Sleep, Meditating, ...): always attempt that effect's own
            // end-it-early action (TryEndRestrictingEffect reads whichever one is actually
            // active). If it fails (unaffordable, or Stun's extra cooldown/class-gate), that's
            // this turn's one action - consume a turn of it (Stun's fixed duration counts down;
            // an indefinite effect is untouched) and stop there. If it succeeds, fall through and
            // still use whatever AP remains to move/attack, same as a normal turn would.
            if (aiUnit.GetRestrictingEffect() != null && !aiUnit.TryEndRestrictingEffect())
            {
                var restrictingEffect = aiUnit.GetRestrictingEffect();
                aiUnit.ConsumeRestrictingEffectTurn();
                ShowCombatMessage($"{aiUnit.Name} is still {restrictingEffect.Name} and can't act.");
                return;
            }

            // Knocked Down: stand up instead of trying (and failing) to move/attack this turn -
            // UNLESS Pinning means standing up would just fail anyway (see
            // IsPinnedAndCannotEscape), in which case fall through to the normal attack logic
            // below instead of wasting the turn on a stand-up attempt that can't succeed; a
            // pinned Knocked Down unit can still attack/cast normally, just with the usual
            // Knocked Down accuracy penalty (Move.GetHitChance).
            if (aiUnit.IsKnockedDown && !IsPinnedAndCannotEscape(aiUnit, out _))
            {
                bool stoodUp = aiUnit.TryStandUp();
                ShowCombatMessage(stoodUp
                    ? $"{aiUnit.Name} stands back up."
                    : $"{aiUnit.Name} is Knocked Down and can't afford to stand up.");
                return;
            }

            // Owned-summon aggro behaviors, OR an untamed Wild creature (no Owner yet) - both
            // need aggro-driven targeting instead of RunHeroAI's full hero-style aggression.
            // Without the second half of this check, a freshly-placed Neutral Wild beast (e.g.
            // Game1's "summon" dev command, or a future wild spawn) would fall through to
            // RunHeroAI and attack on sight every turn - defeating the entire point of "Neutral"
            // (and Tame Beast, which only works against something that's stayed Neutral).
            if ((aiUnit.Owner != null &&
                    (aiUnit.Behavior == SummonBehavior.NeutralAggressionBased || aiUnit.Behavior == SummonBehavior.HostileTowardsEnemies))
                || (aiUnit.Owner == null && aiUnit.Factions.Contains(Faction.Wild)))
            {
                RunAggroDrivenSummonAI(aiUnit);
                return;
            }

            RunHeroAI(aiUnit);
        }

        /// <summary>A dedicated RNG for aggro-tie-breaking (PickAggroTarget) - kept separate from _deckRandom so one's call pattern never shifts the other's sequence.</summary>
        private readonly Random _aiTargetRandom = new Random();

        /// <summary>
        /// Aggro-driven turn for a summon using SummonBehavior.NeutralAggressionBased or
        /// HostileTowardsEnemies - called from RunSimpleAI instead of falling through to its own
        /// player-only logic. Neutral does nothing at all this turn unless Forced Targeting (see
        /// PickAggroTarget/Aggro System) actually applies (true neutrality - nothing has
        /// provoked it); Hostile always picks a target via the same selection either way, forced
        /// or not. Once a target is chosen, this reuses the same attack/approach logic
        /// (TryAiAttack/MoveAiUnitToward) RunSimpleAI does, just aimed at this target instead of
        /// a hardcoded _playerUnit. If the chosen target can't actually be reached or attacked
        /// this turn, the summon just sits tight rather than retrying against a different one -
        /// the same "boxed in" fallback RunSimpleAI's own player-only logic already accepts.
        /// </summary>
        private void RunAggroDrivenSummonAI(BaseUnit summon)
        {
            var summonTeam = GetTeamRoot(summon);
            var potentialEnemies = _units.Where(u => u.IsAlive && GetTeamRoot(u) != summonTeam).ToList();
            if (potentialEnemies.Count == 0)
                return;

            // An ownerless Wild Carnivore within its own AngerRange of ANY potential enemy
            // notices them and turns Hostile on its own - a lasting Disposition change (not
            // just "engage this once"), checked first so the rest of this same turn already
            // sees it. Generates WildAngerAggroGain aggro toward every unit close enough, not
            // just whichever one ends up the actual target.
            if (summon.Owner == null && summon.Disposition != Disposition.Hostile && summon.AngerRange > 0)
            {
                var summonHex = _hexGrid.WorldToHex(summon.Position);
                var tooClose = potentialEnemies.Where(e =>
                        _hexGrid.GetDistance(summonHex.col, summonHex.row, _hexGrid.WorldToHex(e.Position).col, _hexGrid.WorldToHex(e.Position).row) <= summon.AngerRange)
                    .ToList();
                if (tooClose.Count > 0)
                {
                    summon.Disposition = Disposition.Hostile;
                    foreach (var intruder in tooClose)
                        summon.AddAggro(intruder, WildAngerAggroGain);
                    ShowCombatMessage($"{summon.Name} turns hostile!");
                }
            }

            int maxAggro = potentialEnemies.Max(e => summon.GetAggro(e));

            // An owned summon on NeutralAggressionBased, OR an ownerless Wild creature that
            // isn't actively Hostile (Neutral and Friendly both stand down the same way here -
            // the distinction between them only matters for Tame Beast eligibility, not combat
            // behavior) - stays put (or wanders, if Wild - see WanderUnit) until provoked. A
            // Hostile Wild creature skips this entirely, same as HostileTowardsEnemies always does.
            bool actsNeutral = summon.Behavior == SummonBehavior.NeutralAggressionBased
                || (summon.Owner == null && summon.Disposition != Disposition.Hostile);
            if (actsNeutral && maxAggro < AggroForcedTargetThreshold)
            {
                if (summon.Owner == null && summon.Factions.Contains(Faction.Wild))
                    WanderUnit(summon);
                return; // nothing has provoked it yet - stays put/wanders, true to "neutral"
            }

            var target = PickAggroTarget(summon, potentialEnemies, maxAggro);

            if (TryAiAttack(summon, target))
                return;

            MoveAiUnitToward(summon, target);
            TryAiAttack(summon, target);
        }

        /// <summary>
        /// Chooses one unit from `candidates` for `unit` to target: whichever hold(s) `maxAggro`
        /// (the pre-computed highest aggro `unit` has toward anyone in `candidates`) - a real
        /// forced target once that's >= AggroForcedTargetThreshold, or just the natural top pick
        /// otherwise. A tie for the top spot is broken by hex distance (nearest wins); a further
        /// tie (e.g. at the very start of a match, before any aggro exists at all, when
        /// everyone's tied at 0 and distances can coincide too) is broken at random.
        /// </summary>
        private BaseUnit PickAggroTarget(BaseUnit unit, List<BaseUnit> candidates, int maxAggro)
        {
            var topAggro = candidates.Where(c => unit.GetAggro(c) == maxAggro).ToList();
            if (topAggro.Count == 1)
                return topAggro[0];

            var unitHex = _hexGrid.WorldToHex(unit.Position);
            int Distance(BaseUnit c) => _hexGrid.GetDistance(unitHex.col, unitHex.row, _hexGrid.WorldToHex(c.Position).col, _hexGrid.WorldToHex(c.Position).row);

            int minDistance = topAggro.Min(Distance);
            var nearest = topAggro.Where(c => Distance(c) == minDistance).ToList();

            return nearest.Count == 1 ? nearest[0] : nearest[_aiTargetRandom.Next(nearest.Count)];
        }

        /// <summary>Walk `aiUnit` to whichever reachable tile adjacent to `target` is cheapest to path to - shared by RunSimpleAI's player-only logic and RunAggroDrivenSummonAI. No-op (sits tight) if nothing adjacent to `target` is reachable at all (e.g. boxed in).</summary>
        private void MoveAiUnitToward(BaseUnit aiUnit, BaseUnit target)
        {
            var targetHex = _hexGrid.WorldToHex(target.Position);
            var start = _hexGrid.WorldToHex(aiUnit.Position);
            var passageBlocking = GetPassageBlockingTiles(aiUnit);
            var destinationBlocking = GetDestinationBlockingTiles(aiUnit);

            (int col, int row)? bestTile = null;
            int bestPathLength = int.MaxValue;

            foreach (var tile in _hexGrid.GetNeighbors(targetHex.col, targetHex.row))
            {
                var path = Pathfinder.FindPath(_hexGrid, _map, start, tile, aiUnit.TilesPerAP, passageBlocking, destinationBlocking);
                if (path == null)
                    continue;

                if (path.Count < bestPathLength)
                {
                    bestPathLength = path.Count;
                    bestTile = tile;
                }
            }

            if (bestTile.HasValue)
                MoveUnitTowards(aiUnit, bestTile.Value);
        }

        /// <summary>
        /// Placeholder AI attack decision: among every move `aiUnit` can currently afford
        /// (AP/MP/ammo) with `target` in its effective range, use whichever would deal the
        /// most raw damage. At most one attack per call (callers call this up to twice a
        /// turn - once before moving, once after). Returns true if an attack was made.
        /// </summary>
        private bool TryAiAttack(BaseUnit aiUnit, BaseUnit target)
        {
            var candidates = GetUsableAttacks(aiUnit, target);
            if (candidates.Count == 0)
                return false;

            var aiHex = _hexGrid.WorldToHex(aiUnit.Position);
            var targetHex = _hexGrid.WorldToHex(target.Position);
            var (move, sourceWeapon) = candidates[0];

            BaseUnit primaryTarget = move.HitsAllAdjacent || move.HitsCone ? null : target;
            HexDirection? aimDirection = move.HitsCone
                ? _hexGrid.GetDirectionTo(aiHex.col, aiHex.row, targetHex.col, targetHex.row)
                : null;

            var outcomes = AttackResolver.Resolve(aiUnit, primaryTarget, move, sourceWeapon, _hexGrid, _map, _units, aimDirection);
            SpawnAttackAnimations(aiUnit, move, outcomes);
            ShowCombatMessage(BuildCombatLogMessage(aiUnit, move, outcomes));
            return true;
        }

        /// <summary>Every move/weapon pairing `aiUnit` can currently afford (AP/MP/ammo) with `target` already in its effective range, strongest raw damage first. Shared by TryAiAttack (acts on candidates[0]) and GetReachableEnemies (only needs to know whether this is non-empty) so the two never drift apart on what "can hit" means.</summary>
        private List<(Move Move, Weapon SourceWeapon)> GetUsableAttacks(BaseUnit aiUnit, BaseUnit target)
        {
            var aiHex = _hexGrid.WorldToHex(aiUnit.Position);
            var targetHex = _hexGrid.WorldToHex(target.Position);
            int distanceTiles = _hexGrid.GetDistance(aiHex.col, aiHex.row, targetHex.col, targetHex.row);

            return aiUnit.AvailableMovesWithSource
                .Where(entry =>
                {
                    // IsLeap (e.g. the Dusk Roachlin's Leap) needs its own dedicated targeting
                    // mode (tile-or-unit, a dual mode no other move has) that only the player's
                    // menu dispatch currently understands - same scope cut every other
                    // specially-flagged move (IsBlink, IsPlaceTrap, ...) already has, just for a
                    // racial move instead of a SpellCard. AttackResolver would resolve it as a
                    // plain 0-damage hit with no targeting of its own, which isn't what it does.
                    if (entry.Move.IsLeap)
                        return false;

                    int apCost = aiUnit.GetEffectiveAPCost(entry.Move, entry.SourceWeapon);
                    if (apCost > aiUnit.CurrentAP || entry.Move.MPCost > aiUnit.CurrentMP)
                        return false;
                    if (entry.Move.RequiredAmmoType.HasValue && aiUnit.GetAmmo(entry.Move.RequiredAmmoType.Value) < 1)
                        return false;
                    return distanceTiles <= entry.Move.GetEffectiveRange(aiUnit, entry.SourceWeapon);
                })
                .OrderByDescending(entry => entry.Move.GetDamage(aiUnit, target, entry.SourceWeapon))
                .ToList();
        }

        /// <summary>Which of `potentialEnemies` `aiUnit` can actually hit right now with some affordable weapon/racial move (see GetUsableAttacks) - the "attack or spell that can hit enemies nearby" check in RunHeroAI's priority cascade. Spell-card attacks aren't considered yet (see RunHeroAI's own doc comment on scope).</summary>
        private List<BaseUnit> GetReachableEnemies(BaseUnit aiUnit, List<BaseUnit> potentialEnemies)
        {
            return potentialEnemies.Where(e => GetUsableAttacks(aiUnit, e).Count > 0).ToList();
        }

        /// <summary>
        /// Minimal utility pass for RunHeroAI's third priority tier: if `aiUnit` is below half
        /// HP and carrying a Consumable that actually heals (HealHP &gt; 0), use the
        /// strongest-healing one. This is deliberately the ONLY utility behavior implemented -
        /// traps and the ally-healing SpellCards (Tez's Mercy, Bandage Wound, ...) have no
        /// execution path anywhere in the game yet, not even for the player (see
        /// TryCastSelectedCard's final else - casting them just reports "no execution system for
        /// that effect"), so there's nothing real for the AI to invoke there either. Returns true
        /// if a potion was used (consuming 1 AP, same cost the player's own Items menu charges).
        /// </summary>
        private bool TryAiUtility(BaseUnit aiUnit)
        {
            if (aiUnit.HP > aiUnit.EffectiveMaxHP / 2 || aiUnit.CurrentAP < 1)
                return false;

            var potion = aiUnit.Consumables.Where(c => c.HealHP > 0).OrderByDescending(c => c.HealHP).FirstOrDefault();
            if (potion == null)
                return false;

            aiUnit.SpendAP(1);
            string message = potion.Use(aiUnit);
            aiUnit.RemoveConsumable(potion);
            ShowCombatMessage(message);
            return true;
        }

        /// <summary>
        /// Full AI turn for an enemy hero (or any summon not on NeutralAggressionBased/
        /// HostileTowardsEnemies, which get RunAggroDrivenSummonAI instead) - a 4-tier priority
        /// cascade, each tier only reached if every one before it found nothing to do:
        /// 1. Summon (TryAiSummon) - explicitly the top priority per the design request, since
        ///    summons matter enough to the game to always take precedence when affordable/placeable.
        /// 2. Attack whichever reachable enemy (GetReachableEnemies) holds the HIGHEST aggro
        ///    (PickAggroTarget) - never the easiest kill or the biggest hit, aggro always wins.
        ///    Only weapon/racial moves are considered - see GetUsableAttacks' doc comment; a
        ///    damage-dealing SpellCard isn't part of this check yet, since wiring spell-card
        ///    casting into AI decision-making (mana budgeting, card selection) was judged too
        ///    large to fold safely into this same pass.
        /// 3. Utility (TryAiUtility) - currently just a self-heal potion; see its own doc comment
        ///    for why traps/ally-heal spells aren't included.
        /// 4. Move toward the single highest-aggro target across EVERY potential enemy (not just
        ///    reachable ones) and swing once more if that closed into range - same as
        ///    RunAggroDrivenSummonAI's own move+attack fallback, just logged per the design
        ///    request ("Moving towards 'X' target").
        /// No-ops entirely if there's no living enemy at all (GetTeamRoot-based, so a summon's
        /// team always matches its Owner's).
        /// </summary>
        private void RunHeroAI(BaseUnit aiUnit)
        {
            if (TryAiSummon(aiUnit))
                return;

            var aiTeam = GetTeamRoot(aiUnit);
            var potentialEnemies = _units.Where(u => u.IsAlive && GetTeamRoot(u) != aiTeam).ToList();
            if (potentialEnemies.Count == 0)
                return;

            var reachable = GetReachableEnemies(aiUnit, potentialEnemies);
            if (reachable.Count > 0)
            {
                int maxAggroReachable = reachable.Max(e => aiUnit.GetAggro(e));
                var attackTarget = PickAggroTarget(aiUnit, reachable, maxAggroReachable);
                if (TryAiAttack(aiUnit, attackTarget))
                    return;
            }

            if (TryAiUtility(aiUnit))
                return;

            int maxAggroOverall = potentialEnemies.Max(e => aiUnit.GetAggro(e));
            var moveTarget = PickAggroTarget(aiUnit, potentialEnemies, maxAggroOverall);
            _console.Log($"Moving towards '{moveTarget.Name}' target");
            MoveAiUnitToward(aiUnit, moveTarget);

            // May have closed into range this turn - take one swing if so.
            TryAiAttack(aiUnit, moveTarget);
        }

        private void CancelMovementMode()
        {
            _movementModeActive = false;
            _turnMenuIndex = 0;
            _turnMenuActive = true;
        }

        /// <summary>
        /// Open the attack submenu, listing the player unit's racial move plus every attack
        /// granted by a weapon in its inventory, with a trailing option to go back. Each move
        /// shows its actual AP cost - a weapon move that isn't currently equipped shows its
        /// base cost +1 AP, covering the switch to draw it.
        /// </summary>
        private void OpenAttackMenu()
        {
            _attackMenuMoves = _playerUnit.AvailableMovesWithSource.ToList();
            _attackMenuLabels = _attackMenuMoves
                .Select(entry => FormatAttackLabel(entry.Move, entry.SourceWeapon))
                .ToList();
            _attackMenuLabels.Add("Back");
            _attackMenuIndex = 0;
            _attackMenuActive = true;
        }

        /// <summary>
        /// Build a move's submenu label, e.g. "Sword Slash (2 AP)" or "Stab (3 AP)" if its
        /// weapon isn't the one currently equipped. MP cost (or other non-AP costs) is
        /// appended too when present, and a move requiring ammo (e.g. Arrow Shot) shows how
        /// many are left, so running out reads as an obvious reason it can't be selected rather
        /// than a silent no-op.
        /// </summary>
        private string FormatAttackLabel(Move move, Weapon sourceWeapon)
        {
            int apCost = _playerUnit.GetEffectiveAPCost(move, sourceWeapon);
            string cost = $"{apCost} AP";
            if (move.MPCost > 0)
                cost += $", {move.MPCost} MP";
            if (move.RequiredAmmoType.HasValue)
                cost += $", {_playerUnit.GetAmmo(move.RequiredAmmoType.Value)} {move.RequiredAmmoType.Value}s";

            return $"{move.Name} ({cost})";
        }

        /// <summary>
        /// Handle input on the attack submenu: W/S or mouse-hover to highlight an option,
        /// click or E to confirm.
        /// </summary>
        private void HandleAttackMenuInput(KeyboardState keyboardState, MouseState mouseState)
        {
            if (keyboardState.IsKeyDown(Keys.S) && !_previousKeyboardState.IsKeyDown(Keys.S))
                _attackMenuIndex = (_attackMenuIndex + 1) % _attackMenuLabels.Count;
            if (keyboardState.IsKeyDown(Keys.W) && !_previousKeyboardState.IsKeyDown(Keys.W))
                _attackMenuIndex = (_attackMenuIndex - 1 + _attackMenuLabels.Count) % _attackMenuLabels.Count;

            Vector2 viewportSize = new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            Rectangle[] optionRects = GetMenuOptionRects(viewportSize, _attackMenuLabels.Count);

            for (int i = 0; i < optionRects.Length; i++)
            {
                if (optionRects[i].Contains(mouseState.X, mouseState.Y))
                {
                    _attackMenuIndex = i;
                    if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
                        ConfirmAttackMenuSelection();
                }
            }

            if (keyboardState.IsKeyDown(Keys.E) && !_previousKeyboardState.IsKeyDown(Keys.E))
                ConfirmAttackMenuSelection();
        }

        private void ConfirmAttackMenuSelection()
        {
            string label = _attackMenuLabels[_attackMenuIndex];

            if (label == "Back")
            {
                _attackMenuActive = false;
                _turnMenuIndex = 0;
                _turnMenuActive = true;
                return;
            }

            var (move, sourceWeapon) = _attackMenuMoves[_attackMenuIndex];

            int apCost = _playerUnit.GetEffectiveAPCost(move, sourceWeapon);
            if (apCost > _playerUnit.CurrentAP || move.MPCost > _playerUnit.CurrentMP)
            {
                ShowCombatMessage($"Not enough AP/MP for {move.Name}.");
                return; // can't afford it - stay on the submenu
            }

            if (move.RequiredAmmoType.HasValue && _playerUnit.GetAmmo(move.RequiredAmmoType.Value) < 1)
            {
                ShowCombatMessage($"Out of {move.RequiredAmmoType.Value}s.");
                return; // out of ammo - stay on the submenu, same treatment as unaffordable AP/MP
            }

            _pendingMove = move;
            _pendingSourceWeapon = sourceWeapon;

            if (move.HitsCone)
            {
                // Aimed at a direction (see HandleConeAimingInput), not a specific unit - no
                // target list to check, just needs somewhere to click.
                _attackMenuActive = false;
                _coneAimingModeActive = true;
                return;
            }

            if (move.IsLeap)
            {
                OpenLeapTargeting(move);
                return;
            }

            var validTargets = GetValidAttackTargets(move, sourceWeapon);
            if (validTargets.Count == 0)
            {
                ShowCombatMessage($"Nothing in range for {move.Name}.");
                return; // nothing alive in range to hit
            }

            if (move.HitsAllAdjacent)
            {
                // Hits everyone adjacent automatically - no target to pick.
                ExecutePendingAttack(null);
                return;
            }

            _targetCandidates = validTargets;
            _attackMenuActive = false;
            _targetingModeActive = true;
        }

        /// <summary>A unit's team identity - itself, or whoever summoned it (see BaseUnit.Owner). Two units are allied iff this is equal for both.</summary>
        private static BaseUnit GetTeamRoot(BaseUnit unit) => unit.Owner ?? unit;

        /// <summary>
        /// True if some OTHER living unit currently has an active "Pinning" StatusEffect whose
        /// PinnedTarget is `unit`, AND that pinner's EffectiveStrength is higher than `unit`'s own
        /// (see Leap) - the "while the attacker is PINNING, and the target's STR is LOWER than
        /// the attacker, the target remains KNOCKED DOWN, and cannot escape it" rule. Checked at
        /// both of BaseUnit.TryStandUp's call sites (player's "Stand Up" menu choice, AI's own
        /// knocked-down handling) rather than inside BaseUnit itself, since BaseUnit has no view
        /// of every OTHER unit on the field to find the pinner with - only Game1 does. Out
        /// parameter is the pinner itself, for the "cannot escape X's grip" message.
        /// </summary>
        private bool IsPinnedAndCannotEscape(BaseUnit unit, out BaseUnit pinner)
        {
            foreach (var other in _units)
            {
                if (other == unit || !other.IsAlive)
                    continue;

                var pinning = other.StatusEffects.FirstOrDefault(e => e.Name == "Pinning" && e.PinnedTarget == unit);
                if (pinning != null && other.EffectiveStrength > unit.EffectiveStrength)
                {
                    pinner = other;
                    return true;
                }
            }

            pinner = null;
            return false;
        }

        /// <summary>
        /// Bug fix: if `movedUnit` was the PinnedTarget of some other living unit's active
        /// "Pinning" (see ExecuteLeapOntoUnit - the pinner shares `movedUnit`'s own tile the
        /// whole time a pin holds) and the two no longer share a tile after this move - e.g. the
        /// pinned target just teleported away via Blink/Disengage, the one way a Knocked Down
        /// unit can actually move despite the normal "Move" lock - the pin breaks immediately:
        /// the pinner's "Pinning" StatusEffect is removed outright (nothing to lock in, unlike
        /// Meditate), resetting it to its normal stance/turn menu, and `movedUnit` is free to
        /// "Stand Up" normally again (IsPinnedAndCannotEscape no longer finds an active pin on
        /// it). Call right after any teleport-style move that could reposition a Knocked Down
        /// unit - currently just ExecuteBlink (Blink/Disengage).
        /// </summary>
        private void ReleasePinOnMovedTarget(BaseUnit movedUnit)
        {
            var pinner = _units.FirstOrDefault(u => u.IsAlive && u != movedUnit
                && u.StatusEffects.Any(e => e.Name == "Pinning" && e.PinnedTarget == movedUnit));
            if (pinner == null || pinner.Position == movedUnit.Position)
                return;

            pinner.RemoveStatusEffect("Pinning");
            ShowCombatMessage($"{pinner.Name} loses its grip on {movedUnit.Name}.");
        }

        /// <summary>
        /// Fog of War: every tile `teamRoot`'s team can currently see - the union of each living
        /// member's own BaseUnit.ViewRange (hero 5, Beast/Roachlin summon 3) around its current
        /// position, so a scouting summon genuinely extends the team's sight rather than just
        /// adding a body to the field. Recomputed fresh every Draw call (cheap - the map is small
        /// and this is pure hex math, no caching needed) rather than tracked incrementally, so it
        /// always reflects the current frame's positions with no stale-state risk. No persistent
        /// "previously explored" memory exists - a tile not currently in this set is drawn fully
        /// fogged every time, even one the team saw moments ago, a simpler binary than
        /// StarCraft's own explored/unexplored-vs-visible three-tier fog.
        /// </summary>
        private HashSet<(int col, int row)> GetVisibleTilesForTeam(BaseUnit teamRoot)
        {
            var visible = new HashSet<(int col, int row)>();
            foreach (var unit in _units.Where(u => u.IsAlive && GetTeamRoot(u) == teamRoot))
            {
                var unitHex = _hexGrid.WorldToHex(unit.Position);
                foreach (var hex in _hexGrid.GetHexesInRadius(unitHex.col, unitHex.row, unit.ViewRange))
                {
                    if (_hexGrid.IsInBounds(hex.col, hex.row))
                        visible.Add(hex);
                }
            }
            return visible;
        }

        /// <summary>
        /// AI Turn Batching: decides whether `unit`'s upcoming AI turn should be fast-forwarded
        /// (no camera pan, no 3-second dwell - see the camera-tracking block in Update and its
        /// AI-driving branch) rather than individually presented. Three rules, applied in order:
        /// 1) Every animal unit (Faction.Wild - every Beast/Roachlin summon, tamed or not) ALWAYS
        /// batches, regardless of visibility - wild creatures/summons are rarely the interesting
        /// part of an enemy's turn, and there are often several of them on the field at once. 2)
        /// A team (hero) unit batches too UNLESS it's currently visible to the player's own
        /// team's vision (Fog of War, GetVisibleTilesForTeam) - an unseen enemy hero's turn is
        /// just as uninteresting to watch as a wild animal's, since nothing about it is even on
        /// screen. 3) A VISIBLE team unit does NOT batch - that's the one case worth slowing down
        /// for, since the player can actually watch what it does. Only ever called for a
        /// non-player unit (see Update) - the player's own turn is never batched.
        /// </summary>
        private bool ShouldBatchAiTurn(BaseUnit unit)
        {
            if (unit.Factions.Contains(Faction.Wild))
                return true;

            if (_playerUnit == null)
                return false;

            var visibleTiles = GetVisibleTilesForTeam(GetTeamRoot(_playerUnit));
            return !visibleTiles.Contains(_hexGrid.WorldToHex(unit.Position));
        }

        /// <summary>One fixed color per player team (by the team root's HeroClass - Sorcerer/Warrior/Cleric/Hunter), echoing the same rough palette the Spell Card placeholder art already established (purples for Sorcerer, reds for Warrior, forest greens for Hunter) plus gold for Cleric.</summary>
        private static readonly Dictionary<HeroClass, Color> TeamOutlineColors = new Dictionary<HeroClass, Color>
        {
            [HeroClass.Sorcerer] = new Color(170, 60, 220),
            [HeroClass.Warrior] = new Color(220, 40, 40),
            [HeroClass.Cleric] = new Color(255, 200, 40),
            [HeroClass.Hunter] = new Color(50, 200, 60),
        };

        /// <summary>
        /// The outline color `unit` should render with - null for a free-standing, untamed Wild
        /// creature (Owner == null, Factions.Contains(Wild)), which deliberately gets no outline
        /// at all (it isn't on anyone's team, and Wild creatures can be hostile toward each
        /// other). Everything else - a hero, or anything it owns (a summon, a tamed former-Wild
        /// beast, a Portal) - resolves to its team root (GetTeamRoot) and takes that root's
        /// TeamOutlineColors entry, so a summoned/tamed unit always reads as the same color its
        /// owner does.
        /// </summary>
        private Color? GetTeamOutlineColor(BaseUnit unit)
        {
            if (unit.Owner == null && unit.Factions.Contains(Faction.Wild))
                return null;

            var root = GetTeamRoot(unit);
            return TeamOutlineColors.TryGetValue(root.Class, out var color) ? color : (Color?)null;
        }

        /// <summary>Cached by SpriteAssetPath (not by Texture2D reference - several units can share the exact same loaded art, e.g. the generic Pawn placeholder every summon without unique art uses) rather than recomputing per unit instance.</summary>
        private readonly Dictionary<string, Texture2D> _silhouetteCache = new Dictionary<string, Texture2D>();

        /// <summary>
        /// A pure white silhouette of unit.SpriteTexture - every originally-visible pixel (alpha
        /// > 0) forced to solid white, same alpha/shape otherwise - used as the source for the
        /// team-outline draw passes (see the "Draw units" loop) instead of the real art, so
        /// tinting it multiplies cleanly to the exact outline color everywhere, rather than
        /// multiplying against the sprite's own varied colors (which read as a smeared colored
        /// shadow, not a clean outline). Built once per distinct SpriteAssetPath and cached from
        /// then on - a one-time CPU-side GetData/SetData round trip, not a per-frame cost.
        /// </summary>
        private Texture2D GetSilhouetteTexture(BaseUnit unit)
        {
            if (_silhouetteCache.TryGetValue(unit.SpriteAssetPath, out var cached))
                return cached;

            var source = unit.SpriteTexture;
            var pixels = new Color[source.Width * source.Height];
            source.GetData(pixels);
            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].A > 0)
                    pixels[i] = new Color(255, 255, 255, (int)pixels[i].A);
            }

            var silhouette = new Texture2D(GraphicsDevice, source.Width, source.Height);
            silhouette.SetData(pixels);
            _silhouetteCache[unit.SpriteAssetPath] = silhouette;
            return silhouette;
        }

        /// <summary>
        /// Aggro System console log (see DevConsole, `~`): fires once per unit's turn start
        /// (TurnSystem.UnitTurnStarted), printing that unit's own aggro value toward every other
        /// living unit on the field, highest first - exactly the data RunAggroDrivenSummonAI's
        /// own target selection reads, surfaced for debugging/tuning rather than left invisible.
        /// </summary>
        private void LogUnitAggro(BaseUnit unit)
        {
            _console.Log($"{unit.Name} Aggression:");
            foreach (var other in _units.Where(u => u != unit && u.IsAlive).OrderByDescending(u => unit.GetAggro(u)))
                _console.Log($"{other.Name}: {unit.GetAggro(other)}");
        }

        /// <summary>
        /// Mana-aura tick, subscribed to TurnSystem.UnitTurnStarted right alongside LogUnitAggro -
        /// fires once at the start of EVERY unit's turn (`unitWhoseTurnStarted`), including an AI
        /// unit's. For every living unit with a mana aura (BaseUnit.ManaAuraRange &gt; 0 -
        /// currently just the Aetherfluff Beetle) OTHER than the one whose turn just started,
        /// restores ManaAuraAmount MP (clamped at MaxMP) to every living unit within
        /// ManaAuraRange of it - including the aura source itself (distance 0), and with NO team
        /// filtering at all, matching the ability's own wording ("regardless of them being
        /// friendly, neutral, or enemy"). Multiple auras overlapping the same target each apply
        /// independently (no diminishing returns or stacking cap) - same as multiple Carnivores'
        /// AngerRange checks already do nothing to prevent overlap either. Only logs a line when a
        /// target's MP actually changed (skips anyone already at full MP) so this doesn't spam the
        /// console once several units sit permanently inside a beetle's radius.
        /// </summary>
        private void ApplyManaAuras(BaseUnit unitWhoseTurnStarted)
        {
            foreach (var source in _units.Where(u => u.IsAlive && u.ManaAuraRange > 0 && u != unitWhoseTurnStarted))
            {
                var sourceHex = _hexGrid.WorldToHex(source.Position);
                foreach (var target in _units.Where(u => u.IsAlive))
                {
                    var targetHex = _hexGrid.WorldToHex(target.Position);
                    if (_hexGrid.GetDistance(sourceHex.col, sourceHex.row, targetHex.col, targetHex.row) > source.ManaAuraRange)
                        continue;

                    int before = target.CurrentMP;
                    target.CurrentMP = Math.Min(target.MaxMP, target.CurrentMP + source.ManaAuraAmount);
                    if (target.CurrentMP != before)
                        _console.Log($"{source.Name}'s aura restores {target.CurrentMP - before} MP to {target.Name}.");
                }
            }
        }

        /// <summary>
        /// Prints a separator line, then a "{UnitName} - Turn {N}" header, to BOTH the in-game
        /// dev console (`~`) and the terminal log (GameLogger) every time a unit's turn actually
        /// starts (TurnSystem.UnitTurnStarted, subscribed right alongside LogUnitAggro and fired
        /// just before it, so each unit's own block visibly starts with this header followed
        /// immediately by its Aggro summary) - without this, everything every unit's turn logs
        /// runs together with no visual boundary at all. No separator before the very first
        /// unit's turn of a match (_hasLoggedAnyUnitTurn, reset in EnterTestMode/
        /// EnterOriginalMode), since nothing precedes it that needs separating from.
        /// </summary>
        private void LogUnitTurnDivider(BaseUnit unit)
        {
            if (_hasLoggedAnyUnitTurn)
                _console.Log(new string('=', 30));
            _console.Log($"{unit.Name} - Turn {_turnSystem.CurrentTurn}");
            _hasLoggedAnyUnitTurn = true;

            GameLogger.LogUnitTurnStart(unit.Name, _turnSystem.CurrentTurn);
        }

        /// <summary>True if `unit` is a summon the real player hero (_playerHero) should get a normal turn with - owned by the player's hero AND still set to the default SummonBehavior.ControllableBySummoner (see Update's per-turn control handoff). False for the hero itself (Owner is null, not _playerHero), an enemy's own summon, or any summon using one of the other 5 not-yet-implemented behaviors.</summary>
        private bool IsPlayerControllableSummon(BaseUnit unit) =>
            unit.Owner != null && unit.Owner == _playerHero && unit.Behavior == SummonBehavior.ControllableBySummoner;

        /// <summary>Every living enemy unit (not on the player's own team - see GetTeamRoot) within the move's effective Range (see Move.GetEffectiveRange) of the player's current position.</summary>
        private List<BaseUnit> GetValidAttackTargets(Move move, Weapon sourceWeapon)
        {
            var attackerHex = _hexGrid.WorldToHex(_playerUnit.Position);
            int range = move.GetEffectiveRange(_playerUnit, sourceWeapon);
            var playerTeam = GetTeamRoot(_playerUnit);
            return _units.Where(u => GetTeamRoot(u) != playerTeam && u.IsAlive
                    && _hexGrid.GetDistance(attackerHex.col, attackerHex.row, _hexGrid.WorldToHex(u.Position).col, _hexGrid.WorldToHex(u.Position).row) <= range)
                .ToList();
        }

        /// <summary>
        /// Whether a SpellCard's effect can actually be cast right now through the same
        /// AttackResolver pipeline weapon attacks use - true for anything that deals real damage
        /// to a chosen target (or every adjacent enemy). Trap spells are excluded on purpose
        /// despite having a damage formula: Place Trap/Explosive Trap are meant to place
        /// something that triggers later, and resolving them as an immediate hit on a clicked
        /// target would misrepresent what the card actually does - they need a real
        /// placement/trigger system first. A self/ally utility spell - a stat buff (Brace, Rally
        /// Cry, Steady Hands) and/or a next-turn AP grant (Second Wind) - casts through a
        /// separate path instead (see SpellIsSelfUtility/CastSelfUtilitySpell); everything else
        /// (heals, taming, summoning via spell, ...) has no execution path yet at all; see
        /// TryCastSelectedCard.

        /// </summary>
        private static bool SpellCastsAsAttack(SpellCard card)
        {
            if (card.Name == "Place Trap" || card.Name == "Explosive Trap")
                return false;
            return card.Effect.BaseDamage > 0 && !card.Effect.TargetsAllies;
        }

        /// <summary>
        /// Cast a damage-dealing SpellCard through the exact same AP/MP-check, targeting, and
        /// AttackResolver pipeline a weapon attack uses - sourceWeapon is null throughout, same
        /// as a racial move. The card is only removed from hand once it actually resolves (see
        /// ExecutePendingAttack) - cancelling target selection leaves it in hand untouched.
        /// </summary>
        private void CastSpellAsAttack(SpellCard card)
        {
            var move = card.Effect;
            int apCost = _playerUnit.GetEffectiveAPCost(move, null);
            if (apCost > _playerUnit.CurrentAP || move.MPCost > _playerUnit.CurrentMP)
            {
                ShowCombatMessage($"Not enough AP/MP for {move.Name}.");
                return;
            }

            if (!move.HitsAllAdjacent)
            {
                var validTargets = GetValidAttackTargets(move, null);
                if (validTargets.Count == 0)
                {
                    ShowCombatMessage($"Nothing in range for {move.Name}.");
                    return;
                }
                _targetCandidates = validTargets;
            }

            _pendingMove = move;
            _pendingSourceWeapon = null;
            _pendingSpellCard = card;
            _cardMenuActive = false;

            if (move.HitsAllAdjacent)
            {
                ExecutePendingAttack(null);
            }
            else
            {
                _targetingFromCardMenu = true;
                _targetingModeActive = true;
            }
        }

        /// <summary>
        /// Cast an IsBlink spell (e.g. Blink, Disengage): pays AP/MP up front like any other
        /// cast, then opens teleport targeting mode showing every passable, unoccupied tile
        /// within Range of the caster - clicking one instantly moves the caster there (see
        /// HandleTeleportInput/ExecuteBlink). Cancelling (E) refunds nothing since nothing is
        /// spent until a destination is actually chosen.
        /// </summary>
        private void CastBlinkSpell(SpellCard card)
        {
            var move = card.Effect;
            int apCost = _playerUnit.GetEffectiveAPCost(move, null);
            if (apCost > _playerUnit.CurrentAP || move.MPCost > _playerUnit.CurrentMP)
            {
                ShowCombatMessage($"Not enough AP/MP for {move.Name}.");
                return;
            }

            var casterHex = _hexGrid.WorldToHex(_playerUnit.Position);
            var occupied = GetDestinationBlockingTiles(_playerUnit);
            _teleportValidTiles = _hexGrid.GetHexesInRadius(casterHex.col, casterHex.row, move.Range)
                .Where(hex => hex != casterHex && Pathfinder.IsPassable(_hexGrid, _map, hex.col, hex.row, occupied))
                .ToHashSet();

            if (_teleportValidTiles.Count == 0)
            {
                ShowCombatMessage($"Nowhere to go for {move.Name}.");
                return;
            }

            _pendingMove = move;
            _pendingSpellCard = card;
            _cardMenuActive = false;
            _teleportModeActive = true;
        }

        /// <summary>Handle input while picking a teleport destination for an IsBlink spell: click a highlighted tile to blink there, E to cancel back to the Cards menu.</summary>
        private void HandleTeleportInput(KeyboardState keyboardState, MouseState mouseState)
        {
            if (keyboardState.IsKeyDown(Keys.E) && !_previousKeyboardState.IsKeyDown(Keys.E))
            {
                CancelTeleport();
                return;
            }

            if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
            {
                Vector2 viewportSize = new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
                var clickedHex = _renderer.GetHexAtScreenPos(mouseState.X, mouseState.Y, viewportSize);
                if (_teleportValidTiles.Contains(clickedHex))
                    ExecuteBlink(clickedHex);
            }
        }

        /// <summary>Spend _pendingMove's AP/MP, move the caster straight to destHex, consume the card, and return to the turn menu.</summary>
        private void ExecuteBlink((int col, int row) destHex)
        {
            _playerUnit.SpendAP(_playerUnit.GetEffectiveAPCost(_pendingMove, null));
            _playerUnit.CurrentMP = Math.Max(0, _playerUnit.CurrentMP - _pendingMove.MPCost);
            _playerUnit.Position = _hexGrid.HexToWorld(destHex.col, destHex.row);
            ReleasePinOnMovedTarget(_playerUnit);

            ShowCombatMessage($"{_playerUnit.Name} uses {_pendingMove.Name}.");

            if (_pendingSpellCard != null)
                RemoveCardFromHand(_pendingSpellCard);

            CancelTeleport(returnToCardMenu: false);
        }

        private void CancelTeleport(bool returnToCardMenu = true)
        {
            _teleportModeActive = false;
            _pendingMove = null;
            _pendingSpellCard = null;
            _teleportValidTiles.Clear();

            if (returnToCardMenu)
            {
                _cardMenuActive = true;
            }
            else
            {
                _turnMenuIndex = 0;
                _turnMenuActive = true;
            }
        }

        /// <summary>
        /// Opens Leap's own dual targeting mode (see _leapModeActive's own doc comment) -
        /// ConfirmAttackMenuSelection already CHECKED AP/MP affordability before dispatching here,
        /// same as every other move, but hasn't spent anything yet; the actual spend happens on
        /// confirm (ExecuteLeapToTile/ExecuteLeapOntoUnit), same "pay on confirm, not on opening
        /// the targeting mode" pattern Blink/Place Trap/Tame Beast all already follow.
        /// </summary>
        private void OpenLeapTargeting(Move move)
        {
            var casterHex = _hexGrid.WorldToHex(_playerUnit.Position);
            int range = move.GetEffectiveRange(_playerUnit, null);
            var occupied = GetDestinationBlockingTiles(_playerUnit);

            _leapValidTiles = _hexGrid.GetHexesInRadius(casterHex.col, casterHex.row, range)
                .Where(hex => hex != casterHex && _hexGrid.IsInBounds(hex.col, hex.row)
                    && Pathfinder.IsPassable(_hexGrid, _map, hex.col, hex.row, occupied))
                .ToHashSet();

            _leapValidTargets = _units.Where(u => u != _playerUnit && u.IsAlive
                    && _hexGrid.GetDistance(casterHex.col, casterHex.row, _hexGrid.WorldToHex(u.Position).col, _hexGrid.WorldToHex(u.Position).row) <= range
                    && _hexGrid.GetNeighbors(_hexGrid.WorldToHex(u.Position).col, _hexGrid.WorldToHex(u.Position).row)
                        .Any(n => Pathfinder.IsPassable(_hexGrid, _map, n.col, n.row, occupied)))
                .ToList();

            if (_leapValidTiles.Count == 0 && _leapValidTargets.Count == 0)
            {
                ShowCombatMessage($"Nowhere to {move.Name}.");
                return;
            }

            _attackMenuActive = false;
            _leapModeActive = true;
        }

        /// <summary>Handle input while picking a Leap destination: click a highlighted tile to land there, a highlighted unit to pounce onto it instead, E to cancel back to the turn menu.</summary>
        private void HandleLeapInput(KeyboardState keyboardState, MouseState mouseState)
        {
            if (keyboardState.IsKeyDown(Keys.E) && !_previousKeyboardState.IsKeyDown(Keys.E))
            {
                CancelLeap();
                return;
            }

            if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
            {
                Vector2 viewportSize = new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
                var clickedHex = _renderer.GetHexAtScreenPos(mouseState.X, mouseState.Y, viewportSize);

                var targetUnit = _leapValidTargets.FirstOrDefault(u => _hexGrid.WorldToHex(u.Position) == clickedHex);
                if (targetUnit != null)
                {
                    ExecuteLeapOntoUnit(targetUnit);
                    return;
                }

                if (_leapValidTiles.Contains(clickedHex))
                    ExecuteLeapToTile(clickedHex);
            }
        }

        /// <summary>Spend _pendingMove's AP/MP, move the caster straight to destHex (a plain reposition, no landing effect), consume the card, and return to the turn menu.</summary>
        private void ExecuteLeapToTile((int col, int row) destHex)
        {
            _playerUnit.SpendAP(_playerUnit.GetEffectiveAPCost(_pendingMove, null));
            _playerUnit.CurrentMP = Math.Max(0, _playerUnit.CurrentMP - _pendingMove.MPCost);
            _playerUnit.Position = _hexGrid.HexToWorld(destHex.col, destHex.row);

            ShowCombatMessage($"{_playerUnit.Name} leaps to open ground.");
            CancelLeap();
        }

        /// <summary>
        /// Spend _pendingMove's AP/MP, then resolve Leap's own landing effect against `target`.
        /// If target's EffectiveStrength is LOWER than the caster's: the caster lands directly ON
        /// target's own tile (sharing it - on request, the fiction of physically pinning
        /// something down means standing on top of it, not next to it; see Release's own tile
        /// prompt for how the caster steps back off once it lets go), applies Knocked Down to
        /// target, and "Pinning" (PinnedTarget = target) to itself. Otherwise the pin fails: the
        /// caster lands on whichever free tile adjacent to target OpenLeapTargeting already
        /// confirmed exists instead (no tile-sharing without a successful pin), just a
        /// combat-log line, no further effect. Deals no damage either way - Leap is pure
        /// positioning + conditional CC, not an attack.
        /// </summary>
        private void ExecuteLeapOntoUnit(BaseUnit target)
        {
            _playerUnit.SpendAP(_playerUnit.GetEffectiveAPCost(_pendingMove, null));
            _playerUnit.CurrentMP = Math.Max(0, _playerUnit.CurrentMP - _pendingMove.MPCost);

            if (target.EffectiveStrength < _playerUnit.EffectiveStrength)
            {
                _playerUnit.Position = target.Position;
                target.ApplyKnockdown(1);
                _playerUnit.ApplyStatusEffect(new StatusEffect
                {
                    Name = "Pinning",
                    Type = StatusEffectType.Buff,
                    TurnsRemaining = -1,
                    PinnedTarget = target,
                });
                ShowCombatMessage($"{_playerUnit.Name} pins {target.Name} to the ground!");
            }
            else
            {
                var occupied = GetDestinationBlockingTiles(_playerUnit);
                var targetHex = _hexGrid.WorldToHex(target.Position);
                var landingHex = _hexGrid.GetNeighbors(targetHex.col, targetHex.row)
                    .First(n => Pathfinder.IsPassable(_hexGrid, _map, n.col, n.row, occupied));
                _playerUnit.Position = _hexGrid.HexToWorld(landingHex.col, landingHex.row);
                ShowCombatMessage($"{_playerUnit.Name} leaps onto {target.Name}, but can't bring them down.");
            }

            CancelLeap();
        }

        private void CancelLeap()
        {
            _leapModeActive = false;
            _pendingMove = null;
            _leapValidTiles.Clear();
            _leapValidTargets.Clear();

            _turnMenuIndex = 0;
            _turnMenuActive = true;
        }

        /// <summary>
        /// The "Release" turn-menu option (see TurnMenuBuilder) - opens a plain empty-tile
        /// targeting mode over every passable, unoccupied tile adjacent to the caster's CURRENT
        /// position (shared with its pinned target while Pinning is active - see
        /// ExecuteLeapOntoUnit) rather than instantly letting go, since the caster has to
        /// physically step off that shared tile. No cost - Release is free either way. If
        /// literally no adjacent tile is free (fully boxed in), Pinning simply can't be released
        /// yet rather than leaving the caster stuck sharing a tile outside of an active pin.
        /// </summary>
        private void OpenReleaseTargeting()
        {
            var currentHex = _hexGrid.WorldToHex(_playerUnit.Position);
            var occupied = GetDestinationBlockingTiles(_playerUnit);
            _releaseValidTiles = _hexGrid.GetNeighbors(currentHex.col, currentHex.row)
                .Where(hex => _hexGrid.IsInBounds(hex.col, hex.row) && Pathfinder.IsPassable(_hexGrid, _map, hex.col, hex.row, occupied))
                .ToHashSet();

            if (_releaseValidTiles.Count == 0)
            {
                ShowCombatMessage($"{_playerUnit.Name} has nowhere to step back to.");
                return;
            }

            _turnMenuActive = false;
            _releaseModeActive = true;
        }

        /// <summary>Handle input while picking where to step off to on Release: click a highlighted adjacent tile to move there and let go, E to cancel back to the turn menu (still Pinning).</summary>
        private void HandleReleaseInput(KeyboardState keyboardState, MouseState mouseState)
        {
            if (keyboardState.IsKeyDown(Keys.E) && !_previousKeyboardState.IsKeyDown(Keys.E))
            {
                CancelRelease();
                return;
            }

            if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
            {
                Vector2 viewportSize = new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
                var clickedHex = _renderer.GetHexAtScreenPos(mouseState.X, mouseState.Y, viewportSize);
                if (_releaseValidTiles.Contains(clickedHex))
                    ExecuteRelease(clickedHex);
            }
        }

        /// <summary>Step the caster off to destHex, remove "Pinning" (freeing its former target to attempt standing up normally again), and return to the turn menu.</summary>
        private void ExecuteRelease((int col, int row) destHex)
        {
            var pinning = _playerUnit.StatusEffects.FirstOrDefault(e => e.Name == "Pinning");
            string targetName = pinning?.PinnedTarget?.Name ?? "its grip";

            _playerUnit.Position = _hexGrid.HexToWorld(destHex.col, destHex.row);
            _playerUnit.RemoveStatusEffect("Pinning");
            ShowCombatMessage($"{_playerUnit.Name} releases {targetName}.");

            CancelRelease();
        }

        private void CancelRelease()
        {
            _releaseModeActive = false;
            _releaseValidTiles.Clear();

            _turnMenuIndex = 0;
            _turnMenuActive = true;
        }

        /// <summary>
        /// Cast an IsAllyShield spell (e.g. Arcane Shield): pays AP/MP up front, then opens
        /// ally-targeting mode listing every living unit within Range - including the caster
        /// itself, since shielding yourself is a valid choice. Clicking one grants it a shield
        /// sized by move.GetDamage(caster) (reusing the attack-damage formula fields as the
        /// shield's magnitude, not actual damage) and consumes the card.
        /// </summary>
        private void CastAllyShield(SpellCard card)
        {
            var move = card.Effect;
            int apCost = _playerUnit.GetEffectiveAPCost(move, null);
            if (apCost > _playerUnit.CurrentAP || move.MPCost > _playerUnit.CurrentMP)
            {
                ShowCombatMessage($"Not enough AP/MP for {move.Name}.");
                return;
            }

            var casterHex = _hexGrid.WorldToHex(_playerUnit.Position);
            int range = move.GetEffectiveRange(_playerUnit, null);
            _allyTargetCandidates = _units.Where(u => u.IsAlive && (u == _playerUnit
                    || _hexGrid.GetDistance(casterHex.col, casterHex.row, _hexGrid.WorldToHex(u.Position).col, _hexGrid.WorldToHex(u.Position).row) <= range))
                .ToList();

            if (_allyTargetCandidates.Count == 0)
            {
                ShowCombatMessage($"Nothing in range for {move.Name}.");
                return;
            }

            _pendingMove = move;
            _pendingSpellCard = card;
            _cardMenuActive = false;
            _allyTargetModeActive = true;
        }

        /// <summary>Handle input while picking a shield target for an IsAllyShield spell: click a highlighted unit (including the caster's own tile) to shield it, E to cancel back to the Cards menu.</summary>
        private void HandleAllyTargetInput(KeyboardState keyboardState, MouseState mouseState)
        {
            if (keyboardState.IsKeyDown(Keys.E) && !_previousKeyboardState.IsKeyDown(Keys.E))
            {
                CancelAllyTarget();
                return;
            }

            if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
            {
                Vector2 viewportSize = new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
                var clickedHex = _renderer.GetHexAtScreenPos(mouseState.X, mouseState.Y, viewportSize);
                var target = _allyTargetCandidates.FirstOrDefault(u => _hexGrid.WorldToHex(u.Position) == clickedHex);
                if (target != null)
                    ExecuteAllyShield(target);
            }
        }

        /// <summary>Spend _pendingMove's AP/MP, grant target a shield, consume the card, and return to the turn menu.</summary>
        private void ExecuteAllyShield(BaseUnit target)
        {
            _playerUnit.SpendAP(_playerUnit.GetEffectiveAPCost(_pendingMove, null));
            _playerUnit.CurrentMP = Math.Max(0, _playerUnit.CurrentMP - _pendingMove.MPCost);

            int shieldAmount = _pendingMove.GetDamage(_playerUnit);
            target.ApplyShield(shieldAmount);

            ShowCombatMessage(target == _playerUnit
                ? $"{_playerUnit.Name} shields themself ({shieldAmount} points)."
                : $"{_playerUnit.Name} shields {target.Name} ({shieldAmount} points).");

            if (_pendingSpellCard != null)
                RemoveCardFromHand(_pendingSpellCard);

            CancelAllyTarget(returnToCardMenu: false);
        }

        private void CancelAllyTarget(bool returnToCardMenu = true)
        {
            _allyTargetModeActive = false;
            _pendingMove = null;
            _pendingSpellCard = null;
            _allyTargetCandidates.Clear();

            if (returnToCardMenu)
            {
                _cardMenuActive = true;
            }
            else
            {
                _turnMenuIndex = 0;
                _turnMenuActive = true;
            }
        }

        /// <summary>
        /// Cast an IsTameBeast spell (Tame Beast): pays AP/MP up front, then opens tame-targeting
        /// mode listing every genuinely tameable unit within Range - BaseUnit.IsTameable (Wild
        /// faction, no Owner yet, still Neutral - never Friendly or Hostile) AND at or below the
        /// caster's own Level, which IsTameable itself can't check since it has no caster to
        /// compare against. Clicking one rolls move.BaseAccuracy as a flat success chance (not
        /// the usual combat accuracy/evasion math - taming isn't an attack, nothing to dodge) and
        /// consumes the card either way, same "it happened, it just didn't land" convention a
        /// missed attack already follows.
        /// </summary>
        private void CastTameBeast(SpellCard card)
        {
            var move = card.Effect;
            int apCost = _playerUnit.GetEffectiveAPCost(move, null);
            if (apCost > _playerUnit.CurrentAP || move.MPCost > _playerUnit.CurrentMP)
            {
                ShowCombatMessage($"Not enough AP/MP for {move.Name}.");
                return;
            }

            var casterHex = _hexGrid.WorldToHex(_playerUnit.Position);
            int range = move.GetEffectiveRange(_playerUnit, null);
            _tameBeastCandidates = _units.Where(u => u.IsTameable && u.Level <= _playerUnit.Level
                    && _hexGrid.GetDistance(casterHex.col, casterHex.row, _hexGrid.WorldToHex(u.Position).col, _hexGrid.WorldToHex(u.Position).row) <= range)
                .ToList();

            if (_tameBeastCandidates.Count == 0)
            {
                ShowCombatMessage($"Nothing to tame in range for {move.Name}.");
                return;
            }

            _pendingMove = move;
            _pendingSpellCard = card;
            _cardMenuActive = false;
            _tameBeastModeActive = true;
        }

        /// <summary>Handle input while picking a Tame Beast target: click a highlighted candidate to attempt taming it, E to cancel back to the Cards menu.</summary>
        private void HandleTameBeastInput(KeyboardState keyboardState, MouseState mouseState)
        {
            if (keyboardState.IsKeyDown(Keys.E) && !_previousKeyboardState.IsKeyDown(Keys.E))
            {
                CancelTameBeast();
                return;
            }

            if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
            {
                Vector2 viewportSize = new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
                var clickedHex = _renderer.GetHexAtScreenPos(mouseState.X, mouseState.Y, viewportSize);
                var target = _tameBeastCandidates.FirstOrDefault(u => _hexGrid.WorldToHex(u.Position) == clickedHex);
                if (target != null)
                    ExecuteTameBeast(target);
            }
        }

        /// <summary>
        /// Spend _pendingMove's AP/MP, roll BaseAccuracy as a flat success chance, and on success
        /// hand `target` over to the caster entirely - Owner, Behavior reset to
        /// ControllableBySummoner (so it gets a normal turn under the caster's control, same as
        /// any other owned summon), and Disposition set to Friendly (no longer a free Wild agent,
        /// so IsTameable - and therefore a second Tame Beast attempt - naturally excludes it from
        /// here on). Consumes the card regardless of the roll's outcome, same as a missed attack.
        /// </summary>
        private void ExecuteTameBeast(BaseUnit target)
        {
            _playerUnit.SpendAP(_playerUnit.GetEffectiveAPCost(_pendingMove, null));
            _playerUnit.CurrentMP = Math.Max(0, _playerUnit.CurrentMP - _pendingMove.MPCost);

            bool success = _deckRandom.NextDouble() < _pendingMove.BaseAccuracy;
            if (success)
            {
                target.Owner = _playerUnit;
                target.Behavior = SummonBehavior.ControllableBySummoner;
                target.Disposition = Disposition.Friendly;
                ShowCombatMessage($"{_playerUnit.Name} tames {target.Name}!");
            }
            else
            {
                ShowCombatMessage($"{_playerUnit.Name}'s attempt to tame {target.Name} fails.");
            }

            if (_pendingSpellCard != null)
                RemoveCardFromHand(_pendingSpellCard);

            CancelTameBeast(returnToCardMenu: false);
        }

        private void CancelTameBeast(bool returnToCardMenu = true)
        {
            _tameBeastModeActive = false;
            _pendingMove = null;
            _pendingSpellCard = null;
            _tameBeastCandidates.Clear();

            if (returnToCardMenu)
            {
                _cardMenuActive = true;
            }
            else
            {
                _turnMenuIndex = 0;
                _turnMenuActive = true;
            }
        }

        /// <summary>
        /// The trap Move.IsPlaceTrap/CastPlaceTrap actually places - see Objects.Trap for why its
        /// damage/status are hardcoded here rather than read off the casting Move (there's no
        /// caster present at the moment it actually triggers, so nothing to scale off Strength/
        /// Intelligence - the Move's own BaseDamage/InflictsStatusEffect fields are unused for
        /// this spell, only its AP/MP/Range matter). Applies BOTH of its two afflictions
        /// unconditionally (no chance roll - on request, replacing the original single 50%-chance
        /// Bleeding this used to carry):
        /// - Weak Root: PreventsMovement + ZeroesEvasion, TurnsRemaining 2 - the existing generic
        ///   per-turn countdown (BaseUnit.TickStatusEffects, !RestrictsActions so it ticks
        ///   automatically) decrements this once at the start of the victim's NEXT turn (to 1,
        ///   staying active the whole way through it) and again at the start of the turn AFTER
        ///   THAT (to 0, removed) - net effect: blocks movement for the rest of the turn it's
        ///   triggered on PLUS the victim's entire next turn, exactly "until the end of the
        ///   unit's next turn."
        /// - Weak Bleed: EndOfTurn/AffectedUnitsTurn, 5% max HP (DamagePercentMaxHPPerTick,
        ///   dispatched by TurnSystem.DispatchTimedEffects - see Combat.StatusEffectTiming) plus
        ///   1% max HP per action (DamagePercentMaxHPPerAction, see BaseUnit.SpendAP) - both true
        ///   damage, bypassing Defense/Resistance same as the older generic Bleeding mechanic.
        ///   Indefinite duration (TurnsRemaining -1) - no duration was specified, so this persists
        ///   like generic Bleeding does, with no cure path yet either (CuresBleed only clears the
        ///   separate BleedPercentPerTurn field, not this). Dropping a victim under 5% max HP
        ///   needs no extra code at all - BaseUnit.UpdateFaintStatus (already Faints anyone at or
        ///   under that exact threshold) runs automatically inside ApplyTrueDamage already.
        /// </summary>
        private Trap CreateBearatTrap((int col, int row) tile, BaseUnit owner) => new Trap
        {
            Name = "Bearat Trap",
            Tile = tile,
            Owner = owner,
            Range = 0,
            FlatDamage = 1,
            DamageType = DamageType.Sharp,
            AppliedEffects = new List<StatusEffect>
            {
                new StatusEffect
                {
                    Name = "Weak Root",
                    Type = StatusEffectType.Affliction,
                    Timing = StatusEffectTiming.Active,
                    TurnsRemaining = 2,
                    PreventsMovement = true,
                    ZeroesEvasion = true,
                },
                new StatusEffect
                {
                    Name = "Weak Bleed",
                    Type = StatusEffectType.Affliction,
                    Timing = StatusEffectTiming.EndOfTurn,
                    TurnTrigger = StatusEffectTurnTrigger.AffectedUnitsTurn,
                    TurnsRemaining = -1,
                    DamagePercentMaxHPPerTick = 0.05f,
                    DamagePercentMaxHPPerAction = 0.01f,
                },
            },
        };

        /// <summary>
        /// Cast an IsPlaceTrap spell (e.g. Place Trap): pays AP/MP up front like any other cast,
        /// then opens trap-placement targeting showing every passable, unoccupied, not-already-
        /// trapped tile within Range of the caster - clicking one plants a Trap there (see
        /// HandleTrapPlacementInput/ExecutePlaceTrap). Cancelling (E) refunds nothing since
        /// nothing is spent until a tile is actually chosen, same as teleport targeting.
        /// </summary>
        private void CastPlaceTrap(SpellCard card)
        {
            var move = card.Effect;
            int apCost = _playerUnit.GetEffectiveAPCost(move, null);
            if (apCost > _playerUnit.CurrentAP || move.MPCost > _playerUnit.CurrentMP)
            {
                ShowCombatMessage($"Not enough AP/MP for {move.Name}.");
                return;
            }

            var casterHex = _hexGrid.WorldToHex(_playerUnit.Position);
            var occupied = GetDestinationBlockingTiles(_playerUnit);
            var trappedTiles = _placedTraps.Select(t => t.Tile).ToHashSet();
            _trapPlacementValidTiles = _hexGrid.GetHexesInRadius(casterHex.col, casterHex.row, move.GetEffectiveRange(_playerUnit, null))
                .Where(hex => Pathfinder.IsPassable(_hexGrid, _map, hex.col, hex.row, occupied) && !trappedTiles.Contains(hex))
                .ToHashSet();

            if (_trapPlacementValidTiles.Count == 0)
            {
                ShowCombatMessage($"Nowhere to place {move.Name}.");
                return;
            }

            _pendingMove = move;
            _pendingSpellCard = card;
            _cardMenuActive = false;
            _trapPlacementModeActive = true;
        }

        /// <summary>
        /// Cast the auto-granted "Reshuffle" card (see EnsureReshuffleCardIfNeeded): pays its
        /// own (free - 1 AP/0 MP, exempt from the surcharge it itself creates) cost, then moves
        /// EVERY card currently in the caster's class discard pile back into its deck, shuffled -
        /// each one cloned with its MP cost 1 higher than it currently is (Move.WithMPCostIncrease/
        /// SpellCard.WithEffect/SummonCard.WithManaCost - the shared catalog instance every OTHER
        /// class's copy of that card still uses is never touched), so reshuffling has a real,
        /// permanent cost rather than being a free reset - and compounds on a card that gets
        /// reshuffled more than once, since each clone's cost is read fresh off whatever it
        /// currently is. No targeting step - resolves immediately, same as Conjure Potions. Needs
        /// direct `_classDecks`/`_classDiscardPiles` access `Combat.SpellCaster` deliberately
        /// doesn't have (it only ever touches the caster's own BaseUnit state), so this lives here
        /// rather than there, same reasoning CastSummon/CastPlaceTrap/CastTameBeast already do.
        /// </summary>
        private void CastReshuffle(SpellCard card)
        {
            var move = card.Effect;
            int apCost = _playerUnit.GetEffectiveAPCost(move, null);
            if (apCost > _playerUnit.CurrentAP || move.MPCost > _playerUnit.CurrentMP)
            {
                ShowCombatMessage($"Not enough AP/MP for {move.Name}.");
                return;
            }

            _playerUnit.CurrentAP = Math.Max(0, _playerUnit.CurrentAP - apCost);
            _playerUnit.CurrentMP = Math.Max(0, _playerUnit.CurrentMP - move.MPCost);

            var heroClass = _playerUnit.Class;
            _classDiscardPiles.TryGetValue(heroClass, out var pile);
            // Reshuffle itself is excluded even if it somehow ended up in the pile (it's never
            // drawn normally - not part of SpellCatalog.AllCards - but defend against it anyway
            // rather than risk Reshuffle clones polluting a deck that could then draw them).
            var toReshuffle = (pile ?? new List<object>()).Where(c => !(c is SpellCard sc && sc.Effect.IsReshuffle)).ToList();

            if (toReshuffle.Count == 0)
            {
                ShowCombatMessage($"{_playerUnit.Name} has no used cards to reshuffle.");
            }
            else
            {
                if (!_classDecks.TryGetValue(heroClass, out var deck) || deck == null)
                {
                    deck = new List<object>();
                    _classDecks[heroClass] = deck;
                }

                foreach (var usedCard in toReshuffle)
                {
                    object upgraded = usedCard switch
                    {
                        SpellCard sc => sc.WithEffect(sc.Effect.WithMPCostIncrease(1)),
                        SummonCard smc => smc.WithManaCost(smc.ManaCost + 1),
                        _ => usedCard
                    };
                    deck.Add(upgraded);
                }
                pile.Clear();

                for (int i = deck.Count - 1; i > 0; i--)
                {
                    int j = _deckRandom.Next(i + 1);
                    (deck[i], deck[j]) = (deck[j], deck[i]);
                }

                ShowCombatMessage($"{_playerUnit.Name} reshuffles their used cards back into their deck - each now costs 1 more mana.");
            }

            RemoveCardFromHand(card);
            _cardMenuActive = false;
            _turnMenuIndex = 0;
            _turnMenuActive = true;
        }

        /// <summary>Handle input while picking a trap's placement tile: click a highlighted tile to plant it there, E to cancel back to the Cards menu.</summary>
        private void HandleTrapPlacementInput(KeyboardState keyboardState, MouseState mouseState)
        {
            if (keyboardState.IsKeyDown(Keys.E) && !_previousKeyboardState.IsKeyDown(Keys.E))
            {
                CancelTrapPlacement();
                return;
            }

            if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
            {
                Vector2 viewportSize = new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
                var clickedHex = _renderer.GetHexAtScreenPos(mouseState.X, mouseState.Y, viewportSize);
                if (_trapPlacementValidTiles.Contains(clickedHex))
                    ExecutePlaceTrap(clickedHex);
            }
        }

        /// <summary>Spend _pendingMove's AP/MP, record a new Trap (see CreateBearatTrap) at destHex owned by the caster, consume the card, and return to the turn menu.</summary>
        private void ExecutePlaceTrap((int col, int row) destHex)
        {
            _playerUnit.SpendAP(_playerUnit.GetEffectiveAPCost(_pendingMove, null));
            _playerUnit.CurrentMP = Math.Max(0, _playerUnit.CurrentMP - _pendingMove.MPCost);

            var trap = CreateBearatTrap(destHex, _playerUnit);
            _placedTraps.Add(trap);
            ShowCombatMessage($"{_playerUnit.Name} sets a {trap.Name}.");

            if (_pendingSpellCard != null)
                RemoveCardFromHand(_pendingSpellCard);

            CancelTrapPlacement(returnToCardMenu: false);
        }

        private void CancelTrapPlacement(bool returnToCardMenu = true)
        {
            _trapPlacementModeActive = false;
            _pendingMove = null;
            _pendingSpellCard = null;
            _trapPlacementValidTiles.Clear();

            if (returnToCardMenu)
            {
                _cardMenuActive = true;
            }
            else
            {
                _turnMenuIndex = 0;
                _turnMenuActive = true;
            }
        }

        /// <summary>
        /// Resolve `trap` triggering against `mover` (called from MoveUnitTowards the instant ANY
        /// unit - any team, even Owner's own - reaches a tile within its Range): removes the trap
        /// (one use only), deals its flat damage with no hit roll (a trap doesn't miss) and no
        /// stat scaling (see Objects.Trap), grants the same aggro a landed attack would, spawns a
        /// floating damage number (SpawnDamageNumber - a trap trigger has no attack animation of
        /// its own to otherwise carry that feedback), then applies every one of its AppliedEffects
        /// unconditionally (no chance roll - see CreateBearatTrap for the Bearat Trap's own two).
        /// </summary>
        private void TriggerTrap(Trap trap, BaseUnit mover)
        {
            _placedTraps.Remove(trap);

            mover.TakeDamage(trap.FlatDamage, trap.DamageType);
            mover.AddAggro(trap.Owner, 5);
            if (trap.FlatDamage > 0)
                mover.AddAggro(trap.Owner, 2);

            SpawnDamageNumber(mover, trap.FlatDamage);
            ShowCombatMessage($"{mover.Name} triggers {trap.Owner.Name}'s {trap.Name} for {trap.FlatDamage} damage!");

            foreach (var effect in trap.AppliedEffects)
            {
                mover.ApplyStatusEffect(effect);
                ShowCombatMessage($"{mover.Name} is afflicted with {effect.Name}!");
            }
        }

        /// <summary>Flat AP cost to summon a creature - first-pass placeholder, pending a balance pass (see GDD Portal System/Summon Cards). Unchanged whether or not a Generic Portal also needs conjuring first - that only ever costs extra mana (GenericPortalSummonManaSurcharge), never extra AP.</summary>
        private const int SummonAPCost = 2;

        /// <summary>Hard cap on a single class's hand (TryDrawCardFromDeck refuses to draw past this) - first-pass placeholder. Deliberately NOT enforced on EnsureHandInitialized's starting draw (always well under this) or the dev console's "add card" command (an explicit debug tool meant to bypass normal limits, same as Test Mode already ignoring the deck-size cap elsewhere).</summary>
        private const int MaxHandSize = 10;

        /// <summary>How far from its anchor point a summoned creature can be placed - normally its owner's Generic Portal (see OpenSummonCreaturePlacement), or the caster directly in the fallback case where a creature somehow needs placing with no portal present.</summary>
        private const int SummonPlacementRange = 1;

        /// <summary>How far from the caster a Generic Portal itself can be placed - Phase 1 of CastSummon, only needed the caster's first Generic summon (see GenericPortalSummonManaSurcharge/PlayerHasActiveGenericPortal).</summary>
        private const int GenericPortalPlacementRange = 5;

        /// <summary>
        /// Extra mana a Generic summon (every SummonCard today - no non-Generic portal type
        /// exists yet) costs on top of its own ManaCost, charged ONLY the first time a caster
        /// summons one with no Generic Portal of their own currently standing - it conjures that
        /// portal in the same action. 0 extra once the portal exists; back to this if it's ever
        /// destroyed (see PlayerHasActiveGenericPortal, checked fresh every CastSummon/card-cost
        /// display - nothing caches "already paid").
        /// </summary>
        private const int GenericPortalSummonManaSurcharge = 5;

        private const int GenericPortalHP = 20;
        private const int GenericPortalDefense = 10;
        private const int GenericPortalResistance = 10;

        /// <summary>True if `summoner` currently has a living Generic Portal of their own on the battlefield - see GenericPortalSummonManaSurcharge/CastSummon/OpenSummonCreaturePlacement. Checked fresh every time (not cached), so destroying a portal mid-match correctly brings the surcharge right back.</summary>
        private bool PlayerHasActiveGenericPortal(BaseUnit summoner) =>
            _units.Any(u => u is Portal && u.IsAlive && u.Owner == summoner);

        /// <summary>GenericPortalSummonManaSurcharge if `card` would need a fresh Generic Portal right now, else 0 - every SummonCard today is Generic (no non-Generic portal type exists yet to check `card` against), so this currently only depends on the player's own portal status.</summary>
        private int GetGenericPortalSurcharge(SummonCard card) =>
            PlayerHasActiveGenericPortal(_playerUnit) ? 0 : GenericPortalSummonManaSurcharge;

        /// <summary>
        /// Build the actual battlefield BaseUnit for a given SummonCard - one Units.Summons.*
        /// class per card, matching stats (see each class's own file). Null if card isn't a
        /// recognized SummonCard instance (shouldn't happen - every SummonCatalog entry has one).
        /// </summary>
        private BaseUnit CreateSummonUnit(SummonCard card, Vector2 position)
        {
            if (card == SummonCatalog.DuskRoachlin) return new DuskRoachlin(position);
            if (card == SummonCatalog.AetherfluffBeetle) return new AetherfluffBeetle(position);
            if (card == SummonCatalog.DuskRoachlinPriest) return new DuskRoachlinPriest(position);
            if (card == SummonCatalog.Bearat) return new Bearat(position);
            if (card == SummonCatalog.MirebackSlogger) return new MirebackSlogger(position);
            if (card == SummonCatalog.LanternmothCinderwing) return new LanternmothCinderwing(position);
            if (card == SummonCatalog.Brambleboar) return new Brambleboar(position);
            if (card == SummonCatalog.SiltfinMawpike) return new SiltfinMawpike(position);
            if (card == SummonCatalog.GloamravenOssuary) return new GloamravenOssuary(position);
            if (card == SummonCatalog.RootmossStonegloom) return new RootmossStonegloom(position);
            return null;
        }

        /// <summary>Chance, each Turn (full round - see CheckWildAnimalSpawn), that a wild beast spawns - first-pass placeholder pending a balance pass, same as every other number introduced "for now" in this project.</summary>
        private const float WildAnimalSpawnChancePerTurn = 0.3f;

        /// <summary>Hard cap on how many untamed Wild beasts can be on the field at once - without one, a long match would eventually overrun the arena with no way for anything to thin them back out (no despawn/aging system exists yet).</summary>
        private const int MaxWildAnimalsOnField = 3;

        /// <summary>Every "Beast"-type SummonCard (the 10-card catalog minus the 2 Roachlin ones) - the pool CheckWildAnimalSpawn draws from. Computed fresh each time rather than cached, since it's cheap and automatically stays correct if the catalog ever changes.</summary>
        private List<SummonCard> GetWildBeastCards() =>
            SummonCatalog.AllSummons.Where(c => c.UnitType == "Beast").ToList();

        /// <summary>
        /// Subscribed to TurnSystem.UnitTurnStarted in EnterOriginalMode only (never Test Mode -
        /// "the arena map" specifically) - gates itself to once per Turn (full round, not once
        /// per individual unit's turn) via _lastWildSpawnCheckTurn, rolls
        /// WildAnimalSpawnChancePerTurn, and if it hits, drops a random Beast-type creature
        /// (GetWildBeastCards) onto a random passable, unoccupied edge tile (PickRandomEdgeTile)
        /// as a free-standing Wild/Neutral unit - no Owner, so BaseUnit.Disposition's default
        /// (Neutral) applies, making it immediately a valid Tame Beast target. No-ops quietly
        /// (not an error) if the roll misses, the field's already at MaxWildAnimalsOnField, or no
        /// edge tile is currently free. `unit` (whoever's turn just started) is unused beyond
        /// being the event's required parameter - this isn't about who's acting, just when.
        /// </summary>
        private void CheckWildAnimalSpawn(BaseUnit unit)
        {
            if (_turnSystem.CurrentTurn == _lastWildSpawnCheckTurn)
                return;
            _lastWildSpawnCheckTurn = _turnSystem.CurrentTurn;

            if (_deckRandom.NextDouble() >= WildAnimalSpawnChancePerTurn)
                return;

            int currentWildCount = _units.Count(u => u.IsAlive && u.Owner == null && u.Factions.Contains(Faction.Wild));
            if (currentWildCount >= MaxWildAnimalsOnField)
                return;

            var edgeTile = PickRandomEdgeTile();
            if (edgeTile == null)
                return;

            var beastCards = GetWildBeastCards();
            var card = beastCards[_deckRandom.Next(beastCards.Count)];
            var position = _hexGrid.HexToWorld(edgeTile.Value.col, edgeTile.Value.row);
            var beast = CreateSummonUnit(card, position);
            beast.LoadContent(Content, GraphicsDevice);
            _units.Add(beast);

            ShowCombatMessage($"{beast.Name} wandered into the area.");
        }

        /// <summary>A random passable, unoccupied tile along the map's border (col 0/Width-1 or row 0/Height-1) - null if every edge tile is currently blocked or occupied. Shuffles the full edge-tile list once rather than retrying random picks, so it still finds the one free tile left even when the edges are nearly full.</summary>
        private (int col, int row)? PickRandomEdgeTile()
        {
            var edgeTiles = new List<(int col, int row)>();
            for (int col = 0; col < _hexGrid.Width; col++)
            {
                edgeTiles.Add((col, 0));
                edgeTiles.Add((col, _hexGrid.Height - 1));
            }
            for (int row = 0; row < _hexGrid.Height; row++)
            {
                edgeTiles.Add((0, row));
                edgeTiles.Add((_hexGrid.Width - 1, row));
            }

            var occupied = _units.Select(u => _hexGrid.WorldToHex(u.Position)).ToHashSet();
            var candidates = edgeTiles.Distinct()
                .Where(hex => Pathfinder.IsPassable(_hexGrid, _map, hex.col, hex.row, occupied))
                .ToList();

            return candidates.Count > 0 ? candidates[_deckRandom.Next(candidates.Count)] : null;
        }

        /// <summary>A random passable, unoccupied tile ANYWHERE on the map (not just the border - see PickRandomEdgeTile for that) - null if nothing qualifies, which shouldn't normally happen on a real map this size.</summary>
        private (int col, int row)? PickRandomMapTile()
        {
            var occupied = _units.Select(u => _hexGrid.WorldToHex(u.Position)).ToHashSet();
            var candidates = new List<(int col, int row)>();
            for (int col = 0; col < _hexGrid.Width; col++)
            {
                for (int row = 0; row < _hexGrid.Height; row++)
                {
                    if (Pathfinder.IsPassable(_hexGrid, _map, col, row, occupied))
                        candidates.Add((col, row));
                }
            }

            return candidates.Count > 0 ? candidates[_deckRandom.Next(candidates.Count)] : null;
        }

        /// <summary>Minimum/maximum wild beasts present at the very start of an arena match (inclusive, see EnterOriginalMode) - a one-time initial population, distinct from CheckWildAnimalSpawn's ongoing per-Turn edge trickle and NOT limited by its MaxWildAnimalsOnField cap (that cap is specifically about the ongoing spawn rate, not how many can ever be on the field at once).</summary>
        private const int InitialWildAnimalCountMin = 3;
        private const int InitialWildAnimalCountMax = 10;

        /// <summary>Places InitialWildAnimalCountMin-Max (inclusive) random Beast-type creatures on random passable, unoccupied tiles anywhere on the map - called once from EnterOriginalMode, after the 4 heroes are placed (so they're correctly treated as occupied) but before character select, so the arena is already populated the moment a match actually starts. Each pick re-reads _units fresh (via PickRandomMapTile), so placing one before picking the next naturally avoids stacking two beasts on the same tile.</summary>
        private void SpawnInitialWildAnimals()
        {
            int count = _deckRandom.Next(InitialWildAnimalCountMin, InitialWildAnimalCountMax + 1);
            var beastCards = GetWildBeastCards();
            int spawned = 0;

            for (int i = 0; i < count; i++)
            {
                var tile = PickRandomMapTile();
                if (tile == null)
                    break; // map's out of free space - stop rather than loop forever

                var card = beastCards[_deckRandom.Next(beastCards.Count)];
                var position = _hexGrid.HexToWorld(tile.Value.col, tile.Value.row);
                var beast = CreateSummonUnit(card, position);
                beast.LoadContent(Content, GraphicsDevice);
                _units.Add(beast);
                spawned++;
            }

            if (spawned > 0)
                ShowCombatMessage($"{spawned} wild {(spawned == 1 ? "beast" : "beasts")} already roam the arena.");
        }

        /// <summary>
        /// Dev-console "summon &lt;UnitID&gt; &lt;Team&gt;" handler (see DevConsole.SummonCallback).
        /// Validates the UnitID/Team, then opens tile-click placement
        /// (HandleDevSummonPlacementInput) - bypasses AP/MP/hand/range entirely, since this is a
        /// testing shortcut (e.g. spawning a Neutral creature to later test Tame Beast against),
        /// not the normal card-cast path (CastSummon). Returns the prompt/error line DevConsole
        /// prints back.
        /// </summary>
        private string DevSummonCommand(int unitId, string team)
        {
            var card = SummonCatalog.AllSummons.FirstOrDefault(c => c.Id == unitId);
            if (card == null)
                return $"Error: no summon card with ID {unitId}. Try 'list cards' to find one (SummonCard IDs start at 1001).";

            BaseUnit owner = null;
            string teamLabel = "Neutral";
            if (!team.Equals("Neutral", StringComparison.OrdinalIgnoreCase))
            {
                owner = _units.FirstOrDefault(u => u.Name.Equals(team, StringComparison.OrdinalIgnoreCase));
                if (owner == null)
                    return $"Error: no unit named '{team}' on the field. Use 'Neutral' or an exact unit Name.";
                teamLabel = owner.Name;
            }

            _pendingDevSummonCard = card;
            _pendingDevSummonOwner = owner;
            _devSummonModeActive = true;
            _consoleOpen = false;

            return $"Click a tile to place {card.Name} ({teamLabel}) - E to cancel.";
        }

        /// <summary>Handle input while picking a dev-summoned creature's placement tile: click any passable, unoccupied tile anywhere on the map to spawn it there, E to cancel.</summary>
        private void HandleDevSummonPlacementInput(KeyboardState keyboardState, MouseState mouseState)
        {
            if (keyboardState.IsKeyDown(Keys.E) && !_previousKeyboardState.IsKeyDown(Keys.E))
            {
                _devSummonModeActive = false;
                _pendingDevSummonCard = null;
                _pendingDevSummonOwner = null;
                return;
            }

            if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
            {
                Vector2 viewportSize = new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
                var clickedHex = _renderer.GetHexAtScreenPos(mouseState.X, mouseState.Y, viewportSize);
                if (!_hexGrid.IsInBounds(clickedHex.col, clickedHex.row))
                    return;

                var occupied = _units.Select(u => _hexGrid.WorldToHex(u.Position)).ToHashSet();
                if (!Pathfinder.IsPassable(_hexGrid, _map, clickedHex.col, clickedHex.row, occupied))
                    return;

                ExecuteDevSummon(clickedHex);
            }
        }

        /// <summary>Spawn _pendingDevSummonCard at destHex, owned by _pendingDevSummonOwner (null = Neutral) - no AP/MP/card consumed, this is a dev-only shortcut (see DevSummonCommand).</summary>
        private void ExecuteDevSummon((int col, int row) destHex)
        {
            var position = _hexGrid.HexToWorld(destHex.col, destHex.row);
            var summon = CreateSummonUnit(_pendingDevSummonCard, position);
            summon.Owner = _pendingDevSummonOwner;
            summon.LoadContent(Content, GraphicsDevice);
            _units.Add(summon);

            string teamLabel = _pendingDevSummonOwner != null ? _pendingDevSummonOwner.Name : "Neutral";
            ShowCombatMessage($"Dev-summoned {summon.Name} ({teamLabel}) at ({destHex.col}, {destHex.row}).");

            _devSummonModeActive = false;
            _pendingDevSummonCard = null;
            _pendingDevSummonOwner = null;
        }

        /// <summary>
        /// Cast a SummonCard: pays SummonAPCost + the card's own ManaCost (plus
        /// GenericPortalSummonManaSurcharge on top if the caster has no Generic Portal standing
        /// yet) up front (same afford-first convention as every other card/move). If a portal's
        /// needed, opens Phase 1 (portal placement - HandlePortalPlacementInput) first; otherwise
        /// goes straight to Phase 2 (OpenSummonCreaturePlacement) for the creature itself.
        /// </summary>
        private void CastSummon(SummonCard card)
        {
            bool needsPortal = !PlayerHasActiveGenericPortal(_playerUnit);
            int totalManaCost = card.ManaCost + (needsPortal ? GenericPortalSummonManaSurcharge : 0);

            if (SummonAPCost > _playerUnit.CurrentAP || totalManaCost > _playerUnit.CurrentMP)
            {
                ShowCombatMessage($"Not enough AP/MP to summon {card.Name}.");
                return;
            }

            _pendingSummonCard = card;
            _cardMenuActive = false;

            if (needsPortal)
            {
                var casterHex = _hexGrid.WorldToHex(_playerUnit.Position);
                var occupied = GetDestinationBlockingTiles(_playerUnit);
                _portalPlacementValidTiles = _hexGrid.GetHexesInRadius(casterHex.col, casterHex.row, GenericPortalPlacementRange)
                    .Where(hex => hex != casterHex && Pathfinder.IsPassable(_hexGrid, _map, hex.col, hex.row, occupied))
                    .ToHashSet();

                if (_portalPlacementValidTiles.Count == 0)
                {
                    ShowCombatMessage($"Nowhere to place a portal for {card.Name}.");
                    _pendingSummonCard = null;
                    _cardMenuActive = true;
                    return;
                }

                _portalPlacementModeActive = true;
            }
            else
            {
                OpenSummonCreaturePlacement();
            }
        }

        /// <summary>Handle input while picking a Generic Portal's placement tile (Phase 1 - see CastSummon): click a highlighted tile to conjure it there, E to cancel the WHOLE summon (nothing's been spent yet at this point).</summary>
        private void HandlePortalPlacementInput(KeyboardState keyboardState, MouseState mouseState)
        {
            if (keyboardState.IsKeyDown(Keys.E) && !_previousKeyboardState.IsKeyDown(Keys.E))
            {
                CancelSummonPlacement();
                return;
            }

            if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
            {
                Vector2 viewportSize = new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
                var clickedHex = _renderer.GetHexAtScreenPos(mouseState.X, mouseState.Y, viewportSize);
                if (_portalPlacementValidTiles.Contains(clickedHex))
                    ExecutePortalPlacement(clickedHex);
            }
        }

        /// <summary>
        /// Spend GenericPortalSummonManaSurcharge, conjure a Portal at destHex owned by the
        /// caster, add it to the battlefield (it never takes a turn of its own - see
        /// BaseUnit.IsRelic), then immediately open Phase 2 (OpenSummonCreaturePlacement) for
        /// the creature itself - the caster may still cancel THAT step and keep only the portal
        /// (see HandleSummonPlacementInput's E handling; the card stays in hand uncast either
        /// way, since RemoveCardFromHand only runs from ExecuteSummon).
        /// </summary>
        private void ExecutePortalPlacement((int col, int row) destHex)
        {
            _playerUnit.CurrentMP = Math.Max(0, _playerUnit.CurrentMP - GenericPortalSummonManaSurcharge);

            var position = _hexGrid.HexToWorld(destHex.col, destHex.row);
            var portal = new Portal("Generic Portal", GenericPortalHP, GenericPortalDefense, GenericPortalResistance, position);
            portal.Owner = _playerUnit;
            portal.LoadContent(Content, GraphicsDevice);
            _units.Add(portal);

            ShowCombatMessage($"{_playerUnit.Name} conjures a Generic Portal.");

            _portalPlacementModeActive = false;
            _portalPlacementValidTiles.Clear();

            OpenSummonCreaturePlacement();
        }

        /// <summary>
        /// Phase 2 of CastSummon: open placement targeting for the creature itself - every
        /// passable, unoccupied tile within SummonPlacementRange of the caster's Generic Portal
        /// (just conjured in Phase 1, or already standing from an earlier summon), same mechanics
        /// as teleport targeting (CastBlinkSpell) just for picking where the new unit appears.
        /// Falls back to the caster's own position as the anchor in the (not normally reachable)
        /// case of no portal at all, rather than crashing.
        /// </summary>
        private void OpenSummonCreaturePlacement()
        {
            var portal = _units.OfType<Portal>().FirstOrDefault(p => p.IsAlive && p.Owner == _playerUnit);
            var anchorHex = portal != null ? _hexGrid.WorldToHex(portal.Position) : _hexGrid.WorldToHex(_playerUnit.Position);

            // GetDestinationBlockingTiles(_playerUnit) deliberately leaves the caster's OWN tile
            // out (it's meant for placing something at a specific DESTINATION, where excluding
            // the mover's own tile is correct) - every other caller that places something at a
            // specific destination (the old single-step summon placement, CastBlinkSpell's
            // teleport targeting) always paired it with an explicit `hex != casterHex` for
            // exactly this reason. Anchoring on the portal instead of the caster here dropped
            // that guard, which is what let a creature be placed directly on top of the summoner
            // whenever they stood within 1 tile of their own portal.
            var casterHex = _hexGrid.WorldToHex(_playerUnit.Position);
            var occupied = GetDestinationBlockingTiles(_playerUnit);
            _summonPlacementValidTiles = _hexGrid.GetHexesInRadius(anchorHex.col, anchorHex.row, SummonPlacementRange)
                .Where(hex => hex != anchorHex && hex != casterHex && Pathfinder.IsPassable(_hexGrid, _map, hex.col, hex.row, occupied))
                .ToHashSet();

            if (_summonPlacementValidTiles.Count == 0)
            {
                ShowCombatMessage($"Nowhere to place {_pendingSummonCard.Name}.");
                CancelSummonPlacement(returnToCardMenu: false);
                return;
            }

            _summonPlacementModeActive = true;
        }

        /// <summary>Handle input while picking a summon's placement tile (Phase 2): click a highlighted tile to spawn it there, E to cancel - if a portal was already conjured this cast, it's kept (only the creature is cancelled).</summary>
        private void HandleSummonPlacementInput(KeyboardState keyboardState, MouseState mouseState)
        {
            if (keyboardState.IsKeyDown(Keys.E) && !_previousKeyboardState.IsKeyDown(Keys.E))
            {
                CancelSummonPlacement();
                return;
            }

            if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
            {
                Vector2 viewportSize = new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
                var clickedHex = _renderer.GetHexAtScreenPos(mouseState.X, mouseState.Y, viewportSize);
                if (_summonPlacementValidTiles.Contains(clickedHex))
                    ExecuteSummon(clickedHex);
            }
        }

        /// <summary>
        /// Spend SummonAPCost/the card's own MP (the portal's surcharge, if any, was already
        /// spent in ExecutePortalPlacement), spawn the creature at destHex owned by the caster
        /// (see BaseUnit.Owner - keeps it off the caster's own valid-attack-target lists and
        /// counted as the caster's team for the FFA win check), load its sprite (a no-op if it
        /// has none yet), add it to the battlefield, consume the card, and return to the turn
        /// menu. It gets a turn THIS SAME Turn - spliced into the already-built turn order via
        /// TurnSystem.AddUnitMidTurn, landing at the very end (see Summoning Curse below) - not
        /// held off until the next one.
        /// </summary>
        private void ExecuteSummon((int col, int row) destHex)
        {
            _playerUnit.SpendAP(SummonAPCost);
            _playerUnit.CurrentMP = Math.Max(0, _playerUnit.CurrentMP - _pendingSummonCard.ManaCost);

            var position = _hexGrid.HexToWorld(destHex.col, destHex.row);
            var summon = CreateSummonUnit(_pendingSummonCard, position);
            summon.Owner = _playerUnit;
            summon.LoadContent(Content, GraphicsDevice);

            // Summoning Curse: forces it to act after every non-cursed unit THIS Turn regardless
            // of its own Speed (see TurnSystem.AddUnitMidTurn, called below), speed only
            // mattering against other units summoned this same Turn - cleared automatically the
            // moment its own first turn starts (TurnSystem.OnUnitTurnStart), so EffectiveSpeed
            // applies normally starting next Turn.
            summon.ApplyStatusEffect(new StatusEffect { Name = "Summoning Curse", Type = StatusEffectType.Affliction });

            _units.Add(summon);
            _turnSystem.AddUnitMidTurn(summon);

            ShowCombatMessage($"{_playerUnit.Name} summons a {summon.Name}.");
            RemoveCardFromHand(_pendingSummonCard);

            CancelSummonPlacement(returnToCardMenu: false);
        }

        private void CancelSummonPlacement(bool returnToCardMenu = true)
        {
            _summonPlacementModeActive = false;
            _portalPlacementModeActive = false;
            _pendingSummonCard = null;
            _summonPlacementValidTiles.Clear();
            _portalPlacementValidTiles.Clear();

            if (returnToCardMenu)
            {
                _cardMenuActive = true;
            }
            else
            {
                _turnMenuIndex = 0;
                _turnMenuActive = true;
            }
        }

        /// <summary>
        /// AI's own version of the player's two-phase CastSummon/ExecutePortalPlacement/
        /// ExecuteSummon flow - no UI/targeting-mode state machine involved (there's no player to
        /// click a tile), so this resolves everything in one synchronous call instead. Tries each
        /// SummonCard in `aiUnit`'s hand in order until one actually goes through (affordable AND
        /// a valid tile exists for the portal, if one's needed, and the creature) - not
        /// necessarily the cheapest or strongest, same "no real tactical depth yet" placeholder
        /// bar every other AI decision in this game already uses. Portal/creature tiles are
        /// whichever valid one is found FIRST, not picked for any tactical reason. Returns true
        /// if a summon actually happened.
        /// </summary>
        private bool TryAiSummon(BaseUnit aiUnit)
        {
            var hand = EnsureHandInitialized(aiUnit.Class);
            var summonCards = hand.OfType<SummonCard>().ToList();
            if (summonCards.Count == 0)
                return false;

            var casterHex = _hexGrid.WorldToHex(aiUnit.Position);
            var destinationBlocking = GetDestinationBlockingTiles(aiUnit);

            foreach (var card in summonCards)
            {
                // Leveling System (first pass): same Level-vs-ManaCost gate TryCastSelectedCard
                // applies to the player - an AI caster can't summon anything whose ManaCost
                // exceeds its own Level either. Checked against the card's own ManaCost, not
                // totalManaCost below - the Generic Portal surcharge is a one-time bootstrap
                // cost, not part of the card's own "how strong a summon is this" price tag.
                if (card.ManaCost > aiUnit.Level)
                    continue;

                var existingPortal = _units.OfType<Portal>().FirstOrDefault(p => p.IsAlive && p.Owner == aiUnit);
                bool needsPortal = existingPortal == null;
                int totalManaCost = card.ManaCost + (needsPortal ? GenericPortalSummonManaSurcharge : 0);
                if (SummonAPCost > aiUnit.CurrentAP || totalManaCost > aiUnit.CurrentMP)
                    continue;

                var portal = existingPortal;
                if (needsPortal)
                {
                    var portalTiles = _hexGrid.GetHexesInRadius(casterHex.col, casterHex.row, GenericPortalPlacementRange)
                        .Where(hex => hex != casterHex && Pathfinder.IsPassable(_hexGrid, _map, hex.col, hex.row, destinationBlocking))
                        .ToList();
                    if (portalTiles.Count == 0)
                        continue; // nowhere to conjure a portal - try the next card instead

                    aiUnit.CurrentMP = Math.Max(0, aiUnit.CurrentMP - GenericPortalSummonManaSurcharge);
                    var portalPos = _hexGrid.HexToWorld(portalTiles[0].col, portalTiles[0].row);
                    portal = new Portal("Generic Portal", GenericPortalHP, GenericPortalDefense, GenericPortalResistance, portalPos);
                    portal.Owner = aiUnit;
                    portal.LoadContent(Content, GraphicsDevice);
                    _units.Add(portal);
                    ShowCombatMessage($"{aiUnit.Name} conjures a Generic Portal.");
                }

                var anchorHex = _hexGrid.WorldToHex(portal.Position);
                var creatureTiles = _hexGrid.GetHexesInRadius(anchorHex.col, anchorHex.row, SummonPlacementRange)
                    .Where(hex => hex != anchorHex && hex != casterHex && Pathfinder.IsPassable(_hexGrid, _map, hex.col, hex.row, destinationBlocking))
                    .ToList();
                if (creatureTiles.Count == 0)
                    continue; // portal placed (if it needed to be) but nowhere for the creature itself - try another card

                aiUnit.SpendAP(SummonAPCost);
                aiUnit.CurrentMP = Math.Max(0, aiUnit.CurrentMP - card.ManaCost);

                var summonPos = _hexGrid.HexToWorld(creatureTiles[0].col, creatureTiles[0].row);
                var summon = CreateSummonUnit(card, summonPos);
                summon.Owner = aiUnit;
                summon.LoadContent(Content, GraphicsDevice);
                summon.ApplyStatusEffect(new StatusEffect { Name = "Summoning Curse", Type = StatusEffectType.Affliction });
                _units.Add(summon);
                _turnSystem.AddUnitMidTurn(summon);

                ShowCombatMessage($"{aiUnit.Name} summons a {summon.Name}.");
                DiscardCard(aiUnit.Class, hand, card); // NOT RemoveCardFromHand - that's hardcoded to _playerUnit's own hand/UI state, wrong unit here

                return true;
            }

            return false;
        }

        /// <summary>
        /// Attempt to cast whichever card is currently selected in the hand (_handCardIndex) -
        /// an IsBlink spell opens teleport targeting (CastBlinkSpell), an IsManaShield spell
        /// grants/toggles a shield (CastManaShield), an IsAllyShield spell opens ally-targeting
        /// (CastAllyShield), an IsSleepSpell puts the caster into Deep Sleep (CastSleepSpell), an
        /// IsMeditateSpell starts (or, if already meditating, ends and locks in) Meditate
        /// (CastMeditateSpell), a damage-dealing Spell Card goes through CastSpellAsAttack for
        /// real, a self/ally utility spell (Brace/Rally Cry/Steady Hands/Second Wind) applies
        /// instantly (CastSelfUtilitySpell), a SummonCard opens placement targeting (CastSummon);
        /// everything else (heals and other utility spells with no execution system yet) just
        /// reports that casting isn't
        /// implemented for it yet, same honest treatment as an unaffordable move rather than a
        /// silent no-op.
        /// </summary>
        private void TryCastSelectedCard()
        {
            if (_availableHandCards.Count == 0)
                return;

            // Cast whichever card is actually shown big/centered right now (see
            // GetFocusedHandCardIndex) - a mouse-hovered side card counts as "selected" too,
            // not just whatever A/D or a click last set _handCardIndex to, so the card Space
            // casts always matches the card the player is looking at.
            var viewportSize = new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            _handCardIndex = GetFocusedHandCardIndex(viewportSize, Mouse.GetState().Position) ?? _handCardIndex;
            _highlightedHandCardIndex = _handCardIndex;

            object card = _availableHandCards[_handCardIndex];

            // Leveling System (first pass): a caster can't cast anything whose MP cost exceeds
            // their own Level - checked once here, ahead of every dispatch branch below, so it
            // uniformly gates every SpellCard (Effect.MPCost) and SummonCard (ManaCost) the same
            // way regardless of which Cast* method would otherwise handle it. This is on top of,
            // not instead of, the normal MP-affordability check each Cast* method already does.
            (string Name, int Cost) gatedCard = card switch
            {
                SpellCard sc => (sc.Name, sc.Effect.MPCost),
                SummonCard smc => (smc.Name, smc.ManaCost),
                _ => (null, 0)
            };
            if (gatedCard.Cost > _playerUnit.Level)
            {
                ShowCombatMessage($"{gatedCard.Name} requires Level {gatedCard.Cost} - {_playerUnit.Name} is only Level {_playerUnit.Level}.");
                return;
            }

            if (card is SpellCard spellCard)
            {
                if (spellCard.Effect.IsBlink)
                    CastBlinkSpell(spellCard);
                else if (spellCard.Effect.IsAllyShield)
                    CastAllyShield(spellCard);
                else if (spellCard.Effect.IsTameBeast)
                    CastTameBeast(spellCard);
                else if (spellCard.Effect.IsPlaceTrap)
                    CastPlaceTrap(spellCard);
                else if (spellCard.Effect.IsReshuffle)
                    CastReshuffle(spellCard);
                else if (SpellCastsAsAttack(spellCard))
                    CastSpellAsAttack(spellCard);
                else if (SpellCaster.CanResolveInstantly(spellCard.Effect))
                    ResolveInstantCast(spellCard);
                else
                    ShowCombatMessage($"{spellCard.Name} can't be cast yet - no execution system for that effect.");
            }
            else if (card is SummonCard summonCard)
            {
                CastSummon(summonCard);
            }
        }

        /// <summary>
        /// Cast a SpellCard whose entire effect SpellCaster.Cast can resolve on its own - Mana
        /// Shield, Deep Sleep, Meditate, and the generic self/ally utility spells (Brace, Rally
        /// Cry, Steady Hands, Second Wind). Game1's only job here is gathering the one piece of
        /// UI-side context that logic can't reach on its own (which adjacent units are actually
        /// this caster's allies, for a HitsAllAdjacent spell like Rally Cry) and then reflecting
        /// the result back into the UI - showing the message, removing the card from hand unless
        /// told not to (SpellCaster.CastResult.ConsumesCard - Mana Shield/Meditate's own
        /// toggle-off, and Meditate's "stays in hand to serve as its own off switch" start, both
        /// say no), and returning to the turn menu on success. No AP/MP/stat-effect logic lives
        /// in Game1 for any of this.
        /// </summary>
        private void ResolveInstantCast(SpellCard card)
        {
            var allies = card.Effect.HitsAllAdjacent ? GetAdjacentAllies(_playerUnit) : new List<BaseUnit>();
            var result = SpellCaster.Cast(card, _playerUnit, allies);

            ShowCombatMessage(result.Message);
            if (!result.Success)
                return;

            if (result.ConsumesCard)
                RemoveCardFromHand(card);

            _cardMenuActive = false;
            _turnMenuIndex = 0;
            _turnMenuActive = true;
        }

        /// <summary>Every living unit on `unit`'s own team (see GetTeamRoot), adjacent to it, excluding itself - same adjacency AttackResolver.GetAdjacentTargets uses for an enemy-facing HitsAllAdjacent move, just filtered to allies instead.</summary>
        private List<BaseUnit> GetAdjacentAllies(BaseUnit unit)
        {
            var hex = _hexGrid.WorldToHex(unit.Position);
            var adjacentHexes = _hexGrid.GetNeighbors(hex.col, hex.row).ToHashSet();
            var team = GetTeamRoot(unit);
            return _units.Where(u => u != unit && u.IsAlive && GetTeamRoot(u) == team
                    && adjacentHexes.Contains(_hexGrid.WorldToHex(u.Position)))
                .ToList();
        }

        /// <summary>
        /// Resolve _pendingMove against the given target (null for a HitsAllAdjacent move, which
        /// ignores it) or aimDirection (for a HitsCone move) via AttackResolver, log the
        /// outcome, and return to the turn menu.
        /// </summary>
        private void ExecutePendingAttack(BaseUnit target, HexDirection? aimDirection = null)
        {
            var outcomes = AttackResolver.Resolve(_playerUnit, target, _pendingMove, _pendingSourceWeapon, _hexGrid, _map, _units, aimDirection);
            SpawnAttackAnimations(_playerUnit, _pendingMove, outcomes);
            ShowCombatMessage(BuildCombatLogMessage(_playerUnit, _pendingMove, outcomes));

            // A spell cast successfully - consume it from the hand, same as any card play.
            if (_pendingSpellCard != null)
                RemoveCardFromHand(_pendingSpellCard);

            _targetingModeActive = false;
            _coneAimingModeActive = false;
            _attackMenuActive = false;
            _pendingMove = null;
            _pendingSourceWeapon = null;
            _pendingSpellCard = null;
            _targetingFromCardMenu = false;
            _targetCandidates.Clear();

            _turnMenuIndex = 0;
            _turnMenuActive = true;
        }

        /// <summary>
        /// Queue a simple visual effect for each target `move` was just used against - a Slash
        /// (melee) flashes near the target, a Shooting move animates a projectile traveling
        /// from the attacker to the target. Purely presentational - runs regardless of hit/miss,
        /// so a whiffed swing/shot still visibly happens.
        /// </summary>
        /// <summary>A floating "-N" number that rises and fades above target over 1 second (see ActiveDamageNumber/Draw's rendering of it) - currently only spawned from TriggerTrap, since a normal weapon/spell hit already gets equivalent feedback from its own Slash/Shooting animation plus the combat log, where a trap's trigger has neither.</summary>
        private void SpawnDamageNumber(BaseUnit target, int amount)
        {
            _activeDamageNumbers.Add(new ActiveDamageNumber { Target = target, Amount = amount, Duration = 1f });
        }

        private void SpawnAttackAnimations(BaseUnit attacker, Move move, List<AttackOutcome> outcomes)
        {
            Vector2 from = attacker.Position;
            var fromHex = _hexGrid.WorldToHex(from);

            foreach (var outcome in outcomes)
            {
                if (outcome.Target == null)
                    continue;

                float travelDuration;
                float holdDuration;
                if (move.AnimationType == AttackAnimationType.Shooting)
                {
                    var toHex = _hexGrid.WorldToHex(outcome.Target.Position);
                    int distanceTiles = _hexGrid.GetDistance(fromHex.col, fromHex.row, toHex.col, toHex.row);
                    // Slower than the old 0.15 + 0.03/tile - the Update loop now actually pauses
                    // the game on this (see the ShowingTurnAnnouncement gate and Draw's turn-menu
                    // check), so it needs to be slow enough to actually SEE the shot travel
                    // rather than a near-instant flash, for Arcane Missile in particular. Plus a
                    // brief hold once it lands (see TravelDuration/Duration on ActiveAttackEffect)
                    // so a short hop between two adjacent units - the common case on Test Mode's
                    // 3x3 map - still gets a clearly visible pause, not just a flicker.
                    travelDuration = 0.4f + 0.06f * distanceTiles;
                    holdDuration = 0.25f;
                }
                else
                {
                    // Was 0.25s/0f - long enough to resolve but too short to actually register
                    // before the turn menu reopens (see the Update pause gate's
                    // _activeAttackEffects.Count check), which also squeezed the hit-reaction
                    // flash on the target into an almost-invisible sliver.
                    travelDuration = 0.35f;
                    holdDuration = 0.1f;
                }

                // A magical bolt (e.g. Arcane Missile, Frost Blast) reads as a glowing
                // purple/blue orb instead of the plain yellow every physical projectile
                // (arrow/bolt/bullet) uses - simple, but enough to tell them apart at a glance.
                Color projectileColor = move.DamageType == DamageType.Magical
                    ? new Color(170, 100, 255)
                    : Color.Yellow;

                _activeAttackEffects.Add(new ActiveAttackEffect
                {
                    Type = move.AnimationType,
                    From = from,
                    To = outcome.Target.Position,
                    Elapsed = 0f,
                    Duration = travelDuration + holdDuration,
                    TravelDuration = travelDuration,
                    ProjectileColor = projectileColor,
                });

                // Only an actual hit gets a reaction - a miss shouldn't flinch. Shooting waits
                // out the projectile's own travel time first, so the flash/nudge lands with the
                // impact instead of the instant the spell/shot is cast.
                if (outcome.Hit)
                {
                    Vector2 nudgeDirection = outcome.Target.Position - from;
                    nudgeDirection = nudgeDirection.LengthSquared() > 0.001f
                        ? Vector2.Normalize(nudgeDirection)
                        : Vector2.UnitX;

                    float hitReactionDelay = move.AnimationType == AttackAnimationType.Shooting ? travelDuration : 0f;

                    _activeHitReactions.Add(new ActiveHitReaction
                    {
                        Target = outcome.Target,
                        Elapsed = 0f,
                        StartDelay = hitReactionDelay,
                        Duration = HitReactionDuration,
                        NudgeDirection = nudgeDirection,
                    });
                }
            }
        }

        private string BuildCombatLogMessage(BaseUnit attacker, Move move, List<AttackOutcome> outcomes)
        {
            if (outcomes.Count == 0)
                return $"{attacker.Name} used {move.Name}, but there was nothing to hit.";

            var parts = outcomes.Select(o =>
            {
                if (!o.Hit)
                    return $"missed {o.Target.Name}";

                string crit = o.Crit ? " (crit!)" : "";
                string status = o.StatusApplied != null ? $", inflicting {o.StatusApplied}" : "";
                string fainted = o.TargetFainted ? " - fainted!" : "";
                string levelUp = o.LeveledUpUnit != null ? $" - {o.LeveledUpUnit.Name} reaches Level {o.LeveledUpUnit.Level}!" : "";
                return $"hit {o.Target.Name} for {o.Damage}{crit}{status}{fainted}{levelUp}";
            });

            return $"{attacker.Name} used {move.Name}: {string.Join("; ", parts)}";
        }

        /// <summary>Show a short-lived line at the bottom of the screen - both the actual combat log and "why didn't that work" feedback (unaffordable AP/MP, out of ammo, nothing in range) go through here so nothing is ever a silent no-op. Also mirrored to the terminal log (see GameLogger) - attacks, casts, equips, failures, all of it - so the full match history is readable after the fact, not just whatever's currently faded on screen.</summary>
        private void ShowCombatMessage(string message)
        {
            _combatLogMessage = message;
            _combatLogTimer = 4f;
            GameLogger.Log(message);
            _console.Log(message);
            _combatMessagesThisUnitTurn++;
        }

        /// <summary>
        /// How many ShowCombatMessage calls have happened since the CURRENT unit's turn started -
        /// reset to 0 by ResetCombatMessageCount, subscribed to TurnSystem.UnitTurnStarted right
        /// alongside LogUnitTurnDivider/LogUnitAggro/ApplyManaAuras, so it's accurate for every
        /// unit's turn, player or AI, batched or not. Checked right after RunSimpleAI returns (see
        /// its two call sites in Update) to guarantee a visible "X does nothing this turn." log
        /// line whenever an AI turn would otherwise produce NO log output at all (on request -
        /// AI Turn Batching can make several units act within the same frame, with nothing shown
        /// on screen beyond "Other units are taking their turns..." - without this, a silently
        /// idle unit's entire turn left no trace anywhere, including the terminal log, which is
        /// exactly the trail needed to diagnose where a stuck/hung turn sequence actually stopped).
        /// </summary>
        private int _combatMessagesThisUnitTurn = 0;

        private void ResetCombatMessageCount(BaseUnit unit) => _combatMessagesThisUnitTurn = 0;

        /// <summary>Called right after RunSimpleAI(aiUnit) returns, at BOTH of its call sites (batched and individually-presented AI turns) - see _combatMessagesThisUnitTurn's own doc comment for why this exists.</summary>
        private void LogIfAiTurnWasSilent(BaseUnit aiUnit)
        {
            if (_combatMessagesThisUnitTurn == 0)
                ShowCombatMessage($"{aiUnit.Name} does nothing this turn.");
        }

        /// <summary>Handle input while picking a target for _pendingMove: click a highlighted unit to attack it, E to cancel back to the attack submenu.</summary>
        private void HandleTargetingInput(KeyboardState keyboardState, MouseState mouseState)
        {
            if (keyboardState.IsKeyDown(Keys.E) && !_previousKeyboardState.IsKeyDown(Keys.E))
            {
                CancelTargeting();
                return;
            }

            if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
            {
                Vector2 viewportSize = new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
                var clickedHex = _renderer.GetHexAtScreenPos(mouseState.X, mouseState.Y, viewportSize);
                var target = _targetCandidates.FirstOrDefault(u => _hexGrid.WorldToHex(u.Position) == clickedHex);
                if (target != null)
                    ExecutePendingAttack(target);
            }
        }

        private void CancelTargeting()
        {
            _targetingModeActive = false;
            _pendingMove = null;
            _pendingSourceWeapon = null;
            _pendingSpellCard = null;
            _targetCandidates.Clear();

            if (_targetingFromCardMenu)
            {
                _targetingFromCardMenu = false;
                _cardMenuActive = true;
            }
            else
            {
                _attackMenuIndex = 0;
                _attackMenuActive = true;
            }
        }

        /// <summary>Handle input while aiming _pendingMove's cone: click any hex to set the direction and fire, E to cancel back to the attack submenu.</summary>
        private void HandleConeAimingInput(KeyboardState keyboardState, MouseState mouseState)
        {
            if (keyboardState.IsKeyDown(Keys.E) && !_previousKeyboardState.IsKeyDown(Keys.E))
            {
                CancelConeAiming();
                return;
            }

            if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
            {
                Vector2 viewportSize = new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
                var clickedHex = _renderer.GetHexAtScreenPos(mouseState.X, mouseState.Y, viewportSize);
                var attackerHex = _hexGrid.WorldToHex(_playerUnit.Position);
                if (clickedHex == attackerHex)
                    return; // aiming at your own tile doesn't define a direction

                var direction = _hexGrid.GetDirectionTo(attackerHex.col, attackerHex.row, clickedHex.col, clickedHex.row);
                ExecutePendingAttack(null, direction);
            }
        }

        private void CancelConeAiming()
        {
            _coneAimingModeActive = false;
            _pendingMove = null;
            _pendingSourceWeapon = null;
            _attackMenuIndex = 0;
            _attackMenuActive = true;
        }

        /// <summary>
        /// Snap the camera back onto the player's unit and reopen the turn menu.
        /// </summary>
        private void ResumeFromMapView()
        {
            _viewingMap = false;
            _renderer.CameraPosition = _playerUnit.Position;
            _renderer.ZoomLevel = _cameraZoomTarget;
            _turnMenuIndex = 0;
            _turnMenuActive = true;
        }

        /// <summary>
        /// Compute the turn menu's option rectangles, anchored just off the player unit's
        /// screen position and clamped so the menu stays fully onscreen.
        /// </summary>
        private Rectangle[] GetTurnMenuOptionRects(Vector2 viewportSize) =>
            GetMenuOptionRects(viewportSize, GetTurnMenuOptions().Count);

        /// <summary>
        /// Compute a vertical menu's option rectangles, anchored just off the player unit's
        /// screen position and clamped so the menu stays fully onscreen. Shared by the turn
        /// menu and the attack submenu.
        /// </summary>
        private Rectangle[] GetMenuOptionRects(Vector2 viewportSize, int optionCount)
        {
            const int optionHeight = 28;
            const int menuWidth = 160;

            Vector2 anchor = _renderer.WorldToScreen(_playerUnit.Position, viewportSize) + new Vector2(40, -60);
            anchor.X = MathHelper.Clamp(anchor.X, 10, viewportSize.X - menuWidth - 10);
            anchor.Y = MathHelper.Clamp(anchor.Y, 10, viewportSize.Y - optionCount * optionHeight - 10);

            var rects = new Rectangle[optionCount];
            for (int i = 0; i < optionCount; i++)
                rects[i] = new Rectangle((int)anchor.X, (int)anchor.Y + i * optionHeight, menuWidth, optionHeight);
            return rects;
        }

        /// <summary>Same row-of-options layout as GetMenuOptionRects, but anchored to a fixed bottom-right screen position instead of the player's (moving) world position - used for the item-detail action list (Equip/Unequip/Use, Discard, Back) specifically, so it can never collide with that view's own info panel (DrawItemDetail), which is anchored top-left and can grow tall with a long weapon description.</summary>
        private Rectangle[] GetFixedBottomRightMenuOptionRects(Vector2 viewportSize, int optionCount)
        {
            const int optionHeight = 28;
            const int menuWidth = 200;

            Vector2 anchor = new Vector2(viewportSize.X - menuWidth - 20, viewportSize.Y - optionCount * optionHeight - 20);

            var rects = new Rectangle[optionCount];
            for (int i = 0; i < optionCount; i++)
                rects[i] = new Rectangle((int)anchor.X, (int)anchor.Y + i * optionHeight, menuWidth, optionHeight);
            return rects;
        }

        /// <summary>
        /// True only while free-looking the map (_viewingMap) - the unit detail box is a View
        /// Map-only inspection tool. Originally this excluded every other click-driven mode
        /// individually (movement/targeting/cone/teleport/ally-target/summon-placement/items/
        /// cards/console) instead, but that still let it pop up over an attack: HandleTargetingInput
        /// runs BEFORE HandleMapControls each Update(), so clicking a target resolved the attack
        /// AND cleared _targetingModeActive back to false in the same frame, before
        /// HandleMapControls' own click check ran - by then the exclusion list no longer excluded
        /// anything, so that same click re-read as "click a unit, open its box." Gating on
        /// _viewingMap specifically sidesteps that ordering issue entirely, since nothing about
        /// resolving an attack or casting a spell ever touches that flag.
        /// </summary>
        private bool CanShowUnitDetailBox => _viewingMap;

        private void HandleMapControls(KeyboardState keyboardState, MouseState mouseState, float deltaTime)
        {
            // Camera pan (WASD) - suppressed while the turn menu or attack submenu is open,
            // since W/S there navigate the menu instead (Move mode doesn't use W/S for
            // anything, so panning stays available there - see OpenMovementMode for its
            // one-time entry snap). Scaled by deltaTime (not a flat per-frame step) so it's
            // smooth and frame-rate-independent instead of speeding up or stuttering with the
            // frame rate - this was the actual cause of View Map feeling laggy/jittery.
            if (!_turnMenuActive && !_attackMenuActive && !_cardMenuActive && !_itemsMenuActive && !_itemDetailActive)
            {
                float panSpeed = 400f; // pixels/second
                if (keyboardState.IsKeyDown(Keys.W))
                    _renderer.PanCamera(-Vector2.UnitY * panSpeed * deltaTime);  // W = up (negative Y)
                if (keyboardState.IsKeyDown(Keys.S))
                    _renderer.PanCamera(Vector2.UnitY * panSpeed * deltaTime);   // S = down (positive Y)
                if (keyboardState.IsKeyDown(Keys.A))
                    _renderer.PanCamera(-Vector2.UnitX * panSpeed * deltaTime);  // A = left (negative X)
                if (keyboardState.IsKeyDown(Keys.D))
                    _renderer.PanCamera(Vector2.UnitX * panSpeed * deltaTime);   // D = right (positive X)
            }

            // Zoom (mouse wheel) - _cameraZoomTarget has to move WITH ZoomLevel here, not just
            // ZoomLevel alone: once the camera's initial swoop-in settles, Update's own
            // camera-follow block locks ZoomLevel to _cameraZoomTarget every single frame (so the
            // zoom stays put while a unit walks mid-turn) - without updating the target too, that
            // lock would snap the zoom right back to its old value the very next frame, making
            // the scroll wheel look like it does nothing beyond a 1-frame flicker.
            //
            // Outside free-look (_viewingMap/_movementModeActive), that SAME per-frame block also
            // re-locks CameraPosition to the current unit every single frame - so zooming "toward
            // the cursor" (Zoom's normal behavior, shifting CameraPosition along with ZoomLevel)
            // fought that lock every frame while scrolling: one frame nudges the camera toward
            // the cursor, the very next snaps it back to the unit, over and over, which is the
            // "jitter" scrolling while unit-locked produced. Passing the camera's OWN current
            // position as the "cursor" zeroes out that position shift (see HexGridRenderer.Zoom)
            // so only ZoomLevel actually changes, matching what the position-lock already holds
            // steady anyway. Free-look modes have no such lock fighting it, so the cursor-relative
            // zoom (truly centered on the mouse) is kept there, where it works as intended.
            // Suppressed while the item-detail view is open - its own info panel claims the
            // scroll wheel to scroll its (possibly overflowing) text instead (see
            // HandleItemDetailInput/DrawItemDetail); without this, the same wheel notch would
            // both scroll the panel AND zoom the camera underneath it.
            if (!_itemDetailActive)
            {
                bool cameraIsUnitLocked = !_viewingMap && !_movementModeActive;
                Vector2 zoomOrigin = cameraIsUnitLocked ? _renderer.CameraPosition : new Vector2(mouseState.X, mouseState.Y);
                if (mouseState.ScrollWheelValue > _previousMouseState.ScrollWheelValue)
                {
                    _renderer.Zoom(0.1f, zoomOrigin);
                    _cameraZoomTarget = _renderer.ZoomLevel;
                }
                if (mouseState.ScrollWheelValue < _previousMouseState.ScrollWheelValue)
                {
                    _renderer.Zoom(-0.1f, zoomOrigin);
                    _cameraZoomTarget = _renderer.ZoomLevel;
                }
            }

            // Select hex (left click)
            if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
            {
                Vector2 viewportSize = new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
                _selectedHex = _renderer.GetHexAtScreenPos(mouseState.X, mouseState.Y, viewportSize);

                // Click a unit to open its stat sheet / buff-debuff source breakdown (see
                // DrawUnitDetailBox) - clicking the same unit again, or empty space, closes it.
                // View Map only (CanShowUnitDetailBox) - see its own doc comment for why it's
                // restricted to just that one mode instead of excluding every other mode by name.
                if (CanShowUnitDetailBox)
                {
                    var clickedUnit = _units.FirstOrDefault(u => _hexGrid.WorldToHex(u.Position) == _selectedHex);
                    _unitDetailBoxUnit = (clickedUnit != null && clickedUnit != _unitDetailBoxUnit) ? clickedUnit : null;
                }
            }

            // Place test object (right click)
            if (mouseState.RightButton == ButtonState.Pressed && _previousMouseState.RightButton == ButtonState.Released)
            {
                if (_hexGrid.IsInBounds(_selectedHex.col, _selectedHex.row))
                {
                    // Add a test object
                    string objectId = $"test_obj_{DateTime.Now.Ticks}";
                    MapObject obj = new MapObject(objectId, "test", "tree_oak", _selectedHex.col, _selectedHex.row)
                    {
                        Height = 1,
                        Blocking = true,
                        Walkable = false
                    };
                    _map.AddObject(obj);
                }
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            Vector2 viewportSize = new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);

            if (_gameState == GameState.StartMenu)
            {
                _spriteBatch.Begin();
                DrawStartMenu(viewportSize);
                _spriteBatch.End();

                base.Draw(gameTime);
                return;
            }

            if (_gameState == GameState.CharacterSelect)
            {
                _spriteBatch.Begin();
                DrawCharacterSelect(viewportSize);
                _spriteBatch.End();

                base.Draw(gameTime);
                return;
            }

            if (_gameState == GameState.MatchOver)
            {
                _spriteBatch.Begin();
                DrawMatchOver(viewportSize);
                _spriteBatch.End();

                base.Draw(gameTime);
                return;
            }

            // Draw grid with camera transform
            _spriteBatch.Begin(transformMatrix: _renderer.GetCameraMatrixPublic(viewportSize));

            // Fog of War: whatever the player's own current team can see right now (see
            // GetVisibleTilesForTeam). Fog always stays anchored to the human's own hero/team
            // (GetTeamRoot(_playerUnit)), not whoever _turnSystem.CurrentUnit happens to be, so
            // watching an enemy AI's own turn in Arena mode never reveals more than the human
            // could otherwise see - the entire point of fog of war. null (shown as everything
            // visible, fog disabled) in Test Mode - that sandbox already loosens real-match
            // constraints for fast iteration (see "AP/MP effectively unlimited"), and fog would
            // actively work against testing by hiding the very units/AI behavior being tested -
            // and in the one state this far down in Draw that can still have no _playerUnit at all.
            HashSet<(int col, int row)> fogVisibleTiles = (!_testModeActive && _playerUnit != null)
                ? GetVisibleTilesForTeam(GetTeamRoot(_playerUnit))
                : null;

            // Draw all hex tiles with colors
            for (int col = 0; col < _hexGrid.Width; col++)
            {
                for (int row = 0; row < _hexGrid.Height; row++)
                {
                    Vector2 worldPos = _hexGrid.HexToWorld(col, row);
                    Tile tile = _map.GetTile(col, row);
                    Color fillColor = GetTileColor(tile?.Type ?? "grass");
                    DrawHexFilled(worldPos, fillColor, Color.DarkGray);
                }
            }

            // Fog of War tint - a dark semi-transparent overlay on every tile outside the above,
            // similar to the classic RTS "darkened instead of hidden" look (StarCraft, etc.):
            // terrain shape still faintly readable, but clearly obscured. Drawn right over the
            // base terrain, before anything else (targeting highlights, units, ...), so those
            // still read clearly on top within vision, and a unit standing on a fogged tile simply
            // never gets drawn at all (see the unit-drawing loop below) rather than drawing
            // visible-but-dimmed, which wouldn't actually hide anything.
            if (fogVisibleTiles != null)
            {
                for (int col = 0; col < _hexGrid.Width; col++)
                {
                    for (int row = 0; row < _hexGrid.Height; row++)
                    {
                        if (fogVisibleTiles.Contains((col, row)))
                            continue;
                        Vector2 worldPos = _hexGrid.HexToWorld(col, row);
                        DrawHexFilled(worldPos, new Color(0, 0, 0, 170), new Color(0, 0, 0, 170));
                    }
                }
            }

            // Draw movement range (AP cost per tile, red if beyond current AP) and a live
            // path preview toward whichever tile the mouse is over
            if (_movementModeActive)
            {
                DrawMovementRange();
                DrawMovementPathPreview(viewportSize);
            }

            var mouseState = Mouse.GetState();

            // Targeting mode: show the move's full range (dim), every actual valid target on
            // top of that (red), and track which one (if any) the mouse is over so the UI layer
            // can show a damage/hit%/crit/affliction preview for it.
            _hoveredAttackTarget = null;
            if (_targetingModeActive)
            {
                var attackerHexForRange = _hexGrid.WorldToHex(_playerUnit.Position);
                int rangeForIndicator = _pendingMove.GetEffectiveRange(_playerUnit, _pendingSourceWeapon);
                foreach (var (col, row) in _hexGrid.GetHexesInRadius(attackerHexForRange.col, attackerHexForRange.row, rangeForIndicator))
                {
                    // GetHexesInRadius is a pure hex-distance circle with no map-bounds awareness
                    // of its own - skip anything off the edge of the actual map, or the range
                    // indicator spills out past the grid entirely (very visible on a small map,
                    // e.g. Test Mode's 3x3 one, where an ordinary 5-range spell's circle is
                    // several times the size of the map itself).
                    if (!_hexGrid.IsInBounds(col, row))
                        continue;
                    DrawHexFilled(_hexGrid.HexToWorld(col, row), new Color(200, 60, 60, 40), new Color(200, 60, 60, 90));
                }

                foreach (var candidate in _targetCandidates)
                    DrawHexFilled(candidate.Position, new Color(220, 40, 40, 140), Color.Red);

                var hoveredHexForTargeting = _renderer.GetHexAtScreenPos(mouseState.X, mouseState.Y, viewportSize);
                _hoveredAttackTarget = _targetCandidates.FirstOrDefault(u => _hexGrid.WorldToHex(u.Position) == hoveredHexForTargeting);
            }

            // Live cone preview - highlights the exact hexes _pendingMove would hit if fired
            // toward wherever the mouse is right now.
            if (_coneAimingModeActive)
            {
                var hoveredHex = _renderer.GetHexAtScreenPos(mouseState.X, mouseState.Y, viewportSize);
                var attackerHex = _hexGrid.WorldToHex(_playerUnit.Position);
                if (hoveredHex != attackerHex)
                {
                    var direction = _hexGrid.GetDirectionTo(attackerHex.col, attackerHex.row, hoveredHex.col, hoveredHex.row);
                    int range = _pendingMove.GetEffectiveRange(_playerUnit, _pendingSourceWeapon);
                    foreach (var (col, row) in _hexGrid.GetHexesInCone(attackerHex.col, attackerHex.row, direction, range))
                    {
                        // GetHexesInCone has the same no-bounds-awareness issue GetHexesInRadius
                        // does (see the attack-range indicator above) - skip anything off the map.
                        if (!_hexGrid.IsInBounds(col, row))
                            continue;
                        DrawHexFilled(_hexGrid.HexToWorld(col, row), new Color(120, 220, 255, 140), Color.CornflowerBlue);
                    }
                }
            }

            // Teleport targeting (e.g. Blink) - every valid destination tile highlighted purple,
            // whichever one the mouse is over highlighted brighter.
            if (_teleportModeActive)
            {
                var hoveredHex = _renderer.GetHexAtScreenPos(mouseState.X, mouseState.Y, viewportSize);
                foreach (var (col, row) in _teleportValidTiles)
                {
                    bool hovered = (col, row) == hoveredHex;
                    Color fill = hovered ? new Color(220, 150, 255, 180) : new Color(170, 100, 255, 110);
                    Color outline = hovered ? Color.White : Color.MediumPurple;
                    DrawHexFilled(_hexGrid.HexToWorld(col, row), fill, outline);
                }
            }

            // Ally-shield targeting (e.g. Arcane Shield) - every valid target highlighted green
            // (including the caster's own tile), brighter where the mouse is hovering.
            if (_allyTargetModeActive)
            {
                var hoveredHex = _renderer.GetHexAtScreenPos(mouseState.X, mouseState.Y, viewportSize);
                foreach (var candidate in _allyTargetCandidates)
                {
                    bool hovered = _hexGrid.WorldToHex(candidate.Position) == hoveredHex;
                    Color fill = hovered ? new Color(160, 255, 170, 180) : new Color(100, 220, 140, 120);
                    Color outline = hovered ? Color.White : Color.LightGreen;
                    DrawHexFilled(candidate.Position, fill, outline);
                }
            }

            // Tame Beast targeting - every tameable candidate highlighted amber/gold (distinct
            // from attack's red and ally-shield's green - this is neither hostile nor an ally
            // yet), brighter where the mouse is hovering.
            if (_tameBeastModeActive)
            {
                var hoveredHex = _renderer.GetHexAtScreenPos(mouseState.X, mouseState.Y, viewportSize);
                foreach (var candidate in _tameBeastCandidates)
                {
                    bool hovered = _hexGrid.WorldToHex(candidate.Position) == hoveredHex;
                    Color fill = hovered ? new Color(255, 215, 120, 180) : new Color(220, 175, 70, 120);
                    Color outline = hovered ? Color.White : Color.Goldenrod;
                    DrawHexFilled(candidate.Position, fill, outline);
                }
            }

            // Trap placement (Move.IsPlaceTrap, e.g. Place Trap) - every valid tile highlighted
            // dark red/brown (distinct from every other mode's color), brighter where the mouse
            // is hovering. Once actually placed, a Trap draws its own placeholder sprite instead
            // (see the trap-rendering pass right before "Draw units" below) - visible only to its
            // own team, unlike this placement-targeting highlight which only the caster ever sees
            // anyway (it's their own targeting mode).
            if (_trapPlacementModeActive)
            {
                var hoveredHex = _renderer.GetHexAtScreenPos(mouseState.X, mouseState.Y, viewportSize);
                foreach (var (col, row) in _trapPlacementValidTiles)
                {
                    bool hovered = (col, row) == hoveredHex;
                    Color fill = hovered ? new Color(200, 90, 60, 180) : new Color(150, 60, 40, 120);
                    Color outline = hovered ? Color.White : Color.DarkRed;
                    DrawHexFilled(_hexGrid.HexToWorld(col, row), fill, outline);
                }
            }

            // Leap targeting (Move.IsLeap, e.g. the Dusk Roachlin's Leap) - both valid empty
            // tiles AND valid unit targets highlighted the same teal color (a plain reposition
            // and a pounce-onto-a-target are both just "a valid place to click" here, distinct
            // from attack targeting's red and every other mode's own color), brighter under the
            // mouse. A unit target highlights over its own tile, same as attack targeting does.
            if (_leapModeActive)
            {
                var hoveredHex = _renderer.GetHexAtScreenPos(mouseState.X, mouseState.Y, viewportSize);
                foreach (var (col, row) in _leapValidTiles)
                {
                    bool hovered = (col, row) == hoveredHex;
                    Color fill = hovered ? new Color(100, 220, 210, 180) : new Color(60, 170, 160, 110);
                    Color outline = hovered ? Color.White : Color.Teal;
                    DrawHexFilled(_hexGrid.HexToWorld(col, row), fill, outline);
                }
                foreach (var target in _leapValidTargets)
                {
                    bool hovered = _hexGrid.WorldToHex(target.Position) == hoveredHex;
                    Color fill = hovered ? new Color(100, 220, 210, 180) : new Color(60, 170, 160, 110);
                    Color outline = hovered ? Color.White : Color.Teal;
                    DrawHexFilled(target.Position, fill, outline);
                }
            }

            // Release targeting (the "Release" turn-menu option while Pinning) - every adjacent
            // tile the caster could step off onto, same teal as Leap's own highlight (stepping
            // off is really just the back half of the same mechanic).
            if (_releaseModeActive)
            {
                var hoveredHex = _renderer.GetHexAtScreenPos(mouseState.X, mouseState.Y, viewportSize);
                foreach (var (col, row) in _releaseValidTiles)
                {
                    bool hovered = (col, row) == hoveredHex;
                    Color fill = hovered ? new Color(100, 220, 210, 180) : new Color(60, 170, 160, 110);
                    Color outline = hovered ? Color.White : Color.Teal;
                    DrawHexFilled(_hexGrid.HexToWorld(col, row), fill, outline);
                }
            }

            // Summon placement (any SummonCard) - every valid placement tile highlighted
            // orange, whichever one the mouse is over highlighted brighter.
            if (_summonPlacementModeActive)
            {
                var hoveredHex = _renderer.GetHexAtScreenPos(mouseState.X, mouseState.Y, viewportSize);
                foreach (var (col, row) in _summonPlacementValidTiles)
                {
                    bool hovered = (col, row) == hoveredHex;
                    Color fill = hovered ? new Color(255, 200, 120, 180) : new Color(220, 150, 60, 110);
                    Color outline = hovered ? Color.White : Color.Orange;
                    DrawHexFilled(_hexGrid.HexToWorld(col, row), fill, outline);
                }
            }

            // Generic Portal placement (Phase 1 of CastSummon) - every valid placement tile
            // highlighted purple (distinct from the orange creature-placement highlight above,
            // since they're two different steps of the same cast), whichever one the mouse is
            // over highlighted brighter.
            if (_portalPlacementModeActive)
            {
                var hoveredHex = _renderer.GetHexAtScreenPos(mouseState.X, mouseState.Y, viewportSize);
                foreach (var (col, row) in _portalPlacementValidTiles)
                {
                    bool hovered = (col, row) == hoveredHex;
                    Color fill = hovered ? new Color(190, 140, 255, 180) : new Color(140, 90, 220, 110);
                    Color outline = hovered ? Color.White : new Color(170, 110, 255);
                    DrawHexFilled(_hexGrid.HexToWorld(col, row), fill, outline);
                }
            }

            // Draw hover hex
            if (!_consoleOpen)
            {
                var hoverHex = _renderer.GetHexAtScreenPos(mouseState.X, mouseState.Y, viewportSize);
                if (_hexGrid.IsInBounds(hoverHex.col, hoverHex.row))
                {
                    Vector2 worldPos = _hexGrid.HexToWorld(hoverHex.col, hoverHex.row);
                    DrawHexFilled(worldPos, new Color(100, 200, 100, 128), Color.Yellow);
                }
            }

            // Draw selected hex
            if (_hexGrid.IsInBounds(_selectedHex.col, _selectedHex.row))
            {
                Vector2 worldPos = _hexGrid.HexToWorld(_selectedHex.col, _selectedHex.row);
                DrawHexFilled(worldPos, new Color(200, 255, 100, 128), Color.Yellow);
            }

            // Dropped items (e.g. Meat from a slain beast) - a small ground marker at each
            // one's own position, drawn before units so a unit standing on top of one still
            // reads clearly on top of it. Respects Fog of War same as a unit would (skipped
            // entirely outside the player's team's current vision, not just dimmed).
            foreach (var dropped in _droppedItems)
            {
                if (fogVisibleTiles != null && !fogVisibleTiles.Contains(_hexGrid.WorldToHex(dropped.Position)))
                    continue;

                const float size = 14f;
                var rect = new Rectangle((int)(dropped.Position.X - size / 2f), (int)(dropped.Position.Y - size / 2f), (int)size, (int)size);
                _spriteBatch.Draw(_whitePixel, rect, new Color(150, 90, 50));
            }

            // Placed Traps (Objects.Trap, e.g. Place Trap's "Bearat Trap") - visible ONLY to
            // their own team (GetTeamRoot(trap.Owner) == GetTeamRoot(_playerUnit)), regardless of
            // Fog of War's own vision-range check (fogVisibleTiles above) - a trap is a
            // deliberate team secret, not just something far away, so an enemy standing right
            // next to one still can't see it. Re-evaluated every frame against whichever team
            // _playerUnit currently belongs to, so switching control to a different class (e.g.
            // Test Mode cycling through every class on its own turn) immediately hides any trap
            // that class didn't place. Drawn tinted (the placeholder Pawn sprite has no trap-like
            // art of its own) and smaller than a real unit - it's a marker, not a creature.
            if (_trapSpriteTexture != null && _playerUnit != null)
            {
                var viewerTeam = GetTeamRoot(_playerUnit);
                foreach (var trap in _placedTraps)
                {
                    if (GetTeamRoot(trap.Owner) != viewerTeam)
                        continue;

                    const float trapScale = 0.3f;
                    Vector2 trapWorldPos = _hexGrid.HexToWorld(trap.Tile.col, trap.Tile.row);
                    Vector2 trapSize = new Vector2(_trapSpriteTexture.Width * trapScale, _trapSpriteTexture.Height * trapScale);
                    Vector2 trapDrawPos = trapWorldPos - (trapSize / 2f);
                    _spriteBatch.Draw(_trapSpriteTexture, trapDrawPos, null, new Color(140, 60, 40), 0f,
                                    Vector2.Zero, trapScale, SpriteEffects.None, 0f);
                }
            }

            // Whichever unit (if any) the mouse is currently over - drives which neutral/enemy
            // units show their active stat-change icons below (DrawUnitStatBonuses); the
            // player's own unit and its summons always show theirs regardless of hover.
            var statHoverHex = _renderer.GetHexAtScreenPos(mouseState.X, mouseState.Y, viewportSize);
            var statHoveredUnit = _units.FirstOrDefault(u => _hexGrid.WorldToHex(u.Position) == statHoverHex);

            // Draw units
            foreach (var unit in _units)
            {
                // Fog of War: a unit standing on a tile outside the player's own team's current
                // vision is skipped entirely - sprite, outline, HP bar, name, everything - rather
                // than drawn dimmed, since a fogged-but-still-fully-visible unit wouldn't actually
                // hide anything. A unit's own tile is always within its own ViewRange of itself
                // (distance 0), so this never hides a unit from its own team.
                if (fogVisibleTiles != null && !fogVisibleTiles.Contains(_hexGrid.WorldToHex(unit.Position)))
                    continue;

                if (unit.SpriteTexture != null)
                {
                    // Base scale factor (0.15 = much smaller, fits within hex)
                    // Multiply by per-unit scale for individual adjustments
                    float finalScale = 0.15f * unit.Scale;

                    // Center sprite on tile
                    Vector2 spriteSize = new Vector2(unit.SpriteTexture.Width * finalScale, unit.SpriteTexture.Height * finalScale);
                    Vector2 spritePos = unit.Position - (spriteSize / 2f);

                    // No real greyscale shader exists (or is worth building) for a "simple"
                    // death indicator - tinting toward gray is the same approximation IsFainted
                    // already uses for its HP text below, just applied to the sprite too.
                    Color spriteTint = unit.IsAlive ? Color.White : new Color(90, 90, 90);

                    // Generic "took a hit" reaction (see SpawnAttackAnimations/ActiveHitReaction) -
                    // a quick nudge away from the attacker plus a red flash, both eased in and out
                    // over the reaction's lifetime via a single sin(pi*t) bump (0 at start/end, 1 at
                    // the midpoint) rather than a linear fade, so it doesn't snap in/out abruptly.
                    var hitReaction = _activeHitReactions.Find(h => h.Target == unit);
                    if (hitReaction != null && hitReaction.Elapsed >= hitReaction.StartDelay)
                    {
                        float hitT = MathHelper.Clamp((hitReaction.Elapsed - hitReaction.StartDelay) / hitReaction.Duration, 0f, 1f);
                        float intensity = (float)Math.Sin(hitT * MathHelper.Pi);
                        spritePos += hitReaction.NudgeDirection * intensity * HitReactionNudgeDistance;
                        spriteTint = Color.Lerp(spriteTint, Color.Red, intensity * 0.8f);
                    }

                    // Team outline - which player's side this unit is on at a glance (a wild,
                    // untamed beast gets none at all - see GetTeamOutlineColor). No outline
                    // shader exists (or is worth building), so this draws against a cached PURE
                    // WHITE silhouette of the sprite (GetSilhouetteTexture - same alpha shape,
                    // every visible pixel forced to white) instead of the real art. Tinting the
                    // real art directly (the first version of this) multiplied the tint against
                    // the sprite's own varied colors, which read as a smeared colored shadow, not
                    // an outline - tinting a WHITE source multiplies cleanly to the exact outline
                    // color for every visible pixel, so offsetting it a couple pixels in every
                    // direction behind the real sprite reads as an actual crisp-edged outline.
                    Color? outlineColor = unit.IsAlive ? GetTeamOutlineColor(unit) : null;
                    if (outlineColor.HasValue)
                    {
                        var silhouette = GetSilhouetteTexture(unit);
                        const float outlineOffset = 1.5f;
                        Vector2[] outlineOffsets =
                        {
                            new Vector2(-outlineOffset, 0f), new Vector2(outlineOffset, 0f),
                            new Vector2(0f, -outlineOffset), new Vector2(0f, outlineOffset),
                            new Vector2(-outlineOffset, -outlineOffset), new Vector2(outlineOffset, -outlineOffset),
                            new Vector2(-outlineOffset, outlineOffset), new Vector2(outlineOffset, outlineOffset),
                        };
                        foreach (var offset in outlineOffsets)
                        {
                            _spriteBatch.Draw(silhouette, spritePos + offset, null, outlineColor.Value, 0f,
                                            Vector2.Zero, finalScale, SpriteEffects.None, 0f);
                        }
                    }

                    _spriteBatch.Draw(unit.SpriteTexture, spritePos, null, spriteTint, 0f,
                                    Vector2.Zero, finalScale, SpriteEffects.None, 0f);
                }
                else if (unit is Portal)
                {
                    DrawPortalPlaceholder(unit.Position);
                }

                DrawFacingIndicator(unit);

                if (unit.IsAlive)
                    DrawUnitHealthBar(unit);

                if (_font != null)
                {
                    // An untamed Wild creature shows its Disposition right in the status line -
                    // Tame Beast's candidate filter is invisible otherwise (nothing tells a
                    // player which Wild units are even eligible before they commit to casting
                    // it), and "Neutral"/"Friendly"/"Hostile" doubles as a quick combat-threat
                    // read regardless of taming. Skipped for anything already owned (tamed or
                    // otherwise) - Disposition stops being meaningful the moment Owner is set.
                    string dispositionSuffix = unit.Owner == null && unit.Factions.Contains(Faction.Wild)
                        ? $" [{unit.Disposition}]"
                        : "";
                    string statusText = unit.IsAlive ? $"{unit.Name}: {unit.HP}/{unit.EffectiveMaxHP}{dispositionSuffix}" : $"{unit.Name}: DEAD";
                    Vector2 textSize = _font.MeasureString(statusText) * 0.4f;
                    Vector2 textPos = unit.Position - new Vector2(textSize.X / 2f, 48f);
                    Color textColor = !unit.IsAlive ? Color.DarkRed : (unit.IsFainted ? Color.Gray : Color.White);
                    _spriteBatch.DrawString(_font, statusText, textPos, textColor, 0f, Vector2.Zero, 0.4f, SpriteEffects.None, 0f);

                    // Every active BUFF (e.g. Brace, Deep Sleep, Meditate's live stacks, Pinning)
                    // shown as a small cyan line right above the name/HP text - the only
                    // persistent on-screen sign any is active, beyond the fading combat-log
                    // message shown the moment one is applied. Fully generic - a brand new Buff
                    // StatusEffect shows up here automatically, no Game1 change needed. Used to
                    // show Afflictions too (in orange) - now exclusively DrawUnitAfflictions' own
                    // job instead (a detailed red line per affliction, not just its bare name),
                    // so the two don't show duplicate/colliding text for the same effect.
                    var buffs = unit.StatusEffects.Where(e => e.Type == StatusEffectType.Buff).ToList();
                    if (buffs.Count > 0)
                    {
                        string effectText = string.Join(", ", buffs.Select(e =>
                            e.GrowsOverTime ? $"{e.Name} ({e.Stacks}/{e.MaxStacks})" : e.Name));
                        Vector2 effectTextSize = _font.MeasureString(effectText) * 0.35f;
                        Vector2 effectTextPos = unit.Position - new Vector2(effectTextSize.X / 2f, 68f);
                        _spriteBatch.DrawString(_font, effectText, effectTextPos, Color.Cyan, 0f, Vector2.Zero, 0.35f, SpriteEffects.None, 0f);
                    }
                }

                // Per-stat icon + signed value row (sword/book/shield/rune/crosshair for
                // STR/INT/DEF/RES/ACC) for every stat an active StatusEffect is currently
                // pushing off base - shown for the currently-ACTING team (whoever _turnSystem.
                // CurrentUnit's team is - its summoner and every one of that summoner's allies/
                // summons alike, matching "the summoner and all allied units show stat changes
                // whenever it's that summoner's turn"), and for anyone else only while hovered/
                // targeted. Deliberately keyed off CurrentUnit's team rather than `_playerUnit ==
                // unit.Owner` - in Test Mode _playerUnit is reassigned to whoever's turn it is
                // (see EnterTestMode), so a summon's badge would vanish the instant control
                // passed to a DIFFERENT team even though its buff was still genuinely active;
                // CurrentUnit's team doesn't have that problem, and gives identical results to
                // the old check in a normal (non-Test-Mode) match where _playerUnit never moves.
                bool showStatBonuses = GetTeamRoot(unit) == GetTeamRoot(_turnSystem.CurrentUnit) || unit == statHoveredUnit;
                if (showStatBonuses)
                {
                    DrawUnitStatBonuses(unit);
                    DrawUnitAfflictions(unit);
                }
            }

            // Simple slash/shooting attack effects (see SpawnAttackAnimations) - purely visual.
            // t is against TravelDuration, not the full Duration - for Slash the two are equal
            // (no change there), but a Shooting effect also holds at the target for a bit after
            // actually arriving (Duration - TravelDuration), so it doesn't vanish the instant t
            // would otherwise hit 1.
            foreach (var effect in _activeAttackEffects)
            {
                float t = MathHelper.Clamp(effect.Elapsed / effect.TravelDuration, 0f, 1f);
                if (effect.Type == AttackAnimationType.Slash)
                    DrawSlashEffect(effect.To, t);
                else
                    DrawShootingEffect(effect.From, effect.To, t, effect.ProjectileColor);
            }

            // Floating damage numbers (see SpawnDamageNumber) - rises 24px and fades out over its
            // Duration, drawn in world space (camera-transformed, same as the attack effects just
            // above) so it tracks the target correctly even if the camera pans mid-flight.
            if (_font != null)
            {
                foreach (var dmg in _activeDamageNumbers)
                {
                    float t = MathHelper.Clamp(dmg.Elapsed / dmg.Duration, 0f, 1f);
                    Vector2 pos = dmg.Target.Position + new Vector2(0f, -40f - 24f * t);
                    string text = $"-{dmg.Amount}";
                    Vector2 textSize = _font.MeasureString(text) * 0.4f;
                    int alpha = (int)(255 * (1f - t));
                    _spriteBatch.DrawString(_font, text, pos - new Vector2(textSize.X / 2f, 0f),
                        new Color(255, 60, 60, alpha), 0f, Vector2.Zero, 0.4f, SpriteEffects.None, 0f);
                }
            }

            _spriteBatch.End();

            // Draw UI (screen space, no camera transform)
            _spriteBatch.Begin();

            if (_font != null)
            {
                _spriteBatch.DrawString(_font, $"Turn {_turnSystem.CurrentTurn}", new Vector2(16, 16), Color.White);

                if (_turnSystem.CurrentUnit == _playerUnit)
                {
                    _spriteBatch.DrawString(_font, $"AP: {_playerUnit.CurrentAP}/{_playerUnit.MaxAP}", new Vector2(16, 40), Color.White);
                    _spriteBatch.DrawString(_font, $"MP: {_playerUnit.CurrentMP}/{_playerUnit.MaxMP}", new Vector2(16, 64), Color.White);
                }

                if (_turnSystem.ShowingTurnAnnouncement)
                    DrawTurnAnnouncement(viewportSize);

                // A restricting StatusEffect (Stunned, Deep Sleep, Meditating, ...) replaces the
                // normal turn menu entirely - ResolveInstantCast sets _turnMenuActive = true right
                // after a successful cast with no knowledge of whether that cast just restricted
                // the caster (Meditate/Forced Sleep), so without this check both menus would draw
                // at once (the full Attack/Cards/Items/... list AND the restricted one) the moment
                // one of those is cast.
                bool playerRestricted = _turnSystem.CurrentUnit == _playerUnit && _playerUnit.GetRestrictingEffect() != null;

                // Hidden while an attack animation is still playing (same condition the Update
                // loop gates input on) - _turnMenuActive itself stays true the whole time (every
                // Cast*/ExecutePendingAttack call site that reopens the menu doesn't need to know
                // or care about animation timing), this just defers actually SHOWING it until the
                // shot has landed, so it doesn't instantly reappear over a traveling projectile.
                if (_turnMenuActive && _activeAttackEffects.Count == 0 && !playerRestricted)
                    DrawTurnMenu(viewportSize);

                if (playerRestricted)
                    DrawRestrictedMenu(viewportSize);

                if (_attackMenuActive)
                    DrawAttackMenu(viewportSize);

                if (_viewingMap)
                    DrawMapViewIndicator(viewportSize);

                if (_movementModeActive)
                    DrawBottomHint(viewportSize, "Click a highlighted tile to move - E to cancel");

                if (_targetingModeActive)
                {
                    DrawBottomHint(viewportSize, "Select a target, or press E to return");
                    if (_hoveredAttackTarget != null)
                        DrawAttackPreview(_hoveredAttackTarget, viewportSize);
                }
                else if (_coneAimingModeActive)
                    DrawBottomHint(viewportSize, "Click anywhere to aim the cone and fire - E to cancel");
                else if (_teleportModeActive)
                    DrawBottomHint(viewportSize, "Click a highlighted tile to blink there - E to cancel");
                else if (_allyTargetModeActive)
                    DrawBottomHint(viewportSize, "Select a target to shield (yourself included) - E to cancel");
                else if (_tameBeastModeActive)
                    DrawBottomHint(viewportSize, "Select a beast to tame - E to cancel");
                else if (_trapPlacementModeActive)
                    DrawBottomHint(viewportSize, "Click a highlighted tile to place your trap - E to cancel");
                else if (_leapModeActive)
                    DrawBottomHint(viewportSize, "Click a tile to leap there, or a target to pounce onto it - E to cancel");
                else if (_releaseModeActive)
                    DrawBottomHint(viewportSize, "Click an adjacent tile to step off onto - E to cancel");
                else if (_summonPlacementModeActive)
                    DrawBottomHint(viewportSize, "Click a highlighted tile to place your summon - E to cancel");
                else if (_portalPlacementModeActive)
                    DrawBottomHint(viewportSize, "Click a highlighted tile to place your Generic Portal - E to cancel");
                else if (_devSummonModeActive)
                    DrawBottomHint(viewportSize, $"Click a tile to place {_pendingDevSummonCard.Name} ({(_pendingDevSummonOwner != null ? _pendingDevSummonOwner.Name : "Neutral")}) - E to cancel");
                else if (_currentUnitTurnBatched)
                    // AI Turn Batching (see ShouldBatchAiTurn) - shown for the whole stretch of
                    // fast-forwarded animal/unseen-enemy turns, taking priority over the normal
                    // combat-log fallback below so a quick flurry of individual action messages
                    // doesn't flicker in and out while it's happening.
                    DrawBottomHint(viewportSize, "Other units are taking their turns...");
                else if (_combatLogTimer > 0f && !string.IsNullOrEmpty(_combatLogMessage))
                    DrawBottomHint(viewportSize, _combatLogMessage);

                if (_cardMenuActive)
                    DrawCardMenuOverlay(viewportSize);

                if (_itemsMenuActive)
                    DrawItemsMenu(viewportSize);

                if (_itemDetailActive)
                    DrawItemDetail(viewportSize);

                if (_unitDetailBoxUnit != null && CanShowUnitDetailBox)
                    DrawUnitDetailBox(_unitDetailBoxUnit, viewportSize);

                if (_consoleOpen)
                    DrawConsole();
            }

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        /// <summary>The game's actual boot screen (see Initialize/GameState.StartMenu) - "Arena" (the full map, character select, one controlled unit) or "Debug" (the small map, every unit controlled) - same dark-background/centered-list style DrawCharacterSelect uses.</summary>
        private void DrawStartMenu(Vector2 viewportSize)
        {
            _spriteBatch.Draw(_whitePixel, new Rectangle(0, 0, (int)viewportSize.X, (int)viewportSize.Y), new Color(20, 20, 30));

            if (_font == null)
                return;

            string title = "Sages of Ozvaram";
            Vector2 titleSize = _font.MeasureString(title);
            _spriteBatch.DrawString(_font, title, new Vector2((viewportSize.X - titleSize.X) / 2f, 160), Color.White);

            Rectangle[] optionRects = GetStartMenuOptionRects(viewportSize);
            for (int i = 0; i < optionRects.Length; i++)
            {
                bool selected = i == _startMenuIndex;
                Color bg = selected ? new Color(255, 200, 0, 220) : new Color(60, 60, 70);
                Color textColor = selected ? Color.Black : Color.White;

                _spriteBatch.Draw(_whitePixel, optionRects[i], bg);

                string label = StartMenuLabels[i];
                Vector2 labelSize = _font.MeasureString(label);
                Vector2 labelPos = new Vector2(
                    optionRects[i].X + (optionRects[i].Width - labelSize.X) / 2f,
                    optionRects[i].Y + (optionRects[i].Height - labelSize.Y) / 2f);
                _spriteBatch.DrawString(_font, label, labelPos, textColor);
            }

            string hint = "WASD or mouse to choose - Click or E to confirm";
            Vector2 hintSize = _font.MeasureString(hint);
            _spriteBatch.DrawString(_font, hint, new Vector2((viewportSize.X - hintSize.X) / 2f, viewportSize.Y - 60), Color.Gray);
        }

        private void DrawCharacterSelect(Vector2 viewportSize)
        {
            _spriteBatch.Draw(_whitePixel, new Rectangle(0, 0, (int)viewportSize.X, (int)viewportSize.Y), new Color(20, 20, 30));

            if (_font == null)
                return;

            string title = "Select Your Character";
            Vector2 titleSize = _font.MeasureString(title);
            _spriteBatch.DrawString(_font, title, new Vector2((viewportSize.X - titleSize.X) / 2f, 60), Color.White);

            Rectangle[] rects = GetCharacterSelectRects(viewportSize);
            for (int i = 0; i < rects.Length; i++)
            {
                bool selected = i == _selectedCharacterIndex;
                Color highlight = selected ? Color.Gold : new Color(60, 60, 70);

                var border = new Rectangle(rects[i].X - 6, rects[i].Y - 6, rects[i].Width + 12, rects[i].Height + 12);
                _spriteBatch.Draw(_whitePixel, border, highlight);
                _spriteBatch.Draw(_whitePixel, rects[i], Color.Black);

                if (_avatarTextures[i] != null)
                    _spriteBatch.Draw(_avatarTextures[i], rects[i], Color.White);

                string name = _units[i].Name;
                Vector2 nameSize = _font.MeasureString(name);
                _spriteBatch.DrawString(_font, name,
                    new Vector2(rects[i].X + (rects[i].Width - nameSize.X) / 2f, rects[i].Bottom + 10),
                    selected ? Color.Gold : Color.White);
            }

            string hint = "WASD or mouse to choose - Click or E to confirm";
            Vector2 hintSize = _font.MeasureString(hint);
            _spriteBatch.DrawString(_font, hint, new Vector2((viewportSize.X - hintSize.X) / 2f, viewportSize.Y - 60), Color.Gray);
        }

        private void DrawTurnMenu(Vector2 viewportSize)
        {
            var options = GetTurnMenuOptions();
            _turnMenuIndex = Math.Clamp(_turnMenuIndex, 0, options.Count - 1);
            Rectangle[] optionRects = GetTurnMenuOptionRects(viewportSize);

            for (int i = 0; i < options.Count; i++)
            {
                bool selected = i == _turnMenuIndex;
                Color bg = selected ? new Color(255, 200, 0, 220) : new Color(0, 0, 0, 200);
                Color textColor = selected ? Color.Black : Color.White;

                // Label already reflects context (e.g. "Guard" vs "End", "Stand Up" vs "Move") -
                // see TurnMenuBuilder.
                _spriteBatch.Draw(_whitePixel, optionRects[i], bg);
                _spriteBatch.DrawString(_font, options[i].Label,
                    new Vector2(optionRects[i].X + 8, optionRects[i].Y + 6), textColor);
            }
        }

        /// <summary>The restricted menu shown while the player's unit has a restricting StatusEffect active - that effect's own EndEffectLabel (with its own EndEffectAPCost) or ending the turn without acting. Entirely data-driven off the active effect - no per-effect-name special casing, so it works the same for Stun, Deep Sleep, Meditate, or any future one.</summary>
        private void DrawRestrictedMenu(Vector2 viewportSize)
        {
            var effect = _playerUnit.GetRestrictingEffect();
            if (effect == null)
                return;

            var options = GetRestrictedMenuOptions(_playerUnit);
            Rectangle[] optionRects = GetMenuOptionRects(viewportSize, options.Length);

            for (int i = 0; i < options.Length; i++)
            {
                bool selected = i == _restrictedMenuIndex;
                Color bg = selected ? new Color(255, 200, 0, 220) : new Color(0, 0, 0, 200);
                Color textColor = selected ? Color.Black : Color.White;

                string label = options[i] == effect.EndEffectLabel
                    ? $"{options[i]} ({effect.EndEffectAPCost} AP)"
                    : options[i];

                _spriteBatch.Draw(_whitePixel, optionRects[i], bg);
                _spriteBatch.DrawString(_font, label,
                    new Vector2(optionRects[i].X + 8, optionRects[i].Y + 6), textColor);
            }

            DrawBottomHint(viewportSize, $"{effect.Name}!");
        }

        private void DrawAttackMenu(Vector2 viewportSize)
        {
            Rectangle[] optionRects = GetMenuOptionRects(viewportSize, _attackMenuLabels.Count);

            for (int i = 0; i < _attackMenuLabels.Count; i++)
            {
                bool selected = i == _attackMenuIndex;
                Color bg = selected ? new Color(255, 200, 0, 220) : new Color(0, 0, 0, 200);

                // "Back" has no entry in _attackMenuMoves (it's the trailing extra label) -
                // everything else is only unaffordable if the player can't cover its AP or MP.
                bool affordable = true;
                if (i < _attackMenuMoves.Count)
                {
                    var (move, sourceWeapon) = _attackMenuMoves[i];
                    affordable = _playerUnit.CurrentAP >= _playerUnit.GetEffectiveAPCost(move, sourceWeapon)
                        && _playerUnit.CurrentMP >= move.MPCost;
                }
                Color textColor = !affordable ? Color.Red : (selected ? Color.Black : Color.White);

                _spriteBatch.Draw(_whitePixel, optionRects[i], bg);
                _spriteBatch.DrawString(_font, _attackMenuLabels[i],
                    new Vector2(optionRects[i].X + 8, optionRects[i].Y + 6), textColor);
            }
        }

        /// <summary>The flat list of carried items (weapons + consumables, equipment once it exists) - same highlight-on-hover/selected list style as DrawAttackMenu, just with no AP/MP affordability to tint (viewing the list is free; only an action inside a specific item's detail view costs AP).</summary>
        private void DrawItemsMenu(Vector2 viewportSize)
        {
            Rectangle[] optionRects = GetMenuOptionRects(viewportSize, _itemsMenuLabels.Count);

            // Carried weight vs capacity - the combined "item box" (Weapons in Inventory PLUS
            // Consumables, see BaseUnit.CurrentCarriedWeight) now that Meat actually has a real
            // Weight (1) - the 3 original potions are still Weight 0, so this reads identically
            // to the old Weapon-only total for anyone who's never picked up Meat.
            string weightText = $"Weight: {_playerUnit.CurrentCarriedWeight}/{_playerUnit.InventoryWeightCapacity}";
            Vector2 weightPos = new Vector2(optionRects[0].X, Math.Max(10, optionRects[0].Y - 28));
            Vector2 weightTextSize = _font.MeasureString(weightText);
            var weightBoxRect = new Rectangle((int)weightPos.X - 8, (int)weightPos.Y - 4,
                                              (int)weightTextSize.X + 16, (int)weightTextSize.Y + 8);
            _spriteBatch.Draw(_whitePixel, weightBoxRect, new Color(0, 0, 0, 200));
            _spriteBatch.DrawString(_font, weightText, weightPos, Color.White);

            for (int i = 0; i < _itemsMenuLabels.Count; i++)
            {
                bool selected = i == _itemsMenuIndex;
                Color bg = selected ? new Color(255, 200, 0, 220) : new Color(0, 0, 0, 200);
                Color textColor = selected ? Color.Black : Color.White;

                _spriteBatch.Draw(_whitePixel, optionRects[i], bg);
                _spriteBatch.DrawString(_font, _itemsMenuLabels[i],
                    new Vector2(optionRects[i].X + 8, optionRects[i].Y + 6), textColor);
            }
        }

        /// <summary>
        /// A selected item's info panel (stats + granted attacks + description for a Weapon,
        /// just a description for a ConsumableItem - same top-right text-box style
        /// DrawAttackPreview uses) plus its action list (Equip/Unequip or Use, Discard, Back -
        /// same highlightable-option-row style DrawAttackMenu uses, positioned near the player
        /// like every other submenu so it never collides with the info panel).
        /// </summary>
        private void DrawItemDetail(Vector2 viewportSize)
        {
            var lines = new List<string> { _selectedItem.Name };

            if (_selectedItem is Weapon weapon)
            {
                if (weapon.Type.HasValue)
                    lines.Add($"Type: {weapon.Type.Value}");
                if (weapon.AttackPower.HasValue)
                    lines.Add($"Attack Power: {weapon.AttackPower.Value}");
                lines.Add($"Weight: {weapon.Weight}");
                if (weapon.ShieldPassiveDefResBonusPercent.HasValue)
                {
                    string guardBonus = weapon.ShieldGuardBonusPercent.HasValue
                        ? $" (+{weapon.ShieldGuardBonusPercent.Value * 100f:0}% while Guarding)"
                        : "";
                    lines.Add($"Passive DEF/RES: +{weapon.ShieldPassiveDefResBonusPercent.Value * 100f:0}%{guardBonus}");
                }

                // Stat modifiers a specialist gets using THIS weapon (see Weapon's own
                // Specialist* fields) - distinct from the weapon's base Attacks/SpecialistAttacks
                // list below, which are move names/descriptions rather than raw stat bonuses.
                bool hasSpecialistBonus = weapon.SpecialistAccuracyBonus > 0f || weapon.SpecialistRangeBonus > 0
                    || weapon.SpecialistIntDamageBonusDivisor > 0f || weapon.SpecialistFlatDamageBonus > 0;
                if (hasSpecialistBonus)
                {
                    lines.Add("Specialist Bonuses:");
                    if (weapon.SpecialistAccuracyBonus > 0f)
                        lines.Add($" - Accuracy: +{weapon.SpecialistAccuracyBonus * 100f:0}%");
                    if (weapon.SpecialistRangeBonus > 0)
                        lines.Add($" - Range: +{weapon.SpecialistRangeBonus}");
                    if (weapon.SpecialistIntDamageBonusDivisor > 0f)
                        lines.Add($" - Magic Damage: +INT/{weapon.SpecialistIntDamageBonusDivisor:0.#}");
                    if (weapon.SpecialistFlatDamageBonus > 0)
                        lines.Add($" - Damage: +{weapon.SpecialistFlatDamageBonus} {weapon.SpecialistFlatDamageBonusType}");
                }

                // Per-move stat line: ATTACK is a weapon/race stat for FLAT physical damage
                // before modifiers (Sharp/Blunt), MAGIC is the same for magical damage
                // (Magical/Light) - weapon.AttackPower if this weapon has been migrated to it,
                // else the move's own BaseDamage (see WeaponCatalog's migration note). STR/INT
                // scaling shown alongside as "+STR/x"/"+INT/x" when the move actually uses that
                // divisor - both can apply at once (rare, but the fields allow it).
                string FormatMoveLine(Move move)
                {
                    int flatDamage = weapon.AttackPower ?? move.BaseDamage;
                    bool isMagic = move.DamageType == DamageType.Magical || move.DamageType == DamageType.Light;
                    string statLabel = isMagic ? "Magic" : "Attack";

                    string scaling = "";
                    if (move.StrengthDivisor > 0f)
                        scaling += $" +STR/{move.StrengthDivisor:0.#}";
                    if (move.IntelligenceDivisor > 0f)
                        scaling += $" +INT/{move.IntelligenceDivisor:0.#}";

                    return $" - {move.Name} [{statLabel} {flatDamage}{scaling}, Accuracy {move.BaseAccuracy * 100f:0}%]: {move.Description}";
                }

                if (weapon.Attacks.Count > 0)
                {
                    lines.Add("Attacks:");
                    foreach (var move in weapon.Attacks)
                        lines.Add(FormatMoveLine(move));
                }
                if (weapon.SpecialistAttacks.Count > 0)
                {
                    lines.Add("Specialist Attacks:");
                    foreach (var move in weapon.SpecialistAttacks)
                        lines.Add(FormatMoveLine(move));
                }
            }

            lines.Add(_selectedItem.Description);

            // Each logical line (a move's full-sentence description especially) is wrapped to a
            // fixed max width BEFORE measuring the box, so the panel never has to grow wider than
            // that regardless of content length - unwrapped, a single long description could
            // measure wide enough to push the box's right-anchored X position off the left edge
            // of the screen entirely, rendering it unreadable.
            const float maxTextWidth = 420f;
            var wrappedLines = new List<string>();
            foreach (var line in lines)
                wrappedLines.AddRange(WrapTextAtScale(line, maxTextWidth, 1f));

            // A weapon with several moves (e.g. the Iron Sword's Slash/Pierce/Spin, each now with
            // its own stat line - see FormatMoveLine) can wrap into more lines than fit on screen
            // at once. Rather than let the panel grow past the bottom of the screen (unreadable,
            // same problem the earlier unbounded-width version had, just on the other axis), only
            // a vertical window of wrappedLines is actually drawn - scrolled with the mouse wheel
            // (HandleItemDetailInput), clamped here against the real line/visible-window count,
            // the same split DevConsole's own scroll uses.
            Vector2 textPos = new Vector2(16f, 104f);
            float lineHeight = _font.MeasureString("A").Y;
            int visibleLineCount = Math.Max(1, (int)((viewportSize.Y - textPos.Y - 20f) / lineHeight));
            int maxScroll = Math.Max(0, wrappedLines.Count - visibleLineCount);
            _itemDetailScrollOffset = Math.Clamp(_itemDetailScrollOffset, 0, maxScroll);

            var visibleLines = wrappedLines.Skip(_itemDetailScrollOffset).Take(visibleLineCount).ToList();
            string text = string.Join("\n", visibleLines);
            Vector2 textSize = _font.MeasureString(text);

            var boxRect = new Rectangle((int)(textPos.X - 12), (int)(textPos.Y - 8),
                                        (int)(maxTextWidth + 24), (int)(textSize.Y + 16));
            _spriteBatch.Draw(_whitePixel, boxRect, new Color(0, 0, 0, 200));
            _spriteBatch.DrawString(_font, text, textPos, Color.White);

            if (maxScroll > 0)
            {
                string scrollHint = $"Scroll with mouse wheel ({_itemDetailScrollOffset}/{maxScroll})";
                _spriteBatch.DrawString(_font, scrollHint, new Vector2(textPos.X, boxRect.Bottom + 4), Color.Yellow);
            }

            Rectangle[] actionRects = GetFixedBottomRightMenuOptionRects(viewportSize, _itemDetailActions.Count);
            for (int i = 0; i < _itemDetailActions.Count; i++)
            {
                bool selected = i == _itemDetailIndex;
                Color bg = selected ? new Color(255, 200, 0, 220) : new Color(0, 0, 0, 200);
                Color textColor = selected ? Color.Black : Color.White;

                _spriteBatch.Draw(_whitePixel, actionRects[i], bg);
                _spriteBatch.DrawString(_font, _itemDetailActions[i],
                    new Vector2(actionRects[i].X + 8, actionRects[i].Y + 6), textColor);
            }
        }

        private void DrawMapViewIndicator(Vector2 viewportSize) => DrawBottomHint(viewportSize, "Press E to resume");

        /// <summary>A short instructional line, centered near the bottom of the screen.</summary>
        private void DrawBottomHint(Vector2 viewportSize, string text)
        {
            Vector2 textSize = _font.MeasureString(text);
            Vector2 textPos = new Vector2((viewportSize.X - textSize.X) / 2f, viewportSize.Y - 40f);

            var boxRect = new Rectangle((int)(textPos.X - 16), (int)(textPos.Y - 8),
                                        (int)(textSize.X + 32), (int)(textSize.Y + 16));
            _spriteBatch.Draw(_whitePixel, boxRect, new Color(0, 0, 0, 200));
            _spriteBatch.DrawString(_font, text, textPos, Color.Yellow);
        }

        /// <summary>
        /// Damage/damage-type/crit/hit%/affliction tooltip for _pendingMove against `target`,
        /// shown while targeting mode has it hovered - this fires identically for a weapon
        /// attack and a damage-dealing spell (both route through the same _pendingMove/
        /// _targetingModeActive mechanism - see HandleAttackMenuInput and the Cards menu's cast
        /// path), so one tooltip covers both. Everything here reuses the exact same Move/
        /// AttackResolver formulas the actual attack resolves with, just without rolling or
        /// spending anything.
        /// </summary>
        private void DrawAttackPreview(BaseUnit target, Vector2 viewportSize)
        {
            var attackerHex = _hexGrid.WorldToHex(_playerUnit.Position);
            var targetHex = _hexGrid.WorldToHex(target.Position);
            int distance = _hexGrid.GetDistance(attackerHex.col, attackerHex.row, targetHex.col, targetHex.row);

            int damage = _pendingMove.GetDamage(_playerUnit, target, _pendingSourceWeapon);
            float hitChance = _pendingMove.GetHitChance(_playerUnit, target, _pendingSourceWeapon, distance);

            bool isBackstab = Move.IsBackstab(_hexGrid, _playerUnit, target);
            string critLine = _pendingMove.IsGuaranteedCrit(isBackstab)
                ? "Crit: 100% (backstab)"
                : $"Crit: {_pendingMove.CritChance * 100f:0}%";

            string damageTypeLabel = _pendingMove.DamageType.IsPhysical() ? "Physical" : "Magical";

            var lines = new List<string>
            {
                $"Target: {target.Name}",
                $"Damage: {damage} ({damageTypeLabel})",
                $"Hit: {hitChance * 100f:0}%",
                critLine,
            };

            if (_pendingMove.InflictsStatusEffect != null)
            {
                float statusChance = _pendingMove.GetStatusEffectChance(_playerUnit, target) * 100f;
                lines.Add($"{_pendingMove.InflictsStatusEffect}: {statusChance:0}%");
            }

            if (_pendingMove.CanInflictKnockdown)
            {
                float knockdownChance = _pendingMove.GetKnockdownChance(_playerUnit, target) * 100f;
                lines.Add($"Knocked Down: {knockdownChance:0}%");
            }

            string text = string.Join("\n", lines);
            Vector2 textSize = _font.MeasureString(text);
            Vector2 textPos = new Vector2(viewportSize.X - textSize.X - 24f, 16f);

            var boxRect = new Rectangle((int)(textPos.X - 12), (int)(textPos.Y - 8),
                                        (int)(textSize.X + 24), (int)(textSize.Y + 16));
            _spriteBatch.Draw(_whitePixel, boxRect, new Color(0, 0, 0, 200));
            _spriteBatch.DrawString(_font, text, textPos, Color.White);
        }

        /// <summary>
        /// The "simple box" opened by clicking a unit (see HandleMapControls) - unlike the
        /// compact hover badges (DrawUnitStatBonuses), which only show the net flat DEF/RES
        /// number, this lists exactly where each current buff/debuff comes from: a named
        /// StatusEffect (a spell, Guard, ...), a carried item's passive bonus, or an active
        /// health shield. Same plain dark-box-plus-text style as DrawAttackPreview/DrawBottomHint.
        /// </summary>
        private void DrawUnitDetailBox(BaseUnit unit, Vector2 viewportSize)
        {
            var lines = new List<string> { unit.Name };
            lines.AddRange(GetUnitStatLines(unit));

            lines.Add(new string('-', 28));
            lines.Add("Active Effects:");
            var sourceLines = GetBuffDebuffSourceLines(unit);
            lines.AddRange(sourceLines.Count > 0 ? sourceLines : new List<string> { "None" });

            string text = string.Join("\n", lines);
            Vector2 textSize = _font.MeasureString(text);
            Vector2 textPos = new Vector2((viewportSize.X - textSize.X) / 2f, 100f);

            var boxRect = new Rectangle((int)(textPos.X - 14), (int)(textPos.Y - 10),
                                        (int)(textSize.X + 28), (int)(textSize.Y + 20));
            _spriteBatch.Draw(_whitePixel, boxRect, new Color(15, 15, 20, 230));
            _spriteBatch.DrawString(_font, text, textPos, Color.White);
        }

        /// <summary>
        /// The unit detail box's stat sheet - Race ("type"), Class (omitted for a classless
        /// summon, e.g. the Dusk Roachlin), Level (a display placeholder - see BaseUnit.Level,
        /// no leveling system exists yet), HP/MP/AP, and every combat stat. STR/ACC/INT/Evasion
        /// show the Effective* value (racial + any active status bonus already folded in) and
        /// DEF/RES show TotalDefense/TotalResistance (same inclusions, pre-diminishing-curve raw
        /// armor rating) rather than the raw base field, so a unit currently buffed reads its
        /// real current stat here - the Active Effects breakdown right below explains why it's
        /// elevated, rather than this duplicating that explanation.
        /// </summary>
        private List<string> GetUnitStatLines(BaseUnit unit)
        {
            var lines = new List<string>();

            // Which of the 4 player teams this unit belongs to (by its team root's HeroClass -
            // see GetTeamOutlineColor, the same lookup the on-screen outline color uses, so this
            // line and that outline always agree). Omitted entirely for a free-standing, untamed
            // Wild creature - it isn't on anyone's team yet, same reasoning as its missing outline.
            if (GetTeamOutlineColor(unit).HasValue)
                lines.Add($"Faction: {GetTeamRoot(unit).Class}");

            lines.Add($"Type: {unit.Race}");

            if (unit.Class != HeroClass.None)
                lines.Add($"Class: {unit.Class}");

            lines.Add($"Level: {unit.Level}");
            lines.Add($"HP: {unit.HP}/{unit.EffectiveMaxHP}    MP: {unit.CurrentMP}/{unit.MaxMP}    AP: {unit.CurrentAP}/{unit.MaxAP}");
            lines.Add($"STR: {unit.EffectiveStrength}    INT: {unit.EffectiveIntelligence}    DEF: {unit.TotalDefense}    RES: {unit.TotalResistance}");
            lines.Add($"ACC: {unit.EffectiveAccuracy}    EVA: {unit.EffectiveEvasion}    SPD: {unit.EffectiveSpeed}");

            return lines;
        }

        /// <summary>
        /// One line per active buff/debuff SOURCE on `unit`, for DrawUnitDetailBox - every
        /// StatusEffect by name (spells, afflictions, and Guard's own zero-stat marker, which
        /// gets special-cased since its real bonus lives in GetGuardBonusPercent rather than on
        /// the effect itself - see ApplyGuard's doc comment), plus the passive DEF/RES bonus from
        /// whichever shield is carried (not a StatusEffect at all, so it needs its own line), plus
        /// any active health shield (ShieldPoints).
        /// </summary>
        private List<string> GetBuffDebuffSourceLines(BaseUnit unit)
        {
            var lines = new List<string>();

            foreach (var effect in unit.StatusEffects)
                lines.Add(DescribeStatusEffectSource(unit, effect));

            var passiveShield = unit.Inventory
                .Where(w => w.ShieldPassiveDefResBonusPercent.HasValue)
                .OrderByDescending(w => w.ShieldPassiveDefResBonusPercent.Value)
                .FirstOrDefault();
            if (passiveShield != null)
                lines.Add($"{passiveShield.Name} (item): +{passiveShield.ShieldPassiveDefResBonusPercent.Value * 100f:0}% DEF/RES (passive)");

            if (unit.ShieldPoints > 0)
                lines.Add($"Shield: {unit.ShieldPoints} damage absorbed before HP");

            return lines;
        }

        private string DescribeStatusEffectSource(BaseUnit unit, StatusEffect effect)
        {
            if (effect.Name == "Guarding")
                return $"Guard: +{unit.GetGuardBonusPercent():0}% DEF/RES";

            var parts = GetStatusEffectDescriptorParts(unit, effect);
            string summary = parts.Count > 0 ? string.Join(", ", parts) : "no stat effect";
            string duration = effect.TurnsRemaining < 0 ? "" : $" ({effect.TurnsRemaining} turn{(effect.TurnsRemaining == 1 ? "" : "s")} left)";
            return $"{effect.Name}: {summary}{duration}";
        }

        /// <summary>
        /// Every short, human-readable descriptor a StatusEffect's own active fields produce
        /// right now (e.g. "+3 STR", "-40% ACC", "-5% HP/turn") - shared by DescribeStatusEffectSource
        /// (the unit detail box) and DrawUnitAfflictions (the on-screen affliction row), so the
        /// two can never show different numbers for the same effect. "Guarding" is handled by
        /// DescribeStatusEffectSource's own special case before this is ever called for it;
        /// "Knocked Down" is handled HERE instead (by name) since its real -75% accuracy penalty
        /// is a flat multiplier baked directly into Move.GetHitChance, not a StatusEffect field at
        /// all - this is purely a readable mirror of that, not a second source of truth for it.
        /// </summary>
        private List<string> GetStatusEffectDescriptorParts(BaseUnit unit, StatusEffect effect)
        {
            var parts = new List<string>();
            if (effect.Name == "Knocked Down")
                parts.Add("-75% ACC");

            if (effect.StrengthBonus != 0) parts.Add($"{SignedNumber(effect.StrengthBonus)} STR");
            if (effect.AccuracyBonus != 0) parts.Add($"{SignedNumber(effect.AccuracyBonus)} ACC");
            if (effect.DefenseBonus != 0) parts.Add($"{SignedNumber(effect.DefenseBonus)} DEF");
            if (effect.ResistanceBonus != 0) parts.Add($"{SignedNumber(effect.ResistanceBonus)} RES");
            if (effect.IntelligenceBonus != 0) parts.Add($"{SignedNumber(effect.IntelligenceBonus)} INT");
            if (effect.MaxHPBonus != 0) parts.Add($"{SignedNumber(effect.MaxHPBonus)} Max HP");
            if (effect.AccuracyReductionPercent > 0f) parts.Add($"-{effect.AccuracyReductionPercent * 100f:0}% ACC");
            if (effect.ZeroesEvasion) parts.Add($"-{unit.Evasion} EVA");
            if (effect.HealPercentMaxHPPerTurn > 0f) parts.Add($"heals {effect.HealPercentMaxHPPerTurn * 100f:0}% max HP/turn");
            if (effect.DamagePercentMaxHPPerTick > 0f) parts.Add($"-{effect.DamagePercentMaxHPPerTick * 100f:0}% HP/turn");
            if (effect.DamagePercentMaxHPPerAction > 0f) parts.Add($"-{effect.DamagePercentMaxHPPerAction * 100f:0}% HP/action");
            if (effect.RestrictsActions) parts.Add("restricts actions");
            if (effect.PreventsMovement) parts.Add("no movement");

            return parts;
        }

        private static string SignedNumber(int value) => value > 0 ? $"+{value}" : value.ToString();

        private Color GetTileColor(string tileType)
        {
            return tileType switch
            {
                "grass" => new Color(76, 175, 80),       // Medium green
                "water" => new Color(33, 150, 243),      // Blue
                "mountain" => new Color(158, 158, 158),  // Gray
                "forest" => new Color(27, 94, 32),       // Dark green
                "desert" => new Color(255, 193, 7),      // Amber/gold
                "stone" => new Color(117, 117, 117),     // Dark gray
                "dirt" => new Color(165, 42, 42),        // Brown
                _ => new Color(128, 128, 128)            // Default gray
            };
        }

        private void DrawHexFilled(Vector2 center, Color fillColor, Color outlineColor)
        {
            var vertices = _hexGrid.GetHexVertices(center);

            // Draw filled hex with triangles
            for (int i = 0; i < 6; i++)
            {
                int nextI = (i + 1) % 6;
                // Draw triangle from center to edge
                DrawFilledTriangle(center, vertices[i], vertices[nextI], fillColor);
            }

            // Draw outline
            for (int i = 0; i < 6; i++)
            {
                int nextI = (i + 1) % 6;
                DrawLineSimple(vertices[i], vertices[nextI], outlineColor);
            }
        }

        /// <summary>
        /// Draw a small arrow inside the unit's hex tile pointing toward whichever neighboring
        /// tile its Facing points at - a stand-in until sprites have real facing artwork.
        /// </summary>
        /// <summary>
        /// HP bar above a unit's sprite: a dark background sized to MaxHP, filled HP on top
        /// (red to green as it drops), and - whenever ShieldPoints is active - a blue segment
        /// appended past the MaxHP mark, extending the bar's total width rather than overlapping
        /// the HP portion. A unit at 50/100 HP with a 20-point shield reads as [50 filled][50
        /// dark/missing][20 blue], total visual width 120; a unit at full HP with the same
        /// shield just appends the 20 blue points past the already-full bar.
        /// </summary>
        /// <summary>Draws a centered row of icon+signed-value badges above `unit` - one per stat an active StatusEffect is currently pushing off base (see BaseUnit.GetActiveStatBonuses): a sword for STR, a book for INT, a shield for DEF, a ward rune for RES, a crosshair for ACC. Positioned above the StatusEffect name list (see the "Draw units" loop) so the two never overlap. The caller decides WHETHER to show this for a given unit (always for the player/their summons, hover-only for anyone else) - this just draws it once asked to.</summary>
        private void DrawUnitStatBonuses(BaseUnit unit)
        {
            if (_font == null)
                return;

            // Every stat shows ONE merged flat ("+N", never a "%") number - a raw StatusEffect
            // bonus (e.g. Brace's +4 DEF) and Guard/a shield's passive bonus (both really a flat
            // MITIGATION PERCENTAGE added after the diminishing-returns curve, not a raw stat -
            // see ApplyGuard's own doc comment) used to show as two differently-formatted entries
            // under the same icon ("+4" next to "+20%"), which read as the same kind of number
            // but weren't. BaseUnit.GetFlatDefenseBonusFromShieldAndGuard/...Resistance... converts
            // that percentage into the equivalent raw stat points FOR THIS UNIT'S OWN CURRENT
            // STATS (inverting the same curve), so two units with different base Defense show
            // different numbers for the identical "+20%" Guard bonus, rather than a generic figure
            // that reads the same for everyone regardless of their actual build.
            var statOrder = new[] { "STR", "ACC", "DEF", "RES", "INT" };
            var merged = new Dictionary<string, int>();
            foreach (var (statCode, bonus) in unit.GetActiveStatBonuses())
                merged[statCode] = bonus;

            int defExtra = unit.GetFlatDefenseBonusFromShieldAndGuard();
            if (defExtra != 0)
                merged["DEF"] = merged.GetValueOrDefault("DEF") + defExtra;

            int resExtra = unit.GetFlatResistanceBonusFromShieldAndGuard();
            if (resExtra != 0)
                merged["RES"] = merged.GetValueOrDefault("RES") + resExtra;

            // (StatCode for the icon, the label text, whether it reads as a gain for coloring).
            var entries = new List<(string StatCode, string Label, bool Positive)>();
            foreach (var statCode in statOrder)
            {
                if (merged.TryGetValue(statCode, out int bonus) && bonus != 0)
                    entries.Add((statCode, (bonus > 0 ? "+" : "") + bonus, bonus > 0));
            }

            if (entries.Count == 0)
                return;

            const float iconSize = 14f;
            const float iconTextGap = 2f;
            const float entrySpacing = 4f;
            const float textScale = 0.3f;

            var labels = new string[entries.Count];
            float totalWidth = 0f;
            for (int i = 0; i < entries.Count; i++)
            {
                labels[i] = entries[i].Label;
                totalWidth += iconSize + iconTextGap + _font.MeasureString(labels[i]).X * textScale + entrySpacing;
            }
            totalWidth -= entrySpacing;

            Vector2 origin = unit.Position - new Vector2(totalWidth / 2f, 92f);
            float x = origin.X;

            for (int i = 0; i < entries.Count; i++)
            {
                if (_statIcons.TryGetValue(entries[i].StatCode, out var icon))
                {
                    _spriteBatch.Draw(icon, new Vector2(x, origin.Y), null, Color.White, 0f,
                        Vector2.Zero, iconSize / icon.Width, SpriteEffects.None, 0f);
                }
                x += iconSize + iconTextGap;

                Color textColor = entries[i].Positive ? new Color(120, 255, 140) : new Color(255, 110, 110);
                _spriteBatch.DrawString(_font, labels[i], new Vector2(x, origin.Y + 1f), textColor, 0f,
                    Vector2.Zero, textScale, SpriteEffects.None, 0f);
                x += _font.MeasureString(labels[i]).X * textScale + entrySpacing;
            }
        }

        /// <summary>
        /// Debuff/affliction row, drawn directly under DrawUnitStatBonuses' own buff row (92px
        /// above the unit - this one starts at 84px, just below it, growing downward one line per
        /// active affliction, toward the now-buffs-only name-list line at 68px) - on request, so
        /// debuffs read as a visually separate row from buffs
        /// rather than mixed into the same line, in red text (also on request, "so it's clearer").
        /// One line per active Affliction-type StatusEffect (Buffs, e.g. Pinning, never show
        /// here - that's what the row above is for): "{Name} [{comma-separated descriptor
        /// parts}]" via the same GetStatusEffectDescriptorParts the unit detail box's own
        /// DescribeStatusEffectSource uses, so the two never drift apart - a flat "-STR 3" style
        /// entry per nonzero field (stat bonuses, MaxHPBonus, AccuracyReductionPercent,
        /// ZeroesEvasion, the %HP ticks, PreventsMovement, ...), falling back to the bare name
        /// with no brackets at all if an affliction happens to carry none right now (e.g.
        /// Summoning Curse). Same showStatBonuses visibility gating as the buff row it sits under.
        /// Supersedes an earlier version of this that only ever showed a Bleed-type affliction
        /// via a dedicated blood-drop icon - this one is fully generic (so it already covers
        /// Weak Bleed too, via its own DamagePercentMaxHPPerTick/PerAction parts) and was asked
        /// for specifically because Knocked Down had NO indicator anywhere on screen at all.
        /// </summary>
        private void DrawUnitAfflictions(BaseUnit unit)
        {
            if (_font == null)
                return;

            var afflictions = unit.StatusEffects.Where(e => e.Type == StatusEffectType.Affliction).ToList();
            if (afflictions.Count == 0)
                return;

            const float textScale = 0.32f;
            const float lineSpacing = 2f;

            float y = unit.Position.Y - 84f;
            foreach (var effect in afflictions)
            {
                var parts = GetStatusEffectDescriptorParts(unit, effect);
                string line = parts.Count > 0 ? $"{effect.Name} [{string.Join(", ", parts)}]" : effect.Name;

                Vector2 size = _font.MeasureString(line) * textScale;
                Vector2 pos = new Vector2(unit.Position.X - size.X / 2f, y);
                _spriteBatch.DrawString(_font, line, pos, Color.Red, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
                y += size.Y + lineSpacing;
            }
        }

        private void DrawUnitHealthBar(BaseUnit unit)
        {
            const float barWidth = 50f;
            const float barHeight = 6f;
            float pixelsPerPoint = barWidth / Math.Max(1, unit.EffectiveMaxHP);

            float filledWidth = unit.HP * pixelsPerPoint;
            float missingWidth = (unit.EffectiveMaxHP - unit.HP) * pixelsPerPoint;
            float shieldWidth = unit.ShieldPoints * pixelsPerPoint;

            Vector2 barPos = unit.Position - new Vector2(barWidth / 2f, 60f);

            // Dark background spans HP's own range only (filled + missing) - the shield segment
            // gets its own distinct blue block appended after it, not folded into this backing.
            _spriteBatch.Draw(_whitePixel,
                new Rectangle((int)barPos.X, (int)barPos.Y, (int)(filledWidth + missingWidth), (int)barHeight),
                new Color(35, 35, 35, 220));

            Color hpColor = Color.Lerp(Color.Red, Color.LimeGreen, unit.HP / (float)Math.Max(1, unit.EffectiveMaxHP));
            if (filledWidth > 0f)
            {
                _spriteBatch.Draw(_whitePixel,
                    new Rectangle((int)barPos.X, (int)barPos.Y, (int)filledWidth, (int)barHeight),
                    hpColor);
            }

            if (shieldWidth > 0f)
            {
                _spriteBatch.Draw(_whitePixel,
                    new Rectangle((int)(barPos.X + filledWidth + missingWidth), (int)barPos.Y, (int)shieldWidth, (int)barHeight),
                    Color.DeepSkyBlue);
            }
        }

        private void DrawFacingIndicator(BaseUnit unit)
        {
            var (col, row) = _hexGrid.WorldToHex(unit.Position);
            var (neighborCol, neighborRow) = _hexGrid.GetNeighborCoords(col, row, unit.Facing);

            Vector2 tileCenter = _hexGrid.HexToWorld(col, row);
            Vector2 neighborCenter = _hexGrid.HexToWorld(neighborCol, neighborRow);
            Vector2 direction = Vector2.Normalize(neighborCenter - tileCenter);

            DrawArrowhead(unit.Position + direction * 18f, direction, Color.Cyan, 6f);
        }

        /// <summary>
        /// No real portal sprite exists yet (see Units.Relics.Portal), so a simple placeholder
        /// stands in: 3 concentric violet discs, decreasing radius and increasing opacity drawn
        /// on top of each other, approximating a glowing swirl - same "procedural, not an actual
        /// asset" approach the Spell/Summon cards' own placeholder art already uses elsewhere.
        /// </summary>
        private void DrawPortalPlaceholder(Vector2 position)
        {
            for (int ring = 3; ring >= 1; ring--)
            {
                float radius = ring * 9f;
                Color discColor = new Color(150, 70, 230, 90 + (4 - ring) * 50);
                const int segments = 12;
                Vector2 previous = position + new Vector2(radius, 0f);
                for (int i = 1; i <= segments; i++)
                {
                    float angle = MathHelper.TwoPi * i / segments;
                    Vector2 next = position + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * radius;
                    DrawFilledTriangle(position, previous, next, discColor);
                    previous = next;
                }
            }
        }

        /// <summary>A small filled triangle pointing along direction, tip at the given point.</summary>
        private void DrawArrowhead(Vector2 tip, Vector2 direction, Color color, float size = 8f)
        {
            Vector2 perpendicular = new Vector2(-direction.Y, direction.X) * size;
            Vector2 backLeft = tip - direction * size + perpendicular;
            Vector2 backRight = tip - direction * size - perpendicular;

            DrawFilledTriangle(tip, backLeft, backRight, color);
        }

        /// <summary>A red X centered on the given world position.</summary>
        private void DrawXMarker(Vector2 worldPos)
        {
            const float size = 10f;
            DrawLineSimple(worldPos + new Vector2(-size, -size), worldPos + new Vector2(size, size), Color.Red);
            DrawLineSimple(worldPos + new Vector2(-size, size), worldPos + new Vector2(size, -size), Color.Red);
        }

        /// <summary>
        /// Highlight every tile in _reachableTiles: affordable ones (AP cost within the unit's
        /// current AP) in blue, everything beyond that in red - both labeled with their AP cost.
        /// </summary>
        private void DrawMovementRange()
        {
            if (_font == null)
                return;

            foreach (var (tile, steps) in _reachableTiles)
            {
                int apCost = (int)Math.Ceiling(steps.tiles / (float)_playerUnit.TilesPerAP) + steps.waterTiles;
                bool affordable = apCost <= _playerUnit.CurrentAP;

                // Tiles keep their normal terrain color - only the AP cost is overlaid, in red
                // when the tile is out of reach.
                Vector2 worldPos = _hexGrid.HexToWorld(tile.col, tile.row);
                string costText = apCost.ToString();
                Vector2 textSize = _font.MeasureString(costText);
                _spriteBatch.DrawString(_font, costText, worldPos - textSize / 2f,
                    affordable ? Color.White : Color.Red);
            }
        }

        /// <summary>
        /// Draw a live path preview from the player's unit to whichever tile the mouse is over:
        /// a trail of lines through each tile center, ending in an arrowhead. If the hovered
        /// tile itself has no path (off-grid, impassable, or occupied), the trail instead runs
        /// to the closest reachable neighbor of it and an X is drawn at the hovered tile.
        /// </summary>
        private void DrawMovementPathPreview(Vector2 viewportSize)
        {
            var mouseState = Mouse.GetState();
            var hoveredHex = _renderer.GetHexAtScreenPos(mouseState.X, mouseState.Y, viewportSize);

            var start = _hexGrid.WorldToHex(_playerUnit.Position);
            var passageBlocking = GetPassageBlockingTiles(_playerUnit);
            var destinationBlocking = GetDestinationBlockingTiles(_playerUnit);

            var path = Pathfinder.FindPath(_hexGrid, _map, start, hoveredHex, _playerUnit.TilesPerAP, passageBlocking, destinationBlocking);
            Vector2? xMarkerPos = null;

            if (path == null)
            {
                List<(int col, int row)> bestPath = null;
                foreach (var neighbor in _hexGrid.GetNeighbors(hoveredHex.col, hoveredHex.row))
                {
                    var candidate = Pathfinder.FindPath(_hexGrid, _map, start, neighbor, _playerUnit.TilesPerAP, passageBlocking, destinationBlocking);
                    if (candidate != null && (bestPath == null || candidate.Count < bestPath.Count))
                        bestPath = candidate;
                }

                if (bestPath == null)
                    return; // nothing reachable anywhere near the hovered tile either

                path = bestPath;
                xMarkerPos = _hexGrid.HexToWorld(hoveredHex.col, hoveredHex.row);
            }

            if (path.Count == 0 && xMarkerPos == null)
                return; // hovering the unit's own tile - nothing to preview

            Vector2 previous = _playerUnit.Position;
            foreach (var (col, row) in path)
            {
                Vector2 point = _hexGrid.HexToWorld(col, row);
                DrawLineSimple(previous, point, Color.White);
                previous = point;
            }

            if (xMarkerPos.HasValue)
            {
                DrawXMarker(xMarkerPos.Value);
            }
            else
            {
                Vector2 tip = _hexGrid.HexToWorld(path[^1].col, path[^1].row);
                Vector2 prevPoint = path.Count > 1
                    ? _hexGrid.HexToWorld(path[^2].col, path[^2].row)
                    : _playerUnit.Position;
                Vector2 direction = Vector2.Normalize(tip - prevPoint);
                DrawArrowhead(tip, direction, Color.White);
            }
        }

        private void DrawFilledTriangle(Vector2 p1, Vector2 p2, Vector2 p3, Color color)
        {
            // Simple triangle fill using line drawing (inefficient but works for now)
            var minY = (int)Math.Min(p1.Y, Math.Min(p2.Y, p3.Y));
            var maxY = (int)Math.Max(p1.Y, Math.Max(p2.Y, p3.Y));

            for (int y = minY; y <= maxY; y++)
            {
                var xinters = new List<float>();

                // Find intersections with each edge
                IntersectEdge(p1, p2, y, xinters);
                IntersectEdge(p2, p3, y, xinters);
                IntersectEdge(p3, p1, y, xinters);

                if (xinters.Count >= 2)
                {
                    xinters.Sort();
                    for (int i = 0; i < xinters.Count - 1; i += 2)
                    {
                        int x1 = (int)xinters[i];
                        int x2 = (int)xinters[i + 1];
                        for (int x = x1; x <= x2; x++)
                        {
                            _spriteBatch.Draw(_whitePixel, new Vector2(x, y), color);
                        }
                    }
                }
            }
        }

        private void IntersectEdge(Vector2 p1, Vector2 p2, int y, List<float> xinters)
        {
            if ((p1.Y <= y && p2.Y >= y) || (p2.Y <= y && p1.Y >= y))
            {
                if (Math.Abs(p2.Y - p1.Y) > 0.001f)
                {
                    float x = p1.X + (y - p1.Y) * (p2.X - p1.X) / (p2.Y - p1.Y);
                    xinters.Add(x);
                }
            }
        }

        private void DrawLineSimple(Vector2 start, Vector2 end, Color color) => DrawLineSimple(start, end, color, 1f);

        private void DrawLineSimple(Vector2 start, Vector2 end, Color color, float thickness)
        {
            float angle = (float)Math.Atan2(end.Y - start.Y, end.X - start.X);
            float length = Vector2.Distance(start, end);

            _spriteBatch.Draw(_whitePixel, start, null, color, angle, Vector2.Zero,
                            new Vector2(length, thickness), SpriteEffects.None, 0f);
        }

        /// <summary>A quick fading "X" swipe near the target - the melee attack effect (see SpawnAttackAnimations). t goes 0 (just landed) to 1 (fully faded).</summary>
        private void DrawSlashEffect(Vector2 target, float t)
        {
            float alpha = 1f - t;
            float size = 16f;
            Color color = Color.White * alpha;

            DrawLineSimple(target + new Vector2(-size, -size), target + new Vector2(size, size), color, 3f);
            DrawLineSimple(target + new Vector2(-size, size), target + new Vector2(size, -size), color, 3f);
        }

        /// <summary>A small projectile traveling straight from attacker to target, with a soft glow and a short motion trail behind it - the ranged attack effect (see SpawnAttackAnimations). t goes 0 (just fired) to 1 (arrived). color distinguishes a magical bolt (e.g. Arcane Missile) from a physical one (arrow/bolt/bullet). Sized to still read clearly at a fully zoomed-out camera or over a short hop (e.g. between two adjacent units on Test Mode's 3x3 map) - a single small square at the old size could get lost in either case.</summary>
        private void DrawShootingEffect(Vector2 from, Vector2 to, float t, Color color)
        {
            Vector2 pos = Vector2.Lerp(from, to, t);

            // Trail: a fading line stretching back toward the start, capped so it never
            // overshoots `from` even on a very short hop.
            float trailT = MathHelper.Clamp(t - 0.15f, 0f, 1f);
            Vector2 trailPos = Vector2.Lerp(from, to, trailT);
            DrawLineSimple(trailPos, pos, color * 0.5f, 3f);

            const float glowSize = 20f;
            var glowRect = new Rectangle((int)(pos.X - glowSize / 2f), (int)(pos.Y - glowSize / 2f), (int)glowSize, (int)glowSize);
            _spriteBatch.Draw(_whitePixel, glowRect, color * 0.45f);

            const float size = 9f;
            var rect = new Rectangle((int)(pos.X - size / 2f), (int)(pos.Y - size / 2f), (int)size, (int)size);
            _spriteBatch.Draw(_whitePixel, rect, color);
        }

        // Dynamic regions on Content/imgs/Cards/Spells/SpellCard.png, as fractions of the card's
        // own width/height - measured directly from the template's pixel layout (1500x2100), so
        // they scale correctly no matter what size the card is drawn at.
        private static readonly (float x0, float y0, float x1, float y1) CardNameBarRegion = (0.168f, 0.091f, 0.767f, 0.132f);
        private static readonly (float x0, float y0, float x1, float y1) CardCostCircleRegion = (0.795f, 0.04f, 0.963f, 0.16f);
        // Re-measured directly off SpellCard.png via solid-run edge detection (the original
        // naive "any near-white pixel" scan was contaminated by background sparkle decoration
        // and the cost circle bleeding into the bounding box, giving a region that overshot
        // into the name bar). Cross-checked at multiple rows/columns for consistency.
        private static readonly (float x0, float y0, float x1, float y1) CardArtRegion = (0.098f, 0.137f, 0.884f, 0.693f);
        private static readonly (float x0, float y0, float x1, float y1) CardTypeBarRegion = (0.348f, 0.719f, 0.899f, 0.755f);
        private static readonly (float x0, float y0, float x1, float y1) CardDescriptionRegion = (0.1f, 0.729f, 0.886f, 0.947f);
        private static readonly (float x0, float y0, float x1, float y1) CardDamageCircleRegion = (0.829f, 0.869f, 0.952f, 0.952f);

        /// <summary>
        /// Draw a spell card into destRect: template + art + every dynamic value (name, MP
        /// cost, class line, description, and computed damage for the given caster) laid out
        /// via the fractional regions above - nothing about a specific card is hardcoded here,
        /// so any SpellCard renders correctly through this same method.
        /// </summary>
        private void DrawSpellCard(SpellCard card, BaseUnit caster, Rectangle destRect)
        {
            if (_spellCardTemplate == null)
                return;

            // The art is baked directly into the template's own pixel buffer (see
            // GetCardComposite) rather than drawn as a separate layer on top of or under the
            // template, so there's a single finished texture here - no runtime layering that
            // could leave a seam between the art and its frame.
            Texture2D composite = GetCardComposite(card) ?? _spellCardTemplate;
            _spriteBatch.Draw(composite, destRect, Color.White);

            if (_font == null)
                return;

            // Red for either reason a cast would be refused right now: not enough current MP,
            // or (Leveling System) the cost itself exceeds the caster's Level - see
            // TryCastSelectedCard's own gate, which this just mirrors visually.
            bool canAffordMP = caster.CurrentMP >= card.Effect.MPCost && card.Effect.MPCost <= caster.Level;
            DrawTextCentered(card.Name, FractionalRect(destRect, CardNameBarRegion), Color.White);
            DrawTextCentered(card.Effect.MPCost.ToString(), FractionalRect(destRect, CardCostCircleRegion), canAffordMP ? Color.Black : Color.Red);

            // No longer shows AP cost here (briefly did, for one session) - every spell costs a
            // flat 1 AP now (see SpellCatalog's own header comment), so a per-card number would
            // just repeat the same "1 AP" on all 69 cards with no actual information in it. The
            // rare "0 AP left, can't cast anything" case still surfaces through the normal
            // "Not enough AP/MP for X." combat-log message on a failed cast attempt, same as
            // before AP cost ever got its own display.
            DrawTextCentered(card.ClassLabel, FractionalRect(destRect, CardTypeBarRegion), Color.White);
            DrawWrappedText(card.Description, FractionalRect(destRect, CardDescriptionRegion), Color.Black);

            string headline = GetSpellHeadlineNumber(card.Effect, caster);
            if (headline != null)
            {
                Rectangle damageRect = FractionalRect(destRect, CardDamageCircleRegion);
                DrawWandGlyph(damageRect);
                DrawTextCentered(headline, damageRect, Color.Black);
            }
        }

        /// <summary>
        /// The single "headline number" worth showing in a spell card's damage circle - damage
        /// for an attack spell, otherwise whichever non-attack effect the move actually has
        /// (heal amount, heal %, granted AP), checked in that priority order. Null (draw
        /// nothing) for a spell with no single number to show, e.g. a pure status/utility
        /// effect like Meditate or Conjure Potions - showing "0 damage" on those would read as
        /// a bug rather than as "this spell doesn't deal damage."
        /// </summary>
        private string GetSpellHeadlineNumber(Move move, BaseUnit caster)
        {
            int damage = move.GetDamage(caster);
            if (damage > 0)
                return damage.ToString();
            if (move.HealFlat > 0)
                return move.HealFlat.ToString();
            if (move.HealPercentMaxHP > 0f)
                return $"{(int)(move.HealPercentMaxHP * 100f)}%";
            if (move.GrantedAP > 0)
                return $"+{move.GrantedAP}";
            return null;
        }

        /// <summary>Tooltip text matching whichever field GetSpellHeadlineNumber pulled its number from - null if the card shows no number there (nothing to hover).</summary>
        private string GetSpellHeadlineTooltip(Move move)
        {
            if (move.GetDamage(_playerUnit) > 0)
                return "Damage: damage this spell deals, computed from the caster's stats.";
            if (move.HealFlat > 0)
                return "Heal: flat HP this spell restores.";
            if (move.HealPercentMaxHP > 0f)
                return "Heal: % of max HP restored per turn while active.";
            if (move.GrantedAP > 0)
                return "Bonus AP: extra AP this spell grants.";
            return null;
        }

        // Dynamic regions on Content/imgs/Cards/Summons/SummonCard.png, as fractions of the
        // card's own width/height - measured directly from the template's pixel layout
        // (1463x2048). SummonArtRegion is only a seed point for BuildSummonCardArtMask, not the
        // actual art shape (which has 7 notches down its left edge, clearing the stat bars).
        private static readonly (float x0, float y0, float x1, float y1) SummonNameBarRegion = (0.1777f, 0.0913f, 0.7662f, 0.1309f);
        // The top-left circle (a stone/rock medallion) is pure decoration - the mana cost goes
        // in the top-RIGHT white "star" circle instead, matching the Spell card's cost circle.
        private static readonly (float x0, float y0, float x1, float y1) SummonManaCostCircleRegion = (0.8031f, 0.0430f, 0.9549f, 0.1509f);
        private static readonly (float x0, float y0, float x1, float y1) SummonArtRegion = (0.0984f, 0.1318f, 0.8838f, 0.7007f);
        private static readonly (float x0, float y0, float x1, float y1) SummonAttackBarRegion = (0.1155f, 0.1636f, 0.1989f, 0.2026f);
        private static readonly (float x0, float y0, float x1, float y1) SummonIntelligenceBarRegion = (0.1278f, 0.2422f, 0.1989f, 0.2817f);
        private static readonly (float x0, float y0, float x1, float y1) SummonDefenseBarRegion = (0.1217f, 0.3228f, 0.1989f, 0.3623f);
        private static readonly (float x0, float y0, float x1, float y1) SummonResistanceBarRegion = (0.1148f, 0.4028f, 0.1989f, 0.4424f);
        private static readonly (float x0, float y0, float x1, float y1) SummonAccuracyBarRegion = (0.1306f, 0.4800f, 0.1989f, 0.5195f);
        private static readonly (float x0, float y0, float x1, float y1) SummonEvasionBarRegion = (0.1148f, 0.5630f, 0.1989f, 0.6021f);
        private static readonly (float x0, float y0, float x1, float y1) SummonSpeedBarRegion = (0.1169f, 0.6416f, 0.1989f, 0.6807f);
        private static readonly (float x0, float y0, float x1, float y1) SummonTypeBarRegion = (0.3479f, 0.7188f, 0.8996f, 0.7549f);
        private static readonly (float x0, float y0, float x1, float y1) SummonDescriptionRegion = (0.1251f, 0.7646f, 0.8722f, 0.9287f);
        private static readonly (float x0, float y0, float x1, float y1) SummonDamageBarRegion = (0.1757f, 0.8955f, 0.2604f, 0.9346f);
        private static readonly (float x0, float y0, float x1, float y1) SummonHpCircleRegion = (0.8209f, 0.8545f, 0.9508f, 0.9473f);

        /// <summary>
        /// Draw a summon card into destRect: template + art + stat block (name, mana cost, the
        /// seven icon-labelled stat bars, unit type, description, damage range, and HP) laid out
        /// via the fractional regions above - nothing about a specific card is hardcoded here, so
        /// any SummonCard renders correctly through this same method.
        /// </summary>
        private void DrawSummonCard(SummonCard card, Rectangle destRect)
        {
            if (_summonCardTemplate == null)
                return;

            Texture2D composite = GetSummonCardComposite(card) ?? _summonCardTemplate;
            _spriteBatch.Draw(composite, destRect, Color.White);

            if (_font == null)
                return;

            // Showing the actual total mana this cast will spend, including the Generic Portal
            // surcharge if the player has no portal standing yet - always in red when that
            // surcharge applies (distinct signal from "can't afford," which stays red too but
            // for a different reason) so it reads as "this costs more right now," not a plain
            // affordability warning.
            int portalSurcharge = GetGenericPortalSurcharge(card);
            int displayManaCost = card.ManaCost + portalSurcharge;
            // Leveling System: gated on the card's own ManaCost, not displayManaCost - the
            // Generic Portal surcharge is a one-time bootstrap cost, not part of "how strong a
            // summon is this," so it shouldn't count toward whether Level allows casting it.
            bool canAffordMana = _playerUnit.CurrentMP >= displayManaCost && card.ManaCost <= _playerUnit.Level;
            Color manaCostColor = portalSurcharge > 0 ? Color.Red : (canAffordMana ? Color.Black : Color.Red);
            DrawTextCentered(card.Name, FractionalRect(destRect, SummonNameBarRegion), Color.White);
            DrawTextCentered(displayManaCost.ToString(), FractionalRect(destRect, SummonManaCostCircleRegion), manaCostColor);

            DrawTextCentered(card.Attack.ToString(), FractionalRect(destRect, SummonAttackBarRegion), Color.White);
            DrawTextCentered(card.Intelligence.ToString(), FractionalRect(destRect, SummonIntelligenceBarRegion), Color.White);
            DrawTextCentered(card.Defense.ToString(), FractionalRect(destRect, SummonDefenseBarRegion), Color.White);
            DrawTextCentered(card.Resistance.ToString(), FractionalRect(destRect, SummonResistanceBarRegion), Color.White);
            DrawTextCentered(card.Accuracy.ToString(), FractionalRect(destRect, SummonAccuracyBarRegion), Color.White);
            DrawTextCentered(card.Evasion.ToString(), FractionalRect(destRect, SummonEvasionBarRegion), Color.White);
            DrawTextCentered(card.Speed.ToString(), FractionalRect(destRect, SummonSpeedBarRegion), Color.White);

            DrawTextCentered(card.UnitType, FractionalRect(destRect, SummonTypeBarRegion), Color.White);
            DrawWrappedText(card.Description, FractionalRect(destRect, SummonDescriptionRegion), Color.Black);

            DrawTextCentered(card.DamageRangeLabel, FractionalRect(destRect, SummonDamageBarRegion), Color.White);
            DrawTextCentered(card.HP.ToString(), FractionalRect(destRect, SummonHpCircleRegion), Color.White);
        }

        /// <summary>Draw a single card from the hand (either a SpellCard or a SummonCard) into destRect.</summary>
        private void DrawHandCard(object card, Rectangle destRect)
        {
            if (card is SpellCard spell)
                DrawSpellCard(spell, _playerUnit, destRect);
            else if (card is SummonCard summon)
                DrawSummonCard(summon, destRect);
        }

        /// <summary>Card rect centered in the viewport at a given height (aspect-matched to the card templates) and horizontal offset.</summary>
        private Rectangle CenteredCardRect(Vector2 viewportSize, float cardHeight, float xOffset)
        {
            // Both templates share effectively the same aspect ratio (1500:2100 vs 1463:2048),
            // so one shared ratio is fine for layout purposes regardless of which card this is.
            float cardWidth = cardHeight * (1500f / 2100f);
            return new Rectangle(
                (int)((viewportSize.X - cardWidth) / 2f + xOffset),
                (int)((viewportSize.Y - cardHeight) / 2f),
                (int)cardWidth, (int)cardHeight);
        }

        /// <summary>
        /// The on-screen rect for every card in the hand. Cards overlap around the center like
        /// a physical hand of cards; the focused card is enlarged during drawing.
        /// </summary>
        private List<Rectangle> GetHandCardLayout(Vector2 viewportSize)
        {
            var layout = new List<Rectangle>(_availableHandCards.Count);
            if (_availableHandCards.Count == 0)
                return layout;

            const float cardAspectRatio = 1500f / 2100f;
            float sideMargin = viewportSize.X * 0.025f;
            float availableWidth = viewportSize.X - sideMargin * 2f;
            float cardWidth = Math.Min(150f, availableWidth / (_availableHandCards.Count + 0.35f * (_availableHandCards.Count - 1)));
            float cardHeight = cardWidth / cardAspectRatio;
            float overlapStep = cardWidth * 0.7f;
            float spacing = _availableHandCards.Count == 1
                ? 0f
                : Math.Min(overlapStep, availableWidth / (_availableHandCards.Count - 1));
            float handWidth = cardWidth + spacing * (_availableHandCards.Count - 1);
            float startX = (viewportSize.X - handWidth) / 2f;
            float bottom = viewportSize.Y - 58f;

            for (int i = 0; i < _availableHandCards.Count; i++)
            {
                layout.Add(new Rectangle(
                    (int)(startX + i * spacing),
                    (int)(bottom - cardHeight),
                    (int)cardWidth,
                    (int)cardHeight));
            }

            return layout;
        }

        private Rectangle ScaleCardRect(Rectangle rect, float scale)
        {
            int width = (int)(rect.Width * scale);
            int height = (int)(rect.Height * scale);
            return new Rectangle(
                rect.X - (width - rect.Width) / 2,
                rect.Y - (height - rect.Height),
                width,
                height);
        }

        /// <summary>
        /// Whichever hand card is currently shown big/centered - the mouse-hovered side card if
        /// any, else whatever A/D or a prior click last selected (_highlightedHandCardIndex).
        /// This is also exactly what TryCastSelectedCard casts on Space, so the card the player
        /// is actually looking at is always the one Space plays - no silent mismatch where
        /// hovering a card (without clicking it) shows it big but a different card gets cast.
        /// </summary>
        private int? GetFocusedHandCardIndex(Vector2 viewportSize, Point mousePos)
        {
            var layout = GetHandCardLayout(viewportSize);
            for (int i = layout.Count - 1; i >= 0; i--)
            {
                if (layout[i].Contains(mousePos))
                    return i;
            }
            return _highlightedHandCardIndex;
        }

        /// <summary>
        /// Full-screen-ish hand-of-cards viewer: dims the background and shows the player's
        /// available spell + summon cards as a browsable hand - the selected card centered and
        /// large, with the previous/next cards peeking out smaller to either side (drawn first,
        /// so the selected card overlaps them; a click just selects a card, clicking that SAME
        /// already-selected card again casts it - no time limit between the two clicks, see
        /// HandleCardMenuInput), A/D to browse. Hovering the center card shows a tooltip
        /// explaining whatever number/icon the cursor is over. A "no cards" message shows if
        /// the class has neither spells nor summons available yet.
        /// </summary>
        private void DrawCardMenuOverlay(Vector2 viewportSize)
        {
            _spriteBatch.Draw(_whitePixel, new Rectangle(0, 0, (int)viewportSize.X, (int)viewportSize.Y), new Color(0, 0, 0, 160));

            if (_availableHandCards.Count > 0)
            {
                var layout = GetHandCardLayout(viewportSize);
                Point mousePos = Mouse.GetState().Position;

                int? focusedCardIndex = GetFocusedHandCardIndex(viewportSize, mousePos);
                for (int i = 0; i < _availableHandCards.Count; i++)
                {
                    if (i != focusedCardIndex)
                        DrawHandCard(_availableHandCards[i], layout[i]);
                }

                Rectangle focusedRect = Rectangle.Empty;
                if (focusedCardIndex.HasValue)
                {
                    focusedRect = ScaleCardRect(layout[focusedCardIndex.Value], 2.10f);
                    DrawHandCard(_availableHandCards[focusedCardIndex.Value], focusedRect);
                }

                if (_font != null)
                {
                    string counter = _highlightedHandCardIndex.HasValue
                        ? $"{_highlightedHandCardIndex.Value + 1} / {_availableHandCards.Count}"
                        : $"{_availableHandCards.Count} cards";
                    Vector2 size = _font.MeasureString(counter);
                    _spriteBatch.DrawString(_font, counter, new Vector2((viewportSize.X - size.X) / 2f, viewportSize.Y * 0.04f), Color.White);

                    int drawCost = GetDrawAPCost(_playerUnit);
                    string drawText = $"Draw ({drawCost} AP)";
                    Vector2 drawSize = _font.MeasureString(drawText);
                    _spriteBatch.DrawString(_font, drawText,
                        new Vector2((viewportSize.X - drawSize.X) / 2f, viewportSize.Y * 0.9f),
                        _playerUnit.CurrentAP >= drawCost ? Color.Gold : Color.Gray);
                }

                // Gated on focusedRect (the actually-rendered, 2.1x-enlarged card), not on
                // hoveredCardIndex's small original footprint (layout[i]) - a field like the
                // mana-cost circle sits near the TOP of the card, which is well above that small
                // footprint once the card's enlarged (ScaleCardRect grows entirely upward,
                // anchored to the same bottom edge - see its own doc comment). Gating on the
                // small footprint meant the cursor could never actually be "inside" it while
                // pointing at a field that high up, so no tooltip could ever show for one -
                // reported as "I'm not seeing the tooltip over the summon cost."
                if (focusedCardIndex.HasValue && focusedRect.Contains(mousePos))
                {
                    string tooltip = GetHoveredCardTooltip(_availableHandCards[focusedCardIndex.Value], focusedRect, mousePos);
                    if (tooltip != null)
                        DrawTooltip(tooltip, mousePos, viewportSize);
                }
            }
            else if (_font != null)
            {
                string text = $"{_playerUnit.Class} has no cards available yet.";
                Vector2 size = _font.MeasureString(text);
                _spriteBatch.DrawString(_font, text, (viewportSize - size) / 2f, Color.White);
            }

            // A fresh combat-log message (e.g. "Not enough AP/MP for Mana Shield.", "X can't be
            // cast yet") takes priority over the static control hint, same precedence every
            // other input mode already gives it (see Draw's _combatLogTimer branch) - otherwise
            // this unconditional hint, drawn every frame the menu stays open (which is always,
            // since a failed cast never closes it), would immediately overwrite that message at
            // the exact same screen position (DrawBottomHint paints an opaque box first), making
            // every cast-failure message invisible the entire time it was supposed to show.
            DrawBottomHint(viewportSize,
                _combatLogTimer > 0f && !string.IsNullOrEmpty(_combatLogMessage)
                    ? _combatLogMessage
                    : _availableHandCards.Count > 1
                        ? "A/D to browse - click twice (or Space) to cast - F to draw - E to close"
                        : "Click twice (or Space) to cast - F to draw - E to close");
        }

        /// <summary>
        /// Which tooltip (if any) applies to the field the cursor is over on a hand card,
        /// checked against the same fractional regions the card's numbers are actually drawn
        /// into - so a tooltip can never point at the wrong field.
        /// </summary>
        private string GetHoveredCardTooltip(object card, Rectangle destRect, Point mousePos)
        {
            if (card is SpellCard spellCard)
            {
                if (FractionalRect(destRect, CardCostCircleRegion).Contains(mousePos))
                    return "MP Cost: mana this spell costs to cast.";
                if (FractionalRect(destRect, CardDamageCircleRegion).Contains(mousePos))
                    return GetSpellHeadlineTooltip(spellCard.Effect);
            }
            else if (card is SummonCard summonCard)
            {
                if (FractionalRect(destRect, SummonManaCostCircleRegion).Contains(mousePos))
                {
                    int portalSurcharge = GetGenericPortalSurcharge(summonCard);
                    return portalSurcharge > 0
                        ? $"Mana Cost: {summonCard.ManaCost} + Portal Summon Cost {portalSurcharge} - summons a GENERIC portal first (you have none standing yet)."
                        : "Mana Cost: mana required to summon this creature.";
                }
                if (FractionalRect(destRect, SummonAttackBarRegion).Contains(mousePos))
                    return "Attack: base physical attack power.";
                if (FractionalRect(destRect, SummonIntelligenceBarRegion).Contains(mousePos))
                    return "Intelligence: magic power, and magic-scaling bonus damage.";
                if (FractionalRect(destRect, SummonDefenseBarRegion).Contains(mousePos))
                    return "Defense: reduces incoming physical damage.";
                if (FractionalRect(destRect, SummonResistanceBarRegion).Contains(mousePos))
                    return "Resistance: reduces incoming magic damage.";
                if (FractionalRect(destRect, SummonAccuracyBarRegion).Contains(mousePos))
                    return "Accuracy: chance to hit with attacks.";
                if (FractionalRect(destRect, SummonEvasionBarRegion).Contains(mousePos))
                    return "Evasion: chance to dodge incoming attacks.";
                if (FractionalRect(destRect, SummonSpeedBarRegion).Contains(mousePos))
                    return "Speed: turn order, and tiles moved per AP.";
                if (FractionalRect(destRect, SummonDamageBarRegion).Contains(mousePos))
                    return "Damage Range: the least and most damage this summon's attacks can deal.";
                if (FractionalRect(destRect, SummonHpCircleRegion).Contains(mousePos))
                    return "HP: how much damage this summon can take before fainting.";
            }
            return null;
        }

        /// <summary>Small floating tooltip box, word-wrapped and anchored near the cursor but clamped to stay fully on-screen.</summary>
        private void DrawTooltip(string text, Point anchor, Vector2 viewportSize)
        {
            if (_font == null)
                return;

            const float scale = 0.8f;
            const float maxTextWidth = 260f;
            var lines = WrapTextAtScale(text, maxTextWidth, scale);

            float lineHeight = _font.MeasureString("A").Y * scale;
            float boxWidth = maxTextWidth + 16f;
            float boxHeight = lineHeight * lines.Count + 16f;

            float x = Math.Min(anchor.X + 16f, viewportSize.X - boxWidth - 4f);
            float y = Math.Min(anchor.Y + 16f, viewportSize.Y - boxHeight - 4f);

            _spriteBatch.Draw(_whitePixel, new Rectangle((int)x, (int)y, (int)boxWidth, (int)boxHeight), new Color(20, 20, 20, 230));

            for (int i = 0; i < lines.Count; i++)
            {
                Vector2 pos = new Vector2(x + 8f, y + 8f + i * lineHeight);
                _spriteBatch.DrawString(_font, lines[i], pos, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }
        }

        /// <summary>Convert a fractional region (0-1) into pixel coordinates within a container rect.</summary>
        private Rectangle FractionalRect(Rectangle container, (float x0, float y0, float x1, float y1) region)
        {
            int x = container.X + (int)(region.x0 * container.Width);
            int y = container.Y + (int)(region.y0 * container.Height);
            int w = (int)((region.x1 - region.x0) * container.Width);
            int h = (int)((region.y1 - region.y0) * container.Height);
            return new Rectangle(x, y, w, h);
        }

        /// <summary>Draw text centered in a rect, shrinking it (uniformly) to fit if it's too big.</summary>
        private void DrawTextCentered(string text, Rectangle rect, Color color)
        {
            Vector2 size = _font.MeasureString(text);
            float scale = 1f;
            if (size.X > rect.Width || size.Y > rect.Height)
                scale = Math.Min(rect.Width / size.X, rect.Height / size.Y);

            Vector2 scaledSize = size * scale;
            Vector2 pos = new Vector2(rect.X + (rect.Width - scaledSize.X) / 2f, rect.Y + (rect.Height - scaledSize.Y) / 2f);
            _spriteBatch.DrawString(_font, text, pos, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        /// <summary>Word-wrap text at a given scale to fit maxWidth, without drawing anything.</summary>
        private List<string> WrapTextAtScale(string text, float maxWidth, float scale)
        {
            var words = text.Split(' ');
            var lines = new List<string>();
            string currentLine = "";

            foreach (var word in words)
            {
                string candidate = currentLine.Length == 0 ? word : currentLine + " " + word;
                if (_font.MeasureString(candidate).X * scale > maxWidth && currentLine.Length > 0)
                {
                    lines.Add(currentLine);
                    currentLine = word;
                }
                else
                {
                    currentLine = candidate;
                }
            }
            if (currentLine.Length > 0)
                lines.Add(currentLine);
            return lines;
        }

        /// <summary>
        /// Word-wrap text to fit rect's width, vertically centered as a block, shrinking the
        /// font scale (re-wrapping at each step, since fewer/longer lines fit at smaller scales)
        /// until the whole wrapped block fits within rect's height too - so a longer description
        /// never overflows its box instead of just wrapping and spilling out the bottom.
        /// </summary>
        private void DrawWrappedText(string text, Rectangle rect, Color color)
        {
            float lineHeight = _font.MeasureString("A").Y;
            float scale = 1f;
            var lines = WrapTextAtScale(text, rect.Width, scale);

            while (lineHeight * scale * lines.Count > rect.Height && scale > 0.3f)
            {
                scale -= 0.05f;
                lines = WrapTextAtScale(text, rect.Width, scale);
            }

            float scaledLineHeight = lineHeight * scale;
            float startY = rect.Y + (rect.Height - scaledLineHeight * lines.Count) / 2f;

            for (int i = 0; i < lines.Count; i++)
            {
                Vector2 lineSize = _font.MeasureString(lines[i]) * scale;
                Vector2 pos = new Vector2(rect.X + (rect.Width - lineSize.X) / 2f, startY + i * scaledLineHeight);
                _spriteBatch.DrawString(_font, lines[i], pos, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }
        }

        /// <summary>
        /// A simple placeholder "wand with a sparkle" glyph behind the damage circle's number -
        /// stands in until real card iconography exists.
        /// </summary>
        private void DrawWandGlyph(Rectangle rect)
        {
            Vector2 center = new Vector2(rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f);
            float radius = Math.Min(rect.Width, rect.Height) * 0.45f;
            Color glyphColor = new Color(80, 40, 120, 140);

            Vector2 tip = center + new Vector2(-radius, -radius) * 0.7f;
            Vector2 handle = center + new Vector2(radius, radius) * 0.7f;
            DrawLineSimple(tip, handle, glyphColor);

            float s = radius * 0.35f;
            DrawLineSimple(tip + new Vector2(-s, 0), tip + new Vector2(s, 0), glyphColor);
            DrawLineSimple(tip + new Vector2(0, -s), tip + new Vector2(0, s), glyphColor);
        }

        private void DrawTurnAnnouncement(Vector2 viewportSize)
        {
            const float FADE_IN = 0.3f;
            const float HOLD = 1.4f;
            const float FADE_OUT = 0.3f;

            float t = _turnSystem.TurnAnnouncementElapsed;
            float alpha;
            if (t < FADE_IN)
                alpha = t / FADE_IN;
            else if (t < FADE_IN + HOLD)
                alpha = 1f;
            else
                alpha = 1f - MathHelper.Clamp((t - FADE_IN - HOLD) / FADE_OUT, 0f, 1f);

            string text = $"Turn {_turnSystem.CurrentTurn} - Go!";
            Vector2 textSize = _font.MeasureString(text);
            Vector2 textPos = new Vector2((viewportSize.X - textSize.X) / 2f, 60f);

            var boxRect = new Rectangle((int)(textPos.X - 20), (int)(textPos.Y - 10),
                                        (int)(textSize.X + 40), (int)(textSize.Y + 20));
            _spriteBatch.Draw(_whitePixel, boxRect, new Color(0, 0, 0, (int)(180 * alpha)));

            _spriteBatch.DrawString(_font, text, textPos, Color.White * alpha);
        }

        /// <summary>FFA result screen - the last unit standing (not Fainted) wins, or a draw if everyone faints on the same check.</summary>
        private void DrawMatchOver(Vector2 viewportSize)
        {
            string text = _matchWinner != null ? $"{_matchWinner.Name} Wins!" : "Draw!";
            Vector2 textSize = _font.MeasureString(text) * 2f;
            Vector2 textPos = (viewportSize - textSize) / 2f;

            _spriteBatch.DrawString(_font, text, textPos, Color.Gold, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0f);
        }

        private void DrawConsole()
        {
            const int CONSOLE_HEIGHT = 300;
            int consoleY = GraphicsDevice.Viewport.Height - CONSOLE_HEIGHT;

            // Draw semi-transparent background
            _spriteBatch.Draw(_whitePixel,
                            new Rectangle(0, consoleY, GraphicsDevice.Viewport.Width, CONSOLE_HEIGHT),
                            new Color(0, 0, 0, 200));

            // Draw output - _consoleScrollOffset is lines scrolled up from the bottom (0 = the
            // live/latest view); clamped here against the actual output length so it's safe for
            // it to have grown past what's currently valid (e.g. right after a "clear").
            int y = consoleY + 10;
            var allOutput = _console.GetOutput();
            int maxScroll = Math.Max(0, allOutput.Count - ConsoleVisibleLines);
            _consoleScrollOffset = Math.Clamp(_consoleScrollOffset, 0, maxScroll);
            int skip = Math.Max(0, allOutput.Count - ConsoleVisibleLines - _consoleScrollOffset);
            var output = allOutput.Skip(skip).Take(ConsoleVisibleLines).ToList();
            foreach (var line in output)
            {
                _spriteBatch.DrawString(_font, line, new Vector2(10, y), Color.White);
                y += 20;
            }

            // Draw input prompt
            _spriteBatch.DrawString(_font, "> " + _consoleInput + (DateTime.Now.Millisecond % 1000 < 500 ? "_" : ""),
                                   new Vector2(10, consoleY + CONSOLE_HEIGHT - 25),
                                   Color.LimeGreen);

            // Draw help text - replaced with a "scrolled up" notice whenever not at the bottom,
            // so it's obvious new output won't be visible until scrolling back down.
            string helpText = _consoleScrollOffset > 0
                ? $"-- scrolled up {_consoleScrollOffset} lines (PgDn/mouse wheel to return) --"
                : "Type 'help' for commands | ~ to close | PgUp/PgDn or mouse wheel to scroll";
            _spriteBatch.DrawString(_font, helpText,
                                   new Vector2(10, consoleY + CONSOLE_HEIGHT - 45),
                                   _consoleScrollOffset > 0 ? Color.Yellow : Color.Gray);
        }

        protected override void UnloadContent()
        {
            _renderer.Dispose();
            _whitePixel?.Dispose();
            base.UnloadContent();
        }
    }
}
