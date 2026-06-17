// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/02_Base/ResourceManager/ResourceManager.Utilities.cs
// ResourceManager 的事件发布、验证、工具、清理、批量加载（partial class）。
// ══════════════════════════════════════════════════════════════════════
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// ResourceManager 部分类 — 事件发布、验证、工具方法、清理、批量加载、调试UI。
/// </summary>
public partial class ResourceManager
{

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
        if (EnableLogging)
            Debug.Log(message);
    }

    /// <summary>
    /// 警告日志
    /// </summary>
    private void LogWarning(string message)
    {
        if (EnableLogging)
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
        if (!ShowCacheStatusInInspector)
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
            if (CacheConfig != null && totalMemory > (long)CacheConfig.MemoryWarningThresholdMB * 1024 * 1024)
            {
                // 自动清理
                ClearCache();
            }
        }
    }
}
