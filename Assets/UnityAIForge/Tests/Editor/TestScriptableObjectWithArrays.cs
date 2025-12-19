using System;
using System.Collections.Generic;
using UnityEngine;

namespace MCP.Editor.Tests
{
    /// <summary>
    /// テスト用のScriptableObject（配列とList型のプロパティを含む）
    /// </summary>
    public class TestScriptableObjectWithArrays : ScriptableObject
    {
        // 単一値フィールド
        public int singleInt;
        public string singleString;

        // 配列フィールド
        public int[] intArray;
        public float[] floatArray;
        public string[] stringArray;
        public Vector3[] vector3Array;

        // Listフィールド
        public List<int> intList;
        public List<string> stringList;
        public List<float> floatList;
        public List<Color> colorList;
        public List<Vector2> vector2List;

        // ユーザー定義構造体の配列とList
        public TestCustomStruct[] customStructArray;
        public List<TestCustomStruct> customStructList;

        // ネストした構造体の配列
        public TestNestedStruct[] nestedStructArray;

        // Enum配列とList
        public TestActionType[] enumArray;
        public List<TestCharacterState> enumList;
    }

    /// <summary>
    /// テスト用のカスタム構造体
    /// </summary>
    [Serializable]
    public struct TestCustomStruct
    {
        public int id;
        public string name;
        public float value;
    }

    /// <summary>
    /// テスト用のネストした構造体（Unity型を含む）
    /// </summary>
    [Serializable]
    public struct TestNestedStruct
    {
        public string label;
        public Vector3 position;
        public Color color;
    }

    /// <summary>
    /// テスト用のアクションタイプenum
    /// </summary>
    public enum TestActionType
    {
        Attack,
        Defend,
        Heal,
        Special
    }

    /// <summary>
    /// テスト用のキャラクター状態enum
    /// </summary>
    public enum TestCharacterState
    {
        Idle,
        Running,
        Jumping,
        Falling,
        Dead
    }
}
