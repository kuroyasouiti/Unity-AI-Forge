# プロトタイプ ワークフローガイド (Unity-AI-Forge v{VERSION})

プリミティブとプリセットを活用した高速プロトタイプ構築。シーン・スクリプト・UIという後続フェーズでも流用する土台を最短時間で構築し、コアループを検証するための実践ガイドです。

---

## 概要

プロトタイピングフェーズの目的は「動くかどうか」を素早く確認することです。このフェーズで作るシーン構造・スクリプト・UIは、アルファ以降でもそのまま流用・拡張する土台になります。アセットの品質は後回しにし、まずゲームループが成立するか検証します。Unity-AI-Forgeのプリセット機能と即時プレイモード制御を活用して、アイデアから検証のサイクルを高速化します。

**プロトタイピングの原則:**
- 完璧を求めない。まず動かす
- プリミティブ（Cube/Sphere/Capsule）をキャラクター・障害物・ゴールとして使う
- プリセットで物理・カメラを一瞬でセットアップ
- UIは構造を意識して作る（アルファ以降でバインディング・演出を乗せるため）
- Prefab化は「同じものを複数配置するとき」だけ
- playmode_control + console_log で素早くフィードバックを得る

---

## パイプライン位置

```
企画 → 設計 → プロジェクト初期設定 → [プロトタイプ] → アルファ → ベータ → リリース
```

**前提**: 企画書でコアループ・メカニクス・シーン構成が定義済み（`game_workflow_guide(phase='planning')`）。設計でデザインパターン・クラス構造が決定済み（`game_workflow_guide(phase='design')`）。プロジェクト初期設定でタグ・レイヤー・フォルダ構造が整備済み（`game_workflow_guide(phase='project_setup')`）。

---

## ワークフロー概要

```
シーン作成 → プリミティブ配置 → 物理設定 → カメラセットアップ
→ 最小スクリプト添付 → UI構築 → プレイテスト → ログ確認 → 繰り返し
```

---

## 推奨手順

1. **シーン作成** - 専用プロトタイプシーンを用意する
2. **地面・壁配置** - Cubeプリミティブで環境を構築
3. **プレイヤー配置** - Capsuleをプレイヤーとして使用
4. **物理プリセット適用** - characterプリセットで即座に動ける状態に
5. **カメラセットアップ** - followリグで自動追従
6. **入力設定** - New Input Systemのアクションマップを定義
7. **UI構築** - HUD・メニューを構造的に構築（後で拡張しやすく）
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

### Step 5: 入力プロファイルの接続

プロジェクト初期設定（`game_workflow_guide(phase='project_setup')`）で作成済みの InputActions アセットを使用します。

```python
# project_setup で作成済みの InputActions を使用
# ※ InputActions アセットの新規作成は不要（project_setup Step 6 で作成済み）

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

### Step 7: UIを構造的に構築

プロトタイプ段階でもUIの構造（階層・命名）を意識して作ります。この構造はアルファでバインディング、ベータで演出を乗せる土台になります。

```python
# Canvas作成（本番でも流用する構造）
unity_ui_foundation(operation='createCanvas', name='GameUI')

# 宣言的UIでHUDを構築（後でバインディングを追加しやすい構造）
unity_ui_hierarchy(operation='create', parentPath='GameUI',
    hierarchy={
        'type': 'panel', 'name': 'HUD',
        'children': [
            {'type': 'text', 'name': 'ScoreText', 'text': 'Score: 0', 'fontSize': 24},
            {'type': 'text', 'name': 'HPText',    'text': 'HP: 100',  'fontSize': 24},
            {'type': 'text', 'name': 'TimerText', 'text': 'Time: 0',  'fontSize': 20}
        ],
        'layout': 'Vertical', 'spacing': 8
    })

# メニュー構造（ボタン配置 → アルファでイベント接続）
unity_ui_hierarchy(operation='create', parentPath='GameUI',
    hierarchy={
        'type': 'panel', 'name': 'PauseMenu',
        'children': [
            {'type': 'text',   'name': 'Title',     'text': 'PAUSED', 'fontSize': 36},
            {'type': 'button', 'name': 'ResumeBtn', 'text': 'Resume'},
            {'type': 'button', 'name': 'QuitBtn',   'text': 'Quit'}
        ],
        'layout': 'Vertical', 'spacing': 16
    })

# ポーズメニューは初期非表示
unity_ui_hierarchy(operation='hide', targetPath='GameUI/PauseMenu')
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

詳細なテスト・検証手法は `game_workflow_guide(phase='testing')` の PDCA サイクルを参照してください。

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
- [ ] project_setup で作成した InputActions アセットが存在することを確認した
- [ ] input_profile(createPlayerInput) で PlayerInput コンポーネントをアタッチした
- [ ] プレイヤー移動スクリプトを生成してアタッチした
- [ ] compilation_awaitで確実にコンパイル完了を待った

### UI（後続フェーズの土台）
- [ ] Canvas を作成し、HUDパネルを構築した
- [ ] テキスト要素に明確な命名をつけた（ScoreText, HPText 等）
- [ ] メニュー構造を作成した（PauseMenu 等）

### プレイテスト
- [ ] playmode_controlでプレイ・停止を繰り返した
- [ ] console_log(getErrors) でエラーがないことを確認した
- [ ] コアループが成立することを確認した（企画書の検証基準）

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

**UIの命名を一貫させる**
プロトタイプで付けた名前（ScoreText, HPText）はアルファ以降のバインディングで参照されます。後から変更すると影響範囲が広いので、最初から命名規則を守ってください。

---

## 次のフェーズへ

プロトタイプでコアループの動作が確認できたら、次はアルファフェーズです:

1. **アルファ** (`game_workflow_guide(phase='alpha')`) - ゲームロジック・データ設計の本格実装
   - ScriptableObject でデータ駆動化
   - イベント接続でコンポーネント間通信
   - UI バインディングでデータ連動

プロトタイプで作ったシーン構造・UI構造・スクリプトはアルファ以降でそのまま拡張します。

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
| `unity_ui_foundation` | Canvas作成 |
| `unity_ui_hierarchy` | 宣言的UIの構造的構築 |
| `unity_playmode_control` | play/stop/stepでのテストサイクル |
| `unity_console_log` | エラー・警告の即時確認 |
| `unity_validate_integrity` | プロトタイプ完了時の整合性チェック |
| `unity_compilation_await` | スクリプト生成後のコンパイル待機 |
| `unity_transform_batch` | 複数オブジェクトの一括配置 (arrangeLine等) |
