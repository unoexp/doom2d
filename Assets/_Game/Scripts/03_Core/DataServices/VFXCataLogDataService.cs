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

            // 兼容两种格式：Excel 转换器输出数组 [{...}]，手写 JSON 为包装对象 {"entries": [...]}
            if (text.StartsWith("["))
            {
                var entries = JsonConvert.DeserializeObject<List<VFXEntryData>>(text, settings);
                _catalog = new VFXCatalogData { Entries = entries?.ToArray() ?? System.Array.Empty<VFXEntryData>() };
            }
            else
            {
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
