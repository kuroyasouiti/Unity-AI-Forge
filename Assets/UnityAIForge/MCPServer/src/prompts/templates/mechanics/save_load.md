# セーブ/ロードシステム実装ガイド (Unity-AI-Forge {VERSION})

## 概要

セーブ/ロードシステムはゲームの進行状態を永続化し、プレイヤーが中断・再開できるようにする
重要な基盤機能です。何をどの形式で保存するか、複数スロット対応をするかどうかなど、
設計判断がゲームの品質に直結します。

Unity-AI-Forge では `unity_asset_crud` でセーブ管理の C# スクリプトを作成し、
`unity_gamekit_ui_list` でセーブスロット選択 UI、`unity_event_wiring` で
ボタンとセーブ処理の接続を行います。

---

## 設計パターン

### セーブデータの分類

| 分類 | 内容 | 保存頻度 |
|---|---|---|
| プレイヤーデータ | HP/MP、レベル、スキル | イベント時 |
| インベントリ | 所持アイテム、装備 | アイテム取得時 |
| クエスト進行 | 完了/進行中クエスト | クエスト更新時 |
| ワールド状態 | 開いた扉、倒した敵 | シーン離脱時 |
| ゲーム設定 | 音量、解像度、操作設定 | 設定変更時 |

### 保存方式の選択

```
PlayerPrefs（推奨: 設定データのみ）
  - メリット: 簡単、クロスプラットフォーム
  - デメリット: 容量制限、改ざんしやすい
  - 用途: サウンド音量、グラフィック設定

JSON ファイル（推奨: ゲーム進行データ）
  - メリット: 柔軟、デバッグ容易、移行しやすい
  - デメリット: 改ざん可能（暗号化で対策）
  - 用途: プレイヤーデータ、インベントリ、クエスト

Binary ファイル（推奨: 大容量データ）
  - メリット: 高速、コンパクト、改ざん困難
  - デメリット: デバッグ困難
  - 用途: マップデータ、大規模ワールド
```

### セーブデータの JSON 構造例

```json
{
  "version": 1,
  "saveSlot": 1,
  "timestamp": "2025-01-15T14:30:00",
  "playtime": 7200,
  "player": {
    "position": {"x": 10.5, "y": 0, "z": -3.2},
    "hp": 85, "maxHp": 100, "level": 5, "exp": 1240
  },
  "inventory": [
    {"itemId": "potion_health", "amount": 3},
    {"itemId": "sword_iron",    "amount": 1, "equipped": true}
  ],
  "worldFlags": {
    "dungeon_door_01_open": true,
    "boss_01_defeated": false
  }
}
```

---

## 推奨フォルダ構造

```
Assets/
  Scripts/
    SaveLoad/
      SaveManager.cs          # セーブ/ロードの中核クラス
      SaveData.cs             # セーブデータの構造定義
      SaveSlotInfo.cs         # スロット情報（タイムスタンプ等）
      DataMigrator.cs         # バージョン移行処理
```

---

## 実装ワークフロー (MCPツール使用例付き)

### Step 1: セーブデータ構造の定義

```python
# セーブデータクラスを作成
unity_asset_crud(
    operation="create",
    assetPath="Assets/Scripts/SaveLoad/SaveData.cs",
    content="""using UnityEngine;
using System;
using System.Collections.Generic;
[Serializable]
public class SaveData {
    public int version = 1;
    public int saveSlot;
    public string timestamp;
    public float playtime;
    public PlayerSaveData player;
    public List<ItemSaveData> inventory;
    public Dictionary<string, bool> worldFlags;
}
[Serializable]
public class PlayerSaveData {
    public Vector3 position;
    public int hp, maxHp, level, exp;
}
[Serializable]
public class ItemSaveData {
    public string itemId;
    public int amount;
    public bool equipped;
}"""
)

unity_compilation_await(operation="await", timeoutSeconds=30)
```

### Step 2: SaveManager の作成

```python
# SaveManager を作成
unity_asset_crud(
    operation="create",
    assetPath="Assets/Scripts/SaveLoad/SaveManager.cs",
    content="""using UnityEngine;
using System.IO;
public class SaveManager : MonoBehaviour {
    public static SaveManager Instance { get; private set; }
    private const int MAX_SLOTS = 3;
    private const string SAVE_PREFIX = \"save_slot_\";
    void Awake() {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    public void Save(int slot) {
        var data = CollectSaveData();
        data.saveSlot = slot;
        data.timestamp = System.DateTime.Now.ToString(\"o\");
        var json = JsonUtility.ToJson(data, true);
        var path = GetSavePath(slot);
        File.WriteAllText(path, json);
    }
    public SaveData Load(int slot) {
        var path = GetSavePath(slot);
        if (!File.Exists(path)) return null;
        var json = File.ReadAllText(path);
        return JsonUtility.FromJson<SaveData>(json);
    }
    public void DeleteSave(int slot) {
        var path = GetSavePath(slot);
        if (File.Exists(path)) File.Delete(path);
    }
    private string GetSavePath(int slot) =>
        Path.Combine(Application.persistentDataPath, SAVE_PREFIX + slot + \".json\");
    private SaveData CollectSaveData() => new SaveData();
}"""
)

unity_compilation_await(operation="await", timeoutSeconds=30)

# SaveManager GameObject を作成（DontDestroyOnLoad 対象）
unity_gameobject_crud(
    operation="create",
    name="SaveManager",
    tag="GameController",
    components=[{"type": "SaveManager"}]
)
```

### Step 3: セーブスロット選択 UI の構築

```python
# セーブ/ロード画面パネルを作成
unity_ui_foundation(
    operation="createCanvas",
    name="SaveLoadCanvas",
    renderMode="ScreenSpaceOverlay"
)

unity_ui_foundation(
    operation="createPanel",
    name="SaveLoadPanel",
    parentPath="SaveLoadCanvas"
)

# タブ（セーブ/ロード切り替え）
unity_gamekit_ui_selection(
    operation="create",
    parentPath="SaveLoadCanvas/SaveLoadPanel",
    selectionId="save_load_tabs",
    name="SaveLoadTabs",
    selectionType="tab",
    layout="horizontal",
    items=[
        {"id": "tab_save", "label": "セーブ"},
        {"id": "tab_load", "label": "ロード"}
    ]
)

# セーブスロット一覧を UIList で実装
unity_gamekit_ui_list(
    operation="create",
    parentPath="SaveLoadCanvas/SaveLoadPanel",
    listId="save_slot_list",
    name="SaveSlotList",
    layout="vertical",
    selectable=true,
    items=[
        {"id": "slot_1", "name": "スロット 1", "description": "空きスロット"},
        {"id": "slot_2", "name": "スロット 2", "description": "空きスロット"},
        {"id": "slot_3", "name": "スロット 3", "description": "空きスロット"}
    ]
)

# コンパイル待ち
unity_compilation_await(operation="await", timeoutSeconds=30)
```

### Step 4: 確認ダイアログの構築

```python
# 確認ダイアログパネル
unity_ui_hierarchy(
    operation="create",
    parentPath="SaveLoadCanvas",
    definition={
        "name": "ConfirmDialog",
        "active": false,
        "children": [
            {"type": "Text",   "name": "ConfirmMessage", "text": "上書きしますか？"},
            {"type": "Button", "name": "ConfirmYes",     "text": "はい"},
            {"type": "Button", "name": "ConfirmNo",      "text": "いいえ"}
        ]
    }
)

# ダイアログのイベント接続
unity_event_wiring(
    operation="wire",
    source={"gameObject": "SaveLoadCanvas/ConfirmDialog/ConfirmYes",
            "component": "Button", "event": "onClick"},
    target={"gameObject": "SaveManager", "method": "Save"}
)

unity_event_wiring(
    operation="wire",
    source={"gameObject": "SaveLoadCanvas/ConfirmDialog/ConfirmNo",
            "component": "Button", "event": "onClick"},
    target={"gameObject": "SaveLoadCanvas/ConfirmDialog",
            "method": "SetActive", "mode": "Bool", "argument": false}
)
```

### Step 5: クイックセーブ/ロードの入力設定

```python
# F5/F9 キーで即時セーブ/ロード
unity_input_profile(
    operation="createInputActions",
    targetPath="SaveManager",
    actionMapName="System",
    actions=[
        {"name": "QuickSave", "type": "Button", "binding": "<Keyboard>/f5"},
        {"name": "QuickLoad", "type": "Button", "binding": "<Keyboard>/f9"}
    ]
)
```

### Step 6: オートセーブ通知 UI

```python
# オートセーブアイコン表示用の UICommand
unity_gamekit_ui_command(
    operation="createCommandPanel",
    panelId="autosave_indicator",
    parentPath="SaveLoadCanvas",
    commands=[
        {"name": "ShowAutoSave", "label": "オートセーブ中...", "commandType": "custom"}
    ]
)

# コンパイル待ち
unity_compilation_await(operation="await", timeoutSeconds=30)

# 整合性チェック
unity_validate_integrity(operation="all")
```

---

## よくあるパターン

### パターン 1: チェックポイント自動セーブ

特定エリア進入時に自動セーブをトリガーする。

```python
# チェックポイント GameObject を作成
unity_gameobject_crud(
    operation="create",
    name="CheckpointA",
    components=[
        {"type": "UnityEngine.BoxCollider",
         "properties": {"isTrigger": true, "size": {"x": 3, "y": 3, "z": 3}}}
    ]
)

# チェックポイント到達スクリプトを作成
unity_asset_crud(
    operation="create",
    assetPath="Assets/Scripts/SaveLoad/Checkpoint.cs",
    content="""using UnityEngine;
public class Checkpoint : MonoBehaviour {
    [SerializeField] private int checkpointId;
    private bool activated;
    void OnTriggerEnter(Collider other) {
        if (!activated && other.CompareTag(\"Player\")) {
            activated = true;
            SaveManager.Instance.Save(0);
        }
    }
}"""
)
```

### パターン 2: セーブデータのバージョン管理

```python
# DataMigrator スクリプト
unity_asset_crud(
    operation="create",
    assetPath="Assets/Scripts/SaveLoad/DataMigrator.cs",
    content="""using UnityEngine;
public static class DataMigrator {
    public const int CURRENT_VERSION = 2;
    public static SaveData Migrate(SaveData data) {
        if (data.version < 2) MigrateV1ToV2(data);
        return data;
    }
    private static void MigrateV1ToV2(SaveData data) {
        // v1 -> v2: 新フィールドのデフォルト値設定
        data.version = 2;
    }
}"""
)
```

### パターン 3: クラウドセーブ連携の準備

ローカルセーブとクラウドセーブを抽象化するインターフェース設計を最初から用意する。

```
ISaveBackend
  +-- LocalFileSaveBackend  (デフォルト)
  +-- CloudSaveBackend      (Steam、PlayFab 等への拡張)
```

---

## 注意点・落とし穴

1. **コード生成後のコンパイル待ち**
   `unity_asset_crud` でスクリプト作成後、`unity_gamekit_ui_list` 等のコード生成ツール
   実行後は、必ず `unity_compilation_await` を呼ぶこと。

2. **Application.persistentDataPath の使用**
   セーブファイルは `Application.persistentDataPath` に保存すること。
   `Application.dataPath` はビルド後に書き込み不可になる。

3. **非同期セーブによる競合**
   オートセーブと手動セーブが同時に走らないよう、セーブ中フラグで排他制御を実装する。

4. **ロード後の参照再構築**
   ScriptableObject への参照は ID 経由で再取得すること（直接の参照はシリアライズ不可）。

5. **データ破損への対策**
   セーブ前にバックアップファイル（.bak）を作成し、書き込み失敗時にリストアできるようにする。

6. **セーブデータの後方互換性**
   フィールド名の変更はデータ移行が必要。フィールドの追加はデフォルト値で対応可能。
   `version` フィールドを必ずセーブデータに含めること。

7. **Unity の JsonUtility の制限**
   `JsonUtility` はディクショナリ型・インターフェース型をシリアライズできない。
   `Newtonsoft.Json` または手動シリアライズを検討する。

---

## 関連ツール一覧

| ツール名 | 用途 |
|---|---|
| `unity_asset_crud` | セーブ管理の C# スクリプト作成 |
| `unity_scriptableObject_crud` | セーブ設定の ScriptableObject 作成 |
| `unity_gameobject_crud` | SaveManager・チェックポイントの GameObject 作成 |
| `unity_component_crud` | コンポーネントの追加・プロパティ設定 |
| `unity_gamekit_ui_list` | セーブスロット一覧 UI |
| `unity_gamekit_ui_selection` | セーブ/ロードタブの切り替え |
| `unity_gamekit_ui_command` | オートセーブ通知 UI |
| `unity_ui_foundation` | セーブ/ロード画面の UI 基盤 |
| `unity_ui_hierarchy` | 確認ダイアログの階層構造 |
| `unity_event_wiring` | ボタンとセーブ処理の接続 |
| `unity_input_profile` | クイックセーブ/ロードのキーバインド |
| `unity_compilation_await` | スクリプト生成後のコンパイル完了待ち |
| `unity_validate_integrity` | セーブ関連スクリプトの整合性チェック |
| `unity_scene_reference_graph` | 参照関係の確認 |
