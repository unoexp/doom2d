// 📁 05_Show/Inventory/ViewModels/InventoryViewModel.cs
// ⚠️ 纯C#类，无Unity依赖

using System;
using System.Collections.Generic;

/// <summary>
/// 背包ViewModel，封装UI显示所需数据
/// 🏗️ 职责：持有UI状态数据，提供数据绑定接口
/// 🚫 禁止包含Unity依赖，纯C#类
/// </summary>
public class InventoryViewModel
{
    // 槽位数据
    private readonly List<SlotViewModel> _slots = new List<SlotViewModel>();
    private readonly List<SlotViewModel> _quickSlots = new List<SlotViewModel>();

    // UI状态
    private bool _isInventoryOpen = false;
    private int _draggingSlotIndex = -1;
    private string _currentFilter = "All";
    private string _currentSortMethod = "Default";

    // 背包统计
    private int _totalWeight = 0;
    private int _maxWeight = 100;
    private int _goldAmount = 0;

    // 事件
    public event Action<int> OnSlotUpdated;
    public event Action<int> OnQuickSlotUpdated;
    public event Action<bool> OnInventoryVisibilityChanged;
    public event Action<string> OnFilterChanged;
    public event Action<string> OnSortChanged;
    public event Action<int, int> OnWeightChanged;
    public event Action<int> OnGoldChanged;

    // 动态槽位相关事件
    public event Action<int> OnSlotsAdded;           // 添加了槽位（参数：添加的数量）
    public event Action<int> OnSlotsRemoved;         // 移除了槽位（参数：移除的数量）
    public event Action<int, int> OnSlotsCountChanged; // 槽位数量变化（参数：旧数量，新数量）
    public event Action<int> OnQuickSlotsAdded;      // 添加了快捷栏槽位
    public event Action<int> OnQuickSlotsRemoved;    // 移除了快捷栏槽位

    // 配置
    public int TotalSlots => _slots.Count;
    public int QuickSlots => _quickSlots.Count;

    // 动态槽位支持
    private int _initialSlots = 24; // 默认槽位
    private int _initialQuickSlots = 10; // 默认快捷栏槽位

    // 属性
    public bool IsInventoryOpen
    {
        get => _isInventoryOpen;
        set
        {
            if (_isInventoryOpen != value)
            {
                _isInventoryOpen = value;
                OnInventoryVisibilityChanged?.Invoke(value);
            }
        }
    }

    public int DraggingSlotIndex
    {
        get => _draggingSlotIndex;
        set => _draggingSlotIndex = value;
    }

    public string CurrentFilter
    {
        get => _currentFilter;
        set
        {
            if (_currentFilter != value)
            {
                _currentFilter = value;
                OnFilterChanged?.Invoke(value);
            }
        }
    }

    public string CurrentSortMethod
    {
        get => _currentSortMethod;
        set
        {
            if (_currentSortMethod != value)
            {
                _currentSortMethod = value;
                OnSortChanged?.Invoke(value);
            }
        }
    }

    public int TotalWeight
    {
        get => _totalWeight;
        set
        {
            if (_totalWeight != value)
            {
                var oldValue = _totalWeight;
                _totalWeight = value;
                OnWeightChanged?.Invoke(oldValue, value);
            }
        }
    }

    public int MaxWeight => _maxWeight;
    public float WeightPercentage => (float)_totalWeight / _maxWeight;
    public bool IsOverweight => _totalWeight > _maxWeight;

    public int GoldAmount
    {
        get => _goldAmount;
        set
        {
            if (_goldAmount != value)
            {
                _goldAmount = value;
                OnGoldChanged?.Invoke(value);
            }
        }
    }

    public InventoryViewModel()
        : this(24, 10) // 默认24主槽位，10快捷栏槽位
    {
    }

    public InventoryViewModel(int initialSlots, int initialQuickSlots)
    {
        _initialSlots = initialSlots;
        _initialQuickSlots = initialQuickSlots;

        // 初始化主槽位
        for (int i = 0; i < _initialSlots; i++)
        {
            _slots.Add(new SlotViewModel { SlotIndex = i, SlotType = SlotType.Inventory });
        }

        // 初始化快捷栏槽位
        for (int i = 0; i < _initialQuickSlots; i++)
        {
            _quickSlots.Add(new SlotViewModel
            {
                SlotIndex = i,
                SlotType = SlotType.QuickSlot,
                Keybind = (i + 1).ToString() // 快捷键1-0
            });
        }
    }

    /// <summary>更新主槽位物品</summary>
    public void UpdateSlot(int slotIndex, string itemId, int amount)
    {
        if (slotIndex >= 0 && slotIndex < _slots.Count)
        {
            _slots[slotIndex].UpdateItem(itemId, amount);
            OnSlotUpdated?.Invoke(slotIndex);
        }
    }

    /// <summary>更新快捷栏槽位物品</summary>
    public void UpdateQuickSlot(int slotIndex, string itemId, int amount)
    {
        if (slotIndex >= 0 && slotIndex < _quickSlots.Count)
        {
            _quickSlots[slotIndex].UpdateItem(itemId, amount);
            OnQuickSlotUpdated?.Invoke(slotIndex);
        }
    }

    /// <summary>清除槽位</summary>
    public void ClearSlot(int slotIndex, SlotType slotType)
    {
        if (slotType == SlotType.Inventory && slotIndex >= 0 && slotIndex < _slots.Count)
        {
            _slots[slotIndex].Clear();
            OnSlotUpdated?.Invoke(slotIndex);
        }
        else if (slotType == SlotType.QuickSlot && slotIndex >= 0 && slotIndex < _quickSlots.Count)
        {
            _quickSlots[slotIndex].Clear();
            OnQuickSlotUpdated?.Invoke(slotIndex);
        }
    }

    /// <summary>获取主槽位ViewModel</summary>
    public SlotViewModel GetSlot(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < _slots.Count
            ? _slots[slotIndex]
            : null;
    }

    /// <summary>获取快捷栏槽位ViewModel</summary>
    public SlotViewModel GetQuickSlot(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < _quickSlots.Count
            ? _quickSlots[slotIndex]
            : null;
    }

    /// <summary>获取所有非空槽位</summary>
    public List<SlotViewModel> GetOccupiedSlots()
    {
        var occupied = new List<SlotViewModel>();
        foreach (var slot in _slots)
        {
            if (!slot.IsEmpty) occupied.Add(slot);
        }
        return occupied;
    }

    /// <summary>根据过滤条件获取槽位</summary>
    public List<SlotViewModel> GetFilteredSlots(string categoryFilter)
    {
        if (categoryFilter == "All") return GetOccupiedSlots();

        var filtered = new List<SlotViewModel>();
        // 这里需要访问ItemDefinitionSO来获取分类信息
        // 暂时返回所有槽位
        return GetOccupiedSlots();
    }

    /// <summary>设置最大负重</summary>
    public void SetMaxWeight(int maxWeight)
    {
        _maxWeight = maxWeight;
        OnWeightChanged?.Invoke(_totalWeight, _totalWeight);
    }

    /// <summary>重置所有槽位</summary>
    public void ResetAllSlots()
    {
        foreach (var slot in _slots)
        {
            slot.Clear();
        }
        foreach (var slot in _quickSlots)
        {
            slot.Clear();
        }

        // 触发所有槽位更新事件
        for (int i = 0; i < _slots.Count; i++)
        {
            OnSlotUpdated?.Invoke(i);
        }
        for (int i = 0; i < _quickSlots.Count; i++)
        {
            OnQuickSlotUpdated?.Invoke(i);
        }
    }

    /// <summary>为主背包添加新槽位</summary>
    public void AddSlots(int count, bool clearExistingData = false)
    {
        if (count <= 0) return;

        var oldCount = _slots.Count;

        for (int i = 0; i < count; i++)
        {
            int slotIndex = _slots.Count;
            _slots.Add(new SlotViewModel { SlotIndex = slotIndex, SlotType = SlotType.Inventory });

            if (clearExistingData)
            {
                // 如果清除现有数据，直接触发更新事件
                OnSlotUpdated?.Invoke(slotIndex);
            }
        }

        // 触发槽位添加事件
        OnSlotsAdded?.Invoke(count);
        OnSlotsCountChanged?.Invoke(oldCount, _slots.Count);
    }

    /// <summary>为主背包移除槽位</summary>
    public bool RemoveSlots(int count, bool forceRemove = false)
    {
        if (count <= 0 || count > _slots.Count) return false;

        // 检查要移除的槽位是否为空（如果非强制移除）
        if (!forceRemove)
        {
            int startIndex = _slots.Count - count;
            for (int i = startIndex; i < _slots.Count; i++)
            {
                if (!_slots[i].IsEmpty)
                    return false; // 有物品，不能移除
            }
        }

        var oldCount = _slots.Count;

        // 移除最后count个槽位
        _slots.RemoveRange(_slots.Count - count, count);

        // 触发槽位移除事件
        OnSlotsRemoved?.Invoke(count);
        OnSlotsCountChanged?.Invoke(oldCount, _slots.Count);

        return true;
    }

    /// <summary>为快捷栏添加新槽位</summary>
    public void AddQuickSlots(int count)
    {
        if (count <= 0) return;

        var oldCount = _quickSlots.Count;

        for (int i = 0; i < count; i++)
        {
            int slotIndex = _quickSlots.Count;
            _quickSlots.Add(new SlotViewModel
            {
                SlotIndex = slotIndex,
                SlotType = SlotType.QuickSlot,
                Keybind = (slotIndex + 1).ToString() // 继续原有键位编号
            });
        }

        // 触发快捷栏槽位添加事件
        OnQuickSlotsAdded?.Invoke(count);
    }

    /// <summary>为快捷栏移除槽位</summary>
    public bool RemoveQuickSlots(int count, bool forceRemove = false)
    {
        if (count <= 0 || count > _quickSlots.Count) return false;

        // 检查要移除的槽位是否为空（如果非强制移除）
        if (!forceRemove)
        {
            int startIndex = _quickSlots.Count - count;
            for (int i = startIndex; i < _quickSlots.Count; i++)
            {
                if (!_quickSlots[i].IsEmpty)
                    return false; // 有物品，不能移除
            }
        }

        var oldCount = _quickSlots.Count;

        // 移除最后count个槽位
        _quickSlots.RemoveRange(_quickSlots.Count - count, count);

        // 触发快捷栏槽位移除事件
        OnQuickSlotsRemoved?.Invoke(count);

        return true;
    }

    /// <summary>设置主背包总槽位数</summary>
    public bool SetTotalSlots(int targetCount, bool forceResize = false)
    {
        if (targetCount < 0) return false;

        int currentCount = _slots.Count;

        if (targetCount == currentCount) return true;

        if (targetCount > currentCount)
        {
            // 添加槽位
            int toAdd = targetCount - currentCount;
            AddSlots(toAdd);
            return true;
        }
        else
        {
            // 移除槽位
            int toRemove = currentCount - targetCount;
            return RemoveSlots(toRemove, forceResize);
        }
    }

    /// <summary>设置快捷栏总槽位数</summary>
    public bool SetQuickSlots(int targetCount, bool forceResize = false)
    {
        if (targetCount < 0) return false;

        int currentCount = _quickSlots.Count;

        if (targetCount == currentCount) return true;

        if (targetCount > currentCount)
        {
            // 添加槽位
            int toAdd = targetCount - currentCount;
            AddQuickSlots(toAdd);
            return true;
        }
        else
        {
            // 移除槽位
            int toRemove = currentCount - targetCount;
            return RemoveQuickSlots(toRemove, forceResize);
        }
    }

    /// <summary>获取当前容量信息（用于扩展系统）</summary>
    public CapacityInfo GetCapacityInfo()
    {
        return new CapacityInfo
        {
            MainSlotsCount = _slots.Count,
            QuickSlotsCount = _quickSlots.Count,
            OccupiedMainSlots = GetOccupiedSlots().Count,
            OccupiedQuickSlots = GetOccupiedQuickSlots().Count,
            TotalWeight = _totalWeight,
            MaxWeight = _maxWeight
        };
    }

    /// <summary>获取所有占用的快捷栏槽位</summary>
    private List<SlotViewModel> GetOccupiedQuickSlots()
    {
        var occupied = new List<SlotViewModel>();
        foreach (var slot in _quickSlots)
        {
            if (!slot.IsEmpty) occupied.Add(slot);
        }
        return occupied;
    }

    /// <summary>重置到初始槽位配置</summary>
    public void ResetToInitialCapacity()
    {
        // 移除所有槽位（强制移除，清除数据）
        RemoveSlots(_slots.Count, true);
        RemoveQuickSlots(_quickSlots.Count, true);

        // 重新初始化到初始配置
        for (int i = 0; i < _initialSlots; i++)
        {
            _slots.Add(new SlotViewModel { SlotIndex = i, SlotType = SlotType.Inventory });
        }

        for (int i = 0; i < _initialQuickSlots; i++)
        {
            _quickSlots.Add(new SlotViewModel
            {
                SlotIndex = i,
                SlotType = SlotType.QuickSlot,
                Keybind = (i + 1).ToString()
            });
        }

        // 触发槽位变化事件
        OnSlotsCountChanged?.Invoke(0, _initialSlots);
        OnSlotsAdded?.Invoke(_initialSlots);
        OnQuickSlotsAdded?.Invoke(_initialQuickSlots);
    }

    /// <summary>获取槽位扩展统计（用于UI显示）</summary>
    public ExpansionStats GetExpansionStats()
    {
        return new ExpansionStats
        {
            CurrentMainSlots = _slots.Count,
            MaxMainSlotsPossible = _slots.Count * 2, // 假设最大可扩展为当前两倍
            CurrentQuickSlots = _quickSlots.Count,
            MaxQuickSlotsPossible = _quickSlots.Count + 10, // 假设最多可加10个快捷栏槽位
            WeightUsagePercentage = WeightPercentage,
            IsMaxCapacityReached = false // 实际逻辑需要根据配置判断
        };
    }
}

/// <summary>容量信息结构</summary>
public struct CapacityInfo
{
    public int MainSlotsCount;
    public int QuickSlotsCount;
    public int OccupiedMainSlots;
    public int OccupiedQuickSlots;
    public int TotalWeight;
    public int MaxWeight;
}

/// <summary>扩展统计信息</summary>
public struct ExpansionStats
{
    public int CurrentMainSlots;
    public int MaxMainSlotsPossible;
    public int CurrentQuickSlots;
    public int MaxQuickSlotsPossible;
    public float WeightUsagePercentage;
    public bool IsMaxCapacityReached;
}

/// <summary>槽位类型</summary>
public enum SlotType
{
    Inventory,
    QuickSlot,
    Equipment,
    Crafting
}