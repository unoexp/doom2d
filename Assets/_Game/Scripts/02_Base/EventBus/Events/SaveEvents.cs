// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/02_Base/EventBus/Events/SaveEvents.cs
// 存档系统事件定义
// ══════════════════════════════════════════════════════════════════════

/// <summary>存档开始事件</summary>
public struct SaveStartedEvent : IEvent
{
    public int SlotIndex;
}

/// <summary>存档完成事件</summary>
public struct SaveCompletedEvent : IEvent
{
    public int SlotIndex;
    public bool Success;
}

/// <summary>读档开始事件</summary>
public struct LoadStartedEvent : IEvent
{
    public int SlotIndex;
}

/// <summary>读档完成事件</summary>
public struct LoadCompletedEvent : IEvent
{
    public int SlotIndex;
    public bool Success;
}

/// <summary>读档请求事件（UI发起，加载最近存档）</summary>
public struct LoadGameRequestEvent : IEvent { }
