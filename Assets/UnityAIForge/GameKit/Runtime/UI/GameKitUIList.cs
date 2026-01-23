using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UnityAIForge.GameKit
{
    /// <summary>
    /// GameKit UI List: Dynamic list/grid for displaying collections of items.
    /// Supports inventory display, skill lists, and custom data sources.
    /// </summary>
    [AddComponentMenu("UnityAIForge/GameKit/UI/UI List")]
    public class GameKitUIList : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string listId;

        [Header("Layout")]
        [SerializeField] private LayoutType layout = LayoutType.Vertical;
        [SerializeField] private int columns = 4;
        [SerializeField] private Vector2 cellSize = new Vector2(80, 80);
        [SerializeField] private Vector2 spacing = new Vector2(10, 10);
        [SerializeField] private int paddingLeft = 10;
        [SerializeField] private int paddingRight = 10;
        [SerializeField] private int paddingTop = 10;
        [SerializeField] private int paddingBottom = 10;

        // Non-serialized RectOffset created on demand
        private RectOffset _padding;

        [Header("Item Template")]
        [SerializeField] private GameObject itemPrefab;
        [SerializeField] private Transform itemContainer;

        [Header("Data Source")]
        [SerializeField] private DataSourceType dataSource = DataSourceType.Custom;
        [SerializeField] private string sourceId;

        [Header("Selection")]
        [SerializeField] private bool selectable = true;
        [SerializeField] private bool multiSelect = false;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color selectedColor = new Color(0.8f, 0.9f, 1f);
        [SerializeField] private Color hoverColor = new Color(0.9f, 0.95f, 1f);

        [Header("Events")]
        public UnityEvent<int, ListItemData> OnItemSelected = new UnityEvent<int, ListItemData>();
        public UnityEvent<int, ListItemData> OnItemDeselected = new UnityEvent<int, ListItemData>();
        public UnityEvent<int, ListItemData> OnItemClicked = new UnityEvent<int, ListItemData>();
        public UnityEvent<int, ListItemData> OnItemDoubleClicked = new UnityEvent<int, ListItemData>();
        public UnityEvent OnListUpdated = new UnityEvent();

        // Registry
        private static readonly Dictionary<string, GameKitUIList> _registry = new Dictionary<string, GameKitUIList>();

        // State
        private List<ListItemData> _items = new List<ListItemData>();
        private List<GameObject> _itemInstances = new List<GameObject>();
        private HashSet<int> _selectedIndices = new HashSet<int>();
        private LayoutGroup _layoutGroup;
        private bool _isInitialized;

        public string ListId => listId;
        public LayoutType Layout => layout;
        public DataSourceType Source => dataSource;
        public string SourceId => sourceId;
        public int ItemCount => _items.Count;
        public IReadOnlyList<ListItemData> Items => _items.AsReadOnly();
        public IReadOnlyCollection<int> SelectedIndices => _selectedIndices;

        public enum LayoutType
        {
            Vertical,
            Horizontal,
            Grid
        }

        public enum DataSourceType
        {
            Custom,
            Inventory,
            Equipment
        }

        [Serializable]
        public class ListItemData
        {
            public string id;
            public string name;
            public string description;
            public string iconPath;
            public Sprite icon;
            public int quantity = 1;
            public bool enabled = true;
            public Dictionary<string, object> customData = new Dictionary<string, object>();
        }

        public static GameKitUIList FindById(string id)
        {
            return _registry.TryGetValue(id, out var list) ? list : null;
        }

        private void Awake()
        {
            EnsureEventsInitialized();
        }

        private RectOffset Padding
        {
            get
            {
                if (_padding == null)
                {
                    _padding = new RectOffset(paddingLeft, paddingRight, paddingTop, paddingBottom);
                }
                return _padding;
            }
        }

        private void OnEnable()
        {
            if (!string.IsNullOrEmpty(listId))
            {
                _registry[listId] = this;
            }
            Initialize();
        }

        private void OnDisable()
        {
            if (!string.IsNullOrEmpty(listId))
            {
                _registry.Remove(listId);
            }
            UnsubscribeFromSource();
        }

        public void Initialize(string id, LayoutType layoutType, DataSourceType source, string srcId, GameObject prefab)
        {
            listId = id;
            layout = layoutType;
            dataSource = source;
            sourceId = srcId;
            itemPrefab = prefab;

            if (!string.IsNullOrEmpty(listId))
            {
                _registry[listId] = this;
            }

            Initialize();
        }

        private void Initialize()
        {
            if (_isInitialized) return;

            SetupContainer();
            SetupLayoutGroup();
            SubscribeToSource();
            RefreshFromSource();

            _isInitialized = true;
        }

        private void EnsureEventsInitialized()
        {
            OnItemSelected ??= new UnityEvent<int, ListItemData>();
            OnItemDeselected ??= new UnityEvent<int, ListItemData>();
            OnItemClicked ??= new UnityEvent<int, ListItemData>();
            OnItemDoubleClicked ??= new UnityEvent<int, ListItemData>();
            OnListUpdated ??= new UnityEvent();
        }

        private void SetupContainer()
        {
            if (itemContainer == null)
            {
                itemContainer = transform;
            }
        }

        private void SetupLayoutGroup()
        {
            // Remove existing layout groups
            var existingLayouts = GetComponents<LayoutGroup>();
            foreach (var lg in existingLayouts)
            {
                if (Application.isPlaying)
                    Destroy(lg);
                else
                    DestroyImmediate(lg);
            }

            switch (layout)
            {
                case LayoutType.Vertical:
                    var vlg = gameObject.AddComponent<VerticalLayoutGroup>();
                    vlg.spacing = spacing.y;
                    vlg.padding = Padding;
                    vlg.childAlignment = TextAnchor.UpperLeft;
                    vlg.childControlWidth = true;
                    vlg.childControlHeight = false;
                    vlg.childForceExpandWidth = true;
                    vlg.childForceExpandHeight = false;
                    _layoutGroup = vlg;
                    break;

                case LayoutType.Horizontal:
                    var hlg = gameObject.AddComponent<HorizontalLayoutGroup>();
                    hlg.spacing = spacing.x;
                    hlg.padding = Padding;
                    hlg.childAlignment = TextAnchor.MiddleLeft;
                    hlg.childControlWidth = false;
                    hlg.childControlHeight = true;
                    hlg.childForceExpandWidth = false;
                    hlg.childForceExpandHeight = true;
                    _layoutGroup = hlg;
                    break;

                case LayoutType.Grid:
                    var glg = gameObject.AddComponent<GridLayoutGroup>();
                    glg.cellSize = cellSize;
                    glg.spacing = spacing;
                    glg.padding = Padding;
                    glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                    glg.constraintCount = columns;
                    _layoutGroup = glg;
                    break;
            }
        }

        private void SubscribeToSource()
        {
            switch (dataSource)
            {
                case DataSourceType.Inventory:
                    var inventory = GameKitInventory.FindById(sourceId);
                    if (inventory != null)
                    {
                        inventory.OnInventoryChanged.AddListener(OnInventoryChanged);
                    }
                    break;
            }
        }

        private void UnsubscribeFromSource()
        {
            switch (dataSource)
            {
                case DataSourceType.Inventory:
                    var inventory = GameKitInventory.FindById(sourceId);
                    if (inventory != null)
                    {
                        inventory.OnInventoryChanged.RemoveListener(OnInventoryChanged);
                    }
                    break;
            }
        }

        private void OnInventoryChanged()
        {
            RefreshFromSource();
        }

        public void RefreshFromSource()
        {
            switch (dataSource)
            {
                case DataSourceType.Inventory:
                    RefreshFromInventory();
                    break;

                case DataSourceType.Equipment:
                    RefreshFromEquipment();
                    break;
            }
        }

        private void RefreshFromInventory()
        {
            var inventory = GameKitInventory.FindById(sourceId);
            if (inventory == null) return;

            var items = new List<ListItemData>();
            foreach (var slot in inventory.Slots)
            {
                if (!slot.IsEmpty && slot.ItemAsset != null)
                {
                    items.Add(new ListItemData
                    {
                        id = slot.ItemId,
                        name = slot.ItemAsset.DisplayName,
                        description = slot.ItemAsset.Description,
                        icon = slot.ItemAsset.Icon,
                        quantity = slot.Quantity,
                        enabled = true
                    });
                }
            }

            SetItems(items);
        }

        private void RefreshFromEquipment()
        {
            var inventory = GameKitInventory.FindById(sourceId);
            if (inventory == null) return;

            var items = new List<ListItemData>();
            foreach (var equipped in inventory.EquippedItems)
            {
                if (equipped.ItemAsset != null)
                {
                    items.Add(new ListItemData
                    {
                        id = equipped.ItemId,
                        name = equipped.ItemAsset.DisplayName,
                        description = equipped.ItemAsset.Description,
                        icon = equipped.ItemAsset.Icon,
                        quantity = 1,
                        enabled = true,
                        customData = new Dictionary<string, object> { { "equipSlot", equipped.EquipSlot } }
                    });
                }
            }

            SetItems(items);
        }

        /// <summary>
        /// Set items from custom data.
        /// </summary>
        public void SetItems(List<ListItemData> items)
        {
            _items = items ?? new List<ListItemData>();
            _selectedIndices.Clear();
            RebuildList();
            OnListUpdated?.Invoke();
        }

        /// <summary>
        /// Add a single item to the list.
        /// </summary>
        public void AddItem(ListItemData item)
        {
            _items.Add(item);
            CreateItemInstance(_items.Count - 1, item);
            OnListUpdated?.Invoke();
        }

        /// <summary>
        /// Remove item at index.
        /// </summary>
        public void RemoveItemAt(int index)
        {
            if (index < 0 || index >= _items.Count) return;

            _items.RemoveAt(index);
            _selectedIndices.Remove(index);

            // Adjust selected indices
            var newSelected = new HashSet<int>();
            foreach (var i in _selectedIndices)
            {
                if (i > index) newSelected.Add(i - 1);
                else newSelected.Add(i);
            }
            _selectedIndices = newSelected;

            RebuildList();
            OnListUpdated?.Invoke();
        }

        /// <summary>
        /// Clear all items.
        /// </summary>
        public void Clear()
        {
            _items.Clear();
            _selectedIndices.Clear();
            ClearInstances();
            OnListUpdated?.Invoke();
        }

        private void RebuildList()
        {
            ClearInstances();

            for (int i = 0; i < _items.Count; i++)
            {
                CreateItemInstance(i, _items[i]);
            }
        }

        private void ClearInstances()
        {
            foreach (var instance in _itemInstances)
            {
                if (instance != null)
                {
                    if (Application.isPlaying)
                        Destroy(instance);
                    else
                        DestroyImmediate(instance);
                }
            }
            _itemInstances.Clear();
        }

        private void CreateItemInstance(int index, ListItemData data)
        {
            if (itemPrefab == null) return;

            var instance = Instantiate(itemPrefab, itemContainer);
            instance.name = $"Item_{index}_{data.id}";
            _itemInstances.Add(instance);

            // Setup item visuals
            SetupItemVisuals(instance, data);

            // Setup interaction
            SetupItemInteraction(instance, index);
        }

        private void SetupItemVisuals(GameObject instance, ListItemData data)
        {
            // Find and set icon
            var iconImage = instance.GetComponentInChildren<Image>();
            if (iconImage != null && data.icon != null)
            {
                iconImage.sprite = data.icon;
            }

            // Find and set name text
            var texts = instance.GetComponentsInChildren<Text>();
            foreach (var text in texts)
            {
                if (text.gameObject.name.ToLower().Contains("name"))
                {
                    text.text = data.name;
                }
                else if (text.gameObject.name.ToLower().Contains("quantity") || text.gameObject.name.ToLower().Contains("count"))
                {
                    text.text = data.quantity > 1 ? data.quantity.ToString() : "";
                }
                else if (text.gameObject.name.ToLower().Contains("desc"))
                {
                    text.text = data.description;
                }
            }

#if TMP_PRESENT
            var tmpTexts = instance.GetComponentsInChildren<TMPro.TMP_Text>();
            foreach (var text in tmpTexts)
            {
                if (text.gameObject.name.ToLower().Contains("name"))
                {
                    text.text = data.name;
                }
                else if (text.gameObject.name.ToLower().Contains("quantity") || text.gameObject.name.ToLower().Contains("count"))
                {
                    text.text = data.quantity > 1 ? data.quantity.ToString() : "";
                }
                else if (text.gameObject.name.ToLower().Contains("desc"))
                {
                    text.text = data.description;
                }
            }
#endif
        }

        private void SetupItemInteraction(GameObject instance, int index)
        {
            if (!selectable) return;

            var button = instance.GetComponent<Button>();
            if (button == null)
            {
                button = instance.AddComponent<Button>();
            }

            int capturedIndex = index;
            button.onClick.AddListener(() => OnItemClick(capturedIndex));

            // Setup colors
            var colors = button.colors;
            colors.normalColor = normalColor;
            colors.highlightedColor = hoverColor;
            colors.selectedColor = selectedColor;
            button.colors = colors;
        }

        private void OnItemClick(int index)
        {
            if (index < 0 || index >= _items.Count) return;

            var item = _items[index];
            OnItemClicked?.Invoke(index, item);

            if (selectable)
            {
                if (_selectedIndices.Contains(index))
                {
                    // Deselect
                    _selectedIndices.Remove(index);
                    UpdateItemSelection(index, false);
                    OnItemDeselected?.Invoke(index, item);
                }
                else
                {
                    // Select
                    if (!multiSelect)
                    {
                        // Clear previous selection
                        foreach (var i in _selectedIndices)
                        {
                            UpdateItemSelection(i, false);
                            if (i < _items.Count)
                                OnItemDeselected?.Invoke(i, _items[i]);
                        }
                        _selectedIndices.Clear();
                    }

                    _selectedIndices.Add(index);
                    UpdateItemSelection(index, true);
                    OnItemSelected?.Invoke(index, item);
                }
            }
        }

        private void UpdateItemSelection(int index, bool selected)
        {
            if (index < 0 || index >= _itemInstances.Count) return;

            var instance = _itemInstances[index];
            if (instance == null) return;

            var image = instance.GetComponent<Image>();
            if (image != null)
            {
                image.color = selected ? selectedColor : normalColor;
            }
        }

        /// <summary>
        /// Select item at index programmatically.
        /// </summary>
        public void SelectItem(int index)
        {
            if (!selectable || index < 0 || index >= _items.Count) return;

            if (!multiSelect)
            {
                foreach (var i in _selectedIndices)
                {
                    UpdateItemSelection(i, false);
                }
                _selectedIndices.Clear();
            }

            _selectedIndices.Add(index);
            UpdateItemSelection(index, true);
            OnItemSelected?.Invoke(index, _items[index]);
        }

        /// <summary>
        /// Deselect item at index.
        /// </summary>
        public void DeselectItem(int index)
        {
            if (!_selectedIndices.Contains(index)) return;

            _selectedIndices.Remove(index);
            UpdateItemSelection(index, false);

            if (index < _items.Count)
                OnItemDeselected?.Invoke(index, _items[index]);
        }

        /// <summary>
        /// Clear all selections.
        /// </summary>
        public void ClearSelection()
        {
            foreach (var index in _selectedIndices)
            {
                UpdateItemSelection(index, false);
            }
            _selectedIndices.Clear();
        }

        /// <summary>
        /// Get selected items.
        /// </summary>
        public List<ListItemData> GetSelectedItems()
        {
            var result = new List<ListItemData>();
            foreach (var index in _selectedIndices)
            {
                if (index < _items.Count)
                    result.Add(_items[index]);
            }
            return result;
        }

        /// <summary>
        /// Get item at index.
        /// </summary>
        public ListItemData GetItem(int index)
        {
            return index >= 0 && index < _items.Count ? _items[index] : null;
        }

        /// <summary>
        /// Find item by ID.
        /// </summary>
        public int FindItemIndex(string itemId)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].id == itemId)
                    return i;
            }
            return -1;
        }
    }
}
