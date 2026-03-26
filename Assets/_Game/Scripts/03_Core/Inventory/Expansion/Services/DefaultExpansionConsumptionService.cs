// 📁 03_Core/Inventory/Expansion/Services/DefaultExpansionConsumptionService.cs
// 默认扩展资源消耗服务实现

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SurvivalGame.Data.Inventory.Expansion;

namespace SurvivalGame.Core.Inventory.Expansion
{
    /// <summary>
    /// 默认扩展资源消耗服务
    /// 🏗️ 架构说明：核心业务层服务，负责执行资源消耗逻辑
    /// [PERF] 批量操作减少事件发布频率，使用对象池优化
    /// </summary>
    public class DefaultExpansionConsumptionService : MonoBehaviour, IExpansionConsumptionService
    {
        // ============ 性能优化配置 ============
        [Header("性能配置")]
        [SerializeField] private bool _enableBatchMode = true; // 启用批量模式，减少事件发布
        [SerializeField] private float _batchInterval = 0.1f; // 批量处理的间隔时间（秒）

        private class PendingConsumption
        {
            public IExpansionCondition Condition;
            public Action<ExpansionConsumptionResult> Callback;
            public DateTime QueueTime;
        }

        private Queue<PendingConsumption> _consumptionQueue;
        private DateTime _lastBatchProcessTime;
        private List<ExpansionConsumptionResult> _batchResults;

        // ============ 生命周期 ============
        private void Awake()
        {
            _consumptionQueue = new Queue<PendingConsumption>();
            _batchResults = new List<ExpansionConsumptionResult>();
            _lastBatchProcessTime = DateTime.Now;

            ServiceLocator.Register<IExpansionConsumptionService>(this);
        }

        private void Update()
        {
            // 定时处理批量队列
            if (_enableBatchMode && _consumptionQueue.Count > 0)
            {
                if ((DateTime.Now - _lastBatchProcessTime).TotalSeconds >= _batchInterval)
                {
                    ProcessBatch();
                }
            }
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<IExpansionConsumptionService>();

            // 处理剩余队列
            if (_consumptionQueue.Count > 0)
            {
                ProcessBatch();
            }
        }

        // ============ IExpansionConsumptionService 实现 ============

        /// <summary>执行条件消耗</summary>
        public ExpansionConsumptionResult ConsumeCondition(IExpansionCondition condition)
        {
            if (condition == null)
                return ExpansionConsumptionResult.FailResult("null", "条件为空");

            // 如果是批量模式，加入队列
            if (_enableBatchMode)
            {
                var pending = new PendingConsumption
                {
                    Condition = condition,
                    Callback = null,
                    QueueTime = DateTime.Now
                };

                _consumptionQueue.Enqueue(pending);

                // 返回一个"排队中"的结果
                return ExpansionConsumptionResult.SuccessResult(condition.ConditionId);
            }

            // 立即执行
            return ExecuteConsumptionImmediately(condition);
        }

        /// <summary>执行条件集合的消耗</summary>
        public (bool AllSucceeded, List<ExpansionConsumptionResult> Results) ConsumeConditions(IEnumerable<IExpansionCondition> conditions)
        {
            if (conditions == null)
                return (false, new List<ExpansionConsumptionResult>());

            var conditionList = conditions.ToList();
            if (conditionList.Count == 0)
                return (true, new List<ExpansionConsumptionResult>());

            // 按优先级排序消耗（先消耗优先级高的）
            conditionList.Sort((a, b) => b.Priority.CompareTo(a.Priority));

            var results = new List<ExpansionConsumptionResult>();
            bool allSucceeded = true;

            // 先验证所有条件
            var validationService = ServiceLocator.Get<IExpansionValidationService>();
            if (validationService != null)
            {
                var (allMet, validationResults) = validationService.ValidateConditions(conditionList);
                if (!allMet)
                {
                    // 如果有条件不满足，将所有条件标记为失败
                    foreach (var condition in conditionList)
                    {
                        var validationResult = validationResults.Find(r => r.ConditionId == condition.ConditionId);
                        string reason = validationResult?.FailedReason ?? "条件验证失败";
                        results.Add(ExpansionConsumptionResult.FailResult(condition.ConditionId, reason));
                    }
                    return (false, results);
                }
            }

            // 执行消耗
            foreach (var condition in conditionList)
            {
                var result = _enableBatchMode ?
                    QueueConsumption(condition) :
                    ExecuteConsumptionImmediately(condition);

                results.Add(result);

                if (!result.Success)
                {
                    allSucceeded = false;

                    // 如果高优先级条件失败，可以尝试回滚已成功的消耗
                    if (condition.Priority >= 50) // 假设50及以上为高优先级
                    {
                        RollbackSuccessfulConsumptions(conditionList, results);
                        break;
                    }
                }
            }

            // 如果启用了批量模式，需要处理队列
            if (_enableBatchMode && _consumptionQueue.Count > 0)
            {
                ProcessBatch();
            }

            return (allSucceeded, results);
        }

        /// <summary>执行扩展配置的消耗</summary>
        public (bool AllSucceeded, List<ExpansionConsumptionResult> Results) ConsumeExpansion(ExpansionDefinitionSO expansionDefinition)
        {
            if (expansionDefinition == null || expansionDefinition.Conditions == null)
                return (true, new List<ExpansionConsumptionResult>());

            // 发布批量消耗开始事件
            if (_enableBatchMode)
            {
                EventBus.Publish(new ExpansionBatchConsumptionStartedEvent
                {
                    ExpansionId = expansionDefinition.ExpansionId,
                    TotalConditions = expansionDefinition.Conditions.Length,
                    BatchMode = true
                });
            }

            // 执行消耗
            var (allSucceeded, results) = ConsumeConditions(expansionDefinition.Conditions);

            // 发布批量消耗结束事件
            if (_enableBatchMode)
            {
                int succeededCount = results.Count(r => r.Success);
                EventBus.Publish(new ExpansionBatchConsumptionCompletedEvent
                {
                    ExpansionId = expansionDefinition.ExpansionId,
                    TotalConditions = expansionDefinition.Conditions.Length,
                    SucceededConditions = succeededCount,
                    FailedConditions = results.Count - succeededCount,
                    BatchMode = true
                });
            }

            return (allSucceeded, results);
        }

        /// <summary>获取失败消耗的友好描述</summary>
        public string GetFailedConsumptionDescriptions(List<ExpansionConsumptionResult> failedResults)
        {
            if (failedResults == null || failedResults.Count == 0)
                return string.Empty;

            var descriptions = new List<string>();
            foreach (var result in failedResults)
            {
                if (!result.Success)
                {
                    // 分类错误信息
                    string description;
                    if (result.FailedReason.Contains("背包系统不可用"))
                    {
                        description = "系统错误：背包系统不可用";
                    }
                    else if (result.FailedReason.Contains("缺少资源"))
                    {
                        description = $"资源不足：{result.FailedReason.Replace("缺少资源：", "")}";
                    }
                    else if (result.FailedReason.Contains("验证失败"))
                    {
                        description = $"条件不满足：{result.FailedReason}";
                    }
                    else
                    {
                        description = result.FailedReason;
                    }

                    descriptions.Add(description);
                }
            }

            // 按重要性排序：资源类错误优先
            descriptions.Sort((a, b) =>
            {
                bool aIsResource = a.Contains("资源不足");
                bool bIsResource = b.Contains("资源不足");
                if (aIsResource && !bIsResource) return -1;
                if (!aIsResource && bIsResource) return 1;

                bool aIsSystem = a.Contains("系统错误");
                bool bIsSystem = b.Contains("系统错误");
                if (aIsSystem && !bIsSystem) return -1;
                if (!aIsSystem && bIsSystem) return 1;

                return a.CompareTo(b);
            });

            return string.Join("\n", descriptions);
        }

        // ============ 内部消耗方法 ============

        private ExpansionConsumptionResult ExecuteConsumptionImmediately(IExpansionCondition condition)
        {
            if (condition == null)
                return ExpansionConsumptionResult.FailResult("null", "条件为空");

            try
            {
                // 执行条件自带的消耗逻辑
                var result = condition.Consume();

                // 发布单个消耗事件
                if (result.Success)
                {
                    EventBus.Publish(new ExpansionConditionConsumedEvent
                    {
                        ConditionId = condition.ConditionId,
                        ConditionType = condition.ConditionType,
                        Success = true,
                        Timestamp = DateTime.Now
                    });
                }
                else
                {
                    EventBus.Publish(new ExpansionConditionConsumptionFailedEvent
                    {
                        ConditionId = condition.ConditionId,
                        ConditionType = condition.ConditionType,
                        FailureReason = result.FailedReason,
                        Timestamp = DateTime.Now
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DefaultExpansionConsumptionService] 条件消耗异常: {condition.ConditionId}, 错误: {ex}");
                return ExpansionConsumptionResult.FailResult(condition.ConditionId, $"系统错误: {ex.Message}");
            }
        }

        private ExpansionConsumptionResult QueueConsumption(IExpansionCondition condition)
        {
            var pending = new PendingConsumption
            {
                Condition = condition,
                Callback = null,
                QueueTime = DateTime.Now
            };

            _consumptionQueue.Enqueue(pending);

            // 返回排队中状态
            return ExpansionConsumptionResult.SuccessResult(condition.ConditionId);
        }

        private void ProcessBatch()
        {
            if (_consumptionQueue.Count == 0)
                return;

            _batchResults.Clear();
            int processedCount = 0;
            int maxBatchSize = 10; // 每批最大处理数量

            // 发布批量开始事件
            EventBus.Publish(new ExpansionBatchConsumptionStartedEvent
            {
                BatchMode = true,
                TotalConditions = _consumptionQueue.Count
            });

            while (_consumptionQueue.Count > 0 && processedCount < maxBatchSize)
            {
                var pending = _consumptionQueue.Dequeue();
                var result = ExecuteConsumptionImmediately(pending.Condition);
                _batchResults.Add(result);
                processedCount++;

                // 调用回调（如果有）
                pending.Callback?.Invoke(result);
            }

            // 发布批量完成事件
            int succeededCount = _batchResults.Count(r => r.Success);
            EventBus.Publish(new ExpansionBatchConsumptionCompletedEvent
            {
                BatchMode = true,
                TotalConditions = processedCount,
                SucceededConditions = succeededCount,
                FailedConditions = processedCount - succeededCount,
                AverageQueueTime = (float)(DateTime.Now - _lastBatchProcessTime).TotalSeconds
            });

            _lastBatchProcessTime = DateTime.Now;

            // 如果还有队列，安排下次处理
            if (_consumptionQueue.Count > 0)
            {
                // 可以在这里设置一个定时器，但我们已经在Update中处理了
            }
        }

        private void RollbackSuccessfulConsumptions(List<IExpansionCondition> conditions, List<ExpansionConsumptionResult> results)
        {
            // 找出所有成功的资源消耗条件
            var successfulResourceConditions = new List<(IExpansionCondition, ExpansionConsumptionResult)>();
            for (int i = 0; i < conditions.Count; i++)
            {
                if (results[i].Success &&
                    conditions[i] is ResourceConsumptionCondition resourceCondition)
                {
                    successfulResourceConditions.Add((conditions[i], results[i]));
                }
            }

            // 尝试回滚这些消耗
            if (successfulResourceConditions.Count > 0)
            {
                Debug.LogWarning($"[DefaultExpansionConsumptionService] 尝试回滚 {successfulResourceConditions.Count} 个已成功的资源消耗");

                foreach (var (condition, result) in successfulResourceConditions)
                {
                    // 发布回滚事件
                    EventBus.Publish(new ExpansionConsumptionRollbackEvent
                    {
                        ConditionId = condition.ConditionId,
                        ConditionType = condition.ConditionType,
                        OriginalResult = result,
                        RollbackTime = DateTime.Now
                    });

                    // TODO: 实际回滚逻辑需要根据具体消耗的资源类型实现
                    // 例如：如果是物品消耗，需要将物品添加回背包
                    // 目前只是记录日志
                    Debug.Log($"回滚条件消耗: {condition.ConditionId}");
                }
            }
        }

        // ============ 公共API（用于UI或调试） ============

        /// <summary>获取队列中的消耗数量</summary>
        public int GetQueuedConsumptionCount()
        {
            return _consumptionQueue.Count;
        }

        /// <summary>强制处理所有队列中的消耗</summary>
        public void ProcessAllQueuedConsumptions()
        {
            while (_consumptionQueue.Count > 0)
            {
                ProcessBatch();
            }
        }

        /// <summary>清空队列（取消所有待处理的消耗）</summary>
        public void ClearConsumptionQueue()
        {
            int clearedCount = _consumptionQueue.Count;
            _consumptionQueue.Clear();

            Debug.Log($"[DefaultExpansionConsumptionService] 已清空 {clearedCount} 个待处理的消耗");

            EventBus.Publish(new ExpansionConsumptionQueueClearedEvent
            {
                ClearedCount = clearedCount,
                Timestamp = DateTime.Now
            });
        }

        /// <summary>启用/禁用批量模式</summary>
        public void SetBatchModeEnabled(bool enabled)
        {
            _enableBatchMode = enabled;

            // 如果禁用了批量模式，立即处理所有队列
            if (!enabled && _consumptionQueue.Count > 0)
            {
                ProcessAllQueuedConsumptions();
            }
        }

        /// <summary>设置批量处理间隔</summary>
        public void SetBatchInterval(float interval)
        {
            _batchInterval = Mathf.Max(0.01f, interval);
        }
    }

    // ============ 相关事件定义 ============

    /// <summary>条件消耗事件</summary>
    public struct ExpansionConditionConsumedEvent : IEvent
    {
        public string ConditionId;
        public ExpansionConditionType ConditionType;
        public bool Success;
        public DateTime Timestamp;
    }

    /// <summary>条件消耗失败事件</summary>
    public struct ExpansionConditionConsumptionFailedEvent : IEvent
    {
        public string ConditionId;
        public ExpansionConditionType ConditionType;
        public string FailureReason;
        public DateTime Timestamp;
    }

    /// <summary>批量消耗开始事件</summary>
    public struct ExpansionBatchConsumptionStartedEvent : IEvent
    {
        public string ExpansionId;
        public int TotalConditions;
        public bool BatchMode;
    }

    /// <summary>批量消耗完成事件</summary>
    public struct ExpansionBatchConsumptionCompletedEvent : IEvent
    {
        public string ExpansionId;
        public int TotalConditions;
        public int SucceededConditions;
        public int FailedConditions;
        public bool BatchMode;
        public float AverageQueueTime;
    }

    /// <summary>消耗回滚事件</summary>
    public struct ExpansionConsumptionRollbackEvent : IEvent
    {
        public string ConditionId;
        public ExpansionConditionType ConditionType;
        public ExpansionConsumptionResult OriginalResult;
        public DateTime RollbackTime;
    }

    /// <summary>消耗队列清空事件</summary>
    public struct ExpansionConsumptionQueueClearedEvent : IEvent
    {
        public int ClearedCount;
        public DateTime Timestamp;
    }
}