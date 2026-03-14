# ターン制RPG 設計ガイド - Unity-AI-Forge v{VERSION}

## 概要

フィールド探索、エンカウント、ターン制バトル、メニュー・インベントリ・セーブが核となるジャンル。
シーンを「フィールド」「バトル」「メニュー」に明確に分割し、
ScriptableObject によるデータ駆動設計で、キャラクターやスキルのパラメータを柔軟に管理する。
ゲームロジック（ターンフロー・コンバット計算・AI）はカスタムスクリプトとして `unity_asset_crud` で作成し、
GameKit の UI Pillar（ui_command, ui_binding, ui_list, ui_slot, ui_selection）を
バトルコマンドおよびインベントリ UI のバックボーンとして活用する。

---

## シーン構成

```
Scenes/
  Boot.unity          # 初期化・マスターデータロード
  MainMenu.unity      # タイトル・コンティニュー
  Field.unity         # フィールド探索（複数可）
  Battle.unity        # バトルシーン（共通・動的ロード）
  Menu.unity          # メニューシーン（Additive ロード）
  GameOver.unity
  Ending.unity
```

バトルシーンの GameObject 構成例:

```
[Battle Scene]
  - BattleManager       # ターンフロー制御（カスタムスクリプト）
  - PartyGroup/
  |   - Hero
  |   - Mage
  |   - Healer
  - EnemyGroup/
  |   - Slime_01
  |   - Slime_02
  - UI/
  |   - Canvas_Battle
  |       - CommandPanel   # ui_command
  |       - BattleLog      # ui_list（ログ表示）
  |       - PartyStatus    # ui_binding（HP/MP）
  |       - TargetSelector # ui_selection
  - Audio/
  - BattleBG
```

---

## 推奨フォルダ構造

```
Assets/
  Scripts/
    Battle/
      BattleManager.cs         # 手動作成: ターンフロー状態機械
      CombatCalculator.cs      # 手動作成: ダメージ計算ロジック
      BattleCommandPanel.cs    # 生成: unity_gamekit_ui (widgetType=command)
      BattleLogList.cs         # 生成: unity_gamekit_ui (widgetType=list)
      TargetSelection.cs       # 生成: unity_gamekit_ui (widgetType=selection)
    Character/
      CharacterController.cs   # 手動作成: キャラクター制御
    Inventory/
      InventorySlot.cs         # 生成: unity_gamekit_ui (widgetType=slot)
    UI/
      HPBinding.cs             # 生成: unity_gamekit_ui (widgetType=binding)
      MPBinding.cs             # 生成: unity_gamekit_ui (widgetType=binding)
  Data/
    Characters/
      Hero_Data.asset          # ScriptableObject: キャラクターデータ
      Enemy_Slime.asset
    Skills/
      Skill_Fire.asset         # ScriptableObject: スキルデータ
    Items/
      Item_Potion.asset        # ScriptableObject: アイテムデータ
  Prefabs/
    Characters/, Enemies/, FX/
```

---

## 実装ワークフロー (MCPツール使用例付き)

### Step 1: データ駆動設計（ScriptableObject）

```python
# キャラクターデータ ScriptableObject
unity_scriptableObject_crud(operation='create',
    typeName='CharacterData',
    assetPath='Assets/Data/Characters/Hero_Data.asset',
    properties={
        'characterName': 'Hero',
        'maxHP': 100,
        'maxMP': 50,
        'attack': 15,
        'defense': 10,
        'speed': 12,
    })

# スキルデータ
unity_scriptableObject_crud(operation='create',
    typeName='SkillData',
    assetPath='Assets/Data/Skills/Skill_Fire.asset',
    properties={
        'skillName': 'ファイア',
        'mpCost': 10,
        'power': 25,
        'targetType': 'SingleEnemy',
        'element': 'Fire',
    })

# アイテムデータ
unity_scriptableObject_crud(operation='create',
    typeName='ItemData',
    assetPath='Assets/Data/Items/Item_Potion.asset',
    properties={
        'itemName': 'ポーション',
        'itemType': 'Consumable',
        'healAmount': 50,
        'description': 'HPを50回復する',
    })
```

### Step 2: バトルシーン構築

```python
# バトルシーン作成・ロード
unity_scene_crud(operation='create',
    scenePath='Assets/Scenes/Battle.unity')
unity_scene_crud(operation='load', scenePath='Assets/Scenes/Battle.unity')

# BattleManager（ゲームフロー制御）
unity_gameobject_crud(operation='create', name='BattleManager')

# バトルロジック用スクリプトを作成
unity_asset_crud(operation='create',
    assetPath='Assets/Scripts/Battle/BattleManager.cs')
unity_compilation_await(operation='await')

# パーティ・敵グループ配置
unity_gameobject_crud(operation='create', name='PartyGroup')
unity_gameobject_crud(operation='create', name='EnemyGroup')
```

### Step 3: キャラクターオブジェクト配置

```python
# 主人公
unity_gameobject_crud(operation='create', name='Hero',
    parentPath='PartyGroup')

# SpriteRenderer 追加
unity_component_crud(operation='add', gameObjectPath='PartyGroup/Hero',
    componentType='SpriteRenderer')

# アニメーション設定
unity_animation_bundle(operation='setupAnimator',
    gameObjectPath='PartyGroup/Hero',
    controllerPath='Assets/Animations/Characters/HeroAnimator.controller')

# 敵
unity_gameobject_crud(operation='create', name='Slime_01',
    parentPath='EnemyGroup')

unity_component_crud(operation='add', gameObjectPath='EnemyGroup/Slime_01',
    componentType='SpriteRenderer')
```

### Step 4: バトルコマンド UI

```python
# バトル Canvas
unity_ui_foundation(operation='createCanvas', name='Canvas_Battle',
    renderMode='screenSpaceOverlay')

# コマンドパネル（たたかう / まほう / アイテム / にげる）
unity_gamekit_ui(widgetType='command',operation='createCommandPanel',
    panelId='battle_cmd',
    parentPath='Canvas_Battle',
    commands=[
        {'name': 'Attack',  'commandType': 'action',  'label': 'たたかう'},
        {'name': 'Skill',   'commandType': 'submenu', 'label': 'まほう'},
        {'name': 'Item',    'commandType': 'submenu', 'label': 'アイテム'},
        {'name': 'Escape',  'commandType': 'action',  'label': 'にげる'},
    ])
unity_compilation_await(operation='await')

# ターゲット選択（敵選択: radio モード）
unity_gamekit_ui(widgetType='selection',operation='create',
    targetPath='Canvas_Battle/TargetSelector',
    selectionId='target_sel',
    selectionType='radio')
unity_compilation_await(operation='await')

# バトルログ（vertical スクロールリスト）
unity_gamekit_ui(widgetType='list',operation='create',
    targetPath='Canvas_Battle/BattleLog',
    listId='battle_log',
    layout='vertical')
unity_compilation_await(operation='await')
```

### Step 5: HP/MP 表示バインディング

```python
# HP バー
unity_ui_foundation(operation='createText', parentPath='Canvas_Battle',
    name='HeroHP', text='HP: 100/100')

unity_gamekit_ui(widgetType='binding',operation='create',
    targetPath='Canvas_Battle/HeroHP',
    bindingId='hero_hp', format='formatted')
unity_compilation_await(operation='await')

# MP バー
unity_ui_foundation(operation='createText', parentPath='Canvas_Battle',
    name='HeroMP', text='MP: 50/50')

unity_gamekit_ui(widgetType='binding',operation='create',
    targetPath='Canvas_Battle/HeroMP',
    bindingId='hero_mp', format='formatted')
unity_compilation_await(operation='await')
```

### Step 6: インベントリ UI（UISlot + UIList）

```python
# アイテムリスト表示（グリッド）
unity_gamekit_ui(widgetType='list',operation='create',
    targetPath='Canvas_Battle/ItemList',
    listId='item_list',
    layout='grid', columns=4)
unity_compilation_await(operation='await')

# アイテムクイックスロット
unity_gamekit_ui(widgetType='slot',operation='createSlotBar',
    barId='item_quickslot',
    targetPath='Canvas_Battle/QuickItems',
    slotCount=4, slotType='quickslot')
unity_compilation_await(operation='await')
```

### Step 7: シーン遷移・ビルド設定

```python
# シーンをビルドに登録
unity_projectSettings_crud(operation='addSceneToBuild',
    scenePath='Assets/Scenes/Boot.unity')
unity_projectSettings_crud(operation='addSceneToBuild',
    scenePath='Assets/Scenes/Field.unity')
unity_projectSettings_crud(operation='addSceneToBuild',
    scenePath='Assets/Scenes/Battle.unity')

# シーン遷移の整合性を確認
unity_scene_relationship_graph(operation='analyzeAll')

# 参照切れチェック
unity_validate_integrity(operation='all')
```

---

## よくあるパターン

### ターンオーダー管理

BattleManager のカスタムスクリプトで状態機械（PlayerTurn → EnemyTurn → PlayerTurn...）を
実装する。速度パラメータを ScriptableObject に持たせ、行動順をソートで決定する。
ATB の場合はキャラクターごとにゲージを float で管理し、Update で加算する。

### スキルサブメニュー

`ui_command` の `commandType='custom'` で「まほう」を選ぶと
スキルリストに遷移するネスト UI を構築できる。
スキルリストは `ui_list` + `ui_selection` の組み合わせで実装する。

### データ駆動バランス調整

全キャラ・スキル・アイテムデータを ScriptableObject に持たせ、
Playmode に入らずとも `unity_scriptableObject_crud(operation='update')` で
数値調整ができる。デバッグサイクルが大幅に短縮される。

### バトルフェーズ ↔ UI 連動

バトルフェーズの切替時に UI パネルの表示を CanvasGroup で制御する。
`SetActive` ではなく CanvasGroup の alpha/interactable/blocksRaycasts を操作する。

```python
# バトル UI パネルの初期設定（CanvasGroup で非表示）
unity_ui_foundation(operation='hide', targets=[
    'Canvas_Battle/CommandPanel', 'Canvas_Battle/StatusPanel',
    'Canvas_Battle/DamagePredict', 'Canvas_Battle/ResultPanel'])

# フェーズごとの UI 状態を定義
unity_ui_state(operation='defineState', rootPath='Canvas_Battle', stateName='player_turn', elements=[
    {'path': 'CommandPanel', 'visible': True, 'interactable': True},
    {'path': 'StatusPanel', 'visible': True},
    {'path': 'ResultPanel', 'visible': False}
])
unity_ui_state(operation='defineState', rootPath='Canvas_Battle', stateName='enemy_turn', elements=[
    {'path': 'CommandPanel', 'visible': False},
    {'path': 'StatusPanel', 'visible': True, 'interactable': False}
])
```

### シーン間データ受け渡し

ステージ選択やパーティ編成のデータを次のシーンに渡す場合、
static クラスではなく DataContainer (ScriptableObject) を使う。

```python
# DataContainer でシーン間データを定義
unity_gamekit_data(dataType='dataContainer', operation='create', dataId='BattleSetup', fields=[
    {'name': 'encounterDataPath', 'fieldType': 'string', 'defaultValue': ''},
    {'name': 'partySize', 'fieldType': 'int', 'defaultValue': 3}
], resetOnPlay=False, assetPath='Assets/Data/BattleSetup.asset')
```

---

## 注意点・落とし穴

- **Battle シーンの Additive ロード** を使う場合は
  Field シーンのオブジェクトが DontDestroyOnLoad 対象の場合の重複ロードに注意。
- **ScriptableObject の参照**: Inspector からの参照は Prefab に焼き付ける。
  `unity_event_wiring` でスクリプト間イベントを接続する際、SO の参照切れに注意。
  `target.component` でメソッドの所属コンポーネントを明示すること。
- **GameKit 生成後は必ず** `unity_compilation_await` でコンパイルを待つ。
  コンパイル前に次のアタッチ操作を行うと失敗する。
- **カスタムスクリプトの作成**: ターンフロー・ダメージ計算・AI など複雑なロジックは
  `unity_asset_crud` でスクリプトファイルを作成し、手動で実装する。
  GameKit は UI に特化しており、ゲームロジックは含まない。
- **UI パネルは CanvasGroup で表示制御**: `SetActive` は使わない。
  CanvasGroup の alpha/interactable/blocksRaycasts で制御する。
- **複数 UI 要素は LayoutGroup 必須**: 同一親にボタン等を並べる場合、
  LayoutGroup なしでは同じ位置に重なる。

---

## 関連ツール一覧

| カテゴリ | ツール名 | 用途 |
|---------|---------|-----|
| データ | `unity_scriptableObject_crud` | キャラ・スキル・アイテムデータ |
| シーン | `unity_scene_crud` | フィールド・バトル・メニュー管理 |
| オブジェクト | `unity_gameobject_crud` | キャラクター・マネージャ配置 |
| コンポーネント | `unity_component_crud` | SpriteRenderer・コライダー追加 |
| アセット | `unity_asset_crud` | カスタムスクリプト作成 |
| プレハブ | `unity_prefab_crud` | キャラクター・敵プレハブ化 |
| アニメーション | `unity_animation_bundle` | Animator・クリップ管理 |
| UI | `unity_gamekit_ui(widgetType='command')` | バトルコマンドパネル |
| UI | `unity_gamekit_ui(widgetType='binding')` | HP・MP バー表示 |
| UI | `unity_gamekit_ui(widgetType='list')` | バトルログ・スキルリスト |
| UI | `unity_gamekit_ui(widgetType='slot')` | アイテムスロット |
| UI | `unity_gamekit_ui(widgetType='selection')` | ターゲット選択・タブ |
| UI基盤 | `unity_ui_foundation` | Canvas・Text・Button 作成 |
| イベント | `unity_event_wiring` | UnityEvent 接続 |
| 設定 | `unity_projectSettings_crud` | ビルドシーン管理 |
| 検証 | `unity_validate_integrity` | 参照切れ検出 |
| 検証 | `unity_scene_relationship_graph` | シーン遷移確認 |
