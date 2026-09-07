using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SagesOfOzvaram.Maps
{
    /// <summary>
    /// Handles JSON serialization/deserialization for maps.
    /// Used for saving editor state and loading maps at runtime.
    /// </summary>
    public class MapSerializer
    {
        private static JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        /// <summary>
        /// Save map to JSON file.
        /// </summary>
        public static void SaveMap(Map map, string filePath)
        {
            try
            {
                // Create directory if it doesn't exist
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                // Serialize map to JSON
                var mapData = new MapJsonData
                {
                    Name = map.Name,
                    Width = map.Width,
                    Height = map.Height,
                    GridType = map.GridType,
                    BiomeType = map.BiomeType,
                    MetaData = map.MetaData,
                    Tiles = SerializeTiles(map),
                    Objects = SerializeObjects(map)
                };

                string json = JsonSerializer.Serialize(mapData, _jsonOptions);
                File.WriteAllText(filePath, json);

                Console.WriteLine($"Map saved to {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving map: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Load map from JSON file.
        /// </summary>
        public static Map LoadMap(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"Map file not found: {filePath}");
                }

                string json = File.ReadAllText(filePath);
                var mapData = JsonSerializer.Deserialize<MapJsonData>(json, _jsonOptions);

                if (mapData == null)
                {
                    throw new InvalidOperationException("Failed to deserialize map data");
                }

                // Recreate map
                Map map = new Map(mapData.Width, mapData.Height)
                {
                    Name = mapData.Name,
                    GridType = mapData.GridType,
                    BiomeType = mapData.BiomeType,
                    MetaData = mapData.MetaData ?? new()
                };

                // Load tiles
                if (mapData.Tiles != null)
                {
                    foreach (var tileData in mapData.Tiles)
                    {
                        Tile tile = map.GetTile(tileData.Col, tileData.Row);
                        if (tile != null)
                        {
                            tile.Type = tileData.Type;
                            tile.Passable = tileData.Passable;
                            tile.Roofed = tileData.Roofed;
                            tile.StatModifiers = tileData.StatModifiers ?? new();
                        }
                    }
                }

                // Load objects
                if (mapData.Objects != null)
                {
                    foreach (var objData in mapData.Objects)
                    {
                        MapObject obj = new MapObject
                        {
                            Id = objData.Id,
                            Type = objData.Type,
                            AssetKey = objData.AssetKey,
                            Col = objData.Col,
                            Row = objData.Row,
                            Height = objData.Height,
                            Blocking = objData.Blocking,
                            Walkable = objData.Walkable,
                            Rotation = objData.Rotation,
                            ParentId = objData.ParentId,
                            ChildrenIds = objData.ChildrenIds ?? new(),
                            Properties = objData.Properties ?? new()
                        };

                        map.AddObject(obj);
                    }
                }

                Console.WriteLine($"Map loaded from {filePath}");
                return map;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading map: {ex.Message}");
                throw;
            }
        }

        private static List<TileJsonData> SerializeTiles(Map map)
        {
            var tiles = new List<TileJsonData>();

            for (int col = 0; col < map.Width; col++)
            {
                for (int row = 0; row < map.Height; row++)
                {
                    Tile tile = map.GetTile(col, row);
                    if (tile != null)
                    {
                        tiles.Add(new TileJsonData
                        {
                            Col = col,
                            Row = row,
                            Type = tile.Type,
                            Passable = tile.Passable,
                            Roofed = tile.Roofed,
                            StatModifiers = tile.StatModifiers
                        });
                    }
                }
            }

            return tiles;
        }

        private static List<MapObjectJsonData> SerializeObjects(Map map)
        {
            var objects = new List<MapObjectJsonData>();

            foreach (var obj in map.AllObjectsById.Values)
            {
                objects.Add(new MapObjectJsonData
                {
                    Id = obj.Id,
                    Type = obj.Type,
                    AssetKey = obj.AssetKey,
                    Col = obj.Col,
                    Row = obj.Row,
                    Height = obj.Height,
                    Blocking = obj.Blocking,
                    Walkable = obj.Walkable,
                    Rotation = obj.Rotation,
                    ParentId = obj.ParentId,
                    ChildrenIds = obj.ChildrenIds,
                    Properties = obj.Properties
                });
            }

            return objects;
        }

        // JSON Data classes for serialization
        private class MapJsonData
        {
            public string Name { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public string GridType { get; set; }
            public string BiomeType { get; set; }
            public Dictionary<string, object> MetaData { get; set; }
            public List<TileJsonData> Tiles { get; set; }
            public List<MapObjectJsonData> Objects { get; set; }
        }

        private class TileJsonData
        {
            public int Col { get; set; }
            public int Row { get; set; }
            public string Type { get; set; }
            public bool Passable { get; set; }
            public bool Roofed { get; set; }
            public Dictionary<string, float> StatModifiers { get; set; }
        }

        private class MapObjectJsonData
        {
            public string Id { get; set; }
            public string Type { get; set; }
            public string AssetKey { get; set; }
            public int Col { get; set; }
            public int Row { get; set; }
            public int Height { get; set; }
            public bool Blocking { get; set; }
            public bool Walkable { get; set; }
            public float Rotation { get; set; }
            public string ParentId { get; set; }
            public List<string> ChildrenIds { get; set; }
            public Dictionary<string, object> Properties { get; set; }
        }
    }
}
