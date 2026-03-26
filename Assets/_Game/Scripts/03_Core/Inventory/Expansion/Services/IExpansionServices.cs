// 📁 03_Core/Inventory/Expansion/Services/IExpansionServices.cs
// 扩展服务接口定义

using System.Collections.Generic;
using SurvivalGame.Data.Inventory.Expansion;

namespace SurvivalGame.Core.Inventory.Expansion
{
    /// <summary>
    /// 扩展条件验证服务
    /// 🏗️ 架构说明：核心业务层服务，负责验证所有扩展条件
    /// </summary>
    public interface IExpansionValidationService
    {
        /// <summary>验证单个条件</summary>
        ExpansionConditionResult ValidateCondition(IExpansionCondition condition);

        /// <summary>验证条件集合</summary>
        /// <returns>验证结果集合和总体是否通过</returns>
        (bool AllMet, List<ExpansionConditionResult> Results) ValidateConditions(IEnumerable<IExpansionCondition> conditions);

        /// <summary>验证扩展配置</summary>
        /// <returns>验证结果集合和总体是否通过</returns>
        (bool AllMet, List<ExpansionConditionResult> Results) ValidateExpansion(ExpansionDefinitionSO expansionDefinition);

        /// <summary>获取不满足条件的友好描述</summary>
        string GetFailedConditionDescriptions(List<ExpansionConditionResult> failedResults);
    }

    /// <summary>
    /// 扩展资源消耗服务
    /// 🏗️ 架构说明：核心业务层服务，负责执行资源消耗逻辑
    /// </summary>
    public interface IExpansionConsumptionService
    {
        /// <summary>执行条件消耗</summary>
        ExpansionConsumptionResult ConsumeCondition(IExpansionCondition condition);

        /// <summary>执行条件集合的消耗</summary>
        /// <returns>消耗结果集合和总体是否成功</returns>
        (bool AllSucceeded, List<ExpansionConsumptionResult> Results) ConsumeConditions(IEnumerable<IExpansionCondition> conditions);

        /// <summary>执行扩展配置的消耗</summary>
        /// <returns>消耗结果集合和总体是否成功</returns>
        (bool AllSucceeded, List<ExpansionConsumptionResult> Results) ConsumeExpansion(ExpansionDefinitionSO expansionDefinition);

        /// <summary>获取失败消耗的友好描述</summary>
        string GetFailedConsumptionDescriptions(List<ExpansionConsumptionResult> failedResults);
    }

    /// <summary>
    /// 扩展效果应用服务
    /// 🏗️ 架构说明：核心业务层服务，负责应用扩展效果
    /// </summary>
    public interface IExpansionEffectService
    {
        /// <summary>应用扩展效果</summary>
        bool ApplyExpansionEffect(ExpansionDefinitionSO expansionDefinition);

        /// <summary>撤销扩展效果（用于回滚）</summary>
        bool RollbackExpansionEffect(ExpansionDefinitionSO expansionDefinition);

        /// <summary>获取扩展效果描述</summary>
        string GetExpansionEffectDescription(ExpansionDefinitionSO expansionDefinition);

        /// <summary>检查扩展是否已应用</summary>
        bool IsExpansionApplied(string expansionId);
    }

    /// <summary>
    /// 扩展记录服务
    /// 🏗️ 架构说明：核心业务层服务，负责记录扩展状态和进度
    /// </summary>
    public interface IExpansionRecordService : ISaveable
    {
        /// <summary>记录扩展完成</summary>
        void RecordExpansionCompleted(string expansionId);

        /// <summary>检查扩展是否已完成</summary>
        bool IsExpansionCompleted(string expansionId);

        /// <summary>获取扩展完成次数</summary>
        int GetExpansionCompletedCount(string expansionId);

        /// <summary>获取所有已完成的扩展ID</summary>
        IReadOnlyList<string> GetCompletedExpansions();

        /// <summary>获取扩展进度</summary>
        ExpansionProgress GetExpansionProgress(string expansionId);
    }

    /// <summary>
    /// 扩展进度数据结构
    /// </summary>
    public struct ExpansionProgress
    {
        public string ExpansionId;
        public bool IsCompleted;
        public int CompletionCount;
        public System.DateTime LastCompletionTime;
        public string AdditionalData;
    }
}