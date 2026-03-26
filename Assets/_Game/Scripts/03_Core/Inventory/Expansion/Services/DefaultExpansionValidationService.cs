// 📁 03_Core/Inventory/Expansion/Services/DefaultExpansionValidationService.cs
// 默认扩展条件验证服务实现

using System;
using System.Collections.Generic;
using UnityEngine;
using SurvivalGame.Data.Inventory.Expansion;

namespace SurvivalGame.Core.Inventory.Expansion
{
    /// <summary>
    /// 默认扩展条件验证服务
    /// 🏗️ 架构说明：核心业务层服务，负责验证所有扩展条件
    /// [PERF] 使用缓存减少重复验证计算
    /// </summary>
    public class DefaultExpansionValidationService : MonoBehaviour, IExpansionValidationService
    {
        // ============ 缓存配置 ============
        private class ConditionCacheEntry
        {
            public DateTime LastValidationTime;
            public ExpansionConditionResult Result;
            public float CacheDuration = 5f; // 5秒缓存
        }

        private Dictionary<string, ConditionCacheEntry> _conditionCache;
        private Dictionary<string, (DateTime, bool, List<ExpansionConditionResult>)> _expansionCache;

        // ============ 生命周期 ============
        private void Awake()
        {
            _conditionCache = new Dictionary<string, ConditionCacheEntry>();
            _expansionCache = new Dictionary<string, (DateTime, bool, List<ExpansionConditionResult>)>();
            ServiceLocator.Register<IExpansionValidationService>(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<IExpansionValidationService>();
        }

        // ============ IExpansionValidationService 实现 ============

        /// <summary>验证单个条件</summary>
        public ExpansionConditionResult ValidateCondition(IExpansionCondition condition)
        {
            if (condition == null)
                return ExpansionConditionResult.Fail("null", "条件为空", "Condition is null");

            // 检查缓存
            if (TryGetCachedConditionResult(condition.ConditionId, out var cachedResult))
                return cachedResult;

            // 执行验证
            var result = condition.Validate();

            // 缓存结果（无论成功或失败都缓存，但失败可能变化更快）
            CacheConditionResult(condition.ConditionId, result,
                result.IsMet ? 10f : 2f); // 成功缓存10秒，失败缓存2秒

            return result;
        }

        /// <summary>验证条件集合</summary>
        public (bool AllMet, List<ExpansionConditionResult> Results) ValidateConditions(IEnumerable<IExpansionCondition> conditions)
        {
            if (conditions == null)
                return (false, new List<ExpansionConditionResult>());

            var results = new List<ExpansionConditionResult>();
            bool allMet = true;

            // 按优先级排序验证
            var sortedConditions = new List<IExpansionCondition>(conditions);
            sortedConditions.Sort((a, b) => a.Priority.CompareTo(b.Priority));

            foreach (var condition in sortedConditions)
            {
                var result = ValidateCondition(condition);
                results.Add(result);

                if (!result.IsMet)
                    allMet = false;

                // 如果高优先级条件失败，可以提前终止验证
                if (!result.IsMet && condition.Priority >= 50) // 假设50及以上为高优先级
                {
                    // 为剩余条件添加跳过标记
                    foreach (var remaining in sortedConditions)
                    {
                        if (remaining == condition) continue;
                        if (!results.Exists(r => r.ConditionId == remaining.ConditionId))
                        {
                            results.Add(ExpansionConditionResult.Fail(remaining.ConditionId,
                                "由于前置条件失败而跳过验证",
                                $"Skipped due to failed condition: {condition.ConditionId}"));
                        }
                    }
                    break;
                }
            }

            return (allMet, results);
        }

        /// <summary>验证扩展配置</summary>
        public (bool AllMet, List<ExpansionConditionResult> Results) ValidateExpansion(ExpansionDefinitionSO expansionDefinition)
        {
            if (expansionDefinition == null)
                return (false, new List<ExpansionConditionResult>());

            string cacheKey = $"Expansion_{expansionDefinition.ExpansionId}";

            // 检查缓存
            if (TryGetCachedExpansionResult(cacheKey, out var cached))
                return cached;

            // 执行验证
            var (allMet, results) = ValidateConditions(expansionDefinition.Conditions);

            // 缓存结果
            CacheExpansionResult(cacheKey, allMet, results);

            return (allMet, results);
        }

        /// <summary>获取不满足条件的友好描述</summary>
        public string GetFailedConditionDescriptions(List<ExpansionConditionResult> failedResults)
        {
            if (failedResults == null || failedResults.Count == 0)
                return string.Empty;

            var descriptions = new List<string>();
            foreach (var result in failedResults)
            {
                if (!result.IsMet)
                {
                    // 优先使用用户友好的失败原因
                    if (!string.IsNullOrEmpty(result.FailedReason))
                    {
                        descriptions.Add($"{result.FailedReason}");
                    }
                    else
                    {
                        // 回退到技术原因或默认描述
                        string reason = !string.IsNullOrEmpty(result.TechnicalReason)
                            ? result.TechnicalReason
                            : "条件未满足";
                        descriptions.Add($"{result.ConditionId}: {reason}");
                    }
                }
            }

            // 按重要性排序：资源类失败优先
            descriptions.Sort((a, b) =>
            {
                bool aIsResource = a.Contains("缺少资源") || a.Contains("资源");
                bool bIsResource = b.Contains("缺少资源") || b.Contains("资源");
                if (aIsResource && !bIsResource) return -1;
                if (!aIsResource && bIsResource) return 1;
                return a.CompareTo(b);
            });

            return string.Join("\n", descriptions);
        }

        // ============ 缓存管理 ============

        private bool TryGetCachedConditionResult(string conditionId, out ExpansionConditionResult result)
        {
            result = default;
            if (_conditionCache.TryGetValue(conditionId, out var entry))
            {
                if ((DateTime.Now - entry.LastValidationTime).TotalSeconds < entry.CacheDuration)
                {
                    result = entry.Result;
                    return true;
                }
                else
                {
                    _conditionCache.Remove(conditionId);
                }
            }
            return false;
        }

        private void CacheConditionResult(string conditionId, ExpansionConditionResult result, float cacheDuration)
        {
            if (string.IsNullOrEmpty(conditionId))
                return;

            _conditionCache[conditionId] = new ConditionCacheEntry
            {
                LastValidationTime = DateTime.Now,
                Result = result,
                CacheDuration = cacheDuration
            };
        }

        private bool TryGetCachedExpansionResult(string cacheKey, out (bool AllMet, List<ExpansionConditionResult> Results) result)
        {
            result = default;
            if (_expansionCache.TryGetValue(cacheKey, out var cached))
            {
                var (cacheTime, allMet, results) = cached;
                // 扩展验证结果缓存时间较短，因为资源状态可能快速变化
                if ((DateTime.Now - cacheTime).TotalSeconds < 3f)
                {
                    result = (allMet, results);
                    return true;
                }
                else
                {
                    _expansionCache.Remove(cacheKey);
                }
            }
            return false;
        }

        private void CacheExpansionResult(string cacheKey, bool allMet, List<ExpansionConditionResult> results)
        {
            if (string.IsNullOrEmpty(cacheKey))
                return;

            _expansionCache[cacheKey] = (DateTime.Now, allMet, results);
        }

        // ============ 公共API（用于UI或调试） ============

        /// <summary>清除特定条件的缓存</summary>
        public void ClearConditionCache(string conditionId)
        {
            if (conditionId != null)
                _conditionCache.Remove(conditionId);
        }

        /// <summary>清除特定扩展的缓存</summary>
        public void ClearExpansionCache(string expansionId)
        {
            string cacheKey = $"Expansion_{expansionId}";
            if (cacheKey != null)
                _expansionCache.Remove(cacheKey);
        }

        /// <summary>清除所有缓存</summary>
        public void ClearAllCache()
        {
            _conditionCache.Clear();
            _expansionCache.Clear();
        }

        /// <summary>获取缓存统计信息（用于调试）</summary>
        public (int ConditionCacheCount, int ExpansionCacheCount) GetCacheStatistics()
        {
            return (_conditionCache.Count, _expansionCache.Count);
        }
    }
}