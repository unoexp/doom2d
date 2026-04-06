// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/02_Base/EventBus/Events/TutorialEvents.cs
// 教学引导事件定义
// ══════════════════════════════════════════════════════════════════════

/// <summary>教学提示触发事件</summary>
public struct TutorialTriggerEvent : IEvent
{
    public TutorialTrigger TriggerType;
    public string Message;
}

/// <summary>教学提示关闭事件</summary>
public struct TutorialDismissedEvent : IEvent
{
    public TutorialTrigger TriggerType;
}
