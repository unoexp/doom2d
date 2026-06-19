// 📁 03_Core/DataServices/DataLoaderSystem.cs
// 数据加载协调器。按顺序创建所有 JSON 数据服务并分批加载。
// 在 AppMain 中首先创建，支持优先加载窗口配置以尽早显示 LoadingWindow。

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 数据加载协调器。
/// 负责创建所有 JsonDataService 实例并逐个加载其 JSON 数据文件。
/// 加载完成后，各业务系统可通过 ServiceLocator 获取对应接口查询数据。
///
/// 支持两阶段加载：
///   1. LoadWindowConfigFirstAsync() — 优先加载窗口配置，以便尽早显示 LoadingWindow
///   2. LoadRemainingWithProgressAsync() — 加载其余数据，沿途发布 LoadingProgressEvent
///
/// 也支持单次加载（向后兼容）：
///   var loader = CreateSystem&lt;DataLoaderSystem&gt;("DataLoaderSystem");
///   yield return loader.LoadAllDataAsync();
/// </summary>
public class DataLoaderSystem : MonoBehaviour, ISystem
{
    /// <summary>全部数据服务列表（按创建顺序）</summary>
    private readonly List<JsonDataServiceBase> _dataServices = new List<JsonDataServiceBase>();

    /// <summary>是否已调用 CreateAllServices</summary>
    private bool _servicesCreated;

    public void Initialize() { }

    // ══════════════════════════════════════════════════════
    // Phase 1: 窗口配置优先加载（LoadingWindow 先行显示）
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 创建所有数据服务并仅加载窗口配置（WindowConfigDataService）。
    /// 调用此方法后 WindowManager 可立即初始化并打开 LoadingWindow。
    /// 之后调用 LoadRemainingWithProgressAsync() 加载其余数据。
    /// </summary>
    public IEnumerator LoadWindowConfigFirstAsync()
    {
        // 确保服务已创建
        if (!_servicesCreated)
        {
            CreateAllServices();
            _servicesCreated = true;
        }

        // 找到 WindowConfigDataService 并优先加载
        var windowCfgSvc = _dataServices.Find(s => s is WindowConfigDataService);
        if (windowCfgSvc != null && !windowCfgSvc.IsLoaded)
        {
            Debug.Log("[DataLoader] ── 优先加载窗口配置 ──");
            yield return windowCfgSvc.LoadAsync();
        }
        else
        {
            Debug.LogWarning("[DataLoader] WindowConfigDataService 未找到或已加载");
        }
    }

    // ══════════════════════════════════════════════════════
    // Phase 2: 其余数据批量加载（带进度事件）
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 加载所有尚未加载的数据服务，沿途发布 LoadingProgressEvent。
    /// 通常在 LoadWindowConfigFirstAsync() 之后调用。
    /// </summary>
    public IEnumerator LoadRemainingWithProgressAsync()
    {
        // 确保服务已创建
        if (!_servicesCreated)
        {
            CreateAllServices();
            _servicesCreated = true;
        }

        var unloaded = _dataServices.FindAll(s => !s.IsLoaded);
        int total = unloaded.Count;
        int index = 0;

        foreach (var svc in unloaded)
        {
            float progress = total > 0 ? (float)index / total : 0f;
            EventBus.Publish(new LoadingProgressEvent
            {
                Progress = progress,
                StepDescription = svc.GetType().Name
            });

            Debug.Log($"[DataLoader] 加载: {svc.GetType().Name} ({index + 1}/{total})");
            yield return svc.LoadAsync();
            index++;
        }

        // 全部加载完成，进度条填满
        EventBus.Publish(new LoadingProgressEvent
        {
            Progress = 1f,
            StepDescription = "数据加载完成"
        });

        Debug.Log($"[DataLoader] ========== 数据加载完成，共 {_dataServices.Count} 个数据服务 ==========");
    }

    // ══════════════════════════════════════════════════════
    // 传统单次加载（向后兼容）
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 创建所有数据服务实例并按顺序加载（传统单次流程）。
    /// 调用方应使用 StartCoroutine 或 yield return 等待完成。
    /// </summary>
    public IEnumerator LoadAllDataAsync()
    {
        Debug.Log("[DataLoader] ========== 开始加载所有 JSON 数据 ==========");

        if (!_servicesCreated)
        {
            CreateAllServices();
            _servicesCreated = true;
        }

        foreach (var svc in _dataServices)
        {
            yield return svc.LoadAsync();
        }

        Debug.Log($"[DataLoader] ========== 数据加载完成，共 {_dataServices.Count} 个数据服务 ==========");
    }

    // ══════════════════════════════════════════════════════
    // 服务创建
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 创建所有数据服务（骨架版本：仅基础设施数据服务）。
    /// </summary>
    private void CreateAllServices()
    {
        // ── 基础设施配置 ──
        CreateAndAdd<ResourceCacheConfigDataService>("ResourceCacheConfigDataService");

        // ── 音频 ──
        CreateAndAdd<AudioCatalogDataService>("AudioCatalogDataService");

        // ── 特效 ──
        CreateAndAdd<VFXCataLogDataService>("VFXCataLogDataService");

        // ── 窗口配置 ──
        CreateAndAdd<WindowConfigDataService>("WindowConfigDataService");
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
