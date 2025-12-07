using System;
using UnityEditor;
using UnityEngine;
using UnityEditor.TestTools.TestRunner.Api;

namespace UnityAIForge.Tests.Editor
{
    /// <summary>
    /// Test runner utility for executing SkillForUnity tests from the Unity Editor menu.
    /// </summary>
    public static class TestRunner
    {
        private static TestRunnerApi _api;
        private static TestRunnerCallbacks _callbacks;
        private static bool _callbacksRegistered;

        private static class TestGroups
        {
            public const string AssemblyName = "UnityAIForge.Tests.Editor";
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

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            AssemblyReloadEvents.beforeAssemblyReload += Cleanup;
        }

        private static TestRunnerApi GetApi()
        {
            if (_api == null)
            {
                _api = ScriptableObject.CreateInstance<TestRunnerApi>();
            }

            if (!_callbacksRegistered)
            {
                _callbacks = new TestRunnerCallbacks();
                _api.RegisterCallbacks(_callbacks);
                _callbacksRegistered = true;
            }

            return _api;
        }

        private static void Cleanup()
        {
            if (_api != null && _callbacksRegistered && _callbacks != null)
            {
                _api.UnregisterCallbacks(_callbacks);
            }
            _callbacksRegistered = false;
            _callbacks = null;

            if (_api != null)
            {
                ScriptableObject.DestroyImmediate(_api);
                _api = null;
            }
        }

        private static void ExecuteAllTests(string description)
        {
            try
            {
                var api = GetApi();
                var filter = new Filter
                {
                    testMode = TestMode.EditMode,
                    assemblyNames = new[] { TestGroups.AssemblyName }
                };
                Debug.Log($"[TestRunner] Executing {description}...");
                api.Execute(new ExecutionSettings(filter));
                Debug.Log("[TestRunner] Test execution started. Results in Test Runner window (Window > General > Test Runner)");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TestRunner] Failed to execute tests: {ex.Message}");
                Debug.LogException(ex);
            }
        }

        private static void ExecuteTests(string description, params string[] groupNames)
        {
            try
            {
                var api = GetApi();
                var filter = new Filter
                {
                    testMode = TestMode.EditMode,
                    // Some Unity versions treat class names as groupNames, others as testNames.
                    // Set both to ensure filtering works and avoid running the whole suite.
                    groupNames = groupNames,
                    testNames = groupNames,
                    assemblyNames = new[] { TestGroups.AssemblyName }
                };

                Debug.Log($"[TestRunner] Executing {description}...");
                Debug.Log($"[TestRunner] Target groups: {string.Join(", ", groupNames)}");

                api.Execute(new ExecutionSettings(filter));
                Debug.Log("[TestRunner] Test execution started. Results in Test Runner window (Window > General > Test Runner)");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TestRunner] Failed to execute tests: {ex.Message}");
                Debug.LogException(ex);
            }
        }

        [MenuItem("Tools/SkillForUnity/Run All Tests")]
        public static void RunAllTests()
        {
            ExecuteAllTests("all EditMode tests");
        }

        [MenuItem("Tools/SkillForUnity/Run Low-Level Tests")]
        public static void RunLowLevelTests()
        {
            ExecuteTests("Low-Level Tools tests", TestGroups.LowLevel);
        }

        [MenuItem("Tools/SkillForUnity/Run Mid-Level Tests")]
        public static void RunMidLevelTests()
        {
            ExecuteTests("Mid-Level Tools tests", TestGroups.MidLevel);
        }

        [MenuItem("Tools/SkillForUnity/Run GameKit Tests")]
        public static void RunGameKitTests()
        {
            ExecuteTests(
                "GameKit tests",
                TestGroups.GameKitActor,
                TestGroups.GameKitManager,
                TestGroups.GameKitInteraction,
                TestGroups.GameKitUICommand,
                TestGroups.GameKitSceneFlow
            );
        }

        [MenuItem("Tools/SkillForUnity/Run Component Tests")]
        public static void RunComponentTests()
        {
            ExecuteTests(
                "Component tests",
                TestGroups.TextMeshPro,
                TestGroups.CharacterController
            );
        }

        [MenuItem("Tools/SkillForUnity/Run TextMeshPro Tests")]
        public static void RunTextMeshProTests()
        {
            ExecuteTests("TextMeshPro Component tests", TestGroups.TextMeshPro);
        }

        [MenuItem("Tools/SkillForUnity/Run TextMeshPro Improved Tests")]
        public static void RunTextMeshProImprovedTests()
        {
            ExecuteTests("TextMeshPro Improved Component tests", TestGroups.TextMeshProImproved);
        }

        [MenuItem("Tools/SkillForUnity/Run All TextMeshPro Tests")]
        public static void RunAllTextMeshProTests()
        {
            ExecuteTests(
                "all TextMeshPro tests",
                TestGroups.TextMeshPro,
                TestGroups.TextMeshProImproved
            );
        }

        [MenuItem("Tools/SkillForUnity/Open Test Runner Window")]
        public static void OpenTestRunnerWindow()
        {
            EditorWindow.GetWindow<UnityEditor.TestTools.TestRunner.TestRunnerWindow>();
        }

        private class TestRunnerCallbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun)
            {
                var count = testsToRun?.TestCaseCount ?? 0;
                Debug.Log($"[TestRunner] Run started: {testsToRun.FullName} (tests discovered: {count})");
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                var pass = result?.PassCount ?? 0;
                var fail = result?.FailCount ?? 0;
                var skip = result?.SkipCount ?? 0;
                var total = pass + fail + skip;
                var color = fail > 0 ? "red" : "green";
                Debug.Log($"[TestRunner] <color={color}>Run finished: {pass}/{total} passed, {fail} failed, {skip} skipped</color>");

                if (total == 0)
                {
                    Debug.LogWarning("[TestRunner] No tests were discovered. Check assembly name/filter or refresh Test Runner (Window > General > Test Runner).");
                }

                if (fail > 0)
                {
                    Debug.LogWarning($"[TestRunner] {fail} test(s) failed. See Test Runner window for details.");
                }
            }

            public void TestStarted(ITestAdaptor test)
            {
                // optional per-test start logging (can be verbose)
            }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.TestStatus == TestStatus.Failed)
                {
                    Debug.LogError($"[TestRunner] ✗ FAILED: {result.FullName}");
                    if (!string.IsNullOrEmpty(result.Message))
                    {
                        Debug.LogError($"[TestRunner]   Message: {result.Message}");
                    }
                }
            }
        }
    }
}
