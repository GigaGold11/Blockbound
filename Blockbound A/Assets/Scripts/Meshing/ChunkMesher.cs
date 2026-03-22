using Blockbound.Blocks;
using Blockbound.Chunks;
using Blockbound.Core;
using Blockbound.World;
using UnityEngine;

namespace Blockbound.Meshing
{
    public static class ChunkMesher
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

        public static ChunkMeshData BuildMesh(VoxelWorld world, Chunk chunk)
        {
            if (BlockRegistry.Instance == null)
            {
                Debug.LogError("ChunkMesher: BlockRegistry.Instance is null.");
                return new ChunkMeshData
                {
                    OpaqueMesh = new Mesh(),
                    CutoutMesh = new Mesh()
                };
            }

            MeshBuffers opaque = new MeshBuffers();
            MeshBuffers cutout = new MeshBuffers();

            for (int sectionIndex = 0; sectionIndex < VoxelConstants.SectionsPerChunk; sectionIndex++)
            {
                ChunkSection section = chunk.Sections[sectionIndex];
                if (!section.HasNonAir)
                    continue;

                int baseY = sectionIndex * VoxelConstants.SectionHeight;

                for (int localY = 0; localY < VoxelConstants.SectionHeight; localY++)
                {
                    int y = baseY + localY;
                    if (y >= VoxelConstants.WorldHeight)
                        break;

                    for (int z = 0; z < VoxelConstants.ChunkSize; z++)
                    {
                        for (int x = 0; x < VoxelConstants.ChunkSize; x++)
                        {
                            BlockData block = chunk.GetBlockLocal(x, y, z);
                            if (block.Id == 0)
                                continue;

                            BlockDefinition def = BlockRegistry.Instance.Get(block.Id);
                            if (def == null || def.IsAir)
                                continue;

                            int worldX = chunk.Coord.x * VoxelConstants.ChunkSize + x;
                            int worldZ = chunk.Coord.y * VoxelConstants.ChunkSize + z;

                            if (def.RenderShape == BlockRenderShape.Cross)
                            {
                                int textureIndex = def.SideTextureIndex;
                                byte light = world.GetCombinedLight(worldX, y, worldZ);
                                AddCross(cutout, x, y, z, textureIndex, light);
                                continue;
                            }

                            MeshBuffers target = def.RenderType == BlockRenderType.Cutout ? cutout : opaque;

                            for (int face = 0; face < 6; face++)
                            {
                                Vector3Int dir = NeighborDirs[face];
                                BlockData neighbor = world.GetBlock(worldX + dir.x, y + dir.y, worldZ + dir.z);
                                BlockDefinition neighborDef = BlockRegistry.Instance.Get(neighbor.Id);

                                bool hidden = false;

                                if (def.RenderType == BlockRenderType.Opaque)
                                    hidden = neighborDef != null && neighborDef.IsOpaque;
                                else
                                    hidden = neighborDef != null && neighborDef.Id == def.Id;

                                if (hidden)
                                    continue;

                                int textureIndex = GetTextureIndexForFace(def, face);
                                byte light = world.GetCombinedLight(worldX + dir.x, y + dir.y, worldZ + dir.z);
                                byte[] ao = VoxelAO.GetFaceAO(world, worldX, y, worldZ, face);

                                AddFace(target, x, y, z, face, textureIndex, light, ao);
                            }
                        }
                    }
                }
            }

            chunk.ClearMeshDirtyFlags();
            chunk.IsDirty = false;

            return new ChunkMeshData
            {
                OpaqueMesh = BuildUnityMesh(opaque),
                CutoutMesh = BuildUnityMesh(cutout)
            };
        }

        private static int GetTextureIndexForFace(BlockDefinition def, int face)
        {
            switch (face)
            {
                case 0: return def.TopTextureIndex;
                case 1: return def.BottomTextureIndex;
                default: return def.SideTextureIndex;
            }
        }

        private static Mesh BuildUnityMesh(MeshBuffers buffers)
        {
            Mesh mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(buffers.Vertices);
            mesh.SetTriangles(buffers.Triangles, 0);
            mesh.SetUVs(0, buffers.UVs);
            mesh.SetUVs(1, buffers.UV2s);
            mesh.SetColors(buffers.Colors);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddFace(MeshBuffers buffers, int x, int y, int z, int face, int textureIndex, byte light, byte[] ao)
        {
            int start = buffers.Vertices.Count;

            switch (face)
            {
                case 0:
                    buffers.Vertices.Add(new Vector3(x,     y + 1, z));
                    buffers.Vertices.Add(new Vector3(x,     y + 1, z + 1));
                    buffers.Vertices.Add(new Vector3(x + 1, y + 1, z + 1));
                    buffers.Vertices.Add(new Vector3(x + 1, y + 1, z));
                    break;
                case 1:
                    buffers.Vertices.Add(new Vector3(x,     y, z));
                    buffers.Vertices.Add(new Vector3(x + 1, y, z));
                    buffers.Vertices.Add(new Vector3(x + 1, y, z + 1));
                    buffers.Vertices.Add(new Vector3(x,     y, z + 1));
                    break;
                case 2:
                    buffers.Vertices.Add(new Vector3(x,     y,     z));
                    buffers.Vertices.Add(new Vector3(x,     y + 1, z));
                    buffers.Vertices.Add(new Vector3(x + 1, y + 1, z));
                    buffers.Vertices.Add(new Vector3(x + 1, y,     z));
                    break;
                case 3:
                    buffers.Vertices.Add(new Vector3(x + 1, y,     z + 1));
                    buffers.Vertices.Add(new Vector3(x + 1, y + 1, z + 1));
                    buffers.Vertices.Add(new Vector3(x,     y + 1, z + 1));
                    buffers.Vertices.Add(new Vector3(x,     y,     z + 1));
                    break;
                case 4:
                    buffers.Vertices.Add(new Vector3(x, y,     z + 1));
                    buffers.Vertices.Add(new Vector3(x, y + 1, z + 1));
                    buffers.Vertices.Add(new Vector3(x, y + 1, z));
                    buffers.Vertices.Add(new Vector3(x, y,     z));
                    break;
                case 5:
                    buffers.Vertices.Add(new Vector3(x + 1, y,     z));
                    buffers.Vertices.Add(new Vector3(x + 1, y + 1, z));
                    buffers.Vertices.Add(new Vector3(x + 1, y + 1, z + 1));
                    buffers.Vertices.Add(new Vector3(x + 1, y,     z + 1));
                    break;
            }

            buffers.Triangles.Add(start + 0);
            buffers.Triangles.Add(start + 1);
            buffers.Triangles.Add(start + 2);
            buffers.Triangles.Add(start + 0);
            buffers.Triangles.Add(start + 2);
            buffers.Triangles.Add(start + 3);

            buffers.UVs.Add(new Vector2(0, 0));
            buffers.UVs.Add(new Vector2(0, 1));
            buffers.UVs.Add(new Vector2(1, 1));
            buffers.UVs.Add(new Vector2(1, 0));

            int safeIndex = textureIndex < 0 ? 0 : textureIndex;
            Vector2 layer = new Vector2(safeIndex, 0);
            buffers.UV2s.Add(layer);
            buffers.UV2s.Add(layer);
            buffers.UV2s.Add(layer);
            buffers.UV2s.Add(layer);

            buffers.Colors.Add(ApplyLightAndAO(light, ao[0]));
            buffers.Colors.Add(ApplyLightAndAO(light, ao[1]));
            buffers.Colors.Add(ApplyLightAndAO(light, ao[2]));
            buffers.Colors.Add(ApplyLightAndAO(light, ao[3]));
        }

        private static void AddCross(MeshBuffers buffers, int x, int y, int z, int textureIndex, byte light)
        {
            int safeIndex = textureIndex < 0 ? 0 : textureIndex;
            Vector2 layer = new Vector2(safeIndex, 0);
            Color32 c = ApplyLightAndAO(light, 0);

            AddQuad(
                buffers,
                new Vector3(x + 0f, y + 0f, z + 0f),
                new Vector3(x + 1f, y + 0f, z + 1f),
                new Vector3(x + 1f, y + 1f, z + 1f),
                new Vector3(x + 0f, y + 1f, z + 0f),
                layer,
                c
            );

            AddQuad(
                buffers,
                new Vector3(x + 1f, y + 0f, z + 0f),
                new Vector3(x + 0f, y + 0f, z + 1f),
                new Vector3(x + 0f, y + 1f, z + 1f),
                new Vector3(x + 1f, y + 1f, z + 0f),
                layer,
                c
            );
        }

        private static void AddQuad(MeshBuffers buffers, Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, Vector2 layer, Color32 c)
        {
            int start = buffers.Vertices.Count;

            buffers.Vertices.Add(v0);
            buffers.Vertices.Add(v1);
            buffers.Vertices.Add(v2);
            buffers.Vertices.Add(v3);

            buffers.Triangles.Add(start + 0);
            buffers.Triangles.Add(start + 2);
            buffers.Triangles.Add(start + 1);

            buffers.Triangles.Add(start + 0);
            buffers.Triangles.Add(start + 3);
            buffers.Triangles.Add(start + 2);

            buffers.UVs.Add(new Vector2(0, 0));
            buffers.UVs.Add(new Vector2(1, 0));
            buffers.UVs.Add(new Vector2(1, 1));
            buffers.UVs.Add(new Vector2(0, 1));

            buffers.UV2s.Add(layer);
            buffers.UV2s.Add(layer);
            buffers.UV2s.Add(layer);
            buffers.UV2s.Add(layer);

            buffers.Colors.Add(c);
            buffers.Colors.Add(c);
            buffers.Colors.Add(c);
            buffers.Colors.Add(c);
        }

        private static Color32 ApplyLightAndAO(byte light, byte ao)
        {
            if (light > 15)
                light = 15;

            float lightFactor = light / 15f;
            float aoFactor = VoxelAO.AOToBrightness(ao);
            float final = Mathf.Clamp01(lightFactor * aoFactor);

            byte v = (byte)Mathf.RoundToInt(final * 255f);
            return new Color32(v, v, v, 255);
        }
    }
}