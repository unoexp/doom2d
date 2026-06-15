// 📁 07_Shared/Utils/SingleOrArrayConverter.cs
// JSON 转换器：处理 JSON 中单值/数组混用的情况。
// 例如 "path": "Audio/Music" 或 "path": ["Audio/Music", "Audio/SFX"]
// 均反序列化为 T[]，序列化时统一输出数组。

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
/// 将 JSON 单个值或数组统一反序列化为 T[]。
/// 用法：[JsonConverter(typeof(SingleOrArrayConverter&lt;string&gt;))]
/// </summary>
public class SingleOrArrayConverter<T> : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(T[]);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.StartArray)
        {
            // 标准数组格式
            var list = serializer.Deserialize<List<T>>(reader);
            return list?.ToArray() ?? Array.Empty<T>();
        }

        // 单个值 → 包裹为单元素数组
        var single = serializer.Deserialize<T>(reader);
        return new T[] { single };
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var array = (T[])value;
        writer.WriteStartArray();
        foreach (var item in array)
            serializer.Serialize(writer, item);
        writer.WriteEndArray();
    }
}
