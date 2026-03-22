using System.Collections.Generic;
using UnityEngine;

namespace Blockbound.Chunks
{
    public class ChunkSectionMeshData
    {
        public readonly List<Vector3> OpaqueVertices = new List<Vector3>();
        public readonly List<int> OpaqueTriangles = new List<int>();
        public readonly List<Vector2> OpaqueUVs = new List<Vector2>();
        public readonly List<Vector2> OpaqueUV2s = new List<Vector2>();
        public readonly List<Color32> OpaqueColors = new List<Color32>();
        public readonly List<Vector3> OpaqueNormals = new List<Vector3>();

        public readonly List<Vector3> CutoutVertices = new List<Vector3>();
        public readonly List<int> CutoutTriangles = new List<int>();
        public readonly List<Vector2> CutoutUVs = new List<Vector2>();
        public readonly List<Vector2> CutoutUV2s = new List<Vector2>();
        public readonly List<Color32> CutoutColors = new List<Color32>();
        public readonly List<Vector3> CutoutNormals = new List<Vector3>();
    }
}