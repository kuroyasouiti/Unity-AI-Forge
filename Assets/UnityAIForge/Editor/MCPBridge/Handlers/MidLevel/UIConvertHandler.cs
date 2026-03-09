using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using MCP.Editor.Base;
using MCP.Editor.CodeGen;
using MCP.Editor.Utilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MCP.Editor.Handlers
{
    /// <summary>
    /// UGUI ↔ UI Toolkit 変換分析ハンドラー。
    /// Canvas階層またはUXMLファイルを解析し、変換可否と課題を報告します。
    /// </summary>
    public class UIConvertHandler : BaseCommandHandler
    {
        public override string Category => "uiConvert";

        private static readonly string[] Operations =
        {
            "analyze",
            "toUITK",
            "toUGUI",
            "extractStyles",
        };

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) => false;

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload) =>
            operation switch
            {
                "analyze" => Analyze(payload),
                "toUITK" => ConvertToUITK(payload),
                "toUGUI" => ConvertToUGUI(payload),
                "extractStyles" => ExtractStylesOp(payload),
                _ => throw new InvalidOperationException($"Unknown operation: {operation}")
            };

        #region Analyze

        private object Analyze(Dictionary<string, object> payload)
        {
            var sourceType = GetString(payload, "sourceType");
            var sourcePath = GetString(payload, "sourcePath");

            if (string.IsNullOrEmpty(sourceType))
                throw new InvalidOperationException("'sourceType' is required (ugui or uitk).");
            if (string.IsNullOrEmpty(sourcePath))
                throw new InvalidOperationException("'sourcePath' is required.");

            return sourceType.ToLowerInvariant() switch
            {
                "ugui" => AnalyzeUGUI(sourcePath),
                "uitk" => AnalyzeUITK(sourcePath),
                _ => throw new InvalidOperationException(
                    $"Unsupported sourceType: '{sourceType}'. Use 'ugui' or 'uitk'.")
            };
        }

        #endregion

        #region UGUI → UITK Analysis

        private object AnalyzeUGUI(string canvasPath)
        {
            var root = ResolveGameObject(canvasPath);

            var convertible = new List<Dictionary<string, object>>();
            var warnings = new List<Dictionary<string, object>>();
            var unsupported = new List<Dictionary<string, object>>();

            AnalyzeUGUIElement(root, convertible, warnings, unsupported, 0);

            var summary = new Dictionary<string, object>
            {
                ["convertibleCount"] = convertible.Count,
                ["warningCount"] = warnings.Count,
                ["unsupportedCount"] = unsupported.Count,
                ["estimatedAccuracy"] = EstimateAccuracy(convertible.Count, warnings.Count, unsupported.Count)
            };

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["sourceType"] = "ugui",
                ["targetType"] = "uitk",
                ["sourcePath"] = canvasPath,
                ["summary"] = summary,
                ["convertible"] = convertible,
                ["warnings"] = warnings,
                ["unsupported"] = unsupported
            };
        }

        private void AnalyzeUGUIElement(GameObject go, List<Dictionary<string, object>> convertible,
            List<Dictionary<string, object>> warnings, List<Dictionary<string, object>> unsupported, int depth)
        {
            var path = BuildGameObjectPath(go);
            var uguiType = DetectUGUIType(go);
            var uitkEquivalent = MapUGUIToUITK(uguiType);
            var layoutInfo = AnalyzeRectTransformLayout(go);
            var styleInfo = ExtractUGUIStyles(go, uguiType);

            var entry = new Dictionary<string, object>
            {
                ["path"] = path,
                ["uguiType"] = uguiType,
                ["uitkType"] = uitkEquivalent ?? "N/A",
                ["depth"] = depth
            };

            if (styleInfo.Count > 0)
                entry["styles"] = styleInfo;

            if (layoutInfo != null)
                entry["layout"] = layoutInfo;

            // Classify
            if (uitkEquivalent == null)
            {
                entry["reason"] = GetUnsupportedReason(uguiType, go);
                unsupported.Add(entry);
            }
            else
            {
                // Check for conversion warnings
                var elementWarnings = GetConversionWarnings(go, uguiType, layoutInfo);
                if (elementWarnings.Count > 0)
                {
                    entry["warnings"] = elementWarnings;
                    warnings.Add(entry);
                }
                else
                {
                    convertible.Add(entry);
                }
            }

            // Recurse children
            for (int i = 0; i < go.transform.childCount; i++)
            {
                var child = go.transform.GetChild(i).gameObject;
                AnalyzeUGUIElement(child, convertible, warnings, unsupported, depth + 1);
            }
        }

        private string DetectUGUIType(GameObject go)
        {
            if (go.GetComponent<Canvas>() != null) return "Canvas";
            if (go.GetComponent<UnityEngine.UI.Button>() != null) return "Button";
            if (go.GetComponent<InputField>() != null) return "InputField";

            // TMP InputField (check before Toggle since TMP_InputField may also have Toggle-like structure)
            var tmpInputType = Type.GetType("TMPro.TMP_InputField, Unity.TextMeshPro");
            if (tmpInputType != null && go.GetComponent(tmpInputType) != null)
                return "TMP_InputField";

            if (go.GetComponent<UnityEngine.UI.Toggle>() != null) return "Toggle";
            if (go.GetComponent<UnityEngine.UI.Slider>() != null) return "Slider";
            if (go.GetComponent<Dropdown>() != null) return "Dropdown";
            if (go.GetComponent<ScrollRect>() != null) return "ScrollRect";

            // TMP text
            var tmpType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
            if (tmpType != null && go.GetComponent(tmpType) != null)
                return "TextMeshPro";

            if (go.GetComponent<Text>() != null) return "Text";
            if (go.GetComponent<RawImage>() != null) return "RawImage";
            if (go.GetComponent<UnityEngine.UI.Image>() != null) return "Image";

            // Layout containers
            if (go.GetComponent<HorizontalLayoutGroup>() != null) return "HorizontalLayoutGroup";
            if (go.GetComponent<VerticalLayoutGroup>() != null) return "VerticalLayoutGroup";
            if (go.GetComponent<GridLayoutGroup>() != null) return "GridLayoutGroup";

            var rect = go.GetComponent<RectTransform>();
            return rect != null ? "RectTransform" : "GameObject";
        }

        private string MapUGUIToUITK(string uguiType) => uguiType switch
        {
            "Canvas" => "UIDocument",
            "Button" => "Button",
            "Text" => "Label",
            "TextMeshPro" => "Label",
            "Image" => "VisualElement",
            "RawImage" => "VisualElement",
            "InputField" => "TextField",
            "TMP_InputField" => "TextField",
            "Toggle" => "Toggle",
            "Slider" => "Slider",
            "Dropdown" => "DropdownField",
            "ScrollRect" => "ScrollView",
            "HorizontalLayoutGroup" => "VisualElement",
            "VerticalLayoutGroup" => "VisualElement",
            "GridLayoutGroup" => "VisualElement",
            "RectTransform" => "VisualElement",
            "GameObject" => null, // Non-UI element
            _ => null
        };

        private Dictionary<string, object> AnalyzeRectTransformLayout(GameObject go)
        {
            var rect = go.GetComponent<RectTransform>();
            if (rect == null) return null;

            var anchorMin = rect.anchorMin;
            var anchorMax = rect.anchorMax;
            var pattern = ClassifyAnchorPattern(anchorMin, anchorMax);

            var info = new Dictionary<string, object>
            {
                ["anchorPattern"] = pattern,
                ["sizeDelta"] = new Dictionary<string, object> { ["x"] = rect.sizeDelta.x, ["y"] = rect.sizeDelta.y },
                ["anchorMin"] = new Dictionary<string, object> { ["x"] = anchorMin.x, ["y"] = anchorMin.y },
                ["anchorMax"] = new Dictionary<string, object> { ["x"] = anchorMax.x, ["y"] = anchorMax.y },
            };

            // Map to USS
            var ussMapping = MapAnchorToUSS(pattern, rect);
            if (ussMapping != null)
                info["ussEquivalent"] = ussMapping;

            // Layout group info
            var hLayout = go.GetComponent<HorizontalLayoutGroup>();
            var vLayout = go.GetComponent<VerticalLayoutGroup>();
            var gridLayout = go.GetComponent<GridLayoutGroup>();

            if (hLayout != null)
            {
                info["layoutDirection"] = "row";
                info["ussFlexDirection"] = "row";
                info["spacing"] = hLayout.spacing;
                info["padding"] = PaddingToDict(hLayout.padding);
            }
            else if (vLayout != null)
            {
                info["layoutDirection"] = "column";
                info["ussFlexDirection"] = "column";
                info["spacing"] = vLayout.spacing;
                info["padding"] = PaddingToDict(vLayout.padding);
            }
            else if (gridLayout != null)
            {
                info["layoutDirection"] = "grid";
                info["ussFlexDirection"] = "row";
                info["ussFlexWrap"] = "wrap";
                info["cellSize"] = new Dictionary<string, object>
                {
                    ["x"] = gridLayout.cellSize.x, ["y"] = gridLayout.cellSize.y
                };
                info["spacing"] = new Dictionary<string, object>
                {
                    ["x"] = gridLayout.spacing.x, ["y"] = gridLayout.spacing.y
                };
            }

            return info;
        }

        private string ClassifyAnchorPattern(Vector2 min, Vector2 max)
        {
            bool stretchH = Mathf.Approximately(min.x, 0) && Mathf.Approximately(max.x, 1);
            bool stretchV = Mathf.Approximately(min.y, 0) && Mathf.Approximately(max.y, 1);
            bool pointAnchor = Mathf.Approximately(min.x, max.x) && Mathf.Approximately(min.y, max.y);

            if (stretchH && stretchV) return "stretchAll";
            if (stretchH) return "stretchHorizontal";
            if (stretchV) return "stretchVertical";
            if (pointAnchor)
            {
                float x = min.x, y = min.y;
                if (Mathf.Approximately(x, 0.5f) && Mathf.Approximately(y, 0.5f)) return "center";
                if (Mathf.Approximately(x, 0) && Mathf.Approximately(y, 1)) return "topLeft";
                if (Mathf.Approximately(x, 0.5f) && Mathf.Approximately(y, 1)) return "topCenter";
                if (Mathf.Approximately(x, 1) && Mathf.Approximately(y, 1)) return "topRight";
                if (Mathf.Approximately(x, 0) && Mathf.Approximately(y, 0)) return "bottomLeft";
                if (Mathf.Approximately(x, 0.5f) && Mathf.Approximately(y, 0)) return "bottomCenter";
                if (Mathf.Approximately(x, 1) && Mathf.Approximately(y, 0)) return "bottomRight";
                if (Mathf.Approximately(x, 0) && Mathf.Approximately(y, 0.5f)) return "middleLeft";
                if (Mathf.Approximately(x, 1) && Mathf.Approximately(y, 0.5f)) return "middleRight";
                return "fixedPoint";
            }
            return "custom";
        }

        private Dictionary<string, string> MapAnchorToUSS(string pattern, RectTransform rect)
        {
            var uss = new Dictionary<string, string>();

            switch (pattern)
            {
                case "stretchAll":
                    uss["flex-grow"] = "1";
                    uss["width"] = "100%";
                    uss["height"] = "100%";
                    break;
                case "stretchHorizontal":
                    uss["width"] = "100%";
                    uss["height"] = $"{rect.sizeDelta.y}px";
                    break;
                case "stretchVertical":
                    uss["width"] = $"{rect.sizeDelta.x}px";
                    uss["height"] = "100%";
                    break;
                case "center":
                    uss["align-self"] = "center";
                    if (rect.sizeDelta.x > 0) uss["width"] = $"{rect.sizeDelta.x}px";
                    if (rect.sizeDelta.y > 0) uss["height"] = $"{rect.sizeDelta.y}px";
                    break;
                default:
                    // Fixed position — use absolute
                    if (rect.sizeDelta.x > 0) uss["width"] = $"{rect.sizeDelta.x}px";
                    if (rect.sizeDelta.y > 0) uss["height"] = $"{rect.sizeDelta.y}px";
                    if (pattern.StartsWith("top") || pattern.StartsWith("bottom") ||
                        pattern == "fixedPoint" || pattern == "custom")
                    {
                        uss["position"] = "absolute";
                    }
                    break;
            }

            return uss.Count > 0 ? uss : null;
        }

        private Dictionary<string, string> ExtractUGUIStyles(GameObject go, string uguiType)
        {
            var styles = new Dictionary<string, string>();

            // Image/background color
            var image = go.GetComponent<UnityEngine.UI.Image>();
            if (image != null && image.color != Color.white)
            {
                styles["background-color"] = ColorToCSS(image.color);
                if (image.sprite != null)
                {
                    var spritePath = AssetDatabase.GetAssetPath(image.sprite);
                    if (!string.IsNullOrEmpty(spritePath))
                        styles["background-image"] = spritePath;
                }
            }

            // Text styles
            var text = go.GetComponent<Text>();
            if (text != null)
            {
                styles["font-size"] = $"{text.fontSize}px";
                if (text.color != Color.black && text.color != Color.white)
                    styles["color"] = ColorToCSS(text.color);
                styles["-unity-text-align"] = MapTextAlignment(text.alignment);
            }

            // TMP text styles
            var tmpType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
            if (tmpType != null)
            {
                var tmp = go.GetComponent(tmpType);
                if (tmp != null)
                {
                    var fontSizeProp = tmpType.GetProperty("fontSize");
                    if (fontSizeProp != null)
                        styles["font-size"] = $"{fontSizeProp.GetValue(tmp)}px";

                    var colorProp = tmpType.GetProperty("color");
                    if (colorProp != null)
                    {
                        var c = (Color)colorProp.GetValue(tmp);
                        if (c != Color.black && c != Color.white)
                            styles["color"] = ColorToCSS(c);
                    }
                }
            }

            // CanvasGroup → opacity
            var cg = go.GetComponent<CanvasGroup>();
            if (cg != null && cg.alpha < 1f)
                styles["opacity"] = cg.alpha.ToString("F2");

            // ContentSizeFitter
            var csf = go.GetComponent<ContentSizeFitter>();
            if (csf != null)
            {
                if (csf.horizontalFit == ContentSizeFitter.FitMode.PreferredSize)
                    styles["flex-shrink"] = "0";
                if (csf.verticalFit == ContentSizeFitter.FitMode.PreferredSize)
                    styles["flex-shrink"] = "0";
            }

            return styles;
        }

        private List<string> GetConversionWarnings(GameObject go, string uguiType,
            Dictionary<string, object> layoutInfo)
        {
            var warns = new List<string>();

            // Anchor pattern warnings
            if (layoutInfo != null && layoutInfo.ContainsKey("anchorPattern"))
            {
                var pattern = layoutInfo["anchorPattern"].ToString();
                if (pattern == "custom")
                    warns.Add("Custom anchor values require manual Flexbox layout adjustment");
                if (pattern == "fixedPoint")
                    warns.Add("Fixed point anchoring maps to position:absolute (may need manual tuning)");
            }

            // UnityEvent callbacks
            var button = go.GetComponent<UnityEngine.UI.Button>();
            if (button != null && button.onClick.GetPersistentEventCount() > 0)
                warns.Add($"Button.onClick has {button.onClick.GetPersistentEventCount()} persistent listener(s) — manual C# callback wiring required");

            // Mask component
            if (go.GetComponent<Mask>() != null)
                warns.Add("Mask component has no direct UITK equivalent — use overflow:hidden in USS");

            // Animation-driven UI
            var animator = go.GetComponent<Animator>();
            if (animator != null)
                warns.Add("Animator-driven UI requires manual USS transition/animation rewrite");

            // GridLayoutGroup complexity
            if (uguiType == "GridLayoutGroup")
                warns.Add("GridLayoutGroup maps to flex-wrap:wrap — column count needs USS width-based calculation");

            // Canvas nested inside another Canvas
            if (uguiType != "Canvas" && go.GetComponent<Canvas>() != null)
                warns.Add("Nested Canvas — UITK uses single UIDocument tree, consider flattening");

            // RawImage with texture
            if (uguiType == "RawImage")
            {
                var rawImage = go.GetComponent<RawImage>();
                if (rawImage != null && rawImage.texture != null)
                    warns.Add("RawImage.texture maps to background-image — UV rect adjustments will be lost");
            }

            return warns;
        }

        private string GetUnsupportedReason(string uguiType, GameObject go)
        {
            if (uguiType == "GameObject")
                return "Non-UI GameObject (no RectTransform) — cannot convert to UITK";

            // Check for custom/unknown components
            var components = go.GetComponents<Component>();
            var customComponents = components
                .Where(c => c != null && !IsStandardUIComponent(c))
                .Select(c => c.GetType().Name)
                .ToList();

            if (customComponents.Count > 0)
                return $"Contains custom components ({string.Join(", ", customComponents)}) — manual migration required";

            return $"Unsupported UGUI type: {uguiType}";
        }

        private bool IsStandardUIComponent(Component c)
        {
            var type = c.GetType();
            var ns = type.Namespace ?? "";
            return ns.StartsWith("UnityEngine") || ns.StartsWith("TMPro") || ns == "";
        }

        #endregion

        #region UITK → UGUI Analysis

        private object AnalyzeUITK(string uxmlPath)
        {
            if (!File.Exists(uxmlPath))
                throw new InvalidOperationException($"UXML file not found: {uxmlPath}");

            var content = File.ReadAllText(uxmlPath);
            XDocument doc;
            try
            {
                doc = XDocument.Parse(content);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to parse UXML: {ex.Message}");
            }

            var convertible = new List<Dictionary<string, object>>();
            var warnings = new List<Dictionary<string, object>>();
            var unsupported = new List<Dictionary<string, object>>();

            // Find USS references for style analysis
            XNamespace uiNs = "UnityEngine.UIElements";
            var styleRefs = doc.Descendants(uiNs + "Style")
                .Select(e => e.Attribute("src")?.Value)
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();

            // Parse USS files for style info
            var ussRules = new Dictionary<string, Dictionary<string, string>>();
            foreach (var styleRef in styleRefs)
            {
                var ussPath = ResolveRelativePath(uxmlPath, styleRef);
                if (File.Exists(ussPath))
                    ParseUSSRules(File.ReadAllText(ussPath), ussRules);
            }

            // Analyze UXML elements
            var rootElement = doc.Root;
            if (rootElement != null)
            {
                foreach (var child in rootElement.Elements())
                {
                    AnalyzeUITKElement(child, uiNs, ussRules, convertible, warnings, unsupported, 0);
                }
            }

            var summary = new Dictionary<string, object>
            {
                ["convertibleCount"] = convertible.Count,
                ["warningCount"] = warnings.Count,
                ["unsupportedCount"] = unsupported.Count,
                ["styleSheets"] = styleRefs,
                ["estimatedAccuracy"] = EstimateAccuracy(convertible.Count, warnings.Count, unsupported.Count)
            };

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["sourceType"] = "uitk",
                ["targetType"] = "ugui",
                ["sourcePath"] = uxmlPath,
                ["summary"] = summary,
                ["convertible"] = convertible,
                ["warnings"] = warnings,
                ["unsupported"] = unsupported
            };
        }

        private void AnalyzeUITKElement(XElement element, XNamespace uiNs,
            Dictionary<string, Dictionary<string, string>> ussRules,
            List<Dictionary<string, object>> convertible,
            List<Dictionary<string, object>> warnings,
            List<Dictionary<string, object>> unsupported, int depth)
        {
            // Skip Style elements
            if (element.Name == uiNs + "Style") return;

            var typeName = element.Name.LocalName;
            var elementName = element.Attribute("name")?.Value ?? "";
            var classes = element.Attribute("class")?.Value ?? "";
            var uguiEquivalent = MapUITKToUGUI(typeName);

            var entry = new Dictionary<string, object>
            {
                ["uitkType"] = typeName,
                ["uguiType"] = uguiEquivalent ?? "N/A",
                ["depth"] = depth
            };

            if (!string.IsNullOrEmpty(elementName))
                entry["name"] = elementName;
            if (!string.IsNullOrEmpty(classes))
                entry["classes"] = classes;

            // Extract inline styles
            var inlineStyle = element.Attribute("style")?.Value;
            if (!string.IsNullOrEmpty(inlineStyle))
                entry["inlineStyle"] = inlineStyle;

            // Resolve USS styles for this element
            var resolvedStyles = ResolveUSSStyles(elementName, classes, typeName, ussRules);
            if (resolvedStyles.Count > 0)
                entry["resolvedStyles"] = resolvedStyles;

            // Classify
            if (uguiEquivalent == null)
            {
                entry["reason"] = GetUITKUnsupportedReason(typeName);
                unsupported.Add(entry);
            }
            else
            {
                var elementWarnings = GetUITKConversionWarnings(typeName, inlineStyle, resolvedStyles);
                if (elementWarnings.Count > 0)
                {
                    entry["warnings"] = elementWarnings;
                    warnings.Add(entry);
                }
                else
                {
                    convertible.Add(entry);
                }
            }

            // Recurse children
            foreach (var child in element.Elements())
            {
                AnalyzeUITKElement(child, uiNs, ussRules, convertible, warnings, unsupported, depth + 1);
            }
        }

        private string MapUITKToUGUI(string uitkType) => uitkType switch
        {
            "VisualElement" => "Image/RectTransform",
            "Button" => "Button",
            "Label" => "Text/TextMeshPro",
            "TextField" => "InputField/TMP_InputField",
            "Toggle" => "Toggle",
            "Slider" => "Slider",
            "SliderInt" => "Slider",
            "DropdownField" => "Dropdown",
            "ScrollView" => "ScrollRect",
            "Foldout" => "Toggle+Panel",
            "GroupBox" => "Image",
            "RadioButton" => "Toggle",
            "RadioButtonGroup" => "ToggleGroup",
            "ProgressBar" => "Slider",
            "Image" => "RawImage",
            "IntegerField" => "InputField",
            "FloatField" => "InputField",
            "MinMaxSlider" => null, // No UGUI equivalent
            "ListView" => null,    // Complex — needs custom implementation
            _ => null
        };

        private string GetUITKUnsupportedReason(string uitkType) => uitkType switch
        {
            "MinMaxSlider" => "MinMaxSlider has no UGUI equivalent — requires custom implementation",
            "ListView" => "ListView virtualization has no UGUI equivalent — use ScrollRect + LayoutGroup manually",
            _ => $"Unknown UITK element type: {uitkType}"
        };

        private List<string> GetUITKConversionWarnings(string typeName, string inlineStyle,
            Dictionary<string, string> resolvedStyles)
        {
            var warns = new List<string>();

            // Flexbox-based layout
            var allStyles = new Dictionary<string, string>(resolvedStyles);
            if (!string.IsNullOrEmpty(inlineStyle))
            {
                foreach (var prop in ParseInlineStyle(inlineStyle))
                    allStyles[prop.Key] = prop.Value;
            }

            if (allStyles.ContainsKey("flex-grow") || allStyles.ContainsKey("flex-shrink"))
                warns.Add("Flexbox flex-grow/flex-shrink need manual RectTransform anchor configuration");

            if (allStyles.ContainsKey("position") && allStyles["position"] == "absolute")
                warns.Add("position:absolute maps to RectTransform anchored positioning");

            if (allStyles.ContainsKey("transition"))
                warns.Add("USS transitions have no UGUI equivalent — use Animator or DOTween");

            if (typeName == "Foldout")
                warns.Add("Foldout maps to Toggle+Panel combination — requires manual assembly");

            if (typeName == "RadioButton" || typeName == "RadioButtonGroup")
                warns.Add("RadioButton maps to Toggle with ToggleGroup — requires manual ToggleGroup setup");

            if (typeName == "ProgressBar")
                warns.Add("ProgressBar maps to Slider — visual styling differs significantly");

            return warns;
        }

        #endregion

        #region USS Parsing Helpers

        private void ParseUSSRules(string ussContent, Dictionary<string, Dictionary<string, string>> rules)
        {
            // Simple USS parser: selector { prop: value; }
            var rulePattern = new System.Text.RegularExpressions.Regex(
                @"([^{]+)\{([^}]*)\}", System.Text.RegularExpressions.RegexOptions.Singleline);
            var propPattern = new System.Text.RegularExpressions.Regex(
                @"([\w-]+)\s*:\s*([^;]+);");

            foreach (System.Text.RegularExpressions.Match match in rulePattern.Matches(ussContent))
            {
                var selector = match.Groups[1].Value.Trim();
                var body = match.Groups[2].Value;
                var props = new Dictionary<string, string>();

                foreach (System.Text.RegularExpressions.Match propMatch in propPattern.Matches(body))
                {
                    props[propMatch.Groups[1].Value.Trim()] = propMatch.Groups[2].Value.Trim();
                }

                if (props.Count > 0)
                    rules[selector] = props;
            }
        }

        private Dictionary<string, string> ResolveUSSStyles(string name, string classes, string typeName,
            Dictionary<string, Dictionary<string, string>> ussRules)
        {
            var resolved = new Dictionary<string, string>();

            // Type selector
            if (ussRules.TryGetValue(typeName, out var typeStyles))
                foreach (var kv in typeStyles) resolved[kv.Key] = kv.Value;

            // Class selectors
            foreach (var cls in classes.Split(' ').Where(c => !string.IsNullOrEmpty(c)))
            {
                if (ussRules.TryGetValue("." + cls, out var classStyles))
                    foreach (var kv in classStyles) resolved[kv.Key] = kv.Value;
            }

            // Name selector
            if (!string.IsNullOrEmpty(name) && ussRules.TryGetValue("#" + name, out var nameStyles))
                foreach (var kv in nameStyles) resolved[kv.Key] = kv.Value;

            return resolved;
        }

        private Dictionary<string, string> ParseInlineStyle(string style)
        {
            var result = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(style)) return result;

            foreach (var part in style.Split(';').Where(s => !string.IsNullOrWhiteSpace(s)))
            {
                var colonIdx = part.IndexOf(':');
                if (colonIdx > 0)
                {
                    var key = part.Substring(0, colonIdx).Trim();
                    var value = part.Substring(colonIdx + 1).Trim();
                    result[key] = value;
                }
            }
            return result;
        }

        private string ResolveRelativePath(string basePath, string relativePath)
        {
            var dir = Path.GetDirectoryName(basePath) ?? "";
            return Path.Combine(dir, relativePath).Replace("\\", "/");
        }

        #endregion

        #region toUITK — UGUI Canvas → UXML + USS

        private object ConvertToUITK(Dictionary<string, object> payload)
        {
            var sourcePath = GetString(payload, "sourcePath");
            var outputDir = GetString(payload, "outputDir", "Assets/UI/Generated");
            var outputName = GetString(payload, "outputName", "ConvertedUI");

            if (string.IsNullOrEmpty(sourcePath))
                throw new InvalidOperationException("'sourcePath' is required (Canvas GameObject path).");

            var root = ResolveGameObject(sourcePath);

            // Build UXML
            var builder = new UXMLBuilder();
            var ussFileName = outputName + ".uss";
            builder.AddStyleSheet(ussFileName);

            // Collect USS rules during traversal
            var ussRules = new List<KeyValuePair<string, Dictionary<string, string>>>();
            int classCounter = 0;

            var rootElement = builder.AddVisualElement("root", "converted-root");
            BuildUITKTree(root, rootElement, builder, ussRules, ref classCounter, 0);

            // Build USS
            var ussSb = new StringBuilder();
            ussSb.AppendLine(".converted-root {");
            ussSb.AppendLine("    flex-grow: 1;");
            ussSb.AppendLine("}");

            foreach (var rule in ussRules)
            {
                ussSb.AppendLine();
                ussSb.AppendLine($"{rule.Key} {{");
                foreach (var prop in rule.Value)
                    ussSb.AppendLine($"    {prop.Key}: {prop.Value};");
                ussSb.AppendLine("}");
            }

            // Write files (USS first, then UXML)
            var (uxmlPath, ussPath) = UITKGenerationHelper.WriteUXMLAndUSS(
                outputDir, outputName, builder.ToString(), ussSb.ToString());

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["operation"] = "toUITK",
                ["uxmlPath"] = uxmlPath,
                ["ussPath"] = ussPath,
                ["elementCount"] = classCounter,
                ["ussRuleCount"] = ussRules.Count + 1 // +1 for .converted-root
            };
        }

        private void BuildUITKTree(GameObject go, XElement parent, UXMLBuilder builder,
            List<KeyValuePair<string, Dictionary<string, string>>> ussRules, ref int counter, int depth)
        {
            // Skip Canvas root itself (maps to UIDocument, not a VisualElement)
            bool isCanvas = go.GetComponent<Canvas>() != null;

            XElement currentElement;
            if (isCanvas && depth == 0)
            {
                // Canvas children go directly under parent
                currentElement = parent;
            }
            else
            {
                var uguiType = DetectUGUIType(go);
                var uitkType = MapUGUIToUITK(uguiType);
                if (uitkType == null) uitkType = "VisualElement";

                var elementName = SanitizeName(go.name);
                var className = $"c{counter++}";

                // Create element
                currentElement = builder.AddElement(parent, uitkType, elementName);

                // Extract text content
                var textContent = GetTextContent(go);
                if (textContent != null)
                    currentElement.SetAttributeValue("text", textContent);

                // Build USS rule for this element
                var styles = ExtractUGUIStyles(go, uguiType);
                var layoutStyles = ExtractLayoutUSS(go);
                foreach (var kv in layoutStyles)
                    styles[kv.Key] = kv.Value;

                if (styles.Count > 0)
                {
                    currentElement.SetAttributeValue("class", className);
                    ussRules.Add(new KeyValuePair<string, Dictionary<string, string>>(
                        "." + className, styles));
                }
            }

            // Recurse children (skip internal children of compound elements like Slider/ScrollRect)
            var uType = DetectUGUIType(go);
            if (!IsCompoundElement(uType))
            {
                for (int i = 0; i < go.transform.childCount; i++)
                {
                    var child = go.transform.GetChild(i).gameObject;
                    BuildUITKTree(child, currentElement, builder, ussRules, ref counter, depth + 1);
                }
            }
        }

        private Dictionary<string, string> ExtractLayoutUSS(GameObject go)
        {
            var styles = new Dictionary<string, string>();
            var rect = go.GetComponent<RectTransform>();
            if (rect == null) return styles;

            var pattern = ClassifyAnchorPattern(rect.anchorMin, rect.anchorMax);
            var ussMapping = MapAnchorToUSS(pattern, rect);
            if (ussMapping != null)
            {
                foreach (var kv in ussMapping)
                    styles[kv.Key] = kv.Value;
            }

            // LayoutGroup → flex-direction
            var hLayout = go.GetComponent<HorizontalLayoutGroup>();
            var vLayout = go.GetComponent<VerticalLayoutGroup>();
            var gridLayout = go.GetComponent<GridLayoutGroup>();

            if (hLayout != null)
            {
                styles["flex-direction"] = "row";
                if (hLayout.spacing > 0)
                {
                    // USS gap not available in 2022.3, use margin on children note
                    styles["--spacing"] = $"{hLayout.spacing}px";
                }
                ApplyPaddingUSS(styles, hLayout.padding);
                MapChildAlignment(styles, hLayout.childAlignment);
            }
            else if (vLayout != null)
            {
                styles["flex-direction"] = "column";
                if (vLayout.spacing > 0)
                    styles["--spacing"] = $"{vLayout.spacing}px";
                ApplyPaddingUSS(styles, vLayout.padding);
                MapChildAlignment(styles, vLayout.childAlignment);
            }
            else if (gridLayout != null)
            {
                styles["flex-direction"] = "row";
                styles["flex-wrap"] = "wrap";
                ApplyPaddingUSS(styles, gridLayout.padding);
            }

            return styles;
        }

        private void ApplyPaddingUSS(Dictionary<string, string> styles, RectOffset padding)
        {
            if (padding.top > 0) styles["padding-top"] = $"{padding.top}px";
            if (padding.bottom > 0) styles["padding-bottom"] = $"{padding.bottom}px";
            if (padding.left > 0) styles["padding-left"] = $"{padding.left}px";
            if (padding.right > 0) styles["padding-right"] = $"{padding.right}px";
        }

        private void MapChildAlignment(Dictionary<string, string> styles, TextAnchor alignment)
        {
            switch (alignment)
            {
                case TextAnchor.UpperCenter:
                case TextAnchor.MiddleCenter:
                case TextAnchor.LowerCenter:
                    styles["align-items"] = "center";
                    break;
                case TextAnchor.UpperRight:
                case TextAnchor.MiddleRight:
                case TextAnchor.LowerRight:
                    styles["align-items"] = "flex-end";
                    break;
            }

            switch (alignment)
            {
                case TextAnchor.MiddleLeft:
                case TextAnchor.MiddleCenter:
                case TextAnchor.MiddleRight:
                    styles["justify-content"] = "center";
                    break;
                case TextAnchor.LowerLeft:
                case TextAnchor.LowerCenter:
                case TextAnchor.LowerRight:
                    styles["justify-content"] = "flex-end";
                    break;
            }
        }

        private string GetTextContent(GameObject go)
        {
            var text = go.GetComponent<Text>();
            if (text != null) return text.text;

            var tmpType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
            if (tmpType != null)
            {
                var tmp = go.GetComponent(tmpType);
                if (tmp != null)
                {
                    var textProp = tmpType.GetProperty("text");
                    return textProp?.GetValue(tmp)?.ToString();
                }
            }
            return null;
        }

        private bool IsCompoundElement(string uguiType) =>
            uguiType == "Slider" || uguiType == "ScrollRect" || uguiType == "Dropdown" ||
            uguiType == "InputField" || uguiType == "TMP_InputField";

        private string SanitizeName(string name) =>
            System.Text.RegularExpressions.Regex.Replace(
                name.Replace(" ", "-").ToLowerInvariant(), @"[^a-z0-9\-_]", "");

        #endregion

        #region toUGUI — UXML → Canvas Hierarchy

        private object ConvertToUGUI(Dictionary<string, object> payload)
        {
            var sourcePath = GetString(payload, "sourcePath");
            var parentPath = GetString(payload, "parentPath", "");
            var canvasRenderMode = GetString(payload, "canvasRenderMode", "screenSpaceOverlay");

            if (string.IsNullOrEmpty(sourcePath))
                throw new InvalidOperationException("'sourcePath' is required (UXML asset path).");
            if (!File.Exists(sourcePath))
                throw new InvalidOperationException($"UXML file not found: {sourcePath}");

            var content = File.ReadAllText(sourcePath);
            var doc = XDocument.Parse(content);
            XNamespace uiNs = "UnityEngine.UIElements";

            // Parse USS for style info
            var ussRules = new Dictionary<string, Dictionary<string, string>>();
            foreach (var styleEl in doc.Descendants(uiNs + "Style"))
            {
                var src = styleEl.Attribute("src")?.Value;
                if (!string.IsNullOrEmpty(src))
                {
                    var ussPath = ResolveRelativePath(sourcePath, src);
                    if (File.Exists(ussPath))
                        ParseUSSRules(File.ReadAllText(ussPath), ussRules);
                }
            }

            // Create Canvas
            Transform parentTransform = null;
            if (!string.IsNullOrEmpty(parentPath))
                parentTransform = ResolveGameObject(parentPath).transform;

            var canvasGO = new GameObject("Canvas");
            if (parentTransform != null)
                canvasGO.transform.SetParent(parentTransform, false);

            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = canvasRenderMode switch
            {
                "screenSpaceCamera" => RenderMode.ScreenSpaceCamera,
                "worldSpace" => RenderMode.WorldSpace,
                _ => RenderMode.ScreenSpaceOverlay
            };
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            Undo.RegisterCreatedObjectUndo(canvasGO, "Convert UITK to UGUI");

            int elementCount = 0;
            var warnings = new List<string>();

            // Build UGUI tree from UXML elements
            var rootEl = doc.Root;
            if (rootEl != null)
            {
                foreach (var child in rootEl.Elements())
                {
                    if (child.Name == uiNs + "Style") continue;
                    BuildUGUITree(child, uiNs, canvasGO.transform, ussRules, ref elementCount, warnings);
                }
            }

            EditorUtility.SetDirty(canvasGO);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["operation"] = "toUGUI",
                ["canvasPath"] = BuildGameObjectPath(canvasGO),
                ["elementCount"] = elementCount,
                ["warningCount"] = warnings.Count,
                ["warnings"] = warnings
            };
        }

        private void BuildUGUITree(XElement element, XNamespace uiNs, Transform parent,
            Dictionary<string, Dictionary<string, string>> ussRules,
            ref int elementCount, List<string> warnings)
        {
            if (element.Name == uiNs + "Style") return;

            var typeName = element.Name.LocalName;
            var elName = element.Attribute("name")?.Value ?? typeName;
            var classes = element.Attribute("class")?.Value ?? "";
            var textAttr = element.Attribute("text")?.Value;
            var inlineStyle = element.Attribute("style")?.Value;

            // Resolve all styles
            var styles = ResolveUSSStyles(
                element.Attribute("name")?.Value ?? "", classes, typeName, ussRules);
            if (!string.IsNullOrEmpty(inlineStyle))
                foreach (var kv in ParseInlineStyle(inlineStyle))
                    styles[kv.Key] = kv.Value;

            // Create GameObject based on type
            var go = new GameObject(elName);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            elementCount++;

            switch (typeName)
            {
                case "Button":
                    var img = go.AddComponent<UnityEngine.UI.Image>();
                    ApplyBackgroundStyles(img, styles);
                    go.AddComponent<UnityEngine.UI.Button>();
                    if (!string.IsNullOrEmpty(textAttr))
                    {
                        var textGO = new GameObject("Text");
                        textGO.transform.SetParent(go.transform, false);
                        var textRect = textGO.AddComponent<RectTransform>();
                        SetStretchAll(textRect);
                        var txt = textGO.AddComponent<Text>();
                        txt.text = textAttr;
                        txt.alignment = TextAnchor.MiddleCenter;
                        ApplyTextStyles(txt, styles);
                        elementCount++;
                    }
                    break;

                case "Label":
                    var label = go.AddComponent<Text>();
                    label.text = textAttr ?? "";
                    ApplyTextStyles(label, styles);
                    break;

                case "TextField":
                    var inputImg = go.AddComponent<UnityEngine.UI.Image>();
                    inputImg.color = new Color(1, 1, 1, 0.5f);
                    var inputField = go.AddComponent<InputField>();
                    var inputText = new GameObject("Text");
                    inputText.transform.SetParent(go.transform, false);
                    var itRect = inputText.AddComponent<RectTransform>();
                    SetStretchAll(itRect);
                    var itText = inputText.AddComponent<Text>();
                    itText.supportRichText = false;
                    itText.alignment = TextAnchor.MiddleLeft;
                    inputField.textComponent = itText;
                    elementCount++;
                    break;

                case "Toggle":
                    var toggleImg = go.AddComponent<UnityEngine.UI.Image>();
                    toggleImg.color = Color.clear;
                    var toggle = go.AddComponent<UnityEngine.UI.Toggle>();
                    // Checkmark
                    var checkGO = new GameObject("Checkmark");
                    checkGO.transform.SetParent(go.transform, false);
                    var checkRect = checkGO.AddComponent<RectTransform>();
                    checkRect.sizeDelta = new Vector2(20, 20);
                    var checkImg = checkGO.AddComponent<UnityEngine.UI.Image>();
                    toggle.graphic = checkImg;
                    elementCount++;
                    break;

                case "Slider":
                case "SliderInt":
                    go.AddComponent<UnityEngine.UI.Image>().color = new Color(0.2f, 0.2f, 0.2f);
                    var slider = go.AddComponent<UnityEngine.UI.Slider>();
                    // Fill area
                    var fillArea = new GameObject("Fill Area");
                    fillArea.transform.SetParent(go.transform, false);
                    var faRect = fillArea.AddComponent<RectTransform>();
                    SetStretchAll(faRect);
                    var fill = new GameObject("Fill");
                    fill.transform.SetParent(fillArea.transform, false);
                    var fillRect = fill.AddComponent<RectTransform>();
                    SetStretchAll(fillRect);
                    var fillImg = fill.AddComponent<UnityEngine.UI.Image>();
                    fillImg.color = new Color(0.3f, 0.6f, 1f);
                    slider.fillRect = fillRect;
                    elementCount += 2;
                    break;

                case "ScrollView":
                    var scrollImg = go.AddComponent<UnityEngine.UI.Image>();
                    scrollImg.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);
                    var scrollRect = go.AddComponent<ScrollRect>();
                    var viewport = new GameObject("Viewport");
                    viewport.transform.SetParent(go.transform, false);
                    var vpRect = viewport.AddComponent<RectTransform>();
                    SetStretchAll(vpRect);
                    viewport.AddComponent<UnityEngine.UI.Image>().color = Color.clear;
                    viewport.AddComponent<Mask>().showMaskGraphic = false;
                    var contentGO = new GameObject("Content");
                    contentGO.transform.SetParent(viewport.transform, false);
                    var contentRect = contentGO.AddComponent<RectTransform>();
                    contentRect.anchorMin = new Vector2(0, 1);
                    contentRect.anchorMax = Vector2.one;
                    contentRect.pivot = new Vector2(0.5f, 1);
                    contentGO.AddComponent<VerticalLayoutGroup>();
                    contentGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                    scrollRect.viewport = vpRect;
                    scrollRect.content = contentRect;
                    elementCount += 2;
                    // Children go into content
                    foreach (var child in element.Elements())
                        BuildUGUITree(child, uiNs, contentGO.transform, ussRules, ref elementCount, warnings);
                    return; // Already handled children

                case "DropdownField":
                    warnings.Add($"DropdownField '{elName}' — created as empty Dropdown, options must be set manually");
                    go.AddComponent<UnityEngine.UI.Image>();
                    go.AddComponent<Dropdown>();
                    break;

                case "VisualElement":
                    var veImg = go.AddComponent<UnityEngine.UI.Image>();
                    ApplyBackgroundStyles(veImg, styles);
                    ApplyFlexLayout(go, styles);
                    break;

                case "Foldout":
                    warnings.Add($"Foldout '{elName}' — created as Toggle + child panel, manual wiring needed");
                    go.AddComponent<UnityEngine.UI.Image>().color = Color.clear;
                    go.AddComponent<UnityEngine.UI.Toggle>();
                    break;

                case "ProgressBar":
                    warnings.Add($"ProgressBar '{elName}' — created as Slider, visual styling differs");
                    go.AddComponent<UnityEngine.UI.Image>().color = new Color(0.2f, 0.2f, 0.2f);
                    go.AddComponent<UnityEngine.UI.Slider>();
                    break;

                default:
                    warnings.Add($"Unknown element '{typeName}' named '{elName}' — created as empty RectTransform");
                    break;
            }

            // Apply size from styles
            ApplySizeFromStyles(rect, styles);

            // Recurse children
            foreach (var child in element.Elements())
                BuildUGUITree(child, uiNs, go.transform, ussRules, ref elementCount, warnings);
        }

        private void ApplyBackgroundStyles(UnityEngine.UI.Image img, Dictionary<string, string> styles)
        {
            if (styles.TryGetValue("background-color", out var bgColor))
                img.color = ParseCSSColor(bgColor);
        }

        private void ApplyTextStyles(Text text, Dictionary<string, string> styles)
        {
            if (styles.TryGetValue("font-size", out var fs))
            {
                if (int.TryParse(fs.Replace("px", "").Trim(), out var size))
                    text.fontSize = size;
            }
            if (styles.TryGetValue("color", out var color))
                text.color = ParseCSSColor(color);
            if (styles.TryGetValue("-unity-text-align", out var align))
                text.alignment = ParseTextAlignment(align);
        }

        private void ApplyFlexLayout(GameObject go, Dictionary<string, string> styles)
        {
            if (styles.TryGetValue("flex-direction", out var dir))
            {
                if (dir == "row")
                    go.AddComponent<HorizontalLayoutGroup>();
                else
                    go.AddComponent<VerticalLayoutGroup>();
            }
        }

        private void ApplySizeFromStyles(RectTransform rect, Dictionary<string, string> styles)
        {
            float w = rect.sizeDelta.x, h = rect.sizeDelta.y;
            bool hasWidth = false, hasHeight = false;

            if (styles.TryGetValue("width", out var ws) && ws.EndsWith("px"))
            {
                if (float.TryParse(ws.Replace("px", ""), out var wv)) { w = wv; hasWidth = true; }
            }
            if (styles.TryGetValue("height", out var hs) && hs.EndsWith("px"))
            {
                if (float.TryParse(hs.Replace("px", ""), out var hv)) { h = hv; hasHeight = true; }
            }

            if (hasWidth || hasHeight)
                rect.sizeDelta = new Vector2(w, h);

            // Stretch
            if (styles.TryGetValue("width", out var ws2) && ws2 == "100%" &&
                styles.TryGetValue("height", out var hs2) && hs2 == "100%")
            {
                SetStretchAll(rect);
            }
        }

        private void SetStretchAll(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private Color ParseCSSColor(string css)
        {
            css = css.Trim();
            if (css.StartsWith("rgba(") && css.EndsWith(")"))
            {
                var inner = css.Substring(5, css.Length - 6);
                var parts = inner.Split(',');
                if (parts.Length >= 3)
                {
                    float.TryParse(parts[0].Trim(), out var r);
                    float.TryParse(parts[1].Trim(), out var g);
                    float.TryParse(parts[2].Trim(), out var b);
                    float a = 1f;
                    if (parts.Length >= 4) float.TryParse(parts[3].Trim(), out a);
                    return new Color(r / 255f, g / 255f, b / 255f, a);
                }
            }
            if (css.StartsWith("rgb(") && css.EndsWith(")"))
            {
                var inner = css.Substring(4, css.Length - 5);
                var parts = inner.Split(',');
                if (parts.Length >= 3)
                {
                    float.TryParse(parts[0].Trim(), out var r);
                    float.TryParse(parts[1].Trim(), out var g);
                    float.TryParse(parts[2].Trim(), out var b);
                    return new Color(r / 255f, g / 255f, b / 255f, 1f);
                }
            }
            if (css.StartsWith("#") && ColorUtility.TryParseHtmlString(css, out var htmlColor))
                return htmlColor;
            return Color.white;
        }

        private TextAnchor ParseTextAlignment(string align) => align switch
        {
            "upper-left" => TextAnchor.UpperLeft,
            "upper-center" => TextAnchor.UpperCenter,
            "upper-right" => TextAnchor.UpperRight,
            "middle-left" => TextAnchor.MiddleLeft,
            "middle-center" => TextAnchor.MiddleCenter,
            "middle-right" => TextAnchor.MiddleRight,
            "lower-left" => TextAnchor.LowerLeft,
            "lower-center" => TextAnchor.LowerCenter,
            "lower-right" => TextAnchor.LowerRight,
            _ => TextAnchor.UpperLeft
        };

        #endregion

        #region extractStyles — UGUI → USS Only

        private object ExtractStylesOp(Dictionary<string, object> payload)
        {
            var sourcePath = GetString(payload, "sourcePath");
            var outputPath = GetString(payload, "outputPath");

            if (string.IsNullOrEmpty(sourcePath))
                throw new InvalidOperationException("'sourcePath' is required (Canvas GameObject path).");
            if (string.IsNullOrEmpty(outputPath))
                throw new InvalidOperationException("'outputPath' is required (e.g., 'Assets/UI/extracted.uss').");

            var root = ResolveGameObject(sourcePath);
            var rules = new List<KeyValuePair<string, Dictionary<string, string>>>();

            ExtractStylesRecursive(root, rules, 0);

            // Build USS content
            var sb = new StringBuilder();
            sb.AppendLine("/* Extracted from UGUI: " + sourcePath + " */");

            foreach (var rule in rules)
            {
                sb.AppendLine();
                sb.AppendLine($"{rule.Key} {{");
                foreach (var prop in rule.Value)
                    sb.AppendLine($"    {prop.Key}: {prop.Value};");
                sb.AppendLine("}");
            }

            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(outputPath, sb.ToString());
            AssetDatabase.ImportAsset(outputPath);

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["operation"] = "extractStyles",
                ["outputPath"] = outputPath,
                ["ruleCount"] = rules.Count
            };
        }

        private void ExtractStylesRecursive(GameObject go,
            List<KeyValuePair<string, Dictionary<string, string>>> rules, int depth)
        {
            var uguiType = DetectUGUIType(go);
            var styles = ExtractUGUIStyles(go, uguiType);
            var layoutStyles = ExtractLayoutUSS(go);
            foreach (var kv in layoutStyles)
                styles[kv.Key] = kv.Value;

            if (styles.Count > 0)
            {
                var selector = "#" + SanitizeName(go.name);
                rules.Add(new KeyValuePair<string, Dictionary<string, string>>(selector, styles));
            }

            for (int i = 0; i < go.transform.childCount; i++)
            {
                var child = go.transform.GetChild(i).gameObject;
                ExtractStylesRecursive(child, rules, depth + 1);
            }
        }

        #endregion

        #region Utility

        private string ColorToCSS(Color c) =>
            $"rgba({(int)(c.r * 255)}, {(int)(c.g * 255)}, {(int)(c.b * 255)}, {c.a:F2})";

        private Dictionary<string, object> PaddingToDict(RectOffset padding) =>
            new Dictionary<string, object>
            {
                ["left"] = padding.left,
                ["right"] = padding.right,
                ["top"] = padding.top,
                ["bottom"] = padding.bottom
            };

        private string MapTextAlignment(TextAnchor anchor) => anchor switch
        {
            TextAnchor.UpperLeft => "upper-left",
            TextAnchor.UpperCenter => "upper-center",
            TextAnchor.UpperRight => "upper-right",
            TextAnchor.MiddleLeft => "middle-left",
            TextAnchor.MiddleCenter => "middle-center",
            TextAnchor.MiddleRight => "middle-right",
            TextAnchor.LowerLeft => "lower-left",
            TextAnchor.LowerCenter => "lower-center",
            TextAnchor.LowerRight => "lower-right",
            _ => "upper-left"
        };

        private string EstimateAccuracy(int convertible, int warnings, int unsupported)
        {
            int total = convertible + warnings + unsupported;
            if (total == 0) return "N/A";

            float score = (convertible * 1.0f + warnings * 0.5f) / total * 100f;
            return $"{score:F0}%";
        }

        #endregion
    }
}
