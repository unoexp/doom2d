// 📁 01_Data/Inventory/InventorySlot.cs
// 背包槽位定义，支持类型过滤和特殊槽位标记
using System;
using UnityEngine;

namespace SurvivalGame.Data.Inventory
{
    /// <summary>
    /// 槽位类型枚举，支持MOD扩展
    /// </summary>
    public enum SlotType
    {
        General = 0,    // 通用槽位
        QuickAccess = 1,// 快捷栏
        Weapon = 2,     // 武器槽
        Armor = 3,      // 护甲槽
        Tool = 4,       // 工具槽
        ModExtension = 100 // MOD扩展起始值
    }

    /// <summary>
    /// 槽位数据定义，用于配置背包布局
    /// 🏗️ 纯数据：CanAcceptItem 由调用方传入物品分类，不自行加载 SO
    /// </summary>
    [Serializable]
    public struct InventorySlot
    {
        // ============ 配置数据 ============
        [SerializeField] private int _index;
        [SerializeField] private SlotType _slotType;
        [SerializeField] private string[] _allowedCategories;

        // ============ 运行时状态 ============
        [SerializeField] private ItemStack _itemStack;

        public static readonly InventorySlot Empty = new InventorySlot(-1);

        // ============ 构造函数 ============
        public InventorySlot(int index, SlotType slotType = SlotType.General,
            string[] allowedCategories = null)
        {
            _index = index;
            _slotType = slotType;
            _allowedCategories = allowedCategories ?? Array.Empty<string>();
            _itemStack = ItemStack.Empty;
        }

        // ============ 属性访问器 ============
        public int Index => _index;
        public SlotType SlotType => _slotType;
        public string[] AllowedCategories => _allowedCategories;
        public ItemStack ItemStack => _itemStack;

        public bool IsEmpty => _itemStack.IsEmpty;
        public bool IsValid => _index >= 0;

        // ============ 验证方法 ============

        /// <summary>
        /// 检查槽位是否可接受指定物品
        /// 🏗️ category 由调用方通过 IItemDataService 查询后传入，数据层不做资源加载
        /// </summary>
        public bool CanAcceptItem(ItemStack itemStack, ItemCategory category)
        {
            if (!IsValid) return false;
            if (itemStack.IsEmpty) return false;

            // 检查槽位类型限制
            switch (_slotType)
            {
                case SlotType.Weapon:
                    return category == ItemCategory.Weapon;
                case SlotType.Armor:
                    return category == ItemCategory.Armor;
                case SlotType.Tool:
                    return category == ItemCategory.Tool;
                case SlotType.QuickAccess:
                    // 快捷栏允许武器、工具、消耗品
                    return category == ItemCategory.Weapon ||
                           category == ItemCategory.Tool ||
                           category == ItemCategory.Consumable;
            }

            // 检查自定义分类过滤
            if (_allowedCategories.Length > 0)
            {
                string itemCategoryStr = category.ToString();
                foreach (var allowedCategory in _allowedCategories)
                {
                    if (allowedCategory == itemCategoryStr)
                        return true;
                }
                return false;
            }

            return true;
        }

        // ============ 操作方法 ============

        /// <summary>
        /// 设置槽位中的物品
        /// 🏗️ 不做类型验证：验证职责由 InventorySystem 在调用前通过 CanAcceptItem 执行
        /// </summary>
        public InventorySlot WithItem(ItemStack itemStack)
        {
            return new InventorySlot(_index, _slotType, _allowedCategories)
            {
                _itemStack = itemStack
            };
        }

        public InventorySlot Clear()
        {
            return new InventorySlot(_index, _slotType, _allowedCategories)
            {
                _itemStack = ItemStack.Empty
            };
        }

        public InventorySlot ChangeQuantity(int delta)
        {
            if (_itemStack.IsEmpty || delta == 0) return this;

            var newQuantity = _itemStack.Quantity + delta;
            if (newQuantity <= 0) return Clear();

            return WithItem(_itemStack.WithQuantity(newQuantity));
        }
    }
}
