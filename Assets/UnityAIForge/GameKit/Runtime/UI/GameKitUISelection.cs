using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UnityAIForge.GameKit
{
    /// <summary>
    /// GameKit UI Selection: Selection group management for toggles, radio buttons, and tabs.
    /// Supports single and multi-selection with visual state management.
    /// </summary>
    [AddComponentMenu("UnityAIForge/GameKit/UI/UI Selection")]
    public class GameKitUISelection : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string selectionId;

        [Header("Configuration")]
        [SerializeField] private SelectionType selectionType = SelectionType.Radio;
        [SerializeField] private bool allowNone = false;
        [SerializeField] private int defaultIndex = 0;

        [Header("Layout")]
        [SerializeField] private LayoutType layout = LayoutType.Horizontal;
        [SerializeField] private float spacing = 10f;
        [SerializeField] private int paddingLeft = 10;
        [SerializeField] private int paddingRight = 10;
        [SerializeField] private int paddingTop = 10;
        [SerializeField] private int paddingBottom = 10;

        // Non-serialized RectOffset created on demand
        private RectOffset _padding;

        [Header("Item Template")]
        [SerializeField] private GameObject itemPrefab;
        [SerializeField] private Transform itemContainer;

        [Header("Visual States")]
        [SerializeField] private Color normalColor = new Color(0.8f, 0.8f, 0.8f);
        [SerializeField] private Color selectedColor = new Color(0.3f, 0.6f, 1f);
        [SerializeField] private Color hoverColor = new Color(0.9f, 0.9f, 0.9f);
        [SerializeField] private Color disabledColor = new Color(0.5f, 0.5f, 0.5f);

        [Header("Events")]
        public UnityEvent<string, int> OnSelectionChanged = new UnityEvent<string, int>();
        public UnityEvent<string> OnItemSelected = new UnityEvent<string>();
        public UnityEvent<string> OnItemDeselected = new UnityEvent<string>();
        public UnityEvent<List<string>> OnMultiSelectionChanged = new UnityEvent<List<string>>();

        // Registry
        private static readonly Dictionary<string, GameKitUISelection> _registry = new Dictionary<string, GameKitUISelection>();

        // State
        private List<SelectionItem> _items = new List<SelectionItem>();
        private List<GameObject> _itemInstances = new List<GameObject>();
        private HashSet<int> _selectedIndices = new HashSet<int>();
        private List<SelectionAction> _selectionActions = new List<SelectionAction>();
        private LayoutGroup _layoutGroup;
        private bool _isInitialized;

        public string SelectionId => selectionId;
        public SelectionType Type => selectionType;
        public int ItemCount => _items.Count;
        public IReadOnlyList<SelectionItem> Items => _items.AsReadOnly();
        public IReadOnlyCollection<int> SelectedIndices => _selectedIndices;

        public enum SelectionType
        {
            Radio,      // Single selection (like radio buttons)
            Toggle,     // Single selection with toggle off
            Checkbox,   // Multiple selection
            Tab         // Tab-style selection with panels
        }

        public enum LayoutType
        {
            Horizontal,
            Vertical,
            Grid
        }

        [Serializable]
        public class SelectionItem
        {
            public string id;
            public string label;
            public Sprite icon;
            public bool enabled = true;
            public bool defaultSelected = false;
            public GameObject associatedPanel;
        }

        [Serializable]
        public class SelectionAction
        {
            public string selectedId;
            public List<string> showPaths = new List<string>();
            public List<string> hidePaths = new List<string>();
        }

        public static GameKitUISelection FindById(string id)
        {
            return _registry.TryGetValue(id, out var selection) ? selection : null;
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
            if (!string.IsNullOrEmpty(selectionId))
            {
                _registry[selectionId] = this;
            }
            Initialize();
        }

        private void OnDisable()
        {
            if (!string.IsNullOrEmpty(selectionId))
            {
                _registry.Remove(selectionId);
            }
        }

        public void Initialize(string id, SelectionType type, List<SelectionItem> items, bool noneAllowed = false)
        {
            selectionId = id;
            selectionType = type;
            allowNone = noneAllowed;

            if (!string.IsNullOrEmpty(selectionId))
            {
                _registry[selectionId] = this;
            }

            SetItems(items);
        }

        private void Initialize()
        {
            if (_isInitialized) return;

            SetupContainer();
            SetupLayoutGroup();

            // Apply default selection
            if (_items.Count > 0 && !allowNone && _selectedIndices.Count == 0)
            {
                // Find default or use first
                int defaultIdx = -1;
                for (int i = 0; i < _items.Count; i++)
                {
                    if (_items[i].defaultSelected)
                    {
                        defaultIdx = i;
                        break;
                    }
                }

                if (defaultIdx >= 0)
                    SelectItem(defaultIdx, false);
                else if (defaultIndex >= 0 && defaultIndex < _items.Count)
                    SelectItem(defaultIndex, false);
            }

            _isInitialized = true;
        }

        private void EnsureEventsInitialized()
        {
            OnSelectionChanged ??= new UnityEvent<string, int>();
            OnItemSelected ??= new UnityEvent<string>();
            OnItemDeselected ??= new UnityEvent<string>();
            OnMultiSelectionChanged ??= new UnityEvent<List<string>>();
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
                case LayoutType.Horizontal:
                    var hlg = gameObject.AddComponent<HorizontalLayoutGroup>();
                    hlg.spacing = spacing;
                    hlg.padding = Padding;
                    hlg.childAlignment = TextAnchor.MiddleLeft;
                    hlg.childControlWidth = false;
                    hlg.childControlHeight = true;
                    _layoutGroup = hlg;
                    break;

                case LayoutType.Vertical:
                    var vlg = gameObject.AddComponent<VerticalLayoutGroup>();
                    vlg.spacing = spacing;
                    vlg.padding = Padding;
                    vlg.childAlignment = TextAnchor.UpperLeft;
                    vlg.childControlWidth = true;
                    vlg.childControlHeight = false;
                    _layoutGroup = vlg;
                    break;

                case LayoutType.Grid:
                    var glg = gameObject.AddComponent<GridLayoutGroup>();
                    glg.spacing = new Vector2(spacing, spacing);
                    glg.padding = Padding;
                    _layoutGroup = glg;
                    break;
            }
        }

        /// <summary>
        /// Set items for the selection group.
        /// </summary>
        public void SetItems(List<SelectionItem> items)
        {
            _items = items ?? new List<SelectionItem>();
            _selectedIndices.Clear();
            RebuildItems();

            // Apply default selections
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].defaultSelected)
                {
                    SelectItem(i, false);
                }
            }
        }

        /// <summary>
        /// Add a single item.
        /// </summary>
        public void AddItem(SelectionItem item)
        {
            _items.Add(item);
            CreateItemInstance(_items.Count - 1, item);
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

            RebuildItems();
        }

        /// <summary>
        /// Clear all items.
        /// </summary>
        public void Clear()
        {
            _items.Clear();
            _selectedIndices.Clear();
            ClearInstances();
        }

        private void RebuildItems()
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

        private void CreateItemInstance(int index, SelectionItem item)
        {
            GameObject instance;

            if (itemPrefab != null)
            {
                instance = Instantiate(itemPrefab, itemContainer);
            }
            else
            {
                instance = CreateDefaultItem(item);
            }

            instance.name = $"Item_{index}_{item.id}";
            _itemInstances.Add(instance);

            // Setup visuals
            SetupItemVisuals(instance, item);

            // Setup interaction
            SetupItemInteraction(instance, index);

            // Update visual state
            UpdateItemVisualState(index, _selectedIndices.Contains(index));
        }

        private GameObject CreateDefaultItem(SelectionItem item)
        {
            var itemObj = new GameObject("SelectionItem");
            itemObj.transform.SetParent(itemContainer, false);

            // Background
            var image = itemObj.AddComponent<Image>();
            image.color = normalColor;

            // Layout
            var layout = itemObj.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 5, 5);
            layout.spacing = 5;
            layout.childAlignment = TextAnchor.MiddleLeft;

            // Size
            var rect = itemObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(120, 40);
            var fitter = itemObj.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Checkbox/indicator
            var checkObj = new GameObject("Check");
            checkObj.transform.SetParent(itemObj.transform, false);
            var checkImage = checkObj.AddComponent<Image>();
            checkImage.color = selectedColor;
            var checkRect = checkObj.GetComponent<RectTransform>();
            checkRect.sizeDelta = new Vector2(20, 20);
            var checkLayout = checkObj.AddComponent<LayoutElement>();
            checkLayout.minWidth = 20;
            checkLayout.minHeight = 20;

            // Label
            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(itemObj.transform, false);
            var labelText = labelObj.AddComponent<Text>();
            labelText.text = item.label;
            labelText.color = Color.black;
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 14;
            labelText.alignment = TextAnchor.MiddleLeft;
            var labelLayout = labelObj.AddComponent<LayoutElement>();
            labelLayout.flexibleWidth = 1;

            return itemObj;
        }

        private void SetupItemVisuals(GameObject instance, SelectionItem item)
        {
            // Find and set label
            var texts = instance.GetComponentsInChildren<Text>();
            foreach (var text in texts)
            {
                if (text.gameObject.name.ToLower().Contains("label") || text.gameObject.name.ToLower().Contains("text"))
                {
                    text.text = item.label;
                }
            }

#if TMP_PRESENT
            var tmpTexts = instance.GetComponentsInChildren<TMPro.TMP_Text>();
            foreach (var text in tmpTexts)
            {
                if (text.gameObject.name.ToLower().Contains("label") || text.gameObject.name.ToLower().Contains("text"))
                {
                    text.text = item.label;
                }
            }
#endif

            // Find and set icon
            if (item.icon != null)
            {
                var images = instance.GetComponentsInChildren<Image>();
                foreach (var img in images)
                {
                    if (img.gameObject.name.ToLower().Contains("icon"))
                    {
                        img.sprite = item.icon;
                        img.enabled = true;
                    }
                }
            }
        }

        private void SetupItemInteraction(GameObject instance, int index)
        {
            var button = instance.GetComponent<Button>();
            if (button == null)
            {
                button = instance.AddComponent<Button>();
            }

            int capturedIndex = index;
            button.onClick.AddListener(() => OnItemClick(capturedIndex));

            // Disable if item is disabled
            if (index < _items.Count && !_items[index].enabled)
            {
                button.interactable = false;
            }
        }

        private void OnItemClick(int index)
        {
            if (index < 0 || index >= _items.Count) return;

            var item = _items[index];
            if (!item.enabled) return;

            bool wasSelected = _selectedIndices.Contains(index);

            switch (selectionType)
            {
                case SelectionType.Radio:
                    if (!wasSelected)
                    {
                        // Deselect all others
                        foreach (var i in _selectedIndices)
                        {
                            UpdateItemVisualState(i, false);
                            if (i < _items.Count)
                                OnItemDeselected?.Invoke(_items[i].id);
                        }
                        _selectedIndices.Clear();

                        // Select this one
                        _selectedIndices.Add(index);
                        UpdateItemVisualState(index, true);
                        OnItemSelected?.Invoke(item.id);
                        OnSelectionChanged?.Invoke(item.id, index);
                    }
                    break;

                case SelectionType.Toggle:
                    if (wasSelected && allowNone)
                    {
                        // Deselect
                        _selectedIndices.Remove(index);
                        UpdateItemVisualState(index, false);
                        OnItemDeselected?.Invoke(item.id);
                        OnSelectionChanged?.Invoke("", -1);
                    }
                    else if (!wasSelected)
                    {
                        // Deselect all others
                        foreach (var i in _selectedIndices)
                        {
                            UpdateItemVisualState(i, false);
                            if (i < _items.Count)
                                OnItemDeselected?.Invoke(_items[i].id);
                        }
                        _selectedIndices.Clear();

                        // Select this one
                        _selectedIndices.Add(index);
                        UpdateItemVisualState(index, true);
                        OnItemSelected?.Invoke(item.id);
                        OnSelectionChanged?.Invoke(item.id, index);
                    }
                    break;

                case SelectionType.Checkbox:
                    if (wasSelected)
                    {
                        if (allowNone || _selectedIndices.Count > 1)
                        {
                            _selectedIndices.Remove(index);
                            UpdateItemVisualState(index, false);
                            OnItemDeselected?.Invoke(item.id);
                        }
                    }
                    else
                    {
                        _selectedIndices.Add(index);
                        UpdateItemVisualState(index, true);
                        OnItemSelected?.Invoke(item.id);
                    }
                    OnMultiSelectionChanged?.Invoke(GetSelectedIds());
                    break;

                case SelectionType.Tab:
                    if (!wasSelected)
                    {
                        // Hide previous panels
                        foreach (var i in _selectedIndices)
                        {
                            if (i < _items.Count && _items[i].associatedPanel != null)
                            {
                                _items[i].associatedPanel.SetActive(false);
                            }
                            UpdateItemVisualState(i, false);
                            if (i < _items.Count)
                                OnItemDeselected?.Invoke(_items[i].id);
                        }
                        _selectedIndices.Clear();

                        // Select and show new panel
                        _selectedIndices.Add(index);
                        UpdateItemVisualState(index, true);
                        if (item.associatedPanel != null)
                        {
                            item.associatedPanel.SetActive(true);
                        }
                        OnItemSelected?.Invoke(item.id);
                        OnSelectionChanged?.Invoke(item.id, index);
                    }
                    break;
            }

            // Execute selection actions
            ExecuteSelectionActions(item.id);
        }

        private void UpdateItemVisualState(int index, bool selected)
        {
            if (index < 0 || index >= _itemInstances.Count) return;

            var instance = _itemInstances[index];
            if (instance == null) return;

            // Update background color
            var bgImage = instance.GetComponent<Image>();
            if (bgImage != null)
            {
                bgImage.color = selected ? selectedColor : normalColor;
            }

            // Update check indicator
            var checkTransform = instance.transform.Find("Check");
            if (checkTransform != null)
            {
                checkTransform.gameObject.SetActive(selected);
            }

            // Update toggle component if exists
            var toggle = instance.GetComponent<Toggle>();
            if (toggle != null)
            {
                toggle.isOn = selected;
            }
        }

        /// <summary>
        /// Set selection actions for automatic show/hide.
        /// </summary>
        public void SetSelectionActions(List<SelectionAction> actions)
        {
            _selectionActions = actions ?? new List<SelectionAction>();
        }

        private void ExecuteSelectionActions(string selectedId)
        {
            foreach (var action in _selectionActions)
            {
                if (action.selectedId == selectedId)
                {
                    // Show specified objects
                    foreach (var path in action.showPaths)
                    {
                        var obj = GameObject.Find(path);
                        if (obj != null)
                            obj.SetActive(true);
                    }

                    // Hide specified objects
                    foreach (var path in action.hidePaths)
                    {
                        var obj = GameObject.Find(path);
                        if (obj != null)
                            obj.SetActive(false);
                    }
                }
            }
        }

        /// <summary>
        /// Select item at index programmatically.
        /// </summary>
        public void SelectItem(int index, bool fireEvents = true)
        {
            if (index < 0 || index >= _items.Count) return;

            var item = _items[index];

            // Handle based on type
            if (selectionType == SelectionType.Checkbox)
            {
                if (!_selectedIndices.Contains(index))
                {
                    _selectedIndices.Add(index);
                    UpdateItemVisualState(index, true);
                    if (fireEvents)
                    {
                        OnItemSelected?.Invoke(item.id);
                        OnMultiSelectionChanged?.Invoke(GetSelectedIds());
                    }
                }
            }
            else
            {
                // Clear previous
                foreach (var i in _selectedIndices)
                {
                    UpdateItemVisualState(i, false);
                    if (fireEvents && i < _items.Count)
                        OnItemDeselected?.Invoke(_items[i].id);

                    if (selectionType == SelectionType.Tab && i < _items.Count && _items[i].associatedPanel != null)
                    {
                        _items[i].associatedPanel.SetActive(false);
                    }
                }
                _selectedIndices.Clear();

                // Select new
                _selectedIndices.Add(index);
                UpdateItemVisualState(index, true);

                if (selectionType == SelectionType.Tab && item.associatedPanel != null)
                {
                    item.associatedPanel.SetActive(true);
                }

                if (fireEvents)
                {
                    OnItemSelected?.Invoke(item.id);
                    OnSelectionChanged?.Invoke(item.id, index);
                }
            }

            if (fireEvents)
                ExecuteSelectionActions(item.id);
        }

        /// <summary>
        /// Select item by ID.
        /// </summary>
        public void SelectItemById(string itemId, bool fireEvents = true)
        {
            int index = FindItemIndex(itemId);
            if (index >= 0)
            {
                SelectItem(index, fireEvents);
            }
        }

        /// <summary>
        /// Deselect item at index.
        /// </summary>
        public void DeselectItem(int index, bool fireEvents = true)
        {
            if (!_selectedIndices.Contains(index)) return;

            if (!allowNone && _selectedIndices.Count <= 1 && selectionType != SelectionType.Checkbox)
            {
                return; // Can't deselect last item
            }

            _selectedIndices.Remove(index);
            UpdateItemVisualState(index, false);

            if (selectionType == SelectionType.Tab && index < _items.Count && _items[index].associatedPanel != null)
            {
                _items[index].associatedPanel.SetActive(false);
            }

            if (fireEvents && index < _items.Count)
            {
                OnItemDeselected?.Invoke(_items[index].id);
                if (selectionType == SelectionType.Checkbox)
                {
                    OnMultiSelectionChanged?.Invoke(GetSelectedIds());
                }
                else
                {
                    OnSelectionChanged?.Invoke("", -1);
                }
            }
        }

        /// <summary>
        /// Clear all selections.
        /// </summary>
        public void ClearSelection()
        {
            if (!allowNone && selectionType != SelectionType.Checkbox) return;

            foreach (var index in _selectedIndices)
            {
                UpdateItemVisualState(index, false);
            }
            _selectedIndices.Clear();
        }

        /// <summary>
        /// Get currently selected item IDs.
        /// </summary>
        public List<string> GetSelectedIds()
        {
            var result = new List<string>();
            foreach (var index in _selectedIndices)
            {
                if (index < _items.Count)
                    result.Add(_items[index].id);
            }
            return result;
        }

        /// <summary>
        /// Get first selected item ID (for single-select types).
        /// </summary>
        public string GetSelectedId()
        {
            foreach (var index in _selectedIndices)
            {
                if (index < _items.Count)
                    return _items[index].id;
            }
            return null;
        }

        /// <summary>
        /// Get first selected index.
        /// </summary>
        public int GetSelectedIndex()
        {
            foreach (var index in _selectedIndices)
            {
                return index;
            }
            return -1;
        }

        /// <summary>
        /// Find item index by ID.
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

        /// <summary>
        /// Set item enabled state.
        /// </summary>
        public void SetItemEnabled(int index, bool enabled)
        {
            if (index < 0 || index >= _items.Count) return;

            _items[index].enabled = enabled;

            if (index < _itemInstances.Count)
            {
                var button = _itemInstances[index].GetComponent<Button>();
                if (button != null)
                {
                    button.interactable = enabled;
                }

                var image = _itemInstances[index].GetComponent<Image>();
                if (image != null && !enabled)
                {
                    image.color = disabledColor;
                }
            }
        }
    }
}
