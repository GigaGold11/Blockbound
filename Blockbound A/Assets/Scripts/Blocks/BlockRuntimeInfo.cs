namespace Blockbound.Blocks
{
    public struct BlockRuntimeInfo
    {
        public bool Exists;
        public bool IsAir;
        public bool IsSolid;
        public bool IsOpaque;
        public bool IsReplaceable;
        public bool EmitsLight;
        public byte LightLevel;

        public BlockRenderType RenderType;
        public BlockRenderShape RenderShape;

        public int TopTextureIndex;
        public int BottomTextureIndex;
        public int SideTextureIndex;
    }
}