# Animator Controller 設計ガイド (Unity-AI-Forge {VERSION})

## 概要

Animator Controller はキャラクターの動作状態（待機・移動・攻撃・被弾など）を
ステートマシンで管理する Unity のコアシステムです。
適切に設計されたコントローラーは、デバッグが容易で拡張性が高く、
パフォーマンスにも優れます。

このガイドでは Unity 公式推奨のパターン（Hub-and-Spoke、Blend Tree、
Critical Section、Layer 分離）を解説し、Unity-AI-Forge の
`unity_animation2d_bundle` / `unity_animation3d_bundle` / `unity_gamekit_animation_sync`
を活用した効率的な構築ワークフローを提供します。

---

## 設計パターン

### Hub-and-Spoke パターン

中央に空の Hub State を配置し、各アクション状態へ放射状に遷移する構造。
どの状態からでも Hub を経由して別の状態に移行でき、遷移の流れが明確になります。

```
                ┌─── Idle
                │
  Attack ───── Hub ───── Run
                │
                └─── Jump
```

**メリット:**
- 遷移経路が Hub に集約されデバッグが容易
- 新しい状態の追加が Hub との接続のみで完了
- 各状態は Hub への帰還のみを考えればよい

**実装:**
- Hub State は `Motion` なし（Empty State）
- Hub → 各状態: パラメータ条件で遷移
- 各状態 → Hub: `Exit Time` または完了条件で遷移

### Blend Tree パターン

速度・方向などの連続値を扱う場合、個別ステートの代わりに Blend Tree を使用。
ステート数を大幅に削減し、滑らかなアニメーション遷移を実現します。

```
Locomotion BlendTree
  ├── Idle      (Speed = 0)
  ├── Walk      (Speed = 0.5)
  └── Run       (Speed = 1.0)

Direction BlendTree (2D)
  ├── Front     (X=0, Y=-1)
  ├── Back      (X=0, Y=1)
  ├── Left      (X=-1, Y=0)
  └── Right     (X=1, Y=0)
```

**メリット:**
- Blend Tree はステートを持たないため、遷移のバグが発生しない
- パラメータ値の補間で自然なブレンドが得られる
- コードからの制御が `Animator.SetFloat()` のみで完結

### Critical Section パターン

攻撃モーションなど**中断されてはならない区間**を持つアニメーション向けの構造。
「Intro → Critical (中断不可) → Settle (中断可・Idle復帰)」の3フェーズで構成。

```
Intro → Critical Section → Settle → Hub
         (中断不可)          (中断可)
```

**実装:**
- Intro: アニメーション開始（ヒットボックス有効化等のイベントを配置）
- Critical Section: `CanTransitionToSelf = false`、他ステートへの遷移を無効化
- Settle: 任意のタイミングで Hub へ復帰可能

### Layer 分離パターン

体の部位ごとにレイヤーを分離し、独立したアニメーション制御を実現。

| レイヤー | Weight | Blending | AvatarMask | 用途 |
|---------|--------|----------|------------|------|
| Base Layer | 1.0 | Override | Full Body | 移動・ジャンプ |
| Upper Body | 1.0 | Override | Upper Body | 攻撃・アイテム使用 |
| Face | 0.8 | Additive | Head | 表情 |
| Damage | 0.0→1.0 | Additive | Full Body | 被弾リアクション |

---

## 推奨フォルダ構造

```
Assets/
  Animations/
    Player/
      PlayerAnimator.controller    # AnimatorController
      AvatarMasks/
        UpperBody.mask             # 上半身 AvatarMask
      Clips/
        Idle.anim
        Run.anim
        Jump.anim
        Attack_01.anim
        Attack_02.anim
        Hurt.anim
        Death.anim
      BlendTrees/
        Locomotion.blendtree       # (内部的にControllerに含む)
    Enemy/
      EnemyAnimator.controller
      Clips/
        ...
```

---

## 実装ワークフロー (MCPツール使用例付き)

### Step 1: AnimatorController の作成

```python
# 2D の場合
unity_animation2d_bundle(operation="createController",
    controllerName="PlayerAnimator",
    outputPath="Assets/Animations/Player")

# 3D の場合
unity_animation3d_bundle(operation="createController",
    controllerName="PlayerAnimator",
    outputPath="Assets/Animations/Player")
```

### Step 2: Hub-and-Spoke 構造のステート追加

```python
# Hub State (Empty, デフォルトステート)
unity_animation2d_bundle(operation="addState",
    controllerPath="Assets/Animations/Player/PlayerAnimator.controller",
    stateName="Hub", isDefault=True)

# Idle (Hub から速度0で遷移)
unity_animation2d_bundle(operation="addState",
    controllerPath="Assets/Animations/Player/PlayerAnimator.controller",
    stateName="Idle",
    clipPath="Assets/Animations/Player/Clips/Idle.anim")

# Run
unity_animation2d_bundle(operation="addState",
    controllerPath="Assets/Animations/Player/PlayerAnimator.controller",
    stateName="Run",
    clipPath="Assets/Animations/Player/Clips/Run.anim")

# Jump
unity_animation2d_bundle(operation="addState",
    controllerPath="Assets/Animations/Player/PlayerAnimator.controller",
    stateName="Jump",
    clipPath="Assets/Animations/Player/Clips/Jump.anim")

# Attack
unity_animation2d_bundle(operation="addState",
    controllerPath="Assets/Animations/Player/PlayerAnimator.controller",
    stateName="Attack",
    clipPath="Assets/Animations/Player/Clips/Attack_01.anim")

# Hurt
unity_animation2d_bundle(operation="addState",
    controllerPath="Assets/Animations/Player/PlayerAnimator.controller",
    stateName="Hurt",
    clipPath="Assets/Animations/Player/Clips/Hurt.anim")
```

### Step 3: パラメータとトランジションの設定

```python
# パラメータ追加
unity_animation2d_bundle(operation="addParameter",
    controllerPath="Assets/Animations/Player/PlayerAnimator.controller",
    parameterName="Speed", parameterType="float")
unity_animation2d_bundle(operation="addParameter",
    controllerPath="Assets/Animations/Player/PlayerAnimator.controller",
    parameterName="IsGrounded", parameterType="bool")
unity_animation2d_bundle(operation="addParameter",
    controllerPath="Assets/Animations/Player/PlayerAnimator.controller",
    parameterName="Attack", parameterType="trigger")
unity_animation2d_bundle(operation="addParameter",
    controllerPath="Assets/Animations/Player/PlayerAnimator.controller",
    parameterName="Hurt", parameterType="trigger")

# トランジション: Hub → Idle (Speed < 0.1 & IsGrounded)
unity_animation2d_bundle(operation="addTransition",
    controllerPath="Assets/Animations/Player/PlayerAnimator.controller",
    sourceState="Hub", destinationState="Idle",
    conditions=[
        {"parameter": "Speed", "mode": "less", "threshold": 0.1},
        {"parameter": "IsGrounded", "mode": "true"}
    ], hasExitTime=False)

# Hub → Run (Speed >= 0.1 & IsGrounded)
unity_animation2d_bundle(operation="addTransition",
    controllerPath="Assets/Animations/Player/PlayerAnimator.controller",
    sourceState="Hub", destinationState="Run",
    conditions=[
        {"parameter": "Speed", "mode": "greater", "threshold": 0.1},
        {"parameter": "IsGrounded", "mode": "true"}
    ], hasExitTime=False)

# Hub → Jump (!IsGrounded)
unity_animation2d_bundle(operation="addTransition",
    controllerPath="Assets/Animations/Player/PlayerAnimator.controller",
    sourceState="Hub", destinationState="Jump",
    conditions=[
        {"parameter": "IsGrounded", "mode": "false"}
    ], hasExitTime=False)

# Any State → Attack (Attack trigger)
unity_animation2d_bundle(operation="addTransition",
    controllerPath="Assets/Animations/Player/PlayerAnimator.controller",
    sourceState="Any State", destinationState="Attack",
    conditions=[{"parameter": "Attack", "mode": "trigger"}],
    hasExitTime=False)

# Attack → Hub (Exit Time で自動遷移)
unity_animation2d_bundle(operation="addTransition",
    controllerPath="Assets/Animations/Player/PlayerAnimator.controller",
    sourceState="Attack", destinationState="Hub",
    hasExitTime=True, exitTime=0.9)

# Idle/Run → Hub (常時 Hub 経由で遷移するため)
unity_animation2d_bundle(operation="addTransition",
    controllerPath="Assets/Animations/Player/PlayerAnimator.controller",
    sourceState="Idle", destinationState="Hub",
    conditions=[
        {"parameter": "Speed", "mode": "greater", "threshold": 0.1}
    ], hasExitTime=False)
```

### Step 4: Blend Tree (3D 向け Locomotion)

```python
# 3D キャラクターの移動 BlendTree
unity_animation3d_bundle(operation="createBlendTree",
    controllerPath="Assets/Animations/Player/PlayerAnimator.controller",
    blendTreeName="Locomotion",
    blendType="1D",
    blendParameter="Speed",
    motions=[
        {"clip": "Assets/Animations/Player/Clips/Idle.anim", "threshold": 0.0},
        {"clip": "Assets/Animations/Player/Clips/Walk.anim", "threshold": 0.5},
        {"clip": "Assets/Animations/Player/Clips/Run.anim",  "threshold": 1.0}
    ])

# 2D 方向 BlendTree（トップダウンゲーム用）
unity_animation2d_bundle(operation="createBlendTree",
    controllerPath="Assets/Animations/Player/PlayerAnimator.controller",
    blendTreeName="Movement",
    blendType="2D",
    blendParameterX="MoveX",
    blendParameterY="MoveY",
    motions=[
        {"clip": "Assets/Animations/Player/Clips/Walk_Down.anim",
         "position": {"x": 0, "y": -1}},
        {"clip": "Assets/Animations/Player/Clips/Walk_Up.anim",
         "position": {"x": 0, "y": 1}},
        {"clip": "Assets/Animations/Player/Clips/Walk_Left.anim",
         "position": {"x": -1, "y": 0}},
        {"clip": "Assets/Animations/Player/Clips/Walk_Right.anim",
         "position": {"x": 1, "y": 0}}
    ])
```

### Step 5: Layer 設定 (3D)

```python
# 上半身レイヤーの追加
unity_animation3d_bundle(operation="addLayer",
    controllerPath="Assets/Animations/Player/PlayerAnimator.controller",
    layerName="UpperBody",
    weight=1.0,
    blendingMode="Override",
    avatarMaskPath="Assets/Animations/Player/AvatarMasks/UpperBody.mask")

# 上半身レイヤーに攻撃ステートを追加
unity_animation3d_bundle(operation="addState",
    controllerPath="Assets/Animations/Player/PlayerAnimator.controller",
    layerName="UpperBody",
    stateName="Attack",
    clipPath="Assets/Animations/Player/Clips/Attack_01.anim")
```

### Step 6: GameKit AnimationSync で自動連動

```python
# AnimationSync でパラメータを自動更新
unity_gamekit_animation_sync(operation="create",
    targetPath="Player",
    syncId="player_anim",
    syncSource="rigidbody2d",
    animatorPath="Player")

unity_compilation_await(operation="await")

# 速度 → Speed パラメータ
unity_gamekit_animation_sync(operation="addSyncRule",
    syncId="player_anim",
    parameterName="Speed",
    sourceField="velocity.magnitude")

# Y速度 → VelocityY パラメータ
unity_gamekit_animation_sync(operation="addSyncRule",
    syncId="player_anim",
    parameterName="VelocityY",
    sourceField="velocity.y")

# ダメージイベント → Hurt トリガー
unity_gamekit_animation_sync(operation="addTriggerRule",
    syncId="player_anim",
    triggerName="Hurt",
    eventSource="health",
    eventType="damage")
```

### Step 7: 検証

```python
# Animator Controller の整合性を確認
unity_validate_integrity(operation="all")

# プレイモードでテスト
unity_playmode_control(operation="play")

# ランタイム状態を確認
unity_playmode_control(operation="captureState",
    targets=["Player"], includeConsole=True)

unity_playmode_control(operation="stop")
```

---

## よくあるパターン

### パターン 1: 敵AIの行動ステートマシン

```
Hub → Patrol (速度 > 0 & !targetFound)
Hub → Chase  (targetFound & distance > attackRange)
Hub → Attack (targetFound & distance <= attackRange)
Hub → Hurt   (Hurt trigger)
Hub → Death  (isDead)
```

各ステートから Hub への復帰は ExitTime または条件遷移で実現。
`unity_gamekit_animation_sync` の `syncSource='custom'` で
カスタムスクリプトのフィールドを直接パラメータにバインドできます。

### パターン 2: コンボ攻撃チェーン

```
Attack_01 → Attack_02 (AttackCombo trigger, ExitTime 0.7-0.9)
Attack_02 → Attack_03 (AttackCombo trigger, ExitTime 0.7-0.9)
Attack_03 → Hub       (ExitTime 1.0)
Attack_01 → Hub       (ExitTime 1.0, no combo input)
Attack_02 → Hub       (ExitTime 1.0, no combo input)
```

- `ExitTime` の範囲でのみコンボ入力を受け付ける
- 範囲外は自動的に Hub に復帰
- `unity_gamekit_feedback` でヒットストップ・画面振動を各攻撃に追加

### パターン 3: Additive Layer でダメージリアクション

```python
# Damage レイヤー（通常 Weight=0、被弾時に一時的に 1.0）
unity_animation3d_bundle(operation="addLayer",
    controllerPath="Assets/Animations/Player/PlayerAnimator.controller",
    layerName="Damage",
    weight=0.0,
    blendingMode="Additive")
```

スクリプトから `animator.SetLayerWeight(damageLayerIndex, 1.0f)` を設定し、
一定時間後に 0 に戻す。Base Layer のモーションを中断せずにリアクションを重ねられる。

---

## 注意点・落とし穴

1. **パラメータ名の完全一致**
   `animation_sync` の `parameterName` は Animator Controller 内のパラメータ名と
   大文字・小文字を含め完全一致が必要。不一致時はサイレントに無視される。

2. **Any State トランジションの過剰使用**
   Any State からの遷移は全ステートに適用されるため、意図しないタイミングで
   発火しやすい。`canTransitionToSelf = false` を設定し、必要最小限に留める。

3. **Exit Time の罠**
   `hasExitTime = true` の遷移は条件を満たしていてもモーション完了まで待つ。
   即座に遷移したい場合は `hasExitTime = false` を明示すること。

4. **Root Motion の干渉**
   `applyRootMotion = true` の Animator は Transform 移動をアニメーションが制御する。
   スクリプトでの移動と競合する場合は `applyRootMotion = false` に設定する。

5. **Blend Tree 内のステート遷移**
   Blend Tree はステートを持たないため、State Machine Behaviour を使えない。
   状態変化の検出が必要な場合は Blend Tree を使わず個別ステートを使用する。

6. **レイヤーの処理負荷**
   レイヤーが増えると評価コストが線形に増加する。3-4レイヤー以内に収めること。
   空のステートでもレイヤーが有効なら処理が走る。未使用時は `weight = 0` に設定。

---

## 関連ツール一覧

| ツール名 | 用途 |
|---|---|
| `unity_animation2d_bundle` | 2D AnimatorController 作成・ステート/パラメータ/トランジション追加 |
| `unity_animation3d_bundle` | 3D AnimatorController 作成・BlendTree・Layer・AvatarMask |
| `unity_gamekit_animation_sync` | Animator パラメータの自動同期（速度・接地判定等） |
| `unity_gamekit_feedback` | ヒットストップ・画面振動（攻撃ステートと連動） |
| `unity_component_crud` | Animator コンポーネントの追加・設定 |
| `unity_asset_crud` | カスタム State Machine Behaviour スクリプト生成 |
| `unity_compilation_await` | スクリプト生成後のコンパイル完了待ち |
| `unity_validate_integrity` | Animator 参照の整合性チェック |
| `unity_playmode_control` | アニメーション動作のテスト・状態取得 |
| `unity_console_log` | アニメーション警告・エラーの確認 |
