using UnityEditor;
using UnityEngine;

namespace UnityAIForge.Tests.Editor
{
    /// <summary>
    /// Test Result Viewer - テスト結果を見やすく表示するユーティリティ
    /// </summary>
    public class TestResultViewer : EditorWindow
    {
        [MenuItem("Tools/SkillForUnity/Open Test Result Viewer")]
        public static void ShowWindow()
        {
            var window = GetWindow<TestResultViewer>("Test Results");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        private Vector2 scrollPosition;
        
        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            
            // タイトル
            GUILayout.Label("Unity Test Runner Results", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.HelpBox(
                "テスト結果を確認するには以下の方法があります：",
                MessageType.Info
            );
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            // 方法1: Test Runner Window
            DrawSectionHeader("方法1: Test Runner Window を使用");
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("1. Test Runner Window を開く", EditorStyles.boldLabel);
                if (GUILayout.Button("Open Test Runner Window", GUILayout.Height(30)))
                {
                    EditorApplication.ExecuteMenuItem("Window/General/Test Runner");
                }
                
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("2. EditMode タブを選択");
                EditorGUILayout.LabelField("3. テストツリーを展開してテストを確認");
                EditorGUILayout.LabelField("4. テストを選択して 'Run Selected' をクリック");
                EditorGUILayout.LabelField("5. 結果が表示される：");
                EditorGUILayout.LabelField("   ✓ 緑のチェックマーク = 成功", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("   ✗ 赤いバツマーク = 失敗", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("   - グレーのダッシュ = 未実行", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
            
            // 方法2: Console Window
            DrawSectionHeader("方法2: Console Window でログを確認");
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("1. Console Window を開く", EditorStyles.boldLabel);
                if (GUILayout.Button("Open Console Window", GUILayout.Height(30)))
                {
                    EditorApplication.ExecuteMenuItem("Window/General/Console");
                }
                
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("2. テスト実行後、以下のログを確認：");
                EditorGUILayout.LabelField("   [TestRunner] Executing ... tests", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("   Test results will appear in Test Runner", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
            
            // クイックアクションボタン
            DrawSectionHeader("クイックアクション");
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("テストを実行して結果を確認", EditorStyles.boldLabel);
                EditorGUILayout.Space(5);
                
                if (GUILayout.Button("1. Run TextMeshPro Improved Tests", GUILayout.Height(35)))
                {
                    TestRunner.RunTextMeshProImprovedTests();
                    EditorApplication.delayCall += () =>
                    {
                        EditorApplication.ExecuteMenuItem("Window/General/Test Runner");
                    };
                }
                
                if (GUILayout.Button("2. Run All TextMeshPro Tests", GUILayout.Height(35)))
                {
                    TestRunner.RunAllTextMeshProTests();
                    EditorApplication.delayCall += () =>
                    {
                        EditorApplication.ExecuteMenuItem("Window/General/Test Runner");
                    };
                }
                
                if (GUILayout.Button("3. Run All Tests", GUILayout.Height(35)))
                {
                    TestRunner.RunAllTests();
                    EditorApplication.delayCall += () =>
                    {
                        EditorApplication.ExecuteMenuItem("Window/General/Test Runner");
                    };
                }
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
            
            // トラブルシューティング
            DrawSectionHeader("トラブルシューティング");
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.HelpBox(
                    "テスト結果が表示されない場合：",
                    MessageType.Warning
                );
                
                EditorGUILayout.LabelField("• Test Runner Window が開いていることを確認");
                EditorGUILayout.LabelField("• EditMode タブが選択されていることを確認");
                EditorGUILayout.LabelField("• テストツリーを展開してテストを表示");
                EditorGUILayout.LabelField("• Console Window でエラーがないか確認");
                
                EditorGUILayout.Space(5);
                
                if (GUILayout.Button("Clear Console"))
                {
                    var logEntries = System.Type.GetType("UnityEditor.LogEntries, UnityEditor.dll");
                    var clearMethod = logEntries.GetMethod("Clear", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                    clearMethod.Invoke(null, null);
                }
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
            
            // 参考情報
            DrawSectionHeader("参考情報");
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("テストの種類：");
                EditorGUILayout.LabelField("• TextMeshProComponentTests (基本CRUD)", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("  - 12個のテスト", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("• TextMeshProComponentImprovedTests (改善機能)", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("  - 10個のテスト", EditorStyles.miniLabel);
                
                EditorGUILayout.Space(5);
                
                if (GUILayout.Button("View Test Documentation"))
                {
                    var path = "Assets/UnityAIForge/Tests/Editor/README-TextMeshPro-Improved-Tests.md";
                    var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                    if (asset != null)
                    {
                        EditorGUIUtility.PingObject(asset);
                        Selection.activeObject = asset;
                    }
                    else
                    {
                        Debug.LogWarning($"Documentation not found at: {path}");
                    }
                }
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawSectionHeader(string title)
        {
            EditorGUILayout.Space(5);
            var style = new GUIStyle(EditorStyles.boldLabel);
            style.fontSize = 14;
            style.normal.textColor = new Color(0.2f, 0.6f, 1.0f);
            EditorGUILayout.LabelField(title, style);
            EditorGUILayout.Space(3);
        }
    }
}
