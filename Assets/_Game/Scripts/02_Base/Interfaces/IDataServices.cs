// 📁 02_Base/Interfaces/IDataServices.cs
// 所有 JSON 数据服务的接口定义（集中管理）。
// 🏗️ 定义在 02_Base 层，03_Core 的对应 JsonDataService 实现。

/// <summary>资源缓存配置数据服务接口（单例配置）</summary>
public interface IResourceCacheConfigDataService
{
    ResourceCacheConfigData GetConfig();
}

/// <summary>音频目录数据服务接口</summary>
public interface IAudioCatalogDataService
{
    AudioCatalogData GetCatalog();
    System.Collections.Generic.IReadOnlyList<AudioEntryData> GetAllEntries();
}
