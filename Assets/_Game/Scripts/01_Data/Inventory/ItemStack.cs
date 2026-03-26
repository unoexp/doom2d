// 📁 01_Data/Inventory/ItemStack.cs
// 物品堆叠数据，用于背包系统中的物品实例
using System;
using UnityEngine;

namespace SurvivalGame.Data.Inventory
{
    /// <summary>
    /// 物品堆叠数据，包含物品实例的运行时状态
    /// ⚠️ 设计为结构体以支持对象池和零GC分配
    /// </summary>
    [Serializable]
    public struct ItemStack : IEquatable<ItemStack>
    {
        // ============ 核心数据 ============
        [SerializeField] private string _itemId;
        [SerializeField] private int _quantity;
        [SerializeField] private float _durability; // 0-1范围，0表示完全损坏

        // ============ MOD扩展数据 ============
        [SerializeField] private string _customDataJson; // JSON字符串，用于MOD存储自定义数据

        // ============ 运行时状态（不序列化） ============
        [NonSerialized] private ItemDefinitionSO _cachedDefinition;

        public static readonly ItemStack Empty = new ItemStack();

        // ============ 构造函数 ============
        public ItemStack(string itemId, int quantity = 1, float durability = 1.0f)
        {
            _itemId = itemId ?? string.Empty;
            _quantity = Mathf.Max(1, quantity);
            _durability = Mathf.Clamp01(durability);
            _customDataJson = string.Empty;
            _cachedDefinition = null;
        }

        // ============ 属性访问器 ============
        public string ItemId => _itemId;
        public int Quantity => _quantity;
        public float Durability => _durability;
        public string CustomDataJson => _customDataJson;

        public bool IsEmpty => string.IsNullOrEmpty(_itemId) || _quantity <= 0;

        /// <summary>获取物品定义，带缓存机制</summary>
        public ItemDefinitionSO GetDefinition()
        {
            if (_cachedDefinition == null && !string.IsNullOrEmpty(_itemId))
            {
                // 通过Resources或Addressables加载
                // TODO: 实现物品定义的加载逻辑
                _cachedDefinition = Resources.Load<ItemDefinitionSO>($"Items/{_itemId}");
            }
            return _cachedDefinition;
        }

        // ============ 操作方法 ============
        public ItemStack WithQuantity(int newQuantity)
        {
            return new ItemStack(_itemId, newQuantity, _durability)
            {
                _customDataJson = _customDataJson
            };
        }

        public ItemStack WithDurability(float newDurability)
        {
            return new ItemStack(_itemId, _quantity, newDurability)
            {
                _customDataJson = _customDataJson
            };
        }

        public ItemStack WithCustomData(string jsonData)
        {
            return new ItemStack(_itemId, _quantity, _durability)
            {
                _customDataJson = jsonData ?? string.Empty
            };
        }

        /// <summary>消耗耐久度</summary>
        public ItemStack ConsumeDurability(float amount)
        {
            if (amount <= 0 || _durability <= 0) return this;

            float newDurability = Mathf.Max(0f, _durability - amount);
            return WithDurability(newDurability);
        }

        /// <summary>修复耐久度</summary>
        public ItemStack RepairDurability(float amount)
        {
            if (amount <= 0 || _durability >= 1.0f) return this;

            float newDurability = Mathf.Min(1.0f, _durability + amount);
            return WithDurability(newDurability);
        }

        /// <summary>完全修复耐久度</summary>
        public ItemStack RepairFull()
        {
            return WithDurability(1.0f);
        }

        /// <summary>检查物品是否已损坏</summary>
        public bool IsBroken()
        {
            return _durability <= 0f;
        }

        /// <summary>获取当前耐久度百分比（0-1）</summary>
        public float GetDurabilityPercentage()
        {
            return _durability;
        }

        /// <summary>获取当前耐久度数值（基于物品定义的最大耐久度）</summary>
        public float GetCurrentDurabilityValue()
        {
            var definition = GetDefinition();
            if (definition == null || !definition.HasDurability) return 0f;

            return _durability * definition.MaxDurability;
        }

        // ============ 工具方法 ============
        public bool CanStackWith(ItemStack other)
        {
            if (IsEmpty || other.IsEmpty) return false;
            if (_itemId != other._itemId) return false;
            if (Math.Abs(_durability - other._durability) > 0.01f) return false;
            if (_customDataJson != other._customDataJson) return false;

            var definition = GetDefinition();
            if (definition == null) return false;

            return _quantity + other._quantity <= definition.MaxStackSize;
        }

        public ItemStack MergeWith(ItemStack other, out ItemStack overflow)
        {
            overflow = ItemStack.Empty;
            if (!CanStackWith(other)) return this;

            var total = _quantity + other._quantity;
            var definition = GetDefinition();
            var maxStack = definition?.MaxStackSize ?? 1;

            if (total <= maxStack)
            {
                return WithQuantity(total);
            }
            else
            {
                overflow = new ItemStack(_itemId, total - maxStack, _durability)
                {
                    _customDataJson = _customDataJson
                };
                return WithQuantity(maxStack);
            }
        }

        // ============ 接口实现 ============
        public bool Equals(ItemStack other) =>
            _itemId == other._itemId &&
            _quantity == other._quantity &&
            Mathf.Approximately(_durability, other._durability) &&
            _customDataJson == other._customDataJson;

        public override bool Equals(object obj) => obj is ItemStack other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(_itemId, _quantity, _durability, _customDataJson);
    }
}