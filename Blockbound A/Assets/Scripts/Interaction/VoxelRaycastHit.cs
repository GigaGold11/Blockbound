using UnityEngine;

namespace Blockbound.Interaction
{
    public struct VoxelRaycastHit
    {
        public bool Hit;
        public Vector3Int BlockPosition;
        public Vector3Int AdjacentPosition;
        public Vector3 Normal;
        public float Distance;
    }
}