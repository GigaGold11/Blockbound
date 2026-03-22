using Blockbound.Blocks;
using Blockbound.Chunks;
using Blockbound.Core;
using UnityEngine;

namespace Blockbound.Meshing
{
    public static class MidSurfaceMesher
    {
        private static readonly Vector3Int[] NeighborDirs =
        {
            new Vector3Int(0, 1, 0),
            new Vector3Int(0, -1, 0),
            new Vector3Int(0, 0, -1),
            new Vector3Int(0, 0, 1),
            new Vector3Int(-1, 0, 0),
            new Vector3Int(1, 0, 0)
        };

        private static readonly Vector3[] FaceNormals =
        {
            Vector3.up,
            Vector3.down,
            Vector3.back,
            Vector3.forward,
            Vector3.left,
            Vector3.right
        };

        public static ChunkSectionMeshData BuildMidSurfaceMesh(ChunkSnapshot snapshot)
        {
            ChunkSectionMeshData data = new ChunkSectionMeshData();
            BlockRegistry registry = BlockRegistry.Instance;

            int size = VoxelConstants.ChunkSize;
            int height = VoxelConstants.SectionHeight;

            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < size; z++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        int sx = x + 1;
                        int sy = y + 1;
                        int sz = z + 1;

                        ushort blockId = snapshot.GetBlock(sx, sy, sz);
                        if (blockId == 0)
                            continue;

                        BlockRuntimeInfo def = registry.GetRuntimeInfo(blockId);
                        if (!def.Exists || def.IsAir || def.RenderShape == BlockRenderShape.Cross)
                            continue;

                        bool topVisible = snapshot.GetBlock(sx, sy + 1, sz) == 0;
                        bool edgeBlock = x == 0 || x == size - 1 || z == 0 || z == size - 1;

                        if (!topVisible && !edgeBlock)
                            continue;

                        for (int face = 0; face < 6; face++)
                        {
                            if (face != 0 && !edgeBlock)
                                continue;

                            if (face == 1)
                                continue;

                            Vector3Int dir = NeighborDirs[face];
                            int nx = sx + dir.x;
                            int ny = sy + dir.y;
                            int nz = sz + dir.z;

                            if (nx < 0 || nx >= snapshot.SizeX || ny < 0 || ny >= snapshot.SizeY || nz < 0 || nz >= snapshot.SizeZ)
                                continue;

                            ushort neighborId = snapshot.GetBlock(nx, ny, nz);
                            BlockRuntimeInfo neighbor = registry.GetRuntimeInfo(neighborId);

                            if (ShouldHideFace(def, neighbor, neighborId, blockId))
                                continue;

                            AddFace(data, x, y, z, face, GetTextureIndexForFace(def, face), snapshot.GetCombinedLight(sx, sy, sz));
                        }
                    }
                }
            }

            return data;
        }

        private static bool ShouldHideFace(BlockRuntimeInfo self, BlockRuntimeInfo neighbor, ushort neighborId, ushort selfId)
        {
            if (!neighbor.Exists || neighbor.IsAir)
                return false;

            if (neighbor.RenderShape == BlockRenderShape.Cross)
                return false;

            if (self.RenderType == BlockRenderType.Opaque)
                return neighbor.IsOpaque;

            return neighborId == selfId && neighbor.RenderShape == self.RenderShape;
        }

        private static void AddFace(ChunkSectionMeshData data, int x, int y, int z, int face, int textureIndex, byte light)
        {
            var verts = data.OpaqueVertices;
            var tris = data.OpaqueTriangles;
            var uvs = data.OpaqueUVs;
            var uv2s = data.OpaqueUV2s;
            var colors = data.OpaqueColors;
            var normals = data.OpaqueNormals;

            int start = verts.Count;

            Vector3 v0, v1, v2, v3;

            switch (face)
            {
                case 0:
                    v0 = new Vector3(x, y + 1, z);
                    v1 = new Vector3(x, y + 1, z + 1);
                    v2 = new Vector3(x + 1, y + 1, z + 1);
                    v3 = new Vector3(x + 1, y + 1, z);
                    break;
                case 2:
                    v0 = new Vector3(x, y, z);
                    v1 = new Vector3(x, y + 1, z);
                    v2 = new Vector3(x + 1, y + 1, z);
                    v3 = new Vector3(x + 1, y, z);
                    break;
                case 3:
                    v0 = new Vector3(x + 1, y, z + 1);
                    v1 = new Vector3(x + 1, y + 1, z + 1);
                    v2 = new Vector3(x, y + 1, z + 1);
                    v3 = new Vector3(x, y, z + 1);
                    break;
                case 4:
                    v0 = new Vector3(x, y, z + 1);
                    v1 = new Vector3(x, y + 1, z + 1);
                    v2 = new Vector3(x, y + 1, z);
                    v3 = new Vector3(x, y, z);
                    break;
                default:
                    v0 = new Vector3(x + 1, y, z);
                    v1 = new Vector3(x + 1, y + 1, z);
                    v2 = new Vector3(x + 1, y + 1, z + 1);
                    v3 = new Vector3(x + 1, y, z + 1);
                    break;
            }

            verts.Add(v0);
            verts.Add(v1);
            verts.Add(v2);
            verts.Add(v3);

            tris.Add(start);
            tris.Add(start + 1);
            tris.Add(start + 2);
            tris.Add(start);
            tris.Add(start + 2);
            tris.Add(start + 3);

            uvs.Add(new Vector2(0, 0));
            uvs.Add(new Vector2(0, 1));
            uvs.Add(new Vector2(1, 1));
            uvs.Add(new Vector2(1, 0));

            int safe = textureIndex < 0 ? 0 : textureIndex;
            Vector2 layer = new Vector2(safe, 0);
            uv2s.Add(layer);
            uv2s.Add(layer);
            uv2s.Add(layer);
            uv2s.Add(layer);

            float f = 0.25f + (light / (float)VoxelConstants.MaxLight) * 0.75f;
            byte v = (byte)Mathf.RoundToInt(Mathf.Clamp01(f) * 255f);
            Color32 c = new Color32(v, v, v, 255);

            colors.Add(c);
            colors.Add(c);
            colors.Add(c);
            colors.Add(c);

            Vector3 normal = FaceNormals[face];
            normals.Add(normal);
            normals.Add(normal);
            normals.Add(normal);
            normals.Add(normal);
        }

        private static int GetTextureIndexForFace(BlockRuntimeInfo def, int face)
        {
            switch (face)
            {
                case 0: return def.TopTextureIndex;
                case 1: return def.BottomTextureIndex;
                default: return def.SideTextureIndex;
            }
        }
    }
}