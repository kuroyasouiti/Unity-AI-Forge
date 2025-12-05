using System;
using System.Collections.Generic;
using MCP.Editor.Base;
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;

namespace MCP.Editor.Handlers.Settings
{
    /// <summary>
    /// プロジェクト設定管理のコマンドハンドラー。
    /// Player, Quality, Time, Physics, Audio, Editor設定の読み書きをサポート。
    /// </summary>
    public class ProjectSettingsManageHandler : BaseCommandHandler
    {
        public override string Category => "projectSettingsManage";
        
        public override IEnumerable<string> SupportedOperations => new[]
        {
            "read",
            "write",
            "list",
            "addSceneToBuild",
            "removeSceneFromBuild",
            "listBuildScenes",
            "reorderBuildScenes",
            "setBuildSceneEnabled",
        };
        
        public ProjectSettingsManageHandler() : base()
        {
        }
        
        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "read" => ReadProjectSettings(payload),
                "write" => WriteProjectSettings(payload),
                "list" => ListProjectSettings(payload),
                "addSceneToBuild" => AddSceneToBuild(payload),
                "removeSceneFromBuild" => RemoveSceneFromBuild(payload),
                "listBuildScenes" => ListBuildScenes(payload),
                "reorderBuildScenes" => ReorderBuildScenes(payload),
                "setBuildSceneEnabled" => SetBuildSceneEnabled(payload),
                _ => throw new InvalidOperationException($"Unknown operation: {operation}")
            };
        }
        
        protected override bool RequiresCompilationWait(string operation)
        {
            // Read and list operations don't require compilation wait
            return operation != "read" && operation != "list" && operation != "listBuildScenes";
        }
        
        #region Main Operations
        
        private object ReadProjectSettings(Dictionary<string, object> payload)
        {
            var category = GetString(payload, "category");
            if (string.IsNullOrEmpty(category))
            {
                throw new InvalidOperationException("category is required");
            }
            
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
                case "physics2d":
                    result["settings"] = ReadPhysics2DSettings(property);
                    break;
                case "audio":
                    result["settings"] = ReadAudioSettings(property);
                    break;
                case "editor":
                    result["settings"] = ReadEditorSettings(property);
                    break;
                case "tagslayers":
                    result["settings"] = ReadTagsAndLayers(property);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown settings category: {category}");
            }
            
            return result;
        }
        
        private object WriteProjectSettings(Dictionary<string, object> payload)
        {
            var category = GetString(payload, "category");
            if (string.IsNullOrEmpty(category))
            {
                throw new InvalidOperationException("category is required");
            }
            
            var property = GetString(payload, "property");
            if (string.IsNullOrEmpty(property))
            {
                throw new InvalidOperationException("property is required");
            }
            
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
                case "physics2d":
                    WritePhysics2DSettings(property, value);
                    break;
                case "audio":
                    WriteAudioSettings(property, value);
                    break;
                case "editor":
                    WriteEditorSettings(property, value);
                    break;
                case "tagslayers":
                    WriteTagsAndLayers(property, value);
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
        
        private object ListProjectSettings(Dictionary<string, object> payload)
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
                        "physics2d",
                        "audio",
                        "editor",
                        "tagsLayers",
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
                "physics2d" => new List<string>
                {
                    "gravity", "velocityIterations", "positionIterations",
                    "velocityThreshold", "maxLinearCorrection", "maxAngularCorrection",
                    "maxTranslationSpeed", "maxRotationSpeed", "baumgarteScale",
                    "timeToSleep", "linearSleepTolerance", "angularSleepTolerance",
                    "defaultContactOffset", "autoSimulation", "queriesHitTriggers",
                    "queriesStartInColliders", "callbacksOnDisable", "reuseCollisionCallbacks",
                    "autoSyncTransforms", "simulationMode",
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
                "tagslayers" => new List<string>
                {
                    "tags",
                    "layers",
                    "sortingLayers",
                    "addTag",
                    "removeTag",
                    "addLayer",
                    "removeLayer",
                    "addSortingLayer",
                    "removeSortingLayer",
                },
                _ => throw new InvalidOperationException($"Unknown settings category: {category}"),
            };
            
            return new Dictionary<string, object>
            {
                ["category"] = category,
                ["properties"] = properties,
            };
        }
        
        #endregion

        #region Tags & Layers

        private object ReadTagsAndLayers(string property)
        {
            var tags = InternalEditorUtility.tags;
            var layers = InternalEditorUtility.layers;
            var sortingLayers = GetSortingLayerNames();

            if (string.IsNullOrEmpty(property))
            {
                return new Dictionary<string, object>
                {
                    ["tags"] = tags,
                    ["layers"] = layers,
                    ["sortingLayers"] = sortingLayers,
                };
            }

            return property.ToLower() switch
            {
                "tags" => tags,
                "layers" => layers,
                "sortinglayers" => sortingLayers,
                _ => throw new InvalidOperationException($"Unknown tagsLayers property: {property}"),
            };
        }
        
        private string[] GetSortingLayerNames()
        {
            var tagManager = GetTagManagerSerializedObject();
            var sortingLayersProp = tagManager.FindProperty("m_SortingLayers");
            var names = new List<string>();
            
            for (int i = 0; i < sortingLayersProp.arraySize; i++)
            {
                var layerProp = sortingLayersProp.GetArrayElementAtIndex(i);
                var nameProp = layerProp.FindPropertyRelative("name");
                if (nameProp != null)
                {
                    names.Add(nameProp.stringValue);
                }
            }
            
            return names.ToArray();
        }

        private void WriteTagsAndLayers(string property, object value)
        {
            var stringValue = value?.ToString();
            if (string.IsNullOrWhiteSpace(stringValue))
            {
                throw new InvalidOperationException("A non-empty string value is required for tag/layer operations.");
            }

            switch (property.ToLower())
            {
                case "addtag":
                    AddTag(stringValue);
                    break;
                case "removetag":
                    RemoveTag(stringValue);
                    break;
                case "addlayer":
                    AddLayer(stringValue);
                    break;
                case "removelayer":
                    RemoveLayer(stringValue);
                    break;
                case "addsortinglayer":
                    AddSortingLayer(stringValue);
                    break;
                case "removesortinglayer":
                    RemoveSortingLayer(stringValue);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported tagsLayers property: {property}");
            }
        }

        private void AddTag(string tag)
        {
            if (Array.Exists(InternalEditorUtility.tags, existing => existing == tag))
            {
                throw new InvalidOperationException($"Tag '{tag}' already exists.");
            }

            var tagManager = GetTagManagerSerializedObject();
            var tagsProp = tagManager.FindProperty("tags");
            tagsProp.arraySize++;
            tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
            tagManager.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }

        private void RemoveTag(string tag)
        {
            var tagManager = GetTagManagerSerializedObject();
            var tagsProp = tagManager.FindProperty("tags");
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag)
                {
                    tagsProp.DeleteArrayElementAtIndex(i);
                    tagManager.ApplyModifiedProperties();
                    AssetDatabase.SaveAssets();
                    return;
                }
            }

            throw new InvalidOperationException($"Tag '{tag}' does not exist.");
        }

        private void AddLayer(string layerName)
        {
            foreach (var layer in InternalEditorUtility.layers)
            {
                if (layer == layerName)
                {
                    throw new InvalidOperationException($"Layer '{layerName}' already exists.");
                }
            }

            var tagManager = GetTagManagerSerializedObject();
            var layersProp = tagManager.FindProperty("layers");
            for (int i = 8; i < layersProp.arraySize; i++)
            {
                var sp = layersProp.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(sp.stringValue))
                {
                    sp.stringValue = layerName;
                    tagManager.ApplyModifiedProperties();
                    AssetDatabase.SaveAssets();
                    return;
                }
            }

            throw new InvalidOperationException("Maximum number of layers reached. Remove an existing layer before adding a new one.");
        }

        private void RemoveLayer(string layerNameOrIndex)
        {
            var tagManager = GetTagManagerSerializedObject();
            var layersProp = tagManager.FindProperty("layers");

            bool FoundByName()
            {
                for (int i = 8; i < layersProp.arraySize; i++)
                {
                    var sp = layersProp.GetArrayElementAtIndex(i);
                    if (sp.stringValue == layerNameOrIndex)
                    {
                        sp.stringValue = string.Empty;
                        tagManager.ApplyModifiedProperties();
                        AssetDatabase.SaveAssets();
                        return true;
                    }
                }
                return false;
            }

            if (FoundByName())
            {
                return;
            }

            if (int.TryParse(layerNameOrIndex, out var index))
            {
                if (index < 8 || index >= layersProp.arraySize)
                {
                    throw new InvalidOperationException("Layer index must be between 8 and 31 (user layers).");
                }

                layersProp.GetArrayElementAtIndex(index).stringValue = string.Empty;
                tagManager.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
                return;
            }

            throw new InvalidOperationException($"Layer '{layerNameOrIndex}' does not exist.");
        }

        private void AddSortingLayer(string layerName)
        {
            var tagManager = GetTagManagerSerializedObject();
            var sortingLayersProp = tagManager.FindProperty("m_SortingLayers");
            
            // Check if layer already exists
            for (int i = 0; i < sortingLayersProp.arraySize; i++)
            {
                var layerProp = sortingLayersProp.GetArrayElementAtIndex(i);
                var nameProp = layerProp.FindPropertyRelative("name");
                if (nameProp != null && nameProp.stringValue == layerName)
                {
                    throw new InvalidOperationException($"Sorting layer '{layerName}' already exists.");
                }
            }
            
            // Add new sorting layer
            sortingLayersProp.arraySize++;
            var newLayerProp = sortingLayersProp.GetArrayElementAtIndex(sortingLayersProp.arraySize - 1);
            var newNameProp = newLayerProp.FindPropertyRelative("name");
            var newIdProp = newLayerProp.FindPropertyRelative("uniqueID");
            
            if (newNameProp != null)
            {
                newNameProp.stringValue = layerName;
            }
            
            // Assign unique ID (use timestamp-based ID)
            if (newIdProp != null)
            {
                var maxId = 0;
                for (int i = 0; i < sortingLayersProp.arraySize - 1; i++)
                {
                    var idProp = sortingLayersProp.GetArrayElementAtIndex(i).FindPropertyRelative("uniqueID");
                    if (idProp != null && idProp.intValue > maxId)
                    {
                        maxId = idProp.intValue;
                    }
                }
                newIdProp.intValue = maxId + 1;
            }
            
            tagManager.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }
        
        private void RemoveSortingLayer(string layerName)
        {
            var tagManager = GetTagManagerSerializedObject();
            var sortingLayersProp = tagManager.FindProperty("m_SortingLayers");
            
            // Find and remove the sorting layer
            for (int i = 0; i < sortingLayersProp.arraySize; i++)
            {
                var layerProp = sortingLayersProp.GetArrayElementAtIndex(i);
                var nameProp = layerProp.FindPropertyRelative("name");
                
                if (nameProp != null && nameProp.stringValue == layerName)
                {
                    // Don't allow removing the Default sorting layer (index 0)
                    if (i == 0)
                    {
                        throw new InvalidOperationException("Cannot remove the 'Default' sorting layer.");
                    }
                    
                    sortingLayersProp.DeleteArrayElementAtIndex(i);
                    tagManager.ApplyModifiedProperties();
                    AssetDatabase.SaveAssets();
                    return;
                }
            }
            
            throw new InvalidOperationException($"Sorting layer '{layerName}' does not exist.");
        }

        private SerializedObject GetTagManagerSerializedObject()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0)
            {
                throw new InvalidOperationException("Unable to load TagManager asset.");
            }

            return new SerializedObject(assets[0]);
        }

        #endregion
        
        #region PlayerSettings Read/Write
        
        private object ReadPlayerSettings(string property)
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
        
        private void WritePlayerSettings(string property, object value)
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
        
        #endregion
        
        #region QualitySettings Read/Write
        
        private object ReadQualitySettings(string property)
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
        
        private void WriteQualitySettings(string property, object value)
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
        
        #endregion
        
        #region TimeSettings Read/Write
        
        private object ReadTimeSettings(string property)
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
        
        private void WriteTimeSettings(string property, object value)
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
        
        #endregion
        
        #region PhysicsSettings Read/Write
        
        private object ReadPhysicsSettings(string property)
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
                "autosimulation" => Physics.autoSimulation,
                _ => throw new InvalidOperationException($"Unknown PhysicsSettings property: {property}"),
            };
        }
        
        private void WritePhysicsSettings(string property, object value)
        {
            switch (property.ToLower())
            {
                case "gravity":
                    if (value is Dictionary<string, object> gravityDict)
                    {
                        var x = gravityDict.ContainsKey("x") ? Convert.ToSingle(gravityDict["x"]) : Physics.gravity.x;
                        var y = gravityDict.ContainsKey("y") ? Convert.ToSingle(gravityDict["y"]) : Physics.gravity.y;
                        var z = gravityDict.ContainsKey("z") ? Convert.ToSingle(gravityDict["z"]) : Physics.gravity.z;
                        Physics.gravity = new Vector3(x, y, z);
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
                case "autosimulation":
                    Physics.autoSimulation = Convert.ToBoolean(value);
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
        
        #endregion
        
        #region Physics2DSettings Read/Write
        
        private object ReadPhysics2DSettings(string property)
        {
            if (string.IsNullOrEmpty(property))
            {
                return new Dictionary<string, object>
                {
                    ["gravity"] = new Dictionary<string, object>
                    {
                        ["x"] = Physics2D.gravity.x,
                        ["y"] = Physics2D.gravity.y,
                    },
                    ["velocityIterations"] = Physics2D.velocityIterations,
                    ["positionIterations"] = Physics2D.positionIterations,
                    ["velocityThreshold"] = Physics2D.bounceThreshold,
                    ["maxLinearCorrection"] = Physics2D.maxLinearCorrection,
                    ["maxAngularCorrection"] = Physics2D.maxAngularCorrection,
                    ["maxTranslationSpeed"] = Physics2D.maxTranslationSpeed,
                    ["maxRotationSpeed"] = Physics2D.maxRotationSpeed,
                    ["defaultContactOffset"] = Physics2D.defaultContactOffset,
                    ["queriesHitTriggers"] = Physics2D.queriesHitTriggers,
                    ["queriesStartInColliders"] = Physics2D.queriesStartInColliders,
                    ["callbacksOnDisable"] = Physics2D.callbacksOnDisable,
                    ["reuseCollisionCallbacks"] = Physics2D.reuseCollisionCallbacks,
                    ["autoSyncTransforms"] = Physics2D.autoSyncTransforms,
                    ["autoSimulation"] = Physics2D.simulationMode == SimulationMode2D.FixedUpdate,
                };
            }
            
            return property.ToLower() switch
            {
                "gravity" => new Dictionary<string, object>
                {
                    ["x"] = Physics2D.gravity.x,
                    ["y"] = Physics2D.gravity.y,
                },
                "velocityiterations" => Physics2D.velocityIterations,
                "positioniterations" => Physics2D.positionIterations,
                "velocitythreshold" => Physics2D.bounceThreshold,
                "maxlinearcorrection" => Physics2D.maxLinearCorrection,
                "maxangularcorrection" => Physics2D.maxAngularCorrection,
                "maxtranslationspeed" => Physics2D.maxTranslationSpeed,
                "maxrotationspeed" => Physics2D.maxRotationSpeed,
                "baumgartescale" => Physics2D.baumgarteScale,
                "timetosleep" => Physics2D.timeToSleep,
                "linearsleeptolerance" => Physics2D.linearSleepTolerance,
                "angularsleeptolerance" => Physics2D.angularSleepTolerance,
                "defaultcontactoffset" => Physics2D.defaultContactOffset,
                "querieshittriggers" => Physics2D.queriesHitTriggers,
                "queriesstartincolliders" => Physics2D.queriesStartInColliders,
                "callbacksondisable" => Physics2D.callbacksOnDisable,
                "reusecollisioncallbacks" => Physics2D.reuseCollisionCallbacks,
                "autosynctransforms" => Physics2D.autoSyncTransforms,
                "autosimulation" => Physics2D.simulationMode == SimulationMode2D.FixedUpdate,
                "simulationmode" => Physics2D.simulationMode.ToString(),
                _ => throw new InvalidOperationException($"Unknown Physics2D property: {property}"),
            };
        }
        
        private void WritePhysics2DSettings(string property, object value)
        {
            switch (property.ToLower())
            {
                case "gravity":
                    if (value is Dictionary<string, object> gravityDict)
                    {
                        var x = gravityDict.ContainsKey("x") ? Convert.ToSingle(gravityDict["x"]) : Physics2D.gravity.x;
                        var y = gravityDict.ContainsKey("y") ? Convert.ToSingle(gravityDict["y"]) : Physics2D.gravity.y;
                        Physics2D.gravity = new Vector2(x, y);
                    }
                    break;
                case "velocityiterations":
                    Physics2D.velocityIterations = Convert.ToInt32(value);
                    break;
                case "positioniterations":
                    Physics2D.positionIterations = Convert.ToInt32(value);
                    break;
                case "velocitythreshold":
                    Physics2D.bounceThreshold = Convert.ToSingle(value);
                    break;
                case "maxlinearcorrection":
                    Physics2D.maxLinearCorrection = Convert.ToSingle(value);
                    break;
                case "maxangularcorrection":
                    Physics2D.maxAngularCorrection = Convert.ToSingle(value);
                    break;
                case "maxtranslationspeed":
                    Physics2D.maxTranslationSpeed = Convert.ToSingle(value);
                    break;
                case "maxrotationspeed":
                    Physics2D.maxRotationSpeed = Convert.ToSingle(value);
                    break;
                case "baumgartescale":
                    Physics2D.baumgarteScale = Convert.ToSingle(value);
                    break;
                case "timetosleep":
                    Physics2D.timeToSleep = Convert.ToSingle(value);
                    break;
                case "linearsleeptolerance":
                    Physics2D.linearSleepTolerance = Convert.ToSingle(value);
                    break;
                case "angularsleeptolerance":
                    Physics2D.angularSleepTolerance = Convert.ToSingle(value);
                    break;
                case "defaultcontactoffset":
                    Physics2D.defaultContactOffset = Convert.ToSingle(value);
                    break;
                case "querieshittriggers":
                    Physics2D.queriesHitTriggers = Convert.ToBoolean(value);
                    break;
                case "queriesstartincolliders":
                    Physics2D.queriesStartInColliders = Convert.ToBoolean(value);
                    break;
                case "callbacksondisable":
                    Physics2D.callbacksOnDisable = Convert.ToBoolean(value);
                    break;
                case "reusecollisioncallbacks":
                    Physics2D.reuseCollisionCallbacks = Convert.ToBoolean(value);
                    break;
                case "autosynctransforms":
                    Physics2D.autoSyncTransforms = Convert.ToBoolean(value);
                    break;
                case "autosimulation":
                    Physics2D.simulationMode = Convert.ToBoolean(value) ? SimulationMode2D.FixedUpdate : SimulationMode2D.Script;
                    break;
                case "simulationmode":
                    if (Enum.TryParse<SimulationMode2D>(value.ToString(), true, out var simMode))
                    {
                        Physics2D.simulationMode = simMode;
                    }
                    else
                    {
                        throw new InvalidOperationException($"Invalid SimulationMode2D value: {value}");
                    }
                    break;
                default:
                    throw new InvalidOperationException($"Unknown Physics2D property: {property}");
            }
        }
        
        #endregion
        
        #region AudioSettings Read/Write
        
        private object ReadAudioSettings(string property)
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
        
        private void WriteAudioSettings(string property, object value)
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
        
        #endregion
        
        #region EditorSettings Read/Write
        
        private object ReadEditorSettings(string property)
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
        
        private void WriteEditorSettings(string property, object value)
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
        
        #endregion
        
        #region Build Settings
        
        /// <summary>
        /// Lists all scenes in the build settings.
        /// </summary>
        private object ListBuildScenes(Dictionary<string, object> payload)
        {
            var scenes = EditorBuildSettings.scenes;
            var sceneList = scenes.Select((scene, index) => new Dictionary<string, object>
            {
                ["index"] = index,
                ["path"] = scene.path,
                ["guid"] = scene.guid.ToString(),
                ["enabled"] = scene.enabled
            }).ToList();
            
            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["scenes"] = sceneList,
                ["count"] = scenes.Length
            };
        }
        
        /// <summary>
        /// Adds a scene to the build settings.
        /// </summary>
        private object AddSceneToBuild(Dictionary<string, object> payload)
        {
            var scenePath = GetString(payload, "scenePath");
            if (string.IsNullOrEmpty(scenePath))
            {
                throw new InvalidOperationException("scenePath is required");
            }
            
            // Verify scene exists
            var sceneAsset = AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>(scenePath);
            if (sceneAsset == null)
            {
                throw new InvalidOperationException($"Scene not found: {scenePath}");
            }
            
            // Check if scene is already in build settings
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == scenePath))
            {
                throw new InvalidOperationException($"Scene '{scenePath}' is already in build settings");
            }
            
            // Get optional index parameter
            var index = GetInt(payload, "index", -1);
            var enabled = GetBool(payload, "enabled", true);
            
            // Create new scene entry
            var newScene = new EditorBuildSettingsScene(scenePath, enabled);
            
            // Add at specified index or at the end
            if (index >= 0 && index < scenes.Count)
            {
                scenes.Insert(index, newScene);
            }
            else
            {
                scenes.Add(newScene);
                index = scenes.Count - 1;
            }
            
            EditorBuildSettings.scenes = scenes.ToArray();
            
            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["scenePath"] = scenePath,
                ["index"] = index,
                ["enabled"] = enabled,
                ["message"] = "Scene added to build settings"
            };
        }
        
        /// <summary>
        /// Removes a scene from the build settings.
        /// </summary>
        private object RemoveSceneFromBuild(Dictionary<string, object> payload)
        {
            var scenePath = GetString(payload, "scenePath");
            var sceneIndex = GetInt(payload, "index", -1);
            
            if (string.IsNullOrEmpty(scenePath) && sceneIndex < 0)
            {
                throw new InvalidOperationException("Either scenePath or index is required");
            }
            
            var scenes = EditorBuildSettings.scenes.ToList();
            
            // Remove by path
            if (!string.IsNullOrEmpty(scenePath))
            {
                var removed = scenes.RemoveAll(s => s.path == scenePath);
                if (removed == 0)
                {
                    throw new InvalidOperationException($"Scene '{scenePath}' not found in build settings");
                }
            }
            // Remove by index
            else if (sceneIndex >= 0 && sceneIndex < scenes.Count)
            {
                scenePath = scenes[sceneIndex].path;
                scenes.RemoveAt(sceneIndex);
            }
            else
            {
                throw new InvalidOperationException($"Invalid scene index: {sceneIndex}");
            }
            
            EditorBuildSettings.scenes = scenes.ToArray();
            
            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["scenePath"] = scenePath,
                ["message"] = "Scene removed from build settings"
            };
        }
        
        /// <summary>
        /// Reorders a scene in the build settings.
        /// </summary>
        private object ReorderBuildScenes(Dictionary<string, object> payload)
        {
            var fromIndex = GetInt(payload, "fromIndex", -1);
            var toIndex = GetInt(payload, "toIndex", -1);
            
            if (fromIndex < 0 || toIndex < 0)
            {
                throw new InvalidOperationException("Both fromIndex and toIndex are required");
            }
            
            var scenes = EditorBuildSettings.scenes.ToList();
            
            if (fromIndex >= scenes.Count || toIndex >= scenes.Count)
            {
                throw new InvalidOperationException($"Invalid index. Scene count: {scenes.Count}");
            }
            
            var scene = scenes[fromIndex];
            scenes.RemoveAt(fromIndex);
            scenes.Insert(toIndex, scene);
            
            EditorBuildSettings.scenes = scenes.ToArray();
            
            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["scenePath"] = scene.path,
                ["fromIndex"] = fromIndex,
                ["toIndex"] = toIndex,
                ["message"] = "Scene reordered in build settings"
            };
        }
        
        /// <summary>
        /// Enables or disables a scene in the build settings.
        /// </summary>
        private object SetBuildSceneEnabled(Dictionary<string, object> payload)
        {
            var scenePath = GetString(payload, "scenePath");
            var sceneIndex = GetInt(payload, "index", -1);
            var enabled = GetBool(payload, "enabled", true);
            
            if (string.IsNullOrEmpty(scenePath) && sceneIndex < 0)
            {
                throw new InvalidOperationException("Either scenePath or index is required");
            }
            
            var scenes = EditorBuildSettings.scenes;
            int targetIndex = -1;
            
            // Find by path
            if (!string.IsNullOrEmpty(scenePath))
            {
                for (int i = 0; i < scenes.Length; i++)
                {
                    if (scenes[i].path == scenePath)
                    {
                        targetIndex = i;
                        break;
                    }
                }
                
                if (targetIndex < 0)
                {
                    throw new InvalidOperationException($"Scene '{scenePath}' not found in build settings");
                }
            }
            // Find by index
            else if (sceneIndex >= 0 && sceneIndex < scenes.Length)
            {
                targetIndex = sceneIndex;
                scenePath = scenes[sceneIndex].path;
            }
            else
            {
                throw new InvalidOperationException($"Invalid scene index: {sceneIndex}");
            }
            
            // Update the scene
            scenes[targetIndex].enabled = enabled;
            EditorBuildSettings.scenes = scenes;
            
            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["scenePath"] = scenePath,
                ["index"] = targetIndex,
                ["enabled"] = enabled,
                ["message"] = $"Scene {(enabled ? "enabled" : "disabled")} in build settings"
            };
        }
        
        #endregion
    }
}

