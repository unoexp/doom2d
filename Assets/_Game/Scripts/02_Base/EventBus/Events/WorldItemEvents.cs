// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/02_Base/EventBus/Events/WorldItemEvents.cs
// 世界物品相关事件
// ══════════════════════════════════════════════════════════════════════
using UnityEngine;

/// <summary>物品拾取请求事件（WorldItem 发布，InventorySystem 消费）</summary>
public struct ItemPickupRequestEvent : IEvent
{
    public string ItemId;
    public int Amount;
    public float Durability;
    public Vector3 WorldPosition;
}

/// <summary>物品掉落事件（LootSystem 生成 WorldItem 后发布）</summary>
public struct ItemDroppedEvent : IEvent
{
    public string ItemId;
    public int Amount;
    public Vector3 WorldPosition;
}
