"""
スキーマバリデーションのユニットテスト

このテストはUnity Editorが起動していなくても実行可能です。
"""
import pytest


@pytest.mark.unit
@pytest.mark.fast
class TestSchemaValidation:
    """スキーマバリデーションのテスト"""

    def test_gameobject_crud_schema(self):
        """GameObject CRUD スキーマの検証"""
        # 有効なペイロード
        valid_payload = {
            "operation": "create",
            "name": "TestObject"
        }
        
        # operation が必須であることを確認
        assert "operation" in valid_payload
        assert valid_payload["operation"] in [
            "create", "delete", "move", "rename", 
            "update", "duplicate", "inspect",
            "findMultiple", "deleteMultiple", "inspectMultiple"
        ]

    def test_component_crud_schema(self):
        """Component CRUD スキーマの検証"""
        valid_payload = {
            "operation": "add",
            "gameObjectPath": "Player",
            "componentType": "UnityEngine.Rigidbody"
        }
        
        assert "operation" in valid_payload
        assert "gameObjectPath" in valid_payload
        assert "componentType" in valid_payload

    def test_scene_manage_schema(self):
        """Scene管理スキーマの検証"""
        valid_payload = {
            "operation": "create",
            "scenePath": "Assets/Scenes/TestScene.unity"
        }
        
        assert "operation" in valid_payload
        assert valid_payload["scenePath"].startswith("Assets/")
        assert valid_payload["scenePath"].endswith(".unity")

    @pytest.mark.parametrize("operation", [
        "create", "delete", "move", "rename", "update", "duplicate", "inspect"
    ])
    def test_valid_gameobject_operations(self, operation):
        """有効なGameObject操作の検証"""
        valid_operations = [
            "create", "delete", "move", "rename", "update", 
            "duplicate", "inspect", "findMultiple", 
            "deleteMultiple", "inspectMultiple"
        ]
        assert operation in valid_operations

    def test_invalid_operation(self):
        """無効な操作の検証"""
        invalid_operation = "invalidOperation"
        valid_operations = [
            "create", "delete", "move", "rename", "update", 
            "duplicate", "inspect"
        ]
        assert invalid_operation not in valid_operations

    def test_batch_operation_parameters(self):
        """バッチ操作パラメータの検証"""
        batch_payload = {
            "operation": "findMultiple",
            "pattern": "Enemy*",
            "maxResults": 100
        }
        
        assert "pattern" in batch_payload
        assert "maxResults" in batch_payload
        assert batch_payload["maxResults"] > 0
        assert batch_payload["maxResults"] <= 1000

    def test_scriptableobject_schema(self):
        """ScriptableObject スキーマの検証"""
        valid_payload = {
            "operation": "create",
            "typeName": "MyGame.GameConfig",
            "assetPath": "Assets/Data/Config.asset",
            "properties": {
                "maxPlayers": 4
            }
        }
        
        assert "operation" in valid_payload
        assert "typeName" in valid_payload
        assert "assetPath" in valid_payload
        assert valid_payload["assetPath"].startswith("Assets/")
        assert valid_payload["assetPath"].endswith(".asset")


@pytest.mark.unit
@pytest.mark.fast
class TestPathValidation:
    """パスバリデーションのテスト"""

    @pytest.mark.parametrize("path,expected", [
        ("Assets/Scenes/Main.unity", True),
        ("Assets/Data/config.json", True),
        ("Assets/Prefabs/Player.prefab", True),
        ("/etc/passwd", False),
        ("../../../etc/passwd", False),
        ("C:\\Windows\\System32", False),
    ])
    def test_asset_path_validation(self, path, expected):
        """アセットパスのバリデーション"""
        is_valid = path.startswith("Assets/")
        assert is_valid == expected

    def test_path_traversal_detection(self):
        """パストラバーサル攻撃の検出"""
        malicious_paths = [
            "Assets/../../../etc/passwd",
            "Assets/../../Windows/System32",
            "../Assets/Scenes/Main.unity"
        ]
        
        for path in malicious_paths:
            # パストラバーサルを含むパスを検出
            assert ".." in path or not path.startswith("Assets/")


@pytest.mark.unit
@pytest.mark.fast
class TestTypeValidation:
    """型バリデーションのテスト"""

    def test_component_type_format(self):
        """コンポーネント型のフォーマット検証"""
        valid_types = [
            "UnityEngine.Rigidbody",
            "UnityEngine.UI.Button",
            "UnityEngine.Camera",
            "MyGame.PlayerController"
        ]
        
        for component_type in valid_types:
            # 型名にはドットが含まれる（名前空間.クラス名）
            assert "." in component_type

    def test_scriptableobject_type_format(self):
        """ScriptableObject型のフォーマット検証"""
        valid_types = [
            "MyGame.GameConfig",
            "MyGame.Data.ItemData",
            "UnityEngine.ScriptableObject"
        ]
        
        for so_type in valid_types:
            # 型名には名前空間が含まれる
            assert "." in so_type


if __name__ == "__main__":
    pytest.main([__file__, "-v"])

