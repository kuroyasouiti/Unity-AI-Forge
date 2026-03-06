# Unity-AI-Forge MCP Server v{VERSION} - Quick Reference

AI駆動型Unity開発ツールキット。41ツール、3層構造（Low/Mid/High-Level）。

## 🔴 Critical Rules

1. **.metaファイルは絶対に編集しない**（Unity自動管理）
2. **全Unity操作にMCPツール（unity_*）を使用**
3. **変更前にinspect操作で対象を確認**
4. **ツール優先順位: High-Level → Mid-Level → Low-Level**
5. **UI優先設計**: UIから実装し、ロジックは後
6. **PDCA遵守**: Plan(inspect/graph) → Do(実行) → Check(validate_integrity/console_log) → Act(修正)
7. **コンパイル待ち必須**: コード生成ツール(GameKit create操作, asset_crud create *.cs)使用後は必ず `compilation_await(await)` を実行してから次の操作
8. **物理設定のベストプラクティス**: Layer Collision Matrixで不要な衝突を除外、高速オブジェクトのCollision DetectionはContinuousに設定
9. **ゲーム操作/メニュー切替には `ui_state` を使用**: gameplay ↔ pause、gameplay ↔ inventory 等の画面モード切替は `unity_ui_state` の `defineState` + `applyState` で管理。`createStateGroup` で排他制御

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

## 📋 ツール一覧 (41ツール)

### High-Level (9) - 解析・検証 + GameKit UI + Data

| カテゴリ | ツール |
|---------|-------|
| **Logic (7)** 解析・検証 | unity_validate_integrity, unity_class_catalog, unity_class_dependency_graph, unity_scene_reference_graph, unity_scene_relationship_graph, unity_scene_dependency, unity_script_syntax |
| **GameKit UI (1)** UIシステム | unity_gamekit_ui (widgetType: command, binding, list, slot, selection) |
| **GameKit Data (1)** データ・プール | unity_gamekit_data (dataType: pool, eventChannel, dataContainer, runtimeSet) |

### Mid-Level (18) - バッチ操作・プリセット

| カテゴリ | ツール |
|---------|-------|
| Transform | unity_transform_batch, unity_rectTransform_batch |
| Camera | unity_camera_bundle |
| Physics | unity_physics_bundle, unity_navmesh_bundle |
| UI (UGUI) | unity_ui_foundation, unity_ui_state, unity_ui_navigation |
| UI Toolkit | unity_uitk_document, unity_uitk_asset |
| Input | unity_input_profile |
| 2D | unity_tilemap_bundle, unity_sprite2d_bundle, unity_animation2d_bundle |
| 3D/Visual | unity_material_bundle, unity_light_bundle, unity_particle_bundle, unity_animation3d_bundle |

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
| **Check** | 整合性検証 | `validate_integrity(all)`, `validate_integrity(typeCheck)`, `console_log(diff)`, `console_log(filter)`, `scene_relationship_graph(analyzeAll)`, `scene_dependency(findUnusedAssets)`, `script_syntax(findUnusedCode)`, `playmode_control(captureState)` |
| **Act** | 問題修正・動作確認 | `event_wiring(wire)`, `playmode_control(play/stop)` |

---

## 🔍 Logic Pillar - 解析・検証

```python
# シーン整合性（Missing Script, null参照, 壊れたEvent/Prefab）
unity_validate_integrity(operation='all')                    # 全チェック
unity_validate_integrity(operation='typeCheck')              # 型ミスマッチ検出
unity_validate_integrity(operation='report', scope='build_scenes')  # 複数シーンレポート
unity_validate_integrity(operation='checkPrefab', prefabPath='Assets/Prefabs/Player.prefab')  # Prefab検証

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
```

---

## 🎮 UI Pillar - UIシステム（UXML/USS自動生成、スタンドアロンコード）

```python
# UIコマンドパネル（ボタン→Actor/Manager連携）
unity_gamekit_ui(widgetType='command', operation='createCommandPanel', panelId='cmd', canvasPath='Canvas',
    commands=[{'name': 'Attack', 'commandType': 'action', 'label': '攻撃'}], targetType='actor', targetActorId='player')

# データバインディング（sourceType: health|economy|timer|custom, format: raw|percent|ratio|formatted）
unity_gamekit_ui(widgetType='binding', operation='create', targetPath='Canvas/HPBar', bindingId='hp', sourceType='health', sourceId='player_hp', format='percent')

# 動的リスト/グリッド（layout: vertical|horizontal|grid）
unity_gamekit_ui(widgetType='list', operation='create', targetPath='Canvas/Inventory', listId='inv', layout='grid', gridColumns=4)
unity_gamekit_ui(widgetType='list', operation='addItem', listId='inv', itemData={'id': 'sword', 'name': '剣'})

# スロット（slotType: storage|equipment|quickslot|trash）
unity_gamekit_ui(widgetType='slot', operation='create', targetPath='Canvas/WeaponSlot', slotId='weapon', slotType='equipment', acceptTags=['weapon'])
unity_gamekit_ui(widgetType='slot', operation='createSlotBar', barId='quickbar', targetPath='Canvas/QuickBar', slotCount=8, slotType='quickslot')

# 選択グループ（selectionMode: radio|toggle|checkbox|tab）
unity_gamekit_ui(widgetType='selection', operation='create', targetPath='Canvas/Tabs', selectionId='tabs', selectionMode='tab')
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

# ランタイムセット（自動登録/解除パターン）
unity_gamekit_data(dataType='runtimeSet', operation='create', dataId='ActiveEnemies', elementType='GameObject')
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

# UI要素の個別作成
unity_ui_foundation(operation='createPanel', name='Menu', parentPath='Canvas', layoutType='Vertical', spacing=20)
unity_ui_foundation(operation='createText', name='Title', parentPath='Canvas/Menu', text='Game', fontSize=32)
unity_ui_foundation(operation='createButton', name='StartBtn', parentPath='Canvas/Menu', text='Start')

# 表示切替 (show/hide/toggle)
unity_ui_foundation(operation='show', targetPath='Canvas/Menu')
unity_ui_foundation(operation='hide', targetPath='Canvas/Menu')

# UI状態管理（ゲーム操作/メニュー切替の標準手法）
# ゲームプレイ状態: HUDのみ表示、メニュー非表示
unity_ui_state(operation='defineState', rootPath='Canvas', stateName='gameplay', elements=[
    {'path': 'HUD', 'active': True},
    {'path': 'Screens/PauseMenu', 'active': False, 'interactable': False},
    {'path': 'Screens/Inventory', 'active': False}
])
# ポーズ状態: HUD + ポーズメニュー表示
unity_ui_state(operation='defineState', rootPath='Canvas', stateName='paused', elements=[
    {'path': 'HUD', 'active': True, 'interactable': False},
    {'path': 'Screens/PauseMenu', 'active': True, 'interactable': True}
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

# イベント接続
unity_event_wiring(operation='wire',
    source={'gameObject':'Button','component':'Button','event':'onClick'},
    target={'gameObject':'Manager','method':'StartGame'})
unity_event_wiring(operation='wireMultiple', wirings=[...])
unity_event_wiring(operation='listEvents', gameObjectPath='Button')

# プレイモード (operation: play|pause|unpause|stop|step|getState|captureState|waitForScene)
unity_playmode_control(operation='play')
unity_playmode_control(operation='captureState', targets=['Player', 'GameManager'], includeConsole=True)  # ランタイム状態取得
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
unity_scene_crud(operation='load', scenePath='Assets/Scenes/Level1.unity', loadMode='single')

# GameObject（createでcomponents配列指定可）
unity_gameobject_crud(operation='create', name='Player', parentPath='Characters',
    components=[{'type':'UnityEngine.Rigidbody2D','properties':{'gravityScale':0}}])
unity_gameobject_crud(operation='findMultiple', pattern='Enemy*', maxResults=100)

# Component（componentType='*'で全取得、*Multiple操作でバッチ処理）
unity_component_crud(operation='add', gameObjectPath='Player', componentType='UnityEngine.Rigidbody2D', propertyChanges={'gravityScale':0})
unity_component_crud(operation='inspect', gameObjectPath='Player', componentType='*', includeProperties=True)
# Unity Object参照: {'$ref':'Assets/Materials/P.mat'} or {'$ref':'Canvas/Panel/Button'}

# Asset
unity_asset_crud(operation='create', assetPath='Assets/Scripts/Player.cs', content='...')
unity_asset_crud(operation='updateImporter', assetPath='Assets/Textures/s.png', propertyChanges={'textureType':'Sprite'})

# ScriptableObject
unity_scriptableObject_crud(operation='create', typeName='MyGame.Config', assetPath='Assets/Data/Config.asset', properties={'version':1})

# Prefab
unity_prefab_crud(operation='create', gameObjectPath='Player', prefabPath='Assets/Prefabs/Player.prefab')
unity_prefab_crud(operation='instantiate', prefabPath='Assets/Prefabs/Enemy.prefab', parentPath='Enemies', position={'x':0,'y':0,'z':5})

# ProjectSettings (category: player|quality|time|physics|physics2d|audio|editor|tagsLayers)
unity_projectSettings_crud(operation='write', category='tagsLayers', property='addTag', value='Enemy')
unity_projectSettings_crud(operation='addSceneToBuild', scenePath='Assets/Scenes/Level1.unity')
```

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
