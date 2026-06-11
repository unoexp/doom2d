// ─────────────────────────────────────────────────────────────────────
// 📁 Assets/_Game/02_Base/EventBus/Events/DialogEvents.cs
// 对话和NPC交互事件定义
// ─────────────────────────────────────────────────────────────────────

/// <summary>对话开始事件</summary>
public struct DialogStartedEvent : IEvent
{
    public string DialogId;
    public string NPCId;
}

/// <summary>对话节点推进事件（UI更新对话内容）</summary>
public struct DialogNodeAdvancedEvent : IEvent
{
    public string SpeakerName;
    public string Content;
    public int NodeIndex;
    public bool HasChoices;
}

/// <summary>对话结束事件</summary>
public struct DialogEndedEvent : IEvent
{
    public string DialogId;
    public string NPCId;
}

