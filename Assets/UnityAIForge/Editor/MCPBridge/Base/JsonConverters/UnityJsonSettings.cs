using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace MCP.Editor.Base.JsonConverters
{
    /// <summary>
    /// Unity型を含むJSONシリアライズ/デシリアライズ用の設定を提供します。
    /// </summary>
    public static class UnityJsonSettings
    {
        private static JsonSerializerSettings _settings;
        private static JsonSerializer _serializer;

        /// <summary>
        /// Unity型対応のJsonSerializerSettingsを取得します。
        /// </summary>
        public static JsonSerializerSettings Settings
        {
            get
            {
                if (_settings == null)
                {
                    _settings = CreateSettings();
                }
                return _settings;
            }
        }

        /// <summary>
        /// Unity型対応のJsonSerializerを取得します。
        /// </summary>
        public static JsonSerializer Serializer
        {
            get
            {
                if (_serializer == null)
                {
                    _serializer = JsonSerializer.Create(Settings);
                }
                return _serializer;
            }
        }

        /// <summary>
        /// 設定をリセットします（テスト用）。
        /// </summary>
        internal static void Reset()
        {
            _settings = null;
            _serializer = null;
        }

        private static JsonSerializerSettings CreateSettings()
        {
            var settings = new JsonSerializerSettings
            {
                // Enum を文字列として扱う
                Converters =
                {
                    new UnityTypesJsonConverter(),
                    new StringEnumConverter()
                },

                // nullプロパティを出力しない
                NullValueHandling = NullValueHandling.Ignore,

                // デフォルト値も出力
                DefaultValueHandling = DefaultValueHandling.Include,

                // 循環参照を無視
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,

                // camelCase ではなく元の名前を使用
                ContractResolver = new DefaultContractResolver(),

                // 日付のフォーマット
                DateFormatHandling = DateFormatHandling.IsoDateFormat,

                // フォーマットなし（コンパクト）
                Formatting = Formatting.None
            };

            return settings;
        }

        /// <summary>
        /// オブジェクトをJSON文字列にシリアライズします。
        /// </summary>
        public static string Serialize(object obj)
        {
            return JsonConvert.SerializeObject(obj, Settings);
        }

        /// <summary>
        /// JSON文字列をオブジェクトにデシリアライズします。
        /// </summary>
        public static T Deserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, Settings);
        }

        /// <summary>
        /// JSON文字列を指定された型にデシリアライズします。
        /// </summary>
        public static object Deserialize(string json, System.Type type)
        {
            return JsonConvert.DeserializeObject(json, type, Settings);
        }
    }
}
