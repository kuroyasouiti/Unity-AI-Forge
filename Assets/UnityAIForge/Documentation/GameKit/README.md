# GameKit フレームワーク

<div align="center">

**3ピラーアーキテクチャによるAI駆動ゲーム開発フレームワーク**

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
- **3ピラー構造**: UI、プレゼンテーション、ロジックの3層で包括的なゲーム機能を提供

---

## 3ピラーアーキテクチャ

```
GameKit Framework
├── UI ピラー（5ツール）── UI コンポーネント生成
│   ├── UICommand ─────── ボタンコマンドパネル
│   ├── UIBinding ─────── データバインディング
│   ├── UIList ────────── リスト／グリッド表示
│   ├── UISlot ────────── スロット（インベントリ等）
│   └── UISelection ───── 選択グループ（ラジオ/タブ等）
│
├── プレゼンテーションピラー（5ツール）── 演出・フィードバック
│   ├── AnimationSync ─── アニメーションパラメータ同期
│   ├── Effect ────────── 複合エフェクト（パーティクル/サウンド/カメラ）
│   ├── Feedback ──────── ゲームフィール（ヒットストップ/画面シェイク等）
│   ├── VFX ───────────── ビジュアルエフェクトラッパー
│   └── Audio ─────────── サウンド管理（SFX/BGM/環境音等）
│
└── ロジックピラー（5ツール）── 分析・検証
    ├── SceneIntegrity ── シーン整合性チェック
    ├── ClassCatalog ──── クラス一覧と検査
    ├── ClassDependencyGraph ── クラス依存関係分析
    ├── SceneReferenceGraph ── シーン参照分析
    └── SceneRelationshipGraph ── シーン関係分析
```

---

## UI ピラー

UI ピラーは UI Toolkit（UXML/USS）を使用したUI コンポーネントを生成します。生成された C# スクリプトは `FindById()` による静的レジストリパターンを採用し、他のコンポーネントから簡単にアクセスできます。

### UICommand（コマンドパネル）

MCP ツール: `unity_gamekit_ui_command`

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
unity_gamekit_ui_command({
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

MCP ツール: `unity_gamekit_ui_binding`

UI 要素をデータソースに宣言的にバインドします。ProgressBar、Label、Slider 等をリアルタイムに更新できます。

**オペレーション:** `create`, `update`, `inspect`, `delete`, `setRange`, `refresh`, `findByBindingId`

**ソースタイプ:** `health`, `economy`, `timer`, `custom`

**表示フォーマット:** `raw`, `percent`, `formatted`, `ratio`

**使用例:**
```python
# HPバーのバインディングを作成
unity_gamekit_ui_binding({
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

MCP ツール: `unity_gamekit_ui_list`

ScrollView ベースの動的リスト/グリッド表示を生成します。アイテム管理と選択機能を備えています。

**オペレーション:** `create`, `update`, `inspect`, `delete`, `setItems`, `addItem`, `removeItem`, `clear`, `selectItem`, `deselectItem`, `clearSelection`, `refreshFromSource`, `findByListId`

**レイアウト:** `vertical`, `horizontal`, `grid`

**使用例:**
```python
# インベントリリストを作成
unity_gamekit_ui_list({
    "operation": "create",
    "listId": "inventory",
    "layout": "grid",
    "columns": 4,
    "dataSource": "inventory",
    "selectable": true,
    "multiSelect": false
})

# アイテムを追加
unity_gamekit_ui_list({
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

MCP ツール: `unity_gamekit_ui_slot`

単体スロットとスロットバー（複数スロット）を生成します。インベントリ、装備、クイックスロットに適しています。

**オペレーション:**
- スロット: `create`, `update`, `inspect`, `delete`, `setItem`, `clearSlot`, `setHighlight`
- スロットバー: `createSlotBar`, `updateSlotBar`, `inspectSlotBar`, `deleteSlotBar`
- その他: `useSlot`, `refreshFromInventory`, `findBySlotId`, `findByBarId`

**スロットタイプ:** `storage`, `equipment`, `quickslot`, `trash`

**使用例:**
```python
# クイックスロットバーを作成
unity_gamekit_ui_slot({
    "operation": "createSlotBar",
    "barId": "quickbar",
    "slotCount": 8,
    "slotType": "quickslot",
    "layout": "horizontal",
    "dragDropEnabled": true
})

# スロットにアイテムをセット
unity_gamekit_ui_slot({
    "operation": "setItem",
    "slotId": "quickbar_slot_0",
    "itemId": "potion_hp",
    "itemName": "HPポーション",
    "quantity": 3,
    "iconPath": "Assets/Icons/potion_hp.png"
})
```

### UISelection（選択グループ）

MCP ツール: `unity_gamekit_ui_selection`

ラジオボタン、トグル、チェックボックス、タブなどの選択グループを生成します。選択時のパネル表示/非表示を制御する SelectionAction 機能も備えています。

**オペレーション:** `create`, `update`, `inspect`, `delete`, `setItems`, `addItem`, `removeItem`, `clear`, `selectItem`, `selectItemById`, `deselectItem`, `clearSelection`, `setSelectionActions`, `setItemEnabled`, `findBySelectionId`

**選択タイプ:** `radio`, `toggle`, `checkbox`, `tab`

**使用例:**
```python
# タブグループを作成
unity_gamekit_ui_selection({
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
unity_gamekit_ui_selection({
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

## プレゼンテーションピラー

プレゼンテーションピラーはゲームの演出・フィードバック・音響を担当します。

### AnimationSync（アニメーション同期）

MCP ツール: `unity_gamekit_animation_sync`

Animator パラメータをゲーム状態（速度、HP等）に宣言的に同期します。

**オペレーション:** `create`, `update`, `inspect`, `delete`, `addSyncRule`, `removeSyncRule`, `addTriggerRule`, `removeTriggerRule`, `fireTrigger`, `setParameter`, `findBySyncId`

**同期ルール - ソースタイプ:**
- `rigidbody3d` / `rigidbody2d` - Rigidbody の速度等
- `transform` - Transform の位置/回転等
- `health` - HP コンポーネント
- `custom` - カスタムソース

**トリガールール - イベントソース:**
- `health` - HP イベント（OnDamaged, OnHealed, OnDeath 等）
- `input` - 入力アクション
- `manual` - 手動トリガー

**使用例:**
```python
# アニメーション同期コンポーネントを作成
unity_gamekit_animation_sync({
    "operation": "create",
    "syncId": "playerAnim",
    "autoFindAnimator": true,
    "syncRules": [
        {"parameter": "Speed", "parameterType": "float",
         "sourceType": "rigidbody3d",
         "sourceProperty": "velocity.magnitude", "multiplier": 1.0},
        {"parameter": "IsGrounded", "parameterType": "bool",
         "sourceType": "transform",
         "sourceProperty": "position.y", "boolThreshold": 0.1}
    ],
    "triggers": [
        {"triggerName": "Hit", "eventSource": "health",
         "healthEvent": "OnDamaged"},
        {"triggerName": "Die", "eventSource": "health",
         "healthEvent": "OnDeath"}
    ]
})
```

### Effect（複合エフェクト）

MCP ツール: `unity_gamekit_effect`

パーティクル、サウンド、カメラシェイク、画面フラッシュ、タイムスケールを組み合わせた複合エフェクトシステムです。EffectManager による一元管理も可能です。

**オペレーション:** `create`, `update`, `inspect`, `delete`, `addComponent`, `removeComponent`, `clearComponents`, `play`, `playAtPosition`, `playAtTransform`, `shakeCamera`, `flashScreen`, `setTimeScale`, `createManager`, `registerEffect`, `unregisterEffect`, `findByEffectId`, `listEffects`

**エフェクトコンポーネントタイプ:**
- `particle` - パーティクルシステム
- `sound` - サウンド再生
- `cameraShake` - カメラ振動
- `screenFlash` - 画面フラッシュ
- `timeScale` - スローモーション

**使用例:**
```python
# 爆発エフェクトを作成
unity_gamekit_effect({
    "operation": "create",
    "effectId": "explosion",
    "components": [
        {"type": "particle", "prefabPath": "Assets/VFX/Explosion.prefab",
         "duration": 2.0},
        {"type": "sound", "clipPath": "Assets/Audio/Explosion.wav",
         "volume": 0.8},
        {"type": "cameraShake", "intensity": 0.5, "shakeDuration": 0.3},
        {"type": "screenFlash", "color": {"r": 1, "g": 0.8, "b": 0, "a": 0.5},
         "flashDuration": 0.1}
    ]
})

# エフェクトマネージャーを作成して登録
unity_gamekit_effect({
    "operation": "createManager",
    "managerId": "globalEffects",
    "persistent": true
})
```

### Feedback（ゲームフィール）

MCP ツール: `unity_gamekit_feedback`

ヒットストップ、画面シェイク、フラッシュ、スケールパンチなどのゲームフィールエフェクトを管理します。

**オペレーション:** `create`, `update`, `inspect`, `delete`, `addComponent`, `clearComponents`, `setIntensity`, `findByFeedbackId`

**フィードバックコンポーネントタイプ:**
- `hitstop` - 時間停止エフェクト
- `screenShake` - 画面振動
- `flash` / `colorFlash` - 画面フラッシュ
- `scale` / `position` / `rotation` - トランスフォームパンチ
- `sound` - サウンドフィードバック
- `particle` - パーティクルエフェクト
- `haptic` - コントローラー振動

**使用例:**
```python
# ヒットフィードバックを作成
unity_gamekit_feedback({
    "operation": "create",
    "feedbackId": "onHit",
    "playOnEnable": false,
    "globalIntensityMultiplier": 1.0,
    "components": [
        {"type": "hitstop", "duration": 0.05, "hitstopTimeScale": 0.0},
        {"type": "screenShake", "duration": 0.2, "intensity": 0.3,
         "shakeFrequency": 25},
        {"type": "flash", "duration": 0.1, "color": {"r": 1, "g": 0, "b": 0, "a": 0.3}},
        {"type": "scale", "duration": 0.15, "intensity": 1.2}
    ]
})
```

### VFX（ビジュアルエフェクト）

MCP ツール: `unity_gamekit_vfx`

ParticleSystem のラッパーコンポーネントを生成します。オブジェクトプーリングと各種パラメータの動的制御が可能です。

**オペレーション:** `create`, `update`, `inspect`, `delete`, `setMultipliers`, `setColor`, `setLoop`, `findByVFXId`

**使用例:**
```python
# VFXコンポーネントを作成
unity_gamekit_vfx({
    "operation": "create",
    "vfxId": "fireTrail",
    "particlePrefabPath": "Assets/VFX/FireTrail.prefab",
    "autoPlay": true,
    "loop": true,
    "usePooling": true,
    "poolSize": 10,
    "sizeMultiplier": 1.5,
    "emissionMultiplier": 2.0
})
```

### Audio（サウンド管理）

MCP ツール: `unity_gamekit_audio`

SFX、BGM、環境音、ボイス、UI サウンドを管理するコンポーネントを生成します。フェードイン/フェードアウト、3D サウンド、ピッチバリエーションに対応しています。

**オペレーション:** `create`, `update`, `inspect`, `delete`, `setVolume`, `setPitch`, `setLoop`, `setClip`, `findByAudioId`

**オーディオタイプ:** `sfx`, `music`, `ambient`, `voice`, `ui`

**使用例:**
```python
# BGM コンポーネントを作成
unity_gamekit_audio({
    "operation": "create",
    "audioId": "bgm_battle",
    "audioType": "music",
    "audioClipPath": "Assets/Audio/BattleBGM.ogg",
    "playOnEnable": true,
    "loop": true,
    "volume": 0.7,
    "fadeInDuration": 2.0,
    "fadeOutDuration": 1.5
})

# SEコンポーネントを作成
unity_gamekit_audio({
    "operation": "create",
    "audioId": "sfx_sword",
    "audioType": "sfx",
    "audioClipPath": "Assets/Audio/SwordSwing.wav",
    "volume": 0.9,
    "pitchVariation": 0.1,
    "spatialBlend": 1.0,
    "minDistance": 1,
    "maxDistance": 20
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

GameKit のハンドラー（UI ピラー、プレゼンテーションピラー）は、テンプレートベースのコード生成を採用しています。

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
| `AnimationSync.cs.txt` | プレゼンテーション | アニメーション同期 |
| `Effect.cs.txt` | プレゼンテーション | 個別エフェクト |
| `EffectManager.cs.txt` | プレゼンテーション | エフェクトマネージャー |
| `Feedback.cs.txt` | プレゼンテーション | ゲームフィール |
| `VFX.cs.txt` | プレゼンテーション | VFX ラッパー |
| `Audio.cs.txt` | プレゼンテーション | オーディオラッパー |

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
unity_gamekit_ui_command({
    "operation": "createCommandPanel",
    "panelId": "controls",
    "commands": [...]
})

# 2. コンパイル完了を待つ（必須）
unity_compilation_await()

# 3. 後続の操作が可能に
unity_gamekit_ui_command({
    "operation": "addCommand",
    "panelId": "controls",
    "command": {"name": "fire", "label": "Fire", "commandType": "action"}
})
```

---

## MCP ツール一覧

| MCP ツール名 | ブリッジ名 | ピラー | 説明 |
|:---|:---|:---|:---|
| `unity_gamekit_ui_command` | gamekitUICommand | UI | コマンドパネル |
| `unity_gamekit_ui_binding` | gamekitUIBinding | UI | データバインディング |
| `unity_gamekit_ui_list` | gamekitUIList | UI | リスト/グリッド |
| `unity_gamekit_ui_slot` | gamekitUISlot | UI | スロット |
| `unity_gamekit_ui_selection` | gamekitUISelection | UI | 選択グループ |
| `unity_gamekit_animation_sync` | gamekitAnimationSync | プレゼンテーション | アニメーション同期 |
| `unity_gamekit_effect` | gamekitEffect | プレゼンテーション | 複合エフェクト |
| `unity_gamekit_feedback` | gamekitFeedback | プレゼンテーション | ゲームフィール |
| `unity_gamekit_vfx` | gamekitVFX | プレゼンテーション | VFX ラッパー |
| `unity_gamekit_audio` | gamekitAudio | プレゼンテーション | サウンド管理 |
| `unity_validate_integrity` | sceneIntegrity | ロジック | シーン整合性 |
| `unity_class_catalog` | classCatalog | ロジック | クラスカタログ |
| `unity_class_dependency_graph` | classDependencyGraph | ロジック | 依存関係分析 |
| `unity_scene_reference_graph` | sceneReferenceGraph | ロジック | 参照分析 |
| `unity_scene_relationship_graph` | sceneRelationshipGraph | ロジック | 関係分析 |

---

## 関連ドキュメント

- [GameKit 完全ガイド（英語）](../MCPServer/SKILL_GAMEKIT.md) - 全ツールの詳細パラメータ
- [全49ツール リファレンス](../MCPServer/SKILL.md) - GameKit 含む全ツール
- [はじめに](../GETTING_STARTED.md) - セットアップ
- [例](../Examples/README.md) - 実践チュートリアル

---

<div align="center">

[📚 インデックスに戻る](../INDEX.md) | [🚀 はじめに](../GETTING_STARTED.md) | [💡 例](../Examples/README.md)

</div>
