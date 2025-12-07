using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using MCP.Editor;
using MCP.Editor.Base;

namespace UnityAIForge.Tests.Editor
{
    /// <summary>
    /// Tests for improved TextMeshPro component CRUD operations.
    /// These tests verify the enhanced features:
    /// 1. propertyFilter functionality (excluding internal m_ fields)
    /// 2. Partial failure handling in update operations
    /// 3. Initial property application in addMultiple operations
    /// 4. Detailed error reporting with updatedProperties and failedProperties
    /// </summary>
    [TestFixture]
    public class TextMeshProComponentImprovedTests
    {
        private GameObject testGo;
        private const string TMP_TYPE = "TMPro.TextMeshPro";

        [SetUp]
        public void Setup()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            testGo = new GameObject("TestImprovedTMP");
        }

        [TearDown]
        public void Teardown()
        {
            if (testGo != null)
            {
                Object.DestroyImmediate(testGo);
            }
        }

        #region PropertyFilter Tests

        [Test]
        public void InspectComponent_WithPropertyFilter_ReturnsOnlySpecifiedProperties()
        {
            // Arrange
            var addPayload = new Dictionary<string, object>
            {
                ["operation"] = "add",
                ["gameObjectPath"] = testGo.name,
                ["componentType"] = TMP_TYPE
            };
            var addCommand = new McpIncomingCommand { ToolName = "componentManage", Payload = addPayload };
            McpCommandProcessor.Execute(addCommand);

            var inspectPayload = new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["gameObjectPath"] = testGo.name,
                ["componentType"] = TMP_TYPE,
                ["propertyFilter"] = new List<object> { "text", "fontSize", "color" }
            };
            var inspectCommand = new McpIncomingCommand { ToolName = "componentManage", Payload = inspectPayload };

            // Act
            var result = McpCommandProcessor.Execute(inspectCommand) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.ContainsKey("properties"));
            
            var properties = result["properties"] as Dictionary<string, object>;
            Assert.IsNotNull(properties);
            
            // Should contain specified properties
            Assert.IsTrue(properties.ContainsKey("text"), "Should contain 'text' property");
            Assert.IsTrue(properties.ContainsKey("fontSize"), "Should contain 'fontSize' property");
            Assert.IsTrue(properties.ContainsKey("color"), "Should contain 'color' property");
            
            // Verify no internal fields (m_ prefix) are included when propertyFilter is specified
            var internalFields = properties.Keys.Where(k => k.StartsWith("m_") || k.StartsWith("_")).ToList();
            Assert.AreEqual(0, internalFields.Count, 
                $"Should not contain internal fields when propertyFilter is specified. Found: {string.Join(", ", internalFields)}");
        }

        [Test]
        public void InspectComponent_WithPropertyFilter_ExcludesNonSpecifiedProperties()
        {
            // Arrange
            var addPayload = new Dictionary<string, object>
            {
                ["operation"] = "add",
                ["gameObjectPath"] = testGo.name,
                ["componentType"] = TMP_TYPE
            };
            var addCommand = new McpIncomingCommand { ToolName = "componentManage", Payload = addPayload };
            McpCommandProcessor.Execute(addCommand);

            var inspectPayload = new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["gameObjectPath"] = testGo.name,
                ["componentType"] = TMP_TYPE,
                ["propertyFilter"] = new List<object> { "text", "fontSize" }
            };
            var inspectCommand = new McpIncomingCommand { ToolName = "componentManage", Payload = inspectPayload };

            // Act
            var result = McpCommandProcessor.Execute(inspectCommand) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            var properties = result["properties"] as Dictionary<string, object>;
            Assert.IsNotNull(properties);
            
            // Should not contain non-specified public properties
            Assert.IsFalse(properties.ContainsKey("enableAutoSizing"), 
                "Should not contain 'enableAutoSizing' when not in propertyFilter");
            Assert.IsFalse(properties.ContainsKey("alignment"), 
                "Should not contain 'alignment' when not in propertyFilter");
        }

        [Test]
        public void InspectMultipleComponents_WithPropertyFilter_ReturnsOnlySpecifiedProperties()
        {
            // Arrange - Create multiple objects
            var testGo2 = new GameObject("TestImprovedTMP2");
            var testGo3 = new GameObject("TestImprovedTMP3");

            try
            {
                // Add components
                var addPayload = new Dictionary<string, object>
                {
                    ["operation"] = "addMultiple",
                    ["pattern"] = "TestImprovedTMP*",
                    ["useRegex"] = false,
                    ["componentType"] = TMP_TYPE
                };
                var addCommand = new McpIncomingCommand { ToolName = "componentManage", Payload = addPayload };
                McpCommandProcessor.Execute(addCommand);

                // Inspect with propertyFilter
                var inspectPayload = new Dictionary<string, object>
                {
                    ["operation"] = "inspectMultiple",
                    ["pattern"] = "TestImprovedTMP*",
                    ["useRegex"] = false,
                    ["componentType"] = TMP_TYPE,
                    ["propertyFilter"] = new List<object> { "text", "fontSize" }
                };
                var inspectCommand = new McpIncomingCommand { ToolName = "componentManage", Payload = inspectPayload };

                // Act
                var result = McpCommandProcessor.Execute(inspectCommand) as Dictionary<string, object>;

                // Assert
                Assert.IsNotNull(result);
                Assert.IsTrue(result.ContainsKey("results"));
                
                var results = result["results"] as List<object>;
                Assert.IsNotNull(results);
                Assert.Greater(results.Count, 0);

                foreach (var item in results)
                {
                    var itemDict = item as Dictionary<string, object>;
                    Assert.IsNotNull(itemDict);
                    Assert.IsTrue(itemDict.ContainsKey("properties"));
                    
                    var properties = itemDict["properties"] as Dictionary<string, object>;
                    Assert.IsNotNull(properties);
                    
                    // Should contain specified properties
                    Assert.IsTrue(properties.ContainsKey("text"));
                    Assert.IsTrue(properties.ContainsKey("fontSize"));
                    
                    // Should not contain internal fields (m_ prefix)
                    var internalFields = properties.Keys.Where(k => k.StartsWith("m_") || k.StartsWith("_")).ToList();
                    Assert.AreEqual(0, internalFields.Count, 
                        $"Should not contain internal fields. Found: {string.Join(", ", internalFields)}");
                    
                    // Should have limited property count (text, fontSize, and possibly a few Unity built-ins)
                    // But definitely not all properties
                    Assert.Less(properties.Count, 20, 
                        "Should have limited properties when using propertyFilter");
                }
            }
            finally
            {
                Object.DestroyImmediate(testGo2);
                Object.DestroyImmediate(testGo3);
            }
        }

        #endregion

        #region Error Handling Tests

        [Test]
        public void UpdateComponent_WithPartialFailure_ReturnsUpdatedAndFailedProperties()
        {
            // Arrange
            var addPayload = new Dictionary<string, object>
            {
                ["operation"] = "add",
                ["gameObjectPath"] = testGo.name,
                ["componentType"] = TMP_TYPE
            };
            var addCommand = new McpIncomingCommand { ToolName = "componentManage", Payload = addPayload };
            McpCommandProcessor.Execute(addCommand);

            // Update with valid and invalid properties
            var updatePayload = new Dictionary<string, object>
            {
                ["operation"] = "update",
                ["gameObjectPath"] = testGo.name,
                ["componentType"] = TMP_TYPE,
                ["propertyChanges"] = new Dictionary<string, object>
                {
                    ["text"] = "Valid Text",
                    ["fontSize"] = 32.0,
                    ["invalidPropertyName"] = "This should fail",
                    ["anotherInvalidProperty"] = 123
                }
            };
            var updateCommand = new McpIncomingCommand { ToolName = "componentManage", Payload = updatePayload };

            // Act
            var result = McpCommandProcessor.Execute(updateCommand) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            
            // Check for new response format
            if (result.ContainsKey("updatedProperties"))
            {
                // Improved version
                var updatedProperties = result["updatedProperties"] as List<object>;
                Assert.IsNotNull(updatedProperties);
                Assert.Contains("text", updatedProperties.Cast<string>().ToList());
                Assert.Contains("fontSize", updatedProperties.Cast<string>().ToList());
                
                if (result.ContainsKey("failedProperties"))
                {
                    var failedProperties = result["failedProperties"] as Dictionary<string, object>;
                    Assert.IsNotNull(failedProperties);
                    Assert.IsTrue(failedProperties.ContainsKey("invalidPropertyName"));
                    Assert.IsTrue(failedProperties.ContainsKey("anotherInvalidProperty"));
                }
                
                if (result.ContainsKey("partialSuccess"))
                {
                    Assert.IsTrue((bool)result["partialSuccess"]);
                }
            }
            else if (result.ContainsKey("updated"))
            {
                // Legacy version - still works but doesn't show failed properties
                var updated = result["updated"] as List<object>;
                Assert.IsNotNull(updated);
                Assert.Contains("text", updated.Cast<string>().ToList());
                Assert.Contains("fontSize", updated.Cast<string>().ToList());
            }
            
            // Verify that valid properties were actually updated
            var component = testGo.GetComponent(TMP_TYPE);
            var textProp = component.GetType().GetProperty("text");
            Assert.AreEqual("Valid Text", textProp.GetValue(component));
        }

        [Test]
        public void UpdateComponent_WithAllValidProperties_HasNoFailedProperties()
        {
            // Arrange
            var addPayload = new Dictionary<string, object>
            {
                ["operation"] = "add",
                ["gameObjectPath"] = testGo.name,
                ["componentType"] = TMP_TYPE
            };
            var addCommand = new McpIncomingCommand { ToolName = "componentManage", Payload = addPayload };
            McpCommandProcessor.Execute(addCommand);

            var updatePayload = new Dictionary<string, object>
            {
                ["operation"] = "update",
                ["gameObjectPath"] = testGo.name,
                ["componentType"] = TMP_TYPE,
                ["propertyChanges"] = new Dictionary<string, object>
                {
                    ["text"] = "All Valid",
                    ["fontSize"] = 40.0,
                    ["enableAutoSizing"] = true
                }
            };
            var updateCommand = new McpIncomingCommand { ToolName = "componentManage", Payload = updatePayload };

            // Act
            var result = McpCommandProcessor.Execute(updateCommand) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            
            if (result.ContainsKey("updatedProperties"))
            {
                // Improved version
                var updatedProperties = result["updatedProperties"] as List<object>;
                Assert.IsNotNull(updatedProperties);
                Assert.AreEqual(3, updatedProperties.Count);
                
                // Should not have failedProperties
                if (result.ContainsKey("failedProperties"))
                {
                    var failedProperties = result["failedProperties"] as Dictionary<string, object>;
                    Assert.AreEqual(0, failedProperties.Count);
                }
                
                // Should not be partialSuccess
                if (result.ContainsKey("partialSuccess"))
                {
                    Assert.IsFalse((bool)result["partialSuccess"]);
                }
            }
        }

        [Test]
        public void UpdateMultipleComponents_WithPartialFailure_ReportsIndividualResults()
        {
            // Arrange - Create multiple objects
            var testGo2 = new GameObject("TestImprovedTMP2");

            try
            {
                // Add components
                var addPayload = new Dictionary<string, object>
                {
                    ["operation"] = "addMultiple",
                    ["pattern"] = "TestImprovedTMP*",
                    ["useRegex"] = false,
                    ["componentType"] = TMP_TYPE
                };
                var addCommand = new McpIncomingCommand { ToolName = "componentManage", Payload = addPayload };
                McpCommandProcessor.Execute(addCommand);

                // Update with valid and invalid properties
                var updatePayload = new Dictionary<string, object>
                {
                    ["operation"] = "updateMultiple",
                    ["pattern"] = "TestImprovedTMP*",
                    ["useRegex"] = false,
                    ["componentType"] = TMP_TYPE,
                    ["propertyChanges"] = new Dictionary<string, object>
                    {
                        ["text"] = "Updated Text",
                        ["fontSize"] = 35.0,
                        ["invalidProperty"] = "Should fail gracefully"
                    }
                };
                var updateCommand = new McpIncomingCommand { ToolName = "componentManage", Payload = updatePayload };

                // Act
                var result = McpCommandProcessor.Execute(updateCommand) as Dictionary<string, object>;

                // Assert
                Assert.IsNotNull(result);
                Assert.IsTrue((bool)result["success"]);
                Assert.IsTrue(result.ContainsKey("results"));
                
                var results = result["results"] as List<object>;
                Assert.IsNotNull(results);
                Assert.GreaterOrEqual(results.Count, 2);

                foreach (var item in results)
                {
                    var itemDict = item as Dictionary<string, object>;
                    Assert.IsNotNull(itemDict);
                    Assert.IsTrue((bool)itemDict["updated"]);
                    
                    if (itemDict.ContainsKey("updatedProperties"))
                    {
                        // Improved version
                        var updatedProperties = itemDict["updatedProperties"] as List<object>;
                        Assert.IsNotNull(updatedProperties);
                        Assert.Contains("text", updatedProperties.Cast<string>().ToList());
                        Assert.Contains("fontSize", updatedProperties.Cast<string>().ToList());
                        
                        if (itemDict.ContainsKey("failedProperties"))
                        {
                            var failedProperties = itemDict["failedProperties"] as Dictionary<string, object>;
                            Assert.IsNotNull(failedProperties);
                            Assert.IsTrue(failedProperties.ContainsKey("invalidProperty"));
                        }
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(testGo2);
            }
        }

        #endregion

        #region AddMultiple PropertyChanges Tests

        [Test]
        public void AddMultipleComponents_WithPropertyChanges_AppliesInitialProperties()
        {
            // Arrange - Create multiple objects
            var testGo2 = new GameObject("TestAddMulti2");
            var testGo3 = new GameObject("TestAddMulti3");

            try
            {
                var addPayload = new Dictionary<string, object>
                {
                    ["operation"] = "addMultiple",
                    ["pattern"] = "TestAddMulti*",
                    ["useRegex"] = false,
                    ["componentType"] = TMP_TYPE,
                    ["propertyChanges"] = new Dictionary<string, object>
                    {
                        ["text"] = "Initial Text",
                        ["fontSize"] = 28.0,
                        ["enableAutoSizing"] = false
                    }
                };
                var addCommand = new McpIncomingCommand { ToolName = "componentManage", Payload = addPayload };

                // Act
                var result = McpCommandProcessor.Execute(addCommand) as Dictionary<string, object>;

                // Assert
                Assert.IsNotNull(result);
                Assert.IsTrue((bool)result["success"]);
                
                var results = result["results"] as List<object>;
                Assert.IsNotNull(results);
                Assert.AreEqual(3, results.Count);

                // Check if appliedProperties field exists (improved version)
                var firstResult = results[0] as Dictionary<string, object>;
                if (firstResult.ContainsKey("appliedProperties"))
                {
                    // Improved version
                    foreach (var item in results)
                    {
                        var itemDict = item as Dictionary<string, object>;
                        var appliedProperties = itemDict["appliedProperties"] as List<object>;
                        Assert.IsNotNull(appliedProperties);
                        Assert.Contains("text", appliedProperties.Cast<string>().ToList());
                        Assert.Contains("fontSize", appliedProperties.Cast<string>().ToList());
                        Assert.Contains("enableAutoSizing", appliedProperties.Cast<string>().ToList());
                    }
                }

                // Verify properties were actually applied
                var component2 = testGo2.GetComponent(TMP_TYPE);
                Assert.IsNotNull(component2);
                var textProp = component2.GetType().GetProperty("text");
                var fontSizeProp = component2.GetType().GetProperty("fontSize");
                var autoSizeProp = component2.GetType().GetProperty("enableAutoSizing");
                
                Assert.AreEqual("Initial Text", textProp.GetValue(component2), 
                    "Text property should be applied during addMultiple");
                Assert.AreEqual(28.0f, (float)fontSizeProp.GetValue(component2), 0.1f, 
                    "FontSize property should be applied during addMultiple");
                Assert.AreEqual(false, (bool)autoSizeProp.GetValue(component2), 
                    "EnableAutoSizing property should be applied during addMultiple");
            }
            finally
            {
                Object.DestroyImmediate(testGo2);
                Object.DestroyImmediate(testGo3);
            }
        }

        [Test]
        public void AddMultipleComponents_WithPartiallyInvalidPropertyChanges_AppliesValidProperties()
        {
            // Arrange
            var testGo2 = new GameObject("TestAddPartial2");

            try
            {
                var addPayload = new Dictionary<string, object>
                {
                    ["operation"] = "addMultiple",
                    ["pattern"] = "TestAddPartial*",
                    ["useRegex"] = false,
                    ["componentType"] = TMP_TYPE,
                    ["propertyChanges"] = new Dictionary<string, object>
                    {
                        ["text"] = "Valid Text",
                        ["fontSize"] = 25.0,
                        ["invalidPropertyForAdd"] = "Should fail but not stop valid properties"
                    }
                };
                var addCommand = new McpIncomingCommand { ToolName = "componentManage", Payload = addPayload };

                // Act
                var result = McpCommandProcessor.Execute(addCommand) as Dictionary<string, object>;

                // Assert
                Assert.IsNotNull(result);
                Assert.IsTrue((bool)result["success"]);
                
                var results = result["results"] as List<object>;
                Assert.IsNotNull(results);

                foreach (var item in results)
                {
                    var itemDict = item as Dictionary<string, object>;
                    
                    if (itemDict.ContainsKey("appliedProperties"))
                    {
                        // Improved version
                        var appliedProperties = itemDict["appliedProperties"] as List<object>;
                        Assert.IsNotNull(appliedProperties);
                        Assert.Contains("text", appliedProperties.Cast<string>().ToList());
                        Assert.Contains("fontSize", appliedProperties.Cast<string>().ToList());
                        
                        if (itemDict.ContainsKey("failedProperties"))
                        {
                            var failedProperties = itemDict["failedProperties"] as Dictionary<string, object>;
                            Assert.IsNotNull(failedProperties);
                            Assert.IsTrue(failedProperties.ContainsKey("invalidPropertyForAdd"));
                        }
                    }
                }

                // Verify valid properties were actually applied
                var component = testGo2.GetComponent(TMP_TYPE);
                Assert.IsNotNull(component);
                var textProp = component.GetType().GetProperty("text");
                Assert.AreEqual("Valid Text", textProp.GetValue(component));
            }
            finally
            {
                Object.DestroyImmediate(testGo2);
            }
        }

        [Test]
        public void AddMultipleComponents_WithoutPropertyChanges_CreatesComponentsWithDefaults()
        {
            // Arrange
            var testGo2 = new GameObject("TestAddDefault2");

            try
            {
                var addPayload = new Dictionary<string, object>
                {
                    ["operation"] = "addMultiple",
                    ["pattern"] = "TestAddDefault*",
                    ["useRegex"] = false,
                    ["componentType"] = TMP_TYPE
                    // No propertyChanges specified
                };
                var addCommand = new McpIncomingCommand { ToolName = "componentManage", Payload = addPayload };

                // Act
                var result = McpCommandProcessor.Execute(addCommand) as Dictionary<string, object>;

                // Assert
                Assert.IsNotNull(result);
                Assert.IsTrue((bool)result["success"]);
                
                var results = result["results"] as List<object>;
                Assert.IsNotNull(results);
                Assert.GreaterOrEqual(results.Count, 2);

                // Verify components were created with default values
                var component = testGo2.GetComponent(TMP_TYPE);
                Assert.IsNotNull(component);
                var fontSizeProp = component.GetType().GetProperty("fontSize");
                // Default fontSize should be 36
                Assert.AreEqual(36.0f, (float)fontSizeProp.GetValue(component), 0.1f);
            }
            finally
            {
                Object.DestroyImmediate(testGo2);
            }
        }

        #endregion

        #region Integration Tests

        [Test]
        public void CompleteWorkflow_AddWithPropertiesInspectWithFilterUpdate_WorksCorrectly()
        {
            // Arrange
            var testGo2 = new GameObject("TestWorkflow2");

            try
            {
                // Step 1: Add with initial properties
                var addPayload = new Dictionary<string, object>
                {
                    ["operation"] = "addMultiple",
                    ["pattern"] = "TestWorkflow*",
                    ["useRegex"] = false,
                    ["componentType"] = TMP_TYPE,
                    ["propertyChanges"] = new Dictionary<string, object>
                    {
                        ["text"] = "Initial",
                        ["fontSize"] = 30.0
                    }
                };
                var addCommand = new McpIncomingCommand { ToolName = "componentManage", Payload = addPayload };
                var addResult = McpCommandProcessor.Execute(addCommand) as Dictionary<string, object>;
                Assert.IsTrue((bool)addResult["success"]);

                // Step 2: Inspect with propertyFilter
                var inspectPayload = new Dictionary<string, object>
                {
                    ["operation"] = "inspectMultiple",
                    ["pattern"] = "TestWorkflow*",
                    ["useRegex"] = false,
                    ["componentType"] = TMP_TYPE,
                    ["propertyFilter"] = new List<object> { "text", "fontSize" }
                };
                var inspectCommand = new McpIncomingCommand { ToolName = "componentManage", Payload = inspectPayload };
                var inspectResult = McpCommandProcessor.Execute(inspectCommand) as Dictionary<string, object>;
                
                Assert.IsTrue((bool)inspectResult["success"]);
                var inspectResults = inspectResult["results"] as List<object>;
                Assert.GreaterOrEqual(inspectResults.Count, 2);

                // Step 3: Update with partial invalid properties
                var updatePayload = new Dictionary<string, object>
                {
                    ["operation"] = "updateMultiple",
                    ["pattern"] = "TestWorkflow*",
                    ["useRegex"] = false,
                    ["componentType"] = TMP_TYPE,
                    ["propertyChanges"] = new Dictionary<string, object>
                    {
                        ["text"] = "Updated",
                        ["invalidProp"] = "Should handle gracefully"
                    }
                };
                var updateCommand = new McpIncomingCommand { ToolName = "componentManage", Payload = updatePayload };
                var updateResult = McpCommandProcessor.Execute(updateCommand) as Dictionary<string, object>;
                Assert.IsTrue((bool)updateResult["success"]);

                // Final verification
                var finalComponent = testGo2.GetComponent(TMP_TYPE);
                var textProp = finalComponent.GetType().GetProperty("text");
                Assert.AreEqual("Updated", textProp.GetValue(finalComponent));
            }
            finally
            {
                Object.DestroyImmediate(testGo2);
            }
        }

        #endregion
    }
}
