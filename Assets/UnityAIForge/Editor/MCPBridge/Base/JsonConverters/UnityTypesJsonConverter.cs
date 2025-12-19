using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MCP.Editor.Base.JsonConverters
{
    /// <summary>
    /// Unity構造体（Vector2, Vector3, Color, LayerMask等）のJSON変換を担当するコンバーター。
    /// 文字列定数（"red", "zero", "up", "Everything"等）もサポートします。
    /// </summary>
    public class UnityTypesJsonConverter : JsonConverter
    {
        #region Constants Mappings

        private static readonly Dictionary<string, Color> ColorConstants = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase)
        {
            { "red", Color.red },
            { "green", Color.green },
            { "blue", Color.blue },
            { "white", Color.white },
            { "black", Color.black },
            { "yellow", Color.yellow },
            { "cyan", Color.cyan },
            { "magenta", Color.magenta },
            { "gray", Color.gray },
            { "grey", Color.grey },
            { "clear", Color.clear }
        };

        private static readonly Dictionary<string, Vector3> Vector3Constants = new Dictionary<string, Vector3>(StringComparer.OrdinalIgnoreCase)
        {
            { "zero", Vector3.zero },
            { "one", Vector3.one },
            { "up", Vector3.up },
            { "down", Vector3.down },
            { "left", Vector3.left },
            { "right", Vector3.right },
            { "forward", Vector3.forward },
            { "back", Vector3.back }
        };

        private static readonly Dictionary<string, Vector2> Vector2Constants = new Dictionary<string, Vector2>(StringComparer.OrdinalIgnoreCase)
        {
            { "zero", Vector2.zero },
            { "one", Vector2.one },
            { "up", Vector2.up },
            { "down", Vector2.down },
            { "left", Vector2.left },
            { "right", Vector2.right }
        };

        private static readonly Dictionary<string, int> LayerMaskConstants = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "nothing", 0 },
            { "everything", ~0 },
            { "default", 1 << 0 },
            { "transparentfx", 1 << 1 },
            { "ignoreraycast", 1 << 2 },
            { "water", 1 << 4 },
            { "ui", 1 << 5 }
        };

        #endregion

        private static readonly HashSet<Type> SupportedTypes = new HashSet<Type>
        {
            typeof(Vector2),
            typeof(Vector3),
            typeof(Vector4),
            typeof(Vector2Int),
            typeof(Vector3Int),
            typeof(Color),
            typeof(Color32),
            typeof(Quaternion),
            typeof(Rect),
            typeof(RectInt),
            typeof(Bounds),
            typeof(BoundsInt),
            typeof(LayerMask)
        };

        public override bool CanConvert(Type objectType)
        {
            return SupportedTypes.Contains(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return Activator.CreateInstance(objectType);

            // 整数値の直接処理（LayerMask用）
            if (reader.TokenType == JsonToken.Integer)
            {
                if (objectType == typeof(LayerMask))
                    return (LayerMask)(int)(long)reader.Value;
                if (objectType == typeof(Vector2Int))
                    return new Vector2Int((int)(long)reader.Value, 0);
                if (objectType == typeof(Vector3Int))
                    return new Vector3Int((int)(long)reader.Value, 0, 0);
            }

            // 文字列定数の処理
            if (reader.TokenType == JsonToken.String)
            {
                var str = (string)reader.Value;
                return ConvertFromString(str, objectType);
            }

            // オブジェクト（辞書）からの変換
            var jObject = JObject.Load(reader);
            return ConvertFromJObject(jObject, objectType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            var type = value.GetType();

            if (type == typeof(Vector2))
            {
                var v = (Vector2)value;
                WriteObject(writer, new { x = v.x, y = v.y });
            }
            else if (type == typeof(Vector3))
            {
                var v = (Vector3)value;
                WriteObject(writer, new { x = v.x, y = v.y, z = v.z });
            }
            else if (type == typeof(Vector4))
            {
                var v = (Vector4)value;
                WriteObject(writer, new { x = v.x, y = v.y, z = v.z, w = v.w });
            }
            else if (type == typeof(Vector2Int))
            {
                var v = (Vector2Int)value;
                WriteObject(writer, new { x = v.x, y = v.y });
            }
            else if (type == typeof(Vector3Int))
            {
                var v = (Vector3Int)value;
                WriteObject(writer, new { x = v.x, y = v.y, z = v.z });
            }
            else if (type == typeof(Color))
            {
                var c = (Color)value;
                WriteObject(writer, new { r = c.r, g = c.g, b = c.b, a = c.a });
            }
            else if (type == typeof(Color32))
            {
                var c = (Color32)value;
                WriteObject(writer, new { r = (int)c.r, g = (int)c.g, b = (int)c.b, a = (int)c.a });
            }
            else if (type == typeof(Quaternion))
            {
                var q = (Quaternion)value;
                WriteObject(writer, new { x = q.x, y = q.y, z = q.z, w = q.w });
            }
            else if (type == typeof(Rect))
            {
                var r = (Rect)value;
                WriteObject(writer, new { x = r.x, y = r.y, width = r.width, height = r.height });
            }
            else if (type == typeof(RectInt))
            {
                var r = (RectInt)value;
                WriteObject(writer, new { x = r.x, y = r.y, width = r.width, height = r.height });
            }
            else if (type == typeof(Bounds))
            {
                var b = (Bounds)value;
                writer.WriteStartObject();
                writer.WritePropertyName("center");
                WriteObject(writer, new { x = b.center.x, y = b.center.y, z = b.center.z });
                writer.WritePropertyName("size");
                WriteObject(writer, new { x = b.size.x, y = b.size.y, z = b.size.z });
                writer.WriteEndObject();
            }
            else if (type == typeof(BoundsInt))
            {
                var b = (BoundsInt)value;
                writer.WriteStartObject();
                writer.WritePropertyName("position");
                WriteObject(writer, new { x = b.position.x, y = b.position.y, z = b.position.z });
                writer.WritePropertyName("size");
                WriteObject(writer, new { x = b.size.x, y = b.size.y, z = b.size.z });
                writer.WriteEndObject();
            }
            else if (type == typeof(LayerMask))
            {
                var mask = (LayerMask)value;
                var layers = GetLayerNames(mask.value);
                writer.WriteStartObject();
                writer.WritePropertyName("value");
                writer.WriteValue(mask.value);
                writer.WritePropertyName("layers");
                writer.WriteStartArray();
                foreach (var layer in layers)
                    writer.WriteValue(layer);
                writer.WriteEndArray();
                writer.WriteEndObject();
            }
        }

        #region Helper Methods

        private object ConvertFromString(string str, Type objectType)
        {
            if (objectType == typeof(Color) || objectType == typeof(Color32))
            {
                if (ColorConstants.TryGetValue(str, out var color))
                    return objectType == typeof(Color32) ? (object)(Color32)color : color;
            }
            else if (objectType == typeof(Vector3))
            {
                if (Vector3Constants.TryGetValue(str, out var v3))
                    return v3;
            }
            else if (objectType == typeof(Vector2))
            {
                if (Vector2Constants.TryGetValue(str, out var v2))
                    return v2;
            }
            else if (objectType == typeof(Quaternion))
            {
                if (str.Equals("identity", StringComparison.OrdinalIgnoreCase))
                    return Quaternion.identity;
            }
            else if (objectType == typeof(LayerMask))
            {
                return ConvertLayerMaskFromString(str);
            }

            throw new JsonSerializationException($"Unknown constant '{str}' for type {objectType.Name}");
        }

        private LayerMask ConvertLayerMaskFromString(string str)
        {
            var layerNames = str.Split(',');
            int mask = 0;
            foreach (var layerName in layerNames)
            {
                var trimmed = layerName.Trim();
                if (LayerMaskConstants.TryGetValue(trimmed, out var constantMask))
                {
                    mask |= constantMask;
                }
                else
                {
                    int layerIndex = LayerMask.NameToLayer(trimmed);
                    if (layerIndex >= 0)
                        mask |= 1 << layerIndex;
                    else if (int.TryParse(trimmed, out var numericValue))
                        mask |= numericValue;
                }
            }
            return mask;
        }

        private object ConvertFromJObject(JObject jObject, Type objectType)
        {
            if (objectType == typeof(Vector2))
            {
                return new Vector2(
                    jObject.Value<float>("x"),
                    jObject.Value<float>("y")
                );
            }
            if (objectType == typeof(Vector3))
            {
                return new Vector3(
                    jObject.Value<float>("x"),
                    jObject.Value<float>("y"),
                    jObject.Value<float>("z")
                );
            }
            if (objectType == typeof(Vector4))
            {
                return new Vector4(
                    jObject.Value<float>("x"),
                    jObject.Value<float>("y"),
                    jObject.Value<float>("z"),
                    jObject.Value<float>("w")
                );
            }
            if (objectType == typeof(Vector2Int))
            {
                return new Vector2Int(
                    jObject.Value<int>("x"),
                    jObject.Value<int>("y")
                );
            }
            if (objectType == typeof(Vector3Int))
            {
                return new Vector3Int(
                    jObject.Value<int>("x"),
                    jObject.Value<int>("y"),
                    jObject.Value<int>("z")
                );
            }
            if (objectType == typeof(Color))
            {
                // 部分的な値を持つ場合、未指定のコンポーネントはデフォルト値1.0を使用
                return new Color(
                    jObject.Value<float?>("r") ?? 1f,
                    jObject.Value<float?>("g") ?? 1f,
                    jObject.Value<float?>("b") ?? 1f,
                    jObject.Value<float?>("a") ?? 1f
                );
            }
            if (objectType == typeof(Color32))
            {
                // 部分的な値を持つ場合、未指定のコンポーネントはデフォルト値255を使用
                return new Color32(
                    jObject.Value<byte?>("r") ?? 255,
                    jObject.Value<byte?>("g") ?? 255,
                    jObject.Value<byte?>("b") ?? 255,
                    jObject.Value<byte?>("a") ?? 255
                );
            }
            if (objectType == typeof(Quaternion))
            {
                return new Quaternion(
                    jObject.Value<float>("x"),
                    jObject.Value<float>("y"),
                    jObject.Value<float>("z"),
                    jObject.Value<float?>("w") ?? 1f
                );
            }
            if (objectType == typeof(Rect))
            {
                return new Rect(
                    jObject.Value<float>("x"),
                    jObject.Value<float>("y"),
                    jObject.Value<float>("width"),
                    jObject.Value<float>("height")
                );
            }
            if (objectType == typeof(RectInt))
            {
                return new RectInt(
                    jObject.Value<int>("x"),
                    jObject.Value<int>("y"),
                    jObject.Value<int>("width"),
                    jObject.Value<int>("height")
                );
            }
            if (objectType == typeof(Bounds))
            {
                var center = Vector3.zero;
                var size = Vector3.zero;
                if (jObject["center"] is JObject centerObj)
                {
                    center = new Vector3(
                        centerObj.Value<float>("x"),
                        centerObj.Value<float>("y"),
                        centerObj.Value<float>("z")
                    );
                }
                if (jObject["size"] is JObject sizeObj)
                {
                    size = new Vector3(
                        sizeObj.Value<float>("x"),
                        sizeObj.Value<float>("y"),
                        sizeObj.Value<float>("z")
                    );
                }
                return new Bounds(center, size);
            }
            if (objectType == typeof(BoundsInt))
            {
                var position = Vector3Int.zero;
                var size = Vector3Int.zero;
                if (jObject["position"] is JObject posObj)
                {
                    position = new Vector3Int(
                        posObj.Value<int>("x"),
                        posObj.Value<int>("y"),
                        posObj.Value<int>("z")
                    );
                }
                if (jObject["size"] is JObject sizeObj)
                {
                    size = new Vector3Int(
                        sizeObj.Value<int>("x"),
                        sizeObj.Value<int>("y"),
                        sizeObj.Value<int>("z")
                    );
                }
                return new BoundsInt(position, size);
            }
            if (objectType == typeof(LayerMask))
            {
                if (jObject.TryGetValue("value", out var valueToken))
                {
                    return (LayerMask)valueToken.Value<int>();
                }
                if (jObject.TryGetValue("layers", out var layersToken) && layersToken is JArray layersArray)
                {
                    int mask = 0;
                    foreach (var layer in layersArray)
                    {
                        mask |= ConvertLayerNameToMask(layer.Value<string>());
                    }
                    return (LayerMask)mask;
                }
                throw new JsonSerializationException("LayerMask must have 'value' or 'layers' property");
            }

            throw new JsonSerializationException($"Cannot convert to {objectType.Name}");
        }

        private int ConvertLayerNameToMask(string layerName)
        {
            if (string.IsNullOrEmpty(layerName))
                return 0;

            var trimmed = layerName.Trim();
            if (LayerMaskConstants.TryGetValue(trimmed, out var constantMask))
                return constantMask;

            int layerIndex = LayerMask.NameToLayer(trimmed);
            if (layerIndex >= 0)
                return 1 << layerIndex;

            if (int.TryParse(trimmed, out var numericValue))
                return numericValue;

            return 0;
        }

        private List<string> GetLayerNames(int mask)
        {
            var layers = new List<string>();
            for (int i = 0; i < 32; i++)
            {
                if ((mask & (1 << i)) != 0)
                {
                    string layerName = LayerMask.LayerToName(i);
                    layers.Add(!string.IsNullOrEmpty(layerName) ? layerName : i.ToString());
                }
            }
            return layers;
        }

        private void WriteObject(JsonWriter writer, object anonymousObject)
        {
            var type = anonymousObject.GetType();
            writer.WriteStartObject();
            foreach (var prop in type.GetProperties())
            {
                writer.WritePropertyName(prop.Name);
                var value = prop.GetValue(anonymousObject);
                if (value is float f)
                    writer.WriteValue(f);
                else if (value is int i)
                    writer.WriteValue(i);
                else
                    writer.WriteValue(value);
            }
            writer.WriteEndObject();
        }

        #endregion
    }
}
