// 📁 05_Show/Inventory/ViewModels/ExpansionRequirementViewModel.cs
// 扩展条件ViewModel，用于UI显示单个扩展条件的状态
// 🏗️ 架构层级：05_Show - 表现层ViewModel
// 🔧 职责：封装扩展条件的UI显示数据，提供数据绑定接口
// 🚫 禁止包含业务逻辑，纯数据持有类

using System;
using SurvivalGame.Data.Inventory;

namespace SurvivalGame.Show.Inventory
{
    /// <summary>
    /// 扩展条件ViewModel
    /// 📊 单个扩展条件的UI数据封装
    /// </summary>
    public class ExpansionRequirementViewModel
    {
        // 基础数据
        public ExpansionRequirementType Type { get; private set; }
        public string TargetId { get; private set; }
        public int RequiredValue { get; private set; }
        public float RequiredFloatValue { get; private set; }
        public string Description { get; private set; }

        // UI状态
        public bool IsMet { get; private set; }
        public int CurrentValue { get; private set; }
        public float CurrentFloatValue { get; private set; }
        public float ProgressPercentage { get; private set; } // 0-1范围

        // 资源相关（用于ResourceCost类型）
        public string ItemName { get; private set; }
        public string ItemIconPath { get; private set; }
        public int ItemQuantityInInventory { get; private set; }

        // 技能/等级相关（用于SkillLevel/PlayerLevel类型）
        public string SkillName { get; private set; }
        public int CurrentSkillLevel { get; private set; }

        // 任务相关（用于QuestCompletion类型）
        public string QuestName { get; private set; }
        public bool IsQuestCompleted { get; private set; }

        // 格式化显示文本
        public string DisplayText { get; private set; }
        public string StatusText { get; private set; }
        public string ProgressText { get; private set; }

        // 视觉状态
        public bool IsHighlighted { get; private set; }
        public bool IsBlockedByOtherCondition { get; private set; }

        // 事件
        public event Action<ExpansionRequirementViewModel> OnStatusChanged;

        /// <summary>
        /// 更新条件状态
        /// 📈 根据当前游戏状态更新条件的满足情况
        /// </summary>
        public void UpdateStatus(
            bool isMet,
            int currentValue = 0,
            float currentFloatValue = 0f,
            string displayText = null,
            string statusText = null,
            float progressPercentage = 0f)
        {
            bool changed = IsMet != isMet ||
                          CurrentValue != currentValue ||
                          Math.Abs(CurrentFloatValue - currentFloatValue) > 0.001f ||
                          Math.Abs(ProgressPercentage - progressPercentage) > 0.001f;

            IsMet = isMet;
            CurrentValue = currentValue;
            CurrentFloatValue = currentFloatValue;
            ProgressPercentage = Math.Clamp(progressPercentage, 0f, 1f);

            if (!string.IsNullOrEmpty(displayText))
                DisplayText = displayText;

            if (!string.IsNullOrEmpty(statusText))
                StatusText = statusText;

            if (changed)
                OnStatusChanged?.Invoke(this);
        }

        /// <summary>
        /// 更新资源信息
        /// 📦 用于ResourceCost类型的资源显示
        /// </summary>
        public void UpdateResourceInfo(
            string itemName,
            string itemIconPath,
            int quantityInInventory)
        {
            ItemName = itemName;
            ItemIconPath = itemIconPath;
            ItemQuantityInInventory = quantityInInventory;

            // 自动更新显示文本
            DisplayText = $"{itemName} x{RequiredValue}";
            StatusText = $"{quantityInInventory}/{RequiredValue}";
            ProgressPercentage = Math.Clamp((float)quantityInInventory / RequiredValue, 0f, 1f);

            OnStatusChanged?.Invoke(this);
        }

        /// <summary>
        /// 更新技能信息
        /// 🎯 用于SkillLevel类型的技能显示
        /// </summary>
        public void UpdateSkillInfo(
            string skillName,
            int currentSkillLevel)
        {
            SkillName = skillName;
            CurrentSkillLevel = currentSkillLevel;

            // 自动更新显示文本
            DisplayText = $"{skillName} Lv.{RequiredValue}";
            StatusText = $"当前: Lv.{currentSkillLevel}";
            ProgressPercentage = Math.Clamp((float)currentSkillLevel / RequiredValue, 0f, 1f);

            OnStatusChanged?.Invoke(this);
        }

        /// <summary>
        /// 更新任务信息
        /// ✅ 用于QuestCompletion类型的任务显示
        /// </summary>
        public void UpdateQuestInfo(
            string questName,
            bool isCompleted)
        {
            QuestName = questName;
            IsQuestCompleted = isCompleted;

            // 自动更新显示文本
            DisplayText = questName;
            StatusText = isCompleted ? "已完成" : "未完成";
            ProgressPercentage = isCompleted ? 1f : 0f;

            OnStatusChanged?.Invoke(this);
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
                OnStatusChanged?.Invoke(this);
            }
        }

        /// <summary>
        /// 设置阻塞状态
        /// 🚧 表示此条件被其他条件阻塞
        /// </summary>
        public void SetBlocked(bool blocked)
        {
            if (IsBlockedByOtherCondition != blocked)
            {
                IsBlockedByOtherCondition = blocked;
                OnStatusChanged?.Invoke(this);
            }
        }

        /// <summary>
        /// 工厂方法：从数据层Requirement创建ViewModel
        /// 🔄 数据转换适配
        /// </summary>
        public static ExpansionRequirementViewModel CreateFromRequirement(
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

        /// <summary>
        /// 深拷贝
        /// 📋 用于UI状态保存和恢复
        /// </summary>
        public ExpansionRequirementViewModel Clone()
        {
            var clone = new ExpansionRequirementViewModel
            {
                Type = Type,
                TargetId = TargetId,
                RequiredValue = RequiredValue,
                RequiredFloatValue = RequiredFloatValue,
                Description = Description,
                IsMet = IsMet,
                CurrentValue = CurrentValue,
                CurrentFloatValue = CurrentFloatValue,
                ProgressPercentage = ProgressPercentage,
                ItemName = ItemName,
                ItemIconPath = ItemIconPath,
                ItemQuantityInInventory = ItemQuantityInInventory,
                SkillName = SkillName,
                CurrentSkillLevel = CurrentSkillLevel,
                QuestName = QuestName,
                IsQuestCompleted = IsQuestCompleted,
                DisplayText = DisplayText,
                StatusText = StatusText,
                ProgressText = ProgressText,
                IsHighlighted = IsHighlighted,
                IsBlockedByOtherCondition = IsBlockedByOtherCondition
            };

            return clone;
        }

        /// <summary>
        /// 获取条件的简要描述
        /// 📝 用于Tooltip和状态显示
        /// </summary>
        public string GetBriefDescription()
        {
            return Type switch
            {
                ExpansionRequirementType.ResourceCost => $"需要 {RequiredValue}个 {ItemName ?? TargetId}",
                ExpansionRequirementType.SkillLevel => $"需要 {SkillName ?? TargetId} Lv.{RequiredValue}",
                ExpansionRequirementType.PlayerLevel => $"需要玩家等级 Lv.{RequiredValue}",
                ExpansionRequirementType.QuestCompletion => $"需要完成任务: {QuestName ?? TargetId}",
                _ => Description ?? "特殊条件"
            };
        }

        /// <summary>
        /// 检查是否正在进行中
        /// ⏳ 表示条件正在处理（如资源不足但正在收集）
        /// </summary>
        public bool IsInProgress => !IsMet && ProgressPercentage > 0;

        /// <summary>
        /// 检查是否可交互
        /// 🖱️ 表示UI是否允许点击此条件进行相关操作
        /// </summary>
        public bool IsInteractive => Type == ExpansionRequirementType.ResourceCost;
    }
}