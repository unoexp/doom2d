// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/02_Infrastructure/ResourceManager/Interfaces/IResourceLoader.cs
// 资源加载器接口定义
// ══════════════════════════════════════════════════════════════════════
using UnityEngine;

/// <summary>
/// 资源加载器接口，提供同步和异步加载Resources资源的API
/// </summary>
public interface IResourceLoader
{
    /// <summary>
    /// 同步加载资源（Resources.Load包装）
    /// </summary>
    /// <typeparam name="T">资源类型（GameObject, Sprite, Material等）</typeparam>
    /// <param name="path">Resources目录下的相对路径（不带扩展名）</param>
    /// <returns>加载的资源，如果不存在则抛出ResourceLoadException</returns>
    T Load<T>(string path) where T : UnityEngine.Object;

    /// <summary>
    /// 异步加载资源
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="path">资源路径</param>
    /// <returns>加载操作对象，可用于协程等待或进度跟踪</returns>
    LoadOperation<T> LoadAsync<T>(string path) where T : UnityEngine.Object;

    /// <summary>
    /// 预加载资源到缓存（不立即使用）
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="path">资源路径</param>
    /// <param name="priority">加载优先级（0-100，越高越优先）</param>
    void Preload<T>(string path, int priority = 50) where T : UnityEngine.Object;

    /// <summary>
    /// 卸载资源（从缓存中移除，如果无其他引用则真正卸载）
    /// </summary>
    /// <param name="path">资源路径</param>
    /// <param name="forceUnload">是否强制卸载（即使有引用）</param>
    void Unload(string path, bool forceUnload = false);

    /// <summary>
    /// 清理缓存（释放未使用的资源）
    /// </summary>
    /// <param name="force">是否强制清理所有缓存</param>
    /// <returns>清理的资源数量</returns>
    int ClearCache(bool force = false);

    /// <summary>
    /// 检查资源是否已缓存
    /// </summary>
    bool IsCached(string path);

    /// <summary>
    /// 获取缓存统计信息
    /// </summary>
    /// <returns>缓存项数，内存使用量等</returns>
    CacheStats GetCacheStats();
}

/// <summary>
/// 缓存统计信息
/// </summary>
public struct CacheStats
{
    public int ItemCount;          // 缓存项数
    public long MemoryUsageBytes;  // 内存使用字节数
    public int HitCount;           // 命中次数
    public int MissCount;          // 未命中次数
    public float HitRate => HitCount + MissCount > 0 ?
        (float)HitCount / (HitCount + MissCount) : 0f;
}