# オブジェクトプーリング実装ガイド (Unity-AI-Forge {VERSION})

## 概要

オブジェクトプーリングは、頻繁に生成・破棄されるオブジェクト（弾丸、エフェクト、敵など）を
事前に生成してプールに保持し、再利用することでGCによるフレームレート低下を防ぐ最適化パターンです。

`Instantiate()`/`Destroy()` の呼び出しはヒープメモリの割り当て・解放を伴い、
GCスパイクの主要原因となります。プーリングはこれを事前割り当て＋再利用に置き換え、
ランタイムのメモリ割り当てをゼロに近づけます。

Unity-AI-Forge では `unity_gamekit_vfx` が VFX 用プーリングを内蔵しており、
`unity_asset_crud` で汎用プールマネージャーの C# スクリプトを生成し、
`unity_prefab_crud` でプール対象のプレハブを管理できます。

---

## 設計パターン

### Unity 2021+ ObjectPool<T> API

Unity 2021 LTS 以降は `UnityEngine.Pool.ObjectPool<T>` が標準提供されています。
自前実装よりもこちらを優先的に使用してください。

```
ObjectPool<T>(
    createFunc,        // 新規生成ロジック
    actionOnGet,       // プールから取り出し時の初期化
    actionOnRelease,   // プールに返却時のリセット
    actionOnDestroy,   // プール容量超過時の破棄
    collectionCheck,   // 二重返却チェック (デバッグ用)
    defaultCapacity,   // 初期容量
    maxSize            // 最大容量
)
```

### プール対象の選定基準

| 条件 | プーリング推奨 | 例 |
|------|-------------|-----|
| 1秒間に5回以上生成/破棄 | 強く推奨 | 弾丸、パーティクル |
| シーン全体で50以上生成 | 推奨 | 敵、コイン、障害物 |
| 生成コストが高い | 推奨 | 複雑なプレハブ |
| ライフタイムが短い (< 3秒) | 推奨 | エフェクト、ダメージテキスト |
| 常時1-2個しか存在しない | 不要 | プレイヤー、ボス |
| 一度生成して常駐 | 不要 | UI、マネージャー |

### アーキテクチャ図

```
PoolManager (Singleton MonoBehaviour)
  +-- Dictionary<string, IObjectPool> pools
  +-- CreatePool<T>(prefab, initialSize, maxSize)
  +-- Get(poolId) : GameObject
  +-- Release(poolId, instance)

Poolable (各プール対象にアタッチ)
  +-- poolId: string
  +-- OnGetFromPool()   → 有効化・初期化
  +-- OnReturnToPool()  → 無効化・リセット
  +-- ReturnAfter(seconds) → 一定時間後に自動返却
```

---

## 推奨フォルダ構造

```
Assets/
  Scripts/
    Pool/
      PoolManager.cs           # 汎用プールマネージャー
      Poolable.cs              # プール対象基底コンポーネント
    Projectile/
      Bullet.cs                # 弾丸（Poolable 継承/利用）
      BulletSpawner.cs         # 弾丸スポナー（PoolManager 経由）
    Enemy/
      EnemySpawner.cs          # 敵スポナー（PoolManager 経由）
  Prefabs/
    Projectiles/
      Bullet.prefab
    Enemies/
      Enemy_Basic.prefab
    FX/
      HitEffect.prefab
```

---

## 実装ワークフロー (MCPツール使用例付き)

### Step 1: プールマネージャーの作成

```python
# PoolManager スクリプトを生成
unity_asset_crud(
    operation="create",
    assetPath="Assets/Scripts/Pool/PoolManager.cs",
    content="""using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }

    [System.Serializable]
    public class PoolConfig
    {
        public string poolId;
        public GameObject prefab;
        public int initialSize = 10;
        public int maxSize = 50;
    }

    [SerializeField] private List<PoolConfig> poolConfigs = new();
    private Dictionary<string, ObjectPool<GameObject>> pools = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializePools();
    }

    void InitializePools()
    {
        foreach (var config in poolConfigs)
        {
            var c = config;
            var pool = new ObjectPool<GameObject>(
                createFunc: () => {
                    var obj = Instantiate(c.prefab, transform);
                    obj.SetActive(false);
                    return obj;
                },
                actionOnGet: obj => obj.SetActive(true),
                actionOnRelease: obj => obj.SetActive(false),
                actionOnDestroy: obj => Destroy(obj),
                collectionCheck: true,
                defaultCapacity: c.initialSize,
                maxSize: c.maxSize
            );
            // ウォームアップ: 初期数分を事前生成
            var warmup = new List<GameObject>();
            for (int i = 0; i < c.initialSize; i++)
                warmup.Add(pool.Get());
            foreach (var obj in warmup)
                pool.Release(obj);
            pools[c.poolId] = pool;
        }
    }

    public GameObject Get(string poolId)
    {
        return pools.TryGetValue(poolId, out var pool) ? pool.Get() : null;
    }

    public void Release(string poolId, GameObject obj)
    {
        if (pools.TryGetValue(poolId, out var pool))
            pool.Release(obj);
        else
            Destroy(obj);
    }
}"""
)

# コンパイル待ち
unity_compilation_await(operation="await", timeoutSeconds=30)
```

### Step 2: Poolable コンポーネントの作成

```python
# プール対象に付けるヘルパーコンポーネント
unity_asset_crud(
    operation="create",
    assetPath="Assets/Scripts/Pool/Poolable.cs",
    content="""using UnityEngine;

public class Poolable : MonoBehaviour
{
    [SerializeField] private string poolId;
    public string PoolId => poolId;

    public void ReturnToPool()
    {
        if (PoolManager.Instance != null)
            PoolManager.Instance.Release(poolId, gameObject);
        else
            Destroy(gameObject);
    }

    public void ReturnAfter(float seconds)
    {
        CancelInvoke(nameof(ReturnToPool));
        Invoke(nameof(ReturnToPool), seconds);
    }

    void OnDisable()
    {
        CancelInvoke(nameof(ReturnToPool));
    }
}"""
)

unity_compilation_await(operation="await", timeoutSeconds=30)
```

### Step 3: PoolManager の配置とプレハブ準備

```python
# PoolManager GameObject を作成
unity_gameobject_crud(operation="create", name="PoolManager")

# PoolManager コンポーネントをアタッチ
unity_component_crud(operation="add",
    gameObjectPath="PoolManager",
    componentType="PoolManager")

# 弾丸プレハブを作成
unity_gameobject_crud(operation="create", name="Bullet",
    position={'x': 0, 'y': -100, 'z': 0})
unity_component_crud(operation="add",
    gameObjectPath="Bullet", componentType="Rigidbody2D",
    propertyChanges={'gravityScale': 0, 'collisionDetection': 'Continuous'})
unity_component_crud(operation="add",
    gameObjectPath="Bullet", componentType="CircleCollider2D",
    propertyChanges={'radius': 0.1, 'isTrigger': True})
unity_component_crud(operation="add",
    gameObjectPath="Bullet", componentType="Poolable")

# プレハブ化
unity_prefab_crud(operation="create",
    gameObjectPath="Bullet",
    prefabPath="Assets/Prefabs/Projectiles/Bullet.prefab")

# 元のGameObjectを削除（プレハブから参照するため）
unity_gameobject_crud(operation="delete", gameObjectPath="Bullet")
```

### Step 4: VFX にはGameKit VFX プーリングを使用

```python
# GameKit VFX はプーリング内蔵 — 自前プール不要
unity_gameobject_crud(operation="create", name="FX")

unity_gamekit_vfx(operation="create",
    targetPath="FX",
    vfxId="hit_effect",
    particlePrefabPath="Assets/Prefabs/FX/HitEffect.prefab",
    usePooling=True,
    poolSize=20)

unity_compilation_await(operation="await")
```

### Step 5: 弾丸スポナーの実装

```python
# スポナースクリプト（PoolManager 経由で弾丸を取得）
unity_asset_crud(
    operation="create",
    assetPath="Assets/Scripts/Projectile/BulletSpawner.cs",
    content="""using UnityEngine;

public class BulletSpawner : MonoBehaviour
{
    [SerializeField] private string bulletPoolId = "bullet";
    [SerializeField] private float bulletSpeed = 10f;
    [SerializeField] private float bulletLifetime = 3f;
    [SerializeField] private Transform firePoint;

    public void Fire()
    {
        var bullet = PoolManager.Instance.Get(bulletPoolId);
        if (bullet == null) return;

        bullet.transform.position = firePoint.position;
        bullet.transform.rotation = firePoint.rotation;

        var rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(firePoint.right * bulletSpeed, ForceMode2D.Impulse);
        }

        var poolable = bullet.GetComponent<Poolable>();
        poolable?.ReturnAfter(bulletLifetime);
    }
}"""
)

unity_compilation_await(operation="await", timeoutSeconds=30)
```

---

## よくあるパターン

### パターン 1: 敵ウェーブスポーン

```python
# EnemySpawner スクリプト — PoolManager 経由で敵を生成
unity_asset_crud(
    operation="create",
    assetPath="Assets/Scripts/Enemy/EnemySpawner.cs",
    content="""using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private string enemyPoolId = "enemy_basic";
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private int maxActive = 10;
    private int activeCount;

    public void StartWave(int count)
    {
        StartCoroutine(SpawnWave(count));
    }

    IEnumerator SpawnWave(int count)
    {
        for (int i = 0; i < count && activeCount < maxActive; i++)
        {
            var enemy = PoolManager.Instance.Get(enemyPoolId);
            if (enemy == null) yield break;
            var sp = spawnPoints[i % spawnPoints.Length];
            enemy.transform.position = sp.position;
            activeCount++;
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    public void OnEnemyDefeated(GameObject enemy)
    {
        PoolManager.Instance.Release(enemyPoolId, enemy);
        activeCount--;
    }
}"""
)
```

### パターン 2: ダメージテキストポップアップ

プールから TextMeshPro オブジェクトを取得し、ダメージ数値を表示して一定時間後に返却。

```python
# FloatingText プレハブを作成してプールに登録
unity_gameobject_crud(operation="create", name="FloatingText")
unity_ui_foundation(operation="createText",
    canvasPath="Canvas_World", textName="DmgText", text="0",
    fontSize=24)
unity_component_crud(operation="add",
    gameObjectPath="Canvas_World/DmgText", componentType="Poolable")
unity_prefab_crud(operation="create",
    gameObjectPath="Canvas_World/DmgText",
    prefabPath="Assets/Prefabs/UI/FloatingText.prefab")
```

### パターン 3: GameKit VFX + 汎用プールの併用

```
VFX (パーティクル)   → unity_gamekit_vfx (usePooling=True) を使用
弾丸・敵・UIテキスト → PoolManager (ObjectPool<T>) を使用
```

VFX 用途では GameKit のプーリングが最も簡潔です。汎用オブジェクトには
`ObjectPool<T>` ベースの PoolManager を使い分けてください。

---

## パフォーマンス計測

プーリングの効果を確認するためのワークフロー:

```python
# 変更前のログスナップショットを取得
unity_console_log(operation="snapshot")

# プレイモードでテスト
unity_playmode_control(operation="play")

# 一定時間後にランタイム状態を取得
unity_playmode_control(operation="captureState",
    targets=["PoolManager"], includeConsole=True)

# 停止してログ差分を確認
unity_playmode_control(operation="stop")
unity_console_log(operation="diff", severity=["warning"])
```

GC Alloc が表示されなければプーリングは正常に機能しています。
Unity Profiler の Memory セクションで GC.Alloc を監視してください。

---

## 注意点・落とし穴

1. **OnEnable/OnDisable の活用**
   プールから取得時に `OnEnable`、返却時に `OnDisable` が呼ばれる。
   `Start()` は最初の1回しか呼ばれないため、リセット処理は `OnEnable()` に書くこと。

2. **コルーチンのリーク**
   プール返却時に実行中のコルーチンが残ると次回取得時に予期しない動作を起こす。
   `OnDisable()` で `StopAllCoroutines()` を呼ぶか、`CancelInvoke()` で確実に停止する。

3. **Transform のリセット忘れ**
   プールから取得した際、position/rotation/localScale が前回の値のまま残る。
   `actionOnGet` で明示的にリセットするか、取得直後に設定すること。

4. **最大サイズの設定**
   `maxSize` を小さくしすぎると容量超過時に `Destroy` が走り、プーリングの意味がなくなる。
   プロファイリングで最大同時存在数を計測し、その1.5倍を目安に設定する。

5. **シーン遷移時のクリーンアップ**
   `DontDestroyOnLoad` の PoolManager 配下のオブジェクトはシーン遷移で破棄されない。
   シーン遷移時に全プールを `Clear()` するか、シーンごとにプールを再構築する設計にする。

6. **二重返却の防止**
   同じオブジェクトを2回 `Release()` すると例外が発生する。
   `collectionCheck: true` をデバッグ時に有効にして検出する。

---

## 関連ツール一覧

| ツール名 | 用途 |
|---|---|
| `unity_asset_crud` | PoolManager・Poolable スクリプトの生成 |
| `unity_gameobject_crud` | プール対象 GameObject の作成 |
| `unity_component_crud` | Rigidbody・Collider 等のコンポーネント追加 |
| `unity_prefab_crud` | プール対象のプレハブ化 |
| `unity_gamekit_vfx` | VFX 用プーリング内蔵 (`usePooling=True`) |
| `unity_compilation_await` | スクリプト生成後のコンパイル完了待ち |
| `unity_playmode_control` | テスト実行・ランタイム状態取得 |
| `unity_console_log` | パフォーマンス検証・ログ差分 |
| `unity_validate_integrity` | 実装後の整合性チェック |
