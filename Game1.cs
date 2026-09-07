using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SagesOfOzvaram.Maps;
using SagesOfOzvaram.Units;
using SagesOfOzvaram.Units.Heroes;
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
            Playing
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

        // UI
        private SpriteFont _font;
        private Texture2D _whitePixel;

        // Input
        private KeyboardState _previousKeyboardState;
        private MouseState _previousMouseState;
        private string _consoleInput = "";
        private bool _consoleOpen = false;

        // Selected hex
        private (int col, int row) _selectedHex = (-1, -1);

        // Character select
        private GameState _gameState = GameState.CharacterSelect;
        private static readonly string[] AvatarFileNames =
        {
            "ApprenticeSorc_Avatar",     // matches _units[0] (Sorcerer)
            "ApprenticeWarrior_Avatar",  // matches _units[1]
            "ApprenticeCleric_Avatar",   // matches _units[2]
            "ApprenticeHunter_Avatar"    // matches _units[3]
        };
        private Texture2D[] _avatarTextures;
        private int _selectedCharacterIndex = 0;  // Sorcerer is the default selection
        private BaseUnit _playerUnit;

        // Turn menu (shown, next to the player's unit, when it's their turn)
        private static readonly string[] TurnMenuOptions = { "Attack", "Spell", "Items", "End", "Move", "View Map" };
        private int _turnMenuIndex = 0;
        private bool _turnMenuActive = false;
        private bool _viewingMap = false;

        // Attack submenu (opened from the "Attack" turn-menu option)
        private List<string> _attackMenuLabels = new List<string>();
        private int _attackMenuIndex = 0;
        private bool _attackMenuActive = false;

        // Movement mode (opened from the "Move" turn-menu option)
        private bool _movementModeActive = false;
        private Dictionary<(int col, int row), int> _reachableTiles = new Dictionary<(int, int), int>();

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
            // Initialize hex grid (20 cols x 15 rows, 32px tiles)
            _hexGrid = new HexGrid(20, 15, tileSize: 32f);

            // Generate procedural map with seed
            int mapSeed = Environment.TickCount; // Or use a fixed seed like 12345 for reproducibility
            MapGenerator generator = new MapGenerator(mapSeed);
            _map = generator.GenerateMap(20, 15, scale: 0.08f, octaves: 4);

            // Asset registry
            _assetRegistry = new AssetRegistry();
            RegisterDefaultAssets();

            // Dev console
            _console = new DevConsole(_map, _assetRegistry);

            // Initialize units list
            _units = new List<BaseUnit>();

            // Spawn the 4 apprentice units in random corners
            SpawnUnits();

            // Initialize turn system
            _turnSystem = new TurnSystem(_units);
            _cameraTarget = _turnSystem.CurrentUnit.Position;
            _cameraZoomTarget = 2.5f;  // Zoom in on units
            _hasAutoAdvancedThisTurn = false;

            // Renderer
            _renderer = new HexGridRenderer(_hexGrid, _spriteBatch, GraphicsDevice);
            _renderer.CameraPosition = _hexGrid.HexToWorld(10, 7);

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

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // Initialize renderer HERE (after SpriteBatch exists)
            _renderer = new HexGridRenderer(_hexGrid, _spriteBatch, GraphicsDevice);
            _renderer.CameraPosition = _hexGrid.HexToWorld(10, 7);

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
            else
            {
                // Toggle console with grave key (~)
                if (keyboardState.IsKeyDown(Keys.OemTilde) && !_previousKeyboardState.IsKeyDown(Keys.OemTilde))
                {
                    _consoleOpen = !_consoleOpen;
                }

                if (_consoleOpen)
                {
                    // Console input handling
                    HandleConsoleInput(keyboardState);
                }
                else
                {
                    // Update turn system
                    _turnSystem.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

                    // Smooth camera pan to current unit
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
                        // Camera transition complete, allow next auto-advance
                        _hasAutoAdvancedThisTurn = false;
                    }

                    bool isPlayerTurn = _turnSystem.CurrentUnit == _playerUnit;

                    if (isPlayerTurn && !_turnSystem.TransitioningCamera)
                    {
                        if (_viewingMap)
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
                        else if (_movementModeActive)
                        {
                            HandleMovementInput(keyboardState, mouseState);
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
                        _movementModeActive = false;

                        // Auto-advance to next unit after 3 seconds (once per unit, non-player units only)
                        if (!isPlayerTurn && _turnSystem.UnitTurnElapsed > 3f && !_hasAutoAdvancedThisTurn)
                        {
                            _hasAutoAdvancedThisTurn = true;
                            _turnSystem.NextUnit();
                            _cameraTarget = _turnSystem.CurrentUnit.Position;
                        }
                    }

                    // Map editor controls
                    HandleMapControls(keyboardState, mouseState);
                }
            }

            _previousKeyboardState = keyboardState;
            _previousMouseState = mouseState;

            base.Update(gameTime);
        }

        private void HandleConsoleInput(KeyboardState keyboardState)
        {
            // Backspace
            if (keyboardState.IsKeyDown(Keys.Back) && _consoleInput.Length > 0)
            {
                _consoleInput = _consoleInput.Substring(0, _consoleInput.Length - 1);
            }

            // Submit command
            if (keyboardState.IsKeyDown(Keys.Enter) && !_previousKeyboardState.IsKeyDown(Keys.Enter))
            {
                _console.ExecuteCommand(_consoleInput);
                _consoleInput = "";
            }

            // Command history
            if (keyboardState.IsKeyDown(Keys.Up) && !_previousKeyboardState.IsKeyDown(Keys.Up))
            {
                _consoleInput = _console.GetPreviousCommand();
            }
            if (keyboardState.IsKeyDown(Keys.Down) && !_previousKeyboardState.IsKeyDown(Keys.Down))
            {
                _consoleInput = _console.GetNextCommand();
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
            _gameState = GameState.Playing;
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
        /// Handle input on the player unit's turn menu: W/S or mouse-hover to highlight an
        /// option, click or E to confirm.
        /// </summary>
        private void HandleTurnMenuInput(KeyboardState keyboardState, MouseState mouseState)
        {
            if (keyboardState.IsKeyDown(Keys.S) && !_previousKeyboardState.IsKeyDown(Keys.S))
                _turnMenuIndex = (_turnMenuIndex + 1) % TurnMenuOptions.Length;
            if (keyboardState.IsKeyDown(Keys.W) && !_previousKeyboardState.IsKeyDown(Keys.W))
                _turnMenuIndex = (_turnMenuIndex - 1 + TurnMenuOptions.Length) % TurnMenuOptions.Length;

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
            string option = TurnMenuOptions[_turnMenuIndex];

            // "End"/"Guard", "View Map", "Attack" (opens the attack submenu below) and "Move"
            // are wired up - Spell/Items have no systems to act on yet (no spellbook or
            // inventory-use exist yet).
            if (option == "End")
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
                _cameraTarget = _turnSystem.CurrentUnit.Position;
            }
            else if (option == "View Map")
            {
                _turnMenuActive = false;
                _viewingMap = true;
            }
            else if (option == "Attack")
            {
                _turnMenuActive = false;
                OpenAttackMenu();
            }
            else if (option == "Move")
            {
                _turnMenuActive = false;
                OpenMovementMode();
            }
        }

        /// <summary>
        /// Enter movement mode: computes _reachableTiles out to a bit beyond the unit's current
        /// AP budget (so out-of-range tiles nearby still show, tinted red - see DrawMovementRange).
        /// </summary>
        private void OpenMovementMode()
        {
            var start = _hexGrid.WorldToHex(_playerUnit.Position);
            int budget = _playerUnit.CurrentAP * _playerUnit.TilesPerAP;
            int displayRadius = budget + 3;

            _reachableTiles = Pathfinder.GetReachableTiles(_hexGrid, _map, start, displayRadius, GetOccupiedTiles(_playerUnit));
            _reachableTiles.Remove(start);

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
            var start = _hexGrid.WorldToHex(_playerUnit.Position);
            var path = Pathfinder.FindPath(_hexGrid, _map, start, destination, GetOccupiedTiles(_playerUnit));
            if (path == null || path.Count == 0)
                return;

            int budget = _playerUnit.CurrentAP * _playerUnit.TilesPerAP;
            int tilesToMove = Math.Min(path.Count, budget);
            if (tilesToMove <= 0)
                return;

            var actualDestination = path[tilesToMove - 1];
            int apCost = (int)Math.Ceiling(tilesToMove / (float)_playerUnit.TilesPerAP);

            _playerUnit.Position = _hexGrid.HexToWorld(actualDestination.col, actualDestination.row);
            _playerUnit.CurrentAP = Math.Max(0, _playerUnit.CurrentAP - apCost);

            _movementModeActive = false;
            _turnMenuIndex = 0;
            _turnMenuActive = true;
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
            _attackMenuLabels = _playerUnit.AvailableMovesWithSource
                .Select(entry => FormatAttackLabel(entry.Move, entry.SourceWeapon))
                .ToList();
            _attackMenuLabels.Add("Back");
            _attackMenuIndex = 0;
            _attackMenuActive = true;
        }

        /// <summary>
        /// Build a move's submenu label, e.g. "Sword Slash (2 AP)" or "Stab (3 AP)" if its
        /// weapon isn't the one currently equipped. MP cost (or other non-AP costs) is
        /// appended too when present.
        /// </summary>
        private string FormatAttackLabel(Move move, Weapon sourceWeapon)
        {
            int apCost = _playerUnit.GetEffectiveAPCost(move, sourceWeapon);
            string cost = $"{apCost} AP";
            if (move.MPCost > 0)
                cost += $", {move.MPCost} MP";

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
            }
            // Actual moves: no targeting/damage-resolution system exists yet, so picking one
            // is currently a no-op (the submenu just stays open).
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
            GetMenuOptionRects(viewportSize, TurnMenuOptions.Length);

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

        private void HandleMapControls(KeyboardState keyboardState, MouseState mouseState)
        {
            // Camera pan (WASD) - suppressed while the turn menu or attack submenu is open,
            // since W/S there navigate the menu instead
            if (!_turnMenuActive && !_attackMenuActive)
            {
                float panSpeed = 5f;
                if (keyboardState.IsKeyDown(Keys.W))
                    _renderer.PanCamera(-Vector2.UnitY * panSpeed);  // W = up (negative Y)
                if (keyboardState.IsKeyDown(Keys.S))
                    _renderer.PanCamera(Vector2.UnitY * panSpeed);   // S = down (positive Y)
                if (keyboardState.IsKeyDown(Keys.A))
                    _renderer.PanCamera(-Vector2.UnitX * panSpeed);  // A = left (negative X)
                if (keyboardState.IsKeyDown(Keys.D))
                    _renderer.PanCamera(Vector2.UnitX * panSpeed);   // D = right (positive X)
            }

            // Zoom (mouse wheel)
            if (mouseState.ScrollWheelValue > _previousMouseState.ScrollWheelValue)
            {
                _renderer.Zoom(0.1f, new Vector2(mouseState.X, mouseState.Y));
            }
            if (mouseState.ScrollWheelValue < _previousMouseState.ScrollWheelValue)
            {
                _renderer.Zoom(-0.1f, new Vector2(mouseState.X, mouseState.Y));
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

            // Draw hover hex
            var mouseState = Mouse.GetState();
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

                    _spriteBatch.Draw(unit.SpriteTexture, spritePos, null, Color.White, 0f,
                                    Vector2.Zero, finalScale, SpriteEffects.None, 0f);
                }

                DrawFacingIndicator(unit);
            }

            _spriteBatch.End();

            // Draw UI (screen space, no camera transform)
            _spriteBatch.Begin();

            if (_font != null)
            {
                _spriteBatch.DrawString(_font, $"Turn {_turnSystem.CurrentTurn}", new Vector2(16, 16), Color.White);

                if (_turnSystem.CurrentUnit == _playerUnit)
                    _spriteBatch.DrawString(_font, $"AP: {_playerUnit.CurrentAP}/{_playerUnit.MaxAP}", new Vector2(16, 40), Color.White);

                if (_turnSystem.ShowingTurnAnnouncement)
                    DrawTurnAnnouncement(viewportSize);

                if (_turnMenuActive)
                    DrawTurnMenu(viewportSize);

                if (_attackMenuActive)
                    DrawAttackMenu(viewportSize);

                if (_viewingMap)
                    DrawMapViewIndicator(viewportSize);

                if (_movementModeActive)
                    DrawBottomHint(viewportSize, "Click a highlighted tile to move - E to cancel");

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
            Rectangle[] optionRects = GetTurnMenuOptionRects(viewportSize);

            for (int i = 0; i < TurnMenuOptions.Length; i++)
            {
                bool selected = i == _turnMenuIndex;
                Color bg = selected ? new Color(255, 200, 0, 220) : new Color(0, 0, 0, 200);
                Color textColor = selected ? Color.Black : Color.White;

                // The "End" slot displays as "Guard" whenever the unit can afford to use it
                string label = TurnMenuOptions[i] == "End" && _playerUnit.CurrentAP >= _playerUnit.GuardAPCost
                    ? "Guard"
                    : TurnMenuOptions[i];

                _spriteBatch.Draw(_whitePixel, optionRects[i], bg);
                _spriteBatch.DrawString(_font, label,
                    new Vector2(optionRects[i].X + 8, optionRects[i].Y + 6), textColor);
            }
        }

        private void DrawAttackMenu(Vector2 viewportSize)
        {
            Rectangle[] optionRects = GetMenuOptionRects(viewportSize, _attackMenuLabels.Count);

            for (int i = 0; i < _attackMenuLabels.Count; i++)
            {
                bool selected = i == _attackMenuIndex;
                Color bg = selected ? new Color(255, 200, 0, 220) : new Color(0, 0, 0, 200);
                Color textColor = selected ? Color.Black : Color.White;

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
            foreach (var (tile, pathLength) in _reachableTiles)
            {
                int apCost = (int)Math.Ceiling(pathLength / (float)_playerUnit.TilesPerAP);
                bool affordable = apCost <= _playerUnit.CurrentAP;

                Vector2 worldPos = _hexGrid.HexToWorld(tile.col, tile.row);
                Color fill = affordable ? new Color(80, 160, 255, 110) : new Color(220, 50, 50, 100);
                Color outline = affordable ? Color.CornflowerBlue : Color.Red;
                DrawHexFilled(worldPos, fill, outline);

                if (_font != null)
                {
                    string costText = apCost.ToString();
                    Vector2 textSize = _font.MeasureString(costText);
                    _spriteBatch.DrawString(_font, costText, worldPos - textSize / 2f,
                        affordable ? Color.White : Color.OrangeRed);
                }
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

            var path = Pathfinder.FindPath(_hexGrid, _map, start, hoveredHex, occupied);
            Vector2? xMarkerPos = null;

            if (path == null)
            {
                List<(int col, int row)> bestPath = null;
                foreach (var neighbor in _hexGrid.GetNeighbors(hoveredHex.col, hoveredHex.row))
                {
                    var candidate = Pathfinder.FindPath(_hexGrid, _map, start, neighbor, occupied);
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

        private void DrawLineSimple(Vector2 start, Vector2 end, Color color)
        {
            float angle = (float)Math.Atan2(end.Y - start.Y, end.X - start.X);
            float length = Vector2.Distance(start, end);

            _spriteBatch.Draw(_whitePixel, start, null, color, angle, Vector2.Zero,
                            new Vector2(length, 1f), SpriteEffects.None, 0f);
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

        private void DrawConsole()
        {
            const int CONSOLE_HEIGHT = 300;
            int consoleY = GraphicsDevice.Viewport.Height - CONSOLE_HEIGHT;

            // Draw semi-transparent background
            _spriteBatch.Draw(_whitePixel,
                            new Rectangle(0, consoleY, GraphicsDevice.Viewport.Width, CONSOLE_HEIGHT),
                            new Color(0, 0, 0, 200));

            // Draw output
            int y = consoleY + 10;
            var output = _console.GetOutput().TakeLast(12).ToList();
            foreach (var line in output)
            {
                _spriteBatch.DrawString(_font, line, new Vector2(10, y), Color.White);
                y += 20;
            }

            // Draw input prompt
            _spriteBatch.DrawString(_font, "> " + _consoleInput + (DateTime.Now.Millisecond % 1000 < 500 ? "_" : ""),
                                   new Vector2(10, consoleY + CONSOLE_HEIGHT - 25),
                                   Color.LimeGreen);

            // Draw help text
            _spriteBatch.DrawString(_font, "Type 'help' for commands | ~ to close",
                                   new Vector2(10, consoleY + CONSOLE_HEIGHT - 45),
                                   Color.Gray);
        }

        protected override void UnloadContent()
        {
            _renderer.Dispose();
            _whitePixel?.Dispose();
            base.UnloadContent();
        }
    }
}
