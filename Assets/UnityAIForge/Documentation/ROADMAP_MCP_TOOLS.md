# MCP Tools Implementation Roadmap

GameKitに依存しない、LLMが自律的にゲーム開発を行うための必須MCPツール実装計画。

## 背景

LLMはC#スクリプトを自分で生成できるため、GameKitのような高レベル抽象化は必須ではない。
しかし、以下の機能がないとLLMは効率的に開発・デバッグができない：

1. **実行してテストする手段がない** → PlayMode制御が必要
2. **エラーを確認する手段がない** → Consoleログ取得が必要
3. **見た目を調整できない** → Material/Light/Particle設定が必要

---

## Phase 1: 開発サイクル基盤 (最優先)

### 1.1 unity_playmode_control

**目的**: LLMがゲームを実行してテストできるようにする

| 操作 | 説明 |
|------|------|
| `play` | プレイモード開始 |
| `pause` | プレイモード一時停止 |
| `stop` | プレイモード終了 |
| `step` | 1フレーム進める (pause中) |
| `getState` | 現在の状態取得 (playing/paused/stopped) |

**C# Handler**: `PlayModeControlHandler.cs`

```csharp
// 主要実装ポイント
using UnityEditor;

public class PlayModeControlHandler : McpCommandHandlerBase
{
    protected override async Task<McpResponse> ProcessCommandAsync(McpRequest request)
    {
        switch (request.operation)
        {
            case "play":
                EditorApplication.isPlaying = true;
                break;
            case "pause":
                EditorApplication.isPaused = !EditorApplication.isPaused;
                break;
            case "stop":
                EditorApplication.isPlaying = false;
                break;
            case "step":
                EditorApplication.Step();
                break;
            case "getState":
                return Success(new {
                    isPlaying = EditorApplication.isPlaying,
                    isPaused = EditorApplication.isPaused
                });
        }
        return Success();
    }
}
```

**Python Tool定義**:

```python
Tool(
    name="unity_playmode_control",
    description="Control Unity Editor play mode for testing games",
    inputSchema={
        "type": "object",
        "properties": {
            "operation": {
                "type": "string",
                "enum": ["play", "pause", "stop", "step", "getState"]
            }
        },
        "required": ["operation"]
    }
)
```

---

### 1.2 unity_console_log

**目的**: コンパイルエラー・ランタイムエラー・Debug.Logを取得

| 操作 | 説明 |
|------|------|
| `getRecent` | 直近N件のログ取得 |
| `getErrors` | エラーのみ取得 |
| `getWarnings` | 警告のみ取得 |
| `clear` | コンソールクリア |
| `getCompilationErrors` | コンパイルエラー詳細取得 |

**C# Handler**: `ConsoleLogHandler.cs`

```csharp
// 主要実装ポイント
using UnityEditor;
using System.Reflection;

public class ConsoleLogHandler : McpCommandHandlerBase
{
    // Unity内部のLogEntriesクラスを利用
    private static readonly Type LogEntriesType =
        typeof(Editor).Assembly.GetType("UnityEditor.LogEntries");

    protected override async Task<McpResponse> ProcessCommandAsync(McpRequest request)
    {
        switch (request.operation)
        {
            case "getRecent":
                int count = request.GetInt("count", 50);
                return Success(GetRecentLogs(count));
            case "getErrors":
                return Success(GetLogsByType(LogType.Error));
            case "clear":
                LogEntriesType.GetMethod("Clear").Invoke(null, null);
                return Success();
            case "getCompilationErrors":
                return Success(GetCompilationErrors());
        }
        return Success();
    }

    private List<LogEntry> GetRecentLogs(int count)
    {
        // LogEntriesからログを取得
        // ...
    }
}
```

**出力形式**:

```json
{
  "logs": [
    {
      "type": "error",
      "message": "NullReferenceException: Object reference not set...",
      "stackTrace": "at PlayerController.Update() in Assets/Scripts/PlayerController.cs:42",
      "timestamp": "2024-01-15T10:30:00Z"
    }
  ],
  "summary": {
    "errors": 1,
    "warnings": 3,
    "logs": 15
  }
}
```

---

## Phase 2: ビジュアル制御 (重要)

### 2.1 unity_material_bundle

**目的**: マテリアル作成・シェーダー設定

| 操作 | 説明 |
|------|------|
| `create` | 新規マテリアル作成 |
| `update` | プロパティ更新 |
| `setTexture` | テクスチャ設定 |
| `setColor` | カラー設定 |
| `applyPreset` | プリセット適用 |
| `inspect` | マテリアル情報取得 |
| `applyToObjects` | 複数オブジェクトに適用 |

**プリセット**:

| プリセット名 | 説明 | 対応パイプライン |
|-------------|------|-----------------|
| `unlit` | ライティングなし | All |
| `lit` | 標準ライティング | All |
| `transparent` | 透明 | All |
| `sprite` | スプライト用 | All |
| `ui` | UI用 | All |
| `emissive` | 発光 | URP/HDRP |
| `toon` | トゥーンシェーディング | URP |

**入力スキーマ例**:

```json
{
  "operation": "create",
  "name": "PlayerMaterial",
  "preset": "lit",
  "properties": {
    "color": "#FF5500",
    "metallic": 0.5,
    "smoothness": 0.8
  },
  "savePath": "Assets/Materials/PlayerMaterial.mat"
}
```

---

### 2.2 unity_light_bundle

**目的**: ライト作成・設定

| 操作 | 説明 |
|------|------|
| `create` | ライト作成 |
| `update` | ライト設定更新 |
| `inspect` | ライト情報取得 |
| `applyPreset` | プリセット適用 |
| `createLightingSetup` | 完全なライティングセットアップ作成 |

**プリセット**:

| プリセット名 | 内容 |
|-------------|------|
| `daylight` | 暖色の太陽光 + スカイボックス |
| `nighttime` | 月明かり + 青みがかった環境光 |
| `indoor` | 室内照明 (Point Light中心) |
| `dramatic` | コントラスト強めの演出照明 |
| `studio` | 3点照明 (Key/Fill/Back) |

**入力スキーマ例**:

```json
{
  "operation": "create",
  "lightType": "directional",
  "preset": "daylight",
  "properties": {
    "intensity": 1.2,
    "color": "#FFF4E0",
    "shadows": "soft",
    "rotation": {"x": 50, "y": -30, "z": 0}
  }
}
```

---

### 2.3 unity_particle_bundle

**目的**: パーティクルシステム作成・設定

| 操作 | 説明 |
|------|------|
| `create` | パーティクルシステム作成 |
| `update` | 設定更新 |
| `applyPreset` | プリセット適用 |
| `play` | 再生 |
| `stop` | 停止 |
| `inspect` | 設定取得 |

**プリセット**:

| プリセット名 | 用途 |
|-------------|------|
| `explosion` | 爆発エフェクト |
| `fire` | 炎 |
| `smoke` | 煙 |
| `sparkle` | キラキラ |
| `rain` | 雨 |
| `snow` | 雪 |
| `dust` | ほこり |
| `trail` | 軌跡 |
| `hit` | ヒットエフェクト |
| `heal` | 回復エフェクト |

**入力スキーマ例**:

```json
{
  "operation": "create",
  "name": "ExplosionFX",
  "preset": "explosion",
  "overrides": {
    "startSize": 2.0,
    "startLifetime": 1.5,
    "startColor": "#FF6600"
  },
  "position": {"x": 0, "y": 0, "z": 0}
}
```

---

## Phase 3: アニメーション・イベント (推奨)

### 3.1 unity_animation3d_bundle

**目的**: 3Dキャラクターアニメーション設定

| 操作 | 説明 |
|------|------|
| `setupAnimator` | Animatorコンポーネント設定 |
| `createController` | AnimatorController作成 |
| `addState` | ステート追加 |
| `addTransition` | トランジション追加 |
| `setParameter` | パラメータ設定 |
| `addBlendTree` | BlendTree追加 |
| `createAvatarMask` | アバターマスク作成 |
| `inspect` | 設定取得 |

**入力スキーマ例**:

```json
{
  "operation": "createController",
  "name": "PlayerAnimator",
  "savePath": "Assets/Animations/PlayerAnimator.controller",
  "parameters": [
    {"name": "Speed", "type": "float"},
    {"name": "IsGrounded", "type": "bool"},
    {"name": "Jump", "type": "trigger"}
  ],
  "states": [
    {"name": "Idle", "clip": "Assets/Animations/Idle.anim", "isDefault": true},
    {"name": "Walk", "clip": "Assets/Animations/Walk.anim"},
    {"name": "Run", "clip": "Assets/Animations/Run.anim"},
    {"name": "Jump", "clip": "Assets/Animations/Jump.anim"}
  ],
  "transitions": [
    {"from": "Idle", "to": "Walk", "conditions": [{"param": "Speed", "mode": "greater", "value": 0.1}]},
    {"from": "Walk", "to": "Run", "conditions": [{"param": "Speed", "mode": "greater", "value": 0.5}]},
    {"from": "Any", "to": "Jump", "conditions": [{"param": "Jump", "mode": "trigger"}]}
  ]
}
```

---

### 3.2 unity_event_wiring

**目的**: UnityEventの動的接続

| 操作 | 説明 |
|------|------|
| `wire` | UnityEventにリスナー追加 |
| `unwire` | リスナー削除 |
| `inspect` | 接続状況確認 |
| `listEvents` | コンポーネントのイベント一覧取得 |

**入力スキーマ例**:

```json
{
  "operation": "wire",
  "source": {
    "gameObject": "UI/StartButton",
    "component": "Button",
    "event": "onClick"
  },
  "target": {
    "gameObject": "GameManager",
    "component": "GameController",
    "method": "StartGame"
  }
}
```

---

## 実装順序

```
Phase 1 (必須・即座に実装)
├── 1.1 unity_playmode_control  ← テスト実行に必須
└── 1.2 unity_console_log       ← デバッグに必須

Phase 2 (重要・早期に実装)
├── 2.1 unity_material_bundle   ← 見た目の調整
├── 2.2 unity_light_bundle      ← シーンの雰囲気
└── 2.3 unity_particle_bundle   ← エフェクト

Phase 3 (推奨・順次実装)
├── 3.1 unity_animation3d_bundle ← 3Dゲーム用
└── 3.2 unity_event_wiring       ← コード削減
```

---

## ファイル構成

```
Assets/UnityAIForge/
├── Editor/MCPBridge/Handlers/
│   ├── PlayModeControlHandler.cs      # Phase 1.1
│   ├── ConsoleLogHandler.cs           # Phase 1.2
│   ├── MaterialBundleHandler.cs       # Phase 2.1
│   ├── LightBundleHandler.cs          # Phase 2.2
│   ├── ParticleBundleHandler.cs       # Phase 2.3
│   ├── Animation3DBundleHandler.cs    # Phase 3.1
│   └── EventWiringHandler.cs          # Phase 3.2
│
└── MCPServer/src/tools/
    └── register_tools.py              # 各ツールの定義追加
```

---

## 各Handlerの共通実装パターン

```csharp
using UnityEngine;
using UnityEditor;
using MCP.Editor.Base;

namespace MCP.Editor.Handlers
{
    public class NewToolHandler : McpCommandHandlerBase
    {
        public override string CommandName => "newTool";

        protected override Task<McpResponse> ProcessCommandAsync(McpRequest request)
        {
            string operation = request.GetString("operation");

            return operation switch
            {
                "create" => HandleCreate(request),
                "update" => HandleUpdate(request),
                "inspect" => HandleInspect(request),
                _ => Task.FromResult(Error($"Unknown operation: {operation}"))
            };
        }

        private Task<McpResponse> HandleCreate(McpRequest request)
        {
            // 実装
            return Task.FromResult(Success(new { created = true }));
        }
    }
}
```

---

## Python Tool定義テンプレート

```python
# register_tools.py に追加

Tool(
    name="unity_new_tool",
    description="Description of the tool",
    inputSchema={
        "type": "object",
        "properties": {
            "operation": {
                "type": "string",
                "enum": ["create", "update", "inspect"],
                "description": "Operation to perform"
            },
            # 追加のプロパティ
        },
        "required": ["operation"]
    }
)
```

---

## 成功指標

| ツール | 成功指標 |
|--------|----------|
| playmode_control | LLMがプレイ→エラー確認→修正のサイクルを回せる |
| console_log | コンパイルエラーとランタイムエラーを正確に取得できる |
| material_bundle | 10種類以上のマテリアルプリセットが動作する |
| light_bundle | 5種類のライティングセットアップが1コマンドで作成できる |
| particle_bundle | 10種類のVFXプリセットが動作する |
| animation3d_bundle | Humanoid AnimatorControllerを自動生成できる |
| event_wiring | Button.onClickなど主要イベントを接続できる |

---

## 参考: 既存ツールとの関係

| 新ツール | 関連既存ツール | 補完関係 |
|----------|---------------|----------|
| playmode_control | compilation_await | コンパイル待機後に実行 |
| console_log | compilation_await | コンパイルエラー詳細を取得 |
| material_bundle | asset_crud | マテリアルアセット特化 |
| light_bundle | gameobject_crud + component_crud | ライト設定特化 |
| particle_bundle | gameobject_crud + component_crud | パーティクル設定特化 |
| animation3d_bundle | animation2d_bundle | 3D版として並立 |
| event_wiring | component_crud | イベント接続特化 |
