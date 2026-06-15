// 📁 01_Data/JsonData/Audio/AudioCatalogData.cs
// 音频目录数据 JSON 模型

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

/// <summary>
/// 音频条目
/// </summary>
[System.Serializable]
public struct AudioEntryData
{
    [JsonProperty("audioId")] public string AudioId;
    [JsonProperty("group")][JsonConverter(typeof(StringEnumConverter))] public AudioGroup Group;
    [JsonProperty("clipPaths")]
    [JsonConverter(typeof(SingleOrArrayConverter<string>))]
    public string[] ClipPaths;
    [JsonProperty("volumeScale")] public float VolumeScale;
    [JsonProperty("pitchMin")] public float PitchMin;
    [JsonProperty("pitchMax")] public float PitchMax;

    /// <summary>运行时加载的 AudioClip 缓存（不由 JSON 序列化）</summary>
    [JsonIgnore] public UnityEngine.AudioClip[] Clips;

    /// <summary>获取有效音量缩放（默认1）</summary>
    public float EffectiveVolumeScale => VolumeScale > 0f ? VolumeScale : 1f;

    /// <summary>获取有效最小音高（默认1）</summary>
    public float EffectivePitchMin => PitchMin > 0f ? PitchMin : 1f;

    /// <summary>获取有效最大音高（默认1）</summary>
    public float EffectivePitchMax => PitchMax > 0f ? PitchMax : 1f;

    /// <summary>是否有已加载的音频剪辑</summary>
    public bool HasClips => Clips != null && Clips.Length > 0;
}

[JsonObject(MemberSerialization.OptIn)]
public class AudioCatalogData
{
    [JsonProperty("entries")] public AudioEntryData[] Entries;
}
