# GameKit Machinations Asset

マキネーションダイアグラム（Machinations diagram）をScriptableObjectとして管理し、再利用可能な経済システムを構築します。

## 概要

`GameKitMachinationsAsset`は、ゲームの経済システムやリソースフローを視覚的に定義するためのアセットです。以下の要素で構成されます：

### 構成要素

1. **Resource Pools（リソースプール）**
   - 各リソースの初期値、最小値、最大値を定義
   - 例：HP、MP、Gold、Energy

2. **Resource Flows（リソースフロー）**
   - 時間経過による自動生成/消費
   - 例：毎秒1ずつMPが回復、毎秒0.5ずつEnergyが減少

3. **Resource Converters（リソースコンバーター）**
   - あるリソースを別のリソースに変換
   - 例：Gold 10 → HP 50に変換

4. **Resource Triggers（リソーストリガー）**
   - リソースが閾値に達したときにイベント発火
   - 例：HPが0以下になったら「死亡イベント」発火

## Unity エディタでの使い方

### 1. マキネーションアセットの作成

```
Unity Editor:
1. Project ウィンドウで右クリック
2. Create > UnityAIForge > GameKit > Machinations Diagram
3. 名前を付けて保存（例：PlayerEconomy）
```

### 2. アセットの設定

Inspector で以下を設定：

#### Resource Pools
```
- resourceName: "health"
  initialAmount: 100
  minValue: 0
  maxValue: 100

- resourceName: "mana"
  initialAmount: 50
  minValue: 0
  maxValue: 50
```

#### Resource Flows
```
- flowId: "manaRegen"
  resourceName: "mana"
  ratePerSecond: 1.0
  isSource: true (生成)
  enabledByDefault: true
```

#### Resource Converters
```
- converterId: "healthPotion"
  fromResource: "gold"
  toResource: "health"
  conversionRate: 50 (1 gold → 50 health)
  inputCost: 10 (10 gold必要)
  enabledByDefault: true
```

#### Resource Triggers
```
- triggerName: "lowHealth"
  resourceName: "health"
  thresholdType: Below
  thresholdValue: 20
  enabledByDefault: true
```

### 3. GameKitManagerに適用

#### Inspector から設定（推奨）
```
1. GameKitManager の ManagerType を ResourcePool に設定
2. GameKitResourceManager コンポーネントで
   Machinations Asset フィールドに作成したアセットをドラッグ&ドロップ
3. Playすると自動的にアセットの設定が適用される
```

#### スクリプトから適用
```csharp
using UnityEngine;
using UnityAIForge.GameKit;

public class EconomySetup : MonoBehaviour
{
    [SerializeField] private GameKitMachinationsAsset machinationsAsset;
    [SerializeField] private GameKitManager manager;

    void Start()
    {
        var resourceManager = manager.GetComponent<GameKitResourceManager>();
        resourceManager.ApplyMachinationsAsset(machinationsAsset, resetExisting: true);
    }
}
```

## MCP（AIアシスタント）からの使い方

### 1. マキネーションアセットの作成

```python
# シンプルな経済システム
result = await call_tool("gamekitMachinations", "create", {
    "diagramId": "SimpleEconomy",
    "assetPath": "Assets/Economy/SimpleEconomy.asset",
    "description": "Basic player resource system",
    "pools": [
        {
            "resourceName": "health",
            "initialAmount": 100,
            "minValue": 0,
            "maxValue": 100
        },
        {
            "resourceName": "mana",
            "initialAmount": 50,
            "minValue": 0,
            "maxValue": 50
        },
        {
            "resourceName": "gold",
            "initialAmount": 0,
            "minValue": 0,
            "maxValue": 999999
        }
    ],
    "flows": [
        {
            "flowId": "manaRegen",
            "resourceName": "mana",
            "ratePerSecond": 1.0,
            "isSource": True
        }
    ],
    "converters": [
        {
            "converterId": "buyHealthPotion",
            "fromResource": "gold",
            "toResource": "health",
            "conversionRate": 50,
            "inputCost": 10
        }
    ],
    "triggers": [
        {
            "triggerName": "playerDied",
            "resourceName": "health",
            "thresholdType": "Below",
            "thresholdValue": 1
        }
    ]
})
```

### 2. アセットの更新

```python
# 既存アセットに新しいリソースを追加
result = await call_tool("gamekitMachinations", "update", {
    "assetPath": "Assets/Economy/SimpleEconomy.asset",
    "addPools": [
        {
            "resourceName": "energy",
            "initialAmount": 100,
            "minValue": 0,
            "maxValue": 100
        }
    ],
    "addFlows": [
        {
            "flowId": "energyDrain",
            "resourceName": "energy",
            "ratePerSecond": 0.5,
            "isSource": False  # Drain (消費)
        }
    ]
})
```

### 3. アセットの確認

```python
# アセットの内容を確認
result = await call_tool("gamekitMachinations", "inspect", {
    "assetPath": "Assets/Economy/SimpleEconomy.asset"
})

# 結果:
# {
#   "found": True,
#   "diagramId": "SimpleEconomy",
#   "pools": [...],
#   "flows": [...],
#   "converters": [...],
#   "triggers": [...],
#   "summary": "Machinations Diagram 'SimpleEconomy':\n  Pools: 4\n  Flows: 2\n  Converters: 1\n  Triggers: 1"
# }
```

### 4. Managerに適用

```python
# まずGameKitManagerを作成
manager_result = await call_tool("gamekitManager", "create", {
    "managerId": "PlayerEconomy",
    "managerType": "resourcepool",
    "persistent": True
})

# マキネーションアセットを適用
apply_result = await call_tool("gamekitMachinations", "apply", {
    "assetPath": "Assets/Economy/SimpleEconomy.asset",
    "managerId": "PlayerEconomy",
    "resetExisting": True  # 既存のリソースをクリア
})
```

### 5. 実行時設定のエクスポート

```python
# GameKitResourceManagerの現在の設定をアセットとしてエクスポート
export_result = await call_tool("gamekitMachinations", "export", {
    "managerId": "PlayerEconomy",
    "assetPath": "Assets/Economy/ExportedEconomy.asset",
    "diagramId": "ExportedPlayerEconomy"
})
```

## 使用例

### RPGの経済システム

```python
# RPGの完全な経済システムを作成
result = await call_tool("gamekitMachinations", "create", {
    "diagramId": "RPGEconomy",
    "assetPath": "Assets/Game/Economy/RPGEconomy.asset",
    "pools": [
        {"resourceName": "hp", "initialAmount": 100, "minValue": 0, "maxValue": 100},
        {"resourceName": "mp", "initialAmount": 50, "minValue": 0, "maxValue": 50},
        {"resourceName": "gold", "initialAmount": 100, "minValue": 0, "maxValue": 999999},
        {"resourceName": "exp", "initialAmount": 0, "minValue": 0, "maxValue": 999999},
        {"resourceName": "stamina", "initialAmount": 100, "minValue": 0, "maxValue": 100}
    ],
    "flows": [
        {"flowId": "mpRegen", "resourceName": "mp", "ratePerSecond": 0.5, "isSource": True},
        {"flowId": "staminaRegen", "resourceName": "stamina", "ratePerSecond": 2.0, "isSource": True}
    ],
    "converters": [
        # ポーション
        {"converterId": "healthPotion", "fromResource": "gold", "toResource": "hp", 
         "conversionRate": 50, "inputCost": 10},
        {"converterId": "manaPotion", "fromResource": "gold", "toResource": "mp", 
         "conversionRate": 30, "inputCost": 15},
        
        # スキル使用
        {"converterId": "fireballSpell", "fromResource": "mp", "toResource": "exp", 
         "conversionRate": 10, "inputCost": 20},
        {"converterId": "dashAbility", "fromResource": "stamina", "toResource": "exp", 
         "conversionRate": 5, "inputCost": 30}
    ],
    "triggers": [
        {"triggerName": "playerDied", "resourceName": "hp", "thresholdType": "Below", "thresholdValue": 1},
        {"triggerName": "lowMana", "resourceName": "mp", "thresholdType": "Below", "thresholdValue": 10},
        {"triggerName": "lowStamina", "resourceName": "stamina", "thresholdType": "Below", "thresholdValue": 20},
        {"triggerName": "levelUp", "resourceName": "exp", "thresholdType": "Above", "thresholdValue": 100}
    ]
})
```

### タワーディフェンスの経済システム

```python
result = await call_tool("gamekitMachinations", "create", {
    "diagramId": "TowerDefenseEconomy",
    "assetPath": "Assets/Game/Economy/TDEconomy.asset",
    "pools": [
        {"resourceName": "gold", "initialAmount": 200, "minValue": 0, "maxValue": 999999},
        {"resourceName": "lives", "initialAmount": 20, "minValue": 0, "maxValue": 20},
        {"resourceName": "wave", "initialAmount": 1, "minValue": 1, "maxValue": 100}
    ],
    "flows": [
        # 時間経過でゴールド増加
        {"flowId": "passiveGoldIncome", "resourceName": "gold", "ratePerSecond": 5, "isSource": True}
    ],
    "converters": [
        # タワー建設
        {"converterId": "basicTower", "fromResource": "gold", "toResource": "wave", 
         "conversionRate": 0, "inputCost": 50},
        {"converterId": "advancedTower", "fromResource": "gold", "toResource": "wave", 
         "conversionRate": 0, "inputCost": 150}
    ],
    "triggers": [
        {"triggerName": "gameOver", "resourceName": "lives", "thresholdType": "Below", "thresholdValue": 1},
        {"triggerName": "bossWave", "resourceName": "wave", "thresholdType": "Equal", "thresholdValue": 10}
    ]
})
```

## ベストプラクティス

### 1. リソースの命名規則
- 小文字とアンダースコアを使用：`health`, `max_hp`, `gold_coins`
- 一貫性のある命名：`hp` / `mp` / `sp` など

### 2. フローの設定
- `ratePerSecond` は小さめの値から始める（バランス調整しやすい）
- デバッグ時は大きな値で動作確認

### 3. コンバーターの設計
- `conversionRate` と `inputCost` を明確に区別
- 例：10 gold → 50 hp の場合
  - `inputCost: 10` (必要なgold)
  - `conversionRate: 50` (得られるhp)

### 4. トリガーの活用
- 重要な閾値にトリガーを設定
- UI更新やサウンド再生に活用

### 5. アセットの再利用
- 共通の経済システムはアセット化
- プレハブとして配布可能
- プロジェクト間で共有可能

## トラブルシューティング

### Q: アセットを適用してもリソースが初期化されない
A: `resetExisting: true` を指定してください

### Q: Flowが動作しない
A: `enabledByDefault: true` が設定されているか確認してください

### Q: Converterが実行されない
A: 十分なリソースがあるか、`enabled` が true か確認してください

### Q: Triggerが発火しない
A: Threshold値を超えたかどうか、リソースの変化を確認してください

## 関連ドキュメント

- [GameKitResourceManager.README.md](./GameKitResourceManager.README.md) - リソースマネージャーの詳細
- [GameKitManager README](./README.md) - GameKitManager全体のドキュメント

## API リファレンス

### MCP Operations

| Operation | Description |
|-----------|-------------|
| `create` | 新しいマキネーションアセットを作成 |
| `update` | 既存のアセットを更新 |
| `inspect` | アセットの内容を確認 |
| `delete` | アセットを削除 |
| `apply` | アセットをGameKitManagerに適用 |
| `export` | GameKitResourceManagerの設定をアセットとしてエクスポート |

### C# API

```csharp
// アセットを適用
resourceManager.ApplyMachinationsAsset(asset, resetExisting: true);

// 現在の設定をエクスポート
var exportedAsset = resourceManager.ExportToAsset("MyDiagram");

// アセットの検証
if (asset.Validate(out string error))
{
    Debug.Log("Valid asset");
}
else
{
    Debug.LogError($"Invalid asset: {error}");
}
```

