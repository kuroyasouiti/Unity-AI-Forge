# pytest設定ファイル
import pytest
import asyncio

# pytest-asyncioの設定
pytest_plugins = ('pytest_asyncio',)

def pytest_configure(config):
    """pytestマーカーを登録"""
    config.addinivalue_line("markers", "unit: ユニットテスト")
    config.addinivalue_line("markers", "integration: 統合テスト")
    config.addinivalue_line("markers", "e2e: エンドツーエンドテスト")
    config.addinivalue_line("markers", "fast: 高速なテスト（< 1秒）")
    config.addinivalue_line("markers", "slow: 遅いテスト（> 5秒）")
    config.addinivalue_line("markers", "requires_unity: Unity Editorが必要なテスト")

@pytest.fixture
def event_loop():
    """イベントループフィクスチャ"""
    loop = asyncio.get_event_loop_policy().new_event_loop()
    yield loop
    loop.close()

@pytest.fixture
def sample_gameobject_payload():
    """GameObjectペイロードのサンプル"""
    return {
        "operation": "create",
        "name": "TestObject",
        "parentPath": "",
        "template": "Empty"
    }

@pytest.fixture
def sample_component_payload():
    """Componentペイロードのサンプル"""
    return {
        "operation": "add",
        "gameObjectPath": "TestObject",
        "componentType": "UnityEngine.Rigidbody",
        "propertyChanges": {
            "mass": 1.0,
            "useGravity": True
        }
    }

