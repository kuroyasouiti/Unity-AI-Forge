using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using MCP.Editor.Handlers;

namespace MCP.Editor.Tests
{
    /// <summary>
    /// Unit tests for UITKDocumentHandler.
    /// </summary>
    [TestFixture]
    public class UITKDocumentHandlerTests
    {
        private UITKDocumentHandler _handler;
        private List<GameObject> _createdObjects;
        private List<string> _createdAssets;
        private const string TestDir = "Assets/Tests/UITKDocTests";

        [SetUp]
        public void SetUp()
        {
            _handler = new UITKDocumentHandler();
            _createdObjects = new List<GameObject>();
            _createdAssets = new List<string>();

            if (!AssetDatabase.IsValidFolder(TestDir))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Tests"))
                    AssetDatabase.CreateFolder("Assets", "Tests");
                AssetDatabase.CreateFolder("Assets/Tests", "UITKDocTests");
            }
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _createdObjects)
            {
                if (obj != null)
                {
                    Undo.ClearUndo(obj);
                    Object.DestroyImmediate(obj);
                }
            }
            _createdObjects.Clear();

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

        private void TrackGameObject(string name)
        {
            var go = GameObject.Find(name);
            if (go != null && !_createdObjects.Contains(go))
                _createdObjects.Add(go);
        }

        private void TrackAsset(string path)
        {
            if (!_createdAssets.Contains(path))
                _createdAssets.Add(path);
        }

        private string CreateTestPanelSettings()
        {
            var path = $"{TestDir}/TestPanel.asset";
            TrackAsset(path);

            var ps = ScriptableObject.CreateInstance<PanelSettings>();
            ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            ps.referenceResolution = new Vector2Int(1920, 1080);

            var dir = Path.GetDirectoryName(Path.Combine(Application.dataPath, "..", path));
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            AssetDatabase.CreateAsset(ps, path);
            AssetDatabase.SaveAssets();
            return path;
        }

        #region Property Tests

        [Test]
        public void Category_ShouldReturnUITKDocument()
        {
            Assert.AreEqual("uitkDocument", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ShouldContainAllOperations()
        {
            var ops = new List<string>(_handler.SupportedOperations);
            Assert.Contains("create", ops);
            Assert.Contains("inspect", ops);
            Assert.Contains("update", ops);
            Assert.Contains("delete", ops);
            Assert.Contains("query", ops);
        }

        #endregion

        #region Create Tests

        [Test]
        public void Create_BasicUIDocument()
        {
            var result = Execute(new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["name"] = "TestUIDoc",
            });

            Assert.IsTrue((bool)result["success"]);
            TrackGameObject("TestUIDoc");

            var go = GameObject.Find("TestUIDoc");
            Assert.IsNotNull(go);
            Assert.IsNotNull(go.GetComponent<UIDocument>());
        }

        [Test]
        public void Create_WithPanelSettings()
        {
            var psPath = CreateTestPanelSettings();

            var result = Execute(new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["name"] = "TestUIDocPS",
                ["panelSettings"] = psPath,
            });

            Assert.IsTrue((bool)result["success"]);
            TrackGameObject("TestUIDocPS");

            var go = GameObject.Find("TestUIDocPS");
            var uiDoc = go.GetComponent<UIDocument>();
            Assert.IsNotNull(uiDoc.panelSettings);
        }

        [Test]
        public void Create_WithParent()
        {
            var parent = new GameObject("UIParent");
            _createdObjects.Add(parent);

            var result = Execute(new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["name"] = "ChildDoc",
                ["parentPath"] = "UIParent",
            });

            Assert.IsTrue((bool)result["success"]);
            TrackGameObject("ChildDoc");

            var child = GameObject.Find("ChildDoc");
            Assert.IsNotNull(child);
            Assert.AreEqual(parent.transform, child.transform.parent);
        }

        [Test]
        public void Create_WithSortingOrder()
        {
            var result = Execute(new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["name"] = "SortedDoc",
                ["sortingOrder"] = 10.0,
            });

            Assert.IsTrue((bool)result["success"]);
            TrackGameObject("SortedDoc");

            var go = GameObject.Find("SortedDoc");
            var uiDoc = go.GetComponent<UIDocument>();
            Assert.AreEqual(10f, uiDoc.sortingOrder);
        }

        #endregion

        #region Inspect Tests

        [Test]
        public void Inspect_ReturnsDocumentInfo()
        {
            var psPath = CreateTestPanelSettings();

            Execute(new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["name"] = "InspectDoc",
                ["panelSettings"] = psPath,
                ["sortingOrder"] = 5.0,
            });
            TrackGameObject("InspectDoc");

            var result = Execute(new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["gameObjectPath"] = "InspectDoc",
            });

            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual(psPath, result["panelSettings"]);
            Assert.AreEqual(5f, System.Convert.ToSingle(result["sortingOrder"]));
        }

        [Test]
        public void Inspect_NoUIDocument_ReturnsError()
        {
            var go = new GameObject("NoDocObj");
            _createdObjects.Add(go);

            var result = Execute(new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["gameObjectPath"] = "NoDocObj",
            });

            Assert.IsFalse((bool)result["success"]);
        }

        #endregion

        #region Update Tests

        [Test]
        public void Update_SortingOrder()
        {
            Execute(new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["name"] = "UpdateDoc",
            });
            TrackGameObject("UpdateDoc");

            var result = Execute(new Dictionary<string, object>
            {
                ["operation"] = "update",
                ["gameObjectPath"] = "UpdateDoc",
                ["sortingOrder"] = 99.0,
            });

            Assert.IsTrue((bool)result["success"]);
            var uiDoc = GameObject.Find("UpdateDoc").GetComponent<UIDocument>();
            Assert.AreEqual(99f, uiDoc.sortingOrder);
        }

        [Test]
        public void Update_PanelSettings()
        {
            Execute(new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["name"] = "UpdatePSDoc",
            });
            TrackGameObject("UpdatePSDoc");

            var psPath = CreateTestPanelSettings();

            var result = Execute(new Dictionary<string, object>
            {
                ["operation"] = "update",
                ["gameObjectPath"] = "UpdatePSDoc",
                ["panelSettings"] = psPath,
            });

            Assert.IsTrue((bool)result["success"]);
            var uiDoc = GameObject.Find("UpdatePSDoc").GetComponent<UIDocument>();
            Assert.IsNotNull(uiDoc.panelSettings);
        }

        [Test]
        public void Update_ClearPanelSettings()
        {
            var psPath = CreateTestPanelSettings();

            Execute(new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["name"] = "ClearPSDoc",
                ["panelSettings"] = psPath,
            });
            TrackGameObject("ClearPSDoc");

            var result = Execute(new Dictionary<string, object>
            {
                ["operation"] = "update",
                ["gameObjectPath"] = "ClearPSDoc",
                ["panelSettings"] = "",
            });

            Assert.IsTrue((bool)result["success"]);
            var uiDoc = GameObject.Find("ClearPSDoc").GetComponent<UIDocument>();
            Assert.IsNull(uiDoc.panelSettings);
        }

        #endregion

        #region Delete Tests

        [Test]
        public void Delete_ComponentOnly()
        {
            Execute(new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["name"] = "DeleteCompDoc",
            });
            TrackGameObject("DeleteCompDoc");

            var result = Execute(new Dictionary<string, object>
            {
                ["operation"] = "delete",
                ["gameObjectPath"] = "DeleteCompDoc",
            });

            Assert.IsTrue((bool)result["success"]);
            var go = GameObject.Find("DeleteCompDoc");
            Assert.IsNotNull(go); // GO still exists
            Assert.IsNull(go.GetComponent<UIDocument>()); // Component removed
        }

        [Test]
        public void Delete_EntireGameObject()
        {
            Execute(new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["name"] = "DeleteGODoc",
            });
            // Don't track - it will be deleted

            var result = Execute(new Dictionary<string, object>
            {
                ["operation"] = "delete",
                ["gameObjectPath"] = "DeleteGODoc",
                ["deleteGameObject"] = true,
            });

            Assert.IsTrue((bool)result["success"]);
            Assert.IsNull(GameObject.Find("DeleteGODoc"));
        }

        #endregion

        #region Query Tests

        [Test]
        public void Query_NoTree_ReturnsError()
        {
            Execute(new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["name"] = "QueryDoc",
            });
            TrackGameObject("QueryDoc");

            // In editor mode without play, rootVisualElement is null
            var result = Execute(new Dictionary<string, object>
            {
                ["operation"] = "query",
                ["gameObjectPath"] = "QueryDoc",
                ["queryName"] = "test",
            });

            // Should fail gracefully since tree isn't available in edit mode
            Assert.IsFalse((bool)result["success"]);
        }

        [Test]
        public void Query_NoUIDocument_ReturnsError()
        {
            var go = new GameObject("NoDocQuery");
            _createdObjects.Add(go);

            var result = Execute(new Dictionary<string, object>
            {
                ["operation"] = "query",
                ["gameObjectPath"] = "NoDocQuery",
            });

            Assert.IsFalse((bool)result["success"]);
        }

        #endregion

        #region Error Handling Tests

        [Test]
        public void MissingGameObjectPath_ForInspect_ReturnsError()
        {
            var result = Execute(new Dictionary<string, object>
            {
                ["operation"] = "inspect",
            });

            Assert.IsFalse((bool)result["success"]);
        }

        [Test]
        public void InvalidOperation_ReturnsError()
        {
            var result = Execute(new Dictionary<string, object>
            {
                ["operation"] = "invalidOp",
            });

            Assert.IsFalse((bool)result["success"]);
        }

        #endregion
    }
}
