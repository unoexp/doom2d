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

/// <summary>特效目录数据服务接口</summary>
public interface IVFXCataLogDataService
{
    VFXCatalogData GetCatalog();
    System.Collections.Generic.IReadOnlyList<VFXEntryData> GetAllEntries();
}

/// <summary>窗口配置数据服务接口</summary>
public interface IWindowConfigDataService
{
    System.Collections.Generic.IReadOnlyList<WindowConfigEntryData> GetAllEntries();
    WindowConfigEntryData GetByWindowId(string windowId);
    bool TryGetByWindowId(string windowId, out WindowConfigEntryData data);
}

/// <summary>物品数据服务接口</summary>
public interface IItemDataService
{
    System.Collections.Generic.IReadOnlyList<ItemData> GetAllItems();
    ItemData GetByItemId(int itemId);
    bool TryGetByItemId(int itemId, out ItemData data);
}
