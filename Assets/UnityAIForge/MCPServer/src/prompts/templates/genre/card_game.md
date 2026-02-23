# カードゲーム 設計ガイド - Unity-AI-Forge v{VERSION}

## 概要

デジタルトレーディングカードゲーム（TCG）・デッキ構築型ローグライク・
ソリティア系など多様なサブジャンルを含む。
核心は「ScriptableObject によるカードデータ管理」「手札/デッキ/墓地の UIList 表示」
「プレイエリアの UISlot」「ターンフロー管理」「マナ/リソース管理」である。
ゲームロジック（ターンフロー・カード効果解決・AI）はカスタムスクリプトで実装し、
UI が主役のジャンルであるため GameKit UI Pillar の全 5 ツールをフル活用する。

---

## シーン構成

```
Scenes/
  Boot.unity          # 初期化・マスターカードデータロード
  MainMenu.unity      # タイトル・デッキ選択
  DeckBuilder.unity   # デッキ構築画面
  Battle.unity        # バトル画面（メインゲーム）
  Result.unity        # 勝利/敗北・報酬
  Collection.unity    # カードコレクション管理
```

Battle.unity の GameObject 構成例:

```
[Battle Scene]
  - GameManager           # ターンフロー・勝敗判定（カスタムスクリプト）
  - CardManager           # デッキ・手札・墓地管理（カスタムスクリプト）
  - EffectResolver        # カード効果解決（カスタムスクリプト）
  - UI/
  |   - Canvas_Battle
  |       - HandArea       # ui_list（手札）
  |       - DeckDisplay    # ui_binding（残り枚数）
  |       - DiscardDisplay # ui_list（墓地表示）
  |       - PlayArea       # ui_slot（フィールド）
  |       - CommandPanel   # ui_command（ターン終了等）
  |       - ManaDisplay    # ui_binding（マナ）
  |       - HPDisplay      # ui_binding（HP）
  - FeedbackManager
  - Audio/
```

---

## 推奨フォルダ構造

```
Assets/
  Scripts/
    Game/
      GameManager.cs           # 手動作成: ターンフロー状態機械
      CardManager.cs           # 手動作成: デッキ・手札・墓地管理
      EffectResolver.cs        # 手動作成: カード効果解決
      ManaManager.cs           # 手動作成: マナ/リソース管理
    UI/
      HandList.cs              # 生成: unity_gamekit_ui_list
      DiscardList.cs           # 生成: unity_gamekit_ui_list
      PlayAreaSlot.cs          # 生成: unity_gamekit_ui_slot
      TargetSelection.cs       # 生成: unity_gamekit_ui_selection
      CommandPanel.cs          # 生成: unity_gamekit_ui_command
      ManaBinding.cs           # 生成: unity_gamekit_ui_binding
      HPBinding.cs             # 生成: unity_gamekit_ui_binding
      DeckCountBinding.cs      # 生成: unity_gamekit_ui_binding
    Presentation/
      CardPlayFeedback.cs      # 生成: unity_gamekit_feedback
      SpellVFX.cs              # 生成: unity_gamekit_vfx
      CardAudio.cs             # 生成: unity_gamekit_audio
  Data/
    Cards/
      Card_Fireball.asset      # ScriptableObject: カードデータ
      Card_Shield.asset
      Card_Poison.asset
    Decks/
      StarterDeck_Red.asset    # ScriptableObject: デッキデータ
    Effects/
      Effect_Burn.asset        # ScriptableObject: 効果データ
  Prefabs/
    Cards/
      CardPrefab.prefab        # 汎用カード Prefab
    UI/
  Sprites/
    Cards/, UI/
```

---

## 実装ワークフロー (MCPツール使用例付き)

### Step 1: カードデータ（ScriptableObject）

```python
# カードデータ ScriptableObject
unity_scriptableObject_crud(operation='create',
    typeName='CardData',
    assetPath='Assets/Data/Cards/Card_Fireball.asset',
    fields={
        'cardId':       'fireball',
        'cardName':     'ファイアボール',
        'cardType':     'Spell',
        'manaCost':     3,
        'attack':       6,
        'defense':      0,
        'rarity':       'Uncommon',
        'description':  '敵1体に6ダメージを与える。',
        'effectIds':    ['damage_target'],
        'tags':         ['fire', 'direct'],
    })

unity_scriptableObject_crud(operation='create',
    typeName='CardData',
    assetPath='Assets/Data/Cards/Card_Shield.asset',
    fields={
        'cardId':       'iron_shield',
        'cardName':     'アイアンシールド',
        'cardType':     'Skill',
        'manaCost':     2,
        'attack':       0,
        'defense':      5,
        'rarity':       'Common',
        'description':  'このターン、防御+5を得る。',
        'effectIds':    ['add_block'],
    })

unity_scriptableObject_crud(operation='create',
    typeName='CardData',
    assetPath='Assets/Data/Cards/Card_Poison.asset',
    fields={
        'cardId':       'poison_dart',
        'cardName':     'ポイズンダート',
        'cardType':     'Spell',
        'manaCost':     1,
        'attack':       2,
        'defense':      0,
        'rarity':       'Common',
        'description':  '敵1体に2ダメージ＋毒3スタック。',
        'effectIds':    ['damage_target', 'apply_poison'],
    })

# スターターデッキ定義
unity_scriptableObject_crud(operation='create',
    typeName='DeckData',
    assetPath='Assets/Data/Decks/StarterDeck_Red.asset',
    fields={
        'deckName': 'スターターデッキ（炎）',
        'cardEntries': [
            {'cardId': 'fireball',    'count': 2},
            {'cardId': 'iron_shield', 'count': 4},
            {'cardId': 'poison_dart', 'count': 4},
        ]
    })
```

### Step 2: バトルシーン・ゲームマネージャ

```python
# バトルシーン作成
unity_scene_crud(operation='create', sceneName='Battle',
    scenePath='Assets/Scenes/Battle.unity')
unity_scene_crud(operation='load', scenePath='Assets/Scenes/Battle.unity')

# GameManager（ターンフロー制御）
unity_gameobject_crud(operation='create', name='GameManager')
unity_asset_crud(operation='create', assetType='script',
    assetPath='Assets/Scripts/Game/GameManager.cs',
    templateType='MonoBehaviour')
unity_compilation_await(operation='await')

# CardManager（デッキ・手札・墓地管理）
unity_gameobject_crud(operation='create', name='CardManager')
unity_asset_crud(operation='create', assetType='script',
    assetPath='Assets/Scripts/Game/CardManager.cs',
    templateType='MonoBehaviour')
unity_compilation_await(operation='await')

# EffectResolver（カード効果解決）
unity_gameobject_crud(operation='create', name='EffectResolver')
unity_asset_crud(operation='create', assetType='script',
    assetPath='Assets/Scripts/Game/EffectResolver.cs',
    templateType='MonoBehaviour')
unity_compilation_await(operation='await')
```

### Step 3: バトル Canvas と基本 UI

```python
# バトル Canvas
unity_ui_foundation(operation='createCanvas', canvasName='Canvas_Battle',
    renderMode='ScreenSpaceOverlay')

# マナ表示
unity_ui_foundation(operation='createText', canvasPath='Canvas_Battle',
    textName='ManaDisplay', text='Mana: 3/3',
    position={'x': -350, 'y': -250})

unity_gamekit_ui_binding(operation='create',
    targetPath='Canvas_Battle/ManaDisplay',
    bindingId='mana_display', uiType='text', format='formatted')
unity_compilation_await(operation='await')

# HP 表示
unity_ui_foundation(operation='createText', canvasPath='Canvas_Battle',
    textName='HPDisplay', text='HP: 80/80',
    position={'x': -350, 'y': 250})

unity_gamekit_ui_binding(operation='create',
    targetPath='Canvas_Battle/HPDisplay',
    bindingId='hp_display', uiType='text', format='formatted')
unity_compilation_await(operation='await')

# デッキ残り枚数
unity_ui_foundation(operation='createText', canvasPath='Canvas_Battle',
    textName='DeckCount', text='Deck: 10',
    position={'x': 350, 'y': -250})

unity_gamekit_ui_binding(operation='create',
    targetPath='Canvas_Battle/DeckCount',
    bindingId='deck_count', uiType='text', format='formatted')
unity_compilation_await(operation='await')
```

### Step 4: 手札・墓地 UI（UIList）

```python
# 手札エリア（horizontal レイアウト、最大 10 枚）
unity_gamekit_ui_list(operation='create',
    targetPath='Canvas_Battle/HandArea',
    listId='hand_list',
    layout='horizontal',
    maxItems=10)
unity_compilation_await(operation='await')

# 墓地表示（直近 5 枚表示）
unity_gamekit_ui_list(operation='create',
    targetPath='Canvas_Battle/DiscardDisplay',
    listId='discard_list',
    layout='vertical',
    maxItems=5)
unity_compilation_await(operation='await')
```

### Step 5: プレイエリア（UISlot）

```python
# フィールドスロット（5 列、クリーチャー配置用）
unity_gamekit_ui_slot(operation='createSlotBar',
    barId='play_area',
    targetPath='Canvas_Battle/PlayArea',
    slotCount=5, slotType='storage')
unity_compilation_await(operation='await')

# 装備スロット（武器・防具）
unity_gamekit_ui_slot(operation='create',
    targetPath='Canvas_Battle/EquipArea/WeaponSlot',
    slotId='weapon_slot', slotType='equipment')
unity_compilation_await(operation='await')

unity_gamekit_ui_slot(operation='create',
    targetPath='Canvas_Battle/EquipArea/ArmorSlot',
    slotId='armor_slot', slotType='equipment')
unity_compilation_await(operation='await')
```

### Step 6: コマンドパネル・ターゲット選択

```python
# コマンドパネル（ターン終了・降参・履歴）
unity_gamekit_ui_command(operation='createCommandPanel',
    panelId='battle_cmd',
    canvasPath='Canvas_Battle',
    commands=[
        {'name': 'EndTurn',  'commandType': 'action', 'label': 'ターン終了'},
        {'name': 'Concede',  'commandType': 'action', 'label': '降参'},
        {'name': 'History',  'commandType': 'action', 'label': '履歴'},
    ])
unity_compilation_await(operation='await')

# ターゲット選択（敵の選択）
unity_gamekit_ui_selection(operation='create',
    targetPath='Canvas_Battle/TargetSelector',
    selectionId='target_sel',
    selectionMode='radio')
unity_compilation_await(operation='await')
```

### Step 7: デッキビルダー UI

```python
# デッキビルダーシーン: カードコレクションリスト
unity_gamekit_ui_list(operation='create',
    targetPath='Canvas_DeckBuilder/Collection',
    listId='collection_list',
    layout='grid', gridColumns=6)
unity_compilation_await(operation='await')

# フィルタタブ（属性別）
unity_gamekit_ui_selection(operation='create',
    targetPath='Canvas_DeckBuilder/FilterTabs',
    selectionId='element_filter',
    selectionMode='radio')
unity_compilation_await(operation='await')

unity_gamekit_ui_selection(operation='setItems',
    selectionId='element_filter',
    items=[
        {'id': 'all',  'label': 'すべて'},
        {'id': 'fire', 'label': '炎'},
        {'id': 'water','label': '水'},
        {'id': 'wind', 'label': '風'},
    ])

# デッキリスト（現在のデッキ内カード一覧）
unity_gamekit_ui_list(operation='create',
    targetPath='Canvas_DeckBuilder/DeckList',
    listId='deck_list',
    layout='vertical', maxItems=40)
unity_compilation_await(operation='await')
```

### Step 8: イベント接続

```python
# ターン終了ボタンのイベント接続
unity_event_wiring(operation='wire',
    sourcePath='Canvas_Battle/CommandPanel/EndTurn',
    eventName='onClick',
    targetPath='GameManager',
    methodName='OnEndTurnClicked')

# 降参ボタンのイベント接続
unity_event_wiring(operation='wire',
    sourcePath='Canvas_Battle/CommandPanel/Concede',
    eventName='onClick',
    targetPath='GameManager',
    methodName='OnConcedeClicked')
```

### Step 9: バトル演出

```python
unity_gameobject_crud(operation='create', name='FeedbackManager')
unity_gameobject_crud(operation='create', name='FXManager')

# カード使用時の VFX
unity_gamekit_vfx(operation='create', targetPath='FXManager',
    vfxId='spell_cast_vfx')
unity_compilation_await(operation='await')

# ダメージ時フィードバック
unity_gamekit_feedback(operation='create', targetPath='FeedbackManager',
    feedbackId='damage_taken',
    components=[
        {'type': 'screenShake', 'intensity': 0.2, 'duration': 0.15},
        {'type': 'flash', 'color': {'r':1,'g':0,'b':0,'a':0.4}, 'duration': 0.1},
    ])
unity_compilation_await(operation='await')

# BGM
unity_gameobject_crud(operation='create', name='Audio')
unity_gamekit_audio(operation='create', targetPath='Audio',
    audioId='battle_bgm', audioClipPath='Assets/Audio/BGM/Battle.mp3',
    loop=True)
unity_compilation_await(operation='await')

# カード操作 SE
unity_gamekit_audio(operation='create', targetPath='Audio',
    audioId='sfx_card_play', audioClipPath='Assets/Audio/SFX/CardPlay.wav')
unity_compilation_await(operation='await')

unity_gamekit_audio(operation='create', targetPath='Audio',
    audioId='sfx_card_draw', audioClipPath='Assets/Audio/SFX/CardDraw.wav')
unity_compilation_await(operation='await')
```

### Step 10: 検証

```python
# 整合性検証
unity_validate_integrity(operation='all')

# シーン遷移確認
unity_scene_relationship_graph(operation='analyzeAll')
```

---

## よくあるパターン

### デッキシャッフルと山札補充

デッキ枚数が 0 になったとき、墓地の `discard_list` を全取得し、
シャッフルして山札に戻すロジックを `CardManager` カスタムスクリプトに集約する。
`unity_gamekit_ui_binding` で `deck_count` を UI に連動させる。

### カードドラッグ&ドロップ

手札 `ui_list` からフィールド `ui_slot` へのドラッグは
Unity の `IBeginDragHandler / IDragHandler / IDropHandler` インターフェースで実装する。
カスタムスクリプトで `ui_slot` のスロット ID を参照し、配置可否を制御する。

### ランダムカードドロー

`CardManager` でデッキをシャッフルし先頭から N 枚を手札に追加する処理を
`unity_gamekit_ui_list(operation='addItem')` で順次呼び出す。
アニメーション演出が必要な場合は `unity_gamekit_feedback` でドロー演出を付加する。

---

## 注意点・落とし穴

- **UIList の itemTemplate**: カード Prefab には必ずデータ参照フィールドと
  表示スクリプトを持たせる。`addItem` 時の `itemData` でデータバインドする設計にすること。
- **マナ検証**: カード使用前に `mana >= manaCost` を必ず確認する。
  カスタムスクリプトの `CardManager` でガードする。
- **ui_list の maxItems**: 手札上限を超えてカードをドローしようとすると
  `addItem` が失敗する。`CardManager` 側で手札枚数を事前チェックする。
- **デッキビルダーの保存**: デッキ変更は即時保存せず、
  「確定」ボタン押下時にカスタムスクリプトでセーブする設計にすること。
  誤操作によるデッキ破壊を防ぐ。
- **GameKit 生成スクリプト** は create 操作後に `unity_compilation_await` を呼ぶこと。
  コンパイル完了前に次の GameKit 操作を行うと失敗する。

---

## 関連ツール一覧

| カテゴリ | ツール名 | 用途 |
|---------|---------|-----|
| データ | `unity_scriptableObject_crud` | カード・デッキ・効果データ |
| シーン | `unity_scene_crud` | バトル・デッキビルダーシーン |
| オブジェクト | `unity_gameobject_crud` | マネージャ・UI 親オブジェクト作成 |
| アセット | `unity_asset_crud` | ゲームロジックスクリプト作成 |
| プレハブ | `unity_prefab_crud` | カード Prefab 管理 |
| UI | `unity_gamekit_ui_list` | 手札・墓地・コレクション |
| UI | `unity_gamekit_ui_slot` | フィールド・装備スロット |
| UI | `unity_gamekit_ui_selection` | ターゲット選択・フィルタタブ |
| UI | `unity_gamekit_ui_command` | ターン終了・降参 |
| UI | `unity_gamekit_ui_binding` | マナ・HP・デッキ枚数表示 |
| UI基盤 | `unity_ui_foundation` | Canvas・Text・Button 作成 |
| 演出 | `unity_gamekit_vfx` | スペルカスト VFX |
| 演出 | `unity_gamekit_feedback` | ダメージフラッシュ |
| 演出 | `unity_gamekit_audio` | BGM・カード操作 SE |
| イベント | `unity_event_wiring` | ボタン→スクリプト接続 |
| 設定 | `unity_projectSettings_crud` | ビルドシーン管理 |
| 検証 | `unity_validate_integrity` | 参照切れ・Missing Script |
| 検証 | `unity_scene_relationship_graph` | シーン遷移確認 |
