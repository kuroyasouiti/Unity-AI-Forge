using System.Collections.Generic;
using MCP.Editor.Base;
using NUnit.Framework;

namespace MCP.Editor.Tests
{
    /// <summary>
    /// BaseCommandHandlerのユニットテスト。
    /// </summary>
    public class BaseCommandHandlerTests
    {
        private TestCommandHandler _handler;
        
        [SetUp]
        public void SetUp()
        {
            _handler = new TestCommandHandler();
        }
        
        [Test]
        public void TestExecute_ValidPayload_ReturnsSuccess()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "test",
                ["param1"] = "value1"
            };
            
            var result = _handler.Execute(payload) as Dictionary<string, object>;
            
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
        }
        
        [Test]
        public void TestExecute_NullPayload_ReturnsError()
        {
            var result = _handler.Execute(null) as Dictionary<string, object>;
            
            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
            Assert.IsTrue(result.ContainsKey("error"));
        }
        
        [Test]
        public void TestExecute_MissingOperation_ReturnsError()
        {
            var payload = new Dictionary<string, object>
            {
                ["param1"] = "value1"
            };
            
            var result = _handler.Execute(payload) as Dictionary<string, object>;
            
            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
        }
        
        [Test]
        public void TestExecute_UnsupportedOperation_ReturnsError()
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "unsupported"
            };
            
            var result = _handler.Execute(payload) as Dictionary<string, object>;
            
            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
            StringAssert.Contains("not supported", result["error"].ToString());
        }
        
        [Test]
        public void TestGetString_ExistingKey_ReturnsValue()
        {
            var payload = new Dictionary<string, object>
            {
                ["key"] = "value"
            };
            
            var value = _handler.PublicGetString(payload, "key");
            
            Assert.AreEqual("value", value);
        }
        
        [Test]
        public void TestGetString_MissingKey_ReturnsDefault()
        {
            var payload = new Dictionary<string, object>();
            
            var value = _handler.PublicGetString(payload, "key", "default");
            
            Assert.AreEqual("default", value);
        }
        
        [Test]
        public void TestGetBool_ValidValue_ReturnsTrue()
        {
            var payload = new Dictionary<string, object>
            {
                ["flag"] = true
            };
            
            var value = _handler.PublicGetBool(payload, "flag");
            
            Assert.IsTrue(value);
        }
        
        [Test]
        public void TestGetInt_ValidValue_ReturnsInt()
        {
            var payload = new Dictionary<string, object>
            {
                ["count"] = 42
            };
            
            var value = _handler.PublicGetInt(payload, "count");
            
            Assert.AreEqual(42, value);
        }
        
        [Test]
        public void TestGetInt_LongValue_ConvertsToInt()
        {
            var payload = new Dictionary<string, object>
            {
                ["count"] = 42L
            };
            
            var value = _handler.PublicGetInt(payload, "count");
            
            Assert.AreEqual(42, value);
        }
        
        [Test]
        public void TestCreateSuccessResponse_WithAdditionalData_ContainsAllData()
        {
            var response = _handler.PublicCreateSuccessResponse(
                ("key1", "value1"),
                ("key2", 42)
            );
            
            Assert.IsTrue((bool)response["success"]);
            Assert.AreEqual("value1", response["key1"]);
            Assert.AreEqual(42, response["key2"]);
        }
        
        /// <summary>
        /// テスト用のコマンドハンドラー実装。
        /// </summary>
        private class TestCommandHandler : BaseCommandHandler
        {
            public override string Category => "test";
            
            public override IEnumerable<string> SupportedOperations => new[] { "test", "create", "delete" };
            
            protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
            {
                return operation switch
                {
                    "test" => CreateSuccessResponse(("operation", "test")),
                    "create" => CreateSuccessResponse(("operation", "create")),
                    "delete" => CreateSuccessResponse(("operation", "delete")),
                    _ => throw new System.InvalidOperationException($"Unknown operation: {operation}")
                };
            }
            
            // テスト用に protected メソッドを public で公開
            public string PublicGetString(Dictionary<string, object> payload, string key, string defaultValue = null)
                => GetString(payload, key, defaultValue);
            
            public bool PublicGetBool(Dictionary<string, object> payload, string key, bool defaultValue = false)
                => GetBool(payload, key, defaultValue);
            
            public int PublicGetInt(Dictionary<string, object> payload, string key, int defaultValue = 0)
                => GetInt(payload, key, defaultValue);
            
            public Dictionary<string, object> PublicCreateSuccessResponse(params (string key, object value)[] additionalData)
                => CreateSuccessResponse(additionalData);
        }
    }
}

