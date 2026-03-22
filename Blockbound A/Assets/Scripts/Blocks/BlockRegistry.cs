using System.Collections.Generic;
using UnityEngine;

namespace Blockbound.Blocks
{
    public class BlockRegistry : MonoBehaviour
    {
        public static BlockRegistry Instance { get; private set; }

        [SerializeField] private List<BlockDefinition> blockDefinitions = new List<BlockDefinition>();

        private Dictionary<ushort, BlockDefinition> byId = new Dictionary<ushort, BlockDefinition>();
        private Dictionary<string, BlockDefinition> byName = new Dictionary<string, BlockDefinition>();

        private BlockRuntimeInfo[] runtimeInfos = new BlockRuntimeInfo[256];

        public IReadOnlyList<BlockDefinition> AllBlocks => blockDefinitions;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            BuildRegistry();
        }

        private void BuildRegistry()
        {
            byId.Clear();
            byName.Clear();

            ushort maxId = 0;

            foreach (BlockDefinition def in blockDefinitions)
            {
                if (def == null)
                    continue;

                if (byId.ContainsKey(def.Id))
                {
                    Debug.LogError("Duplicate block ID detected: " + def.Id);
                    continue;
                }

                if (!string.IsNullOrEmpty(def.BlockName) && byName.ContainsKey(def.BlockName))
                {
                    Debug.LogError("Duplicate block name detected: " + def.BlockName);
                    continue;
                }

                byId[def.Id] = def;
                byName[def.BlockName] = def;

                if (def.Id > maxId)
                    maxId = def.Id;
            }

            if (!byId.ContainsKey(0))
                Debug.LogWarning("No Air block with ID 0 found in BlockRegistry.");

            if (runtimeInfos == null || runtimeInfos.Length <= maxId)
                runtimeInfos = new BlockRuntimeInfo[maxId + 1];

            for (int i = 0; i < runtimeInfos.Length; i++)
                runtimeInfos[i] = default;

            foreach (BlockDefinition def in blockDefinitions)
            {
                if (def == null)
                    continue;

                runtimeInfos[def.Id] = new BlockRuntimeInfo
                {
                    Exists = true,
                    IsAir = def.IsAir,
                    IsSolid = def.IsSolid,
                    IsOpaque = def.IsOpaque,
                    IsReplaceable = def.IsReplaceable,
                    EmitsLight = def.EmitsLight,
                    LightLevel = def.LightLevel,
                    RenderType = def.RenderType,
                    RenderShape = def.RenderShape,
                    TopTextureIndex = def.TopTextureIndex,
                    BottomTextureIndex = def.BottomTextureIndex,
                    SideTextureIndex = def.SideTextureIndex
                };
            }
        }

        public BlockDefinition Get(ushort id)
        {
            byId.TryGetValue(id, out BlockDefinition def);
            return def;
        }

        public BlockDefinition Get(string blockName)
        {
            byName.TryGetValue(blockName, out BlockDefinition def);
            return def;
        }

        public BlockRuntimeInfo GetRuntimeInfo(ushort id)
        {
            if (id < 0 || id >= runtimeInfos.Length)
                return default;

            return runtimeInfos[id];
        }

        public bool IsOpaque(ushort id)
        {
            return id >= 0 && id < runtimeInfos.Length && runtimeInfos[id].Exists && runtimeInfos[id].IsOpaque;
        }

        public bool IsAir(ushort id)
        {
            return id == 0 || (id >= 0 && id < runtimeInfos.Length && runtimeInfos[id].Exists && runtimeInfos[id].IsAir);
        }

        public bool IsCross(ushort id)
        {
            return id >= 0 && id < runtimeInfos.Length && runtimeInfos[id].Exists && runtimeInfos[id].RenderShape == BlockRenderShape.Cross;
        }
    }
}