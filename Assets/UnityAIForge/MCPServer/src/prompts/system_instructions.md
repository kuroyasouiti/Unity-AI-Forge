# Unity-AI-Forge MCP Server v{VERSION} - Quick Reference

AI駆動型Unity開発ツールキット。49ツール、3層構造（Low/Mid/High-Level）、3-Pillar GameKit（UI, Presentation, Logic）。

## 🔴 Critical Rules

1. **.metaファイルは絶対に編集しない**（Unity自動管理）
2. **全Unity操作にMCPツール（unity_*）を使用**
3. **変更前にinspect操作で対象を確認**
4. **ツール優先順位: High-Level → Mid-Level → Low-Level**
5. **UI優先設計**: UIから実装し、ロジックは後
6. **PDCA遵守**: Plan(inspect/graph) → Do(実行) → Check(validate_integrity/console_log) → Act(修正)

---

## 📋 ツール一覧 (49ツール)

### High-Level GameKit (15) - 3-Pillar Architecture

| Pillar | ツール |
|--------|-------|
| **Logic (5)** 解析・検証 | unity_validate_integrity, unity_class_catalog, unity_class_dependency_graph, unity_scene_reference_graph, unity_scene_relationship_graph |
| **UI (5)** UIシステム | unity_gamekit_ui_command, unity_gamekit_ui_binding, unity_gamekit_ui_list, unity_gamekit_ui_slot, unity_gamekit_ui_selection |
| **Presentation (5)** 演出 | unity_gamekit_animation_sync, unity_gamekit_effect, unity_gamekit_feedback, unity_gamekit_vfx, unity_gamekit_audio |

### Mid-Level (23) - バッチ操作・プリセット

| カテゴリ | ツール |
|---------|-------|
| Transform | unity_transform_batch, unity_rectTransform_batch |
| Physics/Camera | unity_physics_bundle, unity_camera_rig |
| UI (UGUI) | unity_ui_foundation, unity_ui_hierarchy, unity_ui_state, unity_ui_navigation |
| UI Toolkit | unity_uitk_document, unity_uitk_asset |
| Audio/Input/Character | unity_audio_source_bundle, unity_input_profile, unity_character_controller_bundle |
| 2D | unity_tilemap_bundle, unity_sprite2d_bundle, unity_animation2d_bundle |
| 3D/Visual | unity_material_bundle, unity_light_bundle, unity_particle_bundle, unity_animation3d_bundle |
| Events/Dev-Cycle | unity_event_wiring, unity_playmode_control, unity_console_log |

### Low-Level CRUD (8)

unity_scene_crud, unity_gameobject_crud, unity_component_crud, unity_asset_crud, unity_scriptableObject_crud, unity_prefab_crud, unity_vector_sprite_convert, unity_projectSettings_crud

### Utility (2) + Batch (1)

unity_ping, unity_compilation_await, unity_batch_sequential_execute

---

## 🔄 PDCAワークフロー

| Phase | やること | 主要ツール |
|-------|---------|-----------|
| **Plan** | 現状把握・影響調査 | `inspect`操作, `scene_reference_graph(findReferencesTo)`, `class_dependency_graph(analyzeClass)`, `class_catalog(listTypes)` |
| **Do** | 適切なレイヤーで実行 | GameKit, Batch, CRUD → `compilation_await(await)` |
| **Check** | 整合性検証 | `validate_integrity(all)`, `scene_relationship_graph(analyzeAll)`, `console_log(getErrors)` |
| **Act** | 問題修正・動作確認 | `event_wiring(wire)`, `validate_integrity(removeMissingScripts)`, `playmode_control(play/stop)` |

---

## 🔍 Logic Pillar - 解析・検証

```python
# シーン整合性（Missing Script, null参照, 壊れたEvent/Prefab）
unity_validate_integrity(operation='all')                    # 全チェック
unity_validate_integrity(operation='removeMissingScripts')   # 自動除去（Undo可）

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
```

---

## 🎮 UI Pillar - UIシステム（UXML/USS自動生成、スタンドアロンコード）

```python
# UIコマンドパネル（ボタン→Actor/Manager連携）
unity_gamekit_ui_command(operation='createCommandPanel', panelId='cmd', canvasPath='Canvas',
    commands=[{'name': 'Attack', 'commandType': 'action', 'label': '攻撃'}], targetType='actor', targetActorId='player')

# データバインディング（sourceType: health|economy|timer|custom, format: raw|percent|ratio|formatted）
unity_gamekit_ui_binding(operation='create', targetPath='Canvas/HPBar', bindingId='hp', sourceType='health', sourceId='player_hp', format='percent')

# 動的リスト/グリッド（layout: vertical|horizontal|grid）
unity_gamekit_ui_list(operation='create', targetPath='Canvas/Inventory', listId='inv', layout='grid', gridColumns=4)
unity_gamekit_ui_list(operation='addItem', listId='inv', itemData={'id': 'sword', 'name': '剣'})

# スロット（slotType: storage|equipment|quickslot|trash）
unity_gamekit_ui_slot(operation='create', targetPath='Canvas/WeaponSlot', slotId='weapon', slotType='equipment', acceptTags=['weapon'])
unity_gamekit_ui_slot(operation='createSlotBar', barId='quickbar', targetPath='Canvas/QuickBar', slotCount=8, slotType='quickslot')

# 選択グループ（selectionMode: radio|toggle|checkbox|tab）
unity_gamekit_ui_selection(operation='create', targetPath='Canvas/Tabs', selectionId='tabs', selectionMode='tab')
```

---

## 🎨 Presentation Pillar - 演出（コード生成、スタンドアロン）

```python
# 複合エフェクト（componentType: particle|sound|cameraShake|screenFlash|timeScale）
unity_gamekit_effect(operation='create', targetPath='FX/Explosion', effectId='boom',
    components=[{'type': 'particle', 'prefabPath': 'Assets/Prefabs/Boom.prefab'}, {'type': 'cameraShake', 'intensity': 0.5, 'duration': 0.3}])
unity_gamekit_effect(operation='createManager', targetPath='EffectManager')

# フィードバック（type: hitstop|screenShake|flash|colorFlash|scale|position|rotation|sound|particle|haptic）
unity_gamekit_feedback(operation='create', targetPath='FBMgr', feedbackId='hit',
    components=[{'type': 'hitstop', 'duration': 0.05}, {'type': 'screenShake', 'intensity': 0.3, 'duration': 0.15}])

# VFX（プーリング対応）
unity_gamekit_vfx(operation='create', targetPath='FX/Boom', vfxId='boom_vfx', particlePrefabPath='Assets/Prefabs/Boom.prefab', usePooling=True, poolSize=10)

# オーディオ（audioType: sfx|music|ambient|voice|ui）
unity_gamekit_audio(operation='create', targetPath='Audio/BGM', audioId='bgm', audioType='music', audioClipPath='Assets/Audio/BGM.mp3', loop=True)

# アニメーション同期（syncSource: rigidbody2d|rigidbody3d|transform|health|custom）
unity_gamekit_animation_sync(operation='create', targetPath='Player', syncId='anim', syncSource='rigidbody2d', animatorPath='Player')
unity_gamekit_animation_sync(operation='addSyncRule', syncId='anim', parameterName='Speed', sourceField='velocity.magnitude')
unity_gamekit_animation_sync(operation='addTriggerRule', syncId='anim', triggerName='Hit', eventSource='health', eventType='damage')
```

---

## ⚡ Mid-Level 主要ツール

```python
# Transform配置
unity_transform_batch(operation='arrangeCircle', gameObjectPaths=[...], radius=5.0)
unity_transform_batch(operation='arrangeLine', gameObjectPaths=[...], startPosition={'x':0,'y':0,'z':0}, endPosition={'x':10,'y':0,'z':0})

# 物理プリセット (preset: dynamic|kinematic|static|character|platformer|topDown|vehicle|projectile)
unity_physics_bundle(operation='applyPreset2D', gameObjectPaths=['Player'], preset='character')

# カメラリグ (rigType: follow|orbit|splitScreen|fixed|dolly)
unity_camera_rig(operation='createRig', rigType='follow', rigName='MainCam', targetPath='Player', offset={'x':0,'y':5,'z':-10})

# UI Foundation (UGUI)
unity_ui_foundation(operation='createCanvas', name='GameUI')
unity_ui_foundation(operation='createButton', name='Btn', parentPath='GameUI', text='Click')
unity_ui_foundation(operation='addLayoutGroup', targetPath='GameUI/Panel', layoutType='Vertical', spacing=10)

# 宣言的UI構築
unity_ui_hierarchy(operation='create', parentPath='Canvas', hierarchy={
    'type':'panel', 'name':'Menu', 'children':[
        {'type':'text','name':'Title','text':'Game','fontSize':32},
        {'type':'button','name':'StartBtn','text':'Start'}
    ], 'layout':'Vertical', 'spacing':20})
unity_ui_hierarchy(operation='show', targetPath='Canvas/Menu')  # show/hide/toggle

# UI状態管理
unity_ui_state(operation='defineState', rootPath='Canvas', stateName='menu', elements=[...])
unity_ui_state(operation='applyState', rootPath='Canvas', stateName='menu')

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

# オーディオソース (preset: music|sfx|ambient|voice|ui)
unity_audio_source_bundle(operation='create', gameObjectPath='Audio/BGM', preset='music', clipPath='Assets/Audio/BGM.mp3')

# イベント接続
unity_event_wiring(operation='wire',
    source={'gameObject':'Button','component':'Button','event':'onClick'},
    target={'gameObject':'Manager','method':'StartGame'})
unity_event_wiring(operation='wireMultiple', wirings=[...])
unity_event_wiring(operation='listEvents', gameObjectPath='Button')

# プレイモード (operation: play|pause|unpause|stop|step|getState)
unity_playmode_control(operation='play')

# コンソールログ (operation: getRecent|getErrors|getWarnings|getCompilationErrors|getSummary|clear)
unity_console_log(operation='getErrors')
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
**整合性:** `validate_integrity(all)` → `validate_integrity(removeMissingScripts)` → `scene_reference_graph(findOrphans)`

---

## 📚 Unity標準コンポーネント リファレンス

`unity_component_crud`の`componentType`に指定する型名。enum値はint指定可。

| カテゴリ | componentType → 主要プロパティ |
|---------|------|
| **Transform** | `Transform` position,localScale / `RectTransform` anchoredPosition,sizeDelta,anchorMin,anchorMax,pivot |
| **Physics2D** | `Rigidbody2D` bodyType(0=Dynamic,1=Kinematic,2=Static),mass,gravityScale / `BoxCollider2D` size,offset,isTrigger / `CircleCollider2D` radius,isTrigger / `CapsuleCollider2D` size,direction / `CompositeCollider2D` geometryType |
| **Physics3D** | `Rigidbody` mass,drag,useGravity,isKinematic / `BoxCollider` center,size,isTrigger / `SphereCollider` radius / `CapsuleCollider` radius,height,direction / `MeshCollider` convex / `CharacterController` radius,height,slopeLimit,stepOffset |
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
| **NavMesh** | `NavMeshAgent` speed,stoppingDistance,radius / `NavMeshObstacle` shape,carve |
| **Particle** | `ParticleSystem` → `unity_particle_bundle`推奨 |

※ `UnityEngine.`プレフィックスは省略可。UI系は`UnityEngine.UI.`、TextMeshProは`TMPro.`が必要。

---

Unity-AI-Forge v{VERSION} - 49 Tools, 3-Layer Architecture, 3-Pillar GameKit
