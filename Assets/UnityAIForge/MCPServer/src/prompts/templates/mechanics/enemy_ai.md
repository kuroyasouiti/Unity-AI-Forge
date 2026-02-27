# 敵 AI 実装ガイド (Unity-AI-Forge {VERSION})

## 概要

敵 AI はプレイヤーに挑戦と緊張感をもたらすゲームの重要な要素です。
パトロール・索敵・追跡・攻撃の基本行動から、複数フェーズを持つボス AI まで、
Unity-AI-Forge の `unity_asset_crud` でカスタム AI スクリプトを作成し、
`unity_physics_bundle` で物理設定、`unity_gamekit_animation_sync` でアニメーション同期、
`unity_gamekit_effect` で演出を付加して体系的に構築できます。

---

## 設計パターン

### 敵 AI の基本状態遷移

```
[Idle/Patrol]
    |  プレイヤーを検出（視野/音）
    v
[Chase]
    |  攻撃範囲内に入る          |  見失う（一定時間）
    v                            v
[Attack]                    [Search] -> [Patrol]
    |  撃破
    v
[Death]
```

### 検出システムの種類

- **視野検出**: 前方の扇形エリアにプレイヤーが入ったら追跡開始
- **聴覚検出**: 一定半径内での音（走り音、攻撃音）に反応
- **ダメージ検出**: 攻撃を受けたら即座に索敵開始
- **タグ検出**: OverlapSphere + Layer/Tag フィルタリング

### 移動方式の選択

| 方式 | 特徴 | 適用場面 |
|---|---|---|
| NavMesh Agent | 3D 地形の自動経路探索 | 3D アクション、RPG |
| ウェイポイント追跡 | 固定ルートを巡回 | 2D・単純パトロール |
| CharacterController | カスタム移動ロジック | プラットフォーマー |
| Rigidbody | 物理ベースの移動 | 物理演出が重要な場合 |

### ボス AI のフェーズ設計

```
Phase 1 [HP 100-70%]: 基本攻撃パターン（地上移動、近接攻撃）
Phase 2 [HP 70-30%]: 強化パターン（高速移動、範囲攻撃、召喚）
Phase 3 [HP < 30%]:  怒り状態（全攻撃強化、新パターン追加）
```

---

## 推奨フォルダ構造

```
Assets/
  Scripts/
    Enemy/
      Core/
        EnemyBase.cs            # 敵の基底クラス
        EnemyHealth.cs          # HP/ダメージ管理
        EnemyStateMachine.cs    # 敵 AI 状態管理
      Detection/
        DetectionSystem.cs      # 視野・聴覚検出
      States/
        PatrolState.cs
        ChaseState.cs
        AttackState.cs
        DeathState.cs
      Types/
        SlimeEnemy.cs           # スライム（シンプル追跡）
        BossEnemy.cs            # ボス（多段フェーズ）
  Data/
    Enemies/
      SlimeData.asset           # スライムパラメータ
      BossData.asset            # ボスパラメータ
  Prefabs/
    Enemies/
      Slime.prefab
      Boss.prefab
```

---

## 実装ワークフロー (MCPツール使用例付き)

### Step 1: 敵 AI 基盤スクリプトの作成

```python
# 敵の体力管理スクリプト
unity_asset_crud(
    operation="create",
    assetPath="Assets/Scripts/Enemy/Core/EnemyHealth.cs",
    content="""using UnityEngine;
using System;
public class EnemyHealth : MonoBehaviour {
    [SerializeField] private int maxHp = 100;
    private int currentHp;
    public event Action<int> OnDamaged;
    public event Action OnDeath;
    public float HpRatio => (float)currentHp / maxHp;
    void Awake() { currentHp = maxHp; }
    public void TakeDamage(int damage) {
        currentHp = Mathf.Max(0, currentHp - damage);
        OnDamaged?.Invoke(damage);
        if (currentHp <= 0) OnDeath?.Invoke();
    }
}"""
)

# 敵の検出システムスクリプト
unity_asset_crud(
    operation="create",
    assetPath="Assets/Scripts/Enemy/Detection/DetectionSystem.cs",
    content="""using UnityEngine;
public class DetectionSystem : MonoBehaviour {
    [SerializeField] private float detectionRadius = 8f;
    [SerializeField] private float detectionAngle = 120f;
    [SerializeField] private LayerMask targetLayer;
    [SerializeField] private string targetTag = \"Player\";
    public Transform DetectedTarget { get; private set; }
    public bool HasTarget => DetectedTarget != null;
    void FixedUpdate() {
        var hits = Physics.OverlapSphere(transform.position, detectionRadius, targetLayer);
        DetectedTarget = null;
        foreach (var hit in hits) {
            if (!hit.CompareTag(targetTag)) continue;
            var dir = (hit.transform.position - transform.position).normalized;
            var angle = Vector3.Angle(transform.forward, dir);
            if (angle < detectionAngle / 2f) {
                DetectedTarget = hit.transform;
                break;
            }
        }
    }
}"""
)

# コンパイル待ち
unity_compilation_await(operation="await", timeoutSeconds=30)
```

### Step 2: 敵パラメータ用 ScriptableObject の作成

```python
# 敵パラメータ ScriptableObject 定義
unity_asset_crud(
    operation="create",
    assetPath="Assets/Scripts/Enemy/Core/EnemyData.cs",
    content="""using UnityEngine;
[CreateAssetMenu(fileName = \"NewEnemy\", menuName = \"Game/EnemyData\")]
public class EnemyData : ScriptableObject {
    public string enemyName;
    public int maxHp = 50;
    public float moveSpeed = 3f;
    public float chaseSpeed = 5f;
    public float attackRange = 1.5f;
    public float attackCooldown = 1.5f;
    public int attackDamage = 10;
    public float detectionRadius = 8f;
}"""
)

unity_compilation_await(operation="await", timeoutSeconds=30)

# 具体的な敵パラメータを作成
unity_scriptableObject_crud(
    operation="create",
    typeName="EnemyData",
    assetPath="Assets/Data/Enemies/SlimeData.asset",
    properties={
        "enemyName": "スライム",
        "maxHp": 50,
        "moveSpeed": 2.0,
        "chaseSpeed": 4.0,
        "attackRange": 1.5,
        "attackCooldown": 1.5,
        "attackDamage": 10,
        "detectionRadius": 8.0
    }
)
```

### Step 3: 敵 GameObject のセットアップ

```python
# スライム敵を作成
unity_gameobject_crud(
    operation="create",
    name="Slime",
    tag="Enemy",
    components=[
        {"type": "EnemyHealth"},
        {"type": "DetectionSystem"}
    ]
)

# コライダーと Rigidbody を追加
unity_physics_bundle(
    operation="applyPreset3D",
    targetPath="Slime",
    preset="kinematic"
)
```

### Step 4: パトロール用ウェイポイントの配置

```python
# パトロールパスの親オブジェクトを作成
unity_gameobject_crud(operation="create", name="SlimePatrolPath")

# ウェイポイントを配置
unity_gameobject_crud(operation="create", name="WP_01", parentPath="SlimePatrolPath")
unity_gameobject_crud(operation="create", name="WP_02", parentPath="SlimePatrolPath")
unity_gameobject_crud(operation="create", name="WP_03", parentPath="SlimePatrolPath")
unity_gameobject_crud(operation="create", name="WP_04", parentPath="SlimePatrolPath")

# transform_batch でウェイポイントを矩形に整列
unity_transform_batch(
    operation="arrangeLine",
    gameObjectPaths=[
        "SlimePatrolPath/WP_01", "SlimePatrolPath/WP_02",
        "SlimePatrolPath/WP_03", "SlimePatrolPath/WP_04"
    ],
    startPosition={"x": 10, "y": 0, "z": 5},
    endPosition={"x": 20, "y": 0, "z": 10}
)
```

### Step 5: アニメーション同期

```python
# 敵アニメーションと AI 状態を同期
unity_gamekit_animation_sync(
    operation="create",
    targetPath="Slime",
    syncId="slime_anim_sync",
    syncRules=[
        {"parameter": "Speed",     "parameterType": "float",
         "sourceType": "rigidbody3d", "sourceProperty": "velocity.magnitude"},
        {"parameter": "IsChasing", "parameterType": "bool",
         "sourceType": "custom", "boolThreshold": 0.1}
    ],
    triggers=[
        {"triggerName": "Attack", "eventSource": "manual"},
        {"triggerName": "Death",  "eventSource": "health",
         "healthId": "slime_health", "healthEvent": "OnDeath"}
    ]
)

# コンパイル待ち
unity_compilation_await(operation="await", timeoutSeconds=30)
```

### Step 6: ダメージ・死亡エフェクト

```python
# ダメージ時のフィードバック
unity_gamekit_feedback(
    operation="create",
    targetPath="Slime",
    feedbackId="slime_damage_feedback",
    components=[
        {"type": "colorFlash", "color": {"r": 1, "g": 0, "b": 0, "a": 0.5},
         "duration": 0.15, "fadeTime": 0.1},
        {"type": "scale", "scaleAmount": {"x": 0.1, "y": 0.1, "z": 0.1},
         "duration": 0.1}
    ]
)

# 死亡時のエフェクト
unity_gamekit_effect(
    operation="create",
    effectId="slime_death_effect",
    components=[
        {"type": "particle",    "duration": 1.0},
        {"type": "sound",       "volume": 0.8},
        {"type": "cameraShake", "intensity": 0.2, "shakeDuration": 0.3}
    ]
)

# 敵の SE
unity_gamekit_audio(
    operation="create",
    targetPath="Slime",
    audioId="slime_sfx",
    audioType="sfx",
    volume=0.7,
    spatialBlend=1.0,
    minDistance=2.0,
    maxDistance=15.0
)

# コンパイル待ち
unity_compilation_await(operation="await", timeoutSeconds=30)
```

### Step 7: プレハブ化と確認

```python
# 敵をプレハブとして保存
unity_prefab_crud(
    operation="create",
    gameObjectPath="Slime",
    prefabPath="Assets/Prefabs/Enemies/Slime.prefab"
)

# ボス戦カメラリグ
unity_camera_rig(
    operation="createRig",
    targetPath="MainCamera",
    preset="follow"
)

# 整合性チェック
unity_validate_integrity(operation="all")

# 参照関係を確認
unity_scene_reference_graph(
    operation="analyzeObject",
    objectPath="Slime",
    format="summary"
)
```

---

## よくあるパターン

### パターン 1: NavMesh 対応（3D）

NavMesh を使った敵AIの経路探索は以下の手順で構築します。

#### 1-1. NavMeshSurface の設定

Unity 2022 LTS 以降は `AI Navigation` パッケージ（com.unity.ai.navigation）を使用します。
従来の Window > AI > Navigation ではなく、コンポーネントベースでベイクを行います。

```python
# 地形オブジェクトに NavMeshSurface を追加
unity_component_crud(
    operation="add",
    gameObjectPath="Environment/Ground",
    componentType="Unity.AI.Navigation.NavMeshSurface",
    propertyChanges={
        "agentTypeID": 0,
        "collectObjects": 0,
        "useGeometry": 0
    }
)
```

**注意**: NavMeshSurface のベイクは Unity Editor 上で手動実行するか、
エディタスクリプトから `surface.BuildNavMesh()` を呼ぶ必要があります。

#### 1-2. NavMeshAgent の設定

```python
# NavMesh Agent の設定
unity_component_crud(
    operation="add",
    gameObjectPath="Slime",
    componentType="UnityEngine.AI.NavMeshAgent",
    propertyChanges={
        "speed": 4.0,
        "acceleration": 8.0,
        "stoppingDistance": 1.5,
        "angularSpeed": 360,
        "radius": 0.5,
        "height": 1.0,
        "autoBraking": True
    }
)
```

#### 1-3. NavMeshObstacle（動的障害物）

移動しない障害物は Static にして NavMesh に焼き込みますが、
ランタイムで動く障害物は NavMeshObstacle を使用します。

```python
# 動的障害物（移動する箱など）
unity_component_crud(
    operation="add",
    gameObjectPath="MovingBox",
    componentType="UnityEngine.AI.NavMeshObstacle",
    propertyChanges={
        "shape": 0,
        "center": {"x": 0, "y": 0.5, "z": 0},
        "size": {"x": 1, "y": 1, "z": 1},
        "carve": True,
        "carveOnlyStationary": True
    }
)
```

#### 1-4. NavMesh エリアとコスト

特定のエリア（水場、泥地など）に移動コストを設定して経路選択に影響を与えます。

```python
# カスタムエリアをタグ/レイヤーで設定
unity_projectSettings_crud(operation="write",
    category="tagsLayers", property="addTag", value="SlowZone")

# NavMeshModifier で特定オブジェクトのエリアタイプを変更
unity_component_crud(
    operation="add",
    gameObjectPath="Environment/SwampArea",
    componentType="Unity.AI.Navigation.NavMeshModifier",
    propertyChanges={
        "overrideArea": True,
        "area": 3
    }
)
```

#### 1-5. NavMesh のよくある問題

| 問題 | 原因 | 対策 |
|------|------|------|
| Agent が動かない | NavMesh がベイクされていない | NavMeshSurface の Build 実行 |
| Agent がすり抜ける | Agent の radius が小さすぎる | radius をコライダーに合わせる |
| 段差を超えられない | Step Height が低い | Agent の stepHeight を調整 |
| 経路が不自然 | エリアコストが未設定 | NavMeshModifier でコスト調整 |
| Off-Mesh Link が機能しない | 両端が NavMesh 上にない | Link の端点位置を確認 |

### パターン 2: 敵のウェーブスポーン

```python
# スポーンマネージャースクリプトを作成
unity_asset_crud(
    operation="create",
    assetPath="Assets/Scripts/Enemy/Spawner/WaveSpawner.cs",
    content="""using UnityEngine;
using System.Collections;
public class WaveSpawner : MonoBehaviour {
    [SerializeField] private GameObject[] enemyPrefabs;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float timeBetweenWaves = 10f;
    private int waveNumber;
    public void StartWave() {
        StartCoroutine(SpawnWave());
    }
    IEnumerator SpawnWave() {
        waveNumber++;
        int count = 3 + waveNumber * 2;
        for (int i = 0; i < count; i++) {
            var prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
            var point = spawnPoints[Random.Range(0, spawnPoints.Length)];
            Instantiate(prefab, point.position, Quaternion.identity);
            yield return new WaitForSeconds(0.5f);
        }
    }
}"""
)

unity_compilation_await(operation="await", timeoutSeconds=30)
```

### パターン 3: デバッグ用ギズモ

視野範囲・攻撃範囲を Scene ビューで可視化するための OnDrawGizmos() を
DetectionSystem スクリプト内に実装する。

---

## 注意点・落とし穴

1. **NavMesh のベイク忘れ**
   NavMesh Agent を使う場合は必ず Window > AI > Navigation でベイクを実行すること。

2. **Physics レイヤー設定**
   敵同士が互いを検出しないよう、Enemy レイヤーの自己衝突を無効化すること。
   `unity_projectSettings_crud` で Enemy レイヤーを追加できる。

```python
unity_projectSettings_crud(
    operation="write",
    category="tagsLayers",
    property="layers",
    value={"index": 8, "name": "Enemy"}
)
```

3. **コード生成後のコンパイル待ち**
   `unity_asset_crud` でスクリプト作成後、`unity_gamekit_animation_sync` 等のコード生成後は、
   必ず `unity_compilation_await` を呼ぶこと。

4. **パフォーマンス: Update の頻度**
   多数の敵が毎フレーム検出処理を行うと重くなる。
   検出処理は FixedUpdate や 0.2〜0.5 秒ごとの Coroutine で実行するよう設計する。

5. **死亡処理のタイミング**
   死亡アニメーション再生中に Collider を無効化し、
   GameObject の削除はアニメーション完了後に行うこと。

6. **スポーン位置のチェック**
   スポーン時にプレイヤーと重なると即ダメージになるため、
   スポーン位置のコリジョンチェックを必ず実装する。

---

## 関連ツール一覧

| ツール名 | 用途 |
|---|---|
| `unity_asset_crud` | 敵 AI スクリプト・ステート・検出ロジックの作成 |
| `unity_scriptableObject_crud` | 敵パラメータ (ScriptableObject) の作成 |
| `unity_gameobject_crud` | 敵 GameObject・ウェイポイントの作成 |
| `unity_component_crud` | NavMeshAgent 等のコンポーネント追加 |
| `unity_physics_bundle` | コライダー・Rigidbody の物理設定 |
| `unity_transform_batch` | ウェイポイントの一括配置 |
| `unity_prefab_crud` | 敵プレハブの作成・管理 |
| `unity_gamekit_animation_sync` | AI 状態とアニメーションの同期 |
| `unity_gamekit_effect` | ダメージ・死亡エフェクト |
| `unity_gamekit_feedback` | ヒット時のカメラシェイク・フラッシュ |
| `unity_gamekit_audio` | 敵の SE（鳴き声・攻撃音） |
| `unity_camera_rig` | ボス戦・ロックオンカメラ |
| `unity_projectSettings_crud` | Enemy レイヤーの追加 |
| `unity_compilation_await` | スクリプト生成後のコンパイル完了待ち |
| `unity_validate_integrity` | シーン整合性チェック |
| `unity_scene_reference_graph` | 敵 AI の参照関係の確認 |
