# Sages of Ozvaram - Map Creation System (Phase 1)

## Overview

Phase 1 delivers the foundation for map creation in C# + MonoGame:
- **Hex grid system** (rendering + coordinate math)
- **Data structures** (Tile, MapObject, Map, AssetRegistry)
- **Developer console** (map manipulation via commands)
- **JSON serialization** (save/load maps)
- **Hex grid renderer** (visual editing with zoom/pan)

---

## Architecture

### Core Systems

#### 1. **HexGrid.cs**
Handles hex coordinate system and math:
- Axial (odd-r offset) coordinates
- World position conversion (hex ↔ pixels)
- Neighbor calculations
- Distance calculations
- Bounds checking

**Key Methods**:
```csharp
Vector2 HexToWorld(int col, int row)           // Hex coords → pixel position
(int col, int row) WorldToHex(Vector2 worldPos) // Pixel position → hex coords
List<(int, int)> GetNeighbors(int col, int row) // Get 6 neighbors
int GetDistance(int col1, int row1, int col2, int row2)
bool IsInBounds(int col, int row)
```

#### 2. **MapData.cs**
Core data structures:

**Tile**:
- Position (col, row)
- Type ("grass", "stone", "water", etc.)
- Passable (bool)
- Roofed (bool)
- StatModifiers (dictionary)
- Objects on tile (stacked)

**MapObject**:
- ID (unique identifier)
- Type ("mountain", "tree", "structure", "unit", "furniture")
- AssetKey (reference to asset registry)
- Position (col, row)
- Height (0-3, elevation tier)
- Blocking/Walkable (bool)
- Rotation (float)
- Parent/Children IDs (for stacking)
- Properties (extensible data)

**Map**:
- Width, Height
- 2D grid of Tiles
- Dictionary of all Objects
- Metadata (biome type, difficulty, etc.)

**AssetRegistry**:
- Stores asset references (sprites + 3D models)
- Supports multiple asset types (Sprite, Model3D, ParticleEffect, Animation)
- Resolved at runtime

#### 3. **HexGridRenderer.cs**
MonoGame rendering for hex grid:
- Draws hex outlines with grid snapping
- Camera/zoom system (pan and zoom)
- Highlighted hex selection
- Hover effects
- Screen ↔ world coordinate conversion

**Key Methods**:
```csharp
void DrawGrid(Vector2 viewportSize)
void DrawHighlightedHex(int col, int row, Color color)
void DrawHoverHex(Vector2 cursorWorldPos)
void PanCamera(Vector2 delta)
void Zoom(float delta, Vector2 cursorPos)
Vector2 ScreenToWorld(Vector2 screenPos, Vector2 viewportSize)
(int col, int row) GetHexAtScreenPos(Vector2 screenPos, Vector2 viewportSize)
```

#### 4. **DevConsole.cs**
Developer command terminal for map manipulation:
- Tile creation/modification
- Object placement/removal
- Object inspection
- Map utilities
- Command history (up/down arrows)

**Disabled in**:
- Official story mode
- Multiplayer modes
- Enabled in editor mode only

#### 5. **MapSerializer.cs**
JSON save/load for maps:
- Serializes entire map state (tiles + objects)
- Human-readable JSON format
- Full round-trip (save → load maintains all data)
- Used for editor persistence and mod maps

---

## Usage Guide

### 1. Creating a Map in Code

```csharp
// Create new 20x15 hex map
Map map = new Map(20, 15);
map.Name = "Ardalis - Eastern Gate";
map.BiomeType = "coastal";

// Populate asset registry
AssetRegistry registry = new AssetRegistry();
registry.Register("mountain_standard", new AssetReference
{
    Key = "mountain_standard",
    Type = AssetReference.AssetType.Model3D,
    Path = "models/mountain_01.fbx",
    Scale = 1.5f
});

registry.Register("tree_oak", new AssetReference
{
    Key = "tree_oak",
    Type = AssetReference.AssetType.Sprite,
    Path = "sprites/tree_oak.png",
    Scale = 2.0f
});

// Initialize console
DevConsole console = new DevConsole(map, registry);
```

### 2. Using the Dev Console

#### Tile Commands

```
# Create tile at position
tile create 5 5 grass

# Create tile with properties
tile create 5 6 stone --passable true --roofed false

# Modify tile
tile set 5 5 type stone
tile set 5 5 passable false
tile set 5 5 stat armor 1.2

# Get tile info
tile info 5 5
```

#### Object Commands

```
# Place object
object place mountain_1 mountain_standard 5 5 --height 2 --blocking true

# Place furniture (walkable)
object place table_1 furniture_table 10 8 --height 1 --walkable true

# Place unit on furniture (stacking)
object place warrior_1 unit_warrior 10 8 --height 2 --blocking false --parent table_1

# Remove object
object remove mountain_1

# List all objects
object list

# List objects at specific tile
object list 5 5

# Modify object
object set mountain_1 height 3
object set mountain_1 blocking false
```

#### Map Commands

```
# Show map info
map info

# Clear all objects
map clear
```

### 3. Rendering the Hex Grid

```csharp
class MapEditorGame : Game
{
    private HexGrid _hexGrid;
    private HexGridRenderer _renderer;
    private Map _map;
    private DevConsole _console;
    private SpriteBatch _spriteBatch;

    protected override void Initialize()
    {
        _hexGrid = new HexGrid(20, 15, tileSize: 32f);
        _map = new Map(20, 15);
        _console = new DevConsole(_map, registry);
        
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _renderer = new HexGridRenderer(_hexGrid, _spriteBatch, GraphicsDevice);
    }

    protected override void Update(GameTime gameTime)
    {
        // Camera controls
        if (Keyboard.GetState().IsKeyDown(Keys.W))
            _renderer.PanCamera(Vector2.UnitY * 5f);
        if (Keyboard.GetState().IsKeyDown(Keys.S))
            _renderer.PanCamera(-Vector2.UnitY * 5f);
        if (Keyboard.GetState().IsKeyDown(Keys.A))
            _renderer.PanCamera(Vector2.UnitX * 5f);
        if (Keyboard.GetState().IsKeyDown(Keys.D))
            _renderer.PanCamera(-Vector2.UnitX * 5f);

        // Zoom
        var scrollState = Mouse.GetState();
        if (scrollState.ScrollWheelValue > 0)
            _renderer.Zoom(0.1f, new Vector2(scrollState.X, scrollState.Y));
        if (scrollState.ScrollWheelValue < 0)
            _renderer.Zoom(-0.1f, new Vector2(scrollState.X, scrollState.Y));
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        
        // Draw grid
        _renderer.DrawGrid(new Vector2(GraphicsDevice.Viewport.Width, 
                                       GraphicsDevice.Viewport.Height));

        // Draw highlighted hex at mouse
        var mouseState = Mouse.GetState();
        var hexCoords = _renderer.GetHexAtScreenPos(
            new Vector2(mouseState.X, mouseState.Y),
            new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height)
        );
        
        if (_hexGrid.IsInBounds(hexCoords.col, hexCoords.row))
        {
            Vector2 worldPos = _hexGrid.HexToWorld(hexCoords.col, hexCoords.row);
            _renderer.DrawHoverHex(worldPos);
        }
    }
}
```

### 4. Saving and Loading Maps

```csharp
// Save map to JSON
MapSerializer.SaveMap(map, "maps/ardalis_eastern_gate.json");

// Load map from JSON
Map loadedMap = MapSerializer.LoadMap("maps/ardalis_eastern_gate.json");
```

---

## JSON Map Format

```json
{
  "name": "Ardalis - Eastern Gate",
  "width": 20,
  "height": 15,
  "gridType": "hex",
  "biomeType": "coastal",
  "metaData": {
    "difficulty": "normal",
    "recommended_level": 5
  },
  "tiles": [
    {
      "col": 0,
      "row": 0,
      "type": "grass",
      "passable": true,
      "roofed": false,
      "statModifiers": {
        "armor": 1.0,
        "evasion": 1.0
      }
    }
  ],
  "objects": [
    {
      "id": "mountain_1",
      "type": "mountain",
      "assetKey": "mountain_standard",
      "col": 5,
      "row": 5,
      "height": 2,
      "blocking": true,
      "walkable": false,
      "rotation": 0,
      "parentId": null,
      "childrenIds": [],
      "properties": {}
    }
  ]
}
```

---

## Dev Console Integration

Add to your MonoGame game loop:

```csharp
private string _consoleInput = "";

protected override void Update(GameTime gameTime)
{
    var keyState = Keyboard.GetState();
    
    // Toggle console
    if (keyState.IsKeyDown(Keys.Grave)) // ~ key
        _console.IsOpen = !_console.IsOpen;

    if (_console.IsOpen)
    {
        // Text input
        char pressedChar = GetPressedChar(keyState);
        if (pressedChar != '\0')
            _consoleInput += pressedChar;

        // Backspace
        if (keyState.IsKeyDown(Keys.Back) && _consoleInput.Length > 0)
            _consoleInput = _consoleInput.Substring(0, _consoleInput.Length - 1);

        // Submit command
        if (keyState.IsKeyDown(Keys.Enter))
        {
            _console.ExecuteCommand(_consoleInput);
            _consoleInput = "";
        }

        // Command history
        if (keyState.IsKeyDown(Keys.Up))
            _consoleInput = _console.GetPreviousCommand();
        if (keyState.IsKeyDown(Keys.Down))
            _consoleInput = _console.GetNextCommand();
    }
}

protected override void Draw(GameTime gameTime)
{
    if (_console.IsOpen)
    {
        DrawConsoleUI();
    }
}

private void DrawConsoleUI()
{
    // Draw semi-transparent background
    _spriteBatch.Begin();
    _spriteBatch.Draw(_whitePixel, new Rectangle(0, 
                                                   GraphicsDevice.Viewport.Height - 300, 
                                                   GraphicsDevice.Viewport.Width, 300),
                      new Color(0, 0, 0, 200));

    // Draw output
    int y = GraphicsDevice.Viewport.Height - 280;
    foreach (var line in _console.GetOutput().TakeLast(10))
    {
        _spriteBatch.DrawString(_font, line, new Vector2(10, y), Color.White);
        y += 20;
    }

    // Draw input
    _spriteBatch.DrawString(_font, "> " + _consoleInput, 
                           new Vector2(10, GraphicsDevice.Viewport.Height - 30), 
                           Color.LimeGreen);

    _spriteBatch.End();
}
```

---

## Next Steps (Phase 2)

- [ ] Map editor UI (palette, inspector, toolbar)
- [ ] Asset registry UI (browse/import assets)
- [ ] Drag-and-drop object placement
- [ ] Object property inspector UI
- [ ] Undo/redo system
- [ ] Visual feedback (selection outlines, placement preview)
- [ ] More tile types and object types
- [ ] Environmental effects and hazards
- [ ] Spawn point definition
- [ ] Lighting/shadow system
- [ ] Export to game runtime format

---

## Files Included

1. **HexGrid.cs** - Hex coordinate system
2. **HexGridRenderer.cs** - MonoGame rendering
3. **MapData.cs** - Core data structures
4. **DevConsole.cs** - Developer terminal
5. **MapSerializer.cs** - JSON save/load

---

## Integration Checklist

- [ ] Copy all 5 .cs files to your project
- [ ] Add using statements: `using SagesOfOzvaram.Maps;`
- [ ] Create MonoGame game instance
- [ ] Initialize HexGrid, Map, AssetRegistry, DevConsole
- [ ] Add keyboard/input handling
- [ ] Implement rendering loop
- [ ] Test dev console commands
- [ ] Save/load a test map

---

## Notes

- **Asset types**: Currently supports Sprite and Model3D. Add more as needed.
- **Coordinate system**: Axial (odd-r offset) hexes. 0,0 is top-left.
- **Height tiers**: 0-3 represent different elevation levels.
- **Stacking**: Objects can have parent/children for multi-level placement.
- **Dev console**: Fully extensible. Add new commands in DevConsole.cs.

---

Ready to integrate Phase 1 or ask questions?
