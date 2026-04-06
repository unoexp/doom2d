// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/02_Base/EventBus/Events/AchievementEvents.cs
// 成就系统事件定义
// ══════════════════════════════════════════════════════════════════════

/// <summary>成就解锁事件</summary>
public struct AchievementUnlockedEvent : IEvent
{
    public string AchievementId;
    public string DisplayName;
    public string Description;
}
