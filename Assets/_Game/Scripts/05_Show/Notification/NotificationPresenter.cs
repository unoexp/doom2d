// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/05_Show/Notification/NotificationPresenter.cs
// 通知系统Presenter。连接业务事件与通知ViewModel。
// ══════════════════════════════════════════════════════════════════════
using UnityEngine;

/// <summary>
/// 通知系统 Presenter。
///
/// 核心职责：
///   · 订阅各种业务事件（拾取、制作、建造、解锁、预警等）
///   · 将事件转化为通知请求写入 ViewModel
///   · 每帧驱动 ViewModel 更新通知生命周期
///
/// 设计说明：
///   · 自动订阅常用事件，无需手动配置
///   · 外部系统也可直接发布 NotificationRequestEvent 显示自定义通知
///   · 注册为 HUD 常驻面板，始终可见
/// </summary>
public class NotificationPresenter : MonoBehaviour
{
    // ══════════════════════════════════════════════════════
    // 引用
    // ══════════════════════════════════════════════════════

    [SerializeField] private NotificationView _view;

    private NotificationViewModel _viewModel;

    // ══════════════════════════════════════════════════════
    // 生命周期
    // ══════════════════════════════════════════════════════

    private void Awake()
    {
        _viewModel = new NotificationViewModel();
    }

    private void Start()
    {
        if (_view != null)
        {
            _view.Bind(_viewModel);

            // 注册为 HUD（常驻显示）
            var uiManager = ServiceLocator.Get<UIManager>();
            if (uiManager != null)
            {
                uiManager.RegisterPanel(_view);
                uiManager.RegisterHUD(_view);
                _view.Show();
            }
        }
    }

    private void OnEnable()
    {
        // 通用通知入口
        EventBus.Subscribe<NotificationRequestEvent>(OnNotificationRequest);

        // 自动订阅常用业务事件
        EventBus.Subscribe<ItemAddedToInventoryEvent>(OnItemPickup);
        EventBus.Subscribe<CraftingResultEvent>(OnCraftingResult);
        EventBus.Subscribe<BuildCompletedEvent>(OnBuildCompleted);
        EventBus.Subscribe<RecipeUnlockedEvent>(OnRecipeUnlocked);
        EventBus.Subscribe<BuildingUnlockedEvent>(OnBuildingUnlocked);
        EventBus.Subscribe<SurvivalCriticalWarningEvent>(OnCriticalWarning);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<NotificationRequestEvent>(OnNotificationRequest);
        EventBus.Unsubscribe<ItemAddedToInventoryEvent>(OnItemPickup);
        EventBus.Unsubscribe<CraftingResultEvent>(OnCraftingResult);
        EventBus.Unsubscribe<BuildCompletedEvent>(OnBuildCompleted);
        EventBus.Unsubscribe<RecipeUnlockedEvent>(OnRecipeUnlocked);
        EventBus.Unsubscribe<BuildingUnlockedEvent>(OnBuildingUnlocked);
        EventBus.Unsubscribe<SurvivalCriticalWarningEvent>(OnCriticalWarning);
    }

    private void Update()
    {
        _viewModel.Update(Time.time);
    }

    // ══════════════════════════════════════════════════════
    // 事件处理 —— 通用
    // ══════════════════════════════════════════════════════

    private void OnNotificationRequest(NotificationRequestEvent evt)
    {
        _viewModel.Enqueue(evt.Message, evt.Type, evt.Icon, evt.Duration);
    }

    // ══════════════════════════════════════════════════════
    // 事件处理 —— 自动转化
    // ══════════════════════════════════════════════════════

    private void OnItemPickup(ItemAddedToInventoryEvent evt)
    {
        // 获取物品显示名
        string displayName = evt.ItemId;
        var itemDataService = ServiceLocator.Get<IItemDataService>();
        Sprite icon = null;
        if (itemDataService != null)
        {
            var itemDef = itemDataService.GetItemDefinition(evt.ItemId);
            if (itemDef != null)
            {
                displayName = itemDef.DisplayName;
                icon = itemDef.Icon;
            }
        }

        _viewModel.Enqueue($"+{displayName} x{evt.Amount}",
                           NotificationType.ItemPickup, icon);
    }

    private void OnCraftingResult(CraftingResultEvent evt)
    {
        if (evt.Result == CraftingResult.Success)
        {
            string itemName = evt.OutputItemId;
            Sprite icon = null;
            var itemDataService = ServiceLocator.Get<IItemDataService>();
            if (itemDataService != null)
            {
                var itemDef = itemDataService.GetItemDefinition(evt.OutputItemId);
                if (itemDef != null)
                {
                    itemName = itemDef.DisplayName;
                    icon = itemDef.Icon;
                }
            }
            _viewModel.Enqueue($"制作成功: {itemName} x{evt.OutputAmount}",
                               NotificationType.Craft, icon);
        }
        else
        {
            string reason = GetCraftingFailReason(evt.Result);
            _viewModel.Enqueue($"制作失败: {reason}", NotificationType.Error);
        }
    }

    private void OnBuildCompleted(BuildCompletedEvent evt)
    {
        _viewModel.Enqueue($"建造完成: {evt.DisplayName}",
                           NotificationType.Build);
    }

    private void OnRecipeUnlocked(RecipeUnlockedEvent evt)
    {
        _viewModel.Enqueue($"新配方已解锁!", NotificationType.Unlock);
    }

    private void OnBuildingUnlocked(BuildingUnlockedEvent evt)
    {
        _viewModel.Enqueue($"新建筑已解锁!", NotificationType.Unlock);
    }

    private void OnCriticalWarning(SurvivalCriticalWarningEvent evt)
    {
        string attrName = GetAttributeDisplayName(evt.AttributeType);
        string levelText = evt.WarningLevel == CriticalWarningLevel.Lethal
            ? "危险" : "警告";
        _viewModel.Enqueue($"{levelText}: {attrName}过低!",
                           evt.WarningLevel == CriticalWarningLevel.Lethal
                               ? NotificationType.Error
                               : NotificationType.Warning);
    }

    // ══════════════════════════════════════════════════════
    // 辅助方法
    // ══════════════════════════════════════════════════════

    private static string GetCraftingFailReason(CraftingResult result)
    {
        switch (result)
        {
            case CraftingResult.Failed_NoMaterial:   return "材料不足";
            case CraftingResult.Failed_NoUnlock:     return "配方未解锁";
            case CraftingResult.Failed_NoWorkbench:  return "需要工作台";
            case CraftingResult.Failed_Overloaded:   return "背包已满";
            default:                                 return "未知原因";
        }
    }

    private static string GetAttributeDisplayName(SurvivalAttributeType type)
    {
        switch (type)
        {
            case SurvivalAttributeType.Health:      return "生命值";
            case SurvivalAttributeType.Hunger:      return "饱食度";
            case SurvivalAttributeType.Thirst:      return "水分";
            case SurvivalAttributeType.Temperature: return "体温";
            case SurvivalAttributeType.Stamina:     return "体力";
            case SurvivalAttributeType.Oxygen:      return "氧气";
            default:                                return type.ToString();
        }
    }
}
