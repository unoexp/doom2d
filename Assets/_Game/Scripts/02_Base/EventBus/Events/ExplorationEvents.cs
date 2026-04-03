// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/02_Base/EventBus/Events/ExplorationEvents.cs
// 探索系统事件定义
// ══════════════════════════════════════════════════════════════════════

/// <summary>兴趣点发现事件</summary>
public struct POIDiscoveredEvent : IEvent
{
    public string POIId;
    public POIType POIType;
    public string DisplayName;
    public UnityEngine.Vector2 Position;
}

/// <summary>区域探索完成事件</summary>
public struct AreaExploredEvent : IEvent
{
    public string AreaId;
    public int LayerDepth;
}
