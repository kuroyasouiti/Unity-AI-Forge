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
      BattleCommandPanel.cs    # 生成: unity_gamekit_ui_command
      BattleLogList.cs         # 生成: unity_gamekit_ui_list
      TargetSelection.cs       # 生成: unity_gamekit_ui_selection
    Character/
      CharacterController.cs   # 手動作成: キャラクター制御
      CharacterAnimSync.cs     # 生成: unity_gamekit_animation_sync
    Inventory/
      InventorySlot.cs         # 生成: unity_gamekit_ui_slot
    UI/
      HPBinding.cs             # 生成: unity_gamekit_ui_binding
      MPBinding.cs             # 生成: unity_gamekit_ui_binding
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
    fields={
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
    fields={
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
    fields={
        'itemName': 'ポーション',
        'itemType': 'Consumable',
        'healAmount': 50,
        'description': 'HPを50回復する',
    })
```

### Step 2: バトルシーン構築

```python
# バトルシーン作成・ロード
unity_scene_crud(operation='create', sceneName='Battle',
    scenePath='Assets/Scenes/Battle.unity')
unity_scene_crud(operation='load', scenePath='Assets/Scenes/Battle.unity')

# BattleManager（ゲームフロー制御）
unity_gameobject_crud(operation='create', name='BattleManager')

# バトルロジック用スクリプトを作成
unity_asset_crud(operation='create', assetType='script',
    assetPath='Assets/Scripts/Battle/BattleManager.cs',
    templateType='MonoBehaviour')
unity_compilation_await(operation='await')

# パーティ・敵グループ配置
unity_gameobject_crud(operation='create', name='PartyGroup',
    position={'x': -4, 'y': 0, 'z': 0})
unity_gameobject_crud(operation='create', name='EnemyGroup',
    position={'x': 4, 'y': 0, 'z': 0})
```

### Step 3: キャラクターオブジェクト配置

```python
# 主人公
unity_gameobject_crud(operation='create', name='Hero',
    parentPath='PartyGroup', position={'x': 0, 'y': 0, 'z': 0})

# SpriteRenderer 追加
unity_component_crud(operation='add', gameObjectPath='PartyGroup/Hero',
    componentType='SpriteRenderer')

# アニメーション設定
unity_animation2d_bundle(operation='setupAnimator',
    gameObjectPath='PartyGroup/Hero',
    controllerPath='Assets/Animations/Characters/HeroAnimator.controller')

# 敵
unity_gameobject_crud(operation='create', name='Slime_01',
    parentPath='EnemyGroup', position={'x': 0, 'y': 0, 'z': 0})

unity_component_crud(operation='add', gameObjectPath='EnemyGroup/Slime_01',
    componentType='SpriteRenderer')
```

### Step 4: バトルコマンド UI

```python
# バトル Canvas
unity_ui_foundation(operation='createCanvas', canvasName='Canvas_Battle',
    renderMode='ScreenSpaceOverlay')

# コマンドパネル（たたかう / まほう / アイテム / にげる）
unity_gamekit_ui_command(operation='createCommandPanel',
    panelId='battle_cmd',
    canvasPath='Canvas_Battle',
    commands=[
        {'name': 'Attack',  'commandType': 'action',  'label': 'たたかう'},
        {'name': 'Skill',   'commandType': 'submenu', 'label': 'まほう'},
        {'name': 'Item',    'commandType': 'submenu', 'label': 'アイテム'},
        {'name': 'Escape',  'commandType': 'action',  'label': 'にげる'},
    ])
unity_compilation_await(operation='await')

# ターゲット選択（敵選択: radio モード）
unity_gamekit_ui_selection(operation='create',
    targetPath='Canvas_Battle/TargetSelector',
    selectionId='target_sel',
    selectionMode='radio')
unity_compilation_await(operation='await')

# バトルログ（vertical スクロールリスト）
unity_gamekit_ui_list(operation='create',
    targetPath='Canvas_Battle/BattleLog',
    listId='battle_log',
    layout='vertical',
    maxItems=10)
unity_compilation_await(operation='await')
```

### Step 5: HP/MP 表示バインディング

```python
# HP バー
unity_ui_foundation(operation='createText', canvasPath='Canvas_Battle',
    textName='HeroHP', text='HP: 100/100')

unity_gamekit_ui_binding(operation='create',
    targetPath='Canvas_Battle/HeroHP',
    bindingId='hero_hp', uiType='text', format='formatted')
unity_compilation_await(operation='await')

# MP バー
unity_ui_foundation(operation='createText', canvasPath='Canvas_Battle',
    textName='HeroMP', text='MP: 50/50')

unity_gamekit_ui_binding(operation='create',
    targetPath='Canvas_Battle/HeroMP',
    bindingId='hero_mp', uiType='text', format='formatted')
unity_compilation_await(operation='await')
```

### Step 6: インベントリ UI（UISlot + UIList）

```python
# アイテムリスト表示（グリッド）
unity_gamekit_ui_list(operation='create',
    targetPath='Canvas_Battle/ItemList',
    listId='item_list',
    layout='grid', gridColumns=4)
unity_compilation_await(operation='await')

# アイテムクイックスロット
unity_gamekit_ui_slot(operation='createSlotBar',
    barId='item_quickslot',
    targetPath='Canvas_Battle/QuickItems',
    slotCount=4, slotType='quickslot')
unity_compilation_await(operation='await')
```

### Step 7: バトル演出

```python
# ダメージフィードバック
unity_gameobject_crud(operation='create', name='FeedbackManager')
unity_gamekit_feedback(operation='create', targetPath='FeedbackManager',
    feedbackId='damage_taken',
    components=[
        {'type': 'screenShake', 'intensity': 0.2, 'duration': 0.15},
        {'type': 'flash', 'color': {'r':1,'g':0,'b':0,'a':0.4}, 'duration': 0.1},
    ])
unity_compilation_await(operation='await')

# スキル VFX
unity_gamekit_vfx(operation='create', targetPath='FX/SkillEffect',
    vfxId='skill_fire_vfx')
unity_compilation_await(operation='await')

# BGM
unity_gamekit_audio(operation='create', targetPath='Audio/BGM',
    audioId='battle_bgm', audioClipPath='Assets/Audio/BGM/Battle.mp3',
    loop=True)
unity_compilation_await(operation='await')
```

### Step 8: シーン遷移・ビルド設定

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

`ui_command` の `commandType='submenu'` で「まほう」を選ぶと
スキルリストに遷移するネスト UI を構築できる。
スキルリストは `ui_list` + `ui_selection` の組み合わせで実装する。

### データ駆動バランス調整

全キャラ・スキル・アイテムデータを ScriptableObject に持たせ、
Playmode に入らずとも `unity_scriptableObject_crud(operation='update')` で
数値調整ができる。デバッグサイクルが大幅に短縮される。

---

## 注意点・落とし穴

- **Battle シーンの Additive ロード** を使う場合は
  Field シーンのオブジェクトが DontDestroyOnLoad 対象の場合の重複ロードに注意。
- **ScriptableObject の参照**: Inspector からの参照は Prefab に焼き付ける。
  `unity_event_wiring` でスクリプト間イベントを接続する際、SO の参照切れに注意。
- **GameKit 生成後は必ず** `unity_compilation_await` でコンパイルを待つ。
  コンパイル前に次のアタッチ操作を行うと失敗する。
- **カスタムスクリプトの作成**: ターンフロー・ダメージ計算・AI など複雑なロジックは
  `unity_asset_crud` でスクリプトファイルを作成し、手動で実装する。
  GameKit は UI・演出・アニメーション同期に特化しており、ゲームロジックは含まない。

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
| アニメーション | `unity_animation2d_bundle` | Animator・クリップ管理 |
| アニメーション | `unity_gamekit_animation_sync` | バトルアニメ連動 |
| UI | `unity_gamekit_ui_command` | バトルコマンドパネル |
| UI | `unity_gamekit_ui_binding` | HP・MP バー表示 |
| UI | `unity_gamekit_ui_list` | バトルログ・スキルリスト |
| UI | `unity_gamekit_ui_slot` | アイテムスロット |
| UI | `unity_gamekit_ui_selection` | ターゲット選択・タブ |
| UI基盤 | `unity_ui_foundation` | Canvas・Text・Button 作成 |
| 演出 | `unity_gamekit_feedback` | ダメージフラッシュ |
| 演出 | `unity_gamekit_vfx` | スキルエフェクト |
| 演出 | `unity_gamekit_audio` | BGM・SE 管理 |
| イベント | `unity_event_wiring` | UnityEvent 接続 |
| 設定 | `unity_projectSettings_crud` | ビルドシーン管理 |
| 検証 | `unity_validate_integrity` | 参照切れ検出 |
| 検証 | `unity_scene_relationship_graph` | シーン遷移確認 |
