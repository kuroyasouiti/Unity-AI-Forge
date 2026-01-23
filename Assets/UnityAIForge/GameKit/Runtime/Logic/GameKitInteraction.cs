using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

namespace UnityAIForge.GameKit
{
    /// <summary>
    /// GameKit Interaction Hub: bridges game events (triggers) to actions across GameKit components.
    /// Supports traditional triggers (collision, input) and specialized triggers (tilemap, graph, spline).
    /// </summary>
    [AddComponentMenu("SkillForUnity/GameKit/Interaction Hub")]
    public class GameKitInteraction : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string interactionId;
        
        [Header("Trigger Settings")]
        [SerializeField] private TriggerType triggerType;
        
        [Header("Trigger Parameters")]
        [Tooltip("For TilemapCell: cell coordinates (x,y). For SplineProgress: 0-1 value.")]
        [SerializeField] private Vector2Int tilemapCell;
        
        [Tooltip("For SplineProgress: progress value 0-1")]
        [SerializeField] private float splineProgress = 0.5f;
        
        [Tooltip("For GraphNode: ID of target node")]
        [SerializeField] private string targetGraphNodeId;
        
        [Tooltip("For Proximity: detection radius")]
        [SerializeField] private float proximityRadius = 3f;
        
        [Tooltip("For Input: key to press")]
        [SerializeField] private KeyCode inputKey = KeyCode.E;
        
        [Header("Actions")]
        [SerializeField] private List<InteractionAction> actions = new List<InteractionAction>();
        
        [Header("Conditions")]
        [SerializeField] private List<InteractionCondition> conditions = new List<InteractionCondition>();
        
        [Header("Events")]
        [Tooltip("Invoked when interaction is triggered")]
        public UnityEvent<GameObject> OnInteractionTriggered = new UnityEvent<GameObject>();
        
        [Header("Settings")]
        [Tooltip("Log interaction execution for debugging")]
        [SerializeField] private bool logInteractions = false;
        
        [Tooltip("Allow repeated triggering")]
        [SerializeField] private bool allowRepeatedTrigger = true;
        
        [Tooltip("Cooldown between triggers (seconds)")]
        [SerializeField] private float triggerCooldown = 0f;

        private bool hasTriggered = false;
        private float lastTriggerTime = -Infinity;
        private Component monitoredTileMovement;
        private Component monitoredGraphMovement;
        private Component monitoredSplineMovement;

        public string InteractionId => interactionId;
        public TriggerType Trigger => triggerType;

        private const float Infinity = float.PositiveInfinity;

        private void Awake()
        {
            OnInteractionTriggered ??= new UnityEvent<GameObject>();
        }

        private void Start()
        {
            SetupSpecializedTriggers();
        }

        private void SetupSpecializedTriggers()
        {
            // Setup Tilemap monitoring
            if (triggerType == TriggerType.TilemapCell)
            {
                var tileType = System.Type.GetType("UnityAIForge.GameKit.TileGridMovement, UnityAIForge.GameKit.Runtime");
                if (tileType != null)
                {
                    monitoredTileMovement = GetComponent(tileType);
                    if (monitoredTileMovement == null)
                    {
                        Debug.LogWarning($"[GameKitInteraction] TilemapCell trigger requires TileGridMovement component on {gameObject.name}");
                    }
                }
            }

            // Setup Graph node monitoring
            if (triggerType == TriggerType.GraphNode)
            {
                var graphType = System.Type.GetType("UnityAIForge.GameKit.GraphNodeMovement, UnityAIForge.GameKit.Runtime");
                if (graphType != null)
                {
                    monitoredGraphMovement = GetComponent(graphType);
                    if (monitoredGraphMovement == null)
                    {
                        Debug.LogWarning($"[GameKitInteraction] GraphNode trigger requires GraphNodeMovement component on {gameObject.name}");
                    }
                }
            }

            // Setup Spline progress monitoring
            if (triggerType == TriggerType.SplineProgress)
            {
                var splineType = System.Type.GetType("UnityAIForge.GameKit.SplineMovement, UnityAIForge.GameKit.Runtime");
                if (splineType != null)
                {
                    monitoredSplineMovement = GetComponent(splineType);
                    if (monitoredSplineMovement == null)
                    {
                        Debug.LogWarning($"[GameKitInteraction] SplineProgress trigger requires SplineMovement component on {gameObject.name}");
                    }
                }
            }
        }

        public void Initialize(string id, TriggerType trigger)
        {
            interactionId = id;
            triggerType = trigger;
            OnInteractionTriggered ??= new UnityEvent<GameObject>();
        }

        public void AddAction(ActionType type, string target, string parameter)
        {
            actions.Add(new InteractionAction
            {
                type = type,
                target = target,
                parameter = parameter
            });
        }

        public void AddCondition(ConditionType type, string value)
        {
            conditions.Add(new InteractionCondition
            {
                type = type,
                value = value
            });
        }

        /// <summary>
        /// Manually trigger this interaction (useful for script-triggered interactions).
        /// </summary>
        public void ManualTrigger(GameObject triggeringObject = null)
        {
            if (!CanTrigger())
                return;

            if (EvaluateConditions(triggeringObject ?? gameObject))
            {
                ExecuteActions(triggeringObject ?? gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (triggerType == TriggerType.Trigger && CanTrigger() && EvaluateConditions(other.gameObject))
            {
                ExecuteActions(other.gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (triggerType == TriggerType.Trigger && CanTrigger() && EvaluateConditions(other.gameObject))
            {
                ExecuteActions(other.gameObject);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (triggerType == TriggerType.Collision && CanTrigger() && EvaluateConditions(collision.gameObject))
            {
                ExecuteActions(collision.gameObject);
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (triggerType == TriggerType.Collision && CanTrigger() && EvaluateConditions(collision.gameObject))
            {
                ExecuteActions(collision.gameObject);
            }
        }

        private void Update()
        {
            // Input trigger
            if (triggerType == TriggerType.Input && Input.GetKeyDown(inputKey))
            {
                if (CanTrigger() && EvaluateConditions(gameObject))
                {
                    ExecuteActions(gameObject);
                }
            }

            // Proximity trigger
            if (triggerType == TriggerType.Proximity)
            {
                CheckProximityTrigger();
            }

            // Tilemap cell trigger
            if (triggerType == TriggerType.TilemapCell && monitoredTileMovement != null)
            {
                CheckTilemapCellTrigger();
            }

            // Graph node trigger
            if (triggerType == TriggerType.GraphNode && monitoredGraphMovement != null)
            {
                CheckGraphNodeTrigger();
            }

            // Spline progress trigger
            if (triggerType == TriggerType.SplineProgress && monitoredSplineMovement != null)
            {
                CheckSplineProgressTrigger();
            }
        }

        private bool CanTrigger()
        {
            if (!allowRepeatedTrigger && hasTriggered)
                return false;

            if (Time.time - lastTriggerTime < triggerCooldown)
                return false;

            return true;
        }

        private void CheckProximityTrigger()
        {
            if (!CanTrigger())
                return;

            var actors = FindObjectsByType<GameKitActor>(FindObjectsSortMode.None);
            foreach (var actor in actors)
            {
                if (Vector3.Distance(transform.position, actor.transform.position) <= proximityRadius)
                {
                    if (EvaluateConditions(actor.gameObject))
                    {
                        ExecuteActions(actor.gameObject);
                        break;
                    }
                }
            }
        }

        private void CheckTilemapCellTrigger()
        {
            if (!CanTrigger() || monitoredTileMovement == null)
                return;

            // Use reflection to get GridPosition property
            var gridPosProperty = monitoredTileMovement.GetType().GetProperty("GridPosition");
            if (gridPosProperty != null)
            {
                var currentCell = (Vector2Int)gridPosProperty.GetValue(monitoredTileMovement);
                if (currentCell == tilemapCell)
                {
                    if (EvaluateConditions(gameObject))
                    {
                        if (logInteractions)
                            Debug.Log($"[GameKitInteraction] Tilemap cell trigger at {tilemapCell}");
                        ExecuteActions(gameObject);
                    }
                }
            }
        }

        private void CheckGraphNodeTrigger()
        {
            if (!CanTrigger() || monitoredGraphMovement == null || string.IsNullOrEmpty(targetGraphNodeId))
                return;

            // Use reflection to get CurrentNode property
            var currentNodeProperty = monitoredGraphMovement.GetType().GetProperty("CurrentNode");
            if (currentNodeProperty != null)
            {
                var currentNode = currentNodeProperty.GetValue(monitoredGraphMovement);
                if (currentNode != null)
                {
                    var nodeIdProperty = currentNode.GetType().GetProperty("NodeId");
                    if (nodeIdProperty != null)
                    {
                        var currentNodeId = (string)nodeIdProperty.GetValue(currentNode);
                        if (currentNodeId == targetGraphNodeId)
                        {
                            if (EvaluateConditions(gameObject))
                            {
                                if (logInteractions)
                                    Debug.Log($"[GameKitInteraction] Graph node trigger at {targetGraphNodeId}");
                                ExecuteActions(gameObject);
                            }
                        }
                    }
                }
            }
        }

        private void CheckSplineProgressTrigger()
        {
            if (!CanTrigger() || monitoredSplineMovement == null)
                return;

            // Use reflection to get Progress property
            var progressProperty = monitoredSplineMovement.GetType().GetProperty("Progress");
            if (progressProperty != null)
            {
                float currentProgress = (float)progressProperty.GetValue(monitoredSplineMovement);
                float threshold = 0.05f; // 5% threshold for triggering

                if (Mathf.Abs(currentProgress - splineProgress) < threshold)
                {
                    if (EvaluateConditions(gameObject))
                    {
                        if (logInteractions)
                            Debug.Log($"[GameKitInteraction] Spline progress trigger at {splineProgress:F2}");
                        ExecuteActions(gameObject);
                    }
                }
            }
        }

        private bool EvaluateConditions(GameObject other)
        {
            if (conditions.Count == 0) return true;

            foreach (var condition in conditions)
            {
                switch (condition.type)
                {
                    case ConditionType.Tag:
                        if (other != null && !other.CompareTag(condition.value))
                            return false;
                        break;
                    
                    case ConditionType.Layer:
                        if (other != null && other.layer != LayerMask.NameToLayer(condition.value))
                            return false;
                        break;
                    
                    case ConditionType.Distance:
                        if (other != null && float.TryParse(condition.value, out float maxDistance))
                        {
                            if (Vector3.Distance(transform.position, other.transform.position) > maxDistance)
                                return false;
                        }
                        break;

                    case ConditionType.ActorId:
                        if (other != null)
                        {
                            var actor = other.GetComponent<GameKitActor>();
                            if (actor == null || actor.ActorId != condition.value)
                                return false;
                        }
                        break;

                    case ConditionType.ManagerResource:
                        var manager = FindFirstObjectByType<GameKitManager>();
                        if (manager != null)
                        {
                            var parts = condition.value.Split(':');
                            if (parts.Length == 2 && float.TryParse(parts[1], out float minAmount))
                            {
                                if (manager.GetResource(parts[0]) < minAmount)
                                    return false;
                            }
                        }
                        break;
                }
            }

            return true;
        }

        private void ExecuteActions(GameObject triggeringObject)
        {
            // Mark as triggered
            hasTriggered = true;
            lastTriggerTime = Time.time;

            // Invoke UnityEvent
            OnInteractionTriggered?.Invoke(triggeringObject);

            if (logInteractions)
                Debug.Log($"[GameKitInteraction] '{interactionId}' triggered by {(triggeringObject ? triggeringObject.name : "self")}");

            // Execute all actions
            foreach (var action in actions)
            {
                switch (action.type)
                {
                    case ActionType.SpawnPrefab:
                        SpawnPrefab(action.target, action.parameter);
                        break;
                    
                    case ActionType.DestroyObject:
                        DestroyObject(action.target, triggeringObject);
                        break;
                    
                    case ActionType.PlaySound:
                        PlaySound(action.target);
                        break;
                    
                    case ActionType.SendMessage:
                        SendMessageToTarget(action.target, action.parameter, triggeringObject);
                        break;
                    
                    case ActionType.ChangeScene:
                        ChangeScene(action.target);
                        break;

                    case ActionType.TriggerActorAction:
                        TriggerActorAction(action.target, action.parameter);
                        break;

                    case ActionType.UpdateManagerResource:
                        UpdateManagerResource(action.target, action.parameter);
                        break;

                    case ActionType.TriggerSceneFlow:
                        TriggerSceneFlow(action.target);
                        break;

                    case ActionType.TeleportToTile:
                        TeleportToTile(action.target, action.parameter);
                        break;

                    case ActionType.MoveToGraphNode:
                        MoveToGraphNode(action.target);
                        break;

                    case ActionType.SetSplineProgress:
                        SetSplineProgress(action.target, action.parameter);
                        break;
                }
            }
        }

        private void SpawnPrefab(string prefabPath, string positionStr)
        {
            var prefab = UnityEngine.Resources.Load<GameObject>(prefabPath);
            if (prefab != null)
            {
                Vector3 position = transform.position;
                if (!string.IsNullOrEmpty(positionStr))
                {
                    var parts = positionStr.Split(',');
                    if (parts.Length == 3 &&
                        float.TryParse(parts[0], out float x) &&
                        float.TryParse(parts[1], out float y) &&
                        float.TryParse(parts[2], out float z))
                    {
                        position = new Vector3(x, y, z);
                    }
                }
                Instantiate(prefab, position, Quaternion.identity);
                Debug.Log($"[GameKitInteraction] Spawned prefab: {prefabPath}");
            }
        }

        private void DestroyObject(string targetPath, GameObject triggeringObject)
        {
            if (targetPath == "self")
            {
                Destroy(gameObject);
                if (logInteractions)
                    Debug.Log($"[GameKitInteraction] Destroyed self");
            }
            else if (targetPath == "other")
            {
                if (triggeringObject != null)
                {
                    Destroy(triggeringObject);
                    if (logInteractions)
                        Debug.Log($"[GameKitInteraction] Destroyed triggering object: {triggeringObject.name}");
                }
            }
            else
            {
                var target = GameObject.Find(targetPath);
                if (target != null)
                {
                    Destroy(target);
                    if (logInteractions)
                        Debug.Log($"[GameKitInteraction] Destroyed object: {targetPath}");
                }
            }
        }

        private void PlaySound(string audioClipPath)
        {
            var clip = UnityEngine.Resources.Load<AudioClip>(audioClipPath);
            if (clip != null)
            {
                AudioSource.PlayClipAtPoint(clip, transform.position);
                if (logInteractions)
                    Debug.Log($"[GameKitInteraction] Played sound: {audioClipPath}");
            }
        }

        private void SendMessageToTarget(string targetPath, string message, GameObject triggeringObject)
        {
            GameObject target = null;
            
            if (targetPath == "self")
            {
                target = gameObject;
            }
            else if (targetPath == "other")
            {
                target = triggeringObject;
            }
            else
            {
                target = GameObject.Find(targetPath);
            }

            if (target != null)
            {
                target.SendMessage(message, SendMessageOptions.DontRequireReceiver);
                if (logInteractions)
                    Debug.Log($"[GameKitInteraction] Sent message '{message}' to {target.name}");
            }
        }

        private void ChangeScene(string sceneName)
        {
            if (logInteractions)
                Debug.Log($"[GameKitInteraction] Loading scene: {sceneName}");
            SceneManager.LoadScene(sceneName);
        }

        private void TriggerActorAction(string actorId, string actionName)
        {
            var actors = FindObjectsByType<GameKitActor>(FindObjectsSortMode.None);
            foreach (var actor in actors)
            {
                if (actor.ActorId == actorId)
                {
                    actor.SendActionInput(actionName);
                    if (logInteractions)
                        Debug.Log($"[GameKitInteraction] Sent action '{actionName}' to actor '{actorId}'");
                    return;
                }
            }
            Debug.LogWarning($"[GameKitInteraction] Actor '{actorId}' not found");
        }

        private void UpdateManagerResource(string resourceName, string changeStr)
        {
            var manager = FindFirstObjectByType<GameKitManager>();
            if (manager == null)
            {
                Debug.LogWarning($"[GameKitInteraction] GameKitManager not found");
                return;
            }

            if (float.TryParse(changeStr, out float change))
            {
                manager.AddResource(resourceName, change);
                if (logInteractions)
                    Debug.Log($"[GameKitInteraction] Updated resource '{resourceName}' by {change}");
            }
        }

        private void TriggerSceneFlow(string triggerName)
        {
            GameKitSceneFlow.Transition(triggerName);
            if (logInteractions)
                Debug.Log($"[GameKitInteraction] Triggered scene flow: {triggerName}");
        }

        private void TeleportToTile(string actorIdOrSelf, string coordinatesStr)
        {
            GameObject targetObj = GetTargetObject(actorIdOrSelf);
            if (targetObj == null)
                return;

            var tileType = System.Type.GetType("UnityAIForge.GameKit.TileGridMovement, UnityAIForge.GameKit.Runtime");
            if (tileType != null)
            {
                var tileMovement = targetObj.GetComponent(tileType);
                if (tileMovement != null)
                {
                    var parts = coordinatesStr.Split(',');
                    if (parts.Length == 2 && 
                        int.TryParse(parts[0], out int x) && 
                        int.TryParse(parts[1], out int y))
                    {
                        var teleportMethod = tileType.GetMethod("TeleportToGrid");
                        if (teleportMethod != null)
                        {
                            teleportMethod.Invoke(tileMovement, new object[] { new Vector2Int(x, y) });
                            if (logInteractions)
                                Debug.Log($"[GameKitInteraction] Teleported to tile ({x},{y})");
                        }
                    }
                }
            }
        }

        private void MoveToGraphNode(string nodeId)
        {
            var graphType = System.Type.GetType("UnityAIForge.GameKit.GraphNodeMovement, UnityAIForge.GameKit.Runtime");
            if (graphType == null)
                return;

            var movement = GetComponent(graphType);
            if (movement == null)
            {
                Debug.LogWarning($"[GameKitInteraction] GraphNodeMovement not found on {gameObject.name}");
                return;
            }

            // Find node by ID using reflection
            var graphNodeType = System.Type.GetType("UnityAIForge.GameKit.GraphNode, UnityAIForge.GameKit.Runtime");
            if (graphNodeType != null)
            {
                var nodes = FindObjectsByType(graphNodeType, FindObjectsSortMode.None);
                foreach (var node in nodes)
                {
                    var nodeIdProperty = graphNodeType.GetProperty("NodeId");
                    if (nodeIdProperty != null)
                    {
                        var currentNodeId = (string)nodeIdProperty.GetValue(node);
                        if (currentNodeId == nodeId)
                        {
                            var moveToNodeMethod = graphType.GetMethod("MoveToNode");
                            if (moveToNodeMethod != null)
                            {
                                moveToNodeMethod.Invoke(movement, new object[] { node });
                                if (logInteractions)
                                    Debug.Log($"[GameKitInteraction] Moving to graph node: {nodeId}");
                            }
                            return;
                        }
                    }
                }
            }
            Debug.LogWarning($"[GameKitInteraction] Graph node '{nodeId}' not found");
        }

        private void SetSplineProgress(string actorIdOrSelf, string progressStr)
        {
            GameObject targetObj = GetTargetObject(actorIdOrSelf);
            if (targetObj == null)
                return;

            var splineType = System.Type.GetType("UnityAIForge.GameKit.SplineMovement, UnityAIForge.GameKit.Runtime");
            if (splineType != null)
            {
                var splineMovement = targetObj.GetComponent(splineType);
                if (splineMovement != null && float.TryParse(progressStr, out float progress))
                {
                    var setProgressMethod = splineType.GetMethod("SetProgress");
                    if (setProgressMethod != null)
                    {
                        setProgressMethod.Invoke(splineMovement, new object[] { progress });
                        if (logInteractions)
                            Debug.Log($"[GameKitInteraction] Set spline progress to {progress}");
                    }
                }
            }
        }

        private GameObject GetTargetObject(string targetId)
        {
            if (targetId == "self")
            {
                return gameObject;
            }
            else
            {
                var actors = FindObjectsByType<GameKitActor>(FindObjectsSortMode.None);
                foreach (var actor in actors)
                {
                    if (actor.ActorId == targetId)
                    {
                        return actor.gameObject;
                    }
                }
            }
            return null;
        }

        private void OnDrawGizmosSelected()
        {
            // Draw proximity radius
            if (triggerType == TriggerType.Proximity)
            {
                Gizmos.color = new Color(0, 1, 0, 0.3f);
                Gizmos.DrawWireSphere(transform.position, proximityRadius);
            }

            // Draw tilemap cell indicator
            if (triggerType == TriggerType.TilemapCell && monitoredTileMovement != null)
            {
                Gizmos.color = Color.cyan;
                var getWorldPosMethod = monitoredTileMovement.GetType().GetMethod("GetWorldPosition");
                if (getWorldPosMethod != null)
                {
                    var worldPos = (Vector3)getWorldPosMethod.Invoke(monitoredTileMovement, new object[] { tilemapCell });
                    float gridSize = 1f;
                    Gizmos.DrawWireCube(worldPos, new Vector3(gridSize, gridSize, 0.1f));
                }
            }
        }

        [Serializable]
        public class InteractionAction
        {
            [Tooltip("Type of action to execute")]
            public ActionType type;
            
            [Tooltip("Target identifier (actor ID, object name, resource name, etc.)")]
            public string target;
            
            [Tooltip("Additional parameter (position, amount, node ID, etc.)")]
            public string parameter;
        }

        [Serializable]
        public class InteractionCondition
        {
            [Tooltip("Type of condition to check")]
            public ConditionType type;
            
            [Tooltip("Condition value (tag name, layer name, distance, etc.)")]
            public string value;
        }

        /// <summary>
        /// Types of triggers that can activate interactions.
        /// </summary>
        public enum TriggerType
        {
            Collision,          // 3D/2D collision enter
            Trigger,            // 3D/2D trigger enter
            Raycast,            // Raycast hit detection
            Proximity,          // Distance-based detection
            Input,              // Key press
            TilemapCell,        // Specific tilemap cell reached
            GraphNode,          // Specific graph node reached
            SplineProgress      // Specific progress on spline reached
        }

        /// <summary>
        /// Types of actions that can be executed.
        /// </summary>
        public enum ActionType
        {
            SpawnPrefab,            // Instantiate prefab
            DestroyObject,          // Destroy GameObject
            PlaySound,              // Play audio clip
            SendMessage,            // Send Unity message
            ChangeScene,            // Load scene (legacy)
            TriggerActorAction,     // Send action to GameKitActor
            UpdateManagerResource,  // Modify GameKitManager resource
            TriggerSceneFlow,       // Trigger GameKitSceneFlow transition
            TeleportToTile,         // Teleport to tilemap cell
            MoveToGraphNode,        // Move to graph node
            SetSplineProgress       // Set spline progress
        }

        /// <summary>
        /// Types of conditions that must be met.
        /// </summary>
        public enum ConditionType
        {
            Tag,                // GameObject tag match
            Layer,              // GameObject layer match
            Distance,           // Distance threshold
            ActorId,            // GameKitActor ID match
            ManagerResource,    // GameKitManager resource check (format: "resourceName:minAmount")
            Custom              // Custom condition (for scripting)
        }
    }
}

