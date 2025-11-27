using System;
using System.Collections.Generic;
using MCP.Editor.Base;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor.Handlers.Settings
{
    /// <summary>
    /// タグとレイヤー管理のコマンドハンドラー。
    /// GameObject操作（setTag, getTag, setLayer, getLayer, setLayerRecursive）と
    /// Project操作（listTags, addTag, removeTag, listLayers, addLayer, removeLayer）をサポート。
    /// </summary>
    public class TagLayerManageHandler : BaseCommandHandler
    {
        public override string Category => "tagLayerManage";
        
        public override IEnumerable<string> SupportedOperations => new[]
        {
            // GameObject operations
            "setTag",
            "getTag",
            "setLayer",
            "getLayer",
            "setLayerRecursive",
            // Project operations
            "listTags",
            "addTag",
            "removeTag",
            "listLayers",
            "addLayer",
            "removeLayer",
        };
        
        public TagLayerManageHandler() : base()
        {
        }
        
        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            Debug.Log($"[TagLayerManageHandler] Processing operation: {operation}");
            
            return operation switch
            {
                // GameObject operations
                "setTag" => SetTag(payload),
                "getTag" => GetTag(payload),
                "setLayer" => SetLayer(payload),
                "getLayer" => GetLayer(payload),
                "setLayerRecursive" => SetLayerRecursive(payload),
                // Project operations
                "listTags" => ListTags(),
                "addTag" => AddTag(payload),
                "removeTag" => RemoveTag(payload),
                "listLayers" => ListLayers(),
                "addLayer" => AddLayer(payload),
                "removeLayer" => RemoveLayer(payload),
                _ => throw new InvalidOperationException($"Unknown operation: {operation}")
            };
        }
        
        protected override bool RequiresCompilationWait(string operation)
        {
            // Read-only operations don't require compilation wait
            return operation != "getTag" && operation != "getLayer" && operation != "listTags" && operation != "listLayers";
        }
        
        #region GameObject Operations
        
        private object SetTag(Dictionary<string, object> payload)
        {
            var path = GetString(payload, "gameObjectPath");
            if (string.IsNullOrEmpty(path))
            {
                throw new InvalidOperationException("gameObjectPath is required");
            }
            
            var tag = GetString(payload, "tag");
            if (string.IsNullOrEmpty(tag))
            {
                throw new InvalidOperationException("tag is required");
            }
            
            var target = GameObjectResolver.Resolve(path);
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
        
        private object GetTag(Dictionary<string, object> payload)
        {
            var path = GetString(payload, "gameObjectPath");
            if (string.IsNullOrEmpty(path))
            {
                throw new InvalidOperationException("gameObjectPath is required");
            }
            
            var target = GameObjectResolver.Resolve(path);
            
            return new Dictionary<string, object>
            {
                ["gameObjectPath"] = path,
                ["tag"] = target.tag,
                ["operation"] = "getTag",
            };
        }
        
        private object SetLayer(Dictionary<string, object> payload)
        {
            var path = GetString(payload, "gameObjectPath");
            if (string.IsNullOrEmpty(path))
            {
                throw new InvalidOperationException("gameObjectPath is required");
            }
            
            var target = GameObjectResolver.Resolve(path);
            
            int newLayer = ResolveLayerFromPayload(payload);
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
        
        private object GetLayer(Dictionary<string, object> payload)
        {
            var path = GetString(payload, "gameObjectPath");
            if (string.IsNullOrEmpty(path))
            {
                throw new InvalidOperationException("gameObjectPath is required");
            }
            
            var target = GameObjectResolver.Resolve(path);
            
            return new Dictionary<string, object>
            {
                ["gameObjectPath"] = path,
                ["layer"] = target.layer,
                ["layerName"] = LayerMask.LayerToName(target.layer),
                ["operation"] = "getLayer",
            };
        }
        
        private object SetLayerRecursive(Dictionary<string, object> payload)
        {
            var path = GetString(payload, "gameObjectPath");
            if (string.IsNullOrEmpty(path))
            {
                throw new InvalidOperationException("gameObjectPath is required");
            }
            
            var target = GameObjectResolver.Resolve(path);
            int newLayer = ResolveLayerFromPayload(payload);
            
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
        
        private void SetLayerRecursiveInternal(GameObject obj, int layer, ref int count)
        {
            obj.layer = layer;
            EditorUtility.SetDirty(obj);
            count++;
            
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursiveInternal(child.gameObject, layer, ref count);
            }
        }
        
        #endregion
        
        #region Project Operations - Tags
        
        private object ListTags()
        {
            var tags = UnityEditorInternal.InternalEditorUtility.tags;
            
            return new Dictionary<string, object>
            {
                ["tags"] = new List<string>(tags),
                ["count"] = tags.Length,
                ["operation"] = "listTags",
            };
        }
        
        private object AddTag(Dictionary<string, object> payload)
        {
            var tag = GetString(payload, "tag");
            if (string.IsNullOrEmpty(tag))
            {
                throw new InvalidOperationException("tag is required");
            }
            
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
        
        private object RemoveTag(Dictionary<string, object> payload)
        {
            var tag = GetString(payload, "tag");
            if (string.IsNullOrEmpty(tag))
            {
                throw new InvalidOperationException("tag is required");
            }
            
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
        
        #endregion
        
        #region Project Operations - Layers
        
        private object ListLayers()
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
        
        private object AddLayer(Dictionary<string, object> payload)
        {
            var layer = GetString(payload, "layer");
            if (string.IsNullOrEmpty(layer))
            {
                throw new InvalidOperationException("layer is required");
            }
            
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
        
        private object RemoveLayer(Dictionary<string, object> payload)
        {
            var layer = GetString(payload, "layer");
            if (string.IsNullOrEmpty(layer))
            {
                throw new InvalidOperationException("layer is required");
            }
            
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
        
        #endregion
        
        #region Helper Methods
        
        private int ResolveLayerFromPayload(Dictionary<string, object> payload)
        {
            if (!payload.TryGetValue("layer", out var layerObj))
            {
                throw new InvalidOperationException("layer is required");
            }
            
            if (layerObj is string layerName)
            {
                int layer = LayerMask.NameToLayer(layerName);
                if (layer == -1)
                {
                    throw new InvalidOperationException($"Layer '{layerName}' does not exist in the project. Use addLayer operation to create it first.");
                }
                return layer;
            }
            else if (layerObj is int layerIndex)
            {
                return layerIndex;
            }
            else if (layerObj is double layerDouble)
            {
                return (int)layerDouble;
            }
            else
            {
                throw new InvalidOperationException("layer must be a string (layer name) or integer (layer index)");
            }
        }
        
        #endregion
    }
}

