using Blockbound.Blocks;
using Blockbound.Chunks;
using Blockbound.Core;
using UnityEngine;

namespace Blockbound.Generation
{
    public static class BiomeFeatureGenerator
    {
        private const ushort OakLogId = 6;
        private const ushort OakLeavesId = 7;

        private const ushort DandelionId = 8;
        private const ushort RoseId = 9;
        private const ushort LilyId = 10;
        private const ushort GrassPlantId = 14;
        private const ushort LilacId = 16;
        private const ushort PeonieId = 17;
        private const ushort CornflowerId = 18;
        private const ushort DaylillieId = 19;

        public static void Generate(Chunk chunk, int seed, int[,] surfaceY, SeededTerrainGenerator.BiomeType[,] biomeMap)
        {
            int size = VoxelConstants.ChunkSize;
            int baseWorldX = chunk.Coord.x * VoxelConstants.ChunkSize;
            int baseWorldZ = chunk.Coord.y * VoxelConstants.ChunkSize;

            for (int x = 0; x < size; x++)
            {
                for (int z = 0; z < size; z++)
                {
                    int y = surfaceY[x, z];
                    if (y <= 0 || y >= VoxelConstants.WorldHeight - 16)
                        continue;

                    int worldX = baseWorldX + x;
                    int worldZ = baseWorldZ + z;

                    if (chunk.GetBlockLocal(x, y, z).Id != 1)
                        continue;

                    if (chunk.GetBlockLocal(x, y + 1, z).Id != 0)
                        continue;

                    SeededTerrainGenerator.BiomeType biome = biomeMap[x, z];

                    switch (biome)
                    {
                        case SeededTerrainGenerator.BiomeType.Forest:
                            TryPlaceDenseForestTree(chunk, x, y, z, worldX, worldZ, seed);
                            if (chunk.GetBlockLocal(x, y + 1, z).Id == 0)
                            {
                                TryPlaceGrass(chunk, x, y, z, worldX, worldZ, seed, 0.84f, 1f / 38f, 3);
                                TryPlaceForestFlower(chunk, x, y, z, worldX, worldZ, seed, 0.008f);
                            }
                            break;

                        case SeededTerrainGenerator.BiomeType.Plains:
                            TryPlaceSparsePlainsTree(chunk, x, y, z, worldX, worldZ, seed);
                            if (chunk.GetBlockLocal(x, y + 1, z).Id == 0)
                            {
                                TryPlaceGrass(chunk, x, y, z, worldX, worldZ, seed, 0.86f, 1f / 46f, 3);
                                TryPlacePlainsFlower(chunk, x, y, z, worldX, worldZ, seed, 0.028f);
                            }
                            break;

                        case SeededTerrainGenerator.BiomeType.FloralFields:
                            TryPlaceRareFloralTree(chunk, x, y, z, worldX, worldZ, seed);
                            if (chunk.GetBlockLocal(x, y + 1, z).Id == 0)
                            {
                                TryPlaceGrass(chunk, x, y, z, worldX, worldZ, seed, 0.90f, 1f / 52f, 3);
                                TryPlaceFloralFlower(chunk, x, y, z, worldX, worldZ, seed, 0.12f);
                            }
                            break;
                    }
                }
            }
        }

        private static void TryPlaceDenseForestTree(Chunk chunk, int x, int y, int z, int worldX, int worldZ, int seed)
        {
            float broadCluster = GenerationNoise.Fbm2D(worldX, worldZ, seed + 12000, 1f / 150f, 3);
            float localDensity = GenerationNoise.Fbm2D(worldX, worldZ, seed + 12001, 1f / 44f, 3);
            float placement = GenerationNoise.Hash01(worldX, worldZ, seed + 12002);

            float chance = 0.10f + broadCluster * 0.18f + localDensity * 0.16f;
            if (placement > chance)
                return;

            float thickRoll = GenerationNoise.Hash01(worldX, worldZ, seed + 12003);
            bool thickTrunk = thickRoll > 0.78f;

            int height = thickTrunk
                ? 8 + Mathf.FloorToInt(GenerationNoise.Hash01(worldX, worldZ, seed + 12004) * 3f)
                : 6 + Mathf.FloorToInt(GenerationNoise.Hash01(worldX, worldZ, seed + 12005) * 3f);

            if (thickTrunk)
                PlaceOakTree2x2(chunk, x, y + 1, z, height, 3);
            else
                PlaceOakTree(chunk, x, y + 1, z, height, 3, true);
        }

        private static void TryPlaceSparsePlainsTree(Chunk chunk, int x, int y, int z, int worldX, int worldZ, int seed)
        {
            float broad = GenerationNoise.Fbm2D(worldX, worldZ, seed + 13000, 1f / 220f, 2);
            float placement = GenerationNoise.Hash01(worldX, worldZ, seed + 13001);

            float chance = 0.003f + broad * 0.012f;
            if (placement > chance)
                return;

            int height = 4 + Mathf.FloorToInt(GenerationNoise.Hash01(worldX, worldZ, seed + 13002) * 2f);
            PlaceOakTree(chunk, x, y + 1, z, height, 2, false);
        }

        private static void TryPlaceRareFloralTree(Chunk chunk, int x, int y, int z, int worldX, int worldZ, int seed)
        {
            float placement = GenerationNoise.Hash01(worldX, worldZ, seed + 14000);
            if (placement > 0.006f)
                return;

            int height = 4 + Mathf.FloorToInt(GenerationNoise.Hash01(worldX, worldZ, seed + 14001) * 2f);
            PlaceOakTree(chunk, x, y + 1, z, height, 2, false);
        }

        private static void TryPlaceGrass(Chunk chunk, int x, int y, int z, int worldX, int worldZ, int seed, float threshold, float frequency, int octaves)
        {
            float patch = GenerationNoise.Fbm2D(worldX, worldZ, seed + 15000, frequency, octaves);
            if (patch > threshold)
                chunk.SetBlockLocal(x, y + 1, z, new BlockData(GrassPlantId));
        }

        private static void TryPlaceForestFlower(Chunk chunk, int x, int y, int z, int worldX, int worldZ, int seed, float chance)
        {
            if (chunk.GetBlockLocal(x, y + 1, z).Id != 0)
                return;

            if (GenerationNoise.Hash01(worldX, worldZ, seed + 16000) > chance)
                return;

            chunk.SetBlockLocal(x, y + 1, z, new BlockData(PickForestFlower(worldX, worldZ, seed + 16001)));
        }

        private static void TryPlacePlainsFlower(Chunk chunk, int x, int y, int z, int worldX, int worldZ, int seed, float chance)
        {
            if (chunk.GetBlockLocal(x, y + 1, z).Id != 0)
                return;

            if (GenerationNoise.Hash01(worldX, worldZ, seed + 16100) > chance)
                return;

            chunk.SetBlockLocal(x, y + 1, z, new BlockData(PickPlainsFlower(worldX, worldZ, seed + 16101)));
        }

        private static void TryPlaceFloralFlower(Chunk chunk, int x, int y, int z, int worldX, int worldZ, int seed, float chance)
        {
            if (chunk.GetBlockLocal(x, y + 1, z).Id != 0)
                return;

            float patch = GenerationNoise.Fbm2D(worldX, worldZ, seed + 16200, 1f / 28f, 3);
            float roll = GenerationNoise.Hash01(worldX, worldZ, seed + 16201);

            float boostedChance = chance + patch * 0.08f;
            if (roll > boostedChance)
                return;

            chunk.SetBlockLocal(x, y + 1, z, new BlockData(PickFloralFlower(worldX, worldZ, seed + 16202)));
        }

        private static ushort PickForestFlower(int worldX, int worldZ, int seed)
        {
            float r = GenerationNoise.Hash01(worldX, worldZ, seed);
            if (r < 0.50f) return DandelionId;
            if (r < 0.85f) return RoseId;
            return CornflowerId;
        }

        private static ushort PickPlainsFlower(int worldX, int worldZ, int seed)
        {
            float r = GenerationNoise.Hash01(worldX, worldZ, seed);
            if (r < 0.30f) return DandelionId;
            if (r < 0.55f) return RoseId;
            if (r < 0.72f) return LilyId;
            if (r < 0.86f) return CornflowerId;
            return DaylillieId;
        }

        private static ushort PickFloralFlower(int worldX, int worldZ, int seed)
        {
            float r = GenerationNoise.Hash01(worldX, worldZ, seed);
            if (r < 0.15f) return DandelionId;
            if (r < 0.30f) return RoseId;
            if (r < 0.42f) return LilyId;
            if (r < 0.58f) return LilacId;
            if (r < 0.76f) return PeonieId;
            if (r < 0.90f) return CornflowerId;
            return DaylillieId;
        }

        private static void PlaceOakTree(Chunk chunk, int x, int baseY, int z, int trunkHeight, int leafRadius, bool tall)
        {
            if (!CanPlaceTree(chunk, x, baseY, z, trunkHeight, leafRadius, tall))
                return;

            for (int i = 0; i < trunkHeight; i++)
            {
                int yy = baseY + i;
                if (yy >= 0 && yy < VoxelConstants.WorldHeight)
                    chunk.SetBlockLocal(x, yy, z, new BlockData(OakLogId));
            }

            int leafBaseY = baseY + trunkHeight - 2;
            int leafTopY = baseY + trunkHeight + (tall ? 2 : 1);

            for (int yy = leafBaseY; yy <= leafTopY; yy++)
            {
                int layer = yy - leafBaseY;
                int radius = leafRadius;

                if (layer == 0) radius = leafRadius;
                else if (layer == 1) radius = leafRadius;
                else if (layer == 2) radius = Mathf.Max(1, leafRadius - 1);
                else radius = 1;

                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dz = -radius; dz <= radius; dz++)
                    {
                        int ax = Mathf.Abs(dx);
                        int az = Mathf.Abs(dz);

                        if (ax + az > radius + 1)
                            continue;

                        if (ax == radius && az == radius && radius > 1)
                            continue;

                        int px = x + dx;
                        int pz = z + dz;

                        if (!IsWithinChunk(px, pz) || yy < 0 || yy >= VoxelConstants.WorldHeight)
                            continue;

                        BlockData existing = chunk.GetBlockLocal(px, yy, pz);
                        if (existing.Id == 0 || BlockRegistry.Instance.Get(existing.Id)?.IsReplaceable == true)
                            chunk.SetBlockLocal(px, yy, pz, new BlockData(OakLeavesId));
                    }
                }
            }
        }

        private static void PlaceOakTree2x2(Chunk chunk, int x, int baseY, int z, int trunkHeight, int leafRadius)
        {
            if (!CanPlaceTree2x2(chunk, x, baseY, z, trunkHeight, leafRadius))
                return;

            for (int i = 0; i < trunkHeight; i++)
            {
                int yy = baseY + i;
                SetIfInWorld(chunk, x, yy, z, OakLogId);
                SetIfInWorld(chunk, x + 1, yy, z, OakLogId);
                SetIfInWorld(chunk, x, yy, z + 1, OakLogId);
                SetIfInWorld(chunk, x + 1, yy, z + 1, OakLogId);
            }

            int leafBaseY = baseY + trunkHeight - 2;
            int leafTopY = baseY + trunkHeight + 3;

            for (int yy = leafBaseY; yy <= leafTopY; yy++)
            {
                int layer = yy - leafBaseY;
                int radius = layer switch
                {
                    0 => leafRadius,
                    1 => leafRadius,
                    2 => leafRadius - 1,
                    3 => leafRadius - 1,
                    _ => 1
                };

                for (int dx = -radius; dx <= radius + 1; dx++)
                {
                    for (int dz = -radius; dz <= radius + 1; dz++)
                    {
                        int px = x + dx;
                        int pz = z + dz;

                        if (!IsWithinChunk(px, pz) || yy < 0 || yy >= VoxelConstants.WorldHeight)
                            continue;

                        int dist = Mathf.Abs(dx) + Mathf.Abs(dz);
                        if (radius >= 3 && dist > radius + 2)
                            continue;

                        BlockData existing = chunk.GetBlockLocal(px, yy, pz);
                        if (existing.Id == 0 || BlockRegistry.Instance.Get(existing.Id)?.IsReplaceable == true)
                            chunk.SetBlockLocal(px, yy, pz, new BlockData(OakLeavesId));
                    }
                }
            }
        }

        private static void SetIfInWorld(Chunk chunk, int x, int y, int z, ushort id)
        {
            if (!IsWithinChunk(x, z) || y < 0 || y >= VoxelConstants.WorldHeight)
                return;

            chunk.SetBlockLocal(x, y, z, new BlockData(id));
        }

        private static bool CanPlaceTree(Chunk chunk, int x, int baseY, int z, int trunkHeight, int leafRadius, bool tall)
        {
            int maxY = baseY + trunkHeight + (tall ? 3 : 2);

            for (int yy = baseY; yy <= maxY; yy++)
            {
                int radius = yy >= baseY + trunkHeight - 2 ? leafRadius : 0;

                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dz = -radius; dz <= radius; dz++)
                    {
                        int px = x + dx;
                        int pz = z + dz;

                        if (!IsWithinChunk(px, pz) || yy < 0 || yy >= VoxelConstants.WorldHeight)
                            return false;

                        BlockData existing = chunk.GetBlockLocal(px, yy, pz);
                        if (existing.Id != 0)
                        {
                            BlockDefinition def = BlockRegistry.Instance.Get(existing.Id);
                            if (def == null || !def.IsReplaceable)
                                return false;
                        }
                    }
                }
            }

            return true;
        }

        private static bool CanPlaceTree2x2(Chunk chunk, int x, int baseY, int z, int trunkHeight, int leafRadius)
        {
            if (!IsWithinChunk(x, z) || !IsWithinChunk(x + 1, z + 1))
                return false;

            int maxY = baseY + trunkHeight + 4;

            for (int yy = baseY; yy <= maxY; yy++)
            {
                int radius = yy >= baseY + trunkHeight - 2 ? leafRadius : 0;

                for (int dx = -radius; dx <= radius + 1; dx++)
                {
                    for (int dz = -radius; dz <= radius + 1; dz++)
                    {
                        int px = x + dx;
                        int pz = z + dz;

                        if (!IsWithinChunk(px, pz) || yy < 0 || yy >= VoxelConstants.WorldHeight)
                            return false;

                        BlockData existing = chunk.GetBlockLocal(px, yy, pz);
                        if (existing.Id != 0)
                        {
                            BlockDefinition def = BlockRegistry.Instance.Get(existing.Id);
                            if (def == null || !def.IsReplaceable)
                                return false;
                        }
                    }
                }
            }

            return true;
        }

        private static bool IsWithinChunk(int x, int z)
        {
            return x >= 0 && x < VoxelConstants.ChunkSize && z >= 0 && z < VoxelConstants.ChunkSize;
        }
    }
}