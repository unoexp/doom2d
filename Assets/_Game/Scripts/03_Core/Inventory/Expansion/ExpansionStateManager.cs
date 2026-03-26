// 📁 03_Core/Inventory/Expansion/ExpansionStateManager.cs
// 扩展状态管理器，负责管理背包扩展的状态和进度

using System;
using System.Collections.Generic;
using SurvivalGame.Data.Inventory.Expansion;

namespace SurvivalGame.Core.Inventory.Expansion
{
    /// <summary>
    /// 扩展状态数据，支持序列化
    /// </summary>
    [Serializable]
    public class ExpansionStateData
    {
        public string ExpansionId;                          // 扩展ID
        public string ContainerId;                          // 容器ID
        public int CompletionCount;                         // 完成次数
        public DateTime LastCompletionTime;                 // 最后完成时间
        public DateTime NextAvailableTime;                  // 下次可用时间（冷却结束）
        public bool IsApplied;                              // 效果是否已应用
        public Dictionary<string, object> CustomData;       // 自定义数据（用于保存额外状态）
        public int CurrentLevel;                            // 当前扩展等级（用于可升级扩展）
        public int MaxLevel;                                // 最大扩展等级

        public ExpansionStateData()
        {
            CustomData = new Dictionary<string, object>();
        }

        public ExpansionStateData(string expansionId, string containerId) : this()
        {
            ExpansionId = expansionId;
            ContainerId = containerId;
            CompletionCount = 0;
            LastCompletionTime = DateTime.MinValue;
            NextAvailableTime = DateTime.MinValue;
            IsApplied = false;
            CurrentLevel = 1;
            MaxLevel = 1;
        }

        /// <summary>检查扩展是否可用（冷却时间结束）</summary>
        public bool IsAvailable(DateTime currentTime)
        {
            return NextAvailableTime <= currentTime;
        }

        /// <summary>获取剩余冷却时间（秒）</summary>
        public float GetRemainingCooldownSeconds(DateTime currentTime)
        {
            if (NextAvailableTime <= currentTime)
                return 0f;

            return (float)(NextAvailableTime - currentTime).TotalSeconds;
        }

        /// <summary>设置冷却时间</summary>
        public void SetCooldown(float cooldownSeconds, DateTime currentTime)
        {
            NextAvailableTime = currentTime.AddSeconds(cooldownSeconds);
        }

        /// <summary>记录扩展完成</summary>
        public void RecordCompletion(DateTime completionTime)
        {
            CompletionCount++;
            LastCompletionTime = completionTime;
            IsApplied = true;
        }

        /// <summary>升级扩展等级</summary>
        public bool UpgradeLevel(int newLevel)
        {
            if (newLevel <= CurrentLevel || newLevel > MaxLevel)
                return false;

            CurrentLevel = newLevel;
            return true;
        }
    }

    /// <summary>
    /// 扩展状态管理器
    /// 🏗️ 架构说明：核心业务层组件，管理所有背包扩展的状态
    /// </summary>
    public class ExpansionStateManager : ISaveable, IExpansionRecordService
    {
        private Dictionary<string, ExpansionStateData> _expansionStates;
        private Dictionary<string, List<ExpansionStateData>> _containerExpansions;

        public ExpansionStateManager()
        {
            _expansionStates = new Dictionary<string, ExpansionStateData>();
            _containerExpansions = new Dictionary<string, List<ExpansionStateData>>();
        }

        // ============ ISaveable实现 ============
        public string SaveKey => nameof(ExpansionStateManager);

        public object CaptureState()
        {
            return new
            {
                States = _expansionStates,
                ContainerMappings = _containerExpansions
            };
        }

        public void RestoreState(object state)
        {
            if (state == null) return;

            // 使用反射恢复状态，避免复杂的类型转换
            var stateDict = state as System.Collections.IDictionary;
            if (stateDict == null) return;

            _expansionStates = new Dictionary<string, ExpansionStateData>();
            _containerExpansions = new Dictionary<string, List<ExpansionStateData>>();

            // 这里需要根据实际的序列化格式进行恢复
            // 实际实现中会使用具体的序列化格式
        }

        // ============ 公共API ============
        /// <summary>获取扩展状态</summary>
        public ExpansionStateData GetExpansionState(string expansionId)
        {
            _expansionStates.TryGetValue(expansionId, out var state);
            return state;
        }

        /// <summary>获取或创建扩展状态</summary>
        public ExpansionStateData GetOrCreateExpansionState(string expansionId, string containerId)
        {
            if (!_expansionStates.TryGetValue(expansionId, out var state))
            {
                state = new ExpansionStateData(expansionId, containerId);
                _expansionStates[expansionId] = state;

                if (!_containerExpansions.TryGetValue(containerId, out var containerList))
                {
                    containerList = new List<ExpansionStateData>();
                    _containerExpansions[containerId] = containerList;
                }
                containerList.Add(state);
            }

            return state;
        }

        /// <summary>检查扩展是否已完成</summary>
        public bool IsExpansionCompleted(string expansionId)
        {
            var state = GetExpansionState(expansionId);
            return state != null && state.CompletionCount > 0;
        }

        /// <summary>检查扩展是否可用</summary>
        public bool IsExpansionAvailable(string expansionId)
        {
            var state = GetExpansionState(expansionId);
            if (state == null) return true; // 新扩展默认可用

            return state.IsAvailable(DateTime.Now);
        }

        /// <summary>记录扩展完成</summary>
        public void RecordExpansionCompleted(string expansionId, string containerId, float cooldownSeconds)
        {
            var state = GetOrCreateExpansionState(expansionId, containerId);
            state.RecordCompletion(DateTime.Now);

            if (cooldownSeconds > 0)
                state.SetCooldown(cooldownSeconds, DateTime.Now);
        }

        /// <summary>获取容器的所有扩展状态</summary>
        public IReadOnlyList<ExpansionStateData> GetContainerExpansions(string containerId)
        {
            if (_containerExpansions.TryGetValue(containerId, out var list))
                return list.AsReadOnly();

            return Array.Empty<ExpansionStateData>();
        }

        /// <summary>获取已完成的扩展ID列表</summary>
        public IReadOnlyList<string> GetCompletedExpansionIds()
        {
            var completed = new List<string>();
            foreach (var kvp in _expansionStates)
            {
                if (kvp.Value.CompletionCount > 0)
                    completed.Add(kvp.Key);
            }
            return completed;
        }

        /// <summary>获取扩展进度</summary>
        public ExpansionProgress GetExpansionProgress(string expansionId)
        {
            var state = GetExpansionState(expansionId);
            if (state == null)
                return new ExpansionProgress { ExpansionId = expansionId, IsCompleted = false };

            return new ExpansionProgress
            {
                ExpansionId = expansionId,
                IsCompleted = state.CompletionCount > 0,
                CompletionCount = state.CompletionCount,
                LastCompletionTime = state.LastCompletionTime,
                AdditionalData = $"Level: {state.CurrentLevel}/{state.MaxLevel}"
            };
        }

        /// <summary>清除容器的所有扩展状态</summary>
        public void ClearContainerExpansions(string containerId)
        {
            if (_containerExpansions.TryGetValue(containerId, out var list))
            {
                foreach (var state in list)
                {
                    _expansionStates.Remove(state.ExpansionId);
                }
                _containerExpansions.Remove(containerId);
            }
        }

        /// <summary>获取扩展的剩余可用次数（针对可重复扩展）</summary>
        public int GetRemainingUses(string expansionId, int maxRepeatCount)
        {
            var state = GetExpansionState(expansionId);
            if (state == null) return maxRepeatCount;

            return Math.Max(0, maxRepeatCount - state.CompletionCount);
        }

        /// <summary>检查扩展是否已达到最大重复次数</summary>
        public bool IsMaxRepeatReached(string expansionId, int maxRepeatCount)
        {
            var state = GetExpansionState(expansionId);
            if (state == null) return false;

            return state.CompletionCount >= maxRepeatCount;
        }

        /// <summary>设置扩展的最大等级</summary>
        public void SetExpansionMaxLevel(string expansionId, int maxLevel)
        {
            var state = GetOrCreateExpansionState(expansionId, "unknown");
            state.MaxLevel = maxLevel;
        }

        // ============ IExpansionRecordService 接口实现 ============

        /// <summary>记录扩展完成（接口方法，使用默认容器和无冷却）</summary>
        void IExpansionRecordService.RecordExpansionCompleted(string expansionId)
        {
            RecordExpansionCompleted(expansionId, "", 0f);
        }

        /// <summary>获取扩展完成次数</summary>
        public int GetExpansionCompletedCount(string expansionId)
        {
            var state = GetExpansionState(expansionId);
            return state?.CompletionCount ?? 0;
        }

        /// <summary>获取所有已完成的扩展ID列表（IExpansionRecordService接口）</summary>
        public IReadOnlyList<string> GetCompletedExpansions()
        {
            return GetCompletedExpansionIds();
        }
    }
}