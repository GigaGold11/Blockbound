namespace Blockbound.Blocks
{
    public struct BlockData
    {
        public ushort Id;
        public byte State;

        public BlockData(ushort id, byte state = 0)
        {
            Id = id;
            State = state;
        }
    }
}