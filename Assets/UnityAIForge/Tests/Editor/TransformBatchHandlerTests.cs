using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MCP.Editor.Handlers;

namespace MCP.Editor.Tests
{
    /// <summary>
    /// Unit tests for TransformBatchHandler.
    /// </summary>
    [TestFixture]
    public class TransformBatchHandlerTests
    {
        private TransformBatchHandler _handler;
        private List<GameObject> _createdObjects;

        [SetUp]
        public void SetUp()
        {
            _handler = new TransformBatchHandler();
            _createdObjects = new List<GameObject>();
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
        }

        private GameObject CreateTestGameObject(string name, Transform parent = null)
        {
            var go = new GameObject(name);
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }
            _createdObjects.Add(go);
            return go;
        }

        #region Property Tests

        [Test]
        public void Category_ShouldReturnTransformBatch()
        {
            Assert.AreEqual("transformBatch", _handler.Category);
        }

        [Test]
        public void Version_ShouldReturn100()
        {
            Assert.AreEqual("1.0.0", _handler.Version);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            var operations = new List<string>(_handler.SupportedOperations);
            Assert.Contains("arrangeCircle", operations);
            Assert.Contains("arrangeLine", operations);
            Assert.Contains("renameSequential", operations);
            Assert.Contains("renameFromList", operations);
            Assert.Contains("createMenuList", operations);
        }

        #endregion

        #region ArrangeCircle Tests

        [Test]
        public void Execute_ArrangeCircle_ShouldArrangeObjectsInCircle()
        {
            var obj1 = CreateTestGameObject("CircleObj1");
            var obj2 = CreateTestGameObject("CircleObj2");
            var obj3 = CreateTestGameObject("CircleObj3");
            var obj4 = CreateTestGameObject("CircleObj4");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "arrangeCircle",
                ["gameObjectPaths"] = new List<object> { "CircleObj1", "CircleObj2", "CircleObj3", "CircleObj4" },
                ["center"] = new Dictionary<string, object> { ["x"] = 0, ["y"] = 0, ["z"] = 0 },
                ["radius"] = 5f
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual(4, (int)result["count"]);

            // Objects should be arranged in a circle
            var positions = new[] { obj1, obj2, obj3, obj4 }.Select(o => o.transform.position).ToList();
            foreach (var pos in positions)
            {
                var distance = Vector3.Distance(Vector3.zero, pos);
                Assert.AreEqual(5f, distance, 0.01f);
            }
        }

        [Test]
        public void Execute_ArrangeCircle_WithStartAngle_ShouldUseSpecifiedAngle()
        {
            var obj1 = CreateTestGameObject("AngleObj1");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "arrangeCircle",
                ["gameObjectPaths"] = new List<object> { "AngleObj1" },
                ["center"] = new Dictionary<string, object> { ["x"] = 0, ["y"] = 0, ["z"] = 0 },
                ["radius"] = 1f,
                ["startAngle"] = 90f
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
        }

        #endregion

        #region ArrangeLine Tests

        [Test]
        public void Execute_ArrangeLine_ShouldArrangeObjectsInLine()
        {
            var obj1 = CreateTestGameObject("LineObj1");
            var obj2 = CreateTestGameObject("LineObj2");
            var obj3 = CreateTestGameObject("LineObj3");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "arrangeLine",
                ["gameObjectPaths"] = new List<object> { "LineObj1", "LineObj2", "LineObj3" },
                ["startPosition"] = new Dictionary<string, object> { ["x"] = 0, ["y"] = 0, ["z"] = 0 },
                ["endPosition"] = new Dictionary<string, object> { ["x"] = 10, ["y"] = 0, ["z"] = 0 }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual(3, (int)result["count"]);

            Assert.AreEqual(0f, obj1.transform.position.x, 0.01f);
            Assert.AreEqual(5f, obj2.transform.position.x, 0.01f);
            Assert.AreEqual(10f, obj3.transform.position.x, 0.01f);
        }

        [Test]
        public void Execute_ArrangeLine_WithSingleObject_ShouldPlaceAtStart()
        {
            var obj1 = CreateTestGameObject("SingleLineObj");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "arrangeLine",
                ["gameObjectPaths"] = new List<object> { "SingleLineObj" },
                ["startPosition"] = new Dictionary<string, object> { ["x"] = 5, ["y"] = 0, ["z"] = 0 },
                ["endPosition"] = new Dictionary<string, object> { ["x"] = 10, ["y"] = 0, ["z"] = 0 }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual(5f, obj1.transform.position.x, 0.01f);
        }

        #endregion

        #region RenameSequential Tests

        [Test]
        public void Execute_RenameSequential_ShouldRenameWithNumbers()
        {
            var obj1 = CreateTestGameObject("OldName1");
            var obj2 = CreateTestGameObject("OldName2");
            var obj3 = CreateTestGameObject("OldName3");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "renameSequential",
                ["gameObjectPaths"] = new List<object> { "OldName1", "OldName2", "OldName3" },
                ["baseName"] = "Item_",
                ["startIndex"] = 1
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual(3, (int)result["count"]);

            Assert.AreEqual("Item_1", obj1.name);
            Assert.AreEqual("Item_2", obj2.name);
            Assert.AreEqual("Item_3", obj3.name);
        }

        [Test]
        public void Execute_RenameSequential_WithPadding_ShouldPadNumbers()
        {
            var obj1 = CreateTestGameObject("PadObj1");
            var obj2 = CreateTestGameObject("PadObj2");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "renameSequential",
                ["gameObjectPaths"] = new List<object> { "PadObj1", "PadObj2" },
                ["baseName"] = "Item",
                ["startIndex"] = 1,
                ["padding"] = 3
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            Assert.AreEqual("Item001", obj1.name);
            Assert.AreEqual("Item002", obj2.name);
        }

        [Test]
        public void Execute_RenameSequential_MissingBaseName_ShouldReturnError()
        {
            CreateTestGameObject("RenameObj");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "renameSequential",
                ["gameObjectPaths"] = new List<object> { "RenameObj" }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
        }

        #endregion

        #region RenameFromList Tests

        [Test]
        public void Execute_RenameFromList_ShouldRenameWithProvidedNames()
        {
            var obj1 = CreateTestGameObject("ListObj1");
            var obj2 = CreateTestGameObject("ListObj2");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "renameFromList",
                ["gameObjectPaths"] = new List<object> { "ListObj1", "ListObj2" },
                ["names"] = new List<object> { "NewName1", "NewName2" }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            Assert.AreEqual("NewName1", obj1.name);
            Assert.AreEqual("NewName2", obj2.name);
        }

        [Test]
        public void Execute_RenameFromList_MismatchedCount_ShouldReturnError()
        {
            CreateTestGameObject("MismatchObj1");
            CreateTestGameObject("MismatchObj2");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "renameFromList",
                ["gameObjectPaths"] = new List<object> { "MismatchObj1", "MismatchObj2" },
                ["names"] = new List<object> { "OnlyOneName" }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
        }

        #endregion

        #region CreateMenuList Tests

        [Test]
        public void Execute_CreateMenuList_ShouldCreateChildObjects()
        {
            var parent = CreateTestGameObject("MenuParent");

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "createMenuList",
                ["parentPath"] = "MenuParent",
                ["names"] = new List<object> { "MenuItem1", "MenuItem2", "MenuItem3" }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual(3, (int)result["count"]);

            Assert.AreEqual(3, parent.transform.childCount);

            // Cleanup children
            for (int i = parent.transform.childCount - 1; i >= 0; i--)
            {
                _createdObjects.Add(parent.transform.GetChild(i).gameObject);
            }
        }

        [Test]
        public void Execute_CreateMenuList_MissingParent_ShouldReturnError()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "createMenuList",
                ["names"] = new List<object> { "Item1" }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
        }

        #endregion

        #region Error Handling Tests

        [Test]
        public void Execute_MissingOperation_ShouldReturnError()
        {
            var payload = new Dictionary<string, object>
            {
                ["gameObjectPaths"] = new List<object> { "SomeObject" }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
        }

        [Test]
        public void Execute_UnsupportedOperation_ShouldReturnError()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "unsupportedOperation"
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
        }

        [Test]
        public void Execute_ArrangeCircle_EmptyObjects_ShouldReturnError()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "arrangeCircle",
                ["gameObjectPaths"] = new List<object>()
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
        }

        #endregion
    }
}
