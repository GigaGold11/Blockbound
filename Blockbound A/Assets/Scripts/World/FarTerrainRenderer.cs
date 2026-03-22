using System.Collections.Generic;
using Blockbound.Core;
using Blockbound.Generation;
using UnityEngine;
using UnityEngine.Rendering;
namespace Blockbound.World
{
    public class FarTerrainRenderer : MonoBehaviour
    {
        [SerializeField] private Material farTerrainMaterial;
        [SerializeField] private Transform playerTransform;
        [SerializeField] private int regionSizeInChunks = 4;
        [SerializeField] private int renderDistanceInRegions = 6;

        private readonly Dictionary<Vector2Int, GameObject> activeRegions = new Dictionary<Vector2Int, GameObject>();

        public void Initialize(Material material, Transform player)
        {
            farTerrainMaterial = material;
            playerTransform = player;
        }

        private void Update()
        {
            if (playerTransform == null || farTerrainMaterial == null) return;

            Vector2Int playerChunk = VoxelMath.WorldToChunkCoord(
                Mathf.FloorToInt(playerTransform.position.x),
                Mathf.FloorToInt(playerTransform.position.z));

            Vector2Int playerRegion = new Vector2Int(
                Mathf.FloorToInt((float)playerChunk.x / regionSizeInChunks),
                Mathf.FloorToInt((float)playerChunk.y / regionSizeInChunks));

            HashSet<Vector2Int> wanted = new HashSet<Vector2Int>();

            for (int rz = playerRegion.y - renderDistanceInRegions; rz <= playerRegion.y + renderDistanceInRegions; rz++)
            {
                for (int rx = playerRegion.x - renderDistanceInRegions; rx <= playerRegion.x + renderDistanceInRegions; rx++)
                {
                    Vector2Int region = new Vector2Int(rx, rz);
                    wanted.Add(region);

                    if (!activeRegions.ContainsKey(region))
                        activeRegions[region] = CreateBlockyRegion(region);
                }
            }

            List<Vector2Int> toRemove = new List<Vector2Int>();
            foreach (var kvp in activeRegions)
                if (!wanted.Contains(kvp.Key)) toRemove.Add(kvp.Key);

            foreach (var r in toRemove)
            {
                Destroy(activeRegions[r]);
                activeRegions.Remove(r);
            }
        }

        private GameObject CreateBlockyRegion(Vector2Int region)
        {
            GameObject go = new GameObject($"FarRegion_{region.x}_{region.y}");
            go.transform.SetParent(transform, false);

            MeshFilter mf = go.AddComponent<MeshFilter>();
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = farTerrainMaterial;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;

            mf.sharedMesh = BuildBlockyMesh(region);
            return go;
        }

        private Mesh BuildBlockyMesh(Vector2Int region)
        {
            int chunkSize = VoxelConstants.ChunkSize;
            int regionBlocks = regionSizeInChunks * chunkSize;

            int startX = region.x * regionBlocks;
            int startZ = region.y * regionBlocks;

            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();
            List<Vector2> uvs = new List<Vector2>();
            List<Color32> colors = new List<Color32>();

            for (int x = 0; x < regionBlocks; x += 2)
            {
                for (int z = 0; z < regionBlocks; z += 2)
                {
                    float wx = startX + x;
                    float wz = startZ + z;
                    float height = SampleHeight(wx, wz);

                    int baseIndex = verts.Count;

                    verts.Add(new Vector3(wx, height, wz));
                    verts.Add(new Vector3(wx + 2, height, wz));
                    verts.Add(new Vector3(wx + 2, height, wz + 2));
                    verts.Add(new Vector3(wx, height, wz + 2));

                    tris.Add(baseIndex); tris.Add(baseIndex + 2); tris.Add(baseIndex + 1);
                    tris.Add(baseIndex); tris.Add(baseIndex + 3); tris.Add(baseIndex + 2);

                    Color32 col = SampleBiomeColor(wx, wz);
                    colors.Add(col); colors.Add(col); colors.Add(col); colors.Add(col);

                    uvs.Add(new Vector2(0, 0));
                    uvs.Add(new Vector2(1, 0));
                    uvs.Add(new Vector2(1, 1));
                    uvs.Add(new Vector2(0, 1));
                }
            }

            Mesh mesh = new Mesh();
            mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private float SampleHeight(float wx, float wz)
        {
            return SeededTerrainGenerator.GetHeightAt(wx, wz); // We'll add this helper next
        }

        private Color32 SampleBiomeColor(float wx, float wz)
        {
            // Very simple biome color for distance
            float mountain = RidgedFbm2D(wx, wz, 91000, 1f/1800f, 5);
            if (mountain > 0.65f) return new Color32(130, 130, 130, 255);
            if (Fbm2D(wx, wz, 60000, 1f/420f, 4) < 0.3f) return new Color32(194, 178, 128, 255); // sand
            return new Color32(90, 160, 70, 255);
        }

        private float Fbm2D(float x, float z, int seed, float freq, int octaves)
        {
            float sum = 0, amp = 1, f = freq, norm = 0;
            for (int i = 0; i < octaves; i++)
            {
                sum += ValueNoise(x * f, z * f, seed + i);
                norm += amp;
                amp *= 0.5f;
                f *= 2f;
            }
            return sum / norm;
        }

        private float RidgedFbm2D(float x, float z, int seed, float freq, int octaves)
        {
            float sum = 0, amp = 1, f = freq, norm = 0;
            for (int i = 0; i < octaves; i++)
            {
                float v = ValueNoise(x * f, z * f, seed + i);
                v = Mathf.Abs(v * 2 - 1);
                sum += (1f - v) * (1f - v) * amp;
                norm += amp;
                amp *= 0.5f;
                f *= 2f;
            }
            return sum / norm;
        }

        private float ValueNoise(float x, float z, int seed)
        {
            int ix = Mathf.FloorToInt(x);
            int iz = Mathf.FloorToInt(z);
            float tx = x - ix;
            float tz = z - iz;

            float a = Hash(ix, iz, seed);
            float b = Hash(ix + 1, iz, seed);
            float c = Hash(ix, iz + 1, seed);
            float d = Hash(ix + 1, iz + 1, seed);

            tx = tx * tx * (3f - 2f * tx);
            tz = tz * tz * (3f - 2f * tz);

            return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), tz);
        }

        private float Hash(int x, int z, int seed)
        {
            int n = x * 374761393 + z * 668265263 + seed;
            n = (n ^ (n >> 13)) * 1274126177;
            return (n ^ (n >> 16)) * 0.0000000004656612873077392578125f; // normalized to 0-1
        }
    }
}