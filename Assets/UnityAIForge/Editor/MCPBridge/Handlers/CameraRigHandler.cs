using System;
using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Base;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MCP.Editor.Handlers
{
    /// <summary>
    /// Mid-level camera rig utilities: create and configure camera rigs (follow, orbit, split-screen, etc.).
    /// </summary>
    public class CameraRigHandler : BaseCommandHandler
    {
        private static readonly string[] Operations = { "createRig", "updateRig", "inspect" };

        public override string Category => "cameraRig";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) => false;

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "createRig" => CreateRig(payload),
                "updateRig" => UpdateRig(payload),
                "inspect" => InspectRig(payload),
                _ => throw new InvalidOperationException($"Unsupported camera rig operation: {operation}"),
            };
        }

        #region Create Rig

        private object CreateRig(Dictionary<string, object> payload)
        {
            var rigType = GetString(payload, "rigType")?.ToLowerInvariant() ?? "follow";
            var rigName = GetString(payload, "rigName") ?? $"CameraRig_{rigType}";
            var parentPath = GetString(payload, "parentPath");

            GameObject parent = null;
            if (!string.IsNullOrEmpty(parentPath))
            {
                parent = ResolveGameObject(parentPath);
            }

            // Create rig root
            var rigRoot = new GameObject(rigName);
            Undo.RegisterCreatedObjectUndo(rigRoot, "Create Camera Rig");

            if (parent != null)
            {
                rigRoot.transform.SetParent(parent.transform, false);
            }

            // Create camera child
            var cameraGo = new GameObject("Camera");
            Undo.RegisterCreatedObjectUndo(cameraGo, "Create Camera");
            cameraGo.transform.SetParent(rigRoot.transform, false);

            var camera = Undo.AddComponent<Camera>(cameraGo);
            ConfigureCamera(camera, payload);

            // Apply rig-specific setup
            switch (rigType)
            {
                case "follow":
                    SetupFollowRig(rigRoot, cameraGo, payload);
                    break;
                case "orbit":
                    SetupOrbitRig(rigRoot, cameraGo, payload);
                    break;
                case "splitscreen":
                    SetupSplitScreenRig(rigRoot, cameraGo, payload);
                    break;
                case "fixed":
                    SetupFixedRig(rigRoot, cameraGo, payload);
                    break;
                case "dolly":
                    SetupDollyRig(rigRoot, cameraGo, payload);
                    break;
            }

            EditorSceneManager.MarkSceneDirty(rigRoot.scene);

            return CreateSuccessResponse(
                ("rigPath", BuildGameObjectPath(rigRoot)),
                ("cameraPath", BuildGameObjectPath(cameraGo)),
                ("rigType", rigType)
            );
        }

        private void ConfigureCamera(Camera camera, Dictionary<string, object> payload)
        {
            if (payload.TryGetValue("fieldOfView", out var fovObj))
            {
                camera.fieldOfView = Convert.ToSingle(fovObj);
            }

            if (payload.TryGetValue("orthographic", out var orthoObj) && Convert.ToBoolean(orthoObj))
            {
                camera.orthographic = true;
                if (payload.TryGetValue("orthographicSize", out var sizeObj))
                {
                    camera.orthographicSize = Convert.ToSingle(sizeObj);
                }
            }
        }

        private void SetupFollowRig(GameObject rigRoot, GameObject cameraGo, Dictionary<string, object> payload)
        {
            var offset = GetVector3(payload, "offset", new Vector3(0, 5, -10));
            var followSpeed = GetFloatFromPayload(payload, "followSpeed", 5f);
            var lookAtTarget = GetBool(payload, "lookAtTarget", true);

            cameraGo.transform.localPosition = offset;

            // Add a simple follow script component (placeholder - would need actual script)
            var script = rigRoot.AddComponent<CameraFollowHelper>();
            script.offset = offset;
            script.followSpeed = followSpeed;
            script.lookAtTarget = lookAtTarget;

            if (payload.TryGetValue("targetPath", out var targetPathObj))
            {
                var targetPath = targetPathObj.ToString();
                if (!string.IsNullOrEmpty(targetPath))
                {
                    try
                    {
                        script.target = ResolveGameObject(targetPath).transform;
                    }
                    catch
                    {
                        Debug.LogWarning($"Target '{targetPath}' not found for follow rig.");
                    }
                }
            }
        }

        private void SetupOrbitRig(GameObject rigRoot, GameObject cameraGo, Dictionary<string, object> payload)
        {
            var distance = GetFloatFromPayload(payload, "distance", 10f);
            var lookAtTarget = GetBool(payload, "lookAtTarget", true);

            cameraGo.transform.localPosition = new Vector3(0, 0, -distance);

            var script = rigRoot.AddComponent<CameraOrbitHelper>();
            script.distance = distance;
            script.lookAtTarget = lookAtTarget;

            if (payload.TryGetValue("targetPath", out var targetPathObj))
            {
                var targetPath = targetPathObj.ToString();
                if (!string.IsNullOrEmpty(targetPath))
                {
                    try
                    {
                        script.target = ResolveGameObject(targetPath).transform;
                    }
                    catch
                    {
                        Debug.LogWarning($"Target '{targetPath}' not found for orbit rig.");
                    }
                }
            }
        }

        private void SetupSplitScreenRig(GameObject rigRoot, GameObject cameraGo, Dictionary<string, object> payload)
        {
            var camera = cameraGo.GetComponent<Camera>();
            var splitIndex = GetInt(payload, "splitScreenIndex", 0);

            // Set viewport rect based on split index
            switch (splitIndex)
            {
                case 0: // Top-left
                    camera.rect = new Rect(0, 0.5f, 0.5f, 0.5f);
                    break;
                case 1: // Top-right
                    camera.rect = new Rect(0.5f, 0.5f, 0.5f, 0.5f);
                    break;
                case 2: // Bottom-left
                    camera.rect = new Rect(0, 0, 0.5f, 0.5f);
                    break;
                case 3: // Bottom-right
                    camera.rect = new Rect(0.5f, 0, 0.5f, 0.5f);
                    break;
            }
        }

        private void SetupFixedRig(GameObject rigRoot, GameObject cameraGo, Dictionary<string, object> payload)
        {
            var offset = GetVector3(payload, "offset", new Vector3(0, 10, -10));
            cameraGo.transform.localPosition = offset;

            var lookAtTarget = GetBool(payload, "lookAtTarget", false);
            if (lookAtTarget && payload.TryGetValue("targetPath", out var targetPathObj))
            {
                var targetPath = targetPathObj.ToString();
                if (!string.IsNullOrEmpty(targetPath))
                {
                    try
                    {
                        var target = ResolveGameObject(targetPath);
                        cameraGo.transform.LookAt(target.transform);
                    }
                    catch
                    {
                        Debug.LogWarning($"Target '{targetPath}' not found for fixed rig.");
                    }
                }
            }
        }

        private void SetupDollyRig(GameObject rigRoot, GameObject cameraGo, Dictionary<string, object> payload)
        {
            var offset = GetVector3(payload, "offset", new Vector3(0, 5, -10));
            cameraGo.transform.localPosition = offset;

            var script = rigRoot.AddComponent<CameraDollyHelper>();
            script.offset = offset;

            if (payload.TryGetValue("targetPath", out var targetPathObj))
            {
                var targetPath = targetPathObj.ToString();
                if (!string.IsNullOrEmpty(targetPath))
                {
                    try
                    {
                        script.target = ResolveGameObject(targetPath).transform;
                    }
                    catch
                    {
                        Debug.LogWarning($"Target '{targetPath}' not found for dolly rig.");
                    }
                }
            }
        }

        #endregion

        #region Update Rig

        private object UpdateRig(Dictionary<string, object> payload)
        {
            var parentPath = GetString(payload, "parentPath");
            if (string.IsNullOrEmpty(parentPath))
            {
                throw new InvalidOperationException("parentPath is required for updateRig.");
            }

            var rigRoot = ResolveGameObject(parentPath);
            Undo.RecordObject(rigRoot.transform, "Update Camera Rig");

            var camera = rigRoot.GetComponentInChildren<Camera>();
            if (camera != null)
            {
                Undo.RecordObject(camera, "Update Camera");
                ConfigureCamera(camera, payload);
            }

            // Update helper scripts if present
            var followHelper = rigRoot.GetComponent<CameraFollowHelper>();
            if (followHelper != null)
            {
                Undo.RecordObject(followHelper, "Update Follow Helper");
                if (payload.TryGetValue("offset", out var offsetObj))
                {
                    followHelper.offset = GetVector3(payload, "offset", followHelper.offset);
                }
                if (payload.TryGetValue("followSpeed", out var speedObj))
                {
                    followHelper.followSpeed = Convert.ToSingle(speedObj);
                }
            }

            var orbitHelper = rigRoot.GetComponent<CameraOrbitHelper>();
            if (orbitHelper != null)
            {
                Undo.RecordObject(orbitHelper, "Update Orbit Helper");
                if (payload.TryGetValue("distance", out var distObj))
                {
                    orbitHelper.distance = Convert.ToSingle(distObj);
                }
            }

            EditorSceneManager.MarkSceneDirty(rigRoot.scene);
            return CreateSuccessResponse(("rigPath", BuildGameObjectPath(rigRoot)));
        }

        #endregion

        #region Inspect

        private object InspectRig(Dictionary<string, object> payload)
        {
            var parentPath = GetString(payload, "parentPath");
            if (string.IsNullOrEmpty(parentPath))
            {
                throw new InvalidOperationException("parentPath is required for inspect.");
            }

            var rigRoot = ResolveGameObject(parentPath);
            var camera = rigRoot.GetComponentInChildren<Camera>();

            var info = new Dictionary<string, object>
            {
                { "rigPath", BuildGameObjectPath(rigRoot) },
                { "hasCamera", camera != null }
            };

            if (camera != null)
            {
                info["cameraPath"] = BuildGameObjectPath(camera.gameObject);
                info["fieldOfView"] = camera.fieldOfView;
                info["orthographic"] = camera.orthographic;
                info["orthographicSize"] = camera.orthographicSize;
            }

            var followHelper = rigRoot.GetComponent<CameraFollowHelper>();
            if (followHelper != null)
            {
                info["rigType"] = "follow";
                info["offset"] = new Dictionary<string, object>
                {
                    { "x", followHelper.offset.x },
                    { "y", followHelper.offset.y },
                    { "z", followHelper.offset.z }
                };
                info["followSpeed"] = followHelper.followSpeed;
            }

            var orbitHelper = rigRoot.GetComponent<CameraOrbitHelper>();
            if (orbitHelper != null)
            {
                info["rigType"] = "orbit";
                info["distance"] = orbitHelper.distance;
            }

            return CreateSuccessResponse(("rig", info));
        }

        #endregion

        #region Helpers

        private Vector3 GetVector3(Dictionary<string, object> payload, string key, Vector3 fallback)
        {
            if (!payload.TryGetValue(key, out var value) || value == null)
            {
                return fallback;
            }

            if (value is Dictionary<string, object> dict)
            {
                float x = dict.TryGetValue("x", out var xObj) ? Convert.ToSingle(xObj) : fallback.x;
                float y = dict.TryGetValue("y", out var yObj) ? Convert.ToSingle(yObj) : fallback.y;
                float z = dict.TryGetValue("z", out var zObj) ? Convert.ToSingle(zObj) : fallback.z;
                return new Vector3(x, y, z);
            }

            return fallback;
        }

        private float GetFloatFromPayload(Dictionary<string, object> payload, string key, float defaultValue)
        {
            if (!payload.TryGetValue(key, out var value) || value == null)
            {
                return defaultValue;
            }
            return Convert.ToSingle(value);
        }

        // GetBool and GetInt are inherited from BaseCommandHandler

        private string BuildGameObjectPath(GameObject go)
        {
            var path = go.name;
            var current = go.transform.parent;
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            return path;
        }

        #endregion
    }

    #region Helper Components

    /// <summary>
    /// Simple camera follow helper component.
    /// </summary>
    [AddComponentMenu("")]
    public class CameraFollowHelper : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0, 5, -10);
        public float followSpeed = 5f;
        public bool lookAtTarget = true;

        private void LateUpdate()
        {
            if (target == null) return;

            var targetPosition = target.position + offset;
            transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);

            if (lookAtTarget)
            {
                transform.LookAt(target);
            }
        }
    }

    /// <summary>
    /// Simple camera orbit helper component.
    /// </summary>
    [AddComponentMenu("")]
    public class CameraOrbitHelper : MonoBehaviour
    {
        public Transform target;
        public float distance = 10f;
        public bool lookAtTarget = true;
        public float rotationSpeed = 100f;

        private float currentAngle = 0f;

        private void LateUpdate()
        {
            if (target == null) return;

            currentAngle += Input.GetAxis("Horizontal") * rotationSpeed * Time.deltaTime;
            var rotation = Quaternion.Euler(0, currentAngle, 0);
            var position = target.position + rotation * new Vector3(0, 0, -distance);

            transform.position = position;

            if (lookAtTarget)
            {
                transform.LookAt(target);
            }
        }
    }

    /// <summary>
    /// Simple camera dolly helper component.
    /// </summary>
    [AddComponentMenu("")]
    public class CameraDollyHelper : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0, 5, -10);

        private void LateUpdate()
        {
            if (target == null) return;

            transform.position = target.position + offset;
            transform.LookAt(target);
        }
    }

    #endregion
}

