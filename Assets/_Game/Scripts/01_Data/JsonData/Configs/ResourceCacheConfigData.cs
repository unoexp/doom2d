// 📁 01_Data/JsonData/Configs/ResourceCacheConfigData.cs
// 资源缓存配置数据 JSON 模型

using Newtonsoft.Json;

/// <summary>
/// 预加载资源条目
/// </summary>
[System.Serializable]
public struct PreloadResourceEntryData
{
    [JsonProperty("path")] public string Path;
    [JsonProperty("type")] public string Type;
}

[JsonObject(MemberSerialization.OptIn)]
public class ResourceCacheConfigData
{
    [JsonProperty("maxCacheItems")] public int MaxCacheItems = 100;
    [JsonProperty("maxMemoryUsageMB")] public int MaxMemoryUsageMB = 256;
    [JsonProperty("memoryWarningThresholdMB")] public int MemoryWarningThresholdMB = 200;
    [JsonProperty("cacheCleanupInterval")] public float CacheCleanupInterval = 60f;
    [JsonProperty("cacheItemTimeout")] public float CacheItemTimeout = 300f;
    [JsonProperty("remoteBundleBaseUrl")] public string RemoteBundleBaseUrl;
    [JsonProperty("downloadRetryCount")] public int DownloadRetryCount = 3;
    [JsonProperty("downloadTimeoutSeconds")] public int DownloadTimeoutSeconds = 30;
    [JsonProperty("downloadChunkSizeKB")] public int DownloadChunkSizeKB = 256;
    [JsonProperty("maxConcurrentDownloads")] public int MaxConcurrentDownloads = 2;
    [JsonProperty("localBundleDirectory")] public string LocalBundleDirectory;
    [JsonProperty("bundleCacheDirectory")] public string BundleCacheDirectory;
    [JsonProperty("preloadResources")] public PreloadResourceEntryData[] PreloadResources;
}
