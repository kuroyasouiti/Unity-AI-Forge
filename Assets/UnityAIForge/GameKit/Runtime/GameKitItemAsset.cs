using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityAIForge.GameKit
{
    /// <summary>
    /// GameKit Item Asset: ScriptableObject defining item data.
    /// </summary>
    [CreateAssetMenu(fileName = "Item", menuName = "UnityAIForge/GameKit/Item")]
    public class GameKitItemAsset : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string itemId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;

        [Header("Category")]
        [SerializeField] private ItemCategory category = ItemCategory.Misc;
        [SerializeField] private string customCategory;

        [Header("Stacking")]
        [SerializeField] private bool stackable = false;
        [SerializeField] private int maxStack = 1;

        [Header("Visuals")]
        [SerializeField] private Sprite icon;
        [SerializeField] private GameObject worldPrefab;

        [Header("Use Action")]
        [SerializeField] private ItemUseAction onUse = new ItemUseAction();

        [Header("Equipment")]
        [SerializeField] private bool equippable = false;
        [SerializeField] private EquipmentSlot equipSlot = EquipmentSlot.None;
        [SerializeField] private List<StatModifier> equipStats = new List<StatModifier>();

        [Header("Value")]
        [SerializeField] private int buyPrice = 0;
        [SerializeField] private int sellPrice = 0;

        // Properties
        public string ItemId => itemId;
        public string DisplayName => displayName;
        public string Description => description;
        public ItemCategory Category => category;
        public string CustomCategory => customCategory;
        public bool Stackable => stackable;
        public int MaxStack => stackable ? maxStack : 1;
        public Sprite Icon => icon;
        public GameObject WorldPrefab => worldPrefab;
        public ItemUseAction OnUse => onUse;
        public bool Equippable => equippable;
        public EquipmentSlot EquipSlot => equipSlot;
        public IReadOnlyList<StatModifier> EquipStats => equipStats.AsReadOnly();
        public int BuyPrice => buyPrice;
        public int SellPrice => sellPrice;

        /// <summary>
        /// Initialize item data.
        /// </summary>
        public void Initialize(string id, string name, string desc, ItemCategory cat)
        {
            itemId = id;
            displayName = name;
            description = desc;
            category = cat;
        }

        /// <summary>
        /// Set stacking properties.
        /// </summary>
        public void SetStacking(bool isStackable, int max)
        {
            stackable = isStackable;
            maxStack = Mathf.Max(1, max);
        }

        /// <summary>
        /// Set visual properties.
        /// </summary>
        public void SetVisuals(Sprite itemIcon, GameObject prefab)
        {
            icon = itemIcon;
            worldPrefab = prefab;
        }

        /// <summary>
        /// Set use action.
        /// </summary>
        public void SetUseAction(ItemUseAction action)
        {
            onUse = action ?? new ItemUseAction();
        }

        /// <summary>
        /// Set equipment properties.
        /// </summary>
        public void SetEquipment(bool canEquip, EquipmentSlot slot, List<StatModifier> stats)
        {
            equippable = canEquip;
            equipSlot = slot;
            equipStats = stats ?? new List<StatModifier>();
        }

        /// <summary>
        /// Set prices.
        /// </summary>
        public void SetPrices(int buy, int sell)
        {
            buyPrice = Mathf.Max(0, buy);
            sellPrice = Mathf.Max(0, sell);
        }

        /// <summary>
        /// Add equipment stat modifier.
        /// </summary>
        public void AddStatModifier(StatModifier modifier)
        {
            if (modifier != null)
            {
                equipStats.Add(modifier);
            }
        }

        #region Serializable Types

        public enum ItemCategory
        {
            Weapon,
            Armor,
            Consumable,
            Material,
            Key,
            Quest,
            Misc
        }

        public enum EquipmentSlot
        {
            None,
            MainHand,
            OffHand,
            Head,
            Body,
            Hands,
            Feet,
            Accessory1,
            Accessory2
        }

        [Serializable]
        public class ItemUseAction
        {
            [Tooltip("Type of use action")]
            public UseActionType type = UseActionType.None;

            [Header("Heal Settings")]
            [Tooltip("Health ID to heal (if type is Heal)")]
            public string healthId;
            [Tooltip("Amount to heal")]
            public float healAmount;

            [Header("Resource Settings")]
            [Tooltip("Resource manager ID (if type is AddResource)")]
            public string resourceManagerId;
            [Tooltip("Resource name to add")]
            public string resourceName;
            [Tooltip("Amount to add")]
            public float resourceAmount;

            [Header("Effect Settings")]
            [Tooltip("Effect to play (if type is PlayEffect)")]
            public string effectId;

            [Header("Custom Settings")]
            [Tooltip("Custom event name (if type is Custom)")]
            public string customEventName;
            [Tooltip("Custom data")]
            public string customData;

            [Header("General")]
            [Tooltip("Consume item on use")]
            public bool consumeOnUse = true;
        }

        public enum UseActionType
        {
            None,
            Heal,
            AddResource,
            PlayEffect,
            Custom
        }

        [Serializable]
        public class StatModifier
        {
            [Tooltip("Stat name to modify")]
            public string statName;

            [Tooltip("Modifier type")]
            public ModifierType modifierType = ModifierType.Flat;

            [Tooltip("Modifier value")]
            public float value;
        }

        public enum ModifierType
        {
            Flat,
            PercentAdd,
            PercentMultiply
        }

        #endregion
    }
}
