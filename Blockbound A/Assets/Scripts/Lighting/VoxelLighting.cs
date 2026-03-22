using System.Collections.Generic;
using Blockbound.Blocks;
using Blockbound.Chunks;
using Blockbound.Core;
using Blockbound.Meshing;
using Blockbound.World;
using UnityEngine;

namespace Blockbound.Lighting
{
    public static class VoxelLighting
    {
        private static readonly Vector3Int[] Dirs =
        {
            new Vector3Int(1, 0, 0),
            new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 1, 0),
            new Vector3Int(0, -1, 0),
            new Vector3Int(0, 0, 1),
            new Vector3Int(0, 0, -1)
        };

        public static void RebuildChunkLighting(VoxelWorld world, ChunkBuildScheduler scheduler, Chunk chunk)
        {
            if (world == null || scheduler == null || chunk == null || BlockRegistry.Instance == null)
                return;

            RebuildLightingRegion(world, scheduler, chunk.Coord);
        }

        public static void RebuildChunkLighting(VoxelWorld world, ChunkBuildScheduler scheduler, Vector2Int chunkCoord)
        {
            if (world == null || scheduler == null || BlockRegistry.Instance == null)
                return;

            RebuildLightingRegion(world, scheduler, chunkCoord);
        }

        public static void RebuildLightingRegion(VoxelWorld world, ChunkBuildScheduler scheduler, Vector2Int centerCoord)
        {
            if (world == null || scheduler == null || BlockRegistry.Instance == null)
                return;

            BlockRegistry registry = BlockRegistry.Instance;

            List<Chunk> region = GatherRegion(world, centerCoord);
            if (region.Count == 0)
                return;

            HashSet<Chunk> touchedChunks = new HashSet<Chunk>();
            Queue<Vector3Int> skyQueue = new Queue<Vector3Int>(16384);
            Queue<Vector3Int> blockQueue = new Queue<Vector3Int>(4096);

            for (int i = 0; i < region.Count; i++)
            {
                Chunk chunk = region[i];
                chunk.ClearAllLighting();
                touchedChunks.Add(chunk);
            }

            for (int i = 0; i < region.Count; i++)
                SeedSkyLight(world, region[i], registry, skyQueue, touchedChunks);

            PropagateSkyLight(world, registry, skyQueue, touchedChunks);

            for (int i = 0; i < region.Count; i++)
                SeedBlockLight(region[i], registry, blockQueue, touchedChunks);

            PropagateBlockLight(world, registry, blockQueue, touchedChunks);

            foreach (Chunk touched in touchedChunks)
            {
                touched.LightDirty = false;
                touched.IsDirty = true;
                scheduler.EnqueueChunk(touched.Coord);
            }
        }

        private static List<Chunk> GatherRegion(VoxelWorld world, Vector2Int centerCoord)
        {
            List<Chunk> result = new List<Chunk>(9);

            for (int dz = -1; dz <= 1; dz++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    Vector2Int coord = new Vector2Int(centerCoord.x + dx, centerCoord.y + dz);
                    if (world.TryGetChunk(coord, out Chunk chunk) && chunk != null)
                        result.Add(chunk);
                }
            }

            return result;
        }

        private static void SeedSkyLight(
            VoxelWorld world,
            Chunk chunk,
            BlockRegistry registry,
            Queue<Vector3Int> queue,
            HashSet<Chunk> touchedChunks)
        {
            int baseWorldX = chunk.Coord.x * VoxelConstants.ChunkSize;
            int baseWorldZ = chunk.Coord.y * VoxelConstants.ChunkSize;

            for (int lx = 0; lx < VoxelConstants.ChunkSize; lx++)
            {
                for (int lz = 0; lz < VoxelConstants.ChunkSize; lz++)
                {
                    bool blocked = false;

                    for (int y = VoxelConstants.WorldHeight - 1; y >= 0; y--)
                    {
                        BlockData block = chunk.GetBlockLocal(lx, y, lz);
                        BlockDefinition def = registry.Get(block.Id);
                        bool opaque = def != null && def.IsOpaque;

                        if (opaque)
                        {
                            blocked = true;
                            continue;
                        }

                        if (!blocked)
                        {
                            byte existing = chunk.GetSkyLightLocal(lx, y, lz);
                            if (existing < VoxelConstants.MaxLight)
                            {
                                chunk.SetSkyLightLocal(lx, y, lz, VoxelConstants.MaxLight);
                                touchedChunks.Add(chunk);
                                queue.Enqueue(new Vector3Int(baseWorldX + lx, y, baseWorldZ + lz));
                            }
                        }
                    }
                }
            }
        }

        private static void PropagateSkyLight(
            VoxelWorld world,
            BlockRegistry registry,
            Queue<Vector3Int> queue,
            HashSet<Chunk> touchedChunks)
        {
            while (queue.Count > 0)
            {
                Vector3Int pos = queue.Dequeue();
                byte current = world.GetSkyLight(pos.x, pos.y, pos.z);
                if (current == 0)
                    continue;

                for (int i = 0; i < Dirs.Length; i++)
                {
                    Vector3Int dir = Dirs[i];
                    int nx = pos.x + dir.x;
                    int ny = pos.y + dir.y;
                    int nz = pos.z + dir.z;

                    if (ny < 0 || ny >= VoxelConstants.WorldHeight)
                        continue;

                    BlockData neighbor = world.GetBlock(nx, ny, nz);
                    BlockDefinition neighborDef = registry.Get(neighbor.Id);
                    bool opaque = neighborDef != null && neighborDef.IsOpaque;

                    if (opaque)
                        continue;

                    byte next;

                    if (dir.y == -1 && current == VoxelConstants.MaxLight)
                    {
                        next = VoxelConstants.MaxLight;
                    }
                    else
                    {
                        if (current <= 1)
                            continue;

                        next = (byte)(current - 1);
                    }

                    if (SetSkyLightWorld(world, nx, ny, nz, next, touchedChunks))
                        queue.Enqueue(new Vector3Int(nx, ny, nz));
                }
            }
        }

        private static void SeedBlockLight(
            Chunk chunk,
            BlockRegistry registry,
            Queue<Vector3Int> queue,
            HashSet<Chunk> touchedChunks)
        {
            int baseWorldX = chunk.Coord.x * VoxelConstants.ChunkSize;
            int baseWorldZ = chunk.Coord.y * VoxelConstants.ChunkSize;

            for (int y = 0; y < VoxelConstants.WorldHeight; y++)
            {
                for (int lz = 0; lz < VoxelConstants.ChunkSize; lz++)
                {
                    for (int lx = 0; lx < VoxelConstants.ChunkSize; lx++)
                    {
                        BlockData block = chunk.GetBlockLocal(lx, y, lz);
                        if (block.Id == 0)
                            continue;

                        BlockDefinition def = registry.Get(block.Id);
                        if (def == null || !def.EmitsLight || def.LightLevel == 0)
                            continue;

                        byte existing = chunk.GetBlockLightLocal(lx, y, lz);
                        if (existing >= def.LightLevel)
                            continue;

                        chunk.SetBlockLightLocal(lx, y, lz, def.LightLevel);
                        touchedChunks.Add(chunk);
                        queue.Enqueue(new Vector3Int(baseWorldX + lx, y, baseWorldZ + lz));
                    }
                }
            }
        }

        private static void PropagateBlockLight(
            VoxelWorld world,
            BlockRegistry registry,
            Queue<Vector3Int> queue,
            HashSet<Chunk> touchedChunks)
        {
            while (queue.Count > 0)
            {
                Vector3Int pos = queue.Dequeue();
                byte current = world.GetBlockLight(pos.x, pos.y, pos.z);
                if (current <= 1)
                    continue;

                byte next = (byte)(current - 1);

                for (int i = 0; i < Dirs.Length; i++)
                {
                    int nx = pos.x + Dirs[i].x;
                    int ny = pos.y + Dirs[i].y;
                    int nz = pos.z + Dirs[i].z;

                    if (ny < 0 || ny >= VoxelConstants.WorldHeight)
                        continue;

                    BlockData neighbor = world.GetBlock(nx, ny, nz);
                    BlockDefinition neighborDef = registry.Get(neighbor.Id);
                    bool opaque = neighborDef != null && neighborDef.IsOpaque;

                    if (opaque)
                        continue;

                    if (SetBlockLightWorld(world, nx, ny, nz, next, touchedChunks))
                        queue.Enqueue(new Vector3Int(nx, ny, nz));
                }
            }
        }

        private static bool SetSkyLightWorld(
            VoxelWorld world,
            int worldX,
            int y,
            int worldZ,
            byte value,
            HashSet<Chunk> touchedChunks)
        {
            if (y < 0 || y >= VoxelConstants.WorldHeight)
                return false;

            Vector2Int coord = VoxelMath.WorldToChunkCoord(worldX, worldZ);
            if (!world.TryGetChunk(coord, out Chunk chunk) || chunk == null)
                return false;

            Vector3Int local = VoxelMath.WorldToLocalBlock(worldX, y, worldZ);
            byte existing = chunk.GetSkyLightLocal(local.x, local.y, local.z);

            if (value <= existing)
                return false;

            chunk.SetSkyLightLocal(local.x, local.y, local.z, value);
            touchedChunks.Add(chunk);
            return true;
        }

        private static bool SetBlockLightWorld(
            VoxelWorld world,
            int worldX,
            int y,
            int worldZ,
            byte value,
            HashSet<Chunk> touchedChunks)
        {
            if (y < 0 || y >= VoxelConstants.WorldHeight)
                return false;

            Vector2Int coord = VoxelMath.WorldToChunkCoord(worldX, worldZ);
            if (!world.TryGetChunk(coord, out Chunk chunk) || chunk == null)
                return false;

            Vector3Int local = VoxelMath.WorldToLocalBlock(worldX, y, worldZ);
            byte existing = chunk.GetBlockLightLocal(local.x, local.y, local.z);

            if (value <= existing)
                return false;

            chunk.SetBlockLightLocal(local.x, local.y, local.z, value);
            touchedChunks.Add(chunk);
            return true;
        }
    }
}