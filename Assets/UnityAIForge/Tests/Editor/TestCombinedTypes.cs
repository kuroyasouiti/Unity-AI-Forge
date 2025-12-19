using System;
using UnityEngine;

namespace MCP.Editor.Tests
{
    #region User-Defined Constants

    /// <summary>
    /// ゲーム設定用の定数クラス
    /// </summary>
    public static class GameConstants
    {
        public const int MaxInventorySlots = 20;
        public const float DefaultMoveSpeed = 5.0f;
        public const string DefaultPlayerName = "Player1";
        public const int MaxLevel = 100;
        public const float CriticalMultiplier = 2.0f;
    }

    /// <summary>
    /// アイテム種別
    /// </summary>
    public enum ItemCategory
    {
        None = 0,
        Weapon = 1,
        Armor = 2,
        Consumable = 3,
        Material = 4,
        Quest = 5
    }

    /// <summary>
    /// レアリティ
    /// </summary>
    public enum Rarity
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Epic = 3,
        Legendary = 4
    }

    /// <summary>
    /// ステータス効果
    /// </summary>
    [Flags]
    public enum StatusEffect
    {
        None = 0,
        Poison = 1,
        Burn = 2,
        Freeze = 4,
        Stun = 8,
        Bleed = 16
    }

    #endregion

    #region User-Defined Serializable Classes

    /// <summary>
    /// アイテムデータ（シリアライズ可能なクラス）
    /// </summary>
    [Serializable]
    public class ItemData
    {
        public int itemId;
        public string itemName;
        public ItemCategory category;
        public Rarity rarity;
        public int stackCount;
        public float weight;
        public string description;
    }

    /// <summary>
    /// スキルデータ（シリアライズ可能なクラス）
    /// </summary>
    [Serializable]
    public class SkillData
    {
        public int skillId;
        public string skillName;
        public float cooldown;
        public int manaCost;
        public StatusEffect appliedEffects;
        public float damageMultiplier;
        public Vector2 effectRange;
    }

    /// <summary>
    /// キャラクタービルド（ネストしたクラス参照）
    /// </summary>
    [Serializable]
    public class CharacterBuild
    {
        public string buildName;
        public int level;
        public StatBlock stats;
        public int[] equippedItemIds;
        public int[] learnedSkillIds;
    }

    #endregion

    #region User-Defined Serializable Structs

    /// <summary>
    /// ステータスブロック（構造体）
    /// </summary>
    [Serializable]
    public struct StatBlock
    {
        public int strength;
        public int dexterity;
        public int intelligence;
        public int vitality;
        public float healthMultiplier;
        public float manaMultiplier;
    }

    /// <summary>
    /// ドロップテーブルエントリ（構造体）
    /// </summary>
    [Serializable]
    public struct DropTableEntry
    {
        public int itemId;
        public float dropChance;
        public int minQuantity;
        public int maxQuantity;
        public Rarity minRarity;
    }

    /// <summary>
    /// ウェイポイント（Unity型とプリミティブの組み合わせ）
    /// </summary>
    [Serializable]
    public struct Waypoint
    {
        public string waypointName;
        public Vector3 position;
        public float waitTime;
        public bool isTeleportPoint;
        public Color markerColor;
    }

    /// <summary>
    /// ダメージ情報（複合型）
    /// </summary>
    [Serializable]
    public struct DamageInfo
    {
        public float baseDamage;
        public float criticalChance;
        public float criticalMultiplier;
        public StatusEffect statusEffects;
        public Vector2 damageRange;
        public Color damageColor;
    }

    /// <summary>
    /// クエスト報酬（配列を含む構造体）
    /// </summary>
    [Serializable]
    public struct QuestReward
    {
        public string rewardName;
        public int experiencePoints;
        public int goldAmount;
        public int[] itemIds;
        public Rarity guaranteedRarity;
    }

    /// <summary>
    /// スポーンルール（複雑なネスト）
    /// </summary>
    [Serializable]
    public struct SpawnRule
    {
        public string ruleName;
        public Vector3 spawnPosition;
        public float spawnRadius;
        public int minCount;
        public int maxCount;
        public float respawnTime;
        public bool isActive;
    }

    #endregion
}
