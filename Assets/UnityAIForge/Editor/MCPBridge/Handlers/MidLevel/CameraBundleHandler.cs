using System;
using System.Collections.Generic;
using MCP.Editor.Base;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor.Handlers
{
    /// <summary>
    /// Camera Bundle Handler: Creates and configures cameras with presets.
    /// Directly manipulates Camera component properties without helper MonoBehaviours.
    /// </summary>
    public class CameraBundleHandler : BaseCommandHandler
    {
        public override IEnumerable<string> SupportedOperations => new[]
        {
            "create",
            "applyPreset",
            "listPresets"
        };

        public override string Category => "cameraBundle";

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "create" => HandleCreate(payload),
                "applyPreset" => HandleApplyPreset(payload),
                "listPresets" => HandleListPresets(payload),
                _ => throw new InvalidOperationException($"Unknown operation: {operation}")
            };
        }

        private object HandleCreate(Dictionary<string, object> payload)
        {
            string name = GetString(payload, "name", "New Camera");
            string parentPath = GetString(payload, "parentPath");
            string preset = GetString(payload, "preset");

            GameObject cameraGO = new GameObject(name);
            Camera camera = cameraGO.AddComponent<Camera>();

            if (!string.IsNullOrEmpty(parentPath))
            {
                GameObject parent = TryResolveGameObject(parentPath);
                if (parent != null)
                {
                    cameraGO.transform.SetParent(parent.transform, false);
                }
            }

            if (!string.IsNullOrEmpty(preset))
            {
                ApplyCameraPreset(camera, preset);
            }

            ApplyTransformFromPayload(cameraGO.transform, payload);
            ApplyCameraProperties(camera, payload);

            Undo.RegisterCreatedObjectUndo(cameraGO, $"Create {name}");

            return CreateSuccessResponse(
                ("message", $"Camera '{name}' created"),
                ("gameObjectPath", BuildGameObjectPath(cameraGO)),
                ("preset", preset ?? "none")
            );
        }

        private object HandleApplyPreset(Dictionary<string, object> payload)
        {
            GameObject go = ResolveGameObjectFromPayload(payload);
            Camera camera = go.GetComponent<Camera>();
            string preset = GetString(payload, "preset");

            if (camera == null)
            {
                return CreateFailureResponse($"GameObject '{go.name}' does not have a Camera component");
            }

            if (string.IsNullOrEmpty(preset))
            {
                return CreateFailureResponse("'preset' is required");
            }

            Undo.RecordObject(camera, "Apply Camera Preset");
            ApplyCameraPreset(camera, preset);
            EditorUtility.SetDirty(go);

            return CreateSuccessResponse(
                ("message", $"Preset '{preset}' applied"),
                ("gameObjectPath", BuildGameObjectPath(go))
            );
        }

        private object HandleListPresets(Dictionary<string, object> payload)
        {
            var presets = new List<Dictionary<string, object>>
            {
                new() { ["name"] = "default", ["description"] = "Default perspective camera" },
                new() { ["name"] = "orthographic2D", ["description"] = "Orthographic camera for 2D games (size=5, near=-0.3, solid color bg)" },
                new() { ["name"] = "firstPerson", ["description"] = "First-person camera (FOV=60, near=0.1)" },
                new() { ["name"] = "thirdPerson", ["description"] = "Third-person camera (FOV=50, far=500)" },
                new() { ["name"] = "topDown", ["description"] = "Top-down view camera (position Y=20, look down)" },
                new() { ["name"] = "splitScreenLeft", ["description"] = "Left-half viewport for split-screen" },
                new() { ["name"] = "splitScreenRight", ["description"] = "Right-half viewport for split-screen" },
                new() { ["name"] = "splitScreenTop", ["description"] = "Top-half viewport for split-screen" },
                new() { ["name"] = "splitScreenBottom", ["description"] = "Bottom-half viewport for split-screen" },
                new() { ["name"] = "minimap", ["description"] = "Orthographic minimap camera (corner viewport, depth=1)" },
                new() { ["name"] = "uiCamera", ["description"] = "Dedicated UI camera (depth=10, culling UI layer only)" },
            };

            return CreateSuccessResponse(("presets", presets));
        }

        #region Preset Helpers

        private void ApplyCameraPreset(Camera camera, string preset)
        {
            switch (preset.ToLower())
            {
                case "default":
                    camera.orthographic = false;
                    camera.fieldOfView = 60f;
                    camera.nearClipPlane = 0.3f;
                    camera.farClipPlane = 1000f;
                    camera.clearFlags = CameraClearFlags.Skybox;
                    camera.rect = new Rect(0, 0, 1, 1);
                    break;

                case "orthographic2d":
                    camera.orthographic = true;
                    camera.orthographicSize = 5f;
                    camera.nearClipPlane = -0.3f;
                    camera.farClipPlane = 100f;
                    camera.clearFlags = CameraClearFlags.SolidColor;
                    camera.backgroundColor = new Color(0.192f, 0.302f, 0.475f);
                    camera.rect = new Rect(0, 0, 1, 1);
                    break;

                case "firstperson":
                    camera.orthographic = false;
                    camera.fieldOfView = 60f;
                    camera.nearClipPlane = 0.1f;
                    camera.farClipPlane = 1000f;
                    camera.clearFlags = CameraClearFlags.Skybox;
                    camera.rect = new Rect(0, 0, 1, 1);
                    break;

                case "thirdperson":
                    camera.orthographic = false;
                    camera.fieldOfView = 50f;
                    camera.nearClipPlane = 0.3f;
                    camera.farClipPlane = 500f;
                    camera.clearFlags = CameraClearFlags.Skybox;
                    camera.rect = new Rect(0, 0, 1, 1);
                    break;

                case "topdown":
                    camera.orthographic = false;
                    camera.fieldOfView = 60f;
                    camera.nearClipPlane = 0.3f;
                    camera.farClipPlane = 1000f;
                    camera.clearFlags = CameraClearFlags.Skybox;
                    camera.transform.position = new Vector3(0, 20, 0);
                    camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                    camera.rect = new Rect(0, 0, 1, 1);
                    break;

                case "splitscreenleft":
                    camera.rect = new Rect(0, 0, 0.5f, 1);
                    break;

                case "splitscreenright":
                    camera.rect = new Rect(0.5f, 0, 0.5f, 1);
                    break;

                case "splitscreentop":
                    camera.rect = new Rect(0, 0.5f, 1, 0.5f);
                    break;

                case "splitscreenbottom":
                    camera.rect = new Rect(0, 0, 1, 0.5f);
                    break;

                case "minimap":
                    camera.orthographic = true;
                    camera.orthographicSize = 50f;
                    camera.nearClipPlane = 0.3f;
                    camera.farClipPlane = 500f;
                    camera.clearFlags = CameraClearFlags.SolidColor;
                    camera.backgroundColor = Color.black;
                    camera.depth = 1;
                    camera.rect = new Rect(0.75f, 0.75f, 0.24f, 0.24f);
                    camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                    break;

                case "uicamera":
                    camera.orthographic = true;
                    camera.orthographicSize = 5f;
                    camera.nearClipPlane = 0.3f;
                    camera.farClipPlane = 100f;
                    camera.clearFlags = CameraClearFlags.Depth;
                    camera.depth = 10;
                    camera.cullingMask = 1 << LayerMask.NameToLayer("UI");
                    camera.rect = new Rect(0, 0, 1, 1);
                    break;
            }
        }

        #endregion

        #region Helper Methods

        private void ApplyTransformFromPayload(Transform transform, Dictionary<string, object> payload)
        {
            if (payload.ContainsKey("position") && payload["position"] is Dictionary<string, object> posDict)
            {
                transform.position = new Vector3(
                    Convert.ToSingle(posDict.GetValueOrDefault("x", 0f)),
                    Convert.ToSingle(posDict.GetValueOrDefault("y", 0f)),
                    Convert.ToSingle(posDict.GetValueOrDefault("z", 0f))
                );
            }

            if (payload.ContainsKey("rotation") && payload["rotation"] is Dictionary<string, object> rotDict)
            {
                transform.eulerAngles = new Vector3(
                    Convert.ToSingle(rotDict.GetValueOrDefault("x", 0f)),
                    Convert.ToSingle(rotDict.GetValueOrDefault("y", 0f)),
                    Convert.ToSingle(rotDict.GetValueOrDefault("z", 0f))
                );
            }
        }

        private void ApplyCameraProperties(Camera camera, Dictionary<string, object> payload)
        {
            if (payload.ContainsKey("fieldOfView"))
            {
                camera.fieldOfView = GetFloat(payload, "fieldOfView", camera.fieldOfView);
            }

            if (payload.ContainsKey("orthographic"))
            {
                camera.orthographic = GetBool(payload, "orthographic", camera.orthographic);
            }

            if (payload.ContainsKey("orthographicSize"))
            {
                camera.orthographicSize = GetFloat(payload, "orthographicSize", camera.orthographicSize);
            }

            if (payload.ContainsKey("clearFlags"))
            {
                string flagStr = GetString(payload, "clearFlags", "skybox");
                camera.clearFlags = flagStr.ToLower() switch
                {
                    "skybox" => CameraClearFlags.Skybox,
                    "solidcolor" or "color" => CameraClearFlags.SolidColor,
                    "depth" => CameraClearFlags.Depth,
                    "nothing" or "none" => CameraClearFlags.Nothing,
                    _ => camera.clearFlags
                };
            }

            if (payload.ContainsKey("backgroundColor"))
            {
                var colorValue = payload["backgroundColor"];
                if (colorValue is string hexColor)
                {
                    if (ColorUtility.TryParseHtmlString(hexColor, out Color parsed))
                    {
                        camera.backgroundColor = parsed;
                    }
                }
                else if (colorValue is Dictionary<string, object> colorDict)
                {
                    camera.backgroundColor = new Color(
                        Convert.ToSingle(colorDict.GetValueOrDefault("r", 0f)),
                        Convert.ToSingle(colorDict.GetValueOrDefault("g", 0f)),
                        Convert.ToSingle(colorDict.GetValueOrDefault("b", 0f)),
                        Convert.ToSingle(colorDict.GetValueOrDefault("a", 1f))
                    );
                }
            }

            if (payload.ContainsKey("cullingMask"))
            {
                camera.cullingMask = GetInt(payload, "cullingMask", camera.cullingMask);
            }

            if (payload.ContainsKey("depth"))
            {
                camera.depth = GetFloat(payload, "depth", camera.depth);
            }

            if (payload.ContainsKey("nearClipPlane"))
            {
                camera.nearClipPlane = GetFloat(payload, "nearClipPlane", camera.nearClipPlane);
            }

            if (payload.ContainsKey("farClipPlane"))
            {
                camera.farClipPlane = GetFloat(payload, "farClipPlane", camera.farClipPlane);
            }

            if (payload.ContainsKey("rect") && payload["rect"] is Dictionary<string, object> rectDict)
            {
                camera.rect = new Rect(
                    Convert.ToSingle(rectDict.GetValueOrDefault("x", camera.rect.x)),
                    Convert.ToSingle(rectDict.GetValueOrDefault("y", camera.rect.y)),
                    Convert.ToSingle(rectDict.GetValueOrDefault("width", camera.rect.width)),
                    Convert.ToSingle(rectDict.GetValueOrDefault("height", camera.rect.height))
                );
            }

            if (payload.ContainsKey("targetDisplay"))
            {
                camera.targetDisplay = GetInt(payload, "targetDisplay", camera.targetDisplay);
            }

            if (payload.ContainsKey("renderingPath"))
            {
                string rpStr = GetString(payload, "renderingPath", "useplayersettings");
                camera.renderingPath = rpStr.ToLower() switch
                {
                    "useplayersettings" or "default" => UnityEngine.RenderingPath.UsePlayerSettings,
                    "forward" => UnityEngine.RenderingPath.Forward,
                    "deferred" => UnityEngine.RenderingPath.DeferredShading,
                    "vertexlit" => UnityEngine.RenderingPath.VertexLit,
                    _ => camera.renderingPath
                };
            }

            if (payload.ContainsKey("allowHDR"))
            {
                camera.allowHDR = GetBool(payload, "allowHDR", camera.allowHDR);
            }

            if (payload.ContainsKey("allowMSAA"))
            {
                camera.allowMSAA = GetBool(payload, "allowMSAA", camera.allowMSAA);
            }
        }

        #endregion
    }
}
