// 📁 03_Core/DataServices/VFXCataLogDataService.cs
// 特效目录数据服务。从 vfx_catalog.json 加载特效条目定义。
// ⚠️ VFXCatalogData 为单例对象（包含 Entries 数组），需重写 LoadAsync。

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.IO;

/// <summary>
/// 特效目录数据服务。
/// vfx_catalog.json 是一个包含 Entries 数组的单例对象。
/// </summary>
public class VFXCataLogDataService : JsonDataService<VFXCatalogData>, IVFXCataLogDataService
{
    public override string DataFileName => "vfx_catalog.json";

    private VFXCatalogData _catalog;

    private void Awake()
    {
        ServiceLocator.Register<IVFXCataLogDataService>(this);
    }

    protected override string GetIdFromItem(VFXCatalogData item) => "_catalog";

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
                Debug.LogError($"[VFXCataLogDataService] 加载失败: {path}\n{www.error}");
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

            // 兼容三种格式：列式（条目数组） → 旧数组 [{...}] → 包装对象 {"entries": [...]}
            if (ColumnTableConverter.IsColumnTableFormat(text))
            {
                // 列式格式：columns 对应 VFXEntryData 字段
                var entries = ColumnTableConverter.DeserializeColumnTable<VFXEntryData>(text, settings);
                _catalog = new VFXCatalogData { Entries = entries?.ToArray() ?? System.Array.Empty<VFXEntryData>() };
            }
            else if (text.StartsWith("["))
            {
                // 旧数组格式：[{...}]
                var entries = JsonConvert.DeserializeObject<List<VFXEntryData>>(text, settings);
                _catalog = new VFXCatalogData { Entries = entries?.ToArray() ?? System.Array.Empty<VFXEntryData>() };
            }
            else
            {
                // 旧包装格式：{"entries": [...]}
                _catalog = JsonConvert.DeserializeObject<VFXCatalogData>(text, settings);
            }

            _allData = new List<VFXCatalogData> { _catalog };
            _dataMap.Clear();
            if (_catalog != null)
                _dataMap["_catalog"] = _catalog;

            IsLoaded = true;
            int entryCount = _catalog?.Entries?.Length ?? 0;
            Debug.Log($"[VFXCataLogDataService] 已加载特效目录，共 {entryCount} 条");
        }
    }

    public VFXCatalogData GetCatalog() => _catalog;

    public System.Collections.Generic.IReadOnlyList<VFXEntryData> GetAllEntries()
    {
        return _catalog?.Entries ?? System.Array.Empty<VFXEntryData>();
    }

    public override void Shutdown()
    {
        _catalog = null;
        ServiceLocator.Unregister<IVFXCataLogDataService>();
        base.Shutdown();
    }
}
