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
    /// GameKit Spawner handler: create and manage spawning systems.
    /// Uses code generation to produce standalone Spawner scripts with zero package dependency.
    /// </summary>
    public class GameKitSpawnerHandler : BaseCommandHandler
    {
        private static readonly string[] Operations =
        {
            "create", "update", "inspect", "delete",
            "start", "stop", "reset", "spawnOne", "spawnBurst",
            "despawnAll", "addSpawnPoint", "addWave", "findBySpawnerId"
        };

        public override string Category => "gamekitSpawner";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) =>
            operation == "create";

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "create" => CreateSpawner(payload),
                "update" => UpdateSpawner(payload),
                "inspect" => InspectSpawner(payload),
                "delete" => DeleteSpawner(payload),
                "start" => StartSpawner(payload),
                "stop" => StopSpawner(payload),
                "reset" => ResetSpawner(payload),
                "spawnOne" => SpawnOne(payload),
                "spawnBurst" => SpawnBurst(payload),
                "despawnAll" => DespawnAll(payload),
                "addSpawnPoint" => AddSpawnPoint(payload),
                "addWave" => AddWave(payload),
                "findBySpawnerId" => FindBySpawnerId(payload),
                _ => throw new InvalidOperationException($"Unsupported GameKit Spawner operation: {operation}")
            };
        }

        #region Create

        private object CreateSpawner(Dictionary<string, object> payload)
        {
            var targetPath = GetString(payload, "targetPath");
            GameObject targetGo;

            var spawnerId = GetString(payload, "spawnerId") ?? $"Spawner_{Guid.NewGuid().ToString().Substring(0, 8)}";

            if (string.IsNullOrEmpty(targetPath))
            {
                targetGo = new GameObject(spawnerId);
                Undo.RegisterCreatedObjectUndo(targetGo, "Create GameKit Spawner");

                if (payload.TryGetValue("position", out var posObj) && posObj is Dictionary<string, object> posDict)
                    targetGo.transform.position = GetVector3FromDict(posDict, Vector3.zero);
            }
            else
            {
                targetGo = ResolveGameObject(targetPath);
                if (targetGo == null)
                    throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            // Check if already has spawner component
            var existingSpawner = CodeGenHelper.FindComponentByField(targetGo, "spawnerId", null);
            if (existingSpawner != null)
                throw new InvalidOperationException($"GameObject already has a Spawner component.");

            var spawnMode = ParseSpawnMode(GetString(payload, "spawnMode") ?? "interval");
            var prefabPath = GetString(payload, "prefabPath") ?? "";
            var spawnInterval = GetFloat(payload, "spawnInterval", 3f);
            var initialDelay = GetFloat(payload, "initialDelay", 0f);
            var maxActive = GetInt(payload, "maxActive", 10);
            var maxTotal = GetInt(payload, "maxTotal", 0);
            var autoStart = GetBool(payload, "autoStart", false);
            var usePool = GetBool(payload, "usePool", false);
            var poolInitialSize = GetInt(payload, "poolInitialSize", 5);

            var className = GetString(payload, "className")
                ?? ScriptGenerator.ToPascalCase(spawnerId, "Spawner");

            var variables = new Dictionary<string, object>
            {
                { "SPAWNER_ID", spawnerId },
                { "SPAWN_MODE", spawnMode },
                { "PREFAB_PATH", prefabPath },
                { "SPAWN_INTERVAL", spawnInterval },
                { "INITIAL_DELAY", initialDelay },
                { "MAX_ACTIVE", maxActive },
                { "MAX_TOTAL", maxTotal },
                { "AUTO_START", autoStart.ToString().ToLowerInvariant() },
                { "USE_POOL", usePool.ToString().ToLowerInvariant() },
                { "POOL_INITIAL_SIZE", poolInitialSize }
            };

            var outputDir = GetString(payload, "outputPath");

            var result = CodeGenHelper.GenerateAndAttach(
                targetGo, "Spawner", spawnerId, className, variables, outputDir);

            if (result.TryGetValue("success", out var success) && !(bool)success)
            {
                throw new InvalidOperationException(result.TryGetValue("error", out var err)
                    ? err.ToString()
                    : "Failed to generate Spawner script.");
            }

            // Set prefab reference if component was added and prefab exists
            if (result.TryGetValue("componentAdded", out var added) && (bool)added && !string.IsNullOrEmpty(prefabPath))
            {
                var component = CodeGenHelper.FindComponentByField(targetGo, "spawnerId", spawnerId);
                if (component != null)
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    if (prefab != null)
                    {
                        var so = new SerializedObject(component);
                        so.FindProperty("prefab").objectReferenceValue = prefab;
                        so.ApplyModifiedProperties();
                    }
                }
            }

            EditorSceneManager.MarkSceneDirty(targetGo.scene);

            result["spawnerId"] = spawnerId;
            result["path"] = BuildGameObjectPath(targetGo);
            result["spawnMode"] = spawnMode;
            result["prefabPath"] = prefabPath;

            return result;
        }

        #endregion

        #region Update

        private object UpdateSpawner(Dictionary<string, object> payload)
        {
            var component = ResolveSpawnerComponent(payload);

            Undo.RecordObject(component, "Update Spawner");
            ApplySpawnerSettings(component, payload);
            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            var so = new SerializedObject(component);
            return CreateSuccessResponse(
                ("spawnerId", so.FindProperty("spawnerId").stringValue),
                ("path", BuildGameObjectPath(component.gameObject)),
                ("updated", true)
            );
        }

        private void ApplySpawnerSettings(Component component, Dictionary<string, object> payload)
        {
            var so = new SerializedObject(component);

            if (payload.TryGetValue("prefabPath", out var prefabObj))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabObj.ToString());
                if (prefab != null)
                    so.FindProperty("prefab").objectReferenceValue = prefab;
            }

            if (payload.TryGetValue("spawnMode", out var modeObj))
            {
                var modeName = ParseSpawnMode(modeObj.ToString());
                var prop = so.FindProperty("spawnMode");
                var names = prop.enumDisplayNames;
                for (int i = 0; i < names.Length; i++)
                {
                    if (string.Equals(names[i], modeName, StringComparison.OrdinalIgnoreCase))
                    {
                        prop.enumValueIndex = i;
                        break;
                    }
                }
            }

            if (payload.TryGetValue("autoStart", out var autoObj))
                so.FindProperty("autoStart").boolValue = Convert.ToBoolean(autoObj);
            if (payload.TryGetValue("spawnInterval", out var intervalObj))
                so.FindProperty("spawnInterval").floatValue = Convert.ToSingle(intervalObj);
            if (payload.TryGetValue("initialDelay", out var delayObj))
                so.FindProperty("initialDelay").floatValue = Convert.ToSingle(delayObj);
            if (payload.TryGetValue("maxActive", out var maxActiveObj))
                so.FindProperty("maxActive").intValue = Convert.ToInt32(maxActiveObj);
            if (payload.TryGetValue("maxTotal", out var maxTotalObj))
                so.FindProperty("maxTotal").intValue = Convert.ToInt32(maxTotalObj);
            if (payload.TryGetValue("usePool", out var poolObj))
                so.FindProperty("usePool").boolValue = Convert.ToBoolean(poolObj);
            if (payload.TryGetValue("poolInitialSize", out var poolSizeObj))
                so.FindProperty("poolInitialSize").intValue = Convert.ToInt32(poolSizeObj);
            if (payload.TryGetValue("loopWaves", out var loopObj))
                so.FindProperty("loopWaves").boolValue = Convert.ToBoolean(loopObj);
            if (payload.TryGetValue("delayBetweenWaves", out var waveDelayObj))
                so.FindProperty("delayBetweenWaves").floatValue = Convert.ToSingle(waveDelayObj);

            if (payload.TryGetValue("positionRandomness", out var posRandObj) && posRandObj is Dictionary<string, object> posRandDict)
                so.FindProperty("positionRandomness").vector3Value = GetVector3FromDict(posRandDict, Vector3.zero);
            if (payload.TryGetValue("rotationRandomness", out var rotRandObj) && rotRandObj is Dictionary<string, object> rotRandDict)
                so.FindProperty("rotationRandomness").vector3Value = GetVector3FromDict(rotRandDict, Vector3.zero);

            // Handle waves array
            if (payload.TryGetValue("waves", out var wavesObj) && wavesObj is List<object> wavesList)
            {
                var wavesProp = so.FindProperty("waves");
                wavesProp.ClearArray();
                foreach (var waveObj in wavesList)
                {
                    if (waveObj is Dictionary<string, object> waveDict)
                    {
                        wavesProp.InsertArrayElementAtIndex(wavesProp.arraySize);
                        var waveProp = wavesProp.GetArrayElementAtIndex(wavesProp.arraySize - 1);
                        if (waveDict.TryGetValue("count", out var countObj))
                            waveProp.FindPropertyRelative("count").intValue = Convert.ToInt32(countObj);
                        if (waveDict.TryGetValue("delay", out var delayObjW))
                            waveProp.FindPropertyRelative("delay").floatValue = Convert.ToSingle(delayObjW);
                        if (waveDict.TryGetValue("spawnInterval", out var spawnIntObj))
                            waveProp.FindPropertyRelative("spawnInterval").floatValue = Convert.ToSingle(spawnIntObj);
                    }
                }
            }

            so.ApplyModifiedProperties();
        }

        #endregion

        #region Inspect

        private object InspectSpawner(Dictionary<string, object> payload)
        {
            var component = ResolveSpawnerComponent(payload);
            var so = new SerializedObject(component);

            var wavesProp = so.FindProperty("waves");
            var wavesInfo = new List<Dictionary<string, object>>();
            for (int i = 0; i < wavesProp.arraySize; i++)
            {
                var waveProp = wavesProp.GetArrayElementAtIndex(i);
                wavesInfo.Add(new Dictionary<string, object>
                {
                    { "count", waveProp.FindPropertyRelative("count").intValue },
                    { "delay", waveProp.FindPropertyRelative("delay").floatValue },
                    { "spawnInterval", waveProp.FindPropertyRelative("spawnInterval").floatValue }
                });
            }

            var spawnPointsProp = so.FindProperty("spawnPoints");
            var spawnPointsInfo = new List<Dictionary<string, object>>();
            if (spawnPointsProp != null)
            {
                for (int i = 0; i < spawnPointsProp.arraySize; i++)
                {
                    var pointRef = spawnPointsProp.GetArrayElementAtIndex(i).objectReferenceValue as Transform;
                    if (pointRef != null)
                    {
                        spawnPointsInfo.Add(new Dictionary<string, object>
                        {
                            { "path", BuildGameObjectPath(pointRef.gameObject) },
                            { "position", new Dictionary<string, object>
                                {
                                    { "x", pointRef.position.x },
                                    { "y", pointRef.position.y },
                                    { "z", pointRef.position.z }
                                }
                            }
                        });
                    }
                }
            }

            var prefabRef = so.FindProperty("prefab").objectReferenceValue as GameObject;
            var prefabPath = prefabRef != null ? AssetDatabase.GetAssetPath(prefabRef) : "";

            var spawnModeProp = so.FindProperty("spawnMode");

            var info = new Dictionary<string, object>
            {
                { "spawnerId", so.FindProperty("spawnerId").stringValue },
                { "path", BuildGameObjectPath(component.gameObject) },
                { "prefabPath", prefabPath },
                { "spawnMode", spawnModeProp.enumValueIndex < spawnModeProp.enumDisplayNames.Length
                    ? spawnModeProp.enumDisplayNames[spawnModeProp.enumValueIndex] : "Interval" },
                { "waves", wavesInfo },
                { "spawnPoints", spawnPointsInfo },
                { "spawnInterval", so.FindProperty("spawnInterval").floatValue },
                { "maxActive", so.FindProperty("maxActive").intValue },
                { "usePool", so.FindProperty("usePool").boolValue }
            };

            return CreateSuccessResponse(("spawner", info));
        }

        #endregion

        #region Delete

        private object DeleteSpawner(Dictionary<string, object> payload)
        {
            var component = ResolveSpawnerComponent(payload);
            var path = BuildGameObjectPath(component.gameObject);
            var spawnerId = new SerializedObject(component).FindProperty("spawnerId").stringValue;
            var scene = component.gameObject.scene;

            Undo.DestroyObjectImmediate(component);
            ScriptGenerator.Delete(spawnerId);
            EditorSceneManager.MarkSceneDirty(scene);

            return CreateSuccessResponse(
                ("spawnerId", spawnerId),
                ("path", path),
                ("deleted", true)
            );
        }

        #endregion

        #region Control Operations

        private object StartSpawner(Dictionary<string, object> payload)
        {
            var component = ResolveSpawnerComponent(payload);
            var so = new SerializedObject(component);
            return CreateSuccessResponse(
                ("spawnerId", so.FindProperty("spawnerId").stringValue),
                ("note", "Start command registered. Spawning will begin in Play mode.")
            );
        }

        private object StopSpawner(Dictionary<string, object> payload)
        {
            var component = ResolveSpawnerComponent(payload);
            var so = new SerializedObject(component);
            return CreateSuccessResponse(
                ("spawnerId", so.FindProperty("spawnerId").stringValue),
                ("note", "Stop command registered. Spawning will stop in Play mode.")
            );
        }

        private object ResetSpawner(Dictionary<string, object> payload)
        {
            var component = ResolveSpawnerComponent(payload);
            var so = new SerializedObject(component);
            return CreateSuccessResponse(
                ("spawnerId", so.FindProperty("spawnerId").stringValue),
                ("note", "Reset command registered. Spawner will reset in Play mode.")
            );
        }

        private object SpawnOne(Dictionary<string, object> payload)
        {
            var component = ResolveSpawnerComponent(payload);
            var so = new SerializedObject(component);
            return CreateSuccessResponse(
                ("spawnerId", so.FindProperty("spawnerId").stringValue),
                ("note", "SpawnOne requires Play mode. Configure spawner and test in Play mode.")
            );
        }

        private object SpawnBurst(Dictionary<string, object> payload)
        {
            var component = ResolveSpawnerComponent(payload);
            var count = GetInt(payload, "count", 5);
            var so = new SerializedObject(component);
            return CreateSuccessResponse(
                ("spawnerId", so.FindProperty("spawnerId").stringValue),
                ("count", count),
                ("note", "SpawnBurst requires Play mode. Configure spawner and test in Play mode.")
            );
        }

        private object DespawnAll(Dictionary<string, object> payload)
        {
            var component = ResolveSpawnerComponent(payload);
            var so = new SerializedObject(component);
            return CreateSuccessResponse(
                ("spawnerId", so.FindProperty("spawnerId").stringValue),
                ("note", "DespawnAll requires Play mode.")
            );
        }

        #endregion

        #region Configuration Operations

        private object AddSpawnPoint(Dictionary<string, object> payload)
        {
            var component = ResolveSpawnerComponent(payload);
            var pointPath = GetString(payload, "pointPath");
            var so = new SerializedObject(component);
            var spawnPointsProp = so.FindProperty("spawnPoints");
            var spawnerId = so.FindProperty("spawnerId").stringValue;

            if (string.IsNullOrEmpty(pointPath))
            {
                var pointGo = new GameObject($"SpawnPoint_{spawnerId}_{DateTime.Now.Ticks}");
                Undo.RegisterCreatedObjectUndo(pointGo, "Create Spawn Point");

                if (payload.TryGetValue("position", out var posObj) && posObj is Dictionary<string, object> posDict)
                    pointGo.transform.position = GetVector3FromDict(posDict, component.transform.position + Vector3.right * 2);
                else
                    pointGo.transform.position = component.transform.position + Vector3.right * 2;

                pointGo.transform.SetParent(component.transform);

                spawnPointsProp.InsertArrayElementAtIndex(spawnPointsProp.arraySize);
                spawnPointsProp.GetArrayElementAtIndex(spawnPointsProp.arraySize - 1).objectReferenceValue = pointGo.transform;
                so.ApplyModifiedProperties();

                EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

                return CreateSuccessResponse(
                    ("spawnerId", spawnerId),
                    ("pointPath", BuildGameObjectPath(pointGo)),
                    ("pointIndex", spawnPointsProp.arraySize - 1)
                );
            }
            else
            {
                var pointGo = ResolveGameObject(pointPath);
                if (pointGo == null)
                    throw new InvalidOperationException($"GameObject not found at path: {pointPath}");

                spawnPointsProp.InsertArrayElementAtIndex(spawnPointsProp.arraySize);
                spawnPointsProp.GetArrayElementAtIndex(spawnPointsProp.arraySize - 1).objectReferenceValue = pointGo.transform;
                so.ApplyModifiedProperties();

                EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

                return CreateSuccessResponse(
                    ("spawnerId", spawnerId),
                    ("pointPath", pointPath),
                    ("pointIndex", spawnPointsProp.arraySize - 1)
                );
            }
        }

        private object AddWave(Dictionary<string, object> payload)
        {
            var component = ResolveSpawnerComponent(payload);
            var count = GetInt(payload, "count", 5);
            var delay = GetFloat(payload, "delay", 0f);
            var spawnInterval = GetFloat(payload, "spawnInterval", 0.5f);

            var so = new SerializedObject(component);
            var wavesProp = so.FindProperty("waves");

            wavesProp.InsertArrayElementAtIndex(wavesProp.arraySize);
            var waveProp = wavesProp.GetArrayElementAtIndex(wavesProp.arraySize - 1);
            waveProp.FindPropertyRelative("count").intValue = count;
            waveProp.FindPropertyRelative("delay").floatValue = delay;
            waveProp.FindPropertyRelative("spawnInterval").floatValue = spawnInterval;

            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

            return CreateSuccessResponse(
                ("spawnerId", so.FindProperty("spawnerId").stringValue),
                ("waveIndex", wavesProp.arraySize - 1),
                ("wave", new Dictionary<string, object>
                {
                    { "count", count },
                    { "delay", delay },
                    { "spawnInterval", spawnInterval }
                })
            );
        }

        private object FindBySpawnerId(Dictionary<string, object> payload)
        {
            var spawnerId = GetString(payload, "spawnerId");
            if (string.IsNullOrEmpty(spawnerId))
                throw new InvalidOperationException("spawnerId is required.");

            var component = CodeGenHelper.FindComponentInSceneByField("spawnerId", spawnerId);
            if (component == null)
                return CreateSuccessResponse(("found", false), ("spawnerId", spawnerId));

            var so = new SerializedObject(component);
            var spawnModeProp = so.FindProperty("spawnMode");

            return CreateSuccessResponse(
                ("found", true),
                ("spawnerId", spawnerId),
                ("path", BuildGameObjectPath(component.gameObject)),
                ("spawnMode", spawnModeProp.enumValueIndex < spawnModeProp.enumDisplayNames.Length
                    ? spawnModeProp.enumDisplayNames[spawnModeProp.enumValueIndex] : "Interval")
            );
        }

        #endregion

        #region Helpers

        private Component ResolveSpawnerComponent(Dictionary<string, object> payload)
        {
            var spawnerId = GetString(payload, "spawnerId");
            if (!string.IsNullOrEmpty(spawnerId))
            {
                var spawnerById = CodeGenHelper.FindComponentInSceneByField("spawnerId", spawnerId);
                if (spawnerById != null)
                    return spawnerById;
            }

            var targetPath = GetString(payload, "targetPath");
            if (!string.IsNullOrEmpty(targetPath))
            {
                var targetGo = ResolveGameObject(targetPath);
                if (targetGo != null)
                {
                    var spawnerByPath = CodeGenHelper.FindComponentByField(targetGo, "spawnerId", null);
                    if (spawnerByPath != null)
                        return spawnerByPath;
                    throw new InvalidOperationException($"No Spawner component found on '{targetPath}'.");
                }
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            throw new InvalidOperationException("Either spawnerId or targetPath is required.");
        }

        private string ParseSpawnMode(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "interval" => "Interval",
                "wave" => "Wave",
                "burst" => "Burst",
                "manual" => "Manual",
                _ => "Interval"
            };
        }

        #endregion
    }
}
