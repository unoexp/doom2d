// 📁 03_Core/DataServices/AudioCatalogDataService.cs
// 音频目录数据服务。从 audio_catalog.json 加载音频条目定义。
// ⚠️ AudioCatalogData 为单例对象（包含 Entries 数组），需重写 LoadAsync。

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.IO;

/// <summary>
/// 音频目录数据服务。
/// audio_catalog.json 是一个包含 Entries 数组的单例对象。
/// </summary>
public class AudioCatalogDataService : JsonDataService<AudioCatalogData>, IAudioCatalogDataService
{
    public override string DataFileName => "audio_catalog.json";

    private AudioCatalogData _catalog;

    private void Awake()
    {
        ServiceLocator.Register<IAudioCatalogDataService>(this);
    }

    protected override string GetIdFromItem(AudioCatalogData item) => "_catalog";

    /// <summary>重写加载逻辑：单个对象而非数组</summary>
    public override IEnumerator LoadAsync()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "Data", DataFileName);
        // UnityWebRequest 加载本地文件需要 file:// 前缀（Editor/Standalone/iOS）
        if (!path.StartsWith("file://") && !path.StartsWith("jar:"))
            path = "file://" + path;

        using (var www = UnityWebRequest.Get(path))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[AudioCatalogDataService] 加载失败: {path}\n{www.error}");
                yield break;
            }

            var settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new StringEnumConverter() },
                MissingMemberHandling = MissingMemberHandling.Ignore
            };

            string text = www.downloadHandler.text.TrimStart();
            // 移除 UTF-8 BOM（﻿，部分编辑器自动添加）
            text = text.TrimStart('﻿');

            // 兼容两种格式：Excel 转换器输出数组 [{...}]，手写 JSON 为包装对象 {"entries": [...]}
            if (text.StartsWith("["))
            {
                var entries = JsonConvert.DeserializeObject<List<AudioEntryData>>(text, settings);
                _catalog = new AudioCatalogData { Entries = entries?.ToArray() ?? System.Array.Empty<AudioEntryData>() };
            }
            else
            {
                _catalog = JsonConvert.DeserializeObject<AudioCatalogData>(text, settings);
            }

            _allData = new List<AudioCatalogData> { _catalog };
            _dataMap.Clear();
            if (_catalog != null)
                _dataMap["_catalog"] = _catalog;

            IsLoaded = true;
            int entryCount = _catalog?.Entries?.Length ?? 0;
            Debug.Log($"[AudioCatalogDataService] 已加载音频目录，共 {entryCount} 条");
        }
    }

    public AudioCatalogData GetCatalog() => _catalog;

    public System.Collections.Generic.IReadOnlyList<AudioEntryData> GetAllEntries()
    {
        return _catalog?.Entries ?? System.Array.Empty<AudioEntryData>();
    }

    public override void Shutdown()
    {
        _catalog = null;
        ServiceLocator.Unregister<IAudioCatalogDataService>();
        base.Shutdown();
    }
}
