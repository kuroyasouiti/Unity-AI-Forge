using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using MCP.Editor.Handlers;

namespace MCP.Editor.Tests
{
    /// <summary>
    /// Animation3DBundleHandler unit tests.
    /// Tests AnimatorController creation, state management, and transitions.
    /// </summary>
    [TestFixture]
    public class Animation3DBundleHandlerTests
    {
        private Animation3DBundleHandler _handler;
        private List<string> _createdAssetPaths;
        private List<GameObject> _createdObjects;
        private const string TestAssetFolder = "Assets/UnityAIForge/Tests/Editor/TestAssets";

        [SetUp]
        public void SetUp()
        {
            _handler = new Animation3DBundleHandler();
            _createdAssetPaths = new List<string>();
            _createdObjects = new List<GameObject>();

            // Ensure test folder exists
            if (!AssetDatabase.IsValidFolder(TestAssetFolder))
            {
                string parentFolder = Path.GetDirectoryName(TestAssetFolder).Replace("\\", "/");
                string folderName = Path.GetFileName(TestAssetFolder);
                AssetDatabase.CreateFolder(parentFolder, folderName);
            }
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up GameObjects
            foreach (var obj in _createdObjects)
            {
                if (obj != null)
                {
                    Undo.ClearUndo(obj);
                    Object.DestroyImmediate(obj);
                }
            }
            _createdObjects.Clear();

            // Clean up created assets
            foreach (var path in _createdAssetPaths)
            {
                if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
                {
                    AssetDatabase.DeleteAsset(path);
                }
            }
            _createdAssetPaths.Clear();
            AssetDatabase.Refresh();
        }

        private GameObject CreateTestGameObject(string name)
        {
            var go = new GameObject(name);
            _createdObjects.Add(go);
            return go;
        }

        #region Property Tests

        [Test]
        public void Category_ShouldReturnAnimation3DBundle()
        {
            Assert.AreEqual("Animation3DBundle", _handler.Category);
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

            Assert.Contains("createController", operations);
            Assert.Contains("setParameter", operations);
            Assert.Contains("addState", operations);
            Assert.Contains("addTransition", operations);
            Assert.Contains("addBlendTree", operations);
            Assert.Contains("createAvatarMask", operations);
            Assert.Contains("setupAnimator", operations);
            Assert.Contains("inspect", operations);
        }

        #endregion

        #region CreateController Operation Tests

        [Test]
        public void Execute_CreateController_ShouldCreateAnimatorController()
        {
            string assetPath = $"{TestAssetFolder}/TestController.controller";
            _createdAssetPaths.Add(assetPath);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "createController",
                ["name"] = "TestController",
                ["savePath"] = assetPath
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(assetPath);
            Assert.IsNotNull(controller);
        }

        [Test]
        public void Execute_CreateController_WithParameters_ShouldAddParameters()
        {
            string assetPath = $"{TestAssetFolder}/TestControllerParams.controller";
            _createdAssetPaths.Add(assetPath);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "createController",
                ["name"] = "TestControllerParams",
                ["savePath"] = assetPath,
                ["parameters"] = new List<object>
                {
                    new Dictionary<string, object> { ["name"] = "Speed", ["type"] = "float" },
                    new Dictionary<string, object> { ["name"] = "IsGrounded", ["type"] = "bool" },
                    new Dictionary<string, object> { ["name"] = "Jump", ["type"] = "trigger" }
                }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(assetPath);
            Assert.AreEqual(3, controller.parameters.Length);
        }

        [Test]
        public void Execute_CreateController_WithStates_ShouldAddStates()
        {
            string assetPath = $"{TestAssetFolder}/TestControllerStates.controller";
            _createdAssetPaths.Add(assetPath);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "createController",
                ["name"] = "TestControllerStates",
                ["savePath"] = assetPath,
                ["states"] = new List<object>
                {
                    new Dictionary<string, object> { ["name"] = "Idle", ["isDefault"] = true },
                    new Dictionary<string, object> { ["name"] = "Walk" },
                    new Dictionary<string, object> { ["name"] = "Run" }
                }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(assetPath);
            var states = controller.layers[0].stateMachine.states;
            Assert.AreEqual(3, states.Length);
        }

        #endregion

        #region AddParameter Operation Tests

        [Test]
        public void Execute_AddParameter_ShouldAddParameterToController()
        {
            // Create controller first
            string assetPath = $"{TestAssetFolder}/TestAddParam.controller";
            _createdAssetPaths.Add(assetPath);

            var createPayload = new Dictionary<string, object>
            {
                ["operation"] = "createController",
                ["name"] = "TestAddParam",
                ["savePath"] = assetPath
            };
            _handler.Execute(createPayload);

            // Add parameter
            var addParamPayload = new Dictionary<string, object>
            {
                ["operation"] = "setParameter",
                ["controllerPath"] = assetPath,
                ["parameterName"] = "Health",
                ["parameterType"] = "float",
                ["defaultValue"] = 100f
            };

            var result = _handler.Execute(addParamPayload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(assetPath);
            var param = controller.parameters.FirstOrDefault(p => p.name == "Health");
            Assert.IsNotNull(param);
            Assert.AreEqual(AnimatorControllerParameterType.Float, param.type);
        }

        #endregion

        #region AddState Operation Tests

        [Test]
        public void Execute_AddState_ShouldAddStateToController()
        {
            // Create controller first
            string assetPath = $"{TestAssetFolder}/TestAddState.controller";
            _createdAssetPaths.Add(assetPath);

            var createPayload = new Dictionary<string, object>
            {
                ["operation"] = "createController",
                ["name"] = "TestAddState",
                ["savePath"] = assetPath
            };
            _handler.Execute(createPayload);

            // Add state
            var addStatePayload = new Dictionary<string, object>
            {
                ["operation"] = "addState",
                ["controllerPath"] = assetPath,
                ["stateName"] = "Attack",
                ["speed"] = 1.5f
            };

            var result = _handler.Execute(addStatePayload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(assetPath);
            var state = controller.layers[0].stateMachine.states
                .FirstOrDefault(s => s.state.name == "Attack");
            Assert.IsNotNull(state.state);
            Assert.AreEqual(1.5f, state.state.speed, 0.01f);
        }

        #endregion

        #region AddTransition Operation Tests

        [Test]
        public void Execute_AddTransition_ShouldAddTransitionBetweenStates()
        {
            // Create controller with states
            string assetPath = $"{TestAssetFolder}/TestAddTransition.controller";
            _createdAssetPaths.Add(assetPath);

            var createPayload = new Dictionary<string, object>
            {
                ["operation"] = "createController",
                ["name"] = "TestAddTransition",
                ["savePath"] = assetPath,
                ["states"] = new List<object>
                {
                    new Dictionary<string, object> { ["name"] = "Idle", ["isDefault"] = true },
                    new Dictionary<string, object> { ["name"] = "Walk" }
                },
                ["parameters"] = new List<object>
                {
                    new Dictionary<string, object> { ["name"] = "Speed", ["type"] = "float" }
                }
            };
            _handler.Execute(createPayload);

            // Add transition
            var addTransitionPayload = new Dictionary<string, object>
            {
                ["operation"] = "addTransition",
                ["controllerPath"] = assetPath,
                ["sourceState"] = "Idle",
                ["destinationState"] = "Walk",
                ["hasExitTime"] = false,
                ["conditions"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["parameter"] = "Speed",
                        ["mode"] = "Greater",
                        ["threshold"] = 0.1f
                    }
                }
            };

            var result = _handler.Execute(addTransitionPayload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
        }

        #endregion

        #region AssignController Operation Tests

        [Test]
        public void Execute_AssignController_ShouldAssignToAnimator()
        {
            // Create controller
            string assetPath = $"{TestAssetFolder}/TestAssign.controller";
            _createdAssetPaths.Add(assetPath);

            var createPayload = new Dictionary<string, object>
            {
                ["operation"] = "createController",
                ["name"] = "TestAssign",
                ["savePath"] = assetPath
            };
            _handler.Execute(createPayload);

            // Create GameObject with Animator
            var go = CreateTestGameObject("TestAnimator");
            var animator = go.AddComponent<Animator>();

            // Assign controller
            var assignPayload = new Dictionary<string, object>
            {
                ["operation"] = "setupAnimator",
                ["gameObjectPath"] = "TestAnimator",
                ["controllerPath"] = assetPath
            };

            var result = _handler.Execute(assignPayload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsNotNull(animator.runtimeAnimatorController);
        }

        #endregion

        #region InspectController Operation Tests

        [Test]
        public void Execute_InspectController_ShouldReturnControllerInfo()
        {
            // Create controller with parameters and states
            string assetPath = $"{TestAssetFolder}/TestInspect.controller";
            _createdAssetPaths.Add(assetPath);

            var createPayload = new Dictionary<string, object>
            {
                ["operation"] = "createController",
                ["name"] = "TestInspect",
                ["savePath"] = assetPath,
                ["parameters"] = new List<object>
                {
                    new Dictionary<string, object> { ["name"] = "Speed", ["type"] = "float" }
                },
                ["states"] = new List<object>
                {
                    new Dictionary<string, object> { ["name"] = "Idle" },
                    new Dictionary<string, object> { ["name"] = "Walk" }
                }
            };
            _handler.Execute(createPayload);

            // Inspect controller
            var inspectPayload = new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["controllerPath"] = assetPath
            };

            var result = _handler.Execute(inspectPayload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsTrue(result.ContainsKey("parameters"));
            Assert.IsTrue(result.ContainsKey("layers"));
        }

        #endregion

        #region SetupBlendTree Operation Tests

        [Test]
        public void Execute_SetupBlendTree_ShouldCreateBlendTree()
        {
            // Create controller
            string assetPath = $"{TestAssetFolder}/TestBlendTree.controller";
            _createdAssetPaths.Add(assetPath);

            var createPayload = new Dictionary<string, object>
            {
                ["operation"] = "createController",
                ["name"] = "TestBlendTree",
                ["savePath"] = assetPath,
                ["parameters"] = new List<object>
                {
                    new Dictionary<string, object> { ["name"] = "Speed", ["type"] = "float" }
                }
            };
            _handler.Execute(createPayload);

            // Setup blend tree
            var blendTreePayload = new Dictionary<string, object>
            {
                ["operation"] = "addBlendTree",
                ["controllerPath"] = assetPath,
                ["stateName"] = "Locomotion",
                ["blendParameter"] = "Speed",
                ["blendType"] = "Simple1D"
            };

            var result = _handler.Execute(blendTreePayload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
        }

        #endregion
    }
}
