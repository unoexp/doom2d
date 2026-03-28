// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/05_Show/Inventory/Tooltip/ItemTooltipViewModel.cs
// 物品详情Tooltip的ViewModel。管理物品信息的显示数据。
// ══════════════════════════════════════════════════════════════════════
using System;
using UnityEngine;

/// <summary>
/// 物品Tooltip显示数据
/// </summary>
public struct ItemTooltipData
{
    public string DisplayName;
    public string Description;
    public Sprite Icon;
    public ItemCategory Category;
    public ItemRarity Rarity;
    public int StackSize;
    public int MaxStackSize;
    public float Weight;
    public bool HasDurability;
    public float CurrentDurability;
    public float MaxDurability;

    /// <summary>额外属性行（武器攻击力、防具防御等）</summary>
    public string[] ExtraLines;
}

/// <summary>
/// 物品Tooltip ViewModel。
///
/// 核心职责：
///   · 持有当前显示物品的详情数据
///   · 暴露事件通知 View 更新
///   · 管理显示/隐藏状态
/// </summary>
public class ItemTooltipViewModel
{
    // ══════════════════════════════════════════════════════
    // 数据
    // ══════════════════════════════════════════════════════

    private ItemTooltipData _currentData;
    private bool _isVisible;

    // ══════════════════════════════════════════════════════
    // 事件
    // ══════════════════════════════════════════════════════

    /// <summary>Tooltip显示，携带数据</summary>
    public event Action<ItemTooltipData> OnShow;

    /// <summary>Tooltip隐藏</summary>
    public event Action OnHide;

    // ══════════════════════════════════════════════════════
    // 属性
    // ══════════════════════════════════════════════════════

    public bool IsVisible => _isVisible;
    public ItemTooltipData CurrentData => _currentData;

    // ══════════════════════════════════════════════════════
    // 公有 API
    // ══════════════════════════════════════════════════════

    /// <summary>显示物品Tooltip</summary>
    public void Show(ItemTooltipData data)
    {
        _currentData = data;
        _isVisible = true;
        OnShow?.Invoke(data);
    }

    /// <summary>隐藏Tooltip</summary>
    public void Hide()
    {
        if (!_isVisible) return;
        _isVisible = false;
        OnHide?.Invoke();
    }

    // ══════════════════════════════════════════════════════
    // 辅助方法
    // ══════════════════════════════════════════════════════

    /// <summary>获取稀有度颜色</summary>
    public static Color GetRarityColor(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common:     return Color.white;
            case ItemRarity.Uncommon:   return new Color(0.4f, 0.9f, 0.4f);
            case ItemRarity.Rare:       return new Color(0.4f, 0.6f, 1f);
            case ItemRarity.Epic:       return new Color(0.7f, 0.4f, 0.9f);
            case ItemRarity.Legendary:  return new Color(1f, 0.65f, 0.15f);
            case ItemRarity.Unique:     return new Color(1f, 0.3f, 0.3f);
            default:                    return Color.gray;
        }
    }

    /// <summary>获取分类显示名</summary>
    public static string GetCategoryText(ItemCategory category)
    {
        switch (category)
        {
            case ItemCategory.Weapon:      return "武器";
            case ItemCategory.Armor:       return "护甲";
            case ItemCategory.Tool:        return "工具";
            case ItemCategory.Consumable:  return "消耗品";
            case ItemCategory.Resource:    return "资源";
            case ItemCategory.Material:    return "材料";
            case ItemCategory.KeyItem:     return "关键物品";
            default:                       return "通用";
        }
    }
}
