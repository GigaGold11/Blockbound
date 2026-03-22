using Blockbound.Blocks;
using Blockbound.Chunks;
using Blockbound.Core;
using UnityEngine;

namespace Blockbound.Generation
{
    public static class WorldFeatureGenerator
    {
        private const ushort AirId = 0;
        private const ushort StoneId = 3;
        private const ushort CloudId = 15;

        public static void Generate(Chunk chunk, int seed, int[,] surfaceY, SeededTerrainGenerator.BiomeType[,] biomeMap)
        {
            int size = VoxelConstants.ChunkSize;

            for (int x = 0; x < size; x++)
            {
                for (int z = 0; z < size; z++)
                {
                    TryRareSurfaceCaveEntrance(chunk, x, z, seed, biomeMap, surfaceY);
                    TryStoneOutcrop(chunk, x, z, seed, biomeMap, surfaceY);
                    TryFlowerPatchAccent(chunk, x, z, seed, biomeMap, surfaceY);
                }
            }

            TryGenerateCloudIslands(chunk, seed);
        }

        private static void TryGenerateCloudIslands(Chunk chunk, int seed)
        {
            int chunkWorldX = chunk.Coord.x * VoxelConstants.ChunkSize;
            int chunkWorldZ = chunk.Coord.y * VoxelConstants.ChunkSize;

            int regionX = Mathf.FloorToInt(chunkWorldX / 96f);
            int regionZ = Mathf.FloorToInt(chunkWorldZ / 96f);

            float regionRoll = GenerationNoise.Hash01(regionX, regionZ, seed + 21000);
            if (regionRoll < 0.82f)
                return;

            int islandCenterX = regionX * 96 + Mathf.FloorToInt(GenerationNoise.Hash01(regionX, regionZ, seed + 21001) * 96f);
            int islandCenterZ = regionZ * 96 + Mathf.FloorToInt(GenerationNoise.Hash01(regionX, regionZ, seed + 21002) * 96f);
            int islandCenterY = 190 + Mathf.FloorToInt(GenerationNoise.Hash01(regionX, regionZ, seed + 21003) * 28f);

            int radiusX = 8 + Mathf.FloorToInt(GenerationNoise.Hash01(regionX, regionZ, seed + 21004) * 12f);
            int radiusZ = 7 + Mathf.FloorToInt(GenerationNoise.Hash01(regionX, regionZ, seed + 21005) * 11f);
            int thickness = 2 + Mathf.FloorToInt(GenerationNoise.Hash01(regionX, regionZ, seed + 21006) * 3f);

            int chunkMinX = chunk.Coord.x * VoxelConstants.ChunkSize;
            int chunkMinZ = chunk.Coord.y * VoxelConstants.ChunkSize;
            int chunkMaxX = chunkMinX + VoxelConstants.ChunkSize - 1;
            int chunkMaxZ = chunkMinZ + VoxelConstants.ChunkSize - 1;

            int cloudMinX = islandCenterX - radiusX - 2;
            int cloudMaxX = islandCenterX + radiusX + 2;
            int cloudMinZ = islandCenterZ - radiusZ - 2;
            int cloudMaxZ = islandCenterZ + radiusZ + 2;

            if (cloudMaxX < chunkMinX || cloudMinX > chunkMaxX || cloudMaxZ < chunkMinZ || cloudMinZ > chunkMaxZ)
                return;

            for (int localX = 0; localX < VoxelConstants.ChunkSize; localX++)
            {
                for (int localZ = 0; localZ < VoxelConstants.ChunkSize; localZ++)
                {
                    int worldX = chunkMinX + localX;
                    int worldZ = chunkMinZ + localZ;

                    float dx = (worldX - islandCenterX) / (float)radiusX;
                    float dz = (worldZ - islandCenterZ) / (float)radiusZ;
                    float dist = dx * dx + dz * dz;

                    if (dist > 1.35f)
                        continue;

                    float edgeNoise = GenerationNoise.Fbm2D(worldX, worldZ, seed + 21007, 1f / 18f, 3);
                    float density = 1.15f - dist + (edgeNoise - 0.5f) * 0.35f;
                    if (density <= 0.18f)
                        continue;

                    int localThickness = thickness;
                    if (dist > 0.85f)
                        localThickness = Mathf.Max(1, thickness - 1);

                    if (dist > 1.05f)
                        localThickness = 1;

                    for (int i = 0; i < localThickness; i++)
                    {
                        int y = islandCenterY - i;
                        if (y < 0 || y >= VoxelConstants.WorldHeight)
                            continue;

                        chunk.SetBlockLocal(localX, y, localZ, new BlockData(CloudId));
                    }
                }
            }
        }

        private static void TryRareSurfaceCaveEntrance(Chunk chunk, int x, int z, int seed, SeededTerrainGenerator.BiomeType[,] biomeMap, int[,] surfaceY)
        {
            int worldX = chunk.Coord.x * VoxelConstants.ChunkSize + x;
            int worldZ = chunk.Coord.y * VoxelConstants.ChunkSize + z;

            float region = GenerationNoise.RidgedFbm2D(worldX, worldZ, seed + 17000, 1f / 1000f, 4);
            float detail = GenerationNoise.RidgedFbm2D(worldX, worldZ, seed + 17001, 1f / 180f, 3);

            if (region < 0.968f || detail < 0.74f)
                return;

            int y = surfaceY[x, z];
            if (y < 40 || y > VoxelConstants.WorldHeight - 30)
                return;

            if (biomeMap[x, z] == SeededTerrainGenerator.BiomeType.Dunes)
                return;

            for (int dx = -2; dx <= 2; dx++)
            {
                for (int dz = -2; dz <= 2; dz++)
                {
                    int lx = x + dx;
                    int lz = z + dz;
                    if (lx < 0 || lx >= VoxelConstants.ChunkSize || lz < 0 || lz >= VoxelConstants.ChunkSize)
                        continue;

                    int ny = surfaceY[lx, lz];
                    if (Mathf.Abs(ny - y) > 4)
                        return;
                }
            }

            for (int dx = -2; dx <= 2; dx++)
            {
                for (int dz = -2; dz <= 2; dz++)
                {
                    int lx = x + dx;
                    int lz = z + dz;
                    if (lx < 0 || lx >= VoxelConstants.ChunkSize || lz < 0 || lz >= VoxelConstants.ChunkSize)
                        continue;

                    int dist = Mathf.Abs(dx) + Mathf.Abs(dz);
                    int carveDepth = dist switch
                    {
                        0 => 5,
                        1 => 4,
                        2 => 3,
                        3 => 2,
                        _ => 1
                    };

                    int top = surfaceY[lx, lz];
                    for (int cy = 0; cy < carveDepth; cy++)
                    {
                        int yy = top - cy;
                        if (yy >= 1 && yy < VoxelConstants.WorldHeight - 1)
                            chunk.SetBlockLocal(lx, yy, lz, new BlockData(AirId));
                    }

                    for (int cy = 1; cy <= carveDepth + 2; cy++)
                    {
                        int yy = top - cy;
                        if (yy >= 0 && yy < VoxelConstants.WorldHeight)
                        {
                            BlockData b = chunk.GetBlockLocal(lx, yy, lz);
                            if (b.Id != AirId)
                                chunk.SetBlockLocal(lx, yy, lz, new BlockData(StoneId));
                        }
                    }
                }
            }
        }

        private static void TryStoneOutcrop(Chunk chunk, int x, int z, int seed, SeededTerrainGenerator.BiomeType[,] biomeMap, int[,] surfaceY)
        {
            if (biomeMap[x, z] == SeededTerrainGenerator.BiomeType.Dunes)
                return;

            int worldX = chunk.Coord.x * VoxelConstants.ChunkSize + x;
            int worldZ = chunk.Coord.y * VoxelConstants.ChunkSize + z;

            float region = GenerationNoise.RidgedFbm2D(worldX, worldZ, seed + 18000, 1f / 850f, 4);
            float detail = GenerationNoise.Hash01(worldX, worldZ, seed + 18001);

            if (region < 0.982f || detail < 0.92f)
                return;

            int y = surfaceY[x, z];
            if (y < 50)
                return;

            int height = 2 + Mathf.FloorToInt(GenerationNoise.Hash01(worldX, worldZ, seed + 18002) * 3f);
            int radius = GenerationNoise.Hash01(worldX, worldZ, seed + 18003) > 0.6f ? 2 : 1;

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    int lx = x + dx;
                    int lz = z + dz;
                    if (lx < 0 || lx >= VoxelConstants.ChunkSize || lz < 0 || lz >= VoxelConstants.ChunkSize)
                        continue;

                    int dist = Mathf.Abs(dx) + Mathf.Abs(dz);
                    if (dist > radius + 1)
                        continue;

                    int localTop = surfaceY[lx, lz];
                    int pillarHeight = Mathf.Max(1, height - dist);

                    for (int i = 1; i <= pillarHeight; i++)
                    {
                        int yy = localTop + i;
                        if (yy >= 0 && yy < VoxelConstants.WorldHeight)
                            chunk.SetBlockLocal(lx, yy, lz, new BlockData(StoneId));
                    }
                }
            }
        }

        private static void TryFlowerPatchAccent(Chunk chunk, int x, int z, int seed, SeededTerrainGenerator.BiomeType[,] biomeMap, int[,] surfaceY)
        {
            if (biomeMap[x, z] != SeededTerrainGenerator.BiomeType.FloralFields)
                return;

            int worldX = chunk.Coord.x * VoxelConstants.ChunkSize + x;
            int worldZ = chunk.Coord.y * VoxelConstants.ChunkSize + z;

            float region = GenerationNoise.Fbm2D(worldX, worldZ, seed + 19000, 1f / 120f, 3);
            float roll = GenerationNoise.Hash01(worldX, worldZ, seed + 19001);

            if (region < 0.78f || roll < 0.985f)
                return;

            ushort flowerId = PickFlower(worldX, worldZ, seed + 19002);

            for (int dx = -2; dx <= 2; dx++)
            {
                for (int dz = -2; dz <= 2; dz++)
                {
                    int lx = x + dx;
                    int lz = z + dz;
                    if (lx < 0 || lx >= VoxelConstants.ChunkSize || lz < 0 || lz >= VoxelConstants.ChunkSize)
                        continue;

                    int top = surfaceY[lx, lz];
                    if (chunk.GetBlockLocal(lx, top, lz).Id != 1)
                        continue;

                    if (chunk.GetBlockLocal(lx, top + 1, lz).Id != 0)
                        continue;

                    float place = GenerationNoise.Hash01(worldX + dx, worldZ + dz, seed + 19003);
                    if (place > 0.52f)
                        continue;

                    chunk.SetBlockLocal(lx, top + 1, lz, new BlockData(flowerId));
                }
            }
        }

        private static ushort PickFlower(int worldX, int worldZ, int seed)
        {
            float flowerType = GenerationNoise.Hash01(worldX, worldZ, seed);
            if (flowerType < 0.14f) return 8;
            if (flowerType < 0.28f) return 9;
            if (flowerType < 0.40f) return 10;
            if (flowerType < 0.58f) return 16;
            if (flowerType < 0.76f) return 17;
            if (flowerType < 0.90f) return 18;
            return 19;
        }
    }
}