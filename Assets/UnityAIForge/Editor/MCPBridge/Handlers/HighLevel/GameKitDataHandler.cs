using System;
using System.Collections.Generic;
using System.Text;
using MCP.Editor.Base;
using MCP.Editor.CodeGen;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor.Handlers.HighLevel
{
    /// <summary>
    /// Unified GameKit Data handler: routes by <c>dataType</c> (pool / eventChannel / dataContainer / runtimeSet).
    /// Pool operations are delegated to <see cref="GameKitPoolHandler"/>.
    /// </summary>
    public class GameKitDataHandler : BaseCommandHandler
    {
        private static readonly string[] Operations =
        {
            "create", "inspect", "find"
        };

        private readonly GameKitPoolHandler _poolHandler = new();

        public override string Category => "gamekitData";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) =>
            operation == "create";

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            var dataType = GetString(payload, "dataType");
            if (string.IsNullOrEmpty(dataType))
                return CreateFailureResponse("'dataType' is required. Use: pool, eventChannel, dataContainer, runtimeSet");

            return dataType switch
            {
                "pool" => DispatchPool(operation, payload),
                "eventChannel" => DispatchEventChannel(operation, payload),
                "dataContainer" => DispatchDataContainer(operation, payload),
                "runtimeSet" => DispatchRuntimeSet(operation, payload),
                _ => CreateFailureResponse(
                    $"Unknown dataType '{dataType}'. Use: pool, eventChannel, dataContainer, runtimeSet")
            };
        }

        #region Pool Dispatch

        private object DispatchPool(string operation, Dictionary<string, object> payload)
        {
            // Map normalized operations to pool handler's operation names
            var poolOp = operation switch
            {
                "create" => "create",
                "inspect" => "inspect",
                "find" => "findByPoolId",
                _ => null
            };

            if (poolOp == null)
                return CreateFailureResponse(
                    $"Operation '{operation}' is not supported for dataType 'pool'. " +
                    "Supported: create, inspect, find");

            payload["operation"] = poolOp;
            return _poolHandler.InvokeOperation(poolOp, payload);
        }

        #endregion

        #region Event Channel

        private object DispatchEventChannel(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "create" => CreateEventChannel(payload),
                "inspect" => InspectData(payload),
                "find" => FindByDataId(payload),
                _ => CreateFailureResponse(
                    $"Operation '{operation}' is not supported for dataType 'eventChannel'. " +
                    "Supported: create, inspect, find")
            };
        }

        private object CreateEventChannel(Dictionary<string, object> payload)
        {
            var dataId = GetString(payload, "dataId");
            if (string.IsNullOrEmpty(dataId))
                dataId = $"Event_{Guid.NewGuid().ToString().Substring(0, 8)}";

            var eventType = GetString(payload, "eventType") ?? "void";
            var assetPath = GetString(payload, "assetPath");
            var outputDir = GetString(payload, "scriptOutputDir") ?? "Assets/Scripts/Generated";
            var createListener = GetBool(payload, "createListener", false);

            var channelClassName = ScriptGenerator.ToPascalCase(dataId, "EventChannel");

            // Resolve event type variations for template
            ResolveEventType(eventType, out var eventTypeLabel, out var eventTypeGeneric,
                out var eventTypeParam, out var eventTypeArg);

            var channelVars = new Dictionary<string, object>
            {
                { "DATA_ID", dataId },
                { "EVENT_TYPE_LABEL", eventTypeLabel },
                { "EVENT_TYPE_GENERIC", eventTypeGeneric },
                { "EVENT_TYPE_PARAM", eventTypeParam },
                { "EVENT_TYPE_ARG", eventTypeArg },
                { "DESCRIPTION", $"Event channel: {dataId} ({eventType})" }
            };

            // Generate channel script (null target — ScriptableObject, no GameObject)
            var channelResult = ScriptGenerator.Generate(null, "EventChannel", channelClassName, dataId, channelVars, outputDir);
            if (!channelResult.Success)
            {
                throw new InvalidOperationException(channelResult.ErrorMessage ?? "Failed to generate EventChannel script.");
            }

            var result = CreateSuccessResponse(
                ("dataId", dataId),
                ("eventType", eventType),
                ("channelClassName", channelClassName),
                ("channelScriptPath", channelResult.ScriptPath),
                ("requiresCompilationWait", true)
            );

            // Optionally create listener
            if (createListener)
            {
                var listenerClassName = ScriptGenerator.ToPascalCase(dataId, "EventListener");
                var listenerVars = new Dictionary<string, object>
                {
                    { "CHANNEL_CLASS_NAME", channelClassName },
                    { "EVENT_TYPE_GENERIC", eventTypeGeneric },
                    { "EVENT_TYPE_PARAM", eventTypeParam },
                    { "EVENT_TYPE_ARG", eventTypeArg }
                };

                // If targetPath is provided, use it for pending auto-attach
                var targetPath = GetString(payload, "targetPath");
                GameObject targetGo = null;
                if (!string.IsNullOrEmpty(targetPath))
                    targetGo = TryResolveGameObject(targetPath);

                var listenerResult = ScriptGenerator.Generate(
                    targetGo, "EventListener", listenerClassName, $"{dataId}_Listener", listenerVars, outputDir);

                result["listenerClassName"] = listenerClassName;
                result["listenerScriptPath"] = listenerResult.Success ? listenerResult.ScriptPath : null;

                // Mark for auto-attach after compilation if target was specified
                if (targetGo != null && listenerResult.Success)
                {
                    var entry = GeneratedScriptTracker.Instance.FindByComponentId($"{dataId}_Listener");
                    if (entry != null)
                    {
                        entry.pendingAttach = true;
                        GeneratedScriptTracker.Instance.Register(entry);
                    }
                    result["listenerTarget"] = targetPath;
                }
            }

            // If assetPath is provided, note that the asset must be created after compilation
            if (!string.IsNullOrEmpty(assetPath))
            {
                result["assetPath"] = assetPath;
                result["note"] = "After compilation, create the ScriptableObject asset using unity_scriptableObject_crud with typeName='" + channelClassName + "'.";
            }

            return result;
        }

        #endregion

        #region Data Container

        private object DispatchDataContainer(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "create" => CreateDataContainer(payload),
                "inspect" => InspectData(payload),
                "find" => FindByDataId(payload),
                _ => CreateFailureResponse(
                    $"Operation '{operation}' is not supported for dataType 'dataContainer'. " +
                    "Supported: create, inspect, find")
            };
        }

        private object CreateDataContainer(Dictionary<string, object> payload)
        {
            var dataId = GetString(payload, "dataId");
            if (string.IsNullOrEmpty(dataId))
                dataId = $"Data_{Guid.NewGuid().ToString().Substring(0, 8)}";

            var resetOnPlay = GetBool(payload, "resetOnPlay", true);
            var outputDir = GetString(payload, "scriptOutputDir") ?? "Assets/Scripts/Generated";
            var assetPath = GetString(payload, "assetPath");

            var className = ScriptGenerator.ToPascalCase(dataId, "DataContainer");

            // Build fields from payload
            var fieldsCode = BuildFieldsCode(payload);

            var variables = new Dictionary<string, object>
            {
                { "DATA_ID", dataId },
                { "RESET_ON_PLAY", resetOnPlay },
                { "FIELDS", fieldsCode }
            };

            var genResult = ScriptGenerator.Generate(null, "DataContainer", className, dataId, variables, outputDir);
            if (!genResult.Success)
            {
                throw new InvalidOperationException(genResult.ErrorMessage ?? "Failed to generate DataContainer script.");
            }

            var result = CreateSuccessResponse(
                ("dataId", dataId),
                ("className", className),
                ("scriptPath", genResult.ScriptPath),
                ("resetOnPlay", resetOnPlay),
                ("requiresCompilationWait", true)
            );

            if (!string.IsNullOrEmpty(assetPath))
            {
                result["assetPath"] = assetPath;
                result["note"] = "After compilation, create the ScriptableObject asset using unity_scriptableObject_crud with typeName='" + className + "'.";
            }

            return result;
        }

        private string BuildFieldsCode(Dictionary<string, object> payload)
        {
            if (!payload.TryGetValue("fields", out var fieldsObj) || fieldsObj == null)
                return "// No fields defined";

            var fields = fieldsObj as List<object>;
            if (fields == null || fields.Count == 0)
                return "// No fields defined";

            var sb = new StringBuilder();
            foreach (var fieldObj in fields)
            {
                if (fieldObj is not Dictionary<string, object> field) continue;

                var name = field.TryGetValue("name", out var n) ? n?.ToString() ?? "value" : "value";
                var fieldType = field.TryGetValue("fieldType", out var ft) ? ft?.ToString() ?? "float" : "float";
                var defaultValue = field.TryGetValue("defaultValue", out var dv) ? dv : null;

                var csType = MapFieldType(fieldType);
                var csDefault = MapDefaultValue(fieldType, defaultValue);

                sb.AppendLine($"    [SerializeField] public {csType} {name}{csDefault};");
            }

            return sb.ToString().TrimEnd();
        }

        private static string MapFieldType(string fieldType)
        {
            return fieldType switch
            {
                "int" => "int",
                "float" => "float",
                "string" => "string",
                "bool" => "bool",
                "Vector2" => "Vector2",
                "Vector3" => "Vector3",
                "Color" => "Color",
                _ => "float"
            };
        }

        private static string MapDefaultValue(string fieldType, object defaultValue)
        {
            if (defaultValue == null) return "";

            return fieldType switch
            {
                "string" => $" = \"{defaultValue}\"",
                "bool" => $" = {defaultValue.ToString().ToLower()}",
                "float" => $" = {Convert.ToSingle(defaultValue)}f",
                "int" => $" = {Convert.ToInt32(defaultValue)}",
                _ => ""
            };
        }

        #endregion

        #region Runtime Set

        private object DispatchRuntimeSet(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "create" => CreateRuntimeSet(payload),
                "inspect" => InspectData(payload),
                "find" => FindByDataId(payload),
                _ => CreateFailureResponse(
                    $"Operation '{operation}' is not supported for dataType 'runtimeSet'. " +
                    "Supported: create, inspect, find")
            };
        }

        private object CreateRuntimeSet(Dictionary<string, object> payload)
        {
            var dataId = GetString(payload, "dataId");
            if (string.IsNullOrEmpty(dataId))
                dataId = $"Set_{Guid.NewGuid().ToString().Substring(0, 8)}";

            var elementType = GetString(payload, "elementType") ?? "GameObject";
            var outputDir = GetString(payload, "scriptOutputDir") ?? "Assets/Scripts/Generated";
            var assetPath = GetString(payload, "assetPath");

            var className = ScriptGenerator.ToPascalCase(dataId, "RuntimeSet");

            var variables = new Dictionary<string, object>
            {
                { "DATA_ID", dataId },
                { "ELEMENT_TYPE", elementType }
            };

            var genResult = ScriptGenerator.Generate(null, "RuntimeSet", className, dataId, variables, outputDir);
            if (!genResult.Success)
            {
                throw new InvalidOperationException(genResult.ErrorMessage ?? "Failed to generate RuntimeSet script.");
            }

            var result = CreateSuccessResponse(
                ("dataId", dataId),
                ("className", className),
                ("scriptPath", genResult.ScriptPath),
                ("elementType", elementType),
                ("requiresCompilationWait", true)
            );

            if (!string.IsNullOrEmpty(assetPath))
            {
                result["assetPath"] = assetPath;
                result["note"] = "After compilation, create the ScriptableObject asset using unity_scriptableObject_crud with typeName='" + className + "'.";
            }

            return result;
        }

        #endregion

        #region Shared: Inspect / Delete / Find

        private object InspectData(Dictionary<string, object> payload)
        {
            var dataId = GetString(payload, "dataId");
            if (string.IsNullOrEmpty(dataId))
                throw new InvalidOperationException("dataId is required for inspect operation.");

            // Check if there's a generated script tracked by this ID
            var entry = GeneratedScriptTracker.Instance.FindByComponentId(dataId);
            if (entry == null)
            {
                return CreateSuccessResponse(
                    ("found", false),
                    ("dataId", dataId)
                );
            }

            return CreateSuccessResponse(
                ("found", true),
                ("dataId", dataId),
                ("className", entry.className),
                ("scriptPath", entry.scriptPath),
                ("templateName", entry.templateName),
                ("gameObjectPath", entry.gameObjectPath)
            );
        }

        private object FindByDataId(Dictionary<string, object> payload)
        {
            var dataId = GetString(payload, "dataId");
            if (string.IsNullOrEmpty(dataId))
                throw new InvalidOperationException("dataId is required for find operation.");

            // Search ScriptableObject assets by dataId field
            var guids = AssetDatabase.FindAssets("t:ScriptableObject");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset == null) continue;

                var so = new SerializedObject(asset);
                var dataIdProp = so.FindProperty("dataId");
                if (dataIdProp != null && dataIdProp.propertyType == SerializedPropertyType.String
                    && dataIdProp.stringValue == dataId)
                {
                    return CreateSuccessResponse(
                        ("found", true),
                        ("dataId", dataId),
                        ("assetPath", path),
                        ("typeName", asset.GetType().Name)
                    );
                }
            }

            // Also check scene components (for listeners)
            var component = CodeGenHelper.FindComponentInSceneByField("dataId", dataId);
            if (component != null)
            {
                return CreateSuccessResponse(
                    ("found", true),
                    ("dataId", dataId),
                    ("path", BuildGameObjectPath(component.gameObject)),
                    ("type", "scene_component")
                );
            }

            // Check script tracker
            var entry = GeneratedScriptTracker.Instance.FindByComponentId(dataId);
            if (entry != null)
            {
                return CreateSuccessResponse(
                    ("found", true),
                    ("dataId", dataId),
                    ("className", entry.className),
                    ("scriptPath", entry.scriptPath),
                    ("type", "script_only"),
                    ("note", "Script exists but no asset instance found. Create one using unity_scriptableObject_crud.")
                );
            }

            return CreateSuccessResponse(("found", false), ("dataId", dataId));
        }

        #endregion

        #region Helpers

        private static void ResolveEventType(string eventType, out string label, out string generic,
            out string param, out string arg)
        {
            switch (eventType)
            {
                case "void":
                    label = "Void";
                    generic = "";
                    param = "";
                    arg = "";
                    break;
                case "int":
                    label = "Int";
                    generic = "<int>";
                    param = "int value";
                    arg = "value";
                    break;
                case "float":
                    label = "Float";
                    generic = "<float>";
                    param = "float value";
                    arg = "value";
                    break;
                case "string":
                    label = "String";
                    generic = "<string>";
                    param = "string value";
                    arg = "value";
                    break;
                case "Vector3":
                    label = "Vector3";
                    generic = "<Vector3>";
                    param = "Vector3 value";
                    arg = "value";
                    break;
                case "GameObject":
                    label = "GameObject";
                    generic = "<GameObject>";
                    param = "GameObject value";
                    arg = "value";
                    break;
                default:
                    label = "Void";
                    generic = "";
                    param = "";
                    arg = "";
                    break;
            }
        }

        #endregion
    }
}
