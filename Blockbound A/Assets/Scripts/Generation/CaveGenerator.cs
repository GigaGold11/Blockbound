using Blockbound.Blocks;
using Blockbound.Chunks;
using Blockbound.Core;
using UnityEngine;

namespace Blockbound.Generation
{
    public static class CaveGenerator
    {
        private const ushort AirId = 0;
        private const ushort GrassId = 1;
        private const ushort DirtId = 2;
        private const ushort StoneId = 3;
        private const ushort SandId = 4;

        // Prevent caves from exposing the surface shell.
        private const int SurfaceProtectionDepth = 6;

        public static void Carve(Chunk chunk, int seed, int[,] surfaceY, SeededTerrainGenerator.BiomeType[,] biomeMap)
        {
            // Carve from nearby source chunks so systems cross borders.
            for (int sourceChunkX = chunk.Coord.x - 2; sourceChunkX <= chunk.Coord.x + 2; sourceChunkX++)
            {
                for (int sourceChunkZ = chunk.Coord.y - 2; sourceChunkZ <= chunk.Coord.y + 2; sourceChunkZ++)
                {
                    int regionSeed = HashInt(sourceChunkX, sourceChunkZ, seed + 500000);

                    // Main cave systems per region
                    int systems = 2 + PositiveMod(regionSeed, 3); // 2-4
                    for (int i = 0; i < systems; i++)
                    {
                        int caveSeed = HashInt(sourceChunkX, sourceChunkZ, seed + i * 92821 + 500100);
                        GenerateCaveSystem(chunk, sourceChunkX, sourceChunkZ, caveSeed, surfaceY);
                    }

                    // Rare large chambers / caverns, but still underground
                    float cavernChance = Hash01(HashInt(sourceChunkX, sourceChunkZ, seed + 600000));
                    if (cavernChance > 0.82f)
                    {
                        int cavernSeed = HashInt(sourceChunkX, sourceChunkZ, seed + 600100);
                        GenerateCavern(chunk, sourceChunkX, sourceChunkZ, cavernSeed, surfaceY);
                    }

                    // Rare surface entrances
                    float entranceChance = Hash01(HashInt(sourceChunkX, sourceChunkZ, seed + 700000));
                    if (entranceChance > 0.88f)
                    {
                        int entranceSeed = HashInt(sourceChunkX, sourceChunkZ, seed + 700100);
                        GenerateEntranceCave(chunk, sourceChunkX, sourceChunkZ, entranceSeed, surfaceY);
                    }
                }
            }
        }

        private static void GenerateCaveSystem(Chunk chunk, int sourceChunkX, int sourceChunkZ, int caveSeed, int[,] surfaceY)
        {
            float startX = sourceChunkX * VoxelConstants.ChunkSize + Hash01(caveSeed + 1) * VoxelConstants.ChunkSize;
            float startZ = sourceChunkZ * VoxelConstants.ChunkSize + Hash01(caveSeed + 2) * VoxelConstants.ChunkSize;
            float startY = 18f + Hash01(caveSeed + 3) * 80f;

            float yaw = Hash01(caveSeed + 4) * Mathf.PI * 2f;
            float pitch = (Hash01(caveSeed + 5) - 0.5f) * 0.18f;

            int steps = 50 + Mathf.FloorToInt(Hash01(caveSeed + 6) * 70f);
            float baseRadius = 1.8f + Hash01(caveSeed + 7) * 1.3f;

            float x = startX;
            float y = startY;
            float z = startZ;

            for (int step = 0; step < steps; step++)
            {
                float radius = baseRadius * (0.85f + Hash01(caveSeed + 1000 + step) * 0.4f);

                // occasional room node
                if (Hash01(caveSeed + 2000 + step) > 0.975f)
                {
                    float roomRadius = radius * (1.8f + Hash01(caveSeed + 3000 + step) * 1.3f);
                    CarveEllipsoid(chunk, x, y, z, roomRadius, roomRadius * 0.7f, roomRadius, surfaceY);
                }
                else
                {
                    CarveEllipsoid(chunk, x, y, z, radius, radius * 0.65f, radius, surfaceY);
                }

                // occasional branch
                if (Hash01(caveSeed + 4000 + step) > 0.985f)
                {
                    GenerateBranch(chunk, caveSeed + 5000 + step, x, y, z, yaw, pitch, baseRadius * 0.8f, surfaceY);
                }

                yaw += (Hash01(caveSeed + 6000 + step) - 0.5f) * 0.22f;
                pitch += (Hash01(caveSeed + 7000 + step) - 0.5f) * 0.08f;
                pitch = Mathf.Clamp(pitch, -0.45f, 0.45f);

                float dx = Mathf.Cos(yaw) * Mathf.Cos(pitch);
                float dy = Mathf.Sin(pitch);
                float dz = Mathf.Sin(yaw) * Mathf.Cos(pitch);

                x += dx * 1.7f;
                y += dy * 1.0f;
                z += dz * 1.7f;

                if (y < 10f || y > 120f)
                    break;
            }
        }

        private static void GenerateBranch(Chunk chunk, int branchSeed, float startX, float startY, float startZ, float parentYaw, float parentPitch, float baseRadius, int[,] surfaceY)
        {
            float x = startX;
            float y = startY;
            float z = startZ;

            float yaw = parentYaw + (Hash01(branchSeed + 1) - 0.5f) * 1.2f;
            float pitch = parentPitch + (Hash01(branchSeed + 2) - 0.5f) * 0.25f;

            int steps = 18 + Mathf.FloorToInt(Hash01(branchSeed + 3) * 26f);

            for (int step = 0; step < steps; step++)
            {
                float radius = baseRadius * (0.8f + Hash01(branchSeed + 100 + step) * 0.35f);
                CarveEllipsoid(chunk, x, y, z, radius, radius * 0.65f, radius, surfaceY);

                yaw += (Hash01(branchSeed + 200 + step) - 0.5f) * 0.24f;
                pitch += (Hash01(branchSeed + 300 + step) - 0.5f) * 0.09f;
                pitch = Mathf.Clamp(pitch, -0.5f, 0.5f);

                float dx = Mathf.Cos(yaw) * Mathf.Cos(pitch);
                float dy = Mathf.Sin(pitch);
                float dz = Mathf.Sin(yaw) * Mathf.Cos(pitch);

                x += dx * 1.5f;
                y += dy * 0.9f;
                z += dz * 1.5f;

                if (y < 10f || y > 110f)
                    break;
            }
        }

        private static void GenerateCavern(Chunk chunk, int sourceChunkX, int sourceChunkZ, int cavernSeed, int[,] surfaceY)
        {
            float centerX = sourceChunkX * VoxelConstants.ChunkSize + Hash01(cavernSeed + 1) * VoxelConstants.ChunkSize;
            float centerZ = sourceChunkZ * VoxelConstants.ChunkSize + Hash01(cavernSeed + 2) * VoxelConstants.ChunkSize;
            float centerY = 24f + Hash01(cavernSeed + 3) * 38f;

            float radiusXZ = 8f + Hash01(cavernSeed + 4) * 10f;
            float radiusY = 4f + Hash01(cavernSeed + 5) * 7f;

            CarveEllipsoid(chunk, centerX, centerY, centerZ, radiusXZ, radiusY, radiusXZ, surfaceY);

            int lobes = 2 + Mathf.FloorToInt(Hash01(cavernSeed + 6) * 3f);
            for (int i = 0; i < lobes; i++)
            {
                float ox = (Hash01(cavernSeed + 100 + i * 3 + 1) - 0.5f) * radiusXZ * 1.6f;
                float oy = (Hash01(cavernSeed + 100 + i * 3 + 2) - 0.5f) * radiusY * 1.2f;
                float oz = (Hash01(cavernSeed + 100 + i * 3 + 3) - 0.5f) * radiusXZ * 1.6f;

                float lobeXZ = radiusXZ * (0.45f + Hash01(cavernSeed + 200 + i) * 0.4f);
                float lobeY = radiusY * (0.55f + Hash01(cavernSeed + 300 + i) * 0.5f);

                CarveEllipsoid(chunk, centerX + ox, centerY + oy, centerZ + oz, lobeXZ, lobeY, lobeXZ, surfaceY);
            }
        }

        private static void GenerateEntranceCave(Chunk chunk, int sourceChunkX, int sourceChunkZ, int entranceSeed, int[,] surfaceY)
        {
            float startX = sourceChunkX * VoxelConstants.ChunkSize + Hash01(entranceSeed + 1) * VoxelConstants.ChunkSize;
            float startZ = sourceChunkZ * VoxelConstants.ChunkSize + Hash01(entranceSeed + 2) * VoxelConstants.ChunkSize;

            int lx = Mathf.FloorToInt(startX) - sourceChunkX * VoxelConstants.ChunkSize;
            int lz = Mathf.FloorToInt(startZ) - sourceChunkZ * VoxelConstants.ChunkSize;

            lx = Mathf.Clamp(lx, 0, VoxelConstants.ChunkSize - 1);
            lz = Mathf.Clamp(lz, 0, VoxelConstants.ChunkSize - 1);

            // Approximate local surface if source chunk == current chunk. If not, just use a reasonable altitude.
            float startY = 75f + Hash01(entranceSeed + 3) * 22f;

            if (sourceChunkX == chunk.Coord.x && sourceChunkZ == chunk.Coord.y)
                startY = surfaceY[lx, lz] - 3f;

            float yaw = Hash01(entranceSeed + 4) * Mathf.PI * 2f;
            float pitch = -0.22f - Hash01(entranceSeed + 5) * 0.18f;

            float x = startX;
            float y = startY;
            float z = startZ;

            int steps = 26 + Mathf.FloorToInt(Hash01(entranceSeed + 6) * 18f);
            float baseRadius = 2.1f + Hash01(entranceSeed + 7) * 1.2f;

            for (int step = 0; step < steps; step++)
            {
                float radius = baseRadius * (0.9f + Hash01(entranceSeed + 100 + step) * 0.4f);
                CarveEntranceEllipsoid(chunk, x, y, z, radius, radius * 0.7f, radius, surfaceY);

                yaw += (Hash01(entranceSeed + 200 + step) - 0.5f) * 0.18f;
                pitch += (Hash01(entranceSeed + 300 + step) - 0.5f) * 0.05f;
                pitch = Mathf.Clamp(pitch, -0.45f, 0.1f);

                float dx = Mathf.Cos(yaw) * Mathf.Cos(pitch);
                float dy = Mathf.Sin(pitch);
                float dz = Mathf.Sin(yaw) * Mathf.Cos(pitch);

                x += dx * 1.5f;
                y += dy * 1.0f;
                z += dz * 1.5f;

                if (y < 18f || y > 120f)
                    break;
            }
        }

        private static void CarveEntranceEllipsoid(Chunk chunk, float worldX, float worldY, float worldZ, float radiusX, float radiusY, float radiusZ, int[,] surfaceY)
        {
            // More conservative near surface than normal cave carving.
            int minX = Mathf.FloorToInt(worldX - radiusX);
            int maxX = Mathf.CeilToInt(worldX + radiusX);
            int minY = Mathf.FloorToInt(worldY - radiusY);
            int maxY = Mathf.CeilToInt(worldY + radiusY);
            int minZ = Mathf.FloorToInt(worldZ - radiusZ);
            int maxZ = Mathf.CeilToInt(worldZ + radiusZ);

            int chunkMinX = chunk.Coord.x * VoxelConstants.ChunkSize;
            int chunkMinZ = chunk.Coord.y * VoxelConstants.ChunkSize;
            int chunkMaxX = chunkMinX + VoxelConstants.ChunkSize - 1;
            int chunkMaxZ = chunkMinZ + VoxelConstants.ChunkSize - 1;

            if (maxX < chunkMinX || minX > chunkMaxX || maxZ < chunkMinZ || minZ > chunkMaxZ)
                return;

            for (int wx = minX; wx <= maxX; wx++)
            {
                if (wx < chunkMinX || wx > chunkMaxX)
                    continue;

                int lx = wx - chunkMinX;

                for (int wy = minY; wy <= maxY; wy++)
                {
                    if (wy < 1 || wy >= VoxelConstants.WorldHeight - 1)
                        continue;

                    for (int wz = minZ; wz <= maxZ; wz++)
                    {
                        if (wz < chunkMinZ || wz > chunkMaxZ)
                            continue;

                        int lz = wz - chunkMinZ;

                        float nx = (wx - worldX) / radiusX;
                        float ny = (wy - worldY) / radiusY;
                        float nz = (wz - worldZ) / radiusZ;

                        if (nx * nx + ny * ny + nz * nz > 1f)
                            continue;

                        // Never expose grass block directly
                        int protectedSurface = surfaceY[lx, lz] - (SurfaceProtectionDepth + 2);
                        if (wy > protectedSurface)
                            continue;

                        ushort current = chunk.GetBlockLocal(lx, wy, lz).Id;
                        if (current == StoneId || current == DirtId || current == SandId)
                            chunk.SetBlockLocal(lx, wy, lz, new BlockData(AirId));
                    }
                }
            }
        }

        private static void CarveEllipsoid(Chunk chunk, float worldX, float worldY, float worldZ, float radiusX, float radiusY, float radiusZ, int[,] surfaceY)
        {
            int minX = Mathf.FloorToInt(worldX - radiusX);
            int maxX = Mathf.CeilToInt(worldX + radiusX);
            int minY = Mathf.FloorToInt(worldY - radiusY);
            int maxY = Mathf.CeilToInt(worldY + radiusY);
            int minZ = Mathf.FloorToInt(worldZ - radiusZ);
            int maxZ = Mathf.CeilToInt(worldZ + radiusZ);

            int chunkMinX = chunk.Coord.x * VoxelConstants.ChunkSize;
            int chunkMinZ = chunk.Coord.y * VoxelConstants.ChunkSize;
            int chunkMaxX = chunkMinX + VoxelConstants.ChunkSize - 1;
            int chunkMaxZ = chunkMinZ + VoxelConstants.ChunkSize - 1;

            if (maxX < chunkMinX || minX > chunkMaxX || maxZ < chunkMinZ || minZ > chunkMaxZ)
                return;

            for (int wx = minX; wx <= maxX; wx++)
            {
                if (wx < chunkMinX || wx > chunkMaxX)
                    continue;

                int lx = wx - chunkMinX;

                for (int wy = minY; wy <= maxY; wy++)
                {
                    if (wy < 1 || wy >= VoxelConstants.WorldHeight - 1)
                        continue;

                    for (int wz = minZ; wz <= maxZ; wz++)
                    {
                        if (wz < chunkMinZ || wz > chunkMaxZ)
                            continue;

                        int lz = wz - chunkMinZ;

                        float nx = (wx - worldX) / radiusX;
                        float ny = (wy - worldY) / radiusY;
                        float nz = (wz - worldZ) / radiusZ;

                        if (nx * nx + ny * ny + nz * nz > 1f)
                            continue;

                        // Protect surface shell so grass is never visible from caves.
                        int protectedSurface = surfaceY[lx, lz] - SurfaceProtectionDepth;
                        if (wy > protectedSurface)
                            continue;

                        ushort current = chunk.GetBlockLocal(lx, wy, lz).Id;

                        if (current == StoneId || current == DirtId || current == SandId)
                        {
                            chunk.SetBlockLocal(lx, wy, lz, new BlockData(AirId));
                        }
                    }
                }
            }
        }

        private static void PlaceCavernSpikes(Chunk chunk, int seed, int[,] surfaceY)
        {
            int baseWorldX = chunk.Coord.x * VoxelConstants.ChunkSize;
            int baseWorldZ = chunk.Coord.y * VoxelConstants.ChunkSize;

            for (int x = 1; x < VoxelConstants.ChunkSize - 1; x++)
            {
                for (int z = 1; z < VoxelConstants.ChunkSize - 1; z++)
                {
                    int worldX = baseWorldX + x;
                    int worldZ = baseWorldZ + z;

                    float spikeChance = Hash01(HashInt(worldX, worldZ, seed + 300000));
                    if (spikeChance > 0.018f)
                        continue;

                    int maxY = Mathf.Min(surfaceY[x, z] - SurfaceProtectionDepth - 2, 120);

                    for (int y = 18; y < maxY; y++)
                    {
                        if (chunk.GetBlockLocal(x, y, z).Id != AirId)
                            continue;

                        if (chunk.GetBlockLocal(x, y - 1, z).Id == StoneId)
                        {
                            int height = 2 + Mathf.FloorToInt(Hash01(HashInt(worldX, worldZ, seed + y + 300001)) * 5f);
                            for (int h = 0; h < height; h++)
                            {
                                int yy = y + h;
                                if (yy >= VoxelConstants.WorldHeight)
                                    break;
                                if (chunk.GetBlockLocal(x, yy, z).Id != AirId)
                                    break;
                                if (yy > surfaceY[x, z] - SurfaceProtectionDepth - 1)
                                    break;
                                chunk.SetBlockLocal(x, yy, z, new BlockData(StoneId));
                            }
                            break;
                        }

                        if (chunk.GetBlockLocal(x, y + 1, z).Id == StoneId)
                        {
                            int height = 2 + Mathf.FloorToInt(Hash01(HashInt(worldX, worldZ, seed + y + 300002)) * 5f);
                            for (int h = 0; h < height; h++)
                            {
                                int yy = y - h;
                                if (yy < 0)
                                    break;
                                if (chunk.GetBlockLocal(x, yy, z).Id != AirId)
                                    break;
                                chunk.SetBlockLocal(x, yy, z, new BlockData(StoneId));
                            }
                            break;
                        }
                    }
                }
            }
        }

        private static int HashInt(int a, int b, int seed)
        {
            unchecked
            {
                int h = a * 374761393 + b * 668265263 + seed * 1442695041;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= (h >> 16);
                return h;
            }
        }

        private static int PositiveMod(int value, int mod)
        {
            int r = value % mod;
            return r < 0 ? r + mod : r;
        }

        private static float Hash01(int value)
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