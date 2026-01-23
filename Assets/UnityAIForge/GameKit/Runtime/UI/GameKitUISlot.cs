using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UnityAIForge.GameKit
{
    /// <summary>
    /// GameKit UI Slot: Single slot for items with drag-drop support.
    /// Used for equipment slots, quickslots, and storage.
    /// </summary>
    [AddComponentMenu("UnityAIForge/GameKit/UI/UI Slot")]
    public class GameKitUISlot : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        [Header("Identity")]
        [SerializeField] private string slotId;
        [SerializeField] private int slotIndex = -1;

        [Header("Slot Configuration")]
        [SerializeField] private SlotType slotType = SlotType.Storage;
        [SerializeField] private string equipSlotName;
        [SerializeField] private List<string> acceptCategories = new List<string>();

        [Header("Data Source")]
        [SerializeField] private string inventoryId;

        [Header("Visuals")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Text quantityText;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image highlightImage;
        [SerializeField] private Color emptyColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        [SerializeField] private Color filledColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
        [SerializeField] private Color highlightColor = new Color(0.5f, 0.7f, 1f, 0.5f);

        [Header("Drag & Drop")]
        [SerializeField] private bool dragDropEnabled = true;
        [SerializeField] private Canvas dragCanvas;

        [Header("Events")]
        public UnityEvent<GameKitUISlot> OnSlotClicked = new UnityEvent<GameKitUISlot>();
        public UnityEvent<GameKitUISlot> OnSlotDoubleClicked = new UnityEvent<GameKitUISlot>();
        public UnityEvent<GameKitUISlot, GameKitUISlot> OnItemDropped = new UnityEvent<GameKitUISlot, GameKitUISlot>();
        public UnityEvent<GameKitUISlot> OnSlotChanged = new UnityEvent<GameKitUISlot>();

        // Registry
        private static readonly Dictionary<string, GameKitUISlot> _registry = new Dictionary<string, GameKitUISlot>();

        // State
        private SlotData _currentItem;
        private GameObject _dragIcon;
        private float _lastClickTime;
        private const float DoubleClickThreshold = 0.3f;

        public string SlotId => slotId;
        public int SlotIndex => slotIndex;
        public SlotType Type => slotType;
        public string EquipSlotName => equipSlotName;
        public string InventoryId => inventoryId;
        public bool IsEmpty => _currentItem == null || string.IsNullOrEmpty(_currentItem.itemId);
        public SlotData CurrentItem => _currentItem;
        public bool DragDropEnabled => dragDropEnabled;

        public enum SlotType
        {
            Storage,        // General inventory slot
            Equipment,      // Equipment slot (weapon, armor, etc.)
            Quickslot,      // Hotbar/quickslot
            Trash           // Trash/delete slot
        }

        [Serializable]
        public class SlotData
        {
            public string itemId;
            public string itemName;
            public Sprite icon;
            public int quantity = 1;
            public string category;
            public Dictionary<string, object> customData = new Dictionary<string, object>();
        }

        public static GameKitUISlot FindById(string id)
        {
            return _registry.TryGetValue(id, out var slot) ? slot : null;
        }

        public static List<GameKitUISlot> FindByInventoryId(string inventoryId)
        {
            var result = new List<GameKitUISlot>();
            foreach (var slot in _registry.Values)
            {
                if (slot.inventoryId == inventoryId)
                    result.Add(slot);
            }
            return result;
        }

        private void Awake()
        {
            EnsureEventsInitialized();
            CacheComponents();
        }

        private void OnEnable()
        {
            if (!string.IsNullOrEmpty(slotId))
            {
                _registry[slotId] = this;
            }
            RefreshVisuals();
        }

        private void OnDisable()
        {
            if (!string.IsNullOrEmpty(slotId))
            {
                _registry.Remove(slotId);
            }
        }

        public void Initialize(string id, SlotType type, string invId, int index = -1, string equipSlot = null)
        {
            slotId = id;
            slotType = type;
            inventoryId = invId;
            slotIndex = index;
            equipSlotName = equipSlot;

            if (!string.IsNullOrEmpty(slotId))
            {
                _registry[slotId] = this;
            }

            CacheComponents();
            RefreshVisuals();
        }

        private void EnsureEventsInitialized()
        {
            OnSlotClicked ??= new UnityEvent<GameKitUISlot>();
            OnSlotDoubleClicked ??= new UnityEvent<GameKitUISlot>();
            OnItemDropped ??= new UnityEvent<GameKitUISlot, GameKitUISlot>();
            OnSlotChanged ??= new UnityEvent<GameKitUISlot>();
        }

        private void CacheComponents()
        {
            if (iconImage == null)
                iconImage = transform.Find("Icon")?.GetComponent<Image>();

            if (quantityText == null)
                quantityText = transform.Find("Quantity")?.GetComponent<Text>();

            if (backgroundImage == null)
                backgroundImage = GetComponent<Image>();

            if (highlightImage == null)
                highlightImage = transform.Find("Highlight")?.GetComponent<Image>();

            if (dragCanvas == null)
                dragCanvas = GetComponentInParent<Canvas>();
        }

        /// <summary>
        /// Set the item in this slot.
        /// </summary>
        public void SetItem(SlotData item)
        {
            _currentItem = item;
            RefreshVisuals();
            OnSlotChanged?.Invoke(this);
        }

        /// <summary>
        /// Set item from inventory slot.
        /// </summary>
        public void SetItemFromInventory(GameKitInventory.InventorySlot inventorySlot)
        {
            if (inventorySlot == null || inventorySlot.IsEmpty)
            {
                ClearSlot();
                return;
            }

            _currentItem = new SlotData
            {
                itemId = inventorySlot.ItemId,
                itemName = inventorySlot.ItemAsset?.DisplayName ?? "",
                icon = inventorySlot.ItemAsset?.Icon,
                quantity = inventorySlot.Quantity,
                category = inventorySlot.ItemAsset?.Category.ToString() ?? ""
            };

            RefreshVisuals();
            OnSlotChanged?.Invoke(this);
        }

        /// <summary>
        /// Clear the slot.
        /// </summary>
        public void ClearSlot()
        {
            _currentItem = null;
            RefreshVisuals();
            OnSlotChanged?.Invoke(this);
        }

        /// <summary>
        /// Refresh visual representation.
        /// </summary>
        public void RefreshVisuals()
        {
            bool hasItem = !IsEmpty;

            // Icon
            if (iconImage != null)
            {
                iconImage.enabled = hasItem && _currentItem?.icon != null;
                if (hasItem && _currentItem?.icon != null)
                {
                    iconImage.sprite = _currentItem.icon;
                }
            }

            // Quantity
            if (quantityText != null)
            {
                quantityText.enabled = hasItem && _currentItem != null && _currentItem.quantity > 1;
                if (hasItem && _currentItem != null)
                {
                    quantityText.text = _currentItem.quantity.ToString();
                }
            }

            // Background
            if (backgroundImage != null)
            {
                backgroundImage.color = hasItem ? filledColor : emptyColor;
            }

            // Highlight
            if (highlightImage != null)
            {
                highlightImage.enabled = false;
            }
        }

        /// <summary>
        /// Check if this slot can accept an item.
        /// </summary>
        public bool CanAcceptItem(SlotData item)
        {
            if (item == null) return false;

            // Check category restrictions
            if (acceptCategories.Count > 0 && !string.IsNullOrEmpty(item.category))
            {
                bool categoryMatch = false;
                foreach (var cat in acceptCategories)
                {
                    if (string.Equals(cat, item.category, StringComparison.OrdinalIgnoreCase))
                    {
                        categoryMatch = true;
                        break;
                    }
                }
                if (!categoryMatch) return false;
            }

            return true;
        }

        /// <summary>
        /// Show/hide highlight.
        /// </summary>
        public void SetHighlight(bool show)
        {
            if (highlightImage != null)
            {
                highlightImage.enabled = show;
                highlightImage.color = highlightColor;
            }
        }

        #region Event Handlers

        public void OnPointerClick(PointerEventData eventData)
        {
            float currentTime = Time.unscaledTime;

            if (currentTime - _lastClickTime < DoubleClickThreshold)
            {
                OnSlotDoubleClicked?.Invoke(this);
                _lastClickTime = 0f;
            }
            else
            {
                OnSlotClicked?.Invoke(this);
                _lastClickTime = currentTime;
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!dragDropEnabled || IsEmpty) return;

            // Create drag icon
            _dragIcon = new GameObject("DragIcon");
            _dragIcon.transform.SetParent(dragCanvas.transform, false);

            var image = _dragIcon.AddComponent<Image>();
            image.sprite = _currentItem?.icon;
            image.raycastTarget = false;
            image.SetNativeSize();

            var rectTransform = _dragIcon.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(64, 64);

            // Dim the original icon
            if (iconImage != null)
            {
                var color = iconImage.color;
                color.a = 0.5f;
                iconImage.color = color;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_dragIcon == null) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                dragCanvas.transform as RectTransform,
                eventData.position,
                dragCanvas.worldCamera,
                out Vector2 localPoint);

            _dragIcon.transform.localPosition = localPoint;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_dragIcon != null)
            {
                Destroy(_dragIcon);
                _dragIcon = null;
            }

            // Restore original icon
            if (iconImage != null)
            {
                var color = iconImage.color;
                color.a = 1f;
                iconImage.color = color;
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (!dragDropEnabled) return;

            var sourceSlot = eventData.pointerDrag?.GetComponent<GameKitUISlot>();
            if (sourceSlot == null || sourceSlot == this) return;

            if (sourceSlot.IsEmpty) return;

            // Check if we can accept the item
            if (!CanAcceptItem(sourceSlot.CurrentItem)) return;

            // Swap items
            var tempItem = _currentItem;
            SetItem(sourceSlot.CurrentItem);
            sourceSlot.SetItem(tempItem);

            OnItemDropped?.Invoke(sourceSlot, this);

            // Update inventory if connected
            UpdateInventory(sourceSlot);
        }

        #endregion

        private void UpdateInventory(GameKitUISlot sourceSlot)
        {
            if (string.IsNullOrEmpty(inventoryId)) return;

            var inventory = GameKitInventory.FindById(inventoryId);
            if (inventory == null) return;

            // Handle equipment slot drops
            if (slotType == SlotType.Equipment && !IsEmpty)
            {
                // Equip the item
                int sourceIndex = sourceSlot.SlotIndex;
                if (sourceIndex >= 0)
                {
                    inventory.Equip(sourceIndex, equipSlotName);
                }
            }
            else if (slotType == SlotType.Trash && !sourceSlot.IsEmpty)
            {
                // Remove from source
                int sourceIndex = sourceSlot.SlotIndex;
                if (sourceIndex >= 0)
                {
                    inventory.RemoveItemFromSlot(sourceIndex, sourceSlot.CurrentItem?.quantity ?? 1);
                }
                ClearSlot();
            }
        }
    }

    /// <summary>
    /// GameKit UI Slot Bar: Container for multiple slots (quickslots, hotbar).
    /// </summary>
    [AddComponentMenu("UnityAIForge/GameKit/UI/UI Slot Bar")]
    public class GameKitUISlotBar : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string barId;

        [Header("Configuration")]
        [SerializeField] private int slotCount = 8;
        [SerializeField] private LayoutType layout = LayoutType.Horizontal;
        [SerializeField] private GameObject slotPrefab;
        [SerializeField] private string inventoryId;

        [Header("Key Bindings")]
        [SerializeField] private List<string> keyBindings = new List<string>();

        [Header("Events")]
        public UnityEvent<int, GameKitUISlot> OnSlotUsed = new UnityEvent<int, GameKitUISlot>();

        // Registry
        private static readonly Dictionary<string, GameKitUISlotBar> _registry = new Dictionary<string, GameKitUISlotBar>();

        private List<GameKitUISlot> _slots = new List<GameKitUISlot>();
        private LayoutGroup _layoutGroup;

        public string BarId => barId;
        public int SlotCount => slotCount;
        public IReadOnlyList<GameKitUISlot> Slots => _slots.AsReadOnly();

        public enum LayoutType
        {
            Horizontal,
            Vertical,
            Grid
        }

        public static GameKitUISlotBar FindById(string id)
        {
            return _registry.TryGetValue(id, out var bar) ? bar : null;
        }

        private void Awake()
        {
            OnSlotUsed ??= new UnityEvent<int, GameKitUISlot>();
        }

        private void OnEnable()
        {
            if (!string.IsNullOrEmpty(barId))
            {
                _registry[barId] = this;
            }
        }

        private void OnDisable()
        {
            if (!string.IsNullOrEmpty(barId))
            {
                _registry.Remove(barId);
            }
        }

        private void Update()
        {
            // Check key bindings
            for (int i = 0; i < keyBindings.Count && i < _slots.Count; i++)
            {
                if (!string.IsNullOrEmpty(keyBindings[i]) && Input.GetKeyDown(keyBindings[i].ToLower()))
                {
                    UseSlot(i);
                }
            }
        }

        public void Initialize(string id, int count, LayoutType layoutType, GameObject prefab, string invId, List<string> keys = null)
        {
            barId = id;
            slotCount = count;
            layout = layoutType;
            slotPrefab = prefab;
            inventoryId = invId;
            keyBindings = keys ?? new List<string>();

            if (!string.IsNullOrEmpty(barId))
            {
                _registry[barId] = this;
            }

            CreateSlots();
        }

        public void CreateSlots()
        {
            // Clear existing
            foreach (var slot in _slots)
            {
                if (slot != null)
                    Destroy(slot.gameObject);
            }
            _slots.Clear();

            // Setup layout
            SetupLayout();

            // Create slots
            for (int i = 0; i < slotCount; i++)
            {
                var slotObj = slotPrefab != null
                    ? Instantiate(slotPrefab, transform)
                    : CreateDefaultSlot();

                slotObj.name = $"Slot_{i}";

                var slot = slotObj.GetComponent<GameKitUISlot>();
                if (slot == null)
                {
                    slot = slotObj.AddComponent<GameKitUISlot>();
                }

                slot.Initialize($"{barId}_slot_{i}", GameKitUISlot.SlotType.Quickslot, inventoryId, i);

                // Setup click handler
                int capturedIndex = i;
                slot.OnSlotClicked.AddListener((s) => UseSlot(capturedIndex));

                _slots.Add(slot);
            }
        }

        private void SetupLayout()
        {
            // Remove existing layout
            var existing = GetComponent<LayoutGroup>();
            if (existing != null)
                DestroyImmediate(existing);

            switch (layout)
            {
                case LayoutType.Horizontal:
                    var hlg = gameObject.AddComponent<HorizontalLayoutGroup>();
                    hlg.spacing = 5;
                    hlg.childAlignment = TextAnchor.MiddleCenter;
                    _layoutGroup = hlg;
                    break;

                case LayoutType.Vertical:
                    var vlg = gameObject.AddComponent<VerticalLayoutGroup>();
                    vlg.spacing = 5;
                    vlg.childAlignment = TextAnchor.MiddleCenter;
                    _layoutGroup = vlg;
                    break;

                case LayoutType.Grid:
                    var glg = gameObject.AddComponent<GridLayoutGroup>();
                    glg.cellSize = new Vector2(64, 64);
                    glg.spacing = new Vector2(5, 5);
                    _layoutGroup = glg;
                    break;
            }
        }

        private GameObject CreateDefaultSlot()
        {
            var slotObj = new GameObject("Slot");
            slotObj.transform.SetParent(transform, false);

            var image = slotObj.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            var rectTransform = slotObj.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(64, 64);

            // Icon child
            var iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(slotObj.transform, false);
            var iconImage = iconObj.AddComponent<Image>();
            iconImage.raycastTarget = false;
            var iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(4, 4);
            iconRect.offsetMax = new Vector2(-4, -4);

            return slotObj;
        }

        /// <summary>
        /// Use (activate) slot at index.
        /// </summary>
        public void UseSlot(int index)
        {
            if (index < 0 || index >= _slots.Count) return;

            var slot = _slots[index];
            if (slot.IsEmpty) return;

            OnSlotUsed?.Invoke(index, slot);

            // If connected to inventory, use the item
            if (!string.IsNullOrEmpty(inventoryId))
            {
                var inventory = GameKitInventory.FindById(inventoryId);
                if (inventory != null && slot.SlotIndex >= 0)
                {
                    inventory.UseItem(slot.SlotIndex);
                }
            }
        }

        /// <summary>
        /// Get slot at index.
        /// </summary>
        public GameKitUISlot GetSlot(int index)
        {
            return index >= 0 && index < _slots.Count ? _slots[index] : null;
        }

        /// <summary>
        /// Refresh all slots from inventory.
        /// </summary>
        public void RefreshFromInventory()
        {
            if (string.IsNullOrEmpty(inventoryId)) return;

            var inventory = GameKitInventory.FindById(inventoryId);
            if (inventory == null) return;

            for (int i = 0; i < _slots.Count && i < inventory.Slots.Count; i++)
            {
                _slots[i].SetItemFromInventory(inventory.Slots[i]);
            }
        }
    }
}
