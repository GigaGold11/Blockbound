using Blockbound.Chunks;

namespace Blockbound.Meshing
{
    public static class SnapshotAO
    {
        public static float AOToBrightness(byte ao)
        {
            switch (ao)
            {
                case 0: return 1.00f;
                case 1: return 0.84f;
                case 2: return 0.68f;
                default: return 0.52f;
            }
        }

        public static byte[] GetFaceAO(ChunkSnapshot snapshot, int x, int y, int z, int face)
        {
            return new byte[4] { 0, 0, 0, 0 };
        }
    }
}