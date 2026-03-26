// 📁 01_Data/Inventory/InventoryContainer.cs
// 通用背包容器，管理多个槽位
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalGame.Data.Inventory
{
    /// <summary>
    /// 背包容器配置，通过ScriptableObject定义布局
    /// </summary>
    [CreateAssetMenu(fileName = "InventoryContainer_", menuName = "SurvivalGame/Inventory/Container")]
    public class InventoryContainerSO : ScriptableObject
    {
        [SerializeField] private string _containerId;
        [SerializeField] private int _capacity = 24;
        [SerializeField] private InventorySlot[] _slotDefinitions;

        [Header("重量限制")]
        [SerializeField] private bool _hasWeightLimit = false;
        [SerializeField] private float _maxWeight = 100f;

        public string ContainerId => _containerId;
        public int Capacity => _capacity;
        public InventorySlot[] SlotDefinitions => _slotDefinitions;
        public bool HasWeightLimit => _hasWeightLimit;
        public float MaxWeight => _maxWeight;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_containerId))
                _containerId = name;

            if (_slotDefinitions == null || _slotDefinitions.Length != _capacity)
            {
                _slotDefinitions = new InventorySlot[_capacity];
                for (int i = 0; i < _capacity; i++)
                    _slotDefinitions[i] = new InventorySlot(i);
            }
        }
    }

    /// <summary>
    /// 背包容器运行时数据，支持序列化
    /// </summary>
    [Serializable]
    public struct InventoryContainer
    {
        // ============ 核心数据 ============
        [SerializeField] private string _containerId;
        [SerializeField] private InventorySlot[] _slots;
        [SerializeField] private bool _hasWeightLimit;
        [SerializeField] private float _maxWeight;

        // ============ 运行时状态 ============
        [NonSerialized] private Dictionary<string, List<int>> _itemIndexCache;

        public static readonly InventoryContainer Empty = new InventoryContainer();

        // ============ 构造函数 ============
        public InventoryContainer(string containerId, int capacity, bool hasWeightLimit = false, float maxWeight = 100f)
        {
            _containerId = containerId ?? string.Empty;
            _slots = new InventorySlot[capacity];
            _hasWeightLimit = hasWeightLimit;
            _maxWeight = maxWeight;
            for (int i = 0; i < capacity; i++)
                _slots[i] = new InventorySlot(i);

            _itemIndexCache = null;
        }

        public InventoryContainer(InventoryContainerSO config)
        {
            _containerId = config.ContainerId;
            _slots = new InventorySlot[config.Capacity];
            _hasWeightLimit = config.HasWeightLimit;
            _maxWeight = config.MaxWeight;

            var definitions = config.SlotDefinitions;
            for (int i = 0; i < _slots.Length; i++)
                _slots[i] = i < definitions.Length ? definitions[i] : new InventorySlot(i);

            _itemIndexCache = null;
        }

        // ============ 属性访问器 ============
        public string ContainerId => _containerId;
        public int Capacity => _slots?.Length ?? 0;
        public InventorySlot[] Slots => _slots;

        public bool IsValid => !string.IsNullOrEmpty(_containerId) && _slots != null;

        // ============ 查询方法 ============
        public bool HasItem(string itemId)
        {
            RebuildCacheIfNeeded();
            return _itemIndexCache != null && _itemIndexCache.ContainsKey(itemId);
        }

        public int GetItemCount(string itemId)
        {
            RebuildCacheIfNeeded();
            if (_itemIndexCache == null || !_itemIndexCache.TryGetValue(itemId, out var indices))
                return 0;

            int total = 0;
            foreach (var index in indices)
                total += _slots[index].ItemStack.Quantity;

            return total;
        }

        public List<int> FindSlotsWithItem(string itemId)
        {
            RebuildCacheIfNeeded();
            if (_itemIndexCache == null || !_itemIndexCache.TryGetValue(itemId, out var indices))
                return new List<int>();

            return new List<int>(indices);
        }

        public int FindFirstEmptySlot(int startIndex = 0)
        {
            for (int i = startIndex; i < _slots.Length; i++)
                if (_slots[i].IsEmpty) return i;

            return -1;
        }

        // ============ 操作方法 ============
        public bool TryAddItem(string itemId, int quantity, out int remaining)
        {
            remaining = quantity;

            if (!IsValid || quantity <= 0) return false;

            // 1. 尝试堆叠到已有槽位
            var slotsWithItem = FindSlotsWithItem(itemId);
            foreach (var slotIndex in slotsWithItem)
            {
                if (remaining <= 0) break;

                var slot = _slots[slotIndex];
                var itemStack = slot.ItemStack;
                var definition = itemStack.GetDefinition();

                if (definition == null) continue;

                int spaceInStack = definition.MaxStackSize - itemStack.Quantity;
                int toAdd = Mathf.Min(remaining, spaceInStack);

                if (toAdd > 0)
                {
                    _slots[slotIndex] = slot.ChangeQuantity(toAdd);
                    remaining -= toAdd;
                }
            }

            // 2. 填充空槽位
            if (remaining > 0)
            {
                int emptySlotIndex = FindFirstEmptySlot();
                while (emptySlotIndex >= 0 && remaining > 0)
                {
                    var definition = Resources.Load<ItemDefinitionSO>($"Items/{itemId}");
                    if (definition == null) break;

                    int toAdd = Mathf.Min(remaining, definition.MaxStackSize);
                    var newItemStack = new ItemStack(itemId, toAdd);

                    _slots[emptySlotIndex] = new InventorySlot(emptySlotIndex)
                        .WithItem(newItemStack);

                    remaining -= toAdd;
                    InvalidateCache();
                    emptySlotIndex = FindFirstEmptySlot(emptySlotIndex + 1);
                }
            }

            return remaining < quantity; // 至少添加了一些物品
        }

        public bool TryRemoveItem(string itemId, int quantity, out int remaining)
        {
            remaining = quantity;
            if (!IsValid || quantity <= 0) return false;

            var slotsWithItem = FindSlotsWithItem(itemId);

            foreach (var slotIndex in slotsWithItem)
            {
                if (remaining <= 0) break;

                var slot = _slots[slotIndex];
                var itemStack = slot.ItemStack;
                int toRemove = Mathf.Min(remaining, itemStack.Quantity);

                if (toRemove == itemStack.Quantity)
                    _slots[slotIndex] = slot.Clear();
                else
                    _slots[slotIndex] = slot.ChangeQuantity(-toRemove);

                remaining -= toRemove;
                InvalidateCache();
            }

            return remaining < quantity; // 至少移除了一些物品
        }

        public bool TryMoveItem(int fromSlotIndex, int toSlotIndex)
        {
            if (!IsValid ||
                fromSlotIndex < 0 || fromSlotIndex >= _slots.Length ||
                toSlotIndex < 0 || toSlotIndex >= _slots.Length)
                return false;

            var fromSlot = _slots[fromSlotIndex];
            var toSlot = _slots[toSlotIndex];

            if (fromSlot.IsEmpty) return false;

            // 情况1：目标槽位为空，直接移动
            if (toSlot.IsEmpty)
            {
                if (!toSlot.CanAcceptItem(fromSlot.ItemStack)) return false;

                _slots[toSlotIndex] = toSlot.WithItem(fromSlot.ItemStack);
                _slots[fromSlotIndex] = fromSlot.Clear();
                InvalidateCache();
                return true;
            }

            // 情况2：目标槽位有相同物品，尝试堆叠
            var fromItem = fromSlot.ItemStack;
            var toItem = toSlot.ItemStack;

            if (fromItem.CanStackWith(toItem))
            {
                var merged = toItem.MergeWith(fromItem, out var overflow);
                _slots[toSlotIndex] = toSlot.WithItem(merged);

                if (overflow.IsEmpty)
                    _slots[fromSlotIndex] = fromSlot.Clear();
                else
                    _slots[fromSlotIndex] = fromSlot.WithItem(overflow);

                InvalidateCache();
                return true;
            }

            // 情况3：交换物品
            if (!fromSlot.CanAcceptItem(toItem) || !toSlot.CanAcceptItem(fromItem))
                return false;

            var temp = fromSlot.ItemStack;
            _slots[fromSlotIndex] = fromSlot.WithItem(toSlot.ItemStack);
            _slots[toSlotIndex] = toSlot.WithItem(temp);
            InvalidateCache();
            return true;
        }

        // ============ 缓存管理 ============
        private void RebuildCacheIfNeeded()
        {
            if (_itemIndexCache != null || !IsValid) return;

            _itemIndexCache = new Dictionary<string, List<int>>();

            for (int i = 0; i < _slots.Length; i++)
            {
                if (!_slots[i].IsEmpty)
                {
                    var itemId = _slots[i].ItemStack.ItemId;
                    if (!_itemIndexCache.ContainsKey(itemId))
                        _itemIndexCache[itemId] = new List<int>();
                    _itemIndexCache[itemId].Add(i);
                }
            }
        }

        public void InvalidateCache()
        {
            _itemIndexCache = null;
        }

        // ============ 序列化支持 ============
        public object CaptureState()
        {
            var state = new InventoryContainerState
            {
                ContainerId = _containerId,
                SlotStates = new SlotState[_slots.Length]
            };

            for (int i = 0; i < _slots.Length; i++)
            {
                var slot = _slots[i];
                state.SlotStates[i] = new SlotState
                {
                    Index = slot.Index,
                    ItemId = slot.ItemStack.ItemId,
                    Quantity = slot.ItemStack.Quantity,
                    Durability = slot.ItemStack.Durability,
                    CustomDataJson = slot.ItemStack.CustomDataJson
                };
            }

            return state;
        }

        public void RestoreState(object state)
        {
            if (!(state is InventoryContainerState containerState))
                return;

            _containerId = containerState.ContainerId;
            _slots = new InventorySlot[containerState.SlotStates.Length];
            _itemIndexCache = null;

            for (int i = 0; i < containerState.SlotStates.Length; i++)
            {
                var slotState = containerState.SlotStates[i];
                var itemStack = string.IsNullOrEmpty(slotState.ItemId)
                    ? ItemStack.Empty
                    : new ItemStack(slotState.ItemId, slotState.Quantity, slotState.Durability)
                        .WithCustomData(slotState.CustomDataJson);

                _slots[i] = new InventorySlot(slotState.Index)
                    .WithItem(itemStack);
            }
        }

        // ============ 排序方法 ============
        public bool Sort(SortType sortType)
        {
            if (!IsValid || _slots.Length <= 1) return false;

            // 收集所有非空槽位
            var nonEmptySlots = new List<(int index, InventorySlot slot)>();
            for (int i = 0; i < _slots.Length; i++)
            {
                if (!_slots[i].IsEmpty)
                {
                    nonEmptySlots.Add((i, _slots[i]));
                }
            }

            if (nonEmptySlots.Count <= 1) return false; // 无需排序

            // 根据排序类型排序
            switch (sortType)
            {
                case SortType.ByName:
                    nonEmptySlots.Sort((a, b) =>
                    {
                        var defA = a.slot.ItemStack.GetDefinition();
                        var defB = b.slot.ItemStack.GetDefinition();
                        if (defA == null && defB == null) return 0;
                        if (defA == null) return 1;
                        if (defB == null) return -1;
                        return string.Compare(defA.DisplayName, defB.DisplayName, StringComparison.Ordinal);
                    });
                    break;

                case SortType.ByQuantity:
                    nonEmptySlots.Sort((a, b) => b.slot.ItemStack.Quantity.CompareTo(a.slot.ItemStack.Quantity)); // 降序
                    break;

                case SortType.ByWeight:
                    nonEmptySlots.Sort((a, b) =>
                    {
                        var defA = a.slot.ItemStack.GetDefinition();
                        var defB = b.slot.ItemStack.GetDefinition();
                        if (defA == null && defB == null) return 0;
                        if (defA == null) return 1;
                        if (defB == null) return -1;
                        return defB.Weight.CompareTo(defA.Weight); // 降序
                    });
                    break;

                case SortType.ByType:
                    nonEmptySlots.Sort((a, b) =>
                    {
                        var defA = a.slot.ItemStack.GetDefinition();
                        var defB = b.slot.ItemStack.GetDefinition();
                        if (defA == null && defB == null) return 0;
                        if (defA == null) return 1;
                        if (defB == null) return -1;
                        return ((int)defA.Category).CompareTo((int)defB.Category);
                    });
                    break;

                case SortType.ByRarity:
                    nonEmptySlots.Sort((a, b) =>
                    {
                        var defA = a.slot.ItemStack.GetDefinition();
                        var defB = b.slot.ItemStack.GetDefinition();
                        if (defA == null && defB == null) return 0;
                        if (defA == null) return 1;
                        if (defB == null) return -1;
                        return ((int)defB.Rarity).CompareTo((int)defA.Rarity); // 降序，稀有度高的在前
                    });
                    break;
            }

            // 重建槽位数组：先清空所有槽位，然后按排序顺序填充
            var newSlots = new InventorySlot[_slots.Length];
            for (int i = 0; i < _slots.Length; i++)
            {
                newSlots[i] = new InventorySlot(i, _slots[i].SlotType, _slots[i].AllowedCategories);
            }

            int newIndex = 0;
            foreach (var (oldIndex, slot) in nonEmptySlots)
            {
                // 保持槽位类型和分类限制，只移动物品
                newSlots[newIndex] = new InventorySlot(newIndex, slot.SlotType, slot.AllowedCategories)
                    .WithItem(slot.ItemStack);
                newIndex++;
            }

            _slots = newSlots;
            InvalidateCache();
            return true;
        }

        // ============ 重量计算方法 ============
        /// <summary>计算容器的当前总重量</summary>
        public float CalculateTotalWeight()
        {
            if (!IsValid) return 0f;

            float totalWeight = 0f;
            foreach (var slot in _slots)
            {
                if (!slot.IsEmpty)
                {
                    var definition = slot.ItemStack.GetDefinition();
                    if (definition != null)
                    {
                        totalWeight += definition.Weight * slot.ItemStack.Quantity;
                    }
                }
            }
            return totalWeight;
        }

        /// <summary>获取容器的最大重量限制（如果配置了）</summary>
        public float GetMaxWeight()
        {
            return _hasWeightLimit ? _maxWeight : float.MaxValue;
        }

        /// <summary>检查容器是否超重</summary>
        public bool IsOverweight()
        {
            float currentWeight = CalculateTotalWeight();
            float maxWeight = GetMaxWeight();
            return currentWeight > maxWeight;
        }

        /// <summary>获取剩余可承受重量</summary>
        public float GetRemainingWeightCapacity()
        {
            float currentWeight = CalculateTotalWeight();
            float maxWeight = GetMaxWeight();
            return Mathf.Max(0f, maxWeight - currentWeight);
        }

        /// <summary>检查物品是否可以添加到容器中而不超重</summary>
        public bool CanAddItemWithoutOverweight(string itemId, int quantity)
        {
            if (!IsValid) return false;

            var definition = Resources.Load<ItemDefinitionSO>($"Items/{itemId}");
            if (definition == null) return false;

            float addedWeight = definition.Weight * quantity;
            float remainingCapacity = GetRemainingWeightCapacity();

            return addedWeight <= remainingCapacity;
        }

        // ============ 容量扩展方法 ============
        /// <summary>扩展容器容量</summary>
        public bool ExpandCapacity(int additionalSlots)
        {
            if (!IsValid || additionalSlots <= 0) return false;

            int oldCapacity = _slots.Length;
            int newCapacity = oldCapacity + additionalSlots;

            // 创建新数组
            var newSlots = new InventorySlot[newCapacity];

            // 复制原有槽位
            for (int i = 0; i < oldCapacity; i++)
            {
                newSlots[i] = _slots[i];
            }

            // 初始化新槽位
            for (int i = oldCapacity; i < newCapacity; i++)
            {
                newSlots[i] = new InventorySlot(i);
            }

            _slots = newSlots;
            InvalidateCache();
            return true;
        }

        /// <summary>扩展容量并重新配置槽位定义</summary>
        public bool ExpandCapacityWithDefinitions(InventorySlot[] slotDefinitions)
        {
            if (!IsValid || slotDefinitions == null || slotDefinitions.Length <= _slots.Length)
                return false;

            int oldCapacity = _slots.Length;
            int newCapacity = slotDefinitions.Length;

            // 创建新数组
            var newSlots = new InventorySlot[newCapacity];

            // 复制原有槽位
            for (int i = 0; i < oldCapacity; i++)
            {
                // 保持原有物品，但更新槽位定义
                var oldSlot = _slots[i];
                var newSlotDef = slotDefinitions[i];

                // 创建新槽位，保持原有物品
                newSlots[i] = new InventorySlot(newSlotDef.Index, newSlotDef.SlotType, newSlotDef.AllowedCategories)
                    .WithItem(oldSlot.ItemStack);
            }

            // 初始化新槽位
            for (int i = oldCapacity; i < newCapacity; i++)
            {
                newSlots[i] = slotDefinitions[i];
            }

            _slots = newSlots;
            InvalidateCache();
            return true;
        }

        /// <summary>重新配置容器（改变槽位类型和过滤）</summary>
        public bool Reconfigure(InventorySlot[] slotDefinitions)
        {
            if (!IsValid || slotDefinitions == null || slotDefinitions.Length != _slots.Length)
                return false;

            var newSlots = new InventorySlot[slotDefinitions.Length];

            for (int i = 0; i < slotDefinitions.Length; i++)
            {
                var slotDef = slotDefinitions[i];
                var oldSlot = _slots[i];

                // 创建新槽位，只移动允许的物品
                if (!oldSlot.IsEmpty)
                {
                    if (slotDef.CanAcceptItem(oldSlot.ItemStack))
                    {
                        // 允许移动，保持物品
                        newSlots[i] = new InventorySlot(slotDef.Index, slotDef.SlotType, slotDef.AllowedCategories)
                            .WithItem(oldSlot.ItemStack);
                    }
                    else
                    {
                        // 不允许移动，创建空槽位
                        newSlots[i] = slotDef;
                        // TODO: 处理被拒绝的物品（可能需要移出到其他容器）
                    }
                }
                else
                {
                    // 空槽位直接使用新定义
                    newSlots[i] = slotDef;
                }
            }

            _slots = newSlots;
            InvalidateCache();
            return true;
        }

        // ============ 内部序列化状态类 ============
        [Serializable]
        private struct InventoryContainerState
        {
            public string ContainerId;
            public SlotState[] SlotStates;
        }

        [Serializable]
        private struct SlotState
        {
            public int Index;
            public string ItemId;
            public int Quantity;
            public float Durability;
            public string CustomDataJson;
        }
    }
}