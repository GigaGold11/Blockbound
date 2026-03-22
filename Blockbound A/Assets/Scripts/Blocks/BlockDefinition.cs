using UnityEngine;

namespace Blockbound.Blocks
{
    public enum BlockRenderType
    {
        Opaque,
        Cutout,
        Transparent
    }

    [CreateAssetMenu(menuName = "Blockbound/Block Definition")]
    public class BlockDefinition : ScriptableObject
    {
        [Header("Identity")]
        public ushort Id;
        public string BlockName;

        [Header("Gameplay")]
        public float Hardness = 1f;
        public bool IsSolid = true;
        public bool IsOpaque = true;
        public bool IsReplaceable = false;

        public bool EmitsLight = false;
        [Range(0, 15)] public byte LightLevel = 0;

        [Header("Rendering")]
        public BlockRenderType RenderType = BlockRenderType.Opaque;
        public BlockRenderShape RenderShape = BlockRenderShape.Cube;

        public Texture2D TopTexture;
        public Texture2D BottomTexture;
        public Texture2D SideTexture;

        [HideInInspector] public int TopTextureIndex = -1;
        [HideInInspector] public int BottomTextureIndex = -1;
        [HideInInspector] public int SideTextureIndex = -1;

        [Header("Special")]
        public bool IsAir = false;
    }
}