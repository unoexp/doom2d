// 📁 05_Show/Inventory/Events/InventoryUIEvents.cs
// ⚠️ 所有UI事件定义为结构体，零GC分配

using UnityEngine;

/// <summary>
/// 背包UI事件定义
/// 🏗️ 所有UI交互事件在此定义
/// 🚫 业务逻辑事件定义在02_Base/EventBus/Events/InventoryEvents.cs
/// </summary>

// 槽位点击事件
public struct SlotClickedEvent : IEvent
{
    public int SlotIndex;
    public bool IsRightClick;  // true=右键，false=左键
    public Vector2 ScreenPosition;
}

// 拖拽开始事件
public struct SlotDragStartedEvent : IEvent
{
    public int SlotIndex;
    public string ItemId;
    public int ItemAmount;
}

// 拖拽结束事件
public struct SlotDragEndedEvent : IEvent
{
    public int SourceSlotIndex;
    public int TargetSlotIndex; // -1表示无效目标（拖到界面外）
    public Vector2 DropPosition;
}

// 快捷栏选择事件
public struct QuickSlotSelectedEvent : IEvent
{
    public int QuickSlotIndex;
    public string ItemId;
}

// 背包开关事件
public struct InventoryToggleEvent : IEvent
{
    public bool IsOpening;
}

// 物品提示显示事件
public struct ItemTooltipShowEvent : IEvent
{
    public string ItemId;
    public Vector2 ScreenPosition;
    public bool ShowImmediately;
}

// 物品提示隐藏事件
public struct ItemTooltipHideEvent : IEvent { }

// 背包过滤事件
public struct InventoryFilterChangedEvent : IEvent
{
    public string FilterCategory;
}

// 排序方式改变事件
public struct InventorySortChangedEvent : IEvent
{
    public string SortMethod; // "Name", "Type", "Quantity", "Weight"
}