# SkillForUnity テストフレームワーク

このディレクトリには、SkillForUnityの統合テストとユニットテストが含まれています。

## ディレクトリ構造

```
tests/
├── README.md                 # このファイル
├── python/                   # Python MCPサーバーのテスト
│   ├── unit/                 # ユニットテスト
│   ├── integration/          # 統合テスト
│   └── conftest.py           # pytest設定
├── csharp/                   # Unity C# ブリッジのテスト
│   ├── Editor/               # Unity Editorテスト
│   └── Runtime/              # ランタイムテスト
└── e2e/                      # エンドツーエンドテスト
    ├── scenarios/            # テストシナリオ
    └── fixtures/             # テストデータ
```

## テストの種類

### 1. ユニットテスト

個々の関数やメソッドの単体テスト。

**Python**:
```bash
# 全ユニットテストを実行
pytest tests/python/unit/

# 特定のテストファイルを実行
pytest tests/python/unit/test_bridge_manager.py

# カバレッジ付きで実行
pytest tests/python/unit/ --cov=src --cov-report=html
```

**C#**:
```csharp
// Unity Test Runnerで実行
// Window > General > Test Runner
```

### 2. 統合テスト

複数のコンポーネントが連携して動作することを確認。

```bash
# Unity Editorが起動している必要があります
pytest tests/python/integration/
```

### 3. エンドツーエンドテスト

実際のユースケースをシミュレート。

```bash
pytest tests/e2e/
```

## セットアップ

### Python環境のセットアップ

```bash
# 仮想環境を作成
python -m venv venv

# 仮想環境を有効化
# Windows:
venv\Scripts\activate
# Linux/macOS:
source venv/bin/activate

# 依存関係をインストール
pip install -r requirements-dev.txt
```

### Unity環境のセットアップ

1. Unity Editor 2021.3以降をインストール
2. Unity Test Frameworkパッケージをインストール
3. テストプロジェクトを開く

## テストの実行

### 全テストを実行

```bash
# Python テスト
pytest

# Unity テスト（CI環境）
unity -runTests -testPlatform EditMode -testResults results.xml
```

### 特定のカテゴリを実行

```bash
# GameObject管理のテストのみ
pytest tests/ -k "gameobject"

# Scene管理のテストのみ
pytest tests/ -k "scene"
```

### マーカーを使用

```bash
# 高速なテストのみ
pytest tests/ -m fast

# 遅いテストをスキップ
pytest tests/ -m "not slow"
```

## テストの書き方

### Python ユニットテスト

```python
# tests/python/unit/test_example.py
import pytest
from src.bridge_manager import BridgeManager

def test_bridge_connection():
    """ブリッジ接続のテスト"""
    manager = BridgeManager("ws://localhost:7077")
    assert manager.is_connected() == False

@pytest.mark.asyncio
async def test_send_command():
    """コマンド送信のテスト"""
    manager = BridgeManager("ws://localhost:7077")
    await manager.connect()
    
    response = await manager.send_command("pingUnityEditor", {})
    
    assert response is not None
    assert "editor" in response
    
    await manager.disconnect()
```

### C# ユニットテスト

```csharp
// Tests/Editor/McpCommandProcessorTests.cs
using NUnit.Framework;
using MCP.Editor;

public class McpCommandProcessorTests
{
    [Test]
    public void TestPingCommand()
    {
        var command = new McpIncomingCommand
        {
            ToolName = "pingUnityEditor",
            Payload = new Dictionary<string, object>()
        };
        
        var result = McpCommandProcessor.Execute(command);
        
        Assert.IsNotNull(result);
        Assert.IsInstanceOf<Dictionary<string, object>>(result);
    }
}
```

### 統合テスト

```python
# tests/python/integration/test_gameobject_crud.py
import pytest
from tests.helpers import unity_client

@pytest.mark.integration
async def test_create_and_delete_gameobject():
    """GameObjectの作成と削除のテスト"""
    client = unity_client()
    
    # GameObjectを作成
    create_response = await client.call_tool(
        "unity_gameobject_crud",
        {
            "operation": "create",
            "name": "TestObject"
        }
    )
    
    assert create_response["success"] == True
    
    # GameObjectを削除
    delete_response = await client.call_tool(
        "unity_gameobject_crud",
        {
            "operation": "delete",
            "gameObjectPath": "TestObject"
        }
    )
    
    assert delete_response["success"] == True
```

## テストマーカー

pytest.iniで定義されているマーカー：

- `@pytest.mark.unit`: ユニットテスト
- `@pytest.mark.integration`: 統合テスト
- `@pytest.mark.e2e`: エンドツーエンドテスト
- `@pytest.mark.fast`: 高速なテスト（< 1秒）
- `@pytest.mark.slow`: 遅いテスト（> 5秒）
- `@pytest.mark.requires_unity`: Unity Editorが必要なテスト

## CI/CD

### GitHub Actions

```yaml
# .github/workflows/test.yml
name: Tests

on: [push, pull_request]

jobs:
  python-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-python@v4
        with:
          python-version: '3.10'
      - run: pip install -r requirements-dev.txt
      - run: pytest tests/python/unit/
  
  unity-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: game-ci/unity-test-runner@v2
        with:
          unityVersion: 2021.3.0f1
```

## トラブルシューティング

### Unity Editorに接続できない

1. Unity Editorが起動していることを確認
2. MCPBridgeが起動していることを確認（Tools > MCP Assistant）
3. WebSocketポート（7077）が開いていることを確認

### テストがタイムアウトする

```python
# タイムアウトを延長
@pytest.mark.timeout(60)  # 60秒
async def test_slow_operation():
    ...
```

### テストが失敗する

```bash
# 詳細な出力で実行
pytest tests/ -v -s

# 特定のテストのみ実行
pytest tests/python/unit/test_example.py::test_specific_function
```

## ベストプラクティス

### 1. テストの独立性

各テストは独立して実行可能にします。

```python
@pytest.fixture
def clean_scene():
    """各テスト前にシーンをクリーンアップ"""
    # セットアップ
    yield
    # ティアダウン：シーンをクリーンアップ
```

### 2. モックの使用

外部依存をモックします。

```python
@patch('src.bridge_manager.websocket')
def test_with_mock(mock_ws):
    """WebSocketをモックしてテスト"""
    mock_ws.connect.return_value = True
    # テストコード
```

### 3. パラメータ化テスト

複数の入力でテストします。

```python
@pytest.mark.parametrize("operation,expected", [
    ("create", True),
    ("delete", True),
    ("invalid", False)
])
def test_operations(operation, expected):
    result = perform_operation(operation)
    assert result["success"] == expected
```

### 4. 意味のあるアサーション

```python
# 悪い例
assert result

# 良い例
assert result["success"] == True, f"Operation failed: {result.get('error')}"
assert "gameObjectPath" in result
assert result["gameObjectPath"] == "Player"
```

## テストカバレッジ

カバレッジレポートを生成：

```bash
# HTML形式
pytest tests/ --cov=src --cov-report=html

# ターミナル出力
pytest tests/ --cov=src --cov-report=term

# XML形式（CI用）
pytest tests/ --cov=src --cov-report=xml
```

目標カバレッジ：
- ユニットテスト: 80%以上
- 統合テスト: 60%以上
- 全体: 70%以上

## 関連ドキュメント

- [API リファレンス](../docs/API.md)
- [開発ガイド](../docs/DEVELOPMENT.md)
- [コントリビューティングガイド](../CONTRIBUTING.md)


