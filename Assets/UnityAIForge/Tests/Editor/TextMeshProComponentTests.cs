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
    /// Tests for TextMeshPro component CRUD operations using UnityAI-Forge's component tools.
    /// These tests verify that the MCP componentManage tool can properly handle TextMeshPro components.
    /// </summary>
    [TestFixture]
    public class TextMeshProComponentTests
    {
        private GameObject testGo;
        private const string TMP_TYPE = "TMPro.TextMeshPro";
        private const string TMP_UGUI_TYPE = "TMPro.TextMeshProUGUI";

        [SetUp]
        public void Setup()
        {
            // Create a new scene for testing
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            testGo = new GameObject("TestTextMeshProObject");
        }

        [TearDown]
        public void Teardown()
        {
            if (testGo != null)
            {
                Object.DestroyImmediate(testGo);
            }
        }

        #region TextMeshPro 3D Tests

        [Test]
        public void AddComponent_TextMeshPro_CreatesComponent()
        {
            // Arrange
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "add",
                ["gameObjectPath"] = testGo.name,
                ["componentType"] = TMP_TYPE
            };
            var command = new McpIncomingCommand { ToolName = "componentManage", Payload = payload };

            // Act
            var result = McpCommandProcessor.Execute(command) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.ContainsKey("type"));
            Assert.AreEqual(TMP_TYPE, result["type"]);
            
            var component = testGo.GetComponent(TMP_TYPE);
            Assert.IsNotNull(component, "TextMeshPro component should be added to GameObject");
        }

        [Test]
        public void UpdateComponent_TextMeshPro_UpdatesText()
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
                    ["text"] = "Hello TextMeshPro!",
                    ["fontSize"] = 36.0,
                    ["enableAutoSizing"] = false
                }
            };
            var updateCommand = new McpIncomingCommand { ToolName = "componentManage", Payload = updatePayload };

            // Act
            var result = McpCommandProcessor.Execute(updateCommand) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            
            var component = testGo.GetComponent(TMP_TYPE);
            Assert.IsNotNull(component);
            
            // Verify properties using reflection
            var textProp = component.GetType().GetProperty("text");
            Assert.AreEqual("Hello TextMeshPro!", textProp.GetValue(component));
            
            var fontSizeProp = component.GetType().GetProperty("fontSize");
            Assert.AreEqual(36.0f, (float)fontSizeProp.GetValue(component), 0.01f);
        }

        [Test]
        public void InspectComponent_TextMeshPro_ReturnsComponentInfo()
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

            // Set some properties
            var component = testGo.GetComponent(TMP_TYPE);
            var textProp = component.GetType().GetProperty("text");
            textProp.SetValue(component, "Test Text");

            var inspectPayload = new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["gameObjectPath"] = testGo.name,
                ["componentType"] = TMP_TYPE
            };
            var inspectCommand = new McpIncomingCommand { ToolName = "componentManage", Payload = inspectPayload };

            // Act
            var result = McpCommandProcessor.Execute(inspectCommand) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.ContainsKey("type"));
            Assert.AreEqual(TMP_TYPE, result["type"]);
            Assert.IsTrue(result.ContainsKey("properties"));
            
            var properties = result["properties"] as Dictionary<string, object>;
            Assert.IsNotNull(properties);
            Assert.IsTrue(properties.ContainsKey("text"));
            Assert.AreEqual("Test Text", properties["text"]);
        }

        [Test]
        public void RemoveComponent_TextMeshPro_RemovesComponent()
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

            var removePayload = new Dictionary<string, object>
            {
                ["operation"] = "remove",
                ["gameObjectPath"] = testGo.name,
                ["componentType"] = TMP_TYPE
            };
            var removeCommand = new McpIncomingCommand { ToolName = "componentManage", Payload = removePayload };

            // Act
            var result = McpCommandProcessor.Execute(removeCommand) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.ContainsKey("removed"));
            Assert.AreEqual(TMP_TYPE, result["removed"]);
            
            var component = testGo.GetComponent(TMP_TYPE);
            Assert.IsNull(component, "TextMeshPro component should be removed from GameObject");
        }

        #endregion

        #region TextMeshProUGUI Tests

        [Test]
        public void AddComponent_TextMeshProUGUI_CreatesComponent()
        {
            // Arrange - Add RectTransform first (required for UI components)
            testGo.AddComponent<RectTransform>();
            
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "add",
                ["gameObjectPath"] = testGo.name,
                ["componentType"] = TMP_UGUI_TYPE
            };
            var command = new McpIncomingCommand { ToolName = "componentManage", Payload = payload };

            // Act
            var result = McpCommandProcessor.Execute(command) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.ContainsKey("type"));
            Assert.AreEqual(TMP_UGUI_TYPE, result["type"]);
            
            var component = testGo.GetComponent(TMP_UGUI_TYPE);
            Assert.IsNotNull(component, "TextMeshProUGUI component should be added to GameObject");
        }

        [Test]
        public void UpdateComponent_TextMeshProUGUI_UpdatesText()
        {
            // Arrange
            testGo.AddComponent<RectTransform>();
            
            var addPayload = new Dictionary<string, object>
            {
                ["operation"] = "add",
                ["gameObjectPath"] = testGo.name,
                ["componentType"] = TMP_UGUI_TYPE
            };
            var addCommand = new McpIncomingCommand { ToolName = "componentManage", Payload = addPayload };
            McpCommandProcessor.Execute(addCommand);

            var updatePayload = new Dictionary<string, object>
            {
                ["operation"] = "update",
                ["gameObjectPath"] = testGo.name,
                ["componentType"] = TMP_UGUI_TYPE,
                ["propertyChanges"] = new Dictionary<string, object>
                {
                    ["text"] = "Hello UI!",
                    ["fontSize"] = 24.0,
                    ["color"] = new Dictionary<string, object>
                    {
                        ["r"] = 1.0,
                        ["g"] = 0.0,
                        ["b"] = 0.0,
                        ["a"] = 1.0
                    }
                }
            };
            var updateCommand = new McpIncomingCommand { ToolName = "componentManage", Payload = updatePayload };

            // Act
            var result = McpCommandProcessor.Execute(updateCommand) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            
            var component = testGo.GetComponent(TMP_UGUI_TYPE);
            Assert.IsNotNull(component);
            
            // Verify properties using reflection
            var textProp = component.GetType().GetProperty("text");
            Assert.AreEqual("Hello UI!", textProp.GetValue(component));
            
            var fontSizeProp = component.GetType().GetProperty("fontSize");
            Assert.AreEqual(24.0f, (float)fontSizeProp.GetValue(component), 0.01f);
        }

        [Test]
        public void InspectComponent_TextMeshProUGUI_ReturnsComponentInfo()
        {
            // Arrange
            testGo.AddComponent<RectTransform>();
            
            var addPayload = new Dictionary<string, object>
            {
                ["operation"] = "add",
                ["gameObjectPath"] = testGo.name,
                ["componentType"] = TMP_UGUI_TYPE
            };
            var addCommand = new McpIncomingCommand { ToolName = "componentManage", Payload = addPayload };
            McpCommandProcessor.Execute(addCommand);

            // Set some properties
            var component = testGo.GetComponent(TMP_UGUI_TYPE);
            var textProp = component.GetType().GetProperty("text");
            textProp.SetValue(component, "UI Text");

            var inspectPayload = new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["gameObjectPath"] = testGo.name,
                ["componentType"] = TMP_UGUI_TYPE
            };
            var inspectCommand = new McpIncomingCommand { ToolName = "componentManage", Payload = inspectPayload };

            // Act
            var result = McpCommandProcessor.Execute(inspectCommand) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.ContainsKey("type"));
            Assert.AreEqual(TMP_UGUI_TYPE, result["type"]);
            Assert.IsTrue(result.ContainsKey("properties"));
            
            var properties = result["properties"] as Dictionary<string, object>;
            Assert.IsNotNull(properties);
            Assert.IsTrue(properties.ContainsKey("text"));
            Assert.AreEqual("UI Text", properties["text"]);
        }

        [Test]
        public void RemoveComponent_TextMeshProUGUI_RemovesComponent()
        {
            // Arrange
            testGo.AddComponent<RectTransform>();
            
            var addPayload = new Dictionary<string, object>
            {
                ["operation"] = "add",
                ["gameObjectPath"] = testGo.name,
                ["componentType"] = TMP_UGUI_TYPE
            };
            var addCommand = new McpIncomingCommand { ToolName = "componentManage", Payload = addPayload };
            McpCommandProcessor.Execute(addCommand);

            var removePayload = new Dictionary<string, object>
            {
                ["operation"] = "remove",
                ["gameObjectPath"] = testGo.name,
                ["componentType"] = TMP_UGUI_TYPE
            };
            var removeCommand = new McpIncomingCommand { ToolName = "componentManage", Payload = removePayload };

            // Act
            var result = McpCommandProcessor.Execute(removeCommand) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.ContainsKey("removed"));
            Assert.AreEqual(TMP_UGUI_TYPE, result["removed"]);
            
            var component = testGo.GetComponent(TMP_UGUI_TYPE);
            Assert.IsNull(component, "TextMeshProUGUI component should be removed from GameObject");
        }

        #endregion

        #region Multiple Component Operations Tests

        [Test]
        public void AddMultipleComponents_TextMeshPro_CreatesMultipleComponents()
        {
            // Arrange - Create multiple test objects
            var testGo2 = new GameObject("TestTextMeshProObject2");
            var testGo3 = new GameObject("TestTextMeshProObject3");

            try
            {
                var payload = new Dictionary<string, object>
                {
                    ["operation"] = "addMultiple",
                    ["pattern"] = "TestTextMeshProObject*",
                    ["useRegex"] = false,
                    ["componentType"] = TMP_TYPE,
                    ["propertyChanges"] = new Dictionary<string, object>
                    {
                        ["text"] = "Batch Added",
                        ["fontSize"] = 20.0
                    }
                };
                var command = new McpIncomingCommand { ToolName = "componentManage", Payload = payload };

                // Act
                var result = McpCommandProcessor.Execute(command) as Dictionary<string, object>;

                // Assert
                Assert.IsNotNull(result);
                Assert.AreEqual(3, result["successCount"]);
                Assert.AreEqual(0, result["errorCount"]);
                
                var results = result["results"] as List<object>;
                Assert.IsNotNull(results);
                Assert.AreEqual(3, results.Count);

                // Verify all components were added
                Assert.IsNotNull(testGo.GetComponent(TMP_TYPE));
                Assert.IsNotNull(testGo2.GetComponent(TMP_TYPE));
                Assert.IsNotNull(testGo3.GetComponent(TMP_TYPE));

                // Verify properties were set
                var component = testGo.GetComponent(TMP_TYPE);
                var textProp = component.GetType().GetProperty("text");
                Assert.AreEqual("Batch Added", textProp.GetValue(component));
            }
            finally
            {
                Object.DestroyImmediate(testGo2);
                Object.DestroyImmediate(testGo3);
            }
        }

        [Test]
        public void UpdateMultipleComponents_TextMeshPro_UpdatesMultipleComponents()
        {
            // Arrange - Create and add components to multiple objects
            var testGo2 = new GameObject("TestTextMeshProObject2");

            try
            {
                // Add components first
                var addPayload = new Dictionary<string, object>
                {
                    ["operation"] = "addMultiple",
                    ["pattern"] = "TestTextMeshProObject*",
                    ["useRegex"] = false,
                    ["componentType"] = TMP_TYPE
                };
                var addCommand = new McpIncomingCommand { ToolName = "componentManage", Payload = addPayload };
                McpCommandProcessor.Execute(addCommand);

                // Now update them
                var updatePayload = new Dictionary<string, object>
                {
                    ["operation"] = "updateMultiple",
                    ["pattern"] = "TestTextMeshProObject*",
                    ["useRegex"] = false,
                    ["componentType"] = TMP_TYPE,
                    ["propertyChanges"] = new Dictionary<string, object>
                    {
                        ["text"] = "Batch Updated",
                        ["fontSize"] = 30.0
                    }
                };
                var updateCommand = new McpIncomingCommand { ToolName = "componentManage", Payload = updatePayload };

                // Act
                var result = McpCommandProcessor.Execute(updateCommand) as Dictionary<string, object>;

                // Assert
                Assert.IsNotNull(result);
                Assert.AreEqual(2, result["successCount"]);
                Assert.AreEqual(0, result["errorCount"]);

                // Verify properties were updated
                var component1 = testGo.GetComponent(TMP_TYPE);
                var textProp1 = component1.GetType().GetProperty("text");
                Assert.AreEqual("Batch Updated", textProp1.GetValue(component1));

                var component2 = testGo2.GetComponent(TMP_TYPE);
                var textProp2 = component2.GetType().GetProperty("text");
                Assert.AreEqual("Batch Updated", textProp2.GetValue(component2));
            }
            finally
            {
                Object.DestroyImmediate(testGo2);
            }
        }

        [Test]
        public void InspectMultipleComponents_TextMeshPro_ReturnsMultipleComponentInfo()
        {
            // Arrange - Create and add components to multiple objects
            var testGo2 = new GameObject("TestTextMeshProObject2");

            try
            {
                // Add components with different text
                var addPayload1 = new Dictionary<string, object>
                {
                    ["operation"] = "add",
                    ["gameObjectPath"] = testGo.name,
                    ["componentType"] = TMP_TYPE
                };
                var addCommand1 = new McpIncomingCommand { ToolName = "componentManage", Payload = addPayload1 };
                McpCommandProcessor.Execute(addCommand1);

                var addPayload2 = new Dictionary<string, object>
                {
                    ["operation"] = "add",
                    ["gameObjectPath"] = testGo2.name,
                    ["componentType"] = TMP_TYPE
                };
                var addCommand2 = new McpIncomingCommand { ToolName = "componentManage", Payload = addPayload2 };
                McpCommandProcessor.Execute(addCommand2);

                // Set different text values
                var component1 = testGo.GetComponent(TMP_TYPE);
                component1.GetType().GetProperty("text").SetValue(component1, "Text 1");
                
                var component2 = testGo2.GetComponent(TMP_TYPE);
                component2.GetType().GetProperty("text").SetValue(component2, "Text 2");

                var inspectPayload = new Dictionary<string, object>
                {
                    ["operation"] = "inspectMultiple",
                    ["pattern"] = "TestTextMeshProObject*",
                    ["useRegex"] = false,
                    ["componentType"] = TMP_TYPE
                };
                var inspectCommand = new McpIncomingCommand { ToolName = "componentManage", Payload = inspectPayload };

                // Act
                var result = McpCommandProcessor.Execute(inspectCommand) as Dictionary<string, object>;

                // Assert
                Assert.IsNotNull(result);
                Assert.AreEqual(2, result["returnedCount"]);
                
                var results = result["results"] as List<object>;
                Assert.IsNotNull(results);
                Assert.AreEqual(2, results.Count);

                // Verify each result has properties
                foreach (var item in results)
                {
                    var itemDict = item as Dictionary<string, object>;
                    Assert.IsNotNull(itemDict);
                    Assert.IsTrue(itemDict.ContainsKey("properties"));
                    
                    var properties = itemDict["properties"] as Dictionary<string, object>;
                    Assert.IsNotNull(properties);
                    Assert.IsTrue(properties.ContainsKey("text"));
                }
            }
            finally
            {
                Object.DestroyImmediate(testGo2);
            }
        }

        [Test]
        public void RemoveMultipleComponents_TextMeshPro_RemovesMultipleComponents()
        {
            // Arrange - Create and add components to multiple objects
            var testGo2 = new GameObject("TestTextMeshProObject2");

            try
            {
                // Add components first
                var addPayload = new Dictionary<string, object>
                {
                    ["operation"] = "addMultiple",
                    ["pattern"] = "TestTextMeshProObject*",
                    ["useRegex"] = false,
                    ["componentType"] = TMP_TYPE
                };
                var addCommand = new McpIncomingCommand { ToolName = "componentManage", Payload = addPayload };
                McpCommandProcessor.Execute(addCommand);

                // Verify components were added
                Assert.IsNotNull(testGo.GetComponent(TMP_TYPE));
                Assert.IsNotNull(testGo2.GetComponent(TMP_TYPE));

                // Now remove them
                var removePayload = new Dictionary<string, object>
                {
                    ["operation"] = "removeMultiple",
                    ["pattern"] = "TestTextMeshProObject*",
                    ["useRegex"] = false,
                    ["componentType"] = TMP_TYPE
                };
                var removeCommand = new McpIncomingCommand { ToolName = "componentManage", Payload = removePayload };

                // Act
                var result = McpCommandProcessor.Execute(removeCommand) as Dictionary<string, object>;

                // Assert
                Assert.IsNotNull(result);
                Assert.AreEqual(2, result["successCount"]);
                
                // Verify components were removed
                Assert.IsNull(testGo.GetComponent(TMP_TYPE));
                Assert.IsNull(testGo2.GetComponent(TMP_TYPE));
            }
            finally
            {
                Object.DestroyImmediate(testGo2);
            }
        }

        #endregion

        #region Advanced Property Tests

        [Test]
        public void UpdateComponent_TextMeshPro_UpdatesAdvancedProperties()
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
                    ["text"] = "Advanced Text",
                    ["fontSize"] = 48.0,
                    ["enableAutoSizing"] = true,
                    ["fontSizeMin"] = 12.0,
                    ["fontSizeMax"] = 72.0,
                    ["alignment"] = 257 // TextAlignmentOptions.Center
                }
            };
            var updateCommand = new McpIncomingCommand { ToolName = "componentManage", Payload = updatePayload };

            // Act
            var result = McpCommandProcessor.Execute(updateCommand) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
            
            var component = testGo.GetComponent(TMP_TYPE);
            var textProp = component.GetType().GetProperty("text");
            Assert.AreEqual("Advanced Text", textProp.GetValue(component));
            
            var enableAutoSizingProp = component.GetType().GetProperty("enableAutoSizing");
            Assert.IsTrue((bool)enableAutoSizingProp.GetValue(component));
        }

        [Test]
        public void InspectComponent_TextMeshPro_WithPropertyFilter()
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
            Assert.IsTrue(properties.ContainsKey("text"));
            Assert.IsTrue(properties.ContainsKey("fontSize"));
            Assert.IsTrue(properties.ContainsKey("color"));
        }

        #endregion
    }
}
