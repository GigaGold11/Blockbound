using Blockbound.Blocks;
using Blockbound.Chunks;
using Blockbound.Core;
using UnityEngine;

namespace Blockbound.Generation
{
    public static class OreGenerator
    {
        private const ushort StoneId = 3;
        private const ushort CoalOreId = 11;
        private const ushort CopperOreId = 12;
        private const ushort IronOreId = 13;

        public static void Generate(Chunk chunk, int seed, int[,] surfaceY, SeededTerrainGenerator.BiomeType[,] biomeMap)
        {
            // Coal: most common, higher underground
            PlaceOreType(chunk, seed + 120000, CoalOreId, 3, 85, 185, 3, 6);

            // Copper: common, slightly lower than coal on average
            PlaceOreType(chunk, seed + 121000, CopperOreId, 2, 70, 155, 3, 5);

            // Iron: medium depth, rarer than coal/copper
            PlaceOreType(chunk, seed + 122000, IronOreId, 1, 35, 105, 2, 4);
        }

        private static void PlaceOreType(Chunk chunk, int seed, ushort oreId, int attempts, int minY, int maxY, int minVein, int maxVein)
        {
            int baseWorldX = chunk.Coord.x * VoxelConstants.ChunkSize;
            int baseWorldZ = chunk.Coord.y * VoxelConstants.ChunkSize;

            for (int i = 0; i < attempts; i++)
            {
                int worldX = baseWorldX + Mathf.FloorToInt(Hash01(baseWorldX, baseWorldZ, seed + i * 31 + 1) * VoxelConstants.ChunkSize);
                int worldZ = baseWorldZ + Mathf.FloorToInt(Hash01(baseWorldX, baseWorldZ, seed + i * 31 + 2) * VoxelConstants.ChunkSize);
                int y = minY + Mathf.FloorToInt(Hash01(baseWorldX, baseWorldZ, seed + i * 31 + 3) * Mathf.Max(1, maxY - minY + 1));

                int localX = Mathf.Clamp(worldX - baseWorldX, 0, VoxelConstants.ChunkSize - 1);
                int localZ = Mathf.Clamp(worldZ - baseWorldZ, 0, VoxelConstants.ChunkSize - 1);

                int veinSize = minVein + Mathf.FloorToInt(Hash01(worldX, worldZ, seed + i * 31 + 4) * (maxVein - minVein + 1));

                float angle = Hash01(worldX, worldZ, seed + i * 31 + 5) * Mathf.PI * 2f;
                float dx = Mathf.Cos(angle);
                float dz = Mathf.Sin(angle);

                float length = 1.3f + Hash01(worldX, worldZ, seed + i * 31 + 6) * 2.0f;

                float startX = localX - dx * length * 0.5f;
                float endX = localX + dx * length * 0.5f;
                float startZ = localZ - dz * length * 0.5f;
                float endZ = localZ + dz * length * 0.5f;

                float startY = y - 0.6f + Hash01(worldX, worldZ, seed + i * 31 + 7) * 1.2f;
                float endY = y - 0.6f + Hash01(worldX, worldZ, seed + i * 31 + 8) * 1.2f;

                for (int n = 0; n < veinSize; n++)
                {
                    float t = veinSize <= 1 ? 0f : (float)n / (veinSize - 1);

                    float px = Mathf.Lerp(startX, endX, t);
                    float py = Mathf.Lerp(startY, endY, t);
                    float pz = Mathf.Lerp(startZ, endZ, t);

                    float radius = 0.75f + Hash01(worldX, worldZ, seed + i * 31 + 100 + n) * 0.45f;

                    int minX = Mathf.FloorToInt(px - radius);
                    int maxX = Mathf.CeilToInt(px + radius);
                    int minYLocal = Mathf.FloorToInt(py - radius);
                    int maxYLocal = Mathf.CeilToInt(py + radius);
                    int minZ = Mathf.FloorToInt(pz - radius);
                    int maxZ = Mathf.CeilToInt(pz + radius);

                    for (int lx = minX; lx <= maxX; lx++)
                    {
                        if (lx < 0 || lx >= VoxelConstants.ChunkSize)
                            continue;

                        for (int ly = minYLocal; ly <= maxYLocal; ly++)
                        {
                            if (ly < 0 || ly >= VoxelConstants.WorldHeight)
                                continue;

                            for (int lz = minZ; lz <= maxZ; lz++)
                            {
                                if (lz < 0 || lz >= VoxelConstants.ChunkSize)
                                    continue;

                                float distSq =
                                    (lx - px) * (lx - px) +
                                    (ly - py) * (ly - py) +
                                    (lz - pz) * (lz - pz);

                                if (distSq > radius * radius)
                                    continue;

                                if (chunk.GetBlockLocal(lx, ly, lz).Id == StoneId)
                                    chunk.SetBlockLocal(lx, ly, lz, new BlockData(oreId));
                            }
                        }
                    }
                }
            }
        }

        private static float Hash01(int x, int z, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + z * 668265263 + seed * 1442695041;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= (h >> 16);
                return (h & 0x7fffffff) / 2147483647f;
            }
        }
    }
}