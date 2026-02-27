using System;
using System.Collections.Generic;
using System.Reflection;
using MCP.Editor.Base;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace MCP.Editor.Handlers
{
    /// <summary>
    /// NavMesh Bundle Handler: Bake navigation meshes, add agents, obstacles, links, and modifiers.
    /// Supports both built-in NavMesh types and com.unity.ai.navigation package types via reflection.
    /// </summary>
    public class NavMeshBundleHandler : BaseCommandHandler
    {
        public override IEnumerable<string> SupportedOperations => new[]
        {
            "bake",
            "addAgent",
            "addObstacle",
            "addLink",
            "addModifier",
            "inspect",
            "clearNavMesh"
        };

        public override string Category => "navmeshBundle";

        // Cached reflection types for com.unity.ai.navigation package
        private static Type _navMeshSurfaceType;
        private static Type _navMeshLinkType;
        private static Type _navMeshModifierType;
        private static bool _reflectionInitialized;

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            EnsureReflectionInitialized();

            return operation switch
            {
                "bake" => HandleBake(payload),
                "addAgent" => HandleAddAgent(payload),
                "addObstacle" => HandleAddObstacle(payload),
                "addLink" => HandleAddLink(payload),
                "addModifier" => HandleAddModifier(payload),
                "inspect" => HandleInspect(payload),
                "clearNavMesh" => HandleClearNavMesh(payload),
                _ => throw new ArgumentException($"Unknown operation: {operation}")
            };
        }

        private object HandleBake(Dictionary<string, object> payload)
        {
            var goPath = GetString(payload, "gameObjectPath");
            if (string.IsNullOrEmpty(goPath))
                throw new ArgumentException("Required parameter missing: gameObjectPath");
            var go = ResolveGameObject(goPath);

            if (_navMeshSurfaceType != null)
            {
                // Use NavMeshSurface from com.unity.ai.navigation package
                var surface = go.GetComponent(_navMeshSurfaceType);
                if (surface == null)
                    surface = Undo.AddComponent(go, _navMeshSurfaceType);

                // Configure surface properties
                if (payload.ContainsKey("agentTypeId"))
                    SetReflectionProperty(surface, "agentTypeID", GetInt(payload, "agentTypeId", 0));

                if (payload.ContainsKey("collectObjects"))
                {
                    var collectStr = payload["collectObjects"].ToString();
                    var collectType = _navMeshSurfaceType.Assembly.GetType("Unity.AI.Navigation.CollectObjects");
                    if (collectType == null)
                        collectType = _navMeshSurfaceType.Assembly.GetType("UnityEngine.AI.CollectObjects");
                    if (collectType != null)
                    {
                        try
                        {
                            var enumValue = Enum.Parse(collectType, collectStr);
                            SetReflectionProperty(surface, "collectObjects", enumValue);
                        }
                        catch { /* ignore parse failures */ }
                    }
                }

                // Trigger bake
                var buildMethod = _navMeshSurfaceType.GetMethod("BuildNavMesh");
                if (buildMethod != null)
                    buildMethod.Invoke(surface, null);

                return CreateSuccessResponse(
                    ("gameObject", goPath),
                    ("method", "NavMeshSurface"),
                    ("message", "NavMesh baked via NavMeshSurface")
                );
            }
            else
            {
                // Fallback: legacy NavMeshBuilder
#pragma warning disable CS0618
                UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
#pragma warning restore CS0618
                return CreateSuccessResponse(
                    ("gameObject", goPath),
                    ("method", "Legacy"),
                    ("message", "NavMesh baked via legacy NavMeshBuilder. Install com.unity.ai.navigation for NavMeshSurface support.")
                );
            }
        }

        private object HandleAddAgent(Dictionary<string, object> payload)
        {
            var goPath = GetString(payload, "gameObjectPath");
            if (string.IsNullOrEmpty(goPath))
                throw new ArgumentException("Required parameter missing: gameObjectPath");
            var go = ResolveGameObject(goPath);

            var agent = go.GetComponent<NavMeshAgent>();
            if (agent == null)
                agent = Undo.AddComponent<NavMeshAgent>(go);
            else
                Undo.RecordObject(agent, "Configure NavMeshAgent");

            agent.speed = GetFloat(payload, "speed", 3.5f);
            agent.angularSpeed = GetFloat(payload, "angularSpeed", 120f);
            agent.acceleration = GetFloat(payload, "acceleration", 8f);
            agent.stoppingDistance = GetFloat(payload, "stoppingDistance", 0f);
            agent.radius = GetFloat(payload, "radius", 0.5f);
            agent.height = GetFloat(payload, "height", 2f);

            if (payload.ContainsKey("agentTypeId"))
                agent.agentTypeID = GetInt(payload, "agentTypeId", 0);

            return CreateSuccessResponse(
                ("gameObject", goPath),
                ("speed", agent.speed),
                ("angularSpeed", agent.angularSpeed),
                ("acceleration", agent.acceleration),
                ("stoppingDistance", agent.stoppingDistance),
                ("radius", agent.radius),
                ("height", agent.height)
            );
        }

        private object HandleAddObstacle(Dictionary<string, object> payload)
        {
            var goPath = GetString(payload, "gameObjectPath");
            if (string.IsNullOrEmpty(goPath))
                throw new ArgumentException("Required parameter missing: gameObjectPath");
            var go = ResolveGameObject(goPath);

            var obstacle = go.GetComponent<NavMeshObstacle>();
            if (obstacle == null)
                obstacle = Undo.AddComponent<NavMeshObstacle>(go);
            else
                Undo.RecordObject(obstacle, "Configure NavMeshObstacle");

            if (payload.ContainsKey("shape"))
            {
                var shapeStr = payload["shape"].ToString();
                obstacle.shape = shapeStr == "Box"
                    ? NavMeshObstacleShape.Box
                    : NavMeshObstacleShape.Capsule;
            }

            obstacle.carving = GetBool(payload, "carve", true);

            if (payload.ContainsKey("radius"))
                obstacle.radius = GetFloat(payload, "radius", 0.5f);
            if (payload.ContainsKey("height"))
                obstacle.height = GetFloat(payload, "height", 1f);

            return CreateSuccessResponse(
                ("gameObject", goPath),
                ("shape", obstacle.shape.ToString()),
                ("carve", obstacle.carving),
                ("radius", obstacle.radius),
                ("height", obstacle.height)
            );
        }

        private object HandleAddLink(Dictionary<string, object> payload)
        {
            if (_navMeshLinkType == null)
                throw new InvalidOperationException("NavMeshLink requires com.unity.ai.navigation package. Install it via Package Manager.");

            var goPath = GetString(payload, "gameObjectPath");
            if (string.IsNullOrEmpty(goPath))
                throw new ArgumentException("Required parameter missing: gameObjectPath");
            var go = ResolveGameObject(goPath);

            var link = go.GetComponent(_navMeshLinkType);
            if (link == null)
                link = Undo.AddComponent(go, _navMeshLinkType);
            else
                Undo.RecordObject(link, "Configure NavMeshLink");

            if (payload.ContainsKey("startPoint"))
                SetReflectionProperty(link, "startPoint", GetVector3(payload, "startPoint"));
            if (payload.ContainsKey("endPoint"))
                SetReflectionProperty(link, "endPoint", GetVector3(payload, "endPoint"));
            if (payload.ContainsKey("linkWidth"))
                SetReflectionProperty(link, "width", GetFloat(payload, "linkWidth", 1f));
            if (payload.ContainsKey("bidirectional"))
                SetReflectionProperty(link, "bidirectional", GetBool(payload, "bidirectional", true));
            if (payload.ContainsKey("area"))
                SetReflectionProperty(link, "area", GetInt(payload, "area", 0));

            return CreateSuccessResponse(("gameObject", goPath), ("component", "NavMeshLink"));
        }

        private object HandleAddModifier(Dictionary<string, object> payload)
        {
            if (_navMeshModifierType == null)
                throw new InvalidOperationException("NavMeshModifier requires com.unity.ai.navigation package. Install it via Package Manager.");

            var goPath = GetString(payload, "gameObjectPath");
            if (string.IsNullOrEmpty(goPath))
                throw new ArgumentException("Required parameter missing: gameObjectPath");
            var go = ResolveGameObject(goPath);

            var modifier = go.GetComponent(_navMeshModifierType);
            if (modifier == null)
                modifier = Undo.AddComponent(go, _navMeshModifierType);
            else
                Undo.RecordObject(modifier, "Configure NavMeshModifier");

            if (payload.ContainsKey("overrideArea"))
                SetReflectionProperty(modifier, "overrideArea", GetBool(payload, "overrideArea", false));
            if (payload.ContainsKey("area"))
                SetReflectionProperty(modifier, "area", GetInt(payload, "area", 0));
            if (payload.ContainsKey("affectedAgents"))
                SetReflectionProperty(modifier, "affectedAgents", GetBool(payload, "affectedAgents", false));

            return CreateSuccessResponse(("gameObject", goPath), ("component", "NavMeshModifier"));
        }

        private object HandleInspect(Dictionary<string, object> payload)
        {
            var goPath = GetString(payload, "gameObjectPath");
            if (string.IsNullOrEmpty(goPath))
                throw new ArgumentException("Required parameter missing: gameObjectPath");
            var go = ResolveGameObject(goPath);

            var result = CreateSuccessResponse(("gameObject", goPath));

            var agent = go.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                result["navMeshAgent"] = new
                {
                    speed = agent.speed,
                    angularSpeed = agent.angularSpeed,
                    acceleration = agent.acceleration,
                    stoppingDistance = agent.stoppingDistance,
                    radius = agent.radius,
                    height = agent.height,
                    agentTypeID = agent.agentTypeID,
                    areaMask = agent.areaMask,
                    enabled = agent.enabled
                };
            }

            var obstacle = go.GetComponent<NavMeshObstacle>();
            if (obstacle != null)
            {
                result["navMeshObstacle"] = new
                {
                    shape = obstacle.shape.ToString(),
                    radius = obstacle.radius,
                    height = obstacle.height,
                    carving = obstacle.carving,
                    enabled = obstacle.enabled
                };
            }

            if (_navMeshSurfaceType != null)
            {
                var surface = go.GetComponent(_navMeshSurfaceType);
                if (surface != null)
                {
                    result["navMeshSurface"] = new
                    {
                        agentTypeID = GetReflectionProperty<int>(surface, "agentTypeID"),
                        collectObjects = GetReflectionProperty<object>(surface, "collectObjects")?.ToString()
                    };
                }
            }

            if (_navMeshLinkType != null)
            {
                var link = go.GetComponent(_navMeshLinkType);
                if (link != null)
                {
                    result["navMeshLink"] = new
                    {
                        startPoint = SerializeVector3(GetReflectionProperty<Vector3>(link, "startPoint")),
                        endPoint = SerializeVector3(GetReflectionProperty<Vector3>(link, "endPoint")),
                        width = GetReflectionProperty<float>(link, "width"),
                        bidirectional = GetReflectionProperty<bool>(link, "bidirectional")
                    };
                }
            }

            if (_navMeshModifierType != null)
            {
                var modifier = go.GetComponent(_navMeshModifierType);
                if (modifier != null)
                {
                    result["navMeshModifier"] = new
                    {
                        overrideArea = GetReflectionProperty<bool>(modifier, "overrideArea"),
                        area = GetReflectionProperty<int>(modifier, "area")
                    };
                }
            }

            return result;
        }

        private object HandleClearNavMesh(Dictionary<string, object> payload)
        {
            if (_navMeshSurfaceType != null && payload.ContainsKey("gameObjectPath"))
            {
                var goPath = GetString(payload, "gameObjectPath");
                if (!string.IsNullOrEmpty(goPath))
                {
                    var go = ResolveGameObject(goPath);
                    var surface = go.GetComponent(_navMeshSurfaceType);
                    if (surface != null)
                    {
                        Undo.RecordObject(surface, "Clear NavMesh");
                        var removeMethod = _navMeshSurfaceType.GetMethod("RemoveData");
                        if (removeMethod != null)
                            removeMethod.Invoke(surface, null);
                        return CreateSuccessResponse(
                            ("gameObject", goPath),
                            ("message", "NavMesh data cleared from NavMeshSurface")
                        );
                    }
                }
            }

            // Legacy clear
#pragma warning disable CS0618
            UnityEditor.AI.NavMeshBuilder.ClearAllNavMeshes();
#pragma warning restore CS0618
            return CreateSuccessResponse(("message", "All NavMesh data cleared"));
        }

        // ── Reflection Helpers ───────────────────────────────────

        private static void EnsureReflectionInitialized()
        {
            if (_reflectionInitialized) return;
            _reflectionInitialized = true;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (_navMeshSurfaceType == null)
                    _navMeshSurfaceType = assembly.GetType("Unity.AI.Navigation.NavMeshSurface");
                if (_navMeshLinkType == null)
                    _navMeshLinkType = assembly.GetType("Unity.AI.Navigation.NavMeshLink");
                if (_navMeshModifierType == null)
                    _navMeshModifierType = assembly.GetType("Unity.AI.Navigation.NavMeshModifier");

                if (_navMeshSurfaceType != null && _navMeshLinkType != null && _navMeshModifierType != null)
                    break;
            }
        }

        private static void SetReflectionProperty(object target, string propertyName, object value)
        {
            var type = target.GetType();
            var prop = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(target, value);
                return;
            }
            var field = type.GetField(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (field != null)
                field.SetValue(target, value);
        }

        private static T GetReflectionProperty<T>(object target, string propertyName)
        {
            var type = target.GetType();
            var prop = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (prop != null)
                return (T)prop.GetValue(target);
            var field = type.GetField(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (field != null)
                return (T)field.GetValue(target);
            return default;
        }
    }
}
