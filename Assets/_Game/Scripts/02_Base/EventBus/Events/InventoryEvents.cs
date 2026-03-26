// 📁 02_Base/EventBus/Events/InventoryEvents.cs
// ⚠️ 所有事件定义为结构体，零GC分配

using SurvivalGame.Data.Inventory;

/// <summary>物品被添加到背包</summary>
public struct ItemAddedToInventoryEvent : IEvent
{
    public string ItemId;       // 物品ID（对应ItemDefinitionSO）
    public int Amount;
    public int SlotIndex;       // 放入的槽位索引
    public string ContainerId;  // 容器ID（主背包或快捷栏）
}

/// <summary>物品被从背包移除</summary>
public struct ItemRemovedFromInventoryEvent : IEvent
{
    public string ItemId;
    public int Amount;
    public string ContainerId;
}

/// <summary>背包已满</summary>
public struct InventoryFullEvent : IEvent
{
    public string ContainerId;
}

/// <summary>物品在背包内移动</summary>
public struct ItemMovedInInventoryEvent : IEvent
{
    public string ContainerId;
    public int FromSlotIndex;
    public int ToSlotIndex;
    public ItemStack ItemStack;  // 移动的物品堆叠
}

/// <summary>背包内容改变</summary>
public struct InventoryChangedEvent : IEvent
{
    public string ContainerId;
}

/// <summary>快捷栏选中改变</summary>
public struct QuickSlotSelectedEvent : IEvent
{
    public string ContainerId;
    public int SelectedSlotIndex;
    public ItemStack SelectedItem;
}

/// <summary>物品使用事件</summary>
public struct ItemUsedEvent : IEvent
{
    public string ItemId;
    public int AmountUsed;
    public int SlotIndex;
    public string ContainerId;
}

/// <summary>背包重量更新</summary>
public struct InventoryWeightUpdatedEvent : IEvent
{
    public string ContainerId;
    public float CurrentWeight;
    public float MaxWeight;
}

/// <summary>背包排序完成</summary>
public struct InventorySortedEvent : IEvent
{
    public string ContainerId;
    public SortType SortType;
}

/// <summary>背包过滤改变</summary>
public struct InventoryFilterChangedEvent : IEvent
{
    public string ContainerId;
    public string FilterCategory;
}

/// <summary>背包容器创建</summary>
public struct InventoryContainerCreatedEvent : IEvent
{
    public string ContainerId;
    public int Capacity;
    public SlotType[] SlotTypes;
}

/// <summary>背包容器销毁</summary>
public struct InventoryContainerDestroyedEvent : IEvent
{
    public string ContainerId;
}

/// <summary>物品耐久度变化</summary>
public struct ItemDurabilityChangedEvent : IEvent
{
    public string ContainerId;
    public int SlotIndex;
    public string ItemId;
    public float OldDurability;
    public float NewDurability;
    public float DurabilityPercentage; // 0-1范围
}

/// <summary>物品损坏</summary>
public struct ItemBrokenEvent : IEvent
{
    public string ContainerId;
    public int SlotIndex;
    public string ItemId;
    public ItemStack BrokenItemStack;
}

/// <summary>背包容量扩展</summary>
public struct InventoryCapacityExpandedEvent : IEvent
{
    public string ContainerId;
    public int OldCapacity;
    public int NewCapacity;
}

// 排序类型枚举
public enum SortType
{
    ByName,
    ByQuantity,
    ByWeight,
    ByType,
    ByRarity
}

// ── 玩家状态事件（从 05_Show 移至此处：业务域事件，低层将来可能发布）──

/// <summary>玩家负重变化事件</summary>
public struct PlayerWeightChangedEvent : IEvent
{
    public float CurrentWeight;
    public float MaxWeight;
    public bool IsOverweight;
}

/// <summary>玩家金币变化事件</summary>
public struct PlayerGoldChangedEvent : IEvent
{
    public int CurrentGold;
    public int Delta; // 正数为增加，负数为减少
}