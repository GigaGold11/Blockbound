namespace Blockbound.Core
{
    public static class VoxelConstants
    {
        public const int ChunkSize = 16;
        public const int SectionHeight = 16;

        // Use 512 internally for clean section math.
        public const int WorldHeight = 512;

        public const int SectionsPerChunk = WorldHeight / SectionHeight;

        public const int MaxLight = 15;

        public const int SeaLevel = 96; // temporary, can tune later

        public const float BlockSize = 1f;
    }
}