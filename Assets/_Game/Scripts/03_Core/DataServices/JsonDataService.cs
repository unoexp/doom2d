// 📁 03_Core/DataServices/JsonDataService.cs
// JSON 数据服务泛型基类。提供 LoadAsync / GetById / GetAll 等通用方法。
// 子类只需指定 DataFileName 和 GetIdFromItem 即可。

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.IO;

/// <summary>
/// JSON 数据服务泛型基类。
/// 从 StreamingAssets/Data/ 加载 JSON 文件并反序列化为 List&lt;T&gt;，
/// 以指定 ID 字段建立字典索引供快速查找。
/// </summary>
public abstract class JsonDataService<T> : JsonDataServiceBase where T : class
{
    /// <summary>ID → 数据 的快速查找字典</summary>
    protected Dictionary<string, T> _dataMap = new Dictionary<string, T>();

    /// <summary>全部数据的列表（保持 JSON 原始顺序）</summary>
    protected List<T> _allData = new List<T>();

    /// <inheritdoc/>
    public override bool IsLoaded { get; protected set; }

    /// <summary>
    /// 子类实现：从数据条目提取 ID 作为字典键。
    /// 返回 null 或空字符串的条目不会被加入字典。
    /// </summary>
    protected abstract string GetIdFromItem(T item);

    /// <summary>
    /// 异步加载 JSON 数据。
    /// 路径：Application.streamingAssetsPath/Data/{DataFileName}
    /// </summary>
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
                Debug.LogError($"[{GetType().Name}] 加载失败: {path}\n{www.error}");
                yield break;
            }

            string json = www.downloadHandler.text;
            // 移除 UTF-8 BOM（﻿，部分编辑器自动添加）
            json = json.TrimStart('﻿');

            var settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new StringEnumConverter() },
                TypeNameHandling = TypeNameHandling.Auto,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };

            _allData = JsonConvert.DeserializeObject<List<T>>(json, settings) ?? new List<T>();

            _dataMap.Clear();
            foreach (var item in _allData)
            {
                string id = GetIdFromItem(item);
                if (!string.IsNullOrEmpty(id))
                    _dataMap[id] = item;
            }

            IsLoaded = true;
            Debug.Log($"[{GetType().Name}] 已加载 {_dataMap.Count} 条数据");
        }
    }

    /// <summary>通过 ID 获取数据条目</summary>
    public T GetById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        _dataMap.TryGetValue(id, out var data);
        return data;
    }

    /// <summary>安全获取数据条目（不抛异常）</summary>
    public bool TryGetById(string id, out T data)
    {
        data = null;
        if (string.IsNullOrEmpty(id)) return false;
        return _dataMap.TryGetValue(id, out data);
    }

    /// <summary>获取全部数据（只读列表）</summary>
    public IReadOnlyList<T> GetAll() => _allData;

    /// <summary>获取全部已索引的 ID 集合</summary>
    public IReadOnlyCollection<string> GetAllIds() => _dataMap.Keys;

    /// <inheritdoc/>
    public override void Shutdown()
    {
        _dataMap.Clear();
        _allData.Clear();
        IsLoaded = false;
    }
}
