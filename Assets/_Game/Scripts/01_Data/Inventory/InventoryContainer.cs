// 📁 01_Data/Inventory/InventoryContainer.cs
// 背包容器纯数据结构
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
    /// 背包容器运行时数据
    /// 🏗️ 纯数据容器：仅持有槽位数组和基础查询，所有操作逻辑在 03_Core/InventorySystem 中
    /// 🚫 禁止在此添加需要 Resources.Load 或 IItemDataService 的任何方法
    /// </summary>
    [Serializable]
    public struct InventoryContainer
    {
        // ============ 核心数据 ============
        [SerializeField] private string _containerId;
        [SerializeField] private InventorySlot[] _slots;
        [SerializeField] private bool _hasWeightLimit;
        [SerializeField] private float _maxWeight;

        // ============ 运行时缓存 ============
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
        public bool HasWeightLimit => _hasWeightLimit;
        public float MaxWeight => _maxWeight;
        public bool IsValid => !string.IsNullOrEmpty(_containerId) && _slots != null;

        // ============ 查询方法（仅操作 itemId，不加载 SO）============

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
            if (_slots == null) return -1;
            for (int i = startIndex; i < _slots.Length; i++)
                if (_slots[i].IsEmpty) return i;

            return -1;
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
    }
}
