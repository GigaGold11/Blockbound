using System.Collections.Generic;
using Blockbound.Blocks;
using UnityEngine;

namespace Blockbound.Rendering
{
    public class BlockTextureArrayBuilder : MonoBehaviour
    {
        [SerializeField] private BlockRegistry blockRegistry;
        [SerializeField] private Material opaqueChunkMaterial;
        [SerializeField] private Material cutoutChunkMaterial;

        private readonly Dictionary<Texture2D, int> textureToIndex = new Dictionary<Texture2D, int>();

        public Texture2DArray TextureArray { get; private set; }

        private void Awake()
        {
            BuildTextureArray();
        }

        private void BuildTextureArray()
        {
            textureToIndex.Clear();

            if (blockRegistry == null)
            {
                Debug.LogError("BlockTextureArrayBuilder: BlockRegistry reference is missing.");
                return;
            }

            if (opaqueChunkMaterial == null)
            {
                Debug.LogError("BlockTextureArrayBuilder: Opaque chunk material reference is missing.");
                return;
            }

            if (cutoutChunkMaterial == null)
            {
                Debug.LogError("BlockTextureArrayBuilder: Cutout chunk material reference is missing.");
                return;
            }

            List<Texture2D> uniqueTextures = new List<Texture2D>();

            foreach (BlockDefinition block in blockRegistry.AllBlocks)
            {
                if (block == null)
                    continue;

                RegisterTexture(block.TopTexture, uniqueTextures);
                RegisterTexture(block.BottomTexture, uniqueTextures);
                RegisterTexture(block.SideTexture, uniqueTextures);
            }

            if (uniqueTextures.Count == 0)
            {
                Debug.LogWarning("No block textures found to build texture array.");
                return;
            }

            int width = uniqueTextures[0].width;
            int height = uniqueTextures[0].height;

            TextureArray = new Texture2DArray(width, height, uniqueTextures.Count, TextureFormat.RGBA32, true, false);
            TextureArray.filterMode = FilterMode.Point;
            TextureArray.wrapMode = TextureWrapMode.Repeat;

            for (int i = 0; i < uniqueTextures.Count; i++)
            {
                Texture2D tex = uniqueTextures[i];

                if (tex == null)
                {
                    Debug.LogError("Null texture found in unique texture list.");
                    return;
                }

                if (tex.width != width || tex.height != height)
                {
                    Debug.LogError("All block textures must have the same dimensions. Problem texture: " + tex.name);
                    return;
                }

                try
                {
                    Color[] pixels = tex.GetPixels();
                    TextureArray.SetPixels(pixels, i);
                }
                catch
                {
                    Debug.LogError("Texture is not readable: " + tex.name + ". Enable Read/Write in import settings.");
                    return;
                }
            }

            TextureArray.Apply();

            foreach (BlockDefinition block in blockRegistry.AllBlocks)
            {
                if (block == null)
                    continue;

                block.TopTextureIndex = GetTextureIndex(block.TopTexture);
                block.BottomTextureIndex = GetTextureIndex(block.BottomTexture);
                block.SideTextureIndex = GetTextureIndex(block.SideTexture);
            }

            opaqueChunkMaterial.SetTexture("_BlockTextures", TextureArray);
            cutoutChunkMaterial.SetTexture("_BlockTextures", TextureArray);

            Debug.Log("Built block texture array successfully.");
        }

        private void RegisterTexture(Texture2D texture, List<Texture2D> uniqueTextures)
        {
            if (texture == null)
                return;

            if (!textureToIndex.ContainsKey(texture))
            {
                int index = uniqueTextures.Count;
                textureToIndex.Add(texture, index);
                uniqueTextures.Add(texture);
            }
        }

        private int GetTextureIndex(Texture2D texture)
        {
            if (texture == null)
                return -1;

            int index;
            if (textureToIndex.TryGetValue(texture, out index))
                return index;

            return -1;
        }
    }
}