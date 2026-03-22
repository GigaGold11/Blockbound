using Blockbound.Core;
using Blockbound.Generation;
using Blockbound.World;
using UnityEngine;

namespace Blockbound.Debugging
{
    public class DebugOverlay : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private bool startVisible = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.F3;

        [Header("References")]
        [SerializeField] private VoxelWorld voxelWorld;
        [SerializeField] private Transform playerTransform;
        [SerializeField] private int seed = 0;

        private bool visible;
        private GUIStyle labelStyle;
        private Texture2D backgroundTexture;

        private void Awake()
        {
            visible = startVisible;

            if (voxelWorld == null)
                voxelWorld = FindFirstObjectByType<VoxelWorld>();

            if (playerTransform == null && Camera.main != null)
                playerTransform = Camera.main.transform;

            backgroundTexture = new Texture2D(1, 1);
            backgroundTexture.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.55f));
            backgroundTexture.Apply();

            labelStyle = new GUIStyle();
            labelStyle.fontSize = 16;
            labelStyle.normal.textColor = Color.white;
            labelStyle.alignment = TextAnchor.UpperLeft;
            labelStyle.padding = new RectOffset(8, 8, 8, 8);
            labelStyle.richText = true;
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
                visible = !visible;

            if (voxelWorld == null)
                voxelWorld = FindFirstObjectByType<VoxelWorld>();

            if (playerTransform == null && Camera.main != null)
                playerTransform = Camera.main.transform;
        }

        private void OnGUI()
        {
            if (!visible || labelStyle == null)
                return;

            DrawPanel(new Rect(10, 10, 340, 140));

            if (playerTransform == null)
            {
                GUI.Label(new Rect(18, 18, 320, 120), "<b>Blockbound Debug</b>\nNo player/camera found.", labelStyle);
                return;
            }

            Vector3 pos = playerTransform.position;

            int worldX = Mathf.FloorToInt(pos.x);
            int worldY = Mathf.FloorToInt(pos.y);
            int worldZ = Mathf.FloorToInt(pos.z);

            Vector2Int chunkCoord = VoxelMath.WorldToChunkCoord(worldX, worldZ);
            string biomeName = BiomeSampler.GetBiomeName(worldX, worldZ, seed);

            string text =
                "<b>Blockbound Debug</b>\n" +
                $"XYZ: {worldX}, {worldY}, {worldZ}\n" +
                $"Chunk: {chunkCoord.x}, {chunkCoord.y}\n" +
                $"Biome: {biomeName}\n" +
                $"Loaded Chunks: {(voxelWorld != null ? voxelWorld.Chunks.Count.ToString() : "0")}\n" +
                $"Toggle: {toggleKey}";

            GUI.Label(new Rect(18, 18, 320, 120), text, labelStyle);
        }

        private void DrawPanel(Rect rect)
        {
            Color old = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(rect, backgroundTexture);
            GUI.color = old;
        }
    }
}