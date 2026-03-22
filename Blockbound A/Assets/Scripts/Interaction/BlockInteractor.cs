using Blockbound.Blocks;
using Blockbound.Player;
using Blockbound.World;
using UnityEngine;

namespace Blockbound.Interaction
{
    public class BlockInteractor : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private VoxelWorld voxelWorld;
        [SerializeField] private CreativeHotbar hotbar;
        [SerializeField] private float interactDistance = 8f;

        private void Start()
        {
            if (playerCamera == null)
                playerCamera = Camera.main;
        }

        private void Update()
        {
            if (playerCamera == null || voxelWorld == null)
                return;

            if (Cursor.lockState != CursorLockMode.Locked)
                return;

            VoxelRaycastHit hit = VoxelRaycaster.Raycast(
                voxelWorld,
                playerCamera.transform.position,
                playerCamera.transform.forward,
                interactDistance
            );

            if (!hit.Hit)
                return;

            if (Input.GetMouseButtonDown(0))
            {
                BreakBlock(hit);
            }

            if (Input.GetMouseButtonDown(1))
            {
                PlaceBlock(hit);
            }
        }

        private void BreakBlock(VoxelRaycastHit hit)
        {
            voxelWorld.SetBlock(hit.BlockPosition.x, hit.BlockPosition.y, hit.BlockPosition.z, new BlockData(0));
        }

        private void PlaceBlock(VoxelRaycastHit hit)
        {
            ushort blockId = hotbar != null ? hotbar.SelectedBlockId : (ushort)1;

            if (blockId == 0)
                return;

            Vector3Int pos = hit.AdjacentPosition;
            voxelWorld.SetBlock(pos.x, pos.y, pos.z, new BlockData(blockId));
        }
    }
}