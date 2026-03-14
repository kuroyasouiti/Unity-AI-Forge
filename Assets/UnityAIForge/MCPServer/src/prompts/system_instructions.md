# Unity-AI-Forge MCP Server v{VERSION} - Quick Reference

AI駆動型Unity開発ツールキット。40ツール、3層構造（Low/Mid/High-Level）。

## 🔴 Critical Rules

1. **.metaファイルは絶対に編集しない**（Unity自動管理）
2. **全Unity操作にMCPツール（unity_*）を使用**（特に .cs ファイルの新規作成・編集は必ず `asset_crud` を使用。外部ファイルツールでの直接書き込みは Unity AssetDatabase に登録されずコンパイルもトリガーされない。`asset_crud(forceReimport)` で復旧可能）
3. **変更前にinspect操作で対象を確認**
4. **ツール優先順位: High-Level → Mid-Level → Low-Level**
5. **UI優先設計**: UIから実装し、ロジックは後
6. **PDCA遵守**: Plan(inspect/graph) → Do(実行) → Check(validate_integrity/console_log) → Act(修正)
7. **コンパイル待ち必須**: コード生成ツール(GameKit create操作, asset_crud create *.cs)使用後は必ず `compilation_await(await)` を実行してから次の操作。**最適化**: `createMultiple` で同種スクリプトを一括生成すれば1回の待ちで済む。`batch_sequential` は自動でコンパイル待ちを注入する
8. **物理設定のベストプラクティス**: Layer Collision Matrixで不要な衝突を除外、高速オブジェクトのCollision DetectionはContinuousに設定
9. **ゲーム操作/メニュー切替には `ui_state` を使用**: gameplay ↔ pause、gameplay ↔ inventory 等の画面モード切替は `unity_ui_state` の `defineState` + `applyState` で管理。`createStateGroup` で排他制御
10. **UI表示制御はCanvasGroupのみ**: `SetActive` は使わず `CanvasGroup`（alpha/interactable/blocksRaycasts）で制御。GameObjectは常にアクティブ状態を維持
11. **シーン間データはDataContainer(SO)で渡す**: staticクラスではなく `gamekit_data(dataType='dataContainer')` で ScriptableObject を作成し、書き込み側・読み取り側の両方から参照する
12. **複数兄弟UI要素にはLayoutGroup必須**: 同一親に複数のUI要素を配置する場合、LayoutGroupなしでは重なる
13. **スクリプト生成順序**: interface/enum → ScriptableObject(データ定義) → 被参照MonoBehaviour → 参照元MonoBehaviour。同一レイヤーは `asset_crud(createMultiple)` で一括生成し `compilation_await` 1回で済ませる
14. **GameKit統合必須**: GameKit生成(EventChannel/Pool/RuntimeSet/DataContainer)後、手書きスクリプトに `[SerializeField]` 追加 + 呼び出しコード挿入 + `component_crud` でInspector参照設定。**生成だけでは動かない — 統合コードの挿入が必須**
15. **プリセット後のtag/layer再確認**: `physics_bundle` / `camera_bundle` の preset 適用後は tag/layer が上書きされる可能性あり。適用後に `gameobject_crud(update)` で再設定すること
16. **Prefab作成はコンパイル後**: カスタムMonoBehaviourを含むPrefabは、`compilation_await` でコンパイル完了を確認してから作成。未コンパイル状態ではコンポーネント付与不可
17. **Input System整合性チェック必須**: PlayerInputを使うスクリプト生成・変更の**前に**必ず以下を確認:
    - `.inputactions` ファイルを読み、各Actionの `expectedControlType` を確認（Axis/Vector2/Button）
    - `PlayerInput` コンポーネントの `notificationBehavior` を確認（SendMessages=0 / InvokeCSharpEvents=3）
    - 下表に従いメソッドシグネチャを決定:

    | notificationBehavior | メソッド形式 | 例 |
    |---------------------|-------------|-----|
    | SendMessages (0) | `void OnXxx(InputValue value)` | `value.Get<float>()`, `value.isPressed` |
    | InvokeCSharpEvents (3) | `void OnXxx(InputAction.CallbackContext ctx)` | `ctx.ReadValue<float>()`, `ctx.performed` |

    | expectedControlType | C# 型 |
    |--------------------|-------|
    | Axis | `float` (`Get<float>()` / `ReadValue<float>()`) |
    | Vector2 | `Vector2` (`Get<Vector2>()` / `ReadValue<Vector2>()`) |
    | Button | `float` / `isPressed` (SendMessages) or `performed`/`canceled` (CSharp) |

    - 変更後は `validate_integrity(inputSystemAudit)` で整合性を検証
18. **車両・キャラ配置前のコース方向確認**: レーシング等でコース上にオブジェクトを配置する場合、チェックポイント/ウェイポイントの最初の2点をinspectし、CP_0→CP_1の方向に車両の初期回転を合わせる
19. **onClick は Void メソッドを推奨**: `Button.onClick` に String/Int 引数付きメソッドを接続すると引数が保存されない場合がある。代わりに `LaunchPatrol()` のような引数不要のラッパーメソッドを作成し、内部で `LaunchMission("Mission_Patrol")` を呼ぶパターンを使う。`event_wiring(clearEvent)` で古いリスナーを一括削除してから再配線すること

---

## 🔰 プロジェクト状態チェック（初回タスク開始前に必ず実施）

新しいプロジェクトまたは初回タスク開始時に、以下のチェックを実行してプロジェクトの技術スタックを把握する。
**コード生成・カメラ設定・UI作成の前に必ず確認すること。**

### チェック手順

```python
# 1. パッケージ確認（Packages/manifest.json を読む）
#    確認項目:
#    - com.unity.inputsystem → 新Input System使用（Input.GetMouseButtonDown NG → Mouse.current を使う）
#    - com.unity.render-pipelines.universal → URP使用（Standard shader NG → URP/Lit を使う）
#    - com.unity.render-pipelines.high-definition → HDRP使用
#    - com.unity.textmeshpro → TMP使用（Text NG → TextMeshProUGUI を使う）
#    - com.unity.ai.navigation → NavMesh使用可能

# 2. PlayerSettings確認
unity_projectSettings_crud(operation='read', category='player')
#    確認項目:
#    - activeInputHandler: InputSystem / InputManager / Both
#    - scriptingBackend: Mono / IL2CPP

# 3. シーン階層確認（既存オブジェクトの把握）
unity_scene_crud(operation='inspect', includeHierarchy=True)
#    確認項目:
#    - Main Camera が存在するか → あれば camera_bundle(applyPreset) で再利用、なければ create
#    - EventSystem が存在するか → あれば作成不要
#    - Canvas が存在するか → あれば再利用

# 4. 既存コード規約の把握（スクリプトがある場合）
unity_class_catalog(operation='listTypes', typeKind='MonoBehaviour', searchPath='Assets/Scripts')
#    確認項目:
#    - namespace の命名規則
#    - 既存クラスとの整合性
```

### 技術スタック判定表

| パッケージ | 判定 | コード生成への影響 |
|-----------|------|------------------|
| `com.unity.inputsystem` あり | 新Input System | `Mouse.current`, `Keyboard.current`, `Gamepad.current` を使用 |
| `com.unity.inputsystem` なし | 旧Input Manager | `Input.GetAxis()`, `Input.GetMouseButtonDown()` を使用 |
| `com.unity.render-pipelines.universal` あり | URP | `Universal Render Pipeline/Lit` シェーダー使用 |
| URP/HDRP なし | Built-in RP | `Standard` シェーダー使用 |
| `com.unity.textmeshpro` あり | TMP | `TMPro.TextMeshProUGUI`, `TMPro.TMP_InputField` を使用 |

---

## 📋 ツール一覧 (40ツール)

### High-Level (9) - 解析・検証 + GameKit UI + Data

| カテゴリ | ツール |
|---------|-------|
| **Logic (8)** 解析・検証 | unity_validate_integrity, unity_class_catalog, unity_class_dependency_graph, unity_scene_reference_graph, unity_scene_relationship_graph, unity_scene_dependency, unity_script_syntax, unity_spatial_analysis |
| **GameKit UI (1)** UIシステム | unity_gamekit_ui (widgetType: command, binding, list, slot, selection) |
| **GameKit Data (1)** データ・プール | unity_gamekit_data (dataType: pool, eventChannel, dataContainer, runtimeSet) |

### Mid-Level (17) - バッチ操作・プリセット

| カテゴリ | ツール |
|---------|-------|
| Transform | unity_transform_batch, unity_rectTransform_batch |
| Camera | unity_camera_bundle |
| Physics | unity_physics_bundle, unity_navmesh_bundle |
| UI (UGUI) | unity_ui_foundation, unity_ui_state, unity_ui_navigation, unity_ui_convert |
| UI Toolkit | unity_uitk_document, unity_uitk_asset |
| Input | unity_input_profile |
| 2D | unity_tilemap_bundle, unity_sprite2d_bundle |
| Animation | unity_animation_bundle (2D+3D統合) |
| Visual | unity_material_bundle, unity_light_bundle, unity_particle_bundle |

### Low-Level CRUD (8)

unity_scene_crud, unity_gameobject_crud, unity_component_crud, unity_asset_crud, unity_scriptableObject_crud, unity_prefab_crud, unity_vector_sprite_convert, unity_projectSettings_crud

### Utility (5) + Batch (1)

unity_ping, unity_compilation_await, unity_event_wiring, unity_playmode_control, unity_console_log, unity_batch_sequential_execute

---

## 🔄 PDCAワークフロー

| Phase | やること | 主要ツール |
|-------|---------|-----------|
| **Plan** | 現状把握・影響調査（初回はプロジェクト状態チェックも実施） | `inspect`操作, `scene_reference_graph(findReferencesTo)`, `class_dependency_graph(analyzeClass)`, `class_catalog(listTypes)`, `scene_dependency(analyzeScene)`, `script_syntax(analyzeScript)`, `projectSettings_crud(read, player)` |
| **Do** | 適切なレイヤーで実行 | GameKit, Batch, CRUD → `compilation_await(await)` |
| **Check** | 整合性検証 | `validate_integrity(all)`, `validate_integrity(typeCheck)`, `validate_integrity(uiAudit)`, `console_log(diff)`, `console_log(filter)`, `scene_relationship_graph(analyzeAll)`, `scene_dependency(findUnusedAssets)`, `script_syntax(findUnusedCode)`, `playmode_control(captureState)` |
| **Act** | 問題修正・動作確認 | `event_wiring(wire)`, `playmode_control(play/stop)` |

---

## 🔍 Logic Pillar - 解析・検証

```python
# シーン整合性（Missing Script, null参照, 壊れたEvent/Prefab）
unity_validate_integrity(operation='all')                    # 全チェック
unity_validate_integrity(operation='typeCheck')              # 型ミスマッチ検出
unity_validate_integrity(operation='report', scope='build_scenes')  # 複数シーンレポート
unity_validate_integrity(operation='checkPrefab', prefabPath='Assets/Prefabs/Player.prefab')  # Prefab検証
unity_validate_integrity(operation='uiAudit')                                # 全UIチェック（7種）
unity_validate_integrity(operation='uiAudit', checks=['overflow', 'overlap']) # 特定UIチェックのみ

# クラスカタログ（型の列挙・詳細）
unity_class_catalog(operation='listTypes', typeKind='MonoBehaviour', searchPath='Assets/Scripts')
unity_class_catalog(operation='inspectType', className='PlayerController', includeFields=True)

# シーン参照グラフ（GameObject間の参照解析）
unity_scene_reference_graph(operation='analyzeScene')                          # 全体
unity_scene_reference_graph(operation='findReferencesTo', objectPath='Player') # 被参照
unity_scene_reference_graph(operation='findOrphans')                           # 孤立検出

# クラス依存関係（C#スクリプト間）format: json|dot|mermaid|summary
unity_class_dependency_graph(operation='analyzeClass', target='PlayerController')
unity_class_dependency_graph(operation='findDependents', target='HealthSystem')

# シーン遷移グラフ
unity_scene_relationship_graph(operation='analyzeAll')
unity_scene_relationship_graph(operation='validateBuildSettings')

# シーンアセット依存関係（AssetDatabase経由）
unity_scene_dependency(operation='analyzeScene', scenePath='Assets/Scenes/Main.unity')
unity_scene_dependency(operation='findAssetUsage', assetPath='Assets/Materials/Player.mat')
unity_scene_dependency(operation='findSharedAssets', minSharedCount=2)
unity_scene_dependency(operation='findUnusedAssets', searchPath='Assets')

# C#ソースコード構文解析（行番号付き、リフレクション不要）
unity_script_syntax(operation='analyzeScript', scriptPath='Assets/Scripts/PlayerController.cs')
unity_script_syntax(operation='findReferences', symbolName='PlayerController', symbolType='class')
unity_script_syntax(operation='findUnusedCode', searchPath='Assets/Scripts')
unity_script_syntax(operation='analyzeMetrics', searchPath='Assets/Scripts')

# C#スクリプト内容監査（レガシーInput API, 廃止API, 空MonoBehaviour）
unity_validate_integrity(operation='scriptContentAudit', searchPath='Assets/Scripts')
```

---

## 🎮 UI Pillar - UIシステム（UXML/USS自動生成、スタンドアロンコード）

```python
# UIコマンドパネル（ボタン→Actor/Manager連携）
unity_gamekit_ui(widgetType='command', operation='createCommandPanel', panelId='cmd', parentPath='Canvas',
    commands=[{'name': 'Attack', 'commandType': 'action', 'label': '攻撃'}], targetType='actor', targetActorId='player')

# データバインディング（sourceType: health|economy|timer|custom, format: raw|percent|ratio|formatted）
unity_gamekit_ui(widgetType='binding', operation='create', targetPath='Canvas/HPBar', bindingId='hp', sourceType='health', sourceId='player_hp', format='percent')

# 動的リスト/グリッド（layout: vertical|horizontal|grid）
unity_gamekit_ui(widgetType='list', operation='create', targetPath='Canvas/Inventory', listId='inv', layout='grid', columns=4)
unity_gamekit_ui(widgetType='list', operation='addItem', listId='inv', item={'id': 'sword', 'name': '剣'})

# スロット（slotType: storage|equipment|quickslot|trash）
unity_gamekit_ui(widgetType='slot', operation='create', targetPath='Canvas/WeaponSlot', slotId='weapon', slotType='equipment', acceptedCategories=['weapon'])
unity_gamekit_ui(widgetType='slot', operation='createSlotBar', barId='quickbar', targetPath='Canvas/QuickBar', slotCount=8, slotType='quickslot')

# 選択グループ（selectionMode: radio|toggle|checkbox|tab）
unity_gamekit_ui(widgetType='selection', operation='create', targetPath='Canvas/Tabs', selectionId='tabs', selectionType='tab')
```

---

## 🏗️ GameKit Data - プール・データアーキテクチャ

```python
# オブジェクトプール（UnityEngine.Pool使用）
unity_gamekit_data(dataType='pool', operation='create', targetPath='PoolManager', poolId='bullets', prefabPath='Assets/Prefabs/Bullet.prefab', initialSize=20, maxSize=100)
unity_gamekit_data(dataType='pool', operation='inspect', poolId='bullets')

# イベントチャンネル（ScriptableObject型、eventType: void|int|float|string|Vector3|GameObject）
unity_gamekit_data(dataType='eventChannel', operation='create', dataId='OnPlayerDeath', eventType='void', createListener=True, targetPath='GameManager')
unity_gamekit_data(dataType='eventChannel', operation='create', dataId='OnDamage', eventType='float', assetPath='Assets/Data/OnDamage.asset')

# データコンテナ（ScriptableObject、リセット対応）
unity_gamekit_data(dataType='dataContainer', operation='create', dataId='PlayerStats', fields=[
    {'name': 'health', 'fieldType': 'int', 'defaultValue': 100},
    {'name': 'speed', 'fieldType': 'float', 'defaultValue': 5.0},
    {'name': 'playerName', 'fieldType': 'string', 'defaultValue': 'Player'}
], resetOnPlay=True)

# シーン間データ受け渡し用 DataContainer（staticクラスの代替）
# StageSelect で書き込み → Battle で読み取り
unity_gamekit_data(dataType='dataContainer', operation='create', dataId='SelectedStage', fields=[
    {'name': 'mapDataPath', 'fieldType': 'string', 'defaultValue': 'Assets/Data/Maps/Stage1.asset'},
    {'name': 'stageIndex', 'fieldType': 'int', 'defaultValue': 1}
], resetOnPlay=False, assetPath='Assets/Data/SelectedStage.asset')

# ランタイムセット（自動登録/解除パターン、OnChanged イベント付き）
unity_gamekit_data(dataType='runtimeSet', operation='create', dataId='ActiveEnemies', elementType='GameObject')

# 一括生成（createMultiple）→ 1回のコンパイル待ちで済む
unity_gamekit_data(dataType='eventChannel', operation='createMultiple', items=[
    {'dataId': 'OnPlayerDeath', 'eventType': 'void', 'assetPath': 'Assets/Data/Events/OnPlayerDeath.asset', 'autoCreateAsset': True},
    {'dataId': 'OnDamage', 'eventType': 'float', 'assetPath': 'Assets/Data/Events/OnDamage.asset', 'autoCreateAsset': True}
])
# autoCreateAsset=True: コンパイル完了後に .asset も自動生成
```

---

## ⚡ Mid-Level 主要ツール

```python
# Transform配置
unity_transform_batch(operation='arrangeCircle', gameObjectPaths=[...], radius=5.0)
unity_transform_batch(operation='arrangeLine', gameObjectPaths=[...], startPosition={'x':0,'y':0,'z':0}, endPosition={'x':10,'y':0,'z':0})

# 物理設定 → unity_physics_bundleで簡単設定、または component_crudで直接設定
# プリセット使用（推奨）:
unity_physics_bundle(operation='applyPreset', gameObjectPath='Player', preset='platformer2D')
# 手動設定:
unity_component_crud(operation='add', gameObjectPath='Player', componentType='Rigidbody2D',
    propertyChanges={'gravityScale':3, 'mass':1, 'collisionDetection':'Continuous', 'constraints':{'freezeRotationZ':True}})
unity_component_crud(operation='add', gameObjectPath='Player', componentType='BoxCollider2D',
    propertyChanges={'size':{'x':1,'y':1}})

# カメラバンドル (preset: default|orthographic2D|firstPerson|thirdPerson|topDown|splitScreenLeft/Right/Top/Bottom|minimap|uiCamera)
# 既存カメラにプリセット適用（推奨: 既存Main Cameraがある場合は削除せずapplyPresetを使う）
unity_camera_bundle(operation='applyPreset', gameObjectPath='Main Camera', preset='orthographic2D')
# 新規カメラ作成（シーンにカメラがない場合のみ）
unity_camera_bundle(operation='create', name='MainCam', preset='thirdPerson', position={'x':0,'y':5,'z':-10})

# UI Foundation (UGUI)
unity_ui_foundation(operation='createCanvas', name='GameUI')
unity_ui_foundation(operation='createButton', name='Btn', parentPath='GameUI', text='Click')
unity_ui_foundation(operation='addLayoutGroup', targetPath='GameUI/Panel', layoutType='Vertical', spacing=10)
unity_ui_foundation(operation='createPanel', name='Overlay', parentPath='GameUI', addCanvasGroup=True, ignoreParentGroups=True)
unity_ui_foundation(operation='extractDesignContext', targetPath='Canvas')  # UI階層の包括的デザイン情報取得

# UI要素の個別作成
unity_ui_foundation(operation='createPanel', name='Menu', parentPath='Canvas', layoutType='Vertical', spacing=20)
unity_ui_foundation(operation='createText', name='Title', parentPath='Canvas/Menu', text='Game', fontSize=32)
unity_ui_foundation(operation='createButton', name='StartBtn', parentPath='Canvas/Menu', text='Start')
unity_ui_foundation(operation='createSlider', name='VolumeSlider', parentPath='Canvas/Menu', minValue=0, maxValue=1, value=0.5)
unity_ui_foundation(operation='createToggle', name='MuteToggle', parentPath='Canvas/Menu', label='Mute', isOn=False)

# 表示切替 (show/hide/toggle)
unity_ui_foundation(operation='show', targetPath='Canvas/Menu')
unity_ui_foundation(operation='hide', targetPath='Canvas/Menu')

# UI状態管理（ゲーム操作/メニュー切替の標準手法）
# ※ 表示/非表示は CanvasGroup (alpha/interactable/blocksRaycasts) で制御。SetActive は使わない。
# ゲームプレイ状態: HUDのみ表示、メニュー非表示
unity_ui_state(operation='defineState', rootPath='Canvas', stateName='gameplay', elements=[
    {'path': 'HUD', 'visible': True},
    {'path': 'Screens/PauseMenu', 'visible': False},
    {'path': 'Screens/Inventory', 'visible': False}
])
# ポーズ状態: HUD + ポーズメニュー表示
unity_ui_state(operation='defineState', rootPath='Canvas', stateName='paused', elements=[
    {'path': 'HUD', 'visible': True, 'interactable': False},
    {'path': 'Screens/PauseMenu', 'visible': True, 'interactable': True}
])
# 排他グループ（gameplay/paused/inventoryは同時に1つだけ）
unity_ui_state(operation='createStateGroup', rootPath='Canvas', groupName='screen_mode',
    states=['gameplay', 'paused', 'inventory', 'settings'], defaultState='gameplay')
# 状態を切替
unity_ui_state(operation='applyState', rootPath='Canvas', stateName='paused')

# UIナビゲーション（キーボード/ゲームパッド）
unity_ui_navigation(operation='autoSetup', rootPath='Canvas/Menu', direction='vertical')

# UI Toolkit
unity_uitk_asset(operation='createUXML', assetPath='Assets/UI/Menu.uxml', elements=[...])
unity_uitk_asset(operation='createUSS', assetPath='Assets/UI/Menu.uss', rules=[...])
unity_uitk_asset(operation='createFromTemplate', template='menu', assetPath='Assets/UI/Menu')  # menu|dialog|hud|settings|inventory
unity_uitk_document(operation='create', gameObjectPath='UI/Menu', uxmlPath='Assets/UI/Menu.uxml')

# UI変換・デザイントークン
unity_ui_convert(operation='extractTokens', sourcePath='Canvas')  # 色・フォント・スペーシングのトークン抽出
unity_ui_convert(operation='analyze', sourcePath='Canvas')  # UGUI→UITK変換分析
unity_ui_convert(operation='extractStyles', sourcePath='Canvas')  # USS スタイル抽出

# マテリアル (preset: unlit|lit|transparent|cutout|fade|sprite|ui|emissive|metallic|glass)
unity_material_bundle(operation='create', materialPath='Assets/Mat/P.mat', shader='Standard')

# ライト (preset: daylight|moonlight|warm|cool|spotlight|candle|neon)
unity_light_bundle(operation='create', gameObjectPath='Light', lightType='directional', intensity=1.0)
unity_light_bundle(operation='createLightingSetup', setupPreset='daylight')  # daylight|nighttime|indoor|dramatic|studio|sunset

# パーティクル (preset: explosion|fire|smoke|sparkle|rain|snow|dust|trail|hit|heal|magic|leaves)
unity_particle_bundle(operation='create', gameObjectPath='FX/Fire', preset='fire')

# オーディオソース → component_crudで直接設定
# BGM例: AudioSource + ループ + 低優先度
unity_component_crud(operation='add', gameObjectPath='Audio/BGM', componentType='AudioSource',
    propertyChanges={'clip':{'$ref':'Assets/Audio/BGM.mp3'}, 'loop':True, 'volume':0.7, 'playOnAwake':True, 'priority':128})

# イベント接続（target.component でメソッドの所属コンポーネントを明示）
unity_event_wiring(operation='wire',
    source={'gameObject':'Button','component':'Button','event':'onClick'},
    target={'gameObject':'Manager','component':'GameManager','method':'StartGame'})
unity_event_wiring(operation='wireMultiple', wirings=[...])
unity_event_wiring(operation='listEvents', gameObjectPath='Button')
unity_event_wiring(operation='clearEvent',
    source={'gameObject':'Button','component':'Button','event':'onClick'})  # 全リスナー削除

# プレイモード (operation: play|pause|unpause|stop|step|getState|captureState|waitForScene)
unity_playmode_control(operation='play')
unity_playmode_control(operation='captureState', targets=['Player', 'GameManager'], includeConsole=True)  # ランタイム状態取得
unity_playmode_control(operation='captureState', targets=['GameManager'], includeSerializedFields=True)  # private フィールド読み取り（_score, _currentWave 等）
unity_playmode_control(operation='waitForScene', sceneName='Level2')  # シーン読込待機（ポーリング）

# コンソールログ (operation: getRecent|getErrors|getWarnings|getCompilationErrors|getSummary|clear|snapshot|diff|filter)
unity_console_log(operation='getErrors')
# スナップショットワークフロー: snapshot → 変更 → diff で新規ログのみ取得
unity_console_log(operation='snapshot')                                    # ログスナップショット取得
unity_console_log(operation='diff', severity=['error','warning'])          # スナップショット後の新規エラー/警告
unity_console_log(operation='filter', severity=['error'], keyword='NullRef')  # 正規表現フィルタ

# 物理プリセット (preset: platformer2D|topDown2D|fps3D|thirdPerson3D|space|racing)
unity_physics_bundle(operation='applyPreset', gameObjectPath='Player', preset='platformer2D')
unity_physics_bundle(operation='setCollisionMatrix', layerA='Player', layerB='PlayerBullet', ignore=True)
unity_physics_bundle(operation='setCollisionMatrixBatch', is2D=True, pairs=[
    {'layerA': 'PlayerBullet', 'layerB': 'Player', 'ignore': True},
    {'layerA': 'EnemyBullet', 'layerB': 'Enemy', 'ignore': True},
    {'layerA': 'PlayerBullet', 'layerB': 'EnemyBullet', 'ignore': True},
])  # 一括設定（推奨）
unity_physics_bundle(operation='createPhysicsMaterial', materialPath='Assets/Physics/Bouncy.physicMaterial', bounciness=0.8)
unity_physics_bundle(operation='createPhysicsMaterial2D', materialPath='Assets/Physics/Slippery.physicsMaterial2D', friction=0.1)
unity_physics_bundle(operation='inspect', gameObjectPath='Player')

# NavMesh (com.unity.ai.navigation パッケージ推奨)
unity_navmesh_bundle(operation='bake', gameObjectPath='Level')
unity_navmesh_bundle(operation='addAgent', gameObjectPath='Enemy', speed=5.0, stoppingDistance=1.5)
unity_navmesh_bundle(operation='addObstacle', gameObjectPath='Rock', shape='Box', carve=True)
unity_navmesh_bundle(operation='addLink', gameObjectPath='Bridge', startPoint={'x':0,'y':0,'z':0}, endPoint={'x':5,'y':2,'z':0})
unity_navmesh_bundle(operation='inspect', gameObjectPath='Enemy')
```

---

## 🔧 Low-Level CRUD

```python
# シーン
unity_scene_crud(operation='inspect', includeHierarchy=True)
unity_scene_crud(operation='load', scenePath='Assets/Scenes/Level1.unity')

# GameObject（createでcomponents配列指定可）
unity_gameobject_crud(operation='create', name='Player', parentPath='Characters',
    components=[{'type':'UnityEngine.Rigidbody2D','properties':{'gravityScale':0}}])
# 一括作成（items配列）
unity_gameobject_crud(operation='createMultiple', items=[
    {'name':'Enemy_01', 'parentPath':'Enemies', 'tag':'Enemy'},
    {'name':'Enemy_02', 'parentPath':'Enemies', 'tag':'Enemy'}
])
unity_gameobject_crud(operation='findMultiple', pattern='Enemy*', maxResults=100)
# matchMode: exact|contains|wildcard|regex（デフォルト: contains）
# exact を使うと 'Boss' が 'BossHPBar' にマッチしない
unity_gameobject_crud(operation='deleteMultiple', pattern='Boss', matchMode='exact')

# Component（componentType='*'で全取得、*Multiple操作でバッチ処理）
unity_component_crud(operation='add', gameObjectPath='Player', componentType='UnityEngine.Rigidbody2D', propertyChanges={'gravityScale':0})
unity_component_crud(operation='inspect', gameObjectPath='Player', componentType='*', includeProperties=True)
# Unity Object参照: {'$ref':'Assets/Materials/P.mat'} or {'$ref':'Canvas/Panel/Button'}
# クロスシーン一括更新（複数シーンの参照を一度に設定。現在のシーンは自動保存→復帰）
unity_component_crud(operation='crossSceneUpdate', updates=[
    {'scenePath':'Assets/Scenes/Home.unity', 'gameObjectPath':'HomeUI', 'componentType':'HomeUI',
     'propertyChanges':{'data':{'$ref':'Assets/Data/Config.asset'}}},
    {'scenePath':'Assets/Scenes/Game.unity', 'gameObjectPath':'GameUI', 'componentType':'GameUI',
     'propertyChanges':{'data':{'$ref':'Assets/Data/Config.asset'}}}
])

# Asset
unity_asset_crud(operation='create', assetPath='Assets/Scripts/Player.cs', content='...')
# 一括作成（1回のRefreshで全ファイルをインポート。.csは1回のコンパイルで済む）
unity_asset_crud(operation='createMultiple', items=[
    {'assetPath':'Assets/Scripts/IWeapon.cs', 'content':'...'},
    {'assetPath':'Assets/Scripts/Sword.cs', 'content':'...'}
])
unity_asset_crud(operation='updateImporter', assetPath='Assets/Textures/s.png', propertyChanges={'textureType':'Sprite'})
unity_asset_crud(operation='forceReimport', assetPath='Assets/Scripts/Player.cs')  # 外部ツールで作成したファイルをUnityに認識させる

# ScriptableObject
unity_scriptableObject_crud(operation='create', typeName='MyGame.Config', assetPath='Assets/Data/Config.asset', properties={'version':1})

# Prefab
unity_prefab_crud(operation='create', gameObjectPath='Player', prefabPath='Assets/Prefabs/Player.prefab')
unity_prefab_crud(operation='instantiate', prefabPath='Assets/Prefabs/Enemy.prefab', parentPath='Enemies', position={'x':0,'y':0,'z':5})
# 一括インスタンス化（レベルデザインに最適）
unity_prefab_crud(operation='instantiateMultiple', items=[
    {'prefabPath':'Assets/Prefabs/Tree.prefab', 'position':{'x':5,'y':0,'z':3}},
    {'prefabPath':'Assets/Prefabs/Tree.prefab', 'position':{'x':-2,'y':0,'z':8}}
])
# Prefab直接編集（instantiate不要）
unity_prefab_crud(operation='editAsset', prefabPath='Assets/Prefabs/Enemy.prefab',
    tag='Enemy', layer='Enemy', componentChanges=[{'componentType':'CircleCollider2D','propertyChanges':{'isTrigger':True}}])
# 複数Prefab一括編集
unity_prefab_crud(operation='editMultiple', prefabPaths=['Assets/Prefabs/Enemies/Basic.prefab','Assets/Prefabs/Enemies/Fast.prefab'],
    tag='Enemy', layer='Enemy')

# ProjectSettings (category: player|quality|time|physics|physics2d|audio|editor|tagsLayers)
unity_projectSettings_crud(operation='write', category='tagsLayers', property='addTag', value='Enemy')
unity_projectSettings_crud(operation='addSceneToBuild', scenePath='Assets/Scenes/Level1.unity')
```

---

## 🔗 GameKit統合チェックリスト（Do→Check間に必須）

GameKit でコード生成した後、以下の統合手順を**必ず**実行すること。

### EventChannel統合
```python
# 1. 手書きスクリプトに [SerializeField] を追加
#    [SerializeField] private OnDamageEventChannel onDamageEvent;
# 2. 発火側: イベント発生箇所で Raise() を呼ぶ
#    - void型: onDamageEvent.Raise()
#    - 型付き: onDamageEvent.Raise(damageValue)  ← eventType に応じた引数
# 3. 購読側: OnEnable で Register(), OnDisable で Unregister()
#    onEnemyKillEvent.Register(OnEnemyKilled);
# 4. Inspector参照を設定
unity_component_crud(operation='update', gameObjectPath='Player',
    componentType='PlayerController', propertyChanges={'onDamageEvent': {'$ref': 'Assets/Data/Events/OnDamage.asset'}})
```

### Pool統合
```python
# 1. Fire() や Spawn() で Pool.Get() を使用（Instantiate の代わり）
#    var bullet = BulletPoolPool.FindById("BulletPool").Get(pos, rot);
# 2. 回収側: Destroy(gameObject) の代わりに Pool.Release() を使用
#    BulletPoolPool.FindById("BulletPool").Release(gameObject);
# 3. Prefab に Pool参照を設定
unity_prefab_crud(operation='editAsset', prefabPath='Assets/Prefabs/Bullet.prefab',
    componentChanges=[{'componentType': 'BulletPoolPool', 'propertyChanges': {'prefab': {'$ref': 'Assets/Prefabs/Bullet.prefab'}}}])
```

### RuntimeSet統合
```python
# 1. 対象スクリプトに [SerializeField] private ActiveEnemiesRuntimeSet activeEnemiesSet;
# 2. OnEnable(): activeEnemiesSet?.Register(gameObject);
# 3. OnDisable(): activeEnemiesSet?.Unregister(gameObject);
# 4. Prefab/シーンオブジェクトに参照設定
unity_prefab_crud(operation='editMultiple', prefabPaths=[...],
    componentChanges=[{'componentType': 'EnemyAI', 'propertyChanges': {'activeEnemiesSet': {'$ref': 'Assets/Data/RuntimeSets/ActiveEnemies.asset'}}}])
```

### DataContainer統合
```python
# 1. 書き込み側: [SerializeField] private PlayerStatsDataContainer playerStats;
#    playerStats.currentHP = _currentHP;  (直接フィールドアクセス)
# 2. 読み取り側: 同じSO参照を持つ別スクリプトから読む
#    hpLabel.text = $"HP: {playerStats.currentHP}";
# 3. UIBinding連携: SetValue() でバインディングを更新
#    PlayerHpUIBinding.FindById("player_hp")?.SetValue(playerStats.currentHP);
```

### GameKit UIパラメータ対応表

| widgetType | create系操作 | パラメータ | 意味 |
|-----------|-------------|-----------|------|
| binding   | create | **targetPath** | バインド先のUI要素パス |
| list      | create | **targetPath** | リスト配置先 |
| command   | createCommandPanel | **parentPath** | コマンドパネルの親 |
| slot      | create | **targetPath** | スロットの配置先 |
| slot      | createSlotBar | **parentPath** | スロットバーの親 |
| selection | create | **targetPath** | 選択グループの配置先 |

---

## ⚡ パフォーマンス & トラブルシューティング

**高速化:** `includeProperties=False`(10倍速), `propertyFilter`, `maxResults`制限, `*Multiple`操作, `unity_batch_sequential_execute`

**接続:** `unity_ping` → Tools > MCP Assistant確認 → ポート7077確認
**コンパイル:** `compilation_await(await)` → `console_log(getCompilationErrors)`
**整合性:** `validate_integrity(all)` → `scene_reference_graph(findOrphans)`

---

## 📚 Unity標準コンポーネント リファレンス

`unity_component_crud`の`componentType`に指定する型名。enum値はint指定可。

| カテゴリ | componentType → 主要プロパティ |
|---------|------|
| **Transform** | `Transform` position,localScale / `RectTransform` anchoredPosition,sizeDelta,anchorMin,anchorMax,pivot |
| **Physics2D** | `Rigidbody2D` bodyType(0=Dynamic,1=Kinematic,2=Static),mass,gravityScale,collisionDetection(0=Discrete,1=Continuous) / `BoxCollider2D` size,offset,isTrigger / `CircleCollider2D` radius,isTrigger / `CapsuleCollider2D` size,direction |
| **Physics3D** | `Rigidbody` mass,drag,useGravity,isKinematic / `BoxCollider` center,size,isTrigger / `SphereCollider` radius / `CapsuleCollider` radius,height,direction / `MeshCollider` convex |
| **CharCtrl** | `CharacterController` radius,height,center,slopeLimit(45),stepOffset(0.3),skinWidth(0.08) — FPS: h=1.8,r=0.4; TPS: h=2.0,r=0.5; Platformer: h=1.0,r=0.3 |
| **Render2D** | `SpriteRenderer` sprite,color,flipX,flipY,sortingLayerName,sortingOrder |
| **Render3D** | `MeshFilter` sharedMesh / `MeshRenderer` sharedMaterials,shadowCastingMode / `LineRenderer` startWidth,endWidth / `TrailRenderer` time,startWidth |
| **Camera** | `Camera` fieldOfView,orthographic,orthographicSize,clearFlags(1=Skybox,2=SolidColor),backgroundColor |
| **Light** | `Light` type(0=Spot,1=Directional,2=Point),color,intensity,range,shadows(0=None,1=Hard,2=Soft) |
| **Audio** | `AudioSource` clip,volume,pitch,loop,playOnAwake,spatialBlend(0=2D,1=3D) |
| **Animation** | `Animator` runtimeAnimatorController,avatar,applyRootMotion,updateMode |
| **UI Canvas** | `Canvas` renderMode(0=Overlay,1=Camera,2=World) / `CanvasScaler` uiScaleMode,referenceResolution |
| **UI Display** | `Image` sprite,color,type,fillAmount / `TMPro.TextMeshProUGUI` text,fontSize,color |
| **UI Input** | `Button` interactable / `Toggle` isOn / `Slider` value,minValue,maxValue / `TMPro.TMP_InputField` text,characterLimit / `ScrollRect` content,horizontal,vertical |
| **UI Layout** | `HorizontalLayoutGroup` spacing,padding,childAlignment / `VerticalLayoutGroup` / `GridLayoutGroup` cellSize,spacing,constraint / `ContentSizeFitter` horizontalFit,verticalFit |
| **NavMesh** | `NavMeshAgent` speed,acceleration,stoppingDistance,radius,height / `NavMeshObstacle` shape,carve,carveOnlyStationary / `Unity.AI.Navigation.NavMeshSurface` agentTypeID,collectObjects / `Unity.AI.Navigation.NavMeshModifier` overrideArea,area |
| **Light2D** | `UnityEngine.Rendering.Universal.Light2D` lightType(0=Freeform,1=Sprite,2=Point,4=Global),color,intensity,pointLightOuterRadius,shapeLightFalloffSize |
| **Particle** | `ParticleSystem` → `unity_particle_bundle`推奨 |

※ `UnityEngine.`プレフィックスは省略可。UI系は`UnityEngine.UI.`、TextMeshProは`TMPro.`が必要。
※ NavMeshSurface/NavMeshModifierは`Unity.AI.Navigation.`プレフィックス必須（com.unity.ai.navigationパッケージ）。
※ Light2Dは`UnityEngine.Rendering.Universal.`プレフィックス必須（URP 2Dライティング）。

---

## 🏗️ Unity ベストプラクティス

### 物理最適化
- **Layer Collision Matrix**: `projectSettings_crud(category='physics')` で不要な衝突ペアを無効化（大幅な負荷軽減）
- **Collision Detection**: 高速移動オブジェクト（弾丸等）は `Continuous` に設定。壁すり抜け（トンネリング）を防止
- **FixedTimestep**: デフォルト 0.02s（50Hz）。モバイルでは 0.04s（25Hz）で負荷軽減可能
- **静的コライダー**: Rigidbody のないコライダーを移動させない（物理ワールドの再構築が走る）

### ScriptableObject 設計パターン
- **データ駆動設計**: 敵パラメータ、アイテムデータ、スキルテーブル等は `scriptableObject_crud` でSOに分離
- **イベントチャネル**: SO ベースの Observer パターンでシステム間を疎結合に（`game_mechanics_guide(mechanic='event_channel')`）
- **ランタイムセット**: シーン内オブジェクトの動的登録/解除に SO リストを使用

### Animator Controller 設計
- **Hub-and-Spoke**: 中央 Empty State から各アクション状態へ放射状遷移。デバッグ容易
- **Blend Tree**: 速度・方向の連続値にはステートではなく BlendTree を使用。ステート数削減
- **Layer 分離**: 上半身/下半身/表情を独立レイヤーで制御（`game_mechanics_guide(mechanic='animation_controller')`）

### オブジェクトプーリング
- **頻繁な生成/破棄を避ける**: 弾丸、エフェクト、敵スポーンには `ObjectPool<T>` を使用
- **GameKit Pool**: `gamekit_data(dataType='pool')` で生成。`game_mechanics_guide(mechanic='object_pooling')` 参照

---

Unity-AI-Forge v{VERSION} - 41 Tools, 3-Layer Architecture
