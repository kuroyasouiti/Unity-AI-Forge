# SkillForUnity ツール使用例集

このドキュメントでは、各ツールの典型的な使用パターンと実例を紹介します。

## 目次

- [シーン管理](#シーン管理)
- [GameObject管理](#gameobject管理)
- [コンポーネント管理](#コンポーネント管理)
- [アセット管理](#アセット管理)
- [ScriptableObject管理](#scriptableobject管理)
- [UI管理](#ui管理)
- [Prefab管理](#prefab管理)
- [プロジェクト設定](#プロジェクト設定)
- [コード生成](#コード生成)

---

## シーン管理

### 新しいシーンを作成

```python
unity_scene_crud({
    "operation": "create",
    "scenePath": "Assets/Scenes/Level1.unity"
})
```

### シーンをロード（既存のシーンを置き換え）

```python
unity_scene_crud({
    "operation": "load",
    "scenePath": "Assets/Scenes/MainMenu.unity"
})
```

### シーンを追加ロード（現在のシーンに追加）

```python
unity_scene_crud({
    "operation": "load",
    "scenePath": "Assets/Scenes/GameUI.unity",
    "additive": True
})
```

### シーンの内容を検査

```python
unity_scene_crud({
    "operation": "inspect",
    "includeHierarchy": True,
    "includeComponents": True,
    "filter": "Player*"  # "Player"で始まるオブジェクトのみ
})
```

### ビルド設定にシーンを追加

```python
unity_scene_crud({
    "operation": "addToBuildSettings",
    "scenePath": "Assets/Scenes/Level1.unity",
    "index": 1,  # 2番目のシーンとして追加
    "enabled": True
})
```

---

## GameObject管理

### GameObjectを作成

```python
unity_gameobject_crud({
    "operation": "create",
    "name": "Player",
    "parentPath": "Game/Characters",
    "template": "Capsule"  # プリミティブを使用
})
```

### GameObjectを検査（全コンポーネント情報を取得）

```python
unity_gameobject_crud({
    "operation": "inspect",
    "gameObjectPath": "Player"
})
```

### GameObjectのプロパティを更新

```python
unity_gameobject_crud({
    "operation": "update",
    "gameObjectPath": "Player",
    "tag": "Player",
    "layer": "Characters",
    "active": True,
    "static": False
})
```

### パターンマッチで複数のGameObjectを検索

```python
unity_gameobject_crud({
    "operation": "findMultiple",
    "pattern": "Enemy*",  # "Enemy"で始まる全オブジェクト
    "maxResults": 50
})
```

### 複数のGameObjectを一括削除

```python
unity_gameobject_crud({
    "operation": "deleteMultiple",
    "pattern": "Temp_*",  # テンポラリオブジェクトを削除
    "useRegex": False
})
```

### 正規表現で複数のGameObjectを検査

```python
unity_gameobject_crud({
    "operation": "inspectMultiple",
    "pattern": "^(Player|Enemy)_\\d+$",  # Player_1, Enemy_2 など
    "useRegex": True,
    "includeComponents": True,
    "maxResults": 100
})
```

---

## コンポーネント管理

### コンポーネントを追加

```python
unity_component_crud({
    "operation": "add",
    "gameObjectPath": "Player",
    "componentType": "UnityEngine.Rigidbody",
    "propertyChanges": {
        "mass": 75.0,
        "drag": 0.5,
        "useGravity": True
    }
})
```

### コンポーネントのプロパティを更新

```python
unity_component_crud({
    "operation": "update",
    "gameObjectPath": "Player/Camera",
    "componentType": "UnityEngine.Camera",
    "propertyChanges": {
        "fieldOfView": 60,
        "clearFlags": 1,  # SolidColor
        "backgroundColor": {"r": 0.2, "g": 0.2, "b": 0.3, "a": 1.0}
    }
})
```

### コンポーネントの状態を検査

```python
unity_component_crud({
    "operation": "inspect",
    "gameObjectPath": "Player",
    "componentType": "UnityEngine.Transform",
    "propertyFilter": ["position", "rotation", "localScale"]
})
```

### 複数のGameObjectに同じコンポーネントを追加

```python
unity_component_crud({
    "operation": "addMultiple",
    "pattern": "Enemy_*",
    "componentType": "UnityEngine.BoxCollider",
    "propertyChanges": {
        "isTrigger": False,
        "size": {"x": 1, "y": 2, "z": 1}
    },
    "stopOnError": False,  # エラーがあっても続行
    "maxResults": 100
})
```

### 複数のコンポーネントを一括更新

```python
unity_component_crud({
    "operation": "updateMultiple",
    "pattern": "Light_*",
    "componentType": "UnityEngine.Light",
    "propertyChanges": {
        "intensity": 1.5,
        "color": {"r": 1.0, "g": 0.9, "b": 0.8, "a": 1.0}
    }
})
```

---

## アセット管理

### JSONファイルを作成

```python
unity_asset_crud({
    "operation": "create",
    "assetPath": "Assets/Data/config.json",
    "content": '{"version": "1.0", "maxPlayers": 4}'
})
```

### 既存のファイルを更新

```python
unity_asset_crud({
    "operation": "update",
    "assetPath": "Assets/Data/config.json",
    "content": '{"version": "1.1", "maxPlayers": 8, "allowBots": true}'
})
```

### テクスチャのインポート設定を変更

```python
unity_asset_crud({
    "operation": "updateImporter",
    "assetPath": "Assets/Textures/icon.png",
    "propertyChanges": {
        "textureType": 2,  # Sprite (2D and UI)
        "isReadable": False,
        "filterMode": 1,  # Bilinear
        "maxTextureSize": 2048
    }
})
```

### パターンマッチで複数のアセットを検索

```python
unity_asset_crud({
    "operation": "findMultiple",
    "pattern": "Assets/Audio/*.mp3",
    "maxResults": 50
})
```

### アセットを検査（インポーター設定を含む）

```python
unity_asset_crud({
    "operation": "inspect",
    "assetPath": "Assets/Models/character.fbx",
    "includeProperties": True
})
```

---

## ScriptableObject管理

### ScriptableObjectを作成

```python
unity_scriptableObject_crud({
    "operation": "create",
    "typeName": "MyGame.GameConfig",
    "assetPath": "Assets/Data/GameConfig.asset",
    "properties": {
        "gameName": "My Awesome Game",
        "version": "1.0.0",
        "maxPlayers": 4,
        "defaultDifficulty": 2
    }
})
```

### ScriptableObjectを検査

```python
unity_scriptableObject_crud({
    "operation": "inspect",
    "assetPath": "Assets/Data/GameConfig.asset",
    "includeProperties": True,
    "propertyFilter": ["version", "maxPlayers"]  # 特定のプロパティのみ
})
```

### ScriptableObjectを更新

```python
unity_scriptableObject_crud({
    "operation": "update",
    "assetPath": "Assets/Data/GameConfig.asset",
    "properties": {
        "version": "1.0.1",
        "maxPlayers": 8
    }
})
```

### 型でScriptableObjectを検索

```python
unity_scriptableObject_crud({
    "operation": "findByType",
    "typeName": "MyGame.ItemData",
    "searchPath": "Assets/Data/Items",
    "includeProperties": False,  # 一覧のみ（パフォーマンス向上）
    "maxResults": 100
})
```

### フォルダ内の全ScriptableObjectを一覧

```python
unity_scriptableObject_crud({
    "operation": "list",
    "searchPath": "Assets/Data",
    "typeName": "MyGame.GameConfig",  # オプション: 型でフィルタ
    "offset": 0,
    "maxResults": 50
})
```

### ScriptableObjectを複製

```python
unity_scriptableObject_crud({
    "operation": "duplicate",
    "sourceAssetPath": "Assets/Data/DefaultConfig.asset",
    "destinationAssetPath": "Assets/Data/CustomConfig.asset"
})
```

---

## UI管理

### Canvasにボタンを作成

```python
unity_ugui_createFromTemplate({
    "template": "Button",
    "name": "StartButton",
    "parentPath": "Canvas/MainMenu",
    "text": "Start Game",
    "width": 200,
    "height": 60,
    "anchorPreset": "middle-center"
})
```

### Textを作成

```python
unity_ugui_createFromTemplate({
    "template": "Text",
    "name": "ScoreText",
    "parentPath": "Canvas/HUD",
    "text": "Score: 0",
    "fontSize": 24,
    "width": 300,
    "height": 50,
    "anchorPreset": "top-right"
})
```

### RectTransformのアンカーを設定

```python
unity_ugui_manage({
    "operation": "setAnchorPreset",
    "gameObjectPath": "Canvas/Panel",
    "preset": "stretch-all",  # 親いっぱいに広がる
    "preservePosition": False
})
```

### RectTransformのプロパティを更新

```python
unity_ugui_manage({
    "operation": "updateRect",
    "gameObjectPath": "Canvas/Button",
    "anchoredPosition": {"x": 0, "y": -50},
    "sizeDelta": {"x": 200, "y": 60},
    "pivot": {"x": 0.5, "y": 0.5}
})
```

### レイアウトグループを追加

```python
unity_ugui_layoutManage({
    "operation": "add",
    "gameObjectPath": "Canvas/Panel",
    "layoutType": "VerticalLayoutGroup",
    "spacing": 10,
    "childAlignment": "MiddleCenter",
    "childControlWidth": True,
    "childControlHeight": False,
    "padding": {"left": 20, "right": 20, "top": 20, "bottom": 20}
})
```

### UI要素の重複を検出

```python
unity_ugui_detectOverlaps({
    "gameObjectPath": "Canvas/Panel",
    "includeChildren": True,
    "threshold": 10  # 10平方ピクセル以上の重複を検出
})
```

---

## Prefab管理

### GameObjectからPrefabを作成

```python
unity_prefab_crud({
    "operation": "create",
    "gameObjectPath": "Player",
    "prefabPath": "Assets/Prefabs/Player.prefab",
    "includeChildren": True
})
```

### Prefabをシーンにインスタンス化

```python
unity_prefab_crud({
    "operation": "instantiate",
    "prefabPath": "Assets/Prefabs/Enemy.prefab",
    "name": "Enemy_1",
    "parentPath": "Game/Enemies"
})
```

### Prefabインスタンスの変更をPrefabに適用

```python
unity_prefab_crud({
    "operation": "applyOverrides",
    "gameObjectPath": "Enemy_1"
})
```

### Prefabインスタンスの変更を元に戻す

```python
unity_prefab_crud({
    "operation": "revertOverrides",
    "gameObjectPath": "Enemy_1"
})
```

### Prefabインスタンスを通常のGameObjectに変換

```python
unity_prefab_crud({
    "operation": "unpack",
    "gameObjectPath": "Enemy_1",
    "unpackMode": "Completely"
})
```

---

## プロジェクト設定

### PlayerSettingsを読み取り

```python
unity_projectSettings_crud({
    "operation": "read",
    "category": "player",
    "property": "companyName"
})
```

### PlayerSettingsを更新

```python
unity_projectSettings_crud({
    "operation": "write",
    "category": "player",
    "property": "productName",
    "value": "My Awesome Game"
})
```

### タグを追加

```python
unity_tagLayer_manage({
    "operation": "addTag",
    "tag": "Collectible"
})
```

### GameObjectにタグを設定

```python
unity_tagLayer_manage({
    "operation": "setTag",
    "gameObjectPath": "Coin",
    "tag": "Collectible"
})
```

### レイヤーを追加

```python
unity_tagLayer_manage({
    "operation": "addLayer",
    "layer": "Characters"
})
```

### GameObject階層全体にレイヤーを設定

```python
unity_tagLayer_manage({
    "operation": "setLayerRecursive",
    "gameObjectPath": "Player",
    "layer": "Characters"
})
```

### Unity定数を変換

```python
unity_constant_convert({
    "operation": "enumToValue",
    "enumType": "UnityEngine.KeyCode",
    "enumValue": "Space"
})
# 戻り値: {"success": True, "value": 32}
```

---

## コード生成

### Singletonパターンを生成

```python
unity_designPattern_generate({
    "patternType": "singleton",
    "className": "GameManager",
    "scriptPath": "Assets/Scripts/GameManager.cs",
    "namespace": "MyGame.Core",
    "options": {
        "monoBehaviour": True,
        "persistent": True,
        "threadSafe": False
    }
})
```

### ObjectPoolパターンを生成

```python
unity_designPattern_generate({
    "patternType": "objectpool",
    "className": "BulletPool",
    "scriptPath": "Assets/Scripts/Pools/BulletPool.cs",
    "namespace": "MyGame.Pools",
    "options": {
        "pooledType": "Bullet",
        "defaultCapacity": "20",
        "maxSize": "100"
    }
})
```

### MonoBehaviourテンプレートを生成

```python
unity_script_template_generate({
    "templateType": "MonoBehaviour",
    "className": "PlayerController",
    "scriptPath": "Assets/Scripts/PlayerController.cs",
    "namespace": "MyGame.Player"
})
```

### ScriptableObjectテンプレートを生成

```python
unity_script_template_generate({
    "templateType": "ScriptableObject",
    "className": "ItemData",
    "scriptPath": "Assets/Scripts/Data/ItemData.cs",
    "namespace": "MyGame.Data"
})
```

### シーンを素早くセットアップ

```python
unity_scene_quickSetup({
    "setupType": "3D",
    "cameraPosition": {"x": 0, "y": 1, "z": -10},
    "lightIntensity": 1.2
})
```

### GameObjectをカスタマイズしてPrefab化

```python
unity_template_manage({
    "operation": "customize",
    "gameObjectPath": "Player",
    "components": [
        {
            "type": "UnityEngine.Rigidbody",
            "properties": {"mass": 75.0, "drag": 0.5}
        },
        {
            "type": "UnityEngine.CapsuleCollider",
            "properties": {"radius": 0.5, "height": 2.0}
        }
    ],
    "children": [
        {
            "name": "Model",
            "position": {"x": 0, "y": 0, "z": 0},
            "components": [
                {"type": "UnityEngine.MeshRenderer"}
            ]
        }
    ]
})

# その後、Prefab化
unity_template_manage({
    "operation": "convertToPrefab",
    "gameObjectPath": "Player",
    "prefabPath": "Assets/Prefabs/Player.prefab",
    "overwrite": False
})
```

### メニュー階層を作成

```python
unity_menu_hierarchyCreate({
    "menuName": "MainMenu",
    "menuStructure": {
        "Start Game": "Start Game",
        "Settings": {
            "text": "Settings",
            "submenus": {
                "Audio": "Audio Settings",
                "Video": "Video Settings",
                "Controls": "Controls"
            }
        },
        "Quit": "Quit Game"
    },
    "buttonWidth": 200,
    "buttonHeight": 50,
    "spacing": 10,
    "generateStateMachine": True,
    "stateMachineScriptPath": "Assets/Scripts/UI/MenuStateMachine.cs",
    "navigationMode": "both"
})
```

---

## 高度な使用例

### ゲームのプレイヤーキャラクターを完全にセットアップ

```python
# 1. GameObjectを作成
unity_gameobject_crud({
    "operation": "create",
    "name": "Player",
    "template": "Capsule"
})

# 2. コンポーネントを追加
unity_component_crud({
    "operation": "add",
    "gameObjectPath": "Player",
    "componentType": "UnityEngine.Rigidbody",
    "propertyChanges": {"mass": 75.0}
})

# 3. タグとレイヤーを設定
unity_tagLayer_manage({
    "operation": "setTag",
    "gameObjectPath": "Player",
    "tag": "Player"
})

unity_tagLayer_manage({
    "operation": "setLayer",
    "gameObjectPath": "Player",
    "layer": "Characters"
})

# 4. Prefab化
unity_prefab_crud({
    "operation": "create",
    "gameObjectPath": "Player",
    "prefabPath": "Assets/Prefabs/Player.prefab"
})
```

### 複数の敵を一括セットアップ

```python
# 1. 既存の敵を検索
enemies = unity_gameobject_crud({
    "operation": "findMultiple",
    "pattern": "Enemy_*"
})

# 2. 全ての敵にコライダーを追加
unity_component_crud({
    "operation": "addMultiple",
    "pattern": "Enemy_*",
    "componentType": "UnityEngine.BoxCollider",
    "propertyChanges": {"isTrigger": False}
})

# 3. 全ての敵にタグを設定
unity_tagLayer_manage({
    "operation": "setTag",
    "gameObjectPath": enemy["path"],  # 各敵に対して
    "tag": "Enemy"
})
```

---

## ベストプラクティス

### 1. バッチ操作を活用

複数のオブジェクトに同じ操作を行う場合は、`*Multiple`操作を使用します。

```python
# 良い例：バッチ操作
unity_component_crud({
    "operation": "addMultiple",
    "pattern": "Enemy_*",
    "componentType": "UnityEngine.Rigidbody"
})

# 悪い例：ループで個別に実行（遅い）
# for i in range(10):
#     unity_component_crud({
#         "operation": "add",
#         "gameObjectPath": f"Enemy_{i}",
#         "componentType": "UnityEngine.Rigidbody"
#     })
```

### 2. maxResultsで結果を制限

大量のオブジェクトを扱う場合は、`maxResults`を使用します。

```python
unity_gameobject_crud({
    "operation": "findMultiple",
    "pattern": "*",
    "maxResults": 100  # タイムアウトを防ぐ
})
```

### 3. パフォーマンス最適化

不要な情報は取得しないようにします。

```python
# 高速：コンポーネント情報なし
unity_gameobject_crud({
    "operation": "inspect",
    "gameObjectPath": "Player",
    "includeComponents": False
})

# 高速：特定のプロパティのみ
unity_component_crud({
    "operation": "inspect",
    "gameObjectPath": "Player",
    "componentType": "UnityEngine.Transform",
    "propertyFilter": ["position"]  # positionのみ取得
})
```

### 4. エラーハンドリング

バッチ操作では`stopOnError: False`を使用して、エラーがあっても続行します。

```python
unity_component_crud({
    "operation": "updateMultiple",
    "pattern": "Character_*",
    "componentType": "UnityEngine.Animator",
    "propertyChanges": {"speed": 1.5},
    "stopOnError": False  # 一部が失敗しても続行
})
```

### 5. GUID vs パス

重要なアセットは GUID で識別すると安全です。

```python
unity_scriptableObject_crud({
    "operation": "inspect",
    "assetGuid": "abc123def456",  # パスが変更されても動作
    "includeProperties": True
})
```

---

## さらなる情報

- [API リファレンス](./API.md) - 全ツールの詳細なドキュメント
- [クイックスタート](../.claude/skills/SkillForUnity/QUICKSTART.md) - 初めての方向け
- [ベストプラクティス](./API.md#best-practices) - 推奨される使用方法


