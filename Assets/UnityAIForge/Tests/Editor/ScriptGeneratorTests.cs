using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MCP.Editor.CodeGen;

namespace MCP.Editor.Tests
{
    /// <summary>
    /// Unit tests for ScriptGenerator.
    /// Tests template loading, script generation, tracker registration, and cleanup.
    /// </summary>
    [TestFixture]
    public class ScriptGeneratorTests
    {
        private const string TestOutputDir = "Assets/TestTemp/Generated";
        private List<GameObject> _createdObjects;
        private List<string> _generatedFiles;

        [SetUp]
        public void SetUp()
        {
            _createdObjects = new List<GameObject>();
            _generatedFiles = new List<string>();

            // Ensure test output directory exists
            if (!Directory.Exists(TestOutputDir))
                Directory.CreateDirectory(TestOutputDir);
        }

        [TearDown]
        public void TearDown()
        {
            // Cleanup generated files
            foreach (var file in _generatedFiles)
            {
                if (File.Exists(file))
                    AssetDatabase.DeleteAsset(file);
            }

            // Cleanup test directory
            if (Directory.Exists(TestOutputDir))
                AssetDatabase.DeleteAsset("Assets/TestTemp");

            // Cleanup GameObjects
            foreach (var obj in _createdObjects)
            {
                if (obj != null)
                {
                    Undo.ClearUndo(obj);
                    Object.DestroyImmediate(obj);
                }
            }
            _createdObjects.Clear();

            // Reset tracker state
            GeneratedScriptTracker.ResetInstance();
        }

        private GameObject CreateTestGameObject(string name)
        {
            var go = new GameObject(name);
            _createdObjects.Add(go);
            return go;
        }

        #region ToPascalCase

        [Test]
        public void ToPascalCase_SnakeCase_ShouldConvert()
        {
            var result = ScriptGenerator.ToPascalCase("player_hp", "Health");

            Assert.AreEqual("PlayerHpHealth", result);
        }

        [Test]
        public void ToPascalCase_KebabCase_ShouldConvert()
        {
            var result = ScriptGenerator.ToPascalCase("enemy-ai", "Behavior");

            Assert.AreEqual("EnemyAiBehavior", result);
        }

        [Test]
        public void ToPascalCase_SingleWord_ShouldCapitalize()
        {
            var result = ScriptGenerator.ToPascalCase("player", "");

            Assert.AreEqual("Player", result);
        }

        [Test]
        public void ToPascalCase_EmptyId_ShouldReturnSuffixOnly()
        {
            var result = ScriptGenerator.ToPascalCase("", "Health");

            Assert.AreEqual("Health", result);
        }

        [Test]
        public void ToPascalCase_NullId_ShouldReturnSuffixOnly()
        {
            var result = ScriptGenerator.ToPascalCase(null, "Health");

            Assert.AreEqual("Health", result);
        }

        #endregion

        #region Template Loading

        [Test]
        public void LoadTemplate_ExistingTemplate_ShouldReturnContent()
        {
            var content = ScriptGenerator.LoadTemplate("Health");

            Assert.IsNotNull(content);
            StringAssert.Contains("{{CLASS_NAME}}", content);
            StringAssert.Contains("MonoBehaviour", content);
        }

        [Test]
        public void LoadTemplate_NonExistentTemplate_ShouldReturnNull()
        {
            var content = ScriptGenerator.LoadTemplate("NonExistentTemplate");

            Assert.IsNull(content);
        }

        #endregion

        #region Generate

        [Test]
        public void Generate_ValidInputs_ShouldCreateFile()
        {
            var go = CreateTestGameObject("TestPlayer");
            var vars = new Dictionary<string, object>
            {
                { "HEALTH_ID", "test_hp" },
                { "MAX_HEALTH", 100f },
                { "CURRENT_HEALTH", 100f },
                { "INVINCIBILITY_DURATION", 0.5f },
                { "DEATH_BEHAVIOR", "Destroy" },
                { "RESPAWN_DELAY", 1f }
            };

            var result = ScriptGenerator.Generate(
                go, "Health", "TestPlayerHealth", "test_hp", vars, TestOutputDir);
            if (result.Success)
                _generatedFiles.Add(result.ScriptPath);

            Assert.IsTrue(result.Success, result.ErrorMessage);
            Assert.IsNotNull(result.ScriptPath);
            Assert.AreEqual("TestPlayerHealth", result.ClassName);
            Assert.IsTrue(File.Exists(result.ScriptPath));
        }

        [Test]
        public void Generate_ShouldSubstituteVariablesInFile()
        {
            var go = CreateTestGameObject("TestEntity");
            var vars = new Dictionary<string, object>
            {
                { "HEALTH_ID", "entity_hp" },
                { "MAX_HEALTH", 200f },
                { "CURRENT_HEALTH", 200f },
                { "INVINCIBILITY_DURATION", 0f },
                { "DEATH_BEHAVIOR", "Event" },
                { "RESPAWN_DELAY", 0f }
            };

            var result = ScriptGenerator.Generate(
                go, "Health", "TestEntityHealth", "entity_hp", vars, TestOutputDir);
            if (result.Success)
                _generatedFiles.Add(result.ScriptPath);

            Assert.IsTrue(result.Success, result.ErrorMessage);

            var content = File.ReadAllText(result.ScriptPath);
            StringAssert.Contains("class TestEntityHealth", content);
            StringAssert.Contains("maxHealth = 200f", content);
            StringAssert.Contains("DeathBehavior.Event", content);
            // Ensure no UnityAIForge namespace references
            StringAssert.DoesNotContain("UnityAIForge", content);
        }

        [Test]
        public void Generate_ShouldRegisterInTracker()
        {
            var go = CreateTestGameObject("TrackerTest");
            var vars = new Dictionary<string, object>
            {
                { "HEALTH_ID", "tracker_hp" },
                { "MAX_HEALTH", 50f },
                { "CURRENT_HEALTH", 50f },
                { "INVINCIBILITY_DURATION", 0f },
                { "DEATH_BEHAVIOR", "Destroy" },
                { "RESPAWN_DELAY", 1f }
            };

            var result = ScriptGenerator.Generate(
                go, "Health", "TrackerTestHealth", "tracker_hp", vars, TestOutputDir);
            if (result.Success)
                _generatedFiles.Add(result.ScriptPath);

            Assert.IsTrue(result.Success, result.ErrorMessage);

            var tracker = GeneratedScriptTracker.Instance;
            var entry = tracker.FindByComponentId("tracker_hp");

            Assert.IsNotNull(entry);
            Assert.AreEqual("TrackerTestHealth", entry.className);
            Assert.AreEqual("Health", entry.templateName);
            Assert.AreEqual("TrackerTest", entry.gameObjectPath);
        }

        [Test]
        public void Generate_NullTarget_ShouldSucceed()
        {
            var vars = new Dictionary<string, object>
            {
                { "HEALTH_ID", "orphan_hp" },
                { "MAX_HEALTH", 100f },
                { "CURRENT_HEALTH", 100f },
                { "INVINCIBILITY_DURATION", 0f },
                { "DEATH_BEHAVIOR", "Destroy" },
                { "RESPAWN_DELAY", 1f }
            };

            var result = ScriptGenerator.Generate(
                null, "Health", "OrphanHealth", "orphan_hp", vars, TestOutputDir);
            if (result.Success)
                _generatedFiles.Add(result.ScriptPath);

            Assert.IsTrue(result.Success, result.ErrorMessage);
        }

        [Test]
        public void Generate_InvalidClassName_ShouldFail()
        {
            var go = CreateTestGameObject("Test");

            var result = ScriptGenerator.Generate(
                go, "Health", "123Invalid", "test_id", new Dictionary<string, object>(), TestOutputDir);

            Assert.IsFalse(result.Success);
            StringAssert.Contains("valid C# class name", result.ErrorMessage);
        }

        [Test]
        public void Generate_EmptyTemplateName_ShouldFail()
        {
            var go = CreateTestGameObject("Test");

            var result = ScriptGenerator.Generate(
                go, "", "TestClass", "test_id", new Dictionary<string, object>(), TestOutputDir);

            Assert.IsFalse(result.Success);
            StringAssert.Contains("templateName", result.ErrorMessage);
        }

        [Test]
        public void Generate_EmptyClassName_ShouldFail()
        {
            var go = CreateTestGameObject("Test");

            var result = ScriptGenerator.Generate(
                go, "Health", "", "test_id", new Dictionary<string, object>(), TestOutputDir);

            Assert.IsFalse(result.Success);
            StringAssert.Contains("className", result.ErrorMessage);
        }

        [Test]
        public void Generate_EmptyComponentId_ShouldFail()
        {
            var go = CreateTestGameObject("Test");

            var result = ScriptGenerator.Generate(
                go, "Health", "TestClass", "", new Dictionary<string, object>(), TestOutputDir);

            Assert.IsFalse(result.Success);
            StringAssert.Contains("componentId", result.ErrorMessage);
        }

        [Test]
        public void Generate_NonExistentTemplate_ShouldFail()
        {
            var go = CreateTestGameObject("Test");

            var result = ScriptGenerator.Generate(
                go, "NoSuchTemplate", "TestClass", "test_id", new Dictionary<string, object>(), TestOutputDir);

            Assert.IsFalse(result.Success);
            StringAssert.Contains("not found", result.ErrorMessage);
        }

        [Test]
        public void Generate_DuplicateComponentId_ShouldOverwrite()
        {
            var go = CreateTestGameObject("TestDup");
            var vars = new Dictionary<string, object>
            {
                { "HEALTH_ID", "dup_hp" },
                { "MAX_HEALTH", 100f },
                { "CURRENT_HEALTH", 100f },
                { "INVINCIBILITY_DURATION", 0f },
                { "DEATH_BEHAVIOR", "Destroy" },
                { "RESPAWN_DELAY", 1f }
            };

            var result1 = ScriptGenerator.Generate(
                go, "Health", "DupHealth", "dup_hp", vars, TestOutputDir);
            if (result1.Success)
                _generatedFiles.Add(result1.ScriptPath);

            // Generate again with same componentId but different class name
            var result2 = ScriptGenerator.Generate(
                go, "Health", "DupHealth", "dup_hp", vars, TestOutputDir);
            if (result2.Success && result2.ScriptPath != result1.ScriptPath)
                _generatedFiles.Add(result2.ScriptPath);

            Assert.IsTrue(result2.Success, result2.ErrorMessage);

            // Tracker should have only one entry
            var tracker = GeneratedScriptTracker.Instance;
            var entry = tracker.FindByComponentId("dup_hp");
            Assert.IsNotNull(entry);
            Assert.AreEqual("DupHealth", entry.className);
        }

        #endregion

        #region Delete

        [Test]
        public void Delete_ExistingScript_ShouldRemoveFileAndTrackerEntry()
        {
            var go = CreateTestGameObject("DeleteTest");
            var vars = new Dictionary<string, object>
            {
                { "HEALTH_ID", "del_hp" },
                { "MAX_HEALTH", 100f },
                { "CURRENT_HEALTH", 100f },
                { "INVINCIBILITY_DURATION", 0f },
                { "DEATH_BEHAVIOR", "Destroy" },
                { "RESPAWN_DELAY", 1f }
            };

            var result = ScriptGenerator.Generate(
                go, "Health", "DeleteTestHealth", "del_hp", vars, TestOutputDir);
            Assert.IsTrue(result.Success, result.ErrorMessage);
            Assert.IsTrue(File.Exists(result.ScriptPath));

            var deleted = ScriptGenerator.Delete("del_hp");

            Assert.IsTrue(deleted);
            Assert.IsFalse(File.Exists(result.ScriptPath));

            var tracker = GeneratedScriptTracker.Instance;
            Assert.IsNull(tracker.FindByComponentId("del_hp"));
        }

        [Test]
        public void Delete_NonExistentComponentId_ShouldReturnFalse()
        {
            var deleted = ScriptGenerator.Delete("nonexistent_id");

            Assert.IsFalse(deleted);
        }

        #endregion

        #region Regenerate

        [Test]
        public void Regenerate_ExistingScript_ShouldUpdateFile()
        {
            var go = CreateTestGameObject("RegenTest");
            var vars = new Dictionary<string, object>
            {
                { "HEALTH_ID", "regen_hp" },
                { "MAX_HEALTH", 100f },
                { "CURRENT_HEALTH", 100f },
                { "INVINCIBILITY_DURATION", 0f },
                { "DEATH_BEHAVIOR", "Destroy" },
                { "RESPAWN_DELAY", 1f }
            };

            var result = ScriptGenerator.Generate(
                go, "Health", "RegenTestHealth", "regen_hp", vars, TestOutputDir);
            if (result.Success)
                _generatedFiles.Add(result.ScriptPath);
            Assert.IsTrue(result.Success, result.ErrorMessage);

            // Regenerate with updated maxHealth
            var newVars = new Dictionary<string, object>
            {
                { "MAX_HEALTH", 200f }
            };

            var regenResult = ScriptGenerator.Regenerate("regen_hp", newVars);

            Assert.IsTrue(regenResult.Success, regenResult.ErrorMessage);
            Assert.AreEqual(result.ScriptPath, regenResult.ScriptPath);

            var content = File.ReadAllText(regenResult.ScriptPath);
            StringAssert.Contains("maxHealth = 200f", content);
        }

        [Test]
        public void Regenerate_NonExistentComponentId_ShouldFail()
        {
            var result = ScriptGenerator.Regenerate("nonexistent_id", new Dictionary<string, object>());

            Assert.IsFalse(result.Success);
            StringAssert.Contains("not found", result.ErrorMessage);
        }

        #endregion

        #region ResolveGeneratedType

        [Test]
        public void ResolveGeneratedType_BuiltInType_ShouldResolve()
        {
            // We can't test generated types without compilation, but we can verify
            // the method works with a known type
            var type = ScriptGenerator.ResolveGeneratedType("UnityEngine.MonoBehaviour");

            // MonoBehaviour is not in the global namespace, so this should return null
            Assert.IsNull(type);
        }

        [Test]
        public void ResolveGeneratedType_NullClassName_ShouldReturnNull()
        {
            var type = ScriptGenerator.ResolveGeneratedType(null);

            Assert.IsNull(type);
        }

        [Test]
        public void ResolveGeneratedType_EmptyClassName_ShouldReturnNull()
        {
            var type = ScriptGenerator.ResolveGeneratedType("");

            Assert.IsNull(type);
        }

        #endregion

        #region Variable Serialization

        [Test]
        public void SerializeDeserialize_RoundTrip_ShouldPreserveValues()
        {
            var vars = new Dictionary<string, object>
            {
                { "STRING_VAR", "hello" },
                { "INT_VAR", 42 },
                { "FLOAT_VAR", 3.14f },
                { "BOOL_VAR", true }
            };

            var json = ScriptGenerator.SerializeVariables(vars);
            var deserialized = ScriptGenerator.DeserializeVariables(json);

            Assert.AreEqual("hello", deserialized["STRING_VAR"]);
            Assert.AreEqual(42, deserialized["INT_VAR"]);
            Assert.AreEqual(3.14f, (float)deserialized["FLOAT_VAR"], 0.001f);
            Assert.AreEqual(true, deserialized["BOOL_VAR"]);
        }

        [Test]
        public void DeserializeVariables_EmptyJson_ShouldReturnEmptyDict()
        {
            var result = ScriptGenerator.DeserializeVariables("{}");

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void DeserializeVariables_NullJson_ShouldReturnEmptyDict()
        {
            var result = ScriptGenerator.DeserializeVariables(null);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        #endregion

        #region Generated File Content

        [Test]
        public void Generate_HealthTemplate_ShouldNotContainUnityAIForgeNamespace()
        {
            var go = CreateTestGameObject("NsTest");
            var vars = new Dictionary<string, object>
            {
                { "HEALTH_ID", "ns_hp" },
                { "MAX_HEALTH", 100f },
                { "CURRENT_HEALTH", 100f },
                { "INVINCIBILITY_DURATION", 1f },
                { "DEATH_BEHAVIOR", "Respawn" },
                { "RESPAWN_DELAY", 2f }
            };

            var result = ScriptGenerator.Generate(
                go, "Health", "NsTestHealth", "ns_hp", vars, TestOutputDir);
            if (result.Success)
                _generatedFiles.Add(result.ScriptPath);

            Assert.IsTrue(result.Success, result.ErrorMessage);

            var content = File.ReadAllText(result.ScriptPath);

            // Core assertion: no dependency on UnityAIForge
            StringAssert.DoesNotContain("UnityAIForge", content);
            StringAssert.DoesNotContain("MCP.Editor", content);

            // Should contain expected class name
            StringAssert.Contains("class NsTestHealth", content);

            // Should contain rendered values
            StringAssert.Contains("maxHealth = 100f", content);
            StringAssert.Contains("DeathBehavior.Respawn", content);
            StringAssert.Contains("respawnDelay = 2f", content);
        }

        #endregion
    }
}
