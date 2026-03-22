namespace Blockbound.Chunks
{
    public class ChunkSnapshot
    {
        public int ChunkX;
        public int ChunkZ;
        public int SectionIndex;

        public ushort[] Blocks;
        public byte[] CombinedLight;

        public int SizeX;
        public int SizeY;
        public int SizeZ;
        public int SliceSize;

        public void EnsureSize(int sizeX, int sizeY, int sizeZ)
        {
            int total = sizeX * sizeY * sizeZ;

            if (Blocks == null || Blocks.Length != total)
                Blocks = new ushort[total];

            if (CombinedLight == null || CombinedLight.Length != total)
                CombinedLight = new byte[total];

            SizeX = sizeX;
            SizeY = sizeY;
            SizeZ = sizeZ;
            SliceSize = sizeX * sizeZ;
        }

        public int Index(int x, int y, int z)
        {
            return x + z * SizeX + y * SliceSize;
        }

        public ushort GetBlock(int x, int y, int z)
        {
            return Blocks[Index(x, y, z)];
        }

        public void SetBlock(int x, int y, int z, ushort id)
        {
            Blocks[Index(x, y, z)] = id;
        }

        public byte GetCombinedLight(int x, int y, int z)
        {
            return CombinedLight[Index(x, y, z)];
        }

        public void SetCombinedLight(int x, int y, int z, byte value)
        {
            CombinedLight[Index(x, y, z)] = value;
        }
    }
}