using System.Collections.Generic;
using System.Linq;
using MCP.Editor.Handlers.Settings;
using NUnit.Framework;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class ProjectSettingsManageHandlerTests
    {
        private ProjectSettingsManageHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new ProjectSettingsManageHandler();
        }

        [Test]
        public void Category_ReturnsProjectSettingsManage()
        {
            Assert.AreEqual("projectSettingsManage", _handler.Category);
        }

        [Test]
        public void SupportedOperations_ContainsExpected()
        {
            var ops = _handler.SupportedOperations.ToList();
            Assert.Contains("read", ops);
            Assert.Contains("write", ops);
            Assert.Contains("list", ops);
            Assert.Contains("addSceneToBuild", ops);
            Assert.Contains("listBuildScenes", ops);
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
        public void ListBuildScenes_ReturnsSuccess()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("listBuildScenes"));
            TestUtilities.AssertSuccess(result);
        }

        [Test]
        public void List_ReturnsCategories()
        {
            var result = _handler.Execute(TestUtilities.CreatePayload("list")) as Dictionary<string, object>;
            Assert.IsNotNull(result, "Result should be a Dictionary");
            Assert.IsTrue(result.ContainsKey("categories"), "Result should contain 'categories' key");
        }
    }
}
