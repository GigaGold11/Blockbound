using Blockbound.Blocks;
using Blockbound.Chunks;
using Blockbound.Core;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Blockbound.Generation
{
    public static class SeededTerrainGenerator
    {
        public enum BiomeType
        {
            Plains,
            Forest,
            FloralFields,
            Dunes,
            Mountain
        }

        private const ushort GrassId = 1;
        private const ushort DirtId = 2;
        private const ushort StoneId = 3;
        private const ushort SandId = 4;

        private const int PlainsBaseHeight = 92;
        private const int ForestBaseHeight = 104;
        private const int FloralBaseHeight = 95;
        private const int DunesBaseHeight = 98;

        public static int GetRandomWorldSeed()
        {
            Random.InitState(System.DateTime.UtcNow.Millisecond + System.Environment.TickCount);
            return Random.Range(int.MinValue, int.MaxValue);
        }

        public static float GetHeightAt(float worldX, float worldZ)
        {
            BiomeSampler.BiomeSample sample = BiomeSampler.Sample(Mathf.FloorToInt(worldX), Mathf.FloorToInt(worldZ), 0);
            return ComputeBlendedHeight(worldX, worldZ, sample, 0);
        }

        public static void GenerateChunk(Chunk chunk, int seed)
        {
            int[,] surfaceY = new int[VoxelConstants.ChunkSize, VoxelConstants.ChunkSize];
            BiomeType[,] biomeMap = new BiomeType[VoxelConstants.ChunkSize, VoxelConstants.ChunkSize];

            int baseWorldX = chunk.Coord.x * VoxelConstants.ChunkSize;
            int baseWorldZ = chunk.Coord.y * VoxelConstants.ChunkSize;

            for (int x = 0; x < VoxelConstants.ChunkSize; x++)
            {
                for (int z = 0; z < VoxelConstants.ChunkSize; z++)
                {
                    int worldX = baseWorldX + x;
                    int worldZ = baseWorldZ + z;

                    BiomeSampler.BiomeSample sample = BiomeSampler.Sample(worldX, worldZ, seed);
                    BiomeType biome = sample.DominantBiome;
                    biomeMap[x, z] = biome;

                    float blendedHeight = ComputeBlendedHeight(worldX, worldZ, sample, seed);

                    float cliffMask = GenerationNoise.RidgedFbm2D(worldX, worldZ, seed + 110000, 1f / 650f, 4);
                    if (cliffMask > 0.82f)
                    {
                        float cliffStrength = Mathf.InverseLerp(0.82f, 1f, cliffMask);
                        cliffStrength = Mathf.Pow(cliffStrength, 1.3f);
                        blendedHeight += cliffStrength * 16f;
                    }

                    int ySurface = Mathf.RoundToInt(blendedHeight);

                    if (biome == BiomeType.Dunes)
                    {
                        float canyonMask = GenerationNoise.RidgedFbm2D(worldX, worldZ, seed + 5000, 1f / 900f, 4);
                        float canyonDetail = GenerationNoise.RidgedFbm2D(worldX, worldZ, seed + 5001, 1f / 180f, 3);

                        if (canyonMask > 0.90f)
                        {
                            float canyonDepth = Mathf.InverseLerp(0.90f, 1f, canyonMask);
                            canyonDepth = Mathf.Pow(canyonDepth, 1.6f);
                            int carve = Mathf.RoundToInt(30f + canyonDepth * 75f + canyonDetail * 20f);
                            ySurface -= carve;
                        }
                    }
                    else if (biome == BiomeType.Plains || biome == BiomeType.Forest || biome == BiomeType.FloralFields)
                    {
                        float ravineMask = GenerationNoise.RidgedFbm2D(worldX, worldZ, seed + 6000, 1f / 1200f, 4);

                        if (ravineMask > 0.935f)
                        {
                            float ravineDepth = Mathf.InverseLerp(0.935f, 1f, ravineMask);
                            ravineDepth = Mathf.Pow(ravineDepth, 1.7f);
                            int carve = Mathf.RoundToInt(18f + ravineDepth * 55f);
                            ySurface -= carve;
                        }
                    }

                    ySurface = Mathf.Clamp(ySurface, 12, VoxelConstants.WorldHeight - 12);
                    surfaceY[x, z] = ySurface;

                    for (int y = 0; y <= ySurface; y++)
                    {
                        ushort id;

                        if (biome == BiomeType.Dunes)
                        {
                            if (y == ySurface) id = SandId;
                            else if (y >= ySurface - 5) id = SandId;
                            else id = StoneId;
                        }
                        else if (biome == BiomeType.Mountain || cliffMask > 0.82f)
                        {
                            id = StoneId;
                        }
                        else
                        {
                            if (y == ySurface) id = GrassId;
                            else if (y >= ySurface - 4) id = DirtId;
                            else id = StoneId;
                        }

                        chunk.SetBlockLocal(x, y, z, new BlockData(id));
                    }
                }
            }

            CaveGenerator.Carve(chunk, seed, surfaceY, biomeMap);
            OreGenerator.Generate(chunk, seed, surfaceY, biomeMap);
            BiomeFeatureGenerator.Generate(chunk, seed, surfaceY, biomeMap);
            WorldFeatureGenerator.Generate(chunk, seed, surfaceY, biomeMap);

            chunk.IsGenerated = true;
            chunk.IsDirty = true;
        }

        private static float ComputeBlendedHeight(float worldX, float worldZ, BiomeSampler.BiomeSample sample, int seed)
        {
            float blendedHeight = 0f;

            float plainsHeight =
                PlainsBaseHeight +
                (GenerationNoise.Fbm2D(worldX, worldZ, seed + 1000, 1f / 260f, 3) - 0.5f) * 7f +
                (GenerationNoise.Fbm2D(worldX, worldZ, seed + 1001, 1f / 110f, 3) - 0.5f) * 10f +
                sample.MountainWeight * 14f;
            blendedHeight += plainsHeight * sample.PlainsWeight;

            float forestHeight =
                ForestBaseHeight +
                (GenerationNoise.Fbm2D(worldX, worldZ, seed + 2000, 1f / 240f, 4) - 0.5f) * 12f +
                (GenerationNoise.Fbm2D(worldX, worldZ, seed + 2001, 1f / 100f, 4) - 0.5f) * 14f +
                sample.MountainWeight * 20f;
            blendedHeight += forestHeight * sample.ForestWeight;

            float floralHeight =
                FloralBaseHeight +
                (GenerationNoise.Fbm2D(worldX, worldZ, seed + 3000, 1f / 250f, 3) - 0.5f) * 7f +
                (GenerationNoise.Fbm2D(worldX, worldZ, seed + 3001, 1f / 110f, 3) - 0.5f) * 9f +
                sample.MountainWeight * 12f;
            blendedHeight += floralHeight * sample.FloralFieldsWeight;

            float dunesHeight =
                DunesBaseHeight +
                Mathf.Pow(GenerationNoise.RidgedFbm2D(worldX, worldZ, seed + 4000, 1f / 220f, 4), 1.8f) * 28f +
                (GenerationNoise.RidgedFbm2D(worldX, worldZ, seed + 4001, 1f / 80f, 3) - 0.5f) * 12f;
            blendedHeight += dunesHeight * sample.DunesWeight;

            float mountainHeight =
                110f +
                sample.MountainWeight * 45f +
                sample.MountainWeight * Mathf.Pow(sample.Foothills, 1.1f) * 55f +
                sample.MountainWeight * Mathf.Pow(sample.MountainBroad, 1.4f) * 95f +
                sample.MountainWeight * Mathf.Pow(sample.MountainDetail, 1.25f) * 65f;
            blendedHeight += mountainHeight * sample.MountainWeight;

            return blendedHeight;
        }
    }
}