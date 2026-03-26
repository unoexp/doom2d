// 📁 01_Data/Inventory/Expansion/ExpansionConditionBase.cs
// 扩展条件基类，支持多种条件类型的序列化和验证

using System;
using UnityEngine;

namespace SurvivalGame.Data.Inventory.Expansion
{
    /// <summary>
    /// 扩展条件验证结果
    /// </summary>
    public struct ExpansionConditionResult
    {
        public bool IsMet;                  // 条件是否满足
        public string ConditionId;          // 条件ID
        public string FailedReason;         // 失败原因（用户友好信息）
        public string TechnicalReason;      // 技术失败原因（调试用）

        public static ExpansionConditionResult Success(string conditionId) => new ExpansionConditionResult
        {
            IsMet = true,
            ConditionId = conditionId,
            FailedReason = string.Empty,
            TechnicalReason = string.Empty
        };

        public static ExpansionConditionResult Fail(string conditionId, string failedReason, string technicalReason = "") => new ExpansionConditionResult
        {
            IsMet = false,
            ConditionId = conditionId,
            FailedReason = failedReason,
            TechnicalReason = technicalReason
        };
    }

    /// <summary>
    /// 扩展条件消耗结果
    /// </summary>
    public struct ExpansionConsumptionResult
    {
        public bool Success;                // 消耗是否成功
        public string ConditionId;          // 条件ID
        public string FailedReason;         // 失败原因

        public static ExpansionConsumptionResult SuccessResult(string conditionId) => new ExpansionConsumptionResult
        {
            Success = true,
            ConditionId = conditionId,
            FailedReason = string.Empty
        };

        public static ExpansionConsumptionResult FailResult(string conditionId, string failedReason) => new ExpansionConsumptionResult
        {
            Success = false,
            ConditionId = conditionId,
            FailedReason = failedReason
        };
    }

    /// <summary>
    /// 扩展条件类型枚举
    /// </summary>
    public enum ExpansionConditionType
    {
        ResourceConsumption,    // 资源消耗
        SkillRequirement,       // 技能等级要求
        ProgressRequirement,    // 游戏进度要求
        PrerequisiteExpansion,  // 前置扩展要求
        TimeRequirement,        // 时间要求
        LevelRequirement,       // 玩家等级要求
        QuestRequirement        // 任务要求
    }

    /// <summary>
    /// 扩展条件基类接口
    /// </summary>
    public interface IExpansionCondition
    {
        string ConditionId { get; }
        ExpansionConditionType ConditionType { get; }
        string DisplayName { get; }          // 显示名称（用于UI）
        string Description { get; }          // 描述（用于UI）
        int Priority { get; }                // 优先级（验证顺序）

        // 验证条件是否满足
        ExpansionConditionResult Validate();

        // 执行消耗（如扣除资源）
        ExpansionConsumptionResult Consume();

        // 获取条件详情（用于UI显示）
        string GetConditionDetails();
    }

    /// <summary>
    /// 基础扩展条件抽象类
    /// </summary>
    [Serializable]
    public abstract class ExpansionConditionBase : IExpansionCondition
    {
        [SerializeField] protected string _conditionId;
        [SerializeField] protected string _displayName;
        [SerializeField] protected string _description;
        [SerializeField] protected int _priority = 0;

        public string ConditionId => _conditionId;
        public string DisplayName => _displayName;
        public string Description => _description;
        public int Priority => _priority;

        public abstract ExpansionConditionType ConditionType { get; }

        public virtual ExpansionConditionResult Validate()
        {
            return ExpansionConditionResult.Fail(_conditionId, "条件验证未实现", "Validate method not implemented");
        }

        public virtual ExpansionConsumptionResult Consume()
        {
            return ExpansionConsumptionResult.FailResult(_conditionId, "条件消耗未实现");
        }

        public virtual string GetConditionDetails()
        {
            return $"条件类型：{ConditionType}\n描述：{_description}";
        }

        protected ExpansionConditionBase() { }

        protected ExpansionConditionBase(string conditionId, string displayName, string description, int priority = 0)
        {
            _conditionId = conditionId ?? Guid.NewGuid().ToString();
            _displayName = displayName ?? "未命名条件";
            _description = description ?? string.Empty;
            _priority = priority;
        }
    }
}