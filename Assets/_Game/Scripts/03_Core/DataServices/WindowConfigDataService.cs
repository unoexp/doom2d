// 📁 03_Core/DataServices/WindowConfigDataService.cs
// 窗口配置数据服务。从 windows.json 加载窗口配置条目。
// 窗口配置为数组格式 [{...}, {...}]，使用泛型基类的数组加载逻辑。

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.IO;

/// <summary>
/// 窗口配置数据服务。
/// windows.json 是一个包含窗口配置条目的数组，
/// 继承 JsonDataService&lt;WindowConfigEntryData&gt; 直接使用数组反序列化逻辑。
/// </summary>
public class WindowConfigDataService : JsonDataService<WindowConfigEntryData>, IWindowConfigDataService
{
    public override string DataFileName => "windows.json";

    private void Awake()
    {
        ServiceLocator.Register<IWindowConfigDataService>(this);
    }

    protected override string GetIdFromItem(WindowConfigEntryData item) => item.WindowId;

    /// <summary>获取全部窗口配置条目</summary>
    public System.Collections.Generic.IReadOnlyList<WindowConfigEntryData> GetAllEntries()
    {
        return GetAll();
    }

    /// <summary>通过窗口ID获取配置</summary>
    public WindowConfigEntryData GetByWindowId(string windowId)
    {
        return GetById(windowId);
    }

    /// <summary>安全获取窗口配置（不抛异常）</summary>
    public bool TryGetByWindowId(string windowId, out WindowConfigEntryData data)
    {
        return TryGetById(windowId, out data);
    }

    public override void Shutdown()
    {
        ServiceLocator.Unregister<IWindowConfigDataService>();
        base.Shutdown();
    }
}
