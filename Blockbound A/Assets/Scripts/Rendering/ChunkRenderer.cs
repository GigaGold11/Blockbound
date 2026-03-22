using System.Collections.Generic;
using Blockbound.Chunks;
using Blockbound.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Blockbound.Rendering
{
    public class ChunkRenderer : MonoBehaviour
    {
        private class SectionRenderObjects
        {
            public GameObject Root;
            public GameObject OpaqueObject;
            public GameObject CutoutObject;
            public MeshFilter OpaqueFilter;
            public MeshFilter CutoutFilter;
            public MeshRenderer OpaqueRenderer;
            public MeshRenderer CutoutRenderer;
            public MeshCollider OpaqueCollider;
            public Mesh OpaqueMesh;
            public Mesh CutoutMesh;
        }

        private readonly Dictionary<int, SectionRenderObjects> sectionObjects = new Dictionary<int, SectionRenderObjects>();

        private Material opaqueMaterial;
        private Material cutoutMaterial;

        public void Initialize(Material opaqueMat, Material cutoutMat)
        {
            opaqueMaterial = opaqueMat;
            cutoutMaterial = cutoutMat;
        }

        public void SetSectionMesh(int sectionIndex, ChunkSectionMeshData meshData)
        {
            SectionRenderObjects sro = GetOrCreateSection(sectionIndex);

            ApplyMesh(
                sro.OpaqueMesh,
                meshData.OpaqueVertices,
                meshData.OpaqueTriangles,
                meshData.OpaqueUVs,
                meshData.OpaqueUV2s,
                meshData.OpaqueColors,
                meshData.OpaqueNormals
            );

            ApplyMesh(
                sro.CutoutMesh,
                meshData.CutoutVertices,
                meshData.CutoutTriangles,
                meshData.CutoutUVs,
                meshData.CutoutUV2s,
                meshData.CutoutColors,
                meshData.CutoutNormals
            );

            sro.OpaqueFilter.sharedMesh = sro.OpaqueMesh;
            sro.CutoutFilter.sharedMesh = sro.CutoutMesh;

            if (sro.OpaqueCollider != null && sro.OpaqueCollider.enabled)
            {
                sro.OpaqueCollider.sharedMesh = null;
                sro.OpaqueCollider.sharedMesh = sro.OpaqueMesh.vertexCount > 0 ? sro.OpaqueMesh : null;
            }

            sro.OpaqueObject.SetActive(sro.OpaqueMesh.vertexCount > 0);
            sro.CutoutObject.SetActive(sro.CutoutMesh.vertexCount > 0);
        }

        public void SetSectionVisible(int sectionIndex, bool visible)
        {
            if (sectionObjects.TryGetValue(sectionIndex, out var sro))
                sro.Root.SetActive(visible);
        }

        public void SetCollisionEnabled(bool enabled)
        {
            foreach (var kvp in sectionObjects)
            {
                if (kvp.Value.OpaqueCollider != null)
                {
                    kvp.Value.OpaqueCollider.enabled = enabled;

                    if (!enabled)
                        kvp.Value.OpaqueCollider.sharedMesh = null;
                    else if (kvp.Value.OpaqueFilter.sharedMesh != null)
                        kvp.Value.OpaqueCollider.sharedMesh = kvp.Value.OpaqueFilter.sharedMesh;
                }
            }
        }

        public void SetShadowMode(ShadowCastingMode shadowMode, bool receiveShadows)
        {
            foreach (var kvp in sectionObjects)
            {
                if (kvp.Value.OpaqueRenderer != null)
                {
                    kvp.Value.OpaqueRenderer.shadowCastingMode = shadowMode;
                    kvp.Value.OpaqueRenderer.receiveShadows = receiveShadows;
                }

                if (kvp.Value.CutoutRenderer != null)
                {
                    kvp.Value.CutoutRenderer.shadowCastingMode = shadowMode;
                    kvp.Value.CutoutRenderer.receiveShadows = receiveShadows;
                }
            }
        }

        private SectionRenderObjects GetOrCreateSection(int sectionIndex)
        {
            if (sectionObjects.TryGetValue(sectionIndex, out var existing))
                return existing;

            SectionRenderObjects sro = new SectionRenderObjects();

            sro.Root = new GameObject("Section_" + sectionIndex);
            sro.Root.transform.SetParent(transform, false);
            sro.Root.transform.localPosition = new Vector3(0f, sectionIndex * VoxelConstants.SectionHeight, 0f);

            sro.OpaqueObject = new GameObject("Opaque");
            sro.OpaqueObject.transform.SetParent(sro.Root.transform, false);
            sro.OpaqueFilter = sro.OpaqueObject.AddComponent<MeshFilter>();
            sro.OpaqueRenderer = sro.OpaqueObject.AddComponent<MeshRenderer>();
            sro.OpaqueRenderer.sharedMaterial = opaqueMaterial;
            sro.OpaqueCollider = sro.OpaqueObject.AddComponent<MeshCollider>();

            sro.CutoutObject = new GameObject("Cutout");
            sro.CutoutObject.transform.SetParent(sro.Root.transform, false);
            sro.CutoutFilter = sro.CutoutObject.AddComponent<MeshFilter>();
            sro.CutoutRenderer = sro.CutoutObject.AddComponent<MeshRenderer>();
            sro.CutoutRenderer.sharedMaterial = cutoutMaterial;

            sro.OpaqueMesh = new Mesh { name = "Section_" + sectionIndex + "_OpaqueMesh", indexFormat = IndexFormat.UInt32 };
            sro.CutoutMesh = new Mesh { name = "Section_" + sectionIndex + "_CutoutMesh", indexFormat = IndexFormat.UInt32 };

            sectionObjects[sectionIndex] = sro;
            return sro;
        }

        private void ApplyMesh(
            Mesh mesh,
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uvs,
            List<Vector2> uv2s,
            List<Color32> colors,
            List<Vector3> normals)
        {
            mesh.Clear();

            if (vertices.Count == 0 || triangles.Count == 0)
            {
                mesh.SetVertices(vertices);
                mesh.SetTriangles(System.Array.Empty<int>(), 0);
                mesh.SetUVs(0, uvs);
                mesh.SetUVs(1, uv2s);
                mesh.SetColors(colors);

                if (normals != null && normals.Count == vertices.Count)
                    mesh.SetNormals(normals);

                mesh.bounds = new Bounds(Vector3.zero, Vector3.zero);
                return;
            }

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0, true);
            mesh.SetUVs(0, uvs);
            mesh.SetUVs(1, uv2s);
            mesh.SetColors(colors);

            if (normals != null && normals.Count == vertices.Count)
                mesh.SetNormals(normals);
            else
                mesh.RecalculateNormals();

            mesh.RecalculateBounds();
        }
    }
}