using System.Collections.Generic;
using UnityEngine;

namespace MCP.Editor.Tests
{
    /// <summary>
    /// 複合型テスト用のScriptableObject
    /// </summary>
    public class TestCombinedTypesScriptableObject : ScriptableObject
    {
        [Header("User-Defined Class Arrays")]
        public ItemData[] itemDataArray;
        public List<ItemData> itemDataList;
        public SkillData[] skillDataArray;
        public List<CharacterBuild> characterBuildList;

        [Header("Complex Struct Arrays")]
        public StatBlock[] statBlockArray;
        public List<StatBlock> statBlockList;
        public DropTableEntry[] dropTableEntries;
        public Waypoint[] waypoints;
        public List<Waypoint> waypointList;
        public DamageInfo[] damageInfoArray;
        public QuestReward[] questRewards;
        public SpawnRule[] spawnRules;

        [Header("Mixed Type Properties")]
        public int playerLevel;
        public string playerName;
        public float moveSpeed;
        public ItemCategory defaultCategory;
        public Rarity defaultRarity;
        public Vector3 homePosition;
        public Color playerColor;

        [Header("Enum Arrays")]
        public ItemCategory[] categories;
        public List<Rarity> rarities;
        public StatusEffect[] statusEffects;

        [Header("LayerMask Properties")]
        public LayerMask singleLayerMask;
        public LayerMask[] layerMaskArray;
        public List<LayerMask> layerMaskList;
    }
}
