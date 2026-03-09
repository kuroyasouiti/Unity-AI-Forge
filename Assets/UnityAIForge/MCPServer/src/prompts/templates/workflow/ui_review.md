# UIレビュー ワークフローガイド (Unity-AI-Forge v{VERSION})

全ビルドシーンのUIを体系的にレビューし、レイアウト・操作性・整合性の問題を検出するガイドです。UGUI（Canvas）とUI Toolkit（UXML/USS）の両方に対応します。

---

## パイプライン位置

```
企画 → 設計 → プロジェクト初期設定 → プロトタイプ → アルファ → ベータ → リリース
                                       ↑            ↑          ↑        ↑
                                  [UIレビュー: UI実装後の各フェーズで実施]
```

本ガイドは**横断的ガイド**です。UI実装後のプロトタイプ以降、各フェーズの品質ゲートとしてUIレビューを実施してください。

**前提**: UIが実装済みであること（Canvas/UIDocument、UXML/USSファイルが存在すること）。

---

## ワークフロー概要

```
Step 1: プロジェクト状態チェック
→ Step 2: 全シーン一括整合性チェック
→ Step 3: UITK アセット監査（USS/UXML品質）
→ Step 4: シーン別レイアウト確認
→ Step 5: レポート・修正
```

---

## Step 1: プロジェクト状態チェック

UIレビューの前提となる技術スタックを確認します。

```python
# Input System確認
unity_projectSettings_crud(operation='read', category='player')

# シーン階層確認（既存UI要素の把握）
unity_scene_crud(operation='inspect', includeHierarchy=True, includeComponents=True)
```

確認項目:
- `activeInputHandler`: InputSystem / InputManager / Both
- Canvas / UIDocument の存在
- EventSystem の存在

---

## Step 2: 全シーン一括整合性チェック

```python
# 全ビルドシーンの整合性を一括チェック
unity_validate_integrity(operation='report', scope='build_scenes')

# UI固有の整合性チェック（アクティブシーンごと）
unity_validate_integrity(operation='touchTargetAudit')     # タッチターゲットサイズ
unity_validate_integrity(operation='eventSystemAudit')     # EventSystem有無
unity_validate_integrity(operation='textOverflowAudit')    # テキスト溢れ
unity_validate_integrity(operation='uiOverflowAudit')      # レイアウト溢れ
unity_validate_integrity(operation='uiOverlapAudit')       # 要素重なり・レイキャスト
unity_validate_integrity(operation='canvasGroupAudit')     # CanvasGroup競合
```

| チェック | 検出内容 | 重要度 |
|---------|---------|--------|
| touchTargetAudit | 44x44未満のインタラクティブ要素 | warning |
| eventSystemAudit | EventSystem未配置・重複 | error/warning |
| textOverflowAudit | テキストがRectTransformからはみ出し | warning |
| uiOverflowAudit | LayoutGroup/sizeDeltaによる溢れ | warning |
| uiOverlapAudit | 同座標兄弟・レイキャストブロック | warning/error |
| canvasGroupAudit | 親alpha=0による子要素ブロック | warning/error |

---

## Step 3: UITK アセット監査

UI Toolkit使用プロジェクトでは、USS/UXMLファイルの品質をチェックします。

```python
# USS品質チェック（疑似クラス・トランジション）
unity_uitk_asset(operation='auditUSS', searchPath='Assets/UI/USS')

# UXMLレイアウトチェック（レスポンシブ・サイズ）
unity_uitk_asset(operation='auditUXML', searchPath='Assets/UI/UXML')

# UXML依存関係チェック（参照切れ）
unity_uitk_asset(operation='validateDependencies', assetPath='Assets/UI/UXML/MyScreen.uxml')
```

### USS監査チェック項目
| issue type | 検出内容 |
|-----------|---------|
| uss_missingPseudo | ボタンセレクタに :active/:focus/:disabled が未定義 |
| uss_noTransition | :hover があるのに transition 未定義 |

### UXML監査チェック項目
| issue type | 検出内容 |
|-----------|---------|
| uxml_smallButton | Button/Toggle が 44px 未満 |
| uxml_fixedWidth | 400px超の固定幅に max-width/% 未使用 |
| uxml_noMinHeight | ListView/ScrollView に min-height 未指定 |

---

## Step 4: シーン別レイアウト確認

各ビルドシーンを順にロードしてレイアウトを確認します。

```python
# シーンをロードして階層を確認
unity_scene_crud(operation='load', scenePath='Assets/Scenes/MyScene.unity')
unity_scene_crud(operation='inspect', includeHierarchy=True, includeComponents=True)

# シーン固有の全チェック
unity_validate_integrity(operation='all')

# UIナビゲーション確認（キーボード/ゲームパッド操作）
unity_ui_navigation(operation='inspect', rootPath='Canvas')

# UI状態管理確認
unity_ui_state(operation='listStates', rootPath='Canvas')
```

### レイアウト確認チェックリスト

- [ ] Canvas/UIDocument の存在と設定（renderMode, scaleMode）
- [ ] EventSystem の存在
- [ ] CanvasScaler の referenceResolution 設定
- [ ] アンカー/ピボットの適切な設定
- [ ] ボタンのサイズが十分か（44x44以上）
- [ ] テキスト要素のオーバーフロー設定
- [ ] オーバーレイパネルの初期状態（CanvasGroup alpha=0, blocksRaycasts=false）
- [ ] レイキャストターゲットの適切な設定
- [ ] UIナビゲーション（Tab/矢印キー）の動作

---

## Step 5: レポート・修正

検出された問題を優先度順に分類し、修正します。

### 優先度順

1. **error** — 即座に修正（EventSystem未配置、レイキャストブロック等）
2. **warning** — 重要度に応じて修正（タッチターゲット、テキスト溢れ等）
3. **info** — 改善推奨（トランジション未設定、固定幅等）

### 修正後の再検証

```python
# 修正後に再度全チェック
unity_validate_integrity(operation='all')

# プレイモードで動作確認
unity_playmode_control(operation='play')
unity_playmode_control(operation='captureState', targets=['Canvas'], includeConsole=True)
```

---

## Tips

- **UGUI/UITK混在プロジェクト**: 両方のチェックを実施する。UGUIは validate_integrity 系、UITKは uitk_asset(auditUSS/auditUXML) を使用
- **Safe Area**: ノッチ付き端末対応が必要な場合、SafeAreaパネルの実装を確認
- **CanvasGroupによる表示制御**: SetActive ではなく CanvasGroup(alpha/interactable/blocksRaycasts) で制御されているか確認
- **パフォーマンス**: GraphicRaycaster の Blocking Objects 設定、不要な raycastTarget=true の除去
