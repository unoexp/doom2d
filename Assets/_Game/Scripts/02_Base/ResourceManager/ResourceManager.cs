// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/02_Base/ResourceManager/ResourceManager.cs
// ResourceManager 核心 — 生命周期、IResourceLoader 实现。
// IAssetBundleLoader → ResourceManager.Bundle.cs | 事件/工具/批量 → ResourceManager.Utilities.cs
// ══════════════════════════════════════════════════════════════════════
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 资源管理器核心类
/// 职责：
/// 1. 统一管理资源加载（同步/异步）
/// 2. 管理AssetBundle（本地/远程）
/// 3. 集成LRU缓存系统
/// 4. 发布加载事件到EventBus
/// 5. 注册到ServiceLocator供其他系统使用
/// </summary>
public partial class ResourceManager : MonoSingleton<ResourceManager>,
    IResourceLoader, IAssetBundleLoader
{
    // ══════════════════════════════════════════════════════
    // 配置字段
    // ══════════════════════════════════════════════════════

    // [MIGRATED] 从 ResourceCacheConfigData (JSON) 加载配置
    public ResourceCacheConfigData CacheConfig { get; set; }

    public int DefaultNetworkTimeout { get; set; } = 30;
    public int MaxRetryCount { get; set; } = 3;

    public bool EnableLogging { get; set; } = true;
    public bool ShowCacheStatusInInspector { get; set; } = true;

    // ══════════════════════════════════════════════════════
    // 内部字段
    // ══════════════════════════════════════════════════════

    /// <summary>资源缓存系统</summary>
    private ResourceCache _resourceCache;

    /// <summary>已加载的AssetBundle映射（名称 -> Bundle）</summary>
    private readonly Dictionary<string, AssetBundle> _loadedBundles
        = new Dictionary<string, AssetBundle>();

    /// <summary>Bundle信息映射（名称 -> BundleInfo）</summary>
    private readonly Dictionary<string, BundleInfo> _bundleInfos
        = new Dictionary<string, BundleInfo>();

    /// <summary>正在进行的异步加载操作</summary>
    private readonly Dictionary<string, object> _activeAsyncOperations
        = new Dictionary<string, object>();

    /// <summary>正在进行的Bundle加载操作</summary>
    private readonly Dictionary<string, BundleLoadOperation> _activeBundleOperations
        = new Dictionary<string, BundleLoadOperation>();

    /// <summary>请求ID计数器（用于跟踪）</summary>
    private int _requestIdCounter = 0;

    /// <summary>Bundle依赖关系映射</summary>
    private readonly Dictionary<string, string[]> _bundleDependencies
        = new Dictionary<string, string[]>();

    // ══════════════════════════════════════════════════════
    // 属性
    // ══════════════════════════════════════════════════════

    /// <summary>是否已初始化</summary>
    public bool IsInitialized { get; private set; }

    /// <summary>缓存统计信息</summary>
    public CacheStats CacheStats => _resourceCache?.GetStats() ?? new CacheStats();

    // ══════════════════════════════════════════════════════
    // MonoSingleton生命周期
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 单例初始化方法，替代Awake
    /// </summary>
    protected override void OnInitialize()
    {
        RegisterServices();
        Log("[ResourceManager] 单例初始化完成（ServiceLocator 已注册）");
    }

    /// <summary>配置注入后的完整初始化（ISystem）</summary>
    public override void Initialize()
    {
        InitSingleton();
        InitializeCache();
        SetupEventListeners();
        IsInitialized = true;
        Log("[ResourceManager] 完整初始化完成");
    }

    /// <summary>系统关闭清理（ISystem）</summary>
    public override void Shutdown()
    {
        ServiceLocator.Unregister<ResourceManager>();
        ServiceLocator.Unregister<IResourceLoader>();
        ServiceLocator.Unregister<IAssetBundleLoader>();
        EventBus.Unsubscribe<MemoryWarningEvent>(OnMemoryWarning);
        CleanupAllOperations();
        UnloadAllBundles();
        _resourceCache?.Clear();
        Log("[ResourceManager] 已关闭");
        base.Shutdown();
    }

    // ══════════════════════════════════════════════════════
    // 初始化方法
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 初始化缓存系统
    /// </summary>
    private void InitializeCache()
    {
        if (CacheConfig == null)
        {
            LogWarning("[ResourceManager] 未找到缓存配置，使用默认值");
            _resourceCache = new ResourceCache(maxCacheItems: 100, maxMemoryUsageMB: 100);
        }
        else
        {
            _resourceCache = new ResourceCache(
                maxCacheItems: CacheConfig.MaxCacheItems,
                maxMemoryUsageMB: CacheConfig.MaxMemoryUsageMB
            );
        }

        Log($"[ResourceManager] 缓存初始化: {_resourceCache.Count}项");
    }

    /// <summary>
    /// 注册服务到ServiceLocator
    /// </summary>
    private void RegisterServices()
    {
        ServiceLocator.Register<ResourceManager>(this);
        ServiceLocator.Register<IResourceLoader>(this);
        ServiceLocator.Register<IAssetBundleLoader>(this);
        Log("[ResourceManager] 服务已注册到ServiceLocator");
    }

    /// <summary>
    /// 设置事件监听器
    /// </summary>
    private void SetupEventListeners()
    {
        // 监听内存警告事件，自动清理缓存
        EventBus.Subscribe<MemoryWarningEvent>(OnMemoryWarning);
        Log("[ResourceManager] 事件监听器已设置");
    }

    // ══════════════════════════════════════════════════════
    // IResourceLoader实现 - 核心加载方法
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 同步加载资源（Resources.Load包装）
    /// </summary>
    public T Load<T>(string path) where T : UnityEngine.Object
    {
        ValidatePath(path);
        string requestId = GenerateRequestId();

        // 发布加载开始事件
        PublishLoadStarted(path, LoadType.Sync, requestId);

        try
        {
            // 1. 检查缓存
            if (_resourceCache.TryGet<T>(path, out var cachedResource))
            {
                Log($"[ResourceManager] 缓存命中: {path}");
                PublishLoadCompleted(path, true, requestId);
                return cachedResource;
            }

            // 2. 从Resources加载
            Log($"[ResourceManager] 同步加载: {path}");
            T resource = Resources.Load<T>(path);

            if (resource == null)
            {
                throw new ResourceLoadException(
                    LoadErrorType.NotFound,
                    $"资源不存在: {path}",
                    path
                );
            }

            // 3. 添加到缓存
            _resourceCache.Cache(path, resource);

            // 4. 发布加载完成事件
            PublishLoadCompleted(path, false, requestId);

            return resource;
        }
        catch (Exception ex)
        {
            // 发布加载失败事件
            var errorType = ex is ResourceLoadException rle ?
                rle.ErrorType : LoadErrorType.Unknown;
            PublishLoadFailed(path, errorType, ex.Message, requestId);

            LogError($"[ResourceManager] 加载失败: {path} - {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 异步加载资源
    /// </summary>
    public LoadOperation<T> LoadAsync<T>(string path) where T : UnityEngine.Object
    {
        ValidatePath(path);
        string requestId = GenerateRequestId();

        // 检查是否已有相同路径的异步加载正在进行
        if (_activeAsyncOperations.TryGetValue(path, out var existingOp))
        {
            Log($"[ResourceManager] 重用已有异步操作: {path}");
            return existingOp as LoadOperation<T>;
        }

        // 检查缓存
        if (_resourceCache.TryGet<T>(path, out var cachedResource))
        {
            Log($"[ResourceManager] 异步加载缓存命中: {path}");
            PublishLoadCompleted(path, true, requestId);
            return LoadOperation<T>.CreateCompleted(path, cachedResource);
        }

        // 创建新的异步加载操作（AsyncLoadOperation在构造时自动开始加载）
        Log($"[ResourceManager] 开始异步加载: {path}");
        PublishLoadStarted(path, LoadType.Async, requestId);
        var operation = new AsyncLoadOperation<T>(path, requestId, this);
        _activeAsyncOperations[path] = operation;

        // 通过事件回调处理缓存和清理，避免重复发起Resources.LoadAsync
        operation.OnSucceeded += resource =>
        {
            _resourceCache.Cache(path, resource);
            _activeAsyncOperations.Remove(path);
            PublishLoadCompleted(path, false, requestId);
            Log($"[ResourceManager] 异步加载完成: {path}");
        };
        operation.OnFailed += error =>
        {
            _activeAsyncOperations.Remove(path);
            var errorType = error is ResourceLoadException rle ? rle.ErrorType : LoadErrorType.Unknown;
            PublishLoadFailed(path, errorType, error.Message, requestId);
            LogError($"[ResourceManager] 异步加载失败: {path} - {error.Message}");
        };

        return operation;
    }

    /// <summary>
    /// 预加载资源到缓存（不立即使用）
    /// </summary>
    public void Preload<T>(string path, int priority = 50) where T : UnityEngine.Object
    {
        ValidatePath(path);

        // 如果已在缓存中，跳过
        if (_resourceCache.ContainsKey(path))
        {
            Log($"[ResourceManager] 预加载跳过 - 已在缓存: {path}");
            return;
        }

        // 检查是否已有相同路径的异步加载
        if (_activeAsyncOperations.ContainsKey(path))
        {
            Log($"[ResourceManager] 预加载跳过 - 已有异步加载: {path}");
            return;
        }

        // 启动低优先级异步加载
        Log($"[ResourceManager] 开始预加载: {path} (优先级: {priority})");
        StartCoroutine(PreloadCoroutine<T>(path, priority));
    }

    /// <summary>
    /// 卸载资源（从缓存中移除）
    /// </summary>
    public void Unload(string path, bool forceUnload = false)
    {
        ValidatePath(path);

        bool removed = _resourceCache.Remove(path, forceUnload);
        if (removed)
        {
            Log($"[ResourceManager] 资源卸载: {path} (强制: {forceUnload})");
        }
        else
        {
            Log($"[ResourceManager] 资源卸载失败 - 不在缓存中: {path}");
        }
    }

    /// <summary>
    /// 清理缓存（释放未使用的资源）
    /// </summary>
    public int ClearCache(bool force = false)
    {
        int cleanedCount = _resourceCache.Cleanup(force);
        Log($"[ResourceManager] 缓存清理完成: {cleanedCount}项 (强制: {force})");
        return cleanedCount;
    }

    /// <summary>
    /// 检查资源是否已缓存
    /// </summary>
    public bool IsCached(string path)
    {
        return _resourceCache.ContainsKey(path);
    }

    /// <summary>
    /// 获取缓存统计信息
    /// </summary>
    public CacheStats GetCacheStats()
    {
        return _resourceCache.GetStats();
    }

    // ══════════════════════════════════════════════════════
    // 异步加载协程处理
    // ══════════════════════════════════════════════════════

    // ProcessAsyncLoad已移除：AsyncLoadOperation在构造时自动启动加载，通过OnSucceeded/OnFailed事件回调处理结果

    /// <summary>
    /// 预加载协程
    /// </summary>
    private IEnumerator PreloadCoroutine<T>(string path, int priority) where T : UnityEngine.Object
    {
        // 低优先级预加载 - 每帧等待
        if (priority < 50)
            yield return null;

        var request = Resources.LoadAsync<T>(path);

        while (!request.isDone)
        {
            // 低优先级：每帧只更新一次
            if (priority < 50)
                yield return null;
            // 中优先级：每帧等待一帧
            else if (priority < 80)
                yield return new WaitForEndOfFrame();
            // 高优先级：立即执行（实际还是需要等待）
            else
                yield return null;
        }

        // 如果资源存在，添加到缓存
        if (request.asset != null)
        {
            _resourceCache.Cache(path, request.asset);
            Log($"[ResourceManager] 预加载完成: {path}");
        }
    }

    // ══════════════════════════════════════════════════════
    // IAssetBundleLoader 实现 → 见 ResourceManager.Bundle.cs (partial)
    // 事件/验证/工具/批量加载/调试 → 见 ResourceManager.Utilities.cs (partial)
    // ══════════════════════════════════════════════════════
}
