using System.Collections.Generic;
using System.Linq;

namespace MCP.Editor.Utilities.GraphAnalysis
{
    /// <summary>
    /// Represents the result of a graph analysis operation.
    /// </summary>
    public class GraphResult
    {
        /// <summary>
        /// Type of graph: "classDependency", "sceneReference", "sceneRelationship"
        /// </summary>
        public string GraphType { get; set; }

        /// <summary>
        /// List of nodes in the graph.
        /// </summary>
        public List<GraphNode> Nodes { get; set; } = new List<GraphNode>();

        /// <summary>
        /// List of edges (relationships) in the graph.
        /// </summary>
        public List<GraphEdge> Edges { get; set; } = new List<GraphEdge>();

        /// <summary>
        /// Additional metadata about the graph.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        public GraphResult() { }

        public GraphResult(string graphType)
        {
            GraphType = graphType;
        }

        /// <summary>
        /// Add a node to the graph if it doesn't already exist.
        /// </summary>
        public void AddNode(GraphNode node)
        {
            if (!Nodes.Any(n => n.Id == node.Id))
            {
                Nodes.Add(node);
            }
        }

        /// <summary>
        /// Add an edge to the graph.
        /// </summary>
        public void AddEdge(GraphEdge edge)
        {
            Edges.Add(edge);
        }

        /// <summary>
        /// Get a node by ID.
        /// </summary>
        public GraphNode GetNode(string id)
        {
            return Nodes.FirstOrDefault(n => n.Id == id);
        }

        /// <summary>
        /// Get all edges from a specific node.
        /// </summary>
        public IEnumerable<GraphEdge> GetEdgesFrom(string nodeId)
        {
            return Edges.Where(e => e.Source == nodeId);
        }

        /// <summary>
        /// Get all edges to a specific node.
        /// </summary>
        public IEnumerable<GraphEdge> GetEdgesTo(string nodeId)
        {
            return Edges.Where(e => e.Target == nodeId);
        }

        /// <summary>
        /// Convert to dictionary for JSON serialization.
        /// </summary>
        public Dictionary<string, object> ToDictionary()
        {
            var result = new Dictionary<string, object>
            {
                ["graphType"] = GraphType,
                ["nodes"] = Nodes.Select(n => n.ToDictionary()).ToList(),
                ["edges"] = Edges.Select(e => e.ToDictionary()).ToList(),
                ["nodeCount"] = Nodes.Count,
                ["edgeCount"] = Edges.Count
            };

            foreach (var kvp in Metadata)
            {
                result[kvp.Key] = kvp.Value;
            }

            return result;
        }

        /// <summary>
        /// Export as DOT format for Graphviz visualization.
        /// </summary>
        public string ToDot(string graphName = "G")
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"digraph {graphName} {{");
            sb.AppendLine("  rankdir=LR;");
            sb.AppendLine("  node [shape=box];");

            // Add nodes
            foreach (var node in Nodes)
            {
                var label = node.Id.Replace("\"", "\\\"");
                var nodeType = node.Type?.ToLower() ?? "default";
                var color = nodeType switch
                {
                    "monobehaviour" => "lightblue",
                    "scriptableobject" => "lightgreen",
                    "interface" => "lightyellow",
                    "gameobject" => "lightgray",
                    "prefabinstance" => "lightpink",
                    "scene" => "lightsalmon",
                    _ => "white"
                };
                sb.AppendLine($"  \"{node.Id}\" [label=\"{label}\", fillcolor=\"{color}\", style=\"filled\"];");
            }

            // Add edges
            foreach (var edge in Edges)
            {
                var style = edge.Relation switch
                {
                    "inherits" => "bold",
                    "implements" => "dashed",
                    "hierarchy_child" => "dotted",
                    _ => "solid"
                };
                var label = edge.Relation.Replace("_", " ");
                sb.AppendLine($"  \"{edge.Source}\" -> \"{edge.Target}\" [label=\"{label}\", style=\"{style}\"];");
            }

            sb.AppendLine("}");
            return sb.ToString();
        }

        /// <summary>
        /// Export as Mermaid format for Markdown documentation.
        /// </summary>
        public string ToMermaid()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("graph TD");

            // Create safe node IDs for Mermaid (no special chars)
            var nodeIdMap = new Dictionary<string, string>();
            int nodeIndex = 0;
            foreach (var node in Nodes)
            {
                var safeId = $"N{nodeIndex++}";
                nodeIdMap[node.Id] = safeId;
                var label = node.Id.Replace("\"", "'");
                sb.AppendLine($"    {safeId}[\"{label}\"]");
            }

            // Add edges
            foreach (var edge in Edges)
            {
                if (nodeIdMap.TryGetValue(edge.Source, out var sourceId) &&
                    nodeIdMap.TryGetValue(edge.Target, out var targetId))
                {
                    var arrow = edge.Relation switch
                    {
                        "inherits" => "==>",
                        "implements" => "-.->",
                        "hierarchy_child" => "-->",
                        _ => "-->"
                    };
                    var label = edge.Relation.Replace("_", " ");
                    sb.AppendLine($"    {sourceId} {arrow}|{label}| {targetId}");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generate a text summary of the graph.
        /// </summary>
        public string ToSummary()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Graph Type: {GraphType}");
            sb.AppendLine($"Nodes: {Nodes.Count}");
            sb.AppendLine($"Edges: {Edges.Count}");
            sb.AppendLine();

            // Group nodes by type
            var nodesByType = Nodes.GroupBy(n => n.Type ?? "Unknown").OrderBy(g => g.Key);
            sb.AppendLine("Nodes by Type:");
            foreach (var group in nodesByType)
            {
                sb.AppendLine($"  {group.Key}: {group.Count()}");
            }
            sb.AppendLine();

            // Group edges by relation
            var edgesByRelation = Edges.GroupBy(e => e.Relation ?? "Unknown").OrderBy(g => g.Key);
            sb.AppendLine("Edges by Relation:");
            foreach (var group in edgesByRelation)
            {
                sb.AppendLine($"  {group.Key}: {group.Count()}");
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// Specialized result for class dependency graphs.
    /// </summary>
    public class ClassDependencyResult : GraphResult
    {
        public ClassDependencyResult() : base("classDependency") { }

        /// <summary>
        /// Target class/assembly/namespace that was analyzed.
        /// </summary>
        public string AnalysisTarget
        {
            get => Metadata.TryGetValue("analysisTarget", out var v) ? v as string : null;
            set => Metadata["analysisTarget"] = value;
        }

        /// <summary>
        /// Analysis depth used.
        /// </summary>
        public int Depth
        {
            get => Metadata.TryGetValue("depth", out var v) && v is int i ? i : 1;
            set => Metadata["depth"] = value;
        }
    }

    /// <summary>
    /// Specialized result for scene reference graphs.
    /// </summary>
    public class SceneReferenceResult : GraphResult
    {
        public SceneReferenceResult() : base("sceneReference") { }

        /// <summary>
        /// Scene path that was analyzed.
        /// </summary>
        public string ScenePath
        {
            get => Metadata.TryGetValue("scenePath", out var v) ? v as string : null;
            set => Metadata["scenePath"] = value;
        }

        /// <summary>
        /// List of orphan objects (not referenced by anything).
        /// </summary>
        public List<string> Orphans
        {
            get => Metadata.TryGetValue("orphans", out var v) ? v as List<string> : null;
            set => Metadata["orphans"] = value;
        }
    }

    /// <summary>
    /// Specialized result for scene relationship graphs.
    /// </summary>
    public class SceneRelationshipResult : GraphResult
    {
        public SceneRelationshipResult() : base("sceneRelationship") { }

        /// <summary>
        /// Build order of scenes in Build Settings.
        /// </summary>
        public List<string> BuildOrder
        {
            get => Metadata.TryGetValue("buildOrder", out var v) ? v as List<string> : null;
            set => Metadata["buildOrder"] = value;
        }

        /// <summary>
        /// Scenes not in Build Settings.
        /// </summary>
        public List<string> UnregisteredScenes
        {
            get => Metadata.TryGetValue("unregisteredScenes", out var v) ? v as List<string> : null;
            set => Metadata["unregisteredScenes"] = value;
        }
    }
}
