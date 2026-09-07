using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace SagesOfOzvaram.Maps
{
    /// <summary>
    /// Developer console for map creation and testing.
    /// Only enabled in editor/dev mode.
    /// </summary>
    public class DevConsole
    {
        private Map _map;
        private AssetRegistry _assetRegistry;
        private List<string> _commandHistory = new();
        private List<string> _output = new();
        private int _historyIndex = -1;
        private const int MAX_OUTPUT_LINES = 100;

        public bool IsEnabled { get; set; } = true;
        public bool IsOpen { get; set; } = false;

        public DevConsole(Map map, AssetRegistry assetRegistry)
        {
            _map = map;
            _assetRegistry = assetRegistry;
            AddOutput("Developer Console initialized. Type 'help' for commands.");
        }

        /// <summary>
        /// Execute a console command.
        /// </summary>
        public void ExecuteCommand(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return;

            AddOutput($"> {input}");
            _commandHistory.Add(input);
            _historyIndex = -1;

            string[] parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string command = parts[0].ToLower();

            try
            {
                switch (command)
                {
                    case "help":
                        ShowHelp();
                        break;
                    case "tile":
                        HandleTileCommand(parts);
                        break;
                    case "object":
                        HandleObjectCommand(parts);
                        break;
                    case "map":
                        HandleMapCommand(parts);
                        break;
                    case "clear":
                        ClearOutput();
                        break;
                    default:
                        AddOutput($"Unknown command: {command}. Type 'help' for available commands.");
                        break;
                }
            }
            catch (Exception ex)
            {
                AddOutput($"Error: {ex.Message}");
            }
        }

        private void HandleTileCommand(string[] parts)
        {
            if (parts.Length < 2)
            {
                AddOutput("Usage: tile <action> [args...]");
                return;
            }

            string action = parts[1].ToLower();

            switch (action)
            {
                case "create":
                    TileCreate(parts);
                    break;
                case "set":
                    TileSet(parts);
                    break;
                case "info":
                    TileInfo(parts);
                    break;
                default:
                    AddOutput($"Unknown tile action: {action}");
                    break;
            }
        }

        private void TileCreate(string[] parts)
        {
            // tile create <col> <row> <type> [--passable true/false] [--roofed true/false]
            if (parts.Length < 5)
            {
                AddOutput("Usage: tile create <col> <row> <type> [--passable true/false] [--roofed true/false]");
                return;
            }

            int col = int.Parse(parts[2]);
            int row = int.Parse(parts[3]);
            string type = parts[4];

            if (!_map.IsInBounds(col, row))
            {
                AddOutput($"Error: Coordinates ({col}, {row}) out of bounds!");
                return;
            }

            Tile tile = _map.GetTile(col, row);
            tile.Type = type;

            // Parse optional flags
            for (int i = 5; i < parts.Length - 1; i++)
            {
                if (parts[i] == "--passable")
                    tile.Passable = bool.Parse(parts[i + 1]);
                if (parts[i] == "--roofed")
                    tile.Roofed = bool.Parse(parts[i + 1]);
            }

            AddOutput($"Created tile at ({col}, {row}): type={type}, passable={tile.Passable}, roofed={tile.Roofed}");
        }

        private void TileSet(string[] parts)
        {
            // tile set <col> <row> <property> <value>
            if (parts.Length < 5)
            {
                AddOutput("Usage: tile set <col> <row> <property> <value>");
                return;
            }

            int col = int.Parse(parts[2]);
            int row = int.Parse(parts[3]);
            string property = parts[4].ToLower();
            string value = string.Join(" ", parts.Skip(5));

            Tile tile = _map.GetTile(col, row);
            if (tile == null)
            {
                AddOutput($"Error: No tile at ({col}, {row})");
                return;
            }

            switch (property)
            {
                case "type":
                    tile.Type = value;
                    AddOutput($"Set tile ({col}, {row}) type to '{value}'");
                    break;
                case "passable":
                    tile.Passable = bool.Parse(value);
                    AddOutput($"Set tile ({col}, {row}) passable to {tile.Passable}");
                    break;
                case "roofed":
                    tile.Roofed = bool.Parse(value);
                    AddOutput($"Set tile ({col}, {row}) roofed to {tile.Roofed}");
                    break;
                case "stat":
                    // tile set <col> <row> stat <statname> <value>
                    if (parts.Length < 6)
                    {
                        AddOutput("Usage: tile set <col> <row> stat <statname> <value>");
                        return;
                    }
                    string statName = parts[5];
                    float statValue = float.Parse(parts[6]);
                    tile.StatModifiers[statName] = statValue;
                    AddOutput($"Set tile ({col}, {row}) stat '{statName}' to {statValue}");
                    break;
                default:
                    AddOutput($"Unknown property: {property}");
                    break;
            }
        }

        private void TileInfo(string[] parts)
        {
            // tile info <col> <row>
            if (parts.Length < 4)
            {
                AddOutput("Usage: tile info <col> <row>");
                return;
            }

            int col = int.Parse(parts[2]);
            int row = int.Parse(parts[3]);

            Tile tile = _map.GetTile(col, row);
            if (tile == null)
            {
                AddOutput($"Error: No tile at ({col}, {row})");
                return;
            }

            AddOutput($"Tile ({col}, {row}):");
            AddOutput($"  Type: {tile.Type}");
            AddOutput($"  Passable: {tile.Passable}");
            AddOutput($"  Roofed: {tile.Roofed}");
            AddOutput($"  Stat Modifiers: {string.Join(", ", tile.StatModifiers.Select(kv => $"{kv.Key}={kv.Value}"))}");
            AddOutput($"  Objects: {tile.Objects.Count}");
            foreach (var obj in tile.Objects)
            {
                AddOutput($"    - {obj.Id} ({obj.Type}) height={obj.Height}");
            }
        }

        private void HandleObjectCommand(string[] parts)
        {
            if (parts.Length < 2)
            {
                AddOutput("Usage: object <action> [args...]");
                return;
            }

            string action = parts[1].ToLower();

            switch (action)
            {
                case "place":
                    ObjectPlace(parts);
                    break;
                case "remove":
                    ObjectRemove(parts);
                    break;
                case "list":
                    ObjectList(parts);
                    break;
                case "set":
                    ObjectSet(parts);
                    break;
                default:
                    AddOutput($"Unknown object action: {action}");
                    break;
            }
        }

        private void ObjectPlace(string[] parts)
        {
            // object place <id> <assetkey> <col> <row> [--height 0] [--blocking true] [--walkable false] [--rotation 0]
            if (parts.Length < 5)
            {
                AddOutput("Usage: object place <id> <assetkey> <col> <row> [--height 0] [--blocking true] [--walkable false]");
                return;
            }

            string id = parts[2];
            string assetKey = parts[3];
            int col = int.Parse(parts[4]);
            int row = int.Parse(parts[5]);

            // Check if asset exists
            if (!_assetRegistry.Contains(assetKey))
            {
                AddOutput($"Error: Asset '{assetKey}' not found in registry!");
                return;
            }

            if (!_map.IsInBounds(col, row))
            {
                AddOutput($"Error: Coordinates ({col}, {row}) out of bounds!");
                return;
            }

            // Check if ID already exists
            if (_map.AllObjectsById.ContainsKey(id))
            {
                AddOutput($"Error: Object with ID '{id}' already exists!");
                return;
            }

            MapObject obj = new MapObject(id, "generic", assetKey, col, row);

            // Parse optional parameters
            for (int i = 6; i < parts.Length - 1; i++)
            {
                if (parts[i] == "--height")
                    obj.Height = int.Parse(parts[i + 1]);
                if (parts[i] == "--blocking")
                    obj.Blocking = bool.Parse(parts[i + 1]);
                if (parts[i] == "--walkable")
                    obj.Walkable = bool.Parse(parts[i + 1]);
                if (parts[i] == "--rotation")
                    obj.Rotation = float.Parse(parts[i + 1]);
            }

            _map.AddObject(obj);
            AddOutput($"Placed object '{id}' ({assetKey}) at ({col}, {row}) height={obj.Height}");
        }

        private void ObjectRemove(string[] parts)
        {
            // object remove <id>
            if (parts.Length < 3)
            {
                AddOutput("Usage: object remove <id>");
                return;
            }

            string id = parts[2];

            if (!_map.AllObjectsById.ContainsKey(id))
            {
                AddOutput($"Error: Object '{id}' not found!");
                return;
            }

            _map.RemoveObject(id);
            AddOutput($"Removed object '{id}'");
        }

        private void ObjectList(string[] parts)
        {
            // object list [col] [row]
            if (parts.Length == 2)
            {
                // List all objects
                AddOutput($"All objects ({_map.AllObjectsById.Count}):");
                foreach (var obj in _map.AllObjectsById.Values)
                {
                    AddOutput($"  {obj.Id} ({obj.Type}) at ({obj.Col}, {obj.Row}) height={obj.Height}");
                }
            }
            else if (parts.Length >= 4)
            {
                // List objects at specific tile
                int col = int.Parse(parts[2]);
                int row = int.Parse(parts[3]);

                var objects = _map.GetObjectsAt(col, row);
                AddOutput($"Objects at ({col}, {row}): {objects.Count}");
                foreach (var obj in objects)
                {
                    AddOutput($"  {obj.Id} ({obj.Type}) height={obj.Height}");
                }
            }
            else
            {
                AddOutput("Usage: object list [col] [row]");
            }
        }

        private void ObjectSet(string[] parts)
        {
            // object set <id> <property> <value>
            if (parts.Length < 5)
            {
                AddOutput("Usage: object set <id> <property> <value>");
                return;
            }

            string id = parts[2];
            string property = parts[3].ToLower();
            string value = string.Join(" ", parts.Skip(4));

            MapObject obj = _map.GetObject(id);
            if (obj == null)
            {
                AddOutput($"Error: Object '{id}' not found!");
                return;
            }

            switch (property)
            {
                case "height":
                    obj.Height = int.Parse(value);
                    break;
                case "blocking":
                    obj.Blocking = bool.Parse(value);
                    break;
                case "walkable":
                    obj.Walkable = bool.Parse(value);
                    break;
                case "rotation":
                    obj.Rotation = float.Parse(value);
                    break;
                default:
                    AddOutput($"Unknown property: {property}");
                    return;
            }

            AddOutput($"Set object '{id}' {property} to {value}");
        }

        private void HandleMapCommand(string[] parts)
        {
            if (parts.Length < 2)
            {
                AddOutput("Usage: map <action> [args...]");
                return;
            }

            string action = parts[1].ToLower();

            switch (action)
            {
                case "info":
                    AddOutput($"Map: {_map.Name} ({_map.Width}x{_map.Height})");
                    AddOutput($"Biome: {_map.BiomeType}");
                    AddOutput($"Objects: {_map.AllObjectsById.Count}");
                    break;
                case "clear":
                    _map.ClearObjects();
                    AddOutput("Cleared all objects from map");
                    break;
                default:
                    AddOutput($"Unknown map action: {action}");
                    break;
            }
        }

        private void ShowHelp()
        {
            AddOutput("=== Developer Console Commands ===");
            AddOutput("tile create <col> <row> <type> [--passable true/false] [--roofed true/false]");
            AddOutput("tile set <col> <row> <property> <value>");
            AddOutput("tile info <col> <row>");
            AddOutput("object place <id> <assetkey> <col> <row> [--height 0] [--blocking true] [--walkable false]");
            AddOutput("object remove <id>");
            AddOutput("object list [col] [row]");
            AddOutput("object set <id> <property> <value>");
            AddOutput("map info");
            AddOutput("map clear");
            AddOutput("clear - Clear console output");
            AddOutput("help - Show this message");
        }

        private void AddOutput(string text)
        {
            _output.Add(text);
            if (_output.Count > MAX_OUTPUT_LINES)
                _output.RemoveAt(0);
        }

        private void ClearOutput()
        {
            _output.Clear();
        }

        public List<string> GetOutput()
        {
            return new List<string>(_output);
        }

        public string GetPreviousCommand()
        {
            if (_commandHistory.Count == 0) return "";
            _historyIndex = Math.Min(_historyIndex + 1, _commandHistory.Count - 1);
            return _commandHistory[_commandHistory.Count - 1 - _historyIndex];
        }

        public string GetNextCommand()
        {
            if (_historyIndex <= 0) return "";
            _historyIndex--;
            return _commandHistory[_commandHistory.Count - 1 - _historyIndex];
        }
    }
}
