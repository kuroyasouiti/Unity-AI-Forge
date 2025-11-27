using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace MCP.Editor
{
    internal static partial class McpCommandProcessor
    {
        #region Settings Management

        private static object HandleTagLayerManage(Dictionary<string, object> payload)
        {
            try
            {
                var operation = GetString(payload, "operation");
                if (string.IsNullOrEmpty(operation))
                {
                    throw new InvalidOperationException("operation is required");
                }

                // Check if compilation is in progress and wait if necessary (skip for read-only operations)
                Dictionary<string, object> compilationWaitInfo = null;
                if (operation != "getTag" && operation != "getLayer" && operation != "listTags" && operation != "listLayers")
                {
                    compilationWaitInfo = EnsureNoCompilationInProgress("tagLayerManage", maxWaitSeconds: 30f);
                }

                Debug.Log($"[tagLayerManage] Processing operation: {operation}");

                object result = operation switch
                {
                    "setTag" => SetTag(payload),
                    "getTag" => GetTag(payload),
                    "setLayer" => SetLayer(payload),
                    "getLayer" => GetLayer(payload),
                    "setLayerRecursive" => SetLayerRecursive(payload),
                    "listTags" => ListTags(),
                    "addTag" => AddTag(payload),
                    "removeTag" => RemoveTag(payload),
                    "listLayers" => ListLayers(),
                    "addLayer" => AddLayer(payload),
                    "removeLayer" => RemoveLayer(payload),
                    _ => throw new InvalidOperationException($"Unknown tagLayerManage operation: {operation}"),
                };

                // Add compilation wait info to result if present
                if (compilationWaitInfo != null && result is Dictionary<string, object> resultDict)
                {
                    resultDict["compilationWait"] = compilationWaitInfo;
                }

                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[tagLayerManage] Error: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Sets the tag of a GameObject.
        /// </summary>
        private static object SetTag(Dictionary<string, object> payload)
        {
            var path = EnsureValue(GetString(payload, "gameObjectPath"), "gameObjectPath");
            var tag = EnsureValue(GetString(payload, "tag"), "tag");

            var target = ResolveGameObject(path);
            var oldTag = target.tag;

            // Verify tag exists
            try
            {
                target.tag = tag;
            }
            catch (UnityException ex)
            {
                throw new InvalidOperationException($"Tag '{tag}' does not exist in the project. Use addTag operation to create it first. {ex.Message}");
            }

            EditorUtility.SetDirty(target);

            return new Dictionary<string, object>
            {
                ["gameObjectPath"] = path,
                ["oldTag"] = oldTag,
                ["newTag"] = tag,
                ["operation"] = "setTag",
            };
        }

        /// <summary>
        /// Gets the tag of a GameObject.
        /// </summary>
        private static object GetTag(Dictionary<string, object> payload)
        {
            var path = EnsureValue(GetString(payload, "gameObjectPath"), "gameObjectPath");
            var target = ResolveGameObject(path);

            return new Dictionary<string, object>
            {
                ["gameObjectPath"] = path,
                ["tag"] = target.tag,
                ["operation"] = "getTag",
            };
        }

        /// <summary>
        /// Sets the layer of a GameObject.
        /// </summary>
        private static object SetLayer(Dictionary<string, object> payload)
        {
            var path = EnsureValue(GetString(payload, "gameObjectPath"), "gameObjectPath");
            var target = ResolveGameObject(path);

            int newLayer;
            if (payload.TryGetValue("layer", out var layerObj))
            {
                if (layerObj is string layerName)
                {
                    newLayer = LayerMask.NameToLayer(layerName);
                    if (newLayer == -1)
                    {
                        throw new InvalidOperationException($"Layer '{layerName}' does not exist in the project. Use addLayer operation to create it first.");
                    }
                }
                else if (layerObj is int layerIndex)
                {
                    newLayer = layerIndex;
                }
                else if (layerObj is double layerDouble)
                {
                    newLayer = (int)layerDouble;
                }
                else
                {
                    throw new InvalidOperationException("layer must be a string (layer name) or integer (layer index)");
                }
            }
            else
            {
                throw new InvalidOperationException("layer is required");
            }

            var oldLayer = target.layer;
            var oldLayerName = LayerMask.LayerToName(oldLayer);

            target.layer = newLayer;
            EditorUtility.SetDirty(target);

            return new Dictionary<string, object>
            {
                ["gameObjectPath"] = path,
                ["oldLayer"] = oldLayer,
                ["oldLayerName"] = oldLayerName,
                ["newLayer"] = newLayer,
                ["newLayerName"] = LayerMask.LayerToName(newLayer),
                ["operation"] = "setLayer",
            };
        }

        /// <summary>
        /// Gets the layer of a GameObject.
        /// </summary>
        private static object GetLayer(Dictionary<string, object> payload)
        {
            var path = EnsureValue(GetString(payload, "gameObjectPath"), "gameObjectPath");
            var target = ResolveGameObject(path);

            return new Dictionary<string, object>
            {
                ["gameObjectPath"] = path,
                ["layer"] = target.layer,
                ["layerName"] = LayerMask.LayerToName(target.layer),
                ["operation"] = "getLayer",
            };
        }

        /// <summary>
        /// Sets the layer of a GameObject and all its children recursively.
        /// </summary>
        private static object SetLayerRecursive(Dictionary<string, object> payload)
        {
            var path = EnsureValue(GetString(payload, "gameObjectPath"), "gameObjectPath");
            var target = ResolveGameObject(path);

            int newLayer;
            if (payload.TryGetValue("layer", out var layerObj))
            {
                if (layerObj is string layerName)
                {
                    newLayer = LayerMask.NameToLayer(layerName);
                    if (newLayer == -1)
                    {
                        throw new InvalidOperationException($"Layer '{layerName}' does not exist in the project. Use addLayer operation to create it first.");
                    }
                }
                else if (layerObj is int layerIndex)
                {
                    newLayer = layerIndex;
                }
                else if (layerObj is double layerDouble)
                {
                    newLayer = (int)layerDouble;
                }
                else
                {
                    throw new InvalidOperationException("layer must be a string (layer name) or integer (layer index)");
                }
            }
            else
            {
                throw new InvalidOperationException("layer is required");
            }

            var affectedCount = 0;
            SetLayerRecursiveInternal(target, newLayer, ref affectedCount);

            return new Dictionary<string, object>
            {
                ["gameObjectPath"] = path,
                ["newLayer"] = newLayer,
                ["newLayerName"] = LayerMask.LayerToName(newLayer),
                ["affectedCount"] = affectedCount,
                ["operation"] = "setLayerRecursive",
            };
        }

        /// <summary>
        /// Internal helper for recursive layer setting.
        /// </summary>
        private static void SetLayerRecursiveInternal(GameObject obj, int layer, ref int count)
        {
            obj.layer = layer;
            EditorUtility.SetDirty(obj);
            count++;

            foreach (Transform child in obj.transform)
            {
                SetLayerRecursiveInternal(child.gameObject, layer, ref count);
            }
        }

        /// <summary>
        /// Lists all tags in the project.
        /// </summary>
        private static object ListTags()
        {
            var tags = UnityEditorInternal.InternalEditorUtility.tags;

            return new Dictionary<string, object>
            {
                ["tags"] = new List<string>(tags),
                ["count"] = tags.Length,
                ["operation"] = "listTags",
            };
        }

        /// <summary>
        /// Adds a new tag to the project.
        /// </summary>
        private static object AddTag(Dictionary<string, object> payload)
        {
            var tag = EnsureValue(GetString(payload, "tag"), "tag");

            // Check if tag already exists
            var existingTags = UnityEditorInternal.InternalEditorUtility.tags;
            if (System.Array.IndexOf(existingTags, tag) != -1)
            {
                return new Dictionary<string, object>
                {
                    ["tag"] = tag,
                    ["added"] = false,
                    ["message"] = "Tag already exists",
                    ["operation"] = "addTag",
                };
            }

            // Add tag using SerializedObject
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var tagsProp = tagManager.FindProperty("tags");

            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            var newTagProp = tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1);
            newTagProp.stringValue = tag;

            tagManager.ApplyModifiedProperties();

            return new Dictionary<string, object>
            {
                ["tag"] = tag,
                ["added"] = true,
                ["operation"] = "addTag",
            };
        }

        /// <summary>
        /// Removes a tag from the project.
        /// </summary>
        private static object RemoveTag(Dictionary<string, object> payload)
        {
            var tag = EnsureValue(GetString(payload, "tag"), "tag");

            // Find tag index
            var existingTags = UnityEditorInternal.InternalEditorUtility.tags;
            var tagIndex = System.Array.IndexOf(existingTags, tag);

            if (tagIndex == -1)
            {
                return new Dictionary<string, object>
                {
                    ["tag"] = tag,
                    ["removed"] = false,
                    ["message"] = "Tag does not exist",
                    ["operation"] = "removeTag",
                };
            }

            // Remove tag using SerializedObject
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var tagsProp = tagManager.FindProperty("tags");

            // Find the property index (it may not match array index due to built-in tags)
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag)
                {
                    tagsProp.DeleteArrayElementAtIndex(i);
                    tagManager.ApplyModifiedProperties();

                    return new Dictionary<string, object>
                    {
                        ["tag"] = tag,
                        ["removed"] = true,
                        ["operation"] = "removeTag",
                    };
                }
            }

            return new Dictionary<string, object>
            {
                ["tag"] = tag,
                ["removed"] = false,
                ["message"] = "Failed to find tag in TagManager",
                ["operation"] = "removeTag",
            };
        }

        /// <summary>
        /// Lists all layers in the project.
        /// </summary>
        private static object ListLayers()
        {
            var layers = new List<Dictionary<string, object>>();

            for (int i = 0; i < 32; i++)
            {
                var layerName = LayerMask.LayerToName(i);
                if (!string.IsNullOrEmpty(layerName))
                {
                    layers.Add(new Dictionary<string, object>
                    {
                        ["index"] = i,
                        ["name"] = layerName,
                    });
                }
            }

            return new Dictionary<string, object>
            {
                ["layers"] = layers,
                ["count"] = layers.Count,
                ["operation"] = "listLayers",
            };
        }

        /// <summary>
        /// Adds a new layer to the project.
        /// </summary>
        private static object AddLayer(Dictionary<string, object> payload)
        {
            var layer = EnsureValue(GetString(payload, "layer"), "layer");

            // Check if layer already exists
            for (int i = 0; i < 32; i++)
            {
                if (LayerMask.LayerToName(i) == layer)
                {
                    return new Dictionary<string, object>
                    {
                        ["layer"] = layer,
                        ["index"] = i,
                        ["added"] = false,
                        ["message"] = "Layer already exists",
                        ["operation"] = "addLayer",
                    };
                }
            }

            // Find first available layer slot (8-31, 0-7 are built-in)
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layersProp = tagManager.FindProperty("layers");

            int availableIndex = -1;
            for (int i = 8; i < 32; i++)
            {
                var layerProp = layersProp.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(layerProp.stringValue))
                {
                    availableIndex = i;
                    break;
                }
            }

            if (availableIndex == -1)
            {
                return new Dictionary<string, object>
                {
                    ["layer"] = layer,
                    ["added"] = false,
                    ["message"] = "No available layer slots (layers 8-31 are full)",
                    ["operation"] = "addLayer",
                };
            }

            var newLayerProp = layersProp.GetArrayElementAtIndex(availableIndex);
            newLayerProp.stringValue = layer;
            tagManager.ApplyModifiedProperties();

            return new Dictionary<string, object>
            {
                ["layer"] = layer,
                ["index"] = availableIndex,
                ["added"] = true,
                ["operation"] = "addLayer",
            };
        }

        /// <summary>
        /// Removes a layer from the project.
        /// </summary>
        private static object RemoveLayer(Dictionary<string, object> payload)
        {
            var layer = EnsureValue(GetString(payload, "layer"), "layer");

            // Find layer index
            int layerIndex = -1;
            for (int i = 0; i < 32; i++)
            {
                if (LayerMask.LayerToName(i) == layer)
                {
                    layerIndex = i;
                    break;
                }
            }

            if (layerIndex == -1)
            {
                return new Dictionary<string, object>
                {
                    ["layer"] = layer,
                    ["removed"] = false,
                    ["message"] = "Layer does not exist",
                    ["operation"] = "removeLayer",
                };
            }

            // Cannot remove built-in layers (0-7)
            if (layerIndex < 8)
            {
                return new Dictionary<string, object>
                {
                    ["layer"] = layer,
                    ["index"] = layerIndex,
                    ["removed"] = false,
                    ["message"] = "Cannot remove built-in layers (0-7)",
                    ["operation"] = "removeLayer",
                };
            }

            // Remove layer using SerializedObject
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layersProp = tagManager.FindProperty("layers");

            var layerProp = layersProp.GetArrayElementAtIndex(layerIndex);
            layerProp.stringValue = "";
            tagManager.ApplyModifiedProperties();

            return new Dictionary<string, object>
            {
                ["layer"] = layer,
                ["index"] = layerIndex,
                ["removed"] = true,
                ["operation"] = "removeLayer",
            };
        }


        // NOTE: Prefab management operations have been moved to Prefab/McpCommandProcessor.Prefab.cs
        // This includes: HandlePrefabManage, CreatePrefab, UpdatePrefab, InspectPrefab,
        // InstantiatePrefab, UnpackPrefab, ApplyPrefabOverrides, and RevertPrefabOverrides.

        private static object HandleProjectSettingsManage(Dictionary<string, object> payload)
        {
            var operation = EnsureValue(GetString(payload, "operation"), "operation");

            // Check if compilation is in progress and wait if necessary (skip for read-only operations)
            Dictionary<string, object> compilationWaitInfo = null;
            if (operation != "read" && operation != "list")
            {
                compilationWaitInfo = EnsureNoCompilationInProgress("projectSettingsManage", maxWaitSeconds: 30f);
            }

            object result = operation switch
            {
                "read" => ReadProjectSettings(payload),
                "write" => WriteProjectSettings(payload),
                "list" => ListProjectSettings(payload),
                _ => throw new InvalidOperationException($"Unknown projectSettingsManage operation: {operation}"),
            };

            // Add compilation wait info to result if present
            if (compilationWaitInfo != null && result is Dictionary<string, object> resultDict)
            {
                resultDict["compilationWait"] = compilationWaitInfo;
            }

            return result;
        }

        private static object ReadProjectSettings(Dictionary<string, object> payload)
        {
            var category = EnsureValue(GetString(payload, "category"), "category");
            var property = GetString(payload, "property");

            var result = new Dictionary<string, object>
            {
                ["category"] = category,
            };

            switch (category.ToLower())
            {
                case "player":
                    result["settings"] = ReadPlayerSettings(property);
                    break;
                case "quality":
                    result["settings"] = ReadQualitySettings(property);
                    break;
                case "time":
                    result["settings"] = ReadTimeSettings(property);
                    break;
                case "physics":
                    result["settings"] = ReadPhysicsSettings(property);
                    break;
                case "audio":
                    result["settings"] = ReadAudioSettings(property);
                    break;
                case "editor":
                    result["settings"] = ReadEditorSettings(property);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown settings category: {category}");
            }

            return result;
        }

        private static object WriteProjectSettings(Dictionary<string, object> payload)
        {
            var category = EnsureValue(GetString(payload, "category"), "category");
            var property = EnsureValue(GetString(payload, "property"), "property");

            if (!payload.TryGetValue("value", out var value))
            {
                throw new InvalidOperationException("value is required for write operation");
            }

            switch (category.ToLower())
            {
                case "player":
                    WritePlayerSettings(property, value);
                    break;
                case "quality":
                    WriteQualitySettings(property, value);
                    break;
                case "time":
                    WriteTimeSettings(property, value);
                    break;
                case "physics":
                    WritePhysicsSettings(property, value);
                    break;
                case "audio":
                    WriteAudioSettings(property, value);
                    break;
                case "editor":
                    WriteEditorSettings(property, value);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown settings category: {category}");
            }

            return new Dictionary<string, object>
            {
                ["category"] = category,
                ["property"] = property,
                ["value"] = value,
                ["message"] = "Settings updated successfully",
            };
        }

        private static object ListProjectSettings(Dictionary<string, object> payload)
        {
            var category = GetString(payload, "category");

            if (string.IsNullOrEmpty(category))
            {
                // Return all available categories
                return new Dictionary<string, object>
                {
                    ["categories"] = new List<string>
                    {
                        "player",
                        "quality",
                        "time",
                        "physics",
                        "audio",
                        "editor",
                    },
                };
            }

            // Return available properties for the specified category
            var properties = category.ToLower() switch
            {
                "player" => new List<string>
                {
                    "companyName", "productName", "version", "bundleVersion",
                    "defaultScreenWidth", "defaultScreenHeight", "runInBackground",
                    "displayResolutionDialog", "defaultIsFullScreen", "defaultIsNativeResolution",
                    "allowFullscreenSwitch", "captureSingleScreen", "resizableWindow",
                },
                "quality" => new List<string>
                {
                    "names", "currentLevel", "pixelLightCount", "shadowDistance",
                    "shadowResolution", "shadowProjection", "shadowCascades", "vSyncCount",
                    "antiAliasing", "softParticles", "realtimeReflectionProbes",
                },
                "time" => new List<string>
                {
                    "fixedDeltaTime", "maximumDeltaTime", "timeScale", "maximumParticleDeltaTime",
                    "captureDeltaTime",
                },
                "physics" => new List<string>
                {
                    "gravity", "defaultSolverIterations", "defaultSolverVelocityIterations",
                    "bounceThreshold", "sleepThreshold", "defaultContactOffset",
                    "queriesHitTriggers", "queriesHitBackfaces", "autoSimulation",
                },
                "audio" => new List<string>
                {
                    "dspBufferSize", "sampleRate", "speakerMode", "numRealVoices",
                    "numVirtualVoices",
                },
                "editor" => new List<string>
                {
                    "serializationMode", "spritePackerMode", "etcTextureCompressorBehavior",
                    "lineEndingsForNewScripts", "defaultBehaviorMode", "prefabRegularEnvironment",
                },
                _ => throw new InvalidOperationException($"Unknown settings category: {category}"),
            };

            return new Dictionary<string, object>
            {
                ["category"] = category,
                ["properties"] = properties,
            };
        }

        // PlayerSettings read/write methods
        private static object ReadPlayerSettings(string property)
        {
            if (string.IsNullOrEmpty(property))
            {
                return new Dictionary<string, object>
                {
                    ["companyName"] = PlayerSettings.companyName,
                    ["productName"] = PlayerSettings.productName,
                    ["version"] = PlayerSettings.bundleVersion,
                    ["defaultScreenWidth"] = PlayerSettings.defaultScreenWidth,
                    ["defaultScreenHeight"] = PlayerSettings.defaultScreenHeight,
                    ["runInBackground"] = PlayerSettings.runInBackground,
                };
            }

            return property.ToLower() switch
            {
                "companyname" => PlayerSettings.companyName,
                "productname" => PlayerSettings.productName,
                "version" or "bundleversion" => PlayerSettings.bundleVersion,
                "defaultscreenwidth" => PlayerSettings.defaultScreenWidth,
                "defaultscreenheight" => PlayerSettings.defaultScreenHeight,
                "runinbackground" => PlayerSettings.runInBackground,
                "fullscreenmode" => PlayerSettings.fullScreenMode.ToString(),
                "defaultisnativeresolution" => PlayerSettings.defaultIsNativeResolution,
                "allowfullscreenswitch" => PlayerSettings.allowFullscreenSwitch,
                "resizablewindow" => PlayerSettings.resizableWindow,
                _ => throw new InvalidOperationException($"Unknown PlayerSettings property: {property}"),
            };
        }

        private static void WritePlayerSettings(string property, object value)
        {
            switch (property.ToLower())
            {
                case "companyname":
                    PlayerSettings.companyName = value.ToString();
                    break;
                case "productname":
                    PlayerSettings.productName = value.ToString();
                    break;
                case "version":
                case "bundleversion":
                    PlayerSettings.bundleVersion = value.ToString();
                    break;
                case "defaultscreenwidth":
                    PlayerSettings.defaultScreenWidth = Convert.ToInt32(value);
                    break;
                case "defaultscreenheight":
                    PlayerSettings.defaultScreenHeight = Convert.ToInt32(value);
                    break;
                case "runinbackground":
                    PlayerSettings.runInBackground = Convert.ToBoolean(value);
                    break;
                case "fullscreenmode":
                    if (Enum.TryParse<FullScreenMode>(value.ToString(), true, out var mode))
                    {
                        PlayerSettings.fullScreenMode = mode;
                    }
                    else
                    {
                        throw new InvalidOperationException($"Invalid FullScreenMode value: {value}");
                    }
                    break;
                case "defaultisnativeresolution":
                    PlayerSettings.defaultIsNativeResolution = Convert.ToBoolean(value);
                    break;
                case "allowfullscreenswitch":
                    PlayerSettings.allowFullscreenSwitch = Convert.ToBoolean(value);
                    break;
                case "resizablewindow":
                    PlayerSettings.resizableWindow = Convert.ToBoolean(value);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown or readonly PlayerSettings property: {property}");
            }
        }

        // QualitySettings read/write methods
        private static object ReadQualitySettings(string property)
        {
            if (string.IsNullOrEmpty(property))
            {
                return new Dictionary<string, object>
                {
                    ["names"] = QualitySettings.names,
                    ["currentLevel"] = QualitySettings.GetQualityLevel(),
                    ["pixelLightCount"] = QualitySettings.pixelLightCount,
                    ["shadowDistance"] = QualitySettings.shadowDistance,
                    ["vSyncCount"] = QualitySettings.vSyncCount,
                    ["antiAliasing"] = QualitySettings.antiAliasing,
                };
            }

            return property.ToLower() switch
            {
                "names" => QualitySettings.names,
                "currentlevel" => QualitySettings.GetQualityLevel(),
                "pixellightcount" => QualitySettings.pixelLightCount,
                "shadowdistance" => QualitySettings.shadowDistance,
                "shadowresolution" => QualitySettings.shadowResolution.ToString(),
                "shadowprojection" => QualitySettings.shadowProjection.ToString(),
                "shadowcascades" => QualitySettings.shadowCascades,
                "vsynccount" => QualitySettings.vSyncCount,
                "antialiasing" => QualitySettings.antiAliasing,
                "softparticles" => QualitySettings.softParticles,
                "realtimereflectionprobes" => QualitySettings.realtimeReflectionProbes,
                _ => throw new InvalidOperationException($"Unknown QualitySettings property: {property}"),
            };
        }

        private static void WriteQualitySettings(string property, object value)
        {
            switch (property.ToLower())
            {
                case "currentlevel":
                    QualitySettings.SetQualityLevel(Convert.ToInt32(value));
                    break;
                case "pixellightcount":
                    QualitySettings.pixelLightCount = Convert.ToInt32(value);
                    break;
                case "shadowdistance":
                    QualitySettings.shadowDistance = Convert.ToSingle(value);
                    break;
                case "shadowcascades":
                    QualitySettings.shadowCascades = Convert.ToInt32(value);
                    break;
                case "vsynccount":
                    QualitySettings.vSyncCount = Convert.ToInt32(value);
                    break;
                case "antialiasing":
                    QualitySettings.antiAliasing = Convert.ToInt32(value);
                    break;
                case "softparticles":
                    QualitySettings.softParticles = Convert.ToBoolean(value);
                    break;
                case "realtimereflectionprobes":
                    QualitySettings.realtimeReflectionProbes = Convert.ToBoolean(value);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown or readonly QualitySettings property: {property}");
            }
        }

        // TimeSettings read/write methods
        private static object ReadTimeSettings(string property)
        {
            if (string.IsNullOrEmpty(property))
            {
                return new Dictionary<string, object>
                {
                    ["fixedDeltaTime"] = Time.fixedDeltaTime,
                    ["maximumDeltaTime"] = Time.maximumDeltaTime,
                    ["timeScale"] = Time.timeScale,
                    ["maximumParticleDeltaTime"] = Time.maximumParticleDeltaTime,
                };
            }

            return property.ToLower() switch
            {
                "fixeddeltatime" => Time.fixedDeltaTime,
                "maximumdeltatime" => Time.maximumDeltaTime,
                "timescale" => Time.timeScale,
                "maximumparticledeltatime" => Time.maximumParticleDeltaTime,
                "capturedeltatime" => Time.captureDeltaTime,
                _ => throw new InvalidOperationException($"Unknown TimeSettings property: {property}"),
            };
        }

        private static void WriteTimeSettings(string property, object value)
        {
            switch (property.ToLower())
            {
                case "fixeddeltatime":
                    Time.fixedDeltaTime = Convert.ToSingle(value);
                    break;
                case "maximumdeltatime":
                    Time.maximumDeltaTime = Convert.ToSingle(value);
                    break;
                case "timescale":
                    Time.timeScale = Convert.ToSingle(value);
                    break;
                case "maximumparticledeltatime":
                    Time.maximumParticleDeltaTime = Convert.ToSingle(value);
                    break;
                case "capturedeltatime":
                    Time.captureDeltaTime = Convert.ToSingle(value);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown TimeSettings property: {property}");
            }
        }

        // PhysicsSettings read/write methods
        private static object ReadPhysicsSettings(string property)
        {
            if (string.IsNullOrEmpty(property))
            {
                return new Dictionary<string, object>
                {
                    ["gravity"] = new Dictionary<string, object>
                    {
                        ["x"] = Physics.gravity.x,
                        ["y"] = Physics.gravity.y,
                        ["z"] = Physics.gravity.z,
                    },
                    ["defaultSolverIterations"] = Physics.defaultSolverIterations,
                    ["defaultSolverVelocityIterations"] = Physics.defaultSolverVelocityIterations,
                    ["bounceThreshold"] = Physics.bounceThreshold,
                    ["sleepThreshold"] = Physics.sleepThreshold,
                    ["queriesHitTriggers"] = Physics.queriesHitTriggers,
                };
            }

            return property.ToLower() switch
            {
                "gravity" => new Dictionary<string, object>
                {
                    ["x"] = Physics.gravity.x,
                    ["y"] = Physics.gravity.y,
                    ["z"] = Physics.gravity.z,
                },
                "defaultsolveriterations" => Physics.defaultSolverIterations,
                "defaultsolvervelocityiterations" => Physics.defaultSolverVelocityIterations,
                "bouncethreshold" => Physics.bounceThreshold,
                "sleepthreshold" => Physics.sleepThreshold,
                "defaultcontactoffset" => Physics.defaultContactOffset,
                "querieshittriggers" => Physics.queriesHitTriggers,
                "querieshitbackfaces" => Physics.queriesHitBackfaces,
                "simulationmode" => Physics.simulationMode.ToString(),
                _ => throw new InvalidOperationException($"Unknown PhysicsSettings property: {property}"),
            };
        }

        private static void WritePhysicsSettings(string property, object value)
        {
            switch (property.ToLower())
            {
                case "gravity":
                    if (value is Dictionary<string, object> gravityDict)
                    {
                        Physics.gravity = new Vector3(
                            Convert.ToSingle(gravityDict.GetValueOrDefault("x", 0f)),
                            Convert.ToSingle(gravityDict.GetValueOrDefault("y", -9.81f)),
                            Convert.ToSingle(gravityDict.GetValueOrDefault("z", 0f))
                        );
                    }
                    break;
                case "defaultsolveriterations":
                    Physics.defaultSolverIterations = Convert.ToInt32(value);
                    break;
                case "defaultsolvervelocityiterations":
                    Physics.defaultSolverVelocityIterations = Convert.ToInt32(value);
                    break;
                case "bouncethreshold":
                    Physics.bounceThreshold = Convert.ToSingle(value);
                    break;
                case "sleepthreshold":
                    Physics.sleepThreshold = Convert.ToSingle(value);
                    break;
                case "defaultcontactoffset":
                    Physics.defaultContactOffset = Convert.ToSingle(value);
                    break;
                case "querieshittriggers":
                    Physics.queriesHitTriggers = Convert.ToBoolean(value);
                    break;
                case "querieshitbackfaces":
                    Physics.queriesHitBackfaces = Convert.ToBoolean(value);
                    break;
                case "simulationmode":
                    if (Enum.TryParse<SimulationMode>(value.ToString(), true, out var simMode))
                    {
                        Physics.simulationMode = simMode;
                    }
                    else
                    {
                        throw new InvalidOperationException($"Invalid SimulationMode value: {value}");
                    }
                    break;
                default:
                    throw new InvalidOperationException($"Unknown PhysicsSettings property: {property}");
            }
        }

        // AudioSettings read/write methods
        private static object ReadAudioSettings(string property)
        {
            if (string.IsNullOrEmpty(property))
            {
                var config = AudioSettings.GetConfiguration();
                return new Dictionary<string, object>
                {
                    ["dspBufferSize"] = config.dspBufferSize,
                    ["sampleRate"] = config.sampleRate,
                    ["speakerMode"] = config.speakerMode.ToString(),
                    ["numRealVoices"] = config.numRealVoices,
                    ["numVirtualVoices"] = config.numVirtualVoices,
                };
            }

            var audioConfig = AudioSettings.GetConfiguration();
            return property.ToLower() switch
            {
                "dspbuffersize" => audioConfig.dspBufferSize,
                "samplerate" => audioConfig.sampleRate,
                "speakermode" => audioConfig.speakerMode.ToString(),
                "numrealvoices" => audioConfig.numRealVoices,
                "numvirtualvoices" => audioConfig.numVirtualVoices,
                _ => throw new InvalidOperationException($"Unknown AudioSettings property: {property}"),
            };
        }

        private static void WriteAudioSettings(string property, object value)
        {
            var config = AudioSettings.GetConfiguration();
            var modified = false;

            switch (property.ToLower())
            {
                case "dspbuffersize":
                    config.dspBufferSize = Convert.ToInt32(value);
                    modified = true;
                    break;
                case "samplerate":
                    config.sampleRate = Convert.ToInt32(value);
                    modified = true;
                    break;
                case "speakermode":
                    if (Enum.TryParse<AudioSpeakerMode>(value.ToString(), out var mode))
                    {
                        config.speakerMode = mode;
                        modified = true;
                    }
                    break;
                case "numrealvoices":
                    config.numRealVoices = Convert.ToInt32(value);
                    modified = true;
                    break;
                case "numvirtualvoices":
                    config.numVirtualVoices = Convert.ToInt32(value);
                    modified = true;
                    break;
                default:
                    throw new InvalidOperationException($"Unknown AudioSettings property: {property}");
            }

            if (modified)
            {
                AudioSettings.Reset(config);
            }
        }

        // EditorSettings read/write methods
        private static object ReadEditorSettings(string property)
        {
            if (string.IsNullOrEmpty(property))
            {
                return new Dictionary<string, object>
                {
                    ["serializationMode"] = EditorSettings.serializationMode.ToString(),
                    ["spritePackerMode"] = EditorSettings.spritePackerMode.ToString(),
                    ["lineEndingsForNewScripts"] = EditorSettings.lineEndingsForNewScripts.ToString(),
                    ["defaultBehaviorMode"] = EditorSettings.defaultBehaviorMode.ToString(),
                };
            }

            return property.ToLower() switch
            {
                "serializationmode" => EditorSettings.serializationMode.ToString(),
                "spritepackermode" => EditorSettings.spritePackerMode.ToString(),
                "lineendingsfornewscripts" => EditorSettings.lineEndingsForNewScripts.ToString(),
                "defaultbehaviormode" => EditorSettings.defaultBehaviorMode.ToString(),
                "prefabregularenvironment" => EditorSettings.prefabRegularEnvironment?.name,
                _ => throw new InvalidOperationException($"Unknown EditorSettings property: {property}"),
            };
        }

        private static void WriteEditorSettings(string property, object value)
        {
            switch (property.ToLower())
            {
                case "serializationmode":
                    if (Enum.TryParse<SerializationMode>(value.ToString(), out var serMode))
                    {
                        EditorSettings.serializationMode = serMode;
                    }
                    break;
                case "spritepackermode":
                    if (Enum.TryParse<SpritePackerMode>(value.ToString(), out var spriteMode))
                    {
                        EditorSettings.spritePackerMode = spriteMode;
                    }
                    break;
                case "lineendingsfornewscripts":
                    if (Enum.TryParse<LineEndingsMode>(value.ToString(), out var lineMode))
                    {
                        EditorSettings.lineEndingsForNewScripts = lineMode;
                    }
                    break;
                case "defaultbehaviormode":
                    if (Enum.TryParse<EditorBehaviorMode>(value.ToString(), out var behaviorMode))
                    {
                        EditorSettings.defaultBehaviorMode = behaviorMode;
                    }
                    break;
                default:
                    throw new InvalidOperationException($"Unknown or readonly EditorSettings property: {property}");
            }
        }

        /// <summary>
        /// Handles render pipeline management operations (inspect, setAsset, getSettings, updateSettings).
        /// </summary>
        /// <param name="payload">Operation parameters including 'operation' and pipeline-specific settings.</param>
        /// <returns>Result dictionary with operation-specific data.</returns>
        private static object HandleRenderPipelineManage(Dictionary<string, object> payload)
        {
            var operation = EnsureValue(GetString(payload, "operation"), "operation");

            // Check if compilation is in progress and wait if necessary (skip for read-only operations)
            Dictionary<string, object> compilationWaitInfo = null;
            if (operation != "inspect" && operation != "getSettings")
            {
                compilationWaitInfo = EnsureNoCompilationInProgress("renderPipelineManage", maxWaitSeconds: 30f);
            }

            object result = operation switch
            {
                "inspect" => InspectRenderPipeline(),
                "setAsset" => SetRenderPipelineAsset(payload),
                "getSettings" => GetRenderPipelineSettings(payload),
                "updateSettings" => UpdateRenderPipelineSettings(payload),
                _ => throw new InvalidOperationException($"Unknown renderPipelineManage operation: {operation}"),
            };

            // Add compilation wait info to result if present
            if (compilationWaitInfo != null && result is Dictionary<string, object> resultDict)
            {
                resultDict["compilationWait"] = compilationWaitInfo;
            }

            return result;
        }

        private static object InspectRenderPipeline()
        {
            var currentPipeline = GraphicsSettings.currentRenderPipeline;
            var result = new Dictionary<string, object>
            {
                ["hasRenderPipeline"] = currentPipeline != null,
            };

            if (currentPipeline != null)
            {
                result["pipelineName"] = currentPipeline.name;
                result["pipelineType"] = currentPipeline.GetType().FullName;
                result["assetPath"] = AssetDatabase.GetAssetPath(currentPipeline);

                // Detect pipeline type
                var typeName = currentPipeline.GetType().Name;
                if (typeName.Contains("Universal") || typeName.Contains("URP"))
                {
                    result["pipelineKind"] = "URP";
                }
                else if (typeName.Contains("HDRenderPipeline") || typeName.Contains("HDRP"))
                {
                    result["pipelineKind"] = "HDRP";
                }
                else
                {
                    result["pipelineKind"] = "Custom";
                }
            }
            else
            {
                result["pipelineKind"] = "BuiltIn";
            }

            return result;
        }

        private static object SetRenderPipelineAsset(Dictionary<string, object> payload)
        {
            var assetPath = GetString(payload, "assetPath");

            if (string.IsNullOrEmpty(assetPath))
            {
                // Clear render pipeline (set to built-in)
                GraphicsSettings.defaultRenderPipeline = null;
                return new Dictionary<string, object>
                {
                    ["message"] = "Render pipeline cleared (using Built-in)",
                    ["pipelineKind"] = "BuiltIn",
                };
            }

            var asset = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(assetPath);
            if (asset == null)
            {
                throw new InvalidOperationException($"RenderPipelineAsset not found at path: {assetPath}");
            }

            GraphicsSettings.defaultRenderPipeline = asset;

            return new Dictionary<string, object>
            {
                ["message"] = "Render pipeline asset set successfully",
                ["assetPath"] = assetPath,
                ["pipelineName"] = asset.name,
                ["pipelineType"] = asset.GetType().FullName,
            };
        }

        private static object GetRenderPipelineSettings(Dictionary<string, object> payload)
        {
            var currentPipeline = GraphicsSettings.currentRenderPipeline;
            if (currentPipeline == null)
            {
                throw new InvalidOperationException("No render pipeline is currently active. Using Built-in renderer.");
            }

            var result = new Dictionary<string, object>
            {
                ["pipelineName"] = currentPipeline.name,
                ["pipelineType"] = currentPipeline.GetType().FullName,
            };

            // Use reflection to get common properties
            var pipelineType = currentPipeline.GetType();
            var properties = new Dictionary<string, object>();

            foreach (var prop in pipelineType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.CanRead) continue;

                try
                {
                    var value = prop.GetValue(currentPipeline);
                    properties[prop.Name] = SerializeValue(value);
                }
                catch (Exception ex)
                {
                    properties[prop.Name] = $"<Error: {ex.Message}>";
                }
            }

            result["properties"] = properties;
            return result;
        }

        private static object UpdateRenderPipelineSettings(Dictionary<string, object> payload)
        {
            var currentPipeline = GraphicsSettings.currentRenderPipeline;
            if (currentPipeline == null)
            {
                throw new InvalidOperationException("No render pipeline is currently active.");
            }

            if (!payload.TryGetValue("settings", out var settingsObj) || !(settingsObj is Dictionary<string, object> settings))
            {
                throw new InvalidOperationException("settings dictionary is required");
            }

            var pipelineType = currentPipeline.GetType();
            var updatedProperties = new List<string>();

            foreach (var kvp in settings)
            {
                var prop = pipelineType.GetProperty(kvp.Key, BindingFlags.Public | BindingFlags.Instance);
                if (prop != null && prop.CanWrite)
                {
                    try
                    {
                        var converted = ConvertValue(kvp.Value, prop.PropertyType);
                        prop.SetValue(currentPipeline, converted);
                        updatedProperties.Add(kvp.Key);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Failed to set property {kvp.Key}: {ex.Message}");
                    }
                }
            }

            EditorUtility.SetDirty(currentPipeline);
            AssetDatabase.SaveAssets();

            return new Dictionary<string, object>
            {
                ["message"] = "Render pipeline settings updated",
                ["updatedProperties"] = updatedProperties,
            };
        }

        #region Project Compile

        /// <summary>
        /// Gets compilation result by checking for compilation errors.
        /// This is called after compilation completes to check if there were any errors.
        /// Uses enhanced console log parsing for more accurate error and warning detection.
        /// </summary>
        /// <returns>Dictionary with compilation result including success status and any errors.</returns>
        public static Dictionary<string, object> GetCompilationResult()
        {
            var errorMessages = new List<string>();
            var warningMessages = new List<string>();
            var assemblyInfo = new List<string>();

            // Get assembly information
            try
            {
                var assemblies = CompilationPipeline.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    assemblyInfo.Add($"{assembly.name} ({assembly.sourceFiles.Length} files)");
                }
            }
            catch
            {
                // Ignore if we can't get assembly info
            }

            // Parse console logs for errors and warnings
            // This is the most reliable way to get compilation messages after compilation completes
            var logEntries = GetConsoleLogEntries(limit: 200); // Increased from 100 to 200

            foreach (var entry in logEntries)
            {
                if (!entry.ContainsKey("type") || !entry.ContainsKey("message"))
                {
                    continue;
                }

                var message = entry["message"].ToString();
                var entryType = entry["type"].ToString();

                if (entryType == "Error")
                {
                    // Enhanced error detection patterns
                    if (message.Contains("error CS", StringComparison.OrdinalIgnoreCase) ||
                        message.Contains("CompilerError", StringComparison.OrdinalIgnoreCase) ||
                        message.Contains("Build failed", StringComparison.OrdinalIgnoreCase) ||
                        message.Contains("compilation error", StringComparison.OrdinalIgnoreCase) ||
                        message.Contains("error :", StringComparison.OrdinalIgnoreCase))
                    {
                        // Avoid duplicates
                        if (!errorMessages.Contains(message))
                        {
                            errorMessages.Add(message);
                        }
                    }
                }
                else if (entryType == "Warning")
                {
                    // Enhanced warning detection patterns
                    if (message.Contains("warning CS", StringComparison.OrdinalIgnoreCase) ||
                        message.Contains("CompilerWarning", StringComparison.OrdinalIgnoreCase) ||
                        message.Contains("warning :", StringComparison.OrdinalIgnoreCase))
                    {
                        // Avoid duplicates and limit to 20 warnings
                        if (!warningMessages.Contains(message) && warningMessages.Count < 20)
                        {
                            warningMessages.Add(message);
                        }
                    }
                }
            }

            var hasErrors = errorMessages.Count > 0;
            var result = new Dictionary<string, object>
            {
                ["success"] = !hasErrors,
                ["completed"] = true,
                ["timedOut"] = false,
                ["hasErrors"] = hasErrors,
                ["hasWarnings"] = warningMessages.Count > 0,
                ["errors"] = errorMessages,
                ["warnings"] = warningMessages,
                ["errorCount"] = errorMessages.Count,
                ["warningCount"] = warningMessages.Count,
                ["assemblies"] = assemblyInfo,
                ["message"] = hasErrors
                    ? $"Compilation completed with {errorMessages.Count} error(s)" +
                      (warningMessages.Count > 0 ? $" and {warningMessages.Count} warning(s)" : "")
                    : (warningMessages.Count > 0
                        ? $"Compilation completed successfully with {warningMessages.Count} warning(s)"
                        : "Compilation completed successfully"),
            };

            return result;
        }

        /// <summary>
        /// Gets console log entries for error checking.
        /// </summary>
        /// <param name="limit">Maximum number of log entries to retrieve (default: 100)</param>
        /// <returns>List of log entry dictionaries.</returns>
        private static List<Dictionary<string, object>> GetConsoleLogEntries(int limit = 100)
        {
            var logEntries = new List<Dictionary<string, object>>();

            try
            {
                // Use reflection to access Unity's internal LogEntries
                var logEntriesType = Type.GetType("UnityEditor.LogEntries, UnityEditor");
                if (logEntriesType == null)
                {
                    return logEntries;
                }

                var getCountMethod = logEntriesType.GetMethod("GetCount", BindingFlags.Static | BindingFlags.Public);
                var startGettingEntriesMethod = logEntriesType.GetMethod("StartGettingEntries", BindingFlags.Static | BindingFlags.Public);
                var getEntryInternalMethod = logEntriesType.GetMethod("GetEntryInternal", BindingFlags.Static | BindingFlags.Public);
                var endGettingEntriesMethod = logEntriesType.GetMethod("EndGettingEntries", BindingFlags.Static | BindingFlags.Public);

                if (getCountMethod == null || startGettingEntriesMethod == null ||
                    getEntryInternalMethod == null || endGettingEntriesMethod == null)
                {
                    return logEntries;
                }

                var count = (int)getCountMethod.Invoke(null, null);
                startGettingEntriesMethod.Invoke(null, null);

                var logEntryType = Type.GetType("UnityEditor.LogEntry, UnityEditor");
                if (logEntryType == null)
                {
                    endGettingEntriesMethod.Invoke(null, null);
                    return logEntries;
                }

                for (int i = 0; i < Math.Min(count, limit); i++) // Use configurable limit
                {
                    var logEntry = Activator.CreateInstance(logEntryType);
                    var parameters = new object[] { i, logEntry };
                    var success = (bool)getEntryInternalMethod.Invoke(null, parameters);

                    if (success)
                    {
                        var messageField = logEntryType.GetField("message");
                        var modeField = logEntryType.GetField("mode");

                        if (messageField != null && modeField != null)
                        {
                            var message = messageField.GetValue(logEntry)?.ToString() ?? "";
                            var mode = (int)modeField.GetValue(logEntry);

                            var entryType = mode switch
                            {
                                0 => "Log",
                                1 => "Warning",
                                2 => "Error",
                                _ => "Unknown"
                            };

                            logEntries.Add(new Dictionary<string, object>
                            {
                                ["message"] = message,
                                ["type"] = entryType,
                            });
                        }
                    }
                }

                endGettingEntriesMethod.Invoke(null, null);
            }
            catch (Exception)
            {
                // Silently fail if we can't access log entries
            }

            return logEntries;
        }

        #endregion

        // NOTE: Utility Helper Methods have been moved to Core/McpCommandProcessor.Helpers.cs
        // This includes: GetString, GetBool, GetInt, GetFloat, GetList, GetStringList,
        // SerializeValue, ResolveType, ResolveGameObject, ResolveAssetPath, ValidateAssetPath,
        // and all related helper methods for serialization, validation, and type conversion.

        #region Constant Conversion

        /// <summary>
        /// Handles constant conversion operations (enum, color, layer conversions).
        /// </summary>
        /// <param name="payload">Operation parameters including 'operation' type.</param>
        /// <returns>Result dictionary with conversion data.</returns>
        /// <exception cref="InvalidOperationException">Thrown when operation is invalid or missing.</exception>
        private static object HandleConstantConvert(Dictionary<string, object> payload)
        {
            var operation = GetString(payload, "operation");
            if (string.IsNullOrEmpty(operation))
            {
                throw new InvalidOperationException("operation is required");
            }

            switch (operation)
            {
                case "enumToValue":
                    return ConvertEnumToValue(payload);
                case "valueToEnum":
                    return ConvertValueToEnum(payload);
                case "colorToRGBA":
                    return ConvertColorToRGBA(payload);
                case "rgbaToColor":
                    return ConvertRGBAToColor(payload);
                case "layerToIndex":
                    return ConvertLayerToIndex(payload);
                case "indexToLayer":
                    return ConvertIndexToLayer(payload);
                case "listEnums":
                    return ListEnumValues(payload);
                case "listColors":
                    return ListConstantColors();
                case "listLayers":
                    return ListConstantLayers();
                default:
                    throw new InvalidOperationException($"Unknown operation: {operation}");
            }
        }

        /// <summary>
        /// Converts enum name to numeric value.
        /// </summary>
        private static object ConvertEnumToValue(Dictionary<string, object> payload)
        {
            var enumTypeName = EnsureValue(GetString(payload, "enumType"), "enumType");
            var enumValueName = EnsureValue(GetString(payload, "enumValue"), "enumValue");

            var numericValue = McpConstantConverter.EnumNameToValue(enumTypeName, enumValueName);

            return new Dictionary<string, object>
            {
                ["enumType"] = enumTypeName,
                ["enumValue"] = enumValueName,
                ["numericValue"] = numericValue,
                ["success"] = true
            };
        }

        /// <summary>
        /// Converts numeric value to enum name.
        /// </summary>
        private static object ConvertValueToEnum(Dictionary<string, object> payload)
        {
            var enumTypeName = EnsureValue(GetString(payload, "enumType"), "enumType");
            var numericValue = GetInt(payload, "numericValue", 0);

            var enumValueName = McpConstantConverter.EnumValueToName(enumTypeName, numericValue);

            return new Dictionary<string, object>
            {
                ["enumType"] = enumTypeName,
                ["numericValue"] = numericValue,
                ["enumValue"] = enumValueName,
                ["success"] = true
            };
        }

        /// <summary>
        /// Converts Unity color name to RGBA values.
        /// </summary>
        private static object ConvertColorToRGBA(Dictionary<string, object> payload)
        {
            var colorName = EnsureValue(GetString(payload, "colorName"), "colorName");

            var rgba = McpConstantConverter.ColorNameToRGBA(colorName);

            return new Dictionary<string, object>
            {
                ["colorName"] = colorName,
                ["rgba"] = rgba,
                ["r"] = rgba["r"],
                ["g"] = rgba["g"],
                ["b"] = rgba["b"],
                ["a"] = rgba["a"],
                ["success"] = true
            };
        }

        /// <summary>
        /// Converts RGBA values to Unity color name (nearest match).
        /// </summary>
        private static object ConvertRGBAToColor(Dictionary<string, object> payload)
        {
            var r = GetFloat(payload, "r", 0f);
            var g = GetFloat(payload, "g", 0f);
            var b = GetFloat(payload, "b", 0f);
            var a = GetFloat(payload, "a", 1f);

            var colorName = McpConstantConverter.RGBAToColorName(r, g, b, a);

            return new Dictionary<string, object>
            {
                ["r"] = r,
                ["g"] = g,
                ["b"] = b,
                ["a"] = a,
                ["colorName"] = colorName ?? "unknown",
                ["matched"] = colorName != null,
                ["success"] = true
            };
        }

        /// <summary>
        /// Converts layer name to layer index.
        /// </summary>
        private static object ConvertLayerToIndex(Dictionary<string, object> payload)
        {
            var layerName = EnsureValue(GetString(payload, "layerName"), "layerName");

            var layerIndex = McpConstantConverter.LayerNameToIndex(layerName);

            return new Dictionary<string, object>
            {
                ["layerName"] = layerName,
                ["layerIndex"] = layerIndex,
                ["success"] = true
            };
        }

        /// <summary>
        /// Converts layer index to layer name.
        /// </summary>
        private static object ConvertIndexToLayer(Dictionary<string, object> payload)
        {
            var layerIndex = GetInt(payload, "layerIndex", 0);

            var layerName = McpConstantConverter.LayerIndexToName(layerIndex);

            return new Dictionary<string, object>
            {
                ["layerIndex"] = layerIndex,
                ["layerName"] = layerName,
                ["success"] = true
            };
        }

        /// <summary>
        /// Lists all values for a given enum type.
        /// </summary>
        private static object ListEnumValues(Dictionary<string, object> payload)
        {
            var enumTypeName = EnsureValue(GetString(payload, "enumType"), "enumType");

            var enumValues = McpConstantConverter.ListEnumValues(enumTypeName);

            return new Dictionary<string, object>
            {
                ["enumType"] = enumTypeName,
                ["values"] = enumValues,
                ["count"] = enumValues.Count,
                ["success"] = true
            };
        }

        /// <summary>
        /// Lists all Unity built-in color names.
        /// </summary>
        private static object ListConstantColors()
        {
            var colorNames = McpConstantConverter.ListColorNames();

            return new Dictionary<string, object>
            {
                ["colors"] = colorNames,
                ["count"] = colorNames.Count,
                ["success"] = true
            };
        }

        /// <summary>
        /// Lists all layer names and their indices (for constant conversion).
        /// </summary>
        private static object ListConstantLayers()
        {
            var layers = McpConstantConverter.ListLayers();

            return new Dictionary<string, object>
            {
                ["layers"] = layers,
                ["count"] = layers.Count,
                ["success"] = true
            };
        }

        #endregion

        #endregion
    }
}
