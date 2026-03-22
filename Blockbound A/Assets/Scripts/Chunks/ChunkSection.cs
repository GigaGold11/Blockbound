using Blockbound.Blocks;
using Blockbound.Core;

namespace Blockbound.Chunks
{
    public class ChunkSection
    {
        public int SectionY { get; private set; }
        public bool HasNonAir { get; private set; }
        public bool NeedsMeshRebuild { get; set; } = true;
        public bool NeedsLightRebuild { get; set; } = true;

        public ChunkSectionMeshData MeshData { get; set; }

        private readonly BlockData[] blocks;
        private readonly byte[] skyLight;
        private readonly byte[] blockLight;

        public ChunkSection(int sectionY)
        {
            SectionY = sectionY;

            int size = VoxelConstants.ChunkSize * VoxelConstants.SectionHeight * VoxelConstants.ChunkSize;
            blocks = new BlockData[size];
            skyLight = new byte[size];
            blockLight = new byte[size];
        }

        public BlockData GetBlock(int x, int y, int z)
        {
            return blocks[GetIndex(x, y, z)];
        }

        public void SetBlock(int x, int y, int z, BlockData block)
        {
            blocks[GetIndex(x, y, z)] = block;

            if (block.Id != 0)
                HasNonAir = true;

            NeedsMeshRebuild = true;
            NeedsLightRebuild = true;
        }

        public void RecalculateHasNonAir()
        {
            HasNonAir = false;
            for (int i = 0; i < blocks.Length; i++)
            {
                if (blocks[i].Id != 0)
                {
                    HasNonAir = true;
                    return;
                }
            }
        }

        public byte GetSkyLight(int x, int y, int z) => skyLight[GetIndex(x, y, z)];
        public byte GetBlockLight(int x, int y, int z) => blockLight[GetIndex(x, y, z)];
        public void SetSkyLight(int x, int y, int z, byte value) => skyLight[GetIndex(x, y, z)] = value;
        public void SetBlockLight(int x, int y, int z, byte value) => blockLight[GetIndex(x, y, z)] = value;

        public void ClearSkyLight()
        {
            for (int i = 0; i < skyLight.Length; i++) skyLight[i] = 0;
        }

        public void ClearBlockLight()
        {
            for (int i = 0; i < blockLight.Length; i++) blockLight[i] = 0;
        }

        private int GetIndex(int x, int y, int z)
        {
            return x + (z * VoxelConstants.ChunkSize) + (y * VoxelConstants.ChunkSize * VoxelConstants.ChunkSize);
        }
    }
}