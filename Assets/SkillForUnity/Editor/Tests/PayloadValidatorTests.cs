using System;
using System.Collections.Generic;
using MCP.Editor.Base;
using MCP.Editor.Interfaces;
using NUnit.Framework;

namespace MCP.Editor.Tests
{
    /// <summary>
    /// StandardPayloadValidatorのユニットテスト。
    /// </summary>
    public class PayloadValidatorTests
    {
        private StandardPayloadValidator _validator;
        
        [SetUp]
        public void SetUp()
        {
            _validator = new StandardPayloadValidator();
        }
        
        [Test]
        public void TestValidate_NullPayload_ReturnsInvalid()
        {
            var result = _validator.Validate(null, "test");
            
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Count > 0);
        }
        
        [Test]
        public void TestValidate_UnregisteredOperation_ReturnsSuccess()
        {
            var payload = new Dictionary<string, object>
            {
                ["param1"] = "value1"
            };
            
            var result = _validator.Validate(payload, "unregistered");
            
            Assert.IsTrue(result.IsValid);
        }
        
        [Test]
        public void TestValidate_RequiredParameterMissing_ReturnsInvalid()
        {
            var schema = OperationSchema.Builder()
                .RequireParameter("name", typeof(string))
                .Build();
            
            _validator.RegisterOperation("create", schema);
            
            var payload = new Dictionary<string, object>();
            var result = _validator.Validate(payload, "create");
            
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(e => e.Contains("name")));
        }
        
        [Test]
        public void TestValidate_RequiredParameterPresent_ReturnsValid()
        {
            var schema = OperationSchema.Builder()
                .RequireParameter("name", typeof(string))
                .Build();
            
            _validator.RegisterOperation("create", schema);
            
            var payload = new Dictionary<string, object>
            {
                ["name"] = "TestObject"
            };
            
            var result = _validator.Validate(payload, "create");
            
            Assert.IsTrue(result.IsValid);
        }
        
        [Test]
        public void TestValidate_OptionalParameterWithDefault_AppliesDefault()
        {
            var schema = OperationSchema.Builder()
                .OptionalParameter("count", typeof(int), 10)
                .Build();
            
            _validator.RegisterOperation("test", schema);
            
            var payload = new Dictionary<string, object>();
            var result = _validator.Validate(payload, "test");
            
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(10, result.NormalizedPayload["count"]);
        }
        
        [Test]
        public void TestValidate_TypeConversion_ConvertsCorrectly()
        {
            var schema = OperationSchema.Builder()
                .OptionalParameter("count", typeof(int))
                .Build();
            
            _validator.RegisterOperation("test", schema);
            
            var payload = new Dictionary<string, object>
            {
                ["count"] = "42"
            };
            
            var result = _validator.Validate(payload, "test");
            
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(42, result.NormalizedPayload["count"]);
        }
        
        [Test]
        public void TestValidate_CustomValidator_ExecutesValidator()
        {
            var validatorCalled = false;
            
            var schema = OperationSchema.Builder()
                .RequireParameter("name", typeof(string))
                .AddCustomValidator((payload, result) =>
                {
                    validatorCalled = true;
                    if (!payload["name"].ToString().StartsWith("Test"))
                    {
                        result.AddError("Name must start with 'Test'");
                    }
                })
                .Build();
            
            _validator.RegisterOperation("test", schema);
            
            var payload = new Dictionary<string, object>
            {
                ["name"] = "InvalidName"
            };
            
            var result = _validator.Validate(payload, "test");
            
            Assert.IsTrue(validatorCalled);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(e => e.Contains("Test")));
        }
        
        [Test]
        public void TestOperationSchemaBuilder_BuildsCorrectSchema()
        {
            var schema = OperationSchema.Builder()
                .WithDescription("Test operation")
                .RequireParameter("name", typeof(string))
                .OptionalParameter("count", typeof(int), 5)
                .Build();
            
            Assert.AreEqual("Test operation", schema.Description);
            Assert.Contains("name", schema.RequiredParameters);
            Assert.AreEqual(typeof(string), schema.ParameterTypes["name"]);
            Assert.AreEqual(typeof(int), schema.ParameterTypes["count"]);
            Assert.AreEqual(5, schema.DefaultValues["count"]);
        }
    }
}

