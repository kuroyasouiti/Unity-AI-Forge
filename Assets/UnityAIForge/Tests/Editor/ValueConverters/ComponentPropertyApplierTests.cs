using System.Collections.Generic;
using MCP.Editor.Base;
using NUnit.Framework;
using UnityEngine;

namespace MCP.Editor.Tests.ValueConverters
{
    /// <summary>
    /// ComponentPropertyApplierのテストクラス。
    /// </summary>
    [TestFixture]
    public class ComponentPropertyApplierTests
    {
        private ComponentPropertyApplier _applier;
        private GameObjectTracker _tracker;

        [SetUp]
        public void SetUp()
        {
            ValueConverterManager.ResetInstance();
            _applier = new ComponentPropertyApplier();
            _tracker = new GameObjectTracker();
        }

        [TearDown]
        public void TearDown()
        {
            _tracker.Dispose();
            ValueConverterManager.ResetInstance();
        }

        #region Transform Property Tests

        [Test]
        public void ApplyProperties_TransformPosition_Success()
        {
            var (go, transform) = _tracker.CreateWithComponent<Transform>("TestObject");

            var properties = new Dictionary<string, object>
            {
                ["position"] = new Dictionary<string, object>
                {
                    ["x"] = 10.0,
                    ["y"] = 20.0,
                    ["z"] = 30.0
                }
            };

            var result = _applier.ApplyProperties(transform, properties);

            Assert.IsTrue(result.AllSucceeded);
            Assert.Contains("position", result.Updated);
            Assert.AreEqual(new Vector3(10f, 20f, 30f), transform.position);
        }

        [Test]
        public void ApplyProperties_TransformLocalScale_Success()
        {
            var (go, transform) = _tracker.CreateWithComponent<Transform>("TestObject");

            var properties = new Dictionary<string, object>
            {
                ["localScale"] = new Dictionary<string, object>
                {
                    ["x"] = 2.0,
                    ["y"] = 2.0,
                    ["z"] = 2.0
                }
            };

            var result = _applier.ApplyProperties(transform, properties);

            Assert.IsTrue(result.AllSucceeded);
            Assert.Contains("localScale", result.Updated);
            Assert.AreEqual(new Vector3(2f, 2f, 2f), transform.localScale);
        }

        #endregion

        #region Rigidbody2D Property Tests

        [Test]
        public void ApplyProperties_Rigidbody2D_GravityScale_Success()
        {
            var (go, rb) = _tracker.CreateWithComponent<Rigidbody2D>("TestRigidbody");

            var properties = new Dictionary<string, object>
            {
                ["gravityScale"] = 0.5
            };

            var result = _applier.ApplyProperties(rb, properties);

            Assert.IsTrue(result.AllSucceeded);
            Assert.Contains("gravityScale", result.Updated);
            Assert.AreEqual(0.5f, rb.gravityScale, 0.001f);
        }

        [Test]
        public void ApplyProperties_Rigidbody2D_Mass_Success()
        {
            var (go, rb) = _tracker.CreateWithComponent<Rigidbody2D>("TestRigidbody");

            var properties = new Dictionary<string, object>
            {
                ["mass"] = 5.0
            };

            var result = _applier.ApplyProperties(rb, properties);

            Assert.IsTrue(result.AllSucceeded);
            Assert.Contains("mass", result.Updated);
            Assert.AreEqual(5f, rb.mass, 0.001f);
        }

        [Test]
        public void ApplyProperties_Rigidbody2D_BodyType_FromString_Success()
        {
            var (go, rb) = _tracker.CreateWithComponent<Rigidbody2D>("TestRigidbody");

            var properties = new Dictionary<string, object>
            {
                ["bodyType"] = "Kinematic"
            };

            var result = _applier.ApplyProperties(rb, properties);

            Assert.IsTrue(result.AllSucceeded);
            Assert.Contains("bodyType", result.Updated);
            Assert.AreEqual(RigidbodyType2D.Kinematic, rb.bodyType);
        }

        [Test]
        public void ApplyProperties_Rigidbody2D_MultipleProperties_Success()
        {
            var (go, rb) = _tracker.CreateWithComponent<Rigidbody2D>("TestRigidbody");

            var properties = new Dictionary<string, object>
            {
                ["mass"] = 2.0,
                ["gravityScale"] = 0.0,
                ["bodyType"] = "Static"
            };

            var result = _applier.ApplyProperties(rb, properties);

            Assert.IsTrue(result.AllSucceeded);
            Assert.AreEqual(3, result.Updated.Count);
            Assert.AreEqual(2f, rb.mass, 0.001f);
            Assert.AreEqual(0f, rb.gravityScale, 0.001f);
            Assert.AreEqual(RigidbodyType2D.Static, rb.bodyType);
        }

        #endregion

        #region BoxCollider2D Property Tests

        [Test]
        public void ApplyProperties_BoxCollider2D_Size_Success()
        {
            var (go, collider) = _tracker.CreateWithComponent<BoxCollider2D>("TestCollider");

            var properties = new Dictionary<string, object>
            {
                ["size"] = new Dictionary<string, object>
                {
                    ["x"] = 2.0,
                    ["y"] = 3.0
                }
            };

            var result = _applier.ApplyProperties(collider, properties);

            Assert.IsTrue(result.AllSucceeded);
            Assert.Contains("size", result.Updated);
            Assert.AreEqual(new Vector2(2f, 3f), collider.size);
        }

        [Test]
        public void ApplyProperties_BoxCollider2D_IsTrigger_Success()
        {
            var (go, collider) = _tracker.CreateWithComponent<BoxCollider2D>("TestCollider");

            var properties = new Dictionary<string, object>
            {
                ["isTrigger"] = true
            };

            var result = _applier.ApplyProperties(collider, properties);

            Assert.IsTrue(result.AllSucceeded);
            Assert.Contains("isTrigger", result.Updated);
            Assert.IsTrue(collider.isTrigger);
        }

        #endregion

        #region Invalid Property Tests

        [Test]
        public void ApplyProperties_NonExistentProperty_Fails()
        {
            var (go, transform) = _tracker.CreateWithComponent<Transform>("TestObject");

            var properties = new Dictionary<string, object>
            {
                ["nonExistentProperty"] = 123
            };

            var result = _applier.ApplyProperties(transform, properties);

            Assert.IsFalse(result.AllSucceeded);
            Assert.Contains("nonExistentProperty", result.Failed);
            Assert.IsTrue(result.Errors.ContainsKey("nonExistentProperty"));
        }

        [Test]
        public void ApplyProperties_MixedValidAndInvalid_PartialSuccess()
        {
            var (go, rb) = _tracker.CreateWithComponent<Rigidbody2D>("TestRigidbody");

            var properties = new Dictionary<string, object>
            {
                ["mass"] = 5.0,
                ["invalidProperty"] = "value"
            };

            var result = _applier.ApplyProperties(rb, properties);

            Assert.IsFalse(result.AllSucceeded);
            Assert.Contains("mass", result.Updated);
            Assert.Contains("invalidProperty", result.Failed);
            Assert.AreEqual(5f, rb.mass, 0.001f);
        }

        #endregion

        #region Empty Properties Tests

        [Test]
        public void ApplyProperties_EmptyDictionary_ReturnsEmptyResult()
        {
            var (go, transform) = _tracker.CreateWithComponent<Transform>("TestObject");

            var properties = new Dictionary<string, object>();

            var result = _applier.ApplyProperties(transform, properties);

            Assert.IsTrue(result.AllSucceeded);
            Assert.AreEqual(0, result.Updated.Count);
            Assert.AreEqual(0, result.Failed.Count);
        }

        #endregion

        #region SpriteRenderer Property Tests

        [Test]
        public void ApplyProperties_SpriteRenderer_Color_Success()
        {
            var (go, renderer) = _tracker.CreateWithComponent<SpriteRenderer>("TestSprite");

            var properties = new Dictionary<string, object>
            {
                ["color"] = new Dictionary<string, object>
                {
                    ["r"] = 1.0,
                    ["g"] = 0.0,
                    ["b"] = 0.0,
                    ["a"] = 1.0
                }
            };

            var result = _applier.ApplyProperties(renderer, properties);

            Assert.IsTrue(result.AllSucceeded);
            Assert.Contains("color", result.Updated);
            Assert.AreEqual(Color.red, renderer.color);
        }

        [Test]
        public void ApplyProperties_SpriteRenderer_FlipX_Success()
        {
            var (go, renderer) = _tracker.CreateWithComponent<SpriteRenderer>("TestSprite");

            var properties = new Dictionary<string, object>
            {
                ["flipX"] = true
            };

            var result = _applier.ApplyProperties(renderer, properties);

            Assert.IsTrue(result.AllSucceeded);
            Assert.Contains("flipX", result.Updated);
            Assert.IsTrue(renderer.flipX);
        }

        #endregion

        #region AudioSource Property Tests

        [Test]
        public void ApplyProperties_AudioSource_Volume_Success()
        {
            var (go, audio) = _tracker.CreateWithComponent<AudioSource>("TestAudio");

            var properties = new Dictionary<string, object>
            {
                ["volume"] = 0.5
            };

            var result = _applier.ApplyProperties(audio, properties);

            Assert.IsTrue(result.AllSucceeded);
            Assert.Contains("volume", result.Updated);
            Assert.AreEqual(0.5f, audio.volume, 0.001f);
        }

        [Test]
        public void ApplyProperties_AudioSource_Loop_Success()
        {
            var (go, audio) = _tracker.CreateWithComponent<AudioSource>("TestAudio");

            var properties = new Dictionary<string, object>
            {
                ["loop"] = true
            };

            var result = _applier.ApplyProperties(audio, properties);

            Assert.IsTrue(result.AllSucceeded);
            Assert.Contains("loop", result.Updated);
            Assert.IsTrue(audio.loop);
        }

        [Test]
        public void ApplyProperties_AudioSource_Pitch_Success()
        {
            var (go, audio) = _tracker.CreateWithComponent<AudioSource>("TestAudio");

            var properties = new Dictionary<string, object>
            {
                ["pitch"] = 1.5
            };

            var result = _applier.ApplyProperties(audio, properties);

            Assert.IsTrue(result.AllSucceeded);
            Assert.Contains("pitch", result.Updated);
            Assert.AreEqual(1.5f, audio.pitch, 0.001f);
        }

        #endregion

        #region Null Argument Tests

        [Test]
        public void ApplyProperties_NullComponent_ThrowsException()
        {
            var properties = new Dictionary<string, object>
            {
                ["mass"] = 5.0
            };

            Assert.Throws<System.ArgumentNullException>(() =>
            {
                _applier.ApplyProperties(null, properties);
            });
        }

        [Test]
        public void ApplyProperties_NullProperties_ThrowsException()
        {
            var (go, transform) = _tracker.CreateWithComponent<Transform>("TestObject");

            Assert.Throws<System.ArgumentNullException>(() =>
            {
                _applier.ApplyProperties(transform, null);
            });
        }

        #endregion

        #region Prefab Reference Tests

        [Test]
        public void ApplyProperties_PrefabReference_WithStringPath_Success()
        {
            var (go, component) = _tracker.CreateWithComponent<TestPrefabReferenceComponent>("TestObject");

            // Create a test prefab
            var prefabGo = new GameObject("TestPrefabForProperty");
            var prefabPath = "Assets/TestTemp/TestPrefabForProperty.prefab";

            try
            {
                if (!System.IO.Directory.Exists("Assets/TestTemp"))
                {
                    System.IO.Directory.CreateDirectory("Assets/TestTemp");
                    UnityEditor.AssetDatabase.Refresh();
                }

                UnityEditor.PrefabUtility.SaveAsPrefabAsset(prefabGo, prefabPath);
                UnityEditor.AssetDatabase.Refresh();

                var properties = new Dictionary<string, object>
                {
                    ["prefabReference"] = prefabPath
                };

                var result = _applier.ApplyProperties(component, properties);

                Assert.IsTrue(result.AllSucceeded);
                Assert.Contains("prefabReference", result.Updated);
                Assert.IsNotNull(component.prefabReference);
                Assert.AreEqual("TestPrefabForProperty", component.prefabReference.name);
            }
            finally
            {
                Object.DestroyImmediate(prefabGo);
                UnityEditor.AssetDatabase.DeleteAsset(prefabPath);
                UnityEditor.AssetDatabase.DeleteAsset("Assets/TestTemp");
            }
        }

        [Test]
        public void ApplyProperties_PrefabReference_WithRefFormat_Success()
        {
            var (go, component) = _tracker.CreateWithComponent<TestPrefabReferenceComponent>("TestObject");

            // Create a test prefab
            var prefabGo = new GameObject("TestPrefabRefFormat");
            var prefabPath = "Assets/TestTemp/TestPrefabRefFormat.prefab";

            try
            {
                if (!System.IO.Directory.Exists("Assets/TestTemp"))
                {
                    System.IO.Directory.CreateDirectory("Assets/TestTemp");
                    UnityEditor.AssetDatabase.Refresh();
                }

                UnityEditor.PrefabUtility.SaveAsPrefabAsset(prefabGo, prefabPath);
                UnityEditor.AssetDatabase.Refresh();

                var properties = new Dictionary<string, object>
                {
                    ["prefabReference"] = new Dictionary<string, object>
                    {
                        ["$ref"] = prefabPath
                    }
                };

                var result = _applier.ApplyProperties(component, properties);

                Assert.IsTrue(result.AllSucceeded);
                Assert.Contains("prefabReference", result.Updated);
                Assert.IsNotNull(component.prefabReference);
                Assert.AreEqual("TestPrefabRefFormat", component.prefabReference.name);
            }
            finally
            {
                Object.DestroyImmediate(prefabGo);
                UnityEditor.AssetDatabase.DeleteAsset(prefabPath);
                UnityEditor.AssetDatabase.DeleteAsset("Assets/TestTemp");
            }
        }

        [Test]
        public void ApplyProperties_MaterialReference_WithStringPath_Success()
        {
            var (go, component) = _tracker.CreateWithComponent<TestPrefabReferenceComponent>("TestObject");

            // Create a test material
            var prefabPath = "Assets/TestTemp/TestMaterial.mat";

            try
            {
                if (!System.IO.Directory.Exists("Assets/TestTemp"))
                {
                    System.IO.Directory.CreateDirectory("Assets/TestTemp");
                    UnityEditor.AssetDatabase.Refresh();
                }

                var material = new Material(Shader.Find("Standard"));
                UnityEditor.AssetDatabase.CreateAsset(material, prefabPath);
                UnityEditor.AssetDatabase.Refresh();

                var properties = new Dictionary<string, object>
                {
                    ["materialReference"] = prefabPath
                };

                var result = _applier.ApplyProperties(component, properties);

                Assert.IsTrue(result.AllSucceeded);
                Assert.Contains("materialReference", result.Updated);
                Assert.IsNotNull(component.materialReference);
            }
            finally
            {
                UnityEditor.AssetDatabase.DeleteAsset(prefabPath);
                UnityEditor.AssetDatabase.DeleteAsset("Assets/TestTemp");
            }
        }

        [Test]
        public void ApplyProperties_PrivatePrefabReference_WithSerializeField_Success()
        {
            var (go, component) = _tracker.CreateWithComponent<TestPrefabReferenceComponent>("TestObject");

            // Create a test prefab
            var prefabGo = new GameObject("TestPrivatePrefab");
            var prefabPath = "Assets/TestTemp/TestPrivatePrefab.prefab";

            try
            {
                if (!System.IO.Directory.Exists("Assets/TestTemp"))
                {
                    System.IO.Directory.CreateDirectory("Assets/TestTemp");
                    UnityEditor.AssetDatabase.Refresh();
                }

                UnityEditor.PrefabUtility.SaveAsPrefabAsset(prefabGo, prefabPath);
                UnityEditor.AssetDatabase.Refresh();

                // Access private field via [SerializeField]
                var properties = new Dictionary<string, object>
                {
                    ["_privatePrefabReference"] = prefabPath
                };

                var result = _applier.ApplyProperties(component, properties);

                Assert.IsTrue(result.AllSucceeded);
                Assert.Contains("_privatePrefabReference", result.Updated);
                Assert.IsNotNull(component.PrivatePrefabReference);
                Assert.AreEqual("TestPrivatePrefab", component.PrivatePrefabReference.name);
            }
            finally
            {
                Object.DestroyImmediate(prefabGo);
                UnityEditor.AssetDatabase.DeleteAsset(prefabPath);
                UnityEditor.AssetDatabase.DeleteAsset("Assets/TestTemp");
            }
        }

        [Test]
        public void ApplyProperties_SceneObjectReference_WithPath_Success()
        {
            var (go, component) = _tracker.CreateWithComponent<TestPrefabReferenceComponent>("TestObject");

            // Create a scene object with Transform (not a prefab)
            var targetGo = _tracker.Create("TargetSceneObject");
            var targetRb = targetGo.AddComponent<Rigidbody2D>();

            // Use scene object path (without "Assets/" prefix)
            var properties = new Dictionary<string, object>
            {
                ["transformReference"] = "TargetSceneObject",
                ["rigidbodyReference"] = "TargetSceneObject"
            };

            var result = _applier.ApplyProperties(component, properties);

            Assert.IsTrue(result.AllSucceeded);
            Assert.Contains("transformReference", result.Updated);
            Assert.Contains("rigidbodyReference", result.Updated);
            Assert.AreSame(targetGo.transform, component.transformReference);
            Assert.AreSame(targetRb, component.rigidbodyReference);
        }

        [Test]
        public void ApplyProperties_InvalidPrefabPath_SetsNull()
        {
            var (go, component) = _tracker.CreateWithComponent<TestPrefabReferenceComponent>("TestObject");

            var properties = new Dictionary<string, object>
            {
                ["prefabReference"] = "Assets/NonExistent/Prefab.prefab"
            };

            var result = _applier.ApplyProperties(component, properties);

            // Property is updated (to null), so it should succeed
            Assert.IsTrue(result.AllSucceeded);
            Assert.Contains("prefabReference", result.Updated);
            Assert.IsNull(component.prefabReference);
        }

        #endregion
    }
}
