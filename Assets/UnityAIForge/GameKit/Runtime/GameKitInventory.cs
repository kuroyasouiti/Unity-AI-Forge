using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace UnityAIForge.GameKit
{
    /// <summary>
    /// GameKit Inventory: Manages items, stacking, and equipment.
    /// </summary>
    [AddComponentMenu("UnityAIForge/GameKit/Inventory")]
    public class GameKitInventory : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string inventoryId;

        [Header("Capacity")]
        [SerializeField] private int maxSlots = 20;
        [SerializeField] private List<InventorySlot> slots = new List<InventorySlot>();

        [Header("Categories")]
        [SerializeField] private List<string> allowedCategories = new List<string>();
        [SerializeField] private List<string> stackableCategories = new List<string>();
        [SerializeField] private int defaultMaxStack = 99;

        [Header("Equipment")]
        [SerializeField] private List<EquippedItem> equippedItems = new List<EquippedItem>();

        [Header("Events")]
        public UnityEvent<string, int> OnItemAdded = new UnityEvent<string, int>();
        public UnityEvent<string, int> OnItemRemoved = new UnityEvent<string, int>();
        public UnityEvent<string> OnItemUsed = new UnityEvent<string>();
        public UnityEvent<string, string> OnItemEquipped = new UnityEvent<string, string>();
        public UnityEvent<string, string> OnItemUnequipped = new UnityEvent<string, string>();
        public UnityEvent OnInventoryChanged = new UnityEvent();
        public UnityEvent OnInventoryFull = new UnityEvent();

        // Registry for finding inventories by ID
        private static readonly Dictionary<string, GameKitInventory> _registry = new Dictionary<string, GameKitInventory>();

        // Properties
        public string InventoryId => inventoryId;
        public int MaxSlots => maxSlots;
        public int UsedSlots => slots.Count(s => !s.IsEmpty);
        public int FreeSlots => maxSlots - UsedSlots;
        public bool IsFull => FreeSlots <= 0;
        public IReadOnlyList<InventorySlot> Slots => slots.AsReadOnly();
        public IReadOnlyList<EquippedItem> EquippedItems => equippedItems.AsReadOnly();

        private void Awake()
        {
            EnsureEventsInitialized();
            InitializeSlots();
        }

        private void OnEnable()
        {
            if (!string.IsNullOrEmpty(inventoryId))
            {
                _registry[inventoryId] = this;
            }
        }

        private void OnDisable()
        {
            if (!string.IsNullOrEmpty(inventoryId))
            {
                _registry.Remove(inventoryId);
            }
        }

        /// <summary>
        /// Find inventory by ID.
        /// </summary>
        public static GameKitInventory FindById(string id)
        {
            return _registry.TryGetValue(id, out var inventory) ? inventory : null;
        }

        /// <summary>
        /// Initialize inventory with settings.
        /// </summary>
        public void Initialize(string id, int slots, List<string> categories = null, List<string> stackable = null, int maxStack = 99)
        {
            inventoryId = id;
            maxSlots = Mathf.Max(1, slots);
            allowedCategories = categories ?? new List<string>();
            stackableCategories = stackable ?? new List<string>();
            defaultMaxStack = maxStack;
            InitializeSlots();
            EnsureEventsInitialized();

            if (!string.IsNullOrEmpty(inventoryId))
            {
                _registry[inventoryId] = this;
            }
        }

        private void InitializeSlots()
        {
            // Ensure we have the correct number of slots
            while (slots.Count < maxSlots)
            {
                slots.Add(new InventorySlot());
            }
            while (slots.Count > maxSlots)
            {
                slots.RemoveAt(slots.Count - 1);
            }
        }

        private void EnsureEventsInitialized()
        {
            OnItemAdded ??= new UnityEvent<string, int>();
            OnItemRemoved ??= new UnityEvent<string, int>();
            OnItemUsed ??= new UnityEvent<string>();
            OnItemEquipped ??= new UnityEvent<string, string>();
            OnItemUnequipped ??= new UnityEvent<string, string>();
            OnInventoryChanged ??= new UnityEvent();
            OnInventoryFull ??= new UnityEvent();
        }

        /// <summary>
        /// Add item to inventory.
        /// </summary>
        /// <param name="itemAsset">Item asset to add</param>
        /// <param name="quantity">Quantity to add</param>
        /// <returns>Number of items actually added</returns>
        public int AddItem(GameKitItemAsset itemAsset, int quantity = 1)
        {
            if (itemAsset == null || quantity <= 0)
                return 0;

            // Check category restriction
            if (allowedCategories.Count > 0)
            {
                string cat = itemAsset.Category.ToString().ToLower();
                if (!allowedCategories.Contains(cat) && !allowedCategories.Contains(itemAsset.CustomCategory))
                {
                    return 0;
                }
            }

            int remaining = quantity;
            int totalAdded = 0;

            // Check if stackable
            bool isStackable = itemAsset.Stackable ||
                               stackableCategories.Contains(itemAsset.Category.ToString().ToLower()) ||
                               stackableCategories.Contains(itemAsset.CustomCategory);

            if (isStackable)
            {
                int maxStack = itemAsset.MaxStack > 0 ? itemAsset.MaxStack : defaultMaxStack;

                // First try to stack with existing slots
                foreach (var slot in slots)
                {
                    if (slot.ItemId == itemAsset.ItemId && slot.Quantity < maxStack)
                    {
                        int canAdd = Mathf.Min(remaining, maxStack - slot.Quantity);
                        slot.Quantity += canAdd;
                        remaining -= canAdd;
                        totalAdded += canAdd;

                        if (remaining <= 0)
                            break;
                    }
                }
            }

            // Add to empty slots
            while (remaining > 0)
            {
                var emptySlot = slots.FirstOrDefault(s => s.IsEmpty);
                if (emptySlot == null)
                {
                    OnInventoryFull?.Invoke();
                    break;
                }

                int maxStack = isStackable ? (itemAsset.MaxStack > 0 ? itemAsset.MaxStack : defaultMaxStack) : 1;
                int canAdd = isStackable ? Mathf.Min(remaining, maxStack) : 1;

                emptySlot.ItemId = itemAsset.ItemId;
                emptySlot.ItemAsset = itemAsset;
                emptySlot.Quantity = canAdd;

                remaining -= canAdd;
                totalAdded += canAdd;
            }

            if (totalAdded > 0)
            {
                OnItemAdded?.Invoke(itemAsset.ItemId, totalAdded);
                OnInventoryChanged?.Invoke();
            }

            return totalAdded;
        }

        /// <summary>
        /// Add item by ID (looks up asset).
        /// </summary>
        public int AddItemById(string itemId, int quantity = 1)
        {
            var asset = Resources.Load<GameKitItemAsset>($"Items/{itemId}");
            if (asset == null)
            {
                // Try finding in loaded assets
                asset = FindItemAsset(itemId);
            }

            return asset != null ? AddItem(asset, quantity) : 0;
        }

        /// <summary>
        /// Remove item from inventory.
        /// </summary>
        /// <param name="itemId">Item ID to remove</param>
        /// <param name="quantity">Quantity to remove</param>
        /// <returns>Number of items actually removed</returns>
        public int RemoveItem(string itemId, int quantity = 1)
        {
            if (string.IsNullOrEmpty(itemId) || quantity <= 0)
                return 0;

            int remaining = quantity;
            int totalRemoved = 0;

            // Remove from slots (from last to first to preserve order)
            for (int i = slots.Count - 1; i >= 0 && remaining > 0; i--)
            {
                var slot = slots[i];
                if (slot.ItemId == itemId)
                {
                    int canRemove = Mathf.Min(remaining, slot.Quantity);
                    slot.Quantity -= canRemove;
                    remaining -= canRemove;
                    totalRemoved += canRemove;

                    if (slot.Quantity <= 0)
                    {
                        slot.Clear();
                    }
                }
            }

            if (totalRemoved > 0)
            {
                OnItemRemoved?.Invoke(itemId, totalRemoved);
                OnInventoryChanged?.Invoke();
            }

            return totalRemoved;
        }

        /// <summary>
        /// Remove item from specific slot.
        /// </summary>
        public int RemoveItemFromSlot(int slotIndex, int quantity = 1)
        {
            if (slotIndex < 0 || slotIndex >= slots.Count)
                return 0;

            var slot = slots[slotIndex];
            if (slot.IsEmpty)
                return 0;

            string itemId = slot.ItemId;
            int canRemove = Mathf.Min(quantity, slot.Quantity);
            slot.Quantity -= canRemove;

            if (slot.Quantity <= 0)
            {
                slot.Clear();
            }

            if (canRemove > 0)
            {
                OnItemRemoved?.Invoke(itemId, canRemove);
                OnInventoryChanged?.Invoke();
            }

            return canRemove;
        }

        /// <summary>
        /// Use item at slot index.
        /// </summary>
        public bool UseItem(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slots.Count)
                return false;

            var slot = slots[slotIndex];
            if (slot.IsEmpty || slot.ItemAsset == null)
                return false;

            var useAction = slot.ItemAsset.OnUse;
            if (useAction == null || useAction.type == GameKitItemAsset.UseActionType.None)
                return false;

            // Execute use action
            bool success = ExecuteUseAction(useAction);

            if (success)
            {
                OnItemUsed?.Invoke(slot.ItemId);

                // Consume item if configured
                if (useAction.consumeOnUse)
                {
                    RemoveItemFromSlot(slotIndex, 1);
                }
            }

            return success;
        }

        private bool ExecuteUseAction(GameKitItemAsset.ItemUseAction action)
        {
            switch (action.type)
            {
                case GameKitItemAsset.UseActionType.Heal:
                    var health = GameKitHealth.FindById(action.healthId);
                    if (health != null)
                    {
                        health.Heal(action.healAmount);
                        return true;
                    }
                    return false;

                case GameKitItemAsset.UseActionType.AddResource:
                    var resourceManager = GameKitResourceManager.FindById(action.resourceManagerId);
                    if (resourceManager != null)
                    {
                        resourceManager.AddResource(action.resourceName, action.resourceAmount);
                        return true;
                    }
                    return false;

                case GameKitItemAsset.UseActionType.PlayEffect:
                    if (GameKitEffectManager.Instance != null)
                    {
                        GameKitEffectManager.Instance.PlayEffect(action.effectId, transform.position);
                        return true;
                    }
                    return false;

                case GameKitItemAsset.UseActionType.Custom:
                    // Custom actions handled by event listeners
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Equip item from slot.
        /// </summary>
        public bool Equip(int slotIndex, string equipSlot = null)
        {
            if (slotIndex < 0 || slotIndex >= slots.Count)
                return false;

            var slot = slots[slotIndex];
            if (slot.IsEmpty || slot.ItemAsset == null || !slot.ItemAsset.Equippable)
                return false;

            var targetSlot = string.IsNullOrEmpty(equipSlot)
                ? slot.ItemAsset.EquipSlot.ToString()
                : equipSlot;

            // Unequip existing item in slot
            Unequip(targetSlot);

            // Remove from inventory (1 item)
            string itemId = slot.ItemId;
            var itemAsset = slot.ItemAsset;
            RemoveItemFromSlot(slotIndex, 1);

            // Add to equipped items
            equippedItems.Add(new EquippedItem
            {
                EquipSlot = targetSlot,
                ItemId = itemId,
                ItemAsset = itemAsset
            });

            OnItemEquipped?.Invoke(itemId, targetSlot);
            OnInventoryChanged?.Invoke();

            return true;
        }

        /// <summary>
        /// Unequip item from equipment slot.
        /// </summary>
        public bool Unequip(string equipSlot)
        {
            var equipped = equippedItems.FirstOrDefault(e => e.EquipSlot == equipSlot);
            if (equipped == null || equipped.ItemAsset == null)
                return false;

            // Try to add back to inventory
            int added = AddItem(equipped.ItemAsset, 1);
            if (added <= 0)
            {
                // Inventory full
                OnInventoryFull?.Invoke();
                return false;
            }

            string itemId = equipped.ItemId;
            equippedItems.Remove(equipped);

            OnItemUnequipped?.Invoke(itemId, equipSlot);
            OnInventoryChanged?.Invoke();

            return true;
        }

        /// <summary>
        /// Get equipped item in slot.
        /// </summary>
        public EquippedItem GetEquipped(string equipSlot)
        {
            return equippedItems.FirstOrDefault(e => e.EquipSlot == equipSlot);
        }

        /// <summary>
        /// Check if item exists in inventory.
        /// </summary>
        public bool HasItem(string itemId, int quantity = 1)
        {
            return GetItemCount(itemId) >= quantity;
        }

        /// <summary>
        /// Get total count of item.
        /// </summary>
        public int GetItemCount(string itemId)
        {
            return slots.Where(s => s.ItemId == itemId).Sum(s => s.Quantity);
        }

        /// <summary>
        /// Get slot at index.
        /// </summary>
        public InventorySlot GetSlot(int index)
        {
            return index >= 0 && index < slots.Count ? slots[index] : null;
        }

        /// <summary>
        /// Find first slot containing item.
        /// </summary>
        public int FindSlotIndex(string itemId)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].ItemId == itemId)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Clear all items.
        /// </summary>
        public void Clear()
        {
            foreach (var slot in slots)
            {
                slot.Clear();
            }
            equippedItems.Clear();
            OnInventoryChanged?.Invoke();
        }

        /// <summary>
        /// Sort inventory by category.
        /// </summary>
        public void Sort()
        {
            var items = slots.Where(s => !s.IsEmpty)
                            .OrderBy(s => s.ItemAsset?.Category)
                            .ThenBy(s => s.ItemId)
                            .ToList();

            for (int i = 0; i < slots.Count; i++)
            {
                if (i < items.Count)
                {
                    slots[i].ItemId = items[i].ItemId;
                    slots[i].ItemAsset = items[i].ItemAsset;
                    slots[i].Quantity = items[i].Quantity;
                }
                else
                {
                    slots[i].Clear();
                }
            }

            OnInventoryChanged?.Invoke();
        }

        /// <summary>
        /// Get serializable inventory data.
        /// </summary>
        public InventoryData GetSaveData()
        {
            return new InventoryData
            {
                inventoryId = inventoryId,
                slots = slots.Select(s => new SlotData
                {
                    itemId = s.ItemId,
                    quantity = s.Quantity
                }).ToList(),
                equipped = equippedItems.Select(e => new EquippedData
                {
                    equipSlot = e.EquipSlot,
                    itemId = e.ItemId
                }).ToList()
            };
        }

        /// <summary>
        /// Load inventory from data.
        /// </summary>
        public void LoadSaveData(InventoryData data)
        {
            if (data == null)
                return;

            // Clear current
            Clear();

            // Load slots
            for (int i = 0; i < data.slots.Count && i < slots.Count; i++)
            {
                var slotData = data.slots[i];
                if (!string.IsNullOrEmpty(slotData.itemId))
                {
                    var asset = FindItemAsset(slotData.itemId);
                    if (asset != null)
                    {
                        slots[i].ItemId = slotData.itemId;
                        slots[i].ItemAsset = asset;
                        slots[i].Quantity = slotData.quantity;
                    }
                }
            }

            // Load equipped
            foreach (var equipped in data.equipped)
            {
                var asset = FindItemAsset(equipped.itemId);
                if (asset != null)
                {
                    equippedItems.Add(new EquippedItem
                    {
                        EquipSlot = equipped.equipSlot,
                        ItemId = equipped.itemId,
                        ItemAsset = asset
                    });
                }
            }

            OnInventoryChanged?.Invoke();
        }

        private GameKitItemAsset FindItemAsset(string itemId)
        {
            // Try Resources folder
            var asset = Resources.Load<GameKitItemAsset>($"Items/{itemId}");
            if (asset != null)
                return asset;

            // Try all loaded assets
#if UNITY_EDITOR
            var guids = UnityEditor.AssetDatabase.FindAssets("t:GameKitItemAsset");
            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var loadedAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<GameKitItemAsset>(path);
                if (loadedAsset != null && loadedAsset.ItemId == itemId)
                    return loadedAsset;
            }
#endif

            return null;
        }

        #region Serializable Types

        [Serializable]
        public class InventorySlot
        {
            public string ItemId;
            public GameKitItemAsset ItemAsset;
            public int Quantity;

            public bool IsEmpty => string.IsNullOrEmpty(ItemId) || Quantity <= 0;

            public void Clear()
            {
                ItemId = null;
                ItemAsset = null;
                Quantity = 0;
            }
        }

        [Serializable]
        public class EquippedItem
        {
            public string EquipSlot;
            public string ItemId;
            public GameKitItemAsset ItemAsset;
        }

        [Serializable]
        public class InventoryData
        {
            public string inventoryId;
            public List<SlotData> slots = new List<SlotData>();
            public List<EquippedData> equipped = new List<EquippedData>();
        }

        [Serializable]
        public class SlotData
        {
            public string itemId;
            public int quantity;
        }

        [Serializable]
        public class EquippedData
        {
            public string equipSlot;
            public string itemId;
        }

        #endregion
    }
}
