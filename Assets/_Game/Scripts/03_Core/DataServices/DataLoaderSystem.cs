// 📁 03_Core/DataServices/DataLoaderSystem.cs
// 数据加载协调器。按顺序创建所有 JSON 数据服务并并行加载。
// 在 AppMain 中首先创建，等待加载完成后才创建业务系统。

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 数据加载协调器。
/// 负责创建所有 JsonDataService 实例并逐个加载其 JSON 数据文件。
/// 加载完成后，各业务系统可通过 ServiceLocator 获取对应接口查询数据。
///
/// 使用方式（AppMain 中）：
///   var loader = CreateSystem&lt;DataLoaderSystem&gt;("DataLoaderSystem");
///   yield return loader.LoadAllDataAsync();
/// </summary>
public class DataLoaderSystem : MonoBehaviour, ISystem
{
    /// <summary>全部数据服务列表（按创建顺序）</summary>
    private readonly List<JsonDataServiceBase> _dataServices = new List<JsonDataServiceBase>();

    public void Initialize() { }

    /// <summary>
    /// 创建所有数据服务实例并按顺序加载。
    /// 调用方应使用 StartCoroutine 或 yield return 等待完成。
    /// </summary>
    public IEnumerator LoadAllDataAsync()
    {
        Debug.Log("[DataLoader] ========== 开始加载所有 JSON 数据 ==========");

        // ── Phase 1: 创建所有数据服务（无依赖，可安全创建） ──
        CreateAllServices();

        // ── Phase 2: 逐个加载 JSON 文件 ──
        foreach (var svc in _dataServices)
        {
            yield return svc.LoadAsync();
        }

        Debug.Log($"[DataLoader] ========== 数据加载完成，共 {_dataServices.Count} 个数据服务 ==========");
    }

    /// <summary>
    /// 创建所有数据服务（骨架版本：仅基础设施数据服务）。
    /// </summary>
    private void CreateAllServices()
    {
        // ── 基础设施配置 ──
        CreateAndAdd<ResourceCacheConfigDataService>("ResourceCacheConfigDataService");

        // ── 音频 ──
        CreateAndAdd<AudioCatalogDataService>("AudioCatalogDataService");
    }

    /// <summary>创建单个数据服务 GameObject 并加入列表</summary>
    private void CreateAndAdd<T>(string name) where T : JsonDataServiceBase
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform);
        var svc = go.AddComponent<T>();
        _dataServices.Add(svc);
    }

    /// <summary>逆序关闭所有数据服务</summary>
    public void Shutdown()
    {
        for (int i = _dataServices.Count - 1; i >= 0; i--)
        {
            _dataServices[i]?.Shutdown();
        }
        _dataServices.Clear();
    }
}
