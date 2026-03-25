// 📁 02_Infrastructure/EventBus/Events/ResourceEvents.cs
// ⚠️ 所有事件定义为结构体，零GC分配

/// <summary>加载类型</summary>
public enum LoadType
{
    Sync,    // 同步加载
    Async,   // 异步加载
    Bundle   // AssetBundle加载
}

/// <summary>加载错误类型</summary>
public enum LoadErrorType
{
    NotFound,      // 资源不存在
    NetworkError,  // 网络错误
    ParseError,    // 解析错误
    IOError,       // IO错误
    Timeout,       // 超时
    Unknown        // 未知错误
}

/// <summary>资源加载开始事件</summary>
public struct ResourceLoadStartedEvent : IEvent
{
    public string ResourcePath;      // 资源路径
    public LoadType LoadType;        // 加载类型（Sync/Async/Bundle）
    public string RequestId;         // 请求ID（用于跟踪）
}

/// <summary>资源加载进度更新事件</summary>
public struct ResourceLoadProgressEvent : IEvent
{
    public string ResourcePath;
    public float Progress;           // 0-1进度
    public string RequestId;
}

/// <summary>资源加载完成事件</summary>
public struct ResourceLoadCompletedEvent : IEvent
{
    public string ResourcePath;
    public bool FromCache;           // 是否来自缓存
    public string RequestId;
}

/// <summary>资源加载失败事件</summary>
public struct ResourceLoadFailedEvent : IEvent
{
    public string ResourcePath;
    public LoadErrorType ErrorType;  // 错误类型
    public string ErrorMessage;
    public string RequestId;
}

/// <summary>AssetBundle下载进度事件</summary>
public struct AssetBundleDownloadProgressEvent : IEvent
{
    public string BundleName;
    public long DownloadedBytes;     // 已下载字节
    public long TotalBytes;          // 总字节
    public float Progress;           // 0-1进度
}

/// <summary>内存警告事件（用于触发自动清理）</summary>
public struct MemoryWarningEvent : IEvent
{
    public long UsedBytes;           // 当前使用内存
    public long ThresholdBytes;      // 警告阈值
}

/// <summary>缓存命中事件（用于统计）</summary>
public struct CacheHitEvent : IEvent
{
    public string ResourcePath;
    public int CacheSize;            // 当前缓存大小
}

/// <summary>缓存淘汰事件</summary>
public struct CacheEvictedEvent : IEvent
{
    public string ResourcePath;
    public int RemainingCacheSize;   // 淘汰后剩余缓存大小
}

/// <summary>批量加载开始事件</summary>
public struct BatchLoadStartedEvent : IEvent
{
    public int TotalItems;           // 总加载项数
    public string BatchId;           // 批次ID
}

/// <summary>批量加载进度事件</summary>
public struct BatchLoadProgressEvent : IEvent
{
    public int CompletedItems;       // 已完成项数
    public int TotalItems;           // 总项数
    public float Progress;           // 总体进度
    public string BatchId;
}

/// <summary>批量加载完成事件</summary>
public struct BatchLoadCompletedEvent : IEvent
{
    public int TotalItems;           // 总加载项数
    public int FailedItems;          // 失败项数
    public string BatchId;
}