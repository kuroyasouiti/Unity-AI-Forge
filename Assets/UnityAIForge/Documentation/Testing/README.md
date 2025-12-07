# SkillForUnity Test Suite

Unity Test Framework を使用した包括的なエディタテストスイートです。

> **Note:** レガシーテスト (`Assets/SkillForUnity/Editor/Tests/`) は削除され、この新しいテストスイートに統合されました。

## テストカテゴリ

### 1. Low-Level Tools Tests (`LowLevelToolsTests.cs`)
- **Scene CRUD**: シーン作成テスト
- **GameObject CRUD**: GameObject作成・リネーム・削除テスト
- **Component CRUD**: コンポーネント追加・削除・更新テスト
- **ScriptableObject CRUD**: ScriptableObject作成テスト

### 2. Mid-Level Tools Tests (`MidLevelToolsTests.cs`)
- **Transform Batch**: 円形配置・連番リネームテスト
- **RectTransform Batch**: アンカー設定テスト
- **Physics Bundle**: 2D/3D物理プリセット適用テスト
- **Audio Source Bundle**: AudioSource作成テスト
- **UI Foundation**: Canvas作成テスト

### 3. GameKit Actor Tests (`GameKitActorTests.cs`)
- アクター作成・初期化テスト
- Stats設定・取得・更新テスト
- Ability追加・確認テスト
- Weapon追加テスト

### 4. GameKit Manager Tests (`GameKitManagerTests.cs`)
- マネージャー作成・初期化テスト
- ターンフェーズ追加・進行テスト
- リソース設定・取得・消費・追加テスト

### 5. GameKit Interaction Tests (`GameKitInteractionTests.cs`)
- インタラクション作成・初期化テスト
- アクション追加テスト
- 条件追加テスト
- トリガーコライダー設定テスト

### 6. GameKit UI Command Tests (`GameKitUICommandTests.cs`)
- UIコマンドパネル作成テスト
- ボタン登録テスト
- ターゲットアクター設定テスト

### 7. GameKit SceneFlow Tests (`GameKitSceneFlowTests.cs`)
- SceneFlow作成・初期化テスト
- シーン追加テスト
- トランジション追加テスト
- 共有グループ追加テスト
- トランジショントリガーテスト

### 8. TextMeshPro Component Tests (`TextMeshProComponentTests.cs`)
- TextMeshPro 3D コンポーネントの CRUD 操作テスト
- TextMeshProUGUI (UI) コンポーネントの CRUD 操作テスト
- 複数コンポーネントの一括操作テスト（Add/Update/Inspect/Remove Multiple）
- プロパティフィルタを使用した情報取得テスト
- 高度なプロパティ（自動サイズ調整、配置等）の更新テスト

詳細については [TextMeshPro Tests Documentation](./TextMeshPro-Tests.md) を参照してください。

### 9. TextMeshPro Component Improved Tests (`TextMeshProComponentImprovedTests.cs`)
- **propertyFilter機能テスト**: 指定したプロパティのみを取得し、内部フィールドを除外
- **部分的エラーハンドリングテスト**: 一部のプロパティ更新が失敗しても他は成功
- **addMultiple propertyChangesテスト**: コンポーネント追加時の初期プロパティ適用
- **統合ワークフローテスト**: すべての改善機能が連携して動作
- **詳細なエラーレポート**: updatedPropertiesとfailedPropertiesを分けて報告

詳細については [TextMeshPro Improved Tests Documentation](../../Tests/Editor/README-TextMeshPro-Improved-Tests.md) を参照してください。

### 10. Character Controller Bundle Tests (`CharacterControllerBundleTests.cs`)
- CharacterController のプリセット適用テスト（FPS、TPS、Platformer等）
- CharacterController のプロパティ更新テスト
- CharacterController の情報取得テスト

## テスト実行方法

### Unity Editor内で実行

1. Unity Editorを開く
2. **Window > General > Test Runner** を選択
3. **EditMode** タブを選択
4. **Run All** をクリックして全テストを実行

### 個別テスト実行

Test Runnerウィンドウで特定のテストクラスまたはメソッドを選択し、**Run Selected** をクリック

### コマンドラインから実行

```bash
# Windows
Unity.exe -runTests -batchmode -projectPath "D:\Projects\SkillForUnity" -testResults results.xml -testPlatform EditMode

# macOS/Linux
/Applications/Unity/Unity.app/Contents/MacOS/Unity -runTests -batchmode -projectPath "/path/to/SkillForUnity" -testResults results.xml -testPlatform EditMode
```

## テスト結果の確認

- Test Runnerウィンドウに結果が表示されます
- 緑のチェックマーク: テスト成功
- 赤いX: テスト失敗
- コマンドライン実行時は `results.xml` に結果が出力されます

## テスト追加ガイドライン

新しいツールを追加した場合:

1. 対応するテストクラスを作成（`{ToolName}Tests.cs`）
2. `[TestFixture]` 属性を付与
3. `[SetUp]` でテスト環境を初期化
4. `[TearDown]` でクリーンアップ
5. `[Test]` 属性を付けたテストメソッドを作成

### テスト命名規則

```csharp
[Test]
public void MethodName_Scenario_ExpectedBehavior()
{
    // Arrange - テストデータ準備
    // Act - テスト対象メソッド実行
    // Assert - 結果検証
}
```

## 注意事項

- エディタテストはPlayModeではなくEditModeで実行されます
- シーン遷移やランタイム動作のテストは制限があります
- テスト後は必ずリソースをクリーンアップしてください
- テストは独立して実行可能である必要があります（他のテストに依存しない）

## CI/CD統合

GitHub Actionsを使用した自動テスト実行が設定されています：

- `.github/workflows/unity-tests.yml` - GitHub Actions ワークフロー
- `run-tests.ps1` - Windows用テスト実行スクリプト
- `run-tests.sh` - macOS/Linux用テスト実行スクリプト

### ローカルでのバッチテスト実行

#### Windows (PowerShell)

```powershell
.\run-tests.ps1 -UnityPath "C:\Program Files\Unity\Hub\Editor\2022.3.0f1\Editor\Unity.exe"
```

#### macOS/Linux (Bash)

```bash
./run-tests.sh --unity-path "/Applications/Unity/Hub/Editor/2022.3.0f1/Unity.app/Contents/MacOS/Unity"
```

## Unity Editorメニューからの実行

**Tools > SkillForUnity** メニューから以下のオプションを選択できます：

- **Run All Tests** - 全テストを実行
- **Run Low-Level Tests** - ローレベルツールのテストのみ実行
- **Run Mid-Level Tests** - ミドルレベルツールのテストのみ実行
- **Run GameKit Tests** - GameKitツールのテストのみ実行
- **Run Component Tests** - コンポーネント関連のテストのみ実行
- **Run TextMeshPro Tests** - TextMeshPro基本テストのみ実行
- **Run TextMeshPro Improved Tests** - TextMeshPro改善機能テストのみ実行
- **Run All TextMeshPro Tests** - すべてのTextMeshProテストを実行
- **Open Test Runner Window** - Test Runnerウィンドウを開く

## テスト実装の詳細ドキュメント

各テストカテゴリの詳細な実装ドキュメント：

- [TextMeshPro Tests](./TextMeshPro-Tests.md) - TextMeshProコンポーネントの基本CRUD操作テスト
- [TextMeshPro Improved Tests](../../Tests/Editor/README-TextMeshPro-Improved-Tests.md) - TextMeshProコンポーネントの改善機能テスト

