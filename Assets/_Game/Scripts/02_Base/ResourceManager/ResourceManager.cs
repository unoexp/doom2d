// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/02_Infrastructure/ResourceManager/ResourceManager.cs
// 资源管理器核心类，继承MonoSingleton，实现资源加载和AssetBundle加载接口
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
public class ResourceManager : MonoSingleton<ResourceManager>,
    IResourceLoader, IAssetBundleLoader
{
    // ══════════════════════════════════════════════════════
    // 配置字段
    // ══════════════════════════════════════════════════════

    [Header("缓存配置")]
    [SerializeField] private ResourceCacheConfigSO _cacheConfig;

    [Header("网络配置")]
    [SerializeField] private int _defaultNetworkTimeout = 30;
    [SerializeField] private int _maxRetryCount = 3;

    [Header("调试")]
    [SerializeField] private bool _enableLogging = true;
    [SerializeField] private bool _showCacheStatusInInspector = true;

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
        InitializeCache();
        RegisterServices();
        SetupEventListeners();
        IsInitialized = true;

        Log("[ResourceManager] 初始化完成");
    }

    protected override void OnDestroy()
    {
        // 清理所有正在进行的操作
        CleanupAllOperations();

        // 卸载所有AssetBundle
        UnloadAllBundles();

        // 清空缓存
        _resourceCache?.Clear();

        base.OnDestroy();
    }

    // ══════════════════════════════════════════════════════
    // 初始化方法
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 初始化缓存系统
    /// </summary>
    private void InitializeCache()
    {
        if (_cacheConfig == null)
        {
            LogWarning("[ResourceManager] 未找到缓存配置，使用默认值");
            _resourceCache = new ResourceCache(maxCacheItems: 100, maxMemoryUsageMB: 100);
        }
        else
        {
            _resourceCache = new ResourceCache(
                maxCacheItems: _cacheConfig.MaxCacheItems,
                maxMemoryUsageMB: _cacheConfig.MaxMemoryUsageMB
            );
        }

        Log($"[ResourceManager] 缓存初始化: {_resourceCache.Count}项");
    }

    /// <summary>
    /// 注册服务到ServiceLocator
    /// </summary>
    private void RegisterServices()
    {
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
    // IAssetBundleLoader实现 - AssetBundle加载
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 加载AssetBundle（同步）
    /// </summary>
    public AssetBundle LoadBundle(string bundlePath, uint crc = 0)
    {
        ValidateBundlePath(bundlePath);
        string bundleName = GetBundleNameFromPath(bundlePath);
        string requestId = GenerateRequestId();

        Log($"[ResourceManager] 同步加载Bundle: {bundlePath}");

        try
        {
            // 检查是否已加载
            if (_loadedBundles.TryGetValue(bundleName, out var loadedBundle))
            {
                Log($"[ResourceManager] Bundle已加载: {bundleName}");
                return loadedBundle;
            }

            // 加载Bundle
            AssetBundle bundle;
            if (IsRemotePath(bundlePath))
            {
                // 远程Bundle - 需要先下载到本地（简化实现，实际可能需要更复杂的处理）
                throw new NotImplementedException("远程同步加载需要先实现下载功能");
            }
            else
            {
                // 本地Bundle
                bundle = AssetBundle.LoadFromFile(bundlePath, crc);
            }

            if (bundle == null)
            {
                throw new ResourceLoadException(
                    LoadErrorType.NotFound,
                    $"AssetBundle不存在或加载失败: {bundlePath}",
                    bundlePath
                );
            }

            // 记录已加载的Bundle
            _loadedBundles[bundleName] = bundle;
            UpdateBundleInfo(bundleName, bundlePath, bundle, isRemote: IsRemotePath(bundlePath));

            Log($"[ResourceManager] Bundle加载成功: {bundleName}");
            return bundle;
        }
        catch (Exception ex)
        {
            LogError($"[ResourceManager] Bundle加载失败: {bundlePath} - {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 异步加载AssetBundle
    /// </summary>
    public LoadOperation<AssetBundle> LoadBundleAsync(string bundlePath, uint crc = 0)
    {
        ValidateBundlePath(bundlePath);
        string bundleName = GetBundleNameFromPath(bundlePath);
        string requestId = GenerateRequestId();

        // 检查是否已有相同Bundle的异步加载正在进行
        if (_activeBundleOperations.TryGetValue(bundleName, out var existingOp))
        {
            Log($"[ResourceManager] 重用已有Bundle异步操作: {bundleName}");
            return existingOp;
        }

        // 检查是否已加载
        if (_loadedBundles.TryGetValue(bundleName, out var loadedBundle))
        {
            Log($"[ResourceManager] Bundle异步加载缓存命中: {bundleName}");
            return LoadOperation<AssetBundle>.CreateCompleted(bundlePath, loadedBundle);
        }

        // 创建新的Bundle加载操作
        Log($"[ResourceManager] 开始异步加载Bundle: {bundlePath}");
        var operation = new BundleLoadOperation(bundlePath, requestId)
        {
            IsRemote = IsRemotePath(bundlePath)
        };
        _activeBundleOperations[bundleName] = operation;

        // 启动协程处理异步加载
        StartCoroutine(ProcessBundleLoadAsync(bundlePath, bundleName, crc, operation));

        return operation;
    }

    /// <summary>
    /// 异步加载AssetBundle的协程
    /// </summary>
    private IEnumerator ProcessBundleLoadAsync(string bundlePath, string bundleName, uint crc, BundleLoadOperation operation)
    {
        AssetBundle bundle = null;
        Exception loadError = null;
        bool isRemote = IsRemotePath(bundlePath);

        if (isRemote)
        {
            var webRequest = UnityWebRequestAssetBundle.GetAssetBundle(bundlePath, crc);
            operation.DownloadSize = 0;
            operation.DownloadedBytes = 0;
            webRequest.SendWebRequest();

            while (!webRequest.isDone)
            {
                operation.SetProgress(webRequest.downloadProgress);
                operation.DownloadedBytes = (long)webRequest.downloadedBytes;
                yield return null;
            }

            if (webRequest.result != UnityWebRequest.Result.Success)
                loadError = new ResourceLoadException(LoadErrorType.NetworkError, $"远程Bundle下载失败: {webRequest.error}", bundlePath);
            else
                bundle = DownloadHandlerAssetBundle.GetContent(webRequest);
        }
        else
        {
            var bundleRequest = AssetBundle.LoadFromFileAsync(bundlePath, crc);
            while (!bundleRequest.isDone)
            {
                operation.SetProgress(bundleRequest.progress);
                yield return null;
            }
            bundle = bundleRequest.assetBundle;
        }

        if (loadError == null && bundle == null)
            loadError = new ResourceLoadException(LoadErrorType.NotFound, $"AssetBundle不存在或加载失败: {bundlePath}", bundlePath);

        _activeBundleOperations.Remove(bundleName);

        if (loadError != null)
        {
            operation.NotifyFailure(loadError);
            LogError($"[ResourceManager] Bundle异步加载失败: {bundlePath} - {loadError.Message}");
            yield break;
        }

        _loadedBundles[bundleName] = bundle;
        UpdateBundleInfo(bundleName, bundlePath, bundle, isRemote: isRemote);
        operation.NotifySuccess(bundle);
        Log($"[ResourceManager] Bundle异步加载完成: {bundleName}");
    }

    /// <summary>
    /// 从已加载的Bundle中加载资源
    /// </summary>
    public T LoadFromBundle<T>(string bundleName, string assetPath) where T : UnityEngine.Object
    {
        ValidateBundleName(bundleName);
        ValidateAssetPath(assetPath);

        if (!_loadedBundles.TryGetValue(bundleName, out var bundle))
        {
            throw new ResourceLoadException(
                LoadErrorType.NotFound,
                $"Bundle未加载: {bundleName}",
                bundleName
            );
        }

        try
        {
            T asset = bundle.LoadAsset<T>(assetPath);
            if (asset == null)
            {
                throw new ResourceLoadException(
                    LoadErrorType.NotFound,
                    $"资源不存在或类型不匹配: {assetPath} in bundle {bundleName}",
                    assetPath
                );
            }

            Log($"[ResourceManager] 从Bundle加载资源: {assetPath} from {bundleName}");
            return asset;
        }
        catch (Exception ex)
        {
            LogError($"[ResourceManager] Bundle资源加载失败: {assetPath} from {bundleName} - {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 异步从Bundle中加载资源
    /// </summary>
    public LoadOperation<T> LoadFromBundleAsync<T>(string bundleName, string assetPath) where T : UnityEngine.Object
    {
        ValidateBundleName(bundleName);
        ValidateAssetPath(assetPath);

        string requestId = GenerateRequestId();
        string operationKey = $"{bundleName}:{assetPath}";

        // 检查是否已有相同操作
        if (_activeAsyncOperations.TryGetValue(operationKey, out var existingOp))
        {
            Log($"[ResourceManager] 重用已有Bundle资源异步操作: {operationKey}");
            return existingOp as LoadOperation<T>;
        }

        if (!_loadedBundles.TryGetValue(bundleName, out var bundle))
        {
            return LoadOperation<T>.CreateFailed(assetPath,
                new ResourceLoadException(LoadErrorType.NotFound, $"Bundle未加载: {bundleName}", bundleName));
        }

        // 创建待驱动的操作
        var operation = LoadOperation<T>.CreatePending(assetPath, requestId);
        _activeAsyncOperations[operationKey] = operation;

        // 启动协程
        StartCoroutine(ProcessBundleAssetLoadAsync<T>(bundle, assetPath, operationKey, operation));

        return operation;
    }

    /// <summary>
    /// 卸载AssetBundle
    /// </summary>
    public void UnloadBundle(string bundleName, bool unloadAllLoadedObjects = false)
    {
        ValidateBundleName(bundleName);

        if (!_loadedBundles.TryGetValue(bundleName, out var bundle))
        {
            Log($"[ResourceManager] Bundle卸载失败 - 未加载: {bundleName}");
            return;
        }

        try
        {
            bundle.Unload(unloadAllLoadedObjects);
            _loadedBundles.Remove(bundleName);
            _bundleInfos.Remove(bundleName);

            Log($"[ResourceManager] Bundle卸载成功: {bundleName} (卸载资源: {unloadAllLoadedObjects})");
        }
        catch (Exception ex)
        {
            LogError($"[ResourceManager] Bundle卸载失败: {bundleName} - {ex.Message}");
        }
    }

    /// <summary>
    /// 检查Bundle是否已加载
    /// </summary>
    public bool IsBundleLoaded(string bundleName)
    {
        ValidateBundleName(bundleName);
        return _loadedBundles.ContainsKey(bundleName);
    }

    /// <summary>
    /// 获取已加载Bundle的信息
    /// </summary>
    public BundleInfo GetBundleInfo(string bundleName)
    {
        ValidateBundleName(bundleName);

        if (_bundleInfos.TryGetValue(bundleName, out var info))
            return info;

        // 返回默认信息
        return new BundleInfo
        {
            Name = bundleName,
            IsLoaded = false,
            Path = string.Empty,
            Size = 0,
            AssetCount = 0
        };
    }

    /// <summary>
    /// 下载远程Bundle到本地缓存
    /// </summary>
    public LoadOperation<string> DownloadBundle(string remoteUrl, string localBundleName, string hash = null)
    {
        ValidateRemoteUrl(remoteUrl);
        ValidateBundleName(localBundleName);

        string requestId = GenerateRequestId();
        var operation = new DownloadOperation(remoteUrl, requestId)
        {
            LocalPath = GetLocalCachePath(localBundleName),
            Hash = hash
        };

        // 启动下载协程
        StartCoroutine(ProcessBundleDownload(remoteUrl, localBundleName, hash, operation));

        return operation;
    }

    /// <summary>
    /// 检查远程Bundle更新
    /// </summary>
    public bool CheckForUpdate(string bundleName, string remoteHash)
    {
        ValidateBundleName(bundleName);

        if (string.IsNullOrEmpty(remoteHash))
            return false;

        // 检查本地Bundle是否存在
        string localPath = GetLocalCachePath(bundleName);
        if (!System.IO.File.Exists(localPath))
            return true; // 本地不存在，需要下载

        // 如果有本地哈希，进行比较
        string localHash = GetFileHash(localPath);
        return localHash != remoteHash;
    }

    /// <summary>
    /// 清理Bundle缓存
    /// </summary>
    public void ClearBundleCache(string bundleName = null)
    {
        if (string.IsNullOrEmpty(bundleName))
        {
            // 清理所有缓存
            string cacheDir = GetBundleCacheDirectory();
            if (System.IO.Directory.Exists(cacheDir))
            {
                System.IO.Directory.Delete(cacheDir, true);
                Log($"[ResourceManager] 清理所有Bundle缓存: {cacheDir}");
            }
        }
        else
        {
            // 清理指定Bundle
            string filePath = GetLocalCachePath(bundleName);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
                Log($"[ResourceManager] 清理Bundle缓存: {filePath}");
            }
        }
    }

    // ══════════════════════════════════════════════════════
    // 协程处理方法
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 处理从Bundle中异步加载资源的协程
    /// </summary>
    private IEnumerator ProcessBundleAssetLoadAsync<T>(AssetBundle bundle, string assetPath, string operationKey, LoadOperation<T> operation) where T : UnityEngine.Object
    {
        var assetRequest = bundle.LoadAssetAsync<T>(assetPath);

        while (!assetRequest.isDone)
        {
            operation.SetProgress(assetRequest.progress);
            yield return null;
        }

        _activeAsyncOperations.Remove(operationKey);

        T asset = assetRequest.asset as T;
        if (asset == null)
        {
            operation.NotifyFailure(new ResourceLoadException(LoadErrorType.NotFound, $"Bundle资源不存在或类型不匹配: {assetPath}", assetPath));
            LogError($"[ResourceManager] Bundle资源异步加载失败: {assetPath}");
        }
        else
        {
            operation.NotifySuccess(asset);
            Log($"[ResourceManager] Bundle资源异步加载完成: {assetPath}");
        }
    }

    /// <summary>
    /// 处理Bundle下载的协程
    /// </summary>
    private IEnumerator ProcessBundleDownload(string remoteUrl, string localBundleName, string expectedHash, DownloadOperation operation)
    {
        string localPath = operation.LocalPath;

        string cacheDir = System.IO.Path.GetDirectoryName(localPath);
        if (!System.IO.Directory.Exists(cacheDir))
            System.IO.Directory.CreateDirectory(cacheDir);

        var webRequest = UnityWebRequest.Get(remoteUrl);
        webRequest.downloadHandler = new DownloadHandlerFile(localPath);
        webRequest.SendWebRequest();

        while (!webRequest.isDone)
        {
            operation.SetProgress(webRequest.downloadProgress);
            operation.DownloadedBytes = (long)webRequest.downloadedBytes;
            yield return null;
        }

        if (webRequest.result != UnityWebRequest.Result.Success)
        {
            operation.NotifyFailure(new ResourceLoadException(LoadErrorType.NetworkError, $"Bundle下载失败: {webRequest.error}", remoteUrl));
            LogError($"[ResourceManager] Bundle下载失败: {remoteUrl} - {webRequest.error}");
            yield break;
        }

        if (!string.IsNullOrEmpty(expectedHash))
        {
            string actualHash = GetFileHash(localPath);
            if (actualHash != expectedHash)
            {
                if (System.IO.File.Exists(localPath))
                    System.IO.File.Delete(localPath);
                operation.NotifyFailure(new ResourceLoadException(LoadErrorType.ParseError, $"Bundle哈希验证失败: expected {expectedHash}, got {actualHash}", remoteUrl));
                LogError($"[ResourceManager] Bundle哈希验证失败: {localBundleName}");
                yield break;
            }
        }

        operation.TotalBytes = (long)webRequest.downloadedBytes;
        operation.NotifySuccess(localPath);
        Log($"[ResourceManager] Bundle下载完成: {localBundleName} ({operation.TotalBytes}字节)");
    }

    // ══════════════════════════════════════════════════════
    // 事件发布方法
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 发布加载开始事件
    /// </summary>
    private void PublishLoadStarted(string path, LoadType loadType, string requestId)
    {
        EventBus.Publish(new ResourceLoadStartedEvent
        {
            ResourcePath = path,
            LoadType = loadType,
            RequestId = requestId
        });
    }

    /// <summary>
    /// 发布加载进度事件
    /// </summary>
    private void PublishLoadProgress(string path, float progress, string requestId)
    {
        EventBus.Publish(new ResourceLoadProgressEvent
        {
            ResourcePath = path,
            Progress = progress,
            RequestId = requestId
        });
    }

    /// <summary>
    /// 发布加载完成事件
    /// </summary>
    private void PublishLoadCompleted(string path, bool fromCache, string requestId)
    {
        EventBus.Publish(new ResourceLoadCompletedEvent
        {
            ResourcePath = path,
            FromCache = fromCache,
            RequestId = requestId
        });
    }

    /// <summary>
    /// 发布加载失败事件
    /// </summary>
    private void PublishLoadFailed(string path, LoadErrorType errorType, string errorMessage, string requestId)
    {
        EventBus.Publish(new ResourceLoadFailedEvent
        {
            ResourcePath = path,
            ErrorType = errorType,
            ErrorMessage = errorMessage,
            RequestId = requestId
        });
    }

    /// <summary>
    /// 发布Bundle下载进度事件
    /// </summary>
    private void PublishBundleDownloadProgress(string bundleName, long downloadedBytes, long totalBytes)
    {
        EventBus.Publish(new AssetBundleDownloadProgressEvent
        {
            BundleName = bundleName,
            DownloadedBytes = downloadedBytes,
            TotalBytes = totalBytes,
            Progress = totalBytes > 0 ? (float)downloadedBytes / totalBytes : 0f
        });
    }

    /// <summary>
    /// 内存警告事件处理
    /// </summary>
    private void OnMemoryWarning(MemoryWarningEvent evt)
    {
        Log($"[ResourceManager] 收到内存警告: {evt.UsedBytes / (1024*1024)}MB (阈值: {evt.ThresholdBytes / (1024*1024)}MB)");

        // 自动清理缓存
        int cleaned = ClearCache(false);
        Log($"[ResourceManager] 内存警告触发缓存清理: {cleaned}项");
    }

    // ══════════════════════════════════════════════════════
    // 验证方法
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 验证资源路径
    /// </summary>
    private void ValidatePath(string path)
    {
        if (string.IsNullOrEmpty(path))
            throw new ArgumentException("资源路径不能为空", nameof(path));
    }

    /// <summary>
    /// 验证Bundle路径
    /// </summary>
    private void ValidateBundlePath(string bundlePath)
    {
        if (string.IsNullOrEmpty(bundlePath))
            throw new ArgumentException("Bundle路径不能为空", nameof(bundlePath));
    }

    /// <summary>
    /// 验证Bundle名称
    /// </summary>
    private void ValidateBundleName(string bundleName)
    {
        if (string.IsNullOrEmpty(bundleName))
            throw new ArgumentException("Bundle名称不能为空", nameof(bundleName));
    }

    /// <summary>
    /// 验证资源路径（Bundle内）
    /// </summary>
    private void ValidateAssetPath(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath))
            throw new ArgumentException("资源路径不能为空", nameof(assetPath));
    }

    /// <summary>
    /// 验证远程URL
    /// </summary>
    private void ValidateRemoteUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
            throw new ArgumentException("远程URL不能为空", nameof(url));

        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            throw new ArgumentException($"URL必须以http://或https://开头: {url}", nameof(url));
    }

    // ══════════════════════════════════════════════════════
    // 工具方法
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 生成请求ID
    /// </summary>
    private string GenerateRequestId()
    {
        return $"req_{_requestIdCounter++}_{DateTime.Now.Ticks}";
    }

    /// <summary>
    /// 从路径提取Bundle名称
    /// </summary>
    private string GetBundleNameFromPath(string bundlePath)
    {
        return System.IO.Path.GetFileNameWithoutExtension(bundlePath);
    }

    /// <summary>
    /// 判断是否为远程路径
    /// </summary>
    private bool IsRemotePath(string path)
    {
        return path.StartsWith("http://") || path.StartsWith("https://");
    }

    /// <summary>
    /// 获取Bundle缓存目录
    /// </summary>
    private string GetBundleCacheDirectory()
    {
        return System.IO.Path.Combine(Application.persistentDataPath, "BundleCache");
    }

    /// <summary>
    /// 获取本地缓存路径
    /// </summary>
    private string GetLocalCachePath(string bundleName)
    {
        string cacheDir = GetBundleCacheDirectory();
        return System.IO.Path.Combine(cacheDir, $"{bundleName}.unity3d");
    }

    /// <summary>
    /// 获取文件哈希（简化实现）
    /// </summary>
    private string GetFileHash(string filePath)
    {
        if (!System.IO.File.Exists(filePath))
            return string.Empty;

        try
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            using (var stream = System.IO.File.OpenRead(filePath))
            {
                byte[] hashBytes = md5.ComputeHash(stream);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 更新Bundle信息
    /// </summary>
    private void UpdateBundleInfo(string bundleName, string bundlePath, AssetBundle bundle, bool isRemote = false)
    {
        var info = new BundleInfo
        {
            Name = bundleName,
            IsLoaded = true,
            IsRemote = isRemote,
            Path = bundlePath,
            Size = 0, // 实际需要获取文件大小
            Hash = string.Empty,
            AssetCount = bundle.GetAllAssetNames().Length,
            Dependencies = Array.Empty<string>()
        };

        _bundleInfos[bundleName] = info;
    }

    // ══════════════════════════════════════════════════════
    // 清理和卸载方法
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 清理所有正在进行的操作
    /// </summary>
    private void CleanupAllOperations()
    {
        // 清理异步资源加载操作（AsyncLoadOperation自管理，清空记录即可）
        _activeAsyncOperations.Clear();

        // 清理Bundle加载操作
        foreach (var operation in _activeBundleOperations.Values)
        {
            operation.Cancel();
        }
        _activeBundleOperations.Clear();

        Log("[ResourceManager] 清理所有进行中的加载操作");
    }

    /// <summary>
    /// 卸载所有AssetBundle
    /// </summary>
    private void UnloadAllBundles()
    {
        foreach (var kvp in _loadedBundles)
        {
            try
            {
                kvp.Value.Unload(true);
                Log($"[ResourceManager] Bundle卸载: {kvp.Key}");
            }
            catch (Exception ex)
            {
                LogError($"[ResourceManager] Bundle卸载失败: {kvp.Key} - {ex.Message}");
            }
        }

        _loadedBundles.Clear();
        _bundleInfos.Clear();
        Log("[ResourceManager] 所有Bundle已卸载");
    }

    // ══════════════════════════════════════════════════════
    // 日志方法
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 普通日志
    /// </summary>
    private void Log(string message)
    {
        if (_enableLogging)
            Debug.Log(message);
    }

    /// <summary>
    /// 警告日志
    /// </summary>
    private void LogWarning(string message)
    {
        if (_enableLogging)
            Debug.LogWarning(message);
    }

    /// <summary>
    /// 错误日志
    /// </summary>
    private void LogError(string message)
    {
        Debug.LogError(message);
    }

    // ══════════════════════════════════════════════════════
    // 批量加载方法
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 批量加载资源
    /// </summary>
    /// <param name="paths">资源路径数组</param>
    /// <param name="batchId">批次ID，null则自动生成</param>
    /// <returns>批量加载操作</returns>
    public BatchLoadOperation BatchLoad<T>(string[] paths, string batchId = null) where T : UnityEngine.Object
    {
        if (paths == null || paths.Length == 0)
            throw new ArgumentException("资源路径数组不能为空", nameof(paths));

        if (string.IsNullOrEmpty(batchId))
            batchId = $"batch_{DateTime.Now.Ticks}";

        var operation = new BatchLoadOperation(batchId, paths.Length);

        // 发布批量加载开始事件
        EventBus.Publish(new BatchLoadStartedEvent
        {
            BatchId = batchId,
            TotalItems = paths.Length
        });

        // 启动批量加载协程
        StartCoroutine(ProcessBatchLoad<T>(paths, operation));

        return operation;
    }

    /// <summary>
    /// 处理批量加载的协程
    /// </summary>
    private IEnumerator ProcessBatchLoad<T>(string[] paths, BatchLoadOperation operation) where T : UnityEngine.Object
    {
        int completedCount = 0;
        int failedCount = 0;

        for (int i = 0; i < paths.Length; i++)
        {
            string path = paths[i];

            // 尝试缓存命中（同步）
            if (_resourceCache.TryGet<T>(path, out var cachedResource))
            {
                operation.Results[path] = cachedResource;
                completedCount++;
            }
            else
            {
                // 异步加载（yield在try-catch外）
                var asyncOp = LoadAsync<T>(path);
                yield return asyncOp;

                if (asyncOp.IsSuccessful)
                {
                    operation.Results[path] = asyncOp.Result;
                    completedCount++;
                }
                else
                {
                    operation.Errors[path] = asyncOp.Error?.Message ?? "Unknown error";
                    failedCount++;
                }
            }

            // 更新进度
            operation.Progress = (float)(completedCount + failedCount) / paths.Length;

            EventBus.Publish(new BatchLoadProgressEvent
            {
                BatchId = operation.BatchId,
                CompletedItems = completedCount + failedCount,
                TotalItems = paths.Length,
                Progress = operation.Progress
            });

            if (i % 5 == 0)
                yield return null;
        }

        // 完成批量加载
        operation.IsDone = true;

        // 发布批量加载完成事件
        EventBus.Publish(new BatchLoadCompletedEvent
        {
            BatchId = operation.BatchId,
            TotalItems = paths.Length,
            FailedItems = failedCount
        });

        Log($"[ResourceManager] 批量加载完成: {completedCount}成功, {failedCount}失败");
    }

    /// <summary>
    /// 批量加载操作类
    /// </summary>
    public class BatchLoadOperation
    {
        public string BatchId { get; private set; }
        public bool IsDone { get; set; }
        public float Progress { get; set; }
        public Dictionary<string, UnityEngine.Object> Results { get; private set; }
        public Dictionary<string, string> Errors { get; private set; }

        public BatchLoadOperation(string batchId, int totalItems)
        {
            BatchId = batchId;
            Results = new Dictionary<string, UnityEngine.Object>(totalItems);
            Errors = new Dictionary<string, string>();
        }
    }

    // ══════════════════════════════════════════════════════
    // Inspector调试显示（如果启用）
    // ══════════════════════════════════════════════════════

    private void OnGUI()
    {
        if (!_showCacheStatusInInspector)
            return;

        if (Event.current.type == EventType.Repaint)
        {
            // 在屏幕上显示缓存状态（调试用）
            var stats = CacheStats;
            GUI.Label(new Rect(10, 10, 300, 100),
                $"ResourceManager状态:\n" +
                $"缓存项: {stats.ItemCount}\n" +
                $"内存使用: {stats.MemoryUsageBytes / (1024 * 1024):F2}MB\n" +
                $"命中率: {stats.HitRate:P2}\n" +
                $"加载中: {_activeAsyncOperations.Count}");
        }
    }

    private void Update()
    {
        // 定期检查内存使用
        if (Time.frameCount % 300 == 0) // 每5秒（假设60FPS）
        {
            long totalMemory = System.GC.GetTotalMemory(false);
            if (_cacheConfig != null && totalMemory > (long)_cacheConfig.MemoryWarningThresholdMB * 1024 * 1024)
            {
                // 自动清理
                ClearCache();
            }
        }
    }
}