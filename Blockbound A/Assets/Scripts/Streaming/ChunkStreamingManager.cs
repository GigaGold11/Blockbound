using System.Collections.Generic;
using Blockbound.Chunks;
using Blockbound.Core;
using Blockbound.Rendering;
using Blockbound.Streaming;
using Blockbound.Meshing;
using Blockbound.Generation;
using Blockbound.Threading;
using UnityEngine;
using UnityEngine.Rendering;

namespace Blockbound.World
{
    public class ChunkStreamingManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private VoxelWorld voxelWorld;
        [SerializeField] private Material opaqueChunkMaterial;
        [SerializeField] private Material cutoutChunkMaterial;
        [SerializeField] private Material farTerrainMaterial;
        [SerializeField] private Transform playerTransform;
        [SerializeField] private ThreadedChunkPipeline chunkPipeline;
        [SerializeField] private Camera renderCamera;

        [Header("Distances")]
        [SerializeField] private int closeVoxelRadius = 8;
        [SerializeField] private int midSurfaceRadius = 16;
        [SerializeField] private int farHeightmeshRadius = 32;
        [SerializeField] private int unloadRadius = 36;

        [Header("Budgets")]
        [SerializeField] private int chunkRequestBudgetPerFrame = 32;
        [SerializeField] private int lightingBudgetPerFrame = 12;
        [SerializeField] private int meshingBudgetPerFrame = 24;

        [Header("Rendering")]
        [SerializeField] private int colliderRadius = 6;
        [SerializeField] private bool disableShadowsOnDistantChunks = true;
        [SerializeField] private float fogStartDistance = 180f;
        [SerializeField] private float fogEndDistance = 700f;
        [SerializeField] private Color fogColor = new Color(0.72f, 0.82f, 0.92f, 1f);

        private readonly Dictionary<Vector2Int, ChunkRenderer> renderers = new Dictionary<Vector2Int, ChunkRenderer>();
        private readonly Dictionary<Vector2Int, ChunkRenderLOD> lodStates = new Dictionary<Vector2Int, ChunkRenderLOD>();
        private readonly List<Vector2Int> sortedCoords = new List<Vector2Int>();

        private FarTerrainRenderer farTerrainRenderer;
        private Vector2Int lastPlayerChunk;
        private Plane[] frustumPlanes = new Plane[6];

        private void Start()
        {
            if (renderCamera == null)
                renderCamera = Camera.main;

            if (playerTransform == null && Camera.main != null)
                playerTransform = Camera.main.transform;

            SetupCameraDistanceAndFog();
            SetupFarTerrainRenderer();

            lastPlayerChunk = GetPlayerChunkCoord();
            RefreshWantedChunks();
        }

        private void Update()
        {
            Vector2Int current = GetPlayerChunkCoord();
            if (current != lastPlayerChunk)
            {
                lastPlayerChunk = current;
                RefreshWantedChunks();
            }

            ConsumePipelineResults();
            RequeueDirtyChunks();
            UpdateChunkLODsAndVisibility();
        }

        private void SetupCameraDistanceAndFog()
        {
            if (renderCamera != null)
                renderCamera.farClipPlane = Mathf.Max(renderCamera.farClipPlane, fogEndDistance + 300f);

            RenderSettings.fog = true;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = fogStartDistance;
            RenderSettings.fogEndDistance = fogEndDistance;
        }

        private void SetupFarTerrainRenderer()
        {
            GameObject go = new GameObject("FarTerrainRenderer");
            go.transform.SetParent(transform, false);
            farTerrainRenderer = go.AddComponent<FarTerrainRenderer>();
            farTerrainRenderer.Initialize(farTerrainMaterial, playerTransform);
        }

        private Vector2Int GetPlayerChunkCoord()
        {
            int wx = Mathf.FloorToInt(playerTransform.position.x);
            int wz = Mathf.FloorToInt(playerTransform.position.z);
            return VoxelMath.WorldToChunkCoord(wx, wz);
        }

        private void RefreshWantedChunks()
        {
            Vector2Int center = lastPlayerChunk;
            Vector3 camForward = renderCamera != null ? renderCamera.transform.forward : Vector3.forward;

            sortedCoords.Clear();

            for (int cz = center.y - midSurfaceRadius; cz <= center.y + midSurfaceRadius; cz++)
            {
                for (int cx = center.x - midSurfaceRadius; cx <= center.x + midSurfaceRadius; cx++)
                    sortedCoords.Add(new Vector2Int(cx, cz));
            }

            Vector3 playerWorldPos = playerTransform.position;
            sortedCoords.Sort((a, b) =>
            {
                float pa = ChunkPriorityUtility.ComputePriority(playerWorldPos, camForward, a, VoxelConstants.ChunkSize);
                float pb = ChunkPriorityUtility.ComputePriority(playerWorldPos, camForward, b, VoxelConstants.ChunkSize);
                return pa.CompareTo(pb);
            });

            int requested = 0;

            for (int i = 0; i < sortedCoords.Count; i++)
            {
                Vector2Int coord = sortedCoords[i];
                Chunk chunk = voxelWorld.GetOrCreateChunk(coord);

                if (!renderers.ContainsKey(coord))
                {
                    GameObject go = new GameObject($"Chunk_{coord.x}_{coord.y}");
                    go.transform.position = VoxelMath.ChunkToWorldPosition(coord);

                    ChunkRenderer renderer = go.AddComponent<ChunkRenderer>();
                    renderer.Initialize(opaqueChunkMaterial, cutoutChunkMaterial);
                    renderers.Add(coord, renderer);
                }

                if (chunk.LoadState == ChunkLoadState.Unloaded && requested < chunkRequestBudgetPerFrame)
                {
                    chunk.LoadState = ChunkLoadState.QueuedForGeneration;
                    chunkPipeline.RequestGeneration(chunk);
                    requested++;
                }
            }

            List<Vector2Int> toRemove = new List<Vector2Int>();
            foreach (var kvp in voxelWorld.Chunks)
            {
                Vector2Int coord = kvp.Key;
                int dx = Mathf.Abs(coord.x - center.x);
                int dz = Mathf.Abs(coord.y - center.y);

                if (dx > unloadRadius || dz > unloadRadius)
                    toRemove.Add(coord);
            }

            for (int i = 0; i < toRemove.Count; i++)
            {
                Vector2Int coord = toRemove[i];

                if (renderers.TryGetValue(coord, out var renderer))
                {
                    Destroy(renderer.gameObject);
                    renderers.Remove(coord);
                }

                lodStates.Remove(coord);
                voxelWorld.RemoveChunk(coord);
            }
        }

        private void ConsumePipelineResults()
        {
            List<Chunk> generated = chunkPipeline.ConsumeGenerated();
            int litRequests = 0;

            for (int i = 0; i < generated.Count; i++)
            {
                Chunk chunk = generated[i];
                chunk.LoadState = ChunkLoadState.Generated;

                if (litRequests < lightingBudgetPerFrame)
                {
                    chunkPipeline.RequestLighting(chunk);
                    chunk.LoadState = ChunkLoadState.QueuedForLighting;
                    litRequests++;
                }
            }

            List<Chunk> lit = chunkPipeline.ConsumeLit();
            int meshed = 0;

            for (int i = 0; i < lit.Count; i++)
            {
                Chunk chunk = lit[i];
                chunk.LoadState = ChunkLoadState.Lit;

                ChunkRenderLOD lod = GetLODForCoord(chunk.Coord);
                if (lod == ChunkRenderLOD.Hidden || lod == ChunkRenderLOD.FarHeightmesh)
                    continue;

                for (int s = 0; s < VoxelConstants.SectionsPerChunk; s++)
                {
                    if (!chunk.Sections[s].HasNonAir)
                        continue;

                    if (meshed >= meshingBudgetPerFrame)
                        break;

                    chunkPipeline.RequestMeshing(chunk, s);
                    meshed++;
                }

                chunk.LoadState = ChunkLoadState.QueuedForMeshing;
            }

            var meshes = chunkPipeline.ConsumeMeshes();
            for (int i = 0; i < meshes.Count; i++)
            {
                var item = meshes[i];
                item.chunk.Sections[item.sectionIndex].MeshData = item.meshData;

                if (renderers.TryGetValue(item.chunk.Coord, out var renderer))
                    renderer.SetSectionMesh(item.sectionIndex, item.meshData);

                item.chunk.LoadState = ChunkLoadState.Meshed;
                item.chunk.IsDirty = false;
            }
        }

        private void RequeueDirtyChunks()
        {
            int relit = 0;

            foreach (var kvp in voxelWorld.Chunks)
            {
                if (relit >= lightingBudgetPerFrame)
                    break;

                Chunk chunk = kvp.Value;

                if (!chunk.IsDirty && !chunk.LightDirty)
                    continue;

                ChunkRenderLOD lod = GetLODForCoord(chunk.Coord);
                if (lod == ChunkRenderLOD.Hidden || lod == ChunkRenderLOD.FarHeightmesh)
                    continue;

                if (chunk.LoadState == ChunkLoadState.Meshed || chunk.LoadState == ChunkLoadState.Lit)
                {
                    chunkPipeline.RequestLighting(chunk);
                    chunk.LoadState = ChunkLoadState.QueuedForLighting;
                    relit++;
                }
            }
        }

        private void UpdateChunkLODsAndVisibility()
        {
            if (renderCamera == null)
                return;

            frustumPlanes = GeometryUtility.CalculateFrustumPlanes(renderCamera);
            Vector2Int center = lastPlayerChunk;

            foreach (var kvp in renderers)
            {
                Vector2Int coord = kvp.Key;
                ChunkRenderer renderer = kvp.Value;

                if (!voxelWorld.TryGetChunk(coord, out var chunk))
                    continue;

                ChunkRenderLOD lod = GetLODForCoord(coord);
                lodStates[coord] = lod;

                bool close = lod == ChunkRenderLOD.CloseVoxel;
                bool mid = lod == ChunkRenderLOD.MidSurface;
                bool hidden = lod == ChunkRenderLOD.Hidden || lod == ChunkRenderLOD.FarHeightmesh;

                int dx = Mathf.Abs(coord.x - center.x);
                int dz = Mathf.Abs(coord.y - center.y);
                bool enableCollider = dx <= colliderRadius && dz <= colliderRadius;

                renderer.SetCollisionEnabled(enableCollider);
                renderer.SetShadowMode(GetShadowModeForLOD(lod), lod == ChunkRenderLOD.CloseVoxel);

                for (int s = 0; s < VoxelConstants.SectionsPerChunk; s++)
                {
                    bool visible = false;

                    if (!hidden && chunk.Sections[s].HasNonAir)
                    {
                        Vector3 sectionWorldPos = new Vector3(
                            coord.x * VoxelConstants.ChunkSize,
                            s * VoxelConstants.SectionHeight,
                            coord.y * VoxelConstants.ChunkSize
                        );

                        Bounds bounds = new Bounds(
                            sectionWorldPos + new Vector3(8f, 8f, 8f),
                            new Vector3(VoxelConstants.ChunkSize, VoxelConstants.SectionHeight, VoxelConstants.ChunkSize)
                        );

                        visible = GeometryUtility.TestPlanesAABB(frustumPlanes, bounds);
                    }

                    renderer.SetSectionVisible(s, visible);
                }

                if (mid && chunk.LoadState == ChunkLoadState.Lit)
                {
                    for (int s = 0; s < VoxelConstants.SectionsPerChunk; s++)
                    {
                        if (!chunk.Sections[s].HasNonAir)
                            continue;

                        ChunkSnapshot snapshot = ChunkSnapshotBuilder.BuildSectionSnapshot(voxelWorld, chunk, s);
                        ChunkSectionMeshData meshData = MidSurfaceMesher.BuildMidSurfaceMesh(snapshot);
                        renderer.SetSectionMesh(s, meshData);
                    }

                    chunk.LoadState = ChunkLoadState.Meshed;
                }
            }
        }

        private ChunkRenderLOD GetLODForCoord(Vector2Int coord)
        {
            int dx = Mathf.Abs(coord.x - lastPlayerChunk.x);
            int dz = Mathf.Abs(coord.y - lastPlayerChunk.y);
            int dist = Mathf.Max(dx, dz);

            if (dist <= closeVoxelRadius)
                return ChunkRenderLOD.CloseVoxel;

            if (dist <= midSurfaceRadius)
                return ChunkRenderLOD.MidSurface;

            if (dist <= farHeightmeshRadius)
                return ChunkRenderLOD.FarHeightmesh;

            return ChunkRenderLOD.Hidden;
        }

        private ShadowCastingMode GetShadowModeForLOD(ChunkRenderLOD lod)
        {
            if (!disableShadowsOnDistantChunks)
                return ShadowCastingMode.On;

            switch (lod)
            {
                case ChunkRenderLOD.CloseVoxel:
                    return ShadowCastingMode.On;
                case ChunkRenderLOD.MidSurface:
                    return ShadowCastingMode.Off;
                case ChunkRenderLOD.FarHeightmesh:
                case ChunkRenderLOD.Hidden:
                default:
                    return ShadowCastingMode.Off;
            }
        }
    }
}