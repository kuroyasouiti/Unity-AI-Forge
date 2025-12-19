using System;
using System.Collections.Generic;
using System.IO;
using MCP.Editor.Base;
using MCP.Editor.Handlers;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor.Tests
{
    /// <summary>
    /// 配列、ユーザー定義定数、ユーザー定義クラスを組み合わせた複合テスト。
    /// ScriptableObjectのパラメータ操作における複雑なシナリオをカバーします。
    /// </summary>
    [TestFixture]
    public class CombinedTypesScriptableObjectTests
    {
        private ScriptableObjectCommandHandler _handler;
        private List<string> _createdAssets;
        private const string TestTypeName = "MCP.Editor.Tests.TestCombinedTypesScriptableObject";

        [SetUp]
        public void SetUp()
        {
            _handler = new ScriptableObjectCommandHandler();
            _createdAssets = new List<string>();
            ValueConverterManager.ResetInstance();
            TestUtilities.CreateTestDirectory();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var path in _createdAssets)
            {
                if (File.Exists(path))
                {
                    AssetDatabase.DeleteAsset(path);
                }
            }
            _createdAssets.Clear();
            TestUtilities.CleanupTestDirectory();
            ValueConverterManager.ResetInstance();
        }

        #region User-Defined Class Array Tests

        [Test]
        public void Create_WithItemDataArray_Success()
        {
            var assetPath = $"{TestUtilities.TestAssetsPath}/TestItemDataArray.asset";
            _createdAssets.Add(assetPath);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["typeName"] = TestTypeName,
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["itemDataArray"] = new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["itemId"] = 1001,
                            ["itemName"] = "Iron Sword",
                            ["category"] = "Weapon",
                            ["rarity"] = "Common",
                            ["stackCount"] = 1,
                            ["weight"] = 5.5,
                            ["description"] = "A basic iron sword."
                        },
                        new Dictionary<string, object>
                        {
                            ["itemId"] = 2001,
                            ["itemName"] = "Health Potion",
                            ["category"] = "Consumable",
                            ["rarity"] = "Uncommon",
                            ["stackCount"] = GameConstants.MaxInventorySlots,
                            ["weight"] = 0.5,
                            ["description"] = "Restores HP."
                        }
                    }
                }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<TestCombinedTypesScriptableObject>(assetPath);
            Assert.IsNotNull(asset);
            Assert.IsNotNull(asset.itemDataArray);
            Assert.AreEqual(2, asset.itemDataArray.Length);

            // First item verification
            Assert.AreEqual(1001, asset.itemDataArray[0].itemId);
            Assert.AreEqual("Iron Sword", asset.itemDataArray[0].itemName);
            Assert.AreEqual(ItemCategory.Weapon, asset.itemDataArray[0].category);
            Assert.AreEqual(Rarity.Common, asset.itemDataArray[0].rarity);
            Assert.AreEqual(1, asset.itemDataArray[0].stackCount);
            Assert.AreEqual(5.5f, asset.itemDataArray[0].weight, 0.001f);

            // Second item verification with constant reference
            Assert.AreEqual(2001, asset.itemDataArray[1].itemId);
            Assert.AreEqual(ItemCategory.Consumable, asset.itemDataArray[1].category);
            Assert.AreEqual(Rarity.Uncommon, asset.itemDataArray[1].rarity);
            Assert.AreEqual(GameConstants.MaxInventorySlots, asset.itemDataArray[1].stackCount);
        }

        [Test]
        public void Create_WithSkillDataArray_Success()
        {
            var assetPath = $"{TestUtilities.TestAssetsPath}/TestSkillDataArray.asset";
            _createdAssets.Add(assetPath);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["typeName"] = TestTypeName,
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["skillDataArray"] = new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["skillId"] = 100,
                            ["skillName"] = "Fireball",
                            ["cooldown"] = 3.0,
                            ["manaCost"] = 25,
                            ["appliedEffects"] = 2, // Burn
                            ["damageMultiplier"] = GameConstants.CriticalMultiplier,
                            ["effectRange"] = new Dictionary<string, object> { ["x"] = 5.0, ["y"] = 5.0 }
                        },
                        new Dictionary<string, object>
                        {
                            ["skillId"] = 101,
                            ["skillName"] = "Ice Blast",
                            ["cooldown"] = 5.0,
                            ["manaCost"] = 40,
                            ["appliedEffects"] = 4, // Freeze
                            ["damageMultiplier"] = 1.5,
                            ["effectRange"] = new Dictionary<string, object> { ["x"] = 8.0, ["y"] = 3.0 }
                        }
                    }
                }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<TestCombinedTypesScriptableObject>(assetPath);
            Assert.IsNotNull(asset);
            Assert.IsNotNull(asset.skillDataArray);
            Assert.AreEqual(2, asset.skillDataArray.Length);

            // Fireball skill verification
            Assert.AreEqual(100, asset.skillDataArray[0].skillId);
            Assert.AreEqual("Fireball", asset.skillDataArray[0].skillName);
            Assert.AreEqual(3.0f, asset.skillDataArray[0].cooldown, 0.001f);
            Assert.AreEqual(25, asset.skillDataArray[0].manaCost);
            Assert.AreEqual(StatusEffect.Burn, asset.skillDataArray[0].appliedEffects);
            Assert.AreEqual(GameConstants.CriticalMultiplier, asset.skillDataArray[0].damageMultiplier, 0.001f);
            Assert.AreEqual(new Vector2(5.0f, 5.0f), asset.skillDataArray[0].effectRange);

            // Ice Blast skill verification
            Assert.AreEqual(101, asset.skillDataArray[1].skillId);
            Assert.AreEqual(StatusEffect.Freeze, asset.skillDataArray[1].appliedEffects);
        }

        [Test]
        public void Create_WithCharacterBuildList_NestedArrays_Success()
        {
            var assetPath = $"{TestUtilities.TestAssetsPath}/TestCharacterBuildList.asset";
            _createdAssets.Add(assetPath);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["typeName"] = TestTypeName,
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["characterBuildList"] = new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["buildName"] = "Warrior Build",
                            ["level"] = 50,
                            ["stats"] = new Dictionary<string, object>
                            {
                                ["strength"] = 30,
                                ["dexterity"] = 15,
                                ["intelligence"] = 10,
                                ["vitality"] = 25,
                                ["healthMultiplier"] = 1.5,
                                ["manaMultiplier"] = 0.8
                            },
                            ["equippedItemIds"] = new List<object> { 1001, 1002, 1003 },
                            ["learnedSkillIds"] = new List<object> { 100, 101 }
                        },
                        new Dictionary<string, object>
                        {
                            ["buildName"] = "Mage Build",
                            ["level"] = GameConstants.MaxLevel,
                            ["stats"] = new Dictionary<string, object>
                            {
                                ["strength"] = 10,
                                ["dexterity"] = 15,
                                ["intelligence"] = 35,
                                ["vitality"] = 20,
                                ["healthMultiplier"] = 0.8,
                                ["manaMultiplier"] = 2.0
                            },
                            ["equippedItemIds"] = new List<object> { 2001, 2002 },
                            ["learnedSkillIds"] = new List<object> { 200, 201, 202, 203 }
                        }
                    }
                }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<TestCombinedTypesScriptableObject>(assetPath);
            Assert.IsNotNull(asset);
            Assert.IsNotNull(asset.characterBuildList);
            Assert.AreEqual(2, asset.characterBuildList.Count);

            // Warrior Build verification
            var warrior = asset.characterBuildList[0];
            Assert.AreEqual("Warrior Build", warrior.buildName);
            Assert.AreEqual(50, warrior.level);
            Assert.AreEqual(30, warrior.stats.strength);
            Assert.AreEqual(25, warrior.stats.vitality);
            Assert.AreEqual(1.5f, warrior.stats.healthMultiplier, 0.001f);
            Assert.IsNotNull(warrior.equippedItemIds);
            Assert.AreEqual(3, warrior.equippedItemIds.Length);
            Assert.AreEqual(1001, warrior.equippedItemIds[0]);

            // Mage Build verification with constant
            var mage = asset.characterBuildList[1];
            Assert.AreEqual("Mage Build", mage.buildName);
            Assert.AreEqual(GameConstants.MaxLevel, mage.level);
            Assert.AreEqual(35, mage.stats.intelligence);
            Assert.AreEqual(2.0f, mage.stats.manaMultiplier, 0.001f);
            Assert.AreEqual(4, mage.learnedSkillIds.Length);
        }

        #endregion

        #region Complex Struct Array Tests

        [Test]
        public void Create_WithStatBlockArray_Success()
        {
            var assetPath = $"{TestUtilities.TestAssetsPath}/TestStatBlockArray.asset";
            _createdAssets.Add(assetPath);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["typeName"] = TestTypeName,
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["statBlockArray"] = new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["strength"] = 20,
                            ["dexterity"] = 15,
                            ["intelligence"] = 10,
                            ["vitality"] = 25,
                            ["healthMultiplier"] = 1.2,
                            ["manaMultiplier"] = 0.9
                        },
                        new Dictionary<string, object>
                        {
                            ["strength"] = 10,
                            ["dexterity"] = 30,
                            ["intelligence"] = 15,
                            ["vitality"] = 15,
                            ["healthMultiplier"] = 1.0,
                            ["manaMultiplier"] = 1.0
                        }
                    }
                }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<TestCombinedTypesScriptableObject>(assetPath);
            Assert.IsNotNull(asset.statBlockArray);
            Assert.AreEqual(2, asset.statBlockArray.Length);
            Assert.AreEqual(20, asset.statBlockArray[0].strength);
            Assert.AreEqual(30, asset.statBlockArray[1].dexterity);
        }

        [Test]
        public void Create_WithDropTableEntries_Success()
        {
            var assetPath = $"{TestUtilities.TestAssetsPath}/TestDropTable.asset";
            _createdAssets.Add(assetPath);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["typeName"] = TestTypeName,
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["dropTableEntries"] = new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["itemId"] = 1001,
                            ["dropChance"] = 0.1,
                            ["minQuantity"] = 1,
                            ["maxQuantity"] = 1,
                            ["minRarity"] = "Rare"
                        },
                        new Dictionary<string, object>
                        {
                            ["itemId"] = 2001,
                            ["dropChance"] = 0.5,
                            ["minQuantity"] = 1,
                            ["maxQuantity"] = 5,
                            ["minRarity"] = "Common"
                        },
                        new Dictionary<string, object>
                        {
                            ["itemId"] = 3001,
                            ["dropChance"] = 0.01,
                            ["minQuantity"] = 1,
                            ["maxQuantity"] = 1,
                            ["minRarity"] = "Legendary"
                        }
                    }
                }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<TestCombinedTypesScriptableObject>(assetPath);
            Assert.IsNotNull(asset.dropTableEntries);
            Assert.AreEqual(3, asset.dropTableEntries.Length);
            Assert.AreEqual(Rarity.Rare, asset.dropTableEntries[0].minRarity);
            Assert.AreEqual(0.5f, asset.dropTableEntries[1].dropChance, 0.001f);
            Assert.AreEqual(Rarity.Legendary, asset.dropTableEntries[2].minRarity);
        }

        [Test]
        public void Create_WithWaypointsArray_UnityTypesAndPrimitives_Success()
        {
            var assetPath = $"{TestUtilities.TestAssetsPath}/TestWaypoints.asset";
            _createdAssets.Add(assetPath);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["typeName"] = TestTypeName,
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["waypoints"] = new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["waypointName"] = "Spawn Point",
                            ["position"] = new Dictionary<string, object> { ["x"] = 0.0, ["y"] = 1.0, ["z"] = 0.0 },
                            ["waitTime"] = 0.0,
                            ["isTeleportPoint"] = true,
                            ["markerColor"] = new Dictionary<string, object> { ["r"] = 0.0, ["g"] = 1.0, ["b"] = 0.0, ["a"] = 1.0 }
                        },
                        new Dictionary<string, object>
                        {
                            ["waypointName"] = "Town Gate",
                            ["position"] = new Dictionary<string, object> { ["x"] = 100.0, ["y"] = 0.0, ["z"] = 50.0 },
                            ["waitTime"] = 2.0,
                            ["isTeleportPoint"] = false,
                            ["markerColor"] = new Dictionary<string, object> { ["r"] = 1.0, ["g"] = 1.0, ["b"] = 0.0, ["a"] = 1.0 }
                        },
                        new Dictionary<string, object>
                        {
                            ["waypointName"] = "Boss Arena",
                            ["position"] = new Dictionary<string, object> { ["x"] = 500.0, ["y"] = 10.0, ["z"] = 500.0 },
                            ["waitTime"] = 5.0,
                            ["isTeleportPoint"] = true,
                            ["markerColor"] = new Dictionary<string, object> { ["r"] = 1.0, ["g"] = 0.0, ["b"] = 0.0, ["a"] = 1.0 }
                        }
                    }
                }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<TestCombinedTypesScriptableObject>(assetPath);
            Assert.IsNotNull(asset.waypoints);
            Assert.AreEqual(3, asset.waypoints.Length);

            // Spawn Point verification
            Assert.AreEqual("Spawn Point", asset.waypoints[0].waypointName);
            Assert.AreEqual(new Vector3(0, 1, 0), asset.waypoints[0].position);
            Assert.IsTrue(asset.waypoints[0].isTeleportPoint);
            Assert.AreEqual(Color.green, asset.waypoints[0].markerColor);

            // Town Gate verification
            Assert.AreEqual("Town Gate", asset.waypoints[1].waypointName);
            Assert.AreEqual(new Vector3(100, 0, 50), asset.waypoints[1].position);
            Assert.AreEqual(2.0f, asset.waypoints[1].waitTime, 0.001f);
            Assert.IsFalse(asset.waypoints[1].isTeleportPoint);

            // Boss Arena verification
            Assert.AreEqual("Boss Arena", asset.waypoints[2].waypointName);
            Assert.AreEqual(Color.red, asset.waypoints[2].markerColor);
        }

        [Test]
        public void Create_WithDamageInfoArray_ComplexStruct_Success()
        {
            var assetPath = $"{TestUtilities.TestAssetsPath}/TestDamageInfo.asset";
            _createdAssets.Add(assetPath);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["typeName"] = TestTypeName,
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["damageInfoArray"] = new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["baseDamage"] = 100.0,
                            ["criticalChance"] = 0.15,
                            ["criticalMultiplier"] = GameConstants.CriticalMultiplier,
                            ["statusEffects"] = 3, // Poison | Burn
                            ["damageRange"] = new Dictionary<string, object> { ["x"] = 80.0, ["y"] = 120.0 },
                            ["damageColor"] = new Dictionary<string, object> { ["r"] = 1.0, ["g"] = 0.5, ["b"] = 0.0, ["a"] = 1.0 }
                        },
                        new Dictionary<string, object>
                        {
                            ["baseDamage"] = 50.0,
                            ["criticalChance"] = 0.25,
                            ["criticalMultiplier"] = 1.75,
                            ["statusEffects"] = 4, // Freeze
                            ["damageRange"] = new Dictionary<string, object> { ["x"] = 40.0, ["y"] = 60.0 },
                            ["damageColor"] = new Dictionary<string, object> { ["r"] = 0.0, ["g"] = 0.5, ["b"] = 1.0, ["a"] = 1.0 }
                        }
                    }
                }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<TestCombinedTypesScriptableObject>(assetPath);
            Assert.IsNotNull(asset.damageInfoArray);
            Assert.AreEqual(2, asset.damageInfoArray.Length);

            // Fire damage verification
            Assert.AreEqual(100.0f, asset.damageInfoArray[0].baseDamage, 0.001f);
            Assert.AreEqual(0.15f, asset.damageInfoArray[0].criticalChance, 0.001f);
            Assert.AreEqual(GameConstants.CriticalMultiplier, asset.damageInfoArray[0].criticalMultiplier, 0.001f);
            Assert.AreEqual(StatusEffect.Poison | StatusEffect.Burn, asset.damageInfoArray[0].statusEffects);
            Assert.AreEqual(new Vector2(80, 120), asset.damageInfoArray[0].damageRange);

            // Ice damage verification
            Assert.AreEqual(StatusEffect.Freeze, asset.damageInfoArray[1].statusEffects);
        }

        [Test]
        public void Create_WithSpawnRules_Success()
        {
            var assetPath = $"{TestUtilities.TestAssetsPath}/TestSpawnRules.asset";
            _createdAssets.Add(assetPath);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["typeName"] = TestTypeName,
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["spawnRules"] = new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["ruleName"] = "Goblin Camp",
                            ["spawnPosition"] = new Dictionary<string, object> { ["x"] = 100.0, ["y"] = 0.0, ["z"] = 100.0 },
                            ["spawnRadius"] = 15.0,
                            ["minCount"] = 3,
                            ["maxCount"] = 8,
                            ["respawnTime"] = 60.0,
                            ["isActive"] = true
                        },
                        new Dictionary<string, object>
                        {
                            ["ruleName"] = "Treasure Room",
                            ["spawnPosition"] = new Dictionary<string, object> { ["x"] = 200.0, ["y"] = -5.0, ["z"] = 300.0 },
                            ["spawnRadius"] = 5.0,
                            ["minCount"] = 1,
                            ["maxCount"] = 1,
                            ["respawnTime"] = 3600.0,
                            ["isActive"] = false
                        }
                    }
                }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<TestCombinedTypesScriptableObject>(assetPath);
            Assert.IsNotNull(asset.spawnRules);
            Assert.AreEqual(2, asset.spawnRules.Length);

            Assert.AreEqual("Goblin Camp", asset.spawnRules[0].ruleName);
            Assert.AreEqual(new Vector3(100, 0, 100), asset.spawnRules[0].spawnPosition);
            Assert.AreEqual(15.0f, asset.spawnRules[0].spawnRadius, 0.001f);
            Assert.IsTrue(asset.spawnRules[0].isActive);

            Assert.AreEqual("Treasure Room", asset.spawnRules[1].ruleName);
            Assert.AreEqual(3600.0f, asset.spawnRules[1].respawnTime, 0.001f);
            Assert.IsFalse(asset.spawnRules[1].isActive);
        }

        #endregion

        #region Combined Mixed Type Tests

        [Test]
        public void Create_WithAllMixedTypes_Success()
        {
            var assetPath = $"{TestUtilities.TestAssetsPath}/TestAllMixedTypes.asset";
            _createdAssets.Add(assetPath);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["typeName"] = TestTypeName,
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    // Primitive types with constants
                    ["playerLevel"] = GameConstants.MaxLevel,
                    ["playerName"] = GameConstants.DefaultPlayerName,
                    ["moveSpeed"] = GameConstants.DefaultMoveSpeed,

                    // Enum types
                    ["defaultCategory"] = "Weapon",
                    ["defaultRarity"] = "Epic",

                    // Unity struct types
                    ["homePosition"] = new Dictionary<string, object> { ["x"] = 0.0, ["y"] = 1.0, ["z"] = 0.0 },
                    ["playerColor"] = new Dictionary<string, object> { ["r"] = 0.2, ["g"] = 0.4, ["b"] = 0.8, ["a"] = 1.0 },

                    // User-defined class array
                    ["itemDataArray"] = new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["itemId"] = 9999,
                            ["itemName"] = "Legendary Sword",
                            ["category"] = "Weapon",
                            ["rarity"] = "Legendary",
                            ["stackCount"] = 1,
                            ["weight"] = 10.0,
                            ["description"] = "A sword of legends."
                        }
                    },

                    // Complex struct array
                    ["waypoints"] = new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["waypointName"] = "Home",
                            ["position"] = new Dictionary<string, object> { ["x"] = 0.0, ["y"] = 1.0, ["z"] = 0.0 },
                            ["waitTime"] = 0.0,
                            ["isTeleportPoint"] = true,
                            ["markerColor"] = new Dictionary<string, object> { ["r"] = 0.0, ["g"] = 1.0, ["b"] = 0.0, ["a"] = 1.0 }
                        }
                    },

                    // Enum arrays
                    ["categories"] = new List<object> { "Weapon", "Armor", "Consumable" },
                    ["rarities"] = new List<object> { "Common", "Rare", "Legendary" },
                    ["statusEffects"] = new List<object> { 1, 2, 4 } // Poison, Burn, Freeze as int values
                }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<TestCombinedTypesScriptableObject>(assetPath);
            Assert.IsNotNull(asset);

            // Verify primitives with constants
            Assert.AreEqual(GameConstants.MaxLevel, asset.playerLevel);
            Assert.AreEqual(GameConstants.DefaultPlayerName, asset.playerName);
            Assert.AreEqual(GameConstants.DefaultMoveSpeed, asset.moveSpeed, 0.001f);

            // Verify enums
            Assert.AreEqual(ItemCategory.Weapon, asset.defaultCategory);
            Assert.AreEqual(Rarity.Epic, asset.defaultRarity);

            // Verify Unity types
            Assert.AreEqual(new Vector3(0, 1, 0), asset.homePosition);
            Assert.AreEqual(0.2f, asset.playerColor.r, 0.001f);

            // Verify user-defined class array
            Assert.AreEqual(1, asset.itemDataArray.Length);
            Assert.AreEqual(Rarity.Legendary, asset.itemDataArray[0].rarity);

            // Verify complex struct array
            Assert.AreEqual(1, asset.waypoints.Length);
            Assert.AreEqual("Home", asset.waypoints[0].waypointName);

            // Verify enum arrays
            Assert.AreEqual(3, asset.categories.Length);
            Assert.AreEqual(ItemCategory.Weapon, asset.categories[0]);
            Assert.AreEqual(ItemCategory.Armor, asset.categories[1]);

            Assert.AreEqual(3, asset.rarities.Count);
            Assert.AreEqual(Rarity.Common, asset.rarities[0]);
            Assert.AreEqual(Rarity.Legendary, asset.rarities[2]);
        }

        #endregion

        #region Update Tests

        [Test]
        public void Update_ItemDataArray_Success()
        {
            var assetPath = $"{TestUtilities.TestAssetsPath}/TestUpdateItemDataArray.asset";
            _createdAssets.Add(assetPath);

            // Create initial asset
            var createPayload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["typeName"] = TestTypeName,
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["itemDataArray"] = new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["itemId"] = 1,
                            ["itemName"] = "Old Item",
                            ["category"] = "Material",
                            ["rarity"] = "Common",
                            ["stackCount"] = 1,
                            ["weight"] = 1.0,
                            ["description"] = "Old description"
                        }
                    }
                }
            };
            _handler.Execute(createPayload);

            // Update with new data
            var updatePayload = new Dictionary<string, object>
            {
                ["operation"] = "update",
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["itemDataArray"] = new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["itemId"] = 100,
                            ["itemName"] = "New Sword",
                            ["category"] = "Weapon",
                            ["rarity"] = "Epic",
                            ["stackCount"] = 1,
                            ["weight"] = 8.0,
                            ["description"] = "A powerful new sword"
                        },
                        new Dictionary<string, object>
                        {
                            ["itemId"] = 200,
                            ["itemName"] = "New Shield",
                            ["category"] = "Armor",
                            ["rarity"] = "Rare",
                            ["stackCount"] = 1,
                            ["weight"] = 12.0,
                            ["description"] = "A sturdy shield"
                        }
                    }
                }
            };

            var result = _handler.Execute(updatePayload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<TestCombinedTypesScriptableObject>(assetPath);
            Assert.AreEqual(2, asset.itemDataArray.Length);
            Assert.AreEqual(100, asset.itemDataArray[0].itemId);
            Assert.AreEqual("New Sword", asset.itemDataArray[0].itemName);
            Assert.AreEqual(ItemCategory.Weapon, asset.itemDataArray[0].category);
            Assert.AreEqual(Rarity.Epic, asset.itemDataArray[0].rarity);
            Assert.AreEqual(200, asset.itemDataArray[1].itemId);
            Assert.AreEqual(ItemCategory.Armor, asset.itemDataArray[1].category);
        }

        [Test]
        public void Update_WaypointList_Success()
        {
            var assetPath = $"{TestUtilities.TestAssetsPath}/TestUpdateWaypointList.asset";
            _createdAssets.Add(assetPath);

            var createPayload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["typeName"] = TestTypeName,
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["waypointList"] = new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["waypointName"] = "Old Point",
                            ["position"] = new Dictionary<string, object> { ["x"] = 0.0, ["y"] = 0.0, ["z"] = 0.0 },
                            ["waitTime"] = 0.0,
                            ["isTeleportPoint"] = false,
                            ["markerColor"] = new Dictionary<string, object> { ["r"] = 1.0, ["g"] = 1.0, ["b"] = 1.0, ["a"] = 1.0 }
                        }
                    }
                }
            };
            _handler.Execute(createPayload);

            var updatePayload = new Dictionary<string, object>
            {
                ["operation"] = "update",
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["waypointList"] = new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["waypointName"] = "Start",
                            ["position"] = new Dictionary<string, object> { ["x"] = 10.0, ["y"] = 5.0, ["z"] = 20.0 },
                            ["waitTime"] = 1.0,
                            ["isTeleportPoint"] = true,
                            ["markerColor"] = new Dictionary<string, object> { ["r"] = 0.0, ["g"] = 1.0, ["b"] = 0.0, ["a"] = 1.0 }
                        },
                        new Dictionary<string, object>
                        {
                            ["waypointName"] = "End",
                            ["position"] = new Dictionary<string, object> { ["x"] = 100.0, ["y"] = 0.0, ["z"] = 100.0 },
                            ["waitTime"] = 0.0,
                            ["isTeleportPoint"] = true,
                            ["markerColor"] = new Dictionary<string, object> { ["r"] = 1.0, ["g"] = 0.0, ["b"] = 0.0, ["a"] = 1.0 }
                        }
                    }
                }
            };

            var result = _handler.Execute(updatePayload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<TestCombinedTypesScriptableObject>(assetPath);
            Assert.AreEqual(2, asset.waypointList.Count);
            Assert.AreEqual("Start", asset.waypointList[0].waypointName);
            Assert.AreEqual(new Vector3(10, 5, 20), asset.waypointList[0].position);
            Assert.AreEqual(Color.green, asset.waypointList[0].markerColor);
            Assert.AreEqual("End", asset.waypointList[1].waypointName);
            Assert.AreEqual(Color.red, asset.waypointList[1].markerColor);
        }

        #endregion

        #region Inspect Tests

        [Test]
        public void Inspect_WithItemDataArray_ReturnsClassDictionaries()
        {
            var assetPath = $"{TestUtilities.TestAssetsPath}/TestInspectItemDataArray.asset";
            _createdAssets.Add(assetPath);

            var createPayload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["typeName"] = TestTypeName,
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["itemDataArray"] = new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["itemId"] = 12345,
                            ["itemName"] = "Inspect Test Item",
                            ["category"] = "Quest",
                            ["rarity"] = "Rare",
                            ["stackCount"] = 10,
                            ["weight"] = 0.1,
                            ["description"] = "For inspection testing"
                        }
                    }
                }
            };
            _handler.Execute(createPayload);

            var inspectPayload = new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["assetPath"] = assetPath,
                ["includeProperties"] = true,
                ["propertyFilter"] = new List<object> { "itemDataArray" }
            };

            var result = _handler.Execute(inspectPayload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var properties = result["properties"] as Dictionary<string, object>;
            Assert.IsNotNull(properties);
            Assert.IsTrue(properties.ContainsKey("itemDataArray"));

            var itemDataArrayValue = properties["itemDataArray"] as List<object>;
            Assert.IsNotNull(itemDataArrayValue);
            Assert.AreEqual(1, itemDataArrayValue.Count);

            var itemDict = itemDataArrayValue[0] as Dictionary<string, object>;
            Assert.IsNotNull(itemDict);
            Assert.AreEqual(12345, Convert.ToInt32(itemDict["itemId"]));
            Assert.AreEqual("Inspect Test Item", itemDict["itemName"]);
            Assert.AreEqual("Quest", itemDict["category"].ToString());
            Assert.AreEqual("Rare", itemDict["rarity"].ToString());
        }

        [Test]
        public void Inspect_WithWaypointArray_ReturnsNestedStructures()
        {
            var assetPath = $"{TestUtilities.TestAssetsPath}/TestInspectWaypointArray.asset";
            _createdAssets.Add(assetPath);

            var createPayload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["typeName"] = TestTypeName,
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["waypoints"] = new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["waypointName"] = "Inspect Waypoint",
                            ["position"] = new Dictionary<string, object> { ["x"] = 50.0, ["y"] = 25.0, ["z"] = 75.0 },
                            ["waitTime"] = 3.5,
                            ["isTeleportPoint"] = true,
                            ["markerColor"] = new Dictionary<string, object> { ["r"] = 0.5, ["g"] = 0.5, ["b"] = 0.5, ["a"] = 1.0 }
                        }
                    }
                }
            };
            _handler.Execute(createPayload);

            var inspectPayload = new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["assetPath"] = assetPath,
                ["includeProperties"] = true,
                ["propertyFilter"] = new List<object> { "waypoints" }
            };

            var result = _handler.Execute(inspectPayload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            var properties = result["properties"] as Dictionary<string, object>;
            var waypointsValue = properties["waypoints"] as List<object>;

            Assert.IsNotNull(waypointsValue);
            Assert.AreEqual(1, waypointsValue.Count);

            var waypointDict = waypointsValue[0] as Dictionary<string, object>;
            Assert.IsNotNull(waypointDict);
            Assert.AreEqual("Inspect Waypoint", waypointDict["waypointName"]);
            Assert.AreEqual(3.5f, Convert.ToSingle(waypointDict["waitTime"]), 0.001f);
            Assert.AreEqual(true, waypointDict["isTeleportPoint"]);

            // Verify nested Vector3
            var positionDict = waypointDict["position"] as Dictionary<string, object>;
            Assert.IsNotNull(positionDict);
            Assert.AreEqual(50.0f, Convert.ToSingle(positionDict["x"]), 0.001f);
            Assert.AreEqual(25.0f, Convert.ToSingle(positionDict["y"]), 0.001f);
            Assert.AreEqual(75.0f, Convert.ToSingle(positionDict["z"]), 0.001f);
        }

        [Test]
        public void Inspect_WithMixedProperties_ReturnsAllTypes()
        {
            var assetPath = $"{TestUtilities.TestAssetsPath}/TestInspectMixed.asset";
            _createdAssets.Add(assetPath);

            var createPayload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["typeName"] = TestTypeName,
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["playerLevel"] = 50,
                    ["playerName"] = "TestPlayer",
                    ["moveSpeed"] = 7.5,
                    ["defaultCategory"] = "Armor",
                    ["defaultRarity"] = "Epic",
                    ["homePosition"] = new Dictionary<string, object> { ["x"] = 1.0, ["y"] = 2.0, ["z"] = 3.0 },
                    ["categories"] = new List<object> { "Weapon", "Armor" }
                }
            };
            _handler.Execute(createPayload);

            var inspectPayload = new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["assetPath"] = assetPath,
                ["includeProperties"] = true,
                ["propertyFilter"] = new List<object>
                {
                    "playerLevel", "playerName", "moveSpeed",
                    "defaultCategory", "defaultRarity", "homePosition", "categories"
                }
            };

            var result = _handler.Execute(inspectPayload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            var properties = result["properties"] as Dictionary<string, object>;

            // Verify primitives
            Assert.AreEqual(50, Convert.ToInt32(properties["playerLevel"]));
            Assert.AreEqual("TestPlayer", properties["playerName"]);
            Assert.AreEqual(7.5f, Convert.ToSingle(properties["moveSpeed"]), 0.001f);

            // Verify enums (returned as strings)
            Assert.AreEqual("Armor", properties["defaultCategory"].ToString());
            Assert.AreEqual("Epic", properties["defaultRarity"].ToString());

            // Verify Unity struct
            var homePositionDict = properties["homePosition"] as Dictionary<string, object>;
            Assert.IsNotNull(homePositionDict);
            Assert.AreEqual(1.0f, Convert.ToSingle(homePositionDict["x"]), 0.001f);

            // Verify enum array
            var categoriesValue = properties["categories"] as List<object>;
            Assert.IsNotNull(categoriesValue);
            Assert.AreEqual(2, categoriesValue.Count);
            Assert.AreEqual("Weapon", categoriesValue[0].ToString());
            Assert.AreEqual("Armor", categoriesValue[1].ToString());
        }

        #endregion

        #region Edge Case Tests

        [Test]
        public void Create_WithEmptyClassArray_Success()
        {
            var assetPath = $"{TestUtilities.TestAssetsPath}/TestEmptyClassArray.asset";
            _createdAssets.Add(assetPath);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["typeName"] = TestTypeName,
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["itemDataArray"] = new List<object>()
                }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<TestCombinedTypesScriptableObject>(assetPath);
            Assert.IsNotNull(asset.itemDataArray);
            Assert.AreEqual(0, asset.itemDataArray.Length);
        }

        [Test]
        public void Create_WithLargeItemDataArray_Success()
        {
            var assetPath = $"{TestUtilities.TestAssetsPath}/TestLargeItemDataArray.asset";
            _createdAssets.Add(assetPath);

            var categories = new[] { "None", "Weapon", "Armor", "Consumable", "Material" };
            var rarities = new[] { "Common", "Uncommon", "Rare", "Epic", "Legendary" };

            var itemList = new List<object>();
            for (int i = 0; i < GameConstants.MaxInventorySlots; i++)
            {
                itemList.Add(new Dictionary<string, object>
                {
                    ["itemId"] = i + 1,
                    ["itemName"] = $"Item_{i + 1}",
                    ["category"] = categories[i % 5],
                    ["rarity"] = rarities[i % 5],
                    ["stackCount"] = (i % 10) + 1,
                    ["weight"] = 1.0 + (i * 0.1),
                    ["description"] = $"Description for item {i + 1}"
                });
            }

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["typeName"] = TestTypeName,
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["itemDataArray"] = itemList
                }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<TestCombinedTypesScriptableObject>(assetPath);
            Assert.IsNotNull(asset.itemDataArray);
            Assert.AreEqual(GameConstants.MaxInventorySlots, asset.itemDataArray.Length);

            // Verify first and last items
            Assert.AreEqual(1, asset.itemDataArray[0].itemId);
            Assert.AreEqual("Item_1", asset.itemDataArray[0].itemName);
            Assert.AreEqual(GameConstants.MaxInventorySlots, asset.itemDataArray[GameConstants.MaxInventorySlots - 1].itemId);
        }

        [Test]
        public void Create_WithPartialClassData_DefaultValuesApplied()
        {
            var assetPath = $"{TestUtilities.TestAssetsPath}/TestPartialClassData.asset";
            _createdAssets.Add(assetPath);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["typeName"] = TestTypeName,
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["itemDataArray"] = new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            // Only provide some fields
                            ["itemId"] = 1,
                            ["itemName"] = "Partial Item"
                            // Other fields should be default values
                        }
                    }
                }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<TestCombinedTypesScriptableObject>(assetPath);
            Assert.AreEqual(1, asset.itemDataArray.Length);
            Assert.AreEqual(1, asset.itemDataArray[0].itemId);
            Assert.AreEqual("Partial Item", asset.itemDataArray[0].itemName);
            // Default values
            Assert.AreEqual(ItemCategory.None, asset.itemDataArray[0].category);
            Assert.AreEqual(Rarity.Common, asset.itemDataArray[0].rarity);
            Assert.AreEqual(0, asset.itemDataArray[0].stackCount);
        }

        [Test]
        public void Create_WithFlagsEnum_StatusEffects_Success()
        {
            var assetPath = $"{TestUtilities.TestAssetsPath}/TestFlagsEnum.asset";
            _createdAssets.Add(assetPath);

            // Test combining multiple flags: Poison | Burn | Freeze = 7
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["typeName"] = TestTypeName,
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["damageInfoArray"] = new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["baseDamage"] = 50.0,
                            ["criticalChance"] = 0.1,
                            ["criticalMultiplier"] = 1.5,
                            ["statusEffects"] = 7, // Poison | Burn | Freeze
                            ["damageRange"] = new Dictionary<string, object> { ["x"] = 40.0, ["y"] = 60.0 },
                            ["damageColor"] = new Dictionary<string, object> { ["r"] = 1.0, ["g"] = 1.0, ["b"] = 1.0, ["a"] = 1.0 }
                        },
                        new Dictionary<string, object>
                        {
                            ["baseDamage"] = 100.0,
                            ["criticalChance"] = 0.2,
                            ["criticalMultiplier"] = 2.0,
                            ["statusEffects"] = 31, // All effects: Poison | Burn | Freeze | Stun | Bleed
                            ["damageRange"] = new Dictionary<string, object> { ["x"] = 90.0, ["y"] = 110.0 },
                            ["damageColor"] = new Dictionary<string, object> { ["r"] = 0.0, ["g"] = 0.0, ["b"] = 0.0, ["a"] = 1.0 }
                        }
                    }
                }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<TestCombinedTypesScriptableObject>(assetPath);
            Assert.AreEqual(2, asset.damageInfoArray.Length);

            // First entry with 3 flags
            var effects1 = asset.damageInfoArray[0].statusEffects;
            Assert.IsTrue(effects1.HasFlag(StatusEffect.Poison));
            Assert.IsTrue(effects1.HasFlag(StatusEffect.Burn));
            Assert.IsTrue(effects1.HasFlag(StatusEffect.Freeze));
            Assert.IsFalse(effects1.HasFlag(StatusEffect.Stun));
            Assert.IsFalse(effects1.HasFlag(StatusEffect.Bleed));

            // Second entry with all flags
            var effects2 = asset.damageInfoArray[1].statusEffects;
            Assert.IsTrue(effects2.HasFlag(StatusEffect.Poison));
            Assert.IsTrue(effects2.HasFlag(StatusEffect.Burn));
            Assert.IsTrue(effects2.HasFlag(StatusEffect.Freeze));
            Assert.IsTrue(effects2.HasFlag(StatusEffect.Stun));
            Assert.IsTrue(effects2.HasFlag(StatusEffect.Bleed));
        }

        #endregion

        #region LayerMask Tests

        [Test]
        public void Create_WithSingleLayerMask_IntegerValue_Success()
        {
            var assetPath = $"{TestUtilities.TestAssetsPath}/TestLayerMaskInt.asset";
            _createdAssets.Add(assetPath);

            // Default layer (0) = 1, UI layer (5) = 32
            // Combined: 1 | 32 = 33
            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["typeName"] = TestTypeName,
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["singleLayerMask"] = 33 // Default + UI
                }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<TestCombinedTypesScriptableObject>(assetPath);
            Assert.IsNotNull(asset);
            Assert.AreEqual(33, asset.singleLayerMask.value);
        }

        [Test]
        public void Create_WithLayerMask_StringConstant_Success()
        {
            var assetPath = $"{TestUtilities.TestAssetsPath}/TestLayerMaskString.asset";
            _createdAssets.Add(assetPath);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["typeName"] = TestTypeName,
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["singleLayerMask"] = "Everything"
                }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<TestCombinedTypesScriptableObject>(assetPath);
            Assert.IsNotNull(asset);
            Assert.AreEqual(~0, asset.singleLayerMask.value); // Everything = all bits set
        }

        [Test]
        public void Create_WithLayerMask_CommaSeparatedLayers_Success()
        {
            var assetPath = $"{TestUtilities.TestAssetsPath}/TestLayerMaskCommaSeparated.asset";
            _createdAssets.Add(assetPath);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["typeName"] = TestTypeName,
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["singleLayerMask"] = "Default, UI" // Layer 0 + Layer 5 = 1 + 32 = 33
                }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<TestCombinedTypesScriptableObject>(assetPath);
            Assert.IsNotNull(asset);
            Assert.AreEqual(33, asset.singleLayerMask.value);
        }

        [Test]
        public void Create_WithLayerMask_DictionaryWithValue_Success()
        {
            var assetPath = $"{TestUtilities.TestAssetsPath}/TestLayerMaskDict.asset";
            _createdAssets.Add(assetPath);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["typeName"] = TestTypeName,
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["singleLayerMask"] = new Dictionary<string, object>
                    {
                        ["value"] = 255 // First 8 layers
                    }
                }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<TestCombinedTypesScriptableObject>(assetPath);
            Assert.IsNotNull(asset);
            Assert.AreEqual(255, asset.singleLayerMask.value);
        }

        [Test]
        public void Create_WithLayerMask_DictionaryWithLayers_Success()
        {
            var assetPath = $"{TestUtilities.TestAssetsPath}/TestLayerMaskDictLayers.asset";
            _createdAssets.Add(assetPath);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["typeName"] = TestTypeName,
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["singleLayerMask"] = new Dictionary<string, object>
                    {
                        ["layers"] = new List<object> { "Default", "UI" }
                    }
                }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<TestCombinedTypesScriptableObject>(assetPath);
            Assert.IsNotNull(asset);
            Assert.AreEqual(33, asset.singleLayerMask.value); // Default (1) + UI (32) = 33
        }

        [Test]
        public void Create_WithLayerMaskArray_Success()
        {
            var assetPath = $"{TestUtilities.TestAssetsPath}/TestLayerMaskArray.asset";
            _createdAssets.Add(assetPath);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["typeName"] = TestTypeName,
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["layerMaskArray"] = new List<object>
                    {
                        1,           // Default layer only
                        "UI",        // UI layer only
                        "Everything" // All layers
                    }
                }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<TestCombinedTypesScriptableObject>(assetPath);
            Assert.IsNotNull(asset);
            Assert.IsNotNull(asset.layerMaskArray);
            Assert.AreEqual(3, asset.layerMaskArray.Length);
            Assert.AreEqual(1, asset.layerMaskArray[0].value);  // Default
            Assert.AreEqual(32, asset.layerMaskArray[1].value); // UI
            Assert.AreEqual(~0, asset.layerMaskArray[2].value); // Everything
        }

        [Test]
        public void Create_WithLayerMaskList_Success()
        {
            var assetPath = $"{TestUtilities.TestAssetsPath}/TestLayerMaskList.asset";
            _createdAssets.Add(assetPath);

            var payload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["typeName"] = TestTypeName,
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["layerMaskList"] = new List<object>
                    {
                        "Nothing",
                        "Default, Water", // Layers 0 and 4 = 1 + 16 = 17
                        new Dictionary<string, object> { ["value"] = 128 }
                    }
                }
            };

            var result = _handler.Execute(payload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<TestCombinedTypesScriptableObject>(assetPath);
            Assert.IsNotNull(asset);
            Assert.IsNotNull(asset.layerMaskList);
            Assert.AreEqual(3, asset.layerMaskList.Count);
            Assert.AreEqual(0, asset.layerMaskList[0].value);   // Nothing
            Assert.AreEqual(17, asset.layerMaskList[1].value);  // Default + Water
            Assert.AreEqual(128, asset.layerMaskList[2].value); // Layer 7
        }

        [Test]
        public void Update_LayerMask_Success()
        {
            var assetPath = $"{TestUtilities.TestAssetsPath}/TestUpdateLayerMask.asset";
            _createdAssets.Add(assetPath);

            var createPayload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["typeName"] = TestTypeName,
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["singleLayerMask"] = 1 // Default layer
                }
            };
            _handler.Execute(createPayload);

            var updatePayload = new Dictionary<string, object>
            {
                ["operation"] = "update",
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["singleLayerMask"] = "Everything"
                }
            };

            var result = _handler.Execute(updatePayload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var asset = AssetDatabase.LoadAssetAtPath<TestCombinedTypesScriptableObject>(assetPath);
            Assert.AreEqual(~0, asset.singleLayerMask.value);
        }

        [Test]
        public void Inspect_WithLayerMask_ReturnsValueAndLayers()
        {
            var assetPath = $"{TestUtilities.TestAssetsPath}/TestInspectLayerMask.asset";
            _createdAssets.Add(assetPath);

            var createPayload = new Dictionary<string, object>
            {
                ["operation"] = "create",
                ["typeName"] = TestTypeName,
                ["assetPath"] = assetPath,
                ["properties"] = new Dictionary<string, object>
                {
                    ["singleLayerMask"] = 33 // Default + UI
                }
            };
            _handler.Execute(createPayload);

            var inspectPayload = new Dictionary<string, object>
            {
                ["operation"] = "inspect",
                ["assetPath"] = assetPath,
                ["includeProperties"] = true,
                ["propertyFilter"] = new List<object> { "singleLayerMask" }
            };

            var result = _handler.Execute(inspectPayload) as Dictionary<string, object>;

            Assert.IsNotNull(result);
            Assert.IsTrue((bool)result["success"]);

            var properties = result["properties"] as Dictionary<string, object>;
            Assert.IsNotNull(properties);
            Assert.IsTrue(properties.ContainsKey("singleLayerMask"));

            var layerMaskValue = properties["singleLayerMask"] as Dictionary<string, object>;
            Assert.IsNotNull(layerMaskValue);
            Assert.AreEqual(33, Convert.ToInt32(layerMaskValue["value"]));

            var layers = layerMaskValue["layers"] as List<object>;
            Assert.IsNotNull(layers);
            Assert.IsTrue(layers.Contains("Default") || layers.Contains("0"));
            Assert.IsTrue(layers.Contains("UI") || layers.Contains("5"));
        }

        #endregion
    }
}
