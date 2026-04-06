// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/05_Show/Inventory/Tooltip/ItemTooltipPresenter.cs
// 物品Tooltip Presenter。处理物品悬停事件，构建显示数据。
// ══════════════════════════════════════════════════════════════════════
using UnityEngine;

/// <summary>
/// 物品Tooltip Presenter。
///
/// 核心职责：
///   · 订阅 ItemTooltipRequestEvent 和 ItemTooltipHideEvent
///   · 从 IItemDataService 获取物品定义数据
///   · 构建 ItemTooltipData 写入 ViewModel
///   · 针对不同物品子类型提取额外属性行
///
/// 设计说明：
///   · 通过 ServiceLocator 获取 IItemDataService
///   · 武器/防具/工具/消耗品各有专属属性行
/// </summary>
public class ItemTooltipPresenter : MonoBehaviour
{
    // ══════════════════════════════════════════════════════
    // 引用
    // ══════════════════════════════════════════════════════

    [SerializeField] private ItemTooltipView _view;

    private ItemTooltipViewModel _viewModel;
    private IItemDataService _itemDataService;

    // ══════════════════════════════════════════════════════
    // 生命周期
    // ══════════════════════════════════════════════════════

    private void Awake()
    {
        _viewModel = new ItemTooltipViewModel();
    }

    private void Start()
    {
        _itemDataService = ServiceLocator.Get<IItemDataService>();

        if (_view != null)
        {
            _view.Bind(_viewModel);
        }
    }

    private void OnEnable()
    {
        EventBus.Subscribe<ItemTooltipRequestEvent>(OnTooltipRequest);
        EventBus.Subscribe<ItemTooltipHideEvent>(OnTooltipHide);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<ItemTooltipRequestEvent>(OnTooltipRequest);
        EventBus.Unsubscribe<ItemTooltipHideEvent>(OnTooltipHide);
    }

    // ══════════════════════════════════════════════════════
    // 事件处理
    // ══════════════════════════════════════════════════════

    private void OnTooltipRequest(ItemTooltipRequestEvent evt)
    {
        if (string.IsNullOrEmpty(evt.ItemId))
        {
            _viewModel.Hide();
            return;
        }

        if (_itemDataService == null) return;

        var itemDef = _itemDataService.GetItemDefinition(evt.ItemId);
        if (itemDef == null)
        {
            _viewModel.Hide();
            return;
        }

        var data = new ItemTooltipData
        {
            DisplayName = itemDef.DisplayName,
            Description = itemDef.Description,
            Icon = itemDef.Icon,
            Category = itemDef.Category,
            Rarity = itemDef.Rarity,
            StackSize = evt.StackSize,
            MaxStackSize = itemDef.MaxStackSize,
            Weight = itemDef.Weight,
            HasDurability = itemDef.HasDurability,
            CurrentDurability = evt.CurrentDurability >= 0f
                ? evt.CurrentDurability : itemDef.MaxDurability,
            MaxDurability = itemDef.MaxDurability,
            ExtraLines = BuildExtraLines(itemDef)
        };

        _viewModel.Show(data);
    }

    private void OnTooltipHide(ItemTooltipHideEvent evt)
    {
        _viewModel.Hide();
    }

    // ══════════════════════════════════════════════════════
    // 额外属性构建
    // ══════════════════════════════════════════════════════

    /// <summary>根据物品子类型构建额外属性行</summary>
    private static string[] BuildExtraLines(ItemDefinitionSO itemDef)
    {
        // 武器
        var weapon = itemDef as WeaponItemSO;
        if (weapon != null)
        {
            return new[]
            {
                $"攻击力: {weapon.AttackDamage}",
                $"攻击速率: {weapon.AttackSpeed:F1}",
                $"攻击范围: {weapon.AttackRange:F1}"
            };
        }

        // 防具
        var armor = itemDef as ArmorItemSO;
        if (armor != null)
        {
            return new[]
            {
                $"防御力: {armor.Defense}",
                $"装备槽位: {armor.EquipSlot}"
            };
        }

        // 工具
        var tool = itemDef as ToolItemSO;
        if (tool != null)
        {
            return new[]
            {
                $"工具类型: {tool.ToolType}",
                $"采集效率: {tool.GatherEfficiency:F1}"
            };
        }

        // 消耗品
        var consumable = itemDef as ConsumableItemSO;
        if (consumable != null && consumable.Effects != null)
        {
            var lines = new System.Collections.Generic.List<string>();
            for (int i = 0; i < consumable.Effects.Length; i++)
            {
                var effect = consumable.Effects[i];
                switch (effect.EffectType)
                {
                    case ConsumableEffectType.RestoreHunger:
                        lines.Add($"恢复饱食: +{effect.Value:F0}");
                        break;
                    case ConsumableEffectType.RestoreThirst:
                        lines.Add($"恢复水分: +{effect.Value:F0}");
                        break;
                    case ConsumableEffectType.RestoreHealth:
                        lines.Add($"恢复生命: +{effect.Value:F0}");
                        break;
                    case ConsumableEffectType.RestoreStamina:
                        lines.Add($"恢复体力: +{effect.Value:F0}");
                        break;
                    case ConsumableEffectType.RestoreTemperature:
                        lines.Add($"恢复体温: +{effect.Value:F0}");
                        break;
                    case ConsumableEffectType.Buff:
                        lines.Add($"增益效果: {effect.StatusEffectId}");
                        break;
                    case ConsumableEffectType.Debuff:
                        lines.Add($"副作用: {effect.StatusEffectId}");
                        break;
                }
            }
            return lines.Count > 0 ? lines.ToArray() : null;
        }

        return null;
    }
}
