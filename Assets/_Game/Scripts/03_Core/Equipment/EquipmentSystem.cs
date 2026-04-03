// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/03_Core/Equipment/EquipmentSystem.cs
// 装备系统。管理装备穿戴/卸下，计算总属性加成。
// ══════════════════════════════════════════════════════════════════════
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 装备管理系统。
///
/// 核心职责：
///   · 管理所有装备槽位的穿戴状态
///   · 穿戴/卸下时从背包消耗/归还物品
///   · 计算总防御力等属性加成
///   · 通过 EventBus 广播装备变化
///
/// 设计说明：
///   · 装备数据通过 IItemDataService 查询 ItemDefinitionSO
///   · 装备槽位使用 EquipmentSlot 枚举（07_Shared）
/// </summary>
public class EquipmentSystem : MonoBehaviour, ISaveable
{
    // ══════════════════════════════════════════════════════
    // 字段
    // ══════════════════════════════════════════════════════

    /// <summary>槽位 → 当前装备的物品ID（空字符串 = 未装备）</summary>
    private readonly Dictionary<EquipmentSlot, string> _equippedItems
        = new Dictionary<EquipmentSlot, string>();

    private IItemDataService _itemDataService;
    private IInventorySystem _inventorySystem;

    // ══════════════════════════════════════════════════════
    // 属性
    // ══════════════════════════════════════════════════════

    /// <summary>当前总防御力</summary>
    public float TotalDefense { get; private set; }

    // ══════════════════════════════════════════════════════
    // ISaveable
    // ══════════════════════════════════════════════════════

    public string SaveKey => nameof(EquipmentSystem);

    // ══════════════════════════════════════════════════════
    // 生命周期
    // ══════════════════════════════════════════════════════

    private void Awake()
    {
        ServiceLocator.Register<EquipmentSystem>(this);

        // 初始化所有槽位
        _equippedItems[EquipmentSlot.Head] = string.Empty;
        _equippedItems[EquipmentSlot.Body] = string.Empty;
        _equippedItems[EquipmentSlot.Legs] = string.Empty;
        _equippedItems[EquipmentSlot.Feet] = string.Empty;
        _equippedItems[EquipmentSlot.Hands] = string.Empty;
        _equippedItems[EquipmentSlot.Back] = string.Empty;
        _equippedItems[EquipmentSlot.MainHand] = string.Empty;
        _equippedItems[EquipmentSlot.OffHand] = string.Empty;
        _equippedItems[EquipmentSlot.Accessory_1] = string.Empty;
        _equippedItems[EquipmentSlot.Accessory_2] = string.Empty;
    }

    private void Start()
    {
        _itemDataService = ServiceLocator.Get<IItemDataService>();
        _inventorySystem = ServiceLocator.Get<IInventorySystem>();

        // 注册到存档系统
        if (ServiceLocator.TryGet<SaveLoadSystem>(out var saveSystem))
            saveSystem.Register(this);
    }

    private void OnDestroy()
    {
        if (ServiceLocator.TryGet<SaveLoadSystem>(out var saveSystem))
            saveSystem.Unregister(this);

        ServiceLocator.Unregister<EquipmentSystem>();
    }

    // ══════════════════════════════════════════════════════
    // 公有 API —— 穿戴/卸下
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 穿戴装备到指定槽位。如果该槽位已有装备，先卸下旧装备。
    /// </summary>
    /// <param name="slot">目标槽位</param>
    /// <param name="itemId">物品ID</param>
    /// <returns>是否成功穿戴</returns>
    public bool Equip(EquipmentSlot slot, string itemId)
    {
        if (slot == EquipmentSlot.None) return false;
        if (string.IsNullOrEmpty(itemId)) return false;

        // 先从背包移除物品（确认拥有该物品）
        if (!_inventorySystem.TryRemoveItem(itemId, 1))
            return false;

        // 卸下旧装备（会归还到背包）
        if (!string.IsNullOrEmpty(_equippedItems[slot]))
            Unequip(slot);

        _equippedItems[slot] = itemId;
        RecalculateStats();

        EventBus.Publish(new ItemEquippedEvent { Slot = slot, ItemId = itemId });
        return true;
    }

    /// <summary>
    /// 卸下指定槽位的装备。
    /// </summary>
    /// <param name="slot">目标槽位</param>
    /// <returns>卸下的物品ID（空字符串 = 该槽位无装备）</returns>
    public string Unequip(EquipmentSlot slot)
    {
        if (slot == EquipmentSlot.None) return string.Empty;
        if (!_equippedItems.TryGetValue(slot, out string itemId)) return string.Empty;
        if (string.IsNullOrEmpty(itemId)) return string.Empty;

        _equippedItems[slot] = string.Empty;
        RecalculateStats();

        // 归还物品到背包
        _inventorySystem.TryAddItem(itemId, 1);

        EventBus.Publish(new ItemUnequippedEvent { Slot = slot, PreviousItemId = itemId });
        return itemId;
    }

    // ══════════════════════════════════════════════════════
    // 公有 API —— 查询
    // ══════════════════════════════════════════════════════

    /// <summary>获取指定槽位当前装备的物品ID</summary>
    public string GetEquippedItemId(EquipmentSlot slot)
    {
        return _equippedItems.TryGetValue(slot, out string itemId) ? itemId : string.Empty;
    }

    /// <summary>指定槽位是否已装备</summary>
    public bool IsSlotEquipped(EquipmentSlot slot)
    {
        return _equippedItems.TryGetValue(slot, out string itemId) && !string.IsNullOrEmpty(itemId);
    }

    // ══════════════════════════════════════════════════════
    // 属性计算
    // ══════════════════════════════════════════════════════

    /// <summary>重新计算所有装备属性加成</summary>
    private void RecalculateStats()
    {
        float totalDef = 0f;

        foreach (var kv in _equippedItems)
        {
            if (string.IsNullOrEmpty(kv.Value)) continue;

            var itemDef = _itemDataService?.GetItemDefinition(kv.Value);
            if (itemDef is ArmorItemSO armor)
                totalDef += armor.Defense;
        }

        TotalDefense = totalDef;
        EventBus.Publish(new PlayerDefenseChangedEvent { TotalDefense = totalDef });
    }

    // ══════════════════════════════════════════════════════
    // ISaveable 实现
    // ══════════════════════════════════════════════════════

    public object CaptureState()
    {
        var data = new EquipmentSaveData();
        foreach (var kv in _equippedItems)
        {
            if (string.IsNullOrEmpty(kv.Value)) continue;
            data.Entries.Add(new EquipmentSaveEntry
            {
                Slot = kv.Key,
                ItemId = kv.Value
            });
        }
        return data;
    }

    public void RestoreState(object state)
    {
        EquipmentSaveData data;
        if (state is string json)
            data = JsonUtility.FromJson<EquipmentSaveData>(json);
        else if (state is EquipmentSaveData directData)
            data = directData;
        else
            return;

        // 清空所有槽位
        var slots = new List<EquipmentSlot>(_equippedItems.Keys);
        for (int i = 0; i < slots.Count; i++)
            _equippedItems[slots[i]] = string.Empty;

        // 恢复装备
        for (int i = 0; i < data.Entries.Count; i++)
        {
            var entry = data.Entries[i];
            _equippedItems[entry.Slot] = entry.ItemId;
        }

        RecalculateStats();
    }
}

/// <summary>装备存档数据</summary>
[System.Serializable]
public class EquipmentSaveData
{
    public System.Collections.Generic.List<EquipmentSaveEntry> Entries
        = new System.Collections.Generic.List<EquipmentSaveEntry>();
}

/// <summary>装备存档条目</summary>
[System.Serializable]
public struct EquipmentSaveEntry
{
    public EquipmentSlot Slot;
    public string ItemId;
}
