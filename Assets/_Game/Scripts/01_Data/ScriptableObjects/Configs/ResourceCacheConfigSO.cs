// 📁 Assets/_Game/01_Data/ScriptableObjects/Configs/ResourceCacheConfigSO.cs
// ─────────────────────────────────────────────────────────────────────
// 资源加载系统的全局数值配置。
// 所有缓存、网络、性能参数均在此 ScriptableObject 中定义，
// 策划和开发者可直接在 Inspector 中调整，无需修改任何代码。
// ─────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "ResourceCacheConfig",
    menuName  = "SurvivalGame/Config/Resource Cache Config")]
public class ResourceCacheConfigSO : ScriptableObject
{
    // ══════════════════════════════════════════════════════
    // 缓存配置
    // ══════════════════════════════════════════════════════

    [Header("─── 内存缓存配置 ───────────────────────────")]
    [Tooltip("最大缓存资源数量（0表示无限制）\n" +
             "达到限制时使用LRU算法淘汰最久未使用的资源")]
    [Range(0, 1000)]
    public int MaxCacheItems = 100;

    [Tooltip("最大内存使用量（MB，0表示无限制）\n" +
             "包含所有缓存资源估计的内存占用")]
    [Range(0, 1024)]
    public int MaxMemoryUsageMB = 256;

    [Tooltip("缓存清理检查间隔（秒）\n" +
             "系统定期检查并清理长时间未使用的资源")]
    [Range(10, 600)]
    public float CacheCleanupInterval = 60f;

    [Tooltip("资源未访问超时时间（秒）\n" +
             "超过此时间未访问的资源将被视为可清理")]
    [Range(60, 3600)]
    public float CacheItemTimeout = 600f;

    // ══════════════════════════════════════════════════════
    // 网络加载配置
    // ══════════════════════════════════════════════════════

    [Header("─── 网络加载配置 ───────────────────────────")]
    [Tooltip("AssetBundle远程下载基础URL\n" +
             "例如: https://cdn.yourgame.com/bundles/")]
    public string RemoteBundleBaseUrl = "";

    [Tooltip("下载重试次数（网络错误或超时时）")]
    [Range(0, 10)]
    public int DownloadRetryCount = 3;

    [Tooltip("下载超时时间（秒）")]
    [Range(5, 120)]
    public float DownloadTimeoutSeconds = 30f;

    [Tooltip("断点续传分块大小（KB）\n" +
             "大文件下载时分割为多个块，支持断点续传")]
    [Range(64, 10240)]
    public int DownloadChunkSizeKB = 1024;

    [Tooltip("最大并发下载数量")]
    [Range(1, 10)]
    public int MaxConcurrentDownloads = 3;

    // ══════════════════════════════════════════════════════
    // 本地AssetBundle配置
    // ══════════════════════════════════════════════════════

    [Header("─── 本地AssetBundle配置 ─────────────────────")]
    [Tooltip("本地AssetBundle存储目录（相对于StreamingAssets）")]
    public string LocalBundleDirectory = "AssetBundles";

    [Tooltip("Bundle缓存目录（相对于Application.persistentDataPath）")]
    public string BundleCacheDirectory = "BundleCache";

    [Tooltip("Bundle清单文件名")]
    public string BundleManifestName = "AssetBundles";

    [Tooltip("是否启用Bundle依赖加载")]
    public bool EnableBundleDependencies = true;

    // ══════════════════════════════════════════════════════
    // 性能优化配置
    // ══════════════════════════════════════════════════════

    [Header("─── 性能优化配置 ───────────────────────────")]
    [Tooltip("异步加载队列最大长度\n" +
             "超过此长度的请求将排队等待")]
    [Range(1, 100)]
    public int MaxAsyncQueueLength = 20;

    [Tooltip("预加载资源列表\n" +
             "游戏启动时自动预加载这些资源到缓存")]
    public List<PreloadResourceEntry> PreloadResources = new List<PreloadResourceEntry>();

    [Tooltip("必须预加载的AssetBundle列表\n" +
             "游戏启动时强制加载，失败则无法进入游戏")]
    public List<string> EssentialBundles = new List<string>();

    [Tooltip("加载操作对象池初始大小\n" +
             "减少运行时GC分配")]
    [Range(10, 200)]
    public int LoadOperationPoolSize = 50;

    // ══════════════════════════════════════════════════════
    // 调试和日志配置
    // ══════════════════════════════════════════════════════

    [Header("─── 调试和日志配置 ─────────────────────────")]
    [Tooltip("是否启用详细调试日志")]
    public bool EnableDebugLog = false;

    [Tooltip("是否在Editor中模拟网络延迟（秒）")]
    [Range(0f, 5f)]
    public float SimulateNetworkLatency = 0f;

    [Tooltip("是否在Editor中模拟下载失败（用于测试）")]
    public bool SimulateDownloadFailure = false;

    [Tooltip("性能监控采样间隔（秒）")]
    [Range(1f, 60f)]
    public float PerformanceSamplingInterval = 10f;

    // ══════════════════════════════════════════════════════
    // 内存监控配置
    // ══════════════════════════════════════════════════════

    [Header("─── 内存监控配置 ───────────────────────────")]
    [Tooltip("内存监控检查间隔（秒）")]
    [Range(10, 300)]
    public float MemoryCheckInterval = 30f;

    [Tooltip("内存警告阈值（MB）\n" +
             "超过此阈值时触发自动清理")]
    [Range(100, 2048)]
    public int MemoryWarningThresholdMB = 512;

    [Tooltip("内存临界阈值（MB）\n" +
             "超过此阈值时强制清理所有非必要资源")]
    [Range(200, 4096)]
    public int MemoryCriticalThresholdMB = 1024;

    // ══════════════════════════════════════════════════════
    // 平台特定配置
    // ══════════════════════════════════════════════════════

    [Header("─── 平台特定配置 ───────────────────────────")]
    [Tooltip("WebGL平台最大并发请求数")]
    [Range(1, 6)]
    public int WebGLMaxConcurrentRequests = 4;

    [Tooltip("移动端（iOS/Android）Bundle压缩格式\n" +
             "LZ4：快速加载，较高压缩比\n" +
             "LZMA：高压缩比，加载较慢")]
    public BundleCompressionFormat MobileCompressionFormat = BundleCompressionFormat.LZ4;

    [Tooltip("Standalone平台是否启用异步文件IO")]
    public bool StandaloneEnableAsyncIO = true;

#if UNITY_EDITOR
    // ══════════════════════════════════════════════════════
    // Editor 校验（仅编辑器下运行，不进包）
    // ══════════════════════════════════════════════════════
    private void OnValidate()
    {
        // 验证阈值设置
        if (MemoryWarningThresholdMB >= MemoryCriticalThresholdMB)
        {
            Debug.LogWarning(
                $"[ResourceCacheConfigSO] MemoryWarningThresholdMB({MemoryWarningThresholdMB}) " +
                $"应小于 MemoryCriticalThresholdMB({MemoryCriticalThresholdMB})！");
        }

        // 验证URL格式
        if (!string.IsNullOrEmpty(RemoteBundleBaseUrl))
        {
            if (!RemoteBundleBaseUrl.EndsWith("/"))
            {
                Debug.LogWarning(
                    "[ResourceCacheConfigSO] RemoteBundleBaseUrl应以'/'结尾以确保路径拼接正确");
            }

            if (!RemoteBundleBaseUrl.StartsWith("http://") &&
                !RemoteBundleBaseUrl.StartsWith("https://"))
            {
                Debug.LogWarning(
                    "[ResourceCacheConfigSO] RemoteBundleBaseUrl应以'http://'或'https://'开头");
            }
        }

        // 验证并发设置
        if (MaxConcurrentDownloads > 10)
        {
            Debug.LogWarning(
                "[ResourceCacheConfigSO] 并发下载数过高可能影响性能，建议不超过10");
        }

        // 验证预加载资源路径
        foreach (var entry in PreloadResources)
        {
            if (string.IsNullOrEmpty(entry.ResourcePath))
            {
                Debug.LogWarning("[ResourceCacheConfigSO] 预加载资源路径不能为空");
            }
            else if (entry.ResourcePath.Contains("\\"))
            {
                Debug.LogWarning($"[ResourceCacheConfigSO] 资源路径应使用正斜杠: {entry.ResourcePath}");
            }
        }

        // 验证Bundle目录
        if (LocalBundleDirectory.Contains(".."))
        {
            Debug.LogWarning("[ResourceCacheConfigSO] Bundle目录不能包含'..'相对路径");
        }
    }
#endif

    // ══════════════════════════════════════════════════════
    // 工具方法
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 获取平台优化的最大并发数
    /// </summary>
    public int GetPlatformMaxConcurrent()
    {
#if UNITY_WEBGL
        return WebGLMaxConcurrentRequests;
#else
        return MaxConcurrentDownloads;
#endif
    }

    /// <summary>
    /// 获取完整的远程Bundle URL
    /// </summary>
    public string GetRemoteBundleUrl(string bundleName)
    {
        if (string.IsNullOrEmpty(RemoteBundleBaseUrl))
            return null;

        return $"{RemoteBundleBaseUrl}{bundleName}";
    }

    /// <summary>
    /// 获取本地Bundle路径
    /// </summary>
    public string GetLocalBundlePath(string bundleName)
    {
        return System.IO.Path.Combine(
            Application.streamingAssetsPath,
            LocalBundleDirectory,
            bundleName);
    }

    /// <summary>
    /// 获取Bundle缓存路径
    /// </summary>
    public string GetBundleCachePath(string bundleName)
    {
        return System.IO.Path.Combine(
            Application.persistentDataPath,
            BundleCacheDirectory,
            bundleName);
    }

    /// <summary>
    /// 检查是否启用远程下载
    /// </summary>
    public bool IsRemoteDownloadEnabled()
    {
        return !string.IsNullOrEmpty(RemoteBundleBaseUrl);
    }
}

// ─────────────────────────────────────────────────────────────────────
// 📁 同文件：配套数据结构
// ─────────────────────────────────────────────────────────────────────

/// <summary>
/// 预加载资源条目
/// </summary>
[Serializable]
public class PreloadResourceEntry
{
    [Tooltip("资源路径（Resources目录下的相对路径）")]
    public string ResourcePath;

    [Tooltip("资源类型（用于类型检查）")]
    public ResourceType Type = ResourceType.Prefab;

    [Tooltip("加载优先级（0-100，越高越优先）")]
    [Range(0, 100)]
    public int Priority = 50;

    [Tooltip("是否必需（失败时记录错误但不阻止游戏启动）")]
    public bool IsEssential = false;

    [Tooltip("资源描述（用于调试）")]
    public string Description = "";

    public PreloadResourceEntry() { }

    public PreloadResourceEntry(string path, ResourceType type, int priority = 50, bool essential = false)
    {
        ResourcePath = path;
        Type = type;
        Priority = priority;
        IsEssential = essential;
    }
}

/// <summary>
/// 资源类型枚举（用于预加载配置）
/// </summary>
public enum ResourceType
{
    Prefab,     // GameObject预制体
    Sprite,     // 精灵
    Material,   // 材质
    AudioClip,  // 音频
    ScriptableObject, // 数据对象
    Scene,      // 场景
    Other       // 其他类型
}

/// <summary>
/// Bundle压缩格式
/// </summary>
public enum BundleCompressionFormat
{
    Uncompressed,   // 无压缩
    LZ4,            // LZ4压缩（推荐）
    LZMA            // LZMA压缩（高压缩比）
}

/// <summary>
/// 平台优化设置
/// </summary>
[Serializable]
public struct PlatformOptimization
{
    public RuntimePlatform Platform;
    public int MaxCacheItems;
    public int MaxMemoryUsageMB;
    public int MaxConcurrentDownloads;
    public BundleCompressionFormat CompressionFormat;
}

/// <summary>
/// 性能采样数据
/// </summary>
[Serializable]
public struct PerformanceSample
{
    public DateTime Timestamp;
    public int CacheHitCount;
    public int CacheMissCount;
    public long MemoryUsageBytes;
    public int ActiveLoadOperations;
    public float AverageLoadTimeMs;
}