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

        private int _selectedCharacterIndex = 0;  // Sorcerer is the default selection
        private BaseUnit _playerUnit;

        // Turn menu (shown, next to the player's unit, when it's their turn)
        private static readonly string[] TurnMenuOptions = { "Attack", "Cards", "Items", "End", "Move", "View Map" };
        private int _turnMenuIndex = 0;
        private bool _turnMenuActive = false;
        private bool _viewingMap = false;

        // Attack submenu (opened from the "Attack" turn-menu option)
        private List<string> _attackMenuLabels = new List<string>();
        private int _attackMenuIndex = 0;
        private bool _attackMenuActive = false;

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

            // Load the spell card template (per-card art and the finished art+template composite
            // are both built lazily, see GetCardArt/GetCardComposite).
            _spellCardTemplate = LoadTextureFromDisk(Path.Combine("Content", "imgs", "Cards", "Spells", "SpellCard.png"));

            // Same deal for the summon card template (see GetSummonCardArt/GetSummonCardComposite).
            _summonCardTemplate = LoadTextureFromDisk(Path.Combine("Content", "imgs", "Cards", "Summons", "SummonCard.png"));

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
                        _movementModeActive = false;
                        _cardMenuActive = false;

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
            else if (option == "Cards")
            {
                _turnMenuActive = false;
                OpenCardMenu();
            }
        }

        /// <summary>
        /// Open the card hand: every spell card the player unit's class has access to (via
        /// SpellCatalog) plus every summon card (via SummonCatalog, not class-gated), combined
        /// into one browsable hand of playing cards - A/D cycles through them, rendered
        /// dynamically through DrawSpellCard/DrawSummonCard via DrawHandCard. Just a viewer for
        /// now - no deck/draw system, spell-casting, or summon-to-battlefield mechanics exist yet.
        /// </summary>
        private void OpenCardMenu()
        {
            _availableHandCards = new List<object>();
            _availableHandCards.AddRange(SpellCatalog.GetSpellsForClass(_playerUnit.Class));
            _availableHandCards.AddRange(SummonCatalog.AllSummons);
            _handCardIndex = 0;
            _cardMenuActive = true;
        }

        private void HandleCardMenuInput(KeyboardState keyboardState, MouseState mouseState)
        {
            if (_availableHandCards.Count > 1)
            {
                if (keyboardState.IsKeyDown(Keys.D) && !_previousKeyboardState.IsKeyDown(Keys.D))
                    _handCardIndex = (_handCardIndex + 1) % _availableHandCards.Count;
                if (keyboardState.IsKeyDown(Keys.A) && !_previousKeyboardState.IsKeyDown(Keys.A))
                    _handCardIndex = (_handCardIndex - 1 + _availableHandCards.Count) % _availableHandCards.Count;

                // Clicking a peeking side card highlights (selects) it, same as A/D.
                if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
                {
                    Vector2 viewportSize = new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
                    var layout = GetHandCardLayout(viewportSize);
                    Point clickPos = new Point(mouseState.X, mouseState.Y);
                    if (layout.prev.Contains(clickPos))
                        _handCardIndex = (_handCardIndex - 1 + _availableHandCards.Count) % _availableHandCards.Count;
                    else if (layout.next.Contains(clickPos))
                        _handCardIndex = (_handCardIndex + 1) % _availableHandCards.Count;
                }
            }

            if (keyboardState.IsKeyDown(Keys.E) && !_previousKeyboardState.IsKeyDown(Keys.E))
            {
                _cardMenuActive = false;
                _turnMenuIndex = 0;
                _turnMenuActive = true;
            }
        }

        /// <summary>
        /// Enter movement mode: computes _reachableTiles out to a bit beyond the unit's current
        /// AP budget (so out-of-range tiles nearby still show, tinted red - see DrawMovementRange).
        /// </summary>
        private void OpenMovementMode()
        {
            var start = _hexGrid.WorldToHex(_playerUnit.Position);
            float displayApBudget = _playerUnit.CurrentAP + 2f; // show a bit beyond current reach too, in red

            _reachableTiles = Pathfinder.GetReachableTiles(_hexGrid, _map, start, _playerUnit.TilesPerAP, displayApBudget, GetOccupiedTiles(_playerUnit));
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
            var path = Pathfinder.FindPath(_hexGrid, _map, start, destination, _playerUnit.TilesPerAP, GetOccupiedTiles(_playerUnit));
            if (path == null || path.Count == 0)
                return;

            // Walk the path tile by tile, stopping at the last one the unit can still afford
            // (using the exact integer charge formula, not Dijkstra's float approximation).
            int tilesPerAP = _playerUnit.TilesPerAP;
            int tiles = 0, water = 0, lastAffordableIndex = -1;
            for (int i = 0; i < path.Count; i++)
            {
                var (col, row) = path[i];
                bool isWater = _map.GetTile(col, row)?.Type == "water";
                int candidateTiles = tiles + 1;
                int candidateWater = water + (isWater ? 1 : 0);
                int candidateApCost = (int)Math.Ceiling(candidateTiles / (float)tilesPerAP) + candidateWater;
                if (candidateApCost > _playerUnit.CurrentAP)
                    break;

                tiles = candidateTiles;
                water = candidateWater;
                lastAffordableIndex = i;
            }

            if (lastAffordableIndex < 0)
                return; // can't afford even the first step

            var actualDestination = path[lastAffordableIndex];
            int apCost = (int)Math.Ceiling(tiles / (float)tilesPerAP) + water;

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
            if (!_turnMenuActive && !_attackMenuActive && !_cardMenuActive)
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

        private void DrawLineSimple(Vector2 start, Vector2 end, Color color)
        {
            float angle = (float)Math.Atan2(end.Y - start.Y, end.X - start.X);
            float length = Vector2.Distance(start, end);

            _spriteBatch.Draw(_whitePixel, start, null, color, angle, Vector2.Zero,
                            new Vector2(length, 1f), SpriteEffects.None, 0f);
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

            DrawTextCentered(card.Name, FractionalRect(destRect, CardNameBarRegion), Color.White);
            DrawTextCentered(card.Effect.MPCost.ToString(), FractionalRect(destRect, CardCostCircleRegion), Color.Black);
            DrawTextCentered(card.ClassLabel, FractionalRect(destRect, CardTypeBarRegion), Color.White);
            DrawWrappedText(card.Description, FractionalRect(destRect, CardDescriptionRegion), Color.Black);

            Rectangle damageRect = FractionalRect(destRect, CardDamageCircleRegion);
            DrawWandGlyph(damageRect);
            DrawTextCentered(card.Effect.GetDamage(caster).ToString(), damageRect, Color.Black);
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

            DrawTextCentered(card.Name, FractionalRect(destRect, SummonNameBarRegion), Color.White);
            DrawTextCentered(card.ManaCost.ToString(), FractionalRect(destRect, SummonManaCostCircleRegion), Color.Black);

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
        /// The on-screen rects for the hand's previous/center/next cards - shared between
        /// drawing and click hit-testing so they can never drift out of sync with each other.
        /// </summary>
        private (Rectangle prev, Rectangle center, Rectangle next) GetHandCardLayout(Vector2 viewportSize)
        {
            float centerHeight = viewportSize.Y * 0.8f;
            float sideHeight = centerHeight * 0.75f;
            float sideOffsetX = viewportSize.X * 0.24f;
            return (
                CenteredCardRect(viewportSize, sideHeight, -sideOffsetX),
                CenteredCardRect(viewportSize, centerHeight, 0f),
                CenteredCardRect(viewportSize, sideHeight, sideOffsetX));
        }

        /// <summary>
        /// Full-screen-ish hand-of-cards viewer: dims the background and shows the player's
        /// available spell + summon cards as a browsable hand - the selected card centered and
        /// large, with the previous/next cards peeking out smaller to either side (drawn first,
        /// so the selected card overlaps them; clicking a side card selects it too, see
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

                if (_availableHandCards.Count > 1)
                {
                    int prevIndex = (_handCardIndex - 1 + _availableHandCards.Count) % _availableHandCards.Count;
                    int nextIndex = (_handCardIndex + 1) % _availableHandCards.Count;
                    DrawHandCard(_availableHandCards[prevIndex], layout.prev);
                    DrawHandCard(_availableHandCards[nextIndex], layout.next);
                }

                object centerCard = _availableHandCards[_handCardIndex];
                DrawHandCard(centerCard, layout.center);

                if (_font != null)
                {
                    string counter = $"{_handCardIndex + 1} / {_availableHandCards.Count}";
                    Vector2 size = _font.MeasureString(counter);
                    _spriteBatch.DrawString(_font, counter, new Vector2((viewportSize.X - size.X) / 2f, viewportSize.Y * 0.04f), Color.White);
                }

                Point mousePos = Mouse.GetState().Position;
                if (layout.center.Contains(mousePos))
                {
                    string tooltip = GetHoveredCardTooltip(centerCard, layout.center, mousePos);
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

            DrawBottomHint(viewportSize, _availableHandCards.Count > 1 ? "A/D or click a side card to browse - E to close" : "E to close");
        }

        /// <summary>
        /// Which tooltip (if any) applies to the field the cursor is over on a hand card,
        /// checked against the same fractional regions the card's numbers are actually drawn
        /// into - so a tooltip can never point at the wrong field.
        /// </summary>
        private string GetHoveredCardTooltip(object card, Rectangle destRect, Point mousePos)
        {
            if (card is SpellCard)
            {
                if (FractionalRect(destRect, CardCostCircleRegion).Contains(mousePos))
                    return "MP Cost: mana this spell costs to cast.";
                if (FractionalRect(destRect, CardDamageCircleRegion).Contains(mousePos))
                    return "Damage: magic damage this spell deals, computed from the caster's Intelligence.";
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
