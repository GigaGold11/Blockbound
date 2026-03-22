using System.Collections.Generic;
using UnityEngine;

namespace Blockbound.Meshing
{
    public class MeshBuffers
    {
        public readonly List<Vector3> Vertices = new List<Vector3>();
        public readonly List<int> Triangles = new List<int>();
        public readonly List<Vector2> UVs = new List<Vector2>();
        public readonly List<Vector2> UV2s = new List<Vector2>();
        public readonly List<Color32> Colors = new List<Color32>();
    }
}