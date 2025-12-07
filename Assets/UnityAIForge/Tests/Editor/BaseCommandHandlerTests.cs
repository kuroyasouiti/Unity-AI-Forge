using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MCP.Editor.Base;
using MCP.Editor.Interfaces;

namespace MCP.Editor.Tests
{
    /// <summary>
    /// BaseCommandHandler のユニットテスト。
    /// TestableCommandHandler を使用してテストを行います。
    /// </summary>
    [TestFixture]
    public class BaseCommandHandlerTests
    {
        private TestableCommandHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new TestableCommandHandler();
        }

        #region Payload Validation Tests

        [Test]
        public void Execute_NullPayload_ShouldReturnError()
        {
            // Act
            var result = _handler.Execute(null) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
            Assert.IsTrue(result["error"].ToString().Contains("null"));
        }

        [Test]
        public void Execute_MissingOperation_ShouldReturnError()
        {
            // Arrange
            var payload = new Dictionary<string, object>
            {
                ["someKey"] = "someValue"
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
            Assert.IsTrue(result["error"].ToString().Contains("operation"));
        }

        [Test]
        public void Execute_EmptyOperation_ShouldReturnError()
        {
            // Arrange
            var payload = new Dictionary<string, object>
            {
                ["operation"] = ""
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
        }

        [Test]
        public void Execute_NullOperation_ShouldReturnError()
        {
            // Arrange
            var payload = new Dictionary<string, object>
            {
                ["operation"] = null
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
        }

        #endregion

        #region Operation Support Tests

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

        [Test]
        public void Execute_SupportedOperation_ShouldSucceed()
        {
            // Arrange
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "testOp"
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);
        }

        [Test]
        public void SupportedOperations_ShouldReturnCorrectOperations()
        {
            // Assert
            Assert.Contains("testOp", _handler.SupportedOperations.ToList());
            Assert.Contains("inspect", _handler.SupportedOperations.ToList());
            Assert.Contains("create", _handler.SupportedOperations.ToList());
        }

        #endregion

        #region Category and Version Tests

        [Test]
        public void Category_ShouldReturnCorrectValue()
        {
            // Assert
            Assert.AreEqual("testable", _handler.Category);
        }

        [Test]
        public void Version_ShouldReturnDefaultValue()
        {
            // Assert
            Assert.AreEqual("1.0.0", _handler.Version);
        }

        #endregion

        #region Helper Method Tests

        [Test]
        public void GetString_ExistingKey_ShouldReturnValue()
        {
            // Arrange
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "testOp",
                ["name"] = "TestName"
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("TestName", result["parsedName"]);
        }

        [Test]
        public void GetString_MissingKey_ShouldReturnDefault()
        {
            // Arrange
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "testOp"
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("default", result["parsedName"]);
        }

        [Test]
        public void GetBool_TrueValue_ShouldReturnTrue()
        {
            // Arrange
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "testOp",
                ["active"] = true
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsTrue((bool)result["parsedActive"]);
        }

        [Test]
        public void GetBool_FalseStringValue_ShouldReturnFalse()
        {
            // Arrange
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "testOp",
                ["active"] = "false"
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsFalse((bool)result["parsedActive"]);
        }

        [Test]
        public void GetInt_IntValue_ShouldReturnCorrectValue()
        {
            // Arrange
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "testOp",
                ["count"] = 42
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.AreEqual(42, result["parsedCount"]);
        }

        [Test]
        public void GetInt_LongValue_ShouldConvertToInt()
        {
            // Arrange
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "testOp",
                ["count"] = 100L
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.AreEqual(100, result["parsedCount"]);
        }

        [Test]
        public void GetFloat_FloatValue_ShouldReturnCorrectValue()
        {
            // Arrange
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "testOp",
                ["value"] = 3.14f
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.AreEqual(3.14f, (float)result["parsedValue"], 0.001f);
        }

        [Test]
        public void GetFloat_DoubleValue_ShouldConvertToFloat()
        {
            // Arrange
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "testOp",
                ["value"] = 2.718
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.AreEqual(2.718f, (float)result["parsedValue"], 0.001f);
        }

        #endregion

        #region RequiresCompilationWait Tests

        [Test]
        public void RequiresCompilationWait_InspectOperation_ShouldReturnFalse()
        {
            // Assert - inspect is a read-only operation
            Assert.IsFalse(_handler.TestRequiresCompilationWait("inspect"));
        }

        [Test]
        public void RequiresCompilationWait_CreateOperation_ShouldReturnTrue()
        {
            // Assert - create is a write operation
            Assert.IsTrue(_handler.TestRequiresCompilationWait("create"));
        }

        #endregion

        #region CreateSuccessResponse Tests

        [Test]
        public void CreateSuccessResponse_ShouldIncludeSuccessTrue()
        {
            // Arrange
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "testOp"
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsTrue((bool)result["success"]);
        }

        #endregion

        #region Error Response Tests

        [Test]
        public void Execute_WhenExceptionThrown_ShouldReturnErrorResponse()
        {
            // Arrange
            _handler.SetShouldThrow(true);
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "testOp"
            };

            // Act
            var result = _handler.Execute(payload) as Dictionary<string, object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse((bool)result["success"]);
            Assert.IsNotNull(result["error"]);
            Assert.IsNotNull(result["errorType"]);
            Assert.AreEqual("testable", result["category"]);
        }

        #endregion
    }

    /// <summary>
    /// テスト用の具象コマンドハンドラー。
    /// BaseCommandHandler のテストに使用します。
    /// </summary>
    public class TestableCommandHandler : BaseCommandHandler
    {
        private bool _shouldThrow = false;

        public override string Category => "testable";

        public override IEnumerable<string> SupportedOperations => new[]
        {
            "testOp",
            "inspect",
            "create",
            "delete"
        };

        public void SetShouldThrow(bool shouldThrow)
        {
            _shouldThrow = shouldThrow;
        }

        public bool TestRequiresCompilationWait(string operation)
        {
            return RequiresCompilationWait(operation);
        }

        protected override object ExecuteOperation(string operation, Dictionary<string, object> payload)
        {
            if (_shouldThrow)
            {
                throw new InvalidOperationException("Test exception");
            }

            return operation switch
            {
                "testOp" => HandleTestOp(payload),
                "inspect" => HandleInspect(payload),
                "create" => HandleCreate(payload),
                "delete" => HandleDelete(payload),
                _ => throw new InvalidOperationException($"Unknown operation: {operation}")
            };
        }

        private object HandleTestOp(Dictionary<string, object> payload)
        {
            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["operation"] = "testOp",
                ["parsedName"] = GetString(payload, "name", "default"),
                ["parsedActive"] = GetBool(payload, "active"),
                ["parsedCount"] = GetInt(payload, "count"),
                ["parsedValue"] = GetFloat(payload, "value")
            };
        }

        private object HandleInspect(Dictionary<string, object> payload)
        {
            return CreateSuccessResponse(
                ("operation", "inspect"),
                ("data", "inspected")
            );
        }

        private object HandleCreate(Dictionary<string, object> payload)
        {
            return CreateSuccessResponse(
                ("operation", "create"),
                ("created", true)
            );
        }

        private object HandleDelete(Dictionary<string, object> payload)
        {
            return CreateSuccessResponse(
                ("operation", "delete"),
                ("deleted", true)
            );
        }
    }
}
