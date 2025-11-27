using System;
using System.Collections.Generic;
using MCP.Editor.Base;
using MCP.Editor.Interfaces;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MCP.Editor.Handlers
{
    /// <summary>
    /// UIレイアウトコンポーネント（HorizontalLayoutGroup, VerticalLayoutGroup, GridLayoutGroup,
    /// ContentSizeFitter, LayoutElement, AspectRatioFitter）を管理するコマンドハンドラー。
    /// </summary>
    public class UguiLayoutManageHandler : BaseCommandHandler
    {
        public override string Category => "uguiLayoutManage";
        
        public override IEnumerable<string> SupportedOperations => new[]
        {
            "add",     // レイアウトコンポーネントを追加
            "update",  // レイアウトコンポーネントを更新
            "remove",  // レイアウトコンポーネントを削除
            "inspect"  // レイアウトコンポーネントを検査
        };
        
        public UguiLayoutManageHandler() : base()
        {
        }
        
        public UguiLayoutManageHandler(
            IPayloadValidator validator,
            IGameObjectResolver gameObjectResolver,
            IAssetResolver assetResolver,
            ITypeResolver typeResolver)
            : base(validator, gameObjectResolver, assetResolver, typeResolver)
        {
        }
        
        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            var path = GetString(payload, "gameObjectPath");
            if (string.IsNullOrEmpty(path))
            {
                throw new InvalidOperationException("gameObjectPath is required");
            }
            
            Debug.Log($"[UguiLayoutManageHandler] Processing operation '{operation}' on: {path}");
            
            var go = GameObjectResolver.Resolve(path);
            
            object result = operation switch
            {
                "add" => AddLayoutComponent(go, payload),
                "update" => UpdateLayoutComponent(go, payload),
                "remove" => RemoveLayoutComponent(go, payload),
                "inspect" => InspectLayoutComponent(go, payload),
                _ => throw new InvalidOperationException($"Unknown operation: {operation}")
            };
            
            EditorUtility.SetDirty(go);
            Debug.Log($"[UguiLayoutManageHandler] Completed successfully");
            
            return result;
        }
        
        #region Operation Implementations
        
        private object AddLayoutComponent(GameObject go, Dictionary<string, object> payload)
        {
            var layoutType = GetString(payload, "layoutType");
            if (string.IsNullOrEmpty(layoutType))
            {
                throw new InvalidOperationException("layoutType is required for add operation");
            }
            
            Component component;
            switch (layoutType)
            {
                case "HorizontalLayoutGroup":
                    component = go.AddComponent<HorizontalLayoutGroup>();
                    ApplyLayoutGroupSettings(component, payload);
                    break;
                case "VerticalLayoutGroup":
                    component = go.AddComponent<VerticalLayoutGroup>();
                    ApplyLayoutGroupSettings(component, payload);
                    break;
                case "GridLayoutGroup":
                    component = go.AddComponent<GridLayoutGroup>();
                    ApplyGridLayoutGroupSettings((GridLayoutGroup)component, payload);
                    break;
                case "ContentSizeFitter":
                    component = go.AddComponent<ContentSizeFitter>();
                    ApplyContentSizeFitterSettings((ContentSizeFitter)component, payload);
                    break;
                case "LayoutElement":
                    component = go.AddComponent<LayoutElement>();
                    ApplyLayoutElementSettings((LayoutElement)component, payload);
                    break;
                case "AspectRatioFitter":
                    component = go.AddComponent<AspectRatioFitter>();
                    ApplyAspectRatioFitterSettings((AspectRatioFitter)component, payload);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown layoutType: {layoutType}");
            }
            
            return new Dictionary<string, object>
            {
                ["operation"] = "add",
                ["layoutType"] = layoutType,
                ["gameObjectPath"] = GetGameObjectPath(go),
            };
        }
        
        private object UpdateLayoutComponent(GameObject go, Dictionary<string, object> payload)
        {
            var layoutType = GetString(payload, "layoutType");
            if (string.IsNullOrEmpty(layoutType))
            {
                throw new InvalidOperationException("layoutType is required for update operation");
            }
            
            Component component = null;
            switch (layoutType)
            {
                case "HorizontalLayoutGroup":
                    component = go.GetComponent<HorizontalLayoutGroup>();
                    if (component != null) ApplyLayoutGroupSettings(component, payload);
                    break;
                case "VerticalLayoutGroup":
                    component = go.GetComponent<VerticalLayoutGroup>();
                    if (component != null) ApplyLayoutGroupSettings(component, payload);
                    break;
                case "GridLayoutGroup":
                    component = go.GetComponent<GridLayoutGroup>();
                    if (component != null) ApplyGridLayoutGroupSettings((GridLayoutGroup)component, payload);
                    break;
                case "ContentSizeFitter":
                    component = go.GetComponent<ContentSizeFitter>();
                    if (component != null) ApplyContentSizeFitterSettings((ContentSizeFitter)component, payload);
                    break;
                case "LayoutElement":
                    component = go.GetComponent<LayoutElement>();
                    if (component != null) ApplyLayoutElementSettings((LayoutElement)component, payload);
                    break;
                case "AspectRatioFitter":
                    component = go.GetComponent<AspectRatioFitter>();
                    if (component != null) ApplyAspectRatioFitterSettings((AspectRatioFitter)component, payload);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown layoutType: {layoutType}");
            }
            
            if (component == null)
            {
                throw new InvalidOperationException($"Component {layoutType} not found on GameObject");
            }
            
            return new Dictionary<string, object>
            {
                ["operation"] = "update",
                ["layoutType"] = layoutType,
                ["gameObjectPath"] = GetGameObjectPath(go),
            };
        }
        
        private object RemoveLayoutComponent(GameObject go, Dictionary<string, object> payload)
        {
            var layoutType = GetString(payload, "layoutType");
            if (string.IsNullOrEmpty(layoutType))
            {
                throw new InvalidOperationException("layoutType is required for remove operation");
            }
            
            Component component = null;
            switch (layoutType)
            {
                case "HorizontalLayoutGroup":
                    component = go.GetComponent<HorizontalLayoutGroup>();
                    break;
                case "VerticalLayoutGroup":
                    component = go.GetComponent<VerticalLayoutGroup>();
                    break;
                case "GridLayoutGroup":
                    component = go.GetComponent<GridLayoutGroup>();
                    break;
                case "ContentSizeFitter":
                    component = go.GetComponent<ContentSizeFitter>();
                    break;
                case "LayoutElement":
                    component = go.GetComponent<LayoutElement>();
                    break;
                case "AspectRatioFitter":
                    component = go.GetComponent<AspectRatioFitter>();
                    break;
                default:
                    throw new InvalidOperationException($"Unknown layoutType: {layoutType}");
            }
            
            if (component == null)
            {
                throw new InvalidOperationException($"Component {layoutType} not found on GameObject");
            }
            
            UnityEngine.Object.DestroyImmediate(component);
            
            return new Dictionary<string, object>
            {
                ["operation"] = "remove",
                ["layoutType"] = layoutType,
                ["gameObjectPath"] = GetGameObjectPath(go),
            };
        }
        
        private object InspectLayoutComponent(GameObject go, Dictionary<string, object> payload)
        {
            var layoutType = GetString(payload, "layoutType");
            var result = new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["gameObjectPath"] = GetGameObjectPath(go),
                ["layouts"] = new List<object>(),
            };
            
            var layouts = new List<object>();
            
            if (string.IsNullOrEmpty(layoutType))
            {
                // Inspect all layout components if layoutType not specified
                var hlg = go.GetComponent<HorizontalLayoutGroup>();
                if (hlg != null) layouts.Add(SerializeLayoutGroup(hlg, "HorizontalLayoutGroup"));
                
                var vlg = go.GetComponent<VerticalLayoutGroup>();
                if (vlg != null) layouts.Add(SerializeLayoutGroup(vlg, "VerticalLayoutGroup"));
                
                var glg = go.GetComponent<GridLayoutGroup>();
                if (glg != null) layouts.Add(SerializeGridLayoutGroup(glg));
                
                var csf = go.GetComponent<ContentSizeFitter>();
                if (csf != null) layouts.Add(SerializeContentSizeFitter(csf));
                
                var le = go.GetComponent<LayoutElement>();
                if (le != null) layouts.Add(SerializeLayoutElement(le));
                
                var arf = go.GetComponent<AspectRatioFitter>();
                if (arf != null) layouts.Add(SerializeAspectRatioFitter(arf));
            }
            else
            {
                // Inspect specific layout type
                switch (layoutType)
                {
                    case "HorizontalLayoutGroup":
                        var hlg = go.GetComponent<HorizontalLayoutGroup>();
                        if (hlg != null) layouts.Add(SerializeLayoutGroup(hlg, "HorizontalLayoutGroup"));
                        break;
                    case "VerticalLayoutGroup":
                        var vlg = go.GetComponent<VerticalLayoutGroup>();
                        if (vlg != null) layouts.Add(SerializeLayoutGroup(vlg, "VerticalLayoutGroup"));
                        break;
                    case "GridLayoutGroup":
                        var glg = go.GetComponent<GridLayoutGroup>();
                        if (glg != null) layouts.Add(SerializeGridLayoutGroup(glg));
                        break;
                    case "ContentSizeFitter":
                        var csf = go.GetComponent<ContentSizeFitter>();
                        if (csf != null) layouts.Add(SerializeContentSizeFitter(csf));
                        break;
                    case "LayoutElement":
                        var le = go.GetComponent<LayoutElement>();
                        if (le != null) layouts.Add(SerializeLayoutElement(le));
                        break;
                    case "AspectRatioFitter":
                        var arf = go.GetComponent<AspectRatioFitter>();
                        if (arf != null) layouts.Add(SerializeAspectRatioFitter(arf));
                        break;
                }
            }
            
            result["layouts"] = layouts;
            return result;
        }
        
        #endregion
        
        #region Apply Settings Methods
        
        private void ApplyLayoutGroupSettings(Component component, Dictionary<string, object> payload)
        {
            var layoutGroup = component as HorizontalOrVerticalLayoutGroup;
            if (layoutGroup == null) return;
            
            // Apply padding
            if (payload.ContainsKey("padding"))
            {
                var paddingDict = payload["padding"] as Dictionary<string, object>;
                if (paddingDict != null)
                {
                    layoutGroup.padding = new RectOffset(
                        GetInt(paddingDict, "left", layoutGroup.padding.left),
                        GetInt(paddingDict, "right", layoutGroup.padding.right),
                        GetInt(paddingDict, "top", layoutGroup.padding.top),
                        GetInt(paddingDict, "bottom", layoutGroup.padding.bottom)
                    );
                }
            }
            
            // Apply spacing
            if (payload.ContainsKey("spacing"))
                layoutGroup.spacing = GetFloat(payload, "spacing");
            
            // Apply childAlignment
            var childAlignment = GetString(payload, "childAlignment");
            if (!string.IsNullOrEmpty(childAlignment))
            {
                layoutGroup.childAlignment = (TextAnchor)Enum.Parse(typeof(TextAnchor), childAlignment);
            }
            
            // Apply child control settings
            if (payload.ContainsKey("childControlWidth"))
                layoutGroup.childControlWidth = GetBool(payload, "childControlWidth");
            
            if (payload.ContainsKey("childControlHeight"))
                layoutGroup.childControlHeight = GetBool(payload, "childControlHeight");
            
            if (payload.ContainsKey("childForceExpandWidth"))
                layoutGroup.childForceExpandWidth = GetBool(payload, "childForceExpandWidth");
            
            if (payload.ContainsKey("childForceExpandHeight"))
                layoutGroup.childForceExpandHeight = GetBool(payload, "childForceExpandHeight");
        }
        
        private void ApplyGridLayoutGroupSettings(GridLayoutGroup grid, Dictionary<string, object> payload)
        {
            // Apply common layout group settings
            if (payload.ContainsKey("padding"))
            {
                var paddingDict = payload["padding"] as Dictionary<string, object>;
                if (paddingDict != null)
                {
                    grid.padding = new RectOffset(
                        GetInt(paddingDict, "left", grid.padding.left),
                        GetInt(paddingDict, "right", grid.padding.right),
                        GetInt(paddingDict, "top", grid.padding.top),
                        GetInt(paddingDict, "bottom", grid.padding.bottom)
                    );
                }
            }
            
            var childAlignment = GetString(payload, "childAlignment");
            if (!string.IsNullOrEmpty(childAlignment))
            {
                grid.childAlignment = (TextAnchor)Enum.Parse(typeof(TextAnchor), childAlignment);
            }
            
            // Apply grid-specific settings
            if (payload.ContainsKey("cellSizeX") || payload.ContainsKey("cellSizeY"))
            {
                grid.cellSize = new Vector2(
                    GetFloat(payload, "cellSizeX", grid.cellSize.x),
                    GetFloat(payload, "cellSizeY", grid.cellSize.y)
                );
            }
            
            if (payload.ContainsKey("spacing") || payload.ContainsKey("spacingY"))
            {
                grid.spacing = new Vector2(
                    GetFloat(payload, "spacing", grid.spacing.x),
                    GetFloat(payload, "spacingY", grid.spacing.y)
                );
            }
            
            var constraint = GetString(payload, "constraint");
            if (!string.IsNullOrEmpty(constraint))
            {
                grid.constraint = (GridLayoutGroup.Constraint)Enum.Parse(typeof(GridLayoutGroup.Constraint), constraint);
            }
            
            if (payload.ContainsKey("constraintCount"))
            {
                var constraintCount = GetInt(payload, "constraintCount", -1);
                if (constraintCount >= 0)
                {
                    grid.constraintCount = constraintCount;
                }
            }
            
            var startCorner = GetString(payload, "startCorner");
            if (!string.IsNullOrEmpty(startCorner))
            {
                grid.startCorner = (GridLayoutGroup.Corner)Enum.Parse(typeof(GridLayoutGroup.Corner), startCorner);
            }
            
            var startAxis = GetString(payload, "startAxis");
            if (!string.IsNullOrEmpty(startAxis))
            {
                grid.startAxis = (GridLayoutGroup.Axis)Enum.Parse(typeof(GridLayoutGroup.Axis), startAxis);
            }
        }
        
        private void ApplyContentSizeFitterSettings(ContentSizeFitter fitter, Dictionary<string, object> payload)
        {
            var horizontalFit = GetString(payload, "horizontalFit");
            if (!string.IsNullOrEmpty(horizontalFit))
            {
                fitter.horizontalFit = (ContentSizeFitter.FitMode)Enum.Parse(typeof(ContentSizeFitter.FitMode), horizontalFit);
            }
            
            var verticalFit = GetString(payload, "verticalFit");
            if (!string.IsNullOrEmpty(verticalFit))
            {
                fitter.verticalFit = (ContentSizeFitter.FitMode)Enum.Parse(typeof(ContentSizeFitter.FitMode), verticalFit);
            }
        }
        
        private void ApplyLayoutElementSettings(LayoutElement element, Dictionary<string, object> payload)
        {
            if (payload.ContainsKey("minWidth"))
                element.minWidth = GetFloat(payload, "minWidth");
            
            if (payload.ContainsKey("minHeight"))
                element.minHeight = GetFloat(payload, "minHeight");
            
            if (payload.ContainsKey("preferredWidth"))
                element.preferredWidth = GetFloat(payload, "preferredWidth");
            
            if (payload.ContainsKey("preferredHeight"))
                element.preferredHeight = GetFloat(payload, "preferredHeight");
            
            if (payload.ContainsKey("flexibleWidth"))
                element.flexibleWidth = GetFloat(payload, "flexibleWidth");
            
            if (payload.ContainsKey("flexibleHeight"))
                element.flexibleHeight = GetFloat(payload, "flexibleHeight");
            
            if (payload.ContainsKey("ignoreLayout"))
                element.ignoreLayout = GetBool(payload, "ignoreLayout");
        }
        
        private void ApplyAspectRatioFitterSettings(AspectRatioFitter fitter, Dictionary<string, object> payload)
        {
            var aspectMode = GetString(payload, "aspectMode");
            if (!string.IsNullOrEmpty(aspectMode))
            {
                fitter.aspectMode = (AspectRatioFitter.AspectMode)Enum.Parse(typeof(AspectRatioFitter.AspectMode), aspectMode);
            }
            
            if (payload.ContainsKey("aspectRatio"))
                fitter.aspectRatio = GetFloat(payload, "aspectRatio");
        }
        
        #endregion
        
        #region Serialize Methods
        
        private Dictionary<string, object> SerializeLayoutGroup(Component component, string typeName)
        {
            var layoutGroup = component as HorizontalOrVerticalLayoutGroup;
            return new Dictionary<string, object>
            {
                ["type"] = typeName,
                ["padding"] = new Dictionary<string, object>
                {
                    ["left"] = layoutGroup.padding.left,
                    ["right"] = layoutGroup.padding.right,
                    ["top"] = layoutGroup.padding.top,
                    ["bottom"] = layoutGroup.padding.bottom,
                },
                ["spacing"] = layoutGroup.spacing,
                ["childAlignment"] = layoutGroup.childAlignment.ToString(),
                ["childControlWidth"] = layoutGroup.childControlWidth,
                ["childControlHeight"] = layoutGroup.childControlHeight,
                ["childForceExpandWidth"] = layoutGroup.childForceExpandWidth,
                ["childForceExpandHeight"] = layoutGroup.childForceExpandHeight,
            };
        }
        
        private Dictionary<string, object> SerializeGridLayoutGroup(GridLayoutGroup grid)
        {
            return new Dictionary<string, object>
            {
                ["type"] = "GridLayoutGroup",
                ["padding"] = new Dictionary<string, object>
                {
                    ["left"] = grid.padding.left,
                    ["right"] = grid.padding.right,
                    ["top"] = grid.padding.top,
                    ["bottom"] = grid.padding.bottom,
                },
                ["cellSize"] = new Dictionary<string, object>
                {
                    ["x"] = grid.cellSize.x,
                    ["y"] = grid.cellSize.y,
                },
                ["spacing"] = new Dictionary<string, object>
                {
                    ["x"] = grid.spacing.x,
                    ["y"] = grid.spacing.y,
                },
                ["childAlignment"] = grid.childAlignment.ToString(),
                ["constraint"] = grid.constraint.ToString(),
                ["constraintCount"] = grid.constraintCount,
                ["startCorner"] = grid.startCorner.ToString(),
                ["startAxis"] = grid.startAxis.ToString(),
            };
        }
        
        private Dictionary<string, object> SerializeContentSizeFitter(ContentSizeFitter fitter)
        {
            return new Dictionary<string, object>
            {
                ["type"] = "ContentSizeFitter",
                ["horizontalFit"] = fitter.horizontalFit.ToString(),
                ["verticalFit"] = fitter.verticalFit.ToString(),
            };
        }
        
        private Dictionary<string, object> SerializeLayoutElement(LayoutElement element)
        {
            return new Dictionary<string, object>
            {
                ["type"] = "LayoutElement",
                ["minWidth"] = element.minWidth,
                ["minHeight"] = element.minHeight,
                ["preferredWidth"] = element.preferredWidth,
                ["preferredHeight"] = element.preferredHeight,
                ["flexibleWidth"] = element.flexibleWidth,
                ["flexibleHeight"] = element.flexibleHeight,
                ["ignoreLayout"] = element.ignoreLayout,
            };
        }
        
        private Dictionary<string, object> SerializeAspectRatioFitter(AspectRatioFitter fitter)
        {
            return new Dictionary<string, object>
            {
                ["type"] = "AspectRatioFitter",
                ["aspectMode"] = fitter.aspectMode.ToString(),
                ["aspectRatio"] = fitter.aspectRatio,
            };
        }
        
        #endregion
        
        #region Helper Methods
        
        private string GetGameObjectPath(GameObject go)
        {
            string path = go.name;
            Transform parent = go.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }
        
        #endregion
        
        protected override bool RequiresCompilationWait(string operation)
        {
            // Inspect operation doesn't require compilation wait
            return operation != "inspect";
        }
    }
}

