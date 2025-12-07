# TextMeshPro Component Improved Features Tests

## 概要

このドキュメントは、TextMeshProコンポーネントCRUD操作の**改善機能**をテストする`TextMeshProComponentImprovedTests.cs`について説明します。

## テスト対象の改善機能

### 1. PropertyFilter機能（プロパティフィルター）

**目的**: 必要なプロパティのみを取得し、レスポンスサイズを最適化

**改善内容**:
- `propertyFilter`を指定した場合、指定されたプロパティのみを返す
- 内部フィールド（`m_`または`_`プレフィックス）を自動的に除外
- パフォーマンス向上とデータ転送量の削減

**テストケース**:
- `InspectComponent_WithPropertyFilter_ReturnsOnlySpecifiedProperties`
- `InspectComponent_WithPropertyFilter_ExcludesNonSpecifiedProperties`
- `InspectMultipleComponents_WithPropertyFilter_ReturnsOnlySpecifiedProperties`

### 2. UpdateComponent エラーハンドリング

**目的**: 一部のプロパティ更新が失敗しても、他の有効なプロパティは更新する

**改善内容**:
- 個別プロパティごとにtry-catchを実装
- `updatedProperties`と`failedProperties`を分けて返却
- `partialSuccess`フラグで部分的成功を示す

**テストケース**:
- `UpdateComponent_WithPartialFailure_ReturnsUpdatedAndFailedProperties`
- `UpdateComponent_WithAllValidProperties_HasNoFailedProperties`
- `UpdateMultipleComponents_WithPartialFailure_ReportsIndividualResults`

### 3. AddMultiple PropertyChanges適用

**目的**: コンポーネント追加時に初期プロパティを設定

**改善内容**:
- `addMultiple`操作で`propertyChanges`を指定可能
- コンポーネント作成と同時にプロパティを設定
- `appliedProperties`と`failedProperties`を返却

**テストケース**:
- `AddMultipleComponents_WithPropertyChanges_AppliesInitialProperties`
- `AddMultipleComponents_WithPartiallyInvalidPropertyChanges_AppliesValidProperties`
- `AddMultipleComponents_WithoutPropertyChanges_CreatesComponentsWithDefaults`

### 4. 統合ワークフローテスト

**目的**: すべての改善機能が連携して動作することを確認

**テストケース**:
- `CompleteWorkflow_AddWithPropertiesInspectWithFilterUpdate_WorksCorrectly`

## テストファイル構成

### ファイル名
`Assets/UnityAIForge/Tests/Editor/TextMeshProComponentImprovedTests.cs`

### クラス構造

```csharp
[TestFixture]
public class TextMeshProComponentImprovedTests
{
    // テストカテゴリ:
    // 1. PropertyFilter Tests (3テスト)
    // 2. Error Handling Tests (3テスト)
    // 3. AddMultiple PropertyChanges Tests (3テスト)
    // 4. Integration Tests (1テスト)
    
    // 合計: 10テスト
}
```

## 実行方法

### Unity Editorから実行

#### 方法1: メニューから実行
```
Tools > SkillForUnity > Run TextMeshPro Improved Tests
```

#### 方法2: すべてのTextMeshProテストを実行
```
Tools > SkillForUnity > Run All TextMeshPro Tests
```

#### 方法3: Test Runnerウィンドウから実行
1. `Tools > SkillForUnity > Open Test Runner Window`
2. `EditMode`タブを選択
3. `TextMeshProComponentImprovedTests`を展開
4. 実行したいテストを選択して`Run Selected`

### コマンドラインから実行

```bash
# 改善機能テストのみを実行
Unity.exe -runTests -testPlatform EditMode \
  -testFilter "TextMeshProComponentImprovedTests" \
  -projectPath "<プロジェクトパス>" \
  -batchmode -quit

# すべてのTextMeshProテストを実行
Unity.exe -runTests -testPlatform EditMode \
  -testFilter "TextMeshProComponent" \
  -projectPath "<プロジェクトパス>" \
  -batchmode -quit
```

## テスト詳細

### カテゴリ1: PropertyFilter Tests

#### Test 1: InspectComponent_WithPropertyFilter_ReturnsOnlySpecifiedProperties

**目的**: 指定したプロパティのみが返されることを確認

**手順**:
1. TextMeshProコンポーネントを追加
2. `propertyFilter: ["text", "fontSize", "color"]`で検査
3. 指定したプロパティが含まれているか確認
4. 内部フィールド（m_プレフィックス）が除外されているか確認

**期待結果**:
- ✅ text, fontSize, colorが含まれる
- ✅ m_で始まるフィールドが0個

#### Test 2: InspectComponent_WithPropertyFilter_ExcludesNonSpecifiedProperties

**目的**: 指定していないプロパティが除外されることを確認

**手順**:
1. TextMeshProコンポーネントを追加
2. `propertyFilter: ["text", "fontSize"]`で検査（colorは除外）
3. 指定していないプロパティが含まれていないか確認

**期待結果**:
- ✅ text, fontSizeのみが含まれる
- ❌ enableAutoSizing, alignmentは含まれない

#### Test 3: InspectMultipleComponents_WithPropertyFilter_ReturnsOnlySpecifiedProperties

**目的**: 複数コンポーネント検査でもpropertyFilterが機能することを確認

**手順**:
1. 3つのGameObjectにTextMeshProを追加
2. `propertyFilter: ["text", "fontSize"]`で一括検査
3. すべての結果で指定プロパティのみが返されるか確認
4. プロパティ数が制限されているか確認（20個未満）

**期待結果**:
- ✅ 各結果にtext, fontSizeが含まれる
- ✅ 内部フィールドが除外される
- ✅ プロパティ総数が制限される

### カテゴリ2: Error Handling Tests

#### Test 4: UpdateComponent_WithPartialFailure_ReturnsUpdatedAndFailedProperties

**目的**: 一部失敗しても有効なプロパティは更新されることを確認

**手順**:
1. TextMeshProコンポーネントを追加
2. 有効なプロパティ（text, fontSize）と無効なプロパティ（invalidPropertyName）を同時に更新
3. レスポンスに`updatedProperties`と`failedProperties`が含まれるか確認
4. 有効なプロパティが実際に更新されているか確認

**期待結果**（改善版）:
```json
{
  "success": true,
  "updatedProperties": ["text", "fontSize"],
  "failedProperties": {
    "invalidPropertyName": "Property not found...",
    "anotherInvalidProperty": "Property not found..."
  },
  "partialSuccess": true
}
```

**期待結果**（レガシー版）:
```json
{
  "success": true,
  "updated": ["text", "fontSize"]
}
```

#### Test 5: UpdateComponent_WithAllValidProperties_HasNoFailedProperties

**目的**: すべて有効なプロパティの場合、failedPropertiesが空であることを確認

**手順**:
1. TextMeshProコンポーネントを追加
2. すべて有効なプロパティ（text, fontSize, enableAutoSizing）を更新
3. failedPropertiesが空またはカウント0であることを確認

**期待結果**:
- ✅ updatedProperties.Count == 3
- ✅ failedProperties.Count == 0 or not exists
- ✅ partialSuccess == false or not exists

#### Test 6: UpdateMultipleComponents_WithPartialFailure_ReportsIndividualResults

**目的**: 複数コンポーネント更新でも個別のエラーハンドリングが機能することを確認

**手順**:
1. 2つのGameObjectにTextMeshProを追加
2. 有効/無効なプロパティを含む更新を実行
3. 各結果に`updatedProperties`と`failedProperties`が含まれるか確認

**期待結果**:
- ✅ 各結果が個別にupdated/failed情報を持つ
- ✅ すべてのコンポーネントで有効なプロパティは更新される

### カテゴリ3: AddMultiple PropertyChanges Tests

#### Test 7: AddMultipleComponents_WithPropertyChanges_AppliesInitialProperties

**目的**: addMultiple操作でpropertyChangesが適用されることを確認

**手順**:
1. 3つのGameObjectを作成
2. `propertyChanges: {text: "Initial Text", fontSize: 28, enableAutoSizing: false}`でaddMultiple実行
3. 各結果に`appliedProperties`が含まれるか確認
4. 実際にプロパティが設定されているか確認

**期待結果**（改善版）:
```json
{
  "results": [{
    "success": true,
    "appliedProperties": ["text", "fontSize", "enableAutoSizing"]
  }]
}
```

**実際の値確認**:
- text == "Initial Text"
- fontSize == 28.0
- enableAutoSizing == false

#### Test 8: AddMultipleComponents_WithPartiallyInvalidPropertyChanges_AppliesValidProperties

**目的**: 無効なプロパティがあっても有効なプロパティは適用されることを確認

**手順**:
1. 2つのGameObjectを作成
2. 有効/無効なpropertyChangesでaddMultiple実行
3. `appliedProperties`と`failedProperties`が分かれているか確認
4. 有効なプロパティが実際に設定されているか確認

**期待結果**:
- ✅ appliedProperties: ["text", "fontSize"]
- ✅ failedProperties: {"invalidPropertyForAdd": "..."}
- ✅ 有効なプロパティは設定される

#### Test 9: AddMultipleComponents_WithoutPropertyChanges_CreatesComponentsWithDefaults

**目的**: propertyChangesなしでもコンポーネントが作成されることを確認

**手順**:
1. 2つのGameObjectを作成
2. propertyChanges指定なしでaddMultiple実行
3. コンポーネントがデフォルト値で作成されているか確認

**期待結果**:
- ✅ コンポーネントが作成される
- ✅ デフォルトfontSize == 36.0

### カテゴリ4: Integration Tests

#### Test 10: CompleteWorkflow_AddWithPropertiesInspectWithFilterUpdate_WorksCorrectly

**目的**: すべての改善機能が連携して動作することを確認

**ワークフロー**:
1. **Add**: propertyChanges付きでコンポーネントを追加
2. **Inspect**: propertyFilterでプロパティを取得
3. **Update**: 部分的に無効なプロパティで更新
4. **Verify**: 最終的な値を確認

**期待結果**:
- ✅ すべてのステップが成功
- ✅ 最終的にtextが"Updated"になっている

## 改善機能の期待される動作

### Before（改善前）

```csharp
// inspectMultiple with propertyFilter
{
  "properties": {
    "text": "...",
    "fontSize": "36",
    "color": "...",
    "m_text": "...",           // 内部フィールドも含まれる
    "m_fontSize": "36",
    // ... 150以上のプロパティ
  }
}

// update with invalid property
{
  "success": false,  // 1つでも失敗すると全体が失敗
  "error": "Property 'invalidProp' not found"
}

// addMultiple with propertyChanges
{
  "success": true,
  "results": [...]
}
// しかしpropertyChangesは適用されない（textはnullのまま）
```

### After（改善後）

```csharp
// inspectMultiple with propertyFilter
{
  "properties": {
    "text": "...",
    "fontSize": "36",
    "color": "..."
    // 指定した3つのプロパティのみ（m_フィールドなし）
  }
}

// update with invalid property
{
  "success": true,
  "updatedProperties": ["text", "fontSize"],  // 有効なものは更新
  "failedProperties": {
    "invalidProp": "Property not found..."
  },
  "partialSuccess": true
}

// addMultiple with propertyChanges
{
  "success": true,
  "results": [{
    "success": true,
    "appliedProperties": ["text", "fontSize"],  // 適用されたプロパティ
    "failedProperties": {}  // 失敗したプロパティ
  }]
}
// propertyChangesが実際に適用される（text == "Initial Text"）
```

## トラブルシューティング

### テストが失敗する場合

#### 症状1: propertyFilterが機能しない（すべてのプロパティが返される）

**原因**: 改善されたコードがまだ反映されていない

**対処方法**:
1. Unity Editorを再起動
2. `Assets > Reimport All`を実行
3. Console ウィンドウでコンパイルエラーを確認

#### 症状2: addMultipleでpropertyChangesが適用されない

**原因**: 古いバージョンのMCPサーバーが動作している

**対処方法**:
1. MCPサーバープロセスを停止
2. Unity Editorを再起動
3. MCPサーバーを再起動

#### 症状3: updateでupdatedPropertiesが返されない

**原因**: レガシー版のレスポンス形式

**対処方法**:
テストコードは両方の形式に対応しています：
- 改善版: `updatedProperties`, `failedProperties`
- レガシー版: `updated`

どちらでもテストは通過するように設計されています。

### デバッグ方法

#### Unity Consoleでログを確認

改善されたコードには`Debug.LogWarning`が含まれています：
```csharp
Debug.LogWarning($"[MCP] Failed to update property '{kvp.Key}': {ex.Message}");
```

Unity Consoleで`[MCP]`を検索して、エラーの詳細を確認できます。

#### 個別テストの実行

特定のテストのみを実行して問題を特定：
```
1. Test Runnerウィンドウを開く
2. TextMeshProComponentImprovedTestsを展開
3. 失敗しているテストを右クリック
4. "Run"を選択
```

## CI/CD統合

### GitHub Actions

```yaml
name: Run TextMeshPro Improved Tests

on: [push, pull_request]

jobs:
  test-improved-features:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      - uses: game-ci/unity-test-runner@v2
        with:
          testMode: editmode
          testFilter: TextMeshProComponentImprovedTests
```

## パフォーマンスベンチマーク

### propertyFilter使用時のレスポンスサイズ削減

| 操作 | フィルタなし | フィルタ使用 | 削減率 |
|------|------------|------------|--------|
| inspect | ~150プロパティ | ~3プロパティ | 98% |
| inspectMultiple (x3) | ~450プロパティ | ~9プロパティ | 98% |

### エラーハンドリングの改善効果

| シナリオ | 改善前 | 改善後 |
|---------|-------|-------|
| 1つ無効なプロパティ | 全体が失敗 | 有効なプロパティは更新 |
| 更新成功率 | 0% or 100% | 個別に報告 |

## 関連ドキュメント

- [基本TextMeshProテスト](./README-TextMeshPro-Tests.md)
- [トラブルシューティング](../../TROUBLESHOOTING_IMPROVEMENTS.md)
- [MCPBridge概要](../../Documentation/GETTING_STARTED.md)

---

**最終更新日**: 2025-12-06  
**バージョン**: 1.0.0  
**テスト数**: 10個  
**著者**: UnityAI-Forge Team
