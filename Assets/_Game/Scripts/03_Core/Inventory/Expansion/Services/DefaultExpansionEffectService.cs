// 📁 03_Core/Inventory/Expansion/Services/DefaultExpansionEffectService.cs
// 默认扩展效果应用服务实现

using System;
using System.Collections.Generic;
using UnityEngine;
using SurvivalGame.Data.Inventory.Expansion;

namespace SurvivalGame.Core.Inventory.Expansion
{
    /// <summary>
    /// 默认扩展效果应用服务
    /// 🏗️ 架构说明：核心业务层服务，负责应用扩展效果
    /// [PERF] 使用增量更新，避免重复应用效果
    /// </summary>
    public class DefaultExpansionEffectService : MonoBehaviour, IExpansionEffectService
    {
        // ============ 配置字段 ============
        [Header("效果应用配置")]
        [SerializeField] private bool _enableEffectLogging = true;
        [SerializeField] private float _effectApplicationDelay = 0.2f; // 效果应用延迟（用于动画）

        // ============ 运行时状态 ============
        private HashSet<string> _appliedExpansionIds;
        private Dictionary<string, ExpansionDefinitionSO> _lastAppliedExpansions;
        private Dictionary<string, DateTime> _effectApplicationTimes;
        private InventorySystem _inventorySystem;

        // ============ 生命周期 ============
        private void Awake()
        {
            _appliedExpansionIds = new HashSet<string>();
            _lastAppliedExpansions = new Dictionary<string, ExpansionDefinitionSO>();
            _effectApplicationTimes = new Dictionary<string, DateTime>();

            ServiceLocator.Register<IExpansionEffectService>(this);
        }

        private void Start()
        {
            _inventorySystem = ServiceLocator.Get<InventorySystem>();
            if (_inventorySystem == null)
            {
                Debug.LogError("[DefaultExpansionEffectService] InventorySystem not found in ServiceLocator");
            }

            // 初始化时重新应用所有已记录的扩展
            ReapplyRecordedExpansions();
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<IExpansionEffectService>();
        }

        // ============ IExpansionEffectService 实现 ============

        /// <summary>应用扩展效果</summary>
        public bool ApplyExpansionEffect(ExpansionDefinitionSO expansionDefinition)
        {
            if (expansionDefinition == null)
            {
                LogError("扩展配置为空");
                return false;
            }

            string expansionId = expansionDefinition.ExpansionId;
            if (string.IsNullOrEmpty(expansionId))
            {
                LogError("扩展ID为空");
                return false;
            }

            // 检查是否已应用
            if (IsExpansionApplied(expansionId))
            {
                LogWarning($"扩展 {expansionId} 已应用，跳过重复应用");
                return true;
            }

            // 记录开始时间
            _effectApplicationTimes[expansionId] = DateTime.Now;

            // 发布效果开始应用事件
            EventBus.Publish(new ExpansionEffectApplicationStartedEvent
            {
                ExpansionId = expansionId,
                ContainerId = GetTargetContainerId(expansionDefinition),
                StartTime = DateTime.Now,
                EffectType = expansionDefinition.Effects != null && expansionDefinition.Effects.Length > 0 ?
                    expansionDefinition.Effects[0].EffectType : ExpansionType.CapacityIncrease
            });

            bool success = ApplyEffectsToContainers(expansionDefinition);

            if (success)
            {
                // 记录已应用的扩展
                _appliedExpansionIds.Add(expansionId);
                _lastAppliedExpansions[expansionId] = expansionDefinition;

                // 记录效果应用时间
                _effectApplicationTimes[expansionId] = DateTime.Now;

                // 发布效果应用成功事件
                EventBus.Publish(new ExpansionEffectAppliedEvent
                {
                    ExpansionId = expansionId,
                    ContainerId = GetTargetContainerId(expansionDefinition),
                    Success = true,
                    EffectType = expansionDefinition.Effects != null && expansionDefinition.Effects.Length > 0 ?
                        expansionDefinition.Effects[0].EffectType : ExpansionType.CapacityIncrease,
                    ApplicationTime = DateTime.Now
                });

                LogSuccess($"扩展效果应用成功: {expansionDefinition.DisplayName} ({expansionId})");
            }
            else
            {
                // 发布效果应用失败事件
                EventBus.Publish(new ExpansionEffectAppliedEvent
                {
                    ExpansionId = expansionId,
                    ContainerId = GetTargetContainerId(expansionDefinition),
                    Success = false,
                    EffectType = ExpansionType.CapacityIncrease,
                    ApplicationTime = DateTime.Now,
                    FailureReason = "效果应用失败"
                });

                LogError($"扩展效果应用失败: {expansionDefinition.DisplayName} ({expansionId})");
            }

            return success;
        }

        /// <summary>撤销扩展效果（用于回滚）</summary>
        public bool RollbackExpansionEffect(ExpansionDefinitionSO expansionDefinition)
        {
            if (expansionDefinition == null)
                return false;

            string expansionId = expansionDefinition.ExpansionId;
            if (string.IsNullOrEmpty(expansionId))
                return false;

            // 检查是否已应用
            if (!IsExpansionApplied(expansionId))
            {
                LogWarning($"尝试回滚未应用的扩展: {expansionId}");
                return false;
            }

            // 发布回滚开始事件
            EventBus.Publish(new ExpansionEffectRollbackStartedEvent
            {
                ExpansionId = expansionId,
                ContainerId = GetTargetContainerId(expansionDefinition),
                RollbackTime = DateTime.Now
            });

            bool success = RollbackEffectsFromContainers(expansionDefinition);

            if (success)
            {
                // 移除记录
                _appliedExpansionIds.Remove(expansionId);
                _lastAppliedExpansions.Remove(expansionId);
                _effectApplicationTimes.Remove(expansionId);

                // 发布回滚成功事件
                EventBus.Publish(new ExpansionEffectRollbackCompletedEvent
                {
                    ExpansionId = expansionId,
                    ContainerId = GetTargetContainerId(expansionDefinition),
                    Success = true,
                    RollbackTime = DateTime.Now
                });

                LogSuccess($"扩展效果回滚成功: {expansionDefinition.DisplayName} ({expansionId})");
            }
            else
            {
                // 发布回滚失败事件
                EventBus.Publish(new ExpansionEffectRollbackCompletedEvent
                {
                    ExpansionId = expansionId,
                    ContainerId = GetTargetContainerId(expansionDefinition),
                    Success = false,
                    RollbackTime = DateTime.Now,
                    FailureReason = "回滚失败"
                });

                LogError($"扩展效果回滚失败: {expansionDefinition.DisplayName} ({expansionId})");
            }

            return success;
        }

        /// <summary>获取扩展效果描述</summary>
        public string GetExpansionEffectDescription(ExpansionDefinitionSO expansionDefinition)
        {
            if (expansionDefinition == null)
                return "无效扩展";

            if (expansionDefinition.Effects == null || expansionDefinition.Effects.Length == 0)
                return "无效果";

            var descriptions = new List<string>();
            foreach (var effect in expansionDefinition.Effects)
            {
                descriptions.Add(GetEffectDescription(effect));
            }

            return string.Join("\n", descriptions);
        }

        /// <summary>检查扩展是否已应用</summary>
        public bool IsExpansionApplied(string expansionId)
        {
            return !string.IsNullOrEmpty(expansionId) && _appliedExpansionIds.Contains(expansionId);
        }

        // ============ 内部效果应用方法 ============

        private bool ApplyEffectsToContainers(ExpansionDefinitionSO expansionDefinition)
        {
            if (expansionDefinition == null || expansionDefinition.Effects == null)
                return false;

            if (_inventorySystem == null)
            {
                LogError("InventorySystem不可用");
                return false;
            }

            bool anyEffectApplied = false;

            foreach (var effect in expansionDefinition.Effects)
            {
                switch (effect.EffectType)
                {
                    case ExpansionType.CapacityIncrease:
                        anyEffectApplied |= ApplyCapacityIncreaseEffect(expansionDefinition, effect);
                        break;

                    case ExpansionType.WeightLimitIncrease:
                        anyEffectApplied |= ApplyWeightLimitIncreaseEffect(expansionDefinition, effect);
                        break;

                    case ExpansionType.SlotTypeUpgrade:
                        anyEffectApplied |= ApplySlotTypeUpgradeEffect(expansionDefinition, effect);
                        break;

                    case ExpansionType.SpecialSlotAddition:
                        anyEffectApplied |= ApplySpecialSlotAdditionEffect(expansionDefinition, effect);
                        break;
                }
            }

            return anyEffectApplied;
        }

        private bool ApplyCapacityIncreaseEffect(ExpansionDefinitionSO expansionDefinition, ExpansionEffect effect)
        {
            if (effect.AdditionalSlots <= 0)
                return false;

            string containerId = GetTargetContainerId(expansionDefinition);
            var container = GetContainerById(containerId);

            if (container == null)
            {
                LogError($"容器不存在: {containerId}");
                return false;
            }

            int oldCapacity = container.Capacity;
            bool success = container.ExpandCapacity(effect.AdditionalSlots);

            if (success)
            {
                LogSuccess($"容器 {containerId} 容量从 {oldCapacity} 扩展到 {container.Capacity}");
            }
            else
            {
                LogError($"容器 {containerId} 容量扩展失败");
            }

            return success;
        }

        private bool ApplyWeightLimitIncreaseEffect(ExpansionDefinitionSO expansionDefinition, ExpansionEffect effect)
        {
            // TODO: 实现重量限制增加效果
            // 需要InventoryContainer支持设置最大重量
            LogWarning($"重量限制增加效果尚未实现: {expansionDefinition.ExpansionId}");
            return false;
        }

        private bool ApplySlotTypeUpgradeEffect(ExpansionDefinitionSO expansionDefinition, ExpansionEffect effect)
        {
            // TODO: 实现槽位类型升级效果
            // 需要InventoryContainer支持设置槽位类型
            LogWarning($"槽位类型升级效果尚未实现: {expansionDefinition.ExpansionId}");
            return false;
        }

        private bool ApplySpecialSlotAdditionEffect(ExpansionDefinitionSO expansionDefinition, ExpansionEffect effect)
        {
            // TODO: 实现特殊槽位添加效果
            // 需要InventoryContainer支持添加特殊槽位
            LogWarning($"特殊槽位添加效果尚未实现: {expansionDefinition.ExpansionId}");
            return false;
        }

        private bool RollbackEffectsFromContainers(ExpansionDefinitionSO expansionDefinition)
        {
            if (expansionDefinition == null || expansionDefinition.Effects == null)
                return false;

            bool anyEffectRolledBack = false;

            foreach (var effect in expansionDefinition.Effects)
            {
                switch (effect.EffectType)
                {
                    case ExpansionType.CapacityIncrease:
                        anyEffectRolledBack |= RollbackCapacityIncreaseEffect(expansionDefinition, effect);
                        break;

                    // 其他效果类型的回滚逻辑...
                }
            }

            return anyEffectRolledBack;
        }

        private bool RollbackCapacityIncreaseEffect(ExpansionDefinitionSO expansionDefinition, ExpansionEffect effect)
        {
            if (effect.AdditionalSlots <= 0)
                return false;

            string containerId = GetTargetContainerId(expansionDefinition);
            var container = GetContainerById(containerId);

            if (container == null)
            {
                LogError($"容器不存在: {containerId}");
                return false;
            }

            // 尝试缩减容量（注意：如果容器中有物品，可能无法缩减）
            // TODO: 实现安全的容量缩减逻辑
            LogWarning($"容量增加效果的回滚尚未完全实现: {expansionDefinition.ExpansionId}");

            // 暂时只记录日志，不实际回滚
            return true;
        }

        private string GetEffectDescription(ExpansionEffect effect)
        {
            switch (effect.EffectType)
            {
                case ExpansionType.CapacityIncrease:
                    return $"增加 {effect.AdditionalSlots} 个槽位";

                case ExpansionType.WeightLimitIncrease:
                    return $"增加重量限制 {effect.WeightLimitIncrease} kg";

                case ExpansionType.SlotTypeUpgrade:
                    return $"升级槽位类型: {effect.SlotType}";

                case ExpansionType.SpecialSlotAddition:
                    return $"添加特殊槽位: {effect.SpecialSlotType}";

                default:
                    return $"未知效果: {effect.EffectType}";
            }
        }

        // ============ 初始化方法 ============

        private void ReapplyRecordedExpansions()
        {
            // 从扩展记录服务获取已完成的扩展ID
            var recordService = ServiceLocator.Get<IExpansionRecordService>();
            if (recordService == null)
                return;

            var completedExpansions = recordService.GetCompletedExpansions();
            if (completedExpansions == null)
                return;

            LogInfo($"重新应用 {completedExpansions.Count} 个已记录的扩展...");

            foreach (var expansionId in completedExpansions)
            {
                var definition = _inventorySystem?.GetExpansionDefinition(expansionId);
                if (definition != null && !IsExpansionApplied(expansionId))
                {
                    // 重新应用效果
                    ApplyExpansionEffect(definition);
                }
            }
        }

        // ============ 工具方法 ============

        private string GetTargetContainerId(ExpansionDefinitionSO expansionDefinition)
        {
            if (_inventorySystem == null)
                return string.Empty;

            // 使用反射调用私有方法，或者复制逻辑
            // 简化实现：根据扩展配置的目标容器返回容器ID
            if (expansionDefinition.TargetContainer == ExpansionTargetContainer.MainInventory)
                return _inventorySystem.MainInventory?.ContainerId ?? "MainInventory";
            else if (expansionDefinition.TargetContainer == ExpansionTargetContainer.QuickAccess)
                return _inventorySystem.QuickAccess?.ContainerId ?? "QuickAccess";
            else
                return expansionDefinition.SpecificContainerId ?? "MainInventory";
        }

        private InventoryContainer GetContainerById(string containerId)
        {
            if (_inventorySystem == null)
                return null;

            if (containerId == _inventorySystem.MainInventory?.ContainerId)
                return _inventorySystem.MainInventory;
            else if (containerId == _inventorySystem.QuickAccess?.ContainerId)
                return _inventorySystem.QuickAccess;
            else
                return null;
        }

        // ============ 日志方法 ============

        private void LogInfo(string message)
        {
            if (_enableEffectLogging)
                Debug.Log($"[DefaultExpansionEffectService] {message}");
        }

        private void LogSuccess(string message)
        {
            if (_enableEffectLogging)
                Debug.Log($"<color=green>[DefaultExpansionEffectService] {message}</color>");
        }

        private void LogWarning(string message)
        {
            if (_enableEffectLogging)
                Debug.LogWarning($"[DefaultExpansionEffectService] {message}");
        }

        private void LogError(string message)
        {
            if (_enableEffectLogging)
                Debug.LogError($"[DefaultExpansionEffectService] {message}");
        }

        // ============ 公共API（用于调试） ============

        /// <summary>获取已应用的扩展ID列表</summary>
        public IReadOnlyCollection<string> GetAppliedExpansionIds()
        {
            return _appliedExpansionIds;
        }

        /// <summary>获取扩展的应用时间</summary>
        public DateTime? GetExpansionApplicationTime(string expansionId)
        {
            if (_effectApplicationTimes.TryGetValue(expansionId, out var time))
                return time;
            return null;
        }

        /// <summary>清除所有扩展效果（用于测试）</summary>
        public void ClearAllEffects()
        {
            int count = _appliedExpansionIds.Count;
            _appliedExpansionIds.Clear();
            _lastAppliedExpansions.Clear();
            _effectApplicationTimes.Clear();

            LogInfo($"已清除所有扩展效果（共 {count} 个）");
        }
    }

    // ============ 相关事件定义 ============

    /// <summary>效果应用开始事件</summary>
    public struct ExpansionEffectApplicationStartedEvent : IEvent
    {
        public string ExpansionId;
        public string ContainerId;
        public DateTime StartTime;
        public ExpansionType EffectType;
    }

    /// <summary>效果应用事件</summary>
    public struct ExpansionEffectAppliedEvent : IEvent
    {
        public string ExpansionId;
        public string ContainerId;
        public bool Success;
        public ExpansionType EffectType;
        public DateTime ApplicationTime;
        public string FailureReason;
    }

    /// <summary>效果回滚开始事件</summary>
    public struct ExpansionEffectRollbackStartedEvent : IEvent
    {
        public string ExpansionId;
        public string ContainerId;
        public DateTime RollbackTime;
    }

    /// <summary>效果回滚完成事件</summary>
    public struct ExpansionEffectRollbackCompletedEvent : IEvent
    {
        public string ExpansionId;
        public string ContainerId;
        public bool Success;
        public DateTime RollbackTime;
        public string FailureReason;
    }
}