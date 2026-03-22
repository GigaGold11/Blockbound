using Blockbound.Blocks;
using Blockbound.Chunks;
using Blockbound.Core;

namespace Blockbound.Generation
{
    public static class FlatWorldGenerator
    {
        public static void GenerateChunk(Chunk chunk)
        {
            for (int x = 0; x < VoxelConstants.ChunkSize; x++)
            {
                for (int z = 0; z < VoxelConstants.ChunkSize; z++)
                {
                    for (int y = 0; y < 64; y++)
                    {
                        ushort blockId;

                        if (y == 63) blockId = 1;       // grass
                        else if (y >= 60) blockId = 2;  // dirt
                        else blockId = 3;               // stone

                        chunk.SetBlockLocal(x, y, z, new BlockData(blockId));
                    }
                }
            }

            chunk.IsGenerated = true;
            chunk.IsDirty = true;
        }
    }
}