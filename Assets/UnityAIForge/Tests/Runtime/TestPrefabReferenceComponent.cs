using UnityEngine;

namespace MCP.Editor.Tests
{
    /// <summary>
    /// プレハブ参照テスト用のコンポーネント。
    /// UnityEngine.Object派生型の様々な参照フィールドを持ちます。
    /// </summary>
    public class TestPrefabReferenceComponent : MonoBehaviour
    {
        [Header("GameObject/Prefab References")]
        public GameObject prefabReference;
        public GameObject[] prefabArray;

        [Header("Component References")]
        public Transform transformReference;
        public Rigidbody2D rigidbodyReference;

        [Header("Asset References")]
        public Material materialReference;
        public Sprite spriteReference;
        public AudioClip audioClipReference;
        public ScriptableObject scriptableObjectReference;

        [Header("Mixed References")]
        [SerializeField]
        private GameObject _privatePrefabReference;

        public GameObject PrivatePrefabReference
        {
            get => _privatePrefabReference;
            set => _privatePrefabReference = value;
        }
    }
}
