using UnityEditor;
using UnityEngine;
using UnityEditor.TestTools.TestRunner.Api;
using System;
using System.Linq;

namespace UnityAIForge.Tests.Editor
{
    /// <summary>
    /// Improved test runner utility for executing SkillForUnity tests from the Unity Editor menu.
    /// Features:
    /// - Reduced code duplication (DRY principle)
    /// - Proper Test Runner API usage (groupNames instead of testNames)
    /// - Resource cleanup
    /// - Test result callbacks
    /// - Error handling
    /// </summary>
    public static class TestRunnerImproved
    {
        // Centralized test class names
        private static class TestClasses
        {
            public const string LowLevel = "UnityAIForge.Tests.Editor.LowLevelToolsTests";
            public const string MidLevel = "UnityAIForge.Tests.Editor.MidLevelToolsTests";
            public const string GameKitActor = "UnityAIForge.Tests.Editor.GameKitActorTests";
            public const string GameKitManager = "UnityAIForge.Tests.Editor.GameKitManagerTests";
            public const string GameKitInteraction = "UnityAIForge.Tests.Editor.GameKitInteractionTests";
            public const string GameKitUICommand = "UnityAIForge.Tests.Editor.GameKitUICommandTests";
            public const string GameKitSceneFlow = "UnityAIForge.Tests.Editor.GameKitSceneFlowTests";
            public const string TextMeshPro = "UnityAIForge.Tests.Editor.TextMeshProComponentTests";
            public const string TextMeshProImproved = "UnityAIForge.Tests.Editor.TextMeshProComponentImprovedTests";
            public const string CharacterController = "UnityAIForge.Tests.Editor.CharacterControllerBundleTests";
        }

        private static TestRunnerApi testRunnerApi;
        private static bool isCallbackRegistered = false;

        /// <summary>
        /// Get or create the TestRunnerApi instance
        /// </summary>
        private static TestRunnerApi GetTestRunnerApi()
        {
            if (testRunnerApi == null)
            {
                testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
                
                // Register callbacks for test results
                if (!isCallbackRegistered)
                {
                    testRunnerApi.RegisterCallbacks(new TestRunnerCallbacks());
                    isCallbackRegistered = true;
                }
            }
            return testRunnerApi;
        }

        /// <summary>
        /// Execute tests by group names (test class names)
        /// </summary>
        private static void ExecuteTests(string description, params string[] groupNames)
        {
            try
            {
                var api = GetTestRunnerApi();
                
                var filter = new Filter
                {
                    testMode = TestMode.EditMode,
                    groupNames = groupNames // Use groupNames instead of testNames for class names
                };

                Debug.Log($"[TestRunner] Executing {description}...");
                Debug.Log($"[TestRunner] Target groups: {string.Join(", ", groupNames)}");
                
                api.Execute(new ExecutionSettings(filter));
                
                // Note: Results will appear in Test Runner window
                Debug.Log("[TestRunner] Test execution started. View results in Test Runner window (Window > General > Test Runner)");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TestRunner] Failed to execute tests: {ex.Message}");
                Debug.LogException(ex);
            }
        }

        /// <summary>
        /// Execute all tests without filter
        /// </summary>
        private static void ExecuteAllTests(string description)
        {
            try
            {
                var api = GetTestRunnerApi();
                
                var filter = new Filter
                {
                    testMode = TestMode.EditMode
                };

                Debug.Log($"[TestRunner] Executing {description}...");
                api.Execute(new ExecutionSettings(filter));
                Debug.Log("[TestRunner] Test execution started. View results in Test Runner window (Window > General > Test Runner)");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TestRunner] Failed to execute tests: {ex.Message}");
                Debug.LogException(ex);
            }
        }

        // Menu Items

        [MenuItem("Tools/SkillForUnity/Run All Tests")]
        public static void RunAllTests()
        {
            ExecuteAllTests("all EditMode tests");
        }

        [MenuItem("Tools/SkillForUnity/Run Low-Level Tests")]
        public static void RunLowLevelTests()
        {
            ExecuteTests("Low-Level Tools tests", TestClasses.LowLevel);
        }

        [MenuItem("Tools/SkillForUnity/Run Mid-Level Tests")]
        public static void RunMidLevelTests()
        {
            ExecuteTests("Mid-Level Tools tests", TestClasses.MidLevel);
        }

        [MenuItem("Tools/SkillForUnity/Run GameKit Tests")]
        public static void RunGameKitTests()
        {
            ExecuteTests(
                "GameKit tests",
                TestClasses.GameKitActor,
                TestClasses.GameKitManager,
                TestClasses.GameKitInteraction,
                TestClasses.GameKitUICommand,
                TestClasses.GameKitSceneFlow
            );
        }

        [MenuItem("Tools/SkillForUnity/Run Component Tests")]
        public static void RunComponentTests()
        {
            ExecuteTests(
                "Component tests",
                TestClasses.TextMeshPro,
                TestClasses.CharacterController
            );
        }

        [MenuItem("Tools/SkillForUnity/Run TextMeshPro Tests")]
        public static void RunTextMeshProTests()
        {
            ExecuteTests("TextMeshPro Component tests", TestClasses.TextMeshPro);
        }

        [MenuItem("Tools/SkillForUnity/Run TextMeshPro Improved Tests")]
        public static void RunTextMeshProImprovedTests()
        {
            ExecuteTests("TextMeshPro Improved Component tests", TestClasses.TextMeshProImproved);
        }

        [MenuItem("Tools/SkillForUnity/Run All TextMeshPro Tests")]
        public static void RunAllTextMeshProTests()
        {
            ExecuteTests(
                "all TextMeshPro tests",
                TestClasses.TextMeshPro,
                TestClasses.TextMeshProImproved
            );
        }

        [MenuItem("Tools/SkillForUnity/Open Test Runner Window")]
        public static void OpenTestRunnerWindow()
        {
            EditorWindow.GetWindow<UnityEditor.TestTools.TestRunner.TestRunnerWindow>();
        }

        /// <summary>
        /// Cleanup when Unity Editor closes or recompiles
        /// </summary>
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            // Cleanup on domain reload
            AssemblyReloadEvents.beforeAssemblyReload += Cleanup;
        }

        private static void Cleanup()
        {
            if (testRunnerApi != null)
            {
                if (isCallbackRegistered)
                {
                    testRunnerApi.UnregisterCallbacks(new TestRunnerCallbacks());
                    isCallbackRegistered = false;
                }
                ScriptableObject.DestroyImmediate(testRunnerApi);
                testRunnerApi = null;
            }
        }

        /// <summary>
        /// Callback handler for test execution results
        /// </summary>
        private class TestRunnerCallbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun)
            {
                Debug.Log($"[TestRunner] Run started: {testsToRun.FullName}");
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                var passCount = result.PassCount;
                var failCount = result.FailCount;
                var skipCount = result.SkipCount;
                var total = passCount + failCount + skipCount;
                
                var resultColor = failCount > 0 ? "red" : "green";
                Debug.Log($"[TestRunner] <color={resultColor}>Run finished: {passCount}/{total} passed, {failCount} failed, {skipCount} skipped</color>");
                
                if (failCount > 0)
                {
                    Debug.LogWarning($"[TestRunner] {failCount} test(s) failed. Check Test Runner window for details.");
                }
            }

            public void TestStarted(ITestAdaptor test)
            {
                // Optional: Log individual test starts (can be verbose)
                // Debug.Log($"[TestRunner] Test started: {test.FullName}");
            }

            public void TestFinished(ITestResultAdaptor result)
            {
                // Log failed tests immediately
                if (result.TestStatus == TestStatus.Failed)
                {
                    Debug.LogError($"[TestRunner] ✗ FAILED: {result.FullName}");
                    if (!string.IsNullOrEmpty(result.Message))
                    {
                        Debug.LogError($"[TestRunner]   Message: {result.Message}");
                    }
                }
                else if (result.TestStatus == TestStatus.Passed)
                {
                    // Optional: Log passed tests (can be verbose)
                    // Debug.Log($"[TestRunner] ✓ PASSED: {result.FullName}");
                }
            }
        }
    }
}
