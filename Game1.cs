using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SagesOfOzvaram.Maps;
using SagesOfOzvaram.Units;
using SagesOfOzvaram.Units.Heroes;
using SagesOfOzvaram.Units.Summons;
using SagesOfOzvaram.Combat;
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

        // Character select
        private GameState _gameState = GameState.CharacterSelect;

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

        private int _selectedCharacterIndex = 0;  // Sorcerer is the default selection
        private BaseUnit _playerUnit;

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

        // Summon placement (opened for a SummonCard) - click any highlighted tile adjacent to
        // the caster to spawn the summoned creature there; see CastSummon/ExecuteSummon.
        private bool _summonPlacementModeActive = false;
        private HashSet<(int col, int row)> _summonPlacementValidTiles = new HashSet<(int col, int row)>();
        private SummonCard _pendingSummonCard;

        /// <summary>Recomputed every Draw frame while _targetingModeActive - whichever _targetCandidates entry the mouse is currently over, or null. Drives the damage/hit%/crit/affliction preview panel.</summary>
        private BaseUnit _hoveredAttackTarget;

        // Cone aiming (opened instead of _targetingModeActive for a HitsCone move, e.g. Frost
        // Blast) - aimed by clicking any hex to set a direction, not by picking a unit.
        private bool _coneAimingModeActive = false;

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

            // Actual typed characters for the console - KeyboardState (polled in
            // HandleConsoleInput) only reports WHICH keys are held, not what character a key
            // produces (layout/shift-dependent), so without this nothing ever appeared when
            // typing. TextInput is keyboard-layout-aware and fires once per keystroke.
            Window.TextInput += OnConsoleTextInput;

            // Renderer
            _renderer = new HexGridRenderer(_hexGrid, _spriteBatch, GraphicsDevice);

            // Test Mode is currently what the game boots directly into, for this ongoing
            // card-testing pass (see EnterTestMode's own doc comment) - the normal 20x15
            // procedural map/character select/standard decks (BuildOriginalMap/EnterOriginalMode)
            // are kept fully intact as a separate mode, just not the default right now; "original
            // mode" in the dev console switches back.
            EnterTestMode();
            _cameraTarget = _turnSystem.CurrentUnit.Position;

            _hasAutoAdvancedThisTurn = false;
            _hasAiActedThisTurn = false;

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
            }

            _playerUnit = _units[0];
            EnsurePlayerHandInitialized();

            _turnSystem = new TurnSystem(_units);
            _renderer.CameraPosition = _hexGrid.HexToWorld(1, 1);

            // Reset every input-mode flag - whatever was on screen before "test mode" was typed
            // (a targeting mode, an open menu, ...) no longer refers to anything valid.
            _gameState = GameState.Playing;
            _turnMenuActive = false;
            _cardMenuActive = false;
            _attackMenuActive = false;
            _targetingModeActive = false;
            _coneAimingModeActive = false;
            _teleportModeActive = false;
            _allyTargetModeActive = false;
            _summonPlacementModeActive = false;
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
            return all.OrderBy(IsCardTested).ToList();
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
            _hexGrid = new HexGrid(20, 15, tileSize: 32f);
            _map = BuildOriginalMap();
            _console.SetMap(_map);

            _units.Clear();
            SpawnUnits();
            foreach (var unit in _units)
                unit.LoadContent(Content, GraphicsDevice);

            _testModeActive = false;
            _playerUnit = null; // back through character select
            GenerateClassDecks();

            _turnSystem = new TurnSystem(_units);
            _renderer.CameraPosition = _hexGrid.HexToWorld(10, 7);
            _cameraZoomTarget = 2.5f;

            _gameState = GameState.CharacterSelect;
            _selectedCharacterIndex = 0;

            // Reset every input-mode flag, same reasoning as EnterTestMode.
            _turnMenuActive = false;
            _cardMenuActive = false;
            _attackMenuActive = false;
            _targetingModeActive = false;
            _coneAimingModeActive = false;
            _teleportModeActive = false;
            _allyTargetModeActive = false;
            _summonPlacementModeActive = false;
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
            // would aim the camera at empty space off the edge of it).
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

            if (_gameState == GameState.CharacterSelect)
            {
                HandleCharacterSelectInput(keyboardState, mouseState);
            }
            else if (_gameState == GameState.MatchOver)
            {
                // Nothing to update - just showing the result screen (see Draw).
            }
            else
            {
                // FFA win check - a unit's team is itself, or whoever summoned it (see
                // GetTeamRoot/BaseUnit.Owner); the moment at most one team still has a living
                // member, the match is over. A lone surviving summon still counts as its owner's
                // team winning, even if the owner itself has already Fainted. Suppressed entirely
                // in Test Mode - it spawns all 4 classes as separate "teams" with AI that can
                // still attack if already in range, and a stray kill ending the "match" would cut
                // a testing session short for no reason.
                var stillStandingTeams = _units.Where(u => !u.IsFainted).Select(GetTeamRoot).Distinct().ToList();
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
                            // Initial swoop-in is done - lock on exactly from here so the camera
                            // can never lag behind a unit mid-walk.
                            _renderer.CameraPosition = _cameraTarget;
                            _renderer.ZoomLevel = _cameraZoomTarget;

                            // Allow this unit's one AI move + the auto-advance that ends its turn
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
                                _attackMenuActive = false;
                                _targetingModeActive = false;
                                _coneAimingModeActive = false;
                                _teleportModeActive = false;
                                _allyTargetModeActive = false;
                                _summonPlacementModeActive = false;
                                _movementModeActive = false;
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
                        else if (_summonPlacementModeActive)
                        {
                            HandleSummonPlacementInput(keyboardState, mouseState);
                        }
                        else if (_movementModeActive)
                        {
                            HandleMovementInput(keyboardState, mouseState);
                        }
                        else if (_cardMenuActive)
                        {
                            HandleCardMenuInput(keyboardState, mouseState);
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
                        _summonPlacementModeActive = false;
                        _movementModeActive = false;
                        _cardMenuActive = false;

                        // Simple placeholder AI (GDD-pending): walk toward the player once per
                        // turn, so there's something in range to test attacks/spells against.
                        // Runs as soon as the camera transition settles, well before the
                        // 3-second auto-advance below ends the turn.
                        if (!isPlayerTurn && !_hasAiActedThisTurn)
                        {
                            _hasAiActedThisTurn = true;
                            RunSimpleAI(_turnSystem.CurrentUnit);
                        }

                        // Auto-advance to next unit after 3 seconds (once per unit, non-player units only)
                        if (!isPlayerTurn && _turnSystem.UnitTurnElapsed > 3f && !_hasAutoAdvancedThisTurn)
                        {
                            _hasAutoAdvancedThisTurn = true;
                            _turnSystem.NextUnit();
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
            EnsurePlayerHandInitialized();
            _gameState = GameState.Playing;
        }

        private void EnsurePlayerHandInitialized()
        {
            if (_playerUnit == null)
                return;

            if (!_classHands.TryGetValue(_playerUnit.Class, out var hand) || hand == null)
            {
                hand = new List<object>();
                _classHands[_playerUnit.Class] = hand;
            }

            if (hand.Count == 0)
            {
                var startingHand = DrawCardsFromDeck(_playerUnit.Class, 3);
                hand.AddRange(startingHand);
                _classHands[_playerUnit.Class] = hand;
            }

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
            _playerUnit.CurrentAP -= drawCost;
            _playerUnit.RecordCardDraw();

            _availableHandCards = new List<object>(hand);
            _handCardIndex = _availableHandCards.Count - 1;
            _highlightedHandCardIndex = _handCardIndex;
        }

        /// <summary>Remove a played card from the player's hand (consumed on a successful cast) and refresh the hand view.</summary>
        private void RemoveCardFromHand(object card)
        {
            if (_classHands.TryGetValue(_playerUnit.Class, out var hand) && hand != null)
                hand.Remove(card);

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

        /// <summary>
        /// This turn's actual menu options for _playerUnit, built fresh every call by
        /// Combat.TurnMenuBuilder off its current Class/Inventory/Knocked-Down state and the
        /// tiles around it - see TurnMenuBuilder for exactly what each option checks. Called from
        /// every place the turn menu reads or draws its options, rather than cached, so it always
        /// reflects whatever just changed (AP spent, a neighboring tile that opened up, ...).
        /// </summary>
        private List<TurnMenuOption> GetTurnMenuOptions() =>
            TurnMenuBuilder.Build(_playerUnit, GameMode.Combat, _hexGrid, _map, _units);

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

            // "End"/"Guard", "ViewMap", "Attack" (opens the attack submenu below) and "Move" are
            // wired up - "Items" has no system to act on yet (no inventory-use UI exists), so
            // selecting it (TurnMenuBuilder only even shows it once there's a weapon to manage)
            // falls through to the implicit no-op below, same honest "nothing happens yet"
            // treatment spells without an execution system get.
            if (optionId == "End")
            {
                _turnMenuActive = false;

                // The slot displays as "Guard" (see DrawTurnMenu) whenever the unit can afford
                // it; selecting it then spends the AP and applies the Guard stance on top of
                // ending the turn.
                if (_playerUnit.CurrentAP >= _playerUnit.GuardAPCost)
                {
                    _playerUnit.CurrentAP -= _playerUnit.GuardAPCost;
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

            _reachableTiles = Pathfinder.GetReachableTiles(_hexGrid, _map, start, _playerUnit.TilesPerAP, displayApBudget, GetOccupiedTiles(_playerUnit));
            _reachableTiles.Remove(start);

            var mouseState = Mouse.GetState();
            var viewportSize = new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            var cursorHex = _renderer.GetHexAtScreenPos(mouseState.X, mouseState.Y, viewportSize);
            _renderer.CameraPosition = _hexGrid.HexToWorld(cursorHex.col, cursorHex.row);

            _movementModeActive = true;
        }

        /// <summary>Every tile currently occupied by a unit other than the given one.</summary>
        private HashSet<(int col, int row)> GetOccupiedTiles(BaseUnit excluding)
        {
            return _units.Where(u => u != excluding)
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
            var start = _hexGrid.WorldToHex(unit.Position);
            var path = Pathfinder.FindPath(_hexGrid, _map, start, destination, unit.TilesPerAP, GetOccupiedTiles(unit));
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

            int apCost = (int)Math.Ceiling(tiles / (float)tilesPerAP) + water;

            var affordablePath = path.Take(lastAffordableIndex + 1);
            unit.SetMovementPath(affordablePath.Select(hex => _hexGrid.HexToWorld(hex.col, hex.row)));
            unit.CurrentAP = Math.Max(0, unit.CurrentAP - apCost);
        }

        /// <summary>
        /// Placeholder AI (GDD-pending - no real tactical depth yet): always targets the
        /// player specifically (matches the FFA's current scope - AI-vs-AI isn't handled).
        /// If the player is already in range of some affordable move, use the hardest-hitting
        /// one instead of moving - no point closing distance you don't need to (this is what
        /// keeps ranged units from walking into melee range for no reason). Otherwise walk
        /// toward the player as before, then check once more - it may have closed into range
        /// this turn - and attack if so. Also handles any restricting StatusEffect (Stun, Deep
        /// Sleep, Meditate, ...: always attempts that effect's own end-it-early action) and
        /// Knockdown (always stands back up). In Test Mode, this entire method is a no-op for
        /// every AI unit (no movement, no attacking either) - see _testModeActive.
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
                aiUnit.ConsumeRestrictingEffectTurn();
                return;
            }

            // Knocked Down: stand up instead of trying (and failing) to move/attack this turn.
            if (aiUnit.IsKnockedDown)
            {
                aiUnit.TryStandUp();
                return;
            }

            if (TryAiAttack(aiUnit))
                return;

            var playerHex = _hexGrid.WorldToHex(_playerUnit.Position);
            var start = _hexGrid.WorldToHex(aiUnit.Position);
            var occupied = GetOccupiedTiles(aiUnit);

            (int col, int row)? bestTile = null;
            int bestPathLength = int.MaxValue;

            foreach (var tile in _hexGrid.GetNeighbors(playerHex.col, playerHex.row))
            {
                var path = Pathfinder.FindPath(_hexGrid, _map, start, tile, aiUnit.TilesPerAP, occupied);
                if (path == null)
                    continue;

                if (path.Count < bestPathLength)
                {
                    bestPathLength = path.Count;
                    bestTile = tile;
                }
            }

            // No reachable tile next to the player (e.g. boxed in) - just sit tight this turn.
            if (bestTile.HasValue)
                MoveUnitTowards(aiUnit, bestTile.Value);

            // May have closed into range this turn - take one swing if so.
            TryAiAttack(aiUnit);
        }

        /// <summary>
        /// Placeholder AI attack decision: among every move `aiUnit` can currently afford
        /// (AP/MP/ammo) with the player in its effective range, use whichever would deal the
        /// most raw damage. At most one attack per call (RunSimpleAI calls this up to twice a
        /// turn - once before moving, once after). Returns true if an attack was made.
        /// </summary>
        private bool TryAiAttack(BaseUnit aiUnit)
        {
            var aiHex = _hexGrid.WorldToHex(aiUnit.Position);
            var playerHex = _hexGrid.WorldToHex(_playerUnit.Position);
            int distanceTiles = _hexGrid.GetDistance(aiHex.col, aiHex.row, playerHex.col, playerHex.row);

            var candidates = aiUnit.AvailableMovesWithSource
                .Where(entry =>
                {
                    int apCost = aiUnit.GetEffectiveAPCost(entry.Move, entry.SourceWeapon);
                    if (apCost > aiUnit.CurrentAP || entry.Move.MPCost > aiUnit.CurrentMP)
                        return false;
                    if (entry.Move.RequiredAmmoType.HasValue && aiUnit.GetAmmo(entry.Move.RequiredAmmoType.Value) < 1)
                        return false;
                    return distanceTiles <= entry.Move.GetEffectiveRange(aiUnit, entry.SourceWeapon);
                })
                .OrderByDescending(entry => entry.Move.GetDamage(aiUnit, _playerUnit, entry.SourceWeapon))
                .ToList();

            if (candidates.Count == 0)
                return false;

            var (move, sourceWeapon) = candidates[0];

            BaseUnit primaryTarget = move.HitsAllAdjacent || move.HitsCone ? null : _playerUnit;
            HexDirection? aimDirection = move.HitsCone
                ? _hexGrid.GetDirectionTo(aiHex.col, aiHex.row, playerHex.col, playerHex.row)
                : null;

            var outcomes = AttackResolver.Resolve(aiUnit, primaryTarget, move, sourceWeapon, _hexGrid, _map, _units, aimDirection);
            SpawnAttackAnimations(aiUnit, move, outcomes);
            ShowCombatMessage(BuildCombatLogMessage(aiUnit, move, outcomes));
            return true;
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
            var occupied = GetOccupiedTiles(_playerUnit);
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
            _playerUnit.CurrentAP = Math.Max(0, _playerUnit.CurrentAP - _playerUnit.GetEffectiveAPCost(_pendingMove, null));
            _playerUnit.CurrentMP = Math.Max(0, _playerUnit.CurrentMP - _pendingMove.MPCost);
            _playerUnit.Position = _hexGrid.HexToWorld(destHex.col, destHex.row);

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
            _playerUnit.CurrentAP = Math.Max(0, _playerUnit.CurrentAP - _playerUnit.GetEffectiveAPCost(_pendingMove, null));
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

        /// <summary>Flat AP cost to summon a creature - first-pass placeholder, pending a balance pass (see GDD Portal System/Summon Cards).</summary>
        private const int SummonAPCost = 2;

        /// <summary>How far from the caster a summoned creature can be placed.</summary>
        private const int SummonPlacementRange = 1;

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

        /// <summary>
        /// Cast a SummonCard: pays SummonAPCost + the card's own ManaCost up front (same
        /// afford-first convention as every other card/move), then opens placement targeting -
        /// every passable, unoccupied tile within SummonPlacementRange of the caster, same
        /// mechanics as teleport targeting (CastBlinkSpell) just for picking where the new unit
        /// appears instead of where the caster goes.
        /// </summary>
        private void CastSummon(SummonCard card)
        {
            if (SummonAPCost > _playerUnit.CurrentAP || card.ManaCost > _playerUnit.CurrentMP)
            {
                ShowCombatMessage($"Not enough AP/MP to summon {card.Name}.");
                return;
            }

            var casterHex = _hexGrid.WorldToHex(_playerUnit.Position);
            var occupied = GetOccupiedTiles(_playerUnit);
            _summonPlacementValidTiles = _hexGrid.GetHexesInRadius(casterHex.col, casterHex.row, SummonPlacementRange)
                .Where(hex => hex != casterHex && Pathfinder.IsPassable(_hexGrid, _map, hex.col, hex.row, occupied))
                .ToHashSet();

            if (_summonPlacementValidTiles.Count == 0)
            {
                ShowCombatMessage($"Nowhere to place {card.Name}.");
                return;
            }

            _pendingSummonCard = card;
            _cardMenuActive = false;
            _summonPlacementModeActive = true;
        }

        /// <summary>Handle input while picking a summon's placement tile: click a highlighted tile to spawn it there, E to cancel back to the Cards menu.</summary>
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
        /// Spend SummonAPCost/the card's MP, spawn the creature at destHex owned by the caster
        /// (see BaseUnit.Owner - keeps it off the caster's own valid-attack-target lists and
        /// counted as the caster's team for the FFA win check), load its sprite (a no-op if it
        /// has none yet), add it to the battlefield, consume the card, and return to the turn
        /// menu. It joins the turn order starting next Turn (TurnSystem rebuilds from the same
        /// _units list each new Turn), not mid-cycle.
        /// </summary>
        private void ExecuteSummon((int col, int row) destHex)
        {
            _playerUnit.CurrentAP = Math.Max(0, _playerUnit.CurrentAP - SummonAPCost);
            _playerUnit.CurrentMP = Math.Max(0, _playerUnit.CurrentMP - _pendingSummonCard.ManaCost);

            var position = _hexGrid.HexToWorld(destHex.col, destHex.row);
            var summon = CreateSummonUnit(_pendingSummonCard, position);
            summon.Owner = _playerUnit;
            summon.LoadContent(Content, GraphicsDevice);
            _units.Add(summon);

            ShowCombatMessage($"{_playerUnit.Name} summons a {summon.Name}.");
            RemoveCardFromHand(_pendingSummonCard);

            CancelSummonPlacement(returnToCardMenu: false);
        }

        private void CancelSummonPlacement(bool returnToCardMenu = true)
        {
            _summonPlacementModeActive = false;
            _pendingSummonCard = null;
            _summonPlacementValidTiles.Clear();

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
            if (card is SpellCard spellCard)
            {
                if (spellCard.Effect.IsBlink)
                    CastBlinkSpell(spellCard);
                else if (spellCard.Effect.IsAllyShield)
                    CastAllyShield(spellCard);
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
                return $"hit {o.Target.Name} for {o.Damage}{crit}{status}{fainted}";
            });

            return $"{attacker.Name} used {move.Name}: {string.Join("; ", parts)}";
        }

        /// <summary>Show a short-lived line at the bottom of the screen - both the actual combat log and "why didn't that work" feedback (unaffordable AP/MP, out of ammo, nothing in range) go through here so nothing is ever a silent no-op.</summary>
        private void ShowCombatMessage(string message)
        {
            _combatLogMessage = message;
            _combatLogTimer = 4f;
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

        private void HandleMapControls(KeyboardState keyboardState, MouseState mouseState, float deltaTime)
        {
            // Camera pan (WASD) - suppressed while the turn menu or attack submenu is open,
            // since W/S there navigate the menu instead (Move mode doesn't use W/S for
            // anything, so panning stays available there - see OpenMovementMode for its
            // one-time entry snap). Scaled by deltaTime (not a flat per-frame step) so it's
            // smooth and frame-rate-independent instead of speeding up or stuttering with the
            // frame rate - this was the actual cause of View Map feeling laggy/jittery.
            if (!_turnMenuActive && !_attackMenuActive && !_cardMenuActive)
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

            // Select hex (left click)
            if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
            {
                Vector2 viewportSize = new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
                _selectedHex = _renderer.GetHexAtScreenPos(mouseState.X, mouseState.Y, viewportSize);
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

            // Whichever unit (if any) the mouse is currently over - drives which neutral/enemy
            // units show their active stat-change icons below (DrawUnitStatBonuses); the
            // player's own unit and its summons always show theirs regardless of hover.
            var statHoverHex = _renderer.GetHexAtScreenPos(mouseState.X, mouseState.Y, viewportSize);
            var statHoveredUnit = _units.FirstOrDefault(u => _hexGrid.WorldToHex(u.Position) == statHoverHex);

            // Draw units
            foreach (var unit in _units)
            {
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

                    _spriteBatch.Draw(unit.SpriteTexture, spritePos, null, spriteTint, 0f,
                                    Vector2.Zero, finalScale, SpriteEffects.None, 0f);
                }

                DrawFacingIndicator(unit);

                if (unit.IsAlive)
                    DrawUnitHealthBar(unit);

                if (_font != null)
                {
                    string statusText = unit.IsAlive ? $"{unit.Name}: {unit.HP}/{unit.MaxHP}" : $"{unit.Name}: DEAD";
                    Vector2 textSize = _font.MeasureString(statusText) * 0.4f;
                    Vector2 textPos = unit.Position - new Vector2(textSize.X / 2f, 48f);
                    Color textColor = !unit.IsAlive ? Color.DarkRed : (unit.IsFainted ? Color.Gray : Color.White);
                    _spriteBatch.DrawString(_font, statusText, textPos, textColor, 0f, Vector2.Zero, 0.4f, SpriteEffects.None, 0f);

                    // Every active StatusEffect (Buffs cyan, Afflictions orange - e.g. Brace,
                    // Deep Sleep, Meditate's live stacks, Stunned) shown as a small line right
                    // above the name/HP text - the only persistent on-screen sign any is active,
                    // beyond the fading combat-log message shown the moment one is applied. Fully
                    // generic - a brand new StatusEffect shows up here automatically, no Game1
                    // change needed.
                    if (unit.StatusEffects.Count > 0)
                    {
                        string effectText = string.Join(", ", unit.StatusEffects.Select(e =>
                            e.GrowsOverTime ? $"{e.Name} ({e.Stacks}/{e.MaxStacks})" : e.Name));
                        Color effectColor = unit.Afflictions.Any() ? new Color(255, 150, 60) : Color.Cyan;
                        Vector2 effectTextSize = _font.MeasureString(effectText) * 0.35f;
                        Vector2 effectTextPos = unit.Position - new Vector2(effectTextSize.X / 2f, 68f);
                        _spriteBatch.DrawString(_font, effectText, effectTextPos, effectColor, 0f, Vector2.Zero, 0.35f, SpriteEffects.None, 0f);
                    }
                }

                // Per-stat icon + signed value row (sword/book/shield/rune/crosshair for
                // STR/INT/DEF/RES/ACC) for every stat an active StatusEffect is currently
                // pushing off base - always shown for the player's own unit and its summons
                // (Owner == _playerUnit), shown for anyone else only while hovered/targeted.
                bool showStatBonuses = unit == _playerUnit || unit.Owner == _playerUnit || unit == statHoveredUnit;
                if (showStatBonuses)
                    DrawUnitStatBonuses(unit);
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
                else if (_summonPlacementModeActive)
                    DrawBottomHint(viewportSize, "Click a highlighted tile to place your summon - E to cancel");
                else if (_combatLogTimer > 0f && !string.IsNullOrEmpty(_combatLogMessage))
                    DrawBottomHint(viewportSize, _combatLogMessage);

                if (_cardMenuActive)
                    DrawCardMenuOverlay(viewportSize);

                if (_consoleOpen)
                    DrawConsole();
            }

            _spriteBatch.End();

            base.Draw(gameTime);
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
        /// Damage/crit/hit%/affliction preview for _pendingMove against `target`, shown while
        /// targeting mode has it hovered - everything here reuses the exact same Move/AttackResolver
        /// formulas the actual attack resolves with, just without rolling or spending anything.
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

            var lines = new List<string>
            {
                $"Target: {target.Name}",
                $"Damage: {damage}",
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

            var bonuses = unit.GetActiveStatBonuses();
            if (bonuses.Count == 0)
                return;

            const float iconSize = 14f;
            const float iconTextGap = 2f;
            const float entrySpacing = 4f;
            const float textScale = 0.3f;

            var labels = new string[bonuses.Count];
            float totalWidth = 0f;
            for (int i = 0; i < bonuses.Count; i++)
            {
                labels[i] = (bonuses[i].Bonus > 0 ? "+" : "") + bonuses[i].Bonus;
                totalWidth += iconSize + iconTextGap + _font.MeasureString(labels[i]).X * textScale + entrySpacing;
            }
            totalWidth -= entrySpacing;

            Vector2 origin = unit.Position - new Vector2(totalWidth / 2f, 92f);
            float x = origin.X;

            for (int i = 0; i < bonuses.Count; i++)
            {
                if (_statIcons.TryGetValue(bonuses[i].StatCode, out var icon))
                {
                    _spriteBatch.Draw(icon, new Vector2(x, origin.Y), null, Color.White, 0f,
                        Vector2.Zero, iconSize / icon.Width, SpriteEffects.None, 0f);
                }
                x += iconSize + iconTextGap;

                Color textColor = bonuses[i].Bonus > 0 ? new Color(120, 255, 140) : new Color(255, 110, 110);
                _spriteBatch.DrawString(_font, labels[i], new Vector2(x, origin.Y + 1f), textColor, 0f,
                    Vector2.Zero, textScale, SpriteEffects.None, 0f);
                x += _font.MeasureString(labels[i]).X * textScale + entrySpacing;
            }
        }

        private void DrawUnitHealthBar(BaseUnit unit)
        {
            const float barWidth = 50f;
            const float barHeight = 6f;
            float pixelsPerPoint = barWidth / Math.Max(1, unit.MaxHP);

            float filledWidth = unit.HP * pixelsPerPoint;
            float missingWidth = (unit.MaxHP - unit.HP) * pixelsPerPoint;
            float shieldWidth = unit.ShieldPoints * pixelsPerPoint;

            Vector2 barPos = unit.Position - new Vector2(barWidth / 2f, 60f);

            // Dark background spans HP's own range only (filled + missing) - the shield segment
            // gets its own distinct blue block appended after it, not folded into this backing.
            _spriteBatch.Draw(_whitePixel,
                new Rectangle((int)barPos.X, (int)barPos.Y, (int)(filledWidth + missingWidth), (int)barHeight),
                new Color(35, 35, 35, 220));

            Color hpColor = Color.Lerp(Color.Red, Color.LimeGreen, unit.HP / (float)Math.Max(1, unit.MaxHP));
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
            var occupied = GetOccupiedTiles(_playerUnit);

            var path = Pathfinder.FindPath(_hexGrid, _map, start, hoveredHex, _playerUnit.TilesPerAP, occupied);
            Vector2? xMarkerPos = null;

            if (path == null)
            {
                List<(int col, int row)> bestPath = null;
                foreach (var neighbor in _hexGrid.GetNeighbors(hoveredHex.col, hoveredHex.row))
                {
                    var candidate = Pathfinder.FindPath(_hexGrid, _map, start, neighbor, _playerUnit.TilesPerAP, occupied);
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

            bool canAffordMP = caster.CurrentMP >= card.Effect.MPCost;
            DrawTextCentered(card.Name, FractionalRect(destRect, CardNameBarRegion), Color.White);
            DrawTextCentered(card.Effect.MPCost.ToString(), FractionalRect(destRect, CardCostCircleRegion), canAffordMP ? Color.Black : Color.Red);
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

            bool canAffordMana = _playerUnit.CurrentMP >= card.ManaCost;
            DrawTextCentered(card.Name, FractionalRect(destRect, SummonNameBarRegion), Color.White);
            DrawTextCentered(card.ManaCost.ToString(), FractionalRect(destRect, SummonManaCostCircleRegion), canAffordMana ? Color.Black : Color.Red);

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
                int hoveredCardIndex = -1;
                for (int i = layout.Count - 1; i >= 0; i--)
                {
                    if (layout[i].Contains(mousePos))
                    {
                        hoveredCardIndex = i;
                        break;
                    }
                }

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

                if (hoveredCardIndex >= 0)
                {
                    string tooltip = GetHoveredCardTooltip(_availableHandCards[hoveredCardIndex], focusedRect, mousePos);
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
            else if (card is SummonCard)
            {
                if (FractionalRect(destRect, SummonManaCostCircleRegion).Contains(mousePos))
                    return "Mana Cost: mana required to summon this creature.";
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
