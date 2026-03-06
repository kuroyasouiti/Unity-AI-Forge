# タクティクス/SRPG 設計ガイド - Unity-AI-Forge v{VERSION}

## 概要

グリッドベースの戦術バトル、ユニット移動・攻撃範囲計算、フェーズ管理、敵AIが核となるジャンル。
シーンを「タイトル」「ステージ選択」「バトル」「リザルト」に分割し、
ScriptableObject によるデータ駆動設計でユニット・スキル・ステージデータを管理する。
バトルフェーズと UI パネルの連動には CanvasGroup を使い、SetActive ではなく alpha + blocksRaycasts で切り替える。
シーン間データ受け渡しには GameKit DataContainer（ScriptableObject）を使い、static クラスへの依存を避ける。

---

## シーン構成

```
Scenes/
  Boot.unity           # 初期化・マスターデータロード
  Title.unity          # タイトル画面
  StageSelect.unity    # ステージ選択マップ
  Battle.unity         # タクティクスバトル（共通）
  Result.unity         # 勝敗リザルト・報酬
```

バトルシーンの GameObject 構成例:

```
[Battle Scene]
  - BattleManager        # フェーズ制御・ターン管理（カスタムスクリプト）
  - GridManager          # グリッドマップ管理
  |   - BattleMap        # GridCell の親オブジェクト
  |       - Cell_0_0
  |       - Cell_0_1
  |       - ...
  - UnitGroup/
  |   - PlayerUnits/
  |   |   - Knight_01
  |   |   - Archer_01
  |   - EnemyUnits/
  |       - Soldier_01
  |       - Mage_01
  - UI/
  |   - Canvas_Battle
  |       - DeploymentPanel     # 配置フェーズ UI
  |       - ActionPanel         # 行動選択（攻撃・スキル・待機）
  |       - StatusPanel         # 選択ユニットのステータス表示
  |       - DamagePreview       # ダメージ予測パネル
  |       - PhaseAnnounce       # 「Player Phase」等の演出テキスト
  |       - EndTurnButton       # ターン終了ボタン
  - CameraRig               # タクティクスカメラ（俯瞰）
  - Audio/
```

---

## 推奨フォルダ構造

```
Assets/
  Scripts/
    Battle/
      BattleManager.cs           # 手動作成: フェーズ状態機械
      BattlePhase.cs             # 手動作成: enum 定義
      CombatCalculator.cs        # 手動作成: ダメージ・命中計算
      DamagePredictor.cs         # 手動作成: 戦闘予測表示
    Grid/
      GridManager.cs             # 手動作成: グリッド生成・管理
      GridCell.cs                # 手動作成: セルデータ（地形・コスト）
      GridPathfinder.cs          # 手動作成: A* 経路探索
      MovementRangeCalculator.cs # 手動作成: BFS 移動範囲計算
    Unit/
      UnitController.cs          # 手動作成: ユニット制御
      UnitStats.cs               # 手動作成: ステータス管理
    AI/
      EnemyAIController.cs       # 手動作成: 敵AI行動決定
      AIBehaviorBase.cs          # 手動作成: AI行動パターン基底
    UI/
      PhaseUIManager.cs          # 手動作成: フェーズ-UI連動
      ActionCommandPanel.cs      # 生成: unity_gamekit_ui (widgetType=command)
      UnitStatusBinding.cs       # 生成: unity_gamekit_ui (widgetType=binding)
      UnitListPanel.cs           # 生成: unity_gamekit_ui (widgetType=list)
  Data/
    Units/
      Unit_Knight.asset          # ScriptableObject: ユニットデータ
      Unit_Archer.asset
      Enemy_Soldier.asset
    Skills/
      Skill_Slash.asset          # ScriptableObject: スキルデータ
    Stages/
      Stage_01.asset             # ScriptableObject: ステージ定義
    CrossScene/
      SelectedStageData.asset    # DataContainer: シーン間データ受け渡し
  Prefabs/
    Units/, Tiles/, FX/
```

---

## 実装ワークフロー (MCPツール使用例付き)

### Step 1: シーン間データ受け渡し (DataContainer)

ステージ選択画面からバトルシーンへのデータ受け渡しに DataContainer を使う。
static クラスを使うとテスタビリティが下がり、シーン単体テストが困難になるため避ける。

```python
# シーン間データ用 DataContainer 作成
unity_gamekit_data(dataType='dataContainer',
    operation='create',
    dataId='SelectedStageData',
    fields=[
        {'name': 'stageId',    'fieldType': 'int',    'defaultValue': 0},
        {'name': 'difficulty', 'fieldType': 'string', 'defaultValue': 'Normal'},
        {'name': 'mapWidth',   'fieldType': 'int',    'defaultValue': 10},
        {'name': 'mapHeight',  'fieldType': 'int',    'defaultValue': 10},
    ],
    resetOnPlay=False,
    assetPath='Assets/Data/CrossScene/SelectedStageData.asset')
unity_compilation_await(operation='await')
```

`assetPath` を指定すると SO アセットも同時作成される。
StageSelect シーンで書き込み、Battle シーンで読み取る。
両シーンの MonoBehaviour に同じ SO アセットを Inspector から参照させる。

### Step 2: グリッドマップ構築

```python
# バトルシーン作成・ロード
unity_scene_crud(operation='create', scenePath='Assets/Scenes/Battle.unity')
unity_scene_crud(operation='load', scenePath='Assets/Scenes/Battle.unity')

# グリッド管理オブジェクト
unity_gameobject_crud(operation='create', name='GridManager')
unity_gameobject_crud(operation='create', name='BattleMap', parentPath='GridManager')

# グリッドセルスクリプト作成
unity_asset_crud(operation='create',
    assetPath='Assets/Scripts/Grid/GridCell.cs',
    content='using UnityEngine;\n\npublic class GridCell : MonoBehaviour\n{\n    public Vector2Int gridPosition;\n    public int movementCost = 1;\n    public int defenseBonus;\n    public int avoidBonus;\n}')

# GridManager スクリプト作成
unity_asset_crud(operation='create',
    assetPath='Assets/Scripts/Grid/GridManager.cs',
    content='using UnityEngine;\n\npublic class GridManager : MonoBehaviour\n{\n    [SerializeField] private int mapWidth = 10;\n    [SerializeField] private int mapHeight = 10;\n}')
unity_compilation_await(operation='await')
```

GridCell の設計ポイント:

```
GridCell:
  - gridPosition: Vector2Int   # グリッド座標
  - terrainType: TerrainType   # 平地/森/山/水/壁
  - movementCost: int          # 移動コスト（平地=1, 森=2, 山=3, 水=99）
  - defenseBonus: int          # 地形防御ボーナス（森=+15, 山=+25）
  - avoidBonus: int            # 地形回避ボーナス
  - occupyingUnit: UnitController  # セル上のユニット（null=空）
```

### Step 3: 移動範囲計算 (BFS)

**重要**: BFS で移動可能範囲を求める際、開始セル（ユニットの現在位置）を必ず結果に含めること。
開始セルを含めないと「移動せずにその場で攻撃」ができなくなる致命的なバグになる。

```
MovementRangeCalculator の正しいパターン:
  1. 開始セルを remainingCost = moveRange でキューに入れる
  2. 開始セルを movableRange に追加する（これを忘れない!）
  3. BFS ループで隣接セルを探索、movementCost を減算
  4. remainingCost >= 0 なら movableRange に追加

誤りパターン（バグ）:
  - 開始セルをキューに入れるが movableRange に追加しない
  - → ユニットが「その場にいる」セルが移動範囲に含まれない
  - → 移動せず攻撃が選べない、行動キャンセルが機能しない
```

### Step 4: A* 経路探索

**重要**: Unity の `Vector2Int` は `IComparable` を実装していないため、
`SortedSet<(int, Vector2Int)>` を使うと実行時エラーが発生する。
List + 線形探索か、独自の Comparer を渡す必要がある。

```
正しいパターン（List + 線形探索）:
  openList = new List<PathNode>()
  最小 fCost のノードを毎回 LINQ Min() または手動ループで取得

  ※ グリッドサイズが 30x30 以下であれば List の線形探索で十分高速。
  ※ 大規模マップ（50x50 超）では PriorityQueue<TElement, TPriority>（.NET 6+）
    または独自 BinaryHeap を検討する。

誤りパターン（コンパイルエラー）:
  SortedSet<(int fCost, Vector2Int position)>
  → ValueTuple の比較で Vector2Int.CompareTo が呼ばれる
  → Vector2Int は IComparable 未実装のため InvalidOperationException
```

### Step 5: バトルフェーズ管理

```python
# BattleManager（フェーズ管理スクリプト）
unity_asset_crud(operation='create',
    assetPath='Assets/Scripts/Battle/BattleManager.cs',
    content='using UnityEngine;\n\npublic class BattleManager : MonoBehaviour\n{\n    private BattlePhase currentPhase;\n}')
unity_compilation_await(operation='await')

# BattlePhase enum 定義スクリプト
unity_asset_crud(operation='create',
    assetPath='Assets/Scripts/Battle/BattlePhase.cs',
    content='public enum BattlePhase\n{\n    Deployment,\n    PlayerIdle,\n    PlayerMove,\n    PlayerAction,\n    EnemyTurn,\n    Result\n}')
unity_compilation_await(operation='await')
```

BattlePhase enum 設計:

```
enum BattlePhase:
  Deployment    # ユニット初期配置フェーズ
  PlayerIdle    # プレイヤーターン：ユニット選択待ち
  PlayerMove    # プレイヤーターン：移動先選択
  PlayerAction  # プレイヤーターン：行動選択（攻撃/スキル/待機）
  EnemyTurn     # 敵ターン：AI行動中
  Result        # 勝敗判定・リザルト
```

### Step 6: バトル UI 構築

```python
# バトル Canvas
unity_ui_foundation(operation='createCanvas', name='Canvas_Battle')

# 各フェーズ用パネル作成（addCanvasGroup=True で CanvasGroup 自動付与）
unity_ui_foundation(operation='createPanel', name='DeploymentPanel', parentPath='Canvas_Battle', addCanvasGroup=True)
unity_ui_foundation(operation='createPanel', name='ActionPanel', parentPath='Canvas_Battle', addCanvasGroup=True)
unity_ui_foundation(operation='createPanel', name='StatusPanel', parentPath='Canvas_Battle', addCanvasGroup=True)
unity_ui_foundation(operation='createPanel', name='DamagePreview', parentPath='Canvas_Battle', addCanvasGroup=True)

# ターン終了ボタン
unity_ui_foundation(operation='createButton', name='EndTurnButton', parentPath='Canvas_Battle', text='ターン終了')

# フェーズ告知テキスト
unity_ui_foundation(operation='createText', name='PhaseAnnounce', parentPath='Canvas_Battle', text='Player Phase', fontSize=48)
```

### Step 7: フェーズ-UI 連動パターン (CanvasGroup)

UI パネルの表示切り替えに `SetActive(true/false)` を使うと、
非アクティブオブジェクトへの参照が切れたり、アニメーション遷移ができない。
代わりに CanvasGroup の alpha / interactable / blocksRaycasts を制御する。

```python
# ui_state でフェーズごとの UI 状態を定義
unity_ui_state(operation='defineState', rootPath='Canvas_Battle', stateName='phase_deployment', elements=[
    {'path': 'DeploymentPanel', 'visible': True, 'interactable': True},
    {'path': 'ActionPanel', 'visible': False},
    {'path': 'StatusPanel', 'visible': False},
    {'path': 'DamagePreview', 'visible': False},
])

unity_ui_state(operation='defineState', rootPath='Canvas_Battle', stateName='phase_player_idle', elements=[
    {'path': 'DeploymentPanel', 'visible': False},
    {'path': 'ActionPanel', 'visible': False},
    {'path': 'StatusPanel', 'visible': True, 'interactable': True},
    {'path': 'DamagePreview', 'visible': False},
])

unity_ui_state(operation='defineState', rootPath='Canvas_Battle', stateName='phase_player_action', elements=[
    {'path': 'DeploymentPanel', 'visible': False},
    {'path': 'ActionPanel', 'visible': True, 'interactable': True},
    {'path': 'StatusPanel', 'visible': True, 'interactable': True},
    {'path': 'DamagePreview', 'visible': True, 'interactable': True},
])

unity_ui_state(operation='defineState', rootPath='Canvas_Battle', stateName='phase_enemy_turn', elements=[
    {'path': 'DeploymentPanel', 'visible': False},
    {'path': 'ActionPanel', 'visible': False},
    {'path': 'StatusPanel', 'visible': True, 'interactable': False},
    {'path': 'DamagePreview', 'visible': False},
])

# フェーズ排他グループ
unity_ui_state(operation='createStateGroup', rootPath='Canvas_Battle', groupName='battle_phase',
    states=['phase_deployment', 'phase_player_idle', 'phase_player_action', 'phase_enemy_turn'],
    defaultState='phase_deployment')

# フェーズ切替
unity_ui_state(operation='applyState', rootPath='Canvas_Battle', stateName='phase_player_idle')
```

BattleManager の `UpdatePhaseUI()` メソッドパターン:

```
void UpdatePhaseUI(BattlePhase phase):
  // まず全パネルを非表示（alpha=0, interactable=false, blocksRaycasts=false）
  HideAllPanels()

  // フェーズに応じて必要なパネルだけ表示
  switch(phase):
    case Deployment:
      ShowPanel(deploymentPanel)
      endTurnButton.interactable = false  // 配置中はターン終了不可
    case PlayerIdle:
      ShowPanel(statusPanel)
      endTurnButton.interactable = true
    case PlayerAction:
      ShowPanel(actionPanel)
      ShowPanel(statusPanel)
      ShowPanel(damagePreview)
      endTurnButton.interactable = false  // 行動中はターン終了不可
    case EnemyTurn:
      ShowPanel(statusPanel)  // 閲覧のみ（interactable=false）
      endTurnButton.interactable = false

  void HideAllPanels():
    foreach panel in allPanels:
      panel.canvasGroup.alpha = 0
      panel.canvasGroup.interactable = false
      panel.canvasGroup.blocksRaycasts = false

  void ShowPanel(panel):
    panel.canvasGroup.alpha = 1
    panel.canvasGroup.interactable = true
    panel.canvasGroup.blocksRaycasts = true
```

### Step 8: 行動コマンド UI

```python
# 行動コマンドパネル（攻撃/スキル/待機）
unity_gamekit_ui(widgetType='command', operation='createCommandPanel',
    panelId='action_cmd',
    parentPath='Canvas_Battle/ActionPanel',
    commands=[
        {'name': 'Attack', 'commandType': 'action', 'label': '攻撃'},
        {'name': 'Skill',  'commandType': 'custom', 'label': 'スキル'},
        {'name': 'Wait',   'commandType': 'action', 'label': '待機'},
    ])
unity_compilation_await(operation='await')

# ユニットステータス表示（事前にText要素を作成）
unity_ui_foundation(operation='createText', name='UnitName', parentPath='Canvas_Battle/StatusPanel', text='')
unity_ui_foundation(operation='createText', name='UnitHP', parentPath='Canvas_Battle/StatusPanel', text='HP: --/--')

unity_gamekit_ui(widgetType='binding', operation='create',
    targetPath='Canvas_Battle/StatusPanel/UnitName',
    bindingId='unit_name', sourceType='custom', format='formatted')
unity_compilation_await(operation='await')

unity_gamekit_ui(widgetType='binding', operation='create',
    targetPath='Canvas_Battle/StatusPanel/UnitHP',
    bindingId='unit_hp', sourceType='health', format='ratio')
unity_compilation_await(operation='await')
```

### Step 9: ユニットデータ (ScriptableObject)

```python
# ユニットデータ
unity_scriptableObject_crud(operation='create',
    typeName='UnitData',
    assetPath='Assets/Data/Units/Unit_Knight.asset',
    properties={
        'unitName': 'Knight',
        'maxHP': 80,
        'attack': 18,
        'defense': 15,
        'magicDefense': 8,
        'speed': 5,
        'moveRange': 4,
        'attackRange': 1,
    })

# ステージデータ
unity_scriptableObject_crud(operation='create',
    typeName='StageData',
    assetPath='Assets/Data/Stages/Stage_01.asset',
    properties={
        'stageName': '草原の戦い',
        'mapWidth': 12,
        'mapHeight': 10,
        'victoryCondition': 'DefeatAllEnemies',
        'turnLimit': 25,
    })
```

### Step 10: カメラ・ライティング

```python
# タクティクス俯瞰カメラ
unity_camera_bundle(operation='create', name='TacticsCamera', preset='topDown')

# 環境ライト
unity_light_bundle(operation='createLightingSetup', setupPreset='daylight')
```

### Step 11: 検証

```python
# シーンビルド登録
unity_projectSettings_crud(operation='addSceneToBuild',
    scenePath='Assets/Scenes/Battle.unity')
unity_projectSettings_crud(operation='addSceneToBuild',
    scenePath='Assets/Scenes/StageSelect.unity')

# 参照整合性チェック
unity_validate_integrity(operation='all')

# シーン遷移確認
unity_scene_relationship_graph(operation='analyzeAll')
```

---

## よくあるパターン

### 戦闘予測計算 (CombatCalculator)

攻撃前にダメージ予測を表示するのは SRPG の基本 UX。
CombatCalculator は以下を算出してプレビューパネルに表示する。

```
CombatCalculator.Predict(attacker, defender, attackerCell, defenderCell):
  // 基本ダメージ
  baseDamage = attacker.attack - defender.defense
  // 地形ボーナス（防御側セルの defenseBonus を適用）
  terrainReduction = defenderCell.defenseBonus
  finalDamage = max(0, baseDamage - terrainReduction)

  // 命中率
  baseHitRate = 90
  avoidBonus = defenderCell.avoidBonus + defender.speed * 2
  hitRate = clamp(baseHitRate - avoidBonus, 0, 100)

  // 反撃可能か（defender の attackRange 内に attacker がいるか）
  canCounter = distance(attackerCell, defenderCell) <= defender.attackRange

  return {finalDamage, hitRate, canCounter, counterDamage}
```

### 敵AI行動パターン

3種の基本 AI パターンで多くのステージをカバーできる。

```
Aggressive（攻撃型）:
  1. 攻撃範囲内に敵がいれば最もHPが低い敵を攻撃
  2. いなければ最も近い敵に向かって移動
  3. 移動後に攻撃範囲に敵がいれば攻撃

Defensive（防御型）:
  1. 自分の初期位置から一定範囲（leashRange）内でのみ行動
  2. 範囲内に敵が来たら攻撃、来なければ待機
  3. 地形防御ボーナスが高いセルを優先

Support（支援型）:
  1. HPが低い味方を探す
  2. 回復スキルの射程内に移動
  3. 回復スキルを使用、対象がいなければ味方の近くで待機
```

AI の移動先決定には Step 4 の A* 経路探索を使う。
ターゲット候補を Score 関数で評価し、最高スコアの行動を選択する。

### ボタン Interactable 制御

フェーズに応じてボタンの `interactable` を制御し、誤操作を防ぐ。

```
EndTurnButton.interactable:
  Deployment  → false（配置完了ボタンは別）
  PlayerIdle  → true （全ユニット行動済みならターン終了可能）
  PlayerMove  → false（移動先選択中）
  PlayerAction → false（行動選択中）
  EnemyTurn   → false（敵ターン中）
```

`unity_event_wiring` で EndTurnButton.onClick と BattleManager.OnEndTurnPressed を接続する:

```python
unity_event_wiring(operation='wire',
    source={'gameObject': 'Canvas_Battle/EndTurnButton', 'component': 'Button', 'event': 'onClick'},
    target={'gameObject': 'BattleManager', 'method': 'OnEndTurnPressed'})
```

---

## 注意点・落とし穴

- **BFS 移動範囲に開始セルを含めること**: 開始セルが含まれないと「移動せず攻撃」ができない。
  SRPG では移動キャンセルやその場での攻撃は必須操作。見落としやすい致命的バグ。
- **A* で SortedSet<(int, Vector2Int)> を使わないこと**: Vector2Int は IComparable 未実装。
  List + 線形探索、または独自 Comparer を持つ SortedSet を使う。
- **UI 切り替えに SetActive を使わないこと**: CanvasGroup の alpha/interactable/blocksRaycasts を使う。
  SetActive(false) にすると GetComponent や Find が失敗し、アニメーション遷移もできない。
- **シーン間データに static クラスを使わないこと**: DataContainer (ScriptableObject) を使えば
  Inspector で確認でき、シーン単体テストも可能。static はリセット漏れバグの温床。
- **GameKit 生成後は必ず** `unity_compilation_await` でコンパイルを待つ。
  コンパイル完了前に次のコンポーネント操作を行うとアタッチに失敗する。
- **カスタムスクリプトの作成**: グリッドロジック・戦闘計算・AI など複雑なロジックは
  `unity_asset_crud` でスクリプトファイルを作成し実装する。
  GameKit は UI に特化しており、ゲームロジックは含まない。
- **CanvasGroup の blocksRaycasts**: alpha=0 にしても blocksRaycasts=true のままだと
  非表示パネルがクリックを吸い込み、背面の UI が操作できなくなる。必ず false にすること。
- **移動コストのバランス**: 水・壁セルの movementCost を 99 にする場合、
  BFS で remainingCost < neighborCost のチェックを入れないと壁を通過するバグが発生する。

---

## 関連ツール一覧

| カテゴリ | ツール名 | 用途 |
|---------|---------|-----|
| データ | `unity_scriptableObject_crud` | ユニット・スキル・ステージデータ |
| データ | `unity_gamekit_data(dataType='dataContainer')` | シーン間データ受け渡し |
| シーン | `unity_scene_crud` | ステージ選択・バトル・リザルト管理 |
| オブジェクト | `unity_gameobject_crud` | グリッド・ユニット・マネージャ配置 |
| コンポーネント | `unity_component_crud` | CanvasGroup・コライダー追加 |
| アセット | `unity_asset_crud` | カスタムスクリプト作成（Grid/AI/Combat） |
| プレハブ | `unity_prefab_crud` | ユニット・タイルプレハブ化 |
| カメラ | `unity_camera_bundle` | タクティクス俯瞰カメラ |
| ライト | `unity_light_bundle` | バトルフィールド照明 |
| UI | `unity_gamekit_ui(widgetType='command')` | 行動コマンドパネル |
| UI | `unity_gamekit_ui(widgetType='binding')` | ステータス表示バインド |
| UI | `unity_gamekit_ui(widgetType='list')` | ユニットリスト・ログ |
| UI基盤 | `unity_ui_foundation` | Canvas・Panel・Button・Text 作成 |
| UI状態 | `unity_ui_state` | フェーズ別 UI 状態定義・切替 |
| イベント | `unity_event_wiring` | ボタン onClick 接続 |
| 設定 | `unity_projectSettings_crud` | ビルドシーン管理 |
| 検証 | `unity_validate_integrity` | 参照切れ検出 |
| 検証 | `unity_scene_relationship_graph` | シーン遷移確認 |
