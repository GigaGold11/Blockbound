using System.Collections.Generic;
using Blockbound.Chunks;
using Blockbound.World;
using UnityEngine;

namespace Blockbound.Meshing
{
    public class ChunkBuildScheduler : MonoBehaviour
    {
        [SerializeField] private VoxelWorld voxelWorld;
        [SerializeField] private int maxChunksPerFrame = 2;

        private readonly Queue<Vector2Int> rebuildQueue = new Queue<Vector2Int>();
        private readonly HashSet<Vector2Int> queued = new HashSet<Vector2Int>();

        public void EnqueueChunk(Vector2Int coord)
        {
            if (queued.Contains(coord))
                return;

            queued.Add(coord);
            rebuildQueue.Enqueue(coord);
        }

        public List<Chunk> DequeueChunks()
        {
            List<Chunk> result = new List<Chunk>();
            int count = 0;

            while (rebuildQueue.Count > 0 && count < maxChunksPerFrame)
            {
                Vector2Int coord = rebuildQueue.Dequeue();
                queued.Remove(coord);

                if (voxelWorld != null && voxelWorld.TryGetChunk(coord, out var chunk))
                {
                    result.Add(chunk);
                    count++;
                }
            }

            return result;
        }
    }
}