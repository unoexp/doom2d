// 📁 03_Core/DataServices/ResourceCacheConfigDataService.cs
// 资源缓存配置数据服务。从 resource_cache_config.json 加载单例配置。
// ⚠️ 此 JSON 文件为单个对象（非数组），需重写 LoadAsync。

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.IO;

/// <summary>
/// 资源缓存配置数据服务。
/// resource_cache_config.json 是一个单例配置对象，非数组。
/// </summary>
public class ResourceCacheConfigDataService : JsonDataService<ResourceCacheConfigData>, IResourceCacheConfigDataService
{
    public override string DataFileName => "resource_cache_config.json";

    private ResourceCacheConfigData _config;

    private void Awake()
    {
        ServiceLocator.Register<IResourceCacheConfigDataService>(this);
    }

    protected override string GetIdFromItem(ResourceCacheConfigData item) => "_config";

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
                Debug.LogError($"[ResourceCacheConfigDataService] 加载失败: {path}\n{www.error}");
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

            // 兼容三种格式：列式 → 单元素数组 [{...}] → 单例对象 {...}
            if (ColumnTableConverter.IsColumnTableFormat(text))
            {
                // 列式格式 {"columns": [...], "rows": [[...]]} → 取首行
                var list = ColumnTableConverter.DeserializeColumnTable<ResourceCacheConfigData>(text, settings);
                _config = (list != null && list.Count > 0) ? list[0] : new ResourceCacheConfigData();
            }
            else if (text.StartsWith("["))
            {
                // 旧数组格式：[{...}]
                var list = JsonConvert.DeserializeObject<List<ResourceCacheConfigData>>(text, settings);
                _config = (list != null && list.Count > 0) ? list[0] : new ResourceCacheConfigData();
            }
            else
            {
                // 旧单例格式：{...}
                _config = JsonConvert.DeserializeObject<ResourceCacheConfigData>(text, settings);
            }
            _allData = new List<ResourceCacheConfigData> { _config };
            _dataMap.Clear();
            if (_config != null)
                _dataMap["_config"] = _config;

            IsLoaded = true;
            Debug.Log($"[ResourceCacheConfigDataService] 已加载资源缓存配置");
        }
    }

    public ResourceCacheConfigData GetConfig() => _config;

    public override void Shutdown()
    {
        _config = null;
        ServiceLocator.Unregister<IResourceCacheConfigDataService>();
        base.Shutdown();
    }
}
