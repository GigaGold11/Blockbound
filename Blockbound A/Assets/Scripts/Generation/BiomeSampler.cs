using UnityEngine;

namespace Blockbound.Generation
{
    public static class BiomeSampler
    {
        public struct BiomeSample
        {
            public SeededTerrainGenerator.BiomeType DominantBiome;
            public float PlainsWeight;
            public float ForestWeight;
            public float FloralFieldsWeight;
            public float DunesWeight;
            public float MountainWeight;

            public float Temp;
            public float Humid;
            public float FloralNoise;
            public float MountainRangeMask;
            public float MountainBroad;
            public float MountainDetail;
            public float Foothills;
        }

        public static BiomeSample Sample(int worldX, int worldZ, int seed)
        {
            BiomeSample sample = new BiomeSample();

            sample.Temp = GenerationNoise.Fbm2D(worldX, worldZ, seed + 20000, 1f / 420f, 4);
            sample.Humid = GenerationNoise.Fbm2D(worldX, worldZ, seed + 60000, 1f / 420f, 4);
            sample.FloralNoise = GenerationNoise.Fbm2D(worldX, worldZ, seed + 85000, 1f / 210f, 3);

            sample.MountainRangeMask = GenerationNoise.RidgedFbm2D(worldX, worldZ, seed + 91000, 1f / 1800f, 5);
            sample.MountainBroad = GenerationNoise.Fbm2D(worldX, worldZ, seed + 92000, 1f / 900f, 4);
            sample.MountainDetail = GenerationNoise.RidgedFbm2D(worldX, worldZ, seed + 93000, 1f / 260f, 4);
            sample.Foothills = GenerationNoise.Fbm2D(worldX, worldZ, seed + 94000, 1f / 420f, 4);

            sample.MountainWeight = Mathf.InverseLerp(0.52f, 0.78f, sample.MountainRangeMask);
            sample.MountainWeight = Mathf.SmoothStep(0f, 1f, sample.MountainWeight);

            if (sample.MountainWeight < 1f)
            {
                sample.DunesWeight = sample.Temp > 0.60f && sample.Humid < 0.28f
                    ? Mathf.InverseLerp(0.60f, 0.8f, sample.Temp) * Mathf.InverseLerp(0.28f, 0.1f, sample.Humid)
                    : 0f;

                sample.FloralFieldsWeight = sample.Temp > 0.42f && sample.Temp < 0.76f &&
                                            sample.Humid > 0.42f && sample.Humid < 0.78f &&
                                            sample.FloralNoise > 0.58f
                    ? Mathf.InverseLerp(0.58f, 0.9f, sample.FloralNoise) * 1.25f
                    : 0f;

                sample.ForestWeight = sample.Humid > 0.58f
                    ? Mathf.InverseLerp(0.58f, 0.85f, sample.Humid)
                    : 0f;

                float sumOther = sample.DunesWeight + sample.FloralFieldsWeight + sample.ForestWeight;
                sample.PlainsWeight = Mathf.Max(0f, 1f - sample.MountainWeight - sumOther);
            }

            float totalWeight = sample.MountainWeight + sample.DunesWeight + sample.FloralFieldsWeight + sample.ForestWeight + sample.PlainsWeight;

            if (totalWeight > 0f)
            {
                sample.MountainWeight /= totalWeight;
                sample.DunesWeight /= totalWeight;
                sample.FloralFieldsWeight /= totalWeight;
                sample.ForestWeight /= totalWeight;
                sample.PlainsWeight /= totalWeight;
            }
            else
            {
                sample.PlainsWeight = 1f;
            }

            float maxWeight = Mathf.Max(sample.MountainWeight, sample.DunesWeight, sample.FloralFieldsWeight, sample.ForestWeight, sample.PlainsWeight);

            if (maxWeight == sample.MountainWeight) sample.DominantBiome = SeededTerrainGenerator.BiomeType.Mountain;
            else if (maxWeight == sample.DunesWeight) sample.DominantBiome = SeededTerrainGenerator.BiomeType.Dunes;
            else if (maxWeight == sample.FloralFieldsWeight) sample.DominantBiome = SeededTerrainGenerator.BiomeType.FloralFields;
            else if (maxWeight == sample.ForestWeight) sample.DominantBiome = SeededTerrainGenerator.BiomeType.Forest;
            else sample.DominantBiome = SeededTerrainGenerator.BiomeType.Plains;

            return sample;
        }

        public static string GetBiomeName(int worldX, int worldZ, int seed)
        {
            return Sample(worldX, worldZ, seed).DominantBiome.ToString();
        }
    }
}