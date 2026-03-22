using Blockbound.Blocks;
using Blockbound.Core;
using Blockbound.World;
using UnityEngine;

namespace Blockbound.Chunks
{
    public class Chunk
    {
        public Vector2Int Coord { get; private set; }
        public ChunkSection[] Sections { get; private set; }

        public bool IsGenerated { get; set; }
        public bool IsDirty { get; set; } = true;
        public bool LightDirty { get; set; } = true;

        public ChunkLoadState LoadState { get; set; } = ChunkLoadState.Unloaded;

        public Chunk(Vector2Int coord)
        {
            Coord = coord;

            Sections = new ChunkSection[VoxelConstants.SectionsPerChunk];
            for (int i = 0; i < Sections.Length; i++)
                Sections[i] = new ChunkSection(i);
        }

        public BlockData GetBlockLocal(int x, int y, int z)
        {
            if (y < 0 || y >= VoxelConstants.WorldHeight)
                return new BlockData(0);

            int sectionIndex = y / VoxelConstants.SectionHeight;
            int localY = y % VoxelConstants.SectionHeight;
            return Sections[sectionIndex].GetBlock(x, localY, z);
        }

        public void SetBlockLocal(int x, int y, int z, BlockData block)
        {
            if (y < 0 || y >= VoxelConstants.WorldHeight)
                return;

            int sectionIndex = y / VoxelConstants.SectionHeight;
            int localY = y % VoxelConstants.SectionHeight;

            Sections[sectionIndex].SetBlock(x, localY, z, block);
            Sections[sectionIndex].RecalculateHasNonAir();

            IsDirty = true;
            LightDirty = true;

            MarkSectionDirty(sectionIndex);

            if (localY == 0)
                MarkSectionDirty(sectionIndex - 1);

            if (localY == VoxelConstants.SectionHeight - 1)
                MarkSectionDirty(sectionIndex + 1);
        }

        public void MarkSectionDirty(int sectionIndex)
        {
            if (sectionIndex < 0 || sectionIndex >= Sections.Length)
                return;

            Sections[sectionIndex].NeedsMeshRebuild = true;
            Sections[sectionIndex].NeedsLightRebuild = true;
        }

        public bool HasAnyDirtySection()
        {
            for (int i = 0; i < Sections.Length; i++)
            {
                if (Sections[i].NeedsMeshRebuild || Sections[i].NeedsLightRebuild)
                    return true;
            }

            return false;
        }

        public byte GetSkyLightLocal(int x, int y, int z)
        {
            if (y < 0 || y >= VoxelConstants.WorldHeight)
                return 0;

            int sectionIndex = y / VoxelConstants.SectionHeight;
            int localY = y % VoxelConstants.SectionHeight;
            return Sections[sectionIndex].GetSkyLight(x, localY, z);
        }

        public byte GetBlockLightLocal(int x, int y, int z)
        {
            if (y < 0 || y >= VoxelConstants.WorldHeight)
                return 0;

            int sectionIndex = y / VoxelConstants.SectionHeight;
            int localY = y % VoxelConstants.SectionHeight;
            return Sections[sectionIndex].GetBlockLight(x, localY, z);
        }

        public void SetSkyLightLocal(int x, int y, int z, byte value)
        {
            if (y < 0 || y >= VoxelConstants.WorldHeight)
                return;

            int sectionIndex = y / VoxelConstants.SectionHeight;
            int localY = y % VoxelConstants.SectionHeight;
            Sections[sectionIndex].SetSkyLight(x, localY, z, value);
        }

        public void SetBlockLightLocal(int x, int y, int z, byte value)
        {
            if (y < 0 || y >= VoxelConstants.WorldHeight)
                return;

            int sectionIndex = y / VoxelConstants.SectionHeight;
            int localY = y % VoxelConstants.SectionHeight;
            Sections[sectionIndex].SetBlockLight(x, localY, z, value);
        }

        public void ClearAllLighting()
        {
            for (int i = 0; i < Sections.Length; i++)
            {
                Sections[i].ClearSkyLight();
                Sections[i].ClearBlockLight();
                Sections[i].NeedsLightRebuild = false;
            }
        }

        public void ClearMeshDirtyFlags()
        {
            for (int i = 0; i < Sections.Length; i++)
                Sections[i].NeedsMeshRebuild = false;
        }
    }
}