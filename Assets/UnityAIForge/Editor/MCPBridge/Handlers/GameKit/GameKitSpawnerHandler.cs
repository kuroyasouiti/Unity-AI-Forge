using System;
using System.Collections.Generic;
using MCP.Editor.Base;
using UnityAIForge.GameKit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MCP.Editor.Handlers.GameKit
{
    /// <summary>
    /// GameKit Spawner handler: create and manage spawning systems.
    /// Supports interval spawning, wave-based spawning, and object pooling.
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

        protected override bool RequiresCompilationWait(string operation) => false;

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

            if (string.IsNullOrEmpty(targetPath))
            {
                // Create new GameObject for spawner
                var spawnerId = GetString(payload, "spawnerId") ?? $"Spawner_{Guid.NewGuid().ToString().Substring(0, 8)}";
                targetGo = new GameObject(spawnerId);
                Undo.RegisterCreatedObjectUndo(targetGo, "Create GameKit Spawner");

                // Set position if provided
                if (payload.TryGetValue("position", out var posObj) && posObj is Dictionary<string, object> posDict)
                {
                    targetGo.transform.position = GetVector3FromDict(posDict, Vector3.zero);
                }
            }
            else
            {
                targetGo = ResolveGameObject(targetPath);
                if (targetGo == null)
                {
                    throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
                }
            }

            // Check if already has spawner component
            var existingSpawner = targetGo.GetComponent<GameKitSpawner>();
            if (existingSpawner != null)
            {
                throw new InvalidOperationException($"GameObject already has a GameKitSpawner component.");
            }

            var spawnMode = ParseSpawnMode(GetString(payload, "spawnMode") ?? "interval");

            // Load prefab
            GameObject prefab = null;
            var prefabPath = GetString(payload, "prefabPath");
            if (!string.IsNullOrEmpty(prefabPath))
            {
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                {
                    Debug.LogWarning($"[GameKitSpawnerHandler] Prefab not found at: {prefabPath}");
                }
            }

            // Add component
            var spawner = Undo.AddComponent<GameKitSpawner>(targetGo);
            var spawnerIdValue = GetString(payload, "spawnerId") ?? $"Spawner_{Guid.NewGuid().ToString().Substring(0, 8)}";
            spawner.Initialize(spawnerIdValue, prefab, spawnMode);

            // Apply settings
            ApplySpawnerSettings(spawner, payload);

            EditorSceneManager.MarkSceneDirty(targetGo.scene);

            return CreateSuccessResponse(
                ("spawnerId", spawnerIdValue),
                ("path", BuildGameObjectPath(targetGo)),
                ("spawnMode", spawnMode.ToString()),
                ("prefabPath", prefabPath ?? "")
            );
        }

        #endregion

        #region Update

        private object UpdateSpawner(Dictionary<string, object> payload)
        {
            var spawner = ResolveSpawnerComponent(payload);

            Undo.RecordObject(spawner, "Update GameKit Spawner");
            ApplySpawnerSettings(spawner, payload);
            EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);

            return CreateSuccessResponse(
                ("spawnerId", spawner.SpawnerId),
                ("path", BuildGameObjectPath(spawner.gameObject)),
                ("updated", true)
            );
        }

        private void ApplySpawnerSettings(GameKitSpawner spawner, Dictionary<string, object> payload)
        {
            var serialized = new SerializedObject(spawner);

            if (payload.TryGetValue("prefabPath", out var prefabObj))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabObj.ToString());
                if (prefab != null)
                {
                    serialized.FindProperty("prefab").objectReferenceValue = prefab;
                }
            }

            if (payload.TryGetValue("spawnMode", out var modeObj))
            {
                var mode = ParseSpawnMode(modeObj.ToString());
                serialized.FindProperty("spawnMode").enumValueIndex = (int)mode;
            }

            if (payload.TryGetValue("autoStart", out var autoObj))
            {
                serialized.FindProperty("autoStart").boolValue = Convert.ToBoolean(autoObj);
            }

            if (payload.TryGetValue("spawnInterval", out var intervalObj))
            {
                serialized.FindProperty("spawnInterval").floatValue = Convert.ToSingle(intervalObj);
            }

            if (payload.TryGetValue("initialDelay", out var delayObj))
            {
                serialized.FindProperty("initialDelay").floatValue = Convert.ToSingle(delayObj);
            }

            if (payload.TryGetValue("maxActive", out var maxActiveObj))
            {
                serialized.FindProperty("maxActive").intValue = Convert.ToInt32(maxActiveObj);
            }

            if (payload.TryGetValue("maxTotal", out var maxTotalObj))
            {
                serialized.FindProperty("maxTotal").intValue = Convert.ToInt32(maxTotalObj);
            }

            if (payload.TryGetValue("spawnPointMode", out var pointModeObj))
            {
                var pointMode = ParseSpawnPointMode(pointModeObj.ToString());
                serialized.FindProperty("spawnPointMode").enumValueIndex = (int)pointMode;
            }

            if (payload.TryGetValue("usePool", out var poolObj))
            {
                serialized.FindProperty("usePool").boolValue = Convert.ToBoolean(poolObj);
            }

            if (payload.TryGetValue("poolInitialSize", out var poolSizeObj))
            {
                serialized.FindProperty("poolInitialSize").intValue = Convert.ToInt32(poolSizeObj);
            }

            if (payload.TryGetValue("loopWaves", out var loopObj))
            {
                serialized.FindProperty("loopWaves").boolValue = Convert.ToBoolean(loopObj);
            }

            if (payload.TryGetValue("delayBetweenWaves", out var waveDelayObj))
            {
                serialized.FindProperty("delayBetweenWaves").floatValue = Convert.ToSingle(waveDelayObj);
            }

            if (payload.TryGetValue("positionRandomness", out var posRandObj) && posRandObj is Dictionary<string, object> posRandDict)
            {
                serialized.FindProperty("positionRandomness").vector3Value = GetVector3FromDict(posRandDict, Vector3.zero);
            }

            if (payload.TryGetValue("rotationRandomness", out var rotRandObj) && rotRandObj is Dictionary<string, object> rotRandDict)
            {
                serialized.FindProperty("rotationRandomness").vector3Value = GetVector3FromDict(rotRandDict, Vector3.zero);
            }

            // Handle waves array
            if (payload.TryGetValue("waves", out var wavesObj) && wavesObj is List<object> wavesList)
            {
                var wavesProp = serialized.FindProperty("waves");
                wavesProp.ClearArray();

                foreach (var waveObj in wavesList)
                {
                    if (waveObj is Dictionary<string, object> waveDict)
                    {
                        wavesProp.InsertArrayElementAtIndex(wavesProp.arraySize);
                        var waveProp = wavesProp.GetArrayElementAtIndex(wavesProp.arraySize - 1);

                        if (waveDict.TryGetValue("count", out var countObj))
                        {
                            waveProp.FindPropertyRelative("count").intValue = Convert.ToInt32(countObj);
                        }
                        if (waveDict.TryGetValue("delay", out var delayObjW))
                        {
                            waveProp.FindPropertyRelative("delay").floatValue = Convert.ToSingle(delayObjW);
                        }
                        if (waveDict.TryGetValue("spawnInterval", out var spawnIntObj))
                        {
                            waveProp.FindPropertyRelative("spawnInterval").floatValue = Convert.ToSingle(spawnIntObj);
                        }
                    }
                }
            }

            serialized.ApplyModifiedProperties();
        }

        #endregion

        #region Inspect

        private object InspectSpawner(Dictionary<string, object> payload)
        {
            var spawner = ResolveSpawnerComponent(payload);

            var serialized = new SerializedObject(spawner);
            var wavesProp = serialized.FindProperty("waves");
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

            var spawnPointsProp = serialized.FindProperty("spawnPoints");
            var spawnPointsInfo = new List<Dictionary<string, object>>();

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

            var prefabRef = serialized.FindProperty("prefab").objectReferenceValue as GameObject;
            var prefabPath = prefabRef != null ? AssetDatabase.GetAssetPath(prefabRef) : "";

            var info = new Dictionary<string, object>
            {
                { "spawnerId", spawner.SpawnerId },
                { "path", BuildGameObjectPath(spawner.gameObject) },
                { "prefabPath", prefabPath },
                { "spawnMode", spawner.Mode.ToString() },
                { "isSpawning", spawner.IsSpawning },
                { "activeCount", spawner.ActiveCount },
                { "totalSpawned", spawner.TotalSpawned },
                { "currentWaveIndex", spawner.CurrentWaveIndex },
                { "waveCount", spawner.WaveCount },
                { "waves", wavesInfo },
                { "spawnPoints", spawnPointsInfo },
                { "spawnInterval", serialized.FindProperty("spawnInterval").floatValue },
                { "maxActive", serialized.FindProperty("maxActive").intValue },
                { "usePool", serialized.FindProperty("usePool").boolValue }
            };

            return CreateSuccessResponse(("spawner", info));
        }

        #endregion

        #region Delete

        private object DeleteSpawner(Dictionary<string, object> payload)
        {
            var spawner = ResolveSpawnerComponent(payload);
            var path = BuildGameObjectPath(spawner.gameObject);
            var spawnerId = spawner.SpawnerId;
            var scene = spawner.gameObject.scene;

            Undo.DestroyObjectImmediate(spawner);
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
            var spawner = ResolveSpawnerComponent(payload);

            return CreateSuccessResponse(
                ("spawnerId", spawner.SpawnerId),
                ("note", "Start command registered. Spawning will begin in Play mode.")
            );
        }

        private object StopSpawner(Dictionary<string, object> payload)
        {
            var spawner = ResolveSpawnerComponent(payload);

            return CreateSuccessResponse(
                ("spawnerId", spawner.SpawnerId),
                ("note", "Stop command registered. Spawning will stop in Play mode.")
            );
        }

        private object ResetSpawner(Dictionary<string, object> payload)
        {
            var spawner = ResolveSpawnerComponent(payload);

            return CreateSuccessResponse(
                ("spawnerId", spawner.SpawnerId),
                ("note", "Reset command registered. Spawner will reset in Play mode.")
            );
        }

        private object SpawnOne(Dictionary<string, object> payload)
        {
            var spawner = ResolveSpawnerComponent(payload);

            return CreateSuccessResponse(
                ("spawnerId", spawner.SpawnerId),
                ("note", "SpawnOne requires Play mode. Configure spawner and test in Play mode.")
            );
        }

        private object SpawnBurst(Dictionary<string, object> payload)
        {
            var spawner = ResolveSpawnerComponent(payload);
            var count = GetInt(payload, "count", 5);

            return CreateSuccessResponse(
                ("spawnerId", spawner.SpawnerId),
                ("count", count),
                ("note", "SpawnBurst requires Play mode. Configure spawner and test in Play mode.")
            );
        }

        private object DespawnAll(Dictionary<string, object> payload)
        {
            var spawner = ResolveSpawnerComponent(payload);

            return CreateSuccessResponse(
                ("spawnerId", spawner.SpawnerId),
                ("note", "DespawnAll requires Play mode.")
            );
        }

        #endregion

        #region Configuration Operations

        private object AddSpawnPoint(Dictionary<string, object> payload)
        {
            var spawner = ResolveSpawnerComponent(payload);
            var pointPath = GetString(payload, "pointPath");

            if (string.IsNullOrEmpty(pointPath))
            {
                // Create new spawn point
                var pointGo = new GameObject($"SpawnPoint_{spawner.SpawnerId}_{DateTime.Now.Ticks}");
                Undo.RegisterCreatedObjectUndo(pointGo, "Create Spawn Point");

                if (payload.TryGetValue("position", out var posObj) && posObj is Dictionary<string, object> posDict)
                {
                    pointGo.transform.position = GetVector3FromDict(posDict, spawner.transform.position + Vector3.right * 2);
                }
                else
                {
                    pointGo.transform.position = spawner.transform.position + Vector3.right * 2;
                }

                pointGo.transform.SetParent(spawner.transform);

                var serialized = new SerializedObject(spawner);
                var spawnPointsProp = serialized.FindProperty("spawnPoints");
                spawnPointsProp.InsertArrayElementAtIndex(spawnPointsProp.arraySize);
                spawnPointsProp.GetArrayElementAtIndex(spawnPointsProp.arraySize - 1).objectReferenceValue = pointGo.transform;
                serialized.ApplyModifiedProperties();

                EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);

                return CreateSuccessResponse(
                    ("spawnerId", spawner.SpawnerId),
                    ("pointPath", BuildGameObjectPath(pointGo)),
                    ("pointIndex", spawnPointsProp.arraySize - 1)
                );
            }
            else
            {
                var pointGo = ResolveGameObject(pointPath);
                if (pointGo == null)
                {
                    throw new InvalidOperationException($"GameObject not found at path: {pointPath}");
                }

                var serialized = new SerializedObject(spawner);
                var spawnPointsProp = serialized.FindProperty("spawnPoints");
                spawnPointsProp.InsertArrayElementAtIndex(spawnPointsProp.arraySize);
                spawnPointsProp.GetArrayElementAtIndex(spawnPointsProp.arraySize - 1).objectReferenceValue = pointGo.transform;
                serialized.ApplyModifiedProperties();

                EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);

                return CreateSuccessResponse(
                    ("spawnerId", spawner.SpawnerId),
                    ("pointPath", pointPath),
                    ("pointIndex", spawnPointsProp.arraySize - 1)
                );
            }
        }

        private object AddWave(Dictionary<string, object> payload)
        {
            var spawner = ResolveSpawnerComponent(payload);
            var count = GetInt(payload, "count", 5);
            var delay = GetFloat(payload, "delay", 0f);
            var spawnInterval = GetFloat(payload, "spawnInterval", 0.5f);

            var serialized = new SerializedObject(spawner);
            var wavesProp = serialized.FindProperty("waves");

            wavesProp.InsertArrayElementAtIndex(wavesProp.arraySize);
            var waveProp = wavesProp.GetArrayElementAtIndex(wavesProp.arraySize - 1);
            waveProp.FindPropertyRelative("count").intValue = count;
            waveProp.FindPropertyRelative("delay").floatValue = delay;
            waveProp.FindPropertyRelative("spawnInterval").floatValue = spawnInterval;

            serialized.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);

            return CreateSuccessResponse(
                ("spawnerId", spawner.SpawnerId),
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
            {
                throw new InvalidOperationException("spawnerId is required.");
            }

            var spawner = FindSpawnerById(spawnerId);
            if (spawner == null)
            {
                return CreateSuccessResponse(("found", false), ("spawnerId", spawnerId));
            }

            return CreateSuccessResponse(
                ("found", true),
                ("spawnerId", spawner.SpawnerId),
                ("path", BuildGameObjectPath(spawner.gameObject)),
                ("spawnMode", spawner.Mode.ToString())
            );
        }

        #endregion

        #region Helpers

        private GameKitSpawner ResolveSpawnerComponent(Dictionary<string, object> payload)
        {
            var spawnerId = GetString(payload, "spawnerId");
            if (!string.IsNullOrEmpty(spawnerId))
            {
                var spawnerById = FindSpawnerById(spawnerId);
                if (spawnerById != null)
                {
                    return spawnerById;
                }
            }

            var targetPath = GetString(payload, "targetPath");
            if (!string.IsNullOrEmpty(targetPath))
            {
                var targetGo = ResolveGameObject(targetPath);
                if (targetGo != null)
                {
                    var spawnerByPath = targetGo.GetComponent<GameKitSpawner>();
                    if (spawnerByPath != null)
                    {
                        return spawnerByPath;
                    }
                    throw new InvalidOperationException($"No GameKitSpawner component found on '{targetPath}'.");
                }
                throw new InvalidOperationException($"GameObject not found at path: {targetPath}");
            }

            throw new InvalidOperationException("Either spawnerId or targetPath is required.");
        }

        private GameKitSpawner FindSpawnerById(string spawnerId)
        {
            var spawners = UnityEngine.Object.FindObjectsByType<GameKitSpawner>(FindObjectsSortMode.None);
            foreach (var spawner in spawners)
            {
                if (spawner.SpawnerId == spawnerId)
                {
                    return spawner;
                }
            }
            return null;
        }

        private GameKitSpawner.SpawnMode ParseSpawnMode(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "interval" => GameKitSpawner.SpawnMode.Interval,
                "wave" => GameKitSpawner.SpawnMode.Wave,
                "burst" => GameKitSpawner.SpawnMode.Burst,
                "manual" => GameKitSpawner.SpawnMode.Manual,
                _ => GameKitSpawner.SpawnMode.Interval
            };
        }

        private GameKitSpawner.SpawnPointMode ParseSpawnPointMode(string str)
        {
            return str.ToLowerInvariant() switch
            {
                "sequential" => GameKitSpawner.SpawnPointMode.Sequential,
                "random" => GameKitSpawner.SpawnPointMode.Random,
                "randomnorepeat" => GameKitSpawner.SpawnPointMode.RandomNoRepeat,
                _ => GameKitSpawner.SpawnPointMode.Sequential
            };
        }

        #endregion
    }
}
