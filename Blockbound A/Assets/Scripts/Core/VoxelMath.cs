using UnityEngine;

namespace Blockbound.Core
{
    public static class VoxelMath
    {
        public static int FloorDiv(int value, int divisor)
        {
            int result = value / divisor;
            int remainder = value % divisor;

            if (remainder != 0 && ((remainder < 0) != (divisor < 0)))
                result--;

            return result;
        }

        public static int Mod(int value, int modulus)
        {
            int result = value % modulus;
            if (result < 0)
                result += modulus;
            return result;
        }

        public static Vector2Int WorldToChunkCoord(int worldX, int worldZ)
        {
            return new Vector2Int(
                FloorDiv(worldX, VoxelConstants.ChunkSize),
                FloorDiv(worldZ, VoxelConstants.ChunkSize)
            );
        }

        public static Vector3Int WorldToLocalBlock(int worldX, int worldY, int worldZ)
        {
            return new Vector3Int(
                Mod(worldX, VoxelConstants.ChunkSize),
                worldY,
                Mod(worldZ, VoxelConstants.ChunkSize)
            );
        }

        public static Vector3 ChunkToWorldPosition(Vector2Int chunkCoord)
        {
            return new Vector3(
                chunkCoord.x * VoxelConstants.ChunkSize,
                0,
                chunkCoord.y * VoxelConstants.ChunkSize
            );
        }
    }
}