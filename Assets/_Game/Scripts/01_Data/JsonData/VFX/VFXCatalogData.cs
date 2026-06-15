// 📁 01_Data/JsonData/VFX/VFXCatalogData.cs
// 特效目录数据 JSON 模型。从 vfx_catalog.json 加载特效条目定义。

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

/// <summary>
/// 特效目录条目（JSON 反序列化）。
/// prefabPath 相对于 Resources 目录，如 "VFX/HitSpark"。
/// </summary>
[System.Serializable]
public struct VFXEntryData
{
    [JsonProperty("vfxId")] public string VFXId;
    [JsonProperty("prefabPath")] public string PrefabPath;
}

/// <summary>
/// 特效目录根对象。包含所有特效条目数组。
/// </summary>
[JsonObject(MemberSerialization.OptIn)]
public class VFXCatalogData
{
    [JsonProperty("entries")] public VFXEntryData[] Entries;
}
