# プロトタイピング ワークフローガイド (Unity-AI-Forge v{VERSION})

プリミティブとプリセットを活用した高速プロトタイプ構築。アイデアを最短時間で動く形にし、検証サイクルを回すための実践ガイドです。

---

## 概要

プロトタイピングフェーズの目的は「動くかどうか」を素早く確認することです。アセットの品質や最終的なコード構造は後回しにし、まずゲームループが成立するか検証します。Unity-AI-Forgeのプリセット機能と即時プレイモード制御を活用して、アイデアから検証のサイクルを高速化します。

**プロトタイピングの原則:**
- 完璧を求めない。まず動かす
- プリミティブ（Cube/Sphere/Capsule）をキャラクター・障害物・ゴールとして使う
- プリセットで物理・カメラを一瞬でセットアップ
- UIは最小限（デバッグ表示程度）で十分
- Prefab化は「同じものを複数配置するとき」だけ
- playmode_control + console_log で素早くフィードバックを得る

---

## ワークフロー概要

```
シーン作成 → プリミティブ配置 → 物理設定 → カメラセットアップ
→ 最小スクリプト添付 → プレイテスト → ログ確認 → 繰り返し
```

---

## 推奨手順

1. **シーン作成** - 専用プロトタイプシーンを用意する
2. **地面・壁配置** - Cubeプリミティブで環境を構築
3. **プレイヤー配置** - Capsuleをプレイヤーとして使用
4. **物理プリセット適用** - characterプリセットで即座に動ける状態に
5. **カメラセットアップ** - followリグで自動追従
6. **入力設定** - New Input Systemのアクションマップを定義
7. **最小UIの追加** - スコア・HP表示程度
8. **プレイテスト** - play → 確認 → stop → 修正を繰り返す
9. **整合性チェック** - プロトタイプが固まったら validate_integrity で確認
10. **Prefab化** - 繰り返し使うものだけPrefabに昇格

---

## 実装ワークフロー (MCPツール使用例付き)

### Step 1: プロトタイプシーンの作成

```python
# 専用シーンを作成
unity_scene_crud(operation='create', scenePath='Assets/Scenes/Prototype.unity')

# Build Settings に追加（別のツールで実行）
unity_projectSettings_crud(operation='addSceneToBuild',
    scenePath='Assets/Scenes/Prototype.unity')

# シーンをロード（Single モード）
unity_scene_crud(operation='load', scenePath='Assets/Scenes/Prototype.unity',
    additive=False)

# 現状確認
unity_scene_crud(operation='inspect', includeHierarchy=True)
```

### Step 2: 環境をプリミティブで高速構築

```python
# 地面 (Cube を平たく配置)
unity_gameobject_crud(operation='create', name='Ground', template='Cube')
# Transform はコンポーネント操作で設定
unity_component_crud(operation='update', gameObjectPath='Ground',
    componentType='Transform',
    propertyChanges={'localPosition': {'x': 0, 'y': -0.5, 'z': 0},
                     'localScale': {'x': 20, 'y': 1, 'z': 20}})

# 壁 (左)
unity_gameobject_crud(operation='create', name='WallLeft', template='Cube')
unity_component_crud(operation='update', gameObjectPath='WallLeft',
    componentType='Transform',
    propertyChanges={'localPosition': {'x': -10, 'y': 2, 'z': 0},
                     'localScale': {'x': 1, 'y': 5, 'z': 20}})

# 壁 (右)
unity_gameobject_crud(operation='create', name='WallRight', template='Cube')
unity_component_crud(operation='update', gameObjectPath='WallRight',
    componentType='Transform',
    propertyChanges={'localPosition': {'x': 10, 'y': 2, 'z': 0},
                     'localScale': {'x': 1, 'y': 5, 'z': 20}})

# プレイヤー (Capsule)
unity_gameobject_crud(operation='create', name='Player', template='Capsule')
unity_component_crud(operation='update', gameObjectPath='Player',
    componentType='Transform',
    propertyChanges={'localPosition': {'x': 0, 'y': 1, 'z': 0}})

# 敵プレースホルダー (Sphere)
unity_gameobject_crud(operation='create', name='Enemy', template='Sphere')
unity_component_crud(operation='update', gameObjectPath='Enemy',
    componentType='Transform',
    propertyChanges={'localPosition': {'x': 5, 'y': 1, 'z': 5}})

# ゴール (Cube)
unity_gameobject_crud(operation='create', name='Goal', template='Cube')
unity_component_crud(operation='update', gameObjectPath='Goal',
    componentType='Transform',
    propertyChanges={'localPosition': {'x': 0, 'y': 0.5, 'z': 9},
                     'localScale': {'x': 2, 'y': 1, 'z': 2}})
```

### Step 3: 物理プリセットで即座にキャラクター動作を有効化

```python
# プレイヤーに characterプリセット (3D)
unity_physics_bundle(operation='applyPreset3D', gameObjectPaths=['Player'],
    preset='character')

# 敵はdynamicプリセット
unity_physics_bundle(operation='applyPreset3D', gameObjectPaths=['Enemy'],
    preset='dynamic')

# 地面・壁はstaticプリセット (Colliderのみ)
unity_physics_bundle(operation='applyPreset3D',
    gameObjectPaths=['Ground', 'WallLeft', 'WallRight'],
    preset='static')

# 2Dプロジェクトの場合は applyPreset2D を使用
unity_physics_bundle(operation='applyPreset2D', gameObjectPaths=['Player'],
    preset='character')
```

### Step 4: カメラをプレイヤー追従に設定

```python
# followリグを即座に設定
unity_camera_rig(operation='createRig', rigType='follow',
    rigName='MainCam',
    targetPath='Player',
    offset={'x': 0, 'y': 5, 'z': -10},
    smoothTime=0.3)

# 2D見下ろし型の場合
unity_camera_rig(operation='createRig', rigType='follow',
    rigName='MainCam',
    targetPath='Player',
    offset={'x': 0, 'y': 10, 'z': 0})

# orbit カメラ（TPSプロトタイプ向け）
unity_camera_rig(operation='createRig', rigType='orbit',
    rigName='MainCam',
    targetPath='Player',
    orbitDistance=8.0,
    orbitSpeed=90.0)
```

### Step 5: 入力プロファイルを素早く定義

```python
# Move + Jump + Attack の最小アクションマップ
unity_input_profile(operation='createInputActions',
    profileName='PlayerInput',
    actionMaps=[{
        'name': 'Player',
        'actions': [
            {'name': 'Move',   'type': 'Value',  'control': 'Vector2',
             'bindings': [{'path': '<Gamepad>/leftStick'}, {'path': '<Keyboard>/wasd'}]},
            {'name': 'Jump',   'type': 'Button',
             'bindings': [{'path': '<Keyboard>/space'}, {'path': '<Gamepad>/buttonSouth'}]},
            {'name': 'Attack', 'type': 'Button',
             'bindings': [{'path': '<Keyboard>/j'}, {'path': '<Gamepad>/buttonWest'}]}
        ]
    }])

# GameObjectにPlayerInputコンポーネントを添付
unity_input_profile(operation='createPlayerInput',
    gameObjectPath='Player')
```

### Step 6: プレイヤースクリプトをプロトタイプ用に生成

```python
# シンプルな移動スクリプトを生成
unity_asset_crud(operation='create', assetPath='Assets/Scripts/Proto/PlayerProto.cs',
    content='''using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerProto : MonoBehaviour
{
    public float speed = 5f;
    public float jumpForce = 7f;
    private Rigidbody rb;
    private Vector2 moveInput;

    void Start() => rb = GetComponent<Rigidbody>();
    void OnMove(InputValue v) => moveInput = v.Get<Vector2>();
    void OnJump() => rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + new Vector3(moveInput.x, 0, moveInput.y) * speed * Time.fixedDeltaTime);
    }
}''')

# コンパイル待機してからアタッチ
unity_compilation_await(operation='await', timeoutSeconds=30)
unity_component_crud(operation='add', gameObjectPath='Player',
    componentType='PlayerProto')
```

### Step 7: 最小限のデバッグUI

```python
# プロトタイプ用Canvas（デバッグ表示）
unity_ui_foundation(operation='createCanvas', name='DebugUI')
unity_ui_foundation(operation='createText', name='DebugText',
    parentPath='DebugUI',
    text='Debug: Running',
    fontSize=18)

# 宣言的UIで素早くHUDを構築
unity_ui_hierarchy(operation='create', parentPath='DebugUI',
    hierarchy={
        'type': 'panel', 'name': 'HUD',
        'children': [
            {'type': 'text', 'name': 'ScoreText', 'text': 'Score: 0', 'fontSize': 24},
            {'type': 'text', 'name': 'HPText',    'text': 'HP: 100',  'fontSize': 24}
        ],
        'layout': 'Vertical', 'spacing': 8
    })
```

### Step 8: プレイモードで素早くテスト

```python
# プレイ開始
unity_playmode_control(operation='play')

# ゲームプレイ後、エラー確認
unity_console_log(operation='getErrors')
unity_console_log(operation='getWarnings')

# サマリーで全体状況を把握
unity_console_log(operation='getSummary')

# 止める
unity_playmode_control(operation='stop')
```

### Step 9: Prefab化（繰り返し使うものだけ）

```python
# 敵をPrefabに昇格
unity_prefab_crud(operation='create',
    gameObjectPath='Enemy',
    prefabPath='Assets/Prefabs/Proto/EnemyProto.prefab')

# 複数配置
unity_prefab_crud(operation='instantiate',
    prefabPath='Assets/Prefabs/Proto/EnemyProto.prefab',
    parentPath='Enemies')

# 配置後にTransformを調整
unity_component_crud(operation='update', gameObjectPath='Enemies/EnemyProto',
    componentType='Transform',
    propertyChanges={'localPosition': {'x': 3, 'y': 1, 'z': 5}})
```

### Step 10: プロトタイプ検証

```python
# 整合性チェック（プロトタイプが一段落したら）
unity_validate_integrity(operation='all')

# Missing Scriptがあれば自動除去
unity_validate_integrity(operation='removeMissingScripts')

# シーン参照の確認（孤立オブジェクト検出）
unity_scene_reference_graph(operation='findOrphans')
```

---

## チェックリスト

### 環境構築
- [ ] プロトタイプ専用シーンを作成した
- [ ] projectSettings_crud(addSceneToBuild) でBuild Settingsに登録した
- [ ] 地面・壁をCube templateで配置した
- [ ] プレイヤーをCapsule templateで配置した

### 物理・カメラ
- [ ] プレイヤーに characterプリセットを適用した
- [ ] 地面・壁に staticプリセットを適用した
- [ ] カメラをfollowリグで追従設定した

### 入力・スクリプト
- [ ] input_profile(createInputActions) で最小限のアクションマップを定義した
- [ ] プレイヤー移動スクリプトを生成してアタッチした
- [ ] compilation_awaitで確実にコンパイル完了を待った

### プレイテスト
- [ ] playmode_controlでプレイ・停止を繰り返した
- [ ] console_log(getErrors) でエラーがないことを確認した
- [ ] ゲームループが成立することを確認した

### 整理
- [ ] 繰り返す要素をPrefabにした
- [ ] validate_integrity(all) で整合性を確認した

---

## 注意点・落とし穴

**gameobject_crud(create) に position/scale パラメータはない**
プリミティブ作成後は component_crud(update) で Transform を設定してください。template パラメータで 'Cube', 'Sphere', 'Capsule' 等を指定します。

**scene_crud に addToBuildSettings パラメータはない**
シーン作成後に projectSettings_crud(operation='addSceneToBuild') を別途実行してください。

**scene_crud のロードモードは additive パラメータで指定**
`additive=True` で加算ロード、`additive=False`（または省略）でSingleロードです。`loadMode` パラメータは存在しません。

**スクリプト生成後は必ず compilation_await を実行する**
コンパイル前に component_crud(add) しようとするとエラーになります。

**プリセット適用順序**
staticプリセットはColliderのみ追加し、Rigidbodyを追加しません。動く物体にstaticを適用しないよう注意。

**playmode_control(stop)後はシーンが元に戻る**
プレイモード中の変更は失われます。変更を保存したい場合はstop前にsave操作を実行してください。

**Prefab化は最小限に**
プロトタイプフェーズでは「3つ以上複製するもの」を目安にします。

---

## 関連ツール一覧

| ツール | プロトタイピングでの用途 |
|--------|------------------------|
| `unity_scene_crud` | プロトタイプシーンの作成・ロード |
| `unity_projectSettings_crud` | Build Settings への登録 (addSceneToBuild) |
| `unity_gameobject_crud` | プリミティブ配置 (template='Cube'等)・階層構築 |
| `unity_component_crud` | Transform設定・コンポーネントのアタッチ |
| `unity_physics_bundle` | applyPreset3D/applyPreset2D でプリセット即時適用 |
| `unity_camera_rig` | followカメラの即時セットアップ |
| `unity_input_profile` | createInputActions / createPlayerInput |
| `unity_asset_crud` | プロトタイプスクリプト生成 |
| `unity_prefab_crud` | 繰り返し要素のPrefab化・複製 |
| `unity_ui_foundation` | 最小デバッグUI作成 |
| `unity_ui_hierarchy` | 宣言的UIの素早い構築 |
| `unity_playmode_control` | play/stop/stepでのテストサイクル |
| `unity_console_log` | エラー・警告の即時確認 |
| `unity_validate_integrity` | プロトタイプ完了時の整合性チェック |
| `unity_compilation_await` | スクリプト生成後のコンパイル待機 |
| `unity_transform_batch` | 複数オブジェクトの一括配置 (arrangeLine等) |
