using UnityEngine;
using System.Collections.Generic;
using System;

namespace UnityAIForge.GameKit
{
    /// <summary>
    /// Represents a node in a movement graph.
    /// Nodes can be connected to other nodes with weighted, traversable connections.
    /// </summary>
    [AddComponentMenu("SkillForUnity/GameKit/Graph Node")]
    public class GraphNode : MonoBehaviour
    {
        [Header("Node Settings")]
        [Tooltip("Unique identifier for this node (optional)")]
        [SerializeField] private string nodeId;
        
        [Tooltip("Connections to other nodes")]
        [SerializeField] private List<NodeConnection> connections = new List<NodeConnection>();
        
        [Header("Visualization")]
        [Tooltip("Show debug visualization")]
        [SerializeField] private bool showDebug = true;
        
        [Tooltip("Node display color")]
        [SerializeField] private Color nodeColor = Color.green;
        
        [Tooltip("Node radius for visualization")]
        [SerializeField] private float nodeRadius = 0.3f;

        public string NodeId
        {
            get => string.IsNullOrEmpty(nodeId) ? gameObject.name : nodeId;
            set => nodeId = value;
        }

        public Vector3 Position => transform.position;
        public List<NodeConnection> Connections => connections;

        /// <summary>
        /// Adds a connection to another node.
        /// </summary>
        public void AddConnection(GraphNode targetNode, float cost = 1.0f, bool bidirectional = true)
        {
            if (targetNode == null || targetNode == this)
                return;

            // Check if connection already exists
            if (connections.Exists(c => c.TargetNode == targetNode))
                return;

            connections.Add(new NodeConnection
            {
                TargetNode = targetNode,
                Cost = cost,
                IsTraversable = true
            });

            if (bidirectional)
            {
                targetNode.AddConnection(this, cost, false);
            }
        }

        /// <summary>
        /// Removes a connection to another node.
        /// </summary>
        public void RemoveConnection(GraphNode targetNode, bool bidirectional = true)
        {
            connections.RemoveAll(c => c.TargetNode == targetNode);

            if (bidirectional)
            {
                targetNode.RemoveConnection(this, false);
            }
        }

        /// <summary>
        /// Checks if this node is directly connected to another node.
        /// </summary>
        public bool IsConnectedTo(GraphNode targetNode)
        {
            return connections.Exists(c => c.TargetNode == targetNode && c.IsTraversable);
        }

        /// <summary>
        /// Gets the connection to a specific node.
        /// </summary>
        public NodeConnection GetConnectionTo(GraphNode targetNode)
        {
            return connections.Find(c => c.TargetNode == targetNode);
        }

        /// <summary>
        /// Sets the traversability of a connection.
        /// </summary>
        public void SetConnectionTraversable(GraphNode targetNode, bool traversable)
        {
            var connection = GetConnectionTo(targetNode);
            if (connection != null)
            {
                connection.IsTraversable = traversable;
            }
        }

        /// <summary>
        /// Automatically connects to nearby nodes within a radius.
        /// </summary>
        public void AutoConnectToNearbyNodes(float radius, LayerMask nodeLayer = default)
        {
            var allNodes = FindObjectsByType<GraphNode>(FindObjectsSortMode.None);
            
            foreach (var node in allNodes)
            {
                if (node == this)
                    continue;

                float distance = Vector3.Distance(Position, node.Position);
                if (distance <= radius)
                {
                    AddConnection(node, distance, true);
                }
            }
        }

        /// <summary>
        /// Connects to specific nodes in a list.
        /// </summary>
        public void ConnectToNodes(List<GraphNode> nodes, bool bidirectional = true)
        {
            foreach (var node in nodes)
            {
                if (node != null && node != this)
                {
                    float cost = Vector3.Distance(Position, node.Position);
                    AddConnection(node, cost, bidirectional);
                }
            }
        }

        /// <summary>
        /// Clears all connections from this node.
        /// </summary>
        public void ClearConnections(bool bidirectional = true)
        {
            if (bidirectional)
            {
                foreach (var connection in connections)
                {
                    connection.TargetNode?.RemoveConnection(this, false);
                }
            }
            connections.Clear();
        }

        private void OnDrawGizmos()
        {
            if (!showDebug)
                return;

            // Draw node
            Gizmos.color = nodeColor;
            Gizmos.DrawWireSphere(Position, nodeRadius);

            // Draw connections
            Gizmos.color = new Color(nodeColor.r, nodeColor.g, nodeColor.b, 0.5f);
            foreach (var connection in connections)
            {
                if (connection.TargetNode != null)
                {
                    Color lineColor = connection.IsTraversable ? Color.cyan : Color.red;
                    lineColor.a = 0.3f;
                    Gizmos.color = lineColor;
                    
                    Vector3 start = Position;
                    Vector3 end = connection.TargetNode.Position;
                    Gizmos.DrawLine(start, end);

                    // Draw cost label in scene view
                    Vector3 midPoint = (start + end) / 2f;
                    
                    // Draw arrow to show direction
                    Vector3 direction = (end - start).normalized;
                    Vector3 arrowPos = end - direction * 0.2f;
                    Gizmos.DrawSphere(arrowPos, 0.08f);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!showDebug)
                return;

            // Highlight selected node
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(Position, nodeRadius * 1.5f);

            // Highlight connections
            foreach (var connection in connections)
            {
                if (connection.TargetNode != null && connection.IsTraversable)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(Position, connection.TargetNode.Position);
                }
            }
        }
    }

    /// <summary>
    /// Represents a connection between two nodes in the graph.
    /// </summary>
    [Serializable]
    public class NodeConnection
    {
        [Tooltip("Target node this connection leads to")]
        public GraphNode TargetNode;
        
        [Tooltip("Cost to traverse this connection (distance, time, etc.)")]
        public float Cost = 1.0f;
        
        [Tooltip("Whether this connection can be traversed")]
        public bool IsTraversable = true;
    }
}

