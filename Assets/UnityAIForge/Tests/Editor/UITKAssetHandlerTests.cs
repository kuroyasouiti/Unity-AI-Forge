using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using MCP.Editor.Handlers;

namespace MCP.Editor.Tests
{
    /// <summary>
    /// Unit tests for UITKAssetHandler.
    /// </summary>
    [TestFixture]
    public class UITKAssetHandlerTests
    {
        private UITKAssetHandler _handler;
        private List<string> _createdAssets;
        private const string TestDir = "Assets/Tests/UITKAssetTests";

        [SetUp]
        public void SetUp()
        {
            _handler = new UITKAssetHandler();
            _createdAssets = new List<string>();

            if (!AssetDatabase.IsValidFolder(TestDir))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Tests"))
                    AssetDatabase.CreateFolder("Assets", "Tests");
                AssetDatabase.CreateFolder("Assets/Tests", "UITKAssetTests");
            }
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var path in _createdAssets)
            {
                if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
                    AssetDatabase.DeleteAsset(path);
            }
            _createdAssets.Clear();

            if (AssetDatabase.IsValidFolder(TestDir))
                AssetDatabase.DeleteAsset(TestDir);

            AssetDatabase.Refresh();
        }

        private Dictionary<string, object> Execute(Dictionary<string, object> payload)
        {
            return _handler.Execute(payload) as Dictionary<string, object>;
        }

        private void TrackAsset(string path)
        {
            if (!_createdAssets.Contains(path))
                _createdAssets.Add(path);
        }

        #region Property Tests

        [Test]
        public void Category_ShouldReturnUITKAsset()
        {
            Assert.AreEqual("uitkAsset", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ShouldContainAllOperations()
        {
            var ops = new List<string>(_handler.SupportedOperations);
            Assert.Contains("createUXML", ops);
            Assert.Contains("createUSS", ops);
            Assert.Contains("inspectUXML", ops);
            Assert.Contains("inspectUSS", ops);
            Assert.Contains("updateUXML", ops);
            Assert.Contains("updateUSS", ops);
            Assert.Contains("createPanelSettings", ops);
            Assert.Contains("createFromTemplate", ops);
            Assert.Contains("validateDependencies", ops);
        }

        #endregion

        #region CreateUXML Tests

        [Test]
        public void CreateUXML_CreatesFile()
        {
            var path = $"{TestDir}/test.uxml";
            TrackAsset(path);

            var result = Execute(new Dictionary<string, object>
            {
                ["operation"] = "createUXML",
                ["assetPath"] = path,
                ["elements"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["type"] = "Button",
                        ["name"] = "my-btn",
                        ["text"] = "Click",
                        ["classes"] = new List<object> { "primary" },
                    },
                },
            });

            Assert.IsTrue((bool)result["success"]);
            Assert.IsTrue(File.Exists(Path.Combine(Application.dataPath, "..", path)));

            // Verify XML content
            var content = File.ReadAllText(Path.Combine(Application.dataPath, "..", path));
            Assert.IsTrue(content.Contains("my-btn"));
            Assert.IsTrue(content.Contains("Click"));
            Assert.IsTrue(content.Contains("primary"));
        }

        [Test]
        public void CreateUXML_WithStyleSheets()
        {
            var path = $"{TestDir}/styled.uxml";
            TrackAsset(path);

            var result = Execute(new Dictionary<string, object>
            {
                ["operation"] = "createUXML",
                ["assetPath"] = path,
                ["styleSheets"] = new List<object> { "Assets/UI/styles.uss" },
                ["elements"] = new List<object>
                {
                    new Dictionary<string, object> { ["type"] = "Label", ["text"] = "Hello" },
                },
            });

            Assert.IsTrue((bool)result["success"]);
            var content = File.ReadAllText(Path.Combine(Application.dataPath, "..", path));
            Assert.IsTrue(content.Contains("Assets/UI/styles.uss"));
        }

        [Test]
        public void CreateUXML_WithNestedChildren()
        {
            var path = $"{TestDir}/nested.uxml";
            TrackAsset(path);

            var result = Execute(new Dictionary<string, object>
            {
                ["operation"] = "createUXML",
                ["assetPath"] = path,
                ["elements"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["type"] = "VisualElement",
                        ["name"] = "container",
                        ["children"] = new List<object>
                        {
                            new Dictionary<string, object> { ["type"] = "Label", ["name"] = "title", ["text"] = "Title" },
                            new Dictionary<string, object> { ["type"] = "Button", ["name"] = "btn", ["text"] = "Go" },
                        },
                    },
                },
            });

            Assert.IsTrue((bool)result["success"]);
            var content = File.ReadAllText(Path.Combine(Application.dataPath, "..", path));
            Assert.IsTrue(content.Contains("container"));
            Assert.IsTrue(content.Contains("title"));
            Assert.IsTrue(content.Contains("btn"));
        }

        [Test]
        public void CreateUXML_WithInlineStyle()
        {
            var path = $"{TestDir}/inline-style.uxml";
            TrackAsset(path);

            var result = Execute(new Dictionary<string, object>
            {
                ["operation"] = "createUXML",
                ["assetPath"] = path,
                ["elements"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["type"] = "Button",
                        ["name"] = "styled-btn",
                        ["text"] = "Styled",
                        ["style"] = new Dictionary<string, object>
                        {
                            ["width"] = "200px",
                            ["height"] = "50px",
                        },
                    },
                },
            });

            Assert.IsTrue((bool)result["success"]);
            var content = File.ReadAllText(Path.Combine(Application.dataPath, "..", path));
            Assert.IsTrue(content.Contains("width: 200px"));
            Assert.IsTrue(content.Contains("height: 50px"));
        }

        #endregion

        #region CreateUSS Tests

        [Test]
        public void CreateUSS_CreatesFile()
        {
            var path = $"{TestDir}/test.uss";
            TrackAsset(path);

            var result = Execute(new Dictionary<string, object>
            {
                ["operation"] = "createUSS",
                ["assetPath"] = path,
                ["rules"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["selector"] = ".primary",
                        ["properties"] = new Dictionary<string, object>
                        {
                            ["background-color"] = "#2d5a27",
                            ["font-size"] = "16px",
                        },
                    },
                },
            });

            Assert.IsTrue((bool)result["success"]);
            var content = File.ReadAllText(Path.Combine(Application.dataPath, "..", path));
            Assert.IsTrue(content.Contains(".primary"));
            Assert.IsTrue(content.Contains("#2d5a27"));
            Assert.IsTrue(content.Contains("16px"));
        }

        #endregion

        #region InspectUXML Tests

        [Test]
        public void InspectUXML_ParsesElements()
        {
            var path = $"{TestDir}/inspect-test.uxml";
            TrackAsset(path);

            // First create a UXML
            Execute(new Dictionary<string, object>
            {
                ["operation"] = "createUXML",
                ["assetPath"] = path,
                ["elements"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["type"] = "Button",
                        ["name"] = "test-btn",
                        ["text"] = "Test",
                        ["classes"] = new List<object> { "cls-a", "cls-b" },
                    },
                },
            });

            // Now inspect
            var result = Execute(new Dictionary<string, object>
            {
                ["operation"] = "inspectUXML",
                ["assetPath"] = path,
            });

            Assert.IsTrue((bool)result["success"]);
            var elements = result["elements"] as List<object>;
            Assert.IsNotNull(elements);
            Assert.AreEqual(1, elements.Count);

            var btn = elements[0] as Dictionary<string, object>;
            Assert.AreEqual("Button", btn["type"]);
            Assert.AreEqual("test-btn", btn["name"]);
            Assert.AreEqual("Test", btn["text"]);
        }

        [Test]
        public void InspectUXML_FileNotFound()
        {
            var result = Execute(new Dictionary<string, object>
            {
                ["operation"] = "inspectUXML",
                ["assetPath"] = $"{TestDir}/nonexistent.uxml",
            });

            Assert.IsFalse((bool)result["success"]);
        }

        #endregion

        #region InspectUSS Tests

        [Test]
        public void InspectUSS_ParsesRules()
        {
            var path = $"{TestDir}/inspect-test.uss";
            TrackAsset(path);

            Execute(new Dictionary<string, object>
            {
                ["operation"] = "createUSS",
                ["assetPath"] = path,
                ["rules"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["selector"] = ".my-class",
                        ["properties"] = new Dictionary<string, object>
                        {
                            ["color"] = "#ff0000",
                        },
                    },
                },
            });

            var result = Execute(new Dictionary<string, object>
            {
                ["operation"] = "inspectUSS",
                ["assetPath"] = path,
            });

            Assert.IsTrue((bool)result["success"]);
            var rules = result["rules"] as List<object>;
            Assert.IsNotNull(rules);
            Assert.AreEqual(1, rules.Count);

            var rule = rules[0] as Dictionary<string, object>;
            Assert.AreEqual(".my-class", rule["selector"]);
        }

        #endregion

        #region UpdateUXML Tests

        [Test]
        public void UpdateUXML_AddElement()
        {
            var path = $"{TestDir}/update-add.uxml";
            TrackAsset(path);

            Execute(new Dictionary<string, object>
            {
                ["operation"] = "createUXML",
                ["assetPath"] = path,
                ["elements"] = new List<object>
                {
                    new Dictionary<string, object> { ["type"] = "VisualElement", ["name"] = "root" },
                },
            });

            var result = Execute(new Dictionary<string, object>
            {
                ["operation"] = "updateUXML",
                ["assetPath"] = path,
                ["action"] = "add",
                ["parentElementName"] = "root",
                ["elements"] = new List<object>
                {
                    new Dictionary<string, object> { ["type"] = "Label", ["name"] = "added-label", ["text"] = "Added" },
                },
            });

            Assert.IsTrue((bool)result["success"]);
            var content = File.ReadAllText(Path.Combine(Application.dataPath, "..", path));
            Assert.IsTrue(content.Contains("added-label"));
        }

        [Test]
        public void UpdateUXML_RemoveElement()
        {
            var path = $"{TestDir}/update-remove.uxml";
            TrackAsset(path);

            Execute(new Dictionary<string, object>
            {
                ["operation"] = "createUXML",
                ["assetPath"] = path,
                ["elements"] = new List<object>
                {
                    new Dictionary<string, object> { ["type"] = "Label", ["name"] = "to-remove", ["text"] = "Remove Me" },
                    new Dictionary<string, object> { ["type"] = "Label", ["name"] = "keep", ["text"] = "Keep" },
                },
            });

            var result = Execute(new Dictionary<string, object>
            {
                ["operation"] = "updateUXML",
                ["assetPath"] = path,
                ["action"] = "remove",
                ["elementName"] = "to-remove",
            });

            Assert.IsTrue((bool)result["success"]);
            var content = File.ReadAllText(Path.Combine(Application.dataPath, "..", path));
            Assert.IsFalse(content.Contains("to-remove"));
            Assert.IsTrue(content.Contains("keep"));
        }

        [Test]
        public void UpdateUXML_ReplaceElement()
        {
            var path = $"{TestDir}/update-replace.uxml";
            TrackAsset(path);

            Execute(new Dictionary<string, object>
            {
                ["operation"] = "createUXML",
                ["assetPath"] = path,
                ["elements"] = new List<object>
                {
                    new Dictionary<string, object> { ["type"] = "Label", ["name"] = "old-elem", ["text"] = "Old" },
                },
            });

            var result = Execute(new Dictionary<string, object>
            {
                ["operation"] = "updateUXML",
                ["assetPath"] = path,
                ["action"] = "replace",
                ["elementName"] = "old-elem",
                ["element"] = new Dictionary<string, object>
                {
                    ["type"] = "Button",
                    ["name"] = "new-elem",
                    ["text"] = "New",
                },
            });

            Assert.IsTrue((bool)result["success"]);
            var content = File.ReadAllText(Path.Combine(Application.dataPath, "..", path));
            Assert.IsFalse(content.Contains("old-elem"));
            Assert.IsTrue(content.Contains("new-elem"));
        }

        #endregion

        #region UpdateUSS Tests

        [Test]
        public void UpdateUSS_AddRule()
        {
            var path = $"{TestDir}/update-add.uss";
            TrackAsset(path);

            Execute(new Dictionary<string, object>
            {
                ["operation"] = "createUSS",
                ["assetPath"] = path,
                ["rules"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["selector"] = ".existing",
                        ["properties"] = new Dictionary<string, object> { ["color"] = "#000" },
                    },
                },
            });

            var result = Execute(new Dictionary<string, object>
            {
                ["operation"] = "updateUSS",
                ["assetPath"] = path,
                ["action"] = "add",
                ["rules"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["selector"] = ".new-rule",
                        ["properties"] = new Dictionary<string, object> { ["font-size"] = "20px" },
                    },
                },
            });

            Assert.IsTrue((bool)result["success"]);
            var content = File.ReadAllText(Path.Combine(Application.dataPath, "..", path));
            Assert.IsTrue(content.Contains(".new-rule"));
            Assert.IsTrue(content.Contains("20px"));
            Assert.IsTrue(content.Contains(".existing"));
        }

        [Test]
        public void UpdateUSS_UpdateExistingRule()
        {
            var path = $"{TestDir}/update-update.uss";
            TrackAsset(path);

            Execute(new Dictionary<string, object>
            {
                ["operation"] = "createUSS",
                ["assetPath"] = path,
                ["rules"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["selector"] = ".target",
                        ["properties"] = new Dictionary<string, object> { ["color"] = "#old" },
                    },
                },
            });

            var result = Execute(new Dictionary<string, object>
            {
                ["operation"] = "updateUSS",
                ["assetPath"] = path,
                ["action"] = "update",
                ["rules"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["selector"] = ".target",
                        ["properties"] = new Dictionary<string, object> { ["color"] = "#new", ["font-size"] = "12px" },
                    },
                },
            });

            Assert.IsTrue((bool)result["success"]);
            var content = File.ReadAllText(Path.Combine(Application.dataPath, "..", path));
            Assert.IsTrue(content.Contains("#new"));
            Assert.IsTrue(content.Contains("12px"));
        }

        [Test]
        public void UpdateUSS_RemoveRule()
        {
            var path = $"{TestDir}/update-remove.uss";
            TrackAsset(path);

            Execute(new Dictionary<string, object>
            {
                ["operation"] = "createUSS",
                ["assetPath"] = path,
                ["rules"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["selector"] = ".keep",
                        ["properties"] = new Dictionary<string, object> { ["color"] = "#111" },
                    },
                    new Dictionary<string, object>
                    {
                        ["selector"] = ".remove-me",
                        ["properties"] = new Dictionary<string, object> { ["color"] = "#222" },
                    },
                },
            });

            var result = Execute(new Dictionary<string, object>
            {
                ["operation"] = "updateUSS",
                ["assetPath"] = path,
                ["action"] = "remove",
                ["selector"] = ".remove-me",
            });

            Assert.IsTrue((bool)result["success"]);
            var content = File.ReadAllText(Path.Combine(Application.dataPath, "..", path));
            Assert.IsTrue(content.Contains(".keep"));
            Assert.IsFalse(content.Contains(".remove-me"));
        }

        #endregion

        #region CreatePanelSettings Tests

        [Test]
        public void CreatePanelSettings_CreatesAsset()
        {
            var path = $"{TestDir}/TestPanelSettings.asset";
            TrackAsset(path);

            var result = Execute(new Dictionary<string, object>
            {
                ["operation"] = "createPanelSettings",
                ["assetPath"] = path,
                ["scaleMode"] = "scaleWithScreenSize",
                ["referenceResolution"] = new Dictionary<string, object> { ["x"] = 1280, ["y"] = 720 },
                ["match"] = 0.5,
            });

            Assert.IsTrue((bool)result["success"]);
            var ps = AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
            Assert.IsNotNull(ps);
            Assert.AreEqual(PanelScaleMode.ScaleWithScreenSize, ps.scaleMode);
            Assert.AreEqual(new Vector2Int(1280, 720), ps.referenceResolution);
        }

        #endregion

        #region CreateFromTemplate Tests

        [Test]
        public void CreateFromTemplate_Menu()
        {
            var uxmlPath = $"{TestDir}/menu.uxml";
            var ussPath = $"{TestDir}/menu.uss";
            TrackAsset(uxmlPath);
            TrackAsset(ussPath);

            var result = Execute(new Dictionary<string, object>
            {
                ["operation"] = "createFromTemplate",
                ["templateName"] = "menu",
                ["outputDir"] = TestDir,
                ["prefix"] = "menu",
                ["title"] = "My Game",
                ["buttons"] = new List<object> { "Play", "Settings", "Exit" },
            });

            Assert.IsTrue((bool)result["success"]);
            Assert.IsTrue(File.Exists(Path.Combine(Application.dataPath, "..", uxmlPath)));
            Assert.IsTrue(File.Exists(Path.Combine(Application.dataPath, "..", ussPath)));

            var uxmlContent = File.ReadAllText(Path.Combine(Application.dataPath, "..", uxmlPath));
            Assert.IsTrue(uxmlContent.Contains("My Game"));
            Assert.IsTrue(uxmlContent.Contains("play-btn"));
        }

        [Test]
        public void CreateFromTemplate_Dialog()
        {
            var uxmlPath = $"{TestDir}/dialog.uxml";
            var ussPath = $"{TestDir}/dialog.uss";
            TrackAsset(uxmlPath);
            TrackAsset(ussPath);

            var result = Execute(new Dictionary<string, object>
            {
                ["operation"] = "createFromTemplate",
                ["templateName"] = "dialog",
                ["outputDir"] = TestDir,
                ["prefix"] = "dialog",
            });

            Assert.IsTrue((bool)result["success"]);
            var content = File.ReadAllText(Path.Combine(Application.dataPath, "..", uxmlPath));
            Assert.IsTrue(content.Contains("ok-btn"));
            Assert.IsTrue(content.Contains("cancel-btn"));
        }

        [Test]
        public void CreateFromTemplate_Hud()
        {
            var uxmlPath = $"{TestDir}/hud.uxml";
            var ussPath = $"{TestDir}/hud.uss";
            TrackAsset(uxmlPath);
            TrackAsset(ussPath);

            var result = Execute(new Dictionary<string, object>
            {
                ["operation"] = "createFromTemplate",
                ["templateName"] = "hud",
                ["outputDir"] = TestDir,
                ["prefix"] = "hud",
            });

            Assert.IsTrue((bool)result["success"]);
            var content = File.ReadAllText(Path.Combine(Application.dataPath, "..", uxmlPath));
            Assert.IsTrue(content.Contains("hp-bar"));
            Assert.IsTrue(content.Contains("mp-bar"));
            Assert.IsTrue(content.Contains("gold-label"));
        }

        [Test]
        public void CreateFromTemplate_Settings()
        {
            var uxmlPath = $"{TestDir}/settings.uxml";
            var ussPath = $"{TestDir}/settings.uss";
            TrackAsset(uxmlPath);
            TrackAsset(ussPath);

            var result = Execute(new Dictionary<string, object>
            {
                ["operation"] = "createFromTemplate",
                ["templateName"] = "settings",
                ["outputDir"] = TestDir,
                ["prefix"] = "settings",
            });

            Assert.IsTrue((bool)result["success"]);
            var content = File.ReadAllText(Path.Combine(Application.dataPath, "..", uxmlPath));
            Assert.IsTrue(content.Contains("fullscreen-toggle"));
            Assert.IsTrue(content.Contains("volume-slider"));
        }

        [Test]
        public void CreateFromTemplate_Inventory()
        {
            var uxmlPath = $"{TestDir}/inventory.uxml";
            var ussPath = $"{TestDir}/inventory.uss";
            TrackAsset(uxmlPath);
            TrackAsset(ussPath);

            var result = Execute(new Dictionary<string, object>
            {
                ["operation"] = "createFromTemplate",
                ["templateName"] = "inventory",
                ["outputDir"] = TestDir,
                ["prefix"] = "inventory",
                ["columns"] = 4,
                ["slotCount"] = 8,
            });

            Assert.IsTrue((bool)result["success"]);
            var content = File.ReadAllText(Path.Combine(Application.dataPath, "..", uxmlPath));
            Assert.IsTrue(content.Contains("slot-0"));
            Assert.IsTrue(content.Contains("slot-7"));
            Assert.IsFalse(content.Contains("slot-8"));
        }

        [Test]
        public void CreateFromTemplate_UnknownTemplate()
        {
            var result = Execute(new Dictionary<string, object>
            {
                ["operation"] = "createFromTemplate",
                ["templateName"] = "nonexistent",
                ["outputDir"] = TestDir,
            });

            Assert.IsFalse((bool)result["success"]);
        }

        #endregion

        #region ValidateDependencies Tests

        [Test]
        public void ValidateDependencies_ValidReferences()
        {
            var ussPath = $"{TestDir}/valid.uss";
            var uxmlPath = $"{TestDir}/valid.uxml";
            TrackAsset(ussPath);
            TrackAsset(uxmlPath);

            Execute(new Dictionary<string, object>
            {
                ["operation"] = "createUSS",
                ["assetPath"] = ussPath,
                ["rules"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["selector"] = ".test",
                        ["properties"] = new Dictionary<string, object> { ["color"] = "#fff" },
                    },
                },
            });

            Execute(new Dictionary<string, object>
            {
                ["operation"] = "createUXML",
                ["assetPath"] = uxmlPath,
                ["styleSheets"] = new List<object> { ussPath },
                ["elements"] = new List<object>
                {
                    new Dictionary<string, object> { ["type"] = "Label", ["text"] = "Hello" },
                },
            });

            var result = Execute(new Dictionary<string, object>
            {
                ["operation"] = "validateDependencies",
                ["assetPath"] = uxmlPath,
            });

            Assert.IsTrue((bool)result["success"]);
            Assert.IsTrue((bool)result["isValid"]);

            var deps = result["dependencies"] as List<object>;
            Assert.IsNotNull(deps);
            Assert.AreEqual(1, deps.Count);

            var dep = deps[0] as Dictionary<string, object>;
            Assert.AreEqual("stylesheet", dep["type"]);
            Assert.IsTrue((bool)dep["exists"]);

            var issues = result["issues"] as List<object>;
            Assert.IsNotNull(issues);
            Assert.AreEqual(0, issues.Count);
        }

        [Test]
        public void ValidateDependencies_MissingStylesheet()
        {
            var uxmlPath = $"{TestDir}/missing-ref.uxml";
            TrackAsset(uxmlPath);

            Execute(new Dictionary<string, object>
            {
                ["operation"] = "createUXML",
                ["assetPath"] = uxmlPath,
                ["styleSheets"] = new List<object> { "Assets/UI/nonexistent.uss" },
                ["elements"] = new List<object>
                {
                    new Dictionary<string, object> { ["type"] = "Label", ["text"] = "Hello" },
                },
            });

            var result = Execute(new Dictionary<string, object>
            {
                ["operation"] = "validateDependencies",
                ["assetPath"] = uxmlPath,
            });

            Assert.IsTrue((bool)result["success"]);
            Assert.IsFalse((bool)result["isValid"]);

            var deps = result["dependencies"] as List<object>;
            Assert.AreEqual(1, deps.Count);
            var dep = deps[0] as Dictionary<string, object>;
            Assert.IsFalse((bool)dep["exists"]);

            var issues = result["issues"] as List<object>;
            Assert.AreEqual(1, issues.Count);
            var issue = issues[0] as Dictionary<string, object>;
            Assert.AreEqual("missing_stylesheet", issue["type"]);
            Assert.AreEqual("error", issue["severity"]);
        }

        [Test]
        public void ValidateDependencies_NoStylesheets()
        {
            var uxmlPath = $"{TestDir}/no-styles.uxml";
            TrackAsset(uxmlPath);

            Execute(new Dictionary<string, object>
            {
                ["operation"] = "createUXML",
                ["assetPath"] = uxmlPath,
                ["elements"] = new List<object>
                {
                    new Dictionary<string, object> { ["type"] = "Label", ["text"] = "Hello" },
                },
            });

            var result = Execute(new Dictionary<string, object>
            {
                ["operation"] = "validateDependencies",
                ["assetPath"] = uxmlPath,
            });

            Assert.IsTrue((bool)result["success"]);
            Assert.IsTrue((bool)result["isValid"]);

            var deps = result["dependencies"] as List<object>;
            Assert.AreEqual(0, deps.Count);

            var issues = result["issues"] as List<object>;
            Assert.AreEqual(0, issues.Count);
        }

        [Test]
        public void ValidateDependencies_FileNotFound()
        {
            var result = Execute(new Dictionary<string, object>
            {
                ["operation"] = "validateDependencies",
                ["assetPath"] = $"{TestDir}/nonexistent.uxml",
            });

            Assert.IsFalse((bool)result["success"]);
        }

        [Test]
        public void CreateUXML_IncludesDependencyInfo()
        {
            var uxmlPath = $"{TestDir}/dep-info.uxml";
            TrackAsset(uxmlPath);

            var result = Execute(new Dictionary<string, object>
            {
                ["operation"] = "createUXML",
                ["assetPath"] = uxmlPath,
                ["elements"] = new List<object>
                {
                    new Dictionary<string, object> { ["type"] = "Label", ["text"] = "Hi" },
                },
            });

            Assert.IsTrue((bool)result["success"]);
            Assert.IsTrue(result.ContainsKey("dependencies"));
            Assert.IsTrue(result.ContainsKey("issues"));
            Assert.IsTrue(result.ContainsKey("isValid"));
            Assert.IsTrue((bool)result["isValid"]);
        }

        [Test]
        public void InspectUXML_IncludesDependencyInfo()
        {
            var ussPath = $"{TestDir}/inspect-dep.uss";
            var uxmlPath = $"{TestDir}/inspect-dep.uxml";
            TrackAsset(ussPath);
            TrackAsset(uxmlPath);

            Execute(new Dictionary<string, object>
            {
                ["operation"] = "createUSS",
                ["assetPath"] = ussPath,
                ["rules"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["selector"] = ".x",
                        ["properties"] = new Dictionary<string, object> { ["color"] = "#000" },
                    },
                },
            });

            Execute(new Dictionary<string, object>
            {
                ["operation"] = "createUXML",
                ["assetPath"] = uxmlPath,
                ["styleSheets"] = new List<object> { ussPath },
                ["elements"] = new List<object>
                {
                    new Dictionary<string, object> { ["type"] = "Label", ["text"] = "Test" },
                },
            });

            var result = Execute(new Dictionary<string, object>
            {
                ["operation"] = "inspectUXML",
                ["assetPath"] = uxmlPath,
            });

            Assert.IsTrue((bool)result["success"]);
            Assert.IsTrue(result.ContainsKey("dependencies"));
            Assert.IsTrue(result.ContainsKey("issues"));
            Assert.IsTrue(result.ContainsKey("isValid"));
            Assert.IsTrue((bool)result["isValid"]);

            var deps = result["dependencies"] as List<object>;
            Assert.AreEqual(1, deps.Count);
        }

        #endregion

        #region Error Handling Tests

        [Test]
        public void UnsupportedElementType_ReturnsError()
        {
            var result = Execute(new Dictionary<string, object>
            {
                ["operation"] = "createUXML",
                ["assetPath"] = $"{TestDir}/error.uxml",
                ["elements"] = new List<object>
                {
                    new Dictionary<string, object> { ["type"] = "InvalidType" },
                },
            });

            Assert.IsFalse((bool)result["success"]);
        }

        [Test]
        public void MissingAssetPath_ReturnsError()
        {
            var result = Execute(new Dictionary<string, object>
            {
                ["operation"] = "createUXML",
            });

            Assert.IsFalse((bool)result["success"]);
        }

        #endregion
    }
}
