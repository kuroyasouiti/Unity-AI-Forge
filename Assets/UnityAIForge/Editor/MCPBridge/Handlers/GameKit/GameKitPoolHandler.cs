using System;
using System.Collections.Generic;
using MCP.Editor.Base;
using MCP.Editor.CodeGen;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MCP.Editor.Handlers.GameKit
{
    /// <summary>
    /// GameKit Object Pool handler: create and manage object pools.
    /// Uses code generation to produce standalone pooling scripts using UnityEngine.Pool.
    /// </summary>
    public class GameKitPoolHandler : BaseCommandHandler
    {
        private static readonly string[] Operations =
        {
            "create", "update", "inspect", "delete", "findByPoolId"
        };

        public override string Category => "gamekitPool";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) =>
            operation == "create";

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "create" => CreatePool(payload),
                "update" => UpdatePool(payload),
                "inspect" => InspectPool(payload),
                "delete" => DeletePool(payload),
                "findByPoolId" => FindByPoolId(payload),
                _ => throw new InvalidOperationException($"Unsupported GameKit Pool operation: {operation}")
            };
        }

        #region Create

        private object CreatePool(Dictionary<string, object> payload)
        {
            var targetPath = GetString(payload, "targetPath");
            if (string.IsNullOrEmpty(targetPath))
                throw new InvalidOperationException("targetPath is required for create operation.");

            var targetGo = ResolveGameObject(targetPath);
            if (targetGo == null)
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");

            var existing = CodeGenHelper.FindComponentByField(targetGo, "poolId", null);
            if (existing != null)
                throw new InvalidOperationException($"GameObject '{targetPath}' already has a Pool component.");

            var poolId = GetString(payload, "poolId") ?? $"Pool_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var initialSize = GetInt(payload, "initialSize", 10);
            var maxSize = GetInt(payload, "maxSize", 100);
            var collectionCheck = GetBool(payload, "collectionCheck", true);

            var className = ScriptGenerator.ToPascalCase(poolId, "Pool");

            var variables = new Dictionary<string, object>
            {
                { "POOL_ID", poolId },
                { "INITIAL_SIZE", initialSize },
                { "MAX_SIZE", maxSize },
                { "COLLECTION_CHECK", collectionCheck }
            };

            var propertiesToSet = new Dictionary<string, object>();
            if (payload.TryGetValue("prefabPath", out var prefabPathObj))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPathObj.ToString());
                if (prefab != null)
                    propertiesToSet["prefab"] = prefab;
            }

            if (payload.TryGetValue("defaultParentPath", out var parentPathObj))
            {
                var parentGo = GameObject.Find(parentPathObj.ToString());
                if (parentGo != null)
                    propertiesToSet["defaultParent"] = parentGo.transform;
            }

            var result = CodeGenHelper.GenerateAndAttach(
                targetGo, "ObjectPool", poolId, className, variables, null,
                propertiesToSet.Count > 0 ? propertiesToSet : null);

            if (result.TryGetValue("success", out var success) && !(bool)success)
            {
                throw new InvalidOperationException(result.TryGetValue("error", out var err)
                    ? err.ToString()
                    : "Failed to generate ObjectPool script.");
            }

            EditorSceneManager.MarkSceneDirty(targetGo.scene);

            result["poolId"] = poolId;
            result["path"] = BuildGameObjectPath(targetGo);
            result["initialSize"] = initialSize;
            result["maxSize"] = maxSize;

            return result;
        }

        #endregion

        #region Update

        private object UpdatePool(Dictionary<string, object> payload)
        {
            var component = ResolvePoolComponent(payload);
            Undo.RecordObject(component, "Update Pool");

            var so = new SerializedObject(component);

            if (payload.TryGetValue("initialSize", out var initObj))
                so.FindProperty("initialSize").intValue = Convert.ToInt32(initObj);

            if (payload.TryGetValue("maxSize", out var maxObj))
                so.FindProperty("maxSize").intValue = Convert.ToInt32(maxObj);

            if (payload.TryGetValue("collectionCheck", out var ccObj))
                so.FindProperty("collectionCheck").boolValue = Convert.ToBoolean(ccObj);

            if (payload.TryGetValue("prefabPath", out var prefabPathObj))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPathObj.ToString());
                if (prefab != null)
                    so.FindProperty("prefab").objectReferenceValue = prefab;
            }

            if (payload.TryGetValue("defaultParentPath", out var parentPathObj))
            {
                var parentGo = GameObject.Find(parentPathObj.ToString());
                if (parentGo != null)
                    so.FindProperty("defaultParent").objectReferenceValue = parentGo.transform;
            }

            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            var poolId = new SerializedObject(component).FindProperty("poolId").stringValue;

            return CreateSuccessResponse(
                ("poolId", poolId),
                ("path", BuildGameObjectPath(component.gameObject)),
                ("updated", true)
            );
        }

        #endregion

        #region Inspect

        private object InspectPool(Dictionary<string, object> payload)
        {
            var component = ResolvePoolComponent(payload);
            var so = new SerializedObject(component);

            var prefab = so.FindProperty("prefab").objectReferenceValue;
            var defaultParent = so.FindProperty("defaultParent").objectReferenceValue;

            var info = new Dictionary<string, object>
            {
                { "poolId", so.FindProperty("poolId").stringValue },
                { "path", BuildGameObjectPath(component.gameObject) },
                { "initialSize", so.FindProperty("initialSize").intValue },
                { "maxSize", so.FindProperty("maxSize").intValue },
                { "collectionCheck", so.FindProperty("collectionCheck").boolValue },
                { "hasPrefab", prefab != null },
                { "prefab", prefab != null ? prefab.name : null },
                { "hasDefaultParent", defaultParent != null },
                { "defaultParent", defaultParent != null ? defaultParent.name : null }
            };

            return CreateSuccessResponse(("pool", info));
        }

        #endregion

        #region Delete

        private object DeletePool(Dictionary<string, object> payload)
        {
            var component = ResolvePoolComponent(payload);
            var path = BuildGameObjectPath(component.gameObject);
            var poolId = new SerializedObject(component).FindProperty("poolId").stringValue;
            var scene = component.gameObject.scene;

            Undo.DestroyObjectImmediate(component);
            ScriptGenerator.Delete(poolId);
            EditorSceneManager.MarkSceneDirty(scene);

            return CreateSuccessResponse(
                ("poolId", poolId),
                ("path", path),
                ("deleted", true)
            );
        }

        #endregion

        #region Find

        private object FindByPoolId(Dictionary<string, object> payload)
        {
            var poolId = GetString(payload, "poolId");
            if (string.IsNullOrEmpty(poolId))
                throw new InvalidOperationException("poolId is required for findByPoolId.");

            var component = CodeGenHelper.FindComponentInSceneByField("poolId", poolId);
            if (component == null)
                return CreateSuccessResponse(("found", false), ("poolId", poolId));

            var so = new SerializedObject(component);

            return CreateSuccessResponse(
                ("found", true),
                ("poolId", poolId),
                ("path", BuildGameObjectPath(component.gameObject)),
                ("initialSize", so.FindProperty("initialSize").intValue),
                ("maxSize", so.FindProperty("maxSize").intValue)
            );
        }

        #endregion

        #region Helpers

        private Component ResolvePoolComponent(Dictionary<string, object> payload)
        {
            var poolId = GetString(payload, "poolId");
            if (!string.IsNullOrEmpty(poolId))
            {
                var comp = CodeGenHelper.FindComponentInSceneByField("poolId", poolId);
                if (comp != null) return comp;
            }

            var targetPath = GetString(payload, "targetPath");
            if (!string.IsNullOrEmpty(targetPath))
            {
                var targetGo = ResolveGameObject(targetPath);
                if (targetGo != null)
                {
                    var comp = CodeGenHelper.FindComponentByField(targetGo, "poolId", null);
                    if (comp != null) return comp;
                    throw new InvalidOperationException($"No Pool component found on '{targetPath}'.");
                }
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            throw new InvalidOperationException("Either poolId or targetPath is required.");
        }

        #endregion
    }
}
