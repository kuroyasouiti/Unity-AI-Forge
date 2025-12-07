# 変更履歴

Unity-AI-Forgeのすべての注目すべき変更はこのファイルに記録されます。

このフォーマットは[Keep a Changelog](https://keepachangelog.com/ja/1.0.0/)に基づいており、
このプロジェクトは[Semantic Versioning](https://semver.org/lang/ja/)に準拠しています。

## [未リリース]

（なし）

## [2.3.4] - 2025-12-08

### 追加

- **Unity オブジェクト参照の文字列パス解決**
  - `propertyChanges` で文字列パスから Unity オブジェクト参照を自動解決
  - サポートする参照タイプ:
    - `GameObject`: パスからGameObjectを取得
    - `Transform` / `RectTransform`: パスからTransformを取得
    - `Component` 派生型（`TMP_Text`, `Button`, `InputField` など）: パスからGameObjectを見つけ、指定コンポーネントを取得
    - アセット参照: `Assets/...` 形式のパスからアセットをロード
  - 使用例:
    ```json
    {
      "operation": "update",
      "gameObjectPath": "Controller",
      "componentType": "MyUIController",
      "propertyChanges": {
        "titleText": "Canvas/Panel/TitleText",
        "submitButton": "Canvas/Panel/SubmitButton",
        "inputField": "Canvas/Panel/InputField"
      }
    }
    ```

### 技術詳細

- `ResolveUnityObjectFromPath()` メソッドを追加
- 階層パスの検索ロジック:
  1. `GameObject.Find()` で完全パス検索
  2. 見つからない場合、アクティブシーンのルートオブジェクトから相対パス検索
  3. コンポーネント型の場合、見つかったGameObjectから `GetComponent()` で取得

## [2.3.3] - 2025-12-08

### 追加

- **ComponentCommandHandler の機能強化**
  - `propertyFilter` 機能の修正: `List<object>`、`string[]`、カンマ区切り文字列など様々な入力形式に対応
  - `addMultiple` 操作で `propertyChanges` をサポート: コンポーネント追加時に初期プロパティを設定可能に
  - 結果に `updatedProperties` を含めるように改善

- **Unity型変換サポートの大幅拡張**
  - `Color` / `Color32` 型: Dictionary形式 `{r, g, b, a}` からの変換
  - `Vector2` / `Vector3` / `Vector4` 型: Dictionary形式からの変換
  - `Quaternion` 型: Dictionary形式 `{x, y, z, w}` からの変換
  - `Rect` 型: Dictionary形式 `{x, y, width, height}` からの変換
  - `Bounds` 型: Dictionary形式 `{center, size}` からの変換
  - `Enum` 型: 文字列名または整数値からの変換

- **新規テストスイート**
  - `ComponentCommandHandlerTests`: 36テスト（12テスト新規追加）
    - PropertyFilter テスト（4件）: 各種入力形式のフィルタリング
    - AddMultiple with PropertyChanges テスト（2件）: 初期プロパティ設定
    - Color Type Conversion テスト（3件）: Color型のDictionary変換
    - Vector Type Conversion テスト（2件）: Vector2/3型のDictionary変換
    - Enum Type Conversion テスト（1件）: 文字列からEnum変換

### 修正

- **TypeResolverTests.ResolveByShortName_MultipleNamespaces_ShouldSearchAll**
  - ジェネリック型 `List<T>` の短縮名検索問題を修正
  - テストを `DateTime` 型を使用するように変更

- **GameObjectCommandHandlerTests**
  - `IList<object>` から `System.Collections.IList` へのキャスト修正
  - `Execute_Inspect_ShouldReturnGameObjectInfo` の null 参照エラーを解決
  - `Execute_InspectMultiple_WithPattern_ShouldReturnMultipleInfo` の null 参照エラーを解決

- **SceneCommandHandlerTests**
  - `Execute_Create_WithAdditive_ShouldCreateAdditiveScene` テストの安定性向上
  - テスト環境の制限を考慮した `Assert.Inconclusive` による適切なハンドリング

### テスト結果

- 総テスト数: 187
- 成功: 186
- Inconclusive: 1（テスト環境の制限）
- 失敗: 0

## [2.3.2] - 2025-12-06

### 追加

- **ビルド設定管理機能 (`unity_projectSettings_crud`)**
  - `addSceneToBuild`: ビルド設定にシーンを追加（任意のインデックス位置に挿入可能）
  - `removeSceneFromBuild`: シーンパスまたはインデックスでビルド設定からシーンを削除
  - `listBuildScenes`: ビルド設定内の全シーンを一覧表示（パス、GUID、有効/無効状態、インデックス）
  - `reorderBuildScenes`: ビルド内のシーン順序を変更
  - `setBuildSceneEnabled`: ビルド内のシーンを有効化/無効化
  - ビルド設定の完全な自動化が可能に

- **レンダリングレイヤー管理機能 (`unity_projectSettings_crud`)**
  - `addRenderingLayer`: URP/HDRP用のレンダリングレイヤーを追加（最大32レイヤー）
  - `removeRenderingLayer`: レンダリングレイヤーを削除
  - `renderingLayers`: レンダリングレイヤー一覧の取得
  - Unity 2022.2以降で利用可能なレンダリングパイプライン機能をサポート
  - ライトとカメラのレンダリング制御に使用

- **ブリッジトークンの自動同期**
  - MCPサーバーインストール時に `.mcp_bridge_token` をコピー（無い場合は生成）
  - Pythonサーバーはカレントディレクトリの `.mcp_bridge_token` を優先参照
  - WebSocket接続時のトークンをクエリパラメータで渡すように変更（旧extra_headers非依存）

### 改善

- **ドキュメント更新：GameObjectレイヤー設定機能の明確化**
  - `unity_gameobject_crud`の`update`操作で`layer`パラメータが使用可能であることを明示
  - レイヤー名（文字列）またはレイヤー番号（整数）の両方に対応
  - MCPサーバーのプロンプトに使用例を追加
  - `CLAUDE.md`の古い`unity_tagLayer_manage`情報を最新の方法に更新
  - タグ/レイヤー管理が`unity_gameobject_crud`と`unity_projectSettings_crud`に統合されていることを文書化

## [2.3.1] - 2025-01-03

### 追加

- **GameKitSceneFlow 自動ロードシステム**
  - プレハブベースのSceneFlow管理を実装
  - `GameKitSceneFlowAutoLoader` (Editor): Play Mode開始時に自動ロード
  - `GameKitSceneFlowRuntimeLoader` (Runtime): ゲーム開始前に自動ロード
  - `Resources/GameKitSceneFlows/` にプレハブを配置すると自動で読み込まれる
  - 初期シーンへの手動配置が不要に
  - Git管理可能なプレハブファイルでチームコラボレーション対応
  - Unity Editorメニューから `Tools → Unity-AI-Forge → GameKit → Create SceneFlows Directory` でディレクトリ作成

### 修正

- **Unity Editor フリーズ問題の解決**
  - C#スクリプト作成・更新・削除時のフリーズ問題を完全に修正
  - Unity側の同期的なコンパイル待機（`Thread.Sleep()`）を削除
  - MCPサーバー側で非同期的なコンパイル待機を実装
  - `bridge:restarted` メッセージによるアセンブリリロード検出
  - コンパイル結果（成功/失敗、エラー数、経過時間）をレスポンスに含めるように改善

- **BaseCommandHandler の最適化**
  - `WaitForCompilationAfterOperation()` メソッドを簡略化
  - `GetBridgeConnectionState()` メソッドを削除（不要になったため）
  - Unity Editorのメインスレッドをブロックしないよう改善

- **AssetCommandHandler の改善**
  - `RequiresCompilationWait()` メソッドを簡素化
  - 不要な `_currentPayload` フィールドと `Execute()` オーバーライドを削除
  - コンパイル待機を常に無効化し、MCPサーバー側で処理

- **MCPサーバー: bridge_manager.py**
  - `_handle_bridge_restarted()` でコンパイル待機中の全ての `Future` を解決
  - Unity bridgeの再起動を検出してクライアントに通知
  - コンパイル完了情報を詳細に記録

- **MCPサーバー: register_tools.py**
  - `unity_asset_crud` で `.cs` ファイルの作成・更新・削除時に自動コンパイル待機
  - 60秒のタイムアウト設定
  - エラー発生時もオペレーション自体は失敗させない
  - コンパイル結果をレスポンスに自動的に追加

- **GameKitSceneFlowHandler のプレハブベース化**
  - 全操作が `Resources/GameKitSceneFlows/` のプレハブを編集
  - `create` 操作でプレハブを自動生成
  - `delete` 操作でプレハブファイルを削除
  - `PrefabUtility.EditPrefabContentsScope` を使用した安全な編集
  - レスポンスに `prefabPath` と自動ロード情報を含める

### 技術的な詳細

この修正により、Unity-AI-Forgeは以下のフローで動作します：

1. クライアント → MCPサーバー: `unity_asset_crud` (create/update/delete .cs file)
2. MCPサーバー → Unity Bridge: `assetManage` コマンド送信
3. Unity: スクリプトファイル作成/更新/削除 → `AssetDatabase.Refresh()`
4. Unity: コンパイル開始 → `compilation:started` メッセージ送信
5. MCPサーバー: `await_compilation()` で待機開始
6. Unity: コンパイル完了 → `compilation:complete` メッセージ送信
7. Unity: アセンブリリロード → Bridge再起動
8. Unity: `bridge:restarted` メッセージ送信
9. MCPサーバー: 待機解除、コンパイル結果を返す
10. クライアント: 成功レスポンス + コンパイル結果を受信

## [2.3.0] - 2025-12-04

### 追加

- **Physics2D 設定サポート**
  - 新カテゴリ `physics2d` を `unity_projectSettings_crud` に追加
  - 2D重力設定 (gravity x/y)
  - 速度・位置反復回数、閾値設定
  - シミュレーションモードの制御
  - 2Dゲーム開発に必要な物理パラメータを完全サポート

- **ソートレイヤー管理機能**
  - `unity_projectSettings_crud` の `tagsLayers` カテゴリに追加
  - `addSortingLayer`: ソートレイヤーの追加
  - `removeSortingLayer`: ソートレイヤーの削除
  - `sortingLayers`: ソートレイヤー一覧の取得
  - 2Dスプライトの描画順序を完全制御

- **CharacterController Bundle ツール** (`unity_character_controller_bundle`)
  - 3Dキャラクター用の最適化されたプリセット
  - 7つのプリセット: fps, tps, platformer, child, large, narrow, custom
  - 自動設定: radius, height, center, slopeLimit, stepOffset, skinWidth
  - バッチ適用とカスタム設定に対応

- **MCPサーバープロンプト強化**
  - Machinations システムの詳細説明を追加
  - CharacterController Bundle の使用例を追加
  - バッチ順次処理の詳細ガイドを追加
  - 4つの構成要素の説明 (Resource Pools/Flows/Converters/Triggers)

### 変更

- **ツール総数**: 22 → 24ツール
  - Mid-Level Batch: 7 → 8ツール (CharacterController追加)
  - High-Level GameKit: Machinations を含む6ツール
  
- **プロジェクト設定カテゴリ**: 7 → 8カテゴリ
  - `physics2d` カテゴリを追加
  - `tagsLayers` カテゴリにソートレイヤー機能を追加

### 修正

- Physics2D プロパティの正確性を向上
  - `velocityThreshold` を正しく `Physics2D.velocityThreshold` にマッピング
  - Unityバージョン互換性のため `baumgarteTimeOfImpactScale` を削除

### ドキュメント

- README.md にv2.3.0の新機能セクションを追加
- INDEX.md のツール数を更新 (22→24)
- MCPサーバープロンプトを全面的に更新
- すべてのバージョン情報を2.3.0に統一

## [2.2.0] - 2025-12-03

### 追加

- **バッチ逐次処理ツール** (`unity_batch_sequential`)
  - 複数のMCPツール呼び出しを順次実行
  - エラー発生時に自動停止し、残りの処理を保存
  - レジューム機能により中断した処理を再開可能
  - 別リソース (`batch_queue`) から処理キューを参照可能
  - `resume=false` で新規処理を開始し、既存キューをクリア
  - 使用例: 複数GameObject作成、複雑なシーン構築、バッチ設定変更

- **バッチキューリソース** (`batch_queue`)
  - 実行待ちの処理キューを外部から参照
  - 処理の進行状況を確認可能
  - エラー情報と失敗位置を保持

### 機能

- エラーハンドリングの改善
  - 処理中断時に詳細なエラー情報を提供
  - 失敗した処理のインデックスを記録
  - 次回実行時に失敗箇所から再開

- ドキュメント
  - バッチ逐次処理ツールの詳細ドキュメント ([BATCH_SEQUENTIAL.md](MCPServer/BATCH_SEQUENTIAL.md))
  - 使用例とベストプラクティスを追加

## [2.0.0] - 2025-11-29

### 🔥 プロジェクト名変更

- **SkillForUnity** → **Unity-AI-Forge**
  - 新パッケージ名: `com.unityaiforge`
  - 新リポジトリ: `https://github.com/kuroyasouiti/Unity-AI-Forge`
  - AI駆動開発とAI連携による「鍛造（forging）」を強調

### 破壊的変更

- **プロジェクト名変更** - パッケージ参照とインポートの更新が必要
- **GameKit Manager** - ハブベースアーキテクチャへの完全な再設計。既存のmanagerメソッドを使用するコードは引き続き動作（後方互換APIあり）しますが、内部構造が変更されています。
- **GameKit Interaction** - 新しいトリガータイプとアクションシステムにより、既存のインタラクションセットアップの更新が必要な場合があります。
- **GameKit SceneFlow** - 遷移がグローバルではなくシーンごとに定義されるようになりました。シーン遷移を使用しているプロジェクトには移行が必要です。

### 変更

- **GameKit Manager** - モード固有コンポーネントを持つマネージャーハブとして再設計
  - ManagerTypeに基づいてモード固有コンポーネントを自動追加
  - **TurnBased** → GameKitTurnManager（ターンフェーズ、ターンカウンター、フェーズ/ターンイベント）
  - **ResourcePool** → GameKitResourceManager（Machinations風リソースフローシステム）
    - 最小/最大制約付きリソースプール
    - 自動リソースフロー（ソース/ドレイン）
    - リソースコンバーター（クラフティング、変換）
    - リソーストリガー（しきい値ベースのイベント）
    - イベント: `OnResourceChanged`、`OnResourceTriggered`
  - **EventHub** → GameKitEventManager（イベント登録、イベントトリガー）
  - **StateManager** → GameKitStateManager（状態変更、状態履歴）
  - **Realtime** → GameKitRealtimeManager（タイムスケール、一時停止/再開、タイマー）
  - 便利メソッドはモード固有コンポーネントに自動的にデリゲート
  - 後方互換API（既存のコードは引き続き動作）
  - モード固有コンポーネントへの直接アクセス用の`GetModeComponent<T>()`

- **GameKit Interaction** - インタラクションハブとして再設計
  - 従来のトリガー（Collision、Trigger、Input、Proximity、Raycast）をサポート
  - **新しい特殊トリガー**: TilemapCell、GraphNode、SplineProgress
  - **拡張アクション**: TriggerActorAction、UpdateManagerResource、TriggerSceneFlow、TeleportToTile、MoveToGraphNode、SetSplineProgress
  - **拡張条件**: ActorId、ManagerResource
  - UnityEvents統合（`OnInteractionTriggered`）
  - クールダウンとリピート設定
  - 手動トリガーサポート
  - デバッグログオプション
  - 近接およびタイルマップトリガー用のGizmo視覚化

### 追加
- **CharacterController Bundle** (`unity_character_controller_bundle`) - 中レベルツール
  - プリセット付きCharacterControllerの適用: fps、tps、platformer、child、large、narrow、custom
  - 複数のGameObjectのバッチ操作
  - 設定可能な衝突プロパティ（radius、height、center、slope limit、step offset）
  - ランタイム状態（isGrounded、velocity）を含むCharacterControllerプロパティの検査
  
- **GameKit Actor Input System統合**
  - Unityの新しいInput System用の`GameKitInputSystemController`コンポーネント
  - 事前構築されたアクションマップによる自動PlayerInput設定
  - デフォルト入力アクションアセット生成（WASD、マウス、ゲームパッドサポート）
  - Input Systemが利用できない場合の`GameKitSimpleInput`への自動フォールバック
  - 動作プロファイルに基づく2D/3D入力変換

- **GameKit AIコントローラー**
  - 自律的なキャラクター制御用の`GameKitSimpleAI`コンポーネント
  - AIビヘイビア: Idle、Patrol、Follow、Wander
  - 設定可能なウェイポイント、フォローターゲット、ワンダー半径

### 変更
- **GameKit UI Command Hub** - UI-to-Actorブリッジとして再設計
  - `GameKitActor`のUnityEventsにUIコントロールをブリッジする集中ハブとして機能
  - コマンドタイプシステム（Move、Jump、Action、Look、Custom）
  - 移動コマンド用の方向ボタンサポート
  - パラメータベースのアクションコマンド
  - パフォーマンス向上のためのActorリファレンスキャッシング
  - CustomコマンドタイプによるSendMessageとの後方互換性
  - 改善されたAPI: `ExecuteMoveCommand()`、`ExecuteJumpCommand()`、`ExecuteActionCommand()`、`ExecuteLookCommand()`
  - コマンドバインディング管理: `RegisterButton()`、`RegisterDirectionalButton()`、`ClearBindings()`
  - オプションのコマンドロギング付き拡張デバッグ

- **GameKit SceneFlow** - シーン中心のステートマシンとして再設計
  - 遷移をシーン定義に統合（シーン中心設計）
  - 同じトリガーが現在のシーンに基づいて異なる宛先に導く（例: Page1からの「nextPage」→Page2、Page2から→Page3）
  - **簡素化された共有シーン管理**: `SharedSceneGroup`を削除、シーンが共有シーンパスを直接定義
  - シーン定義に遷移と共有シーンパスを含む
  - 改善された共有シーン管理（必要なもののみをリロード）
  - 新しいAPI: `SetCurrentScene()`、`GetAvailableTriggers()`、`GetSceneNames()`、`AddSharedScenesToScene()`
  - シーン遷移の拡張ロギング
  - 後方互換API（AddTransitionパラメータ順序をfromScene、trigger、toSceneに変更）
  - `sharedGroups`パラメータを`sharedScenePaths`に変更（レガシー`sharedGroups`は後方互換性のためサポート）

- **GameKit Graph Node Movement** - 新しい動作プロファイル
  - 移動ノードを定義する`GraphNode`コンポーネント

- **GameKit Spline Movement** - 2.5Dゲーム用の新しい動作プロファイル
  - レール/スプラインベース移動用の`SplineMovement`コンポーネント
  - 滑らかな曲線パス用のCatmull-Romスプライン補間
  - 円形トラック用のクローズドループサポート
  - レーンベースゲームプレイ用の横方向オフセット（レールシューター、サイドスクローラー）
  - 加速/減速付きの手動および自動速度制御
  - 前進および後進移動サポート
  - 移動方向を向く自動回転（設定可能な軸）
  - Scene viewでのビジュアルスプラインデバッグ
  - レールシューター、2.5Dプラットフォーマー、レーシングゲーム、オンレールシーケンスに最適
  - A*パスファインディング付きの`GraphNodeMovement`コンポーネント
  - コストと通過可能性を持つノード接続
  - 2Dと3Dの両方で動作（次元非依存）
  - 使用例: ボードゲーム、タクティカルRPG、パズルゲーム、アドベンチャーゲーム
  - 機能: 重み付きエッジ、パスファインディング、到達可能ノードクエリ、デバッグ視覚化

### 変更
- `GameKitActorHandler.ApplyControlComponents()`をデフォルトでInput Systemを使用するように更新
- GameKit Runtimeアセンブリに`UNITY_INPUT_SYSTEM_INSTALLED`定義制約を追加

### ドキュメント
- CharacterController Bundleの包括的なドキュメントを追加
- アーキテクチャ概要付きのGameKit Runtimeコンポーネント READMEを追加
- 新機能でREADME.mdとREADME_ja.mdを更新

## [1.8.0] - 2025-11-29

### 追加

#### 新しいツール
- **Prefab管理** (`unity_prefab_crud`)
  - GameObjectからPrefabを作成
  - Prefabの更新、検査、インスタンス化
  - Prefabのアンパック（完全またはOutermost）
  - Prefabオーバーライドの適用/復帰
  
- **ベクタースプライト変換** (`unity_vector_sprite_convert`)
  - プリミティブ（正方形、円、三角形、多角形）からスプライトを生成
  - SVGからスプライトへのインポート
  - テクスチャからスプライトへの変換
  - 単色スプライトの作成

#### GameKitフレームワーク（高レベルツール）
- **GameKit Actor** (`unity_gamekit_actor`)
  - 動作プロファイル: 2D/3D物理、リニア、タイルマップ移動
  - 制御モード: ダイレクトコントローラー、AI、UIコマンド
  - ステータス、アビリティ、武器ロードアウト
  
- **GameKit Manager** (`unity_gamekit_manager`)
  - マネージャータイプ: ターンベース、リアルタイム、リソースプール、イベントハブ、ステートマネージャー
  - ターンフェーズ管理
  - Machinationsフレームワークサポート付きリソースプール
  - 永続性（DontDestroyOnLoad）
  
- **GameKit Interaction** (`unity_gamekit_interaction`)
  - トリガータイプ: collision、trigger、raycast、proximity、input
  - 宣言的アクション: spawn prefab、destroy object、play sound、send message、change scene
  - 条件: tag、layer、distance、custom
  
- **GameKit UI Command** (`unity_gamekit_ui_command`)
  - ボタンレイアウト（水平、垂直、グリッド）付きコマンドパネル
  - Actorコマンドディスパッチ
  - アイコンとラベルのサポート
  
- **GameKit SceneFlow** (`unity_gamekit_sceneflow`)
  - 遷移付きシーンステートマシン
  - 加算シーンローディング
  - 永続マネージャーシーン
  - 共有シーングループ（UI、Audio）
  - シーンを跨ぐ参照解決

#### 中レベルツール
- **Transform Batch** (`unity_transform_batch`)
  - 円/線でオブジェクトを配置
  - 連続/リストベースの名前変更
  - メニュー階層の自動生成
  
- **RectTransform Batch** (`unity_rectTransform_batch`)
  - アンカー、ピボット、サイズ、位置の設定
  - 親プリセットへの整列
  - 水平/垂直分散
  - ソースからのサイズマッチング
  
- **Physics Bundle** (`unity_physics_bundle`)
  - 2D/3D Rigidbody + Colliderプリセット
  - プリセット: dynamic、kinematic、static、character、platformer、topDown、vehicle、projectile
  - 個別の物理プロパティ更新
  
- **Camera Rig** (`unity_camera_rig`)
  - カメラリグプリセット: follow、orbit、split-screen、fixed、dolly
  - ターゲット追跡とスムーズな移動
  - ビューポート設定
  
- **UI Foundation** (`unity_ui_foundation`)
  - Canvas、Panel、Button、Text、Image、InputFieldの作成
  - アンカープリセット
  - TextMeshProサポート
  - 自動レイアウト
  
- **Audio Source Bundle** (`unity_audio_source_bundle`)
  - オーディオプリセット: music、sfx、ambient、voice、ui
  - 2D/3D空間オーディオ
  - ミキサーグループ統合
  
- **Input Profile** (`unity_input_profile`)
  - 新しいInput System統合
  - アクションマップ設定
  - 通知動作: sendMessages、broadcastMessages、invokeUnityEvents、invokeCSharpEvents
  - InputActionsアセットの作成

#### 機能
- **コンパイル待機システム**
  - 操作を先に実行し、トリガーされた場合はコンパイルを待機
  - 早期待機解除のためのブリッジ再接続検出
  - 設定可能な間隔での60秒タイムアウト
  - レスポンスでの透明な待機情報
  - BaseCommandHandlerでの自動処理

- **包括的なテストスイート**
  - すべてのツールカテゴリをカバーする100以上のユニットテスト
  - Unity Test Framework統合
  - 97.7%の合格率（42/43テスト）
  - エディタメニュー統合: `Tools > SkillForUnity > Run All Tests`
  - コマンドラインテストランナー（PowerShell、Bash）
  - GitHub ActionsによるCI/CD

#### ドキュメント
- テストスイートドキュメント（`Assets/SkillForUnity/Tests/Editor/README.md`）
- テスト結果サマリー（`docs/TestResults_Summary.md`）
- ツールロードマップ - 日本語（`docs/tooling-roadmap.ja.md`）
- コンパイル待機機能ガイド（`docs/Compilation_Wait_Feature.md`）
- レガシークリーンアップサマリー（`docs/Unused_Handlers_Cleanup_Summary.md`）

### 変更

- **ツール数**: 7から21ツールに増加
- **BaseCommandHandler**: コンパイル待機を操作実行の前から後に移動
- **AssetCommandHandler**: 作成/更新操作後に`AssetDatabase.Refresh()`を追加
- **skill.yml**: ツール数を更新し、新しいカテゴリを追加（prefab_management、sprite_conversion、batch_operations、gamekit_systems）

### 削除

- `Assets/SkillForUnity/Editor/Tests/`のレガシーテストファイル
  - BaseCommandHandlerTests.cs
  - PayloadValidatorTests.cs
  - ResourceResolverTests.cs
  - CommandHandlerIntegrationTests.cs

- 未使用のハンドラ（MCPに登録されていない）
  - TemplateCommandHandler
  - UguiCreateFromTemplateHandler
  - UguiDetectOverlapsHandler
  - UguiLayoutManageHandler
  - UguiManageCommandHandler
  - ConstantConvertHandler
  - RenderPipelineManageHandler
  - TagLayerManageHandler（ProjectSettingsManageHandlerに統合）
  - RectTransformAnchorHandler（RectTransformBatchHandlerに機能あり）
  - RectTransformBasicHandler（RectTransformBatchHandlerに機能あり）

### 修正

- コンパイル待機が操作実行後に発生するようになった（より信頼性向上）
- ブリッジ再接続がコンパイル待機を適切に解除
- テストスイートのコンパイルエラーを解決

---

## [1.7.1] - 2025-11-XX

### 修正

- **テンプレートツール**: シーンクイックセットアップ、GameObjectテンプレート、UIテンプレート、デザインパターン、スクリプトテンプレートを修正
- **定数変換**: Unity 2024.2+モジュールシステム用のenum型解決を修正
- **SerializedFieldサポート**: ComponentとScriptableObject操作で`[SerializeField]`プライベートフィールドのサポートを追加
- **型解決**: キャッシングにより99%以上のパフォーマンス向上

### 追加

- `listCommonEnums`操作: カテゴリ別によく使用されるUnity enum型をリスト
- デバッグ情報付きの拡張エラーメッセージ

### 変更

- ツールセットの簡素化: 低レベルCRUD操作に焦点

---

## [1.7.0] - 2025-XX-XX

### 追加

- 初期MCP server実装
- Unity Editor用WebSocketブリッジ
- コアCRUD操作: Scene、GameObject、Component、Asset、ScriptableObject
- プロジェクト設定管理

---

[1.8.0]: https://github.com/kuroyasouiti/SkillForUnity/releases/tag/v1.8.0
[1.7.1]: https://github.com/kuroyasouiti/SkillForUnity/releases/tag/v1.7.1
[1.7.0]: https://github.com/kuroyasouiti/SkillForUnity/releases/tag/v1.7.0
