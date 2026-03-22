using System.Collections.Generic;
using Blockbound.Blocks;
using Blockbound.Chunks;
using Blockbound.Core;
using UnityEngine;

namespace Blockbound.World
{
    public class VoxelWorld : MonoBehaviour
    {
        private readonly Dictionary<Vector2Int, Chunk> chunks = new Dictionary<Vector2Int, Chunk>();
        public IReadOnlyDictionary<Vector2Int, Chunk> Chunks => chunks;

        public bool TryGetChunk(Vector2Int coord, out Chunk chunk)
        {
            return chunks.TryGetValue(coord, out chunk);
        }

        public Chunk GetOrCreateChunk(Vector2Int coord)
        {
            if (!chunks.TryGetValue(coord, out var chunk))
            {
                chunk = new Chunk(coord);
                chunks.Add(coord, chunk);
            }

            return chunk;
        }

        public bool RemoveChunk(Vector2Int coord)
        {
            return chunks.Remove(coord);
        }

        public BlockData GetBlock(int worldX, int worldY, int worldZ)
        {
            if (worldY < 0 || worldY >= VoxelConstants.WorldHeight)
                return new BlockData(0);

            Vector2Int chunkCoord = VoxelMath.WorldToChunkCoord(worldX, worldZ);
            if (!chunks.TryGetValue(chunkCoord, out var chunk))
                return new BlockData(0);

            Vector3Int local = VoxelMath.WorldToLocalBlock(worldX, worldY, worldZ);
            return chunk.GetBlockLocal(local.x, local.y, local.z);
        }

        public void SetBlock(int worldX, int worldY, int worldZ, BlockData block)
        {
            if (worldY < 0 || worldY >= VoxelConstants.WorldHeight)
                return;

            Vector2Int chunkCoord = VoxelMath.WorldToChunkCoord(worldX, worldZ);
            Chunk chunk = GetOrCreateChunk(chunkCoord);

            Vector3Int local = VoxelMath.WorldToLocalBlock(worldX, worldY, worldZ);
            chunk.SetBlockLocal(local.x, local.y, local.z, block);

            chunk.IsDirty = true;
            chunk.LightDirty = true;

            if (chunk.LoadState == ChunkLoadState.Meshed)
                chunk.LoadState = ChunkLoadState.Lit;

            MarkNeighborChunksDirtyIfOnBorder(local.x, local.y, local.z, chunkCoord);
        }

        public byte GetSkyLight(int worldX, int worldY, int worldZ)
        {
            if (worldY < 0 || worldY >= VoxelConstants.WorldHeight)
                return 0;

            Vector2Int chunkCoord = VoxelMath.WorldToChunkCoord(worldX, worldZ);
            if (!chunks.TryGetValue(chunkCoord, out var chunk))
                return 0;

            Vector3Int local = VoxelMath.WorldToLocalBlock(worldX, worldY, worldZ);
            return chunk.GetSkyLightLocal(local.x, local.y, local.z);
        }

        public byte GetBlockLight(int worldX, int worldY, int worldZ)
        {
            if (worldY < 0 || worldY >= VoxelConstants.WorldHeight)
                return 0;

            Vector2Int chunkCoord = VoxelMath.WorldToChunkCoord(worldX, worldZ);
            if (!chunks.TryGetValue(chunkCoord, out var chunk))
                return 0;

            Vector3Int local = VoxelMath.WorldToLocalBlock(worldX, worldY, worldZ);
            return chunk.GetBlockLightLocal(local.x, local.y, local.z);
        }

        public byte GetCombinedLight(int worldX, int worldY, int worldZ)
        {
            byte s = GetSkyLight(worldX, worldY, worldZ);
            byte b = GetBlockLight(worldX, worldY, worldZ);
            return s > b ? s : b;
        }

        private void MarkNeighborChunksDirtyIfOnBorder(int localX, int localY, int localZ, Vector2Int chunkCoord)
        {
            Chunk neighbor;

            if (localX == 0 && chunks.TryGetValue(chunkCoord + Vector2Int.left, out neighbor))
            {
                neighbor.IsDirty = true;
                neighbor.LightDirty = true;
                if (neighbor.LoadState == ChunkLoadState.Meshed)
                    neighbor.LoadState = ChunkLoadState.Lit;
            }

            if (localX == VoxelConstants.ChunkSize - 1 && chunks.TryGetValue(chunkCoord + Vector2Int.right, out neighbor))
            {
                neighbor.IsDirty = true;
                neighbor.LightDirty = true;
                if (neighbor.LoadState == ChunkLoadState.Meshed)
                    neighbor.LoadState = ChunkLoadState.Lit;
            }

            if (localZ == 0 && chunks.TryGetValue(chunkCoord + new Vector2Int(0, -1), out neighbor))
            {
                neighbor.IsDirty = true;
                neighbor.LightDirty = true;
                if (neighbor.LoadState == ChunkLoadState.Meshed)
                    neighbor.LoadState = ChunkLoadState.Lit;
            }

            if (localZ == VoxelConstants.ChunkSize - 1 && chunks.TryGetValue(chunkCoord + new Vector2Int(0, 1), out neighbor))
            {
                neighbor.IsDirty = true;
                neighbor.LightDirty = true;
                if (neighbor.LoadState == ChunkLoadState.Meshed)
                    neighbor.LoadState = ChunkLoadState.Lit;
            }

            int sectionIndex = localY / VoxelConstants.SectionHeight;

            if (localY == 0)
                MarkSectionDirty(chunkCoord, sectionIndex - 1);

            if (localY == VoxelConstants.SectionHeight - 1)
                MarkSectionDirty(chunkCoord, sectionIndex + 1);
        }

        private void MarkSectionDirty(Vector2Int chunkCoord, int sectionIndex)
        {
            if (!chunks.TryGetValue(chunkCoord, out var chunk))
                return;

            chunk.MarkSectionDirty(sectionIndex);
            chunk.IsDirty = true;
            chunk.LightDirty = true;
        }
    }
}