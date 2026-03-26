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
    /// 🏗️ 所有容器操作逻辑集中于此，通过 IItemDataService 查询物品定义，不绕过资源管理层
    /// </summary>
    public class InventorySystem : MonoBehaviour, ISaveable, IInventorySystem
    {
        // ============ 配置字段 ============
        [Header("背包配置")]
        [SerializeField] private InventoryContainerSO _mainInventoryConfig;
        [SerializeField] private InventoryContainerSO _quickAccessConfig;

        // ============ 运行时状态 ============
        private InventoryContainer _mainInventory;
        private InventoryContainer _quickAccess;
        private int _selectedQuickAccessSlot = 0;

        // ============ 依赖服务 ============
        private IItemDataService _itemDataService;

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
            ServiceLocator.Register<IInventorySystem>(this);
        }

        private void Start()
        {
            // Start() 在所有 Awake() 之后执行，可安全获取其他系统注册的服务
            _itemDataService = ServiceLocator.Get<IItemDataService>();
            if (_itemDataService == null)
                Debug.LogWarning("[InventorySystem] IItemDataService 未注册，请在场景中添加 ItemDataService 组件");
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<InventorySystem>();
            ServiceLocator.Unregister<IInventorySystem>();
        }

        // ============ 初始化 ============
        private void InitializeContainers()
        {
            if (_mainInventoryConfig != null)
                _mainInventory = new InventoryContainer(_mainInventoryConfig);
            else
                _mainInventory = new InventoryContainer("MainInventory", 24);

            if (_quickAccessConfig != null)
                _quickAccess = new InventoryContainer(_quickAccessConfig);
            else
                _quickAccess = new InventoryContainer("QuickAccess", 10);
        }

        private void InitializeExpansionSystem()
        {
            _expansionStateManager = new ExpansionStateManager();
            _loadedExpansions = new Dictionary<string, ExpansionDefinitionSO>();

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

            // 1. 先尝试添加到快捷栏
            int remaining = quantity;
            bool quickAccessSuccess = false;
            if (quantity > 0)
                quickAccessSuccess = AddItemToContainer(ref _quickAccess, itemId, quantity, out remaining);

            // 2. 剩余物品添加到主背包
            bool mainInventorySuccess = false;
            if (remaining > 0)
                mainInventorySuccess = AddItemToContainer(ref _mainInventory, itemId, remaining, out remaining);

            bool success = remaining < quantity;

            if (success)
            {
                int amountAdded = quantity - remaining;
                EventBus.Publish(new ItemAddedToInventoryEvent
                {
                    ItemId = itemId,
                    Amount = amountAdded,
                    SlotIndex = -1,
                    ContainerId = quickAccessSuccess ? _quickAccess.ContainerId : _mainInventory.ContainerId
                });

                EventBus.Publish(new InventoryChangedEvent
                {
                    ContainerId = _mainInventory.ContainerId
                });
            }
            else if (remaining == quantity)
            {
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

            int remaining = quantity;

            // 1. 先从快捷栏移除
            bool quickAccessSuccess = RemoveItemFromContainer(ref _quickAccess, itemId, quantity, out int quickRemaining);
            if (quickAccessSuccess)
            {
                remaining = quickRemaining;
                EventBus.Publish(new ItemRemovedFromInventoryEvent
                {
                    ItemId = itemId,
                    Amount = quantity - remaining,
                    ContainerId = _quickAccess.ContainerId
                });
            }

            // 2. 再从主背包移除
            if (remaining > 0)
            {
                bool mainSuccess = RemoveItemFromContainer(ref _mainInventory, itemId, remaining, out int mainRemaining);
                if (mainSuccess)
                {
                    EventBus.Publish(new ItemRemovedFromInventoryEvent
                    {
                        ItemId = itemId,
                        Amount = remaining - mainRemaining,
                        ContainerId = _mainInventory.ContainerId
                    });
                    remaining = mainRemaining;
                }
            }

            bool success = remaining < quantity;

            if (success)
            {
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
            var definition = _itemDataService?.GetItemDefinition(itemStack.ItemId);
            if (definition == null) return false;

            if (!definition.CanUse(user)) return false;

            definition.OnUse(user);

            bool itemConsumed = false;
            float oldDurability = itemStack.Durability;

            if (definition.HasDurability && definition.MaxDurability > 0)
            {
                float durabilityLoss = definition.DurabilityConsumptionPerUse / definition.MaxDurability;
                var newItemStack = itemStack.ConsumeDurability(durabilityLoss);
                float newDurability = newItemStack.Durability;

                _quickAccess.Slots[slotIndex] = slot.WithItem(newItemStack);
                _quickAccess.InvalidateCache();

                EventBus.Publish(new ItemDurabilityChangedEvent
                {
                    ContainerId = _quickAccess.ContainerId,
                    SlotIndex = slotIndex,
                    ItemId = itemStack.ItemId,
                    OldDurability = oldDurability,
                    NewDurability = newDurability,
                    DurabilityPercentage = newDurability
                });

                if (newDurability <= 0f)
                {
                    EventBus.Publish(new ItemBrokenEvent
                    {
                        ContainerId = _quickAccess.ContainerId,
                        SlotIndex = slotIndex,
                        ItemId = itemStack.ItemId,
                        BrokenItemStack = newItemStack
                    });

                    if (definition.DestroyOnZeroDurability)
                    {
                        _quickAccess.Slots[slotIndex] = slot.Clear();
                        itemConsumed = true;
                    }
                }
            }
            else
            {
                if (definition.MaxStackSize > 1)
                {
                    _quickAccess.Slots[slotIndex] = slot.ChangeQuantity(-1);
                    itemConsumed = true;
                }
                else
                {
                    _quickAccess.Slots[slotIndex] = slot.Clear();
                    itemConsumed = true;
                }
                _quickAccess.InvalidateCache();
            }

            EventBus.Publish(new ItemUsedEvent
            {
                ItemId = itemStack.ItemId,
                AmountUsed = 1,
                SlotIndex = slotIndex,
                ContainerId = _quickAccess.ContainerId
            });

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
            if (sourceContainerId == targetContainerId)
            {
                // 同容器内移动
                if (sourceContainerId == _mainInventory.ContainerId)
                    return MoveItemInContainer(ref _mainInventory, sourceSlotIndex, targetSlotIndex);
                if (sourceContainerId == _quickAccess.ContainerId)
                    return MoveItemInContainer(ref _quickAccess, sourceSlotIndex, targetSlotIndex);
                return false;
            }

            // 跨容器移动
            ref InventoryContainer source = ref GetContainerRef(sourceContainerId, out bool sourceValid);
            ref InventoryContainer target = ref GetContainerRef(targetContainerId, out bool targetValid);
            if (!sourceValid || !targetValid) return false;

            return MoveItemBetweenContainers(ref source, sourceSlotIndex, ref target, targetSlotIndex);
        }

        /// <summary>获取指定物品的总数量</summary>
        public int GetTotalItemCount(string itemId)
        {
            return _mainInventory.GetItemCount(itemId) + _quickAccess.GetItemCount(itemId);
        }

        /// <summary>选择快捷栏槽位</summary>
        public void SelectQuickAccessSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _quickAccess.Capacity)
                return;

            _selectedQuickAccessSlot = slotIndex;
        }

        /// <summary>选择快捷栏（IInventorySystem 接口）</summary>
        public void SelectQuickSlot(int slotIndex) => SelectQuickAccessSlot(slotIndex);

        /// <summary>获取当前选中的快捷栏物品</summary>
        public ItemStack GetSelectedQuickAccessItem()
        {
            return _quickAccess.Slots[_selectedQuickAccessSlot].ItemStack;
        }

        /// <summary>对指定容器进行排序</summary>
        public bool SortContainer(string containerId, SortType sortType)
        {
            if (containerId == _mainInventory.ContainerId)
            {
                bool success = SortContainerInternal(ref _mainInventory, sortType);
                if (success) EventBus.Publish(new InventorySortedEvent { ContainerId = containerId, SortType = sortType });
                return success;
            }
            if (containerId == _quickAccess.ContainerId)
            {
                bool success = SortContainerInternal(ref _quickAccess, sortType);
                if (success) EventBus.Publish(new InventorySortedEvent { ContainerId = containerId, SortType = sortType });
                return success;
            }
            return false;
        }

        public bool SortMainInventory(SortType sortType) => SortContainer(_mainInventory.ContainerId, sortType);
        public bool SortQuickAccess(SortType sortType) => SortContainer(_quickAccess.ContainerId, sortType);

        /// <summary>扩展主背包容量</summary>
        public bool ExpandMainInventory(int additionalSlots)
        {
            int oldCapacity = _mainInventory.Capacity;
            bool success = ExpandContainerCapacity(ref _mainInventory, additionalSlots);

            if (success)
            {
                EventBus.Publish(new InventoryCapacityExpandedEvent
                {
                    ContainerId = _mainInventory.ContainerId,
                    OldCapacity = oldCapacity,
                    NewCapacity = _mainInventory.Capacity
                });
                EventBus.Publish(new InventoryChangedEvent { ContainerId = _mainInventory.ContainerId });
            }

            return success;
        }

        /// <summary>扩展快捷栏容量</summary>
        public bool ExpandQuickAccess(int additionalSlots)
        {
            int oldCapacity = _quickAccess.Capacity;
            bool success = ExpandContainerCapacity(ref _quickAccess, additionalSlots);

            if (success)
            {
                EventBus.Publish(new InventoryCapacityExpandedEvent
                {
                    ContainerId = _quickAccess.ContainerId,
                    OldCapacity = oldCapacity,
                    NewCapacity = _quickAccess.Capacity
                });
                EventBus.Publish(new InventoryChangedEvent { ContainerId = _quickAccess.ContainerId });
            }

            return success;
        }

        // ============ 重量相关 ============

        public WeightInfo GetWeightInfo()
        {
            return new WeightInfo
            {
                CurrentWeight = CalculateContainerWeight(_mainInventory) + CalculateContainerWeight(_quickAccess),
                MaxWeight = _mainInventory.HasWeightLimit ? _mainInventory.MaxWeight : float.MaxValue
            };
        }

        public float GetMainInventoryWeight() => CalculateContainerWeight(_mainInventory);
        public float GetMainInventoryMaxWeight() => _mainInventory.HasWeightLimit ? _mainInventory.MaxWeight : float.MaxValue;
        public bool IsMainInventoryOverweight() => CalculateContainerWeight(_mainInventory) > GetMainInventoryMaxWeight();
        public float GetQuickAccessWeight() => CalculateContainerWeight(_quickAccess);
        public float GetQuickAccessMaxWeight() => _quickAccess.HasWeightLimit ? _quickAccess.MaxWeight : float.MaxValue;
        public bool IsQuickAccessOverweight() => CalculateContainerWeight(_quickAccess) > GetQuickAccessMaxWeight();

        // ============ IInventorySystem 接口实现 ============

        public InventoryData[] GetInventoryData()
        {
            var data = new InventoryData[_mainInventory.Capacity];
            for (int i = 0; i < _mainInventory.Capacity; i++)
            {
                var slot = _mainInventory.Slots[i];
                data[i] = new InventoryData
                {
                    SlotIndex = i,
                    ItemId = slot.ItemStack.ItemId,
                    Amount = slot.ItemStack.Quantity,
                    Durability = slot.ItemStack.Durability
                };
            }
            return data;
        }

        public QuickSlotData[] GetQuickSlotData()
        {
            var data = new QuickSlotData[_quickAccess.Capacity];
            for (int i = 0; i < _quickAccess.Capacity; i++)
            {
                var slot = _quickAccess.Slots[i];
                data[i] = new QuickSlotData
                {
                    SlotIndex = i,
                    ItemId = slot.ItemStack.ItemId,
                    Amount = slot.ItemStack.Quantity,
                    Durability = slot.ItemStack.Durability
                };
            }
            return data;
        }

        public void MoveItem(SlotType sourceType, int sourceIndex, SlotType targetType, int targetIndex)
        {
            string sourceContainerId = GetContainerIdBySlotType(sourceType);
            string targetContainerId = GetContainerIdBySlotType(targetType);
            TryMoveItem(sourceContainerId, sourceIndex, targetContainerId, targetIndex);
        }

        // ============ 扩展系统API ============

        /// <summary>执行扩展配置（完整流程）</summary>
        public bool ExecuteExpansion(ExpansionDefinitionSO expansionDefinition)
        {
            if (expansionDefinition == null)
                return false;

            string containerId = GetTargetContainerId(expansionDefinition);
            int currentCapacity = GetContainerCapacityById(containerId);

            EventBus.Publish(new InventoryExpansionValidationStartedEvent
            {
                ExpansionId = expansionDefinition.ExpansionId,
                ContainerId = containerId,
                CurrentCapacity = currentCapacity,
                TargetCapacity = currentCapacity + expansionDefinition.GetTotalAdditionalSlots()
            });

            var validationService = ServiceLocator.Get<IExpansionValidationService>();
            if (validationService == null)
            {
                Debug.LogError("[InventorySystem] IExpansionValidationService not found");
                return false;
            }

            var (allMet, validationResults) = validationService.ValidateExpansion(expansionDefinition);

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
                _expansionStateManager.RecordExpansionCompleted(
                    expansionDefinition.ExpansionId,
                    containerId,
                    expansionDefinition.CooldownSeconds
                );

                int newCapacity = GetContainerCapacityById(containerId);

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

        public (bool Available, string Reason) CheckExpansionAvailability(ExpansionDefinitionSO expansionDefinition)
        {
            if (expansionDefinition == null)
                return (false, "扩展配置为空");

            string containerId = GetTargetContainerId(expansionDefinition);

            var state = _expansionStateManager.GetExpansionState(expansionDefinition.ExpansionId);
            if (state != null)
            {
                if (_expansionStateManager.IsMaxRepeatReached(expansionDefinition.ExpansionId, expansionDefinition.MaxRepeatCount))
                    return (false, $"已达到最大扩展次数 ({expansionDefinition.MaxRepeatCount})");

                if (!state.IsAvailable(DateTime.Now))
                {
                    float remainingSeconds = state.GetRemainingCooldownSeconds(DateTime.Now);
                    return (false, $"冷却时间未结束 ({Mathf.CeilToInt(remainingSeconds)}秒)");
                }
            }

            var validationService = ServiceLocator.Get<IExpansionValidationService>();
            if (validationService == null)
                return (false, "验证服务不可用");

            var (allMet, validationResults) = validationService.ValidateExpansion(expansionDefinition);
            if (!allMet)
            {
                string reason = validationService.GetFailedConditionDescriptions(validationResults);
                return (false, reason);
            }

            if (expansionDefinition.PrerequisiteExpansionIds != null && expansionDefinition.PrerequisiteExpansionIds.Length > 0)
            {
                foreach (var prerequisiteId in expansionDefinition.PrerequisiteExpansionIds)
                {
                    if (!_expansionStateManager.IsExpansionCompleted(prerequisiteId))
                        return (false, $"需要先完成扩展：{prerequisiteId}");
                }
            }

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

        public ExpansionProgress GetExpansionProgress(string expansionId) =>
            _expansionStateManager.GetExpansionProgress(expansionId);

        public void RegisterExpansion(ExpansionDefinitionSO expansionDefinition)
        {
            if (expansionDefinition == null || string.IsNullOrEmpty(expansionDefinition.ExpansionId))
                return;

            _loadedExpansions[expansionDefinition.ExpansionId] = expansionDefinition;
            _expansionStateManager.SetExpansionMaxLevel(expansionDefinition.ExpansionId, expansionDefinition.ExpansionLevel);
        }

        public ExpansionDefinitionSO GetExpansionDefinition(string expansionId)
        {
            _loadedExpansions.TryGetValue(expansionId, out var definition);
            return definition;
        }

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

            available.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
            return available;
        }

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

        // ============ ISaveable实现 ============
        public object CaptureState()
        {
            return new InventorySystemState
            {
                MainInventoryState = CaptureContainerState(_mainInventory),
                QuickAccessState = CaptureContainerState(_quickAccess),
                SelectedQuickAccessSlot = _selectedQuickAccessSlot,
                ExpansionStates = _expansionStateManager?.CaptureState()
            };
        }

        public void RestoreState(object state)
        {
            if (!(state is InventorySystemState systemState))
                return;

            RestoreContainerState(ref _mainInventory, systemState.MainInventoryState);
            RestoreContainerState(ref _quickAccess, systemState.QuickAccessState);
            _selectedQuickAccessSlot = systemState.SelectedQuickAccessSlot;

            if (systemState.ExpansionStates != null && _expansionStateManager != null)
                _expansionStateManager.RestoreState(systemState.ExpansionStates);
        }

        // ============ 私有：容器操作 Helper（原 InventoryContainer 业务方法）============

        /// <summary>向容器添加物品，通过 IItemDataService 查询 MaxStackSize</summary>
        private bool AddItemToContainer(ref InventoryContainer container, string itemId, int quantity, out int remaining)
        {
            remaining = quantity;
            if (!container.IsValid || quantity <= 0) return false;

            var definition = _itemDataService?.GetItemDefinition(itemId);
            if (definition == null)
            {
                Debug.LogWarning($"[InventorySystem] 无法找到物品定义: {itemId}");
                return false;
            }

            // 1. 尝试堆叠到已有槽位
            var slotsWithItem = container.FindSlotsWithItem(itemId);
            foreach (var slotIndex in slotsWithItem)
            {
                if (remaining <= 0) break;

                var slot = container.Slots[slotIndex];
                int spaceInStack = definition.MaxStackSize - slot.ItemStack.Quantity;
                int toAdd = Mathf.Min(remaining, spaceInStack);

                if (toAdd > 0)
                {
                    container.Slots[slotIndex] = slot.ChangeQuantity(toAdd);
                    remaining -= toAdd;
                }
            }

            // 2. 填充空槽位
            if (remaining > 0)
            {
                int emptySlotIndex = container.FindFirstEmptySlot();
                while (emptySlotIndex >= 0 && remaining > 0)
                {
                    int toAdd = Mathf.Min(remaining, definition.MaxStackSize);
                    var newItemStack = new ItemStack(itemId, toAdd);
                    container.Slots[emptySlotIndex] = new InventorySlot(emptySlotIndex).WithItem(newItemStack);

                    remaining -= toAdd;
                    container.InvalidateCache();
                    emptySlotIndex = container.FindFirstEmptySlot(emptySlotIndex + 1);
                }
            }

            return remaining < quantity;
        }

        /// <summary>从容器移除物品（不需要 IItemDataService）</summary>
        private bool RemoveItemFromContainer(ref InventoryContainer container, string itemId, int quantity, out int remaining)
        {
            remaining = quantity;
            if (!container.IsValid || quantity <= 0) return false;

            var slotsWithItem = container.FindSlotsWithItem(itemId);

            foreach (var slotIndex in slotsWithItem)
            {
                if (remaining <= 0) break;

                var slot = container.Slots[slotIndex];
                int toRemove = Mathf.Min(remaining, slot.ItemStack.Quantity);

                container.Slots[slotIndex] = toRemove == slot.ItemStack.Quantity
                    ? slot.Clear()
                    : slot.ChangeQuantity(-toRemove);

                remaining -= toRemove;
                container.InvalidateCache();
            }

            return remaining < quantity;
        }

        /// <summary>在同一容器内移动物品（拖拽）</summary>
        private bool MoveItemInContainer(ref InventoryContainer container, int fromSlotIndex, int toSlotIndex)
        {
            if (!container.IsValid ||
                fromSlotIndex < 0 || fromSlotIndex >= container.Capacity ||
                toSlotIndex < 0 || toSlotIndex >= container.Capacity)
                return false;

            var fromSlot = container.Slots[fromSlotIndex];
            var toSlot = container.Slots[toSlotIndex];

            if (fromSlot.IsEmpty) return false;

            // 情况1：目标槽位为空，直接移动
            if (toSlot.IsEmpty)
            {
                var fromDef = _itemDataService?.GetItemDefinition(fromSlot.ItemStack.ItemId);
                if (fromDef != null && !toSlot.CanAcceptItem(fromSlot.ItemStack, fromDef.Category))
                    return false;

                container.Slots[toSlotIndex] = toSlot.WithItem(fromSlot.ItemStack);
                container.Slots[fromSlotIndex] = fromSlot.Clear();
                container.InvalidateCache();

                EventBus.Publish(new ItemMovedInInventoryEvent
                {
                    ContainerId = container.ContainerId,
                    FromSlotIndex = fromSlotIndex,
                    ToSlotIndex = toSlotIndex,
                    ItemStack = fromSlot.ItemStack
                });
                return true;
            }

            var fromItem = fromSlot.ItemStack;
            var toItem = toSlot.ItemStack;
            var fromItemDef = _itemDataService?.GetItemDefinition(fromItem.ItemId);

            // 情况2：相同物品可堆叠
            if (fromItemDef != null && fromItem.CanStackWith(toItem, fromItemDef.MaxStackSize))
            {
                var merged = toItem.MergeWith(fromItem, fromItemDef.MaxStackSize, out var overflow);
                container.Slots[toSlotIndex] = toSlot.WithItem(merged);
                container.Slots[fromSlotIndex] = overflow.IsEmpty ? fromSlot.Clear() : fromSlot.WithItem(overflow);
                container.InvalidateCache();

                EventBus.Publish(new ItemMovedInInventoryEvent
                {
                    ContainerId = container.ContainerId,
                    FromSlotIndex = fromSlotIndex,
                    ToSlotIndex = toSlotIndex,
                    ItemStack = fromItem
                });
                return true;
            }

            // 情况3：交换物品
            var toItemDef = _itemDataService?.GetItemDefinition(toItem.ItemId);
            bool fromCanAcceptTo = fromItemDef == null || fromSlot.CanAcceptItem(toItem, toItemDef?.Category ?? ItemCategory.General);
            bool toCanAcceptFrom = toItemDef == null || toSlot.CanAcceptItem(fromItem, fromItemDef?.Category ?? ItemCategory.General);

            if (!fromCanAcceptTo || !toCanAcceptFrom) return false;

            container.Slots[toSlotIndex] = toSlot.WithItem(fromItem);
            container.Slots[fromSlotIndex] = fromSlot.WithItem(toItem);
            container.InvalidateCache();

            EventBus.Publish(new ItemMovedInInventoryEvent
            {
                ContainerId = container.ContainerId,
                FromSlotIndex = fromSlotIndex,
                ToSlotIndex = toSlotIndex,
                ItemStack = fromItem
            });
            return true;
        }

        /// <summary>跨容器移动物品</summary>
        private bool MoveItemBetweenContainers(ref InventoryContainer source, int sourceSlotIndex,
                                               ref InventoryContainer target, int targetSlotIndex)
        {
            if (!source.IsValid || !target.IsValid) return false;

            var sourceSlot = source.Slots[sourceSlotIndex];
            var targetSlot = target.Slots[targetSlotIndex];

            if (sourceSlot.IsEmpty) return false;

            var sourceItem = sourceSlot.ItemStack;
            var sourceDef = _itemDataService?.GetItemDefinition(sourceItem.ItemId);

            // 情况1：目标为空，直接移动
            if (targetSlot.IsEmpty)
            {
                if (sourceDef != null && !targetSlot.CanAcceptItem(sourceItem, sourceDef.Category))
                    return false;

                target.Slots[targetSlotIndex] = targetSlot.WithItem(sourceItem);
                source.Slots[sourceSlotIndex] = sourceSlot.Clear();
                target.InvalidateCache();
                source.InvalidateCache();

                EventBus.Publish(new ItemMovedInInventoryEvent
                {
                    ContainerId = source.ContainerId,
                    FromSlotIndex = sourceSlotIndex,
                    ToSlotIndex = targetSlotIndex,
                    ItemStack = sourceItem
                });
                return true;
            }

            var targetItem = targetSlot.ItemStack;

            // 情况2：相同物品可堆叠
            if (sourceDef != null && sourceItem.CanStackWith(targetItem, sourceDef.MaxStackSize))
            {
                var merged = targetItem.MergeWith(sourceItem, sourceDef.MaxStackSize, out var overflow);
                target.Slots[targetSlotIndex] = targetSlot.WithItem(merged);
                source.Slots[sourceSlotIndex] = overflow.IsEmpty ? sourceSlot.Clear() : sourceSlot.WithItem(overflow);
                target.InvalidateCache();
                source.InvalidateCache();

                EventBus.Publish(new ItemMovedInInventoryEvent
                {
                    ContainerId = source.ContainerId,
                    FromSlotIndex = sourceSlotIndex,
                    ToSlotIndex = targetSlotIndex,
                    ItemStack = sourceItem
                });
                return true;
            }

            // 情况3：交换物品
            var targetDef = _itemDataService?.GetItemDefinition(targetItem.ItemId);
            bool sourceCanAcceptTarget = sourceDef == null || sourceSlot.CanAcceptItem(targetItem, targetDef?.Category ?? ItemCategory.General);
            bool targetCanAcceptSource = targetDef == null || targetSlot.CanAcceptItem(sourceItem, sourceDef?.Category ?? ItemCategory.General);

            if (!sourceCanAcceptTarget || !targetCanAcceptSource) return false;

            target.Slots[targetSlotIndex] = targetSlot.WithItem(sourceItem);
            source.Slots[sourceSlotIndex] = sourceSlot.WithItem(targetItem);
            target.InvalidateCache();
            source.InvalidateCache();

            EventBus.Publish(new ItemMovedInInventoryEvent
            {
                ContainerId = source.ContainerId,
                FromSlotIndex = sourceSlotIndex,
                ToSlotIndex = targetSlotIndex,
                ItemStack = sourceItem
            });
            return true;
        }

        /// <summary>排序容器，通过 IItemDataService 获取物品信息</summary>
        private bool SortContainerInternal(ref InventoryContainer container, SortType sortType)
        {
            if (!container.IsValid || container.Capacity <= 1) return false;

            var nonEmptySlots = new List<(int index, InventorySlot slot)>();
            for (int i = 0; i < container.Capacity; i++)
                if (!container.Slots[i].IsEmpty)
                    nonEmptySlots.Add((i, container.Slots[i]));

            if (nonEmptySlots.Count <= 1) return false;

            switch (sortType)
            {
                case SortType.ByName:
                    nonEmptySlots.Sort((a, b) =>
                    {
                        var defA = _itemDataService?.GetItemDefinition(a.slot.ItemStack.ItemId);
                        var defB = _itemDataService?.GetItemDefinition(b.slot.ItemStack.ItemId);
                        if (defA == null && defB == null) return 0;
                        if (defA == null) return 1;
                        if (defB == null) return -1;
                        return string.Compare(defA.DisplayName, defB.DisplayName, StringComparison.Ordinal);
                    });
                    break;

                case SortType.ByQuantity:
                    nonEmptySlots.Sort((a, b) => b.slot.ItemStack.Quantity.CompareTo(a.slot.ItemStack.Quantity));
                    break;

                case SortType.ByWeight:
                    nonEmptySlots.Sort((a, b) =>
                    {
                        var defA = _itemDataService?.GetItemDefinition(a.slot.ItemStack.ItemId);
                        var defB = _itemDataService?.GetItemDefinition(b.slot.ItemStack.ItemId);
                        float wA = defA?.Weight ?? 0f;
                        float wB = defB?.Weight ?? 0f;
                        return wB.CompareTo(wA);
                    });
                    break;

                case SortType.ByType:
                    nonEmptySlots.Sort((a, b) =>
                    {
                        var defA = _itemDataService?.GetItemDefinition(a.slot.ItemStack.ItemId);
                        var defB = _itemDataService?.GetItemDefinition(b.slot.ItemStack.ItemId);
                        int catA = defA != null ? (int)defA.Category : 999;
                        int catB = defB != null ? (int)defB.Category : 999;
                        return catA.CompareTo(catB);
                    });
                    break;

                case SortType.ByRarity:
                    nonEmptySlots.Sort((a, b) =>
                    {
                        var defA = _itemDataService?.GetItemDefinition(a.slot.ItemStack.ItemId);
                        var defB = _itemDataService?.GetItemDefinition(b.slot.ItemStack.ItemId);
                        int rarA = defA != null ? (int)defA.Rarity : 0;
                        int rarB = defB != null ? (int)defB.Rarity : 0;
                        return rarB.CompareTo(rarA);
                    });
                    break;
            }

            // 重建槽位数组
            var newSlots = new InventorySlot[container.Capacity];
            for (int i = 0; i < container.Capacity; i++)
                newSlots[i] = new InventorySlot(i, container.Slots[i].SlotType, container.Slots[i].AllowedCategories);

            int newIndex = 0;
            foreach (var (_, slot) in nonEmptySlots)
            {
                newSlots[newIndex] = new InventorySlot(newIndex, slot.SlotType, slot.AllowedCategories)
                    .WithItem(slot.ItemStack);
                newIndex++;
            }

            // 将排序后的数组写回（通过 Slots 属性访问底层数组引用）
            for (int i = 0; i < container.Capacity; i++)
                container.Slots[i] = newSlots[i];
            container.InvalidateCache();

            return true;
        }

        /// <summary>扩展容器容量</summary>
        private bool ExpandContainerCapacity(ref InventoryContainer container, int additionalSlots)
        {
            if (!container.IsValid || additionalSlots <= 0) return false;

            int oldCapacity = container.Capacity;
            int newCapacity = oldCapacity + additionalSlots;

            var newSlots = new InventorySlot[newCapacity];
            for (int i = 0; i < oldCapacity; i++)
                newSlots[i] = container.Slots[i];
            for (int i = oldCapacity; i < newCapacity; i++)
                newSlots[i] = new InventorySlot(i);

            for (int i = 0; i < newCapacity; i++)
                container.Slots[i] = newSlots[i]; // 写到原数组会越界，需要替换数组

            // [NOTE] 由于 Slots 是 _slots 数组引用，新容量时需要重新分配
            // 此处通过 RestoreContainerState 实现替换
            var state = CaptureContainerStateWithNewCapacity(container, newSlots);
            RestoreContainerState(ref container, state);
            container.InvalidateCache();
            return true;
        }

        /// <summary>计算容器总重量，通过 IItemDataService 获取物品重量</summary>
        private float CalculateContainerWeight(InventoryContainer container)
        {
            if (!container.IsValid) return 0f;

            float totalWeight = 0f;
            foreach (var slot in container.Slots)
            {
                if (!slot.IsEmpty)
                {
                    var definition = _itemDataService?.GetItemDefinition(slot.ItemStack.ItemId);
                    if (definition != null)
                        totalWeight += definition.Weight * slot.ItemStack.Quantity;
                }
            }
            return totalWeight;
        }

        // ============ 私有：存档 Helper ============

        private object CaptureContainerState(InventoryContainer container)
        {
            var state = new ContainerState
            {
                ContainerId = container.ContainerId,
                SlotStates = new SlotState[container.Capacity]
            };

            for (int i = 0; i < container.Capacity; i++)
            {
                var slot = container.Slots[i];
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

        private object CaptureContainerStateWithNewCapacity(InventoryContainer container, InventorySlot[] newSlots)
        {
            var state = new ContainerState
            {
                ContainerId = container.ContainerId,
                SlotStates = new SlotState[newSlots.Length]
            };

            for (int i = 0; i < newSlots.Length; i++)
            {
                var slot = newSlots[i];
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

        private void RestoreContainerState(ref InventoryContainer container, object stateObj)
        {
            if (!(stateObj is ContainerState state)) return;

            // 通过重新赋值 struct 字段来替换内部数组
            var newSlots = new InventorySlot[state.SlotStates.Length];
            for (int i = 0; i < state.SlotStates.Length; i++)
            {
                var s = state.SlotStates[i];
                var itemStack = string.IsNullOrEmpty(s.ItemId)
                    ? ItemStack.Empty
                    : new ItemStack(s.ItemId, s.Quantity, s.Durability).WithCustomData(s.CustomDataJson);

                newSlots[i] = new InventorySlot(s.Index).WithItem(itemStack);
            }

            container = new InventoryContainer(state.ContainerId, newSlots.Length,
                container.HasWeightLimit, container.MaxWeight);
            for (int i = 0; i < newSlots.Length; i++)
                container.Slots[i] = newSlots[i];
            container.InvalidateCache();
        }

        // ============ 私有：工具方法 ============

        private ref InventoryContainer GetContainerRef(string containerId, out bool valid)
        {
            if (containerId == _mainInventory.ContainerId)
            {
                valid = true;
                return ref _mainInventory;
            }
            if (containerId == _quickAccess.ContainerId)
            {
                valid = true;
                return ref _quickAccess;
            }
            valid = false;
            return ref _mainInventory; // 调用方通过 valid=false 判断无效，不使用此引用
        }

        private int GetContainerCapacityById(string containerId)
        {
            if (containerId == _mainInventory.ContainerId) return _mainInventory.Capacity;
            if (containerId == _quickAccess.ContainerId) return _quickAccess.Capacity;
            return 0;
        }

        private string GetTargetContainerId(ExpansionDefinitionSO expansionDefinition)
        {
            switch (expansionDefinition.TargetContainer)
            {
                case ExpansionTargetContainer.MainInventory:
                    return _mainInventory.ContainerId;
                case ExpansionTargetContainer.QuickAccess:
                    return _quickAccess.ContainerId;
                case ExpansionTargetContainer.Both:
                    return _mainInventory.ContainerId;
                default:
                    return expansionDefinition.SpecificContainerId ?? _mainInventory.ContainerId;
            }
        }

        private string GetContainerIdBySlotType(SlotType slotType)
        {
            return slotType == SlotType.QuickAccess
                ? _quickAccess.ContainerId
                : _mainInventory.ContainerId;
        }

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

        private int GetTotalConsumptionCount(ExpansionDefinitionSO expansionDefinition)
        {
            int count = 0;
            if (expansionDefinition.Conditions != null)
            {
                foreach (var condition in expansionDefinition.Conditions)
                {
                    if (condition is ResourceConsumptionCondition resourceCondition)
                        count += resourceCondition.Requirements.Length;
                }
            }
            return count;
        }

        private int GetTotalConsumedResourcesCount(ExpansionDefinitionSO expansionDefinition) =>
            GetTotalResourcesToConsume(expansionDefinition);

        // ============ 内部序列化状态类 ============
        [Serializable]
        private class InventorySystemState
        {
            public object MainInventoryState;
            public object QuickAccessState;
            public int SelectedQuickAccessSlot;
            public object ExpansionStates;
        }

        [Serializable]
        private class ContainerState
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
