namespace Blockbound.World
{
    public enum ChunkLoadState
    {
        Unloaded,
        QueuedForGeneration,
        Generated,
        QueuedForLighting,
        Lit,
        QueuedForMeshing,
        Meshed
    }
}