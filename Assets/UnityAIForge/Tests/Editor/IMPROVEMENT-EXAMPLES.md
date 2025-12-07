# テスト改善実装例

このドキュメントでは、`EditorTestBase`と`TestHelpers`を使用した改善例を示します。

---

## 📋 目次

1. [EditorTestBase の使用方法](#editortestbase-の使用方法)
2. [TestHelpers の使用方法](#testhelpers-の使用方法)
3. [Before/After 比較](#beforeafter-比較)
4. [移行ガイド](#移行ガイド)

---

## EditorTestBase の使用方法

### 基本的な使い方

```csharp
using NUnit.Framework;
using UnityEngine;

namespace UnityAIForge.Tests.Editor
{
    [TestFixture]
    public class MyImprovedTests : EditorTestBase
    {
        [Test]
        public void SimpleTest()
        {
            // Arrange - CreateTestGameObject を使うと自動的にクリーンアップされる
            var go = CreateTestGameObject("TestObject");
            
            // Act
            go.transform.position = Vector3.one;
            
            // Assert
            Assert.AreEqual(Vector3.one, go.transform.position);
            
            // Teardownで自動的に破棄される（手動でDestroyImmediate不要）
        }
    }
}
```

### コンポーネント付きGameObjectの作成

```csharp
[Test]
public void TestWithComponent()
{
    // 方法1: CreateTestGameObjectWith (推奨)
    var (go, rigidbody) = CreateTestGameObjectWith<Rigidbody>("PhysicsObject");
    rigidbody.mass = 2.0f;
    
    Assert.AreEqual(2.0f, rigidbody.mass);
}

[Test]
public void TestWithMultipleComponents()
{
    // 方法2: 型を指定して作成
    var go = CreateTestGameObject("ComplexObject", typeof(Rigidbody), typeof(BoxCollider));
    
    AssertHasComponent<Rigidbody>(go);
    AssertHasComponent<BoxCollider>(go);
}
```

### 既存オブジェクトの追跡

```csharp
[Test]
public void TestWithExternalObject()
{
    // 外部で作成されたオブジェクトを追跡
    var externalGo = new GameObject("External");
    TrackTestObject(externalGo); // クリーンアップリストに追加
    
    // Test logic...
    
    // Teardownで自動的に破棄される
}
```

### カスタムSetup/Teardownの追加

```csharp
[TestFixture]
public class CustomSetupTests : EditorTestBase
{
    private Canvas testCanvas;
    
    [SetUp]
    public override void Setup()
    {
        base.Setup(); // 必ず呼び出す
        
        // カスタムセットアップ
        var (go, canvas) = CreateTestGameObjectWith<Canvas>("TestCanvas");
        testCanvas = canvas;
    }
    
    [TearDown]
    public override void Teardown()
    {
        // カスタムクリーンアップ
        testCanvas = null;
        
        base.Teardown(); // 必ず呼び出す
    }
}
```

---

## TestHelpers の使用方法

### GameKitオブジェクトの作成

```csharp
[Test]
public void TestActorCreation()
{
    // 簡単にActorを作成
    var (go, actor) = TestHelpers.CreateTestActor(
        "player",
        GameKitActor.BehaviorProfile.TwoDPhysics,
        GameKitActor.ControlMode.DirectController
    );
    TrackTestObject(go);
    
    Assert.AreEqual("player", actor.ActorId);
}

[Test]
public void TestManagerCreation()
{
    // 簡単にManagerを作成
    var (go, manager) = TestHelpers.CreateTestManager(
        "game_manager",
        GameKitManager.ManagerType.ResourcePool
    );
    TrackTestObject(go);
    
    manager.SetResource("gold", 100);
    Assert.AreEqual(100, manager.GetResource("gold"));
}
```

### MCPコマンドの実行

```csharp
[Test]
public void TestComponentManage()
{
    // Arrange
    var go = CreateTestGameObject("TestObject");
    
    // Act - ヘルパーで簡潔に
    var result = TestHelpers.ExecuteComponentCommand(
        "add",
        go.name,
        "UnityEngine.Rigidbody"
    );
    
    // Assert - 専用アサーション
    TestHelpers.AssertCommandSuccess(result);
    TestHelpers.AssertComponentExists<Rigidbody>(go);
}

[Test]
public void TestComponentUpdate()
{
    // Arrange
    var go = CreateTestGameObject("TestObject");
    go.AddComponent<Rigidbody>();
    
    // Act
    var propertyChanges = new Dictionary<string, object>
    {
        ["mass"] = 2.5,
        ["useGravity"] = false
    };
    
    var result = TestHelpers.ExecuteComponentCommand(
        "update",
        go.name,
        "UnityEngine.Rigidbody",
        propertyChanges
    );
    
    // Assert
    TestHelpers.AssertCommandSuccess(result);
    TestHelpers.AssertUpdatedProperties(result, "mass", "useGravity");
}
```

### アサーションヘルパー

```csharp
[Test]
public void TestAssertions()
{
    var go = CreateTestGameObject("TestObject");
    go.AddComponent<Rigidbody>();
    
    // コンポーネントの存在確認
    TestHelpers.AssertComponentExists<Rigidbody>(go);
    TestHelpers.AssertComponentNotExists<BoxCollider>(go);
    
    // プロパティ値の確認
    TestHelpers.AssertComponentProperty<Rigidbody>(go, "mass", 1.0f);
    
    // Vector3の比較（許容誤差付き）
    go.transform.position = new Vector3(1.0001f, 2.0f, 3.0f);
    TestHelpers.AssertVector3Equals(Vector3.right + Vector3.up * 2 + Vector3.forward * 3, 
                                   go.transform.position);
}
```

---

## Before/After 比較

### 例1: 基本的なGameObjectテスト

#### Before（改善前）

```csharp
[TestFixture]
public class LowLevelToolsTests
{
    private List<GameObject> testObjects = new List<GameObject>();
    
    [SetUp]
    public void Setup()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        testObjects.Clear();
    }
    
    [TearDown]
    public void Teardown()
    {
        foreach (var obj in testObjects)
        {
            if (obj != null)
            {
                Object.DestroyImmediate(obj);
            }
        }
        testObjects.Clear();
    }
    
    [Test]
    public void GameObjectManage_Create_CreatesGameObject()
    {
        // Arrange & Act
        var go = new GameObject("TestObject");
        testObjects.Add(go);
        
        // Assert
        Assert.IsNotNull(go);
        Assert.AreEqual("TestObject", go.name);
        Assert.IsNotNull(GameObject.Find("TestObject"));
    }
}
```

**問題点**:
- Setup/Teardownのボイラープレートが多い
- 手動でtestObjectsにAdd
- 繰り返しコード

#### After（改善後）

```csharp
[TestFixture]
public class LowLevelToolsTests : EditorTestBase
{
    [Test]
    public void GameObjectManage_Create_CreatesGameObject()
    {
        // Arrange & Act
        var go = CreateTestGameObject("TestObject");
        
        // Assert
        Assert.IsNotNull(go);
        Assert.AreEqual("TestObject", go.name);
        AssertGameObjectExists("TestObject");
    }
}
```

**改善点**:
- ✅ Setup/Teardownのボイラープレート削除
- ✅ 自動的にクリーンアップリストに追加
- ✅ 専用アサーションで可読性向上
- ✅ コード量: 40行 → 15行（-62%）

---

### 例2: GameKitActorテスト

#### Before（改善前）

```csharp
[TestFixture]
public class GameKitActorTests
{
    private GameObject testActorGo;
    
    [SetUp]
    public void Setup()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }
    
    [TearDown]
    public void Teardown()
    {
        if (testActorGo != null)
        {
            Object.DestroyImmediate(testActorGo);
        }
    }
    
    [Test]
    public void CreateActor_WithValidParameters_CreatesGameObject()
    {
        // Arrange
        testActorGo = new GameObject("TestActor");
        var actor = testActorGo.AddComponent<GameKitActor>();
        
        // Act
        actor.Initialize("actor_001", GameKitActor.BehaviorProfile.TwoDPhysics, GameKitActor.ControlMode.DirectController);
        
        // Assert
        Assert.IsNotNull(actor);
        Assert.AreEqual("actor_001", actor.ActorId);
        Assert.AreEqual(GameKitActor.BehaviorProfile.TwoDPhysics, actor.Behavior);
        Assert.AreEqual(GameKitActor.ControlMode.DirectController, actor.Control);
    }
}
```

**問題点**:
- Setup/Teardownの重複
- Actor作成のボイラープレート
- 個別のクリーンアップ変数

#### After（改善後）

```csharp
[TestFixture]
public class GameKitActorTests : EditorTestBase
{
    [Test]
    public void CreateActor_WithValidParameters_CreatesGameObject()
    {
        // Arrange & Act
        var (go, actor) = TestHelpers.CreateTestActor(
            "actor_001",
            GameKitActor.BehaviorProfile.TwoDPhysics,
            GameKitActor.ControlMode.DirectController
        );
        TrackTestObject(go);
        
        // Assert
        Assert.IsNotNull(actor);
        Assert.AreEqual("actor_001", actor.ActorId);
        Assert.AreEqual(GameKitActor.BehaviorProfile.TwoDPhysics, actor.Behavior);
        Assert.AreEqual(GameKitActor.ControlMode.DirectController, actor.Control);
    }
}
```

**改善点**:
- ✅ Setup/Teardownのボイラープレート削除
- ✅ 1行でActor作成
- ✅ 自動クリーンアップ
- ✅ コード量: 35行 → 18行（-49%）

---

### 例3: TextMeshProコンポーネントテスト

#### Before（改善前）

```csharp
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
}
```

**問題点**:
- 冗長なペイロード作成
- コマンド作成が複雑
- アサーションが繰り返し

#### After（改善後）

```csharp
[Test]
public void UpdateComponent_TextMeshPro_UpdatesText()
{
    // Arrange
    var go = CreateTestGameObject("TestObject");
    TestHelpers.ExecuteComponentCommand("add", go.name, TMP_TYPE);
    
    var propertyChanges = new Dictionary<string, object>
    {
        ["text"] = "Hello TextMeshPro!",
        ["fontSize"] = 36.0,
        ["enableAutoSizing"] = false
    };
    
    // Act
    var result = TestHelpers.ExecuteComponentCommand("update", go.name, TMP_TYPE, propertyChanges);
    
    // Assert
    TestHelpers.AssertCommandSuccess(result);
    TestHelpers.AssertUpdatedProperties(result, "text", "fontSize", "enableAutoSizing");
}
```

**改善点**:
- ✅ ヘルパーで簡潔に
- ✅ 専用アサーションで明確に
- ✅ コード量: 30行 → 18行（-40%）

---

## 移行ガイド

### ステップ1: 既存テストファイルの確認

1. テストファイルを開く
2. `[SetUp]`と`[TearDown]`を確認
3. GameObjectのクリーンアップパターンを確認

### ステップ2: 基底クラスの継承

```csharp
// Before
public class MyTests
{
    private List<GameObject> testObjects = new List<GameObject>();
    // ...
}

// After
public class MyTests : EditorTestBase
{
    // testObjects は基底クラスで定義済み
    // Setup/Teardown も基底クラスで実装済み
}
```

### ステップ3: Setup/Teardownの削除または簡略化

```csharp
// Before
[SetUp]
public void Setup()
{
    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    testObjects.Clear();
}

[TearDown]
public void Teardown()
{
    foreach (var obj in testObjects)
    {
        if (obj != null)
        {
            Object.DestroyImmediate(obj);
        }
    }
    testObjects.Clear();
}

// After: 基底クラスに任せる → 削除
// または、カスタムロジックが必要な場合のみ：
[SetUp]
public override void Setup()
{
    base.Setup();
    // カスタムロジックのみ
}
```

### ステップ4: GameObject作成の置き換え

```csharp
// Before
var go = new GameObject("TestObject");
testObjects.Add(go);

// After
var go = CreateTestGameObject("TestObject");
```

### ステップ5: TestHelpersの活用

```csharp
// Before: Actor作成
var actorGo = new GameObject("TestActor");
var actor = actorGo.AddComponent<GameKitActor>();
actor.Initialize("actor_001", ...);

// After: ヘルパー使用
var (go, actor) = TestHelpers.CreateTestActor("actor_001", ...);
TrackTestObject(go);
```

### ステップ6: アサーションの改善

```csharp
// Before
var component = go.GetComponent<Rigidbody>();
Assert.IsNotNull(component, "Rigidbody component should exist");

// After
TestHelpers.AssertComponentExists<Rigidbody>(go);
```

---

## 📊 期待される効果

### コード量の削減

| テストファイル | Before | After | 削減率 |
|--------------|--------|-------|--------|
| LowLevelToolsTests | 192行 | ~120行 | -37% |
| GameKitActorTests | 199行 | ~130行 | -35% |
| TextMeshProComponentTests | 669行 | ~450行 | -33% |
| **平均** | - | - | **-35%** |

### 保守性の向上

- ✅ Setup/Teardownの一元管理
- ✅ コードの重複削除
- ✅ 可読性の向上
- ✅ テスト作成時間の短縮

### 品質の向上

- ✅ 一貫したテストパターン
- ✅ リソースリーク防止
- ✅ 専用アサーションによる明確なエラーメッセージ

---

## 💡 次のステップ

1. **1つのテストファイルで試す**: `LowLevelToolsTests.cs`などの小さなファイルから開始
2. **動作確認**: すべてのテストが正常にパスすることを確認
3. **段階的に適用**: 他のテストファイルにも徐々に適用
4. **フィードバック**: 使いにくい点があれば`EditorTestBase`や`TestHelpers`を改善

---

**作成日**: 2025-12-06  
**バージョン**: 1.0.0  
**対象**: UnityAI-Forge Test Infrastructure
