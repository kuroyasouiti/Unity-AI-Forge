using System.Collections.Generic;

namespace MCP.Editor.Utilities.GraphAnalysis
{
    /// <summary>
    /// Represents an edge (relationship) between two nodes in a graph.
    /// </summary>
    public class GraphEdge
    {
        /// <summary>
        /// Source node ID.
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Target node ID.
        /// </summary>
        public string Target { get; set; }

        /// <summary>
        /// Type of relationship.
        /// For classes: "field_reference", "method_parameter", "return_type", "inherits", "implements", "requires_component"
        /// For scene objects: "component_reference", "unity_event", "hierarchy_child", "prefab_source"
        /// For scenes: "scene_load", "scene_load_additive", "sceneflow_transition"
        /// </summary>
        public string Relation { get; set; }

        /// <summary>
        /// Additional details about the relationship.
        /// </summary>
        public Dictionary<string, object> Details { get; set; } = new Dictionary<string, object>();

        public GraphEdge() { }

        public GraphEdge(string source, string target, string relation)
        {
            Source = source;
            Target = target;
            Relation = relation;
        }

        /// <summary>
        /// Convert to dictionary for JSON serialization.
        /// </summary>
        public Dictionary<string, object> ToDictionary()
        {
            var result = new Dictionary<string, object>
            {
                ["source"] = Source,
                ["target"] = Target,
                ["relation"] = Relation
            };

            foreach (var kvp in Details)
            {
                result[kvp.Key] = kvp.Value;
            }

            return result;
        }
    }

    /// <summary>
    /// Specialized edge for class dependency relationships.
    /// </summary>
    public class ClassDependencyEdge : GraphEdge
    {
        /// <summary>
        /// Field/property/parameter declaration details.
        /// </summary>
        public string Declaration
        {
            get => Details.TryGetValue("details", out var v) ? v as string : null;
            set => Details["details"] = value;
        }

        /// <summary>
        /// Member name (field name, method name, etc).
        /// </summary>
        public string MemberName
        {
            get => Details.TryGetValue("memberName", out var v) ? v as string : null;
            set => Details["memberName"] = value;
        }

        public ClassDependencyEdge() : base() { }

        public ClassDependencyEdge(string source, string target, string relation) : base(source, target, relation) { }
    }

    /// <summary>
    /// Specialized edge for scene object references.
    /// </summary>
    public class SceneReferenceEdge : GraphEdge
    {
        /// <summary>
        /// Component type that holds the reference.
        /// </summary>
        public string SourceComponent
        {
            get => Details.TryGetValue("sourceComponent", out var v) ? v as string : null;
            set => Details["sourceComponent"] = value;
        }

        /// <summary>
        /// Field name that holds the reference.
        /// </summary>
        public string SourceField
        {
            get => Details.TryGetValue("sourceField", out var v) ? v as string : null;
            set => Details["sourceField"] = value;
        }

        /// <summary>
        /// Event name for UnityEvent references.
        /// </summary>
        public string EventName
        {
            get => Details.TryGetValue("eventName", out var v) ? v as string : null;
            set => Details["eventName"] = value;
        }

        /// <summary>
        /// Target component type if referencing a specific component.
        /// </summary>
        public string TargetComponent
        {
            get => Details.TryGetValue("targetComponent", out var v) ? v as string : null;
            set => Details["targetComponent"] = value;
        }

        public SceneReferenceEdge() : base() { }

        public SceneReferenceEdge(string source, string target, string relation) : base(source, target, relation) { }
    }

    /// <summary>
    /// Specialized edge for scene transitions.
    /// </summary>
    public class SceneTransitionEdge : GraphEdge
    {
        /// <summary>
        /// Load type: "single" or "additive".
        /// </summary>
        public string LoadType
        {
            get => Details.TryGetValue("loadType", out var v) ? v as string : null;
            set => Details["loadType"] = value;
        }

        /// <summary>
        /// Script that contains the scene load call.
        /// </summary>
        public string CallerScript
        {
            get => Details.TryGetValue("callerScript", out var v) ? v as string : null;
            set => Details["callerScript"] = value;
        }

        /// <summary>
        /// Line number of the scene load call.
        /// </summary>
        public int CallerLine
        {
            get => Details.TryGetValue("callerLine", out var v) && v is int i ? i : 0;
            set => Details["callerLine"] = value;
        }

        /// <summary>
        /// SceneFlow asset path for sceneflow_transition type.
        /// </summary>
        public string FlowAsset
        {
            get => Details.TryGetValue("flowAsset", out var v) ? v as string : null;
            set => Details["flowAsset"] = value;
        }

        /// <summary>
        /// Trigger name for sceneflow_transition type.
        /// </summary>
        public string Trigger
        {
            get => Details.TryGetValue("trigger", out var v) ? v as string : null;
            set => Details["trigger"] = value;
        }

        public SceneTransitionEdge() : base() { }

        public SceneTransitionEdge(string source, string target, string relation) : base(source, target, relation) { }
    }
}
