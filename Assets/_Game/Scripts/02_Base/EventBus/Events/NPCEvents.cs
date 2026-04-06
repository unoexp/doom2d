// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/02_Base/EventBus/Events/NPCEvents.cs
// NPC 关系事件定义
// ══════════════════════════════════════════════════════════════════════

/// <summary>NPC 信任度变化事件</summary>
public struct NPCTrustChangedEvent : IEvent
{
    public string NPCId;
    public int OldTrust;
    public int NewTrust;
    public int MaxTrust;
}

/// <summary>NPC 交互事件</summary>
public struct NPCInteractionEvent : IEvent
{
    public string NPCId;
    public InteractionType InteractionType;
}

/// <summary>NPC 信任度阈值达成事件</summary>
public struct NPCTrustThresholdReachedEvent : IEvent
{
    public string NPCId;
    public int ThresholdLevel;
    public string UnlockQuestId;
    public string UnlockDialogId;
}
