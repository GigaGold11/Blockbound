using System.Collections.Generic;
using Blockbound.Blocks;
using Blockbound.Chunks;
using Blockbound.Core;
using UnityEngine;

namespace Blockbound.Meshing
{
    public static class SectionMesher
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

        public static ChunkSectionMeshData BuildSectionMesh(ChunkSnapshot snapshot)
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
                        if (!def.Exists || def.IsAir)
                            continue;

                        if (def.RenderShape == BlockRenderShape.Cross)
                        {
                            byte light = snapshot.GetCombinedLight(sx, sy, sz);
                            AddCross(data, x, y, z, def.SideTextureIndex, light);
                            continue;
                        }

                        for (int face = 0; face < 6; face++)
                        {
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

                            AddFace(
                                snapshot,
                                data,
                                x, y, z,
                                face,
                                GetTextureIndexForFace(def, face),
                                def.RenderType != BlockRenderType.Opaque
                            );
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

        private static void AddFace(
            ChunkSnapshot snapshot,
            ChunkSectionMeshData data,
            int x, int y, int z,
            int face,
            int textureIndex,
            bool cutout)
        {
            var vertices = cutout ? data.CutoutVertices : data.OpaqueVertices;
            var triangles = cutout ? data.CutoutTriangles : data.OpaqueTriangles;
            var uvs = cutout ? data.CutoutUVs : data.OpaqueUVs;
            var uv2s = cutout ? data.CutoutUV2s : data.OpaqueUV2s;
            var colors = cutout ? data.CutoutColors : data.OpaqueColors;
            var normals = cutout ? data.CutoutNormals : data.OpaqueNormals;

            int start = vertices.Count;

            Vector3 v0, v1, v2, v3;

            switch (face)
            {
                case 0:
                    v0 = new Vector3(x, y + 1, z);
                    v1 = new Vector3(x, y + 1, z + 1);
                    v2 = new Vector3(x + 1, y + 1, z + 1);
                    v3 = new Vector3(x + 1, y + 1, z);
                    break;
                case 1:
                    v0 = new Vector3(x, y, z);
                    v1 = new Vector3(x + 1, y, z);
                    v2 = new Vector3(x + 1, y, z + 1);
                    v3 = new Vector3(x, y, z + 1);
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

            vertices.Add(v0);
            vertices.Add(v1);
            vertices.Add(v2);
            vertices.Add(v3);

            triangles.Add(start + 0);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
            triangles.Add(start + 0);
            triangles.Add(start + 2);
            triangles.Add(start + 3);

            uvs.Add(new Vector2(0, 0));
            uvs.Add(new Vector2(0, 1));
            uvs.Add(new Vector2(1, 1));
            uvs.Add(new Vector2(1, 0));

            int safeIndex = textureIndex < 0 ? 0 : textureIndex;
            Vector2 layer = new Vector2(safeIndex, 0);
            uv2s.Add(layer);
            uv2s.Add(layer);
            uv2s.Add(layer);
            uv2s.Add(layer);

            colors.Add(SampleSmoothedVertexLight(snapshot, x, y, z, face, 0));
            colors.Add(SampleSmoothedVertexLight(snapshot, x, y, z, face, 1));
            colors.Add(SampleSmoothedVertexLight(snapshot, x, y, z, face, 2));
            colors.Add(SampleSmoothedVertexLight(snapshot, x, y, z, face, 3));

            Vector3 normal = FaceNormals[face];
            normals.Add(normal);
            normals.Add(normal);
            normals.Add(normal);
            normals.Add(normal);
        }

        private static Color32 SampleSmoothedVertexLight(ChunkSnapshot snapshot, int x, int y, int z, int face, int vertex)
        {
            int sx = x + 1;
            int sy = y + 1;
            int sz = z + 1;

            int sum = 0;
            int count = 0;

            void Add(int px, int py, int pz)
            {
                if (px < 0 || px >= snapshot.SizeX || py < 0 || py >= snapshot.SizeY || pz < 0 || pz >= snapshot.SizeZ)
                    return;

                sum += snapshot.GetCombinedLight(px, py, pz);
                count++;
            }

            switch (face)
            {
                case 0:
                    if (vertex == 0) { Add(sx, sy + 1, sz); Add(sx - 1, sy + 1, sz); Add(sx, sy + 1, sz - 1); Add(sx - 1, sy + 1, sz - 1); }
                    if (vertex == 1) { Add(sx, sy + 1, sz + 1); Add(sx - 1, sy + 1, sz + 1); Add(sx, sy + 1, sz); Add(sx - 1, sy + 1, sz); }
                    if (vertex == 2) { Add(sx + 1, sy + 1, sz + 1); Add(sx, sy + 1, sz + 1); Add(sx + 1, sy + 1, sz); Add(sx, sy + 1, sz); }
                    if (vertex == 3) { Add(sx + 1, sy + 1, sz); Add(sx, sy + 1, sz); Add(sx + 1, sy + 1, sz - 1); Add(sx, sy + 1, sz - 1); }
                    break;

                case 1:
                    if (vertex == 0) { Add(sx, sy - 1, sz); Add(sx - 1, sy - 1, sz); Add(sx, sy - 1, sz - 1); Add(sx - 1, sy - 1, sz - 1); }
                    if (vertex == 1) { Add(sx + 1, sy - 1, sz); Add(sx, sy - 1, sz); Add(sx + 1, sy - 1, sz - 1); Add(sx, sy - 1, sz - 1); }
                    if (vertex == 2) { Add(sx + 1, sy - 1, sz + 1); Add(sx, sy - 1, sz + 1); Add(sx + 1, sy - 1, sz); Add(sx, sy - 1, sz); }
                    if (vertex == 3) { Add(sx, sy - 1, sz + 1); Add(sx - 1, sy - 1, sz + 1); Add(sx, sy - 1, sz); Add(sx - 1, sy - 1, sz); }
                    break;

                case 2:
                    if (vertex == 0) { Add(sx - 1, sy, sz - 1); Add(sx, sy, sz - 1); Add(sx - 1, sy - 1, sz - 1); Add(sx, sy - 1, sz - 1); }
                    if (vertex == 1) { Add(sx - 1, sy + 1, sz - 1); Add(sx, sy + 1, sz - 1); Add(sx - 1, sy, sz - 1); Add(sx, sy, sz - 1); }
                    if (vertex == 2) { Add(sx + 1, sy + 1, sz - 1); Add(sx, sy + 1, sz - 1); Add(sx + 1, sy, sz - 1); Add(sx, sy, sz - 1); }
                    if (vertex == 3) { Add(sx + 1, sy, sz - 1); Add(sx, sy, sz - 1); Add(sx + 1, sy - 1, sz - 1); Add(sx, sy - 1, sz - 1); }
                    break;

                case 3:
                    if (vertex == 0) { Add(sx + 1, sy, sz + 1); Add(sx, sy, sz + 1); Add(sx + 1, sy - 1, sz + 1); Add(sx, sy - 1, sz + 1); }
                    if (vertex == 1) { Add(sx + 1, sy + 1, sz + 1); Add(sx, sy + 1, sz + 1); Add(sx + 1, sy, sz + 1); Add(sx, sy, sz + 1); }
                    if (vertex == 2) { Add(sx - 1, sy + 1, sz + 1); Add(sx, sy + 1, sz + 1); Add(sx - 1, sy, sz + 1); Add(sx, sy, sz + 1); }
                    if (vertex == 3) { Add(sx - 1, sy, sz + 1); Add(sx, sy, sz + 1); Add(sx - 1, sy - 1, sz + 1); Add(sx, sy - 1, sz + 1); }
                    break;

                case 4:
                    if (vertex == 0) { Add(sx - 1, sy, sz + 1); Add(sx - 1, sy, sz); Add(sx - 1, sy - 1, sz + 1); Add(sx - 1, sy - 1, sz); }
                    if (vertex == 1) { Add(sx - 1, sy + 1, sz + 1); Add(sx - 1, sy + 1, sz); Add(sx - 1, sy, sz + 1); Add(sx - 1, sy, sz); }
                    if (vertex == 2) { Add(sx - 1, sy + 1, sz - 1); Add(sx - 1, sy + 1, sz); Add(sx - 1, sy, sz - 1); Add(sx - 1, sy, sz); }
                    if (vertex == 3) { Add(sx - 1, sy, sz - 1); Add(sx - 1, sy, sz); Add(sx - 1, sy - 1, sz - 1); Add(sx - 1, sy - 1, sz); }
                    break;

                default:
                    if (vertex == 0) { Add(sx + 1, sy, sz - 1); Add(sx + 1, sy, sz); Add(sx + 1, sy - 1, sz - 1); Add(sx + 1, sy - 1, sz); }
                    if (vertex == 1) { Add(sx + 1, sy + 1, sz - 1); Add(sx + 1, sy + 1, sz); Add(sx + 1, sy, sz - 1); Add(sx + 1, sy, sz); }
                    if (vertex == 2) { Add(sx + 1, sy + 1, sz + 1); Add(sx + 1, sy + 1, sz); Add(sx + 1, sy, sz + 1); Add(sx + 1, sy, sz); }
                    if (vertex == 3) { Add(sx + 1, sy, sz + 1); Add(sx + 1, sy, sz); Add(sx + 1, sy - 1, sz + 1); Add(sx + 1, sy - 1, sz); }
                    break;
            }

            float avg = count > 0 ? (sum / (float)count) / VoxelConstants.MaxLight : 0f;
            float final = Mathf.Clamp01(0.18f + avg * 0.82f);

            byte v = (byte)Mathf.RoundToInt(final * 255f);
            return new Color32(v, v, v, 255);
        }

        private static void AddCross(ChunkSectionMeshData data, int x, int y, int z, int textureIndex, byte light)
        {
            int safe = textureIndex < 0 ? 0 : textureIndex;
            Vector2 layer = new Vector2(safe, 0);

            float lightFactor = 0.18f + (light / (float)VoxelConstants.MaxLight) * 0.82f;
            byte v = (byte)Mathf.RoundToInt(Mathf.Clamp01(lightFactor) * 255f);
            Color32 c = new Color32(v, v, v, 255);

            AddQuad(
                data.CutoutVertices,
                data.CutoutTriangles,
                data.CutoutUVs,
                data.CutoutUV2s,
                data.CutoutColors,
                data.CutoutNormals,
                new Vector3(x, y, z),
                new Vector3(x + 1, y, z + 1),
                new Vector3(x + 1, y + 1, z + 1),
                new Vector3(x, y + 1, z),
                layer,
                c
            );

            AddQuad(
                data.CutoutVertices,
                data.CutoutTriangles,
                data.CutoutUVs,
                data.CutoutUV2s,
                data.CutoutColors,
                data.CutoutNormals,
                new Vector3(x + 1, y, z),
                new Vector3(x, y, z + 1),
                new Vector3(x, y + 1, z + 1),
                new Vector3(x + 1, y + 1, z),
                layer,
                c
            );
        }

        private static void AddQuad(
            List<Vector3> verts,
            List<int> tris,
            List<Vector2> uvs,
            List<Vector2> uv2s,
            List<Color32> cols,
            List<Vector3> normals,
            Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3,
            Vector2 layer,
            Color32 c)
        {
            int start = verts.Count;

            verts.Add(v0);
            verts.Add(v1);
            verts.Add(v2);
            verts.Add(v3);

            tris.Add(start + 0);
            tris.Add(start + 2);
            tris.Add(start + 1);
            tris.Add(start + 0);
            tris.Add(start + 3);
            tris.Add(start + 2);

            uvs.Add(new Vector2(0, 0));
            uvs.Add(new Vector2(1, 0));
            uvs.Add(new Vector2(1, 1));
            uvs.Add(new Vector2(0, 1));

            uv2s.Add(layer);
            uv2s.Add(layer);
            uv2s.Add(layer);
            uv2s.Add(layer);

            cols.Add(c);
            cols.Add(c);
            cols.Add(c);
            cols.Add(c);

            Vector3 normal = Vector3.up;
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