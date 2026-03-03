# GameKit フレームワーク

<div align="center">

**AI駆動ゲーム開発フレームワーク**

コード生成を活用した、ランタイム依存ゼロのスタンドアロンコンポーネント

[📚 インデックスに戻る](../INDEX.md) | [🚀 はじめに](../GETTING_STARTED.md) | [🔧 全ツールガイド](../MCPServer/SKILL_GAMEKIT.md)

</div>

---

## 概要

GameKit は Unity-AI-Forge の高レベルゲーム開発フレームワークです。MCP ツールを通じて AI がゲーム開発を支援します。

### 主な特徴

- **コード生成アーキテクチャ**: テンプレートからスタンドアロン C# スクリプトを生成
- **ランタイム依存ゼロ**: 生成されたコードは Unity-AI-Forge パッケージに依存しない
- **UI Toolkit ベース**: UXML/USS を活用したモダンな UI 構築
- **UI + Logic + Systems 構造**: UI、ロジック、システムで包括的なゲーム機能を提供

---

## GameKit アーキテクチャ

```
GameKit Framework
├── GameKit UI（1ツール）── UI コンポーネント生成
│   ├── UICommand ─────── ボタンコマンドパネル
│   ├── UIBinding ─────── データバインディング
│   ├── UIList ────────── リスト／グリッド表示
│   ├── UISlot ────────── スロット（インベントリ等）
│   └── UISelection ───── 選択グループ（ラジオ/タブ等）
│
├── ロジックピラー（7ツール）── 分析・検証
│   ├── SceneIntegrity ── シーン整合性チェック
│   ├── ClassCatalog ──── クラス一覧と検査
│   ├── ClassDependencyGraph ── クラス依存関係分析
│   ├── SceneReferenceGraph ── シーン参照分析
│   ├── SceneRelationshipGraph ── シーン関係分析
│   ├── SceneDependency ── シーンアセット依存関係分析
│   └── ScriptSyntax ──── C#ソースコード構文解析
│
└── GameKit Data（1ツール）── データ・プーリング
    └── Data ────────────── プーリング、イベントチャンネル、データコンテナ、ランタイムセット
```

---

## UI ピラー

UI ピラーは UI Toolkit（UXML/USS）を使用したUI コンポーネントを生成します。生成された C# スクリプトは `FindById()` による静的レジストリパターンを採用し、他のコンポーネントから簡単にアクセスできます。

### UICommand（コマンドパネル）

MCP ツール: `unity_gamekit_ui(widgetType='command')`

ボタンベースのコマンドパネルを生成します。UXML/USS と C# スクリプトを自動生成し、UIDocument として配置します。

**オペレーション:** `createCommandPanel`, `addCommand`, `inspect`, `delete`

**コマンドタイプ:**
- `move` - 移動方向コマンド
- `jump` - ジャンプコマンド
- `action` - アクションコマンド（パラメータ付き）
- `look` - 視点方向コマンド
- `custom` - カスタムコマンド

**使用例:**
```python
# コマンドパネルを作成
unity_gamekit_ui(widgetType='command', {
    "operation": "createCommandPanel",
    "panelId": "PlayerControls",
    "layout": "horizontal",
    "commands": [
        {"name": "moveUp", "label": "↑", "commandType": "move",
         "moveDirection": {"x": 0, "y": 0, "z": 1}},
        {"name": "jump", "label": "Jump", "commandType": "jump"},
        {"name": "attack", "label": "Attack", "commandType": "action",
         "commandParameter": "sword"}
    ]
})
# → コンパイル待ち: unity_compilation_await が必要
```

### UIBinding（データバインディング）

MCP ツール: `unity_gamekit_ui(widgetType='binding')`

UI 要素をデータソースに宣言的にバインドします。ProgressBar、Label、Slider 等をリアルタイムに更新できます。

**オペレーション:** `create`, `update`, `inspect`, `delete`, `setRange`, `refresh`, `findByBindingId`

**ソースタイプ:** `health`, `economy`, `timer`, `custom`

**表示フォーマット:** `raw`, `percent`, `formatted`, `ratio`

**使用例:**
```python
# HPバーのバインディングを作成
unity_gamekit_ui(widgetType='binding', {
    "operation": "create",
    "bindingId": "playerHP",
    "sourceType": "health",
    "sourceId": "player_health",
    "elementName": "hp-bar",
    "targetProperty": "value",
    "format": "percent",
    "minValue": 0,
    "maxValue": 100,
    "smoothTransition": true,
    "smoothSpeed": 5.0
})
```

### UIList（リスト/グリッド）

MCP ツール: `unity_gamekit_ui(widgetType='list')`

ScrollView ベースの動的リスト/グリッド表示を生成します。アイテム管理と選択機能を備えています。

**オペレーション:** `create`, `update`, `inspect`, `delete`, `setItems`, `addItem`, `removeItem`, `clear`, `selectItem`, `deselectItem`, `clearSelection`, `refreshFromSource`, `findByListId`

**レイアウト:** `vertical`, `horizontal`, `grid`

**使用例:**
```python
# インベントリリストを作成
unity_gamekit_ui(widgetType='list', {
    "operation": "create",
    "listId": "inventory",
    "layout": "grid",
    "columns": 4,
    "dataSource": "inventory",
    "selectable": true,
    "multiSelect": false
})

# アイテムを追加
unity_gamekit_ui(widgetType='list', {
    "operation": "addItem",
    "listId": "inventory",
    "item": {
        "id": "potion_hp",
        "name": "HPポーション",
        "description": "HPを50回復",
        "iconPath": "Assets/Icons/potion_hp.png",
        "quantity": 3
    }
})
```

### UISlot（スロット）

MCP ツール: `unity_gamekit_ui(widgetType='slot')`

単体スロットとスロットバー（複数スロット）を生成します。インベントリ、装備、クイックスロットに適しています。

**オペレーション:**
- スロット: `create`, `update`, `inspect`, `delete`, `setItem`, `clearSlot`, `setHighlight`
- スロットバー: `createSlotBar`, `updateSlotBar`, `inspectSlotBar`, `deleteSlotBar`
- その他: `useSlot`, `refreshFromInventory`, `findBySlotId`, `findByBarId`

**スロットタイプ:** `storage`, `equipment`, `quickslot`, `trash`

**使用例:**
```python
# クイックスロットバーを作成
unity_gamekit_ui(widgetType='slot', {
    "operation": "createSlotBar",
    "barId": "quickbar",
    "slotCount": 8,
    "slotType": "quickslot",
    "layout": "horizontal",
    "dragDropEnabled": true
})

# スロットにアイテムをセット
unity_gamekit_ui(widgetType='slot', {
    "operation": "setItem",
    "slotId": "quickbar_slot_0",
    "itemId": "potion_hp",
    "itemName": "HPポーション",
    "quantity": 3,
    "iconPath": "Assets/Icons/potion_hp.png"
})
```

### UISelection（選択グループ）

MCP ツール: `unity_gamekit_ui(widgetType='selection')`

ラジオボタン、トグル、チェックボックス、タブなどの選択グループを生成します。選択時のパネル表示/非表示を制御する SelectionAction 機能も備えています。

**オペレーション:** `create`, `update`, `inspect`, `delete`, `setItems`, `addItem`, `removeItem`, `clear`, `selectItem`, `selectItemById`, `deselectItem`, `clearSelection`, `setSelectionActions`, `setItemEnabled`, `findBySelectionId`

**選択タイプ:** `radio`, `toggle`, `checkbox`, `tab`

**使用例:**
```python
# タブグループを作成
unity_gamekit_ui(widgetType='selection', {
    "operation": "create",
    "selectionId": "mainTabs",
    "selectionType": "tab",
    "layout": "horizontal",
    "items": [
        {"id": "inventory", "label": "インベントリ"},
        {"id": "equipment", "label": "装備"},
        {"id": "skills", "label": "スキル"}
    ]
})

# タブ切替時のパネル表示制御
unity_gamekit_ui(widgetType='selection', {
    "operation": "setSelectionActions",
    "selectionId": "mainTabs",
    "actions": [
        {"selectedId": "inventory",
         "showPaths": ["InventoryPanel"],
         "hidePaths": ["EquipmentPanel", "SkillPanel"]},
        {"selectedId": "equipment",
         "showPaths": ["EquipmentPanel"],
         "hidePaths": ["InventoryPanel", "SkillPanel"]}
    ]
})
```

---

## ロジックピラー

ロジックピラーはシーンやコードの分析・検証ツールを提供します。

### SceneIntegrity（シーン整合性）

MCP ツール: `unity_validate_integrity`

不足スクリプト、null 参照、壊れたイベントやプレハブをチェックします。

### ClassCatalog（クラスカタログ）

MCP ツール: `unity_class_catalog`

プロジェクト内のクラス、MonoBehaviour、enum 等を列挙・検査します。

### ClassDependencyGraph（クラス依存関係）

MCP ツール: `unity_class_dependency_graph`

C# スクリプトの依存関係を分析します。

### SceneReferenceGraph（シーン参照分析）

MCP ツール: `unity_scene_reference_graph`

シーン内 GameObject 間の参照関係を分析します。

### SceneRelationshipGraph（シーン関係分析）

MCP ツール: `unity_scene_relationship_graph`

シーン遷移と関係性を分析します。

---

## コード生成の仕組み

GameKit のハンドラー（UI ピラー）は、テンプレートベースのコード生成を採用しています。

### ワークフロー

```
MCP ツール呼び出し（create オペレーション）
    ↓
ハンドラーがテンプレート変数を準備
    ↓
CodeGenHelper.GenerateAndAttach() 呼び出し
    ↓
テンプレート (.cs.txt) から C# スクリプトを生成
    ↓
UI Toolkit: UXML/USS ファイルも生成（UIピラーのみ）
    ↓
AssetDatabase.ImportAsset() でコンパイル開始
    ↓
unity_compilation_await でコンパイル待ち（必須）
    ↓
コンポーネントが GameObject にアタッチ
```

### テンプレート一覧

| テンプレート | ピラー | 説明 |
|:---|:---|:---|
| `UICommand.cs.txt` | UI | コマンドパネル |
| `UIBinding.cs.txt` | UI | データバインディング |
| `UIList.cs.txt` | UI | リスト/グリッド |
| `UISlot.cs.txt` | UI | スロット/スロットバー |
| `UISelection.cs.txt` | UI | 選択グループ |

### 生成コードの特徴

- **ランタイム依存ゼロ**: `using UnityEngine` 等の標準 Unity API のみ使用
- **レジストリパターン**: `FindById(id)` で他のスクリプトから簡単にアクセス
- **UnityEvent 連携**: 外部からの購読が可能なイベントを公開
- **Inspector 対応**: シリアライズフィールドでエディタ上から設定可能

### 生成先

デフォルトの出力先: `Assets/Scripts/Generated/`

---

## コンパイル待ちについて

`create` オペレーション（および `createSlotBar`, `createManager`）は C# スクリプトを生成するため、コンパイルが完了するまで待つ必要があります。

```python
# 1. コンポーネントを作成
unity_gamekit_ui(widgetType='command', {
    "operation": "createCommandPanel",
    "panelId": "controls",
    "commands": [...]
})

# 2. コンパイル完了を待つ（必須）
unity_compilation_await()

# 3. 後続の操作が可能に
unity_gamekit_ui(widgetType='command', {
    "operation": "addCommand",
    "panelId": "controls",
    "command": {"name": "fire", "label": "Fire", "commandType": "action"}
})
```

---

## MCP ツール一覧

| MCP ツール名 | ブリッジ名 | ピラー | 説明 |
|:---|:---|:---|:---|
| `unity_gamekit_ui(widgetType='command')` | gamekitUICommand | UI | コマンドパネル |
| `unity_gamekit_ui(widgetType='binding')` | gamekitUIBinding | UI | データバインディング |
| `unity_gamekit_ui(widgetType='list')` | gamekitUIList | UI | リスト/グリッド |
| `unity_gamekit_ui(widgetType='slot')` | gamekitUISlot | UI | スロット |
| `unity_gamekit_ui(widgetType='selection')` | gamekitUISelection | UI | 選択グループ |
| `unity_validate_integrity` | sceneIntegrity | ロジック | シーン整合性 |
| `unity_class_catalog` | classCatalog | ロジック | クラスカタログ |
| `unity_class_dependency_graph` | classDependencyGraph | ロジック | 依存関係分析 |
| `unity_scene_reference_graph` | sceneReferenceGraph | ロジック | 参照分析 |
| `unity_scene_relationship_graph` | sceneRelationshipGraph | ロジック | 関係分析 |

---

## 関連ドキュメント

- [GameKit 完全ガイド（英語）](../MCPServer/SKILL_GAMEKIT.md) - 全ツールの詳細パラメータ
- [全42ツール リファレンス](../MCPServer/SKILL.md) - GameKit 含む全ツール
- [はじめに](../GETTING_STARTED.md) - セットアップ
- [例](../Examples/README.md) - 実践チュートリアル

---

<div align="center">

[📚 インデックスに戻る](../INDEX.md) | [🚀 はじめに](../GETTING_STARTED.md) | [💡 例](../Examples/README.md)

</div>
