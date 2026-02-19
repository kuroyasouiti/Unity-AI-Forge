using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using MCP.Editor.Base;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace MCP.Editor.Handlers
{
    /// <summary>
    /// Mid-level UI Toolkit asset handler: create/inspect/update UXML and USS files,
    /// create PanelSettings assets, and generate from templates.
    /// </summary>
    public class UITKAssetHandler : BaseCommandHandler
    {
        private static readonly string[] Operations =
        {
            "createUXML",
            "createUSS",
            "inspectUXML",
            "inspectUSS",
            "updateUXML",
            "updateUSS",
            "createPanelSettings",
            "createFromTemplate",
            "validateDependencies",
        };

        private static readonly XNamespace UiNs = "UnityEngine.UIElements";
        private static readonly XNamespace UxmlNs = "UnityEngine.UIElements";
        private static readonly XNamespace EditorNs = "UnityEditor.UIElements";

        private static readonly HashSet<string> SupportedElementTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "VisualElement", "Button", "Label", "TextField", "Toggle",
            "Slider", "SliderInt", "MinMaxSlider", "Foldout", "ScrollView",
            "ListView", "DropdownField", "RadioButton", "RadioButtonGroup",
            "GroupBox", "ProgressBar", "Image", "IntegerField", "FloatField",
        };

        public override string Category => "uitkAsset";

        public override IEnumerable<string> SupportedOperations => Operations;

        protected override bool RequiresCompilationWait(string operation) => false;

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            return operation switch
            {
                "createUXML" => CreateUXML(payload),
                "createUSS" => CreateUSS(payload),
                "inspectUXML" => InspectUXML(payload),
                "inspectUSS" => InspectUSS(payload),
                "updateUXML" => UpdateUXML(payload),
                "updateUSS" => UpdateUSS(payload),
                "createPanelSettings" => CreatePanelSettings(payload),
                "createFromTemplate" => CreateFromTemplate(payload),
                "validateDependencies" => ValidateDependenciesOp(payload),
                _ => throw new InvalidOperationException($"Unsupported uitkAsset operation: {operation}"),
            };
        }

        #region CreateUXML

        private object CreateUXML(Dictionary<string, object> payload)
        {
            var assetPath = GetString(payload, "assetPath");
            if (string.IsNullOrEmpty(assetPath))
                throw new InvalidOperationException("'assetPath' is required for createUXML");

            if (!assetPath.EndsWith(".uxml", StringComparison.OrdinalIgnoreCase))
                assetPath += ".uxml";

            var elements = GetListFromPayload(payload, "elements");
            var rootStyleSheets = GetStringList(payload, "styleSheets");

            var doc = BuildUXMLDocument(elements, rootStyleSheets, assetPath);
            var fullPath = Path.Combine(Application.dataPath, "..", assetPath);
            EnsureDirectoryExists(fullPath);

            File.WriteAllText(fullPath, doc.Declaration + "\n" + doc.ToString(), Encoding.UTF8);
            AssetDatabase.ImportAsset(assetPath);

            var result = CreateSuccessResponse(
                (KeyAssetPath, assetPath),
                (KeyMessage, $"Created UXML at {assetPath}")
            );
            return AppendDependencyInfo(result, assetPath);
        }

        private XDocument BuildUXMLDocument(List<object> elements, List<string> styleSheets, string uxmlAssetPath = null)
        {
            var root = new XElement(UiNs + "UXML",
                new XAttribute(XNamespace.Xmlns + "ui", UiNs.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "uie", EditorNs.NamespaceName)
            );

            // Add stylesheet references (convert to relative path from UXML location)
            if (styleSheets != null)
            {
                var uxmlDir = !string.IsNullOrEmpty(uxmlAssetPath)
                    ? Path.GetDirectoryName(uxmlAssetPath)?.Replace('\\', '/') ?? ""
                    : "";

                foreach (var ss in styleSheets)
                {
                    var src = ss;
                    // If USS path starts with the same directory as the UXML, make it relative
                    if (!string.IsNullOrEmpty(uxmlDir) && ss.StartsWith(uxmlDir + "/", StringComparison.OrdinalIgnoreCase))
                    {
                        src = ss.Substring(uxmlDir.Length + 1);
                    }
                    root.Add(new XElement(UiNs + "Style", new XAttribute("src", src)));
                }
            }

            // Add child elements
            if (elements != null)
            {
                foreach (var elem in elements)
                {
                    if (elem is Dictionary<string, object> elemDict)
                    {
                        root.Add(BuildUXMLElement(elemDict));
                    }
                }
            }

            var doc = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                root
            );
            return doc;
        }

        private XElement BuildUXMLElement(Dictionary<string, object> elemDict)
        {
            var type = GetString(elemDict, "type") ?? "VisualElement";
            if (!SupportedElementTypes.Contains(type))
                throw new InvalidOperationException($"Unsupported UXML element type: {type}. Supported: {string.Join(", ", SupportedElementTypes)}");

            var element = new XElement(UiNs + type);

            // name
            var name = GetString(elemDict, "name");
            if (!string.IsNullOrEmpty(name))
                element.SetAttributeValue("name", name);

            // classes
            var classes = GetStringList(elemDict, "classes");
            if (classes != null && classes.Count > 0)
                element.SetAttributeValue("class", string.Join(" ", classes));

            // text
            var text = GetString(elemDict, "text");
            if (!string.IsNullOrEmpty(text))
                element.SetAttributeValue("text", text);

            // inline style
            var styleDict = GetDictFromPayload(elemDict, "style");
            if (styleDict != null && styleDict.Count > 0)
            {
                var styleStr = string.Join(" ", styleDict.Select(kv => $"{kv.Key}: {kv.Value};"));
                element.SetAttributeValue("style", styleStr);
            }

            // arbitrary attributes
            var attrs = GetDictFromPayload(elemDict, "attributes");
            if (attrs != null)
            {
                foreach (var kv in attrs)
                {
                    element.SetAttributeValue(kv.Key, kv.Value?.ToString());
                }
            }

            // children
            var children = GetListFromPayload(elemDict, "children");
            if (children != null)
            {
                foreach (var child in children)
                {
                    if (child is Dictionary<string, object> childDict)
                    {
                        element.Add(BuildUXMLElement(childDict));
                    }
                }
            }

            return element;
        }

        #endregion

        #region CreateUSS

        private object CreateUSS(Dictionary<string, object> payload)
        {
            var assetPath = GetString(payload, "assetPath");
            if (string.IsNullOrEmpty(assetPath))
                throw new InvalidOperationException("'assetPath' is required for createUSS");

            if (!assetPath.EndsWith(".uss", StringComparison.OrdinalIgnoreCase))
                assetPath += ".uss";

            var rules = GetListFromPayload(payload, "rules");
            var content = BuildUSSContent(rules);

            var fullPath = Path.Combine(Application.dataPath, "..", assetPath);
            EnsureDirectoryExists(fullPath);

            File.WriteAllText(fullPath, content, Encoding.UTF8);
            AssetDatabase.ImportAsset(assetPath);

            return CreateSuccessResponse(
                (KeyAssetPath, assetPath),
                (KeyMessage, $"Created USS at {assetPath}")
            );
        }

        private string BuildUSSContent(List<object> rules)
        {
            if (rules == null || rules.Count == 0)
                return "/* Empty USS */\n";

            var sb = new StringBuilder();
            foreach (var rule in rules)
            {
                if (rule is Dictionary<string, object> ruleDict)
                {
                    AppendUSSRule(sb, ruleDict);
                }
            }
            return sb.ToString();
        }

        private void AppendUSSRule(StringBuilder sb, Dictionary<string, object> ruleDict)
        {
            var selector = GetString(ruleDict, "selector");
            if (string.IsNullOrEmpty(selector)) return;

            var properties = GetDictFromPayload(ruleDict, "properties");
            if (properties == null || properties.Count == 0) return;

            sb.AppendLine($"{selector} {{");
            foreach (var kv in properties)
            {
                sb.AppendLine($"    {kv.Key}: {kv.Value};");
            }
            sb.AppendLine("}");
            sb.AppendLine();
        }

        #endregion

        #region InspectUXML

        private object InspectUXML(Dictionary<string, object> payload)
        {
            var assetPath = GetString(payload, "assetPath");
            if (string.IsNullOrEmpty(assetPath))
                throw new InvalidOperationException("'assetPath' is required for inspectUXML");

            var fullPath = Path.Combine(Application.dataPath, "..", assetPath);
            if (!File.Exists(fullPath))
                return CreateFailureResponse($"UXML file not found: {assetPath}");

            var content = File.ReadAllText(fullPath);
            var doc = XDocument.Parse(content);
            var root = doc.Root;

            var result = new Dictionary<string, object>
            {
                [KeySuccess] = true,
                [KeyAssetPath] = assetPath,
            };

            // Extract stylesheets
            var styleSheets = new List<string>();
            foreach (var styleElem in root.Elements(UiNs + "Style"))
            {
                var src = styleElem.Attribute("src")?.Value;
                if (!string.IsNullOrEmpty(src))
                    styleSheets.Add(src);
            }
            result["styleSheets"] = styleSheets;

            // Extract elements
            var elements = new List<object>();
            foreach (var child in root.Elements().Where(e => e.Name.LocalName != "Style"))
            {
                elements.Add(ParseUXMLElement(child));
            }
            result["elements"] = elements;

            return AppendDependencyInfo(result, assetPath);
        }

        private Dictionary<string, object> ParseUXMLElement(XElement element)
        {
            var result = new Dictionary<string, object>
            {
                ["type"] = element.Name.LocalName,
            };

            var name = element.Attribute("name")?.Value;
            if (!string.IsNullOrEmpty(name))
                result["name"] = name;

            var classAttr = element.Attribute("class")?.Value;
            if (!string.IsNullOrEmpty(classAttr))
                result["classes"] = classAttr.Split(' ').ToList();

            var text = element.Attribute("text")?.Value;
            if (!string.IsNullOrEmpty(text))
                result["text"] = text;

            var style = element.Attribute("style")?.Value;
            if (!string.IsNullOrEmpty(style))
                result["style"] = style;

            // Other attributes
            var skipAttrs = new HashSet<string> { "name", "class", "text", "style" };
            var attrs = new Dictionary<string, object>();
            foreach (var attr in element.Attributes().Where(a => !a.IsNamespaceDeclaration && !skipAttrs.Contains(a.Name.LocalName)))
            {
                attrs[attr.Name.LocalName] = attr.Value;
            }
            if (attrs.Count > 0)
                result["attributes"] = attrs;

            // Children
            var children = new List<object>();
            foreach (var child in element.Elements())
            {
                children.Add(ParseUXMLElement(child));
            }
            if (children.Count > 0)
                result["children"] = children;

            return result;
        }

        #endregion

        #region InspectUSS

        private object InspectUSS(Dictionary<string, object> payload)
        {
            var assetPath = GetString(payload, "assetPath");
            if (string.IsNullOrEmpty(assetPath))
                throw new InvalidOperationException("'assetPath' is required for inspectUSS");

            var fullPath = Path.Combine(Application.dataPath, "..", assetPath);
            if (!File.Exists(fullPath))
                return CreateFailureResponse($"USS file not found: {assetPath}");

            var content = File.ReadAllText(fullPath);
            var rules = ParseUSSRules(content);

            return CreateSuccessResponse(
                (KeyAssetPath, assetPath),
                ("rules", rules),
                ("rawContent", content)
            );
        }

        private List<object> ParseUSSRules(string content)
        {
            var rules = new List<object>();
            // Match CSS-like rules: selector { ... }
            var matches = Regex.Matches(content, @"([^{]+)\{([^}]*)\}", RegexOptions.Singleline);
            foreach (Match match in matches)
            {
                var selector = match.Groups[1].Value.Trim();
                var body = match.Groups[2].Value.Trim();

                var properties = new Dictionary<string, object>();
                var propMatches = Regex.Matches(body, @"([\w-]+)\s*:\s*([^;]+);?");
                foreach (Match pm in propMatches)
                {
                    properties[pm.Groups[1].Value.Trim()] = pm.Groups[2].Value.Trim();
                }

                rules.Add(new Dictionary<string, object>
                {
                    ["selector"] = selector,
                    ["properties"] = properties,
                });
            }
            return rules;
        }

        #endregion

        #region UpdateUXML

        private object UpdateUXML(Dictionary<string, object> payload)
        {
            var assetPath = GetString(payload, "assetPath");
            if (string.IsNullOrEmpty(assetPath))
                throw new InvalidOperationException("'assetPath' is required for updateUXML");

            var fullPath = Path.Combine(Application.dataPath, "..", assetPath);
            if (!File.Exists(fullPath))
                return CreateFailureResponse($"UXML file not found: {assetPath}");

            var content = File.ReadAllText(fullPath);
            var doc = XDocument.Parse(content);
            var root = doc.Root;

            var updateAction = GetString(payload, "action") ?? "add";

            switch (updateAction)
            {
                case "add":
                    AddUXMLElements(root, payload);
                    break;
                case "remove":
                    RemoveUXMLElement(root, payload);
                    break;
                case "replace":
                    ReplaceUXMLElement(root, payload);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported updateUXML action: {updateAction}. Supported: add, remove, replace");
            }

            File.WriteAllText(fullPath, doc.Declaration + "\n" + doc.ToString(), Encoding.UTF8);
            AssetDatabase.ImportAsset(assetPath);

            var result = CreateSuccessResponse(
                (KeyAssetPath, assetPath),
                (KeyMessage, $"Updated UXML ({updateAction}) at {assetPath}")
            );
            return AppendDependencyInfo(result, assetPath);
        }

        private void AddUXMLElements(XElement root, Dictionary<string, object> payload)
        {
            var parentName = GetString(payload, "parentElementName");
            var target = parentName != null ? FindElementByName(root, parentName) : root;
            if (target == null)
                throw new InvalidOperationException($"Parent element '{parentName}' not found in UXML");

            var elements = GetListFromPayload(payload, "elements");
            if (elements != null)
            {
                foreach (var elem in elements)
                {
                    if (elem is Dictionary<string, object> elemDict)
                    {
                        target.Add(BuildUXMLElement(elemDict));
                    }
                }
            }
        }

        private void RemoveUXMLElement(XElement root, Dictionary<string, object> payload)
        {
            var elementName = GetString(payload, "elementName");
            if (string.IsNullOrEmpty(elementName))
                throw new InvalidOperationException("'elementName' is required for remove action");

            var element = FindElementByName(root, elementName);
            if (element == null)
                throw new InvalidOperationException($"Element '{elementName}' not found in UXML");

            element.Remove();
        }

        private void ReplaceUXMLElement(XElement root, Dictionary<string, object> payload)
        {
            var elementName = GetString(payload, "elementName");
            if (string.IsNullOrEmpty(elementName))
                throw new InvalidOperationException("'elementName' is required for replace action");

            var element = FindElementByName(root, elementName);
            if (element == null)
                throw new InvalidOperationException($"Element '{elementName}' not found in UXML");

            var replacement = GetDictFromPayload(payload, "element");
            if (replacement == null)
                throw new InvalidOperationException("'element' is required for replace action");

            element.ReplaceWith(BuildUXMLElement(replacement));
        }

        private XElement FindElementByName(XElement root, string name)
        {
            if (root.Attribute("name")?.Value == name)
                return root;
            return root.Descendants().FirstOrDefault(e => e.Attribute("name")?.Value == name);
        }

        #endregion

        #region UpdateUSS

        private object UpdateUSS(Dictionary<string, object> payload)
        {
            var assetPath = GetString(payload, "assetPath");
            if (string.IsNullOrEmpty(assetPath))
                throw new InvalidOperationException("'assetPath' is required for updateUSS");

            var fullPath = Path.Combine(Application.dataPath, "..", assetPath);
            if (!File.Exists(fullPath))
                return CreateFailureResponse($"USS file not found: {assetPath}");

            var content = File.ReadAllText(fullPath);
            var updateAction = GetString(payload, "action") ?? "add";

            switch (updateAction)
            {
                case "add":
                case "update":
                    content = AddOrUpdateUSSRules(content, payload);
                    break;
                case "remove":
                    content = RemoveUSSRule(content, payload);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported updateUSS action: {updateAction}. Supported: add, update, remove");
            }

            File.WriteAllText(fullPath, content, Encoding.UTF8);
            AssetDatabase.ImportAsset(assetPath);

            return CreateSuccessResponse(
                (KeyAssetPath, assetPath),
                (KeyMessage, $"Updated USS ({updateAction}) at {assetPath}")
            );
        }

        private string AddOrUpdateUSSRules(string content, Dictionary<string, object> payload)
        {
            var rules = GetListFromPayload(payload, "rules");
            if (rules == null) return content;

            foreach (var rule in rules)
            {
                if (rule is not Dictionary<string, object> ruleDict) continue;

                var selector = GetString(ruleDict, "selector");
                if (string.IsNullOrEmpty(selector)) continue;

                var properties = GetDictFromPayload(ruleDict, "properties");
                if (properties == null || properties.Count == 0) continue;

                // Try to find and replace existing rule
                var escapedSelector = Regex.Escape(selector);
                var pattern = $@"{escapedSelector}\s*\{{[^}}]*\}}";
                if (Regex.IsMatch(content, pattern, RegexOptions.Singleline))
                {
                    var newRule = BuildSingleUSSRule(selector, properties);
                    content = Regex.Replace(content, pattern, newRule.TrimEnd(), RegexOptions.Singleline);
                }
                else
                {
                    // Append new rule
                    var sb = new StringBuilder(content);
                    if (!content.EndsWith("\n"))
                        sb.AppendLine();
                    sb.AppendLine();
                    AppendUSSRule(sb, ruleDict);
                    content = sb.ToString();
                }
            }

            return content;
        }

        private string RemoveUSSRule(string content, Dictionary<string, object> payload)
        {
            var selector = GetString(payload, "selector");
            if (string.IsNullOrEmpty(selector))
                throw new InvalidOperationException("'selector' is required for remove action");

            var escapedSelector = Regex.Escape(selector);
            var pattern = $@"\s*{escapedSelector}\s*\{{[^}}]*\}}\s*";
            content = Regex.Replace(content, pattern, "\n", RegexOptions.Singleline);

            return content.Trim() + "\n";
        }

        private string BuildSingleUSSRule(string selector, Dictionary<string, object> properties)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{selector} {{");
            foreach (var kv in properties)
            {
                sb.AppendLine($"    {kv.Key}: {kv.Value};");
            }
            sb.Append("}");
            return sb.ToString();
        }

        #endregion

        #region CreatePanelSettings

        private object CreatePanelSettings(Dictionary<string, object> payload)
        {
            var assetPath = GetString(payload, "assetPath");
            if (string.IsNullOrEmpty(assetPath))
                throw new InvalidOperationException("'assetPath' is required for createPanelSettings");

            if (!assetPath.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                assetPath += ".asset";

            var fullDir = Path.GetDirectoryName(Path.Combine(Application.dataPath, "..", assetPath));
            if (!string.IsNullOrEmpty(fullDir) && !Directory.Exists(fullDir))
                Directory.CreateDirectory(fullDir);

            var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();

            // Configure
            var scaleMode = GetString(payload, "scaleMode")?.ToLowerInvariant();
            switch (scaleMode)
            {
                case "constantpixelsize":
                    panelSettings.scaleMode = PanelScaleMode.ConstantPixelSize;
                    break;
                case "constantphysicalsize":
                    panelSettings.scaleMode = PanelScaleMode.ConstantPhysicalSize;
                    break;
                case "scalewithscreensize":
                default:
                    panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                    break;
            }

            var refResDict = GetDictFromPayload(payload, "referenceResolution");
            if (refResDict != null)
            {
                var x = GetFloatFromDict(refResDict, "x", 1920f);
                var y = GetFloatFromDict(refResDict, "y", 1080f);
                panelSettings.referenceResolution = new Vector2Int((int)x, (int)y);
            }
            else
            {
                panelSettings.referenceResolution = new Vector2Int(1920, 1080);
            }

            var match = GetFloat(payload, "match", 0.5f);
            panelSettings.match = match;

            var sortOrder = GetFloat(payload, "sortingOrder", 0f);
            panelSettings.sortingOrder = sortOrder;

            // Assign theme stylesheet if provided
            var themeStyleSheetPath = GetString(payload, "themeStyleSheet");
            if (!string.IsNullOrEmpty(themeStyleSheetPath))
            {
                var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(themeStyleSheetPath);
                if (theme != null)
                    panelSettings.themeStyleSheet = theme;
            }

            AssetDatabase.CreateAsset(panelSettings, assetPath);
            AssetDatabase.SaveAssets();

            return CreateSuccessResponse(
                (KeyAssetPath, assetPath),
                (KeyMessage, $"Created PanelSettings at {assetPath}")
            );
        }

        #endregion

        #region CreateFromTemplate

        private object CreateFromTemplate(Dictionary<string, object> payload)
        {
            var templateName = GetString(payload, "templateName");
            if (string.IsNullOrEmpty(templateName))
                throw new InvalidOperationException("'templateName' is required for createFromTemplate");

            var outputDir = GetString(payload, "outputDir") ?? "Assets/UI";
            var prefix = GetString(payload, "prefix") ?? templateName;

            var uxmlPath = $"{outputDir}/{prefix}.uxml";
            var ussPath = $"{outputDir}/{prefix}.uss";

            List<object> uxmlElements;
            List<object> ussRules;

            switch (templateName.ToLowerInvariant())
            {
                case "menu":
                    (uxmlElements, ussRules) = BuildMenuTemplate(payload);
                    break;
                case "dialog":
                    (uxmlElements, ussRules) = BuildDialogTemplate(payload);
                    break;
                case "hud":
                    (uxmlElements, ussRules) = BuildHudTemplate(payload);
                    break;
                case "settings":
                    (uxmlElements, ussRules) = BuildSettingsTemplate(payload);
                    break;
                case "inventory":
                    (uxmlElements, ussRules) = BuildInventoryTemplate(payload);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown template: {templateName}. Supported: menu, dialog, hud, settings, inventory");
            }

            // Build and write UXML
            var styleSheets = new List<string> { ussPath };
            var uxmlDoc = BuildUXMLDocument(uxmlElements, styleSheets, uxmlPath);
            var uxmlFullPath = Path.Combine(Application.dataPath, "..", uxmlPath);
            EnsureDirectoryExists(uxmlFullPath);
            File.WriteAllText(uxmlFullPath, uxmlDoc.Declaration + "\n" + uxmlDoc.ToString(), Encoding.UTF8);
            AssetDatabase.ImportAsset(uxmlPath);

            // Build and write USS
            var ussContent = BuildUSSContent(ussRules);
            var ussFullPath = Path.Combine(Application.dataPath, "..", ussPath);
            EnsureDirectoryExists(ussFullPath);
            File.WriteAllText(ussFullPath, ussContent, Encoding.UTF8);
            AssetDatabase.ImportAsset(ussPath);

            var result = CreateSuccessResponse(
                ("uxmlPath", uxmlPath),
                ("ussPath", ussPath),
                ("templateName", templateName),
                (KeyMessage, $"Created {templateName} template: {uxmlPath}, {ussPath}")
            );
            return AppendDependencyInfo(result, uxmlPath);
        }

        private (List<object>, List<object>) BuildMenuTemplate(Dictionary<string, object> payload)
        {
            var title = GetString(payload, "title") ?? "Game Title";
            var buttonLabels = GetStringList(payload, "buttons") ?? new List<string> { "Start", "Options", "Quit" };

            var children = new List<object>
            {
                Elem("Label", "title-label", new[] { "title" }, title),
            };
            foreach (var label in buttonLabels)
            {
                var btnName = label.ToLowerInvariant().Replace(" ", "-") + "-btn";
                children.Add(Elem("Button", btnName, new[] { "menu-btn" }, label));
            }

            var uxmlElements = new List<object>
            {
                ElemWithChildren("VisualElement", "menu-root", new[] { "menu-container" }, children),
            };

            var ussRules = new List<object>
            {
                USSRule(".menu-container", new Dictionary<string, object>
                {
                    ["flex-grow"] = "1",
                    ["justify-content"] = "center",
                    ["align-items"] = "center",
                    ["background-color"] = "rgba(0, 0, 0, 0.8)",
                }),
                USSRule(".title", new Dictionary<string, object>
                {
                    ["font-size"] = "48px",
                    ["color"] = "#ffffff",
                    ["-unity-text-align"] = "middle-center",
                    ["margin-bottom"] = "40px",
                }),
                USSRule(".menu-btn", new Dictionary<string, object>
                {
                    ["width"] = "250px",
                    ["height"] = "50px",
                    ["font-size"] = "20px",
                    ["margin"] = "8px",
                    ["border-radius"] = "6px",
                }),
            };

            return (uxmlElements, ussRules);
        }

        private (List<object>, List<object>) BuildDialogTemplate(Dictionary<string, object> payload)
        {
            var title = GetString(payload, "title") ?? "Dialog Title";
            var message = GetString(payload, "message") ?? "Dialog message goes here.";

            var panel = ElemWithChildren("VisualElement", "dialog-panel", new[] { "dialog-panel" },
                new List<object>
                {
                    Elem("Label", "dialog-title", new[] { "dialog-title" }, title),
                    Elem("Label", "dialog-message", new[] { "dialog-message" }, message),
                    ElemWithChildren("VisualElement", "dialog-buttons", new[] { "dialog-buttons" },
                        new List<object>
                        {
                            Elem("Button", "ok-btn", new[] { "dialog-btn", "primary-btn" }, "OK"),
                            Elem("Button", "cancel-btn", new[] { "dialog-btn" }, "Cancel"),
                        }),
                });

            var uxmlElements = new List<object>
            {
                ElemWithChildren("VisualElement", "dialog-overlay", new[] { "dialog-overlay" },
                    new List<object> { panel }),
            };

            var ussRules = new List<object>
            {
                USSRule(".dialog-overlay", new Dictionary<string, object>
                {
                    ["flex-grow"] = "1",
                    ["justify-content"] = "center",
                    ["align-items"] = "center",
                    ["background-color"] = "rgba(0, 0, 0, 0.5)",
                }),
                USSRule(".dialog-panel", new Dictionary<string, object>
                {
                    ["width"] = "400px",
                    ["padding"] = "24px",
                    ["background-color"] = "#2d2d2d",
                    ["border-radius"] = "8px",
                }),
                USSRule(".dialog-title", new Dictionary<string, object>
                {
                    ["font-size"] = "24px",
                    ["color"] = "#ffffff",
                    ["-unity-text-align"] = "middle-center",
                    ["margin-bottom"] = "16px",
                }),
                USSRule(".dialog-message", new Dictionary<string, object>
                {
                    ["font-size"] = "14px",
                    ["color"] = "#cccccc",
                    ["white-space"] = "normal",
                    ["margin-bottom"] = "24px",
                }),
                USSRule(".dialog-buttons", new Dictionary<string, object>
                {
                    ["flex-direction"] = "row",
                    ["justify-content"] = "flex-end",
                }),
                USSRule(".dialog-btn", new Dictionary<string, object>
                {
                    ["width"] = "100px",
                    ["height"] = "36px",
                    ["margin-left"] = "8px",
                }),
                USSRule(".primary-btn", new Dictionary<string, object>
                {
                    ["background-color"] = "#2d5a27",
                    ["color"] = "#ffffff",
                }),
            };

            return (uxmlElements, ussRules);
        }

        private (List<object>, List<object>) BuildHudTemplate(Dictionary<string, object> payload)
        {
            var uxmlElements = new List<object>
            {
                ElemWithChildren("VisualElement", "hud-root", new[] { "hud-root" },
                    new List<object>
                    {
                        ElemWithChildren("VisualElement", "status-area", new[] { "status-area" },
                            new List<object>
                            {
                                ElemWithChildren("VisualElement", "hp-bar-container", new[] { "bar-container" },
                                    new List<object>
                                    {
                                        Elem("Label", "hp-label", new[] { "bar-label" }, "HP"),
                                        Elem("ProgressBar", "hp-bar", new[] { "hp-bar" }, null, new Dictionary<string, object> { ["value"] = "75", ["high-value"] = "100" }),
                                    }),
                                ElemWithChildren("VisualElement", "mp-bar-container", new[] { "bar-container" },
                                    new List<object>
                                    {
                                        Elem("Label", "mp-label", new[] { "bar-label" }, "MP"),
                                        Elem("ProgressBar", "mp-bar", new[] { "mp-bar" }, null, new Dictionary<string, object> { ["value"] = "50", ["high-value"] = "100" }),
                                    }),
                                Elem("Label", "gold-label", new[] { "gold-label" }, "Gold: 0"),
                            }),
                        ElemWithChildren("VisualElement", "action-bar", new[] { "action-bar" },
                            new List<object>
                            {
                                Elem("Button", "action-1", new[] { "action-btn" }, "1"),
                                Elem("Button", "action-2", new[] { "action-btn" }, "2"),
                                Elem("Button", "action-3", new[] { "action-btn" }, "3"),
                                Elem("Button", "action-4", new[] { "action-btn" }, "4"),
                            }),
                    }),
            };

            var ussRules = new List<object>
            {
                USSRule(".hud-root", new Dictionary<string, object>
                {
                    ["flex-grow"] = "1",
                    ["justify-content"] = "space-between",
                }),
                USSRule(".status-area", new Dictionary<string, object>
                {
                    ["padding"] = "12px",
                    ["width"] = "300px",
                }),
                USSRule(".bar-container", new Dictionary<string, object>
                {
                    ["flex-direction"] = "row",
                    ["align-items"] = "center",
                    ["margin-bottom"] = "4px",
                }),
                USSRule(".bar-label", new Dictionary<string, object>
                {
                    ["width"] = "30px",
                    ["font-size"] = "14px",
                    ["color"] = "#ffffff",
                }),
                USSRule(".hp-bar", new Dictionary<string, object>
                {
                    ["flex-grow"] = "1",
                    ["height"] = "20px",
                }),
                USSRule(".mp-bar", new Dictionary<string, object>
                {
                    ["flex-grow"] = "1",
                    ["height"] = "20px",
                }),
                USSRule(".gold-label", new Dictionary<string, object>
                {
                    ["font-size"] = "16px",
                    ["color"] = "#ffd700",
                    ["margin-top"] = "8px",
                }),
                USSRule(".action-bar", new Dictionary<string, object>
                {
                    ["flex-direction"] = "row",
                    ["justify-content"] = "center",
                    ["align-self"] = "flex-end",
                    ["padding"] = "12px",
                }),
                USSRule(".action-btn", new Dictionary<string, object>
                {
                    ["width"] = "50px",
                    ["height"] = "50px",
                    ["font-size"] = "18px",
                    ["margin"] = "4px",
                }),
            };

            return (uxmlElements, ussRules);
        }

        private (List<object>, List<object>) BuildSettingsTemplate(Dictionary<string, object> payload)
        {
            var uxmlElements = new List<object>
            {
                ElemWithChildren("VisualElement", "settings-root", new[] { "settings-root" },
                    new List<object>
                    {
                        Elem("Label", "settings-title", new[] { "settings-title" }, "Settings"),
                        ElemWithChildren("VisualElement", "settings-content", new[] { "settings-content" },
                            new List<object>
                            {
                                ElemWithAttrs("Toggle", "fullscreen-toggle", new[] { "settings-row" }, "Fullscreen", new Dictionary<string, object> { ["value"] = "true" }),
                                ElemWithAttrs("Slider", "volume-slider", new[] { "settings-row" }, "Volume", new Dictionary<string, object> { ["low-value"] = "0", ["high-value"] = "100", ["value"] = "80" }),
                                ElemWithAttrs("SliderInt", "quality-slider", new[] { "settings-row" }, "Quality", new Dictionary<string, object> { ["low-value"] = "0", ["high-value"] = "3", ["value"] = "2" }),
                                ElemWithAttrs("DropdownField", "resolution-dropdown", new[] { "settings-row" }, "Resolution", new Dictionary<string, object>()),
                            }),
                        ElemWithChildren("VisualElement", "settings-buttons", new[] { "settings-buttons" },
                            new List<object>
                            {
                                Elem("Button", "apply-btn", new[] { "settings-btn", "primary-btn" }, "Apply"),
                                Elem("Button", "back-btn", new[] { "settings-btn" }, "Back"),
                            }),
                    }),
            };

            var ussRules = new List<object>
            {
                USSRule(".settings-root", new Dictionary<string, object>
                {
                    ["flex-grow"] = "1",
                    ["padding"] = "24px",
                    ["background-color"] = "rgba(0, 0, 0, 0.9)",
                }),
                USSRule(".settings-title", new Dictionary<string, object>
                {
                    ["font-size"] = "32px",
                    ["color"] = "#ffffff",
                    ["-unity-text-align"] = "middle-center",
                    ["margin-bottom"] = "24px",
                }),
                USSRule(".settings-content", new Dictionary<string, object>
                {
                    ["max-width"] = "500px",
                    ["align-self"] = "center",
                    ["width"] = "100%",
                }),
                USSRule(".settings-row", new Dictionary<string, object>
                {
                    ["margin-bottom"] = "16px",
                }),
                USSRule(".settings-buttons", new Dictionary<string, object>
                {
                    ["flex-direction"] = "row",
                    ["justify-content"] = "center",
                    ["margin-top"] = "24px",
                }),
                USSRule(".settings-btn", new Dictionary<string, object>
                {
                    ["width"] = "120px",
                    ["height"] = "40px",
                    ["margin"] = "8px",
                }),
                USSRule(".primary-btn", new Dictionary<string, object>
                {
                    ["background-color"] = "#2d5a27",
                    ["color"] = "#ffffff",
                }),
            };

            return (uxmlElements, ussRules);
        }

        private (List<object>, List<object>) BuildInventoryTemplate(Dictionary<string, object> payload)
        {
            var columns = GetInt(payload, "columns", 4);
            var slotCount = GetInt(payload, "slotCount", 16);

            var slots = new List<object>();
            for (int i = 0; i < slotCount; i++)
            {
                slots.Add(ElemWithChildren("VisualElement", $"slot-{i}", new[] { "inventory-slot" },
                    new List<object>
                    {
                        Elem("Image", $"slot-icon-{i}", new[] { "slot-icon" }, null),
                        Elem("Label", $"slot-count-{i}", new[] { "slot-count" }, ""),
                    }));
            }

            var uxmlElements = new List<object>
            {
                ElemWithChildren("VisualElement", "inventory-root", new[] { "inventory-root" },
                    new List<object>
                    {
                        Elem("Label", "inventory-title", new[] { "inventory-title" }, "Inventory"),
                        ElemWithChildren("VisualElement", "inventory-grid", new[] { "inventory-grid" }, slots),
                    }),
            };

            var ussRules = new List<object>
            {
                USSRule(".inventory-root", new Dictionary<string, object>
                {
                    ["padding"] = "16px",
                    ["background-color"] = "#1a1a1a",
                    ["border-radius"] = "8px",
                }),
                USSRule(".inventory-title", new Dictionary<string, object>
                {
                    ["font-size"] = "24px",
                    ["color"] = "#ffffff",
                    ["-unity-text-align"] = "middle-center",
                    ["margin-bottom"] = "12px",
                }),
                USSRule(".inventory-grid", new Dictionary<string, object>
                {
                    ["flex-direction"] = "row",
                    ["flex-wrap"] = "wrap",
                    ["justify-content"] = "flex-start",
                }),
                USSRule(".inventory-slot", new Dictionary<string, object>
                {
                    ["width"] = "64px",
                    ["height"] = "64px",
                    ["margin"] = "4px",
                    ["background-color"] = "#333333",
                    ["border-width"] = "1px",
                    ["border-color"] = "#555555",
                    ["border-radius"] = "4px",
                    ["justify-content"] = "center",
                    ["align-items"] = "center",
                }),
                USSRule(".slot-icon", new Dictionary<string, object>
                {
                    ["width"] = "48px",
                    ["height"] = "48px",
                }),
                USSRule(".slot-count", new Dictionary<string, object>
                {
                    ["position"] = "absolute",
                    ["right"] = "2px",
                    ["bottom"] = "2px",
                    ["font-size"] = "10px",
                    ["color"] = "#ffffff",
                }),
            };

            return (uxmlElements, ussRules);
        }

        #endregion

        #region Template Helpers

        private Dictionary<string, object> Elem(string type, string name, string[] classes, string text, Dictionary<string, object> attributes = null)
        {
            var dict = new Dictionary<string, object> { ["type"] = type };
            if (!string.IsNullOrEmpty(name)) dict["name"] = name;
            if (classes != null && classes.Length > 0) dict["classes"] = classes.ToList();
            if (!string.IsNullOrEmpty(text)) dict["text"] = text;
            if (attributes != null && attributes.Count > 0) dict["attributes"] = attributes;
            return dict;
        }

        private Dictionary<string, object> ElemWithAttrs(string type, string name, string[] classes, string label, Dictionary<string, object> attributes)
        {
            var dict = Elem(type, name, classes, null, attributes);
            if (!string.IsNullOrEmpty(label)) dict["attributes"] = MergeDict(attributes ?? new Dictionary<string, object>(), "label", label);
            return dict;
        }

        private Dictionary<string, object> MergeDict(Dictionary<string, object> dict, string key, object value)
        {
            var result = new Dictionary<string, object>(dict) { [key] = value };
            return result;
        }

        private Dictionary<string, object> ElemWithChildren(string type, string name, string[] classes, List<object> children)
        {
            var dict = Elem(type, name, classes, null);
            if (children != null && children.Count > 0)
                dict["children"] = children;
            return dict;
        }

        private Dictionary<string, object> USSRule(string selector, Dictionary<string, object> properties)
        {
            return new Dictionary<string, object>
            {
                ["selector"] = selector,
                ["properties"] = properties,
            };
        }

        #endregion

        #region ValidateDependencies

        private object ValidateDependenciesOp(Dictionary<string, object> payload)
        {
            var assetPath = GetString(payload, "assetPath");
            if (string.IsNullOrEmpty(assetPath))
                throw new InvalidOperationException("'assetPath' is required for validateDependencies");

            var fullPath = Path.Combine(Application.dataPath, "..", assetPath);
            if (!File.Exists(fullPath))
                return CreateFailureResponse($"UXML file not found: {assetPath}");

            var (deps, issues) = ValidateUXMLDependencies(assetPath);
            return new Dictionary<string, object>
            {
                [KeySuccess] = true,
                ["isValid"] = issues.Count == 0,
                [KeyAssetPath] = assetPath,
                ["dependencies"] = deps,
                ["issues"] = issues,
            };
        }

        private (List<object> dependencies, List<object> issues) ValidateUXMLDependencies(string uxmlAssetPath)
        {
            var dependencies = new List<object>();
            var issues = new List<object>();

            var fullPath = Path.Combine(Application.dataPath, "..", uxmlAssetPath);
            if (!File.Exists(fullPath))
                return (dependencies, issues);

            var content = File.ReadAllText(fullPath);
            XDocument doc;
            try
            {
                doc = XDocument.Parse(content);
            }
            catch
            {
                issues.Add(new Dictionary<string, object>
                {
                    ["type"] = "parse_error",
                    ["severity"] = "error",
                    ["message"] = $"Failed to parse UXML: {uxmlAssetPath}",
                });
                return (dependencies, issues);
            }

            var root = doc.Root;
            if (root == null)
                return (dependencies, issues);

            var uxmlDir = Path.GetDirectoryName(uxmlAssetPath)?.Replace('\\', '/') ?? "";

            // Check <Style src="..."> references
            foreach (var styleElem in root.Elements(UiNs + "Style"))
            {
                var src = styleElem.Attribute("src")?.Value;
                if (string.IsNullOrEmpty(src)) continue;

                var resolvedPath = src.StartsWith("Assets/") ? src : $"{uxmlDir}/{src}";
                resolvedPath = resolvedPath.Replace('\\', '/');
                var exists = File.Exists(Path.Combine(Application.dataPath, "..", resolvedPath));

                dependencies.Add(new Dictionary<string, object>
                {
                    ["type"] = "stylesheet",
                    ["src"] = src,
                    ["resolvedPath"] = resolvedPath,
                    ["exists"] = exists,
                });

                if (!exists)
                {
                    issues.Add(new Dictionary<string, object>
                    {
                        ["type"] = "missing_stylesheet",
                        ["severity"] = "error",
                        ["src"] = src,
                        ["resolvedPath"] = resolvedPath,
                    });
                }
            }

            // Check <Template src="..."> references
            foreach (var templateElem in root.Elements(UiNs + "Template"))
            {
                var src = templateElem.Attribute("src")?.Value;
                if (string.IsNullOrEmpty(src)) continue;

                var resolvedPath = src.StartsWith("Assets/") ? src : $"{uxmlDir}/{src}";
                resolvedPath = resolvedPath.Replace('\\', '/');
                var exists = File.Exists(Path.Combine(Application.dataPath, "..", resolvedPath));

                dependencies.Add(new Dictionary<string, object>
                {
                    ["type"] = "template",
                    ["src"] = src,
                    ["resolvedPath"] = resolvedPath,
                    ["exists"] = exists,
                });

                if (!exists)
                {
                    issues.Add(new Dictionary<string, object>
                    {
                        ["type"] = "missing_template",
                        ["severity"] = "error",
                        ["src"] = src,
                        ["resolvedPath"] = resolvedPath,
                    });
                }
            }

            return (dependencies, issues);
        }

        private object AppendDependencyInfo(object response, string uxmlAssetPath)
        {
            if (response is not Dictionary<string, object> dict)
                return response;

            var (deps, issues) = ValidateUXMLDependencies(uxmlAssetPath);
            dict["dependencies"] = deps;
            dict["issues"] = issues;
            dict["isValid"] = issues.Count == 0;
            return dict;
        }

        #endregion

        #region Utilities

        private void EnsureDirectoryExists(string filePath)
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        #endregion
    }
}
