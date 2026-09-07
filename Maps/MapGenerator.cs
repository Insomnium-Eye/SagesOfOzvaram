using System;
using System.Collections.Generic;
 
namespace SagesOfOzvaram.Maps
{
    /// <summary>
    /// Procedural map generator using Perlin noise.
    /// Generates coherent terrain features like coastlines, lakes, and mountain ranges.
    /// </summary>
    public class MapGenerator
    {
        private int _seed;
        private Random _random;
        private PerlinNoise _perlin;
 
        // Noise thresholds for terrain types
        private const float WATER_THRESHOLD = 0.35f;      // Very low noise = water
        private const float MOUNTAIN_THRESHOLD = 0.65f;   // High noise = mountains
        private const float FOREST_THRESHOLD = 0.55f;     // Medium-high = forest
        // Everything else = grass
 
        /// <summary>
        /// Create a new map generator with a seed.
        /// </summary>
        public MapGenerator(int seed)
        {
            _seed = seed;
            _random = new Random(seed);
            _perlin = new PerlinNoise(seed);
        }
 
        /// <summary>
        /// Generate a procedural map.
        /// </summary>
        public Map GenerateMap(int width, int height, float scale = 0.1f, int octaves = 4)
        {
            Map map = new Map(width, height);
            map.Name = $"Procedural Map (Seed: {_seed})";
            map.BiomeType = "mixed";
 
            // Generate terrain using Perlin noise with multiple octaves (Fractional Brownian Motion)
            for (int col = 0; col < width; col++)
            {
                for (int row = 0; row < height; row++)
                {
                    float noiseValue = GetOctaveNoise(col, row, scale, octaves);
                    
                    // Determine tile type based on noise value
                    Tile tile = map.GetTile(col, row);
                    tile.Type = GetTerrainType(noiseValue);
                    tile.Passable = tile.Type != "mountain"; // Only mountain is impassable for now; water is passable but costs extra AP (see Pathfinder)
                }
            }
 
            return map;
        }
 
        /// <summary>
        /// Get noise value using multiple octaves (Fractional Brownian Motion).
        /// </summary>
        private float GetOctaveNoise(int x, int y, float scale, int octaves)
        {
            float noiseValue = 0f;
            float amplitude = 1f;
            float frequency = 1f;
            float maxValue = 0f;
 
            for (int i = 0; i < octaves; i++)
            {
                float sampleX = x * scale * frequency;
                float sampleY = y * scale * frequency;
 
                noiseValue += _perlin.GetNoise(sampleX, sampleY) * amplitude;
                maxValue += amplitude;
 
                amplitude *= 0.5f;       // Reduce amplitude for each octave
                frequency *= 2f;         // Double frequency for each octave
            }
 
            // Normalize to 0-1 range
            return noiseValue / maxValue;
        }
 
        /// <summary>
        /// Map noise value to terrain type.
        /// </summary>
        private string GetTerrainType(float noiseValue)
        {
            // Clamp to 0-1
            noiseValue = Math.Max(0f, Math.Min(1f, noiseValue));
 
            if (noiseValue < WATER_THRESHOLD)
                return "water";
            else if (noiseValue < FOREST_THRESHOLD)
            {
                // Blend forest and grass in the medium range
                if (noiseValue < 0.45f && _random.NextDouble() > 0.3f)
                    return "forest";
                return "grass";
            }
            else if (noiseValue < MOUNTAIN_THRESHOLD)
                return "forest";
            else
                return "mountain";
        }
    }
 
    /// <summary>
    /// Perlin noise implementation for terrain generation.
    /// Based on Perlin's improved noise algorithm.
    /// </summary>
    public class PerlinNoise
    {
        private int[] _permutation;
 
        public PerlinNoise(int seed)
        {
            // Create permutation table with seed-based shuffling
            _permutation = new int[256];
            for (int i = 0; i < 256; i++)
                _permutation[i] = i;
 
            // Shuffle using seeded random
            Random random = new Random(seed);
            for (int i = 255; i > 0; i--)
            {
                int j = random.Next(i + 1);
                int temp = _permutation[i];
                _permutation[i] = _permutation[j];
                _permutation[j] = temp;
            }
        }
 
        /// <summary>
        /// Generate Perlin noise value for given coordinates.
        /// Returns value between 0 and 1.
        /// </summary>
        public float GetNoise(float x, float y)
        {
            // Find unit grid cell coordinates
            int xi = (int)Math.Floor(x) & 255;
            int yi = (int)Math.Floor(y) & 255;
 
            // Get relative coordinates within cell
            float xf = x - (int)Math.Floor(x);
            float yf = y - (int)Math.Floor(y);
 
            // Fade curves for smooth interpolation
            float u = Fade(xf);
            float v = Fade(yf);
 
            // Hash coordinates of 4 corners (with proper wrapping)
            int aa = _permutation[(_permutation[xi] + yi) & 255];
            int ab = _permutation[(_permutation[xi] + yi + 1) & 255];
            int ba = _permutation[(_permutation[xi + 1] + yi) & 255];
            int bb = _permutation[(_permutation[xi + 1] + yi + 1) & 255];
 
            // Generate and interpolate gradient values
            float n00 = DotGridGradient(aa, xf, yf);
            float n10 = DotGridGradient(ba, xf - 1, yf);
            float n01 = DotGridGradient(ab, xf, yf - 1);
            float n11 = DotGridGradient(bb, xf - 1, yf - 1);
 
            // Interpolate x
            float nx0 = Lerp(n00, n10, u);
            float nx1 = Lerp(n01, n11, u);
 
            // Interpolate y
            float result = Lerp(nx0, nx1, v);
 
            // Normalize to 0-1 range
            return (result + 1f) / 2f;
        }
 
        private float Fade(float t)
        {
            // Smoothstep: 6t^5 - 15t^4 + 10t^3
            return t * t * t * (t * (t * 6 - 15) + 10);
        }
 
        private float Lerp(float a, float b, float t)
        {
            return a + t * (b - a);
        }
 
        private float DotGridGradient(int hash, float x, float y)
        {
            // Get pseudo-random gradient vector
            int h = hash & 15;
            float u = h < 8 ? x : y;
            float v = h < 8 ? y : x;
 
            // Return dot product based on gradient direction
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
        }
    }
}
    