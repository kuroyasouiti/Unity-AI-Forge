using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MCP.Editor.Handlers;

namespace MCP.Editor.Tests
{
    /// <summary>
    /// GameObjectCommandHandler のユニットテスト。
    /// </summary>
    [TestFixture]
    public class GameObjectCommandHandlerTests
    {
        private GameObjectCommandHandler _handler;
        private List<GameObject> _createdObjects;

        [SetUp]
        public void SetUp()
        {
            _handler = new GameObjectCommandHandler();
            _createdObjects = new List<GameObject>();
        }

        [TearDown]
        public void TearDown()
        {
            // テスト中に作成したオブジェクトを削除
            foreach (var obj in _createdObjects)
            {
                if (obj != null)
                {
                    Undo.ClearUndo(obj);
                    UnityEngine.Object.DestroyImmediate(obj);
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
        public void Category_ShouldReturnGameObject()
        {
            Assert.AreEqual("gameObject", _handler.Category);
        }

        [Test]
        public void Version_ShouldReturn100()
        {
            Assert.AreEqual("1.0.0", _handler.Version);
        }

        [Test]
        public void SupportedOperations_ShouldContainExpectedOperations()
        {
            var operations = _handler.SupportedOperations.ToList();
            
            Assert.Contains("create", operations);
            Assert.Contains("delete", operations);
            Assert.Contains("move", operations);
            Assert.Contains("rename", operations);
            Assert.Contains("update", operations);
            Assert.Contains("duplicate", operations);
            Assert.Contains("inspect", operations);
            Assert.Contains("findMultiple", operations);
            Assert.Contains("deleteMultiple", operations);
            Assert.Contains("inspectMultiple", operations);
        }

        #endregion

        #region Create Tests

        [Test]
        public void Execute_Create_WithName_ShouldCreateGameObject()
        {
            // Arrange
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["name"] = "TestObject"
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual("TestObject", result["name"]);
            Assert.IsNotNull(result["instanceID"]);
            
            // クリーンアップ用に追加
            var go = GameObject.Find("TestObject");
            if (go != null) _createdObjects.Add(go);
        }

        [Test]
        public void Execute_Create_WithoutName_ShouldCreateGameObjectWithDefaultName()
        {
            // Arrange
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create"
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual("New GameObject", result["name"]);
            
            // クリーンアップ用に追加
            var go = GameObject.Find("New GameObject");
            if (go != null) _createdObjects.Add(go);
        }

        [Test]
        public void Execute_Create_WithParentPath_ShouldSetParent()
        {
            // Arrange
            var parent = CreateTestGameObject("ParentObject");
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["name"] = "ChildObject",
                ["parentPath"] = "ParentObject"
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsTrue(result["gameObjectPath"].ToString().Contains("ParentObject"));
            
            // クリーンアップ用に追加
            var child = parent.transform.Find("ChildObject");
            if (child != null) _createdObjects.Add(child.gameObject);
        }

        #endregion

        #region Delete Tests

        [Test]
        public void Execute_Delete_ExistingGameObject_ShouldDelete()
        {
            // Arrange
            var go = CreateTestGameObject("ToDelete");
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "delete",
                ["gameObjectPath"] = "ToDelete"
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            
            // オブジェクトが削除されていることを確認
            var found = GameObject.Find("ToDelete");
            Assert.IsNull(found);
            
            // 削除済みなのでリストから削除
            _createdObjects.Remove(go);
        }

        [Test]
        public void Execute_Delete_NonExistentGameObject_ShouldReturnError()
        {
            // Arrange
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "delete",
                ["gameObjectPath"] = "NonExistentObject"
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
        }

        #endregion

        #region Rename Tests

        [Test]
        public void Execute_Rename_ShouldRenameGameObject()
        {
            // Arrange
            var go = CreateTestGameObject("OldName");
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "rename",
                ["gameObjectPath"] = "OldName",
                ["name"] = "NewName"
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual("OldName", result["oldName"]);
            Assert.AreEqual("NewName", result["newName"]);
            Assert.AreEqual("NewName", go.name);
        }

        [Test]
        public void Execute_Rename_WithoutName_ShouldReturnError()
        {
            // Arrange
            CreateTestGameObject("TestObject");
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "rename",
                ["gameObjectPath"] = "TestObject"
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
        }

        #endregion

        #region Move Tests

        [Test]
        public void Execute_Move_ToNewParent_ShouldMoveGameObject()
        {
            // Arrange
            var newParent = CreateTestGameObject("NewParent");
            var child = CreateTestGameObject("ChildToMove");
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "move",
                ["gameObjectPath"] = "ChildToMove",
                ["parentPath"] = "NewParent"
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual(newParent.transform, child.transform.parent);
        }

        [Test]
        public void Execute_Move_ToRoot_ShouldMoveToRoot()
        {
            // Arrange
            var parent = CreateTestGameObject("Parent");
            var child = new GameObject("ChildToMoveToRoot");
            child.transform.SetParent(parent.transform, false);
            _createdObjects.Add(child);
            
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "move",
                ["gameObjectPath"] = "Parent/ChildToMoveToRoot",
                ["parentPath"] = ""  // Empty means root
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsNull(child.transform.parent);
        }

        #endregion

        #region Update Tests

        [Test]
        public void Execute_Update_Tag_ShouldUpdateTag()
        {
            // Arrange
            var go = CreateTestGameObject("TestObject");
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "update",
                ["gameObjectPath"] = "TestObject",
                ["tag"] = "MainCamera"
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual("MainCamera", go.tag);
        }

        [Test]
        public void Execute_Update_Layer_ByName_ShouldUpdateLayer()
        {
            // Arrange
            var go = CreateTestGameObject("TestObject");
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "update",
                ["gameObjectPath"] = "TestObject",
                ["layer"] = "UI"
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual(LayerMask.NameToLayer("UI"), go.layer);
        }

        [Test]
        public void Execute_Update_Active_ShouldUpdateActiveState()
        {
            // Arrange
            var go = CreateTestGameObject("TestObject");
            go.SetActive(true);
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "update",
                ["gameObjectPath"] = "TestObject",
                ["active"] = false
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsFalse(go.activeSelf);
        }

        [Test]
        public void Execute_Update_Static_ShouldUpdateStaticFlag()
        {
            // Arrange
            var go = CreateTestGameObject("TestObject");
            go.isStatic = false;
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "update",
                ["gameObjectPath"] = "TestObject",
                ["static"] = true
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsTrue(go.isStatic);
        }

        #endregion

        #region Duplicate Tests

        [Test]
        public void Execute_Duplicate_ShouldCreateCopy()
        {
            // Arrange
            var original = CreateTestGameObject("OriginalObject");
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "duplicate",
                ["gameObjectPath"] = "OriginalObject"
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreNotEqual(result["originalPath"], result["duplicatePath"]);
            
            // クリーンアップ用に追加
            var duplicateName = result["name"].ToString();
            var duplicate = GameObject.Find(duplicateName);
            if (duplicate != null) _createdObjects.Add(duplicate);
        }

        #endregion

        #region Inspect Tests

        [Test]
        public void Execute_Inspect_ShouldReturnGameObjectInfo()
        {
            // Arrange
            var go = CreateTestGameObject("InspectObject");
            go.transform.position = new Vector3(1, 2, 3);
            go.AddComponent<BoxCollider>();
            
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["gameObjectPath"] = "InspectObject"
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual("InspectObject", result["name"]);
            Assert.IsNotNull(result["instanceID"]);
            Assert.IsTrue(result.ContainsKey("transform"));
            Assert.IsTrue(result.ContainsKey("components"));
            
            // components は List<Dictionary<string, object>> として返されるため、IList を使用
            var components = result["components"] as System.Collections.IList;
            Assert.IsNotNull(components, "components should not be null. Actual type: " + result["components"]?.GetType()?.Name);
            Assert.IsTrue(components.Count >= 2); // Transform + BoxCollider
        }

        [Test]
        public void Execute_Inspect_ShouldIncludeTransformInfo()
        {
            // Arrange
            var go = CreateTestGameObject("TransformObject");
            go.transform.position = new Vector3(1, 2, 3);
            go.transform.localScale = new Vector3(2, 2, 2);
            
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["gameObjectPath"] = "TransformObject"
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            var transform = result["transform"] as Dictionary<string, object>;
            Assert.IsNotNull(transform);
            Assert.IsTrue(transform.ContainsKey("position"));
            Assert.IsTrue(transform.ContainsKey("localPosition"));
            Assert.IsTrue(transform.ContainsKey("rotation"));
            Assert.IsTrue(transform.ContainsKey("localScale"));
        }

        #endregion

        #region FindMultiple Tests

        [Test]
        public void Execute_FindMultiple_WithPattern_ShouldFindMatching()
        {
            // Arrange
            CreateTestGameObject("Test_A");
            CreateTestGameObject("Test_B");
            CreateTestGameObject("Other_C");
            
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "findMultiple",
                ["pattern"] = "Test_*"
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual(2, (int)result["count"]);
        }

        [Test]
        public void Execute_FindMultiple_WithRegex_ShouldFindMatching()
        {
            // Arrange
            CreateTestGameObject("Item1");
            CreateTestGameObject("Item2");
            CreateTestGameObject("Other");
            
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "findMultiple",
                ["pattern"] = "Item\\d",
                ["useRegex"] = true
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual(2, (int)result["count"]);
        }

        [Test]
        public void Execute_FindMultiple_WithMaxResults_ShouldLimitResults()
        {
            // Arrange
            for (int i = 0; i < 10; i++)
            {
                CreateTestGameObject($"LimitTest_{i}");
            }
            
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "findMultiple",
                ["pattern"] = "LimitTest_*",
                ["maxResults"] = 5
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.LessOrEqual((int)result["count"], 5);
        }

        #endregion

        #region DeleteMultiple Tests

        [Test]
        public void Execute_DeleteMultiple_WithPattern_ShouldDeleteMatching()
        {
            // Arrange
            var toDelete1 = CreateTestGameObject("Delete_A");
            var toDelete2 = CreateTestGameObject("Delete_B");
            CreateTestGameObject("Keep_C");
            
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "deleteMultiple",
                ["pattern"] = "Delete_*"
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual(2, (int)result["count"]);
            
            // 削除済みなのでリストから削除
            _createdObjects.Remove(toDelete1);
            _createdObjects.Remove(toDelete2);
            
            // Keep_C は残っているはず
            Assert.IsNotNull(GameObject.Find("Keep_C"));
        }

        #endregion

        #region InspectMultiple Tests

        [Test]
        public void Execute_InspectMultiple_WithPattern_ShouldReturnMultipleInfo()
        {
            // Arrange
            CreateTestGameObject("Inspect_A");
            CreateTestGameObject("Inspect_B");
            
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "inspectMultiple",
                ["pattern"] = "Inspect_*",
                ["includeComponents"] = true
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual(2, (int)result["count"]);
            
            // results は List<Dictionary<string, object>> として返されるため、IList を使用
            var results = result["results"] as System.Collections.IList;
            Assert.IsNotNull(results, "results should not be null. Actual type: " + result["results"]?.GetType()?.Name);
            Assert.AreEqual(2, results.Count);
        }

        #endregion

        #region Error Handling Tests

        [Test]
        public void Execute_MissingOperation_ShouldReturnError()
        {
            // Arrange
            var payload = new Dictionary<string, object>
            {
                ["name"] = "TestObject"
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
        }

        [Test]
        public void Execute_UnsupportedOperation_ShouldReturnError()
        {
            // Arrange
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "unsupportedOperation"
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
            Assert.IsTrue(result["error"].ToString().Contains("not supported"));
        }

        #endregion
    }
}
