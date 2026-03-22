using Blockbound.Blocks;
using Blockbound.Chunks;
using Blockbound.Core;
using Blockbound.World;
using UnityEngine;

namespace Blockbound.Meshing
{
    public static class VoxelAO
    {
        public static byte[] GetFaceAO(VoxelWorld world, int worldX, int worldY, int worldZ, int face)
        {
            byte[] ao = new byte[4];

            switch (face)
            {
                case 0:
                    ao[0] = ComputeAO(world, worldX,     worldY + 1, worldZ,     -1, 0, 0, 0, 0, -1, -1, 0, -1);
                    ao[1] = ComputeAO(world, worldX,     worldY + 1, worldZ + 1, -1, 0, 0, 0, 0,  1, -1, 0,  1);
                    ao[2] = ComputeAO(world, worldX + 1, worldY + 1, worldZ + 1,  1, 0, 0, 0, 0,  1,  1, 0,  1);
                    ao[3] = ComputeAO(world, worldX + 1, worldY + 1, worldZ,      1, 0, 0, 0, 0, -1,  1, 0, -1);
                    break;

                case 1:
                    ao[0] = ComputeAO(world, worldX,     worldY, worldZ,     -1, 0, 0, 0, 0, -1, -1, 0, -1);
                    ao[1] = ComputeAO(world, worldX + 1, worldY, worldZ,      1, 0, 0, 0, 0, -1,  1, 0, -1);
                    ao[2] = ComputeAO(world, worldX + 1, worldY, worldZ + 1,  1, 0, 0, 0, 0,  1,  1, 0,  1);
                    ao[3] = ComputeAO(world, worldX,     worldY, worldZ + 1, -1, 0, 0, 0, 0,  1, -1, 0,  1);
                    break;

                case 2:
                    ao[0] = ComputeAO(world, worldX,     worldY,     worldZ, -1, 0, 0, 0, 1, 0, -1, 1, 0);
                    ao[1] = ComputeAO(world, worldX,     worldY + 1, worldZ, -1, 0, 0, 0,-1, 0, -1,-1, 0);
                    ao[2] = ComputeAO(world, worldX + 1, worldY + 1, worldZ,  1, 0, 0, 0,-1, 0,  1,-1, 0);
                    ao[3] = ComputeAO(world, worldX + 1, worldY,     worldZ,  1, 0, 0, 0, 1, 0,  1, 1, 0);
                    break;

                case 3:
                    ao[0] = ComputeAO(world, worldX + 1, worldY,     worldZ + 1, 1, 0, 0, 0, 1, 0,  1, 1, 0);
                    ao[1] = ComputeAO(world, worldX + 1, worldY + 1, worldZ + 1, 1, 0, 0, 0,-1, 0,  1,-1, 0);
                    ao[2] = ComputeAO(world, worldX,     worldY + 1, worldZ + 1,-1, 0, 0, 0,-1, 0, -1,-1, 0);
                    ao[3] = ComputeAO(world, worldX,     worldY,     worldZ + 1,-1, 0, 0, 0, 1, 0, -1, 1, 0);
                    break;

                case 4:
                    ao[0] = ComputeAO(world, worldX, worldY,     worldZ + 1, 0, 0, 1, 0, 1, 0, 0, 1, 1);
                    ao[1] = ComputeAO(world, worldX, worldY + 1, worldZ + 1, 0, 0, 1, 0,-1, 0, 0,-1, 1);
                    ao[2] = ComputeAO(world, worldX, worldY + 1, worldZ,     0, 0,-1, 0,-1, 0, 0,-1,-1);
                    ao[3] = ComputeAO(world, worldX, worldY,     worldZ,     0, 0,-1, 0, 1, 0, 0, 1,-1);
                    break;

                case 5:
                    ao[0] = ComputeAO(world, worldX + 1, worldY,     worldZ,     0, 0,-1, 0, 1, 0, 0, 1,-1);
                    ao[1] = ComputeAO(world, worldX + 1, worldY + 1, worldZ,     0, 0,-1, 0,-1, 0, 0,-1,-1);
                    ao[2] = ComputeAO(world, worldX + 1, worldY + 1, worldZ + 1, 0, 0, 1, 0,-1, 0, 0,-1, 1);
                    ao[3] = ComputeAO(world, worldX + 1, worldY,     worldZ + 1, 0, 0, 1, 0, 1, 0, 0, 1, 1);
                    break;
            }

            return ao;
        }

        private static byte ComputeAO(VoxelWorld world, int vx, int vy, int vz,
            int s1x, int s1y, int s1z,
            int s2x, int s2y, int s2z,
            int cx, int cy, int cz)
        {
            bool side1 = IsOccluding(world, vx + s1x, vy + s1y, vz + s1z);
            bool side2 = IsOccluding(world, vx + s2x, vy + s2y, vz + s2z);
            bool corner = IsOccluding(world, vx + cx, vy + cy, vz + cz);

            int occlusion;
            if (side1 && side2)
                occlusion = 3;
            else
                occlusion = (side1 ? 1 : 0) + (side2 ? 1 : 0) + (corner ? 1 : 0);

            return (byte)occlusion;
        }

        private static bool IsOccluding(VoxelWorld world, int x, int y, int z)
        {
            BlockData b = world.GetBlock(x, y, z);
            if (b.Id == 0)
                return false;

            BlockDefinition def = BlockRegistry.Instance.Get(b.Id);
            return def != null && def.IsOpaque;
        }

        public static float AOToBrightness(byte ao)
        {
            switch (ao)
            {
                case 0: return 1.00f;
                case 1: return 0.84f;
                case 2: return 0.68f;
                default: return 0.52f;
            }
        }
    }
}