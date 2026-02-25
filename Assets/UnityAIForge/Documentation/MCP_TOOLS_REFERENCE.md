# Unity-AI-Forge MCP Tools Reference

このドキュメントはUnity-AI-ForgeのMCPツール一覧をまとめたものです。

## ツール概要

| カテゴリ | ツール数 | 説明 |
|----------|----------|------|
| Utility | 5 | 接続確認・コンパイル待機・プレイモード・ログ |
| Low-Level CRUD | 8 | シーン・GameObject・コンポーネント・アセット管理 |
| Mid-Level Batch | 18 | バッチ操作・プリセット・UI・ビジュアル制御 |
| High-Level GameKit | 17 | 3本柱ゲーム開発フレームワーク |

**合計: 48 ツール**

---

## 1. Core Tools (基本ツール)

### `unity_ping`
Bridge接続確認とハートビート情報取得。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| (なし) | - | - |

---

### `unity_compilation_await`
Unityのスクリプトコンパイル完了を待機。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | 操作（現在は`await`のみ） |
| timeoutSeconds | integer | タイムアウト秒数（デフォルト: 60） |

---

### `unity_scene_crud`
シーン管理: 作成/読込/保存/削除/複製/検査。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `create`, `load`, `save`, `delete`, `duplicate`, `inspect` |
| scenePath | string | シーンファイルパス |
| newSceneName | string | 複製時の新規名 |
| additive | boolean | 追加読込モード |
| includeHierarchy | boolean | ヒエラルキーを含める |
| includeComponents | boolean | コンポーネント詳細を含める |
| filter | string | GameObjectフィルタパターン |

---

### `unity_gameobject_crud`
GameObjectライフサイクル管理。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `create`, `delete`, `move`, `rename`, `update`, `duplicate`, `inspect`, `findMultiple`, `deleteMultiple`, `inspectMultiple` |
| gameObjectPath | string | ターゲットのヒエラルキーパス |
| parentPath | string | 親GameObjectパス |
| template | string | プリミティブテンプレート（Cube, Sphere等） |
| name | string | 新規/リネーム名 |
| tag | string | タグ設定 |
| layer | int/string | レイヤー設定 |
| active | boolean | アクティブ状態 |
| static | boolean | 静的フラグ |
| pattern | string | バッチ操作用パターン |
| components | array | 自動アタッチするコンポーネント |

---

## 2. Asset Management (アセット管理)

### `unity_component_crud`
コンポーネント管理: 追加/削除/更新/検査。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `add`, `remove`, `update`, `inspect`, `addMultiple`, `removeMultiple`, `updateMultiple`, `inspectMultiple` |
| gameObjectPath | string | ターゲットGameObjectパス |
| componentType | string | コンポーネント型名（例: `UnityEngine.Rigidbody2D`） |
| propertyChanges | object | プロパティ変更 |
| pattern | string | バッチ操作用パターン |

**参照形式:**
- アセット参照: `{"$ref": "Assets/Materials/Red.mat"}`
- シーンオブジェクト参照: `{"$ref": "Canvas/Panel/Button"}`

---

### `unity_asset_crud`
アセットファイル管理。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `create`, `update`, `updateImporter`, `delete`, `rename`, `duplicate`, `inspect`, `findMultiple`, `deleteMultiple`, `inspectMultiple` |
| assetPath | string | アセットファイルパス |
| content | string | ファイル内容（create/update用） |
| destinationPath | string | リネーム/複製先 |
| propertyChanges | object | インポーター設定変更 |

---

### `unity_scriptableObject_crud`
ScriptableObjectアセット管理。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `create`, `inspect`, `update`, `delete`, `duplicate`, `list`, `findByType` |
| typeName | string | 型名（例: `MyGame.GameConfig`） |
| assetPath | string | アセットパス |
| properties | object | プロパティ値 |

---

### `unity_prefab_crud`
プレハブワークフロー管理。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `create`, `update`, `inspect`, `instantiate`, `unpack`, `applyOverrides`, `revertOverrides` |
| gameObjectPath | string | GameObjectパス |
| prefabPath | string | プレハブアセットパス |
| parentPath | string | インスタンス化時の親 |
| position | object | 位置 {x, y, z} |
| rotation | object | 回転 {x, y, z} |
| unpackMode | string | `completely`, `outermost` |

---

## 3. Transform & Physics (変形・物理)

### `unity_transform_batch`
バッチ変形操作。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `arrangeCircle`, `arrangeLine`, `renameSequential`, `renameFromList`, `createMenuList` |
| gameObjectPaths | array | ターゲットGameObjectパス配列 |
| center | object | 円形配置の中心 |
| radius | number | 円形配置の半径 |
| plane | string | 配置平面 `XY`, `XZ`, `YZ` |
| spacing | number | オブジェクト間隔 |
| baseName | string | 連番リネーム用ベース名 |

---

### `unity_rectTransform_batch`
UI RectTransformバッチ操作。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `setAnchors`, `setPivot`, `setSizeDelta`, `setAnchoredPosition`, `alignToParent`, `distributeHorizontal`, `distributeVertical`, `matchSize` |
| gameObjectPaths | array | ターゲットUIパス配列 |
| preset | string | アンカープリセット（16種類） |
| anchorMin/Max | object | アンカー座標 |
| spacing | number | 間隔 |

**プリセット:** `topLeft`, `topCenter`, `topRight`, `middleLeft`, `middleCenter`, `middleRight`, `bottomLeft`, `bottomCenter`, `bottomRight`, `stretchLeft`, `stretchCenter`, `stretchRight`, `stretchTop`, `stretchMiddle`, `stretchBottom`, `stretchAll`

---

### `unity_camera_rig`
カメラリグ作成。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `createRig`, `updateRig`, `inspect` |
| rigType | string | `follow`, `orbit`, `splitScreen`, `fixed`, `dolly` |
| targetPath | string | 追跡対象 |
| offset | object | オフセット位置 |
| fieldOfView | number | 視野角 |
| orthographic | boolean | 正投影モード |

---

## 4. UI Foundation (UI基盤)

### `unity_ui_foundation`
UGUI基盤要素作成。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `createCanvas`, `createPanel`, `createButton`, `createText`, `createImage`, `createInputField`, `createScrollView`, `addLayoutGroup`, `updateLayoutGroup`, `removeLayoutGroup`, `createFromTemplate`, `inspect` |
| parentPath | string | 親GameObjectパス |
| name | string | UI要素名 |
| text | string | テキスト内容 |
| fontSize | integer | フォントサイズ |
| color | object | 色 {r, g, b, a} |
| anchorPreset | string | アンカープリセット |
| layoutType | string | `Horizontal`, `Vertical`, `Grid` |
| templateType | string | `dialog`, `hud`, `menu`, `statusBar`, `inventoryGrid` |

---

### `unity_ui_hierarchy`
宣言的UIヒエラルキー管理。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `create`, `clone`, `inspect`, `delete`, `show`, `hide`, `toggle` |
| parentPath | string | 親パス |
| hierarchy | object | 宣言的UIヒエラルキー定義 |
| hierarchyId | string | ヒエラルキー識別子 |

**ヒエラルキー要素タイプ:** `panel`, `button`, `text`, `image`, `inputfield`, `scrollview`, `toggle`, `slider`, `dropdown`

---

### `unity_ui_state`
UI状態管理。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `defineState`, `applyState`, `saveState`, `loadState`, `listStates`, `deleteState`, `createStateGroup`, `transitionTo`, `getActiveState` |
| stateName | string | 状態名 |
| rootPath | string | ルートGameObjectパス |
| elements | array | 要素状態定義 |

---

### `unity_ui_navigation`
UIナビゲーション管理（キーボード/ゲームパッド対応）。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `configure`, `setExplicit`, `autoSetup`, `createGroup`, `setFirstSelected`, `inspect`, `reset`, `disable` |
| direction | string | `vertical`, `horizontal`, `grid`, `both` |
| wrapAround | boolean | ループナビゲーション |
| up/down/left/right | string | 明示的ナビゲーション先 |

---

### `unity_vector_sprite_convert`
ベクター/プリミティブからスプライト変換。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `primitiveToSprite`, `svgToSprite`, `textureToSprite`, `createColorSprite` |
| primitiveType | string | `square`, `circle`, `triangle`, `polygon` |
| width/height | integer | サイズ（ピクセル） |
| color | object | RGBA色 {r, g, b, a} |
| outputPath | string | 出力パス |

---

## 5. 2D Graphics & Animation (2Dグラフィックス・アニメーション)

### `unity_sprite2d_bundle`
2Dスプライト管理。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `createSprite`, `updateSprite`, `inspect`, `updateMultiple`, `setSortingLayer`, `setColor`, `sliceSpriteSheet`, `createSpriteAtlas` |
| spritePath | string | スプライトアセットパス |
| sortingLayerName | string | ソーティングレイヤー名 |
| sortingOrder | integer | ソート順 |
| flipX/flipY | boolean | 反転 |
| drawMode | string | `simple`, `sliced`, `tiled` |

---

### `unity_animation2d_bundle`
2Dアニメーションセットアップ。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `setupAnimator`, `updateAnimator`, `inspectAnimator`, `createController`, `addState`, `addTransition`, `addParameter`, `inspectController`, `createClipFromSprites`, `updateClip`, `inspectClip` |
| controllerPath | string | AnimatorControllerアセットパス |
| stateName | string | ステート名 |
| fromState/toState | string | 遷移元/先 |
| conditions | array | 遷移条件 |
| spritePaths | array | スプライトパス配列（クリップ作成用） |
| frameRate | number | フレームレート |

---

### `unity_tilemap_bundle`
タイルマップ管理。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `createTilemap`, `inspect`, `setTile`, `getTile`, `setTiles`, `clearTile`, `clearTiles`, `clearAllTiles`, `fillArea`, `boxFill`, `worldToCell`, `cellToWorld`, `updateRenderer`, `updateCollider`, `addCollider`, `createTile`, `createRuleTile`, `inspectTile`, `updateTile` |
| tilemapPath | string | タイルマップGameObjectパス |
| cellLayout | string | `Rectangle`, `Hexagon`, `Isometric`, `IsometricZAsY` |
| position | object | タイル位置 {x, y, z} |
| tileAssetPath | string | タイルアセットパス |

---

## 6. Visual & 3D (ビジュアル・3D)

### `unity_material_bundle`
マテリアル管理。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `create`, `update`, `inspect`, `delete`, `setTexture`, `setColor`, `applyPreset`, `duplicate`, `findByShader`, `findByName` |
| savePath | string | 新規マテリアルの保存パス |
| materialPath | string | 既存マテリアルのパス |
| preset | string | プリセット |
| shader | string | シェーダー名 |
| color | object | 色 {r, g, b, a} |
| metallic | number | メタリック度 |
| smoothness | number | スムーズネス |

**プリセット:** `standard`, `metallic`, `glass`, `emissive`, `unlit`, `transparent`, `cutout`, `urpLit`, `urpUnlit`, `hdrpLit`

---

### `unity_light_bundle`
ライトセットアップ。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `create`, `update`, `inspect`, `delete`, `applyPreset`, `createLightingSetup`, `bakeReflectionProbe` |
| lightType | string | `Directional`, `Point`, `Spot`, `Area` |
| preset | string | ライトプリセット |
| setupPreset | string | ライティングセットアッププリセット |
| color | object | 色 {r, g, b, a} |
| intensity | number | 強度 |
| range | number | 範囲 |

**セットアッププリセット:** `daylight`, `sunset`, `night`, `indoor`, `studio`, `dramatic`

---

### `unity_particle_bundle`
パーティクルシステム管理。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `create`, `update`, `inspect`, `delete`, `play`, `stop`, `applyPreset`, `setEmission`, `setShape`, `setColor` |
| gameObjectPath | string | ターゲットパス |
| preset | string | パーティクルプリセット |
| startSize/startLifetime/startSpeed | number | 初期パラメータ |
| emissionRate | number | 放出レート |
| maxParticles | integer | 最大粒子数 |

**プリセット:** `fire`, `smoke`, `sparks`, `rain`, `snow`, `dust`, `explosion`, `magic`, `heal`, `trail`, `waterfall`, `confetti`

---

### `unity_animation3d_bundle`
3Dアニメーション管理（BlendTree、AvatarMask対応）。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `setupAnimator`, `updateAnimator`, `inspectAnimator`, `createController`, `addState`, `addTransition`, `addParameter`, `inspectController`, `addBlendTree`, `addAvatarMask`, `updateAvatarMask` |
| controllerPath | string | AnimatorControllerアセットパス |
| blendTreeName | string | BlendTree名 |
| blendType | string | `Simple1D`, `SimpleDirectional2D`, `FreeformDirectional2D`, `FreeformCartesian2D` |
| blendParameter | string | ブレンドパラメータ名 |

---

### `unity_event_wiring`
UnityEventワイヤリング（Button.onClick等）。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `wire`, `unwire`, `inspect`, `listEvents`, `clearEvent`, `wireMultiple` |
| source | object | ソース {gameObject, component, event} |
| target | object | ターゲット {gameObject, component, method, mode, argument} |
| wirings | array | 複数ワイヤリング定義（wireMultiple用） |

---

## 7. UI Toolkit (モダンUI)

### `unity_uitk_document`
UI Toolkit UIDocument管理。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `create`, `inspect`, `update`, `delete`, `query` |
| name | string | GameObject名 |
| sourceAsset | string | UXMLアセットパス |
| panelSettings | string | PanelSettingsパス |
| sortingOrder | number | ソート順 |
| queryName/queryClass/queryType | string | VisualElement検索 |

---

### `unity_uitk_asset`
UI Toolkitアセット作成（UXML、USS、PanelSettings）。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `createUXML`, `createUSS`, `inspectUXML`, `inspectUSS`, `updateUXML`, `updateUSS`, `createPanelSettings`, `createFromTemplate`, `validateDependencies` |
| assetPath | string | アセットパス |
| elements | array | UXML要素定義（再帰構造） |
| rules | array | USSルール定義 |
| templateName | string | `menu`, `dialog`, `hud`, `settings`, `inventory` |
| outputDir | string | テンプレート出力ディレクトリ |

---

## 8. GameKit - UIピラー (5ツール)

コード生成によりスタンドアロンC#スクリプトを生成。`create`後は`unity_compilation_await`が必要。

### `unity_gamekit_ui_command`
UIコマンドパネル作成（UXML/USS + C#）。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `createCommandPanel`, `addCommand`, `inspect`, `delete` |
| panelId | string | パネル識別子 |
| layout | string | `horizontal`, `vertical`, `grid` |
| commands | array | コマンド定義配列 |
| uiOutputDir | string | UI出力ディレクトリ |

**コマンドタイプ:** `move`, `jump`, `action`, `look`, `custom`, `addResource`, `setResource`, `consumeResource`, `changeState`, `nextTurn`, `triggerScene`

---

### `unity_gamekit_ui_binding`
宣言的UIデータバインディング。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `create`, `update`, `inspect`, `delete`, `setRange`, `refresh`, `findByBindingId` |
| bindingId | string | バインディング識別子 |
| sourceType | string | `health`, `economy`, `timer`, `custom` |
| sourceId | string | データソースID |
| format | string | `raw`, `percent`, `formatted`, `ratio` |

---

### `unity_gamekit_ui_list`
動的ScrollViewリスト/グリッド。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `create`, `inspect`, `delete`, `addItem`, `removeItem`, `updateItem`, `clearItems`, `getItem`, `getItems`, `setItems`, `selectItem`, `findByListId` |
| listId | string | リスト識別子 |
| layout | string | `vertical`, `horizontal`, `grid` |
| dataSource | string | `custom`, `inventory`, `equipment` |
| selectable | boolean | 選択可能 |

---

### `unity_gamekit_ui_slot`
装備/クイックスロットUI。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `create`, `inspect`, `delete`, `setItem`, `clearItem`, `getItem`, `highlight`, `createBar`, `inspectBar`, `deleteBar`, `setBarItem`, `clearBarItem`, `getBarItem`, `findBySlotId` |
| slotId/barId | string | スロット/バー識別子 |
| slotType | string | `storage`, `equipment`, `quickslot`, `trash` |
| layout | string | `horizontal`, `vertical`, `grid` |

---

### `unity_gamekit_ui_selection`
ラジオ/トグル/チェックボックス/タブグループ。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `create`, `inspect`, `delete`, `addItem`, `removeItem`, `selectItem`, `deselectItem`, `getSelected`, `setEnabled`, `clearSelection`, `setActions`, `getItems`, `updateItem`, `findBySelectionId` |
| selectionId | string | 選択グループ識別子 |
| selectionType | string | `radio`, `toggle`, `checkbox`, `tab` |
| items | array | 選択アイテム定義 |

---

## 9. GameKit - Presentationピラー (5ツール)

### `unity_gamekit_animation_sync`
宣言的アニメーション同期。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `create`, `update`, `inspect`, `delete`, `addSyncRule`, `removeSyncRule`, `addTriggerRule`, `removeTriggerRule`, `fireTrigger`, `setParameter`, `findBySyncId` |
| syncId | string | 同期識別子 |
| syncRules | array | 同期ルール配列 |
| triggers | array | トリガールール配列 |

**ソースタイプ:** `rigidbody3d`, `rigidbody2d`, `transform`, `health`, `custom`

---

### `unity_gamekit_effect`
複合エフェクトシステム。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `create`, `update`, `inspect`, `delete`, `addComponent`, `removeComponent`, `clearComponents`, `play`, `playAtPosition`, `playAtTransform`, `shakeCamera`, `flashScreen`, `setTimeScale`, `createManager`, `registerEffect`, `unregisterEffect` |
| effectId | string | エフェクト識別子 |
| components | array | エフェクトコンポーネント配列 |

**コンポーネントタイプ:** `particle`, `sound`, `cameraShake`, `screenFlash`, `timeScale`

---

### `unity_gamekit_feedback`
ゲームフィール（ヒットストップ、画面振動等）。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `create`, `update`, `inspect`, `delete`, `addComponent`, `clearComponents`, `setIntensity`, `findByFeedbackId` |
| feedbackId | string | フィードバック識別子 |
| components | array | フィードバックコンポーネント配列 |
| globalIntensityMultiplier | number | グローバル強度倍率 |

**コンポーネントタイプ:** `hitstop`, `screenShake`, `colorFlash`, `scaleEffect`, `knockback`, `particleEffect`, `soundEffect`, `slowMotion`, `chromaticAberration`, `vignette`

---

### `unity_gamekit_vfx`
ParticleSystemラッパー（プーリング対応）。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `create`, `update`, `inspect`, `delete`, `setMultipliers`, `setColor`, `setLoop`, `findByVFXId` |
| vfxId | string | VFX識別子 |
| particlePrefabPath | string | パーティクルプレハブパス |
| usePooling | boolean | オブジェクトプーリング使用 |
| poolSize | integer | プールサイズ |

---

### `unity_gamekit_audio`
サウンド管理（SFX、BGM、環境音）。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `create`, `update`, `inspect`, `delete`, `setVolume`, `setPitch`, `setLoop`, `setClip`, `findByAudioId` |
| audioId | string | オーディオ識別子 |
| audioType | string | `sfx`, `music`, `ambient`, `voice`, `ui` |
| audioClipPath | string | AudioClipパス |
| volume | number | 音量 |
| fadeInDuration/fadeOutDuration | number | フェード時間 |

---

## 10. GameKit - Logicピラー (7ツール)

### `unity_validate_integrity`
シーン整合性検証。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `missingScripts`, `nullReferences`, `brokenEvents`, `brokenPrefabs`, `all` |
| rootPath | string | 検査対象のルートパス（省略時はシーン全体） |

---

### `unity_class_catalog`
型列挙・検査。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `listTypes`, `inspectType` |
| typeKind | string | `class`, `struct`, `interface`, `enum`, `MonoBehaviour`, `ScriptableObject` |
| className | string | 検査対象の型名（inspectType用） |
| namePattern | string | ワイルドカードパターン |
| maxResults | integer | 最大結果数（デフォルト: 100） |

---

### `unity_class_dependency_graph`
C#クラス依存関係分析。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `analyzeClass`, `analyzeAssembly`, `analyzeNamespace`, `findDependents`, `findDependencies` |
| target | string | 分析対象（クラス名/アセンブリ名/名前空間） |
| depth | integer | 分析深度（デフォルト: 1） |
| format | string | `json`, `dot`, `mermaid`, `summary` |

---

### `unity_scene_reference_graph`
シーンオブジェクト参照分析。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `analyzeScene`, `analyzeObject`, `findReferencesTo`, `findReferencesFrom`, `findOrphans` |
| objectPath | string | 対象GameObjectパス |
| includeHierarchy | boolean | 親子関係を含める |
| includeEvents | boolean | UnityEventを含める |
| format | string | `json`, `dot`, `mermaid`, `summary` |

---

### `unity_scene_relationship_graph`
シーン遷移・関係性分析。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `analyzeAll`, `analyzeScene`, `findTransitionsTo`, `findTransitionsFrom`, `validateBuildSettings` |
| scenePath | string | シーンパス |
| format | string | `json`, `dot`, `mermaid`, `summary` |

---

### `unity_scene_dependency`
シーンアセット依存関係分析（AssetDatabase経由）。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `analyzeScene`, `findAssetUsage`, `findSharedAssets`, `findUnusedAssets` |
| scenePath | string | 分析対象シーンパス（analyzeScene用） |
| assetPath | string | 検索対象アセットパス（findAssetUsage用） |
| includeIndirect | boolean | 間接依存を含めるか（デフォルト: true） |
| typeFilter | string | アセットカテゴリでフィルタ（Material, Texture, Script等） |
| searchPath | string | 検索範囲をフォルダに限定 |
| scenePaths | array | findSharedAssets用のシーンパス配列（デフォルト: 全シーン） |
| minSharedCount | integer | findSharedAssets用の最小共有数（デフォルト: 2） |

**アセットカテゴリ:** Material, Texture, Shader, Model, Audio, AnimationClip, AnimatorController, Prefab, Script, Font, Asset, UXML, USS, Video, Data, Other

---

### `unity_script_syntax`
C#ソースコード構文解析（行番号付き）。リフレクションではなくソースコードを直接解析。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `analyzeScript`, `findReferences`, `findUnusedCode`, `analyzeMetrics` |
| scriptPath | string | 解析対象の.csファイルパス（analyzeScript用） |
| symbolName | string | 検索対象のシンボル名（findReferences用） |
| symbolType | string | シンボル種別: `class`, `method`, `field`, `property` |
| searchPath | string | 検索範囲をフォルダに限定 |
| targetType | string | findUnusedCode用: `method`, `field` |

**参照種別:** method_call, instantiation, type_usage, inheritance, typeof, generic_argument, static_access, member_access

**既存ツールとの違い:**
- `unity_class_dependency_graph`: コンパイル済み型をリフレクションで解析（行番号なし）
- `unity_class_catalog`: コンパイル済み型メタデータをリフレクションで検査
- `unity_script_syntax`: ソースコードを直接解析（行番号あり、参照検索、メトリクス計算）

---

## 11. Utilityツール（追加）

### `unity_playmode_control`
プレイモード制御。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `play`, `pause`, `unpause`, `stop`, `step`, `getState` |

---

### `unity_console_log`
コンソールログ取得。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `getRecent`, `getErrors`, `getWarnings`, `getLogs`, `clear`, `getCompilationErrors`, `getSummary` |
| count | integer | 取得件数 |

---

## その他のツール

### `unity_projectSettings_crud`
プロジェクト設定管理。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `read`, `write`, `list`, `addSceneToBuild`, `removeSceneFromBuild`, `listBuildScenes`, `reorderBuildScenes`, `setBuildSceneEnabled` |
| category | string | 設定カテゴリ |
| property | string | プロパティ名 |
| value | any | 設定値 |
| scenePath | string | ビルドシーンパス |

**カテゴリ:** `player`, `quality`, `time`, `physics`, `physics2d`, `audio`, `editor`, `tagsLayers`

---

### `unity_input_profile`
Input Systemセットアップ。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `createPlayerInput`, `createInputActions`, `inspect` |
| preset | string | `player`, `ui`, `vehicle`, `custom` |
| inputActionsAssetPath | string | InputActionsアセットパス |
| actions | array | カスタムアクション定義 |

---

## バッチ実行ツール

### `unity_batch_sequential_execute`
複数コマンドの順次実行（レジューム機能付き）。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operations | array | 実行するコマンド配列 [{tool, arguments}] |
| stop_on_error | boolean | エラー時に停止 |

---

## 使用例

### 2Dプラットフォーマーキャラクター作成

```python
# プレイヤーGameObject作成
unity_gameobject_crud({
    "operation": "create",
    "name": "Player",
    "template": "Sphere"
})

# 物理設定適用（component_crudで直接設定）
unity_component_crud({
    "operation": "add",
    "gameObjectPath": "Player",
    "componentType": "Rigidbody2D",
    "propertyChanges": {"gravityScale": 3, "mass": 1, "constraints": {"freezeRotationZ": True}}
})
unity_component_crud({
    "operation": "add",
    "gameObjectPath": "Player",
    "componentType": "BoxCollider2D",
    "propertyChanges": {"size": {"x": 1, "y": 1}}
})

# UIコマンドパネルで操作ボタン作成
unity_gamekit_ui_command({
    "operation": "createCommandPanel",
    "panelId": "playerControls",
    "layout": "horizontal",
    "commands": [
        {"name": "moveLeft", "label": "Left", "commandType": "move",
         "moveDirection": {"x": -1, "y": 0, "z": 0}},
        {"name": "jump", "label": "Jump", "commandType": "jump"},
        {"name": "moveRight", "label": "Right", "commandType": "move",
         "moveDirection": {"x": 1, "y": 0, "z": 0}}
    ]
})
unity_compilation_await({"operation": "await"})
```

### UIメニュー作成

```python
# Canvas作成
unity_ui_foundation({
    "operation": "createCanvas",
    "name": "MainCanvas",
    "renderMode": "screenSpaceOverlay"
})

# 宣言的UIヒエラルキー作成
unity_ui_hierarchy({
    "operation": "create",
    "parentPath": "MainCanvas",
    "hierarchy": {
        "type": "panel",
        "name": "MainMenu",
        "children": [
            {"type": "text", "name": "Title", "text": "My Game", "fontSize": 48},
            {"type": "button", "name": "StartBtn", "text": "Start"},
            {"type": "button", "name": "QuitBtn", "text": "Quit"}
        ],
        "layout": "Vertical",
        "spacing": 20
    }
})

# キーボードナビゲーション設定
unity_ui_navigation({
    "operation": "autoSetup",
    "rootPath": "MainCanvas/MainMenu",
    "direction": "vertical",
    "wrapAround": true
})
```

### エフェクトとフィードバック

```python
# 爆発エフェクト作成
unity_gamekit_effect({
    "operation": "create",
    "effectId": "explosion",
    "components": [
        {"type": "particle", "prefabPath": "Assets/Prefabs/ExplosionVFX.prefab", "duration": 1.0},
        {"type": "sound", "clipPath": "Assets/Audio/explosion.wav", "volume": 0.8},
        {"type": "cameraShake", "intensity": 0.5, "shakeDuration": 0.3}
    ]
})
unity_compilation_await({"operation": "await"})

# ヒットフィードバック作成
unity_gamekit_feedback({
    "operation": "create",
    "feedbackId": "onHit",
    "components": [
        {"type": "hitstop", "duration": 0.05, "hitstopTimeScale": 0},
        {"type": "screenShake", "duration": 0.2, "intensity": 0.3},
        {"type": "colorFlash", "color": {"r": 1, "g": 0, "b": 0, "a": 0.5}, "flashDuration": 0.1}
    ]
})
unity_compilation_await({"operation": "await"})

# シーン整合性検証
unity_validate_integrity({"operation": "all"})
```
