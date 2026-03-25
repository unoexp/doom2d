// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/02_Infrastructure/ResourceManager/Interfaces/IAssetBundleLoader.cs
// AssetBundle加载器接口定义
// ══════════════════════════════════════════════════════════════════════
using UnityEngine;

/// <summary>
/// AssetBundle加载器接口，支持本地和远程Bundle加载
/// </summary>
public interface IAssetBundleLoader
{
    /// <summary>
    /// 加载AssetBundle（同步）
    /// </summary>
    /// <param name="bundlePath">Bundle路径（本地文件路径或远程URL）</param>
    /// <param name="crc">可选CRC校验值</param>
    /// <returns>加载的AssetBundle，失败抛出异常</returns>
    AssetBundle LoadBundle(string bundlePath, uint crc = 0);

    /// <summary>
    /// 异步加载AssetBundle
    /// </summary>
    /// <param name="bundlePath">Bundle路径</param>
    /// <param name="crc">可选CRC校验值</param>
    /// <returns>加载操作对象</returns>
    LoadOperation<AssetBundle> LoadBundleAsync(string bundlePath, uint crc = 0);

    /// <summary>
    /// 从已加载的Bundle中加载资源
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="bundleName">Bundle名称</param>
    /// <param name="assetPath">资源在Bundle中的路径</param>
    /// <returns>加载的资源</returns>
    T LoadFromBundle<T>(string bundleName, string assetPath) where T : Object;

    /// <summary>
    /// 异步从Bundle中加载资源
    /// </summary>
    T LoadFromBundleAsync<T>(string bundleName, string assetPath) where T : Object;

    /// <summary>
    /// 卸载AssetBundle
    /// </summary>
    /// <param name="bundleName">Bundle名称</param>
    /// <param name="unloadAllLoadedObjects">是否卸载所有已加载的资源对象</param>
    void UnloadBundle(string bundleName, bool unloadAllLoadedObjects = false);

    /// <summary>
    /// 检查Bundle是否已加载
    /// </summary>
    bool IsBundleLoaded(string bundleName);

    /// <summary>
    /// 获取已加载Bundle的信息
    /// </summary>
    BundleInfo GetBundleInfo(string bundleName);

    /// <summary>
    /// 下载远程Bundle到本地缓存
    /// </summary>
    /// <param name="remoteUrl">远程URL</param>
    /// <param name="localBundleName">本地缓存名称</param>
    /// <param name="hash">校验哈希</param>
    /// <returns>下载操作</returns>
    LoadOperation<string> DownloadBundle(string remoteUrl, string localBundleName, string hash = null);

    /// <summary>
    /// 检查远程Bundle更新
    /// </summary>
    /// <param name="bundleName">Bundle名称</param>
    /// <param name="remoteHash">远程哈希值</param>
    /// <returns>是否需要更新</returns>
    bool CheckForUpdate(string bundleName, string remoteHash);

    /// <summary>
    /// 清理Bundle缓存
    /// </summary>
    /// <param name="bundleName">指定Bundle名称，null表示清理所有</param>
    void ClearBundleCache(string bundleName = null);
}

/// <summary>
/// Bundle信息
/// </summary>
public struct BundleInfo
{
    public string Name;              // Bundle名称
    public bool IsLoaded;            // 是否已加载
    public bool IsRemote;            // 是否远程Bundle
    public string Path;              // 完整路径
    public long Size;                // 文件大小（字节）
    public string Hash;              // 哈希值
    public int AssetCount;           // 包含的资源数量
    public string[] Dependencies;    // 依赖的Bundle列表
}

/// <summary>
/// AssetBundle加载操作（特殊化）
/// </summary>
public class BundleLoadOperation : LoadOperation<AssetBundle>
{
    public BundleLoadOperation(string bundlePath, string requestId = null)
        : base(bundlePath, requestId)
    {
    }

    // Bundle特定属性
    public bool IsRemote { get; internal set; }
    public long DownloadSize { get; internal set; }
    public long DownloadedBytes { get; internal set; }
}

/// <summary>
/// 远程下载操作
/// </summary>
public class DownloadOperation : LoadOperation<string>
{
    public DownloadOperation(string url, string requestId = null)
        : base(url, requestId)
    {
    }

    // 下载特定属性
    public string LocalPath { get; internal set; }
    public long TotalBytes { get; internal set; }
    public long DownloadedBytes { get; internal set; }
    public string Hash { get; internal set; }
}