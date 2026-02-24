# アルファ ワークフローガイド (Unity-AI-Forge v{VERSION})

ゲームロジックとデータ設計の本格実装。プロトタイプで検証したコアループに、ScriptableObject・状態管理・イベント接続・UIバインディングを組み込み、ゲームシステムを完成させるためのガイドです。

---

## 概要

アルファフェーズの目的は「ゲームロジックを完成させる」ことです。プロトタイプで作ったシーン・スクリプト・UIの構造を流用しつつ、データ駆動設計・イベント接続・状態管理を導入してゲームシステムを堅牢にします。演出（VFX・Audio・Feedback）はベータフェーズで追加するため、このフェーズではロジックとデータに集中します。

**アルファフェーズの原則:**
- プロトタイプのシーン・UI構造はそのまま活用する
- ScriptableObject でハードコードされた値をデータ駆動に置き換える
- イベント（C# event / UnityEvent）でコンポーネント間を疎結合に接続する
- UIバインディングでデータとUI表示を自動連動させる
- 演出は入れない（ベータフェーズで追加）
- 各マイルストーンで validate_integrity を実行する

---

## パイプライン位置

```
企画 → 設計 → プロジェクト初期設定 → プロトタイプ → [アルファ] → ベータ → リリース
```

**前提**: プロトタイプでコアループの動作確認済み（`game_workflow_guide(phase='prototype')`）。シーン構造・UI構造・基本スクリプトが存在している状態。

---

## ワークフロー概要

```
ScriptableObject設計 → ゲームロジック実装 → 状態管理導入
→ UIバインディング設定 → イベント接続 → 品質ゲート確認
```

---

## 推奨手順

1. **ScriptableObject でデータ駆動化** - ハードコード値をSO化
2. **ゲームマネージャー実装** - ゲーム状態管理の中核
3. **状態管理の導入** - プレイヤー・敵・ゲーム進行の状態遷移
4. **UIバインディング設定** - プロトタイプUIにデータバインディングを接続
5. **UIコマンド設定** - ボタン→ロジック間の接続
6. **イベント接続** - UnityEventのwiring
7. **UIナビゲーション設定** - キーボード/ゲームパッド対応
8. **ゲームデータの拡充** - 敵データ・アイテムデータ等
9. **品質ゲート** - validate_integrity + class_dependency_graph
10. **Prefab化・整理** - ロジック付きPrefabの整備

---

## 実装ワークフロー (MCPツール使用例付き)

### Step 1: ScriptableObject でデータ駆動化

プロトタイプでハードコードしていたパラメータをScriptableObjectに抽出します。

```python
# ゲーム設定SO型の定義
unity_asset_crud(operation='create',
    assetPath='Assets/Scripts/Data/GameConfig.cs',
    content='''using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "MyGame/GameConfig")]
public class GameConfig : ScriptableObject
{
    [Header("Player")]
    public float playerSpeed   = 5f;
    public float jumpForce     = 7f;
    public int   maxHP         = 100;

    [Header("Game")]
    public int   startingLives = 3;
    public float invincibleTime = 1.5f;

    [Header("Economy")]
    public int   scorePerEnemy = 100;
    public int   scorePerCoin  = 50;
}''')

# 敵パラメータSO型の定義
unity_asset_crud(operation='create',
    assetPath='Assets/Scripts/Data/EnemyData.cs',
    content='''using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "MyGame/EnemyData")]
public class EnemyData : ScriptableObject
{
    public string enemyName;
    public int   maxHP       = 30;
    public float moveSpeed   = 3f;
    public float attackDamage = 10;
    public float attackRange  = 1.5f;
    public int   scoreReward  = 100;
}''')

unity_compilation_await(operation='await', timeoutSeconds=30)

# SOインスタンスの作成
unity_scriptableObject_crud(operation='create',
    typeName='GameConfig',
    assetPath='Assets/Data/ScriptableObjects/GameConfig.asset',
    properties={
        'playerSpeed': 5.0,
        'jumpForce': 7.0,
        'maxHP': 100,
        'startingLives': 3,
        'scorePerEnemy': 100,
        'scorePerCoin': 50
    })

unity_scriptableObject_crud(operation='create',
    typeName='EnemyData',
    assetPath='Assets/Data/ScriptableObjects/EnemyData_Slime.asset',
    properties={
        'enemyName': 'Slime',
        'maxHP': 30,
        'moveSpeed': 3.0,
        'attackDamage': 10,
        'scoreReward': 100
    })
```

### Step 2: ゲームマネージャー実装

```python
# ゲーム状態管理スクリプト
unity_asset_crud(operation='create',
    assetPath='Assets/Scripts/Managers/GameManager.cs',
    content='''using UnityEngine;
using System;

public enum GameState { Menu, Playing, Paused, GameOver, Victory }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public static event Action<GameState> OnGameStateChanged;
    public static event Action<int> OnScoreChanged;
    public static event Action<int, int> OnHealthChanged; // current, max

    [SerializeField] private GameConfig config;
    private GameState currentState;
    private int score;
    private int currentHP;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void StartGame()
    {
        score = 0;
        currentHP = config.maxHP;
        SetState(GameState.Playing);
        OnScoreChanged?.Invoke(score);
        OnHealthChanged?.Invoke(currentHP, config.maxHP);
    }

    public void AddScore(int amount)
    {
        score += amount;
        OnScoreChanged?.Invoke(score);
    }

    public void TakeDamage(int amount)
    {
        currentHP = Mathf.Max(0, currentHP - amount);
        OnHealthChanged?.Invoke(currentHP, config.maxHP);
        if (currentHP <= 0) SetState(GameState.GameOver);
    }

    public void PauseGame() => SetState(GameState.Paused);
    public void ResumeGame() => SetState(GameState.Playing);

    private void SetState(GameState newState)
    {
        currentState = newState;
        OnGameStateChanged?.Invoke(newState);
    }
}''')

unity_compilation_await(operation='await', timeoutSeconds=30)

# GameManagerをシーンに配置してSOを参照設定
unity_gameobject_crud(operation='create', name='GameManager')
unity_component_crud(operation='add',
    gameObjectPath='GameManager', componentType='GameManager',
    propertyChanges={
        'config': {'$ref': 'Assets/Data/ScriptableObjects/GameConfig.asset'}
    })
```

### Step 3: UIバインディング設定

プロトタイプで作ったUI要素にデータバインディングを接続します。

```python
# HPバーにヘルスバインディング（プロトタイプのHPTextを活用）
unity_gamekit_ui_binding(operation='create',
    targetPath='GameUI/HUD/HPText',
    bindingId='hp_display',
    sourceType='health',
    sourceId='player_hp',
    format='ratio')

# スコア表示にカスタムバインディング
unity_gamekit_ui_binding(operation='create',
    targetPath='GameUI/HUD/ScoreText',
    bindingId='score_display',
    sourceType='custom',
    sourceId='score',
    format='formatted',
    formatString='Score: {0:D6}')

# タイマー表示
unity_gamekit_ui_binding(operation='create',
    targetPath='GameUI/HUD/TimerText',
    bindingId='timer_display',
    sourceType='timer',
    sourceId='game_timer',
    format='formatted',
    formatString='Time: {0:F1}')

unity_compilation_await(operation='await', timeoutSeconds=30)
```

### Step 4: UIコマンド設定

ボタンとゲームロジックをコマンドパネルで接続します。

```python
# ポーズメニューのボタンをコマンドとして接続
unity_gamekit_ui_command(operation='createCommandPanel',
    panelId='pause_commands',
    canvasPath='GameUI/PauseMenu',
    commands=[
        {'name': 'Resume', 'commandType': 'action', 'label': 'Resume'},
        {'name': 'Quit',   'commandType': 'action', 'label': 'Quit'}
    ],
    targetType='actor',
    targetActorId='game_manager')

unity_compilation_await(operation='await', timeoutSeconds=30)
```

### Step 5: イベント接続

UnityEventを使ってボタンとマネージャーメソッドを接続します。

```python
# ボタンのイベントを接続
unity_event_wiring(operation='wireMultiple',
    wirings=[
        {
            'source': {'gameObject': 'GameUI/PauseMenu/ResumeBtn', 'component': 'Button', 'event': 'onClick'},
            'target': {'gameObject': 'GameManager', 'method': 'ResumeGame'}
        },
        {
            'source': {'gameObject': 'GameUI/PauseMenu/QuitBtn', 'component': 'Button', 'event': 'onClick'},
            'target': {'gameObject': 'GameManager', 'method': 'QuitGame'}
        }
    ])
```

### Step 6: UIナビゲーション設定

```python
# ポーズメニューにキーボード/ゲームパッドナビゲーション
unity_ui_navigation(operation='autoSetup',
    rootPath='GameUI/PauseMenu',
    direction='vertical')
```

### Step 7: UI状態管理

ゲーム状態に応じたUI表示切替を定義します。

```python
# Playing状態: HUDのみ表示
unity_ui_state(operation='defineState',
    rootPath='GameUI',
    stateName='playing',
    elements=[
        {'path': 'HUD',       'visible': True},
        {'path': 'PauseMenu', 'visible': False}
    ])

# Paused状態: HUD + PauseMenu表示
unity_ui_state(operation='defineState',
    rootPath='GameUI',
    stateName='paused',
    elements=[
        {'path': 'HUD',       'visible': True},
        {'path': 'PauseMenu', 'visible': True}
    ])

# 初期状態を適用
unity_ui_state(operation='applyState',
    rootPath='GameUI', stateName='playing')
```

### Step 8: ゲームデータの拡充

```python
# 追加の敵データ
unity_scriptableObject_crud(operation='create',
    typeName='EnemyData',
    assetPath='Assets/Data/ScriptableObjects/EnemyData_Bat.asset',
    properties={
        'enemyName': 'Bat',
        'maxHP': 15,
        'moveSpeed': 5.0,
        'attackDamage': 5,
        'scoreReward': 50
    })

# アイテムデータSO（新規型 → コンパイル → インスタンス）
unity_asset_crud(operation='create',
    assetPath='Assets/Scripts/Data/ItemData.cs',
    content='''using UnityEngine;

[CreateAssetMenu(fileName = "ItemData", menuName = "MyGame/ItemData")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public string description;
    public int    value;
    public Sprite icon;
}''')

unity_compilation_await(operation='await', timeoutSeconds=30)

unity_scriptableObject_crud(operation='create',
    typeName='ItemData',
    assetPath='Assets/Data/ScriptableObjects/Item_HealthPotion.asset',
    properties={
        'itemName': 'Health Potion',
        'description': 'Restores 30 HP',
        'value': 30
    })
```

### Step 9: 品質ゲート

```python
# 整合性チェック（全項目）
unity_validate_integrity(operation='all')

# Missing Scriptを自動除去
unity_validate_integrity(operation='removeMissingScripts')

# コード依存関係の確認
unity_class_dependency_graph(operation='analyzeClass',
    target='GameManager')

# GameManagerに依存しているクラスを確認
unity_class_dependency_graph(operation='findDependents',
    target='GameManager')

# 全MonoBehaviourの一覧確認
unity_class_catalog(operation='listTypes',
    typeKind='MonoBehaviour',
    searchPath='Assets/Scripts')

# ScriptableObject型の一覧確認
unity_class_catalog(operation='listTypes',
    typeKind='ScriptableObject',
    searchPath='Assets/Scripts/Data')

# シーン参照の確認
unity_scene_reference_graph(operation='findOrphans')

# コンパイルエラー確認
unity_console_log(operation='getCompilationErrors')
unity_console_log(operation='getErrors')

# プレイモードで最終動作確認
unity_playmode_control(operation='play')
unity_console_log(operation='getErrors')
unity_playmode_control(operation='stop')
```

### Step 10: Prefab化・整理

```python
# ロジック付きPrefab作成
unity_prefab_crud(operation='create',
    gameObjectPath='Player',
    prefabPath='Assets/Prefabs/Characters/Player.prefab')

unity_prefab_crud(operation='create',
    gameObjectPath='Enemy',
    prefabPath='Assets/Prefabs/Characters/Enemy.prefab')

# Prefab変更をシーンに適用
unity_prefab_crud(operation='applyOverrides', gameObjectPath='Player')
```

---

## チェックリスト

### ScriptableObject
- [ ] GameConfig SO を作成した（プレイヤーパラメータ・ゲーム設定）
- [ ] EnemyData SO を作成した（敵パラメータ）
- [ ] ハードコード値をSO参照に置き換えた

### ゲームロジック
- [ ] GameManager を実装した（状態管理・スコア・HP）
- [ ] C# event でゲーム状態変化を通知している
- [ ] プレイヤー・敵の状態遷移が動作する

### UIバインディング
- [ ] gamekit_ui_binding でHP表示をデータ連動にした
- [ ] gamekit_ui_binding でスコア表示をデータ連動にした
- [ ] ui_state でゲーム状態に応じたUI切替を定義した

### イベント接続
- [ ] event_wiring でボタンにイベントを接続した
- [ ] ui_navigation でキーボード/ゲームパッド操作を設定した

### 品質ゲート
- [ ] validate_integrity(all) でエラーなし確認
- [ ] class_dependency_graph でコード依存関係を確認
- [ ] class_catalog で全型を確認
- [ ] console_log(getErrors) でエラーなし確認
- [ ] playmode_control で動作最終確認

### Prefab
- [ ] 全キャラクター・アイテムをPrefab化した
- [ ] prefab_crud(applyOverrides) で変更を適用した

---

## 注意点・落とし穴

**GameKit生成コードはcompilation_awaitが必要**
ui_binding, ui_command, ui_selection 等はコード生成を行うため、生成後に必ず compilation_await を実行してください。

**ScriptableObject参照は `$ref` 形式で指定**
`propertyChanges` 内で SO アセットを参照するには `{'$ref': 'Assets/Data/...asset'}` を使用します。

**event_wiring の target メソッドはpublic必須**
UnityEvent から呼び出すメソッドは public で、引数は0個または1個（基本型）である必要があります。

**ui_state の元要素はシーン内に存在必須**
defineState で参照するUI要素パスは、シーン内に存在している必要があります。プロトタイプで作った構造をそのまま使います。

**prefab_crud の適用操作は applyOverrides**
apply ではなく applyOverrides を使用してください。

**validate_integrityはプレイモード中には実行しない**
プレイモード停止後に実行してください。

---

## 次のフェーズへ

アルファでゲームロジックが完成したら、ベータフェーズで演出を追加します:

1. **ベータ** (`game_workflow_guide(phase='beta')`) - アセット・VFX・Audio・Feedbackの統合
   - プリミティブを実アセットに置き換え
   - Presentation Pillar ツールで演出を追加
   - UI を本番品質に引き上げ

ロジック層が安定しているため、ベータでの演出追加はロジックに影響しません。

---

## 関連ツール一覧

| ツール | アルファでの用途 |
|--------|----------------|
| `unity_scriptableObject_crud` | create でSOインスタンス作成 |
| `unity_asset_crud` | create でスクリプト生成 |
| `unity_gamekit_ui_binding` | データバインディング設定 |
| `unity_gamekit_ui_command` | ボタン→ロジック接続 |
| `unity_ui_state` | ゲーム状態に応じたUI切替 |
| `unity_ui_navigation` | キーボード/ゲームパッドナビゲーション |
| `unity_event_wiring` | wire / wireMultiple でUnityEvent接続 |
| `unity_class_dependency_graph` | analyzeClass / findDependents でコード品質確認 |
| `unity_class_catalog` | listTypes / inspectType で型一覧確認 |
| `unity_validate_integrity` | all / removeMissingScripts で品質ゲート |
| `unity_scene_reference_graph` | findOrphans で孤立オブジェクト確認 |
| `unity_prefab_crud` | create / applyOverrides でPrefab管理 |
| `unity_playmode_control` | 動作確認 |
| `unity_console_log` | getCompilationErrors / getErrors でモニタリング |
| `unity_compilation_await` | コード生成後のコンパイル待機 |
