# レジューム機能付き逐次処理ツール

## 概要

`unity_batch_sequential_execute` は、複数のUnity操作を逐次実行し、エラー時に処理を保存して後から再開できる強力なツールです。

## 特徴

✅ **逐次実行**: 操作を1つずつ順番に実行
✅ **エラーハンドリング**: エラー発生時に即座に停止
✅ **自動保存**: 残りの処理をツール側に自動保存
✅ **レジューム機能**: エラー修正後、中断した場所から再開
✅ **進捗確認**: リソースから現在の状態を参照可能
✅ **クリーンスタート**: 保存状態をクリアして新しいバッチを開始

## 使用方法

### 1. 基本的な使い方（新しいバッチ実行）

```python
unity_batch_sequential_execute({
    "operations": [
        {
            "tool": "unity_gameobject_crud",
            "arguments": {
                "operation": "create",
                "name": "Player",
                "parentPath": "Characters"
            }
        },
        {
            "tool": "unity_component_crud",
            "arguments": {
                "operation": "add",
                "gameObjectPath": "Characters/Player",
                "componentType": "UnityEngine.Rigidbody2D"
            }
        },
        {
            "tool": "unity_gamekit_actor",
            "arguments": {
                "operation": "create",
                "actorId": "player_001",
                "behaviorProfile": "2dPhysics"
            }
        }
    ],
    "resume": false,
    "stop_on_error": true
})
```

### 2. エラーからの再開

エラーが発生した場合、レスポンスは以下のようになります：

```json
{
  "success": false,
  "stopped_at_index": 1,
  "completed": [
    {
      "index": 0,
      "tool": "unity_gameobject_crud",
      "success": true
    }
  ],
  "errors": [
    {
      "index": 1,
      "tool": "unity_component_crud",
      "error": "GameObject not found: Characters/Player"
    }
  ],
  "remaining_operations": 2,
  "message": "Execution stopped at operation 2 due to error. Use resume=true to continue.",
  "last_error": "GameObject not found: Characters/Player"
}
```

エラーを修正したら、`resume: true` で再開：

```python
unity_batch_sequential_execute({
    "resume": true,
    "stop_on_error": true
})
```

### 3. 進捗状態の確認

リソースから現在の進捗を確認できます：

```
リソース URI: batch://queue/status
```

レスポンス例：

```json
{
  "operations": [...],
  "current_index": 1,
  "remaining_count": 2,
  "completed_count": 1,
  "total_count": 3,
  "last_error": "GameObject not found: Characters/Player",
  "last_error_index": 1,
  "started_at": "2024-12-03T10:30:00Z",
  "last_updated": "2024-12-03T10:30:15Z",
  "next_operation": {
    "tool": "unity_component_crud",
    "arguments": {...}
  },
  "can_resume": true,
  "resume_hint": "Call unity_batch_sequential_execute with resume=true to continue from operation 2/3"
}
```

## パラメータ

### `operations` (array, オプション*)

実行する操作のリスト。各操作には以下が含まれます：

- `tool` (string, 必須): ツール名（例: `"unity_gameobject_crud"`）
- `arguments` (object, 必須): ツールの引数

*注: `resume: false` の場合は必須

### `resume` (boolean, デフォルト: false)

- `true`: 前回のエラー地点から再開
- `false`: 新しいバッチを開始（既存の保存状態をクリア）

### `stop_on_error` (boolean, デフォルト: true)

- `true`: エラー発生時に即座に停止
- `false`: エラーが発生しても続行（非推奨）

## レスポンスフォーマット

### 成功時

```json
{
  "success": true,
  "completed": [
    {
      "index": 0,
      "tool": "unity_gameobject_crud",
      "success": true,
      "result": {...}
    },
    {
      "index": 1,
      "tool": "unity_component_crud",
      "success": true,
      "result": {...}
    }
  ],
  "errors": [],
  "total_operations": 2,
  "message": "All 2 operations completed successfully."
}
```

### エラー時

```json
{
  "success": false,
  "stopped_at_index": 1,
  "completed": [...],
  "errors": [
    {
      "index": 1,
      "tool": "unity_component_crud",
      "error": "エラーメッセージ"
    }
  ],
  "remaining_operations": 5,
  "message": "Execution stopped at operation 2 due to error. Use resume=true to continue.",
  "last_error": "エラーメッセージ"
}
```

## ユースケース

### 1. 複雑なシーンセットアップ

```python
unity_batch_sequential_execute({
    "operations": [
        # 1. シーン作成
        {"tool": "unity_scene_crud", "arguments": {"operation": "create", "scenePath": "Assets/Scenes/Level1.unity"}},
        # 2. 地面作成
        {"tool": "unity_gameobject_crud", "arguments": {"operation": "create", "name": "Ground"}},
        # 3. 物理コンポーネント追加
        {"tool": "unity_physics_bundle", "arguments": {"operation": "applyPreset2D", "gameObjectPath": "Ground", "preset": "static"}},
        # 4. プレイヤー作成
        {"tool": "unity_gamekit_actor", "arguments": {"operation": "create", "actorId": "player", "behaviorProfile": "2dPhysics"}},
        # 5. カメラ作成
        {"tool": "unity_camera_bundle", "arguments": {"operation": "create", "name": "MainCam", "preset": "thirdPerson"}}
    ]
})
```

### 2. 依存関係のあるオブジェクト作成

```python
unity_batch_sequential_execute({
    "operations": [
        # 親オブジェクト作成
        {"tool": "unity_gameobject_crud", "arguments": {"operation": "create", "name": "Enemies"}},
        # 子オブジェクト作成（親に依存）
        {"tool": "unity_gameobject_crud", "arguments": {"operation": "create", "name": "Enemy1", "parentPath": "Enemies"}},
        {"tool": "unity_gameobject_crud", "arguments": {"operation": "create", "name": "Enemy2", "parentPath": "Enemies"}},
        # 各敵にコンポーネント追加（子オブジェクトに依存）
        {"tool": "unity_gamekit_actor", "arguments": {"operation": "create", "actorId": "enemy1", "behaviorProfile": "2dPhysics", "controlMode": "aiAutonomous"}},
        {"tool": "unity_gamekit_actor", "arguments": {"operation": "create", "actorId": "enemy2", "behaviorProfile": "2dPhysics", "controlMode": "aiAutonomous"}}
    ]
})
```

### 3. 段階的なゲームシステム構築

```python
# フェーズ1: 基本セットアップ
unity_batch_sequential_execute({
    "operations": [
        {"tool": "unity_gamekit_manager", "arguments": {"operation": "create", "managerId": "game_manager", "managerType": "resourcePool"}},
        {"tool": "unity_gamekit_sceneflow", "arguments": {"operation": "create", "flowId": "main_flow"}}
    ]
})

# エラーがあれば修正して再開
unity_batch_sequential_execute({"resume": true})

# フェーズ2: インタラクション追加
unity_batch_sequential_execute({
    "operations": [
        {"tool": "unity_gamekit_interaction", "arguments": {...}},
        {"tool": "unity_gamekit_ui(widgetType='command')", "arguments": {...}}
    ]
})
```

## ベストプラクティス

### ✅ 推奨

1. **適切な粒度で分割**: 1つのバッチに5-20操作程度
2. **依存関係を考慮**: 親オブジェクトを先に作成
3. **エラー後は確認**: リソースで状態を確認してから再開
4. **段階的に実行**: 大きなタスクは複数のバッチに分割

### ❌ 避けるべき

1. **1つのバッチに100以上の操作**: タイムアウトのリスク
2. **依存関係の逆転**: 子オブジェクトを親より先に作成
3. **無条件に再実行**: エラー原因を確認せずに `resume: false`
4. **stop_on_error: false**: エラーが連鎖する可能性

## 保存ファイル

バッチキューの状態は以下に保存されます：

```
Assets/UnityAIForge/MCPServer/.batch_queue_state.json
```

このファイルは自動管理されるため、手動で編集しないでください。

## トラブルシューティング

### 問題: 状態がクリアされない

**解決策**: `resume: false` で新しいバッチを開始すると、既存の状態がクリアされます。

```python
unity_batch_sequential_execute({
    "operations": [...],
    "resume": false  # 既存の状態をクリア
})
```

### 問題: レジューム時に古い操作が実行される

**原因**: 前回のバッチが完了していない

**解決策**: リソースで状態を確認し、不要な場合は `resume: false` でクリア

### 問題: エラーメッセージが不明確

**解決策**: 個別のツールで操作をテストしてから、バッチに追加

```python
# テスト
unity_gameobject_crud({
    "operation": "create",
    "name": "TestObject"
})

# 問題なければバッチに追加
unity_batch_sequential_execute({
    "operations": [
        {"tool": "unity_gameobject_crud", "arguments": {"operation": "create", "name": "TestObject"}}
    ]
})
```

## まとめ

`unity_batch_sequential_execute` は、複雑なUnity操作を安全かつ効率的に実行するための強力なツールです。エラーハンドリングとレジューム機能により、大規模なシーンセットアップやゲームシステム構築を確実に完了できます。

**重要なポイント**:
- 逐次実行で依存関係を保証
- エラー時に自動保存
- `resume: true` で中断から再開
- リソースで進捗確認
- `resume: false` で新規開始

---

[📚 MCPServerドキュメントに戻る](README.md) | [🔧 全ツールリファレンス](SKILL.md)

