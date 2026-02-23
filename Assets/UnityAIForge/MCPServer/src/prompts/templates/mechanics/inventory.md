# インベントリシステム実装ガイド (Unity-AI-Forge {VERSION})

## 概要

インベントリシステムはプレイヤーが収集したアイテムを管理するゲームの中核機能です。
グリッド型のアイテム一覧、装備スロット、消耗品スタック管理など、RPGやアクションゲームで
広く使われるパターンを Unity-AI-Forge のツール群で効率的に構築できます。

`unity_gamekit_ui_list` と `unity_gamekit_ui_slot` を組み合わせることで動的に更新される
グリッド UI を素早く生成し、`unity_asset_crud` でインベントリ管理ロジックの
C# スクリプトを作成、`unity_event_wiring` でワールド内のアイテムピックアップイベントを接続できます。

---

## 設計パターン

### データ駆動型アイテム定義

ScriptableObject でアイテムデータを定義し、ランタイムは参照のみを持つ設計が推奨です。

```
ItemData (ScriptableObject)
  - itemId: string       # 一意の識別子
  - itemName: string     # 表示名
  - description: string  # 説明文
  - icon: Sprite         # UI表示用アイコン
  - category: enum       # Weapon / Armor / Consumable / Material
  - maxStack: int        # スタック上限 (非スタック = 1)
  - value: int           # 売値
```

### インベントリの種類

- **グリッドインベントリ**: UIList + UISlot で格子状に配置（アイテム箱型）
- **装備インベントリ**: 固定スロット型（頭/胴/腕/脚/武器/盾）
- **クイックスロット**: ホットバー型（数字キーや方向パッドで即使用）
- **ショップ/取引**: 2列表示（所持品 と 店舗在庫）

### アーキテクチャ図

```
InventoryManager (MonoBehaviour Singleton)
  +-- List<InventorySlot> slots        # スロットデータ
  +-- AddItem(ItemData, amount)        # アイテム追加
  +-- RemoveItem(itemId, amount)       # アイテム削除
  +-- HasItem(itemId) : bool           # 所持確認
  +-- GetStackCount(itemId) : int      # スタック数取得

[UIList] <- InventoryManager.OnInventoryChanged イベント
  +-- [UISlot] x N (動的生成)
       +-- アイコン表示
       +-- スタック数テキスト
       +-- 選択状態ハイライト
```

---

## 推奨フォルダ構造

```
Assets/
  Scripts/
    Inventory/
      InventoryManager.cs      # インベントリ管理シングルトン
      InventorySlot.cs         # スロットデータクラス
      ItemData.cs              # アイテム ScriptableObject 定義
      ItemPickup.cs            # ワールドアイテムのピックアップ処理
      EquipmentManager.cs      # 装備管理
  Data/
    Items/
      Weapons/
        Sword_Iron.asset
      Consumables/
        Potion_Health.asset
  Prefabs/
    Items/
      WorldItem_Prefab.prefab  # ワールド配置アイテムのプレハブ
```

---

## 実装ワークフロー (MCPツール使用例付き)

### Step 1: アイテムデータ定義スクリプトの作成

```python
# アイテムデータ ScriptableObject クラスを作成
unity_asset_crud(
    operation="create",
    assetPath="Assets/Scripts/Inventory/ItemData.cs",
    content="""using UnityEngine;
[CreateAssetMenu(fileName = \"NewItem\", menuName = \"Game/ItemData\")]
public class ItemData : ScriptableObject {
    public string itemId;
    public string itemName;
    [TextArea] public string description;
    public Sprite icon;
    public enum Category { Weapon, Armor, Consumable, Material }
    public Category category;
    public int maxStack = 1;
    public int value;
}"""
)

# コンパイル待ち
unity_compilation_await(operation="await", timeoutSeconds=30)

# 具体的なアイテムデータを作成
unity_scriptableObject_crud(
    operation="create",
    typeName="ItemData",
    assetPath="Assets/Data/Items/Consumables/Potion_Health.asset",
    properties={
        "itemId":      "potion_health_001",
        "itemName":    "体力回復ポーション",
        "description": "HPを50回復する",
        "category":    1,
        "maxStack":    99,
        "value":       50
    }
)
```

### Step 2: インベントリ管理スクリプトの作成

```python
# InventoryManager を作成
unity_asset_crud(
    operation="create",
    assetPath="Assets/Scripts/Inventory/InventoryManager.cs",
    content="""using UnityEngine;
using System;
using System.Collections.Generic;
public class InventoryManager : MonoBehaviour {
    public static InventoryManager Instance { get; private set; }
    [SerializeField] private int maxSlots = 24;
    private List<InventorySlot> slots = new();
    public event Action OnInventoryChanged;
    void Awake() { Instance = this; }
    public bool AddItem(ItemData item, int amount = 1) {
        // スタック可能なスロットを探して追加
        OnInventoryChanged?.Invoke();
        return true;
    }
    public void RemoveItem(string itemId, int amount = 1) {
        OnInventoryChanged?.Invoke();
    }
}
[Serializable]
public class InventorySlot {
    public ItemData itemData;
    public int amount;
}"""
)

# コンパイル待ち
unity_compilation_await(operation="await", timeoutSeconds=30)
```

### Step 3: インベントリ UI のセットアップ

```python
# Canvas とインベントリパネルを作成
unity_ui_foundation(
    operation="createCanvas",
    name="InventoryCanvas",
    renderMode="ScreenSpaceOverlay"
)

unity_ui_foundation(
    operation="createPanel",
    name="InventoryPanel",
    parentPath="InventoryCanvas"
)

# UIList でグリッドアイテム一覧を生成
unity_gamekit_ui_list(
    operation="create",
    parentPath="InventoryCanvas/InventoryPanel",
    listId="inventory_list",
    name="InventoryList",
    layout="grid",
    columns=6,
    selectable=true
)

# コンパイル待ち（コード生成後は必須）
unity_compilation_await(operation="await", timeoutSeconds=30)
```

### Step 4: 装備スロットバーの設定

```python
# 装備スロットバーを作成（6スロット: 頭/胴/腕/脚/武器/盾）
unity_gamekit_ui_slot(
    operation="createSlotBar",
    barId="equipment_bar",
    parentPath="InventoryCanvas",
    name="EquipmentBar",
    slotCount=6,
    layout="vertical",
    spacing=8,
    slotSize={"x": 64, "y": 64}
)

# コンパイル待ち
unity_compilation_await(operation="await", timeoutSeconds=30)
```

### Step 5: アイテムピックアップの実装

```python
# アイテムピックアップスクリプトを作成
unity_asset_crud(
    operation="create",
    assetPath="Assets/Scripts/Inventory/ItemPickup.cs",
    content="""using UnityEngine;
public class ItemPickup : MonoBehaviour {
    [SerializeField] private ItemData itemData;
    [SerializeField] private int amount = 1;
    private void OnTriggerEnter(Collider other) {
        if (other.CompareTag(\"Player\")) {
            if (InventoryManager.Instance.AddItem(itemData, amount)) {
                Destroy(gameObject);
            }
        }
    }
}"""
)

unity_compilation_await(operation="await", timeoutSeconds=30)

# ワールドアイテム GameObject を作成
unity_gameobject_crud(
    operation="create",
    name="HealthPotion_World",
    components=[
        {"type": "UnityEngine.SphereCollider",
         "properties": {"isTrigger": true, "radius": 1.5}},
        {"type": "ItemPickup"}
    ]
)

# ピックアップ時の演出エフェクト
unity_gamekit_effect(
    operation="create",
    effectId="pickup_effect",
    components=[
        {"type": "sound",    "volume": 0.7},
        {"type": "particle", "duration": 0.5}
    ]
)

# コンパイル待ち
unity_compilation_await(operation="await", timeoutSeconds=30)
```

### Step 6: アイテム選択 UI の実装

```python
# UISelection で選択状態の管理
unity_gamekit_ui_selection(
    operation="create",
    parentPath="InventoryCanvas/InventoryPanel",
    selectionId="inventory_selection",
    name="InventorySelection",
    selectionType="radio",
    layout="grid"
)

# コンパイル待ち
unity_compilation_await(operation="await", timeoutSeconds=30)

# キーボード/ゲームパッドナビゲーション
unity_ui_navigation(
    operation="autoSetup",
    targetPath="InventoryCanvas/InventoryPanel"
)
```

---

## よくあるパターン

### パターン 1: コンテキストメニュー

スロット選択時に「使用/装備/捨てる/詳細」のコンテキストメニューを表示。

```python
unity_gamekit_ui_command(
    operation="createCommandPanel",
    panelId="item_context_menu",
    parentPath="InventoryCanvas/InventoryPanel",
    commands=[
        {"name": "UseItem",    "label": "使用",   "commandType": "custom"},
        {"name": "EquipItem",  "label": "装備",   "commandType": "custom"},
        {"name": "DropItem",   "label": "捨てる", "commandType": "custom"},
        {"name": "ShowDetail", "label": "詳細",   "commandType": "custom"}
    ],
    layout="vertical"
)
```

### パターン 2: クイックスロットバー

```python
# 画面下部にクイックスロットバー（8スロット）
unity_gamekit_ui_slot(
    operation="createSlotBar",
    barId="quickslot_bar",
    parentPath="InventoryCanvas",
    name="QuickSlotBar",
    slotCount=8,
    layout="horizontal",
    spacing=4,
    slotSize={"x": 48, "y": 48}
)
```

### パターン 3: インベントリの開閉を ui_state で管理

```python
unity_ui_state(
    operation="defineState",
    targetPath="InventoryCanvas",
    stateName="InventoryClosed",
    properties={"InventoryPanel": {"active": false}}
)

unity_ui_state(
    operation="defineState",
    targetPath="InventoryCanvas",
    stateName="InventoryOpen",
    properties={"InventoryPanel": {"active": true}}
)
```

---

## 注意点・落とし穴

1. **コード生成後のコンパイル待ち**
   `unity_asset_crud` でスクリプト作成後、`unity_gamekit_ui_list`、`unity_gamekit_ui_slot` 等の
   コード生成ツール実行後は、必ず `unity_compilation_await` を呼ぶこと。

2. **スタック上限の境界値**
   `maxStack = 1` のアイテム（装備品など）に複数追加しようとした場合のハンドリングを
   必ず実装すること。UI 側でも追加不可を明示する。

3. **GameObject 参照の永続化**
   インベントリ内のアイテム参照はシーンをまたぐと無効になる。
   データは ID ベース（itemId + amount）で保存し、ロード時に再構築する設計にする。

4. **UIList の動的更新**
   インベントリ変更時は UIList の `setItems` operation で更新する。
   差分更新（`addItem` / `removeItem`）を活用するとパフォーマンスが改善する。

5. **ドラッグ中の入力無効化**
   ドラッグ操作中はキーボードショートカットや他の入力を無効化し、
   誤操作を防ぐ処理を入れること。

---

## 関連ツール一覧

| ツール名 | 用途 |
|---|---|
| `unity_asset_crud` | インベントリ管理・アイテムデータの C# スクリプト作成 |
| `unity_scriptableObject_crud` | アイテムデータ (ScriptableObject) の作成・管理 |
| `unity_gamekit_ui_list` | アイテムグリッド一覧の動的生成 |
| `unity_gamekit_ui_slot` | スロットバー・装備スロットの作成 |
| `unity_gamekit_ui_selection` | スロット選択状態の管理 |
| `unity_gamekit_ui_command` | アイテムコンテキストメニュー |
| `unity_gamekit_effect` | ピックアップ時のパーティクル・サウンドエフェクト |
| `unity_gamekit_audio` | ピックアップ・装備・使用時の SE |
| `unity_gameobject_crud` | ワールドアイテム GameObject の作成 |
| `unity_component_crud` | コンポーネントの追加・プロパティ設定 |
| `unity_ui_foundation` | インベントリ UI パネルの基盤構築 |
| `unity_ui_navigation` | キーボード/ゲームパッドナビゲーション |
| `unity_ui_state` | インベントリの開閉状態管理 |
| `unity_event_wiring` | UI イベントとインベントリロジックの接続 |
| `unity_compilation_await` | スクリプト生成後のコンパイル完了待ち |
| `unity_validate_integrity` | 実装後の整合性チェック |
