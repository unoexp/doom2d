// 📁 01_Data/Inventory/Expansion/AdditionalExpansionConditionTypes.cs
// 额外的扩展条件类型实现，补充现有的条件类型集合

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalGame.Data.Inventory.Expansion
{
    #region 任务完成条件
    /// <summary>
    /// 任务完成条件：需要完成指定任务
    /// </summary>
    [Serializable]
    public class QuestCompletionCondition : ExpansionConditionBase
    {
        [Serializable]
        public struct QuestRequirement
        {
            public string QuestId;           // 任务ID
            public bool RequireCompletion;   // 是否需要完成（true）或只需要接受（false）
            public string DisplayName;       // 显示名称
            public int RequiredProgress;     // 需要的进度（0-100）
        }

        [SerializeField] private QuestRequirement[] _requirements;

        public override ExpansionConditionType ConditionType => ExpansionConditionType.QuestRequirement;

        public QuestCompletionCondition() : base() { }

        public QuestCompletionCondition(
            string conditionId,
            string displayName,
            string description,
            QuestRequirement[] requirements,
            int priority = 0
        ) : base(conditionId, displayName, description, priority)
        {
            _requirements = requirements ?? Array.Empty<QuestRequirement>();
        }

        public override ExpansionConditionResult Validate()
        {
            if (_requirements == null || _requirements.Length == 0)
                return ExpansionConditionResult.Success(_conditionId);

            // TODO: 实现任务系统后，通过ServiceLocator获取任务系统验证
            // 目前返回成功，作为占位符
            var failedRequirements = new List<string>();
            for (int i = 0; i < _requirements.Length; i++)
            {
                var requirement = _requirements[i];
                // 模拟验证逻辑
                bool requirementMet = UnityEngine.Random.value > 0.3f; // 70%几率通过，用于测试

                if (!requirementMet)
                {
                    string displayName = string.IsNullOrEmpty(requirement.DisplayName) ? requirement.QuestId : requirement.DisplayName;
                    string requirementType = requirement.RequireCompletion ? "完成" : "接受";
                    failedRequirements.Add($"{displayName}（需要{requirementType}）");
                }
            }

            if (failedRequirements.Count > 0)
            {
                string reason = $"任务要求未满足：{string.Join("、", failedRequirements)}";
                return ExpansionConditionResult.Fail(_conditionId, reason, $"Quest requirements not met: {string.Join(", ", failedRequirements)}");
            }

            return ExpansionConditionResult.Success(_conditionId);
        }

        public override ExpansionConsumptionResult Consume()
        {
            // 任务条件不消耗任何资源
            return ExpansionConsumptionResult.SuccessResult(_conditionId);
        }

        public override string GetConditionDetails()
        {
            if (_requirements == null || _requirements.Length == 0)
                return "无需任务要求";

            var details = new List<string>();
            for (int i = 0; i < _requirements.Length; i++)
            {
                var req = _requirements[i];
                string displayName = string.IsNullOrEmpty(req.DisplayName) ? req.QuestId : req.DisplayName;
                string requirementType = req.RequireCompletion ? "完成" : "接受";
                string progressText = req.RequiredProgress > 0 ? $"，进度{req.RequiredProgress}%" : "";
                details.Add($"{displayName}（需要{requirementType}{progressText}）");
            }

            return $"任务要求：\n{string.Join("\n", details)}";
        }
    }
    #endregion

    #region 货币消耗条件
    /// <summary>
    /// 货币消耗条件：需要消耗特定类型的货币
    /// </summary>
    [Serializable]
    public class CurrencyConsumptionCondition : ExpansionConditionBase
    {
        [Serializable]
        public struct CurrencyRequirement
        {
            public string CurrencyType;       // 货币类型（gold, silver, credits等）
            public int RequiredAmount;        // 需要数量
            public bool ConsumeOnSuccess;     // 是否在成功时消耗
            public string DisplayName;        // 显示名称
        }

        [SerializeField] private CurrencyRequirement[] _requirements;

        public override ExpansionConditionType ConditionType => ExpansionConditionType.ResourceConsumption;

        public CurrencyConsumptionCondition() : base() { }

        public CurrencyConsumptionCondition(
            string conditionId,
            string displayName,
            string description,
            CurrencyRequirement[] requirements,
            int priority = 0
        ) : base(conditionId, displayName, description, priority)
        {
            _requirements = requirements ?? Array.Empty<CurrencyRequirement>();
        }

        public override ExpansionConditionResult Validate()
        {
            if (_requirements == null || _requirements.Length == 0)
                return ExpansionConditionResult.Success(_conditionId);

            // TODO: 实现货币系统后，通过ServiceLocator获取货币系统验证
            // 目前模拟验证逻辑
            var failedRequirements = new List<string>();
            for (int i = 0; i < _requirements.Length; i++)
            {
                var requirement = _requirements[i];
                // 模拟验证：随机生成玩家拥有的货币数量
                int playerCurrency = UnityEngine.Random.Range(0, requirement.RequiredAmount * 2);

                if (playerCurrency < requirement.RequiredAmount)
                {
                    string displayName = string.IsNullOrEmpty(requirement.DisplayName) ?
                        $"{requirement.CurrencyType}货币" : requirement.DisplayName;
                    failedRequirements.Add($"{displayName} ({playerCurrency}/{requirement.RequiredAmount})");
                }
            }

            if (failedRequirements.Count > 0)
            {
                string reason = $"货币不足：{string.Join("、", failedRequirements)}";
                return ExpansionConditionResult.Fail(_conditionId, reason, $"Insufficient currency: {string.Join(", ", failedRequirements)}");
            }

            return ExpansionConditionResult.Success(_conditionId);
        }

        public override ExpansionConsumptionResult Consume()
        {
            if (_requirements == null || _requirements.Length == 0)
                return ExpansionConsumptionResult.SuccessResult(_conditionId);

            // 先验证条件
            var validationResult = Validate();
            if (!validationResult.IsMet)
                return ExpansionConsumptionResult.FailResult(_conditionId, validationResult.FailedReason);

            // TODO: 实现货币系统后，通过ServiceLocator获取货币系统执行消耗
            // 目前模拟消耗逻辑
            var failedConsumptions = new List<string>();
            for (int i = 0; i < _requirements.Length; i++)
            {
                var requirement = _requirements[i];
                if (requirement.ConsumeOnSuccess)
                {
                    // 模拟消耗：90%几率成功
                    bool consumptionSuccess = UnityEngine.Random.value > 0.1f;

                    if (!consumptionSuccess)
                    {
                        string displayName = string.IsNullOrEmpty(requirement.DisplayName) ?
                            $"{requirement.CurrencyType}货币" : requirement.DisplayName;
                        failedConsumptions.Add(displayName);
                    }
                }
            }

            if (failedConsumptions.Count > 0)
            {
                string reason = $"货币扣除失败：{string.Join("、", failedConsumptions)}";
                return ExpansionConsumptionResult.FailResult(_conditionId, reason);
            }

            return ExpansionConsumptionResult.SuccessResult(_conditionId);
        }

        public override string GetConditionDetails()
        {
            if (_requirements == null || _requirements.Length == 0)
                return "无需货币消耗";

            var details = new List<string>();
            for (int i = 0; i < _requirements.Length; i++)
            {
                var req = _requirements[i];
                string displayName = string.IsNullOrEmpty(req.DisplayName) ?
                    $"{req.CurrencyType}货币" : req.DisplayName;
                string consumeText = req.ConsumeOnSuccess ? "（消耗）" : "（仅验证）";
                details.Add($"{displayName} ×{req.RequiredAmount}{consumeText}");
            }

            return $"货币需求：\n{string.Join("\n", details)}";
        }
    }
    #endregion

    #region 复合条件（将多个条件组合成一个逻辑条件）
    /// <summary>
    /// 复合条件：将多个子条件组合成一个逻辑条件
    /// 支持 AND、OR 逻辑运算
    /// </summary>
    [Serializable]
    public class CompositeExpansionCondition : ExpansionConditionBase
    {
        public enum CompositeLogic
        {
            AND,    // 所有子条件都必须满足
            OR,     // 至少一个子条件满足
            XOR     // 恰好一个子条件满足
        }

        [SerializeField] private CompositeLogic _logic;
        [SerializeField] private ExpansionConditionBase[] _subConditions;

        public CompositeLogic Logic => _logic;
        public ExpansionConditionBase[] SubConditions => _subConditions;

        public override ExpansionConditionType ConditionType => ExpansionConditionType.LevelRequirement; // 使用一个现有类型，或新增类型

        public CompositeExpansionCondition() : base() { }

        public CompositeExpansionCondition(
            string conditionId,
            string displayName,
            string description,
            CompositeLogic logic,
            ExpansionConditionBase[] subConditions,
            int priority = 0
        ) : base(conditionId, displayName, description, priority)
        {
            _logic = logic;
            _subConditions = subConditions ?? Array.Empty<ExpansionConditionBase>();
        }

        public override ExpansionConditionResult Validate()
        {
            if (_subConditions == null || _subConditions.Length == 0)
                return ExpansionConditionResult.Success(_conditionId);

            var subResults = new List<ExpansionConditionResult>();
            int metCount = 0;

            foreach (var subCondition in _subConditions)
            {
                var result = subCondition.Validate();
                subResults.Add(result);
                if (result.IsMet) metCount++;
            }

            bool isMet = false;
            string technicalReason = string.Empty;

            switch (_logic)
            {
                case CompositeLogic.AND:
                    isMet = metCount == _subConditions.Length;
                    if (!isMet)
                        technicalReason = $"AND逻辑失败：{metCount}/{_subConditions.Length} 个子条件满足";
                    break;

                case CompositeLogic.OR:
                    isMet = metCount > 0;
                    if (!isMet)
                        technicalReason = $"OR逻辑失败：0/{_subConditions.Length} 个子条件满足";
                    break;

                case CompositeLogic.XOR:
                    isMet = metCount == 1;
                    if (!isMet)
                        technicalReason = $"XOR逻辑失败：{metCount}/{_subConditions.Length} 个子条件满足（需要恰好1个）";
                    break;
            }

            if (isMet)
            {
                return ExpansionConditionResult.Success(_conditionId);
            }
            else
            {
                string failedReason = GetFailedReason(subResults);
                return ExpansionConditionResult.Fail(_conditionId, failedReason, technicalReason);
            }
        }

        public override ExpansionConsumptionResult Consume()
        {
            if (_subConditions == null || _subConditions.Length == 0)
                return ExpansionConsumptionResult.SuccessResult(_conditionId);

            // 验证条件是否满足
            var validationResult = Validate();
            if (!validationResult.IsMet)
                return ExpansionConsumptionResult.FailResult(_conditionId, validationResult.FailedReason);

            // 消耗所有需要消耗的子条件
            var failedConsumptions = new List<string>();
            foreach (var subCondition in _subConditions)
            {
                var consumptionResult = subCondition.Consume();
                if (!consumptionResult.Success)
                {
                    failedConsumptions.Add(subCondition.DisplayName);
                }
            }

            if (failedConsumptions.Count > 0)
            {
                string reason = $"子条件消耗失败：{string.Join("、", failedConsumptions)}";
                return ExpansionConsumptionResult.FailResult(_conditionId, reason);
            }

            return ExpansionConsumptionResult.SuccessResult(_conditionId);
        }

        public override string GetConditionDetails()
        {
            if (_subConditions == null || _subConditions.Length == 0)
                return "无子条件";

            var logicText = _logic switch
            {
                CompositeLogic.AND => "且",
                CompositeLogic.OR => "或",
                CompositeLogic.XOR => "异或",
                _ => "未知"
            };

            var details = new List<string> { $"逻辑：{logicText}" };
            for (int i = 0; i < _subConditions.Length; i++)
            {
                details.Add($"{i + 1}. {_subConditions[i].GetConditionDetails()}");
            }

            return $"复合条件（{logicText}）：\n{string.Join("\n", details)}";
        }

        private string GetFailedReason(List<ExpansionConditionResult> subResults)
        {
            var failedSubConditions = new List<string>();
            for (int i = 0; i < subResults.Count; i++)
            {
                if (!subResults[i].IsMet)
                {
                    string subConditionName = _subConditions[i].DisplayName;
                    failedSubConditions.Add($"{subConditionName}（{subResults[i].FailedReason}）");
                }
            }

            string logicText = _logic switch
            {
                CompositeLogic.AND => "需要满足所有条件",
                CompositeLogic.OR => "需要满足至少一个条件",
                CompositeLogic.XOR => "需要满足恰好一个条件",
                _ => "条件不满足"
            };

            if (failedSubConditions.Count > 0)
            {
                return $"{logicText}，但以下条件不满足：{string.Join("、", failedSubConditions)}";
            }

            return logicText;
        }
    }
    #endregion

    #region 时间窗口条件（限时条件）
    /// <summary>
    /// 时间窗口条件：只在特定时间段内有效
    /// </summary>
    [Serializable]
    public class TimeWindowCondition : ExpansionConditionBase
    {
        [Serializable]
        public struct TimeWindow
        {
            public int StartHour;        // 开始时间（小时，0-23）
            public int StartMinute;      // 开始时间（分钟，0-59）
            public int EndHour;          // 结束时间（小时，0-23）
            public int EndMinute;        // 结束时间（分钟，0-59）
            public bool IncludeEndTime;  // 是否包含结束时间
        }

        [SerializeField] private TimeWindow _timeWindow;
        [SerializeField] private bool _useRealTime = true; // true=现实时间，false=游戏内时间

        public override ExpansionConditionType ConditionType => ExpansionConditionType.TimeRequirement;

        public TimeWindowCondition() : base() { }

        public TimeWindowCondition(
            string conditionId,
            string displayName,
            string description,
            TimeWindow timeWindow,
            bool useRealTime = true,
            int priority = 0
        ) : base(conditionId, displayName, description, priority)
        {
            _timeWindow = timeWindow;
            _useRealTime = useRealTime;
        }

        public override ExpansionConditionResult Validate()
        {
            DateTime currentTime = _useRealTime ? DateTime.Now : GetGameTime();

            // 将当前时间转换为分钟数
            int currentTotalMinutes = currentTime.Hour * 60 + currentTime.Minute;
            int startTotalMinutes = _timeWindow.StartHour * 60 + _timeWindow.StartMinute;
            int endTotalMinutes = _timeWindow.EndHour * 60 + _timeWindow.EndMinute;

            bool isInWindow;

            if (startTotalMinutes <= endTotalMinutes)
            {
                // 同一天内的时间窗口
                isInWindow = currentTotalMinutes >= startTotalMinutes &&
                            (_timeWindow.IncludeEndTime ? currentTotalMinutes <= endTotalMinutes : currentTotalMinutes < endTotalMinutes);
            }
            else
            {
                // 跨天的时间窗口（例如 22:00 - 06:00）
                isInWindow = currentTotalMinutes >= startTotalMinutes ||
                            (_timeWindow.IncludeEndTime ? currentTotalMinutes <= endTotalMinutes : currentTotalMinutes < endTotalMinutes);
            }

            if (isInWindow)
            {
                return ExpansionConditionResult.Success(_conditionId);
            }
            else
            {
                string startTime = $"{_timeWindow.StartHour:D2}:{_timeWindow.StartMinute:D2}";
                string endTime = $"{_timeWindow.EndHour:D2}:{_timeWindow.EndMinute:D2}";
                string reason = $"时间窗口外（{startTime} - {endTime}）";
                return ExpansionConditionResult.Fail(_conditionId, reason, $"Current time {currentTime:HH:mm} not in window {startTime}-{endTime}");
            }
        }

        public override ExpansionConsumptionResult Consume()
        {
            // 时间窗口条件不消耗任何资源
            return ExpansionConsumptionResult.SuccessResult(_conditionId);
        }

        public override string GetConditionDetails()
        {
            string startTime = $"{_timeWindow.StartHour:D2}:{_timeWindow.StartMinute:D2}";
            string endTime = $"{_timeWindow.EndHour:D2}:{_timeWindow.EndMinute:D2}";
            string includeText = _timeWindow.IncludeEndTime ? "（包含结束时间）" : "";
            string timeType = _useRealTime ? "现实时间" : "游戏内时间";
            return $"需在{timeType} {startTime} - {endTime} 期间进行{includeText}";
        }

        private DateTime GetGameTime()
        {
            // TODO: 实现游戏时间获取逻辑
            // 目前返回模拟的游戏时间（现实时间减12小时）
            return DateTime.Now.AddHours(-12);
        }
    }
    #endregion
}