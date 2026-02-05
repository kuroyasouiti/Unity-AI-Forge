using System.Collections.Generic;

namespace MCP.Editor.Utilities.GraphAnalysis
{
    /// <summary>
    /// Represents a node in a relationship graph.
    /// </summary>
    public class GraphNode
    {
        /// <summary>
        /// Unique identifier for the node.
        /// For classes: fully qualified type name (e.g., "MyNamespace.PlayerController")
        /// For GameObjects: hierarchy path (e.g., "/Player/Weapon")
        /// For scenes: scene asset path (e.g., "Assets/Scenes/MainMenu.unity")
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Node type classification.
        /// For classes: "MonoBehaviour", "ScriptableObject", "class", "interface", "enum"
        /// For GameObjects: "GameObject", "PrefabInstance"
        /// For scenes: "Scene"
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Additional properties specific to the node type.
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

        public GraphNode() { }

        public GraphNode(string id, string type)
        {
            Id = id;
            Type = type;
        }

        /// <summary>
        /// Convert to dictionary for JSON serialization.
        /// </summary>
        public Dictionary<string, object> ToDictionary()
        {
            var result = new Dictionary<string, object>
            {
                ["id"] = Id,
                ["type"] = Type
            };

            foreach (var kvp in Properties)
            {
                result[kvp.Key] = kvp.Value;
            }

            return result;
        }
    }

    /// <summary>
    /// Specialized node for class dependency graphs.
    /// </summary>
    public class ClassGraphNode : GraphNode
    {
        /// <summary>
        /// Path to the source file containing this class.
        /// </summary>
        public string FilePath
        {
            get => Properties.TryGetValue("filePath", out var v) ? v as string : null;
            set => Properties["filePath"] = value;
        }

        /// <summary>
        /// Assembly name containing this class.
        /// </summary>
        public string Assembly
        {
            get => Properties.TryGetValue("assembly", out var v) ? v as string : null;
            set => Properties["assembly"] = value;
        }

        /// <summary>
        /// Namespace of this class.
        /// </summary>
        public string Namespace
        {
            get => Properties.TryGetValue("namespace", out var v) ? v as string : null;
            set => Properties["namespace"] = value;
        }

        /// <summary>
        /// Base class if any.
        /// </summary>
        public string BaseClass
        {
            get => Properties.TryGetValue("baseClass", out var v) ? v as string : null;
            set => Properties["baseClass"] = value;
        }

        /// <summary>
        /// Implemented interfaces.
        /// </summary>
        public List<string> Interfaces
        {
            get => Properties.TryGetValue("interfaces", out var v) ? v as List<string> : null;
            set => Properties["interfaces"] = value;
        }

        public ClassGraphNode() : base() { }

        public ClassGraphNode(string fullTypeName, string type) : base(fullTypeName, type) { }
    }

    /// <summary>
    /// Specialized node for scene reference graphs.
    /// </summary>
    public class SceneObjectNode : GraphNode
    {
        /// <summary>
        /// Hierarchy path of the GameObject.
        /// </summary>
        public string Path
        {
            get => Properties.TryGetValue("path", out var v) ? v as string : null;
            set => Properties["path"] = value;
        }

        /// <summary>
        /// Unity instance ID.
        /// </summary>
        public int InstanceId
        {
            get => Properties.TryGetValue("instanceId", out var v) && v is int i ? i : 0;
            set => Properties["instanceId"] = value;
        }

        /// <summary>
        /// List of component type names attached to this GameObject.
        /// </summary>
        public List<string> Components
        {
            get => Properties.TryGetValue("components", out var v) ? v as List<string> : null;
            set => Properties["components"] = value;
        }

        /// <summary>
        /// Whether this is a prefab instance.
        /// </summary>
        public bool IsPrefabInstance
        {
            get => Properties.TryGetValue("isPrefabInstance", out var v) && v is bool b && b;
            set => Properties["isPrefabInstance"] = value;
        }

        /// <summary>
        /// Source prefab asset path if this is a prefab instance.
        /// </summary>
        public string PrefabAsset
        {
            get => Properties.TryGetValue("prefabAsset", out var v) ? v as string : null;
            set => Properties["prefabAsset"] = value;
        }

        public SceneObjectNode() : base() { }

        public SceneObjectNode(string id, string path) : base(id, "GameObject")
        {
            Path = path;
        }
    }

    /// <summary>
    /// Specialized node for scene relationship graphs.
    /// </summary>
    public class SceneNode : GraphNode
    {
        /// <summary>
        /// Scene name without path and extension.
        /// </summary>
        public string Name
        {
            get => Properties.TryGetValue("name", out var v) ? v as string : null;
            set => Properties["name"] = value;
        }

        /// <summary>
        /// Build index in Build Settings (-1 if not in build).
        /// </summary>
        public int BuildIndex
        {
            get => Properties.TryGetValue("buildIndex", out var v) && v is int i ? i : -1;
            set => Properties["buildIndex"] = value;
        }

        /// <summary>
        /// Whether this scene is in Build Settings.
        /// </summary>
        public bool InBuildSettings
        {
            get => Properties.TryGetValue("inBuildSettings", out var v) && v is bool b && b;
            set => Properties["inBuildSettings"] = value;
        }

        /// <summary>
        /// Whether this scene is addressable.
        /// </summary>
        public bool IsAddressable
        {
            get => Properties.TryGetValue("isAddressable", out var v) && v is bool b && b;
            set => Properties["isAddressable"] = value;
        }

        public SceneNode() : base() { Type = "Scene"; }

        public SceneNode(string scenePath) : base(scenePath, "Scene")
        {
            var fileName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            Name = fileName;
        }
    }
}
