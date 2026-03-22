using UnityEngine;

namespace Blockbound.Streaming
{
    public static class ChunkPriorityUtility
    {
        public static float ComputePriority(Vector3 playerPosition, Vector3 cameraForward, Vector2Int chunkCoord, int chunkSize)
        {
            Vector3 chunkCenter = new Vector3(
                chunkCoord.x * chunkSize + chunkSize * 0.5f,
                playerPosition.y,
                chunkCoord.y * chunkSize + chunkSize * 0.5f
            );

            Vector3 toChunk = chunkCenter - playerPosition;
            float distance = toChunk.magnitude;

            Vector3 flatForward = new Vector3(cameraForward.x, 0f, cameraForward.z).normalized;
            Vector3 flatToChunk = new Vector3(toChunk.x, 0f, toChunk.z).normalized;

            float facing = Vector3.Dot(flatForward, flatToChunk);
            float facingBias = Mathf.Lerp(1.25f, 0.75f, (facing + 1f) * 0.5f);

            return distance * facingBias;
        }
    }
}