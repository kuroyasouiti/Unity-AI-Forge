using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Handlers.HighLevel;
using NUnit.Framework;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class ClassCatalogHandlerTests
    {
        private ClassCatalogHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new ClassCatalogHandler();
        }

        [Test]
        public void Category_ReturnsClassCatalog()
        {
            Assert.AreEqual("classCatalog", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ContainsExpected()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("listTypes", ops);
            Assert.Contains("inspectType", ops);
            Assert.AreEqual(2, ops.Count);
        }

        [Test]
        public void Execute_NullPayload_ReturnsError()
        {
            TestUtilities.AssertError(_handler.Execute(null));
        }

        [Test]
        public void Execute_UnsupportedOperation_ReturnsError()
        {
            TestUtilities.AssertError(_handler.Execute(TestUtilities.CreatePayload("nonExistent")), "not supported");
        }

        [Test]
        public void ListTypes_NoFilters_ReturnsResults()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("listTypes"));
            TestUtilities.AssertSuccess(result);
            var dict = result as Dictionary<string, object>;
            Assert.IsTrue(dict.ContainsKey("count"));
            Assert.IsTrue(dict.ContainsKey("types"));
            var types = dict["types"] as List<Dictionary<string, object>>;
            Assert.IsNotNull(types);
            Assert.IsTrue((int)dict["count"] > 0, "Should find at least one type");
        }

        [Test]
        public void ListTypes_WithMaxResults_LimitsOutput()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("listTypes",
                ("maxResults", 3)));
            TestUtilities.AssertSuccess(result);
            var dict = result as Dictionary<string, object>;
            Assert.IsTrue((int)dict["count"] <= 3);
        }

        [Test]
        public void ListTypes_WithNamePattern_FiltersResults()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("listTypes",
                ("namePattern", "*Handler"),
                ("maxResults", 100)));
            TestUtilities.AssertSuccess(result);
            var dict = result as Dictionary<string, object>;
            var types = dict["types"] as List<Dictionary<string, object>>;
            foreach (var t in types)
            {
                StringAssert.EndsWith("Handler", (string)t["name"]);
            }
        }

        [Test]
        public void InspectType_MissingClassName_ReturnsError()
        {
            TestUtilities.AssertError(_handler.Execute(TestUtilities.CreatePayload("inspectType")), "className");
        }

        [Test]
        public void InspectType_UnknownType_ReturnsError()
        {
            TestUtilities.AssertError(_handler.Execute(TestUtilities.CreatePayload("inspectType",
                ("className", "NonExistentType_XYZ_12345"))), "not found");
        }

        [Test]
        public void InspectType_KnownType_ReturnsFields()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("inspectType",
                ("className", "ClassCatalogHandler"),
                ("includeFields", true)));
            TestUtilities.AssertSuccess(result);
            var dict = result as Dictionary<string, object>;
            Assert.AreEqual("ClassCatalogHandler", dict["name"]);
            Assert.IsTrue(dict.ContainsKey("fields"));
        }

        [Test]
        public void InspectType_WithMethods_ReturnsMethods()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("inspectType",
                ("className", "ClassCatalogHandler"),
                ("includeMethods", true)));
            TestUtilities.AssertSuccess(result);
            var dict = result as Dictionary<string, object>;
            Assert.IsTrue(dict.ContainsKey("methods"));
            var methods = dict["methods"] as List<Dictionary<string, object>>;
            Assert.IsNotNull(methods);
        }

        [Test]
        public void InspectType_WithProperties_ReturnsProperties()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("inspectType",
                ("className", "ClassCatalogHandler"),
                ("includeProperties", true)));
            TestUtilities.AssertSuccess(result);
            var dict = result as Dictionary<string, object>;
            Assert.IsTrue(dict.ContainsKey("properties"));
        }
    }
}
