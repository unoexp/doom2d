// 📁 05_Show/Inventory/ViewModels/ExpansionConfigViewModel.cs
// 扩展配置ViewModel，用于UI显示整个扩展系统的状态
// 🏗️ 架构层级：05_Show - 表现层ViewModel
// 🔧 职责：封装扩展系统的UI显示数据，提供数据绑定接口
// 🚫 禁止包含业务逻辑，纯数据持有类

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SurvivalGame.Data.Inventory;

namespace SurvivalGame.Show.Inventory
{
    /// <summary>
    /// 扩展配置ViewModel
    /// 📊 整个扩展系统的UI数据封装
    /// </summary>
    public class ExpansionConfigViewModel
    {
        // 基础配置
        public string ExpansionId { get; private set; }
        public string TargetContainerId { get; private set; }
        public string DisplayName { get; private set; }
        public string Description { get; private set; }
        public string IconPath { get; private set; }
        public UnityEngine.Color ThemeColor { get; private set; }

        // 扩展级别
        public List<ExpansionLevelViewModel> ExpansionLevels { get; private set; } = new();
        private Dictionary<string, ExpansionLevelViewModel> _levelsById = new();

        // 系统状态
        public ExpansionSystemState SystemState { get; private set; } = ExpansionSystemState.Inactive;
        public bool CanStartNewExpansion => SystemState == ExpansionSystemState.Idle;
        public bool HasActiveExpansion => SystemState == ExpansionSystemState.Expanding;
        public bool IsCompleted => SystemState == ExpansionSystemState.Completed;

        // 进度统计
        public int CompletedLevelsCount { get; private set; }
        public int AvailableLevelsCount { get; private set; }
        public int TotalLevelsCount { get; private set; }
        public float OverallProgressPercentage { get; private set; }

        // 当前扩展信息
        public string CurrentExpansionLevelId { get; private set; }
        public float CurrentExpansionProgress { get; private set; }
        public float CurrentExpansionRemainingTime { get; private set; }

        // 资源统计
        public Dictionary<string, int> TotalResourceCosts { get; private set; } = new();
        public Dictionary<string, int> AvailableResources { get; private set; } = new();

        // 限制配置
        public int MaxTotalExpansions { get; private set; }
        public int RemainingExpansions { get; private set; }
        public bool AllowParallelExpansion { get; private set; }
        public float DefaultExpansionDuration { get; private set; }

        // UI状态
        public bool IsExpanded { get; private set; }
        public int SelectedLevelIndex { get; private set; } = -1;
        public bool ShowRequirementsPanel { get; private set; }
        public bool ShowProgressPanel { get; private set; }

        // 计算属性
        public ExpansionLevelViewModel SelectedLevel =>
            SelectedLevelIndex >= 0 && SelectedLevelIndex < ExpansionLevels.Count
                ? ExpansionLevels[SelectedLevelIndex]
                : null;

        public ExpansionLevelViewModel CurrentExpansionLevel =>
            !string.IsNullOrEmpty(CurrentExpansionLevelId) && _levelsById.TryGetValue(CurrentExpansionLevelId, out var level)
                ? level
                : null;

        public bool HasAvailableLevels => AvailableLevelsCount > 0;
        public bool HasCompletedLevels => CompletedLevelsCount > 0;
        public bool IsAtMaxExpansion => RemainingExpansions <= 0;

        // 格式化显示
        public string ProgressText => $"{CompletedLevelsCount}/{TotalLevelsCount}";
        public string RemainingExpansionsText => RemainingExpansions > 0 ? $"{RemainingExpansions}次剩余" : "已达上限";

        // 事件
        public event Action<ExpansionConfigViewModel> OnSystemStateChanged;
        public event Action<ExpansionConfigViewModel> OnProgressUpdated;
        public event Action<ExpansionConfigViewModel> OnLevelSelectionChanged;
        public event Action<ExpansionConfigViewModel> OnUIStateChanged;

        /// <summary>
        /// 更新系统状态
        /// 🔄 更新整个扩展系统的状态
        /// </summary>
        public void UpdateSystemState(
            ExpansionSystemState state,
            HashSet<string> completedLevelIds,
            string currentExpansionLevelId = null,
            float currentProgress = 0f,
            float remainingTime = 0f)
        {
            SystemState = state;
            CurrentExpansionLevelId = currentExpansionLevelId;
            CurrentExpansionProgress = currentProgress;
            CurrentExpansionRemainingTime = remainingTime;

            // 更新级别状态
            UpdateLevelsStatus(completedLevelIds);

            // 更新统计信息
            UpdateStatistics(completedLevelIds);

            OnSystemStateChanged?.Invoke(this);
            OnProgressUpdated?.Invoke(this);
        }

        /// <summary>
        /// 更新级别状态
        /// 🎯 根据已完成级别更新所有级别的状态
        /// </summary>
        private void UpdateLevelsStatus(HashSet<string> completedLevelIds)
        {
            CompletedLevelsCount = 0;
            AvailableLevelsCount = 0;

            foreach (var level in ExpansionLevels)
            {
                // 检查是否已完成
                if (completedLevelIds.Contains(level.LevelId))
                {
                    if (!level.IsCompleted)
                    {
                        level.UpdateState(ExpansionLevelState.Completed, 1f, 0f);
                    }
                    CompletedLevelsCount++;
                }
                else
                {
                    // 检查前置条件
                    level.UpdatePrerequisiteStatus(completedLevelIds);

                    // 确定当前状态
                    ExpansionLevelState newState;
                    if (level.LevelId == CurrentExpansionLevelId)
                    {
                        newState = ExpansionLevelState.InProgress;
                        level.UpdateExpansionProgress(CurrentExpansionProgress, CurrentExpansionRemainingTime);
                    }
                    else if (level.AllRequirementsMet && level.State != ExpansionLevelState.Locked)
                    {
                        newState = ExpansionLevelState.Available;
                        AvailableLevelsCount++;
                    }
                    else
                    {
                        newState = ExpansionLevelState.Locked;
                    }

                    if (level.State != newState)
                    {
                        level.UpdateState(newState);
                    }
                }
            }
        }

        /// <summary>
        /// 更新统计信息
        /// 📊 计算整体进度和资源需求
        /// </summary>
        private void UpdateStatistics(HashSet<string> completedLevelIds)
        {
            TotalLevelsCount = ExpansionLevels.Count;
            OverallProgressPercentage = TotalLevelsCount > 0
                ? (float)CompletedLevelsCount / TotalLevelsCount
                : 0f;

            RemainingExpansions = Math.Max(0, MaxTotalExpansions - CompletedLevelsCount);

            // 计算总资源需求
            RecalculateResourceCosts(completedLevelIds);
        }

        /// <summary>
        /// 重新计算资源需求
        /// 📦 聚合所有未完成级别的资源需求
        /// </summary>
        private void RecalculateResourceCosts(HashSet<string> completedLevelIds)
        {
            TotalResourceCosts.Clear();

            foreach (var level in ExpansionLevels)
            {
                // 只计算未完成的级别
                if (!completedLevelIds.Contains(level.LevelId) && level.State == ExpansionLevelState.Available)
                {
                    foreach (var requirement in level.Requirements)
                    {
                        if (requirement.Type == ExpansionRequirementType.ResourceCost)
                        {
                            if (TotalResourceCosts.ContainsKey(requirement.TargetId))
                                TotalResourceCosts[requirement.TargetId] += requirement.RequiredValue;
                            else
                                TotalResourceCosts[requirement.TargetId] = requirement.RequiredValue;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 更新可用资源
        /// 📥 更新玩家当前拥有的资源数量
        /// </summary>
        public void UpdateAvailableResources(Dictionary<string, int> availableResources)
        {
            AvailableResources = new Dictionary<string, int>(availableResources);

            // 更新所有Requirement的资源状态
            foreach (var level in ExpansionLevels)
            {
                if (!level.IsCompleted)
                {
                    var resourceData = new Dictionary<string, (int required, int current)>();

                    foreach (var requirement in level.Requirements)
                    {
                        if (requirement.Type == ExpansionRequirementType.ResourceCost)
                        {
                            var current = availableResources.TryGetValue(requirement.TargetId, out var count) ? count : 0;
                            resourceData[requirement.TargetId] = (requirement.RequiredValue, current);
                        }
                    }

                    level.UpdateResourceCosts(resourceData);
                }
            }

            OnProgressUpdated?.Invoke(this);
        }

        /// <summary>
        /// 选择扩展级别
        /// 🎯 在UI中选择一个级别进行查看或操作
        /// </summary>
        public void SelectLevel(int index)
        {
            if (index < 0 || index >= ExpansionLevels.Count)
                return;

            var oldIndex = SelectedLevelIndex;
            SelectedLevelIndex = index;

            // 更新选中状态
            if (oldIndex >= 0 && oldIndex < ExpansionLevels.Count)
                ExpansionLevels[oldIndex].SetSelected(false);

            ExpansionLevels[index].SetSelected(true);
            ShowRequirementsPanel = true;

            OnLevelSelectionChanged?.Invoke(this);
            OnUIStateChanged?.Invoke(this);
        }

        /// <summary>
        /// 清除选择
        /// ❌ 清除当前选中的级别
        /// </summary>
        public void ClearSelection()
        {
            if (SelectedLevelIndex >= 0 && SelectedLevelIndex < ExpansionLevels.Count)
                ExpansionLevels[SelectedLevelIndex].SetSelected(false);

            SelectedLevelIndex = -1;
            ShowRequirementsPanel = false;

            OnLevelSelectionChanged?.Invoke(this);
            OnUIStateChanged?.Invoke(this);
        }

        /// <summary>
        /// 设置UI展开状态
        /// 📱 控制UI面板的展开/收起
        /// </summary>
        public void SetExpanded(bool expanded)
        {
            if (IsExpanded != expanded)
            {
                IsExpanded = expanded;
                OnUIStateChanged?.Invoke(this);
            }
        }

        /// <summary>
        /// 开始扩展
        /// ⏱️ 标记系统为扩展进行中
        /// </summary>
        public void StartExpansion(string levelId, float duration)
        {
            if (_levelsById.TryGetValue(levelId, out var level) && level.CanStartExpansion())
            {
                SystemState = ExpansionSystemState.Expanding;
                CurrentExpansionLevelId = levelId;
                CurrentExpansionProgress = 0f;
                CurrentExpansionRemainingTime = duration;

                level.StartExpansion(duration);
                UpdateLevelsStatus(new HashSet<string>());

                OnSystemStateChanged?.Invoke(this);
                OnProgressUpdated?.Invoke(this);
            }
        }

        /// <summary>
        /// 更新扩展进度
        /// 📈 更新当前扩展的进度
        /// </summary>
        public void UpdateExpansionProgress(float progress, float remainingTime)
        {
            if (HasActiveExpansion)
            {
                CurrentExpansionProgress = Math.Clamp(progress, 0f, 1f);
                CurrentExpansionRemainingTime = remainingTime;

                if (CurrentExpansionLevel != null)
                {
                    CurrentExpansionLevel.UpdateExpansionProgress(progress, remainingTime);
                }

                OnProgressUpdated?.Invoke(this);
            }
        }

        /// <summary>
        /// 完成扩展
        /// 🎉 标记当前扩展为完成
        /// </summary>
        public void CompleteExpansion()
        {
            if (HasActiveExpansion && CurrentExpansionLevel != null)
            {
                SystemState = ExpansionSystemState.Idle;
                CurrentExpansionLevel.CompleteExpansion();

                // 清除当前扩展信息
                CurrentExpansionLevelId = null;
                CurrentExpansionProgress = 0f;
                CurrentExpansionRemainingTime = 0f;

                OnSystemStateChanged?.Invoke(this);
                OnProgressUpdated?.Invoke(this);
            }
        }

        /// <summary>
        /// 工厂方法：从数据层Config创建ViewModel
        /// 🔄 数据转换适配
        /// </summary>
        public static ExpansionConfigViewModel CreateFromConfig(
            CapacityExpansionConfigSO config,
            string iconPath = null)
        {
            var viewModel = new ExpansionConfigViewModel
            {
                ExpansionId = config.ExpansionId,
                TargetContainerId = config.TargetContainerId,
                DisplayName = config.DisplayName,
                Description = config.Description,
                IconPath = iconPath ?? GetIconPath(config),
                ThemeColor = config.ThemeColor,
                MaxTotalExpansions = config.MaxTotalExpansions,
                AllowParallelExpansion = config.AllowParallelExpansion,
                DefaultExpansionDuration = config.ExpansionDuration
            };

            // 创建级别ViewModels
            foreach (var level in config.ExpansionLevels)
            {
                var levelVm = ExpansionLevelViewModel.CreateFromLevel(level, level.Effects);
                viewModel.ExpansionLevels.Add(levelVm);
                viewModel._levelsById[level.LevelId] = levelVm;
            }

            // 按级别序号排序
            viewModel.ExpansionLevels.Sort((a, b) => a.LevelNumber.CompareTo(b.LevelNumber));

            viewModel.TotalLevelsCount = viewModel.ExpansionLevels.Count;

            return viewModel;
        }

        /// <summary>
        /// 深拷贝
        /// 📋 用于UI状态保存和恢复
        /// </summary>
        public ExpansionConfigViewModel Clone()
        {
            var clone = new ExpansionConfigViewModel
            {
                ExpansionId = ExpansionId,
                TargetContainerId = TargetContainerId,
                DisplayName = DisplayName,
                Description = Description,
                IconPath = IconPath,
                ThemeColor = ThemeColor,
                SystemState = SystemState,
                CompletedLevelsCount = CompletedLevelsCount,
                AvailableLevelsCount = AvailableLevelsCount,
                TotalLevelsCount = TotalLevelsCount,
                OverallProgressPercentage = OverallProgressPercentage,
                CurrentExpansionLevelId = CurrentExpansionLevelId,
                CurrentExpansionProgress = CurrentExpansionProgress,
                CurrentExpansionRemainingTime = CurrentExpansionRemainingTime,
                MaxTotalExpansions = MaxTotalExpansions,
                RemainingExpansions = RemainingExpansions,
                AllowParallelExpansion = AllowParallelExpansion,
                DefaultExpansionDuration = DefaultExpansionDuration,
                IsExpanded = IsExpanded,
                SelectedLevelIndex = SelectedLevelIndex,
                ShowRequirementsPanel = ShowRequirementsPanel,
                ShowProgressPanel = ShowProgressPanel
            };

            clone.ExpansionLevels = ExpansionLevels.Select(l => l.Clone()).ToList();
            clone._levelsById = new Dictionary<string, ExpansionLevelViewModel>();
            foreach (var level in clone.ExpansionLevels)
                clone._levelsById[level.LevelId] = level;

            clone.TotalResourceCosts = new Dictionary<string, int>(TotalResourceCosts);
            clone.AvailableResources = new Dictionary<string, int>(AvailableResources);

            return clone;
        }

        /// <summary>
        /// 获取图标路径
        /// 🖼️ 从Config获取或生成默认图标路径
        /// </summary>
        private static string GetIconPath(CapacityExpansionConfigSO config)
        {
            if (config.Icon != null)
            {
                // 实际项目中需要根据资源管理策略获取路径
                return $"UI/Icons/Expansion/{config.ExpansionId}";
            }
            return "UI/Icons/Expansion/Default";
        }

        /// <summary>
        /// 获取可开始的扩展级别
        /// ✅ 返回所有可开始的级别
        /// </summary>
        public List<ExpansionLevelViewModel> GetStartableLevels()
        {
            return ExpansionLevels.Where(l => l.CanStartExpansion()).ToList();
        }

        /// <summary>
        /// 检查是否有足够的资源
        /// 📊 验证是否满足选定级别的所有资源需求
        /// </summary>
        public bool HasEnoughResourcesForLevel(string levelId)
        {
            if (!_levelsById.TryGetValue(levelId, out var level))
                return false;

            foreach (var requirement in level.Requirements)
            {
                if (requirement.Type == ExpansionRequirementType.ResourceCost)
                {
                    if (!AvailableResources.TryGetValue(requirement.TargetId, out var available) ||
                        available < requirement.RequiredValue)
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 根据ID获取扩展级别ViewModel
        /// </summary>
        public ExpansionLevelViewModel GetLevel(string levelId)
        {
            _levelsById.TryGetValue(levelId, out var level);
            return level;
        }

        /// <summary>
        /// 获取扩展统计信息
        /// </summary>
        public ExpansionStatsData GetExpansionStats()
        {
            return new ExpansionStatsData
            {
                CurrentMainSlots = 0,
                CurrentQuickSlots = 0,
                CompletedLevels = CompletedLevelsCount,
                TotalLevels = TotalLevelsCount
            };
        }

        /// <summary>
        /// 创建默认ViewModel（用于测试和回退场景）
        /// </summary>
        public static ExpansionConfigViewModel CreateDefault()
        {
            return new ExpansionConfigViewModel
            {
                ExpansionId = "Default_Expansion",
                TargetContainerId = "MainInventory",
                DisplayName = "背包扩展",
                Description = "扩展背包容量，增加负重上限",
                IconPath = "UI/Icons/Expansion/Default",
                ThemeColor = UnityEngine.Color.white,
                MaxTotalExpansions = 5,
                AllowParallelExpansion = false,
                DefaultExpansionDuration = 60f,
                SystemState = ExpansionSystemState.Idle,
                TotalLevelsCount = 0,
                CompletedLevelsCount = 0,
                AvailableLevelsCount = 0
            };
        }
    }

    /// <summary>
    /// 扩展系统状态枚举
    /// 🏗️ 描述整个扩展系统的运行状态
    /// </summary>
    public enum ExpansionSystemState
    {
        Inactive = 0,    // 系统未激活
        Idle = 1,        // 系统空闲，可开始新扩展
        Expanding = 2,   // 正在进行扩展
        Completed = 3,   // 所有扩展已完成
        Failed = 4       // 系统失败（如资源不足导致扩展失败）
    }

    /// <summary>
    /// 扩展统计数据
    /// </summary>
    public struct ExpansionStatsData
    {
        public int CurrentMainSlots;
        public int CurrentQuickSlots;
        public int CompletedLevels;
        public int TotalLevels;
    }
}