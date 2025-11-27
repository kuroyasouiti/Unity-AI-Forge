using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace MCP.Editor
{
    /// <summary>
    /// Processes MCP tool commands and executes corresponding Unity Editor operations.
    /// Supports management operations for scenes, GameObjects, components, and assets.
    /// </summary>
    internal static partial class McpCommandProcessor
    {
        /// <summary>
        /// Executes an MCP command and returns the result.
        /// </summary>
        /// <param name="command">The command to execute containing tool name and payload.</param>
        /// <returns>Result dictionary with operation-specific data.</returns>
        /// <exception cref="InvalidOperationException">Thrown when tool name is not supported.</exception>
        public static object Execute(McpIncomingCommand command)
        {
            return command.ToolName switch
            {
                "pingUnityEditor" => HandlePing(),
                "sceneManage" => HandleSceneManage(command.Payload),
                "gameObjectManage" => HandleGameObjectManage(command.Payload),
                "componentManage" => HandleComponentManage(command.Payload),
                "assetManage" => HandleAssetManage(command.Payload),
                "uguiRectAdjust" => HandleUguiRectAdjust(command.Payload),
                "uguiAnchorManage" => HandleUguiAnchorManage(command.Payload),
                "uguiManage" => HandleUguiManage(command.Payload),
                "uguiCreateFromTemplate" => HandleUguiCreateFromTemplate(command.Payload),
                "uguiLayoutManage" => HandleUguiLayoutManage(command.Payload),
                "uguiDetectOverlaps" => HandleUguiDetectOverlaps(command.Payload),
                "sceneQuickSetup" => HandleSceneQuickSetup(command.Payload),
                "gameObjectCreateFromTemplate" => HandleGameObjectCreateFromTemplate(command.Payload),
                "tagLayerManage" => HandleTagLayerManage(command.Payload),
                "prefabManage" => HandlePrefabManage(command.Payload),
                "scriptableObjectManage" => HandleScriptableObjectManage(command.Payload),
                "projectSettingsManage" => HandleProjectSettingsManage(command.Payload),
                "renderPipelineManage" => HandleRenderPipelineManage(command.Payload),
                "constantConvert" => HandleConstantConvert(command.Payload),
                "designPatternGenerate" => HandleDesignPatternGenerate(command.Payload),
                "scriptTemplateGenerate" => HandleScriptTemplateGenerate(command.Payload),
                "templateManage" => HandleTemplateManage(command.Payload),
                "menuHierarchyCreate" => HandleMenuHierarchyCreate(command.Payload),
                _ => throw new InvalidOperationException($"Unsupported tool name: {command.ToolName}"),
            };
        }

        /// <summary>
        /// Handles ping requests to verify Unity Editor connectivity.
        /// </summary>
        /// <returns>Dictionary containing Unity version, project name, and current timestamp.</returns>
        private static object HandlePing()
        {
            return new Dictionary<string, object>
            {
                ["editor"] = Application.unityVersion,
                ["project"] = Application.productName,
                ["time"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            };
        }

        /// <summary>
        /// Handles scene management operations (create, load, save, delete, duplicate).
        /// </summary>
        /// <param name="payload">Operation parameters including 'operation' type and scene path.</param>
        /// <returns>Result dictionary with operation-specific data.</returns>
        /// <exception cref="InvalidOperationException">Thrown when operation is invalid or missing.</exception>
        // NOTE: Scene management operations have been moved to Scene/McpCommandProcessor.Scene.cs
        // This includes: HandleSceneManage, CreateScene, LoadScene, SaveScenes, DeleteScene,
        // DuplicateScene, InspectScene, and all build settings operations.


        /// <summary>
        /// Handles GameObject management operations (create, delete, move, rename, duplicate).
        /// </summary>
        /// <param name="payload">Operation parameters including 'operation' type and GameObject path.</param>
        /// <returns>Result dictionary with GameObject hierarchy path and instance ID.</returns>
        /// <exception cref="InvalidOperationException">Thrown when operation or required parameters are invalid.</exception>

        // NOTE: GameObject management operations have been moved to GameObject/McpCommandProcessor.GameObject.cs
        // This includes: HandleGameObjectManage, CreateGameObject, DeleteGameObject, MoveGameObject, RenameGameObject,
        // UpdateGameObject, DuplicateGameObject, InspectGameObject, FindMultipleGameObjects, DeleteMultipleGameObjects,
        // and InspectMultipleGameObjects.

        /// <summary>
        /// Handles component management operations (add, remove, update, inspect).
        /// Uses reflection to set component properties from the payload.
        /// Monitors compilation status and returns whether compilation was triggered.
        /// </summary>
        /// <param name="payload">Operation parameters including 'operation', 'gameObjectPath', 'componentType', and optional 'propertyChanges'.</param>
        /// <returns>Result dictionary with component type, GameObject path, and compilation status.</returns>
        /// <exception cref="InvalidOperationException">Thrown when GameObject or component type is not found.</exception>

        // NOTE: Component management operations have been moved to Component/McpCommandProcessor.Component.cs
        // This includes: HandleComponentManage, AddComponent, RemoveComponent, UpdateComponent, InspectComponent,
        // AddMultipleComponents, RemoveMultipleComponents, UpdateMultipleComponents, and InspectMultipleComponents.


        // NOTE: Asset management operations have been moved to Asset/McpCommandProcessor.Asset.cs
        // This includes: HandleAssetManage, CreateTextAsset, UpdateTextAsset, UpdateAssetImporter,
        // UpdateAsset, DeleteAsset, RenameAsset, DuplicateAsset, InspectAsset,
        // FindMultipleAssets, DeleteMultipleAssets, and InspectMultipleAssets.



        // NOTE: UI (UGUI) management operations have been moved to UI/McpCommandProcessor.UI.cs
        // This includes: HandleUguiRectAdjust, HandleUguiAnchorManage, HandleUguiManage,
        // HandleUguiCreateFromTemplate, HandleUguiLayoutManage, HandleUguiDetectOverlaps,
        // and all related UI helper methods (DetectRectOverlap, etc.).

        private static object HandleSceneQuickSetup(Dictionary<string, object> payload)
        {
            // Check if compilation is in progress and wait if necessary
            var compilationWaitInfo = EnsureNoCompilationInProgress("sceneQuickSetup", maxWaitSeconds: 30f);

            try
            {
                var setupType = GetString(payload, "setupType");
                if (string.IsNullOrEmpty(setupType))
                {
                    throw new InvalidOperationException("setupType is required");
                }

                Debug.Log($"[sceneQuickSetup] Setting up {setupType} scene");

                var createdObjects = new List<string>();

                switch (setupType)
                {
                    case "3D":
                        createdObjects.AddRange(Setup3DScene(payload));
                        break;
                    case "2D":
                        createdObjects.AddRange(Setup2DScene(payload));
                        break;
                    case "UI":
                        createdObjects.AddRange(SetupUIScene(payload));
                        break;
                    case "VR":
                        createdObjects.AddRange(SetupVRScene(payload));
                        break;
                    case "Empty":
                        // Do nothing for empty scene
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown setupType: {setupType}");
                }

                return new Dictionary<string, object>
                {
                    ["setupType"] = setupType,
                    ["createdObjects"] = createdObjects,
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[sceneQuickSetup] Error: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        private static List<string> Setup3DScene(Dictionary<string, object> payload)
        {
            var created = new List<string>();

            // Check if Main Camera already exists
            var existingCamera = Camera.main;
            if (existingCamera == null)
            {
                // Create Main Camera
                var camera = new GameObject("Main Camera");
                camera.AddComponent<Camera>();
                camera.tag = "MainCamera";

                var camPosDict = payload.ContainsKey("cameraPosition") ? payload["cameraPosition"] as Dictionary<string, object> : null;
                if (camPosDict != null)
                {
                    camera.transform.position = new Vector3(
                        GetFloat(camPosDict, "x") ?? 0,
                        GetFloat(camPosDict, "y") ?? 1,
                        GetFloat(camPosDict, "z") ?? -10
                    );
                }
                else
                {
                    camera.transform.position = new Vector3(0, 1, -10);
                }

                Undo.RegisterCreatedObjectUndo(camera, "Create Main Camera");
                created.Add("Main Camera");
            }

            // Check if Directional Light already exists
            var existingLights = UnityEngine.Object.FindObjectsOfType<Light>();
            var hasDirectionalLight = false;
            foreach (var existingLight in existingLights)
            {
                if (existingLight.type == LightType.Directional)
                {
                    hasDirectionalLight = true;
                    break;
                }
            }

            if (!hasDirectionalLight)
            {
                // Create Directional Light
                var light = new GameObject("Directional Light");
                var lightComp = light.AddComponent<Light>();
                lightComp.type = LightType.Directional;
                lightComp.intensity = GetFloat(payload, "lightIntensity") ?? 1f;
                light.transform.rotation = Quaternion.Euler(50, -30, 0);

                Undo.RegisterCreatedObjectUndo(light, "Create Directional Light");
                created.Add("Directional Light");
            }

            return created;
        }

        private static List<string> Setup2DScene(Dictionary<string, object> payload)
        {
            var created = new List<string>();

            // Check if Main Camera already exists
            var existingCamera = Camera.main;
            if (existingCamera == null)
            {
                // Create Main Camera for 2D
                var camera = new GameObject("Main Camera");
                var cam = camera.AddComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = 5;
                camera.tag = "MainCamera";
                camera.transform.position = new Vector3(0, 0, -10);

                Undo.RegisterCreatedObjectUndo(camera, "Create Main Camera");
                created.Add("Main Camera");
            }

            return created;
        }

        private static List<string> SetupUIScene(Dictionary<string, object> payload)
        {
            var created = new List<string>();

            // Check if Canvas already exists
            var existingCanvas = UnityEngine.Object.FindObjectOfType<Canvas>();
            if (existingCanvas == null)
            {
                // Create Canvas
                var canvas = new GameObject("Canvas");
                var canvasComp = canvas.AddComponent<Canvas>();
                canvasComp.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();

                Undo.RegisterCreatedObjectUndo(canvas, "Create Canvas");
                created.Add("Canvas");
            }

            // Check if EventSystem already exists
            var includeEventSystem = GetBool(payload, "includeEventSystem", true);
            if (includeEventSystem)
            {
                var existingEventSystem = UnityEngine.Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
                if (existingEventSystem == null)
                {
                    var eventSystem = new GameObject("EventSystem");
                    eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                    eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

                    Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
                    created.Add("EventSystem");
                }
            }

            return created;
        }

        private static List<string> SetupVRScene(Dictionary<string, object> payload)
        {
            var created = new List<string>();

            // Check if Main Camera already exists
            var existingCamera = Camera.main;
            if (existingCamera == null)
            {
                // Create XR Origin (simplified - would need XR packages in real implementation)
                var camera = new GameObject("Main Camera");
                camera.AddComponent<Camera>();
                camera.tag = "MainCamera";
                camera.transform.position = new Vector3(0, 1.6f, 0);

                Undo.RegisterCreatedObjectUndo(camera, "Create Main Camera");
                created.Add("Main Camera");
            }

            return created;
        }

        /// <summary>
        /// Creates GameObjects from predefined templates (primitives, lights, camera, etc.).
        /// Each template includes appropriate components and sensible defaults.
        /// </summary>
        /// <param name="payload">Template type, name, parent, transform properties.</param>
        /// <returns>Result dictionary with created GameObject information.</returns>
        private static object HandleGameObjectCreateFromTemplate(Dictionary<string, object> payload)
        {
            // Check if compilation is in progress and wait if necessary
            var compilationWaitInfo = EnsureNoCompilationInProgress("gameObjectCreateFromTemplate", maxWaitSeconds: 30f);

            try
            {
                var template = GetString(payload, "template");
                if (string.IsNullOrEmpty(template))
                {
                    throw new InvalidOperationException("template is required");
                }

                Debug.Log($"[gameObjectCreateFromTemplate] Creating template: {template}");

                var name = GetString(payload, "name");
                if (string.IsNullOrEmpty(name))
                {
                    name = template.Replace("Light-", "");
                }

                GameObject parent = null;
                var parentPath = GetString(payload, "parentPath");
                if (!string.IsNullOrEmpty(parentPath))
                {
                    parent = ResolveGameObject(parentPath);
                }

                GameObject go = null;
                switch (template)
                {
                    case "Camera":
                        go = new GameObject(name);
                        go.AddComponent<Camera>();
                        go.tag = "MainCamera";
                        break;
                    case "Light-Directional":
                        go = new GameObject(name);
                        var dirLight = go.AddComponent<Light>();
                        dirLight.type = LightType.Directional;
                        go.transform.rotation = Quaternion.Euler(50, -30, 0);
                        break;
                    case "Light-Point":
                        go = new GameObject(name);
                        var pointLight = go.AddComponent<Light>();
                        pointLight.type = LightType.Point;
                        break;
                    case "Light-Spot":
                        go = new GameObject(name);
                        var spotLight = go.AddComponent<Light>();
                        spotLight.type = LightType.Spot;
                        break;
                    case "Cube":
                        go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        go.name = name;
                        break;
                    case "Sphere":
                        go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        go.name = name;
                        break;
                    case "Plane":
                        go = GameObject.CreatePrimitive(PrimitiveType.Plane);
                        go.name = name;
                        break;
                    case "Cylinder":
                        go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        go.name = name;
                        break;
                    case "Capsule":
                        go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                        go.name = name;
                        break;
                    case "Quad":
                        go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                        go.name = name;
                        break;
                    case "Empty":
                        go = new GameObject(name);
                        break;
                    case "Player":
                        go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                        go.name = name;
                        go.AddComponent<Rigidbody>();
                        var playerCollider = go.GetComponent<Collider>();
                        if (playerCollider != null)
                        {
                            playerCollider.material = new PhysicsMaterial("PlayerPhysics");
                        }
                        break;
                    case "Enemy":
                        go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        go.name = name;
                        go.AddComponent<Rigidbody>();
                        break;
                    case "Particle System":
                        go = new GameObject(name);
                        go.AddComponent<ParticleSystem>();
                        break;
                    case "Audio Source":
                        go = new GameObject(name);
                        go.AddComponent<AudioSource>();
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown template: {template}");
                }

                if (parent != null)
                {
                    go.transform.SetParent(parent.transform, false);
                }

                // Apply transform properties
                if (payload.ContainsKey("position"))
                {
                    var posDict = payload["position"] as Dictionary<string, object>;
                    if (posDict != null)
                    {
                        go.transform.position = new Vector3(
                            GetFloat(posDict, "x") ?? 0,
                            GetFloat(posDict, "y") ?? 0,
                            GetFloat(posDict, "z") ?? 0
                        );
                    }
                }

                if (payload.ContainsKey("rotation"))
                {
                    var rotDict = payload["rotation"] as Dictionary<string, object>;
                    if (rotDict != null)
                    {
                        go.transform.eulerAngles = new Vector3(
                            GetFloat(rotDict, "x") ?? 0,
                            GetFloat(rotDict, "y") ?? 0,
                            GetFloat(rotDict, "z") ?? 0
                        );
                    }
                }

                if (payload.ContainsKey("scale"))
                {
                    var scaleDict = payload["scale"] as Dictionary<string, object>;
                    if (scaleDict != null)
                    {
                        go.transform.localScale = new Vector3(
                            GetFloat(scaleDict, "x") ?? 1,
                            GetFloat(scaleDict, "y") ?? 1,
                            GetFloat(scaleDict, "z") ?? 1
                        );
                    }
                }

                Undo.RegisterCreatedObjectUndo(go, $"Create {template}");
                Selection.activeGameObject = go;

                return new Dictionary<string, object>
                {
                    ["template"] = template,
                    ["gameObjectPath"] = GetHierarchyPath(go),
                    ["name"] = go.name,
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[gameObjectCreateFromTemplate] Error: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Inspects the current scene context including hierarchy, GameObjects, and components.
        /// Provides a comprehensive overview for Claude to understand the current state.
        /// </summary>
        /// <param name="payload">Options for what to include in the inspection.</param>
        /// <returns>Result dictionary with scene context information.</returns>
        private static object HandleContextInspect(Dictionary<string, object> payload)
        {
            try
            {
                var includeHierarchy = GetBool(payload, "includeHierarchy", true);
                var includeComponents = GetBool(payload, "includeComponents", false);
                var filter = GetString(payload, "filter");

                Debug.Log($"[contextInspect] Inspecting scene context");

                var result = new Dictionary<string, object>
                {
                    ["sceneName"] = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                    ["scenePath"] = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,
                };

                if (includeHierarchy)
                {
                    var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
                    var hierarchy = new List<object>();

                    foreach (var root in rootObjects)
                    {
                        if (!string.IsNullOrEmpty(filter))
                        {
                            if (!McpWildcardUtility.IsMatch(root.name, filter, false))
                            {
                                continue;
                            }
                        }

                        hierarchy.Add(BuildHierarchyInfo(root, includeComponents, filter));
                    }

                    result["hierarchy"] = hierarchy;
                    result["rootObjectCount"] = rootObjects.Length;
                }

                // Add scene statistics
                var allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
                result["totalGameObjects"] = allObjects.Length;

                var cameras = UnityEngine.Object.FindObjectsOfType<Camera>();
                result["cameraCount"] = cameras.Length;

                var lights = UnityEngine.Object.FindObjectsOfType<Light>();
                result["lightCount"] = lights.Length;

                var canvases = UnityEngine.Object.FindObjectsOfType<Canvas>();
                result["canvasCount"] = canvases.Length;

                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[contextInspect] Error: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        private static Dictionary<string, object> BuildHierarchyInfo(GameObject go, bool includeComponents, string filter)
        {
            var info = new Dictionary<string, object>
            {
                ["name"] = go.name,
                ["path"] = GetHierarchyPath(go),
                ["active"] = go.activeSelf,
                ["childCount"] = go.transform.childCount,
            };

            if (includeComponents)
            {
                var components = new List<string>();
                foreach (var comp in go.GetComponents<Component>())
                {
                    if (comp != null)
                    {
                        components.Add(comp.GetType().Name);
                    }
                }
                info["components"] = components;
            }

            // Always include direct child names (one level only)
            if (go.transform.childCount > 0)
            {
                var childNames = new List<string>();
                for (int i = 0; i < go.transform.childCount; i++)
                {
                    var child = go.transform.GetChild(i).gameObject;

                    if (!string.IsNullOrEmpty(filter))
                    {
                        if (!McpWildcardUtility.IsMatch(child.name, filter, false))
                        {
                            continue;
                        }
                    }

                    childNames.Add(child.name);
                }
                if (childNames.Count > 0)
                {
                    info["childNames"] = childNames;
                }
            }

            return info;
        }

        /// <summary>
        /// Handles tag and layer management operations.
        /// Supports setting/getting tags and layers on GameObjects,
        /// and adding/removing tags and layers from the project.
        /// </summary>
        /// <param name="payload">Operation parameters including 'operation' type.</param>
        /// <returns>Result dictionary with operation-specific data.</returns>

        // NOTE: Settings management operations have been moved to Settings/McpCommandProcessor.Settings.cs
        // This includes: HandleTagLayerManage, HandleProjectSettingsManage, HandleRenderPipelineManage,
        // HandleConstantConvert, and all related settings management methods.

        private static object HandleDesignPatternGenerate(Dictionary<string, object> payload)
        {
            // Check if compilation is in progress and wait if necessary
            var compilationWaitInfo = EnsureNoCompilationInProgress("designPatternGenerate", maxWaitSeconds: 30f);

            var patternType = GetString(payload, "patternType");
            var className = GetString(payload, "className");
            var namespaceName = GetString(payload, "namespace");
            var scriptPath = GetString(payload, "scriptPath");
            var options = payload.ContainsKey("options") && payload["options"] is Dictionary<string, object> opts
                ? opts
                : new Dictionary<string, object>();

            if (string.IsNullOrEmpty(patternType))
            {
                throw new InvalidOperationException("patternType is required");
            }

            if (string.IsNullOrEmpty(className))
            {
                throw new InvalidOperationException("className is required");
            }

            if (string.IsNullOrEmpty(scriptPath))
            {
                throw new InvalidOperationException("scriptPath is required");
            }

            // Validate script path
            if (!scriptPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("scriptPath must start with 'Assets/'");
            }

            if (!scriptPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("scriptPath must end with '.cs'");
            }

            // Generate the code
            string code;
            try
            {
                code = PatternTemplates.GeneratePattern(patternType, className, namespaceName, options);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to generate pattern code: {ex.Message}", ex);
            }

            // Create directory if it doesn't exist
            string directory = Path.GetDirectoryName(scriptPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Write the file
            File.WriteAllText(scriptPath, code);

            // Refresh AssetDatabase
            AssetDatabase.Refresh();

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["scriptPath"] = scriptPath,
                ["patternType"] = patternType,
                ["className"] = className,
                ["code"] = code,
                ["message"] = $"Successfully generated {patternType} pattern for class {className}"
            };
        }

        /// <summary>
        /// Handles script template generation for MonoBehaviour and ScriptableObject.
        /// </summary>
        /// <param name="payload">Operation parameters including template type, class name, script path, and optional namespace.</param>
        /// <returns>Result dictionary with generated code and file path.</returns>
        private static object HandleScriptTemplateGenerate(Dictionary<string, object> payload)
        {
            var templateType = GetString(payload, "templateType");
            var className = GetString(payload, "className");
            var scriptPath = GetString(payload, "scriptPath");
            var namespaceName = GetString(payload, "namespace");

            if (string.IsNullOrEmpty(templateType))
            {
                throw new InvalidOperationException("templateType is required");
            }

            if (string.IsNullOrEmpty(className))
            {
                throw new InvalidOperationException("className is required");
            }

            if (string.IsNullOrEmpty(scriptPath))
            {
                throw new InvalidOperationException("scriptPath is required");
            }

            // Validate script path
            if (!scriptPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("scriptPath must start with 'Assets/'");
            }

            if (!scriptPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("scriptPath must end with '.cs'");
            }

            // Generate template code
            string code;
            if (templateType == "MonoBehaviour")
            {
                code = GenerateMonoBehaviourTemplate(className, namespaceName);
            }
            else if (templateType == "ScriptableObject")
            {
                code = GenerateScriptableObjectTemplate(className, namespaceName);
            }
            else
            {
                throw new InvalidOperationException($"Unknown templateType: {templateType}. Supported types: MonoBehaviour, ScriptableObject");
            }

            // Create directory if it doesn't exist
            string directory = Path.GetDirectoryName(scriptPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Write the file
            File.WriteAllText(scriptPath, code);

            // Refresh AssetDatabase
            AssetDatabase.Refresh();

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["scriptPath"] = scriptPath,
                ["templateType"] = templateType,
                ["className"] = className,
                ["code"] = code,
                ["message"] = $"Successfully generated {templateType} template for class {className}"
            };
        }

        /// <summary>
        /// Generates a MonoBehaviour template with standard Unity lifecycle methods.
        /// </summary>
        private static string GenerateMonoBehaviourTemplate(string className, string namespaceName)
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();

            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.AppendLine($"namespace {namespaceName}");
                sb.AppendLine("{");
            }

            string indent = string.IsNullOrEmpty(namespaceName) ? "" : "    ";

            sb.AppendLine($"{indent}public class {className} : MonoBehaviour");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}    void Awake()");
            sb.AppendLine($"{indent}    {{");
            sb.AppendLine($"{indent}        ");
            sb.AppendLine($"{indent}    }}");
            sb.AppendLine();
            sb.AppendLine($"{indent}    void Start()");
            sb.AppendLine($"{indent}    {{");
            sb.AppendLine($"{indent}        ");
            sb.AppendLine($"{indent}    }}");
            sb.AppendLine();
            sb.AppendLine($"{indent}    void Update()");
            sb.AppendLine($"{indent}    {{");
            sb.AppendLine($"{indent}        ");
            sb.AppendLine($"{indent}    }}");
            sb.AppendLine();
            sb.AppendLine($"{indent}    void OnDestroy()");
            sb.AppendLine($"{indent}    {{");
            sb.AppendLine($"{indent}        ");
            sb.AppendLine($"{indent}    }}");
            sb.AppendLine($"{indent}}}");

            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.AppendLine("}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generates a ScriptableObject template with CreateAssetMenu attribute.
        /// </summary>
        private static string GenerateScriptableObjectTemplate(string className, string namespaceName)
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();

            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.AppendLine($"namespace {namespaceName}");
                sb.AppendLine("{");
            }

            string indent = string.IsNullOrEmpty(namespaceName) ? "" : "    ";

            sb.AppendLine($"{indent}[CreateAssetMenu(fileName = \"{className}\", menuName = \"ScriptableObjects/{className}\")]");
            sb.AppendLine($"{indent}public class {className} : ScriptableObject");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}    // Add your fields here");
            sb.AppendLine($"{indent}    ");
            sb.AppendLine($"{indent}}}");

            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.AppendLine("}");
            }

            return sb.ToString();
        }


        /// <summary>
        /// Detects if compilation has started after script operations.
        /// Waits for a short period and checks if EditorApplication.isCompiling becomes true.
        /// </summary>
        /// <param name="wasCompilingBefore">Whether compilation was already running before operations.</param>
        /// <param name="maxWaitSeconds">Maximum time to wait for compilation to start.</param>
        /// <returns>True if compilation started, false otherwise.</returns>
        private static bool DetectCompilationStart(bool wasCompilingBefore, float maxWaitSeconds = 1.5f)
        {
            // If already compiling, compilation was triggered
            if (wasCompilingBefore)
            {
                return true;
            }

            // Wait and check if compilation starts
            var startTime = EditorApplication.timeSinceStartup;
            var checkInterval = 0.1f; // Check every 100ms

            while ((EditorApplication.timeSinceStartup - startTime) < maxWaitSeconds)
            {
                if (EditorApplication.isCompiling)
                {
                    Debug.Log("MCP Bridge: Compilation detected after script operations");
                    return true;
                }

                // Small delay before next check
                System.Threading.Thread.Sleep((int)(checkInterval * 1000));
            }

            Debug.Log("MCP Bridge: No compilation detected after script operations");
            return false;
        }

        /// <summary>
        /// Waits for ongoing compilation to complete.
        /// Returns immediately if not compiling.
        /// </summary>
        /// <param name="maxWaitSeconds">Maximum time to wait for compilation to complete.</param>
        /// <returns>True if compilation completed successfully, false if timeout or still compiling.</returns>
        private static bool WaitForCompilationComplete(float maxWaitSeconds = 30f)
        {
            if (!EditorApplication.isCompiling)
            {
                return true; // Not compiling, return immediately
            }

            Debug.Log($"MCP Bridge: Waiting for ongoing compilation to complete (max {maxWaitSeconds}s)...");
            var startTime = EditorApplication.timeSinceStartup;
            var checkInterval = 0.2f; // Check every 200ms

            while ((EditorApplication.timeSinceStartup - startTime) < maxWaitSeconds)
            {
                if (!EditorApplication.isCompiling)
                {
                    var elapsedSeconds = EditorApplication.timeSinceStartup - startTime;
                    Debug.Log($"MCP Bridge: Compilation completed after {elapsedSeconds:F1}s");
                    return true;
                }

                // Small delay before next check
                System.Threading.Thread.Sleep((int)(checkInterval * 1000));
            }

            Debug.LogWarning($"MCP Bridge: Compilation did not complete within {maxWaitSeconds}s");
            return false;
        }

        /// <summary>
        /// Ensures no compilation is in progress before executing a tool operation.
        /// If compilation is in progress, waits for it to complete.
        /// </summary>
        /// <param name="toolName">Name of the tool being executed (for logging).</param>
        /// <param name="maxWaitSeconds">Maximum time to wait for compilation to complete.</param>
        /// <returns>Dictionary with status information if waiting occurred, null if no wait was needed.</returns>
        private static Dictionary<string, object> EnsureNoCompilationInProgress(string toolName, float maxWaitSeconds = 30f)
        {
            if (!EditorApplication.isCompiling)
            {
                return null; // No compilation in progress
            }

            Debug.Log($"MCP Bridge: Tool '{toolName}' called during compilation, waiting for completion...");
            var startTime = EditorApplication.timeSinceStartup;
            var completed = WaitForCompilationComplete(maxWaitSeconds);
            var elapsedSeconds = EditorApplication.timeSinceStartup - startTime;

            return new Dictionary<string, object>
            {
                ["waitedForCompilation"] = true,
                ["compilationCompleted"] = completed,
                ["waitTimeSeconds"] = (float)Math.Round(elapsedSeconds, 2),
                ["message"] = completed
                    ? $"Waited {elapsedSeconds:F1}s for ongoing compilation to complete"
                    : $"Compilation did not complete within {maxWaitSeconds}s"
            };
        }

        #region Template Management

        /// <summary>
        /// Handles template management operations for customizing existing GameObjects.
        /// Supports operations: customize (add components/children to existing object), convertToPrefab (save as prefab).
        /// </summary>
        /// <param name="payload">Operation parameters including 'operation' type.</param>
        /// <returns>Result dictionary with operation-specific data.</returns>
        private static object HandleTemplateManage(Dictionary<string, object> payload)
        {
            var operation = GetString(payload, "operation");
            if (string.IsNullOrEmpty(operation))
            {
                throw new InvalidOperationException("operation is required");
            }

            // Check if compilation is in progress and wait if necessary
            var compilationWaitInfo = EnsureNoCompilationInProgress("templateManage", maxWaitSeconds: 30f);

            object result;
            switch (operation)
            {
                case "customize":
                    result = CustomizeGameObject(payload);
                    break;
                case "convertToPrefab":
                    result = ConvertGameObjectToPrefab(payload);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown operation: {operation}");
            }

            // Add compilation wait info if present
            if (compilationWaitInfo != null && result is Dictionary<string, object> resultDict)
            {
                resultDict["compilationWaitInfo"] = compilationWaitInfo;
            }

            return result;
        }

        /// <summary>
        /// Customizes an existing GameObject by adding components and child objects.
        /// </summary>
        private static object CustomizeGameObject(Dictionary<string, object> payload)
        {
            try
            {
                var gameObjectPath = GetString(payload, "gameObjectPath");
                if (string.IsNullOrEmpty(gameObjectPath))
                {
                    throw new InvalidOperationException("gameObjectPath is required");
                }

                var targetObject = ResolveGameObject(gameObjectPath);
                if (targetObject == null)
                {
                    throw new InvalidOperationException($"GameObject not found: {gameObjectPath}");
                }

                Debug.Log($"[templateManage:customize] Customizing GameObject: {gameObjectPath}");

                var addedComponents = new List<string>();
                var addedChildren = new List<string>();

                // Add components if specified
                if (payload.TryGetValue("components", out var componentsObj) && componentsObj is List<object> componentsList)
                {
                    foreach (var compObj in componentsList)
                    {
                        if (compObj is Dictionary<string, object> compDict)
                        {
                            var componentType = GetString(compDict, "type");
                            if (string.IsNullOrEmpty(componentType))
                            {
                                Debug.LogWarning("[templateManage:customize] Component type is required, skipping");
                                continue;
                            }

                            // Try to find the component type
                            Type type = null;
                            try
                            {
                                type = ResolveType(componentType);
                            }
                            catch (InvalidOperationException)
                            {
                                Debug.LogWarning($"[templateManage:customize] Component type not found: {componentType}, skipping");
                                continue;
                            }

                            // Check if component already exists (unless allowDuplicates is true)
                            var allowDuplicates = GetBool(compDict, "allowDuplicates", false);
                            if (!allowDuplicates && targetObject.GetComponent(type) != null)
                            {
                                Debug.LogWarning($"[templateManage:customize] Component {componentType} already exists on {gameObjectPath}, skipping");
                                continue;
                            }

                            // Add the component
                            var component = targetObject.AddComponent(type);
                            addedComponents.Add(componentType);

                            // Apply properties if specified
                            if (compDict.TryGetValue("properties", out var propsObj) && propsObj is Dictionary<string, object> properties)
                            {
                                foreach (var kvp in properties)
                                {
                                    ApplyProperty(component, kvp.Key, kvp.Value);
                                }
                            }

                            Debug.Log($"[templateManage:customize] Added component: {componentType}");
                        }
                    }
                }

                // Add child objects if specified
                if (payload.TryGetValue("children", out var childrenObj) && childrenObj is List<object> childrenList)
                {
                    foreach (var childObj in childrenList)
                    {
                        if (childObj is Dictionary<string, object> childDict)
                        {
                            var childName = GetString(childDict, "name");
                            if (string.IsNullOrEmpty(childName))
                            {
                                Debug.LogWarning("[templateManage:customize] Child name is required, skipping");
                                continue;
                            }

                            // Determine if this is a UI object
                            var isUI = GetBool(childDict, "isUI", false);

                            // Create child GameObject
                            GameObject childGo;
                            if (isUI)
                            {
                                childGo = new GameObject(childName, typeof(RectTransform));
                            }
                            else
                            {
                                childGo = new GameObject(childName);
                            }

                            childGo.transform.SetParent(targetObject.transform, false);

                            // Add components to child if specified
                            if (childDict.TryGetValue("components", out var childComponentsObj) && childComponentsObj is List<object> childComponentsList)
                            {
                                foreach (var compObj in childComponentsList)
                                {
                                    if (compObj is Dictionary<string, object> compDict)
                                    {
                                        var componentType = GetString(compDict, "type");
                                        if (!string.IsNullOrEmpty(componentType))
                                        {
                                            try
                                            {
                                                var type = ResolveType(componentType);
                                                var component = childGo.AddComponent(type);

                                                // Apply properties if specified
                                                if (compDict.TryGetValue("properties", out var propsObj) && propsObj is Dictionary<string, object> properties)
                                                {
                                                    foreach (var kvp in properties)
                                                    {
                                                        ApplyProperty(component, kvp.Key, kvp.Value);
                                                    }
                                                }
                                            }
                                            catch (InvalidOperationException ex)
                                            {
                                                Debug.LogWarning($"[templateManage:customize] Component type not found: {componentType}, error: {ex.Message}");
                                            }
                                        }
                                    }
                                }
                            }

                            // Apply transform properties
                            if (childDict.TryGetValue("position", out var posObj) && posObj is Dictionary<string, object> posDict)
                            {
                                childGo.transform.localPosition = new Vector3(
                                    GetFloat(posDict, "x") ?? 0,
                                    GetFloat(posDict, "y") ?? 0,
                                    GetFloat(posDict, "z") ?? 0
                                );
                            }

                            if (childDict.TryGetValue("rotation", out var rotObj) && rotObj is Dictionary<string, object> rotDict)
                            {
                                childGo.transform.localEulerAngles = new Vector3(
                                    GetFloat(rotDict, "x") ?? 0,
                                    GetFloat(rotDict, "y") ?? 0,
                                    GetFloat(rotDict, "z") ?? 0
                                );
                            }

                            if (childDict.TryGetValue("scale", out var scaleObj) && scaleObj is Dictionary<string, object> scaleDict)
                            {
                                childGo.transform.localScale = new Vector3(
                                    GetFloat(scaleDict, "x") ?? 1,
                                    GetFloat(scaleDict, "y") ?? 1,
                                    GetFloat(scaleDict, "z") ?? 1
                                );
                            }

                            addedChildren.Add(childName);
                            Debug.Log($"[templateManage:customize] Added child: {childName}");

                            // Register undo
                            Undo.RegisterCreatedObjectUndo(childGo, $"Create child {childName}");
                        }
                    }
                }

                // Register undo for component additions
                if (addedComponents.Count > 0)
                {
                    Undo.RegisterCompleteObjectUndo(targetObject, "Customize GameObject");
                }

                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["gameObjectPath"] = gameObjectPath,
                    ["addedComponents"] = addedComponents,
                    ["addedChildren"] = addedChildren,
                    ["message"] = $"Customized {gameObjectPath}: added {addedComponents.Count} components and {addedChildren.Count} children"
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[templateManage:customize] Error: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Converts a GameObject to a prefab and saves it at the specified path.
        /// </summary>
        private static object ConvertGameObjectToPrefab(Dictionary<string, object> payload)
        {
            try
            {
                var gameObjectPath = GetString(payload, "gameObjectPath");
                if (string.IsNullOrEmpty(gameObjectPath))
                {
                    throw new InvalidOperationException("gameObjectPath is required");
                }

                var prefabPath = GetString(payload, "prefabPath");
                if (string.IsNullOrEmpty(prefabPath))
                {
                    throw new InvalidOperationException("prefabPath is required");
                }

                if (!prefabPath.StartsWith("Assets/"))
                {
                    throw new InvalidOperationException("prefabPath must start with 'Assets/'");
                }

                if (!prefabPath.EndsWith(".prefab"))
                {
                    prefabPath += ".prefab";
                }

                var targetObject = ResolveGameObject(gameObjectPath);
                if (targetObject == null)
                {
                    throw new InvalidOperationException($"GameObject not found: {gameObjectPath}");
                }

                Debug.Log($"[templateManage:convertToPrefab] Converting {gameObjectPath} to prefab: {prefabPath}");

                // Ensure directory exists
                var directory = Path.GetDirectoryName(prefabPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Check if prefab already exists
                var overwrite = GetBool(payload, "overwrite", false);
                if (File.Exists(prefabPath) && !overwrite)
                {
                    throw new InvalidOperationException($"Prefab already exists at {prefabPath}. Set 'overwrite' to true to replace it.");
                }

                // Create the prefab
                var prefab = PrefabUtility.SaveAsPrefabAsset(targetObject, prefabPath);
                if (prefab == null)
                {
                    throw new InvalidOperationException($"Failed to create prefab at {prefabPath}");
                }

                AssetDatabase.Refresh();

                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["gameObjectPath"] = gameObjectPath,
                    ["prefabPath"] = prefabPath,
                    ["message"] = $"Successfully converted {gameObjectPath} to prefab at {prefabPath}"
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[templateManage:convertToPrefab] Error: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        #endregion

        #region Menu Hierarchy Management

        /// <summary>
        /// Creates a hierarchical menu system with nested submenus and optional State pattern navigation.
        /// </summary>
        private static object HandleMenuHierarchyCreate(Dictionary<string, object> payload)
        {
            try
            {
                var menuName = EnsureValue(GetString(payload, "menuName"), "menuName");

                if (!payload.ContainsKey("menuStructure") || !(payload["menuStructure"] is Dictionary<string, object>))
                {
                    throw new InvalidOperationException("menuStructure is required and must be a dictionary");
                }
                var menuStructure = (Dictionary<string, object>)payload["menuStructure"];
                var generateStateMachine = GetBool(payload, "generateStateMachine", defaultValue: true);
                var stateMachineScriptPath = GetString(payload, "stateMachineScriptPath");
                var navigationMode = GetString(payload, "navigationMode") ?? "both";
                var buttonWidth = GetFloat(payload, "buttonWidth", defaultValue: 200f);
                var buttonHeight = GetFloat(payload, "buttonHeight", defaultValue: 50f);
                var spacing = GetFloat(payload, "spacing", defaultValue: 10f);
                var enableBackNavigation = GetBool(payload, "enableBackNavigation", defaultValue: true);

                // Validate navigation mode
                if (navigationMode != "keyboard" && navigationMode != "gamepad" && navigationMode != "both")
                {
                    navigationMode = "both";
                }

                // Ensure Canvas exists
                var canvas = GameObject.FindFirstObjectByType<Canvas>();
                if (canvas == null)
                {
                    throw new InvalidOperationException("No Canvas found in the scene. Create a Canvas first using unity_scene_quickSetup with setupType='UI' or unity_ugui_createFromTemplate.");
                }

                // Create root menu container
                var rootMenu = new GameObject(menuName);
                rootMenu.transform.SetParent(canvas.transform, false);

                var rootRect = rootMenu.AddComponent<RectTransform>();
                rootRect.anchorMin = Vector2.zero;
                rootRect.anchorMax = Vector2.one;
                rootRect.sizeDelta = Vector2.zero;
                rootRect.anchoredPosition = Vector2.zero;

                // Add CanvasGroup for showing/hiding menu
                var canvasGroup = rootMenu.AddComponent<CanvasGroup>();

                var createdMenus = new List<string>();
                var menuStates = new List<string>();

                // Build menu hierarchy recursively
                BuildMenuLevel(rootMenu, menuStructure, buttonWidth, buttonHeight, spacing, enableBackNavigation, createdMenus, menuStates, null);

                // Generate State Machine script if requested
                string generatedScriptPath = null;
                if (generateStateMachine && !string.IsNullOrEmpty(stateMachineScriptPath))
                {
                    generatedScriptPath = GenerateMenuStateMachineScript(stateMachineScriptPath, menuStates, navigationMode);
                }

                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["menuName"] = menuName,
                    ["menuPath"] = $"{canvas.name}/{menuName}",
                    ["createdMenus"] = createdMenus,
                    ["menuStateCount"] = menuStates.Count,
                    ["stateMachineGenerated"] = generateStateMachine && !string.IsNullOrEmpty(generatedScriptPath),
                    ["stateMachineScriptPath"] = generatedScriptPath,
                    ["message"] = $"Successfully created hierarchical menu '{menuName}' with {createdMenus.Count} menu panels"
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[menuHierarchyCreate] Error: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Recursively builds menu levels with buttons and submenus.
        /// </summary>
        private static void BuildMenuLevel(
            GameObject parentMenuObject,
            Dictionary<string, object> menuItems,
            float buttonWidth,
            float buttonHeight,
            float spacing,
            bool enableBackNavigation,
            List<string> createdMenus,
            List<string> menuStates,
            string parentMenuName)
        {
            // Add vertical layout group to parent menu
            var layoutGroup = parentMenuObject.GetComponent<VerticalLayoutGroup>();
            if (layoutGroup == null)
            {
                layoutGroup = parentMenuObject.AddComponent<VerticalLayoutGroup>();
            }
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.spacing = spacing;
            layoutGroup.padding = new RectOffset(20, 20, 20, 20);
            layoutGroup.childAlignment = TextAnchor.UpperCenter;

            // Add Content Size Fitter
            var sizeFitter = parentMenuObject.GetComponent<ContentSizeFitter>();
            if (sizeFitter == null)
            {
                sizeFitter = parentMenuObject.AddComponent<ContentSizeFitter>();
            }
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            createdMenus.Add(parentMenuObject.name);
            menuStates.Add(parentMenuObject.name + "State");

            // Add back button if this is a submenu
            if (!string.IsNullOrEmpty(parentMenuName) && enableBackNavigation)
            {
                CreateMenuButton(parentMenuObject, "Back", buttonWidth, buttonHeight);
            }

            // Iterate through menu items
            foreach (var item in menuItems)
            {
                var itemName = item.Key;
                var itemValue = item.Value;

                // Check if this item has submenus
                Dictionary<string, object> submenus = null;
                string buttonText = itemName;

                if (itemValue is string strValue)
                {
                    // Simple button with text
                    buttonText = strValue;
                }
                else if (itemValue is Dictionary<string, object> dict)
                {
                    // Complex item with text and submenus
                    if (dict.ContainsKey("text"))
                    {
                        buttonText = GetString(dict, "text") ?? itemName;
                    }
                    if (dict.ContainsKey("submenus") && dict["submenus"] is Dictionary<string, object> submenuDict)
                    {
                        submenus = submenuDict;
                    }
                }
                else if (itemValue is List<object> list)
                {
                    // List of submenu items - convert to dictionary
                    submenus = new Dictionary<string, object>();
                    for (int i = 0; i < list.Count; i++)
                    {
                        var listItem = list[i];
                        if (listItem is string strItem)
                        {
                            submenus[$"Option{i + 1}"] = strItem;
                        }
                        else if (listItem is Dictionary<string, object> dictItem)
                        {
                            var itemKey = GetString(dictItem, "name") ?? $"Option{i + 1}";
                            submenus[itemKey] = dictItem;
                        }
                    }
                }

                // Create button for this item
                CreateMenuButton(parentMenuObject, buttonText, buttonWidth, buttonHeight);

                // If there are submenus, create a submenu panel
                if (submenus != null && submenus.Count > 0)
                {
                    var submenuPanel = new GameObject(itemName + "Menu");
                    submenuPanel.transform.SetParent(parentMenuObject.transform.parent, false);

                    var submenuRect = submenuPanel.AddComponent<RectTransform>();
                    submenuRect.anchorMin = Vector2.zero;
                    submenuRect.anchorMax = Vector2.one;
                    submenuRect.sizeDelta = Vector2.zero;
                    submenuRect.anchoredPosition = Vector2.zero;

                    // Add CanvasGroup for showing/hiding
                    var submenuCanvasGroup = submenuPanel.AddComponent<CanvasGroup>();
                    submenuCanvasGroup.alpha = 0;
                    submenuCanvasGroup.interactable = false;
                    submenuCanvasGroup.blocksRaycasts = false;

                    // Recursively build submenu
                    BuildMenuLevel(submenuPanel, submenus, buttonWidth, buttonHeight, spacing, enableBackNavigation, createdMenus, menuStates, parentMenuObject.name);
                }
            }
        }

        /// <summary>
        /// Creates a UI button for a menu item.
        /// </summary>
        private static GameObject CreateMenuButton(GameObject parent, string text, float width, float height)
        {
            var buttonObj = new GameObject(text + "Button");
            buttonObj.transform.SetParent(parent.transform, false);

            var buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(width, height);

            var buttonImage = buttonObj.AddComponent<UnityEngine.UI.Image>();
            buttonImage.color = new Color(1f, 1f, 1f, 1f);

            var button = buttonObj.AddComponent<UnityEngine.UI.Button>();

            // Create text child
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);

            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;

            var textComponent = textObj.AddComponent<UnityEngine.UI.Text>();
            textComponent.text = text;
            textComponent.alignment = TextAnchor.MiddleCenter;
            textComponent.color = Color.black;
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComponent.fontSize = 14;

            return buttonObj;
        }

        /// <summary>
        /// Generates a MenuStateMachine script using the State pattern.
        /// </summary>
        private static string GenerateMenuStateMachineScript(string scriptPath, List<string> menuStates, string navigationMode)
        {
            if (string.IsNullOrEmpty(scriptPath) || !scriptPath.StartsWith("Assets/") || !scriptPath.EndsWith(".cs"))
            {
                throw new InvalidOperationException("Invalid script path. Must start with 'Assets/' and end with '.cs'");
            }

            // Generate namespace from folder structure
            var directory = Path.GetDirectoryName(scriptPath).Replace("\\", "/");
            var namespaceName = directory.Replace("Assets/", "").Replace("/", ".");
            var className = Path.GetFileNameWithoutExtension(scriptPath);

            var keyboardControls = navigationMode == "keyboard" || navigationMode == "both";
            var gamepadControls = navigationMode == "gamepad" || navigationMode == "both";

            var scriptContent = $@"using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

{(string.IsNullOrEmpty(namespaceName) ? "" : $"namespace {namespaceName}\n{{")}

    /// <summary>
    /// Menu navigation system using the State pattern.
    /// Manages menu transitions and input handling.
    /// </summary>
    public class {className} : MonoBehaviour
    {{
        [Header(""Menu References"")]
        [SerializeField] private List<CanvasGroup> menuPanels = new List<CanvasGroup>();

        [Header(""Navigation Settings"")]
        [SerializeField] private float transitionDuration = 0.3f;

        private IMenuState currentState;
        private Dictionary<string, CanvasGroup> menuDict = new Dictionary<string, CanvasGroup>();
        private int selectedButtonIndex = 0;
        private List<Button> currentButtons = new List<Button>();

        private void Start()
        {{
            // Initialize menu dictionary
            foreach (var panel in menuPanels)
            {{
                menuDict[panel.gameObject.name] = panel;
                HideMenu(panel);
            }}

            // Start with first menu
            if (menuPanels.Count > 0)
            {{
                ChangeState(menuPanels[0].gameObject.name);
            }}
        }}

        private void Update()
        {{
            currentState?.Update();
            HandleInput();
        }}

        private void HandleInput()
        {{
            if (currentButtons.Count == 0) return;

            int previousIndex = selectedButtonIndex;

            {(keyboardControls ? @"// Keyboard navigation
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                selectedButtonIndex = (selectedButtonIndex - 1 + currentButtons.Count) % currentButtons.Count;
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            {
                selectedButtonIndex = (selectedButtonIndex + 1) % currentButtons.Count;
            }
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                currentButtons[selectedButtonIndex].onClick.Invoke();
            }" : "")}

            {(gamepadControls ? @"// Gamepad navigation
            if (Input.GetKeyDown(KeyCode.Joystick1Button0) || Input.GetButtonDown(""Submit""))
            {
                currentButtons[selectedButtonIndex].onClick.Invoke();
            }

            float verticalInput = Input.GetAxis(""Vertical"");
            if (verticalInput > 0.5f)
            {
                selectedButtonIndex = (selectedButtonIndex - 1 + currentButtons.Count) % currentButtons.Count;
            }
            else if (verticalInput < -0.5f)
            {
                selectedButtonIndex = (selectedButtonIndex + 1) % currentButtons.Count;
            }" : "")}

            // Update visual feedback
            if (previousIndex != selectedButtonIndex)
            {{
                UpdateButtonHighlight();
            }}
        }}

        private void UpdateButtonHighlight()
        {{
            for (int i = 0; i < currentButtons.Count; i++)
            {{
                var colors = currentButtons[i].colors;
                if (i == selectedButtonIndex)
                {{
                    currentButtons[i].Select();
                }}
            }}
        }}

        public void ChangeState(string menuName)
        {{
            if (!menuDict.ContainsKey(menuName))
            {{
                Debug.LogWarning($""Menu '{{menuName}}' not found!"");
                return;
            }}

            currentState?.Exit();

            // Hide all menus
            foreach (var panel in menuPanels)
            {{
                HideMenu(panel);
            }}

            // Show target menu
            ShowMenu(menuDict[menuName]);

            // Update current buttons
            currentButtons.Clear();
            currentButtons.AddRange(menuDict[menuName].GetComponentsInChildren<Button>());
            selectedButtonIndex = 0;
            UpdateButtonHighlight();

            // Set new state
            currentState = new MenuState(menuName, this);
            currentState.Enter();
        }}

        private void ShowMenu(CanvasGroup panel)
        {{
            panel.alpha = 1;
            panel.interactable = true;
            panel.blocksRaycasts = true;
        }}

        private void HideMenu(CanvasGroup panel)
        {{
            panel.alpha = 0;
            panel.interactable = false;
            panel.blocksRaycasts = false;
        }}

        // Menu state interface
        private interface IMenuState
        {{
            void Enter();
            void Update();
            void Exit();
        }}

        // Concrete menu state
        private class MenuState : IMenuState
        {{
            private string menuName;
            private {className} manager;

            public MenuState(string menuName, {className} manager)
            {{
                this.menuName = menuName;
                this.manager = manager;
            }}

            public void Enter()
            {{
                Debug.Log($""Entered {{menuName}}"");
            }}

            public void Update()
            {{
                // State-specific update logic can go here
            }}

            public void Exit()
            {{
                Debug.Log($""Exited {{menuName}}"");
            }}
        }}
    }}
{(string.IsNullOrEmpty(namespaceName) ? "" : "}")}
";

            // Ensure directory exists
            var directoryPath = Path.GetDirectoryName(scriptPath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // Write script file
            File.WriteAllText(scriptPath, scriptContent);
            AssetDatabase.Refresh();

            Debug.Log($"[menuHierarchyCreate] Generated MenuStateMachine script at: {scriptPath}");

            return scriptPath;
        }

        #endregion
    }
}

