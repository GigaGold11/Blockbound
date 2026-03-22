using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Blockbound.Chunks;
using Blockbound.Generation;
using Blockbound.Lighting;
using Blockbound.Meshing;
using Blockbound.World;
using UnityEngine;

namespace Blockbound.Threading
{
    public class ThreadedChunkPipeline : MonoBehaviour
    {
        [SerializeField] private VoxelWorld voxelWorld;
        [SerializeField] private int seed = 123456;
        [SerializeField] private int maxUploadsPerFrame = 8;
        [SerializeField] private int maxGenerationThreads = 4;
        [SerializeField] private int maxLightingThreads = 2;
        [SerializeField] private int maxMeshingThreads = 4;

        private readonly ConcurrentQueue<Chunk> generationQueue = new ConcurrentQueue<Chunk>();
        private readonly ConcurrentQueue<Chunk> lightingQueue = new ConcurrentQueue<Chunk>();
        private readonly ConcurrentQueue<(Chunk chunk, int sectionIndex)> meshingQueue = new ConcurrentQueue<(Chunk, int)>();

        private readonly ConcurrentQueue<Chunk> generatedResults = new ConcurrentQueue<Chunk>();
        private readonly ConcurrentQueue<Chunk> litResults = new ConcurrentQueue<Chunk>();
        private readonly ConcurrentQueue<(Chunk chunk, int sectionIndex, ChunkSectionMeshData meshData)> meshResults =
            new ConcurrentQueue<(Chunk, int, ChunkSectionMeshData)>();

        private readonly HashSet<Vector2Int> generating = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> lighting = new HashSet<Vector2Int>();
        private readonly HashSet<string> meshing = new HashSet<string>();

        private int activeGenerationThreads;
        private int activeLightingThreads;
        private int activeMeshingThreads;

        public int Seed => seed;
        public void SetSeed(int newSeed) { seed = newSeed; }

        public void RequestGeneration(Chunk chunk)
        {
            lock (generating)
            {
                if (generating.Contains(chunk.Coord))
                    return;

                generating.Add(chunk.Coord);
            }

            generationQueue.Enqueue(chunk);
            TrySpawnGenerationThread();
        }

        public void RequestLighting(Chunk chunk)
        {
            lock (lighting)
            {
                if (lighting.Contains(chunk.Coord))
                    return;

                lighting.Add(chunk.Coord);
            }

            lightingQueue.Enqueue(chunk);
            TrySpawnLightingThread();
        }

        public void RequestMeshing(Chunk chunk, int sectionIndex)
        {
            string key = MeshKey(chunk.Coord, sectionIndex);

            lock (meshing)
            {
                if (meshing.Contains(key))
                    return;

                meshing.Add(key);
            }

            meshingQueue.Enqueue((chunk, sectionIndex));
            TrySpawnMeshingThread();
        }

        private void TrySpawnGenerationThread()
        {
            lock (generating)
            {
                if (activeGenerationThreads >= maxGenerationThreads)
                    return;

                activeGenerationThreads++;
            }

            Task.Run(() =>
            {
                try
                {
                    while (generationQueue.TryDequeue(out var chunk))
                    {
                        SeededTerrainGenerator.GenerateChunk(chunk, seed);
                        generatedResults.Enqueue(chunk);
                    }
                }
                finally
                {
                    lock (generating)
                        activeGenerationThreads--;

                    if (!generationQueue.IsEmpty)
                        TrySpawnGenerationThread();
                }
            });
        }

        private void TrySpawnLightingThread()
        {
            lock (lighting)
            {
                if (activeLightingThreads >= maxLightingThreads)
                    return;

                activeLightingThreads++;
            }

            Task.Run(() =>
            {
                try
                {
                    while (lightingQueue.TryDequeue(out var chunk))
                    {
                        VoxelLighting.RebuildChunkLighting(voxelWorld, chunk);
                        litResults.Enqueue(chunk);
                    }
                }
                finally
                {
                    lock (lighting)
                        activeLightingThreads--;

                    if (!lightingQueue.IsEmpty)
                        TrySpawnLightingThread();
                }
            });
        }

        private void TrySpawnMeshingThread()
        {
            lock (meshing)
            {
                if (activeMeshingThreads >= maxMeshingThreads)
                    return;

                activeMeshingThreads++;
            }

            Task.Run(() =>
            {
                try
                {
                    while (meshingQueue.TryDequeue(out var item))
                    {
                        ChunkSnapshot snapshot = ChunkSnapshotBuilder.BuildSectionSnapshot(voxelWorld, item.chunk, item.sectionIndex);
                        ChunkSectionMeshData meshData = SectionMesher.BuildSectionMesh(snapshot);
                        ChunkSnapshotBuilder.Release(snapshot);

                        meshResults.Enqueue((item.chunk, item.sectionIndex, meshData));
                    }
                }
                finally
                {
                    lock (meshing)
                        activeMeshingThreads--;

                    if (!meshingQueue.IsEmpty)
                        TrySpawnMeshingThread();
                }
            });
        }

        public List<Chunk> ConsumeGenerated()
        {
            List<Chunk> result = new List<Chunk>();

            while (generatedResults.TryDequeue(out var chunk))
            {
                lock (generating)
                    generating.Remove(chunk.Coord);

                result.Add(chunk);
            }

            return result;
        }

        public List<Chunk> ConsumeLit()
        {
            List<Chunk> result = new List<Chunk>();

            while (litResults.TryDequeue(out var chunk))
            {
                lock (lighting)
                    lighting.Remove(chunk.Coord);

                result.Add(chunk);
            }

            return result;
        }

        public List<(Chunk chunk, int sectionIndex, ChunkSectionMeshData meshData)> ConsumeMeshes()
        {
            var result = new List<(Chunk, int, ChunkSectionMeshData)>();

            int uploaded = 0;
            while (uploaded < maxUploadsPerFrame && meshResults.TryDequeue(out var item))
            {
                string key = MeshKey(item.chunk.Coord, item.sectionIndex);

                lock (meshing)
                    meshing.Remove(key);

                result.Add(item);
                uploaded++;
            }

            return result;
        }

        private static string MeshKey(Vector2Int coord, int section)
        {
            return coord.x + "_" + coord.y + "_" + section;
        }
    }
}