// 📁 01_Data/Inventory/Expansion/ExpansionConditionTypes.cs
// 具体的扩展条件类型实现

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalGame.Data.Inventory.Expansion
{
    #region 资源消耗条件
    /// <summary>
    /// 资源消耗条件：需要消耗特定的物品或货币
    /// </summary>
    [Serializable]
    public class ResourceConsumptionCondition : ExpansionConditionBase
    {
        [Serializable]
        public struct ResourceRequirement
        {
            public string ItemId;            // 物品ID
            public int RequiredQuantity;     // 需要数量
            public bool ConsumeOnSuccess;    // 是否在成功时消耗
            public string DisplayName;       // 显示名称（用于UI）
        }

        [SerializeField] private ResourceRequirement[] _requirements;

        public ResourceRequirement[] Requirements => _requirements;

        public override ExpansionConditionType ConditionType => ExpansionConditionType.ResourceConsumption;

        public ResourceConsumptionCondition() : base() { }

        public ResourceConsumptionCondition(
            string conditionId,
            string displayName,
            string description,
            ResourceRequirement[] requirements,
            int priority = 0
        ) : base(conditionId, displayName, description, priority)
        {
            _requirements = requirements ?? Array.Empty<ResourceRequirement>();
        }

        public override ExpansionConditionResult Validate()
        {
            if (_requirements == null || _requirements.Length == 0)
                return ExpansionConditionResult.Success(_conditionId);

            // 通过ServiceLocator获取背包系统
            var inventorySystem = ServiceLocator.Get<InventorySystem>();
            if (inventorySystem == null)
                return ExpansionConditionResult.Fail(_conditionId, "背包系统不可用", "InventorySystem not found in ServiceLocator");

            // 检查所有资源需求
            var failedRequirements = new List<string>();
            for (int i = 0; i < _requirements.Length; i++)
            {
                var requirement = _requirements[i];
                int availableCount = inventorySystem.GetTotalItemCount(requirement.ItemId);

                if (availableCount < requirement.RequiredQuantity)
                {
                    string displayName = string.IsNullOrEmpty(requirement.DisplayName) ? requirement.ItemId : requirement.DisplayName;
                    failedRequirements.Add($"{displayName} ({availableCount}/{requirement.RequiredQuantity})");
                }
            }

            if (failedRequirements.Count > 0)
            {
                string reason = $"缺少资源：{string.Join("、", failedRequirements)}";
                return ExpansionConditionResult.Fail(_conditionId, reason, $"Missing resources: {string.Join(", ", failedRequirements)}");
            }

            return ExpansionConditionResult.Success(_conditionId);
        }

        public override ExpansionConsumptionResult Consume()
        {
            if (_requirements == null || _requirements.Length == 0)
                return ExpansionConsumptionResult.SuccessResult(_conditionId);

            // 获取背包系统
            var inventorySystem = ServiceLocator.Get<InventorySystem>();
            if (inventorySystem == null)
                return ExpansionConsumptionResult.FailResult(_conditionId, "背包系统不可用");

            // 先验证所有条件
            var validationResult = Validate();
            if (!validationResult.IsMet)
                return ExpansionConsumptionResult.FailResult(_conditionId, validationResult.FailedReason);

            // 消耗所有需要消耗的资源
            var failedConsumptions = new List<string>();
            for (int i = 0; i < _requirements.Length; i++)
            {
                var requirement = _requirements[i];
                if (requirement.ConsumeOnSuccess)
                {
                    bool removed = inventorySystem.TryRemoveItem(requirement.ItemId, requirement.RequiredQuantity);
                    if (!removed)
                    {
                        string displayName = string.IsNullOrEmpty(requirement.DisplayName) ? requirement.ItemId : requirement.DisplayName;
                        failedConsumptions.Add(displayName);
                    }
                }
            }

            if (failedConsumptions.Count > 0)
            {
                string reason = $"资源消耗失败：{string.Join("、", failedConsumptions)}";
                return ExpansionConsumptionResult.FailResult(_conditionId, reason);
            }

            return ExpansionConsumptionResult.SuccessResult(_conditionId);
        }

        public override string GetConditionDetails()
        {
            if (_requirements == null || _requirements.Length == 0)
                return "无需资源消耗";

            var details = new List<string>();
            for (int i = 0; i < _requirements.Length; i++)
            {
                var req = _requirements[i];
                string displayName = string.IsNullOrEmpty(req.DisplayName) ? req.ItemId : req.DisplayName;
                string consumeText = req.ConsumeOnSuccess ? "（消耗）" : "（仅验证）";
                details.Add($"{displayName} ×{req.RequiredQuantity}{consumeText}");
            }

            return $"资源需求：\n{string.Join("\n", details)}";
        }
    }
    #endregion

    #region 技能等级条件
    /// <summary>
    /// 技能等级条件：需要特定技能达到指定等级
    /// </summary>
    [Serializable]
    public class SkillRequirementCondition : ExpansionConditionBase
    {
        [Serializable]
        public struct SkillRequirement
        {
            public string SkillId;           // 技能ID
            public int RequiredLevel;        // 需要等级
            public string DisplayName;       // 显示名称
        }

        [SerializeField] private SkillRequirement[] _requirements;

        public override ExpansionConditionType ConditionType => ExpansionConditionType.SkillRequirement;

        public SkillRequirementCondition() : base() { }

        public SkillRequirementCondition(
            string conditionId,
            string displayName,
            string description,
            SkillRequirement[] requirements,
            int priority = 0
        ) : base(conditionId, displayName, description, priority)
        {
            _requirements = requirements ?? Array.Empty<SkillRequirement>();
        }

        public override ExpansionConditionResult Validate()
        {
            // TODO: 实现技能系统后，通过ServiceLocator获取技能系统验证等级
            // 目前返回成功，作为占位符
            return ExpansionConditionResult.Success(_conditionId);
        }

        public override ExpansionConsumptionResult Consume()
        {
            // 技能条件不消耗任何资源
            return ExpansionConsumptionResult.SuccessResult(_conditionId);
        }

        public override string GetConditionDetails()
        {
            if (_requirements == null || _requirements.Length == 0)
                return "无需技能要求";

            var details = new List<string>();
            for (int i = 0; i < _requirements.Length; i++)
            {
                var req = _requirements[i];
                string displayName = string.IsNullOrEmpty(req.DisplayName) ? req.SkillId : req.DisplayName;
                details.Add($"{displayName} 等级 {req.RequiredLevel}+");
            }

            return $"技能要求：\n{string.Join("\n", details)}";
        }
    }
    #endregion

    #region 游戏进度条件
    /// <summary>
    /// 游戏进度条件：需要完成特定任务或达到指定进度
    /// </summary>
    [Serializable]
    public class ProgressRequirementCondition : ExpansionConditionBase
    {
        [Serializable]
        public struct ProgressRequirement
        {
            public string ProgressId;        // 进度ID（任务ID、成就ID等）
            public bool RequiredCompletion;  // 是否需要完成
            public string DisplayName;       // 显示名称
        }

        [SerializeField] private ProgressRequirement[] _requirements;

        public override ExpansionConditionType ConditionType => ExpansionConditionType.ProgressRequirement;

        public ProgressRequirementCondition() : base() { }

        public ProgressRequirementCondition(
            string conditionId,
            string displayName,
            string description,
            ProgressRequirement[] requirements,
            int priority = 0
        ) : base(conditionId, displayName, description, priority)
        {
            _requirements = requirements ?? Array.Empty<ProgressRequirement>();
        }

        public override ExpansionConditionResult Validate()
        {
            // TODO: 实现进度系统后，通过ServiceLocator获取进度系统验证
            // 目前返回成功，作为占位符
            return ExpansionConditionResult.Success(_conditionId);
        }

        public override ExpansionConsumptionResult Consume()
        {
            // 进度条件不消耗任何资源
            return ExpansionConsumptionResult.SuccessResult(_conditionId);
        }

        public override string GetConditionDetails()
        {
            if (_requirements == null || _requirements.Length == 0)
                return "无需进度要求";

            var details = new List<string>();
            for (int i = 0; i < _requirements.Length; i++)
            {
                var req = _requirements[i];
                string displayName = string.IsNullOrEmpty(req.DisplayName) ? req.ProgressId : req.DisplayName;
                string completionText = req.RequiredCompletion ? "（已完成）" : "（已开始）";
                details.Add($"{displayName}{completionText}");
            }

            return $"进度要求：\n{string.Join("\n", details)}";
        }
    }
    #endregion

    #region 前置扩展条件
    /// <summary>
    /// 前置扩展条件：需要先完成指定的背包扩展
    /// </summary>
    [Serializable]
    public class PrerequisiteExpansionCondition : ExpansionConditionBase
    {
        [Serializable]
        public struct ExpansionPrerequisite
        {
            public string ExpansionId;       // 扩展ID
            public int RequiredLevel;        // 需要达到的扩展等级（0表示只需要解锁）
            public string DisplayName;       // 显示名称
        }

        [SerializeField] private ExpansionPrerequisite[] _prerequisites;

        public override ExpansionConditionType ConditionType => ExpansionConditionType.PrerequisiteExpansion;

        public PrerequisiteExpansionCondition() : base() { }

        public PrerequisiteExpansionCondition(
            string conditionId,
            string displayName,
            string description,
            ExpansionPrerequisite[] prerequisites,
            int priority = 0
        ) : base(conditionId, displayName, description, priority)
        {
            _prerequisites = prerequisites ?? Array.Empty<ExpansionPrerequisite>();
        }

        public override ExpansionConditionResult Validate()
        {
            // TODO: 实现扩展记录系统后，验证前置扩展是否完成
            // 目前返回成功，作为占位符
            return ExpansionConditionResult.Success(_conditionId);
        }

        public override ExpansionConsumptionResult Consume()
        {
            // 前置条件不消耗任何资源
            return ExpansionConsumptionResult.SuccessResult(_conditionId);
        }

        public override string GetConditionDetails()
        {
            if (_prerequisites == null || _prerequisites.Length == 0)
                return "无需前置扩展";

            var details = new List<string>();
            for (int i = 0; i < _prerequisites.Length; i++)
            {
                var req = _prerequisites[i];
                string displayName = string.IsNullOrEmpty(req.DisplayName) ? req.ExpansionId : req.DisplayName;
                string levelText = req.RequiredLevel > 0 ? $" 等级{req.RequiredLevel}+" : "";
                details.Add($"{displayName}{levelText}");
            }

            return $"前置扩展：\n{string.Join("\n", details)}";
        }
    }
    #endregion

    #region 时间要求条件
    /// <summary>
    /// 时间要求条件：需要游戏内达到指定天数
    /// </summary>
    [Serializable]
    public class TimeRequirementCondition : ExpansionConditionBase
    {
        [SerializeField] private int _requiredDays;     // 需要的游戏天数
        [SerializeField] private bool _exactDay;        // 是否需要精确天数（false表示至少需要这么多天）

        public override ExpansionConditionType ConditionType => ExpansionConditionType.TimeRequirement;

        public TimeRequirementCondition() : base() { }

        public TimeRequirementCondition(
            string conditionId,
            string displayName,
            string description,
            int requiredDays,
            bool exactDay = false,
            int priority = 0
        ) : base(conditionId, displayName, description, priority)
        {
            _requiredDays = Mathf.Max(0, requiredDays);
            _exactDay = exactDay;
        }

        public override ExpansionConditionResult Validate()
        {
            // TODO: 实现游戏时间系统后，获取当前游戏天数
            // 目前返回成功，作为占位符
            return ExpansionConditionResult.Success(_conditionId);
        }

        public override ExpansionConsumptionResult Consume()
        {
            // 时间条件不消耗任何资源
            return ExpansionConsumptionResult.SuccessResult(_conditionId);
        }

        public override string GetConditionDetails()
        {
            string exactText = _exactDay ? "在第" : "至少需要";
            return $"{exactText}{_requiredDays}天";
        }
    }
    #endregion

    #region 玩家等级条件
    /// <summary>
    /// 玩家等级条件：需要玩家达到指定等级
    /// </summary>
    [Serializable]
    public class LevelRequirementCondition : ExpansionConditionBase
    {
        [SerializeField] private int _requiredPlayerLevel;

        public override ExpansionConditionType ConditionType => ExpansionConditionType.LevelRequirement;

        public LevelRequirementCondition() : base() { }

        public LevelRequirementCondition(
            string conditionId,
            string displayName,
            string description,
            int requiredPlayerLevel,
            int priority = 0
        ) : base(conditionId, displayName, description, priority)
        {
            _requiredPlayerLevel = Mathf.Max(1, requiredPlayerLevel);
        }

        public override ExpansionConditionResult Validate()
        {
            // TODO: 实现玩家等级系统后，验证玩家等级
            // 目前返回成功，作为占位符
            return ExpansionConditionResult.Success(_conditionId);
        }

        public override ExpansionConsumptionResult Consume()
        {
            // 等级条件不消耗任何资源
            return ExpansionConsumptionResult.SuccessResult(_conditionId);
        }

        public override string GetConditionDetails()
        {
            return $"玩家等级达到{_requiredPlayerLevel}级";
        }
    }
    #endregion
}