# Phase 6a 実装レポート: Template Handler の実装

## 概要

このレポートは、`McpCommandProcessor` のインターフェース抽出リファクタリング計画における Phase 6a の完了を報告します。当初 Phase 6 として計画されていた UI/Template ハンドラーの実装を、管理しやすい2つのサブフェーズに分割しました。Phase 6a では、比較的シンプルな Template ハンドラーを実装し、Phase 6b では複雑な UGUI ハンドラーに取り組む予定です。

## 達成された目標

**`TemplateCommandHandler`** (~800行) が実装され、6つのテンプレート関連ツールを統合しました：

1. `sceneQuickSetup` - シーンクイックセットアップ
2. `gameObjectCreateFromTemplate` - テンプレートからGameObject作成
3. `designPatternGenerate` - デザインパターンコード生成
4. `scriptTemplateGenerate` - スクリプトテンプレート生成
5. `templateManage` - GameObjectカスタマイズとPrefab変換
6. `menuHierarchyCreate` - 階層的メニューシステム作成

## 実装詳細

### TemplateCommandHandler

**場所**: `Assets/SkillForUnity/Editor/MCPBridge/Handlers/TemplateCommandHandler.cs`

**統合アプローチ**:
- 1つのハンドラーが6つの異なるツールをサポート
- `CommandHandlerFactory`に6つのツール名で同じインスタンスを登録
- 各ツールが独自の操作ロジックを持つ

**サポートされる操作**:

#### 1. sceneQuickSetup
シーンのクイックセットアップ機能：
- **3D**: Main Camera + Directional Light
- **2D**: Orthographic Camera
- **UI**: Canvas + EventSystem
- **VR**: VR対応Camera
- **Empty**: 空のシーン

**特徴**:
- 既存オブジェクトの検出（重複作成を防止）
- カスタマイズ可能な位置/回転
- Undoサポート

#### 2. gameObjectCreateFromTemplate
15+のGameObjectテンプレート：
- **Lights**: Directional, Point, Spot
- **Primitives**: Cube, Sphere, Plane, Cylinder, Capsule, Quad
- **Common**: Camera, Empty, Player, Enemy
- **Effects**: Particle System, Audio Source

**特徴**:
- 親子関係の設定
- Transform（position, rotation, scale）の適用
- Undoサポート

#### 3. designPatternGenerate
デザインパターンコード生成：
- **Singleton**: MonoBehaviour版 / Plain C#版
- **ObjectPool**: オブジェクトプーリング
- **StateMachine**: ステートマシン
- **Observer**: オブザーバーパターン
- **Command**: コマンドパターン
- **Factory**: ファクトリーパターン
- **ServiceLocator**: サービスロケーター

**特徴**:
- カスタマイズ可能なオプション
- Namespace サポート
- 完全なC#コード生成

**実装例（Singleton）**:
```csharp
private string GenerateSingletonPattern(string className, string namespaceName, Dictionary<string, object> options)
{
    var monoBehaviour = GetOptionBool(options, "monoBehaviour", true);
    var persistent = GetOptionBool(options, "persistent", false);
    
    // MonoBehaviour版とPlain C#版の両方をサポート
    // Persistent オプションで DontDestroyOnLoad を追加
}
```

#### 4. scriptTemplateGenerate
Unity スクリプトテンプレート：
- **MonoBehaviour**: Awake, Start, Update付き
- **ScriptableObject**: CreateAssetMenu付き

**特徴**:
- Namespace サポート
- 標準的なUnityライフサイクルメソッド
- カスタマイズ可能な属性

#### 5. templateManage
GameObjectのカスタマイズとPrefab変換：
- **customize**: コンポーネント追加＋プロパティ設定
- **convertToPrefab**: GameObjectをPrefabに変換

**特徴**:
- 複数コンポーネントの一括追加
- プロパティの自動適用
- Prefab変換の簡略化

#### 6. menuHierarchyCreate
階層的メニューシステムの作成：
- 入れ子のサブメニュー構造
- 自動的なレイアウト管理
- ボタンサイズとスペーシングのカスタマイズ

**特徴**:
- Dictionary形式のメニュー構造定義
- VerticalLayoutGroupの自動追加
- 再帰的なサブメニュー構築

## アーキテクチャの特徴

### 1. 統合ハンドラーパターン

```csharp
// 1つのハンドラーインスタンスを複数のツール名で登録
var templateHandler = new TemplateCommandHandler();
CommandHandlerFactory.Register("sceneQuickSetup", templateHandler);
CommandHandlerFactory.Register("gameObjectCreateFromTemplate", templateHandler);
CommandHandlerFactory.Register("designPatternGenerate", templateHandler);
// ...
```

**利点**:
- コードの共有とメンテナンス性の向上
- 一貫したエラーハンドリング
- メモリ効率の向上（単一インスタンス）

### 2. 柔軟なコード生成

デザインパターンとスクリプトテンプレートの生成で、以下をサポート：
- Namespace の有無
- カスタマイズ可能なオプション
- インデント自動調整

### 3. Unity APIとの統合

- `Undo`システムの完全サポート
- `Selection`との統合
- `AssetDatabase`自動リフレッシュ

## Phase 6 の分割理由

### 当初の計画
Phase 6: UI/Template ハンドラーの実装（2-3週間）
- TemplateCommandHandler
- UguiCommandHandler（6ツール、2081行）

### 分割の決定

**Phase 6a**: TemplateCommandHandler（1週間）✅ 完了
- 実装: 6ツール、~800行
- 複雑度: 中
- 理由: 管理可能なサイズ、明確な責任

**Phase 6b**: UguiCommandHandler（2-3週間）
- 実装予定: 6ツール、~2081行
- 複雑度: 非常に高
- 理由: 
  - RectTransform操作は微妙で複雑
  - レイアウトシステムの深い理解が必要
  - テンプレート生成とオーバーラップ検出
  - 十分なテストが必要

### 分割のメリット

1. **リスク軽減**: 小さな単位での実装とテスト
2. **段階的な進捗**: 部分的な成果を早期に提供
3. **品質維持**: 複雑な部分に十分な時間を確保
4. **レビュー容易性**: 小さなPRで詳細なレビューが可能

## 現在の実行状況

### ✅ 新システムで動作中（7ハンドラー、12ツール）

| ツール名 | ハンドラー | 操作数 | Phase |
|---------|-----------|--------|-------|
| ✅ sceneManage | SceneCommandHandler | 11 | 3 |
| ✅ gameObjectManage | GameObjectCommandHandler | 10 | 3 |
| ✅ componentManage | ComponentCommandHandler | 8 | 3 |
| ✅ assetManage | AssetCommandHandler | 10 | 3 |
| ✅ prefabManage | PrefabCommandHandler | 7 | 5 |
| ✅ scriptableObjectManage | ScriptableObjectCommandHandler | 7 | 5 |
| ✅ **sceneQuickSetup** | **TemplateCommandHandler** | **1** | **6a** |
| ✅ **gameObjectCreateFromTemplate** | **TemplateCommandHandler** | **1** | **6a** |
| ✅ **designPatternGenerate** | **TemplateCommandHandler** | **1** | **6a** |
| ✅ **scriptTemplateGenerate** | **TemplateCommandHandler** | **1** | **6a** |
| ✅ **templateManage** | **TemplateCommandHandler** | **2** | **6a** |
| ✅ **menuHierarchyCreate** | **TemplateCommandHandler** | **1** | **6a** |

**合計**: 7ハンドラー（実インスタンス）、12ツール（登録名）、59操作

### ⚠️ 既存システムで動作中

| ツール名 | 理由 | 予定 |
|---------|------|------|
| uguiManage (+ 5関連ツール) | 複雑すぎる（2081行） | Phase 6b |
| 設定系ツール | 未実装 | Phase 7 |

## 進捗メトリクス

```
進捗: █████████████████░░░░░░░░░░░░░░░░░░░ 63%

実装済み:  12ツール / 19ツール
ハンドラー: 7インスタンス
操作数:    59操作
コード行数: ~3,890行（新ハンドラーコード）
```

| Phase | 内容 | ハンドラー | ツール | 行数 |
|-------|------|-----------|--------|------|
| Phase 1-2 | 基盤実装 | - | - | ~1,300 |
| Phase 3 | 最初の4ハンドラー | 4 | 4 | ~1,700 |
| Phase 4 | ファクトリー統合 | - | - | +150 |
| Phase 5 | Prefab/SO | 2 | 2 | +940 |
| **Phase 6a** | **Template** | **1** | **6** | **+800** |
| **合計** | | **7** | **12** | **~4,890** |

## 次のステップ

### Phase 6b: UguiCommandHandler（予定2-3週間）

**実装対象**:
1. **uguiManage**: 統合UGUI管理（rectAdjust, setAnchor, etc.）
2. **uguiRectAdjust**: RectTransform調整
3. **uguiAnchorManage**: アンカー管理
4. **uguiCreateFromTemplate**: UIテンプレート生成
5. **uguiLayoutManage**: レイアウトコンポーネント管理
6. **uguiDetectOverlaps**: オーバーラップ検出

**実装戦略**:
1. 主要なuguiManage操作から開始
2. 補助ツール（rectAdjust, anchorManage）を統合
3. テンプレートとレイアウト機能を追加
4. オーバーラップ検出を実装
5. 包括的なテストスイートを作成

### Phase 7: 設定系ハンドラー（予定1週間）

**実装対象**:
- SettingsCommandHandler
- ConstantCommandHandler
- その他の設定関連ツール

## 使用例

### シーンクイックセットアップ

```csharp
var payload = new Dictionary<string, object>
{
    ["operation"] = "sceneQuickSetup",
    ["setupType"] = "3D",
    ["cameraPosition"] = new Dictionary<string, object>
    {
        ["x"] = 0, ["y"] = 2, ["z"] = -10
    },
    ["lightIntensity"] = 1.5f
};

_templateHandler.Execute("sceneQuickSetup", payload);
// → Main Camera + Directional Light を作成
```

### デザインパターン生成

```csharp
var payload = new Dictionary<string, object>
{
    ["operation"] = "designPatternGenerate",
    ["patternType"] = "singleton",
    ["className"] = "GameManager",
    ["namespace"] = "MyGame",
    ["scriptPath"] = "Assets/Scripts/GameManager.cs",
    ["options"] = new Dictionary<string, object>
    {
        ["monoBehaviour"] = true,
        ["persistent"] = true  // DontDestroyOnLoad
    }
};

_templateHandler.Execute("designPatternGenerate", payload);
// → Singleton パターンのコードを生成
```

### 階層的メニュー作成

```csharp
var payload = new Dictionary<string, object>
{
    ["operation"] = "menuHierarchyCreate",
    ["menuName"] = "MainMenu",
    ["menuStructure"] = new Dictionary<string, object>
    {
        ["Play"] = null,  // シンプルなボタン
        ["Settings"] = new Dictionary<string, object>
        {
            ["Audio"] = null,
            ["Graphics"] = null,
            ["Controls"] = null
        },
        ["Quit"] = null
    },
    ["buttonWidth"] = 200,
    ["buttonHeight"] = 50,
    ["spacing"] = 10
};

_templateHandler.Execute("menuHierarchyCreate", payload);
// → 入れ子のメニューシステムを作成
```

## 結論

Phase 6a は成功裏に完了しました。TemplateCommandHandlerが実装され、6つのテンプレート関連ツールが新しいアーキテクチャに統合されました：

1. ✅ **統合ハンドラーパターン**: 1つのハンドラーが複数のツールをサポート
2. ✅ **柔軟なコード生成**: デザインパターンとスクリプトテンプレート
3. ✅ **Unity APIとの統合**: Undo, Selection, AssetDatabase
4. ✅ **段階的な移行**: Phase 6を2つのサブフェーズに分割
5. ✅ **品質維持**: 複雑なUGUIハンドラーに十分な時間を確保

**合計12ツール（7ハンドラーインスタンス）が新システムで実行中！移行率63%達成！** 🎉

Phase 6b では、最も複雑なUGUIハンドラーに集中して取り組む予定です。この段階的なアプローチにより、品質とテストカバレッジを維持しながら、着実に移行を進めることができます。

## 変更履歴

| 日付 | 変更内容 |
|------|---------|
| 2025-11-27 | Phase 6a 完了: TemplateCommandHandlerの実装 |

