# ScriptableObject イベントチャネル実装ガイド (Unity-AI-Forge {VERSION})

## 概要

イベントチャネルは ScriptableObject をイベントバスとして使い、
システム間の通信を疎結合に実現する Unity 公式推奨のアーキテクチャパターンです。

従来の C# `event`/`Action` はクラス間に直接の参照依存を生み、
`UnityEvent` はインスペクタで接続するためシーンに閉じます。
ScriptableObject ベースのイベントチャネルはアセットとして存在するため、
シーンをまたいだ通知、エディタ上での接続可視化、テスト容易性を同時に実現します。

Unity-AI-Forge では `unity_asset_crud` でイベントチャネルの C# スクリプトを生成し、
`unity_scriptableObject_crud` でチャネルアセットを作成、
`unity_event_wiring` でリスナーを接続できます。

---

## 設計パターン

### 3つの構成要素

```
EventChannel<T> (ScriptableObject)
  +-- event Action<T> OnEventRaised
  +-- Raise(T value)       # ブロードキャスト

EventRaiser<T> (MonoBehaviour)
  +-- EventChannel<T> channel   # Inspector で接続
  +-- RaiseEvent(T value)       # チャネルに通知

EventListener<T> (MonoBehaviour)
  +-- EventChannel<T> channel   # Inspector で接続
  +-- UnityEvent<T> response    # レスポンスアクション
  +-- OnEnable()  → channel に登録
  +-- OnDisable() → channel から解除
```

### 通信フローの比較

| 方式 | 結合度 | シーン横断 | Inspector 可視 | テスト容易性 |
|------|-------|-----------|---------------|------------|
| 直接参照 (`GetComponent`) | 強 | 不可 | 不可 | 低 |
| C# `event`/`Action` | 中 | 不可 | 不可 | 中 |
| `UnityEvent` | 中 | 不可 | 可 | 中 |
| **SO Event Channel** | **弱** | **可** | **可** | **高** |

### 用途別チャネル型

| 型 | 用途例 |
|---|---|
| `VoidEventChannel` | ゲーム開始、ポーズ、リトライなどの通知 |
| `IntEventChannel` | スコア変化、ダメージ値 |
| `FloatEventChannel` | HP割合、タイマー |
| `StringEventChannel` | ダイアログテキスト、通知メッセージ |
| `Vector3EventChannel` | スポーン位置、移動先座標 |
| `GameObjectEventChannel` | 敵撃破通知、アイテム取得通知 |

---

## 推奨フォルダ構造

```
Assets/
  Scripts/
    Events/
      Core/
        VoidEventChannel.cs        # 引数なしイベント
        IntEventChannel.cs         # int 引数イベント
        FloatEventChannel.cs       # float 引数イベント
        GenericEventChannel.cs     # ジェネリック基底（オプション）
      Listeners/
        VoidEventListener.cs       # 汎用リスナー
        IntEventListener.cs
        FloatEventListener.cs
  Data/
    Events/
      OnGameStart.asset            # VoidEventChannel
      OnScoreChanged.asset         # IntEventChannel
      OnPlayerHealthChanged.asset  # FloatEventChannel
      OnEnemyDefeated.asset        # GameObjectEventChannel
```

---

## 実装ワークフロー (MCPツール使用例付き)

### Step 1: VoidEventChannel の作成

```python
# 引数なしイベントチャネル
unity_asset_crud(
    operation="create",
    assetPath="Assets/Scripts/Events/Core/VoidEventChannel.cs",
    content="""using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Events/Void Event Channel")]
public class VoidEventChannel : ScriptableObject
{
    public event Action OnEventRaised;

    public void Raise()
    {
        OnEventRaised?.Invoke();
    }
}"""
)

unity_compilation_await(operation="await", timeoutSeconds=30)
```

### Step 2: 型付きイベントチャネルの作成

```python
# int 引数イベントチャネル（スコア、ダメージ等）
unity_asset_crud(
    operation="create",
    assetPath="Assets/Scripts/Events/Core/IntEventChannel.cs",
    content="""using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Events/Int Event Channel")]
public class IntEventChannel : ScriptableObject
{
    public event Action<int> OnEventRaised;

    public void Raise(int value)
    {
        OnEventRaised?.Invoke(value);
    }
}"""
)

# float 引数イベントチャネル（HP、タイマー等）
unity_asset_crud(
    operation="create",
    assetPath="Assets/Scripts/Events/Core/FloatEventChannel.cs",
    content="""using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Events/Float Event Channel")]
public class FloatEventChannel : ScriptableObject
{
    public event Action<float> OnEventRaised;

    public void Raise(float value)
    {
        OnEventRaised?.Invoke(value);
    }
}"""
)

unity_compilation_await(operation="await", timeoutSeconds=30)
```

### Step 3: 汎用リスナーコンポーネントの作成

```python
# VoidEventListener — Inspector でレスポンスを接続可能
unity_asset_crud(
    operation="create",
    assetPath="Assets/Scripts/Events/Listeners/VoidEventListener.cs",
    content="""using UnityEngine;
using UnityEngine.Events;

public class VoidEventListener : MonoBehaviour
{
    [SerializeField] private VoidEventChannel channel;
    [SerializeField] private UnityEvent response;

    void OnEnable()
    {
        if (channel != null)
            channel.OnEventRaised += OnEventRaised;
    }

    void OnDisable()
    {
        if (channel != null)
            channel.OnEventRaised -= OnEventRaised;
    }

    void OnEventRaised()
    {
        response?.Invoke();
    }
}"""
)

# IntEventListener
unity_asset_crud(
    operation="create",
    assetPath="Assets/Scripts/Events/Listeners/IntEventListener.cs",
    content="""using UnityEngine;
using UnityEngine.Events;

public class IntEventListener : MonoBehaviour
{
    [SerializeField] private IntEventChannel channel;
    [SerializeField] private UnityEvent<int> response;

    void OnEnable()
    {
        if (channel != null)
            channel.OnEventRaised += OnEventRaised;
    }

    void OnDisable()
    {
        if (channel != null)
            channel.OnEventRaised -= OnEventRaised;
    }

    void OnEventRaised(int value)
    {
        response?.Invoke(value);
    }
}"""
)

unity_compilation_await(operation="await", timeoutSeconds=30)
```

### Step 4: チャネルアセットの作成

```python
# ゲーム開始イベント
unity_scriptableObject_crud(
    operation="create",
    typeName="VoidEventChannel",
    assetPath="Assets/Data/Events/OnGameStart.asset"
)

# スコア変化イベント
unity_scriptableObject_crud(
    operation="create",
    typeName="IntEventChannel",
    assetPath="Assets/Data/Events/OnScoreChanged.asset"
)

# プレイヤーHP変化イベント
unity_scriptableObject_crud(
    operation="create",
    typeName="FloatEventChannel",
    assetPath="Assets/Data/Events/OnPlayerHealthChanged.asset"
)
```

### Step 5: リスナーの配置とイベント接続

```python
# HUD にリスナーを追加（スコア変化を受信 → テキスト更新）
unity_component_crud(operation="add",
    gameObjectPath="Canvas_HUD/ScoreText",
    componentType="IntEventListener",
    propertyChanges={
        'channel': {'$ref': 'Assets/Data/Events/OnScoreChanged.asset'}
    })

# UnityEvent でレスポンスを接続
unity_event_wiring(operation="wire",
    source={'gameObject': 'Canvas_HUD/ScoreText',
            'component': 'IntEventListener', 'event': 'response'},
    target={'gameObject': 'Canvas_HUD/ScoreText',
            'method': 'SetText'})
```

### Step 6: GameKit UIBinding との連携

```python
# UIBinding でも同様のデータバインドが可能
# イベントチャネルは「多対多の通知」、UIBinding は「1対1の表示更新」
# 使い分け:
#   - HP バーの表示更新 → unity_gamekit_ui_binding (シンプル)
#   - HP 変化で複数システムに通知 → EventChannel (柔軟)

unity_gamekit_ui_binding(operation="create",
    targetPath="Canvas_HUD/HPBar",
    bindingId="player_hp",
    sourceType="health", sourceId="player_hp",
    format="percent")
unity_compilation_await(operation="await")
```

---

## よくあるパターン

### パターン 1: ゲームフロー制御

```
VoidEventChannel: OnGameStart
VoidEventChannel: OnGamePause
VoidEventChannel: OnGameResume
VoidEventChannel: OnGameOver

GameManager.StartGame() → OnGameStart.Raise()
  → 各システムの VoidEventListener が受信
  → EnemySpawner: スポーン開始
  → BGMManager: BGM 再生
  → HUD: 表示開始
```

### パターン 2: HP → 複数システム通知

```
FloatEventChannel: OnPlayerHealthChanged

PlayerHealth.TakeDamage() → OnPlayerHealthChanged.Raise(currentHP / maxHP)
  → HPBar (FloatEventListener): バー更新
  → PostProcess (FloatEventListener): 低HP時に赤フィルター
  → AudioManager (FloatEventListener): 心拍音再生
  → CameraRig (FloatEventListener): 低HP時に画面揺れ
```

### パターン 3: デバッグ用イベントロガー

```python
# 全イベントチャネルを監視するデバッグ用リスナー
unity_asset_crud(
    operation="create",
    assetPath="Assets/Scripts/Events/Debug/EventLogger.cs",
    content="""using UnityEngine;

public class EventLogger : MonoBehaviour
{
    [SerializeField] private VoidEventChannel[] voidChannels;
    [SerializeField] private IntEventChannel[] intChannels;

    void OnEnable()
    {
        foreach (var ch in voidChannels)
            ch.OnEventRaised += () => Debug.Log($\"[Event] {ch.name} raised\");
        foreach (var ch in intChannels)
            ch.OnEventRaised += v => Debug.Log($\"[Event] {ch.name} raised: {v}\");
    }
}"""
)
```

---

## C# event/Action との使い分け

| 用途 | 推奨方式 |
|------|---------|
| 同一 GameObject 内の通知 | C# `event` / `Action` |
| 親子関係のある GameObject 間 | C# `event` or `UnityEvent` |
| シーン内の疎結合な通知 | **SO EventChannel** |
| シーンをまたぐ通知 | **SO EventChannel** |
| デザイナーが接続を編集 | **SO EventChannel** or `UnityEvent` |
| パフォーマンスクリティカル (毎フレーム) | C# `event` (GC-free) |

---

## 注意点・落とし穴

1. **OnDisable での解除忘れ**
   `OnEnable()` で登録したら必ず `OnDisable()` で解除すること。
   解除忘れは破棄済みオブジェクトへの参照によるエラーの原因となる。

2. **ScriptableObject のライフサイクル**
   SO はシーン遷移で破棄されない。エディタ上ではプレイモード終了時にリセットされるが、
   ビルドではアプリケーション終了まで永続する。登録済みリスナーが残らないよう注意。

3. **実行順序の保証なし**
   複数リスナーが同一チャネルに登録されている場合、呼び出し順序は保証されない。
   順序依存のロジックはイベントチャネルに載せず、マネージャーで制御すること。

4. **過度な細分化**
   チャネルを作りすぎるとアセット管理が煩雑になる。
   1つのシステムに1-3チャネル程度を目安にし、関連イベントはまとめる。

5. **ジェネリック SO の制限**
   Unity は `ScriptableObject<T>` のジェネリック直接シリアライズをサポートしない。
   型ごとに具体クラス（`IntEventChannel`, `FloatEventChannel`）を作る必要がある。

6. **パフォーマンス**
   毎フレーム発火するイベントには SO チャネルを使わないこと。
   `Invoke` のオーバーヘッドは C# event より大きい。
   位置同期やアニメーション更新には直接参照か `unity_gamekit_animation_sync` を使用。

---

## 関連ツール一覧

| ツール名 | 用途 |
|---|---|
| `unity_asset_crud` | EventChannel・Listener スクリプトの生成 |
| `unity_scriptableObject_crud` | チャネルアセットの作成 |
| `unity_component_crud` | Listener コンポーネントの追加・設定 |
| `unity_event_wiring` | UnityEvent レスポンスの接続 |
| `unity_gamekit_ui_binding` | 1対1 UI データバインド（チャネルとの使い分け） |
| `unity_gamekit_ui_command` | UI コマンドパネル（イベント駆動 UI） |
| `unity_compilation_await` | スクリプト生成後のコンパイル完了待ち |
| `unity_class_dependency_graph` | イベント経由の依存関係可視化 |
| `unity_scene_reference_graph` | SO チャネル参照の確認 |
| `unity_validate_integrity` | 実装後の整合性チェック |
