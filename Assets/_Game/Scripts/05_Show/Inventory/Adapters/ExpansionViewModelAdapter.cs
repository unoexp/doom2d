// 📁 05_Show/Inventory/Adapters/ExpansionViewModelAdapter.cs
// 扩展系统ViewModel和数据层之间的转换器
// 🏗️ 架构层级：05_Show - 表现层适配器
// 🔧 职责：将数据层的扩展配置转换为ViewModel，处理业务层数据的适配
// ⚠️ 纯转换逻辑，无业务逻辑，无Unity依赖（仅数据转换）

using System;
using System.Collections.Generic;
using SurvivalGame.Data.Inventory;
using SurvivalGame.Show.Inventory;

namespace SurvivalGame.Show.Inventory.Adapters
{
    /// <summary>
    /// 扩展系统ViewModel适配器
    /// 🔄 负责数据层到ViewModel的转换和数据同步
    /// </summary>
    public static class ExpansionViewModelAdapter
    {
        // ============ 核心转换方法 ============

        /// <summary>
        /// 将CapacityExpansionConfigSO转换为ExpansionConfigViewModel
        /// 📊 数据层配置 -> ViewModel
        /// </summary>
        public static ExpansionConfigViewModel ConvertConfigToViewModel(
            CapacityExpansionConfigSO config,
            HashSet<string> completedLevelIds,
            Dictionary<string, int> availableResources,
            string currentExpansionLevelId = null,
            float currentProgress = 0f,
            float remainingTime = 0f)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            // 创建基础ViewModel
            var viewModel = new ExpansionConfigViewModel();

            // 设置基础配置
            viewModel.ExpansionId = config.ExpansionId;
            viewModel.TargetContainerId = config.TargetContainerId;
            viewModel.DisplayName = config.DisplayName;
            viewModel.Description = config.Description;
            viewModel.IconPath = GetIconPath(config);
            viewModel.ThemeColor = ConvertUnityColor(config.ThemeColor);
            viewModel.MaxTotalExpansions = config.MaxTotalExpansions;
            viewModel.AllowParallelExpansion = config.AllowParallelExpansion;
            viewModel.DefaultExpansionDuration = config.ExpansionDuration;

            // 创建级别ViewModels
            var levels = new List<ExpansionLevelViewModel>();
            foreach (var levelConfig in config.ExpansionLevels)
            {
                var levelVm = ConvertLevelToViewModel(levelConfig, levelConfig.Effects);
                levels.Add(levelVm);
            }

            // 按级别序号排序
            levels.Sort((a, b) => a.LevelNumber.CompareTo(b.LevelNumber));
            viewModel.ExpansionLevels = levels;

            // 初始化索引
            var levelsById = new Dictionary<string, ExpansionLevelViewModel>();
            foreach (var level in levels)
                levelsById[level.LevelId] = level;

            // 设置当前系统状态
            var systemState = DetermineSystemState(completedLevelIds, config, currentExpansionLevelId);
            viewModel.UpdateSystemState(
                systemState,
                completedLevelIds,
                currentExpansionLevelId,
                currentProgress,
                remainingTime
            );

            // 更新可用资源
            if (availableResources != null)
            {
                viewModel.UpdateAvailableResources(availableResources);
            }

            return viewModel;
        }

        /// <summary>
        /// 将ExpansionLevel转换为ExpansionLevelViewModel
        /// 📊 数据层级别 -> ViewModel
        /// </summary>
        public static ExpansionLevelViewModel ConvertLevelToViewModel(
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
            var requirements = new List<ExpansionRequirementViewModel>();
            foreach (var requirement in level.Requirements)
            {
                var requirementVm = ConvertRequirementToViewModel(requirement);
                requirements.Add(requirementVm);
            }
            viewModel.Requirements = requirements;

            return viewModel;
        }

        /// <summary>
        /// 将ExpansionRequirement转换为ExpansionRequirementViewModel
        /// 📊 数据层条件 -> ViewModel
        /// </summary>
        public static ExpansionRequirementViewModel ConvertRequirementToViewModel(
            ExpansionRequirement requirement)
        {
            var viewModel = new ExpansionRequirementViewModel
            {
                Type = requirement.Type,
                TargetId = requirement.TargetId,
                RequiredValue = requirement.RequiredValue,
                RequiredFloatValue = requirement.RequiredFloatValue,
                Description = requirement.Description,
                DisplayText = requirement.Description ?? "条件未知",
                StatusText = "未检查",
                ProgressPercentage = 0f,
                IsMet = false
            };

            return viewModel;
        }

        // ============ 状态同步方法 ============

        /// <summary>
        /// 同步ViewModel与游戏状态
        /// 🔄 更新ViewModel以反映当前游戏状态
        /// </summary>
        public static void SyncViewModelWithGameState(
            ExpansionConfigViewModel viewModel,
            HashSet<string> completedLevelIds,
            Dictionary<string, int> availableResources,
            Dictionary<string, (int skillLevel, bool questCompleted)> skillAndQuestStatus,
            string currentExpansionLevelId = null,
            float currentProgress = 0f,
            float remainingTime = 0f)
        {
            // 更新系统状态
            var systemState = DetermineSystemState(completedLevelIds, null, currentExpansionLevelId);
            viewModel.UpdateSystemState(
                systemState,
                completedLevelIds,
                currentExpansionLevelId,
                currentProgress,
                remainingTime
            );

            // 更新资源状态
            if (availableResources != null)
            {
                viewModel.UpdateAvailableResources(availableResources);
            }

            // 更新技能和任务状态
            SyncRequirementStatus(viewModel, skillAndQuestStatus);
        }

        /// <summary>
        /// 同步条件状态
        /// 🎯 根据游戏状态更新所有条件的满足情况
        /// </summary>
        private static void SyncRequirementStatus(
            ExpansionConfigViewModel viewModel,
            Dictionary<string, (int skillLevel, bool questCompleted)> skillAndQuestStatus)
        {
            foreach (var level in viewModel.ExpansionLevels)
            {
                foreach (var requirement in level.Requirements)
                {
                    bool isMet = false;
                    float progress = 0f;
                    string statusText = "";

                    switch (requirement.Type)
                    {
                        case ExpansionRequirementType.ResourceCost:
                            // 资源条件在UpdateAvailableResources中处理
                            break;

                        case ExpansionRequirementType.SkillLevel:
                            if (skillAndQuestStatus != null &&
                                skillAndQuestStatus.TryGetValue(requirement.TargetId, out var skillStatus))
                            {
                                isMet = skillStatus.skillLevel >= requirement.RequiredValue;
                                progress = Math.Clamp((float)skillStatus.skillLevel / requirement.RequiredValue, 0f, 1f);
                                statusText = $"当前: Lv.{skillStatus.skillLevel}";
                            }
                            break;

                        case ExpansionRequirementType.PlayerLevel:
                            // 玩家等级需要从游戏状态获取
                            // TODO: 从游戏状态获取玩家等级
                            break;

                        case ExpansionRequirementType.QuestCompletion:
                            if (skillAndQuestStatus != null &&
                                skillAndQuestStatus.TryGetValue(requirement.TargetId, out var questStatus))
                            {
                                isMet = questStatus.questCompleted;
                                progress = isMet ? 1f : 0f;
                                statusText = isMet ? "已完成" : "未完成";
                            }
                            break;
                    }

                    if (isMet != requirement.IsMet || progress > 0)
                    {
                        requirement.UpdateStatus(isMet, progressPercentage: progress, statusText: statusText);
                    }
                }
            }
        }

        // ============ 辅助方法 ============

        /// <summary>
        /// 确定系统状态
        /// 🏗️ 根据完成状态和当前扩展计算系统状态
        /// </summary>
        private static ExpansionSystemState DetermineSystemState(
            HashSet<string> completedLevelIds,
            CapacityExpansionConfigSO config,
            string currentExpansionLevelId)
        {
            if (!string.IsNullOrEmpty(currentExpansionLevelId))
                return ExpansionSystemState.Expanding;

            if (config != null && completedLevelIds != null)
            {
                if (completedLevelIds.Count >= config.ExpansionLevels.Length)
                    return ExpansionSystemState.Completed;
            }

            return ExpansionSystemState.Idle;
        }

        /// <summary>
        /// 获取图标路径
        /// 🖼️ 根据Config生成图标路径
        /// </summary>
        private static string GetIconPath(CapacityExpansionConfigSO config)
        {
            // 实际项目中需要根据资源管理策略获取路径
            if (config.Icon != null)
            {
                // 假设图标资源按命名规范存储
                return $"UI/Icons/Expansion/{config.ExpansionId}";
            }
            return "UI/Icons/Expansion/Default";
        }

        /// <summary>
        /// 转换Unity颜色
        /// 🎨 Unity Color -> 自定义Color结构
        /// </summary>
        public static Color ConvertUnityColor(UnityEngine.Color unityColor)
        {
            return new Color(unityColor.r, unityColor.g, unityColor.b, unityColor.a);
        }

        /// <summary>
        /// 转换回Unity颜色
        /// 🎨 自定义Color结构 -> Unity Color
        /// </summary>
        public static UnityEngine.Color ConvertToUnityColor(Color customColor)
        {
            return new UnityEngine.Color(customColor.R, customColor.G, customColor.B, customColor.A);
        }

        /// <summary>
        /// 获取扩展所需资源列表
        /// 📦 提取所有级别的资源需求
        /// </summary>
        public static Dictionary<string, int> GetTotalResourceRequirements(
            CapacityExpansionConfigSO config,
            HashSet<string> completedLevelIds)
        {
            var requirements = new Dictionary<string, int>();

            foreach (var level in config.ExpansionLevels)
            {
                // 只计算未完成的级别
                if (completedLevelIds != null && completedLevelIds.Contains(level.LevelId))
                    continue;

                foreach (var requirement in level.Requirements)
                {
                    if (requirement.Type == ExpansionRequirementType.ResourceCost)
                    {
                        if (requirements.ContainsKey(requirement.TargetId))
                            requirements[requirement.TargetId] += requirement.RequiredValue;
                        else
                            requirements[requirement.TargetId] = requirement.RequiredValue;
                    }
                }
            }

            return requirements;
        }

        /// <summary>
        /// 验证扩展条件是否满足
        /// ✅ 检查特定级别的所有条件是否满足
        /// </summary>
        public static (bool allMet, List<string> failedConditions) ValidateLevelRequirements(
            ExpansionLevelViewModel level,
            Dictionary<string, int> availableResources,
            Dictionary<string, (int skillLevel, bool questCompleted)> skillAndQuestStatus)
        {
            var failedConditions = new List<string>();

            foreach (var requirement in level.Requirements)
            {
                bool conditionMet = false;

                switch (requirement.Type)
                {
                    case ExpansionRequirementType.ResourceCost:
                        if (availableResources != null &&
                            availableResources.TryGetValue(requirement.TargetId, out var available) &&
                            available >= requirement.RequiredValue)
                        {
                            conditionMet = true;
                        }
                        else
                        {
                            failedConditions.Add($"需要 {requirement.RequiredValue}个 {requirement.TargetId}");
                        }
                        break;

                    case ExpansionRequirementType.SkillLevel:
                        if (skillAndQuestStatus != null &&
                            skillAndQuestStatus.TryGetValue(requirement.TargetId, out var skillStatus) &&
                            skillStatus.skillLevel >= requirement.RequiredValue)
                        {
                            conditionMet = true;
                        }
                        else
                        {
                            failedConditions.Add($"需要 {requirement.TargetId}技能 Lv.{requirement.RequiredValue}");
                        }
                        break;

                    case ExpansionRequirementType.QuestCompletion:
                        if (skillAndQuestStatus != null &&
                            skillAndQuestStatus.TryGetValue(requirement.TargetId, out var questStatus) &&
                            questStatus.questCompleted)
                        {
                            conditionMet = true;
                        }
                        else
                        {
                            failedConditions.Add($"需要完成任务: {requirement.TargetId}");
                        }
                        break;

                    default:
                        // 其他条件类型默认为未满足
                        failedConditions.Add($"特殊条件: {requirement.Description}");
                        break;
                }

                if (!conditionMet && requirement.Type != ExpansionRequirementType.ResourceCost)
                {
                    // 对于非资源条件，添加到失败列表
                }
            }

            bool allMet = failedConditions.Count == 0;
            return (allMet, failedConditions);
        }

        /// <summary>
        /// 创建默认的扩展配置ViewModel
        /// 🏭 用于测试和回退场景
        /// </summary>
        public static ExpansionConfigViewModel CreateDefaultViewModel()
        {
            var viewModel = new ExpansionConfigViewModel
            {
                ExpansionId = "Default_Expansion",
                TargetContainerId = "MainInventory",
                DisplayName = "背包扩展",
                Description = "扩展背包容量，增加负重上限",
                IconPath = "UI/Icons/Expansion/Default",
                ThemeColor = Color.White,
                MaxTotalExpansions = 5,
                AllowParallelExpansion = false,
                DefaultExpansionDuration = 60f,
                SystemState = ExpansionSystemState.Idle,
                TotalLevelsCount = 0,
                CompletedLevelsCount = 0,
                AvailableLevelsCount = 0
            };

            return viewModel;
        }
    }
}