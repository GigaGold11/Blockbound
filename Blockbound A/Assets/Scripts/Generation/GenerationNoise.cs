using UnityEngine;

namespace Blockbound.Generation
{
    public static class GenerationNoise
    {
        public static float Hash01(int x, int z, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + z * 668265263 + seed * 1442695041;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= (h >> 16);
                return (h & 0x7fffffff) / 2147483647f;
            }
        }

        public static float ValueNoise2D(float x, float z, int seed)
        {
            int x0 = Mathf.FloorToInt(x);
            int z0 = Mathf.FloorToInt(z);
            int x1 = x0 + 1;
            int z1 = z0 + 1;

            float tx = x - x0;
            float tz = z - z0;

            float sx = tx * tx * (3f - 2f * tx);
            float sz = tz * tz * (3f - 2f * tz);

            float v00 = Hash01(x0, z0, seed);
            float v10 = Hash01(x1, z0, seed);
            float v01 = Hash01(x0, z1, seed);
            float v11 = Hash01(x1, z1, seed);

            float a = Mathf.Lerp(v00, v10, sx);
            float b = Mathf.Lerp(v01, v11, sx);
            return Mathf.Lerp(a, b, sz);
        }

        public static float Fbm2D(float x, float z, int seed, float baseFrequency, int octaves)
        {
            float sum = 0f;
            float amp = 1f;
            float freq = baseFrequency;
            float norm = 0f;

            for (int i = 0; i < octaves; i++)
            {
                float v = ValueNoise2D(x * freq, z * freq, seed + i * 1013);
                sum += v * amp;
                norm += amp;
                amp *= 0.5f;
                freq *= 2f;
            }

            return sum / norm;
        }

        public static float RidgedFbm2D(float x, float z, int seed, float baseFrequency, int octaves)
        {
            float sum = 0f;
            float amp = 1f;
            float freq = baseFrequency;
            float norm = 0f;

            for (int i = 0; i < octaves; i++)
            {
                float v = ValueNoise2D(x * freq, z * freq, seed + i * 7919);
                v = 2f * v - 1f;
                float r = 1f - Mathf.Abs(v);
                sum += r * r * amp;
                norm += amp;
                amp *= 0.5f;
                freq *= 2f;
            }

            return sum / norm;
        }

        public static int HashInt(int a, int b, int seed)
        {
            unchecked
            {
                int h = a * 374761393 + b * 668265263 + seed * 1442695041;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= (h >> 16);
                return h;
            }
        }

        public static int PositiveMod(int value, int mod)
        {
            int r = value % mod;
            return r < 0 ? r + mod : r;
        }

        public static float Hash01FromInt(int value)
        {
            uint h = (uint)value;
            h ^= h >> 16;
            h *= 0x7feb352d;
            h ^= h >> 15;
            h *= 0x846ca68b;
            h ^= h >> 16;
            return (h & 0x7fffffff) / 2147483647f;
        }
    }
}