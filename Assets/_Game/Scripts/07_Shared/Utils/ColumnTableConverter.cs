// 📁 07_Shared/Utils/ColumnTableConverter.cs
// 列式 JSON 格式转换工具。
// 将 {"columns": [...], "rows": [[...], ...]} 列式 JSON 反序列化为 List<T>，
// 消除行式 JSON 中每行重复键名的冗余。
//
// 使用 Newtonsoft.Json 的 Contract 系统解析 T 的属性映射，
// 自动尊重 [JsonProperty]、[JsonConverter]、[JsonObject] 等特性。

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

/// <summary>
/// 列式 JSON 反序列化工具。
/// 支持将列式格式反序列化为任意 C# 类型（class 或 struct），
/// 自动处理属性级别的 JsonConverter（如 StringEnumConverter、SingleOrArrayConverter）。
/// </summary>
public static class ColumnTableConverter
{
    /// <summary>
    /// 快速判断 JSON 字符串是否为列式格式。
    /// 列式格式定义为包含 "columns" 和 "rows" 两个顶层键的对象。
    /// </summary>
    /// <param name="json">原始 JSON 字符串</param>
    /// <returns>true 表示可安全传入 DeserializeColumnTable</returns>
    public static bool IsColumnTableFormat(string json)
    {
        if (string.IsNullOrEmpty(json))
            return false;

        // 去除空白，检测首字符
        string trimmed = json.TrimStart();
        if (trimmed.Length == 0)
            return false;

        // 若以 '[' 开头，直接判定为旧行式数组格式
        if (trimmed[0] == '[')
            return false;

        // 若以 '{' 开头，检查是否同时包含 columns 和 rows 键
        if (trimmed[0] != '{')
            return false;

        try
        {
            JObject root = JObject.Parse(json);
            return root.TryGetValue("columns", out _) && root.TryGetValue("rows", out _);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 将列式 JSON 字符串反序列化为指定类型的列表。
    /// </summary>
    /// <typeparam name="T">目标数据类型（class 或 struct）</typeparam>
    /// <param name="json">列式 JSON 字符串，格式 {"columns": [...], "rows": [[...], ...]}</param>
    /// <param name="settings">JsonSerializerSettings（含 Converters、ContractResolver 等）</param>
    /// <returns>成功解析的数据列表，列式格式不匹配或解析失败时返回空列表</returns>
    public static List<T> DeserializeColumnTable<T>(string json, JsonSerializerSettings settings = null)
    {
        if (string.IsNullOrEmpty(json))
            return new List<T>();

        JObject root;
        try
        {
            root = JObject.Parse(json);
        }
        catch
        {
            return new List<T>();
        }

        // ── 提取 columns 和 rows ──
        JToken columnsToken = root["columns"];
        JToken rowsToken = root["rows"];

        if (columnsToken == null || rowsToken == null || !(columnsToken is JArray columnsArr))
            return new List<T>();

        string[] columns = columnsArr.ToObject<string[]>();
        if (columns == null || columns.Length == 0)
            return new List<T>();

        if (!(rowsToken is JArray rowsArr) || rowsArr.Count == 0)
            return new List<T>();

        // ── 构建序列化器 ──
        var resolver = settings?.ContractResolver ?? new DefaultContractResolver();
        JsonSerializer serializer = JsonSerializer.Create(settings ?? new JsonSerializerSettings());
        serializer.MissingMemberHandling = settings?.MissingMemberHandling ?? MissingMemberHandling.Ignore;

        // ── 解析 T 的属性契约，构建 JSON属性名 → JsonProperty 映射 ──
        var contract = resolver.ResolveContract(typeof(T)) as JsonObjectContract;
        if (contract == null)
            return new List<T>();

        var propertyMap = new Dictionary<string, JsonProperty>(StringComparer.OrdinalIgnoreCase);
        foreach (JsonProperty prop in contract.Properties)
        {
            if (prop.Writable && !string.IsNullOrEmpty(prop.PropertyName))
            {
                propertyMap[prop.PropertyName] = prop;
            }
        }

        // ── 按 columns 顺序建立 (列索引 → JsonProperty) 映射 ──
        // [PERF] 用数组实现 O(1) 列查找，避免每次查字典
        var colMappings = new (int colIndex, JsonProperty property)[columns.Length];
        int mappingCount = 0;

        for (int i = 0; i < columns.Length; i++)
        {
            if (propertyMap.TryGetValue(columns[i], out JsonProperty prop))
            {
                colMappings[mappingCount++] = (i, prop);
            }
        }

        // ── 遍历行，构建 T 实例 ──
        var result = new List<T>(rowsArr.Count);

        foreach (JToken rowToken in rowsArr)
        {
            if (!(rowToken is JArray rowArr))
                continue;

            T obj = Activator.CreateInstance<T>();

            for (int m = 0; m < mappingCount; m++)
            {
                (int colIndex, JsonProperty jsonProp) = colMappings[m];

                if (colIndex >= rowArr.Count)
                    continue;

                JToken value = rowArr[colIndex];
                if (value == null || value.Type == JTokenType.Null)
                    continue;

                try
                {
                    object convertedValue = ConvertToken(value, jsonProp, serializer);
                    SetMemberValue(jsonProp, ref obj, convertedValue);
                }
                catch (Exception ex)
                {
                    // 单个字段解析失败不中断整行，保留默认值并记录警告
                    UnityEngine.Debug.LogWarning(
                        $"[ColumnTableConverter] 字段 '{jsonProp.PropertyName}' (列 {colIndex}) 解析失败: {ex.Message}");
                }
            }

            result.Add(obj);
        }

        return result;
    }

    // ══════════════════════════════════════════════════════
    // 私有辅助方法
    // ══════════════════════════════════════════════════════

    /// <summary>将 JToken 转换为目标属性类型，优先使用属性级 JsonConverter</summary>
    private static object ConvertToken(JToken value, JsonProperty jsonProp, JsonSerializer serializer)
    {
        // 属性上有自定义 JsonConverter 时优先使用（处理 SingleOrArrayConverter 等场景）
        if (jsonProp.Converter != null && jsonProp.Converter.CanRead)
        {
            using (var reader = value.CreateReader())
            {
                return jsonProp.Converter.ReadJson(reader, jsonProp.PropertyType, null, serializer);
            }
        }

        // 默认：通过序列化器反序列化
        using (var reader = value.CreateReader())
        {
            return serializer.Deserialize(reader, jsonProp.PropertyType);
        }
    }

    /// <summary>通过反射设置对象的成员值（兼容 class 和 struct，兼容 PropertyInfo 和 FieldInfo）</summary>
    /// <remarks>
    /// 对于值类型（struct），需要先装箱→SetValue修改堆副本→拆箱恢复。
    /// 对于引用类型（class），装箱退化为引用拷贝，无额外分配。
    /// </remarks>
    private static void SetMemberValue<T>(JsonProperty jsonProp, ref T obj, object value)
    {
        // [PERF] 值类型需装箱才能通过 ValueProvider 修改字段（struct 是值拷贝）
        // 引用类型装箱退化为引用拷贝，零额外 GC
        object boxed = obj;

        // 优先通过 ValueProvider（处理 property 和 field）
        if (jsonProp.ValueProvider != null)
        {
            jsonProp.ValueProvider.SetValue(boxed, value);
            obj = (T)boxed;
            return;
        }

        // 回退：直接反射
        var member = jsonProp.UnderlyingName;
        if (string.IsNullOrEmpty(member))
            return;

        var type = typeof(T);
        var propInfo = type.GetProperty(member);
        if (propInfo != null && propInfo.CanWrite)
        {
            propInfo.SetValue(boxed, value);
            obj = (T)boxed;
            return;
        }

        var fieldInfo = type.GetField(member);
        if (fieldInfo != null)
        {
            fieldInfo.SetValue(boxed, value);
            obj = (T)boxed;
        }
    }
}
