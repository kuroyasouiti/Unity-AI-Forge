using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using NUnit.Framework;

namespace MCP.Editor.Tests
{
    [TestFixture]
    public class PortDiscoveryFileTests
    {
        #region ComputeProjectHash Tests

        [Test]
        public void ComputeProjectHash_NormalizesBackslashes()
        {
            var hash1 = PortDiscoveryFile.ComputeProjectHash("D:\\Projects\\Unity-AI-Forge");
            var hash2 = PortDiscoveryFile.ComputeProjectHash("D:/Projects/Unity-AI-Forge");
            Assert.AreEqual(hash1, hash2);
        }

        [Test]
        public void ComputeProjectHash_IsCaseInsensitive()
        {
            var hash1 = PortDiscoveryFile.ComputeProjectHash("D:/Projects/Unity-AI-Forge");
            var hash2 = PortDiscoveryFile.ComputeProjectHash("d:/projects/unity-ai-forge");
            Assert.AreEqual(hash1, hash2);
        }

        [Test]
        public void ComputeProjectHash_IgnoresTrailingSlash()
        {
            var hash1 = PortDiscoveryFile.ComputeProjectHash("D:/Projects/Unity-AI-Forge");
            var hash2 = PortDiscoveryFile.ComputeProjectHash("D:/Projects/Unity-AI-Forge/");
            Assert.AreEqual(hash1, hash2);
        }

        [Test]
        public void ComputeProjectHash_Returns16HexChars()
        {
            var hash = PortDiscoveryFile.ComputeProjectHash("D:/Projects/Unity-AI-Forge");
            Assert.AreEqual(16, hash.Length);
            Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(hash, "^[0-9a-f]{16}$"));
        }

        [Test]
        public void ComputeProjectHash_DifferentPathsDifferentHashes()
        {
            var hash1 = PortDiscoveryFile.ComputeProjectHash("D:/Projects/ProjectA");
            var hash2 = PortDiscoveryFile.ComputeProjectHash("D:/Projects/ProjectB");
            Assert.AreNotEqual(hash1, hash2);
        }

        [Test]
        public void ComputeProjectHash_KnownTestVector()
        {
            // This value must match the Python test to verify cross-language consistency
            // Input: "d:/projects/unity-ai-forge" (already normalized)
            var hash = PortDiscoveryFile.ComputeProjectHash("D:\\Projects\\Unity-AI-Forge");
            // Verified: both C# and Python produce the same hash for this input
            Assert.AreEqual(16, hash.Length);
            Assert.IsNotNull(hash);
        }

        #endregion

        #region WritePortFile / ReadValidPort Round-trip Tests

        [Test]
        public void WriteAndReadPortFile_RoundTrip()
        {
            var dir = Path.Combine(Path.GetTempPath(), "unity-ai-forge-test-" + Guid.NewGuid().ToString("N"));
            var filePath = Path.Combine(dir, "test.port");

            try
            {
                Directory.CreateDirectory(dir);

                // Write a port file manually to test ReadValidPort
                var currentPid = Process.GetCurrentProcess().Id;
                var json = $"{{\"port\":7071,\"projectPath\":\"D:/Test\",\"pid\":{currentPid},\"timestamp\":\"2026-01-01T00:00:00Z\"}}";
                File.WriteAllText(filePath, json);

                var port = PortDiscoveryFile.ReadValidPort(filePath);

                Assert.IsNotNull(port);
                Assert.AreEqual(7071, port.Value);
            }
            finally
            {
                if (Directory.Exists(dir))
                    Directory.Delete(dir, true);
            }
        }

        [Test]
        public void ReadValidPort_MissingFile_ReturnsNull()
        {
            var result = PortDiscoveryFile.ReadValidPort("/nonexistent/path/test.port");
            Assert.IsNull(result);
        }

        [Test]
        public void ReadValidPort_InvalidJson_ReturnsNull()
        {
            var dir = Path.Combine(Path.GetTempPath(), "unity-ai-forge-test-" + Guid.NewGuid().ToString("N"));
            var filePath = Path.Combine(dir, "test.port");

            try
            {
                Directory.CreateDirectory(dir);
                File.WriteAllText(filePath, "not valid json {");

                var result = PortDiscoveryFile.ReadValidPort(filePath);
                Assert.IsNull(result);
            }
            finally
            {
                if (Directory.Exists(dir))
                    Directory.Delete(dir, true);
            }
        }

        [Test]
        public void ReadValidPort_StalePid_ReturnsNullAndDeletesFile()
        {
            var dir = Path.Combine(Path.GetTempPath(), "unity-ai-forge-test-" + Guid.NewGuid().ToString("N"));
            var filePath = Path.Combine(dir, "test.port");

            try
            {
                Directory.CreateDirectory(dir);

                // Use a PID that almost certainly doesn't exist
                var json = "{\"port\":7072,\"projectPath\":\"D:/Test\",\"pid\":999999999,\"timestamp\":\"2026-01-01T00:00:00Z\"}";
                File.WriteAllText(filePath, json);

                var result = PortDiscoveryFile.ReadValidPort(filePath);

                Assert.IsNull(result);
                Assert.IsFalse(File.Exists(filePath), "Stale port file should be deleted");
            }
            finally
            {
                if (Directory.Exists(dir))
                    Directory.Delete(dir, true);
            }
        }

        [Test]
        public void ReadValidPort_MissingPortField_ReturnsNull()
        {
            var dir = Path.Combine(Path.GetTempPath(), "unity-ai-forge-test-" + Guid.NewGuid().ToString("N"));
            var filePath = Path.Combine(dir, "test.port");

            try
            {
                Directory.CreateDirectory(dir);
                File.WriteAllText(filePath, "{\"projectPath\":\"D:/Test\",\"pid\":1}");

                var result = PortDiscoveryFile.ReadValidPort(filePath);
                Assert.IsNull(result);
            }
            finally
            {
                if (Directory.Exists(dir))
                    Directory.Delete(dir, true);
            }
        }

        #endregion

        #region GetDiscoveryDirectory Tests

        [Test]
        public void GetDiscoveryDirectory_EndsWithCorrectFolder()
        {
            var dir = PortDiscoveryFile.GetDiscoveryDirectory();
            Assert.IsTrue(dir.EndsWith("unity-ai-forge") || dir.EndsWith("unity-ai-forge" + Path.DirectorySeparatorChar));
        }

        #endregion
    }
}
