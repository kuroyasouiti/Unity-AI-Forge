using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityAIForge.GameKit;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace UnityAIForge.Tests.Editor
{
    [TestFixture]
    public class GameKitMachinationsTests
    {
        private GameObject testManagerGo;
        private GameKitMachinationsAsset testAsset;

        [SetUp]
        public void Setup()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            testAsset = ScriptableObject.CreateInstance<GameKitMachinationsAsset>();
        }

        [TearDown]
        public void Teardown()
        {
            if (testManagerGo != null)
            {
                UnityEngine.Object.DestroyImmediate(testManagerGo);
            }
            
            if (testAsset != null)
            {
                UnityEngine.Object.DestroyImmediate(testAsset);
            }
        }

        #region MachinationsAsset Tests

        [Test]
        public void MachinationsAsset_CreateAsset_IsNotNull()
        {
            // Assert
            Assert.IsNotNull(testAsset);
            Assert.IsNotNull(testAsset.Pools);
            Assert.IsNotNull(testAsset.Flows);
            Assert.IsNotNull(testAsset.Converters);
            Assert.IsNotNull(testAsset.Triggers);
        }

        [Test]
        public void MachinationsAsset_AddPool_CreatesPool()
        {
            // Act
            testAsset.AddPool("gold", 100f, 0f, 1000f);
            testAsset.AddPool("health", 100f, 0f, 100f);

            // Assert
            Assert.AreEqual(2, testAsset.Pools.Count);
            var goldPool = testAsset.GetPool("gold");
            Assert.IsNotNull(goldPool);
            Assert.AreEqual("gold", goldPool.resourceName);
            Assert.AreEqual(100f, goldPool.initialAmount);
            Assert.AreEqual(0f, goldPool.minValue);
            Assert.AreEqual(1000f, goldPool.maxValue);
        }

        [Test]
        public void MachinationsAsset_AddFlow_CreatesFlow()
        {
            // Arrange
            testAsset.AddPool("mana", 0f, 0f, 100f);

            // Act
            testAsset.AddFlow("manaRegen", "mana", 5f, true);

            // Assert
            Assert.AreEqual(1, testAsset.Flows.Count);
            var flows = testAsset.GetFlowsForResource("mana");
            Assert.AreEqual(1, flows.Count);
            Assert.AreEqual("manaRegen", flows[0].flowId);
            Assert.AreEqual(5f, flows[0].ratePerSecond);
            Assert.IsTrue(flows[0].isSource);
        }

        [Test]
        public void MachinationsAsset_AddConverter_CreatesConverter()
        {
            // Arrange
            testAsset.AddPool("wood", 100f, 0f, 1000f);
            testAsset.AddPool("planks", 0f, 0f, 1000f);

            // Act
            testAsset.AddConverter("woodToPlanks", "wood", "planks", 4f, 1f);

            // Assert
            Assert.AreEqual(1, testAsset.Converters.Count);
            var converter = testAsset.GetConverter("woodToPlanks");
            Assert.IsNotNull(converter);
            Assert.AreEqual("wood", converter.fromResource);
            Assert.AreEqual("planks", converter.toResource);
            Assert.AreEqual(4f, converter.conversionRate);
            Assert.AreEqual(1f, converter.inputCost);
        }

        [Test]
        public void MachinationsAsset_AddTrigger_CreatesTrigger()
        {
            // Arrange
            testAsset.AddPool("health", 100f, 0f, 100f);

            // Act
            testAsset.AddTrigger("lowHealth", "health", 
                GameKitMachinationsAsset.ThresholdType.Below, 30f);

            // Assert
            Assert.AreEqual(1, testAsset.Triggers.Count);
            var trigger = testAsset.GetTrigger("lowHealth");
            Assert.IsNotNull(trigger);
            Assert.AreEqual("health", trigger.resourceName);
            Assert.AreEqual(GameKitMachinationsAsset.ThresholdType.Below, trigger.thresholdType);
            Assert.AreEqual(30f, trigger.thresholdValue);
        }

        [Test]
        public void MachinationsAsset_Validate_EmptyAsset_ReturnsFalse()
        {
            // Act
            bool isValid = testAsset.Validate(out string errorMessage);

            // Assert
            Assert.IsFalse(isValid);
            Assert.IsTrue(errorMessage.Contains("No resource pools"));
        }

        [Test]
        public void MachinationsAsset_Validate_ValidAsset_ReturnsTrue()
        {
            // Arrange
            testAsset.AddPool("gold", 100f, 0f, 1000f);
            testAsset.AddFlow("goldGen", "gold", 5f, true);

            // Act
            bool isValid = testAsset.Validate(out string errorMessage);

            // Assert
            Assert.IsTrue(isValid);
            Assert.IsEmpty(errorMessage);
        }

        [Test]
        public void MachinationsAsset_Validate_DuplicatePoolNames_ReturnsFalse()
        {
            // Arrange
            testAsset.AddPool("gold", 100f, 0f, 1000f);
            // Directly add duplicate to bypass AddPool's duplicate check
            testAsset.Pools.Add(new GameKitMachinationsAsset.ResourcePool
            {
                resourceName = "gold",
                initialAmount = 200f,
                minValue = 0f,
                maxValue = 2000f
            });

            // Act
            bool isValid = testAsset.Validate(out string errorMessage);

            // Assert
            Assert.IsFalse(isValid);
            Assert.IsTrue(errorMessage.Contains("Duplicate pool name"));
        }

        [Test]
        public void MachinationsAsset_Validate_InvalidMinMax_ReturnsFalse()
        {
            // Arrange
            testAsset.AddPool("gold", 100f, 1000f, 0f); // min > max

            // Act
            bool isValid = testAsset.Validate(out string errorMessage);

            // Assert
            Assert.IsFalse(isValid);
            Assert.IsTrue(errorMessage.Contains("minValue"));
            Assert.IsTrue(errorMessage.Contains("maxValue"));
        }

        [Test]
        public void MachinationsAsset_Validate_FlowReferencesNonexistentPool_ReturnsFalse()
        {
            // Arrange
            testAsset.AddPool("gold", 100f, 0f, 1000f);
            testAsset.AddFlow("manaRegen", "mana", 5f, true); // mana pool doesn't exist

            // Act
            bool isValid = testAsset.Validate(out string errorMessage);

            // Assert
            Assert.IsFalse(isValid);
            Assert.IsTrue(errorMessage.Contains("non-existent pool: mana"));
        }

        [Test]
        public void MachinationsAsset_Validate_ConverterReferencesNonexistentPool_ReturnsFalse()
        {
            // Arrange
            testAsset.AddPool("wood", 100f, 0f, 1000f);
            testAsset.AddConverter("woodToPlanks", "wood", "planks", 4f, 1f); // planks doesn't exist

            // Act
            bool isValid = testAsset.Validate(out string errorMessage);

            // Assert
            Assert.IsFalse(isValid);
            Assert.IsTrue(errorMessage.Contains("non-existent"));
        }

        [Test]
        public void MachinationsAsset_Validate_InvalidConversionRate_ReturnsFalse()
        {
            // Arrange
            testAsset.AddPool("wood", 100f, 0f, 1000f);
            testAsset.AddPool("planks", 0f, 0f, 1000f);
            testAsset.AddConverter("woodToPlanks", "wood", "planks", 0f, 1f); // rate = 0

            // Act
            bool isValid = testAsset.Validate(out string errorMessage);

            // Assert
            Assert.IsFalse(isValid);
            Assert.IsTrue(errorMessage.Contains("invalid conversion rate"));
        }

        [Test]
        public void MachinationsAsset_RemovePool_RemovesPoolAndRelatedElements()
        {
            // Arrange
            testAsset.AddPool("gold", 100f, 0f, 1000f);
            testAsset.AddPool("gems", 50f, 0f, 500f);
            testAsset.AddFlow("goldGen", "gold", 5f, true);
            testAsset.AddConverter("goldToGems", "gold", "gems", 2f, 10f);
            testAsset.AddTrigger("lowGold", "gold", GameKitMachinationsAsset.ThresholdType.Below, 20f);

            // Act
            testAsset.RemovePool("gold");

            // Assert
            Assert.AreEqual(1, testAsset.Pools.Count);
            Assert.AreEqual(0, testAsset.Flows.Count); // goldGen removed
            Assert.AreEqual(0, testAsset.Converters.Count); // goldToGems removed
            Assert.AreEqual(0, testAsset.Triggers.Count); // lowGold removed
        }

        [Test]
        public void MachinationsAsset_GetSummary_ReturnsCorrectInfo()
        {
            // Arrange
            testAsset.AddPool("gold", 100f, 0f, 1000f);
            testAsset.AddPool("health", 100f, 0f, 100f);
            testAsset.AddFlow("goldGen", "gold", 5f, true);
            testAsset.AddConverter("goldToHealth", "gold", "health", 1f, 10f);
            testAsset.AddTrigger("lowHealth", "health", GameKitMachinationsAsset.ThresholdType.Below, 30f);

            // Act
            string summary = testAsset.GetSummary();

            // Assert
            Assert.IsTrue(summary.Contains("Pools: 2"));
            Assert.IsTrue(summary.Contains("Flows: 1"));
            Assert.IsTrue(summary.Contains("Converters: 1"));
            Assert.IsTrue(summary.Contains("Triggers: 1"));
        }

        #endregion

        #region ResourceManager + Machinations Integration Tests

        [Test]
        public void ResourceManager_ApplyMachinationsAsset_InitializesResources()
        {
            // Arrange
            testManagerGo = new GameObject("TestManager");
            var resourceManager = testManagerGo.AddComponent<GameKitResourceManager>();
            
            testAsset.AddPool("gold", 100f, 0f, 1000f);
            testAsset.AddPool("health", 80f, 0f, 100f);

            // Act
            resourceManager.ApplyMachinationsAsset(testAsset, true);

            // Assert
            Assert.AreEqual(100f, resourceManager.GetResource("gold"));
            Assert.AreEqual(80f, resourceManager.GetResource("health"));
        }

        [Test]
        public void ResourceManager_ApplyMachinationsAsset_InvalidAsset_LogsError()
        {
            // Arrange
            testManagerGo = new GameObject("TestManager");
            var resourceManager = testManagerGo.AddComponent<GameKitResourceManager>();
            
            // Empty asset (invalid)

            // Act
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Invalid machinations asset"));
            resourceManager.ApplyMachinationsAsset(testAsset, true);

            // Assert - LogAssert handles verification
        }

        [Test]
        public void ResourceManager_ProcessDiagramFlows_GeneratesResources()
        {
            // Arrange
            testManagerGo = new GameObject("TestManager");
            var resourceManager = testManagerGo.AddComponent<GameKitResourceManager>();
            
            testAsset.AddPool("mana", 0f, 0f, 100f);
            testAsset.AddFlow("manaRegen", "mana", 10f, true); // 10 per second
            resourceManager.ApplyMachinationsAsset(testAsset, true);

            // Act - Simulate 0.5 seconds
            resourceManager.ProcessDiagramFlows(0.5f);

            // Assert - Should have gained 5 mana (10 * 0.5)
            Assert.AreEqual(5f, resourceManager.GetResource("mana"), 0.01f);
        }

        [Test]
        public void ResourceManager_ProcessDiagramFlows_ConsumesResources()
        {
            // Arrange
            testManagerGo = new GameObject("TestManager");
            var resourceManager = testManagerGo.AddComponent<GameKitResourceManager>();
            
            testAsset.AddPool("health", 100f, 0f, 100f);
            testAsset.AddFlow("healthDrain", "health", 2f, false); // drain 2 per second
            resourceManager.ApplyMachinationsAsset(testAsset, true);

            // Act - Simulate 5 seconds
            resourceManager.ProcessDiagramFlows(5f);

            // Assert - Should have lost 10 health (2 * 5)
            Assert.AreEqual(90f, resourceManager.GetResource("health"), 0.01f);
        }

        [Test]
        public void ResourceManager_ProcessDiagramFlows_RespectsMaxConstraint()
        {
            // Arrange
            testManagerGo = new GameObject("TestManager");
            var resourceManager = testManagerGo.AddComponent<GameKitResourceManager>();
            
            testAsset.AddPool("mana", 95f, 0f, 100f); // Near max
            testAsset.AddFlow("manaRegen", "mana", 10f, true);
            resourceManager.ApplyMachinationsAsset(testAsset, true);

            // Act - Would generate 10 mana, but should cap at 100
            resourceManager.ProcessDiagramFlows(1f);

            // Assert - Should be clamped to max
            Assert.AreEqual(100f, resourceManager.GetResource("mana"), 0.01f);
        }

        [Test]
        public void ResourceManager_ProcessDiagramFlows_RespectsMinConstraint()
        {
            // Arrange
            testManagerGo = new GameObject("TestManager");
            var resourceManager = testManagerGo.AddComponent<GameKitResourceManager>();
            
            testAsset.AddPool("health", 5f, 0f, 100f); // Near min
            testAsset.AddFlow("healthDrain", "health", 10f, false); // Drain 10 per second
            resourceManager.ApplyMachinationsAsset(testAsset, true);

            // Act - Would drain 10 health (5 - 10 = -5), but should clamp to min (0)
            resourceManager.ProcessDiagramFlows(1f);

            // Assert - Should be clamped to min
            Assert.AreEqual(0f, resourceManager.GetResource("health"), 0.01f);
        }

        [Test]
        public void ResourceManager_ExecuteConverter_ConvertsResources()
        {
            // Arrange
            testManagerGo = new GameObject("TestManager");
            var resourceManager = testManagerGo.AddComponent<GameKitResourceManager>();
            
            testAsset.AddPool("wood", 100f, 0f, 1000f);
            testAsset.AddPool("planks", 0f, 0f, 1000f);
            testAsset.AddConverter("woodToPlanks", "wood", "planks", 4f, 1f);
            resourceManager.ApplyMachinationsAsset(testAsset, true);

            // Act - Convert 10 wood (should get 40 planks)
            bool success = resourceManager.ExecuteConverter("woodToPlanks", 10f);

            // Assert
            Assert.IsTrue(success);
            Assert.AreEqual(90f, resourceManager.GetResource("wood")); // 100 - 10
            Assert.AreEqual(40f, resourceManager.GetResource("planks")); // 10 * 4
        }

        [Test]
        public void ResourceManager_ExecuteConverter_InsufficientResources_ReturnsFalse()
        {
            // Arrange
            testManagerGo = new GameObject("TestManager");
            var resourceManager = testManagerGo.AddComponent<GameKitResourceManager>();
            
            testAsset.AddPool("wood", 5f, 0f, 1000f); // Only 5 wood
            testAsset.AddPool("planks", 0f, 0f, 1000f);
            testAsset.AddConverter("woodToPlanks", "wood", "planks", 4f, 1f);
            resourceManager.ApplyMachinationsAsset(testAsset, true);

            // Act - Try to convert 10 wood (insufficient)
            bool success = resourceManager.ExecuteConverter("woodToPlanks", 10f);

            // Assert
            Assert.IsFalse(success);
            Assert.AreEqual(5f, resourceManager.GetResource("wood")); // Unchanged
            Assert.AreEqual(0f, resourceManager.GetResource("planks")); // Unchanged
        }

        [Test]
        public void ResourceManager_ExecuteConverter_NonexistentConverter_ReturnsFalse()
        {
            // Arrange
            testManagerGo = new GameObject("TestManager");
            var resourceManager = testManagerGo.AddComponent<GameKitResourceManager>();
            
            testAsset.AddPool("wood", 100f, 0f, 1000f);
            resourceManager.ApplyMachinationsAsset(testAsset, true);

            // Act
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("not found"));
            bool success = resourceManager.ExecuteConverter("nonexistent", 10f);

            // Assert
            Assert.IsFalse(success);
        }

        [Test]
        public void ResourceManager_SetFlowEnabled_TogglesFlow()
        {
            // Arrange
            testManagerGo = new GameObject("TestManager");
            var resourceManager = testManagerGo.AddComponent<GameKitResourceManager>();
            
            testAsset.AddPool("mana", 0f, 0f, 100f);
            testAsset.AddFlow("manaRegen", "mana", 10f, true);
            resourceManager.ApplyMachinationsAsset(testAsset, true);

            // Act - Disable flow
            resourceManager.SetFlowEnabled("manaRegen", false);
            resourceManager.ProcessDiagramFlows(1f);

            // Assert - No change since flow is disabled
            Assert.AreEqual(0f, resourceManager.GetResource("mana"));

            // Act - Re-enable flow
            resourceManager.SetFlowEnabled("manaRegen", true);
            resourceManager.ProcessDiagramFlows(1f);

            // Assert - Now it should work
            Assert.AreEqual(10f, resourceManager.GetResource("mana"), 0.01f);
        }

        [Test]
        public void ResourceManager_IsFlowEnabled_ReturnsCorrectState()
        {
            // Arrange
            testManagerGo = new GameObject("TestManager");
            var resourceManager = testManagerGo.AddComponent<GameKitResourceManager>();
            
            testAsset.AddPool("mana", 0f, 0f, 100f);
            testAsset.AddFlow("manaRegen", "mana", 10f, true);
            resourceManager.ApplyMachinationsAsset(testAsset, true);

            // Act & Assert
            Assert.IsTrue(resourceManager.IsFlowEnabled("manaRegen"));
            
            resourceManager.SetFlowEnabled("manaRegen", false);
            Assert.IsFalse(resourceManager.IsFlowEnabled("manaRegen"));
        }

        [Test]
        public void ResourceManager_CheckDiagramTriggers_FiresTriggerOnThreshold()
        {
            // Arrange
            testManagerGo = new GameObject("TestManager");
            var resourceManager = testManagerGo.AddComponent<GameKitResourceManager>();
            
            testAsset.AddPool("health", 100f, 0f, 100f);
            testAsset.AddTrigger("lowHealth", "health", 
                GameKitMachinationsAsset.ThresholdType.Below, 30f);
            resourceManager.ApplyMachinationsAsset(testAsset, true);

            string triggeredName = null;
            resourceManager.OnTriggerFired.AddListener((name, resource, value) => {
                triggeredName = name;
            });

            // Act - Check triggers at initial state (should not trigger)
            resourceManager.CheckDiagramTriggers();
            Assert.IsNull(triggeredName);

            // Drop health below threshold
            resourceManager.SetResource("health", 25f);
            resourceManager.CheckDiagramTriggers();

            // Assert
            Assert.AreEqual("lowHealth", triggeredName);
        }

        [Test]
        public void ResourceManager_CheckDiagramTriggers_AboveThreshold()
        {
            // Arrange
            testManagerGo = new GameObject("TestManager");
            var resourceManager = testManagerGo.AddComponent<GameKitResourceManager>();
            
            testAsset.AddPool("power", 10f, 0f, 100f);
            testAsset.AddTrigger("powerUp", "power", 
                GameKitMachinationsAsset.ThresholdType.Above, 50f);
            resourceManager.ApplyMachinationsAsset(testAsset, true);

            string triggeredName = null;
            resourceManager.OnTriggerFired.AddListener((name, resource, value) => {
                triggeredName = name;
            });

            // Act - Increase power above threshold
            resourceManager.SetResource("power", 60f);
            resourceManager.CheckDiagramTriggers();

            // Assert
            Assert.AreEqual("powerUp", triggeredName);
        }

        [Test]
        public void ResourceManager_GetFlowStates_ReturnsAllFlowStates()
        {
            // Arrange
            testManagerGo = new GameObject("TestManager");
            var resourceManager = testManagerGo.AddComponent<GameKitResourceManager>();
            
            testAsset.AddPool("mana", 0f, 0f, 100f);
            testAsset.AddPool("health", 100f, 0f, 100f);
            testAsset.AddFlow("manaRegen", "mana", 10f, true);
            testAsset.AddFlow("healthDrain", "health", 2f, false);
            resourceManager.ApplyMachinationsAsset(testAsset, true);

            // Act
            var flowStates = resourceManager.GetFlowStates();

            // Assert
            Assert.AreEqual(2, flowStates.Count);
            Assert.IsTrue(flowStates.ContainsKey("manaRegen"));
            Assert.IsTrue(flowStates.ContainsKey("healthDrain"));
            Assert.IsTrue(flowStates["manaRegen"]);
            Assert.IsTrue(flowStates["healthDrain"]);
        }

        [Test]
        public void ResourceManager_OnResourceChanged_InvokesEvent()
        {
            // Arrange
            testManagerGo = new GameObject("TestManager");
            var resourceManager = testManagerGo.AddComponent<GameKitResourceManager>();
            
            testAsset.AddPool("gold", 100f, 0f, 1000f);
            resourceManager.ApplyMachinationsAsset(testAsset, true);

            string changedResource = null;
            float newAmount = 0f;
            
            resourceManager.OnResourceChanged.AddListener((resource, amount) => {
                changedResource = resource;
                newAmount = amount;
            });

            // Act
            resourceManager.SetResource("gold", 150f);

            // Assert
            Assert.AreEqual("gold", changedResource);
            Assert.AreEqual(150f, newAmount);
        }

        [Test]
        public void ResourceManager_ExportState_IncludesAllData()
        {
            // Arrange
            testManagerGo = new GameObject("TestManager");
            var resourceManager = testManagerGo.AddComponent<GameKitResourceManager>();
            
            testAsset.AddPool("gold", 100f, 0f, 1000f);
            testAsset.AddPool("health", 80f, 0f, 100f);
            testAsset.AddFlow("goldGen", "gold", 5f, true);
            resourceManager.ApplyMachinationsAsset(testAsset, true);

            // Modify some resources
            resourceManager.SetResource("gold", 250f);
            resourceManager.SetFlowEnabled("goldGen", false);

            // Act
            var state = resourceManager.ExportState("test_manager");

            // Assert
            Assert.AreEqual("test_manager", state.managerId);
            Assert.AreEqual(2, state.resources.Count);
            Assert.AreEqual(1, state.flowStates.Count);
            
            var goldSnapshot = state.resources.Find(r => r.name == "gold");
            Assert.IsNotNull(goldSnapshot);
            Assert.AreEqual(250f, goldSnapshot.amount);
            
            var flowSnapshot = state.flowStates.Find(f => f.flowId == "goldGen");
            Assert.IsNotNull(flowSnapshot);
            Assert.IsFalse(flowSnapshot.enabled);
        }

        [Test]
        public void ResourceManager_ImportState_RestoresAllData()
        {
            // Arrange
            testManagerGo = new GameObject("TestManager");
            var resourceManager = testManagerGo.AddComponent<GameKitResourceManager>();
            
            var state = new GameKitResourceManager.ResourceState
            {
                managerId = "test_manager",
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            
            state.resources.Add(new GameKitResourceManager.ResourceState.ResourceSnapshot
            {
                name = "gold",
                amount = 300f,
                minValue = 0f,
                maxValue = 1000f
            });
            
            state.flowStates.Add(new GameKitResourceManager.ResourceState.FlowStateSnapshot
            {
                flowId = "goldGen",
                enabled = false
            });

            // Act
            resourceManager.ImportState(state, true);

            // Assert
            Assert.AreEqual(300f, resourceManager.GetResource("gold"));
            Assert.IsFalse(resourceManager.IsFlowEnabled("goldGen"));
        }

        #endregion

        #region Complex Scenario Tests

        [Test]
        public void ComplexScenario_RPGHealthManaSystem()
        {
            // Arrange - Create a simple RPG health/mana system
            testManagerGo = new GameObject("TestManager");
            var resourceManager = testManagerGo.AddComponent<GameKitResourceManager>();
            
            testAsset.AddPool("health", 100f, 0f, 100f);
            testAsset.AddPool("mana", 50f, 0f, 100f);
            testAsset.AddPool("gold", 0f, 0f, 10000f);
            
            // Mana regenerates over time
            testAsset.AddFlow("manaRegen", "mana", 5f, true);
            
            // Can convert gold to health (health potion)
            testAsset.AddConverter("buyHealthPotion", "gold", "health", 20f, 10f);
            
            // Trigger when health is low
            testAsset.AddTrigger("lowHealth", "health", 
                GameKitMachinationsAsset.ThresholdType.Below, 30f);
            
            resourceManager.ApplyMachinationsAsset(testAsset, true);

            bool lowHealthTriggered = false;
            resourceManager.OnTriggerFired.AddListener((name, resource, value) => {
                if (name == "lowHealth") lowHealthTriggered = true;
            });

            // Act - Simulate gameplay
            
            // 1. Take damage
            resourceManager.ConsumeResource("health", 75f); // Health drops to 25
            
            // 2. Trigger check
            resourceManager.CheckDiagramTriggers();
            Assert.IsTrue(lowHealthTriggered, "Low health trigger should fire");
            
            // 3. Gain gold
            resourceManager.AddResource("gold", 50f);
            
            // 4. Buy health potion (10 gold → 20 health)
            bool purchased = resourceManager.ExecuteConverter("buyHealthPotion", 1f);
            Assert.IsTrue(purchased);
            Assert.AreEqual(45f, resourceManager.GetResource("health"), 0.01f);
            Assert.AreEqual(40f, resourceManager.GetResource("gold"), 0.01f);
            
            // 5. Mana regenerates over 2 seconds
            resourceManager.ProcessDiagramFlows(2f);
            Assert.AreEqual(60f, resourceManager.GetResource("mana"), 0.01f); // 50 + (5*2), capped at 100
        }

        [Test]
        public void ComplexScenario_CraftingSystem()
        {
            // Arrange - Create a crafting system
            testManagerGo = new GameObject("TestManager");
            var resourceManager = testManagerGo.AddComponent<GameKitResourceManager>();
            
            testAsset.AddPool("wood", 100f, 0f, 1000f);
            testAsset.AddPool("planks", 0f, 0f, 1000f);
            testAsset.AddPool("sticks", 0f, 0f, 1000f);
            testAsset.AddPool("tools", 0f, 0f, 100f);
            
            // Convert wood to planks
            testAsset.AddConverter("woodToPlanks", "wood", "planks", 4f, 1f);
            
            // Convert planks to sticks
            testAsset.AddConverter("planksToSticks", "planks", "sticks", 4f, 1f);
            
            // Convert sticks to tools (requires multiple sticks)
            testAsset.AddConverter("sticksToTools", "sticks", "tools", 1f, 2f);
            
            resourceManager.ApplyMachinationsAsset(testAsset, true);

            // Act - Craft a tool
            
            // 1. Convert 10 wood to 40 planks
            bool step1 = resourceManager.ExecuteConverter("woodToPlanks", 10f);
            Assert.IsTrue(step1);
            Assert.AreEqual(90f, resourceManager.GetResource("wood"));
            Assert.AreEqual(40f, resourceManager.GetResource("planks"));
            
            // 2. Convert 10 planks to 40 sticks
            bool step2 = resourceManager.ExecuteConverter("planksToSticks", 10f);
            Assert.IsTrue(step2);
            Assert.AreEqual(30f, resourceManager.GetResource("planks"));
            Assert.AreEqual(40f, resourceManager.GetResource("sticks"));
            
            // 3. Convert 4 sticks to 2 tools (4 sticks at 2 cost each → 2 tools)
            bool step3 = resourceManager.ExecuteConverter("sticksToTools", 2f);
            Assert.IsTrue(step3);
            Assert.AreEqual(36f, resourceManager.GetResource("sticks"));
            Assert.AreEqual(2f, resourceManager.GetResource("tools"));
        }

        #endregion
    }
}
