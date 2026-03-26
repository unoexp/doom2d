// 📁 05_Show/Inventory/ViewModels/SlotViewModel.cs
// ⚠️ 纯C#类，无Unity依赖

using System;

/// <summary>
/// 槽位ViewModel，单个槽位的数据容器
/// 🏗️ 职责：封装槽位显示所需数据，提供数据绑定接口
/// 🚫 禁止包含业务逻辑，仅数据持有
/// </summary>
public class SlotViewModel
{
    // 槽位基本信息
    public int SlotIndex { get; set; }
    public SlotType SlotType { get; set; }
    public string Keybind { get; set; } // 快捷键显示文本（如"1","2"）

    // 物品数据
    public string ItemId { get; private set; }
    public int ItemAmount { get; private set; }
    public float ItemDurability { get; private set; } = 1.0f;

    // UI状态
    public bool IsSelected { get; set; }
    public bool IsHighlighted { get; set; }
    public bool IsDragging { get; set; }

    // 计算属性
    public bool IsEmpty => string.IsNullOrEmpty(ItemId) || ItemAmount <= 0;
    public bool HasItem => !IsEmpty;
    public bool IsStackable => ItemAmount > 1;
    public bool IsDamaged => ItemDurability < 0.99f;
    public bool IsBroken => ItemDurability <= 0.01f;

    // 格式化文本
    public string AmountText => ItemAmount > 1 ? ItemAmount.ToString() : "";
    public string DurabilityText => IsDamaged ? $"{ItemDurability:P0}" : "";

    // 事件
    public event Action<SlotViewModel> OnItemChanged;
    public event Action<SlotViewModel> OnSelectionChanged;
    public event Action<SlotViewModel> OnHighlightChanged;

    /// <summary>更新槽位物品</summary>
    public void UpdateItem(string itemId, int amount, float durability = 1.0f)
    {
        bool changed = ItemId != itemId || ItemAmount != amount;

        ItemId = itemId;
        ItemAmount = amount;
        ItemDurability = Math.Clamp(durability, 0f, 1f);

        if (changed)
        {
            OnItemChanged?.Invoke(this);
        }
    }

    /// <summary>更新物品数量</summary>
    public void UpdateAmount(int amount)
    {
        if (ItemAmount != amount)
        {
            ItemAmount = amount;
            OnItemChanged?.Invoke(this);
        }
    }

    /// <summary>更新耐久度</summary>
    public void UpdateDurability(float durability)
    {
        float clamped = Math.Clamp(durability, 0f, 1f);
        if (Math.Abs(ItemDurability - clamped) > 0.001f)
        {
            ItemDurability = clamped;
            OnItemChanged?.Invoke(this);
        }
    }

    /// <summary>清除槽位</summary>
    public void Clear()
    {
        if (!IsEmpty)
        {
            ItemId = null;
            ItemAmount = 0;
            ItemDurability = 1.0f;
            OnItemChanged?.Invoke(this);
        }
    }

    /// <summary>设置选中状态</summary>
    public void SetSelected(bool selected)
    {
        if (IsSelected != selected)
        {
            IsSelected = selected;
            OnSelectionChanged?.Invoke(this);
        }
    }

    /// <summary>设置高亮状态</summary>
    public void SetHighlighted(bool highlighted)
    {
        if (IsHighlighted != highlighted)
        {
            IsHighlighted = highlighted;
            OnHighlightChanged?.Invoke(this);
        }
    }

    /// <summary>设置拖拽状态</summary>
    public void SetDragging(bool dragging)
    {
        IsDragging = dragging;
    }

    /// <summary>获取物品名称（通过ServiceLocator访问）</summary>
    public string GetItemName()
    {
        if (IsEmpty) return "";

        // 通过ServiceLocator获取ItemDataService
        var itemService = ServiceLocator.Get<IItemDataService>();
        if (itemService != null)
        {
            var definition = itemService.GetItemDefinition(ItemId);
            return definition?.DisplayName ?? ItemId;
        }

        return ItemId; // 后备方案
    }

    /// <summary>获取物品描述</summary>
    public string GetItemDescription()
    {
        if (IsEmpty) return "";

        var itemService = ServiceLocator.Get<IItemDataService>();
        if (itemService != null)
        {
            var definition = itemService.GetItemDefinition(ItemId);
            return definition?.Description ?? "";
        }

        return "";
    }

    /// <summary>获取物品分类</summary>
    public string GetItemCategory()
    {
        if (IsEmpty) return "";

        var itemService = ServiceLocator.Get<IItemDataService>();
        if (itemService != null)
        {
            var definition = itemService.GetItemDefinition(ItemId);
            if (definition != null)
            {
                // 根据类型判断分类
                return definition switch
                {
                    ConsumableItemSO => "Consumable",
                    ToolItemSO => "Tool",
                    WeaponItemSO => "Weapon",
                    ArmorItemSO => "Armor",
                    MaterialItemSO => "Material",
                    _ => "Misc"
                };
            }
        }

        return "Unknown";
    }

    /// <summary>获取图标资源路径</summary>
    public string GetIconPath()
    {
        if (IsEmpty) return "";

        // 使用配置的路径模板
        var config = ServiceLocator.Get<IUIConfigService>()?.GetInventoryConfig();
        if (config != null && !string.IsNullOrEmpty(config.IconPathTemplate))
        {
            return string.Format(config.IconPathTemplate, ItemId);
        }

        return $"UI/Icons/{ItemId}"; // 默认路径
    }

    /// <summary>获取物品重量</summary>
    public float GetItemWeight()
    {
        if (IsEmpty) return 0;

        var itemService = ServiceLocator.Get<IItemDataService>();
        if (itemService != null)
        {
            var definition = itemService.GetItemDefinition(ItemId);
            return definition?.Weight ?? 0.1f;
        }

        return 0.1f;
    }

    /// <summary>获取总重量（数量×单件重量）</summary>
    public float GetTotalWeight()
    {
        return GetItemWeight() * ItemAmount;
    }

    /// <summary>判断是否可堆叠</summary>
    public bool CanStackWith(SlotViewModel other)
    {
        if (IsEmpty || other.IsEmpty) return false;
        if (ItemId != other.ItemId) return false;
        if (Math.Abs(ItemDurability - other.ItemDurability) > 0.01f) return false;

        var itemService = ServiceLocator.Get<IItemDataService>();
        if (itemService != null)
        {
            var definition = itemService.GetItemDefinition(ItemId);
            if (definition != null)
            {
                return ItemAmount + other.ItemAmount <= definition.MaxStackSize;
            }
        }

        return false;
    }

    /// <summary>深拷贝</summary>
    public SlotViewModel Clone()
    {
        return new SlotViewModel
        {
            SlotIndex = SlotIndex,
            SlotType = SlotType,
            Keybind = Keybind,
            ItemId = ItemId,
            ItemAmount = ItemAmount,
            ItemDurability = ItemDurability,
            IsSelected = IsSelected,
            IsHighlighted = IsHighlighted,
            IsDragging = IsDragging
        };
    }
}

/// <summary>物品数据服务接口（需要业务层实现）</summary>
public interface IItemDataService
{
    ItemDefinitionSO GetItemDefinition(string itemId);
}

/// <summary>UI配置服务接口</summary>
public interface IUIConfigService
{
    InventoryUIConfigSO GetInventoryConfig();
}

// 物品类型引用（需要根据实际项目调整）
// 这些类应该定义在01_Data层
public class ConsumableItemSO { }
public class ToolItemSO { }
public class WeaponItemSO { }
public class ArmorItemSO { }
public class MaterialItemSO { }