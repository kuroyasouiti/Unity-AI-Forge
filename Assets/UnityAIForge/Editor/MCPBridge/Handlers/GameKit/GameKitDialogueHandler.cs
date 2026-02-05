using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MCP.Editor.Base;
using UnityAIForge.GameKit;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor.Handlers.GameKit
{
    /// <summary>
    /// GameKit Dialogue handler: create and manage dialogue systems for NPCs and conversations.
    /// Provides declarative dialogue creation without custom scripts.
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

        protected override bool RequiresCompilationWait(string operation) => false;

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

            // Check if asset already exists
            if (AssetDatabase.LoadAssetAtPath<GameKitDialogueAsset>(assetPath) != null)
            {
                throw new InvalidOperationException($"Dialogue asset already exists at: {assetPath}");
            }

            // Create ScriptableObject
            var dialogue = ScriptableObject.CreateInstance<GameKitDialogueAsset>();
            dialogue.Initialize(dialogueId);

            // Set settings
            var serializedDialogue = new SerializedObject(dialogue);

            if (payload.TryGetValue("startNodeId", out var startNode))
            {
                serializedDialogue.FindProperty("startNodeId").stringValue = startNode.ToString();
            }

            if (payload.TryGetValue("autoAdvance", out var autoAdvance))
            {
                serializedDialogue.FindProperty("autoAdvance").boolValue = Convert.ToBoolean(autoAdvance);
            }

            if (payload.TryGetValue("autoAdvanceDelay", out var autoAdvanceDelay))
            {
                serializedDialogue.FindProperty("autoAdvanceDelay").floatValue = Convert.ToSingle(autoAdvanceDelay);
            }

            serializedDialogue.ApplyModifiedPropertiesWithoutUndo();

            // Add initial nodes if provided
            if (payload.TryGetValue("nodes", out var nodesObj) && nodesObj is List<object> nodesList)
            {
                foreach (var nodeObj in nodesList)
                {
                    if (nodeObj is Dictionary<string, object> nodeDict)
                    {
                        var node = ParseDialogueNode(nodeDict);
                        dialogue.AddNode(node);
                    }
                }
            }

            // Save asset
            AssetDatabase.CreateAsset(dialogue, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return CreateSuccessResponse(
                ("dialogueId", dialogueId),
                ("assetPath", assetPath),
                ("nodeCount", dialogue.Nodes.Count)
            );
        }

        private object UpdateDialogue(Dictionary<string, object> payload)
        {
            var dialogue = ResolveDialogueAsset(payload);
            var serializedDialogue = new SerializedObject(dialogue);

            if (payload.TryGetValue("startNodeId", out var startNode))
            {
                serializedDialogue.FindProperty("startNodeId").stringValue = startNode.ToString();
            }

            if (payload.TryGetValue("autoAdvance", out var autoAdvance))
            {
                serializedDialogue.FindProperty("autoAdvance").boolValue = Convert.ToBoolean(autoAdvance);
            }

            if (payload.TryGetValue("autoAdvanceDelay", out var autoAdvanceDelay))
            {
                serializedDialogue.FindProperty("autoAdvanceDelay").floatValue = Convert.ToSingle(autoAdvanceDelay);
            }

            serializedDialogue.ApplyModifiedProperties();
            EditorUtility.SetDirty(dialogue);
            AssetDatabase.SaveAssets();

            return CreateSuccessResponse(
                ("dialogueId", dialogue.DialogueId),
                ("updated", true)
            );
        }

        private object InspectDialogue(Dictionary<string, object> payload)
        {
            var dialogue = ResolveDialogueAsset(payload);

            var nodesInfo = new List<Dictionary<string, object>>();
            foreach (var node in dialogue.Nodes)
            {
                nodesInfo.Add(SerializeDialogueNode(node));
            }

            return CreateSuccessResponse(
                ("dialogueId", dialogue.DialogueId),
                ("assetPath", AssetDatabase.GetAssetPath(dialogue)),
                ("startNodeId", dialogue.StartNodeId),
                ("autoAdvance", dialogue.AutoAdvance),
                ("autoAdvanceDelay", dialogue.AutoAdvanceDelay),
                ("nodeCount", dialogue.Nodes.Count),
                ("nodes", nodesInfo)
            );
        }

        private object DeleteDialogue(Dictionary<string, object> payload)
        {
            var dialogue = ResolveDialogueAsset(payload);
            var assetPath = AssetDatabase.GetAssetPath(dialogue);
            var dialogueId = dialogue.DialogueId;

            AssetDatabase.DeleteAsset(assetPath);
            AssetDatabase.Refresh();

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

            var node = ParseDialogueNode(nodeDict);
            dialogue.AddNode(node);

            EditorUtility.SetDirty(dialogue);
            AssetDatabase.SaveAssets();

            return CreateSuccessResponse(
                ("dialogueId", dialogue.DialogueId),
                ("nodeId", node.nodeId),
                ("nodeCount", dialogue.Nodes.Count)
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

            var existingNode = dialogue.GetNode(nodeId);
            if (existingNode == null)
            {
                throw new InvalidOperationException($"Node '{nodeId}' not found in dialogue.");
            }

            if (!payload.TryGetValue("node", out var nodeObj) || nodeObj is not Dictionary<string, object> nodeDict)
            {
                throw new InvalidOperationException("node data is required for updateNode operation.");
            }

            // Preserve nodeId
            nodeDict["nodeId"] = nodeId;
            var updatedNode = ParseDialogueNode(nodeDict);
            dialogue.UpdateNode(updatedNode);

            EditorUtility.SetDirty(dialogue);
            AssetDatabase.SaveAssets();

            return CreateSuccessResponse(
                ("dialogueId", dialogue.DialogueId),
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

            var removed = dialogue.RemoveNode(nodeId);
            if (!removed)
            {
                throw new InvalidOperationException($"Node '{nodeId}' not found in dialogue.");
            }

            EditorUtility.SetDirty(dialogue);
            AssetDatabase.SaveAssets();

            return CreateSuccessResponse(
                ("dialogueId", dialogue.DialogueId),
                ("nodeId", nodeId),
                ("removed", true),
                ("nodeCount", dialogue.Nodes.Count)
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

            var node = dialogue.GetNode(nodeId);
            if (node == null)
            {
                throw new InvalidOperationException($"Node '{nodeId}' not found in dialogue.");
            }

            if (!payload.TryGetValue("choice", out var choiceObj) || choiceObj is not Dictionary<string, object> choiceDict)
            {
                throw new InvalidOperationException("choice data is required for addChoice operation.");
            }

            var choice = ParseDialogueChoice(choiceDict);
            node.choices.Add(choice);

            EditorUtility.SetDirty(dialogue);
            AssetDatabase.SaveAssets();

            return CreateSuccessResponse(
                ("dialogueId", dialogue.DialogueId),
                ("nodeId", nodeId),
                ("choiceId", choice.choiceId),
                ("choiceCount", node.choices.Count)
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

            var node = dialogue.GetNode(nodeId);
            if (node == null)
            {
                throw new InvalidOperationException($"Node '{nodeId}' not found in dialogue.");
            }

            var choiceIndex = node.choices.FindIndex(c => c.choiceId == choiceId);
            if (choiceIndex < 0)
            {
                throw new InvalidOperationException($"Choice '{choiceId}' not found in node '{nodeId}'.");
            }

            if (!payload.TryGetValue("choice", out var choiceObj) || choiceObj is not Dictionary<string, object> choiceDict)
            {
                throw new InvalidOperationException("choice data is required for updateChoice operation.");
            }

            choiceDict["choiceId"] = choiceId;
            var updatedChoice = ParseDialogueChoice(choiceDict);
            node.choices[choiceIndex] = updatedChoice;

            EditorUtility.SetDirty(dialogue);
            AssetDatabase.SaveAssets();

            return CreateSuccessResponse(
                ("dialogueId", dialogue.DialogueId),
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

            var node = dialogue.GetNode(nodeId);
            if (node == null)
            {
                throw new InvalidOperationException($"Node '{nodeId}' not found in dialogue.");
            }

            var removed = node.choices.RemoveAll(c => c.choiceId == choiceId) > 0;
            if (!removed)
            {
                throw new InvalidOperationException($"Choice '{choiceId}' not found in node '{nodeId}'.");
            }

            EditorUtility.SetDirty(dialogue);
            AssetDatabase.SaveAssets();

            return CreateSuccessResponse(
                ("dialogueId", dialogue.DialogueId),
                ("nodeId", nodeId),
                ("choiceId", choiceId),
                ("removed", true),
                ("choiceCount", node.choices.Count)
            );
        }

        #endregion

        #region Runtime Operations

        private object StartDialogue(Dictionary<string, object> payload)
        {
            var manager = GetDialogueManager();
            var dialogueId = GetString(payload, "dialogueId");
            var speakerPath = GetString(payload, "speakerPath");

            if (string.IsNullOrEmpty(dialogueId))
            {
                throw new InvalidOperationException("dialogueId is required for startDialogue operation.");
            }

            var dialogue = ResolveDialogueAsset(payload);
            var speaker = string.IsNullOrEmpty(speakerPath) ? null : ResolveGameObject(speakerPath);

            // Register dialogue
            GameKitDialogueManager.RegisterDialogue(dialogue);

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

            var existingManager = targetGo.GetComponent<GameKitDialogueManager>();
            if (existingManager != null)
            {
                throw new InvalidOperationException("GameObject already has a GameKitDialogueManager component.");
            }

            var manager = Undo.AddComponent<GameKitDialogueManager>(targetGo);

            var serializedManager = new SerializedObject(manager);

            if (payload.TryGetValue("pauseGameDuringDialogue", out var pauseObj))
            {
                serializedManager.FindProperty("pauseGameDuringDialogue").boolValue = Convert.ToBoolean(pauseObj);
            }

            serializedManager.ApplyModifiedProperties();

            return CreateSuccessResponse(
                ("path", BuildGameObjectPath(targetGo)),
                ("created", true)
            );
        }

        private object InspectManager(Dictionary<string, object> payload)
        {
            var manager = GetDialogueManager();

            return CreateSuccessResponse(
                ("path", BuildGameObjectPath(manager.gameObject)),
                ("isDialogueActive", manager.IsDialogueActive),
                ("currentDialogue", manager.CurrentDialogue?.DialogueId),
                ("currentNode", manager.CurrentNode?.nodeId)
            );
        }

        private object DeleteManager(Dictionary<string, object> payload)
        {
            var manager = GetDialogueManager();
            var path = BuildGameObjectPath(manager.gameObject);

            Undo.DestroyObjectImmediate(manager);

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

            var guids = AssetDatabase.FindAssets("t:GameKitDialogueAsset");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var dialogue = AssetDatabase.LoadAssetAtPath<GameKitDialogueAsset>(path);
                if (dialogue != null && dialogue.DialogueId == dialogueId)
                {
                    return CreateSuccessResponse(
                        ("found", true),
                        ("dialogueId", dialogue.DialogueId),
                        ("assetPath", path),
                        ("nodeCount", dialogue.Nodes.Count)
                    );
                }
            }

            return CreateSuccessResponse(("found", false), ("dialogueId", dialogueId));
        }

        #endregion

        #region Helpers

        private GameKitDialogueAsset ResolveDialogueAsset(Dictionary<string, object> payload)
        {
            var dialogueId = GetString(payload, "dialogueId");
            var assetPath = GetString(payload, "assetPath");

            if (!string.IsNullOrEmpty(assetPath))
            {
                var dialogue = AssetDatabase.LoadAssetAtPath<GameKitDialogueAsset>(assetPath);
                if (dialogue != null) return dialogue;
            }

            if (!string.IsNullOrEmpty(dialogueId))
            {
                var guids = AssetDatabase.FindAssets("t:GameKitDialogueAsset");
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var dialogue = AssetDatabase.LoadAssetAtPath<GameKitDialogueAsset>(path);
                    if (dialogue != null && dialogue.DialogueId == dialogueId)
                    {
                        return dialogue;
                    }
                }
            }

            throw new InvalidOperationException("Either assetPath or dialogueId is required to resolve dialogue asset.");
        }

        private GameKitDialogueManager GetDialogueManager()
        {
            var manager = UnityEngine.Object.FindAnyObjectByType<GameKitDialogueManager>();
            if (manager == null)
            {
                throw new InvalidOperationException("No GameKitDialogueManager found in scene. Create one first.");
            }
            return manager;
        }

        private GameKitDialogueAsset.DialogueNode ParseDialogueNode(Dictionary<string, object> dict)
        {
            var node = new GameKitDialogueAsset.DialogueNode
            {
                nodeId = dict.TryGetValue("nodeId", out var id) ? id?.ToString() : $"node_{Guid.NewGuid().ToString().Substring(0, 8)}",
                speaker = dict.TryGetValue("speaker", out var speaker) ? speaker?.ToString() : "",
                text = dict.TryGetValue("text", out var text) ? text?.ToString() : "",
                portrait = dict.TryGetValue("portrait", out var portrait) ? portrait?.ToString() : "",
                nextNodeId = dict.TryGetValue("nextNode", out var next) ? next?.ToString() : "",
                voiceClipPath = dict.TryGetValue("voiceClipPath", out var voice) ? voice?.ToString() : "",
                soundEffectPath = dict.TryGetValue("soundEffectPath", out var sfx) ? sfx?.ToString() : ""
            };

            if (dict.TryGetValue("type", out var typeObj))
            {
                node.type = ParseNodeType(typeObj.ToString());
            }

            if (dict.TryGetValue("choices", out var choicesObj) && choicesObj is List<object> choicesList)
            {
                foreach (var choiceObj in choicesList)
                {
                    if (choiceObj is Dictionary<string, object> choiceDict)
                    {
                        node.choices.Add(ParseDialogueChoice(choiceDict));
                    }
                }
            }

            if (dict.TryGetValue("conditions", out var conditionsObj) && conditionsObj is List<object> conditionsList)
            {
                foreach (var condObj in conditionsList)
                {
                    if (condObj is Dictionary<string, object> condDict)
                    {
                        node.conditions.Add(ParseDialogueCondition(condDict));
                    }
                }
            }

            if (dict.TryGetValue("actions", out var actionsObj) && actionsObj is List<object> actionsList)
            {
                foreach (var actionObj in actionsList)
                {
                    if (actionObj is Dictionary<string, object> actionDict)
                    {
                        node.actions.Add(ParseDialogueAction(actionDict));
                    }
                }
            }

            return node;
        }

        private GameKitDialogueAsset.DialogueChoice ParseDialogueChoice(Dictionary<string, object> dict)
        {
            var choice = new GameKitDialogueAsset.DialogueChoice
            {
                choiceId = dict.TryGetValue("choiceId", out var id) ? id?.ToString() : $"choice_{Guid.NewGuid().ToString().Substring(0, 8)}",
                text = dict.TryGetValue("text", out var text) ? text?.ToString() : "",
                nextNodeId = dict.TryGetValue("nextNode", out var next) ? next?.ToString() : "",
                isDefault = dict.TryGetValue("isDefault", out var def) && Convert.ToBoolean(def)
            };

            if (dict.TryGetValue("conditions", out var conditionsObj) && conditionsObj is List<object> conditionsList)
            {
                foreach (var condObj in conditionsList)
                {
                    if (condObj is Dictionary<string, object> condDict)
                    {
                        choice.conditions.Add(ParseDialogueCondition(condDict));
                    }
                }
            }

            if (dict.TryGetValue("actions", out var actionsObj) && actionsObj is List<object> actionsList)
            {
                foreach (var actionObj in actionsList)
                {
                    if (actionObj is Dictionary<string, object> actionDict)
                    {
                        choice.actions.Add(ParseDialogueAction(actionDict));
                    }
                }
            }

            return choice;
        }

        private GameKitDialogueAsset.DialogueCondition ParseDialogueCondition(Dictionary<string, object> dict)
        {
            return new GameKitDialogueAsset.DialogueCondition
            {
                type = dict.TryGetValue("type", out var type) ? ParseConditionType(type.ToString()) : GameKitDialogueAsset.ConditionType.Variable,
                targetId = dict.TryGetValue("targetId", out var targetId) ? targetId?.ToString() : "",
                propertyName = dict.TryGetValue("propertyName", out var prop) ? prop?.ToString() : "",
                comparison = dict.TryGetValue("comparison", out var comp) ? ParseComparisonOperator(comp.ToString()) : GameKitDialogueAsset.ComparisonOperator.Equals,
                value = dict.TryGetValue("value", out var val) ? val?.ToString() : "",
                negate = dict.TryGetValue("negate", out var neg) && Convert.ToBoolean(neg)
            };
        }

        private GameKitDialogueAsset.DialogueAction ParseDialogueAction(Dictionary<string, object> dict)
        {
            return new GameKitDialogueAsset.DialogueAction
            {
                type = dict.TryGetValue("type", out var type) ? ParseActionType(type.ToString()) : GameKitDialogueAsset.ActionType.Custom,
                targetId = dict.TryGetValue("targetId", out var targetId) ? targetId?.ToString() : "",
                parameter = dict.TryGetValue("parameter", out var param) ? param?.ToString() : "",
                value = dict.TryGetValue("value", out var val) ? val?.ToString() : ""
            };
        }

        private GameKitDialogueAsset.NodeType ParseNodeType(string str)
        {
            return str?.ToLowerInvariant() switch
            {
                "dialogue" => GameKitDialogueAsset.NodeType.Dialogue,
                "choice" => GameKitDialogueAsset.NodeType.Choice,
                "branch" => GameKitDialogueAsset.NodeType.Branch,
                "action" => GameKitDialogueAsset.NodeType.Action,
                "exit" => GameKitDialogueAsset.NodeType.Exit,
                _ => GameKitDialogueAsset.NodeType.Dialogue
            };
        }

        private GameKitDialogueAsset.ConditionType ParseConditionType(string str)
        {
            return str?.ToLowerInvariant() switch
            {
                "quest" => GameKitDialogueAsset.ConditionType.Quest,
                "resource" => GameKitDialogueAsset.ConditionType.Resource,
                "inventory" => GameKitDialogueAsset.ConditionType.Inventory,
                "variable" => GameKitDialogueAsset.ConditionType.Variable,
                "health" => GameKitDialogueAsset.ConditionType.Health,
                "custom" => GameKitDialogueAsset.ConditionType.Custom,
                _ => GameKitDialogueAsset.ConditionType.Variable
            };
        }

        private GameKitDialogueAsset.ComparisonOperator ParseComparisonOperator(string str)
        {
            return str?.ToLowerInvariant() switch
            {
                "equals" or "==" or "eq" => GameKitDialogueAsset.ComparisonOperator.Equals,
                "notequals" or "!=" or "ne" => GameKitDialogueAsset.ComparisonOperator.NotEquals,
                "greaterthan" or ">" or "gt" => GameKitDialogueAsset.ComparisonOperator.GreaterThan,
                "lessthan" or "<" or "lt" => GameKitDialogueAsset.ComparisonOperator.LessThan,
                "greaterorequal" or ">=" or "gte" => GameKitDialogueAsset.ComparisonOperator.GreaterOrEqual,
                "lessorequal" or "<=" or "lte" => GameKitDialogueAsset.ComparisonOperator.LessOrEqual,
                "contains" => GameKitDialogueAsset.ComparisonOperator.Contains,
                _ => GameKitDialogueAsset.ComparisonOperator.Equals
            };
        }

        private GameKitDialogueAsset.ActionType ParseActionType(string str)
        {
            return str?.ToLowerInvariant() switch
            {
                "startquest" => GameKitDialogueAsset.ActionType.StartQuest,
                "completequest" => GameKitDialogueAsset.ActionType.CompleteQuest,
                "addresource" => GameKitDialogueAsset.ActionType.AddResource,
                "removeresource" => GameKitDialogueAsset.ActionType.RemoveResource,
                "additem" => GameKitDialogueAsset.ActionType.AddItem,
                "removeitem" => GameKitDialogueAsset.ActionType.RemoveItem,
                "setvariable" => GameKitDialogueAsset.ActionType.SetVariable,
                "playeffect" => GameKitDialogueAsset.ActionType.PlayEffect,
                "openshop" => GameKitDialogueAsset.ActionType.OpenShop,
                "teleport" => GameKitDialogueAsset.ActionType.Teleport,
                "custom" => GameKitDialogueAsset.ActionType.Custom,
                _ => GameKitDialogueAsset.ActionType.Custom
            };
        }

        private Dictionary<string, object> SerializeDialogueNode(GameKitDialogueAsset.DialogueNode node)
        {
            var dict = new Dictionary<string, object>
            {
                { "nodeId", node.nodeId },
                { "type", node.type.ToString() },
                { "speaker", node.speaker },
                { "text", node.text },
                { "portrait", node.portrait },
                { "nextNode", node.nextNodeId }
            };

            if (node.choices.Count > 0)
            {
                var choices = new List<Dictionary<string, object>>();
                foreach (var choice in node.choices)
                {
                    choices.Add(new Dictionary<string, object>
                    {
                        { "choiceId", choice.choiceId },
                        { "text", choice.text },
                        { "nextNode", choice.nextNodeId },
                        { "isDefault", choice.isDefault }
                    });
                }
                dict["choices"] = choices;
            }

            return dict;
        }

        #endregion
    }
}
