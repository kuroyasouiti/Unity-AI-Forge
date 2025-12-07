# テスト結果の表示方法

## 📺 Test Result Viewer の使用（推奨）

最も簡単な方法は、専用のTest Result Viewerを使用することです。

### ステップ1: Test Result Viewerを開く

Unity Editorで：
```
Tools > SkillForUnity > Open Test Result Viewer
```

### ステップ2: テストを実行

Test Result Viewerウィンドウの「クイックアクション」セクションから：
- **Run TextMeshPro Improved Tests** ボタンをクリック
- **Run All TextMeshPro Tests** ボタンをクリック
- **Run All Tests** ボタンをクリック

→ 自動的にTest Runner Windowが開き、テスト結果が表示されます。

---

## 🔍 手動でテスト結果を確認する方法

### 方法1: Test Runner Window（推奨）

#### ステップ1: Test Runner Windowを開く

以下のいずれかの方法で：

**A. メニューから**
```
Window > General > Test Runner
```

**B. Test Result Viewerから**
```
Tools > SkillForUnity > Open Test Result Viewer
→ "Open Test Runner Window" ボタンをクリック
```

**C. TestRunnerメニューから**
```
Tools > SkillForUnity > Open Test Runner Window
```

#### ステップ2: EditModeタブを選択

Test Runner Windowの上部に2つのタブがあります：
- **PlayMode** （実行時テスト）
- **EditMode** （エディタテスト）← こちらを選択

#### ステップ3: テストツリーを確認

左側のツリービューに以下のように表示されます：

```
📁 UnityAIForge.Tests.Editor
  📁 TextMeshProComponentTests
    ✓ AddComponent_TextMeshPro_CreatesComponent
    ✓ UpdateComponent_TextMeshPro_UpdatesText
    ✓ InspectComponent_TextMeshPro_ReturnsComponentInfo
    ...
  📁 TextMeshProComponentImprovedTests
    ✓ InspectComponent_WithPropertyFilter_ReturnsOnlySpecifiedProperties
    ✓ UpdateComponent_WithPartialFailure_ReturnsUpdatedAndFailedProperties
    ...
```

#### ステップ4: テストを実行

**すべてのテストを実行**:
- "Run All" ボタンをクリック

**特定のテストのみを実行**:
1. テストを選択（クリック）
2. "Run Selected" ボタンをクリック

#### ステップ5: 結果を確認

テスト実行後、各テストの左側にアイコンが表示されます：

- ✅ **緑のチェックマーク** = テスト成功
- ❌ **赤いバツマーク** = テスト失敗
- ⏸️ **グレーのダッシュ** = 未実行または中断

**詳細情報を見る**:
1. テストをクリック
2. 右側のパネルに詳細が表示される：
   - テスト名
   - 実行時間
   - エラーメッセージ（失敗した場合）
   - スタックトレース（失敗した場合）

---

### 方法2: Console Window

#### ステップ1: Console Windowを開く

```
Window > General > Console
```

または

```
Ctrl + Shift + C (Windows)
Cmd + Shift + C (Mac)
```

#### ステップ2: ログを確認

テスト実行時、以下のようなログが表示されます：

```
[TestRunner] Executing TextMeshPro Improved Component tests...
Test results will appear in Test Runner window.
```

**注意**: Console Windowには詳細な結果は表示されません。Test Runner Windowで確認してください。

---

## 📊 テスト実行の例

### 例1: TextMeshPro Improved Testsを実行

1. **メニューから実行**:
   ```
   Tools > SkillForUnity > Run TextMeshPro Improved Tests
   ```

2. **Test Runner Windowで確認**:
   ```
   Window > General > Test Runner
   ```

3. **結果を確認**:
   - EditModeタブを選択
   - TextMeshProComponentImprovedTestsを展開
   - 10個のテスト結果を確認

**期待される結果**:

改善機能が正しく動作している場合：
```
✅ InspectComponent_WithPropertyFilter_ReturnsOnlySpecifiedProperties
✅ InspectComponent_WithPropertyFilter_ExcludesNonSpecifiedProperties
✅ InspectMultipleComponents_WithPropertyFilter_ReturnsOnlySpecifiedProperties
✅ UpdateComponent_WithPartialFailure_ReturnsUpdatedAndFailedProperties
✅ UpdateComponent_WithAllValidProperties_HasNoFailedProperties
✅ UpdateMultipleComponents_WithPartialFailure_ReportsIndividualResults
✅ AddMultipleComponents_WithPropertyChanges_AppliesInitialProperties
✅ AddMultipleComponents_WithPartiallyInvalidPropertyChanges_AppliesValidProperties
✅ AddMultipleComponents_WithoutPropertyChanges_CreatesComponentsWithDefaults
✅ CompleteWorkflow_AddWithPropertiesInspectWithFilterUpdate_WorksCorrectly
```

改善機能がまだ反映されていない場合：
- 一部のテストが条件付きでパス
- レガシー版の動作も検証可能

---

## 🔧 トラブルシューティング

### 問題1: Test Runner Windowが空白

**症状**: Test Runner Windowを開いたが、何も表示されない

**原因**: テストが読み込まれていない

**解決方法**:
1. Unity Editorを再起動
2. Test Runner Windowで "Refresh" ボタンをクリック
3. `Assets > Reimport All` を実行

### 問題2: EditModeタブにテストが表示されない

**症状**: PlayModeタブには何かあるが、EditModeタブが空

**原因**: テストアセンブリがコンパイルされていない

**解決方法**:
1. Console Windowでコンパイルエラーを確認
2. `UnityAIForge.Tests.Editor.asmdef` が存在するか確認
3. Unity Editorを再起動

### 問題3: テスト実行後も結果が表示されない

**症状**: "Run All" をクリックしたが、結果が更新されない

**解決方法**:

**方法A: Test Runner Windowを再度開く**
```
Window > General > Test Runner
```

**方法B: テストを個別に実行**
1. テストツリーで特定のテストをクリック
2. "Run Selected" をクリック
3. 実行完了を待つ（数秒）

**方法C: Console Windowを確認**
```
Window > General > Console
```
エラーメッセージがないか確認

### 問題4: すべてのテストが失敗する

**症状**: すべてのテストに赤いバツマークが付く

**原因**: TextMeshProパッケージがインストールされていない

**解決方法**:
1. `Window > Package Manager` を開く
2. "TextMeshPro" を検索
3. "Install" をクリック
4. Unity Editorを再起動
5. テストを再実行

### 問題5: 特定のテストだけ失敗する

**症状**: 一部のテストは成功するが、特定のテストだけ失敗

**解決方法**:
1. 失敗したテストをクリック
2. 右側のパネルでエラーメッセージを確認
3. スタックトレースから原因を特定
4. Console Windowで詳細ログを確認

---

## 📸 スクリーンショットガイド

### Test Runner Windowの見方

```
┌─────────────────────────────────────────────────────────┐
│ Test Runner                                    ☐ ☐ ✕ │
├─────────────────────────────────────────────────────────┤
│ [PlayMode] [EditMode] ← EditModeを選択             │
├──────────────────────┬──────────────────────────────────┤
│ 📁 Tests             │ Test Details                     │
│   📁 Editor          │                                  │
│     📁 TextMeshPro.. │ Name: InspectComponent_With...   │
│       ✓ InspectCo..  │ Status: Passed                   │
│       ✓ UpdateCom..  │ Duration: 0.123s                 │
│       ❌ AddMulti..   │ Message: (失敗時のみ表示)          │
│                      │ Stack Trace: (失敗時のみ表示)      │
├──────────────────────┴──────────────────────────────────┤
│ [Run All] [Run Selected] [Rerun Failed]                │
└─────────────────────────────────────────────────────────┘
```

### Test Result Viewerの見方

```
┌─────────────────────────────────────────────────────────┐
│ Test Results                                   ☐ ☐ ✕ │
├─────────────────────────────────────────────────────────┤
│ Unity Test Runner Results                               │
│                                                         │
│ ℹ️ テスト結果を確認するには以下の方法があります：          │
│                                                         │
│ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━         │
│ 方法1: Test Runner Window を使用                        │
│ ╭─────────────────────────────────────────────────╮    │
│ │ 1. Test Runner Window を開く                    │    │
│ │ [Open Test Runner Window]                      │    │
│ │ 2. EditMode タブを選択                          │    │
│ │ 3. テストツリーを展開してテストを確認              │    │
│ ╰─────────────────────────────────────────────────╯    │
│                                                         │
│ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━         │
│ クイックアクション                                        │
│ ╭─────────────────────────────────────────────────╮    │
│ │ [1. Run TextMeshPro Improved Tests]            │    │
│ │ [2. Run All TextMeshPro Tests]                 │    │
│ │ [3. Run All Tests]                             │    │
│ ╰─────────────────────────────────────────────────╯    │
└─────────────────────────────────────────────────────────┘
```

---

## 💡 ヒントとコツ

### ヒント1: Test Runner Windowを常に開いておく

テスト開発中は、Test Runner Windowを常に開いておくと便利です：
1. Test Runner Windowをドッキング
2. 右下などに配置
3. コードを編集→テストを実行→結果を即座に確認

### ヒント2: キーボードショートカットを使う

Unity Test Runnerには以下のショートカットがあります：
- `Ctrl/Cmd + R` - 選択したテストを再実行
- `Ctrl/Cmd + A` - すべて選択
- `↑↓` - テストを移動

### ヒント3: フィルタを使う

Test Runner Windowの検索ボックスで：
- テスト名で検索
- クラス名で検索
- 失敗したテストのみ表示

### ヒント4: 自動実行を設定

`Preferences > Test Runner` で：
- **Run tests when code changes**: コード変更時に自動実行
- **Run in background**: バックグラウンドで実行

---

## 📚 関連ドキュメント

- [TextMeshPro Tests Documentation](./README-TextMeshPro-Tests.md)
- [TextMeshPro Improved Tests Documentation](./README-TextMeshPro-Improved-Tests.md)
- [Troubleshooting Guide](../../../TROUBLESHOOTING_IMPROVEMENTS.md)

---

## 🆘 それでも解決しない場合

以下の情報を収集してサポートに連絡してください：

1. **Unity バージョン**:
   ```
   Help > About Unity
   ```

2. **Console ログ**:
   - Console Window の内容をコピー
   - エラーメッセージをすべて含める

3. **Test Runner スクリーンショット**:
   - Test Runner Window の状態
   - 失敗したテストの詳細

4. **テスト実行手順**:
   - どのメニューから実行したか
   - どのテストを実行したか

---

**最終更新日**: 2025-12-06  
**バージョン**: 1.0.0  
**対象**: UnityAI-Forge TextMeshPro Tests
