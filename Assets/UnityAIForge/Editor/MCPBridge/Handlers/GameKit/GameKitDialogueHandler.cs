using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MCP.Editor.Base;
using MCP.Editor.CodeGen;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MCP.Editor.Handlers.GameKit
{
    /// <summary>
    /// GameKit Dialogue handler: create and manage dialogue systems for NPCs and conversations.
    /// Uses code generation to produce standalone DialogueManager and DialogueData scripts
    /// with zero package dependency.
    /// </summary>
    public class GameKitDialogueHandler : BaseCommandHandler
    {
        private static readonly string[] Operations =
        {
            "createDialogue", "updateDialogue", "inspectDialogue", "deleteDialogue",
            "addNode", "updateNode", "removeNode",
            "addChoice", "updateChoice", "removeChoice",
            "startDialogue", "selectChoice", "advanceDialogue", "endDialogue",
            "createManager", "inspectManager", "deleteManager",
            "findByDialogueId"
        };

        public override string Category => "gamekitDialogue";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) =>
            operation == "createManager" || operation == "createDialogue";

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "createDialogue" => CreateDialogue(payload),
                "updateDialogue" => UpdateDialogue(payload),
                "inspectDialogue" => InspectDialogue(payload),
                "deleteDialogue" => DeleteDialogue(payload),
                "addNode" => AddNode(payload),
                "updateNode" => UpdateNode(payload),
                "removeNode" => RemoveNode(payload),
                "addChoice" => AddChoice(payload),
                "updateChoice" => UpdateChoice(payload),
                "removeChoice" => RemoveChoice(payload),
                "startDialogue" => StartDialogue(payload),
                "selectChoice" => SelectChoice(payload),
                "advanceDialogue" => AdvanceDialogue(payload),
                "endDialogue" => EndDialogue(payload),
                "createManager" => CreateManager(payload),
                "inspectManager" => InspectManager(payload),
                "deleteManager" => DeleteManager(payload),
                "findByDialogueId" => FindByDialogueId(payload),
                _ => throw new InvalidOperationException($"Unsupported GameKit Dialogue operation: {operation}")
            };
        }

        #region Dialogue CRUD

        private object CreateDialogue(Dictionary<string, object> payload)
        {
            var dialogueId = GetString(payload, "dialogueId") ?? $"Dialogue_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var assetPath = GetString(payload, "assetPath") ?? $"Assets/Dialogues/{dialogueId}.asset";

            // Ensure directory exists
            var directory = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Check if asset already exists at path
            var existingAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
            if (existingAsset != null)
            {
                throw new InvalidOperationException($"Dialogue asset already exists at: {assetPath}");
            }

            // Generate the DialogueData ScriptableObject script via code generation
            var className = GetString(payload, "className")
                ?? ScriptGenerator.ToPascalCase(dialogueId, "DialogueData");

            var variables = new Dictionary<string, object>
            {
                { "DIALOGUE_ID", dialogueId }
            };

            var outputDir = GetString(payload, "outputPath");

            var genResult = ScriptGenerator.Generate(null, "DialogueData", dialogueId, className, variables, outputDir);
            if (!genResult.Success)
            {
                throw new InvalidOperationException(genResult.ErrorMessage ?? "Failed to generate DialogueData script.");
            }

            // Try to resolve the generated type and create the asset immediately
            var soType = ScriptGenerator.ResolveGeneratedType(className);
            if (soType != null)
            {
                var dialogue = ScriptableObject.CreateInstance(soType);
                var serializedDialogue = new SerializedObject(dialogue);

                // Set dialogueId
                var dialogueIdProp = serializedDialogue.FindProperty("dialogueId");
                if (dialogueIdProp != null)
                    dialogueIdProp.stringValue = dialogueId;

                if (payload.TryGetValue("startNodeId", out var startNode))
                {
                    var prop = serializedDialogue.FindProperty("startNodeId");
                    if (prop != null) prop.stringValue = startNode.ToString();
                }

                if (payload.TryGetValue("autoAdvance", out var autoAdvance))
                {
                    var prop = serializedDialogue.FindProperty("autoAdvance");
                    if (prop != null) prop.boolValue = Convert.ToBoolean(autoAdvance);
                }

                if (payload.TryGetValue("autoAdvanceDelay", out var autoAdvanceDelay))
                {
                    var prop = serializedDialogue.FindProperty("autoAdvanceDelay");
                    if (prop != null) prop.floatValue = Convert.ToSingle(autoAdvanceDelay);
                }

                serializedDialogue.ApplyModifiedPropertiesWithoutUndo();

                // Add initial nodes if provided
                if (payload.TryGetValue("nodes", out var nodesObj) && nodesObj is List<object> nodesList)
                {
                    foreach (var nodeObj in nodesList)
                    {
                        if (nodeObj is Dictionary<string, object> nodeDict)
                        {
                            AddNodeToDialogueAsset(dialogue, soType, nodeDict);
                        }
                    }
                }

                // Save asset
                AssetDatabase.CreateAsset(dialogue, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                // Read back node count
                var nodesProp = new SerializedObject(dialogue).FindProperty("nodes");
                var nodeCount = nodesProp != null ? nodesProp.arraySize : 0;

                return CreateSuccessResponse(
                    ("dialogueId", dialogueId),
                    ("assetPath", assetPath),
                    ("nodeCount", nodeCount),
                    ("scriptPath", genResult.ScriptPath),
                    ("className", genResult.ClassName),
                    ("compilationRequired", false)
                );
            }

            // Type not yet compiled — script was generated but asset creation deferred
            return CreateSuccessResponse(
                ("dialogueId", dialogueId),
                ("assetPath", assetPath),
                ("scriptPath", genResult.ScriptPath),
                ("className", genResult.ClassName),
                ("compilationRequired", true),
                ("note", "DialogueData script generated. After compilation, re-run createDialogue to create the asset.")
            );
        }

        private object UpdateDialogue(Dictionary<string, object> payload)
        {
            var dialogue = ResolveDialogueAsset(payload);
            var serializedDialogue = new SerializedObject(dialogue);

            if (payload.TryGetValue("startNodeId", out var startNode))
            {
                var prop = serializedDialogue.FindProperty("startNodeId");
                if (prop != null) prop.stringValue = startNode.ToString();
            }

            if (payload.TryGetValue("autoAdvance", out var autoAdvance))
            {
                var prop = serializedDialogue.FindProperty("autoAdvance");
                if (prop != null) prop.boolValue = Convert.ToBoolean(autoAdvance);
            }

            if (payload.TryGetValue("autoAdvanceDelay", out var autoAdvanceDelay))
            {
                var prop = serializedDialogue.FindProperty("autoAdvanceDelay");
                if (prop != null) prop.floatValue = Convert.ToSingle(autoAdvanceDelay);
            }

            serializedDialogue.ApplyModifiedProperties();
            EditorUtility.SetDirty(dialogue);
            AssetDatabase.SaveAssets();

            var dialogueIdProp = serializedDialogue.FindProperty("dialogueId");
            var dialogueIdVal = dialogueIdProp != null ? dialogueIdProp.stringValue : "";

            return CreateSuccessResponse(
                ("dialogueId", dialogueIdVal),
                ("updated", true)
            );
        }

        private object InspectDialogue(Dictionary<string, object> payload)
        {
            var dialogue = ResolveDialogueAsset(payload);
            var so = new SerializedObject(dialogue);

            var dialogueId = so.FindProperty("dialogueId")?.stringValue ?? "";
            var startNodeId = so.FindProperty("startNodeId")?.stringValue ?? "";
            var autoAdvance = so.FindProperty("autoAdvance")?.boolValue ?? false;
            var autoAdvanceDelay = so.FindProperty("autoAdvanceDelay")?.floatValue ?? 2f;

            var nodesProp = so.FindProperty("nodes");
            var nodeCount = nodesProp != null ? nodesProp.arraySize : 0;

            var nodesInfo = new List<Dictionary<string, object>>();
            if (nodesProp != null)
            {
                for (int i = 0; i < nodesProp.arraySize; i++)
                {
                    nodesInfo.Add(SerializeDialogueNodeFromProperty(nodesProp.GetArrayElementAtIndex(i)));
                }
            }

            return CreateSuccessResponse(
                ("dialogueId", dialogueId),
                ("assetPath", AssetDatabase.GetAssetPath(dialogue)),
                ("startNodeId", startNodeId),
                ("autoAdvance", autoAdvance),
                ("autoAdvanceDelay", autoAdvanceDelay),
                ("nodeCount", nodeCount),
                ("nodes", nodesInfo)
            );
        }

        private object DeleteDialogue(Dictionary<string, object> payload)
        {
            var dialogue = ResolveDialogueAsset(payload);
            var assetPath = AssetDatabase.GetAssetPath(dialogue);
            var so = new SerializedObject(dialogue);
            var dialogueId = so.FindProperty("dialogueId")?.stringValue ?? "";

            AssetDatabase.DeleteAsset(assetPath);
            AssetDatabase.Refresh();

            // Clean up generated script from tracker
            if (!string.IsNullOrEmpty(dialogueId))
            {
                ScriptGenerator.Delete(dialogueId);
            }

            return CreateSuccessResponse(
                ("dialogueId", dialogueId),
                ("assetPath", assetPath),
                ("deleted", true)
            );
        }

        #endregion

        #region Node Operations

        private object AddNode(Dictionary<string, object> payload)
        {
            var dialogue = ResolveDialogueAsset(payload);

            if (!payload.TryGetValue("node", out var nodeObj) || nodeObj is not Dictionary<string, object> nodeDict)
            {
                throw new InvalidOperationException("node data is required for addNode operation.");
            }

            AddNodeToDialogueAsset(dialogue, dialogue.GetType(), nodeDict);

            EditorUtility.SetDirty(dialogue);
            AssetDatabase.SaveAssets();

            var so = new SerializedObject(dialogue);
            var dialogueId = so.FindProperty("dialogueId")?.stringValue ?? "";
            var nodesProp = so.FindProperty("nodes");
            var nodeCount = nodesProp != null ? nodesProp.arraySize : 0;

            // Get the nodeId from the dict
            var nodeId = nodeDict.TryGetValue("nodeId", out var id)
                ? id?.ToString()
                : "";

            return CreateSuccessResponse(
                ("dialogueId", dialogueId),
                ("nodeId", nodeId),
                ("nodeCount", nodeCount)
            );
        }

        private object UpdateNode(Dictionary<string, object> payload)
        {
            var dialogue = ResolveDialogueAsset(payload);
            var nodeId = GetString(payload, "nodeId");

            if (string.IsNullOrEmpty(nodeId))
            {
                throw new InvalidOperationException("nodeId is required for updateNode operation.");
            }

            var so = new SerializedObject(dialogue);
            var nodesProp = so.FindProperty("nodes");
            if (nodesProp == null)
            {
                throw new InvalidOperationException("No nodes property found on dialogue asset.");
            }

            // Find the node index by nodeId
            int nodeIndex = -1;
            for (int i = 0; i < nodesProp.arraySize; i++)
            {
                var nodeProp = nodesProp.GetArrayElementAtIndex(i);
                var nodeIdProp = nodeProp.FindPropertyRelative("nodeId");
                if (nodeIdProp != null && nodeIdProp.stringValue == nodeId)
                {
                    nodeIndex = i;
                    break;
                }
            }

            if (nodeIndex < 0)
            {
                throw new InvalidOperationException($"Node '{nodeId}' not found in dialogue.");
            }

            if (!payload.TryGetValue("node", out var nodeObj) || nodeObj is not Dictionary<string, object> nodeDict)
            {
                throw new InvalidOperationException("node data is required for updateNode operation.");
            }

            // Preserve nodeId
            nodeDict["nodeId"] = nodeId;

            // Update the node properties via SerializedProperty
            var targetNodeProp = nodesProp.GetArrayElementAtIndex(nodeIndex);
            SetNodeProperties(targetNodeProp, nodeDict);

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(dialogue);
            AssetDatabase.SaveAssets();

            var dialogueId = so.FindProperty("dialogueId")?.stringValue ?? "";

            return CreateSuccessResponse(
                ("dialogueId", dialogueId),
                ("nodeId", nodeId),
                ("updated", true)
            );
        }

        private object RemoveNode(Dictionary<string, object> payload)
        {
            var dialogue = ResolveDialogueAsset(payload);
            var nodeId = GetString(payload, "nodeId");

            if (string.IsNullOrEmpty(nodeId))
            {
                throw new InvalidOperationException("nodeId is required for removeNode operation.");
            }

            var so = new SerializedObject(dialogue);
            var nodesProp = so.FindProperty("nodes");
            if (nodesProp == null)
            {
                throw new InvalidOperationException("No nodes property found on dialogue asset.");
            }

            int nodeIndex = -1;
            for (int i = 0; i < nodesProp.arraySize; i++)
            {
                var nodeProp = nodesProp.GetArrayElementAtIndex(i);
                var nodeIdProp = nodeProp.FindPropertyRelative("nodeId");
                if (nodeIdProp != null && nodeIdProp.stringValue == nodeId)
                {
                    nodeIndex = i;
                    break;
                }
            }

            if (nodeIndex < 0)
            {
                throw new InvalidOperationException($"Node '{nodeId}' not found in dialogue.");
            }

            nodesProp.DeleteArrayElementAtIndex(nodeIndex);
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(dialogue);
            AssetDatabase.SaveAssets();

            var dialogueId = so.FindProperty("dialogueId")?.stringValue ?? "";

            return CreateSuccessResponse(
                ("dialogueId", dialogueId),
                ("nodeId", nodeId),
                ("removed", true),
                ("nodeCount", nodesProp.arraySize)
            );
        }

        #endregion

        #region Choice Operations

        private object AddChoice(Dictionary<string, object> payload)
        {
            var dialogue = ResolveDialogueAsset(payload);
            var nodeId = GetString(payload, "nodeId");

            if (string.IsNullOrEmpty(nodeId))
            {
                throw new InvalidOperationException("nodeId is required for addChoice operation.");
            }

            var so = new SerializedObject(dialogue);
            var nodesProp = so.FindProperty("nodes");
            if (nodesProp == null)
            {
                throw new InvalidOperationException("No nodes property found on dialogue asset.");
            }

            // Find the node
            SerializedProperty targetNodeProp = null;
            for (int i = 0; i < nodesProp.arraySize; i++)
            {
                var nodeProp = nodesProp.GetArrayElementAtIndex(i);
                var nodeIdProp = nodeProp.FindPropertyRelative("nodeId");
                if (nodeIdProp != null && nodeIdProp.stringValue == nodeId)
                {
                    targetNodeProp = nodeProp;
                    break;
                }
            }

            if (targetNodeProp == null)
            {
                throw new InvalidOperationException($"Node '{nodeId}' not found in dialogue.");
            }

            if (!payload.TryGetValue("choice", out var choiceObj) || choiceObj is not Dictionary<string, object> choiceDict)
            {
                throw new InvalidOperationException("choice data is required for addChoice operation.");
            }

            var choiceId = choiceDict.TryGetValue("choiceId", out var cid)
                ? cid?.ToString()
                : $"choice_{Guid.NewGuid().ToString().Substring(0, 8)}";
            choiceDict["choiceId"] = choiceId;

            var choicesProp = targetNodeProp.FindPropertyRelative("choices");
            if (choicesProp == null)
            {
                throw new InvalidOperationException("No choices property found on node.");
            }

            var newIndex = choicesProp.arraySize;
            choicesProp.InsertArrayElementAtIndex(newIndex);
            var newChoiceProp = choicesProp.GetArrayElementAtIndex(newIndex);
            SetChoiceProperties(newChoiceProp, choiceDict);

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(dialogue);
            AssetDatabase.SaveAssets();

            var dialogueId = so.FindProperty("dialogueId")?.stringValue ?? "";

            return CreateSuccessResponse(
                ("dialogueId", dialogueId),
                ("nodeId", nodeId),
                ("choiceId", choiceId),
                ("choiceCount", choicesProp.arraySize)
            );
        }

        private object UpdateChoice(Dictionary<string, object> payload)
        {
            var dialogue = ResolveDialogueAsset(payload);
            var nodeId = GetString(payload, "nodeId");
            var choiceId = GetString(payload, "choiceId");

            if (string.IsNullOrEmpty(nodeId) || string.IsNullOrEmpty(choiceId))
            {
                throw new InvalidOperationException("nodeId and choiceId are required for updateChoice operation.");
            }

            var so = new SerializedObject(dialogue);
            var nodesProp = so.FindProperty("nodes");
            if (nodesProp == null)
            {
                throw new InvalidOperationException("No nodes property found on dialogue asset.");
            }

            // Find the node
            SerializedProperty targetNodeProp = null;
            for (int i = 0; i < nodesProp.arraySize; i++)
            {
                var nodeProp = nodesProp.GetArrayElementAtIndex(i);
                var nodeIdProp = nodeProp.FindPropertyRelative("nodeId");
                if (nodeIdProp != null && nodeIdProp.stringValue == nodeId)
                {
                    targetNodeProp = nodeProp;
                    break;
                }
            }

            if (targetNodeProp == null)
            {
                throw new InvalidOperationException($"Node '{nodeId}' not found in dialogue.");
            }

            var choicesProp = targetNodeProp.FindPropertyRelative("choices");
            if (choicesProp == null)
            {
                throw new InvalidOperationException("No choices property found on node.");
            }

            int choiceIndex = -1;
            for (int i = 0; i < choicesProp.arraySize; i++)
            {
                var cp = choicesProp.GetArrayElementAtIndex(i);
                var cidProp = cp.FindPropertyRelative("choiceId");
                if (cidProp != null && cidProp.stringValue == choiceId)
                {
                    choiceIndex = i;
                    break;
                }
            }

            if (choiceIndex < 0)
            {
                throw new InvalidOperationException($"Choice '{choiceId}' not found in node '{nodeId}'.");
            }

            if (!payload.TryGetValue("choice", out var choiceObj) || choiceObj is not Dictionary<string, object> choiceDict)
            {
                throw new InvalidOperationException("choice data is required for updateChoice operation.");
            }

            choiceDict["choiceId"] = choiceId;
            var targetChoiceProp = choicesProp.GetArrayElementAtIndex(choiceIndex);
            SetChoiceProperties(targetChoiceProp, choiceDict);

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(dialogue);
            AssetDatabase.SaveAssets();

            var dialogueIdVal = so.FindProperty("dialogueId")?.stringValue ?? "";

            return CreateSuccessResponse(
                ("dialogueId", dialogueIdVal),
                ("nodeId", nodeId),
                ("choiceId", choiceId),
                ("updated", true)
            );
        }

        private object RemoveChoice(Dictionary<string, object> payload)
        {
            var dialogue = ResolveDialogueAsset(payload);
            var nodeId = GetString(payload, "nodeId");
            var choiceId = GetString(payload, "choiceId");

            if (string.IsNullOrEmpty(nodeId) || string.IsNullOrEmpty(choiceId))
            {
                throw new InvalidOperationException("nodeId and choiceId are required for removeChoice operation.");
            }

            var so = new SerializedObject(dialogue);
            var nodesProp = so.FindProperty("nodes");
            if (nodesProp == null)
            {
                throw new InvalidOperationException("No nodes property found on dialogue asset.");
            }

            // Find the node
            SerializedProperty targetNodeProp = null;
            for (int i = 0; i < nodesProp.arraySize; i++)
            {
                var nodeProp = nodesProp.GetArrayElementAtIndex(i);
                var nodeIdProp = nodeProp.FindPropertyRelative("nodeId");
                if (nodeIdProp != null && nodeIdProp.stringValue == nodeId)
                {
                    targetNodeProp = nodeProp;
                    break;
                }
            }

            if (targetNodeProp == null)
            {
                throw new InvalidOperationException($"Node '{nodeId}' not found in dialogue.");
            }

            var choicesProp = targetNodeProp.FindPropertyRelative("choices");
            if (choicesProp == null)
            {
                throw new InvalidOperationException("No choices property found on node.");
            }

            int choiceIndex = -1;
            for (int i = 0; i < choicesProp.arraySize; i++)
            {
                var cp = choicesProp.GetArrayElementAtIndex(i);
                var cidProp = cp.FindPropertyRelative("choiceId");
                if (cidProp != null && cidProp.stringValue == choiceId)
                {
                    choiceIndex = i;
                    break;
                }
            }

            if (choiceIndex < 0)
            {
                throw new InvalidOperationException($"Choice '{choiceId}' not found in node '{nodeId}'.");
            }

            choicesProp.DeleteArrayElementAtIndex(choiceIndex);
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(dialogue);
            AssetDatabase.SaveAssets();

            var dialogueIdVal = so.FindProperty("dialogueId")?.stringValue ?? "";

            return CreateSuccessResponse(
                ("dialogueId", dialogueIdVal),
                ("nodeId", nodeId),
                ("choiceId", choiceId),
                ("removed", true),
                ("choiceCount", choicesProp.arraySize)
            );
        }

        #endregion

        #region Runtime Operations

        private object StartDialogue(Dictionary<string, object> payload)
        {
            var dialogueId = GetString(payload, "dialogueId");
            if (string.IsNullOrEmpty(dialogueId))
            {
                throw new InvalidOperationException("dialogueId is required for startDialogue operation.");
            }

            var dialogueAsset = ResolveDialogueAsset(payload);
            var manager = GetDialogueManager();

            // Use reflection to call RegisterDialogue on the manager
            var managerType = manager.GetType();
            var registerMethod = managerType.GetMethod("RegisterDialogue",
                BindingFlags.Public | BindingFlags.Instance);

            if (registerMethod != null)
            {
                // Build the DialogueData argument using reflection
                var dialogueDataType = FindNestedType(managerType, "DialogueData");
                if (dialogueDataType != null)
                {
                    var dialogueData = CreateDialogueDataFromAsset(dialogueAsset, dialogueDataType);
                    if (dialogueData != null)
                    {
                        registerMethod.Invoke(manager, new[] { dialogueData });
                    }
                }
            }

            return CreateSuccessResponse(
                ("dialogueId", dialogueId),
                ("started", true),
                ("note", "Dialogue registered. Start dialogue in play mode to run it.")
            );
        }

        private object SelectChoice(Dictionary<string, object> payload)
        {
            var choiceIndex = GetInt(payload, "choiceIndex", -1);

            return CreateSuccessResponse(
                ("choiceIndex", choiceIndex),
                ("note", "Choice selection only works in play mode.")
            );
        }

        private object AdvanceDialogue(Dictionary<string, object> payload)
        {
            return CreateSuccessResponse(
                ("note", "Dialogue advancement only works in play mode.")
            );
        }

        private object EndDialogue(Dictionary<string, object> payload)
        {
            return CreateSuccessResponse(
                ("note", "End dialogue only works in play mode.")
            );
        }

        #endregion

        #region Manager Operations

        private object CreateManager(Dictionary<string, object> payload)
        {
            var targetPath = GetString(payload, "targetPath");
            GameObject targetGo;

            if (string.IsNullOrEmpty(targetPath))
            {
                targetGo = new GameObject("DialogueManager");
                Undo.RegisterCreatedObjectUndo(targetGo, "Create Dialogue Manager");
            }
            else
            {
                targetGo = ResolveGameObject(targetPath);
            }

            if (targetGo == null)
            {
                throw new InvalidOperationException("Failed to create or find target GameObject.");
            }

            // Check if already has a dialogue manager component (by checking for dialogueManagerId field)
            var existingManager = CodeGenHelper.FindComponentByField(targetGo, "dialogueManagerId", null);
            if (existingManager != null)
            {
                throw new InvalidOperationException("GameObject already has a DialogueManager component.");
            }

            var dialogueManagerId = GetString(payload, "dialogueManagerId")
                ?? $"DialogueManager_{Guid.NewGuid().ToString().Substring(0, 8)}";

            var defaultTypingSpeed = GetFloat(payload, "defaultTypingSpeed", 0.05f);
            var pauseGame = GetBool(payload, "pauseGameDuringDialogue", false);

            var className = GetString(payload, "className")
                ?? ScriptGenerator.ToPascalCase(dialogueManagerId, "DialogueManager");

            // Build template variables
            var variables = new Dictionary<string, object>
            {
                { "DEFAULT_TYPING_SPEED", defaultTypingSpeed },
                { "PAUSE_GAME", pauseGame.ToString().ToLowerInvariant() }
            };

            // Build properties to set after component is added
            var propertiesToSet = new Dictionary<string, object>();
            if (payload.TryGetValue("defaultTypingSpeed", out var typingObj))
                propertiesToSet["defaultTypingSpeed"] = Convert.ToSingle(typingObj);
            if (payload.TryGetValue("pauseGameDuringDialogue", out var pauseObj))
                propertiesToSet["pauseGameDuringDialogue"] = Convert.ToBoolean(pauseObj);

            var outputDir = GetString(payload, "outputPath");

            // Generate script and optionally attach component
            var result = CodeGenHelper.GenerateAndAttach(
                targetGo, "DialogueManager", dialogueManagerId, className, variables, outputDir,
                propertiesToSet.Count > 0 ? propertiesToSet : null);

            if (result.TryGetValue("success", out var success) && !(bool)success)
            {
                throw new InvalidOperationException(result.TryGetValue("error", out var err)
                    ? err.ToString()
                    : "Failed to generate DialogueManager script.");
            }

            EditorSceneManager.MarkSceneDirty(targetGo.scene);

            result["dialogueManagerId"] = dialogueManagerId;
            result["path"] = BuildGameObjectPath(targetGo);

            return result;
        }

        private object InspectManager(Dictionary<string, object> payload)
        {
            var manager = GetDialogueManager();
            var so = new SerializedObject(manager);

            var info = new Dictionary<string, object>
            {
                { "path", BuildGameObjectPath(manager.gameObject) },
                { "defaultTypingSpeed", so.FindProperty("defaultTypingSpeed")?.floatValue ?? 0.05f },
                { "pauseGameDuringDialogue", so.FindProperty("pauseGameDuringDialogue")?.boolValue ?? false },
                { "note", "Runtime state (isDialogueActive, currentNode) is only available in play mode." }
            };

            return CreateSuccessResponse(("manager", info));
        }

        private object DeleteManager(Dictionary<string, object> payload)
        {
            var manager = GetDialogueManager();
            var path = BuildGameObjectPath(manager.gameObject);
            var scene = manager.gameObject.scene;

            // Try to get the dialogueManagerId before destroying
            var so = new SerializedObject(manager);
            var dialogueManagerId = "";
            var idProp = so.FindProperty("dialogueManagerId");
            if (idProp != null)
                dialogueManagerId = idProp.stringValue;

            Undo.DestroyObjectImmediate(manager);

            // Clean up the generated script from tracker
            if (!string.IsNullOrEmpty(dialogueManagerId))
            {
                ScriptGenerator.Delete(dialogueManagerId);
            }

            EditorSceneManager.MarkSceneDirty(scene);

            return CreateSuccessResponse(
                ("path", path),
                ("deleted", true)
            );
        }

        #endregion

        #region Find

        private object FindByDialogueId(Dictionary<string, object> payload)
        {
            var dialogueId = GetString(payload, "dialogueId");
            if (string.IsNullOrEmpty(dialogueId))
            {
                throw new InvalidOperationException("dialogueId is required for findByDialogueId.");
            }

            // Search all ScriptableObject assets for one with a matching dialogueId field
            var guids = AssetDatabase.FindAssets("t:ScriptableObject");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset == null) continue;

                var so = new SerializedObject(asset);
                var idProp = so.FindProperty("dialogueId");
                if (idProp != null && idProp.propertyType == SerializedPropertyType.String
                    && idProp.stringValue == dialogueId)
                {
                    var nodesProp = so.FindProperty("nodes");
                    var nodeCount = nodesProp != null ? nodesProp.arraySize : 0;

                    return CreateSuccessResponse(
                        ("found", true),
                        ("dialogueId", dialogueId),
                        ("assetPath", path),
                        ("nodeCount", nodeCount)
                    );
                }
            }

            return CreateSuccessResponse(("found", false), ("dialogueId", dialogueId));
        }

        #endregion

        #region Helpers

        private ScriptableObject ResolveDialogueAsset(Dictionary<string, object> payload)
        {
            var dialogueId = GetString(payload, "dialogueId");
            var assetPath = GetString(payload, "assetPath");

            if (!string.IsNullOrEmpty(assetPath))
            {
                var dialogue = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
                if (dialogue != null)
                {
                    // Verify it has a dialogueId field
                    var so = new SerializedObject(dialogue);
                    if (so.FindProperty("dialogueId") != null)
                        return dialogue;
                }
            }

            if (!string.IsNullOrEmpty(dialogueId))
            {
                var guids = AssetDatabase.FindAssets("t:ScriptableObject");
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var dialogue = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                    if (dialogue == null) continue;

                    var so = new SerializedObject(dialogue);
                    var idProp = so.FindProperty("dialogueId");
                    if (idProp != null && idProp.propertyType == SerializedPropertyType.String
                        && idProp.stringValue == dialogueId)
                    {
                        return dialogue;
                    }
                }
            }

            throw new InvalidOperationException("Either assetPath or dialogueId is required to resolve dialogue asset.");
        }

        private Component GetDialogueManager()
        {
            // Search for any MonoBehaviour with a dialogueManagerId or that looks like a dialogue manager
            // Use the pattern of finding by a known field from the DialogueManager template
            var component = CodeGenHelper.FindComponentInSceneByField("defaultTypingSpeed", null);

            // Fallback: search all MonoBehaviours for one that has isDialogueActive field (from template)
            if (component == null)
            {
                var allMono = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
                foreach (var mb in allMono)
                {
                    if (mb == null) continue;
                    try
                    {
                        var so = new SerializedObject(mb);
                        var prop = so.FindProperty("pauseGameDuringDialogue");
                        if (prop != null && prop.propertyType == SerializedPropertyType.Boolean)
                        {
                            // Verify it also has the dialogueRegistry signature (has events)
                            var onDialogueEndProp = so.FindProperty("OnDialogueEnd");
                            if (onDialogueEndProp != null)
                            {
                                return mb;
                            }
                        }
                    }
                    catch
                    {
                        // Skip components that can't be serialized
                    }
                }
            }
            else
            {
                return component;
            }

            throw new InvalidOperationException("No DialogueManager found in scene. Create one first with createManager.");
        }

        /// <summary>
        /// Adds a node to a dialogue ScriptableObject asset via SerializedObject.
        /// </summary>
        private void AddNodeToDialogueAsset(UnityEngine.Object dialogue, Type assetType, Dictionary<string, object> nodeDict)
        {
            // Use reflection to call AddNode if available
            var addNodeMethod = assetType.GetMethod("AddNode", BindingFlags.Public | BindingFlags.Instance);
            if (addNodeMethod != null)
            {
                var nodeParamType = addNodeMethod.GetParameters()[0].ParameterType;
                var node = CreateNodeInstance(nodeParamType, nodeDict);
                if (node != null)
                {
                    addNodeMethod.Invoke(dialogue, new[] { node });
                    return;
                }
            }

            // Fallback: use SerializedObject to append to nodes array
            var so = new SerializedObject(dialogue);
            var nodesProp = so.FindProperty("nodes");
            if (nodesProp != null)
            {
                var newIndex = nodesProp.arraySize;
                nodesProp.InsertArrayElementAtIndex(newIndex);
                var newNodeProp = nodesProp.GetArrayElementAtIndex(newIndex);
                SetNodeProperties(newNodeProp, nodeDict);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        /// <summary>
        /// Creates a node instance via reflection from a dictionary.
        /// </summary>
        private object CreateNodeInstance(Type nodeType, Dictionary<string, object> dict)
        {
            try
            {
                var node = Activator.CreateInstance(nodeType);

                SetFieldValue(node, nodeType, "nodeId",
                    dict.TryGetValue("nodeId", out var id) ? id?.ToString() : $"node_{Guid.NewGuid().ToString().Substring(0, 8)}");
                SetFieldValue(node, nodeType, "speaker",
                    dict.TryGetValue("speaker", out var speaker) ? speaker?.ToString() : "");
                SetFieldValue(node, nodeType, "text",
                    dict.TryGetValue("text", out var text) ? text?.ToString() : "");
                SetFieldValue(node, nodeType, "nextNodeId",
                    dict.TryGetValue("nextNode", out var next) ? next?.ToString() : "");

                if (dict.TryGetValue("portraitPath", out var portrait))
                    SetFieldValue(node, nodeType, "portraitPath", portrait?.ToString() ?? "");
                if (dict.TryGetValue("voiceClipPath", out var voice))
                    SetFieldValue(node, nodeType, "voiceClipPath", voice?.ToString() ?? "");
                if (dict.TryGetValue("soundEffectPath", out var sfx))
                    SetFieldValue(node, nodeType, "soundEffectPath", sfx?.ToString() ?? "");

                // Set type enum via reflection
                if (dict.TryGetValue("type", out var typeObj))
                {
                    var nodeTypeField = nodeType.GetField("type");
                    if (nodeTypeField != null)
                    {
                        var enumType = nodeTypeField.FieldType;
                        var enumStr = ParseNodeType(typeObj.ToString());
                        if (Enum.TryParse(enumType, enumStr, true, out var enumVal))
                        {
                            nodeTypeField.SetValue(node, enumVal);
                        }
                    }
                }

                // Add choices via reflection
                if (dict.TryGetValue("choices", out var choicesObj) && choicesObj is List<object> choicesList)
                {
                    var choicesField = nodeType.GetField("choices");
                    if (choicesField != null)
                    {
                        var choiceListType = choicesField.FieldType;
                        var choiceItemType = choiceListType.GetGenericArguments()[0];
                        var addMethod = choiceListType.GetMethod("Add");
                        var choiceList = choicesField.GetValue(node);

                        foreach (var choiceObj in choicesList)
                        {
                            if (choiceObj is Dictionary<string, object> choiceDict)
                            {
                                var choice = CreateChoiceInstance(choiceItemType, choiceDict);
                                if (choice != null)
                                {
                                    addMethod.Invoke(choiceList, new[] { choice });
                                }
                            }
                        }
                    }
                }

                // Add conditions via reflection
                if (dict.TryGetValue("conditions", out var conditionsObj) && conditionsObj is List<object> conditionsList)
                {
                    var conditionsField = nodeType.GetField("conditions");
                    if (conditionsField != null)
                    {
                        var condListType = conditionsField.FieldType;
                        var condItemType = condListType.GetGenericArguments()[0];
                        var addMethod = condListType.GetMethod("Add");
                        var condList = conditionsField.GetValue(node);

                        foreach (var condObj in conditionsList)
                        {
                            if (condObj is Dictionary<string, object> condDict)
                            {
                                var cond = CreateConditionInstance(condItemType, condDict);
                                if (cond != null)
                                {
                                    addMethod.Invoke(condList, new[] { cond });
                                }
                            }
                        }
                    }
                }

                // Add actions via reflection
                if (dict.TryGetValue("actions", out var actionsObj) && actionsObj is List<object> actionsList)
                {
                    var actionsField = nodeType.GetField("actions");
                    if (actionsField != null)
                    {
                        var actionListType = actionsField.FieldType;
                        var actionItemType = actionListType.GetGenericArguments()[0];
                        var addMethod = actionListType.GetMethod("Add");
                        var actionList = actionsField.GetValue(node);

                        foreach (var actionObj in actionsList)
                        {
                            if (actionObj is Dictionary<string, object> actionDict)
                            {
                                var action = CreateActionInstance(actionItemType, actionDict);
                                if (action != null)
                                {
                                    addMethod.Invoke(actionList, new[] { action });
                                }
                            }
                        }
                    }
                }

                return node;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Creates a choice instance via reflection from a dictionary.
        /// </summary>
        private object CreateChoiceInstance(Type choiceType, Dictionary<string, object> dict)
        {
            try
            {
                var choice = Activator.CreateInstance(choiceType);

                SetFieldValue(choice, choiceType, "choiceId",
                    dict.TryGetValue("choiceId", out var id) ? id?.ToString() : $"choice_{Guid.NewGuid().ToString().Substring(0, 8)}");
                SetFieldValue(choice, choiceType, "text",
                    dict.TryGetValue("text", out var text) ? text?.ToString() : "");
                SetFieldValue(choice, choiceType, "targetNodeId",
                    dict.TryGetValue("nextNode", out var next) ? next?.ToString() : "");

                if (dict.TryGetValue("isDefault", out var def))
                    SetFieldValue(choice, choiceType, "isDefault", Convert.ToBoolean(def));

                // Add conditions via reflection
                if (dict.TryGetValue("conditions", out var conditionsObj) && conditionsObj is List<object> conditionsList)
                {
                    var conditionsField = choiceType.GetField("conditions");
                    if (conditionsField != null)
                    {
                        var condListType = conditionsField.FieldType;
                        var condItemType = condListType.GetGenericArguments()[0];
                        var addMethod = condListType.GetMethod("Add");
                        var condList = conditionsField.GetValue(choice);

                        foreach (var condObj in conditionsList)
                        {
                            if (condObj is Dictionary<string, object> condDict)
                            {
                                var cond = CreateConditionInstance(condItemType, condDict);
                                if (cond != null)
                                {
                                    addMethod.Invoke(condList, new[] { cond });
                                }
                            }
                        }
                    }
                }

                // Add actions via reflection
                if (dict.TryGetValue("actions", out var actionsObj) && actionsObj is List<object> actionsList)
                {
                    var actionsField = choiceType.GetField("actions");
                    if (actionsField != null)
                    {
                        var actionListType = actionsField.FieldType;
                        var actionItemType = actionListType.GetGenericArguments()[0];
                        var addMethod = actionListType.GetMethod("Add");
                        var actionList = actionsField.GetValue(choice);

                        foreach (var actionObj in actionsList)
                        {
                            if (actionObj is Dictionary<string, object> actionDict)
                            {
                                var action = CreateActionInstance(actionItemType, actionDict);
                                if (action != null)
                                {
                                    addMethod.Invoke(actionList, new[] { action });
                                }
                            }
                        }
                    }
                }

                return choice;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Creates a condition instance via reflection from a dictionary.
        /// </summary>
        private object CreateConditionInstance(Type condType, Dictionary<string, object> dict)
        {
            try
            {
                var cond = Activator.CreateInstance(condType);

                if (dict.TryGetValue("type", out var type))
                {
                    var typeField = condType.GetField("type");
                    if (typeField != null)
                    {
                        var enumStr = ParseConditionType(type.ToString());
                        if (Enum.TryParse(typeField.FieldType, enumStr, true, out var enumVal))
                            typeField.SetValue(cond, enumVal);
                    }
                }

                SetFieldValue(cond, condType, "target",
                    dict.TryGetValue("targetId", out var targetId) ? targetId?.ToString() : "");
                SetFieldValue(cond, condType, "property",
                    dict.TryGetValue("propertyName", out var prop) ? prop?.ToString() : "");

                if (dict.TryGetValue("comparison", out var comp))
                {
                    var compField = condType.GetField("comparison");
                    if (compField != null)
                    {
                        var compStr = ParseComparisonOperator(comp.ToString());
                        if (Enum.TryParse(compField.FieldType, compStr, true, out var enumVal))
                            compField.SetValue(cond, enumVal);
                    }
                }

                SetFieldValue(cond, condType, "value",
                    dict.TryGetValue("value", out var val) ? val?.ToString() : "");

                if (dict.TryGetValue("negate", out var neg))
                    SetFieldValue(cond, condType, "negate", Convert.ToBoolean(neg));

                return cond;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Creates an action instance via reflection from a dictionary.
        /// </summary>
        private object CreateActionInstance(Type actionType, Dictionary<string, object> dict)
        {
            try
            {
                var action = Activator.CreateInstance(actionType);

                if (dict.TryGetValue("type", out var type))
                {
                    var typeField = actionType.GetField("type");
                    if (typeField != null)
                    {
                        var enumStr = ParseActionType(type.ToString());
                        if (Enum.TryParse(typeField.FieldType, enumStr, true, out var enumVal))
                            typeField.SetValue(action, enumVal);
                    }
                }

                SetFieldValue(action, actionType, "target",
                    dict.TryGetValue("targetId", out var targetId) ? targetId?.ToString() : "");
                SetFieldValue(action, actionType, "parameter",
                    dict.TryGetValue("parameter", out var param) ? param?.ToString() : "");

                if (dict.TryGetValue("value", out var val))
                {
                    var numField = actionType.GetField("numericValue");
                    if (numField != null)
                    {
                        try { numField.SetValue(action, Convert.ToSingle(val)); }
                        catch { /* non-numeric value, skip */ }
                    }
                }

                return action;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Creates a DialogueData runtime object from a ScriptableObject asset via reflection.
        /// Used for registering dialogue with the manager.
        /// </summary>
        private object CreateDialogueDataFromAsset(ScriptableObject asset, Type dialogueDataType)
        {
            try
            {
                var data = Activator.CreateInstance(dialogueDataType);
                var so = new SerializedObject(asset);

                // Copy dialogueId
                var dialogueIdProp = so.FindProperty("dialogueId");
                if (dialogueIdProp != null)
                    SetFieldValue(data, dialogueDataType, "dialogueId", dialogueIdProp.stringValue);

                // Copy startNodeId
                var startNodeProp = so.FindProperty("startNodeId");
                if (startNodeProp != null)
                    SetFieldValue(data, dialogueDataType, "startNodeId", startNodeProp.stringValue);

                // Copy autoAdvance
                var autoAdvanceProp = so.FindProperty("autoAdvance");
                if (autoAdvanceProp != null)
                    SetFieldValue(data, dialogueDataType, "autoAdvance", autoAdvanceProp.boolValue);

                // Copy autoAdvanceDelay
                var autoAdvanceDelayProp = so.FindProperty("autoAdvanceDelay");
                if (autoAdvanceDelayProp != null)
                    SetFieldValue(data, dialogueDataType, "autoAdvanceDelay", autoAdvanceDelayProp.floatValue);

                return data;
            }
            catch
            {
                return null;
            }
        }

        private static void SetFieldValue(object obj, Type type, string fieldName, object value)
        {
            var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(obj, value);
            }
        }

        private static Type FindNestedType(Type parentType, string nestedTypeName)
        {
            return parentType.GetNestedType(nestedTypeName, BindingFlags.Public | BindingFlags.NonPublic);
        }

        /// <summary>
        /// Sets node properties on a SerializedProperty representing a DialogueNode.
        /// </summary>
        private void SetNodeProperties(SerializedProperty nodeProp, Dictionary<string, object> dict)
        {
            if (dict.TryGetValue("nodeId", out var id))
            {
                var p = nodeProp.FindPropertyRelative("nodeId");
                if (p != null) p.stringValue = id?.ToString() ?? "";
            }

            if (dict.TryGetValue("speaker", out var speaker))
            {
                var p = nodeProp.FindPropertyRelative("speaker");
                if (p != null) p.stringValue = speaker?.ToString() ?? "";
            }

            if (dict.TryGetValue("text", out var text))
            {
                var p = nodeProp.FindPropertyRelative("text");
                if (p != null) p.stringValue = text?.ToString() ?? "";
            }

            if (dict.TryGetValue("nextNode", out var next))
            {
                var p = nodeProp.FindPropertyRelative("nextNodeId");
                if (p != null) p.stringValue = next?.ToString() ?? "";
            }

            if (dict.TryGetValue("portraitPath", out var portrait))
            {
                var p = nodeProp.FindPropertyRelative("portraitPath");
                if (p != null) p.stringValue = portrait?.ToString() ?? "";
            }

            if (dict.TryGetValue("voiceClipPath", out var voice))
            {
                var p = nodeProp.FindPropertyRelative("voiceClipPath");
                if (p != null) p.stringValue = voice?.ToString() ?? "";
            }

            if (dict.TryGetValue("soundEffectPath", out var sfx))
            {
                var p = nodeProp.FindPropertyRelative("soundEffectPath");
                if (p != null) p.stringValue = sfx?.ToString() ?? "";
            }

            if (dict.TryGetValue("type", out var typeObj))
            {
                var p = nodeProp.FindPropertyRelative("type");
                if (p != null)
                {
                    var enumStr = ParseNodeType(typeObj.ToString());
                    var names = p.enumDisplayNames;
                    for (int i = 0; i < names.Length; i++)
                    {
                        if (string.Equals(names[i], enumStr, StringComparison.OrdinalIgnoreCase))
                        {
                            p.enumValueIndex = i;
                            break;
                        }
                    }
                }
            }

            // Choices
            if (dict.TryGetValue("choices", out var choicesObj) && choicesObj is List<object> choicesList)
            {
                var choicesProp = nodeProp.FindPropertyRelative("choices");
                if (choicesProp != null)
                {
                    choicesProp.ClearArray();
                    for (int i = 0; i < choicesList.Count; i++)
                    {
                        if (choicesList[i] is Dictionary<string, object> choiceDict)
                        {
                            choicesProp.InsertArrayElementAtIndex(i);
                            var choiceProp = choicesProp.GetArrayElementAtIndex(i);
                            SetChoiceProperties(choiceProp, choiceDict);
                        }
                    }
                }
            }

            // Conditions
            if (dict.TryGetValue("conditions", out var conditionsObj) && conditionsObj is List<object> conditionsList)
            {
                var conditionsProp = nodeProp.FindPropertyRelative("conditions");
                if (conditionsProp != null)
                {
                    conditionsProp.ClearArray();
                    for (int i = 0; i < conditionsList.Count; i++)
                    {
                        if (conditionsList[i] is Dictionary<string, object> condDict)
                        {
                            conditionsProp.InsertArrayElementAtIndex(i);
                            var condProp = conditionsProp.GetArrayElementAtIndex(i);
                            SetConditionProperties(condProp, condDict);
                        }
                    }
                }
            }

            // Actions
            if (dict.TryGetValue("actions", out var actionsObj) && actionsObj is List<object> actionsList)
            {
                var actionsProp = nodeProp.FindPropertyRelative("actions");
                if (actionsProp != null)
                {
                    actionsProp.ClearArray();
                    for (int i = 0; i < actionsList.Count; i++)
                    {
                        if (actionsList[i] is Dictionary<string, object> actionDict)
                        {
                            actionsProp.InsertArrayElementAtIndex(i);
                            var actionProp = actionsProp.GetArrayElementAtIndex(i);
                            SetActionProperties(actionProp, actionDict);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sets choice properties on a SerializedProperty representing a DialogueChoice.
        /// </summary>
        private void SetChoiceProperties(SerializedProperty choiceProp, Dictionary<string, object> dict)
        {
            if (dict.TryGetValue("choiceId", out var id))
            {
                var p = choiceProp.FindPropertyRelative("choiceId");
                if (p != null) p.stringValue = id?.ToString() ?? "";
            }

            if (dict.TryGetValue("text", out var text))
            {
                var p = choiceProp.FindPropertyRelative("text");
                if (p != null) p.stringValue = text?.ToString() ?? "";
            }

            if (dict.TryGetValue("nextNode", out var next))
            {
                var p = choiceProp.FindPropertyRelative("targetNodeId");
                if (p != null) p.stringValue = next?.ToString() ?? "";
            }

            if (dict.TryGetValue("isDefault", out var def))
            {
                var p = choiceProp.FindPropertyRelative("isDefault");
                if (p != null) p.boolValue = Convert.ToBoolean(def);
            }

            // Conditions
            if (dict.TryGetValue("conditions", out var conditionsObj) && conditionsObj is List<object> conditionsList)
            {
                var conditionsProp = choiceProp.FindPropertyRelative("conditions");
                if (conditionsProp != null)
                {
                    conditionsProp.ClearArray();
                    for (int i = 0; i < conditionsList.Count; i++)
                    {
                        if (conditionsList[i] is Dictionary<string, object> condDict)
                        {
                            conditionsProp.InsertArrayElementAtIndex(i);
                            var condProp = conditionsProp.GetArrayElementAtIndex(i);
                            SetConditionProperties(condProp, condDict);
                        }
                    }
                }
            }

            // Actions
            if (dict.TryGetValue("actions", out var actionsObj) && actionsObj is List<object> actionsList)
            {
                var actionsProp = choiceProp.FindPropertyRelative("actions");
                if (actionsProp != null)
                {
                    actionsProp.ClearArray();
                    for (int i = 0; i < actionsList.Count; i++)
                    {
                        if (actionsList[i] is Dictionary<string, object> actionDict)
                        {
                            actionsProp.InsertArrayElementAtIndex(i);
                            var actionProp = actionsProp.GetArrayElementAtIndex(i);
                            SetActionProperties(actionProp, actionDict);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sets condition properties on a SerializedProperty representing a DialogueCondition.
        /// </summary>
        private void SetConditionProperties(SerializedProperty condProp, Dictionary<string, object> dict)
        {
            if (dict.TryGetValue("type", out var type))
            {
                var p = condProp.FindPropertyRelative("type");
                if (p != null)
                {
                    var enumStr = ParseConditionType(type.ToString());
                    var names = p.enumDisplayNames;
                    for (int i = 0; i < names.Length; i++)
                    {
                        if (string.Equals(names[i], enumStr, StringComparison.OrdinalIgnoreCase))
                        {
                            p.enumValueIndex = i;
                            break;
                        }
                    }
                }
            }

            if (dict.TryGetValue("targetId", out var targetId))
            {
                var p = condProp.FindPropertyRelative("target");
                if (p != null) p.stringValue = targetId?.ToString() ?? "";
            }

            if (dict.TryGetValue("propertyName", out var prop))
            {
                var p = condProp.FindPropertyRelative("property");
                if (p != null) p.stringValue = prop?.ToString() ?? "";
            }

            if (dict.TryGetValue("comparison", out var comp))
            {
                var p = condProp.FindPropertyRelative("comparison");
                if (p != null)
                {
                    var enumStr = ParseComparisonOperator(comp.ToString());
                    var names = p.enumDisplayNames;
                    for (int i = 0; i < names.Length; i++)
                    {
                        if (string.Equals(names[i], enumStr, StringComparison.OrdinalIgnoreCase))
                        {
                            p.enumValueIndex = i;
                            break;
                        }
                    }
                }
            }

            if (dict.TryGetValue("value", out var val))
            {
                var p = condProp.FindPropertyRelative("value");
                if (p != null) p.stringValue = val?.ToString() ?? "";
            }

            if (dict.TryGetValue("negate", out var neg))
            {
                var p = condProp.FindPropertyRelative("negate");
                if (p != null) p.boolValue = Convert.ToBoolean(neg);
            }
        }

        /// <summary>
        /// Sets action properties on a SerializedProperty representing a DialogueAction.
        /// </summary>
        private void SetActionProperties(SerializedProperty actionProp, Dictionary<string, object> dict)
        {
            if (dict.TryGetValue("type", out var type))
            {
                var p = actionProp.FindPropertyRelative("type");
                if (p != null)
                {
                    var enumStr = ParseActionType(type.ToString());
                    var names = p.enumDisplayNames;
                    for (int i = 0; i < names.Length; i++)
                    {
                        if (string.Equals(names[i], enumStr, StringComparison.OrdinalIgnoreCase))
                        {
                            p.enumValueIndex = i;
                            break;
                        }
                    }
                }
            }

            if (dict.TryGetValue("targetId", out var targetId))
            {
                var p = actionProp.FindPropertyRelative("target");
                if (p != null) p.stringValue = targetId?.ToString() ?? "";
            }

            if (dict.TryGetValue("parameter", out var param))
            {
                var p = actionProp.FindPropertyRelative("parameter");
                if (p != null) p.stringValue = param?.ToString() ?? "";
            }

            if (dict.TryGetValue("value", out var val))
            {
                var p = actionProp.FindPropertyRelative("numericValue");
                if (p != null)
                {
                    try { p.floatValue = Convert.ToSingle(val); }
                    catch { /* non-numeric value, skip */ }
                }
            }
        }

        /// <summary>
        /// Serializes a dialogue node from a SerializedProperty into a dictionary for inspection.
        /// </summary>
        private Dictionary<string, object> SerializeDialogueNodeFromProperty(SerializedProperty nodeProp)
        {
            var dict = new Dictionary<string, object>
            {
                { "nodeId", nodeProp.FindPropertyRelative("nodeId")?.stringValue ?? "" },
                { "speaker", nodeProp.FindPropertyRelative("speaker")?.stringValue ?? "" },
                { "text", nodeProp.FindPropertyRelative("text")?.stringValue ?? "" },
                { "nextNode", nodeProp.FindPropertyRelative("nextNodeId")?.stringValue ?? "" }
            };

            var typeProp = nodeProp.FindPropertyRelative("type");
            if (typeProp != null)
            {
                dict["type"] = typeProp.enumValueIndex < typeProp.enumDisplayNames.Length
                    ? typeProp.enumDisplayNames[typeProp.enumValueIndex]
                    : "Dialogue";
            }

            var portraitProp = nodeProp.FindPropertyRelative("portraitPath");
            if (portraitProp != null && !string.IsNullOrEmpty(portraitProp.stringValue))
                dict["portraitPath"] = portraitProp.stringValue;

            var voiceProp = nodeProp.FindPropertyRelative("voiceClipPath");
            if (voiceProp != null && !string.IsNullOrEmpty(voiceProp.stringValue))
                dict["voiceClipPath"] = voiceProp.stringValue;

            var sfxProp = nodeProp.FindPropertyRelative("soundEffectPath");
            if (sfxProp != null && !string.IsNullOrEmpty(sfxProp.stringValue))
                dict["soundEffectPath"] = sfxProp.stringValue;

            // Choices
            var choicesProp = nodeProp.FindPropertyRelative("choices");
            if (choicesProp != null && choicesProp.arraySize > 0)
            {
                var choices = new List<Dictionary<string, object>>();
                for (int i = 0; i < choicesProp.arraySize; i++)
                {
                    var choiceProp = choicesProp.GetArrayElementAtIndex(i);
                    choices.Add(new Dictionary<string, object>
                    {
                        { "choiceId", choiceProp.FindPropertyRelative("choiceId")?.stringValue ?? "" },
                        { "text", choiceProp.FindPropertyRelative("text")?.stringValue ?? "" },
                        { "nextNode", choiceProp.FindPropertyRelative("targetNodeId")?.stringValue ?? "" },
                        { "isDefault", choiceProp.FindPropertyRelative("isDefault")?.boolValue ?? false }
                    });
                }
                dict["choices"] = choices;
            }

            return dict;
        }

        private string ParseNodeType(string str)
        {
            return str?.ToLowerInvariant() switch
            {
                "dialogue" => "Dialogue",
                "choice" => "Choice",
                "branch" => "Branch",
                "action" => "Action",
                "exit" => "Exit",
                _ => "Dialogue"
            };
        }

        private string ParseConditionType(string str)
        {
            return str?.ToLowerInvariant() switch
            {
                "quest" => "Quest",
                "resource" => "Resource",
                "inventory" => "Inventory",
                "variable" => "Variable",
                "health" => "Health",
                "custom" => "Custom",
                _ => "Variable"
            };
        }

        private string ParseComparisonOperator(string str)
        {
            return str?.ToLowerInvariant() switch
            {
                "equals" or "==" or "eq" => "Equals",
                "notequals" or "!=" or "ne" => "NotEquals",
                "greaterthan" or ">" or "gt" => "GreaterThan",
                "lessthan" or "<" or "lt" => "LessThan",
                "greaterorequal" or ">=" or "gte" => "GreaterOrEqual",
                "lessorequal" or "<=" or "lte" => "LessOrEqual",
                "contains" => "Contains",
                _ => "Equals"
            };
        }

        private string ParseActionType(string str)
        {
            return str?.ToLowerInvariant() switch
            {
                "startquest" => "StartQuest",
                "completequest" => "CompleteQuest",
                "addresource" => "AddResource",
                "removeresource" => "RemoveResource",
                "additem" => "AddItem",
                "removeitem" => "RemoveItem",
                "setvariable" => "SetVariable",
                "playeffect" => "PlayEffect",
                "openshop" => "OpenShop",
                "teleport" => "Teleport",
                "custom" => "Custom",
                _ => "Custom"
            };
        }

        #endregion
    }
}
