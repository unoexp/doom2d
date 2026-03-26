// 📁 05_Show/Inventory/ViewModels/ExpansionLevelViewModel.cs
// 扩展级别ViewModel，用于UI显示单个扩展级别的状态
// 🏗️ 架构层级：05_Show - 表现层ViewModel
// 🔧 职责：封装扩展级别的UI显示数据，提供数据绑定接口
// 🚫 禁止包含业务逻辑，纯数据持有类

using System;
using System.Collections.Generic;
using System.Linq;

namespace SurvivalGame.Show.Inventory
{
    /// <summary>
    /// 扩展级别ViewModel
    /// 📊 单个扩展级别的UI数据封装
    /// </summary>
    public class ExpansionLevelViewModel
    {
        // 基础数据
        public string LevelId { get; private set; }
        public int LevelNumber { get; private set; }
        public string DisplayName { get; private set; }
        public string Description { get; private set; }

        // 扩展效果
        public int AdditionalSlots { get; private set; }
        public float WeightLimitBoost { get; private set; }
        public bool UnlockSpecialSlots { get; private set; }
        public string[] NewSlotTypes { get; private set; }
        public string EffectDescription { get; private set; }

        // 条件状态
        public List<ExpansionRequirementViewModel> Requirements { get; private set; } = new();
        public string[] PrerequisiteLevelIds { get; private set; }

        // UI状态
        public ExpansionLevelState State { get; private set; } = ExpansionLevelState.Locked;
        public bool IsAvailable => State == ExpansionLevelState.Available;
        public bool IsInProgress => State == ExpansionLevelState.InProgress;
        public bool IsCompleted => State == ExpansionLevelState.Completed;
        public bool IsLocked => State == ExpansionLevelState.Locked;
        public bool IsFailed => State == ExpansionLevelState.Failed;

        // 进度信息
        public float ProgressPercentage { get; private set; }
        public float RemainingTime { get; private set; } // 剩余时间（秒）
        public string TimeRemainingText { get; private set; }

        // 资源消耗
        public Dictionary<string, int> ResourceCosts { get; private set; } = new();

        // 视觉状态
        public bool IsHighlighted { get; private set; }
        public bool IsSelected { get; private set; }
        public bool ShowProgressBar { get; private set; }

        // 计算属性
        public bool AllRequirementsMet => Requirements.All(r => r.IsMet);
        public int MetRequirementsCount => Requirements.Count(r => r.IsMet);
        public int TotalRequirementsCount => Requirements.Count;
        public string RequirementsText => $"{MetRequirementsCount}/{TotalRequirementsCount}";

        // 格式化显示
        public string LevelText => $"Lv.{LevelNumber}";
        public string StateText => GetStateText();
        public string EffectSummary => GetEffectSummary();

        // 事件
        public event Action<ExpansionLevelViewModel> OnStateChanged;
        public event Action<ExpansionLevelViewModel> OnProgressChanged;
        public event Action<ExpansionLevelViewModel> OnSelectionChanged;
        public event Action<ExpansionLevelViewModel> OnHighlightChanged;

        /// <summary>
        /// 更新级别状态
        /// 🔄 根据游戏状态更新扩展级别的可用性
        /// </summary>
        public void UpdateState(
            ExpansionLevelState state,
            float progressPercentage = 0f,
            float remainingTime = 0f)
        {
            bool stateChanged = State != state;
            bool progressChanged = Math.Abs(ProgressPercentage - progressPercentage) > 0.001f;
            bool timeChanged = Math.Abs(RemainingTime - remainingTime) > 0.001f;

            State = state;
            ProgressPercentage = Math.Clamp(progressPercentage, 0f, 1f);
            RemainingTime = remainingTime;
            TimeRemainingText = FormatTimeRemaining(remainingTime);
            ShowProgressBar = IsInProgress || (State == ExpansionLevelState.Available && ProgressPercentage > 0);

            if (stateChanged)
                OnStateChanged?.Invoke(this);

            if (progressChanged || timeChanged)
                OnProgressChanged?.Invoke(this);
        }

        /// <summary>
        /// 更新资源消耗信息
        /// 📦 更新资源需求的当前状态
        /// </summary>
        public void UpdateResourceCosts(Dictionary<string, (int required, int current)> resourceData)
        {
            ResourceCosts.Clear();
            foreach (var kvp in resourceData)
            {
                ResourceCosts[kvp.Key] = kvp.Value.current;
            }

            // 更新对应的RequirementViewModel
            foreach (var requirement in Requirements)
            {
                if (requirement.Type == ExpansionRequirementType.ResourceCost &&
                    resourceData.TryGetValue(requirement.TargetId, out var data))
                {
                    bool isMet = data.current >= data.required;
                    requirement.UpdateStatus(
                        isMet: isMet,
                        currentValue: data.current,
                        progressPercentage: Math.Clamp((float)data.current / data.required, 0f, 1f),
                        displayText: $"{requirement.ItemName} x{data.required}",
                        statusText: $"{data.current}/{data.required}"
                    );
                }
            }
        }

        /// <summary>
        /// 更新前置条件状态
        /// 🔗 检查前置级别是否已完成
        /// </summary>
        public void UpdatePrerequisiteStatus(HashSet<string> completedLevelIds)
        {
            bool allPrerequisitesMet = true;
            var missingPrerequisites = new List<string>();

            foreach (var prereqId in PrerequisiteLevelIds)
            {
                if (!completedLevelIds.Contains(prereqId))
                {
                    allPrerequisitesMet = false;
                    missingPrerequisites.Add(prereqId);
                }
            }

            // 如果前置条件不满足，设置所有Requirement为阻塞状态
            foreach (var requirement in Requirements)
            {
                requirement.SetBlocked(!allPrerequisitesMet);
            }
        }

        /// <summary>
        /// 设置选中状态
        /// 🎯 UI交互反馈
        /// </summary>
        public void SetSelected(bool selected)
        {
            if (IsSelected != selected)
            {
                IsSelected = selected;
                OnSelectionChanged?.Invoke(this);
            }
        }

        /// <summary>
        /// 设置高亮状态
        /// 🔦 UI交互反馈
        /// </summary>
        public void SetHighlighted(bool highlighted)
        {
            if (IsHighlighted != highlighted)
            {
                IsHighlighted = highlighted;
                OnHighlightChanged?.Invoke(this);
            }
        }

        /// <summary>
        /// 检查是否可开始扩展
        /// ✅ 验证所有条件是否满足
        /// </summary>
        public bool CanStartExpansion()
        {
            return State == ExpansionLevelState.Available && AllRequirementsMet;
        }

        /// <summary>
        /// 开始扩展进度
        /// ⏱️ 标记扩展为进行中
        /// </summary>
        public void StartExpansion(float duration)
        {
            if (CanStartExpansion())
            {
                UpdateState(ExpansionLevelState.InProgress, 0f, duration);
            }
        }

        /// <summary>
        /// 更新扩展进度
        /// 📈 更新进度百分比和剩余时间
        /// </summary>
        public void UpdateExpansionProgress(float progress, float remainingTime)
        {
            if (IsInProgress)
            {
                UpdateState(ExpansionLevelState.InProgress, progress, remainingTime);
            }
        }

        /// <summary>
        /// 完成扩展
        /// 🎉 标记扩展为已完成
        /// </summary>
        public void CompleteExpansion()
        {
            UpdateState(ExpansionLevelState.Completed, 1f, 0f);
        }

        /// <summary>
        /// 失败扩展
        /// ❌ 标记扩展为失败
        /// </summary>
        public void FailExpansion(string reason = null)
        {
            UpdateState(ExpansionLevelState.Failed, ProgressPercentage, 0f);
        }

        /// <summary>
        /// 工厂方法：从数据层Level创建ViewModel
        /// 🔄 数据转换适配
        /// </summary>
        public static ExpansionLevelViewModel CreateFromLevel(
            ExpansionLevel level,
            ExpansionEffect effect)
        {
            var viewModel = new ExpansionLevelViewModel
            {
                LevelId = level.LevelId,
                LevelNumber = level.LevelNumber,
                DisplayName = level.DisplayName,
                Description = level.Description,
                AdditionalSlots = effect.AdditionalSlots,
                WeightLimitBoost = effect.WeightLimitBoost,
                UnlockSpecialSlots = effect.UnlockSpecialSlots,
                NewSlotTypes = effect.NewSlotTypes ?? Array.Empty<string>(),
                EffectDescription = effect.Description,
                PrerequisiteLevelIds = level.PrerequisiteLevelIds ?? Array.Empty<string>()
            };

            // 创建Requirement ViewModels
            foreach (var requirement in level.Requirements)
            {
                var requirementVm = ExpansionRequirementViewModel.CreateFromRequirement(requirement);
                viewModel.Requirements.Add(requirementVm);
            }

            return viewModel;
        }

        /// <summary>
        /// 深拷贝
        /// 📋 用于UI状态保存和恢复
        /// </summary>
        public ExpansionLevelViewModel Clone()
        {
            var clone = new ExpansionLevelViewModel
            {
                LevelId = LevelId,
                LevelNumber = LevelNumber,
                DisplayName = DisplayName,
                Description = Description,
                AdditionalSlots = AdditionalSlots,
                WeightLimitBoost = WeightLimitBoost,
                UnlockSpecialSlots = UnlockSpecialSlots,
                NewSlotTypes = NewSlotTypes?.ToArray() ?? Array.Empty<string>(),
                EffectDescription = EffectDescription,
                PrerequisiteLevelIds = PrerequisiteLevelIds?.ToArray() ?? Array.Empty<string>(),
                State = State,
                ProgressPercentage = ProgressPercentage,
                RemainingTime = RemainingTime,
                TimeRemainingText = TimeRemainingText,
                IsHighlighted = IsHighlighted,
                IsSelected = IsSelected,
                ShowProgressBar = ShowProgressBar
            };

            clone.Requirements = Requirements.Select(r => r.Clone()).ToList();
            clone.ResourceCosts = new Dictionary<string, int>(ResourceCosts);

            return clone;
        }

        /// <summary>
        /// 获取状态文本
        /// 📝 用于UI显示
        /// </summary>
        private string GetStateText()
        {
            return State switch
            {
                ExpansionLevelState.Available => "可扩展",
                ExpansionLevelState.InProgress => $"进行中 ({ProgressPercentage:P0})",
                ExpansionLevelState.Completed => "已完成",
                ExpansionLevelState.Locked => "已锁定",
                ExpansionLevelState.Failed => "失败",
                _ => "未知"
            };
        }

        /// <summary>
        /// 获取效果摘要
        /// 📊 显示扩展带来的好处
        /// </summary>
        private string GetEffectSummary()
        {
            var effects = new List<string>();

            if (AdditionalSlots > 0)
                effects.Add($"+{AdditionalSlots}槽位");

            if (WeightLimitBoost > 0)
                effects.Add($"负重+{WeightLimitBoost:F0}%");

            if (UnlockSpecialSlots && NewSlotTypes.Length > 0)
                effects.Add($"解锁{string.Join(",", NewSlotTypes)}槽位");

            return effects.Count > 0 ? string.Join("，", effects) : "无特殊效果";
        }

        /// <summary>
        /// 格式化剩余时间
        /// ⏰ 转换为可读的时间格式
        /// </summary>
        private static string FormatTimeRemaining(float seconds)
        {
            if (seconds <= 0) return "";

            var timeSpan = TimeSpan.FromSeconds(seconds);
            if (timeSpan.TotalHours >= 1)
                return $"{timeSpan.Hours}h {timeSpan.Minutes}m";
            else if (timeSpan.TotalMinutes >= 1)
                return $"{timeSpan.Minutes}m {timeSpan.Seconds}s";
            else
                return $"{timeSpan.Seconds}s";
        }
    }

    /// <summary>
    /// 扩展级别状态枚举
    /// 🎯 描述扩展级别的当前状态
    /// </summary>
    public enum ExpansionLevelState
    {
        Locked = 0,      // 未解锁（前置条件不满足）
        Available = 1,   // 可开始扩展
        InProgress = 2,  // 扩展进行中
        Completed = 3,   // 扩展完成
        Failed = 4       // 扩展失败
    }
}