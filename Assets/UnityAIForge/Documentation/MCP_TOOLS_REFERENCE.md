# Unity-AI-Forge MCP Tools Reference

このドキュメントはUnity-AI-ForgeのMCPツール一覧をまとめたものです。

## ツール概要

| カテゴリ | ツール数 | 説明 |
|----------|----------|------|
| Core | 4 | 基本的なUnity操作 |
| Asset Management | 4 | アセット・プレハブ・ScriptableObject管理 |
| Transform & Physics | 4 | 変形・物理設定 |
| UI Foundation | 5 | UI要素の作成・管理 |
| 2D Graphics & Animation | 3 | スプライト・アニメーション |
| GameKit Core | 6 | 高レベルゲーム開発フレームワーク |
| GameKit Gameplay | 7 | ゲームプレイ機能 |
| GameKit Advanced | 4 | エフェクト・セーブ・インベントリ |

**合計: 37 ツール**

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

### `unity_physics_bundle`
物理セットアップ。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `applyPreset2D`, `applyPreset3D`, `updateRigidbody2D`, `updateRigidbody3D`, `updateCollider2D`, `updateCollider3D`, `inspect` |
| gameObjectPaths | array | ターゲットパス配列 |
| preset | string | プリセット |
| colliderType | string | コライダー種類 |
| mass/drag/angularDrag | number | 物理パラメータ |
| constraints | object | 制約設定 |

**プリセット:** `dynamic`, `kinematic`, `static`, `character`, `platformer`, `topDown`, `vehicle`, `projectile`

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
| operation | string | `create`, `clone`, `inspect`, `delete`, `show`, `hide`, `toggle`, `setNavigation` |
| parentPath | string | 親パス |
| hierarchy | object | 宣言的UIヒエラルキー定義 |
| navigationMode | string | `none`, `auto-vertical`, `auto-horizontal`, `explicit` |

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

## 6. GameKit Core (ゲームキット基盤)

### `unity_gamekit_actor`
高レベルゲームアクター作成。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `create`, `update`, `inspect`, `delete` |
| actorId | string | アクター識別子 |
| behaviorProfile | string | 移動プロファイル |
| controlMode | string | 制御モード |
| spritePath/modelPath | string | ビジュアルアセット |

**behaviorProfile:** `2dLinear`, `2dPhysics`, `2dTileGrid`, `graphNode`, `splineMovement`, `3dCharacterController`, `3dPhysics`, `3dNavMesh`

**controlMode:** `directController`, `aiAutonomous`, `uiCommand`, `scriptTriggerOnly`

---

### `unity_gamekit_manager`
ゲームシステムマネージャー作成。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `create`, `update`, `inspect`, `delete`, `exportState`, `importState`, `setFlowEnabled` |
| managerId | string | マネージャー識別子 |
| managerType | string | マネージャー種類 |
| persistent | boolean | シーン跨ぎ保持 |
| turnPhases | array | ターンフェーズ名 |
| initialResources | object | 初期リソース量 |

**managerType:** `turnBased`, `realtime`, `resourcePool`, `eventHub`, `stateManager`

---

### `unity_gamekit_interaction`
トリガーベースインタラクション作成。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `create`, `update`, `inspect`, `delete` |
| interactionId | string | インタラクション識別子 |
| triggerType | string | `collision`, `trigger`, `raycast`, `proximity`, `input` |
| triggerShape | string | コライダー形状 |
| is2D | boolean | 2Dコライダー使用 |
| actions | array | アクション配列 |
| conditions | array | 条件配列 |

**アクションタイプ:** `spawnPrefab`, `destroyObject`, `playSound`, `sendMessage`, `changeScene`

---

### `unity_gamekit_ui_command`
UIコマンドパネル作成。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `createCommandPanel`, `addCommand`, `inspect`, `delete` |
| panelId | string | パネル識別子 |
| targetType | string | `actor`, `manager` |
| targetActorId/targetManagerId | string | ターゲットID |
| commands | array | コマンド定義配列 |
| layout | string | `horizontal`, `vertical`, `grid` |

**コマンドタイプ:** `move`, `jump`, `action`, `look`, `custom`, `addResource`, `setResource`, `consumeResource`, `changeState`, `nextTurn`, `triggerScene`

---

### `unity_gamekit_machinations`
Machinations経済システムダイアグラム管理。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `create`, `update`, `inspect`, `delete`, `apply`, `export` |
| diagramId | string | ダイアグラム識別子 |
| assetPath | string | アセットパス |
| initialResources | array | リソースプール定義 |
| flows | array | リソースフロー定義 |
| converters | array | リソース変換器定義 |
| triggers | array | 閾値トリガー定義 |

---

### `unity_gamekit_sceneflow`
シーン遷移管理。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `create`, `inspect`, `delete`, `transition`, `addScene`, `removeScene`, `updateScene`, `addTransition`, `removeTransition`, `addSharedScene`, `removeSharedScene` |
| flowId | string | フロー識別子 |
| sceneName | string | シーン名 |
| scenePath | string | シーンアセットパス |
| loadMode | string | `single`, `additive` |
| fromScene/toScene | string | 遷移元/先シーン |
| trigger | string | トリガー名 |

---

## 7. GameKit Gameplay (ゲームプレイ機能)

### `unity_gamekit_health`
ヘルス/ダメージシステム。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `create`, `update`, `inspect`, `delete`, `applyDamage`, `heal`, `kill`, `respawn`, `setInvincible`, `findByHealthId` |
| healthId | string | ヘルス識別子 |
| maxHealth | number | 最大HP（デフォルト: 100） |
| invincibilityDuration | number | ダメージ後無敵時間 |
| onDeath | string | `destroy`, `disable`, `respawn`, `event` |
| respawnDelay | number | リスポーン遅延 |

---

### `unity_gamekit_spawner`
スポナーシステム。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `create`, `update`, `inspect`, `delete`, `start`, `stop`, `reset`, `spawnOne`, `spawnBurst`, `despawnAll`, `addSpawnPoint`, `addWave`, `findBySpawnerId` |
| spawnerId | string | スポナー識別子 |
| prefabPath | string | スポーンするプレハブ |
| spawnMode | string | `interval`, `wave`, `burst`, `manual` |
| spawnInterval | number | スポーン間隔 |
| maxActive | integer | 最大同時存在数 |
| waves | array | ウェーブ設定 |

---

### `unity_gamekit_timer`
タイマー/クールダウンシステム。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `createTimer`, `updateTimer`, `inspectTimer`, `deleteTimer`, `createCooldown`, `updateCooldown`, `inspectCooldown`, `deleteCooldown`, `createCooldownManager`, `addCooldownToManager`, `inspectCooldownManager`, `findByTimerId`, `findByCooldownId` |
| timerId/cooldownId | string | 識別子 |
| duration | number | 継続時間 |
| loop | boolean | ループ |
| unscaledTime | boolean | TimeScale無視 |

---

### `unity_gamekit_ai`
AIビヘイビアシステム。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `create`, `update`, `inspect`, `delete`, `setTarget`, `clearTarget`, `setState`, `addPatrolPoint`, `clearPatrolPoints`, `findByAIId` |
| aiId | string | AI識別子 |
| behaviorType | string | `patrol`, `chase`, `flee`, `patrolAndChase` |
| moveSpeed | number | 移動速度 |
| detectionRadius | number | 検知範囲 |
| fieldOfView | number | 視野角（度） |
| patrolMode | string | `loop`, `pingPong`, `random` |

---

### `unity_gamekit_collectible`
収集アイテムシステム。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `create`, `update`, `inspect`, `delete`, `collect`, `respawn`, `reset`, `findByCollectibleId` |
| collectibleId | string | 収集アイテム識別子 |
| collectibleType | string | `coin`, `health`, `mana`, `powerup`, `key`, `ammo`, `experience`, `custom` |
| value | number | 値 |
| collectionBehavior | string | `destroy`, `disable`, `respawn` |
| enableFloatAnimation | boolean | 浮遊アニメーション |
| enableRotation | boolean | 回転アニメーション |

---

### `unity_gamekit_projectile`
発射物システム。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `create`, `update`, `inspect`, `delete`, `launch`, `setHomingTarget`, `destroy`, `findByProjectileId` |
| projectileId | string | 発射物識別子 |
| movementType | string | `transform`, `rigidbody`, `rigidbody2d` |
| speed | number | 速度 |
| damage | number | ダメージ |
| lifetime | number | 寿命 |
| isHoming | boolean | ホーミング |
| canBounce | boolean | バウンス |
| canPierce | boolean | 貫通 |

---

### `unity_gamekit_waypoint`
ウェイポイント/パスフォロワー。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `create`, `update`, `inspect`, `delete`, `addWaypoint`, `removeWaypoint`, `clearWaypoints`, `startPath`, `stopPath`, `pausePath`, `resumePath`, `resetPath`, `goToWaypoint`, `findByWaypointId` |
| waypointId | string | ウェイポイント識別子 |
| pathMode | string | `once`, `loop`, `pingpong` |
| moveSpeed | number | 移動速度 |
| waitTimeAtPoint | number | 各ポイントでの待機時間 |
| waypointPositions | array | ウェイポイント位置配列 |

---

### `unity_gamekit_trigger_zone`
トリガーゾーンシステム。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `create`, `update`, `inspect`, `delete`, `activate`, `deactivate`, `reset`, `setTeleportDestination`, `findByZoneId` |
| zoneId | string | ゾーン識別子 |
| zoneType | string | ゾーンタイプ |
| triggerMode | string | `once`, `onceperentity`, `repeat`, `whileinside` |
| effectAmount | number | エフェクト量 |
| effectInterval | number | エフェクト間隔 |

**zoneType:** `generic`, `checkpoint`, `damagezone`, `healzone`, `teleport`, `speedboost`, `slowdown`, `killzone`, `safezone`, `trigger`

---

## 8. GameKit Advanced (高度な機能)

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
| operation | string | `create`, `update`, `inspect`, `delete`, `addComponent`, `removeComponent`, `clearComponents`, `play`, `playAtPosition`, `playAtTransform`, `shakeCamera`, `flashScreen`, `setTimeScale`, `createManager`, `registerEffect`, `unregisterEffect`, `findByEffectId`, `listEffects` |
| effectId | string | エフェクト識別子 |
| assetPath | string | エフェクトアセットパス |
| components | array | エフェクトコンポーネント配列 |

**コンポーネントタイプ:** `particle`, `sound`, `cameraShake`, `screenFlash`, `timeScale`

---

### `unity_gamekit_save`
セーブ/ロードシステム。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `createProfile`, `updateProfile`, `inspectProfile`, `deleteProfile`, `addTarget`, `removeTarget`, `clearTargets`, `save`, `load`, `listSlots`, `deleteSlot`, `createManager`, `inspectManager`, `deleteManager`, `findByProfileId` |
| profileId | string | プロファイル識別子 |
| slotId | string | セーブスロット識別子 |
| saveTargets | array | セーブ対象配列 |
| autoSave | object | オートセーブ設定 |

**セーブターゲットタイプ:** `transform`, `component`, `resourceManager`, `health`, `sceneFlow`, `inventory`, `playerPrefs`

---

### `unity_gamekit_inventory`
インベントリシステム。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `create`, `update`, `inspect`, `delete`, `defineItem`, `updateItem`, `inspectItem`, `deleteItem`, `addItem`, `removeItem`, `useItem`, `equip`, `unequip`, `getEquipped`, `clear`, `sort`, `findByInventoryId`, `findByItemId` |
| inventoryId | string | インベントリ識別子 |
| maxSlots | integer | 最大スロット数（デフォルト: 20） |
| itemId | string | アイテム識別子 |
| itemData | object | アイテムデータ定義 |
| equipSlot | string | 装備スロット |

**カテゴリ:** `weapon`, `armor`, `consumable`, `material`, `key`, `quest`, `misc`

**装備スロット:** `mainHand`, `offHand`, `head`, `body`, `hands`, `feet`, `accessory1`, `accessory2`

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

### `unity_character_controller_bundle`
CharacterControllerセットアップ。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `applyPreset`, `update`, `inspect` |
| preset | string | `fps`, `tps`, `platformer`, `child`, `large`, `narrow`, `custom` |
| radius/height | number | カプセルサイズ |
| slopeLimit | number | 最大傾斜角度 |
| stepOffset | number | 最大段差高さ |

---

### `unity_audio_source_bundle`
AudioSourceセットアップ。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| operation | string | `createAudioSource`, `updateAudioSource`, `inspect` |
| preset | string | `music`, `sfx`, `ambient`, `voice`, `ui`, `custom` |
| audioClipPath | string | オーディオクリップパス |
| volume | number | 音量 (0-1) |
| spatialBlend | number | 2D/3Dブレンド |

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

### `batch_sequential`
複数コマンドの順次実行（レジューム機能付き）。

| パラメータ | 型 | 説明 |
|------------|-----|------|
| commands | array | 実行するコマンド配列 |
| resumeFromIndex | integer | 再開インデックス |

---

## 使用例

### 2Dプラットフォーマーキャラクター作成

```python
# プレイヤーアクター作成
unity_gamekit_actor({
    "operation": "create",
    "actorId": "player",
    "behaviorProfile": "2dPhysics",
    "controlMode": "directController",
    "spritePath": "Assets/Sprites/Player.png"
})

# ヘルスコンポーネント追加
unity_gamekit_health({
    "operation": "create",
    "targetPath": "player",
    "healthId": "player_health",
    "maxHealth": 100,
    "invincibilityDuration": 1.0
})

# 物理設定適用
unity_physics_bundle({
    "operation": "applyPreset2D",
    "gameObjectPaths": ["player"],
    "preset": "platformer"
})
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
    "wrapAround": True
})
```

### 敵AIとスポナー

```python
# 敵プレハブにAI追加
unity_gamekit_ai({
    "operation": "create",
    "targetPath": "EnemyPrefab",
    "aiId": "enemy_ai",
    "behaviorType": "patrolAndChase",
    "moveSpeed": 3.0,
    "detectionRadius": 8.0,
    "patrolMode": "pingPong"
})

# スポナー作成
unity_gamekit_spawner({
    "operation": "create",
    "targetPath": "EnemySpawner",
    "spawnerId": "enemy_spawner",
    "prefabPath": "Assets/Prefabs/Enemy.prefab",
    "spawnMode": "wave",
    "waves": [
        {"count": 3, "spawnInterval": 1.0},
        {"count": 5, "spawnInterval": 0.8},
        {"count": 8, "spawnInterval": 0.5}
    ]
})
```
