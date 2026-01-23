using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
#if TMP_PRESENT
using TMPro;
#endif

namespace UnityAIForge.GameKit
{
    /// <summary>
    /// GameKit UI Binding: Declarative UI data binding system.
    /// Automatically updates UI elements when source data changes.
    /// Supports health, economy, timer, and custom data sources.
    /// </summary>
    [AddComponentMenu("UnityAIForge/GameKit/UI/UI Binding")]
    public class GameKitUIBinding : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string bindingId;

        [Header("Source Configuration")]
        [SerializeField] private SourceType sourceType = SourceType.Health;
        [SerializeField] private string sourceId;

        [Header("Target Configuration")]
        [SerializeField] private GameObject targetObject;
        [SerializeField] private string targetComponentType;
        [SerializeField] private string targetProperty;

        [Header("Value Format")]
        [SerializeField] private ValueFormat format = ValueFormat.Raw;
        [SerializeField] private string formatString = "{0}";
        [SerializeField] private float minValue = 0f;
        [SerializeField] private float maxValue = 100f;

        [Header("Update Settings")]
        [SerializeField] private float updateInterval = 0.1f;
        [SerializeField] private bool smoothTransition = false;
        [SerializeField] private float smoothSpeed = 5f;

        [Header("Events")]
        public UnityEvent<float> OnValueChanged = new UnityEvent<float>();
        public UnityEvent<float> OnPercentChanged = new UnityEvent<float>();
        public UnityEvent<string> OnFormattedValueChanged = new UnityEvent<string>();

        // Registry
        private static readonly Dictionary<string, GameKitUIBinding> _registry = new Dictionary<string, GameKitUIBinding>();

        // Cached components
        private Slider _slider;
        private Image _image;
        private Text _text;
#if TMP_PRESENT
        private TMPro.TMP_Text _tmpText;
#endif
        private Component _customComponent;

        // State
        private float _currentValue;
        private float _targetValue;
        private float _updateTimer;
        private bool _isInitialized;

        public string BindingId => bindingId;
        public SourceType Source => sourceType;
        public string SourceId => sourceId;
        public float CurrentValue => _currentValue;
        public float CurrentPercent => maxValue > minValue ? (_currentValue - minValue) / (maxValue - minValue) : 0f;

        /// <summary>
        /// Data source types for binding.
        /// </summary>
        public enum SourceType
        {
            Health,     // GameKitHealth component
            Economy,    // GameKitManager resource
            Timer,      // GameKitTimer countdown
            Custom      // Manual value updates
        }

        /// <summary>
        /// Value format for display.
        /// </summary>
        public enum ValueFormat
        {
            Raw,        // Direct value (e.g., 75)
            Percent,    // Percentage (e.g., 75%)
            Formatted,  // Custom format string
            Ratio       // Current/Max (e.g., 75/100)
        }

        /// <summary>
        /// Find binding by ID.
        /// </summary>
        public static GameKitUIBinding FindById(string id)
        {
            return _registry.TryGetValue(id, out var binding) ? binding : null;
        }

        private void Awake()
        {
            EnsureEventsInitialized();
        }

        private void OnEnable()
        {
            if (!string.IsNullOrEmpty(bindingId))
            {
                _registry[bindingId] = this;
            }
            Initialize();
        }

        private void OnDisable()
        {
            if (!string.IsNullOrEmpty(bindingId))
            {
                _registry.Remove(bindingId);
            }
            UnsubscribeFromSource();
        }

        private void Update()
        {
            // Periodic polling for sources that don't have events
            _updateTimer += Time.deltaTime;
            if (_updateTimer >= updateInterval)
            {
                _updateTimer = 0f;
                PollSourceValue();
            }

            // Smooth transition
            if (smoothTransition && !Mathf.Approximately(_currentValue, _targetValue))
            {
                _currentValue = Mathf.Lerp(_currentValue, _targetValue, Time.deltaTime * smoothSpeed);
                ApplyValueToTarget();
            }
        }

        /// <summary>
        /// Initialize the binding with specified configuration.
        /// </summary>
        public void Initialize(string id, SourceType type, string srcId, GameObject target, string componentType, string property)
        {
            bindingId = id;
            sourceType = type;
            sourceId = srcId;
            targetObject = target;
            targetComponentType = componentType;
            targetProperty = property;

            if (!string.IsNullOrEmpty(bindingId))
            {
                _registry[bindingId] = this;
            }

            Initialize();
        }

        private void Initialize()
        {
            if (_isInitialized) return;

            CacheTargetComponent();
            SubscribeToSource();
            PollSourceValue();

            _isInitialized = true;
        }

        private void EnsureEventsInitialized()
        {
            OnValueChanged ??= new UnityEvent<float>();
            OnPercentChanged ??= new UnityEvent<float>();
            OnFormattedValueChanged ??= new UnityEvent<string>();
        }

        private void CacheTargetComponent()
        {
            if (targetObject == null) return;

            // Try common UI components first
            _slider = targetObject.GetComponent<Slider>();
            _image = targetObject.GetComponent<Image>();
            _text = targetObject.GetComponent<Text>();
#if TMP_PRESENT
            _tmpText = targetObject.GetComponent<TMPro.TMP_Text>();
#endif

            // Try custom component type
            if (!string.IsNullOrEmpty(targetComponentType))
            {
                var type = Type.GetType(targetComponentType);
                if (type != null)
                {
                    _customComponent = targetObject.GetComponent(type);
                }
            }
        }

        private void SubscribeToSource()
        {
            switch (sourceType)
            {
                case SourceType.Health:
                    var health = GameKitHealth.FindById(sourceId);
                    if (health != null)
                    {
                        health.OnHealthChanged.AddListener(OnHealthChanged);
                        minValue = 0f;
                        maxValue = health.MaxHealth;
                    }
                    break;

                case SourceType.Economy:
                    // GameKitManager resource events would be subscribed here
                    // For now, relies on polling
                    break;

                case SourceType.Timer:
                    // GameKitTimer events would be subscribed here
                    break;
            }
        }

        private void UnsubscribeFromSource()
        {
            switch (sourceType)
            {
                case SourceType.Health:
                    var health = GameKitHealth.FindById(sourceId);
                    if (health != null)
                    {
                        health.OnHealthChanged.RemoveListener(OnHealthChanged);
                    }
                    break;
            }
        }

        private void OnHealthChanged(float current, float max)
        {
            maxValue = max;
            SetValue(current);
        }

        private void PollSourceValue()
        {
            float value = 0f;

            switch (sourceType)
            {
                case SourceType.Health:
                    var health = GameKitHealth.FindById(sourceId);
                    if (health != null)
                    {
                        value = health.CurrentHealth;
                        maxValue = health.MaxHealth;
                    }
                    break;

                case SourceType.Economy:
                    var manager = FindManagerById(sourceId);
                    if (manager != null && !string.IsNullOrEmpty(targetProperty))
                    {
                        value = manager.GetResource(targetProperty);
                    }
                    break;

                case SourceType.Timer:
                    // Poll timer value
                    break;

                case SourceType.Custom:
                    // Custom source uses SetValue directly
                    return;
            }

            SetValue(value);
        }

        private GameKitManager FindManagerById(string managerId)
        {
            if (string.IsNullOrEmpty(managerId)) return null;

            var managers = FindObjectsByType<GameKitManager>(FindObjectsSortMode.None);
            foreach (var manager in managers)
            {
                if (manager.ManagerId == managerId)
                {
                    return manager;
                }
            }
            return null;
        }

        /// <summary>
        /// Set the binding value directly (for custom sources).
        /// </summary>
        public void SetValue(float value)
        {
            _targetValue = value;

            if (!smoothTransition)
            {
                _currentValue = value;
                ApplyValueToTarget();
            }

            OnValueChanged?.Invoke(value);
            OnPercentChanged?.Invoke(CurrentPercent);
            OnFormattedValueChanged?.Invoke(GetFormattedValue());
        }

        /// <summary>
        /// Set min/max range for the binding.
        /// </summary>
        public void SetRange(float min, float max)
        {
            minValue = min;
            maxValue = max;
            ApplyValueToTarget();
        }

        private void ApplyValueToTarget()
        {
            if (targetObject == null) return;

            float percent = CurrentPercent;
            string formatted = GetFormattedValue();

            // Apply to Slider
            if (_slider != null)
            {
                _slider.minValue = minValue;
                _slider.maxValue = maxValue;
                _slider.value = _currentValue;
            }

            // Apply to Image (fill amount)
            if (_image != null && _image.type == Image.Type.Filled)
            {
                _image.fillAmount = percent;
            }

            // Apply to Text
            if (_text != null)
            {
                _text.text = formatted;
            }

            // Apply to TMP_Text
#if TMP_PRESENT
            if (_tmpText != null)
            {
                _tmpText.text = formatted;
            }
#endif

            // Apply to custom component property via reflection
            if (_customComponent != null && !string.IsNullOrEmpty(targetProperty))
            {
                var prop = _customComponent.GetType().GetProperty(targetProperty);
                if (prop != null && prop.CanWrite)
                {
                    if (prop.PropertyType == typeof(float))
                    {
                        prop.SetValue(_customComponent, _currentValue);
                    }
                    else if (prop.PropertyType == typeof(string))
                    {
                        prop.SetValue(_customComponent, formatted);
                    }
                }
            }
        }

        /// <summary>
        /// Get the formatted value string based on format settings.
        /// </summary>
        public string GetFormattedValue()
        {
            return format switch
            {
                ValueFormat.Raw => _currentValue.ToString("F0"),
                ValueFormat.Percent => $"{CurrentPercent * 100:F0}%",
                ValueFormat.Ratio => $"{_currentValue:F0}/{maxValue:F0}",
                ValueFormat.Formatted => string.Format(formatString, _currentValue, maxValue, CurrentPercent * 100),
                _ => _currentValue.ToString()
            };
        }

        /// <summary>
        /// Force refresh the binding value from source.
        /// </summary>
        public void Refresh()
        {
            PollSourceValue();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (targetObject != null && !_isInitialized)
            {
                CacheTargetComponent();
            }
        }
#endif
    }
}
