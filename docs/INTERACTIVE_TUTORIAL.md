# SkillForUnity 対話型チュートリアル

このチュートリアルでは、SkillForUnityを使って実際のゲーム開発タスクを段階的に学習します。

## 🎯 チュートリアルの目的

- SkillForUnityの基本的な使い方を学ぶ
- 実践的なゲーム開発タスクを体験する
- AIアシスタントとUnityの連携を理解する

## 📋 前提条件

- ✅ Unity Editor 2021.3以降がインストール済み
- ✅ SkillForUnityのセットアップ完了
- ✅ MCPBridgeが起動している
- ✅ Claudeまたは互換AIアシスタントが利用可能

---

## 🚀 チュートリアル1: はじめてのGameObject

**所要時間**: 5分  
**難易度**: ⭐☆☆☆☆

### 学習内容

- GameObjectの作成
- 基本的なコンポーネントの追加
- プロパティの設定

### ステップ1: シーンの準備

Claudeに以下のように依頼してください：

```
新しい3Dシーンを作成して、"TutorialScene"という名前で保存してください。
パスは Assets/Scenes/TutorialScene.unity です。
```

**期待される動作**:
- 新しい空の3Dシーンが作成される
- Main CameraとDirectional Lightが自動的に配置される

<details>
<summary>💡 ヒント（クリックして表示）</summary>

Claudeは以下のツールを使用します：
- `unity_scene_quickSetup` で3Dシーンを作成
- `unity_scene_crud` でシーンを保存

</details>

### ステップ2: プレイヤーキャラクターを作成

```
"Player"という名前のCapsule型のGameObjectを作成して、
位置を (0, 1, 0) に設定してください。
```

**期待される動作**:
- Capsule型のGameObjectが作成される
- シーンビューで確認できる

<details>
<summary>💡 ヒント</summary>

使用されるツール：
- `unity_gameobject_createFromTemplate` でCapsuleを作成
- 位置は自動的に設定される

</details>

### ステップ3: Rigidbodyを追加

```
Playerに Rigidbody コンポーネントを追加して、
mass を 75、useGravity を true に設定してください。
```

**期待される動作**:
- Rigidbodyコンポーネントが追加される
- 物理演算が有効になる

### ステップ4: タグを設定

```
PlayerにPlayerタグを設定してください。
```

**確認方法**:
1. UnityでPlayerオブジェクトを選択
2. Inspectorでタグが"Player"になっていることを確認

### 🎉 チュートリアル1完了！

以下を学びました：
- ✅ GameObjectの作成
- ✅ コンポーネントの追加
- ✅ プロパティの設定
- ✅ タグの設定

---

## 🎮 チュートリアル2: ゲームUIの作成

**所要時間**: 10分  
**難易度**: ⭐⭐☆☆☆

### 学習内容

- Canvasの作成
- UIボタンの配置
- レイアウトグループの使用

### ステップ1: UIシーンをセットアップ

```
UIシーンをセットアップして、Canvasを作成してください。
```

**期待される動作**:
- CanvasとEventSystemが作成される
- Canvas Scalerが設定される

### ステップ2: メインメニューパネルを作成

```
Canvas の下に "MainMenu" という名前のPanelを作成してください。
アンカーはstretch-allに設定してください。
```

### ステップ3: ボタンを作成

```
MainMenuパネルの下に以下のボタンを作成してください：
1. "StartButton" - テキスト: "Start Game"
2. "SettingsButton" - テキスト: "Settings"
3. "QuitButton" - テキスト: "Quit"

全てのボタンのサイズは幅200、高さ60にしてください。
```

### ステップ4: レイアウトグループを追加

```
MainMenuパネルにVerticalLayoutGroupを追加して、
ボタンを縦に並べてください。
spacing は 20、childAlignment は MiddleCenter にしてください。
```

**確認方法**:
1. Unityでゲームビューを確認
2. ボタンが縦に並んでいることを確認

### 🎉 チュートリアル2完了！

以下を学びました：
- ✅ UIシーンのセットアップ
- ✅ Panelとボタンの作成
- ✅ レイアウトグループの使用

---

## 🏗️ チュートリアル3: Prefabワークフロー

**所要時間**: 15分  
**難易度**: ⭐⭐⭐☆☆

### 学習内容

- GameObjectからPrefabを作成
- Prefabのインスタンス化
- Prefabオーバーライドの管理

### ステップ1: 敵キャラクターを作成

```
"Enemy"という名前のCube型のGameObjectを作成してください。
位置は (0, 0.5, 5) にしてください。
```

### ステップ2: コンポーネントを追加

```
Enemyに以下のコンポーネントを追加してください：
1. Rigidbody - mass: 50, useGravity: true
2. BoxCollider - isTrigger: false
```

### ステップ3: Prefabを作成

```
EnemyをPrefab化して、Assets/Prefabs/Enemy.prefab として保存してください。
```

### ステップ4: Prefabをインスタンス化

```
Enemy Prefabを3つインスタンス化して、
それぞれ異なる位置に配置してください：
- Enemy_1: (0, 0.5, 5)
- Enemy_2: (3, 0.5, 5)
- Enemy_3: (-3, 0.5, 5)
```

### ステップ5: インスタンスを変更

```
Enemy_1 のみ、色を赤に変更してください
（Renderer.material.color を変更）。
```

### ステップ6: 変更をPrefabに適用

```
Enemy_1の変更をPrefabに適用してください。
他のインスタンスも赤くなることを確認してください。
```

### 🎉 チュートリアル3完了！

以下を学びました：
- ✅ Prefabの作成
- ✅ Prefabのインスタンス化
- ✅ Prefabオーバーライドの管理

---

## 📊 チュートリアル4: ScriptableObjectでゲームデータを管理

**所要時間**: 15分  
**難易度**: ⭐⭐⭐☆☆

### 学習内容

- ScriptableObjectの作成
- データの保存と読み込み
- 型ベースの検索

### ステップ1: ScriptableObjectクラスを作成

まず、以下のC#クラスを作成してください（コードエディタで）：

```csharp
// Assets/Scripts/Data/ItemData.cs
using UnityEngine;

namespace TutorialGame.Data
{
    [CreateAssetMenu(fileName = "NewItem", menuName = "Tutorial/Item Data")]
    public class ItemData : ScriptableObject
    {
        public string itemName;
        public int price;
        public string description;
    }
}
```

Claudeに依頼する場合：

```
ScriptableObjectテンプレートを使って、
TutorialGame.Data.ItemData クラスを作成してください。
パスは Assets/Scripts/Data/ItemData.cs です。
```

### ステップ2: コンパイルを待つ

```
Unityのコンパイルが完了するまで待ってください。
```

### ステップ3: ScriptableObjectインスタンスを作成

```
以下のItemDataアセットを作成してください：

1. Assets/Data/Items/Sword.asset
   - itemName: "Iron Sword"
   - price: 100
   - description: "A basic iron sword"

2. Assets/Data/Items/Potion.asset
   - itemName: "Health Potion"
   - price: 50
   - description: "Restores 50 HP"

3. Assets/Data/Items/Shield.asset
   - itemName: "Wooden Shield"
   - price: 75
   - description: "A simple wooden shield"
```

### ステップ4: アイテムを検索

```
TutorialGame.Data.ItemData型の全てのScriptableObjectを検索して、
一覧を表示してください。
```

### ステップ5: アイテムデータを更新

```
Sword の price を 150 に値上げしてください。
```

### 🎉 チュートリアル4完了！

以下を学びました：
- ✅ ScriptableObjectクラスの作成
- ✅ インスタンスの作成と管理
- ✅ データの更新
- ✅ 型ベース検索

---

## 🎨 チュートリアル5: バッチ操作で効率化

**所要時間**: 10分  
**難易度**: ⭐⭐⭐⭐☆

### 学習内容

- パターンマッチング
- バッチ操作
- パフォーマンス最適化

### ステップ1: 複数のオブジェクトを作成

```
10個のCubeを作成して、"Obstacle_1"から"Obstacle_10"という名前にしてください。
位置はランダムに配置してください（X: -10〜10, Y: 0.5, Z: -10〜10）。
```

### ステップ2: パターンマッチで検索

```
"Obstacle_"で始まる全てのGameObjectを検索して、
その数を教えてください。
```

### ステップ3: 一括でコンポーネントを追加

```
"Obstacle_"で始まる全てのGameObjectに、
BoxColliderを追加してください。
```

### ステップ4: プロパティを一括更新

```
全てのObstacleのLayerを"Environment"に変更してください
（Layerが存在しない場合は作成してください）。
```

### ステップ5: 一括削除

```
"Obstacle_"で始まる全てのGameObjectを削除してください。
```

### 🎉 チュートリアル5完了！

以下を学びました：
- ✅ パターンマッチング
- ✅ バッチ操作
- ✅ 効率的なオブジェクト管理

---

## 🏆 最終チャレンジ: シンプルなゲームを作成

**所要時間**: 30分  
**難易度**: ⭐⭐⭐⭐⭐

### 目標

SkillForUnityを使って、以下の要素を含むシンプルなゲームを作成します：

1. **プレイヤーキャラクター**
   - Capsule型
   - Rigidbody、Collider付き
   - "Player"タグ

2. **地面**
   - Plane型
   - 適切なサイズ

3. **敵キャラクター**
   - 5体のCube型敵
   - Prefab化
   - "Enemy"タグ

4. **収集アイテム**
   - 10個のSphere型アイテム
   - "Collectible"タグ

5. **UI**
   - スコア表示テキスト
   - "Score: 0"から開始

6. **ゲームデータ**
   - ScriptableObjectで敵の設定を管理
   - 体力、速度などのパラメータ

### チャレンジ開始！

以下の順番で作業を進めてください：

```
1. 新しい3Dシーンを作成
2. プレイヤーを配置
3. 地面を作成
4. 敵Prefabを作成してインスタンス化
5. 収集アイテムを配置
6. UIを作成
7. ゲームデータScriptableObjectを作成
```

### ボーナスチャレンジ 🎯

- [ ] ライティングを調整して雰囲気を作る
- [ ] Skyboxを設定する
- [ ] ビルド設定にシーンを追加
- [ ] メインメニューシーンを作成

---

## 📚 次のステップ

チュートリアルを完了したあなたは、SkillForUnityの基本をマスターしました！

### さらに学ぶ

1. **API リファレンス** - 全ツールの詳細な使い方
   - [docs/API.md](./API.md)

2. **ツール使用例集** - 実践的なコード例
   - [docs/TOOL_EXAMPLES.md](./TOOL_EXAMPLES.md)

3. **ベストプラクティス** - 効率的な使い方
   - [docs/API.md#best-practices](./API.md#best-practices)

### コミュニティ

- 💬 [GitHub Discussions](https://github.com/kuroyasouiti/SkillForUnity/discussions)
- 🐛 [Issue Tracker](https://github.com/kuroyasouiti/SkillForUnity/issues)
- 📖 [Wiki](https://github.com/kuroyasouiti/SkillForUnity/wiki)

---

## 🎓 修了証

このチュートリアルを完了したあなたは、以下のスキルを習得しました：

- ✅ SkillForUnityの基本操作
- ✅ GameObjectとコンポーネントの管理
- ✅ UIの作成と配置
- ✅ Prefabワークフロー
- ✅ ScriptableObjectでのデータ管理
- ✅ バッチ操作による効率化
- ✅ AIアシスタントとの協働開発

**おめでとうございます！🎉**

---

## ❓ トラブルシューティング

### Unity Editorに接続できない

1. Unity Editorが起動していることを確認
2. MCPBridgeが起動していることを確認（Tools > MCP Assistant）
3. ポート7077が空いていることを確認

### コマンドが失敗する

1. エラーメッセージを確認
2. オブジェクト名やパスが正しいか確認
3. 必要なコンポーネントが存在するか確認

### コンパイルエラー

1. エラーメッセージを読む
2. Claudeにエラーの修正を依頼
3. 必要に応じて手動で修正

---

## 📝 フィードバック

このチュートリアルについてのフィードバックをお待ちしています：

- 👍 良かった点
- 👎 改善してほしい点
- 💡 追加してほしい内容

[GitHub Issues](https://github.com/kuroyasouiti/SkillForUnity/issues) でお知らせください！


