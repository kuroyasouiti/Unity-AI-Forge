using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UnityAIForge.GameKit
{
    /// <summary>
    /// Node-based graph movement component for games with discrete movement spaces.
    /// Works in both 2D and 3D by treating positions as abstract nodes in a graph.
    /// Ideal for board games, tactical RPGs, puzzle games, and adventure games.
    /// Implements IMovementStrategy for unified movement handling.
    /// </summary>
    [RequireComponent(typeof(GameKitActor))]
    public class GraphNodeMovement : MonoBehaviour, IMovementStrategy
    {
        [Header("Movement Settings")]
        [Tooltip("Time to move from one node to another")]
        [SerializeField] private float moveSpeed = 0.5f;
        
        [Tooltip("Use smooth interpolation between nodes")]
        [SerializeField] private bool smoothMovement = true;
        
        [Tooltip("Animation curve for movement")]
        [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Header("Pathfinding Settings")]
        [Tooltip("Maximum path length for pathfinding (0 = unlimited)")]
        [SerializeField] private int maxPathLength = 50;
        
        [Tooltip("Cost of diagonal movement (set to 1.0 for grid-like movement)")]
        [SerializeField] private float diagonalCost = 1.414f; // sqrt(2)

        [Header("Visualization")]
        [Tooltip("Show debug visualization of nodes and connections")]
        [SerializeField] private bool showDebugVisualization = false;
        
        [Tooltip("Color for node visualization")]
        [SerializeField] private Color nodeColor = Color.green;
        
        [Tooltip("Color for connection visualization")]
        [SerializeField] private Color connectionColor = Color.cyan;

        private GameKitActor actor;
        private GraphNode currentNode;
        private GraphNode targetNode;
        private List<GraphNode> currentPath;
        private int currentPathIndex;
        private bool isMoving = false;
        private Coroutine moveCoroutine;

        // Public properties
        public GraphNode CurrentNode => currentNode;
        public bool IsMoving => isMoving;
        public List<GraphNode> CurrentPath => currentPath;

        /// <summary>
        /// Movement speed (time to move between nodes).
        /// IMovementStrategy implementation.
        /// </summary>
        float IMovementStrategy.MoveSpeed
        {
            get => moveSpeed;
            set => moveSpeed = value;
        }

        /// <summary>
        /// Public getter for movement speed.
        /// </summary>
        public float MoveSpeedValue => moveSpeed;

        private void Awake()
        {
            actor = GetComponent<GameKitActor>();
            
            // Subscribe to actor input events early for editor tests
            if (actor != null && actor.OnMoveInput != null)
            {
                actor.OnMoveInput.AddListener(HandleMoveInput);
            }
        }

        private void Start()
        {
            // Find nearest node at start
            SnapToNearestNode();
            
            // Ensure subscription (defensive programming)
            if (actor != null && actor.OnMoveInput != null)
            {
                actor.OnMoveInput.RemoveListener(HandleMoveInput);
                actor.OnMoveInput.AddListener(HandleMoveInput);
            }
        }

        private void OnDestroy()
        {
            if (actor != null && actor.OnMoveInput != null)
            {
                actor.OnMoveInput.RemoveListener(HandleMoveInput);
            }
        }

        /// <summary>
        /// Handles move input from the actor hub.
        /// Input direction is used to select adjacent node or trigger pathfinding.
        /// IMovementStrategy implementation.
        /// </summary>
        public void HandleMoveInput(Vector3 direction)
        {
            if (currentNode == null || direction.magnitude < 0.1f)
                return;

            if (isMoving)
                return; // Ignore input while moving

            // Find best matching adjacent node based on direction
            GraphNode targetNode = FindBestAdjacentNode(direction);
            if (targetNode != null)
            {
                MoveToNode(targetNode);
            }
        }

        /// <summary>
        /// Moves to a specific node (direct movement, no pathfinding).
        /// </summary>
        public bool MoveToNode(GraphNode node)
        {
            if (node == null || isMoving)
                return false;

            // Check if node is connected to current node
            if (currentNode != null && !currentNode.IsConnectedTo(node))
                return false;

            targetNode = node;
            
            if (smoothMovement)
            {
                if (moveCoroutine != null)
                    StopCoroutine(moveCoroutine);
                moveCoroutine = StartCoroutine(SmoothMoveToNode(node));
            }
            else
            {
                transform.position = node.Position;
                currentNode = node;
            }

            return true;
        }

        /// <summary>
        /// Moves to a target node using A* pathfinding.
        /// </summary>
        public bool MoveToNodeWithPathfinding(GraphNode target)
        {
            if (target == null || currentNode == null || isMoving)
                return false;

            List<GraphNode> path = FindPath(currentNode, target);
            if (path == null || path.Count == 0)
                return false;

            currentPath = path;
            currentPathIndex = 0;
            
            if (smoothMovement)
            {
                if (moveCoroutine != null)
                    StopCoroutine(moveCoroutine);
                moveCoroutine = StartCoroutine(FollowPath());
            }
            else
            {
                // Instant movement along path
                foreach (var node in path)
                {
                    currentNode = node;
                }
                transform.position = target.Position;
            }

            return true;
        }

        /// <summary>
        /// Snaps to the nearest node in the scene.
        /// </summary>
        public void SnapToNearestNode()
        {
            var allNodes = FindObjectsByType<GraphNode>(FindObjectsSortMode.None);
            if (allNodes.Length == 0)
            {
                Debug.LogWarning($"[GraphNodeMovement] No GraphNodes found in scene for {gameObject.name}");
                return;
            }

            GraphNode nearest = null;
            float nearestDist = float.MaxValue;

            foreach (var node in allNodes)
            {
                float dist = Vector3.Distance(transform.position, node.Position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = node;
                }
            }

            if (nearest != null)
            {
                currentNode = nearest;
                transform.position = nearest.Position;
            }
        }

        /// <summary>
        /// Teleports to a specific node instantly.
        /// </summary>
        public void TeleportToNode(GraphNode node)
        {
            if (node == null)
                return;

            if (moveCoroutine != null)
            {
                StopCoroutine(moveCoroutine);
                moveCoroutine = null;
            }

            currentNode = node;
            transform.position = node.Position;
            isMoving = false;
        }

        /// <summary>
        /// Gets all nodes reachable from current node within a certain distance.
        /// </summary>
        public List<GraphNode> GetReachableNodes(int maxDistance)
        {
            if (currentNode == null)
                return new List<GraphNode>();

            var reachable = new List<GraphNode>();
            var visited = new HashSet<GraphNode>();
            var queue = new Queue<(GraphNode node, int distance)>();

            queue.Enqueue((currentNode, 0));
            visited.Add(currentNode);

            while (queue.Count > 0)
            {
                var (node, distance) = queue.Dequeue();

                if (distance > 0) // Don't include starting node
                    reachable.Add(node);

                if (distance < maxDistance)
                {
                    foreach (var connection in node.Connections)
                    {
                        if (!visited.Contains(connection.TargetNode))
                        {
                            visited.Add(connection.TargetNode);
                            queue.Enqueue((connection.TargetNode, distance + 1));
                        }
                    }
                }
            }

            return reachable;
        }

        #region IMovementStrategy Implementation

        /// <summary>
        /// Stops any ongoing movement.
        /// IMovementStrategy implementation.
        /// </summary>
        public void StopMovement()
        {
            if (moveCoroutine != null)
            {
                StopCoroutine(moveCoroutine);
                moveCoroutine = null;
            }
            isMoving = false;
            currentPath = null;
            currentPathIndex = 0;
        }

        /// <summary>
        /// Teleports to a world position (finds nearest node).
        /// IMovementStrategy implementation.
        /// </summary>
        public void TeleportTo(Vector3 position)
        {
            StopMovement();

            // Find nearest node to target position
            var allNodes = FindObjectsByType<GraphNode>(FindObjectsSortMode.None);
            GraphNode nearest = null;
            float nearestDist = float.MaxValue;

            foreach (var node in allNodes)
            {
                float dist = Vector3.Distance(position, node.Position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = node;
                }
            }

            if (nearest != null)
            {
                TeleportToNode(nearest);
            }
            else
            {
                // No nodes in scene, just set position directly
                transform.position = position;
            }
        }

        /// <summary>
        /// Initializes the movement strategy.
        /// IMovementStrategy implementation.
        /// </summary>
        void IMovementStrategy.Initialize()
        {
            SnapToNearestNode();
        }

        /// <summary>
        /// Cleans up resources.
        /// IMovementStrategy implementation.
        /// </summary>
        void IMovementStrategy.Cleanup()
        {
            StopMovement();
            if (actor != null && actor.OnMoveInput != null)
            {
                actor.OnMoveInput.RemoveListener(HandleMoveInput);
            }
        }

        #endregion

        private GraphNode FindBestAdjacentNode(Vector3 direction)
        {
            if (currentNode == null || currentNode.Connections.Count == 0)
                return null;

            direction.Normalize();
            GraphNode bestNode = null;
            float bestDot = -1f;

            foreach (var connection in currentNode.Connections)
            {
                if (!connection.IsTraversable)
                    continue;

                Vector3 toNode = (connection.TargetNode.Position - currentNode.Position).normalized;
                float dot = Vector3.Dot(direction, toNode);

                if (dot > bestDot)
                {
                    bestDot = dot;
                    bestNode = connection.TargetNode;
                }
            }

            return bestNode;
        }

        private IEnumerator SmoothMoveToNode(GraphNode node)
        {
            isMoving = true;
            Vector3 startPos = transform.position;
            Vector3 endPos = node.Position;
            float elapsed = 0f;

            while (elapsed < moveSpeed)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / moveSpeed);
                float curveValue = movementCurve.Evaluate(t);
                transform.position = Vector3.Lerp(startPos, endPos, curveValue);
                yield return null;
            }

            transform.position = endPos;
            currentNode = node;
            isMoving = false;
            moveCoroutine = null;
        }

        private IEnumerator FollowPath()
        {
            isMoving = true;

            for (int i = 0; i < currentPath.Count; i++)
            {
                GraphNode node = currentPath[i];
                Vector3 startPos = transform.position;
                Vector3 endPos = node.Position;
                float elapsed = 0f;

                while (elapsed < moveSpeed)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / moveSpeed);
                    float curveValue = movementCurve.Evaluate(t);
                    transform.position = Vector3.Lerp(startPos, endPos, curveValue);
                    yield return null;
                }

                transform.position = endPos;
                currentNode = node;
                currentPathIndex = i;
            }

            isMoving = false;
            currentPath = null;
            currentPathIndex = 0;
            moveCoroutine = null;
        }

        /// <summary>
        /// A* pathfinding from start to goal node.
        /// </summary>
        private List<GraphNode> FindPath(GraphNode start, GraphNode goal)
        {
            var openSet = new List<GraphNode> { start };
            var cameFrom = new Dictionary<GraphNode, GraphNode>();
            var gScore = new Dictionary<GraphNode, float> { { start, 0f } };
            var fScore = new Dictionary<GraphNode, float> { { start, Heuristic(start, goal) } };

            int iterations = 0;
            int maxIterations = maxPathLength > 0 ? maxPathLength * 10 : 1000;

            while (openSet.Count > 0 && iterations < maxIterations)
            {
                iterations++;

                // Find node with lowest fScore
                GraphNode current = openSet.OrderBy(n => fScore.GetValueOrDefault(n, float.MaxValue)).First();

                if (current == goal)
                    return ReconstructPath(cameFrom, current);

                openSet.Remove(current);

                foreach (var connection in current.Connections)
                {
                    if (!connection.IsTraversable)
                        continue;

                    GraphNode neighbor = connection.TargetNode;
                    float tentativeGScore = gScore[current] + connection.Cost;

                    if (tentativeGScore < gScore.GetValueOrDefault(neighbor, float.MaxValue))
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeGScore;
                        fScore[neighbor] = tentativeGScore + Heuristic(neighbor, goal);

                        if (!openSet.Contains(neighbor))
                            openSet.Add(neighbor);
                    }
                }
            }

            return null; // No path found
        }

        private float Heuristic(GraphNode a, GraphNode b)
        {
            return Vector3.Distance(a.Position, b.Position);
        }

        private List<GraphNode> ReconstructPath(Dictionary<GraphNode, GraphNode> cameFrom, GraphNode current)
        {
            var path = new List<GraphNode> { current };
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                path.Insert(0, current);
            }
            // Remove starting node from path
            if (path.Count > 0)
                path.RemoveAt(0);
            return path;
        }

        private void OnDrawGizmos()
        {
            if (!showDebugVisualization || currentNode == null)
                return;

            // Draw current node
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(currentNode.Position, 0.3f);

            // Draw all connections from current node
            Gizmos.color = connectionColor;
            foreach (var connection in currentNode.Connections)
            {
                if (connection.IsTraversable)
                {
                    Gizmos.DrawLine(currentNode.Position, connection.TargetNode.Position);
                    // Draw arrow head
                    Vector3 direction = (connection.TargetNode.Position - currentNode.Position).normalized;
                    Vector3 arrowPos = connection.TargetNode.Position - direction * 0.2f;
                    Gizmos.DrawSphere(arrowPos, 0.1f);
                }
            }

            // Draw current path if any
            if (currentPath != null && currentPath.Count > 0)
            {
                Gizmos.color = Color.magenta;
                Vector3 prev = transform.position;
                foreach (var node in currentPath)
                {
                    Gizmos.DrawLine(prev, node.Position);
                    prev = node.Position;
                }
            }
        }
    }
}

