using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
 
namespace SagesOfOzvaram.Maps
{
    /// <summary>
    /// Tile type enum for different terrain.
    /// </summary>
    public enum TileType
    {
        Grass,
        Water,
        Mountain,
        Forest,
        Desert,
        Stone,
        Dirt
    }
 
    /// <summary>
    /// Represents a single hex tile on the map.
    /// </summary>
    public class Tile
    {
        public int Col { get; set; }
        public int Row { get; set; }
        public string Type { get; set; } = "grass"; // "grass", "stone", "water", "lava", etc.
        public bool Passable { get; set; } = true;
        public bool Roofed { get; set; } = false;
        public Dictionary<string, float> StatModifiers { get; set; } = new();
 
        // Objects on this tile (stacked)
        public List<MapObject> Objects { get; set; } = new();
 
        public Tile() { }
 
        public Tile(int col, int row)
        {
            Col = col;
            Row = row;
        }
 
        public Tile Clone()
        {
            return new Tile
            {
                Col = Col,
                Row = Row,
                Type = Type,
                Passable = Passable,
                Roofed = Roofed,
                StatModifiers = new Dictionary<string, float>(StatModifiers),
                Objects = new List<MapObject>(Objects)
            };
        }
    }
 
    /// <summary>
    /// Represents any object placed on the map (buildings, furniture, units, etc).
    /// Objects can have parent/children for stacking.
    /// </summary>
    public class MapObject
    {
        public string Id { get; set; }
        public string Type { get; set; } // "mountain", "tree", "structure", "unit", "furniture"
        public string AssetKey { get; set; } // Reference to asset in registry
        public int Col { get; set; }
        public int Row { get; set; }
        public int Height { get; set; } = 0; // 0-3 elevation tiers
        public bool Blocking { get; set; } = true; // Blocks movement/vision?
        public bool Walkable { get; set; } = false; // Can units walk ON it?
        public float Rotation { get; set; } = 0f;
 
        // Parent/child relationships for stacking
        public string ParentId { get; set; } = null; // ID of object this sits on
        public List<string> ChildrenIds { get; set; } = new();
 
        // Extensible properties
        public Dictionary<string, object> Properties { get; set; } = new();
 
        public MapObject() { }
 
        public MapObject(string id, string type, string assetKey, int col, int row)
        {
            Id = id;
            Type = type;
            AssetKey = assetKey;
            Col = col;
            Row = row;
        }
 
        public MapObject Clone()
        {
            return new MapObject
            {
                Id = Id,
                Type = Type,
                AssetKey = AssetKey,
                Col = Col,
                Row = Row,
                Height = Height,
                Blocking = Blocking,
                Walkable = Walkable,
                Rotation = Rotation,
                ParentId = ParentId,
                ChildrenIds = new List<string>(ChildrenIds),
                Properties = new Dictionary<string, object>(Properties)
            };
        }
    }
 
    /// <summary>
    /// Represents the entire map.
    /// </summary>
    public class Map
    {
        public string Name { get; set; } = "Untitled Map";
        public int Width { get; set; } = 20;
        public int Height { get; set; } = 15;
        public string GridType { get; set; } = "hex";
        public string BiomeType { get; set; } = "forest";
        public Dictionary<string, object> MetaData { get; set; } = new();
 
        // Grid of tiles
        public Tile[,] Grid { get; set; }
 
        // All objects (for quick lookup)
        public Dictionary<string, MapObject> AllObjectsById { get; set; } = new();
 
        public Map() 
        {
            InitializeGrid();
        }
 
        public Map(int width, int height)
        {
            Width = width;
            Height = height;
            InitializeGrid();
        }
 
        private void InitializeGrid()
        {
            Grid = new Tile[Width, Height];
            for (int col = 0; col < Width; col++)
            {
                for (int row = 0; row < Height; row++)
                {
                    Grid[col, row] = new Tile(col, row);
                }
            }
        }
 
        /// <summary>
        /// Get tile at coordinates.
        /// </summary>
        public Tile GetTile(int col, int row)
        {
            if (col >= 0 && col < Width && row >= 0 && row < Height)
                return Grid[col, row];
            return null;
        }
 
        /// <summary>
        /// Add object to map and place on tile.
        /// </summary>
        public void AddObject(MapObject obj)
        {
            if (!IsInBounds(obj.Col, obj.Row))
            {
                Console.WriteLine($"Error: Object {obj.Id} at ({obj.Col}, {obj.Row}) is out of bounds!");
                return;
            }
 
            Tile tile = GetTile(obj.Col, obj.Row);
            tile.Objects.Add(obj);
            AllObjectsById[obj.Id] = obj;
        }
 
        /// <summary>
        /// Remove object from map.
        /// </summary>
        public void RemoveObject(string objectId)
        {
            if (AllObjectsById.TryGetValue(objectId, out var obj))
            {
                Tile tile = GetTile(obj.Col, obj.Row);
                tile.Objects.Remove(obj);
                AllObjectsById.Remove(objectId);
 
                // Update parent/child relationships
                if (!string.IsNullOrEmpty(obj.ParentId) && AllObjectsById.TryGetValue(obj.ParentId, out var parent))
                {
                    parent.ChildrenIds.Remove(objectId);
                }
 
                foreach (string childId in obj.ChildrenIds)
                {
                    if (AllObjectsById.TryGetValue(childId, out var child))
                    {
                        child.ParentId = null;
                    }
                }
            }
        }
 
        /// <summary>
        /// Get object by ID.
        /// </summary>
        public MapObject GetObject(string id)
        {
            AllObjectsById.TryGetValue(id, out var obj);
            return obj;
        }
 
        /// <summary>
        /// Get all objects at a tile.
        /// </summary>
        public List<MapObject> GetObjectsAt(int col, int row)
        {
            var tile = GetTile(col, row);
            return tile?.Objects ?? new List<MapObject>();
        }
 
        /// <summary>
        /// Check if coordinates are in bounds.
        /// </summary>
        public bool IsInBounds(int col, int row)
        {
            return col >= 0 && col < Width && row >= 0 && row < Height;
        }
 
        /// <summary>
        /// Clear all objects from map.
        /// </summary>
        public void ClearObjects()
        {
            foreach (var tile in Grid)
            {
                if (tile != null)
                    tile.Objects.Clear();
            }
            AllObjectsById.Clear();
        }
    }
 
    /// <summary>
    /// Asset reference with type information.
    /// </summary>
    public class AssetReference
    {
        public enum AssetType
        {
            Sprite,
            Model3D,
            ParticleEffect,
            Animation
        }
 
        public string Key { get; set; }
        public AssetType Type { get; set; }
        public string Path { get; set; }
        public float Scale { get; set; } = 1f;
        public Vector2 Pivot { get; set; } = new Vector2(0.5f, 0.5f);
    }
 
    /// <summary>
    /// Asset registry for all map assets.
    /// </summary>
    public class AssetRegistry
    {
        public Dictionary<string, AssetReference> Assets { get; set; } = new();
 
        public void Register(string key, AssetReference asset)
        {
            Assets[key] = asset;
        }
 
        public AssetReference Get(string key)
        {
            Assets.TryGetValue(key, out var asset);
            return asset;
        }
 
        public bool Contains(string key)
        {
            return Assets.ContainsKey(key);
        }
    }
}
