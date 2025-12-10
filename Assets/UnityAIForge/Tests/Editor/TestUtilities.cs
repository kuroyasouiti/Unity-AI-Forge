using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor.Tests
{
    /// <summary>
    /// テスト用のユーティリティクラス。
    /// テスト全体で共通して使用するヘルパーメソッドを提供します。
    /// </summary>
    public static class TestUtilities
    {
        /// <summary>
        /// テスト用の一時ディレクトリパス。
        /// </summary>
        public const string TestAssetsPath = "Assets/TestTemp";

        /// <summary>
        /// テスト用の一時ディレクトリを作成します。
        /// </summary>
        public static void CreateTestDirectory()
        {
            if (!Directory.Exists(TestAssetsPath))
            {
                Directory.CreateDirectory(TestAssetsPath);
                AssetDatabase.Refresh();
            }
        }

        /// <summary>
        /// テスト用の一時ディレクトリを削除します。
        /// </summary>
        public static void CleanupTestDirectory()
        {
            if (Directory.Exists(TestAssetsPath))
            {
                AssetDatabase.DeleteAsset(TestAssetsPath);
                AssetDatabase.Refresh();
            }
        }

        /// <summary>
        /// テスト用のGameObjectを作成します。
        /// </summary>
        /// <param name="name">GameObjectの名前</param>
        /// <param name="parent">親Transform（オプション）</param>
        /// <returns>作成されたGameObject</returns>
        public static GameObject CreateGameObject(string name, Transform parent = null)
        {
            var go = new GameObject(name);
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }
            return go;
        }

        /// <summary>
        /// テスト用のコンポーネント付きGameObjectを作成します。
        /// </summary>
        /// <typeparam name="T">追加するコンポーネントの型</typeparam>
        /// <param name="name">GameObjectの名前</param>
        /// <returns>作成されたGameObjectとコンポーネントのタプル</returns>
        public static (GameObject gameObject, T component) CreateGameObjectWithComponent<T>(string name) 
            where T : Component
        {
            var go = new GameObject(name);
            var component = go.AddComponent<T>();
            return (go, component);
        }

        /// <summary>
        /// テスト用の階層構造を作成します。
        /// </summary>
        /// <param name="rootName">ルートGameObjectの名前</param>
        /// <param name="childNames">子GameObjectの名前リスト</param>
        /// <returns>ルートGameObject</returns>
        public static GameObject CreateHierarchy(string rootName, params string[] childNames)
        {
            var root = new GameObject(rootName);
            foreach (var childName in childNames)
            {
                var child = new GameObject(childName);
                child.transform.SetParent(root.transform, false);
            }
            return root;
        }

        /// <summary>
        /// GameObjectの階層パスを取得します。
        /// </summary>
        /// <param name="gameObject">対象のGameObject</param>
        /// <returns>階層パス</returns>
        public static string GetHierarchyPath(GameObject gameObject)
        {
            if (gameObject == null)
                return null;

            var path = gameObject.name;
            var parent = gameObject.transform.parent;

            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }

        /// <summary>
        /// テスト用のペイロードを作成します。
        /// </summary>
        /// <param name="operation">操作名</param>
        /// <param name="additionalParams">追加パラメータ</param>
        /// <returns>ペイロード辞書</returns>
        public static Dictionary<string, object> CreatePayload(
            string operation, 
            params (string key, object value)[] additionalParams)
        {
            var payload = new Dictionary<string, object>
            {
                ["operation"] = operation
            };

            foreach (var (key, value) in additionalParams)
            {
                payload[key] = value;
            }

            return payload;
        }

        /// <summary>
        /// テスト用のMcpIncomingCommandを作成します。
        /// </summary>
        /// <param name="toolName">ツール名</param>
        /// <param name="payload">ペイロード</param>
        /// <returns>McpIncomingCommand</returns>
        internal static McpIncomingCommand CreateCommand(
            string toolName, 
            Dictionary<string, object> payload)
        {
            var commandId = Guid.NewGuid().ToString();
            return new McpIncomingCommand(commandId, toolName, payload);
        }

        /// <summary>
        /// 結果が成功かどうかを確認します。
        /// </summary>
        /// <param name="result">結果辞書</param>
        /// <returns>成功の場合true</returns>
        public static bool IsSuccess(Dictionary<string, object> result)
        {
            if (result == null)
                return false;

            return result.TryGetValue("success", out var success) && 
                   success is bool boolValue && 
                   boolValue;
        }

        /// <summary>
        /// 結果からエラーメッセージを取得します。
        /// </summary>
        /// <param name="result">結果辞書</param>
        /// <returns>エラーメッセージ、または null</returns>
        public static string GetError(Dictionary<string, object> result)
        {
            if (result == null)
                return null;

            if (result.TryGetValue("error", out var error))
            {
                return error?.ToString();
            }

            return null;
        }

        /// <summary>
        /// テスト用のスクリプトファイルを作成します。
        /// </summary>
        /// <param name="fileName">ファイル名（.cs含む）</param>
        /// <param name="content">スクリプトの内容</param>
        /// <returns>作成されたファイルのパス</returns>
        public static string CreateScriptFile(string fileName, string content)
        {
            CreateTestDirectory();
            var path = Path.Combine(TestAssetsPath, fileName);
            File.WriteAllText(path, content);
            AssetDatabase.ImportAsset(path);
            return path;
        }

        /// <summary>
        /// テスト用のマテリアルを作成します。
        /// </summary>
        /// <param name="name">マテリアル名</param>
        /// <returns>作成されたマテリアルのパス</returns>
        public static string CreateMaterial(string name)
        {
            CreateTestDirectory();
            var material = new Material(Shader.Find("Standard"));
            var path = Path.Combine(TestAssetsPath, $"{name}.mat");
            AssetDatabase.CreateAsset(material, path);
            return path;
        }

        /// <summary>
        /// GameObjectリストをクリーンアップします。
        /// </summary>
        /// <param name="gameObjects">クリーンアップするGameObjectリスト</param>
        public static void CleanupGameObjects(List<GameObject> gameObjects)
        {
            if (gameObjects == null)
                return;

            foreach (var obj in gameObjects)
            {
                if (obj != null)
                {
                    Undo.ClearUndo(obj);
                    UnityEngine.Object.DestroyImmediate(obj);
                }
            }
            gameObjects.Clear();
        }
    }

    /// <summary>
    /// テスト用のGameObjectトラッカー。
    /// テスト中に作成されたGameObjectを追跡し、TearDownで自動的にクリーンアップします。
    /// </summary>
    public class GameObjectTracker : IDisposable
    {
        private readonly List<GameObject> _trackedObjects = new List<GameObject>();

        /// <summary>
        /// GameObjectを作成し、追跡リストに追加します。
        /// </summary>
        /// <param name="name">GameObjectの名前</param>
        /// <param name="parent">親Transform（オプション）</param>
        /// <returns>作成されたGameObject</returns>
        public GameObject Create(string name, Transform parent = null)
        {
            var go = TestUtilities.CreateGameObject(name, parent);
            _trackedObjects.Add(go);
            return go;
        }

        /// <summary>
        /// コンポーネント付きGameObjectを作成し、追跡リストに追加します。
        /// Transformの場合は既存のTransformを返します（Transformは全GameObjectに自動的に存在するため）。
        /// </summary>
        /// <typeparam name="T">追加するコンポーネントの型</typeparam>
        /// <param name="name">GameObjectの名前</param>
        /// <returns>作成されたGameObjectとコンポーネントのタプル</returns>
        public (GameObject gameObject, T component) CreateWithComponent<T>(string name)
            where T : Component
        {
            // Transformは全GameObjectに自動的に存在するため、AddComponentではなくGetComponentを使用
            if (typeof(T) == typeof(Transform))
            {
                var go = new GameObject(name);
                _trackedObjects.Add(go);
                return (go, go.GetComponent<T>());
            }

            var (gameObject, component) = TestUtilities.CreateGameObjectWithComponent<T>(name);
            _trackedObjects.Add(gameObject);
            return (gameObject, component);
        }

        /// <summary>
        /// 既存のGameObjectを追跡リストに追加します。
        /// </summary>
        /// <param name="gameObject">追跡するGameObject</param>
        public void Track(GameObject gameObject)
        {
            if (gameObject != null && !_trackedObjects.Contains(gameObject))
            {
                _trackedObjects.Add(gameObject);
            }
        }

        /// <summary>
        /// 追跡中のGameObjectから削除します（クリーンアップ対象外にします）。
        /// </summary>
        /// <param name="gameObject">追跡を解除するGameObject</param>
        public void Untrack(GameObject gameObject)
        {
            _trackedObjects.Remove(gameObject);
        }

        /// <summary>
        /// 追跡中の全てのGameObjectをクリーンアップします。
        /// </summary>
        public void Dispose()
        {
            TestUtilities.CleanupGameObjects(_trackedObjects);
        }
    }
}
