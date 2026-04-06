// 📁 05_Show/Inventory/Events/InventoryUIEvents.cs
// ⚠️ 所有UI事件定义为结构体，零GC分配

using UnityEngine;
using SurvivalGame.Data.Inventory;

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

// 快捷栏选择请求事件（UI交互发出，区别于业务层的QuickSlotSelectedEvent）
public struct QuickSlotSelectRequestEvent : IEvent
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

/// <summary>
/// 物品Tooltip悬停请求事件。UI槽位在鼠标悬停时发布。
/// </summary>
public struct ItemTooltipRequestEvent : IEvent
{
    /// <summary>物品ID（空字符串表示隐藏Tooltip）</summary>
    public string ItemId;

    /// <summary>当前堆叠数量</summary>
    public int StackSize;

    /// <summary>当前耐久度（无耐久度时为 -1）</summary>
    public float CurrentDurability;
}

// 背包过滤请求事件（UI交互发出，区别于业务层的InventoryFilterChangedEvent）
public struct InventoryFilterRequestEvent : IEvent
{
    public string FilterCategory;
}

// 排序方式改变事件
public struct InventorySortChangedEvent : IEvent
{
    public string SortMethod; // "Name", "Type", "Quantity", "Weight"
}

// ── 纯 UI 反馈事件（从 InventoryPresenter.cs 移至此处）──

public enum UIFeedbackType
{
    ItemAdded,
    ItemRemoved,
    DragStart,
    DragEnd,
    InventoryOpen,
    InventoryClose,
    ItemMove
}

/// <summary>UI 槽位操作反馈事件</summary>
public struct UIFeedbackEvent : IEvent
{
    public UIFeedbackType Type;
    public int SlotIndex;
}

/// <summary>UI 消息通知事件</summary>
public struct UINotificationEvent : IEvent
{
    public string Message;
    public float Duration;
}

/// <summary>UI 右键菜单事件</summary>
public struct UIContextMenuEvent : IEvent
{
    public int SlotIndex;
    public SlotType SlotType;
    public string ItemId;
    public UnityEngine.Vector2 ScreenPosition;
}

/// <summary>背包筛选应用事件</summary>
public struct UIFilterAppliedEvent : IEvent
{
    public string FilterCategory;
}

/// <summary>背包排序应用事件</summary>
public struct UISortAppliedEvent : IEvent
{
    public string SortMethod;
}