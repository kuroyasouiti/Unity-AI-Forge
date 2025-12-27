using UnityEngine;

namespace UnityAIForge.Tests
{
    /// <summary>
    /// Test script for compilation await functionality.
    /// Created to trigger Unity compilation.
    /// </summary>
    public class CompilationAwaitTestScript : MonoBehaviour
    {
        [SerializeField] private string testMessage = "Compilation await test successful!";
        [SerializeField] private int testValue = 100;
        [SerializeField] private float testFloat = 3.14159f;
        [SerializeField] private bool testBool = false;
        [SerializeField] private double testDouble = 1.41421356;
        [SerializeField] private Vector3 testVector = Vector3.one;

        private void Start()
        {
            Debug.Log($"{testMessage} Value: {testValue}");
        }

        public void TestMethod()
        {
            Debug.Log("Test method called");
        }
    }
}
