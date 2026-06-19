// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/02_Base/EventBus/Events/SceneEvents.cs
// 场景加载/卸载事件定义。全部为 struct，零 GC 分配。
// ══════════════════════════════════════════════════════════════════════

/// <summary>
/// 场景加载开始事件。在单个场景开始异步加载时发布。
/// </summary>
public struct SceneLoadStartedEvent : IEvent
{
    /// <summary>场景名称</summary>
    public string SceneName;
}

/// <summary>
/// 场景加载进度事件。在异步加载过程中每帧发布。
/// </summary>
public struct SceneLoadProgressEvent : IEvent
{
    /// <summary>场景名称</summary>
    public string SceneName;

    /// <summary>加载进度（0-1）</summary>
    public float Progress;
}

/// <summary>
/// 场景加载完成事件。在单个场景加载并激活后发布。
/// </summary>
public struct SceneLoadCompletedEvent : IEvent
{
    /// <summary>场景名称</summary>
    public string SceneName;
}

/// <summary>
/// 场景卸载开始事件。在单个场景开始异步卸载时发布。
/// </summary>
public struct SceneUnloadStartedEvent : IEvent
{
    /// <summary>场景名称</summary>
    public string SceneName;
}

/// <summary>
/// 场景卸载完成事件。在单个场景卸载完成后发布。
/// </summary>
public struct SceneUnloadCompletedEvent : IEvent
{
    /// <summary>场景名称</summary>
    public string SceneName;
}

/// <summary>
/// 场景切换开始事件。在 SwitchToScene 流程开始时发布，
/// 早于 LoadingStartedEvent，供需要预处理的系统使用。
/// </summary>
public struct SceneSwitchStartedEvent : IEvent
{
    /// <summary>来源场景（可能为 null 表示首次进入）</summary>
    public string FromScene;

    /// <summary>目标场景</summary>
    public string ToScene;
}
