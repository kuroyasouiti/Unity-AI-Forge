# ステートマシン実装ガイド (Unity-AI-Forge {VERSION})

## 概要

ステートマシンはゲームオブジェクトの振る舞いを明確な「状態」として管理するパターンです。
プレイヤーの動作状態（待機/移動/ジャンプ/攻撃）やゲーム全体のフロー（タイトル/インゲーム/ポーズ/ゲームオーバー）など、
あらゆる「状態遷移」を伴うシステムに適用できます。

Unity-AI-Forge では `unity_asset_crud` で C# スクリプトを生成し、
`unity_gamekit_animation_sync` でアニメーション連動、`unity_gamekit_feedback` でエフェクト付きの
本格的なステートマシンを素早く構築できます。

---

## 設計パターン

### IState インターフェースパターン

```
interface IState {
    void Enter()   // 状態に入ったときの初期化処理
    void Update()  // 毎フレーム呼ばれる更新処理
    void Exit()    // 状態を抜けるときのクリーンアップ
}
```

### ステートマシン構成要素

- **Context (StateMachine)**: 現在の状態を保持し、遷移を管理するクラス
- **ConcreteState**: IState を実装した各状態クラス
- **Trigger**: 遷移条件（入力、タイマー、HP変化など）
- **AnimationSync**: Animator パラメータと状態を同期するコンポーネント

### プレイヤー状態の例

```
PlayerIdleState  -> [移動入力]     -> PlayerRunState
PlayerRunState   -> [停止]         -> PlayerIdleState
PlayerRunState   -> [ジャンプ入力] -> PlayerJumpState
PlayerJumpState  -> [着地]         -> PlayerIdleState
PlayerIdleState  -> [攻撃入力]     -> PlayerAttackState
PlayerAttackState-> [攻撃終了]     -> PlayerIdleState
```

### ゲームフロー状態の例

```
TitleState    -> [ゲーム開始]  -> InGameState
InGameState   -> [ポーズ入力]  -> PauseState
PauseState    -> [再開]        -> InGameState
InGameState   -> [死亡/クリア] -> GameOverState / ResultState
GameOverState -> [リトライ]    -> InGameState
```

---

## 推奨フォルダ構造

```
Assets/
  Scripts/
    StateMachine/
      Core/
        IState.cs              # インターフェース定義
        StateMachine.cs        # 汎用ステートマシン基底クラス
      Player/
        PlayerStateMachine.cs  # プレイヤー用ステートマシン
        States/
          PlayerIdleState.cs
          PlayerRunState.cs
          PlayerJumpState.cs
          PlayerAttackState.cs
      GameFlow/
        GameFlowManager.cs     # ゲームフロー管理シングルトン
        States/
          TitleState.cs
          InGameState.cs
          PauseState.cs
          GameOverState.cs
```

---

## 実装ワークフロー (MCPツール使用例付き)

### Step 1: ステートマシン基盤スクリプトの作成

```python
# IState インターフェースを作成
unity_asset_crud(
    operation="create",
    assetPath="Assets/Scripts/StateMachine/Core/IState.cs",
    content="""using UnityEngine;
public interface IState {
    void Enter();
    void Execute();
    void Exit();
}"""
)

# 汎用ステートマシンクラスを作成
unity_asset_crud(
    operation="create",
    assetPath="Assets/Scripts/StateMachine/Core/StateMachineBase.cs",
    content="""using UnityEngine;
public class StateMachineBase : MonoBehaviour {
    private IState _currentState;
    public IState CurrentState => _currentState;
    public void ChangeState(IState newState) {
        _currentState?.Exit();
        _currentState = newState;
        _currentState?.Enter();
    }
    private void Update() { _currentState?.Execute(); }
}"""
)

# コンパイル待ち（スクリプト作成後は必須）
unity_compilation_await(operation="await", timeoutSeconds=30)
```

### Step 2: Player GameObject のセットアップ

```python
# プレイヤー GameObject を作成
unity_gameobject_crud(
    operation="create",
    name="Player",
    tag="Player"
)

# CharacterController を追加（物理移動用）
unity_component_crud(
    operation="add",
    gameObjectPath="Player",
    componentType="CharacterController",
    propertyChanges={"height": 1.8, "radius": 0.4, "center": {"x": 0, "y": 0.9, "z": 0},
        "slopeLimit": 45, "stepOffset": 0.3}
)

# 入力プロファイルを設定
unity_input_profile(
    operation="createPlayerInput",
    targetPath="Player",
    actionMapName="Player",
    actions=[
        {"name": "Move",   "type": "Value",  "binding": "<Gamepad>/leftStick"},
        {"name": "Jump",   "type": "Button", "binding": "<Keyboard>/space"},
        {"name": "Attack", "type": "Button", "binding": "<Keyboard>/z"}
    ]
)
```

### Step 3: アニメーション同期のセットアップ

```python
# AnimationSync コンポーネントを生成・アタッチ
unity_gamekit_animation_sync(
    operation="create",
    targetPath="Player",
    syncId="player_anim_sync",
    syncRules=[
        {"parameter": "Speed",      "parameterType": "float",
         "sourceType": "rigidbody3d", "sourceProperty": "velocity.magnitude"},
        {"parameter": "IsGrounded", "parameterType": "bool",
         "sourceType": "custom",     "boolThreshold": 0.1}
    ],
    triggers=[
        {"triggerName": "Attack", "eventSource": "input", "inputAction": "Attack"},
        {"triggerName": "Jump",   "eventSource": "input", "inputAction": "Jump"}
    ]
)

# コンパイル待ち（コード生成後は必須）
unity_compilation_await(operation="await", timeoutSeconds=30)
```

### Step 4: フィードバックの設定

```python
# 攻撃ヒット時のフィードバック
unity_gamekit_feedback(
    operation="create",
    targetPath="Player",
    feedbackId="player_attack_feedback",
    components=[
        {"type": "screenShake", "intensity": 0.3, "duration": 0.2},
        {"type": "hitstop",     "duration": 0.05, "hitstopTimeScale": 0},
        {"type": "sound",       "soundVolume": 0.8}
    ]
)

# コンパイル待ち
unity_compilation_await(operation="await", timeoutSeconds=30)
```

### Step 5: ゲームフローの UI 制御

```python
# GameFlowManager GameObject を作成
unity_gameobject_crud(operation="create", name="GameFlowManager")

# UICommand で各状態の UI パネルを制御
unity_gamekit_ui_command(
    operation="createCommandPanel",
    panelId="gameflow_ui_cmd",
    parentPath="GameFlowManager",
    commands=[
        {"name": "ShowTitle",  "label": "タイトル表示", "commandType": "custom"},
        {"name": "HideTitle",  "label": "タイトル非表示", "commandType": "custom"},
        {"name": "ShowHUD",    "label": "HUD表示",     "commandType": "custom"},
        {"name": "ShowPause",  "label": "ポーズ表示",   "commandType": "custom"},
        {"name": "HidePause",  "label": "ポーズ非表示", "commandType": "custom"}
    ]
)

# コンパイル待ち
unity_compilation_await(operation="await", timeoutSeconds=30)
```

### Step 6: シーン整合性の確認

```python
# 実装後に整合性チェック
unity_validate_integrity(operation="all")

# 参照関係の可視化
unity_scene_reference_graph(operation="analyzeScene", format="summary")
```

---

## よくあるパターン

### パターン 1: ScriptableObject ベースの状態定義

各状態を ScriptableObject として定義し、データ駆動型にする設計パターン。

```python
# 状態設定データを作成
unity_asset_crud(
    operation="create",
    assetPath="Assets/Scripts/StateMachine/Core/StateConfig.cs",
    content="""using UnityEngine;
[CreateAssetMenu(fileName = \"NewState\", menuName = \"Game/StateConfig\")]
public class StateConfig : ScriptableObject {
    public string stateName;
    public float moveSpeed;
    public bool canAttack;
    public bool canJump;
}"""
)

unity_compilation_await(operation="await", timeoutSeconds=30)

# 具体的な状態データを ScriptableObject で作成
unity_scriptableObject_crud(
    operation="create",
    typeName="StateConfig",
    assetPath="Assets/Data/States/IdleState.asset",
    properties={"stateName": "Idle", "moveSpeed": 0, "canAttack": true, "canJump": true}
)
```

### パターン 2: 階層型ステートマシン (HSM)

攻撃状態をさらに「通常攻撃」「特殊攻撃」「チャージ攻撃」に分割する場合、
子ステートを持つ階層型が有効です。

```
PlayerAttackState (親)
  +-- NormalAttackState
  +-- SpecialAttackState
  +-- ChargeAttackState
```

### パターン 3: ボスの複数フェーズ

ボス戦は HP しきい値で状態が変わる多段階ステートマシンが効果的です。

```
BossPhase1State [HP > 70%]  -> 通常攻撃パターン
BossPhase2State [HP 30-70%] -> 強化攻撃パターン
BossPhase3State [HP < 30%]  -> ランページパターン
BossDeathState              -> 死亡演出
```

---

## 注意点・落とし穴

1. **コード生成後の忘れがちなコンパイル待ち**
   `unity_asset_crud` でスクリプトを作成した後、および `unity_gamekit_animation_sync` 等の
   コード生成ツール使用後は、必ず `unity_compilation_await` を呼んでから次の操作を行うこと。

2. **Update ループ内での状態遷移の無限ループ**
   Enter() から即座に Exit() を呼ぶような状態を避けること。
   必ずフレームをまたぐか、フラグで再入防止を実装する。

3. **AnimatorController との整合性**
   `unity_gamekit_animation_sync` のパラメータ名は Animator Controller 内のパラメータ名と
   完全一致させること。大文字・小文字の違いでバグが発生しやすい。

4. **DontDestroyOnLoad でのゲームフロー管理**
   GameFlowManager をシーンをまたいで使う場合は DontDestroyOnLoad を設定し、
   `unity_scene_crud` でのシーン遷移時に重複しないよう管理する。

5. **シリアライズ問題**
   ステートを MonoBehaviour フィールドに保持する場合、インターフェース型は
   Unity のインスペクターでシリアライズできない。ScriptableObject パターンを検討する。

---

## 関連ツール一覧

| ツール名 | 用途 |
|---|---|
| `unity_asset_crud` | ステートマシンの C# スクリプト作成 |
| `unity_gameobject_crud` | ステートマシンホスト GameObject の作成 |
| `unity_component_crud` | カスタムコンポーネントの追加・設定 |
| `unity_scriptableObject_crud` | 状態設定データの ScriptableObject 作成 |
| `unity_gamekit_animation_sync` | Animator パラメータと状態の同期 |
| `unity_gamekit_feedback` | 状態遷移時のスクリーンシェイク・ヒットストップ |
| `unity_gamekit_ui_command` | 状態に応じた UI パネルの表示/非表示 |
| `unity_input_profile` | 入力アクションの定義（遷移トリガー） |
| `unity_component_crud` (CharacterController) | プレイヤー移動の物理基盤 |
| `unity_compilation_await` | スクリプト生成後のコンパイル完了待ち |
| `unity_validate_integrity` | 実装後の整合性チェック |
| `unity_scene_reference_graph` | 参照関係の確認・可視化 |
| `unity_playmode_control` | テスト実行・停止 |
| `unity_console_log` | デバッグログの確認 |
