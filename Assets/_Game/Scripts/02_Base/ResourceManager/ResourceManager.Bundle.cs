// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/02_Base/ResourceManager/ResourceManager.Bundle.cs
// ResourceManager 的 IAssetBundleLoader 实现（partial class）。
// ══════════════════════════════════════════════════════════════════════
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// ResourceManager 部分类 — AssetBundle 加载。
/// 实现 IAssetBundleLoader 接口：同步/异步加载 Bundle、从 Bundle 加载资源。
/// </summary>
public partial class ResourceManager
{
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
}
