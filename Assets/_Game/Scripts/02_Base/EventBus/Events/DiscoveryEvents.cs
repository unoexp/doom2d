// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/02_Base/EventBus/Events/DiscoveryEvents.cs
// 永久发现物事件定义
// ══════════════════════════════════════════════════════════════════════

/// <summary>发现物获取事件</summary>
public struct DiscoveryFoundEvent : IEvent
{
    public string DiscoveryId;
    public string DisplayName;
    public DiscoveryEffectType EffectType;
    public float EffectValue;
}
