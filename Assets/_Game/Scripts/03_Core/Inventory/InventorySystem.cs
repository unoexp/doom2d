// 📁 03_Core/Inventory/InventorySystem.cs
// 核心业务层：背包系统主逻辑
using System;
using System.Collections.Generic;
using UnityEngine;
using SurvivalGame.Data.Inventory;
using SurvivalGame.Data.Inventory.Expansion;
using SurvivalGame.Core.Inventory.Expansion;

namespace SurvivalGame.Core.Inventory
{
    /// <summary>
    /// 背包系统主类，管理玩家的背包和快捷栏
    /// 🏗️ 架构说明：业务层核心系统，通过EventBus与表现层通信
    /// </summary>
    public class InventorySystem : MonoBehaviour, ISaveable
    {
        // ============ 配置字段 ============
        [Header("背包配置")]
        [SerializeField] private InventoryContainerSO _mainInventoryConfig;
        [SerializeField] private InventoryContainerSO _quickAccessConfig;

        [Header("物品配置")]
        [SerializeField] private string _itemsFolderPath = "Items/";

        // ============ 运行时状态 ============
        private InventoryContainer _mainInventory;
        private InventoryContainer _quickAccess;
        private int _selectedQuickAccessSlot = 0;

        // ============ 扩展系统相关 ============
        private ExpansionStateManager _expansionStateManager;
        private Dictionary<string, ExpansionDefinitionSO> _loadedExpansions;

        // ============ ISaveable实现 ============
        public string SaveKey => nameof(InventorySystem);

        // ============ 属性访问器 ============
        public InventoryContainer MainInventory => _mainInventory;
        public InventoryContainer QuickAccess => _quickAccess;
        public int SelectedQuickAccessSlot => _selectedQuickAccessSlot;

        // ============ 生命周期 ============
        private void Awake()
        {
            InitializeContainers();
            InitializeExpansionSystem();
            ServiceLocator.Register<InventorySystem>(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<InventorySystem>();
        }

        // ============ 初始化 ============
        private void InitializeContainers()
        {
            if (_mainInventoryConfig != null)
                _mainInventory = new InventoryContainer(_mainInventoryConfig);
            else
                _mainInventory = new InventoryContainer("MainInventory", 24); // 默认24槽位

            if (_quickAccessConfig != null)
                _quickAccess = new InventoryContainer(_quickAccessConfig);
            else
                _quickAccess = new InventoryContainer("QuickAccess", 10); // 默认10快捷栏槽位
        }

        // ============ 扩展系统初始化 ============
        private void InitializeExpansionSystem()
        {
            _expansionStateManager = new ExpansionStateManager();
            _loadedExpansions = new Dictionary<string, ExpansionDefinitionSO>();

            // 注册扩展服务
            ServiceLocator.Register<IExpansionRecordService>(_expansionStateManager);

            // TODO: 从Resources加载扩展配置
            // LoadExpansionConfigs();
        }

        // ============ 公共API：物品操作 ============
        /// <summary>尝试添加物品到背包</summary>
        public bool TryAddItem(string itemId, int quantity)
        {
            if (string.IsNullOrEmpty(itemId) || quantity <= 0)
                return false;

            // 1. 先尝试添加到快捷栏（快捷栏有特殊物品类型限制）
            int remaining = quantity;
            bool quickAccessSuccess = false;
            if (quantity > 0)
            {
                quickAccessSuccess = _quickAccess.TryAddItem(itemId, quantity, out remaining);
            }

            // 2. 剩余物品添加到主背包
            bool mainInventorySuccess = false;
            if (remaining > 0)
            {
                mainInventorySuccess = _mainInventory.TryAddItem(itemId, remaining, out int newRemaining);
                if (mainInventorySuccess)
                {
                    remaining = newRemaining;
                }
            }

            bool success = remaining < quantity;

            if (success)
            {
                int amountAdded = quantity - remaining;
                // 发布事件通知UI更新
                EventBus.Publish(new ItemAddedToInventoryEvent
                {
                    ItemId = itemId,
                    Amount = amountAdded,
                    SlotIndex = -1, // 表示自动分配
                    ContainerId = quickAccessSuccess ? _quickAccess.ContainerId : _mainInventory.ContainerId
                });

                // 发布背包改变事件
                EventBus.Publish(new InventoryChangedEvent
                {
                    ContainerId = _mainInventory.ContainerId
                });
            }
            else if (remaining == quantity)
            {
                // 背包已满
                EventBus.Publish(new InventoryFullEvent
                {
                    ContainerId = _mainInventory.ContainerId
                });
            }

            return success;
        }

        /// <summary>从背包移除物品</summary>
        public bool TryRemoveItem(string itemId, int quantity)
        {
            if (string.IsNullOrEmpty(itemId) || quantity <= 0)
                return false;

            // 1. 先尝试从快捷栏移除
            int remaining = quantity;
            bool quickAccessSuccess = _quickAccess.TryRemoveItem(itemId, quantity, out int quickRemaining);
            if (quickAccessSuccess)
            {
                remaining = quickRemaining;
                // 发布快捷栏物品移除事件
                EventBus.Publish(new ItemRemovedFromInventoryEvent
                {
                    ItemId = itemId,
                    Amount = quantity - remaining,
                    ContainerId = _quickAccess.ContainerId
                });
            }

            // 2. 再从主背包移除
            bool mainInventorySuccess = false;
            if (remaining > 0)
            {
                mainInventorySuccess = _mainInventory.TryRemoveItem(itemId, remaining, out int mainRemaining);
                if (mainInventorySuccess)
                {
                    remaining = mainRemaining;
                    // 发布主背包物品移除事件
                    EventBus.Publish(new ItemRemovedFromInventoryEvent
                    {
                        ItemId = itemId,
                        Amount = quantity - remaining,
                        ContainerId = _mainInventory.ContainerId
                    });
                }
            }

            bool success = remaining < quantity;

            if (success)
            {
                // 发布背包改变事件
                EventBus.Publish(new InventoryChangedEvent
                {
                    ContainerId = _mainInventory.ContainerId
                });
            }

            return success;
        }

        /// <summary>使用快捷栏指定槽位的物品</summary>
        public bool TryUseQuickAccessItem(int slotIndex, GameObject user)
        {
            if (slotIndex < 0 || slotIndex >= _quickAccess.Capacity)
                return false;

            var slot = _quickAccess.Slots[slotIndex];
            if (slot.IsEmpty) return false;

            var itemStack = slot.ItemStack;
            var definition = itemStack.GetDefinition();
            if (definition == null) return false;

            // 检查是否可以使用
            if (!definition.CanUse(user)) return false;

            // 执行使用效果
            definition.OnUse(user);

            bool itemConsumed = false;
            float oldDurability = itemStack.Durability;

            // 检查物品是否有耐久度
            if (definition.HasDurability && definition.MaxDurability > 0)
            {
                // 消耗耐久度
                float durabilityLoss = definition.DurabilityConsumptionPerUse / definition.MaxDurability;
                var newItemStack = itemStack.ConsumeDurability(durabilityLoss);
                float newDurability = newItemStack.Durability;

                // 更新槽位中的物品
                _quickAccess.Slots[slotIndex] = slot.WithItem(newItemStack);
                _quickAccess.InvalidateCache();

                // 发布耐久度变化事件
                EventBus.Publish(new ItemDurabilityChangedEvent
                {
                    ContainerId = _quickAccess.ContainerId,
                    SlotIndex = slotIndex,
                    ItemId = itemStack.ItemId,
                    OldDurability = oldDurability,
                    NewDurability = newDurability,
                    DurabilityPercentage = newDurability
                });

                // 检查物品是否损坏
                if (newDurability <= 0f)
                {
                    // 发布物品损坏事件
                    EventBus.Publish(new ItemBrokenEvent
                    {
                        ContainerId = _quickAccess.ContainerId,
                        SlotIndex = slotIndex,
                        ItemId = itemStack.ItemId,
                        BrokenItemStack = newItemStack
                    });

                    // 如果配置了损坏时销毁，则移除物品
                    if (definition.DestroyOnZeroDurability)
                    {
                        _quickAccess.Slots[slotIndex] = slot.Clear();
                        itemConsumed = true;
                    }
                }
            }
            else
            {
                // 没有耐久度系统，按原逻辑处理
                if (definition.MaxStackSize > 1)
                {
                    // 消耗品，减少数量
                    _quickAccess.Slots[slotIndex] = slot.ChangeQuantity(-1);
                    itemConsumed = true;
                }
                else
                {
                    // 非堆叠物品，直接移除
                    _quickAccess.Slots[slotIndex] = slot.Clear();
                    itemConsumed = true;
                }
                _quickAccess.InvalidateCache();
            }

            // 发布物品使用事件
            EventBus.Publish(new ItemUsedEvent
            {
                ItemId = itemStack.ItemId,
                AmountUsed = 1,
                SlotIndex = slotIndex,
                ContainerId = _quickAccess.ContainerId
            });

            // 发布背包改变事件
            EventBus.Publish(new InventoryChangedEvent
            {
                ContainerId = _quickAccess.ContainerId
            });

            return true;
        }

        /// <summary>移动物品（拖拽操作）</summary>
        public bool TryMoveItem(string sourceContainerId, int sourceSlotIndex,
                                string targetContainerId, int targetSlotIndex)
        {
            InventoryContainer sourceContainer = GetContainerById(sourceContainerId);
            InventoryContainer targetContainer = GetContainerById(targetContainerId);

            if (sourceContainer == null || targetContainer == null)
                return false;

            // 暂时实现为在同一容器内移动
            if (sourceContainerId == targetContainerId)
            {
                return sourceContainer.TryMoveItem(sourceSlotIndex, targetSlotIndex);
            }

            // 跨容器移动逻辑
            var sourceSlot = sourceContainer.Slots[sourceSlotIndex];
            var targetSlot = targetContainer.Slots[targetSlotIndex];

            if (sourceSlot.IsEmpty) return false;

            var sourceItem = sourceSlot.ItemStack;

            // 情况1：目标槽位为空，直接移动
            if (targetSlot.IsEmpty)
            {
                if (!targetSlot.CanAcceptItem(sourceItem)) return false;

                targetContainer.Slots[targetSlotIndex] = targetSlot.WithItem(sourceItem);
                sourceContainer.Slots[sourceSlotIndex] = sourceSlot.Clear();

                targetContainer.InvalidateCache();
                sourceContainer.InvalidateCache();

                // 发布移动事件（注意：这里需要两个事件或扩展事件结构）
                EventBus.Publish(new ItemMovedInInventoryEvent
                {
                    ContainerId = sourceContainerId,
                    FromSlotIndex = sourceSlotIndex,
                    ToSlotIndex = targetSlotIndex,
                    ItemStack = sourceItem
                });

                return true;
            }

            var targetItem = targetSlot.ItemStack;

            // 情况2：相同物品可堆叠
            if (sourceItem.CanStackWith(targetItem))
            {
                var merged = targetItem.MergeWith(sourceItem, out var overflow);
                targetContainer.Slots[targetSlotIndex] = targetSlot.WithItem(merged);

                if (overflow.IsEmpty)
                    sourceContainer.Slots[sourceSlotIndex] = sourceSlot.Clear();
                else
                    sourceContainer.Slots[sourceSlotIndex] = sourceSlot.WithItem(overflow);

                targetContainer.InvalidateCache();
                sourceContainer.InvalidateCache();

                EventBus.Publish(new ItemMovedInInventoryEvent
                {
                    ContainerId = sourceContainerId,
                    FromSlotIndex = sourceSlotIndex,
                    ToSlotIndex = targetSlotIndex,
                    ItemStack = sourceItem
                });

                return true;
            }

            // 情况3：交换物品
            if (!sourceSlot.CanAcceptItem(targetItem) || !targetSlot.CanAcceptItem(sourceItem))
                return false;

            // 交换物品
            targetContainer.Slots[targetSlotIndex] = targetSlot.WithItem(sourceItem);
            sourceContainer.Slots[sourceSlotIndex] = sourceSlot.WithItem(targetItem);

            targetContainer.InvalidateCache();
            sourceContainer.InvalidateCache();

            // 注意：交换需要两个移动事件，这里先发布一个
            EventBus.Publish(new ItemMovedInInventoryEvent
            {
                ContainerId = sourceContainerId,
                FromSlotIndex = sourceSlotIndex,
                ToSlotIndex = targetSlotIndex,
                ItemStack = sourceItem
            });

            return true;
        }

        /// <summary>获取指定物品的总数量</summary>
        public int GetTotalItemCount(string itemId)
        {
            int count = _mainInventory.GetItemCount(itemId);
            count += _quickAccess.GetItemCount(itemId);
            return count;
        }

        /// <summary>选择快捷栏槽位</summary>
        public void SelectQuickAccessSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _quickAccess.Capacity)
                return;

            _selectedQuickAccessSlot = slotIndex;
            // 可以发布事件通知UI更新选中状态
        }

        /// <summary>获取当前选中的快捷栏物品</summary>
        public ItemStack GetSelectedQuickAccessItem()
        {
            var slot = _quickAccess.Slots[_selectedQuickAccessSlot];
            return slot.ItemStack;
        }

        /// <summary>对指定容器进行排序</summary>
        public bool SortContainer(string containerId, SortType sortType)
        {
            var container = GetContainerById(containerId);
            if (container == null) return false;

            bool success = container.Sort(sortType);
            if (success)
            {
                EventBus.Publish(new InventorySortedEvent
                {
                    ContainerId = containerId,
                    SortType = sortType
                });
            }

            return success;
        }

        /// <summary>对主背包进行排序</summary>
        public bool SortMainInventory(SortType sortType)
        {
            return SortContainer(_mainInventory.ContainerId, sortType);
        }

        /// <summary>对快捷栏进行排序</summary>
        public bool SortQuickAccess(SortType sortType)
        {
            return SortContainer(_quickAccess.ContainerId, sortType);
        }

        /// <summary>扩展主背包容量</summary>
        public bool ExpandMainInventory(int additionalSlots)
        {
            int oldCapacity = _mainInventory.Capacity;
            bool success = _mainInventory.ExpandCapacity(additionalSlots);

            if (success)
            {
                EventBus.Publish(new InventoryCapacityExpandedEvent
                {
                    ContainerId = _mainInventory.ContainerId,
                    OldCapacity = oldCapacity,
                    NewCapacity = _mainInventory.Capacity
                });

                EventBus.Publish(new InventoryChangedEvent
                {
                    ContainerId = _mainInventory.ContainerId
                });
            }

            return success;
        }

        /// <summary>扩展快捷栏容量</summary>
        public bool ExpandQuickAccess(int additionalSlots)
        {
            int oldCapacity = _quickAccess.Capacity;
            bool success = _quickAccess.ExpandCapacity(additionalSlots);

            if (success)
            {
                EventBus.Publish(new InventoryCapacityExpandedEvent
                {
                    ContainerId = _quickAccess.ContainerId,
                    OldCapacity = oldCapacity,
                    NewCapacity = _quickAccess.Capacity
                });

                EventBus.Publish(new InventoryChangedEvent
                {
                    ContainerId = _quickAccess.ContainerId
                });
            }

            return success;
        }

        // ============ 扩展系统API ============

        /// <summary>执行扩展配置（完整流程）</summary>
        public bool ExecuteExpansion(ExpansionDefinitionSO expansionDefinition)
        {
            if (expansionDefinition == null)
                return false;

            string containerId = GetTargetContainerId(expansionDefinition);
            int currentCapacity = GetContainerById(containerId)?.Capacity ?? 0;

            // 1. 发布验证开始事件
            EventBus.Publish(new InventoryExpansionValidationStartedEvent
            {
                ExpansionId = expansionDefinition.ExpansionId,
                ContainerId = containerId,
                CurrentCapacity = currentCapacity,
                TargetCapacity = currentCapacity + expansionDefinition.GetTotalAdditionalSlots()
            });

            // 2. 验证条件
            var validationService = ServiceLocator.Get<IExpansionValidationService>();
            if (validationService == null)
            {
                Debug.LogError("[InventorySystem] IExpansionValidationService not found");
                return false;
            }

            var (allMet, validationResults) = validationService.ValidateExpansion(expansionDefinition);

            // 3. 发布验证结果事件
            EventBus.Publish(new InventoryExpansionValidationResultEvent
            {
                ExpansionId = expansionDefinition.ExpansionId,
                ContainerId = containerId,
                AllConditionsMet = allMet,
                TotalConditions = expansionDefinition.Conditions?.Length ?? 0,
                MetConditions = allMet ? (expansionDefinition.Conditions?.Length ?? 0) :
                    (expansionDefinition.Conditions?.Length ?? 0) - validationResults.Count,
                FailedResults = validationResults,
                FailureSummary = allMet ? string.Empty : validationService.GetFailedConditionDescriptions(validationResults)
            });

            if (!allMet)
                return false;

            // 4. 执行资源消耗
            var consumptionService = ServiceLocator.Get<IExpansionConsumptionService>();
            if (consumptionService == null)
            {
                Debug.LogError("[InventorySystem] IExpansionConsumptionService not found");
                return false;
            }

            EventBus.Publish(new InventoryExpansionConsumptionStartedEvent
            {
                ExpansionId = expansionDefinition.ExpansionId,
                ContainerId = containerId,
                TotalResourcesToConsume = GetTotalResourcesToConsume(expansionDefinition)
            });

            var (allSucceeded, consumptionResults) = consumptionService.ConsumeExpansion(expansionDefinition);

            EventBus.Publish(new InventoryExpansionConsumptionResultEvent
            {
                ExpansionId = expansionDefinition.ExpansionId,
                ContainerId = containerId,
                AllConsumptionsSucceeded = allSucceeded,
                TotalConsumptions = GetTotalConsumptionCount(expansionDefinition),
                SucceededConsumptions = allSucceeded ? GetTotalConsumptionCount(expansionDefinition) :
                    GetTotalConsumptionCount(expansionDefinition) - consumptionResults.Count,
                FailedResults = consumptionResults,
                FailureSummary = allSucceeded ? string.Empty : consumptionService.GetFailedConsumptionDescriptions(consumptionResults)
            });

            if (!allSucceeded)
                return false;

            // 5. 应用扩展效果
            EventBus.Publish(new InventoryExpansionEffectStartedEvent
            {
                ExpansionId = expansionDefinition.ExpansionId,
                ContainerId = containerId,
                CurrentCapacity = currentCapacity,
                NewCapacity = currentCapacity + expansionDefinition.GetTotalAdditionalSlots(),
                ExpansionName = expansionDefinition.DisplayName
            });

            var effectService = ServiceLocator.Get<IExpansionEffectService>();
            if (effectService == null)
            {
                Debug.LogError("[InventorySystem] IExpansionEffectService not found");
                return false;
            }

            bool effectApplied = effectService.ApplyExpansionEffect(expansionDefinition);

            if (effectApplied)
            {
                // 记录扩展完成
                _expansionStateManager.RecordExpansionCompleted(
                    expansionDefinition.ExpansionId,
                    containerId,
                    expansionDefinition.CooldownSeconds
                );

                // 发布完成事件
                int newCapacity = GetContainerById(containerId)?.Capacity ?? currentCapacity;
                EventBus.Publish(new InventoryExpansionEffectAppliedEvent
                {
                    ExpansionId = expansionDefinition.ExpansionId,
                    ContainerId = containerId,
                    OldCapacity = currentCapacity,
                    NewCapacity = newCapacity,
                    CapacityIncrease = newCapacity - currentCapacity,
                    ExpansionName = expansionDefinition.DisplayName,
                    CompletionTime = DateTime.Now
                });

                EventBus.Publish(new InventoryExpansionCompletedEvent
                {
                    ExpansionId = expansionDefinition.ExpansionId,
                    ContainerId = containerId,
                    OldCapacity = currentCapacity,
                    NewCapacity = newCapacity,
                    ExpansionName = expansionDefinition.DisplayName,
                    CompletionTime = DateTime.Now,
                    TotalResourcesConsumed = GetTotalConsumedResourcesCount(expansionDefinition)
                });

                // 更新UI状态
                EventBus.Publish(new InventoryExpansionStatusUpdatedEvent
                {
                    ExpansionId = expansionDefinition.ExpansionId,
                    ContainerId = containerId,
                    Status = ExpansionStatus.Complete,
                    ProgressPercentage = 1f,
                    StatusMessage = "扩展完成",
                    IsComplete = true
                });
            }
            else
            {
                EventBus.Publish(new InventoryExpansionEffectFailedEvent
                {
                    ExpansionId = expansionDefinition.ExpansionId,
                    ContainerId = containerId,
                    FailureReason = "扩展效果应用失败",
                    CanRetry = true,
                    TechnicalError = "IExpansionEffectService.ApplyExpansionEffect returned false"
                });

                EventBus.Publish(new InventoryExpansionStatusUpdatedEvent
                {
                    ExpansionId = expansionDefinition.ExpansionId,
                    ContainerId = containerId,
                    Status = ExpansionStatus.Failed,
                    ProgressPercentage = 0f,
                    StatusMessage = "扩展失败",
                    IsComplete = false
                });
            }

            return effectApplied;
        }

        /// <summary>检查扩展是否可用</summary>
        public (bool Available, string Reason) CheckExpansionAvailability(ExpansionDefinitionSO expansionDefinition)
        {
            if (expansionDefinition == null)
                return (false, "扩展配置为空");

            string containerId = GetTargetContainerId(expansionDefinition);

            // 1. 检查扩展状态
            var state = _expansionStateManager.GetExpansionState(expansionDefinition.ExpansionId);
            if (state != null)
            {
                // 检查是否已达到最大重复次数
                if (_expansionStateManager.IsMaxRepeatReached(expansionDefinition.ExpansionId, expansionDefinition.MaxRepeatCount))
                    return (false, $"已达到最大扩展次数 ({expansionDefinition.MaxRepeatCount})");

                // 检查冷却时间
                if (!state.IsAvailable(DateTime.Now))
                {
                    float remainingSeconds = state.GetRemainingCooldownSeconds(DateTime.Now);
                    return (false, $"冷却时间未结束 ({Mathf.CeilToInt(remainingSeconds)}秒)");
                }
            }

            // 2. 检查条件
            var validationService = ServiceLocator.Get<IExpansionValidationService>();
            if (validationService == null)
                return (false, "验证服务不可用");

            var (allMet, validationResults) = validationService.ValidateExpansion(expansionDefinition);
            if (!allMet)
            {
                string reason = validationService.GetFailedConditionDescriptions(validationResults);
                return (false, reason);
            }

            // 3. 检查前置扩展
            if (expansionDefinition.PrerequisiteExpansionIds != null && expansionDefinition.PrerequisiteExpansionIds.Length > 0)
            {
                foreach (var prerequisiteId in expansionDefinition.PrerequisiteExpansionIds)
                {
                    if (!_expansionStateManager.IsExpansionCompleted(prerequisiteId))
                        return (false, $"需要先完成扩展：{prerequisiteId}");
                }
            }

            // 4. 检查互斥扩展
            if (expansionDefinition.MutuallyExclusive && expansionDefinition.ExclusiveExpansionIds != null)
            {
                foreach (var exclusiveId in expansionDefinition.ExclusiveExpansionIds)
                {
                    if (_expansionStateManager.IsExpansionCompleted(exclusiveId))
                        return (false, $"与扩展冲突：{exclusiveId}");
                }
            }

            return (true, "可用");
        }

        /// <summary>获取扩展进度信息</summary>
        public ExpansionProgress GetExpansionProgress(string expansionId)
        {
            return _expansionStateManager.GetExpansionProgress(expansionId);
        }

        /// <summary>注册扩展配置</summary>
        public void RegisterExpansion(ExpansionDefinitionSO expansionDefinition)
        {
            if (expansionDefinition == null || string.IsNullOrEmpty(expansionDefinition.ExpansionId))
                return;

            _loadedExpansions[expansionDefinition.ExpansionId] = expansionDefinition;
            _expansionStateManager.SetExpansionMaxLevel(expansionDefinition.ExpansionId, expansionDefinition.ExpansionLevel);
        }

        /// <summary>获取已注册的扩展配置</summary>
        public ExpansionDefinitionSO GetExpansionDefinition(string expansionId)
        {
            _loadedExpansions.TryGetValue(expansionId, out var definition);
            return definition;
        }

        /// <summary>获取所有可用的扩展配置</summary>
        public List<ExpansionDefinitionSO> GetAvailableExpansions(string containerId = null)
        {
            var available = new List<ExpansionDefinitionSO>();
            foreach (var kvp in _loadedExpansions)
            {
                var definition = kvp.Value;
                bool containerMatches = containerId == null ||
                    GetTargetContainerId(definition) == containerId ||
                    definition.TargetContainer == ExpansionTargetContainer.Both;

                if (containerMatches)
                {
                    var (availableResult, _) = CheckExpansionAvailability(definition);
                    if (availableResult)
                        available.Add(definition);
                }
            }

            // 按排序顺序排序
            available.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
            return available;
        }

        /// <summary>获取扩展状态</summary>
        public ExpansionStatus GetExpansionStatus(string expansionId)
        {
            var progress = GetExpansionProgress(expansionId);
            if (progress.IsCompleted)
                return ExpansionStatus.Complete;

            var definition = GetExpansionDefinition(expansionId);
            if (definition == null)
                return ExpansionStatus.Unavailable;

            var (available, _) = CheckExpansionAvailability(definition);
            return available ? ExpansionStatus.Idle : ExpansionStatus.Unavailable;
        }

        // ============ 内部工具方法 ============

        /// <summary>获取目标容器ID</summary>
        private string GetTargetContainerId(ExpansionDefinitionSO expansionDefinition)
        {
            switch (expansionDefinition.TargetContainer)
            {
                case ExpansionTargetContainer.MainInventory:
                    return _mainInventory.ContainerId;
                case ExpansionTargetContainer.QuickAccess:
                    return _quickAccess.ContainerId;
                case ExpansionTargetContainer.Both:
                    // 对于两者都扩展的情况，默认返回主背包ID
                    return _mainInventory.ContainerId;
                default:
                    return expansionDefinition.SpecificContainerId ?? _mainInventory.ContainerId;
            }
        }

        /// <summary>计算需要消耗的总资源数量</summary>
        private int GetTotalResourcesToConsume(ExpansionDefinitionSO expansionDefinition)
        {
            int total = 0;
            if (expansionDefinition.Conditions != null)
            {
                foreach (var condition in expansionDefinition.Conditions)
                {
                    if (condition is ResourceConsumptionCondition resourceCondition)
                    {
                        foreach (var requirement in resourceCondition.Requirements)
                        {
                            if (requirement.ConsumeOnSuccess)
                                total += requirement.RequiredQuantity;
                        }
                    }
                }
            }
            return total;
        }

        /// <summary>计算总消耗次数</summary>
        private int GetTotalConsumptionCount(ExpansionDefinitionSO expansionDefinition)
        {
            int count = 0;
            if (expansionDefinition.Conditions != null)
            {
                foreach (var condition in expansionDefinition.Conditions)
                {
                    if (condition is ResourceConsumptionCondition resourceCondition)
                    {
                        count += resourceCondition.Requirements.Length;
                    }
                }
            }
            return count;
        }

        /// <summary>计算已消耗的资源总数</summary>
        private int GetTotalConsumedResourcesCount(ExpansionDefinitionSO expansionDefinition)
        {
            // 与GetTotalResourcesToConsume相同，因为成功时所有需要消耗的资源都会被消耗
            return GetTotalResourcesToConsume(expansionDefinition);
        }

        /// <summary>根据扩展效果执行容量扩展</summary>
        /// <returns>是否成功</returns>
        private bool ApplyExpansionEffectsToContainer(ExpansionDefinitionSO expansionDefinition, InventoryContainer container)
        {
            if (container == null || expansionDefinition?.Effects == null)
                return false;

            bool anyEffectApplied = false;
            foreach (var effect in expansionDefinition.Effects)
            {
                switch (effect.EffectType)
                {
                    case ExpansionType.CapacityIncrease:
                        if (effect.AdditionalSlots > 0)
                        {
                            bool success = container.ExpandCapacity(effect.AdditionalSlots);
                            if (success)
                                anyEffectApplied = true;
                        }
                        break;

                    case ExpansionType.WeightLimitIncrease:
                        // TODO: 实现重量限制增加
                        // 需要InventoryContainer支持设置最大重量
                        break;

                    case ExpansionType.SlotTypeUpgrade:
                        // TODO: 实现槽位类型升级
                        break;

                    case ExpansionType.SpecialSlotAddition:
                        // TODO: 实现特殊槽位添加
                        break;
                }
            }

            return anyEffectApplied;
        }

        /// <summary>获取主背包的当前重量</summary>
        public float GetMainInventoryWeight()
        {
            return _mainInventory.CalculateTotalWeight();
        }

        /// <summary>获取主背包的最大重量限制</summary>
        public float GetMainInventoryMaxWeight()
        {
            return _mainInventory.GetMaxWeight();
        }

        /// <summary>获取主背包是否超重</summary>
        public bool IsMainInventoryOverweight()
        {
            return _mainInventory.IsOverweight();
        }

        /// <summary>获取快捷栏的当前重量</summary>
        public float GetQuickAccessWeight()
        {
            return _quickAccess.CalculateTotalWeight();
        }

        /// <summary>获取快捷栏的最大重量限制</summary>
        public float GetQuickAccessMaxWeight()
        {
            return _quickAccess.GetMaxWeight();
        }

        /// <summary>获取快捷栏是否超重</summary>
        public bool IsQuickAccessOverweight()
        {
            return _quickAccess.IsOverweight();
        }

        /// <summary>更新并发布重量事件</summary>
        private void UpdateAndPublishWeightEvents(string containerId)
        {
            var container = GetContainerById(containerId);
            if (container == null) return;

            EventBus.Publish(new InventoryWeightUpdatedEvent
            {
                ContainerId = containerId,
                CurrentWeight = container.CalculateTotalWeight(),
                MaxWeight = container.GetMaxWeight()
            });
        }

        // ============ 工具方法 ============
        private InventoryContainer GetContainerById(string containerId)
        {
            if (containerId == _mainInventory.ContainerId)
                return _mainInventory;
            if (containerId == _quickAccess.ContainerId)
                return _quickAccess;
            return null;
        }

        // ============ ISaveable实现 ============
        public object CaptureState()
        {
            var state = new InventorySystemState
            {
                MainInventoryState = _mainInventory.CaptureState(),
                QuickAccessState = _quickAccess.CaptureState(),
                SelectedQuickAccessSlot = _selectedQuickAccessSlot,
                ExpansionStates = _expansionStateManager?.CaptureState()
            };
            return state;
        }

        public void RestoreState(object state)
        {
            if (!(state is InventorySystemState systemState))
                return;

            _mainInventory.RestoreState(systemState.MainInventoryState);
            _quickAccess.RestoreState(systemState.QuickAccessState);
            _selectedQuickAccessSlot = systemState.SelectedQuickAccessSlot;

            if (systemState.ExpansionStates != null && _expansionStateManager != null)
            {
                _expansionStateManager.RestoreState(systemState.ExpansionStates);
            }
        }

        // ============ 内部序列化状态类 ============
        [Serializable]
        private class InventorySystemState
        {
            public object MainInventoryState;
            public object QuickAccessState;
            public int SelectedQuickAccessSlot;
            public object ExpansionStates;
        }
    }
}