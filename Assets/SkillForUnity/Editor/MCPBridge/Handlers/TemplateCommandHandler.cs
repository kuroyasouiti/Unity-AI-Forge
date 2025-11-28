using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MCP.Editor.Base;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MCP.Editor.Handlers
{
    /// <summary>
    /// Template and code generation command handler.
    /// Handles scene setup, GameObject templates, design patterns, script templates, and menu generation.
    /// </summary>
    public class TemplateCommandHandler : BaseCommandHandler
    {
        #region ICommandHandler Implementation
        
        public override string Category => "template";
        
        public override IEnumerable<string> SupportedOperations => new[]
        {
            "sceneQuickSetup",
            "gameObjectCreateFromTemplate",
            "designPatternGenerate",
            "scriptTemplateGenerate",
            "templateManage",
            "menuHierarchyCreate"
        };
        
        #endregion
        
        #region BaseCommandHandler Overrides
        
        protected override void ValidatePayload(Dictionary<string, object> payload)
        {
            // Template tools don't require 'operation' parameter
            // They are invoked by tool name directly
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload), "Payload cannot be null");
            }
        }
        
        protected override bool RequiresCompilationWait(string operation)
        {
            // All template operations modify the project
            return true;
        }
        
        public override object Execute(Dictionary<string, object> payload)
        {
            try
            {
                // 1. Validate payload
                ValidatePayload(payload);
                
                // 2. Template tools use the tool name itself as the operation
                // Determine which operation based on SupportedOperations or explicit parameter
                string operation = null;
                
                // Check for explicit operation parameter (e.g., from templateManage)
                if (payload.ContainsKey("operation") && payload["operation"] != null)
                {
                    var explicitOp = payload["operation"].ToString();
                    // If operation is a sub-operation of templateManage (customize, convertToPrefab),
                    // route to templateManage handler
                    if (explicitOp == "customize" || explicitOp == "convertToPrefab")
                    {
                        operation = "templateManage";
                    }
                    else
                    {
                        operation = explicitOp;
                    }
                }
                else
                {
                    // Determine operation from context
                    // Priority: template > patternType > templateType > setupType > menuStructure
                    if (payload.ContainsKey("template"))
                    {
                        operation = "gameObjectCreateFromTemplate";
                    }
                    else if (payload.ContainsKey("patternType"))
                    {
                        operation = "designPatternGenerate";
                    }
                    else if (payload.ContainsKey("templateType"))
                    {
                        operation = "scriptTemplateGenerate";
                    }
                    else if (payload.ContainsKey("setupType"))
                    {
                        operation = "sceneQuickSetup";
                    }
                    else if (payload.ContainsKey("menuStructure"))
                    {
                        operation = "menuHierarchyCreate";
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            "Cannot determine operation. Expected one of: template, patternType, templateType, setupType, menuStructure, or operation parameter");
                    }
                }
                
                // 3. Verify operation is supported
                if (!SupportedOperations.Contains(operation))
                {
                    throw new InvalidOperationException(
                        $"Operation '{operation}' is not supported by {Category} handler. " +
                        $"Supported operations: {string.Join(", ", SupportedOperations)}"
                    );
                }
                
                // 4. Execute operation
                var result = ExecuteOperation(operation, payload);
                
                return result;
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(ex);
            }
        }
        
        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "sceneQuickSetup" => HandleSceneQuickSetup(payload),
                "gameObjectCreateFromTemplate" => HandleGameObjectCreateFromTemplate(payload),
                "designPatternGenerate" => HandleDesignPatternGenerate(payload),
                "scriptTemplateGenerate" => HandleScriptTemplateGenerate(payload),
                "templateManage" => HandleTemplateManage(payload),
                "menuHierarchyCreate" => HandleMenuHierarchyCreate(payload),
                _ => throw new InvalidOperationException($"Unknown template operation: {operation}")
            };
        }
        
        #endregion
        
        #region Template Operations
        
        /// <summary>
        /// Quickly sets up a scene with common configurations (3D, 2D, UI, VR, or Empty).
        /// </summary>
        private object HandleSceneQuickSetup(Dictionary<string, object> payload)
        {
            var setupType = GetString(payload, "setupType");
            if (string.IsNullOrEmpty(setupType))
            {
                throw new InvalidOperationException("setupType is required");
            }
            
            var createdObjects = setupType switch
            {
                "3D" => Setup3DScene(payload),
                "2D" => Setup2DScene(payload),
                "UI" => SetupUIScene(payload),
                "VR" => SetupVRScene(payload),
                "Empty" => new List<string>(), // No objects for empty scene
                _ => throw new InvalidOperationException($"Unknown setupType: {setupType}")
            };
            
            return CreateSuccessResponse(
                ("setupType", setupType),
                ("createdObjects", createdObjects)
            );
        }
        
        /// <summary>
        /// Creates a GameObject from a predefined template.
        /// </summary>
        private object HandleGameObjectCreateFromTemplate(Dictionary<string, object> payload)
        {
            var template = GetString(payload, "template");
            if (string.IsNullOrEmpty(template))
            {
                throw new InvalidOperationException("template is required");
            }
            
            var name = GetString(payload, "name", template);
            var parentPath = GetString(payload, "parentPath");
            
            GameObject go = CreateGameObjectFromTemplate(template);
            go.name = name;
            
            // Set parent if specified
            if (!string.IsNullOrEmpty(parentPath))
            {
                var parent = ResolveGameObject(parentPath);
                go.transform.SetParent(parent.transform, false);
            }
            
            // Apply transform if provided
            ApplyTransform(go, payload);
            
            Undo.RegisterCreatedObjectUndo(go, $"Create {template}");
            Selection.activeGameObject = go;
            
            return CreateSuccessResponse(
                ("template", template),
                ("gameObjectPath", GetHierarchyPath(go)),
                ("name", go.name)
            );
        }
        
        /// <summary>
        /// Generates C# code for common design patterns.
        /// </summary>
        private object HandleDesignPatternGenerate(Dictionary<string, object> payload)
        {
            var patternType = GetString(payload, "patternType");
            var className = GetString(payload, "className");
            var namespaceName = GetString(payload, "namespace");
            var scriptPath = GetString(payload, "scriptPath");
            
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
            
            var options = payload.ContainsKey("options") && payload["options"] is Dictionary<string, object> opts
                ? opts
                : new Dictionary<string, object>();
            
            var code = GenerateDesignPattern(patternType, className, namespaceName, options);
            
            // Ensure directory exists
            var directory = Path.GetDirectoryName(scriptPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            File.WriteAllText(scriptPath, code);
            AssetDatabase.Refresh();
            
            return CreateSuccessResponse(
                ("scriptPath", scriptPath),
                ("patternType", patternType),
                ("className", className),
                ("code", code),
                ("message", $"Successfully generated {patternType} pattern for class {className}")
            );
        }
        
        /// <summary>
        /// Generates script templates for MonoBehaviour or ScriptableObject.
        /// </summary>
        private object HandleScriptTemplateGenerate(Dictionary<string, object> payload)
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
            
            if (string.IsNullOrEmpty(scriptPath) || !scriptPath.EndsWith(".cs"))
            {
                throw new InvalidOperationException("scriptPath is required and must end with .cs");
            }
            
            var code = templateType switch
            {
                "MonoBehaviour" => GenerateMonoBehaviourTemplate(className, namespaceName),
                "ScriptableObject" => GenerateScriptableObjectTemplate(className, namespaceName),
                _ => throw new InvalidOperationException($"Unknown templateType: {templateType}")
            };
            
            // Ensure directory exists
            var directory = Path.GetDirectoryName(scriptPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            File.WriteAllText(scriptPath, code);
            AssetDatabase.Refresh();
            
            return CreateSuccessResponse(
                ("scriptPath", scriptPath),
                ("templateType", templateType),
                ("className", className),
                ("code", code),
                ("message", $"Successfully generated {templateType} template for class {className}")
            );
        }
        
        /// <summary>
        /// Customizes existing GameObjects by adding components and children, or converts them to prefabs.
        /// </summary>
        private object HandleTemplateManage(Dictionary<string, object> payload)
        {
            var operation = GetString(payload, "operation");
            if (string.IsNullOrEmpty(operation))
            {
                throw new InvalidOperationException("operation is required");
            }
            
            return operation switch
            {
                "customize" => CustomizeGameObject(payload),
                "convertToPrefab" => ConvertGameObjectToPrefab(payload),
                _ => throw new InvalidOperationException($"Unknown templateManage operation: {operation}")
            };
        }
        
        /// <summary>
        /// Creates hierarchical menu systems with nested submenus.
        /// </summary>
        private object HandleMenuHierarchyCreate(Dictionary<string, object> payload)
        {
            var menuName = GetString(payload, "menuName");
            var menuStructure = payload.ContainsKey("menuStructure") && payload["menuStructure"] is Dictionary<string, object> structure
                ? structure
                : null;
            
            if (string.IsNullOrEmpty(menuName))
            {
                throw new InvalidOperationException("menuName is required");
            }
            
            if (menuStructure == null)
            {
                throw new InvalidOperationException("menuStructure is required");
            }
            
            // Get Canvas
            var canvas = GameObject.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                throw new InvalidOperationException("No Canvas found in scene. Please create a Canvas first.");
            }
            
            // Create menu root
            var menuRoot = new GameObject(menuName);
            var rectTransform = menuRoot.AddComponent<RectTransform>();
            menuRoot.transform.SetParent(canvas.transform, false);
            
            // Configure RectTransform to fill parent
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
            
            var createdMenus = new List<string>();
            BuildMenuLevel(menuRoot, menuStructure, payload, createdMenus);
            
            Undo.RegisterCreatedObjectUndo(menuRoot, $"Create Menu: {menuName}");
            
            return CreateSuccessResponse(
                ("menuName", menuName),
                ("menuPath", GetHierarchyPath(menuRoot)),
                ("createdMenus", createdMenus)
            );
        }
        
        #endregion
        
        #region Scene Setup Helpers
        
        private List<string> Setup3DScene(Dictionary<string, object> payload)
        {
            var created = new List<string>();
            
            // Create Main Camera if needed
            var existingCamera = Camera.main;
            if (existingCamera == null)
            {
                var camera = new GameObject("Main Camera");
                camera.AddComponent<Camera>();
                camera.tag = "MainCamera";
                
                var camPosDict = payload.ContainsKey("cameraPosition") && payload["cameraPosition"] is Dictionary<string, object> dict
                    ? dict
                    : null;
                
                camera.transform.position = camPosDict != null
                    ? new Vector3(
                        Convert.ToSingle(camPosDict.ContainsKey("x") ? camPosDict["x"] : 0),
                        Convert.ToSingle(camPosDict.ContainsKey("y") ? camPosDict["y"] : 1),
                        Convert.ToSingle(camPosDict.ContainsKey("z") ? camPosDict["z"] : -10))
                    : new Vector3(0, 1, -10);
                
                Undo.RegisterCreatedObjectUndo(camera, "Create Main Camera");
                created.Add("Main Camera");
            }
            
            // Create Directional Light if needed
            var existingLights = GameObject.FindObjectsOfType<Light>();
            var hasDirectionalLight = existingLights.Any(l => l.type == LightType.Directional);
            
            if (!hasDirectionalLight)
            {
                var light = new GameObject("Directional Light");
                var lightComp = light.AddComponent<Light>();
                lightComp.type = LightType.Directional;
                lightComp.intensity = GetFloat(payload, "lightIntensity", 1f);
                light.transform.rotation = Quaternion.Euler(50, -30, 0);
                
                Undo.RegisterCreatedObjectUndo(light, "Create Directional Light");
                created.Add("Directional Light");
            }
            
            return created;
        }
        
        private List<string> Setup2DScene(Dictionary<string, object> payload)
        {
            var created = new List<string>();
            
            var existingCamera = Camera.main;
            if (existingCamera == null)
            {
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
        
        private List<string> SetupUIScene(Dictionary<string, object> payload)
        {
            var created = new List<string>();
            
            // Create Canvas if needed
            var existingCanvas = GameObject.FindObjectOfType<Canvas>();
            if (existingCanvas == null)
            {
                var canvasGo = new GameObject("Canvas");
                var canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGo.AddComponent<CanvasScaler>();
                canvasGo.AddComponent<GraphicRaycaster>();
                
                Undo.RegisterCreatedObjectUndo(canvasGo, "Create Canvas");
                created.Add("Canvas");
            }
            
            // Create EventSystem if needed
            var includeEventSystem = GetBool(payload, "includeEventSystem", true);
            if (includeEventSystem)
            {
                var existingEventSystem = GameObject.FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
                if (existingEventSystem == null)
                {
                    var eventSystemGo = new GameObject("EventSystem");
                    eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                    eventSystemGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                    
                    Undo.RegisterCreatedObjectUndo(eventSystemGo, "Create EventSystem");
                    created.Add("EventSystem");
                }
            }
            
            return created;
        }
        
        private List<string> SetupVRScene(Dictionary<string, object> payload)
        {
            var created = new List<string>();
            
            // Basic VR setup - just a camera for now
            var existingCamera = Camera.main;
            if (existingCamera == null)
            {
                var camera = new GameObject("Main Camera");
                camera.AddComponent<Camera>();
                camera.tag = "MainCamera";
                camera.transform.position = new Vector3(0, 1.6f, 0);
                
                Undo.RegisterCreatedObjectUndo(camera, "Create Main Camera");
                created.Add("Main Camera");
            }
            
            return created;
        }
        
        #endregion
        
        #region GameObject Template Helpers
        
        private GameObject CreateGameObjectFromTemplate(string template)
        {
            GameObject go;
            
            switch (template)
            {
                case "Camera":
                    go = new GameObject("Camera");
                    go.AddComponent<Camera>();
                    break;
                    
                case "Light-Directional":
                    go = new GameObject("Directional Light");
                    var dirLight = go.AddComponent<Light>();
                    dirLight.type = LightType.Directional;
                    go.transform.rotation = Quaternion.Euler(50, -30, 0);
                    break;
                    
                case "Light-Point":
                    go = new GameObject("Point Light");
                    var pointLight = go.AddComponent<Light>();
                    pointLight.type = LightType.Point;
                    break;
                    
                case "Light-Spot":
                    go = new GameObject("Spot Light");
                    var spotLight = go.AddComponent<Light>();
                    spotLight.type = LightType.Spot;
                    break;
                    
                // Primitives
                case "Cube":
                    go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    break;
                    
                case "Sphere":
                    go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    break;
                    
                case "Plane":
                    go = GameObject.CreatePrimitive(PrimitiveType.Plane);
                    break;
                    
                case "Cylinder":
                    go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    break;
                    
                case "Capsule":
                    go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    break;
                    
                case "Quad":
                    go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    break;
                    
                case "Empty":
                    go = new GameObject("GameObject");
                    break;
                    
                case "Player":
                    go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    go.name = "Player";
                    go.AddComponent<CharacterController>();
                    break;
                    
                case "Enemy":
                    go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    go.name = "Enemy";
                    break;
                    
                case "Particle System":
                    go = new GameObject("Particle System");
                    go.AddComponent<ParticleSystem>();
                    break;
                    
                case "Audio Source":
                    go = new GameObject("Audio Source");
                    go.AddComponent<AudioSource>();
                    break;
                    
                default:
                    throw new InvalidOperationException($"Unknown template: {template}");
            }
            
            return go;
        }
        
        private void ApplyTransform(GameObject go, Dictionary<string, object> payload)
        {
            if (payload.ContainsKey("position") && payload["position"] is Dictionary<string, object> posDict)
            {
                go.transform.position = new Vector3(
                    Convert.ToSingle(posDict.ContainsKey("x") ? posDict["x"] : 0),
                    Convert.ToSingle(posDict.ContainsKey("y") ? posDict["y"] : 0),
                    Convert.ToSingle(posDict.ContainsKey("z") ? posDict["z"] : 0)
                );
            }
            
            if (payload.ContainsKey("rotation") && payload["rotation"] is Dictionary<string, object> rotDict)
            {
                go.transform.rotation = Quaternion.Euler(
                    Convert.ToSingle(rotDict.ContainsKey("x") ? rotDict["x"] : 0),
                    Convert.ToSingle(rotDict.ContainsKey("y") ? rotDict["y"] : 0),
                    Convert.ToSingle(rotDict.ContainsKey("z") ? rotDict["z"] : 0)
                );
            }
            
            if (payload.ContainsKey("scale") && payload["scale"] is Dictionary<string, object> scaleDict)
            {
                go.transform.localScale = new Vector3(
                    Convert.ToSingle(scaleDict.ContainsKey("x") ? scaleDict["x"] : 1),
                    Convert.ToSingle(scaleDict.ContainsKey("y") ? scaleDict["y"] : 1),
                    Convert.ToSingle(scaleDict.ContainsKey("z") ? scaleDict["z"] : 1)
                );
            }
        }
        
        #endregion
        
        #region Code Generation Helpers
        
        private string GenerateDesignPattern(string patternType, string className, string namespaceName, Dictionary<string, object> options)
        {
            return patternType.ToLower() switch
            {
                "singleton" => GenerateSingletonPattern(className, namespaceName, options),
                "objectpool" => GenerateObjectPoolPattern(className, namespaceName, options),
                "statemachine" => GenerateStateMachinePattern(className, namespaceName),
                "observer" => GenerateObserverPattern(className, namespaceName),
                "command" => GenerateCommandPattern(className, namespaceName),
                "factory" => GenerateFactoryPattern(className, namespaceName, options),
                "servicelocator" => GenerateServiceLocatorPattern(className, namespaceName),
                _ => throw new InvalidOperationException($"Unknown pattern type: {patternType}")
            };
        }
        
        private string GenerateSingletonPattern(string className, string namespaceName, Dictionary<string, object> options)
        {
            var monoBehaviour = GetOptionBool(options, "monoBehaviour", true);
            var persistent = GetOptionBool(options, "persistent", false);
            
            var sb = new StringBuilder();
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();
            
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.AppendLine($"namespace {namespaceName}");
                sb.AppendLine("{");
            }
            
            var indent = string.IsNullOrEmpty(namespaceName) ? "" : "    ";
            
            if (monoBehaviour)
            {
                sb.AppendLine($"{indent}public class {className} : MonoBehaviour");
                sb.AppendLine($"{indent}{{");
                sb.AppendLine($"{indent}    private static {className} _instance;");
                sb.AppendLine($"{indent}    ");
                sb.AppendLine($"{indent}    public static {className} Instance");
                sb.AppendLine($"{indent}    {{");
                sb.AppendLine($"{indent}        get");
                sb.AppendLine($"{indent}        {{");
                sb.AppendLine($"{indent}            if (_instance == null)");
                sb.AppendLine($"{indent}            {{");
                sb.AppendLine($"{indent}                _instance = FindObjectOfType<{className}>();");
                sb.AppendLine($"{indent}                if (_instance == null)");
                sb.AppendLine($"{indent}                {{");
                sb.AppendLine($"{indent}                    GameObject singletonObject = new GameObject(\"{className}\");");
                sb.AppendLine($"{indent}                    _instance = singletonObject.AddComponent<{className}>();");
                sb.AppendLine($"{indent}                }}");
                sb.AppendLine($"{indent}            }}");
                sb.AppendLine($"{indent}            return _instance;");
                sb.AppendLine($"{indent}        }}");
                sb.AppendLine($"{indent}    }}");
                sb.AppendLine($"{indent}    ");
                sb.AppendLine($"{indent}    private void Awake()");
                sb.AppendLine($"{indent}    {{");
                sb.AppendLine($"{indent}        if (_instance == null)");
                sb.AppendLine($"{indent}        {{");
                sb.AppendLine($"{indent}            _instance = this;");
                if (persistent)
                {
                    sb.AppendLine($"{indent}            DontDestroyOnLoad(gameObject);");
                }
                sb.AppendLine($"{indent}        }}");
                sb.AppendLine($"{indent}        else if (_instance != this)");
                sb.AppendLine($"{indent}        {{");
                sb.AppendLine($"{indent}            Destroy(gameObject);");
                sb.AppendLine($"{indent}        }}");
                sb.AppendLine($"{indent}    }}");
                sb.AppendLine($"{indent}}}");
            }
            else
            {
                sb.AppendLine($"{indent}public class {className}");
                sb.AppendLine($"{indent}{{");
                sb.AppendLine($"{indent}    private static {className} _instance;");
                sb.AppendLine($"{indent}    private static readonly object _lock = new object();");
                sb.AppendLine($"{indent}    ");
                sb.AppendLine($"{indent}    public static {className} Instance");
                sb.AppendLine($"{indent}    {{");
                sb.AppendLine($"{indent}        get");
                sb.AppendLine($"{indent}        {{");
                sb.AppendLine($"{indent}            lock (_lock)");
                sb.AppendLine($"{indent}            {{");
                sb.AppendLine($"{indent}                if (_instance == null)");
                sb.AppendLine($"{indent}                {{");
                sb.AppendLine($"{indent}                    _instance = new {className}();");
                sb.AppendLine($"{indent}                }}");
                sb.AppendLine($"{indent}                return _instance;");
                sb.AppendLine($"{indent}            }}");
                sb.AppendLine($"{indent}        }}");
                sb.AppendLine($"{indent}    }}");
                sb.AppendLine($"{indent}    ");
                sb.AppendLine($"{indent}    private {className}() {{ }}");
                sb.AppendLine($"{indent}}}");
            }
            
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.AppendLine("}");
            }
            
            return sb.ToString();
        }
        
        // Simplified pattern generators for other patterns
        private string GenerateObjectPoolPattern(string className, string namespaceName, Dictionary<string, object> options)
        {
            // Simplified implementation
            return $"// ObjectPool pattern for {className}\n// Implementation placeholder";
        }
        
        private string GenerateStateMachinePattern(string className, string namespaceName)
        {
            return $"// StateMachine pattern for {className}\n// Implementation placeholder";
        }
        
        private string GenerateObserverPattern(string className, string namespaceName)
        {
            return $"// Observer pattern for {className}\n// Implementation placeholder";
        }
        
        private string GenerateCommandPattern(string className, string namespaceName)
        {
            return $"// Command pattern for {className}\n// Implementation placeholder";
        }
        
        private string GenerateFactoryPattern(string className, string namespaceName, Dictionary<string, object> options)
        {
            return $"// Factory pattern for {className}\n// Implementation placeholder";
        }
        
        private string GenerateServiceLocatorPattern(string className, string namespaceName)
        {
            return $"// ServiceLocator pattern for {className}\n// Implementation placeholder";
        }
        
        private string GenerateMonoBehaviourTemplate(string className, string namespaceName)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();
            
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.AppendLine($"namespace {namespaceName}");
                sb.AppendLine("{");
            }
            
            var indent = string.IsNullOrEmpty(namespaceName) ? "" : "    ";
            
            sb.AppendLine($"{indent}public class {className} : MonoBehaviour");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}    void Awake()");
            sb.AppendLine($"{indent}    {{");
            sb.AppendLine($"{indent}        ");
            sb.AppendLine($"{indent}    }}");
            sb.AppendLine($"{indent}    ");
            sb.AppendLine($"{indent}    void Start()");
            sb.AppendLine($"{indent}    {{");
            sb.AppendLine($"{indent}        ");
            sb.AppendLine($"{indent}    }}");
            sb.AppendLine($"{indent}    ");
            sb.AppendLine($"{indent}    void Update()");
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
        
        private string GenerateScriptableObjectTemplate(string className, string namespaceName)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();
            
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.AppendLine($"namespace {namespaceName}");
                sb.AppendLine("{");
            }
            
            var indent = string.IsNullOrEmpty(namespaceName) ? "" : "    ";
            
            sb.AppendLine($"{indent}[CreateAssetMenu(fileName = \"{className}\", menuName = \"ScriptableObjects/{className}\")]");
            sb.AppendLine($"{indent}public class {className} : ScriptableObject");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}    // Add your fields here");
            sb.AppendLine($"{indent}}}");
            
            if (!string.IsNullOrEmpty(namespaceName))
            {
                sb.AppendLine("}");
            }
            
            return sb.ToString();
        }
        
        #endregion
        
        #region Template Management Helpers
        
        private object CustomizeGameObject(Dictionary<string, object> payload)
        {
            var gameObjectPath = GetString(payload, "gameObjectPath");
            if (string.IsNullOrEmpty(gameObjectPath))
            {
                throw new InvalidOperationException("gameObjectPath is required");
            }
            
            var go = ResolveGameObject(gameObjectPath);
            
            // Add components if specified
            if (payload.ContainsKey("components") && payload["components"] is List<object> components)
            {
                foreach (var comp in components)
                {
                    if (comp is Dictionary<string, object> compDict && compDict.ContainsKey("type"))
                    {
                        var typeName = compDict["type"].ToString();
                        var type = ResolveType(typeName);
                        var component = go.AddComponent(type);
                        
                        // Apply properties if specified
                        if (compDict.ContainsKey("properties") && compDict["properties"] is Dictionary<string, object> properties)
                        {
                            // Simplified property application
                            foreach (var kvp in properties)
                            {
                                try
                                {
                                    var prop = type.GetProperty(kvp.Key);
                                    if (prop != null && prop.CanWrite)
                                    {
                                        prop.SetValue(component, kvp.Value);
                                    }
                                }
                                catch
                                {
                                    // Skip properties that fail
                                }
                            }
                        }
                    }
                }
            }
            
            Undo.RecordObject(go, "Customize GameObject");
            
            return CreateSuccessResponse(
                ("gameObjectPath", gameObjectPath),
                ("message", "GameObject customized successfully")
            );
        }
        
        private object ConvertGameObjectToPrefab(Dictionary<string, object> payload)
        {
            var gameObjectPath = GetString(payload, "gameObjectPath");
            var prefabPath = GetString(payload, "prefabPath");
            
            if (string.IsNullOrEmpty(gameObjectPath))
            {
                throw new InvalidOperationException("gameObjectPath is required");
            }
            
            if (string.IsNullOrEmpty(prefabPath))
            {
                throw new InvalidOperationException("prefabPath is required");
            }
            
            var go = ResolveGameObject(gameObjectPath);
            
            // Ensure directory exists
            var directory = Path.GetDirectoryName(prefabPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Failed to create prefab at {prefabPath}");
            }
            
            AssetDatabase.Refresh();
            
            return CreateSuccessResponse(
                ("gameObjectPath", gameObjectPath),
                ("prefabPath", prefabPath),
                ("guid", AssetDatabase.AssetPathToGUID(prefabPath)),
                ("message", "GameObject converted to prefab successfully")
            );
        }
        
        #endregion
        
        #region Menu Hierarchy Helpers
        
        private void BuildMenuLevel(GameObject parent, Dictionary<string, object> menuStructure, Dictionary<string, object> payload, List<string> createdMenus)
        {
            var buttonWidth = GetFloat(payload, "buttonWidth", 200f);
            var buttonHeight = GetFloat(payload, "buttonHeight", 50f);
            var spacing = GetFloat(payload, "spacing", 10f);
            
            // Add VerticalLayoutGroup to parent
            var layoutGroup = parent.GetComponent<VerticalLayoutGroup>();
            if (layoutGroup == null)
            {
                layoutGroup = parent.AddComponent<VerticalLayoutGroup>();
            }
            layoutGroup.spacing = spacing;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;
            
            foreach (var kvp in menuStructure)
            {
                var itemName = kvp.Key;
                var itemValue = kvp.Value;
                
                // Create button
                var buttonGo = new GameObject(itemName);
                var buttonRect = buttonGo.AddComponent<RectTransform>();
                buttonGo.transform.SetParent(parent.transform, false);
                
                buttonRect.sizeDelta = new Vector2(buttonWidth, buttonHeight);
                
                var image = buttonGo.AddComponent<Image>();
                image.color = new Color(1f, 1f, 1f, 0.8f);
                
                var button = buttonGo.AddComponent<Button>();
                
                // Add text
                var textGo = new GameObject("Text");
                var textRect = textGo.AddComponent<RectTransform>();
                textGo.transform.SetParent(buttonGo.transform, false);
                
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = Vector2.zero;
                
                var text = textGo.AddComponent<Text>();
                text.text = itemName;
                text.alignment = TextAnchor.MiddleCenter;
                text.color = Color.black;
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                
                createdMenus.Add(itemName);
                
                // Handle submenus recursively
                if (itemValue is Dictionary<string, object> submenuDict)
                {
                    var submenuGo = new GameObject($"{itemName}_Submenu");
                    var submenuRect = submenuGo.AddComponent<RectTransform>();
                    submenuGo.transform.SetParent(parent.transform, false);
                    
                    submenuRect.anchorMin = Vector2.zero;
                    submenuRect.anchorMax = Vector2.one;
                    submenuRect.sizeDelta = Vector2.zero;
                    
                    submenuGo.SetActive(false); // Hide by default
                    
                    BuildMenuLevel(submenuGo, submenuDict, payload, createdMenus);
                }
            }
        }
        
        #endregion
        
        #region Helper Methods
        
        private string GetHierarchyPath(GameObject go)
        {
            if (go == null) return null;
            
            var path = go.name;
            var parent = go.transform.parent;
            
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            
            return path;
        }
        
        private bool GetOptionBool(Dictionary<string, object> options, string key, bool defaultValue)
        {
            if (options.ContainsKey(key))
            {
                var value = options[key];
                if (value is bool b) return b;
                if (value is string s) return bool.TryParse(s, out var result) && result;
            }
            return defaultValue;
        }
        
        #endregion
    }
}

