# エラーコード標準化ガイドライン

SkillForUnityのエラーハンドリングを一貫性のあるものにするためのガイドラインです。

## エラーコード体系

### フォーマット

```
{CATEGORY}_{SUBCATEGORY}_{NUMBER}
```

- **CATEGORY**: エラーカテゴリ（3-4文字の略語）
- **SUBCATEGORY**: サブカテゴリ（2-3文字の略語）
- **NUMBER**: 連番（001-999）

### 例

```
GOBJ_NOT_FOUND_001: GameObject not found: 'Player'
COMP_TYPE_INVALID_001: Invalid component type: 'UnityEngine.InvalidComponent'
ASSET_PATH_INVALID_001: Asset path must start with 'Assets/'
```

---

## カテゴリ一覧

### 1. GOBJ - GameObject関連エラー

| コード | 説明 | 例 |
|--------|------|-----|
| `GOBJ_NOT_FOUND_001` | GameObjectが見つからない | "GameObject not found: 'Player'" |
| `GOBJ_ALREADY_EXISTS_001` | GameObjectが既に存在 | "GameObject already exists: 'Player'" |
| `GOBJ_INVALID_PATH_001` | GameObject階層パスが不正 | "Invalid GameObject path: '//Player'" |
| `GOBJ_INVALID_NAME_001` | GameObject名が不正 | "Invalid GameObject name: ''" |
| `GOBJ_PARENT_NOT_FOUND_001` | 親GameObjectが見つからない | "Parent GameObject not found: 'NonExistent'" |

### 2. COMP - Component関連エラー

| コード | 説明 | 例 |
|--------|------|-----|
| `COMP_TYPE_INVALID_001` | コンポーネント型が不正 | "Invalid component type: 'InvalidType'" |
| `COMP_NOT_FOUND_001` | コンポーネントが見つからない | "Component not found: 'Rigidbody'" |
| `COMP_ALREADY_EXISTS_001` | コンポーネントが既に存在 | "Component already exists: 'Camera'" |
| `COMP_PROP_INVALID_001` | プロパティが不正 | "Invalid property: 'invalidProperty'" |
| `COMP_PROP_TYPE_MISMATCH_001` | プロパティ型が一致しない | "Property type mismatch: expected float, got string" |
| `COMP_PROP_READONLY_001` | 読み取り専用プロパティ | "Property is read-only: 'name'" |

### 3. ASSET - Asset関連エラー

| コード | 説明 | 例 |
|--------|------|-----|
| `ASSET_PATH_INVALID_001` | アセットパスが不正 | "Asset path must start with 'Assets/'" |
| `ASSET_NOT_FOUND_001` | アセットが見つからない | "Asset not found: 'Assets/Textures/icon.png'" |
| `ASSET_ALREADY_EXISTS_001` | アセットが既に存在 | "Asset already exists: 'Assets/Config.json'" |
| `ASSET_GUID_INVALID_001` | GUIDが不正 | "Invalid GUID: 'invalid-guid'" |
| `ASSET_TYPE_INVALID_001` | アセット型が不正 | "Invalid asset type for operation" |
| `ASSET_PATH_TRAVERSAL_001` | パストラバーサル検出 | "Path traversal detected: '../../../etc/passwd'" |
| `ASSET_CS_RESTRICTED_001` | .csファイル操作制限 | "C# file operations restricted. Use code editor tools." |

### 4. SCENE - Scene関連エラー

| コード | 説明 | 例 |
|--------|------|-----|
| `SCENE_NOT_FOUND_001` | シーンが見つからない | "Scene not found: 'Assets/Scenes/Level1.unity'" |
| `SCENE_ALREADY_EXISTS_001` | シーンが既に存在 | "Scene already exists: 'Assets/Scenes/NewScene.unity'" |
| `SCENE_INVALID_PATH_001` | シーンパスが不正 | "Scene path must end with '.unity'" |
| `SCENE_NOT_LOADED_001` | シーンがロードされていない | "Scene not loaded" |
| `SCENE_BUILD_INDEX_INVALID_001` | ビルドインデックスが不正 | "Invalid build settings index: -1" |

### 5. PREFAB - Prefab関連エラー

| コード | 説明 | 例 |
|--------|------|-----|
| `PREFAB_NOT_FOUND_001` | Prefabが見つからない | "Prefab not found: 'Assets/Prefabs/Player.prefab'" |
| `PREFAB_INVALID_PATH_001` | Prefabパスが不正 | "Prefab path must end with '.prefab'" |
| `PREFAB_NOT_INSTANCE_001` | GameObjectがPrefabインスタンスではない | "GameObject is not a prefab instance" |
| `PREFAB_HAS_NO_OVERRIDES_001` | Prefabにオーバーライドがない | "Prefab instance has no overrides to apply" |

### 6. SCROBJ - ScriptableObject関連エラー

| コード | 説明 | 例 |
|--------|------|-----|
| `SCROBJ_TYPE_INVALID_001` | ScriptableObject型が不正 | "Invalid ScriptableObject type: 'InvalidType'" |
| `SCROBJ_NOT_FOUND_001` | ScriptableObjectが見つからない | "ScriptableObject not found: 'Assets/Data/Config.asset'" |
| `SCROBJ_PROP_FAILED_001` | プロパティ設定失敗 | "Failed to set property: 'maxPlayers'" |
| `SCROBJ_TYPE_NOT_SCROBJ_001` | ScriptableObject型ではない | "Type does not derive from ScriptableObject" |

### 7. UI - UI関連エラー

| コード | 説明 | 例 |
|--------|------|-----|
| `UI_NO_RECT_TRANSFORM_001` | RectTransformがない | "GameObject does not have a RectTransform" |
| `UI_NO_CANVAS_001` | Canvasが見つからない | "No Canvas found in scene" |
| `UI_ANCHOR_INVALID_001` | アンカー値が不正 | "Anchor values must be between 0 and 1" |
| `UI_LAYOUT_TYPE_INVALID_001` | レイアウト型が不正 | "Invalid layout type: 'InvalidLayout'" |

### 8. PROJ - Project設定関連エラー

| コード | 説明 | 例 |
|--------|------|-----|
| `PROJ_CATEGORY_INVALID_001` | 設定カテゴリが不正 | "Invalid settings category: 'invalid'" |
| `PROJ_PROP_INVALID_001` | プロパティが不正 | "Invalid property: 'invalidProperty'" |
| `PROJ_TAG_EXISTS_001` | タグが既に存在 | "Tag already exists: 'Player'" |
| `PROJ_LAYER_EXISTS_001` | レイヤーが既に存在 | "Layer already exists: 'Characters'" |
| `PROJ_TAG_LIMIT_001` | タグ数上限 | "Maximum tag limit reached" |
| `PROJ_LAYER_LIMIT_001` | レイヤー数上限 | "Maximum layer limit reached (32)" |

### 9. COMP_WAIT - コンパイル関連エラー

| コード | 説明 | 例 |
|--------|------|-----|
| `COMP_TIMEOUT_001` | コンパイルタイムアウト | "Compilation timeout after 60 seconds" |
| `COMP_FAILED_001` | コンパイル失敗 | "Compilation failed with 3 errors" |
| `COMP_IN_PROGRESS_001` | コンパイル中 | "Compilation is already in progress" |

### 10. GEN - コード生成関連エラー

| コード | 説明 | 例 |
|--------|------|-----|
| `GEN_PATTERN_INVALID_001` | デザインパターン型が不正 | "Invalid pattern type: 'invalid'" |
| `GEN_TEMPLATE_INVALID_001` | テンプレート型が不正 | "Invalid template type: 'invalid'" |
| `GEN_CLASSNAME_INVALID_001` | クラス名が不正 | "Invalid class name: '123Invalid'" |
| `GEN_SCRIPT_EXISTS_001` | スクリプトが既に存在 | "Script already exists: 'GameManager.cs'" |

### 11. VAL - バリデーション関連エラー

| コード | 説明 | 例 |
|--------|------|-----|
| `VAL_PARAM_MISSING_001` | 必須パラメータ不足 | "Missing required parameter: 'operation'" |
| `VAL_PARAM_INVALID_001` | パラメータが不正 | "Invalid parameter value: 'operation'" |
| `VAL_ENUM_INVALID_001` | Enum値が不正 | "Invalid enum value: 'InvalidOperation'" |
| `VAL_TYPE_MISMATCH_001` | 型が一致しない | "Type mismatch: expected string, got integer" |

### 12. RUNTIME - 実行時エラー

| コード | 説明 | 例 |
|--------|------|-----|
| `RUNTIME_EXCEPTION_001` | 予期しない例外 | "Unexpected exception occurred" |
| `RUNTIME_NULL_REF_001` | Null参照 | "Null reference exception" |
| `RUNTIME_ACCESS_DENIED_001` | アクセス拒否 | "Access denied to protected resource" |

---

## 実装ガイドライン

### 1. エラーメッセージの構造

```csharp
throw new InvalidOperationException(
    $"[GOBJ_NOT_FOUND_001] GameObject not found: '{gameObjectPath}'. " +
    $"Please verify the hierarchy path is correct."
);
```

**構成要素**:
- `[エラーコード]`: エラーを一意に識別
- `基本メッセージ`: 何が問題かを簡潔に
- `詳細情報`: 具体的な値（パス、名前など）
- `ヒント`: 解決方法の提案（オプション）

### 2. エラーレスポンスの標準フォーマット

```json
{
  "success": false,
  "error": {
    "code": "GOBJ_NOT_FOUND_001",
    "message": "GameObject not found: 'Player'",
    "details": {
      "gameObjectPath": "Player",
      "searchedInScene": "SampleScene"
    },
    "suggestion": "Verify the GameObject exists in the scene hierarchy"
  }
}
```

### 3. コード例（C#）

```csharp
// 良い例
public static GameObject ResolveGameObject(string path)
{
    if (string.IsNullOrEmpty(path))
    {
        throw new InvalidOperationException(
            "[GOBJ_INVALID_PATH_001] GameObject path cannot be null or empty."
        );
    }

    GameObject go = GameObject.Find(path);
    if (go == null)
    {
        throw new InvalidOperationException(
            $"[GOBJ_NOT_FOUND_001] GameObject not found: '{path}'. " +
            $"Scene: '{SceneManager.GetActiveScene().name}'"
        );
    }

    return go;
}
```

### 4. コード例（Python）

```python
def handle_error_response(error_message: str) -> dict:
    """エラーメッセージからエラーコードを抽出し、構造化されたレスポンスを返す"""
    
    # エラーコードを抽出
    import re
    match = re.search(r'\[([A-Z_0-9]+)\]', error_message)
    code = match.group(1) if match else "UNKNOWN_ERROR_001"
    
    # メッセージからコードを削除
    clean_message = re.sub(r'\[([A-Z_0-9]+)\]\s*', '', error_message)
    
    return {
        "success": False,
        "error": {
            "code": code,
            "message": clean_message
        }
    }
```

---

## エラーカテゴリの追加

新しいエラーカテゴリを追加する際の手順：

1. **カテゴリ名を決定**: 3-4文字の略語（例: `ANIM` for Animation）
2. **エラーコード範囲を予約**: 001-099 など
3. **このドキュメントに追加**: 新しいセクションとテーブルを作成
4. **コードに実装**: 新しいエラーコードを使用
5. **テストを追加**: エラーコードのテストケースを作成

---

## ベストプラクティス

### 1. ユーザーフレンドリーなメッセージ

```csharp
// 悪い例
throw new Exception("Error");

// 良い例
throw new InvalidOperationException(
    "[COMP_NOT_FOUND_001] Component 'Rigidbody' not found on GameObject 'Player'. " +
    "Add the component using unity_component_crud with operation='add'."
);
```

### 2. コンテキスト情報を含める

```csharp
// 悪い例
throw new Exception("Invalid value");

// 良い例
throw new InvalidOperationException(
    $"[VAL_PARAM_INVALID_001] Invalid value for parameter 'maxResults': {value}. " +
    $"Expected: 1-10000, Got: {value}"
);
```

### 3. エラーコードの一貫性

同じ種類のエラーには同じエラーコードを使用します。

```csharp
// GameObject not found エラーは常に GOBJ_NOT_FOUND_001 を使用
if (go == null)
{
    throw new InvalidOperationException(
        $"[GOBJ_NOT_FOUND_001] GameObject not found: '{path}'"
    );
}
```

### 4. ログにエラーコードを含める

```csharp
Debug.LogError($"[GOBJ_NOT_FOUND_001] GameObject not found: '{path}'");
```

---

## エラーコードリファレンス（完全版）

| カテゴリ | コード範囲 | 説明 |
|---------|-----------|------|
| GOBJ | 001-099 | GameObject操作エラー |
| COMP | 001-099 | Component操作エラー |
| ASSET | 001-099 | Asset操作エラー |
| SCENE | 001-099 | Scene操作エラー |
| PREFAB | 001-099 | Prefab操作エラー |
| SCROBJ | 001-099 | ScriptableObject操作エラー |
| UI | 001-099 | UI操作エラー |
| PROJ | 001-099 | Project設定エラー |
| COMP_WAIT | 001-099 | コンパイル関連エラー |
| GEN | 001-099 | コード生成エラー |
| VAL | 001-099 | バリデーションエラー |
| RUNTIME | 001-099 | 実行時エラー |

---

## 今後の拡張

将来的に以下のカテゴリを追加する可能性があります：

- `ANIM_*`: Animation関連
- `PHYS_*`: Physics関連
- `AUDIO_*`: Audio関連
- `LIGHT_*`: Lighting関連
- `REND_*`: Rendering関連
- `NET_*`: Networking関連
- `INPUT_*`: Input関連

---

## 関連ドキュメント

- [API リファレンス](./API.md#error-handling) - エラーハンドリングの詳細
- [ベストプラクティス](./API.md#best-practices) - 推奨される使用方法


