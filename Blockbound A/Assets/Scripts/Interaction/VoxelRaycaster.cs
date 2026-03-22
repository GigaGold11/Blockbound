using Blockbound.Blocks;
using Blockbound.World;
using UnityEngine;

namespace Blockbound.Interaction
{
    public static class VoxelRaycaster
    {
        public static VoxelRaycastHit Raycast(VoxelWorld world, Vector3 origin, Vector3 direction, float maxDistance)
        {
            VoxelRaycastHit result = new VoxelRaycastHit { Hit = false };

            direction.Normalize();

            int x = Mathf.FloorToInt(origin.x);
            int y = Mathf.FloorToInt(origin.y);
            int z = Mathf.FloorToInt(origin.z);

            int stepX = direction.x > 0 ? 1 : (direction.x < 0 ? -1 : 0);
            int stepY = direction.y > 0 ? 1 : (direction.y < 0 ? -1 : 0);
            int stepZ = direction.z > 0 ? 1 : (direction.z < 0 ? -1 : 0);

            float tMaxX = IntBound(origin.x, direction.x);
            float tMaxY = IntBound(origin.y, direction.y);
            float tMaxZ = IntBound(origin.z, direction.z);

            float tDeltaX = stepX != 0 ? Mathf.Abs(1f / direction.x) : float.PositiveInfinity;
            float tDeltaY = stepY != 0 ? Mathf.Abs(1f / direction.y) : float.PositiveInfinity;
            float tDeltaZ = stepZ != 0 ? Mathf.Abs(1f / direction.z) : float.PositiveInfinity;

            Vector3Int lastBlock = new Vector3Int(x, y, z);
            Vector3 hitNormal = Vector3.zero;

            while (true)
            {
                BlockData block = world.GetBlock(x, y, z);
                if (block.Id != 0)
                {
                    result.Hit = true;
                    result.BlockPosition = new Vector3Int(x, y, z);
                    result.AdjacentPosition = lastBlock;
                    result.Normal = hitNormal;
                    result.Distance = Vector3.Distance(origin, new Vector3(x + 0.5f, y + 0.5f, z + 0.5f));
                    return result;
                }

                lastBlock = new Vector3Int(x, y, z);

                if (tMaxX < tMaxY)
                {
                    if (tMaxX < tMaxZ)
                    {
                        if (tMaxX > maxDistance) break;
                        x += stepX;
                        hitNormal = new Vector3(-stepX, 0, 0);
                        tMaxX += tDeltaX;
                    }
                    else
                    {
                        if (tMaxZ > maxDistance) break;
                        z += stepZ;
                        hitNormal = new Vector3(0, 0, -stepZ);
                        tMaxZ += tDeltaZ;
                    }
                }
                else
                {
                    if (tMaxY < tMaxZ)
                    {
                        if (tMaxY > maxDistance) break;
                        y += stepY;
                        hitNormal = new Vector3(0, -stepY, 0);
                        tMaxY += tDeltaY;
                    }
                    else
                    {
                        if (tMaxZ > maxDistance) break;
                        z += stepZ;
                        hitNormal = new Vector3(0, 0, -stepZ);
                        tMaxZ += tDeltaZ;
                    }
                }
            }

            return result;
        }

        private static float IntBound(float s, float ds)
        {
            if (ds == 0)
                return float.PositiveInfinity;

            if (ds < 0)
                return IntBound(-s, -ds);

            s = Mod(s, 1f);
            return (1f - s) / ds;
        }

        private static float Mod(float value, float modulus)
        {
            return (value % modulus + modulus) % modulus;
        }
    }
}