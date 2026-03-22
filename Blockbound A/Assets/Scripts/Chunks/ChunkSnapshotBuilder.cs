using System.Collections.Concurrent;
using Blockbound.Blocks;
using Blockbound.Core;
using Blockbound.World;

namespace Blockbound.Chunks
{
    public static class ChunkSnapshotBuilder
    {
        private static readonly ConcurrentBag<ChunkSnapshot> SnapshotPool = new ConcurrentBag<ChunkSnapshot>();

        public static ChunkSnapshot BuildSectionSnapshot(VoxelWorld world, Chunk chunk, int sectionIndex)
        {
            int sectionBaseY = sectionIndex * VoxelConstants.SectionHeight;

            int sizeX = VoxelConstants.ChunkSize + 2;
            int sizeY = VoxelConstants.SectionHeight + 2;
            int sizeZ = VoxelConstants.ChunkSize + 2;

            if (!SnapshotPool.TryTake(out ChunkSnapshot snapshot))
                snapshot = new ChunkSnapshot();

            snapshot.ChunkX = chunk.Coord.x;
            snapshot.ChunkZ = chunk.Coord.y;
            snapshot.SectionIndex = sectionIndex;
            snapshot.EnsureSize(sizeX, sizeY, sizeZ);

            int chunkWorldX = chunk.Coord.x * VoxelConstants.ChunkSize;
            int chunkWorldZ = chunk.Coord.y * VoxelConstants.ChunkSize;

            for (int sx = 0; sx < sizeX; sx++)
            {
                for (int sy = 0; sy < sizeY; sy++)
                {
                    for (int sz = 0; sz < sizeZ; sz++)
                    {
                        int localX = sx - 1;
                        int worldY = sectionBaseY + (sy - 1);
                        int localZ = sz - 1;

                        int worldX = chunkWorldX + localX;
                        int worldZ = chunkWorldZ + localZ;

                        BlockData b = world.GetBlock(worldX, worldY, worldZ);
                        snapshot.SetBlock(sx, sy, sz, b.Id);
                        snapshot.SetCombinedLight(sx, sy, sz, world.GetCombinedLight(worldX, worldY, worldZ));
                    }
                }
            }

            return snapshot;
        }

        public static void Release(ChunkSnapshot snapshot)
        {
            if (snapshot == null)
                return;

            SnapshotPool.Add(snapshot);
        }

        public static bool IsOpaque(ChunkSnapshot snapshot, int x, int y, int z)
        {
            if (x < 0 || x >= snapshot.SizeX || y < 0 || y >= snapshot.SizeY || z < 0 || z >= snapshot.SizeZ)
                return false;

            ushort id = snapshot.GetBlock(x, y, z);
            return BlockRegistry.Instance != null && BlockRegistry.Instance.IsOpaque(id);
        }
    }
}