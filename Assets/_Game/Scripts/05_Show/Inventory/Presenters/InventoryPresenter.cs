// 📁 05_Show/Inventory/Presenters/InventoryPresenter.cs
// ⚠️ Presenter层，连接ViewModel和View

using System;
using System.Collections.Generic;
using UnityEngine;
using SurvivalGame.Data.Inventory.Expansion;

/// <summary>
/// 背包Presenter，连接ViewModel和View
/// 🏗️ 职责：处理业务事件→UI更新，处理用户交互→业务调用
/// 🚫 禁止直接访问View组件，通过ViewModel中介
/// </summary>
public class InventoryPresenter : MonoBehaviour
{
    // 依赖
    [SerializeField] private InventoryViewModel _viewModel;
    [SerializeField] private InventoryPanelView _panelView;
    [SerializeField] private QuickSlotBarView _quickSlotView;

    // 业务系统引用（通过ServiceLocator获取）
    private IInventorySystem _inventorySystem;
    private IItemDataService _itemDataService;

    // 拖拽状态
    private int _draggingSourceIndex = -1;
    private SlotType _draggingSourceType = SlotType.Inventory;

    // 扩展系统状态
    private Dictionary<string, string> _activeExpansions = new Dictionary<string, string>(); // expansionId -> status

    private void Awake()
    {
        // 初始化ViewModel（如果未设置）
        if (_viewModel == null)
            _viewModel = new InventoryViewModel();

        // 获取业务系统
        _inventorySystem = ServiceLocator.Get<IInventorySystem>();
        _itemDataService = ServiceLocator.Get<IItemDataService>();

        if (_inventorySystem == null)
            Debug.LogWarning("[InventoryPresenter] IInventorySystem not found in ServiceLocator");
        if (_itemDataService == null)
            Debug.LogWarning("[InventoryPresenter] IItemDataService not found in ServiceLocator");
    }

    private void Start()
    {
        // 订阅事件
        SubscribeToBusinessEvents();
        SubscribeToUIEvents();

        // 初始化UI状态
        InitializeUI();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void SubscribeToBusinessEvents()
    {
        // 订阅背包业务事件
        EventBus.Subscribe<ItemAddedToInventoryEvent>(OnItemAdded);
        EventBus.Subscribe<ItemRemovedFromInventoryEvent>(OnItemRemoved);
        EventBus.Subscribe<InventoryFullEvent>(OnInventoryFull);

        // 订阅玩家状态事件
        EventBus.Subscribe<PlayerWeightChangedEvent>(OnPlayerWeightChanged);
        EventBus.Subscribe<PlayerGoldChangedEvent>(OnPlayerGoldChanged);

        // 订阅扩展系统事件
        EventBus.Subscribe<InventoryExpansionValidationStartedEvent>(OnExpansionValidationStarted);
        EventBus.Subscribe<InventoryExpansionValidationResultEvent>(OnExpansionValidationResult);
        EventBus.Subscribe<InventoryExpansionConditionValidatedEvent>(OnExpansionConditionValidated);
        EventBus.Subscribe<InventoryExpansionConsumptionStartedEvent>(OnExpansionConsumptionStarted);
        EventBus.Subscribe<InventoryExpansionConsumptionResultEvent>(OnExpansionConsumptionResult);
        EventBus.Subscribe<InventoryExpansionResourceConsumedEvent>(OnExpansionResourceConsumed);
        EventBus.Subscribe<InventoryExpansionEffectStartedEvent>(OnExpansionEffectStarted);
        EventBus.Subscribe<InventoryExpansionEffectAppliedEvent>(OnExpansionEffectApplied);
        EventBus.Subscribe<InventoryExpansionEffectFailedEvent>(OnExpansionEffectFailed);
        EventBus.Subscribe<InventoryExpansionCompletedEvent>(OnExpansionCompleted);
        EventBus.Subscribe<InventoryExpansionStatusUpdatedEvent>(OnExpansionStatusUpdated);
        EventBus.Subscribe<InventoryExpansionRollbackStartedEvent>(OnExpansionRollbackStarted);
        EventBus.Subscribe<InventoryExpansionRollbackCompletedEvent>(OnExpansionRollbackCompleted);
        EventBus.Subscribe<InventoryExpansionProgressQueriedEvent>(OnExpansionProgressQueried);
        EventBus.Subscribe<InventoryExpansionUnlockedEvent>(OnExpansionUnlocked);
        EventBus.Subscribe<InventoryExpansionConfigsLoadedEvent>(OnExpansionConfigsLoaded);
    }

    private void SubscribeToUIEvents()
    {
        // 订阅UI交互事件
        EventBus.Subscribe<SlotClickedEvent>(OnSlotClicked);
        EventBus.Subscribe<SlotDragStartedEvent>(OnDragStarted);
        EventBus.Subscribe<SlotDragEndedEvent>(OnDragEnded);
        EventBus.Subscribe<QuickSlotSelectedEvent>(OnQuickSlotSelected);
        EventBus.Subscribe<InventoryToggleEvent>(OnInventoryToggle);
        EventBus.Subscribe<InventoryFilterChangedEvent>(OnFilterChanged);
        EventBus.Subscribe<InventorySortChangedEvent>(OnSortChanged);
    }

    private void UnsubscribeFromEvents()
    {
        EventBus.Unsubscribe<ItemAddedToInventoryEvent>(OnItemAdded);
        EventBus.Unsubscribe<ItemRemovedFromInventoryEvent>(OnItemRemoved);
        EventBus.Unsubscribe<InventoryFullEvent>(OnInventoryFull);
        EventBus.Unsubscribe<PlayerWeightChangedEvent>(OnPlayerWeightChanged);
        EventBus.Unsubscribe<PlayerGoldChangedEvent>(OnPlayerGoldChanged);

        // 取消订阅扩展系统事件
        EventBus.Unsubscribe<InventoryExpansionValidationStartedEvent>(OnExpansionValidationStarted);
        EventBus.Unsubscribe<InventoryExpansionValidationResultEvent>(OnExpansionValidationResult);
        EventBus.Unsubscribe<InventoryExpansionConditionValidatedEvent>(OnExpansionConditionValidated);
        EventBus.Unsubscribe<InventoryExpansionConsumptionStartedEvent>(OnExpansionConsumptionStarted);
        EventBus.Unsubscribe<InventoryExpansionConsumptionResultEvent>(OnExpansionConsumptionResult);
        EventBus.Unsubscribe<InventoryExpansionResourceConsumedEvent>(OnExpansionResourceConsumed);
        EventBus.Unsubscribe<InventoryExpansionEffectStartedEvent>(OnExpansionEffectStarted);
        EventBus.Unsubscribe<InventoryExpansionEffectAppliedEvent>(OnExpansionEffectApplied);
        EventBus.Unsubscribe<InventoryExpansionEffectFailedEvent>(OnExpansionEffectFailed);
        EventBus.Unsubscribe<InventoryExpansionCompletedEvent>(OnExpansionCompleted);
        EventBus.Unsubscribe<InventoryExpansionStatusUpdatedEvent>(OnExpansionStatusUpdated);
        EventBus.Unsubscribe<InventoryExpansionRollbackStartedEvent>(OnExpansionRollbackStarted);
        EventBus.Unsubscribe<InventoryExpansionRollbackCompletedEvent>(OnExpansionRollbackCompleted);
        EventBus.Unsubscribe<InventoryExpansionProgressQueriedEvent>(OnExpansionProgressQueried);
        EventBus.Unsubscribe<InventoryExpansionUnlockedEvent>(OnExpansionUnlocked);
        EventBus.Unsubscribe<InventoryExpansionConfigsLoadedEvent>(OnExpansionConfigsLoaded);

        EventBus.Unsubscribe<SlotClickedEvent>(OnSlotClicked);
        EventBus.Unsubscribe<SlotDragStartedEvent>(OnDragStarted);
        EventBus.Unsubscribe<SlotDragEndedEvent>(OnDragEnded);
        EventBus.Unsubscribe<QuickSlotSelectedEvent>(OnQuickSlotSelected);
        EventBus.Unsubscribe<InventoryToggleEvent>(OnInventoryToggle);
        EventBus.Unsubscribe<InventoryFilterChangedEvent>(OnFilterChanged);
        EventBus.Unsubscribe<InventorySortChangedEvent>(OnSortChanged);
    }

    private void InitializeUI()
    {
        // 设置初始UI状态
        _viewModel.IsInventoryOpen = false;

        // 如果有已存数据，初始化背包
        if (_inventorySystem != null)
        {
            RefreshInventoryData();
        }
    }

    // ================== 业务事件处理 ==================

    private void OnItemAdded(ItemAddedToInventoryEvent evt)
    {
        // 更新ViewModel
        _viewModel.UpdateSlot(evt.SlotIndex, evt.ItemId, evt.Amount);

        // 更新总重量
        UpdateTotalWeight();

        // 播放添加反馈（通过View）
        PlayItemAddedFeedback(evt.SlotIndex);
    }

    private void OnItemRemoved(ItemRemovedFromInventoryEvent evt)
    {
        // 查找并清除对应槽位
        // 注意：业务事件不包含槽位索引，需要从ViewModel查找
        var occupiedSlots = _viewModel.GetOccupiedSlots();
        foreach (var slot in occupiedSlots)
        {
            if (slot.ItemId == evt.ItemId)
            {
                _viewModel.ClearSlot(slot.SlotIndex, SlotType.Inventory);
                break;
            }
        }

        // 更新总重量
        UpdateTotalWeight();

        // 播放移除反馈
        PlayItemRemovedFeedback();
    }

    private void OnInventoryFull(InventoryFullEvent evt)
    {
        // 显示背包已满提示
        ShowNotification("背包已满！");
    }

    private void OnPlayerWeightChanged(PlayerWeightChangedEvent evt)
    {
        // 更新负重显示
        _viewModel.TotalWeight = evt.CurrentWeight;
        _viewModel.SetMaxWeight(evt.MaxWeight);

        // 如果超重，显示警告
        if (evt.CurrentWeight > evt.MaxWeight)
        {
            ShowNotification("负重超限！移动速度降低。");
        }
    }

    private void OnPlayerGoldChanged(PlayerGoldChangedEvent evt)
    {
        _viewModel.GoldAmount = evt.NewAmount;
    }

    // ================== UI事件处理 ==================

    private void OnSlotClicked(SlotClickedEvent evt)
    {
        if (_inventorySystem == null) return;

        var slotType = evt.SlotIndex < InventoryViewModel.QUICK_SLOTS
            ? SlotType.QuickSlot
            : SlotType.Inventory;

        var adjustedIndex = slotType == SlotType.QuickSlot
            ? evt.SlotIndex
            : evt.SlotIndex - InventoryViewModel.QUICK_SLOTS;

        if (evt.IsRightClick)
        {
            // 右键点击：使用物品或显示上下文菜单
            HandleRightClick(adjustedIndex, slotType);
        }
        else
        {
            // 左键点击：选择物品
            HandleLeftClick(adjustedIndex, slotType);
        }
    }

    private void OnDragStarted(SlotDragStartedEvent evt)
    {
        // 记录拖拽源
        _draggingSourceIndex = evt.SlotIndex;

        // 设置ViewModel拖拽状态
        _viewModel.DraggingSlotIndex = evt.SlotIndex;

        // 开始拖拽动画
        StartDragAnimation(evt.SlotIndex);
    }

    private void OnDragEnded(SlotDragEndedEvent evt)
    {
        if (_inventorySystem == null) return;

        if (evt.TargetSlotIndex >= 0 && _draggingSourceIndex >= 0)
        {
            // 执行物品移动
            var sourceType = _draggingSourceIndex < InventoryViewModel.QUICK_SLOTS
                ? SlotType.QuickSlot
                : SlotType.Inventory;

            var targetType = evt.TargetSlotIndex < InventoryViewModel.QUICK_SLOTS
                ? SlotType.QuickSlot
                : SlotType.Inventory;

            var sourceIndex = sourceType == SlotType.QuickSlot
                ? _draggingSourceIndex
                : _draggingSourceIndex - InventoryViewModel.QUICK_SLOTS;

            var targetIndex = targetType == SlotType.QuickSlot
                ? evt.TargetSlotIndex
                : evt.TargetSlotIndex - InventoryViewModel.QUICK_SLOTS;

            _inventorySystem.MoveItem(
                sourceType, sourceIndex,
                targetType, targetIndex
            );

            // 播放物品移动音效
            PlayItemMoveSound();
        }

        // 重置拖拽状态
        _draggingSourceIndex = -1;
        _draggingSourceType = SlotType.Inventory;
        _viewModel.DraggingSlotIndex = -1;

        // 结束拖拽动画
        EndDragAnimation();
    }

    private void OnQuickSlotSelected(QuickSlotSelectedEvent evt)
    {
        if (_inventorySystem == null) return;

        // 设置快捷栏选中
        _inventorySystem.SelectQuickSlot(evt.QuickSlotIndex);
    }

    private void OnInventoryToggle(InventoryToggleEvent evt)
    {
        _viewModel.IsInventoryOpen = evt.IsOpening;

        if (evt.IsOpening)
        {
            // 打开背包时刷新数据
            RefreshInventoryData();
            PlayInventoryOpenSound();
        }
        else
        {
            PlayInventoryCloseSound();
        }
    }

    private void OnFilterChanged(InventoryFilterChangedEvent evt)
    {
        _viewModel.CurrentFilter = evt.FilterCategory;

        // 应用过滤，这通常需要View支持
        ApplyFilterToView(evt.FilterCategory);
    }

    private void OnSortChanged(InventorySortChangedEvent evt)
    {
        _viewModel.CurrentSortMethod = evt.SortMethod;

        // 应用排序，这通常需要View支持
        ApplySortToView(evt.SortMethod);
    }

    // ================== 工具方法 ==================

    private void RefreshInventoryData()
    {
        if (_inventorySystem == null) return;

        // 获取背包数据并更新ViewModel
        var inventoryData = _inventorySystem.GetInventoryData();
        var quickSlotData = _inventorySystem.GetQuickSlotData();

        // 更新主槽位
        foreach (var slotData in inventoryData)
        {
            _viewModel.UpdateSlot(slotData.SlotIndex, slotData.ItemId, slotData.Amount);
        }

        // 更新快捷栏
        foreach (var slotData in quickSlotData)
        {
            _viewModel.UpdateQuickSlot(slotData.SlotIndex, slotData.ItemId, slotData.Amount);
        }

        // 更新重量
        UpdateTotalWeight();
    }

    private void UpdateTotalWeight()
    {
        if (_inventorySystem != null)
        {
            var weightInfo = _inventorySystem.GetWeightInfo();
            _viewModel.TotalWeight = weightInfo.CurrentWeight;
            _viewModel.SetMaxWeight(weightInfo.MaxWeight);
        }
    }

    private void HandleRightClick(int slotIndex, SlotType slotType)
    {
        // 显示上下文菜单或直接使用
        var itemId = slotType == SlotType.QuickSlot
            ? _viewModel.GetQuickSlot(slotIndex)?.ItemId
            : _viewModel.GetSlot(slotIndex)?.ItemId;

        if (!string.IsNullOrEmpty(itemId))
        {
            ShowContextMenu(slotIndex, slotType, itemId);
        }
    }

    private void HandleLeftClick(int slotIndex, SlotType slotType)
    {
        // 选中槽位
        if (slotType == SlotType.QuickSlot)
        {
            var quickSlot = _viewModel.GetQuickSlot(slotIndex);
            if (quickSlot != null)
            {
                quickSlot.SetSelected(true);
            }
        }
        else
        {
            var slot = _viewModel.GetSlot(slotIndex);
            if (slot != null)
            {
                slot.SetSelected(true);
            }
        }
    }

    // ================== 扩展系统事件处理 ==================

    private void OnExpansionValidationStarted(InventoryExpansionValidationStartedEvent evt)
    {
        _activeExpansions[evt.ExpansionId] = "验证中";
        ShowNotification($"正在验证扩展: {evt.ExpansionId}");
    }

    private void OnExpansionValidationResult(InventoryExpansionValidationResultEvent evt)
    {
        if (evt.AllConditionsMet)
        {
            ShowNotification($"{evt.ExpansionId} 验证通过");
        }
        else
        {
            ShowNotification($"验证失败: {evt.FailureSummary}");
        }
    }

    private void OnExpansionConditionValidated(InventoryExpansionConditionValidatedEvent evt)
    {
        // 单个条件验证完成，可用于UI条件状态更新
        if (!evt.IsMet)
        {
            Debug.Log($"[扩展条件] {evt.ConditionId}: {evt.FailedReason}");
        }
    }

    private void OnExpansionConsumptionStarted(InventoryExpansionConsumptionStartedEvent evt)
    {
        _activeExpansions[evt.ExpansionId] = "消耗中";
        ShowNotification($"开始消耗资源: {evt.TotalResourcesToConsume}项资源");
    }

    private void OnExpansionConsumptionResult(InventoryExpansionConsumptionResultEvent evt)
    {
        if (evt.AllConsumptionsSucceeded)
        {
            ShowNotification($"资源消耗完成");
        }
        else
        {
            ShowNotification($"资源消耗失败: {evt.FailureSummary}");
        }
    }

    private void OnExpansionResourceConsumed(InventoryExpansionResourceConsumedEvent evt)
    {
        // 单个资源消耗完成，更新UI中的资源显示
        ShowNotification($"消耗 {evt.AmountConsumed}个 {evt.ResourceName}");
    }

    private void OnExpansionEffectStarted(InventoryExpansionEffectStartedEvent evt)
    {
        _activeExpansions[evt.ExpansionId] = "应用效果中";
        ShowNotification($"正在应用扩展效果: {evt.ExpansionName}");
    }

    private void OnExpansionEffectApplied(InventoryExpansionEffectAppliedEvent evt)
    {
        // 扩展效果已应用，更新背包容量
        _activeExpansions[evt.ExpansionId] = "完成";

        // 通知ViewModel容量已变化
        if (evt.ContainerId == "MainInventory")
        {
            // 主背包容量增加
            _viewModel.AddSlots(evt.CapacityIncrease);
            ShowNotification($"{evt.ExpansionName} 完成！背包容量增加 {evt.CapacityIncrease}格");
        }
        else if (evt.ContainerId == "QuickSlots")
        {
            // 快捷栏容量增加
            _viewModel.AddQuickSlots(evt.CapacityIncrease);
            ShowNotification($"{evt.ExpansionName} 完成！快捷栏增加 {evt.CapacityIncrease}格");
        }
    }

    private void OnExpansionEffectFailed(InventoryExpansionEffectFailedEvent evt)
    {
        _activeExpansions[evt.ExpansionId] = "失败";
        ShowNotification($"扩展失败: {evt.FailureReason}");
    }

    private void OnExpansionCompleted(InventoryExpansionCompletedEvent evt)
    {
        ShowNotification($"{evt.ExpansionName} 扩展完成！总计消耗 {evt.TotalResourcesConsumed}个资源");
        _activeExpansions.Remove(evt.ExpansionId);
    }

    private void OnExpansionStatusUpdated(InventoryExpansionStatusUpdatedEvent evt)
    {
        // 更新扩展状态显示
        if (_activeExpansions.ContainsKey(evt.ExpansionId))
        {
            _activeExpansions[evt.ExpansionId] = evt.Status.ToString();
        }

        // 可以在这里更新进度条等UI元素
        if (evt.IsComplete)
        {
            Debug.Log($"[扩展完成] {evt.ExpansionId}: {evt.StatusMessage}");
        }
    }

    private void OnExpansionRollbackStarted(InventoryExpansionRollbackStartedEvent evt)
    {
        _activeExpansions[evt.ExpansionId] = "回滚中";
        ShowNotification($"开始回滚扩展: {evt.ExpansionId}");
    }

    private void OnExpansionRollbackCompleted(InventoryExpansionRollbackCompletedEvent evt)
    {
        // 扩展回滚完成
        if (evt.ResourcesRestored)
        {
            ShowNotification($"扩展回滚完成，已恢复 {evt.ResourcesRestoredCount}个资源");
        }
        else
        {
            ShowNotification("扩展回滚完成");
        }
        _activeExpansions.Remove(evt.ExpansionId);
    }

    private void OnExpansionProgressQueried(InventoryExpansionProgressQueriedEvent evt)
    {
        // 扩展进度查询结果，可用于UI显示当前状态
        if (!evt.IsAvailable)
        {
            Debug.Log($"[扩展不可用] {evt.ExpansionId}: 缺失条件 {evt.MissingConditions?.Count}");
        }
    }

    private void OnExpansionUnlocked(InventoryExpansionUnlockedEvent evt)
    {
        if (evt.NewlyUnlocked)
        {
            ShowNotification($"新扩展解锁: {evt.ExpansionId} ({evt.UnlockMethod})");
        }
    }

    private void OnExpansionConfigsLoaded(InventoryExpansionConfigsLoadedEvent evt)
    {
        ShowNotification($"加载 {evt.TotalConfigs} 个扩展配置，{evt.AvailableConfigs} 个可用");
    }

    // ================== 反馈方法 ==================

    private void PlayItemAddedFeedback(int slotIndex)
    {
        // 触发View播放添加动画
        EventBus.Publish(new UIFeedbackEvent
        {
            Type = UIFeedbackType.ItemAdded,
            SlotIndex = slotIndex
        });
    }

    private void PlayItemRemovedFeedback()
    {
        // 触发View播放移除动画
        EventBus.Publish(new UIFeedbackEvent
        {
            Type = UIFeedbackType.ItemRemoved
        });
    }

    private void StartDragAnimation(int slotIndex)
    {
        // 触发View开始拖拽动画
        EventBus.Publish(new UIFeedbackEvent
        {
            Type = UIFeedbackType.DragStart,
            SlotIndex = slotIndex
        });
    }

    private void EndDragAnimation()
    {
        // 触发View结束拖拽动画
        EventBus.Publish(new UIFeedbackEvent
        {
            Type = UIFeedbackType.DragEnd
        });
    }

    private void PlayInventoryOpenSound()
    {
        EventBus.Publish(new UIFeedbackEvent
        {
            Type = UIFeedbackType.InventoryOpen
        });
    }

    private void PlayInventoryCloseSound()
    {
        EventBus.Publish(new UIFeedbackEvent
        {
            Type = UIFeedbackType.InventoryClose
        });
    }

    private void PlayItemMoveSound()
    {
        EventBus.Publish(new UIFeedbackEvent
        {
            Type = UIFeedbackType.ItemMove
        });
    }

    private void ShowNotification(string message)
    {
        EventBus.Publish(new UINotificationEvent
        {
            Message = message,
            Duration = 2.0f
        });
    }

    private void ShowContextMenu(int slotIndex, SlotType slotType, string itemId)
    {
        EventBus.Publish(new UIContextMenuEvent
        {
            SlotIndex = slotIndex,
            SlotType = slotType,
            ItemId = itemId,
            ScreenPosition = Input.mousePosition
        });
    }

    private void ApplyFilterToView(string filter)
    {
        // 通知View应用过滤
        EventBus.Publish(new UIFilterAppliedEvent
        {
            FilterCategory = filter
        });
    }

    private void ApplySortToView(string sortMethod)
    {
        // 通知View应用排序
        EventBus.Publish(new UISortAppliedEvent
        {
            SortMethod = sortMethod
        });
    }
}

// ================== 支持的事件定义 ==================

public struct PlayerWeightChangedEvent : IEvent
{
    public int CurrentWeight;
    public int MaxWeight;
}

public struct PlayerGoldChangedEvent : IEvent
{
    public int NewAmount;
}

public struct UIFeedbackEvent : IEvent
{
    public UIFeedbackType Type;
    public int SlotIndex;
}

public enum UIFeedbackType
{
    ItemAdded,
    ItemRemoved,
    DragStart,
    DragEnd,
    InventoryOpen,
    InventoryClose,
    ItemMove
}

public struct UINotificationEvent : IEvent
{
    public string Message;
    public float Duration;
}

public struct UIContextMenuEvent : IEvent
{
    public int SlotIndex;
    public SlotType SlotType;
    public string ItemId;
    public Vector2 ScreenPosition;
}

public struct UIFilterAppliedEvent : IEvent
{
    public string FilterCategory;
}

public struct UISortAppliedEvent : IEvent
{
    public string SortMethod;
}

/// <summary>背包系统接口（需要业务层实现）</summary>
public interface IInventorySystem
{
    InventoryData[] GetInventoryData();
    QuickSlotData[] GetQuickSlotData();
    void MoveItem(SlotType sourceType, int sourceIndex, SlotType targetType, int targetIndex);
    void SelectQuickSlot(int slotIndex);
    WeightInfo GetWeightInfo();
}

public struct InventoryData
{
    public int SlotIndex;
    public string ItemId;
    public int Amount;
}

public struct QuickSlotData
{
    public int SlotIndex;
    public string ItemId;
    public int Amount;
}

public struct WeightInfo
{
    public int CurrentWeight;
    public int MaxWeight;
}