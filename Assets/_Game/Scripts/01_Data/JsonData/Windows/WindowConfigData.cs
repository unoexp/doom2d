// 📁 01_Data/JsonData/Windows/WindowConfigData.cs
// 窗口配置数据模型。对应 windows.json 中的窗口配置条目。
// WindowManager 在 Initialize 时通过 IWindowConfigDataService 读取此数据构建配置映射。

using Newtonsoft.Json;

/// <summary>
/// 窗口配置条目（JSON 反序列化）。
/// 每个条目描述一个可通过 WindowManager 打开的窗口。
/// </summary>
[System.Serializable]
public struct WindowConfigEntryData
{
    /// <summary>窗口唯一标识，如 "InventoryWindow"</summary>
    [JsonProperty("windowId")]
    public string WindowId;

    /// <summary>窗口类名，用于实例化后校验组件类型</summary>
    [JsonProperty("windowClass")]
    public string WindowClass;

    /// <summary>预制体 Resources 路径，如 "UI/Windows/InventoryWindow"</summary>
    [JsonProperty("prefabPath")]
    public string PrefabPath;

    /// <summary>打开动画类型字符串（对应 WindowAnimationType 枚举名称）</summary>
    [JsonProperty("openAnimType")]
    public string OpenAnimType;

    /// <summary>打开动画时长（秒）</summary>
    [JsonProperty("openAnimDuration")]
    public float OpenAnimDuration;

    /// <summary>关闭动画类型字符串（对应 WindowAnimationType 枚举名称）</summary>
    [JsonProperty("closeAnimType")]
    public string CloseAnimType;

    /// <summary>关闭动画时长（秒）</summary>
    [JsonProperty("closeAnimDuration")]
    public float CloseAnimDuration;

    /// <summary>默认 Canvas sortingOrder（窗口渲染层级）</summary>
    [JsonProperty("defaultSortingOrder")]
    public int DefaultSortingOrder;

    /// <summary>点击时是否自动提到最前</summary>
    [JsonProperty("bringToFrontOnFocus")]
    public bool BringToFrontOnFocus;
}
