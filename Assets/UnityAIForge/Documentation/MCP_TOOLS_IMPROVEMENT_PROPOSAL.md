# MCP Tools 機能重複分析と改善提案

## 概要

37のMCPツールを分析した結果、以下の重複・改善点を発見しました。

---

## 1. 機能重複の発見

### 1.1 UI ナビゲーション機能の重複 (高優先度)

| ツール | 操作 | 機能 |
|--------|------|------|
| `unity_ui_hierarchy` | `setNavigation` | 基本的なナビゲーション設定 |
| `unity_ui_navigation` | 8種類の操作 | 詳細なナビゲーション管理 |

**問題点:**
- `unity_ui_hierarchy.setNavigation` は `unity_ui_navigation` の機能サブセット
- ユーザーがどちらを使うべきか混乱する

**改善案 A: 統合**
```
unity_ui_hierarchy から setNavigation を削除
→ ナビゲーション設定は unity_ui_navigation に一本化
```

**改善案 B: 役割明確化**
```
unity_ui_hierarchy.setNavigation: 簡易設定（auto-vertical/horizontal のみ）
unity_ui_navigation: 詳細設定（explicit, group, wrapAround など）
→ ドキュメントで使い分けを明記
```

**推奨:** 改善案A（統合）

---

### 1.2 UI 要素作成機能の重複 (中優先度)

| ツール | 操作 | 機能 |
|--------|------|------|
| `unity_ui_foundation` | `createButton`, `createPanel`, etc. | 個別UI要素作成 |
| `unity_ui_hierarchy` | `create` | 宣言的階層UI作成 |

**問題点:**
- 両方で Panel, Button, Text, Image 等を作成可能
- `unity_ui_hierarchy` 内で同様のUI作成ロジックを重複実装

**改善案 A: 役割分担明確化**
```
unity_ui_foundation: 単一要素作成 + LayoutGroup管理
unity_ui_hierarchy: 複数要素の一括作成（内部でFoundationを呼び出し）
```

**改善案 B: 統合**
```
unity_ui を単一ツールに統合:
- operation: "createSingle", "createHierarchy", "addLayoutGroup", etc.
```

**推奨:** 改善案A（役割分担維持、ドキュメント改善）

---

### 1.3 GameKit Interaction と TriggerZone の重複 (高優先度)

| ツール | 用途 | トリガー機能 |
|--------|------|-------------|
| `unity_gamekit_interaction` | 汎用インタラクション | collision, trigger, proximity, input |
| `unity_gamekit_trigger_zone` | エリアベースのエフェクト | 特化型ゾーン（checkpoint, damage, heal, teleport等） |

**問題点:**
- 両方ともトリガー検出 + アクション実行
- `interaction` の `actions` と `trigger_zone` の `zoneType` 効果が重複
- 例: ダメージゾーン
  - `interaction` で `actions: [{type: "sendMessage", target: "Health", parameter: "TakeDamage"}]`
  - `trigger_zone` で `zoneType: "damagezone", effectAmount: 10`

**改善案 A: 統合**
```
unity_gamekit_trigger を新設:
- zoneType プリセット（checkpoint, damage 等）を維持
- カスタムアクション（spawnPrefab, sendMessage 等）も対応
- interaction と trigger_zone を deprecate
```

**改善案 B: 役割明確化**
```
unity_gamekit_interaction: カスタムアクション向け（スクリプト連携）
unity_gamekit_trigger_zone: ビルトインエフェクト向け（ノーコード）
→ 相互参照・連携なし
```

**推奨:** 改善案B（役割明確化）+ ドキュメント充実

---

### 1.4 AI パトロールと Waypoint の類似性 (低優先度)

| ツール | 機能 |
|--------|------|
| `unity_gamekit_ai` | パトロール、追跡、逃走（AIビヘイビア） |
| `unity_gamekit_waypoint` | パス追従（移動ロジックのみ） |

**問題点:**
- AI の `patrol` モードと Waypoint の基本機能が類似
- ユーザーが「敵のパトロール」にどちらを使うべきか迷う

**改善案:**
```
役割を明確にドキュメント化:
- AI: 「知能的ビヘイビア」（ターゲット検知、状態遷移）
- Waypoint: 「単純パス追従」（移動プラットフォーム、非AIキャラ）

オプション: AI内でWaypointを活用するオプション追加
  "patrolPointsSource": "waypoint"
  "waypointId": "patrol_route_1"
```

**推奨:** ドキュメント改善のみ（統合不要）

---

### 1.5 Actor の aiAutonomous と AI ツールの関係 (中優先度)

| ツール | 機能 |
|--------|------|
| `unity_gamekit_actor` | `controlMode: "aiAutonomous"` |
| `unity_gamekit_ai` | AI ビヘイビア追加 |

**問題点:**
- Actor の `aiAutonomous` は AI ツールの機能を暗黙的に含む想定か？
- 両方を使う必要があるのか不明

**現状の調査結果:**
```csharp
// GameKitActorHandler.cs
case "aiAutonomous":
    // AIコントローラーロジックを追加（基本的なワンダー等）
    break;
```
→ Actor.aiAutonomous は基本AIのみ。高度なAI（パトロール、追跡）は別途 `unity_gamekit_ai` が必要

**改善案:**
```
ドキュメントで明記:
- Actor.aiAutonomous: 基本的な自律行動（ランダム移動）
- gamekit_ai: 高度なAIビヘイビア（パトロール、追跡、状態遷移）
- 使用例: Actor(aiAutonomous) + AI(patrolAndChase) の組み合わせ
```

---

### 1.6 スプライト生成機能の分散 (低優先度)

| ツール | 機能 |
|--------|------|
| `unity_vector_sprite_convert` | プリミティブ/SVG/テクスチャからスプライト生成 |
| `unity_sprite2d_bundle` | SpriteRenderer管理 + スプライトシートスライス |

**問題点:**
- スプライト関連機能が2つのツールに分散
- `vector_sprite_convert` はアセット生成、`sprite2d_bundle` はシーンオブジェクト管理

**改善案:**
```
現状維持で問題なし:
- vector_sprite_convert: アセット生成パイプライン
- sprite2d_bundle: ランタイムオブジェクト管理
→ 役割が異なるため分離は適切
```

---

### 1.7 物理プリセットの重複 (低優先度)

| ツール | プリセット |
|--------|-----------|
| `unity_physics_bundle` | `character` プリセット |
| `unity_character_controller_bundle` | FPS/TPS/platformer プリセット |

**問題点:**
- `physics_bundle.character` は Rigidbody ベース
- `character_controller_bundle` は CharacterController ベース
- 同じ「キャラクター」だが物理システムが異なる

**改善案:**
```
ドキュメントで明確化:
- physics_bundle.character: 物理ベースキャラ（ラグドール対応、衝突反応）
- character_controller_bundle: 制御ベースキャラ（精密な移動制御、階段昇降）

命名案:
- physics_bundle: "characterPhysics" に改名
- character_controller_bundle: 現状維持
```

---

## 2. 改善優先度まとめ

| 優先度 | 項目 | 推奨アクション |
|--------|------|--------------|
| **高** | UIナビゲーション重複 | `ui_hierarchy.setNavigation` 削除 |
| **高** | Interaction/TriggerZone重複 | 役割明確化 + ドキュメント |
| **中** | UI要素作成重複 | ドキュメント改善 |
| **中** | Actor.aiAutonomous/AI関係 | ドキュメント改善 |
| **低** | AI/Waypoint類似性 | ドキュメント改善 |
| **低** | スプライト機能分散 | 現状維持 |
| **低** | 物理プリセット重複 | ドキュメント改善 |

---

## 3. 具体的な改善アクション

### 3.1 コード変更（高優先度）

#### `unity_ui_hierarchy` から `setNavigation` 操作を削除

**ファイル:** `UIHierarchyHandler.cs`

```csharp
// 変更前
private static readonly string[] Operations =
{
    "create", "clone", "inspect", "delete",
    "show", "hide", "toggle", "setNavigation",  // 削除対象
};

// 変更後
private static readonly string[] Operations =
{
    "create", "clone", "inspect", "delete",
    "show", "hide", "toggle",
};
```

**ファイル:** `register_tools.py` のスキーマからも `setNavigation` 関連を削除

### 3.2 ドキュメント改善

#### 新規ドキュメント: 「ツール選択ガイド」

```markdown
# ツール選択ガイド

## トリガー/インタラクション

| ユースケース | 推奨ツール |
|------------|----------|
| ドア、スイッチ、宝箱 | `unity_gamekit_interaction` |
| ダメージゾーン、ヒールゾーン | `unity_gamekit_trigger_zone` |
| チェックポイント、テレポーター | `unity_gamekit_trigger_zone` |
| カスタムスクリプト連携 | `unity_gamekit_interaction` |

## AIとパス追従

| ユースケース | 推奨ツール |
|------------|----------|
| 敵の巡回 + プレイヤー追跡 | `unity_gamekit_ai` |
| 移動プラットフォーム | `unity_gamekit_waypoint` |
| NPCの固定ルート移動 | `unity_gamekit_waypoint` |
| AIボス（複雑な行動パターン） | `unity_gamekit_ai` |

## 物理設定

| ユースケース | 推奨ツール |
|------------|----------|
| FPS/TPSプレイヤー | `unity_character_controller_bundle` |
| プラットフォーマーキャラ | `unity_physics_bundle` (platformer) |
| 車両/物理オブジェクト | `unity_physics_bundle` |
| ラグドール対応キャラ | `unity_physics_bundle` (character) |
```

---

## 4. 長期的な改善案

### 4.1 ツール統合案

現在37ツールを、以下のように整理統合することを検討：

```
現状: 37ツール
→ 目標: 25-30ツール

統合候補:
1. unity_ui_foundation + unity_ui_hierarchy → unity_ui (10操作)
2. unity_gamekit_interaction + unity_gamekit_trigger_zone → unity_gamekit_trigger (15操作)
3. unity_physics_bundle + unity_character_controller_bundle → unity_physics (10操作)
```

### 4.2 命名規則の統一

```
現状の不整合:
- unity_XXX_crud (CRUD操作)
- unity_XXX_bundle (プリセット適用)
- unity_gamekit_XXX (GameKit系)

提案:
- unity_core_XXX: 基本操作（scene, gameobject, component, asset）
- unity_preset_XXX: プリセット適用（physics, camera, audio）
- unity_gamekit_XXX: GameKit系（現状維持）
```

---

## 5. 結論

### 即座に対応すべき項目:
1. `unity_ui_hierarchy.setNavigation` の削除
2. ツール選択ガイドドキュメントの作成

### 次フェーズで対応すべき項目:
1. `unity_gamekit_interaction` と `unity_gamekit_trigger_zone` の役割明確化
2. 各ツールのdescriptionに「いつ使うべきか」を追記

### 長期的に検討すべき項目:
1. ツール数の削減（統合）
2. 命名規則の統一
