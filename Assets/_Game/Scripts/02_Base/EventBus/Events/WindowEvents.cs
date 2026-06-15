// ─────────────────────────────────────────────────────────────────────
// 📁 Assets/_Game/02_Base/EventBus/Events/WindowEvents.cs
// 窗口生命周期相关事件（struct，零 GC）。
// WindowManager 在窗口打开/关闭/聚焦时发布这些事件。
// ─────────────────────────────────────────────────────────────────────

/// <summary>窗口已打开（含动画完成）</summary>
public struct WindowOpenedEvent : IEvent
{
    /// <summary>窗口唯一标识</summary>
    public string WindowId;
}

/// <summary>窗口已关闭（含动画完成，销毁前）</summary>
public struct WindowClosedEvent : IEvent
{
    /// <summary>窗口唯一标识</summary>
    public string WindowId;
}

/// <summary>窗口获得焦点（被提至最前）</summary>
public struct WindowFocusedEvent : IEvent
{
    /// <summary>窗口唯一标识</summary>
    public string WindowId;
}

/// <summary>所有窗口已关闭</summary>
public struct AllWindowsClosedEvent : IEvent { }
