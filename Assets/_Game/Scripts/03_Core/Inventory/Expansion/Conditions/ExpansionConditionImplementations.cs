// 📁 03_Core/Inventory/Expansion/Conditions/ExpansionConditionImplementations.cs
// 扩展条件具体实现，包含所有需要运行时服务的条件类
// 🏗️ 从 01_Data 移至此处原因：条件验证/消耗逻辑需要通过 ServiceLocator 访问运行时服务，
//    属于核心业务层职责，不能放在纯数据层（01_Data）

using System;
using System.Collections.Generic;
using UnityEngine;
using SurvivalGame.Data.Inventory.Expansion;

namespace SurvivalGame.Core.Inventory.Expansion
{
    #region 资源消耗条件
    /// <summary>
    /// 资源消耗条件：需要消耗特定的物品
    /// 🏗️ 通过 IInventorySystem 验证和消耗背包物品（ServiceLocator 在 03_Core 中合法）
    /// </summary>
    [Serializable]
    public class ResourceConsumptionCondition : ExpansionConditionBase
    {
        [Serializable]
        public struct ResourceRequirement
        {
            public string ItemId;
            public int RequiredQuantity;
            public bool ConsumeOnSuccess;
            public string DisplayName;
        }

        [SerializeField] private ResourceRequirement[] _requirements;
        public ResourceRequirement[] Requirements => _requirements;

        public override ExpansionConditionType ConditionType => ExpansionConditionType.ResourceConsumption;

        public ResourceConsumptionCondition() : base() { }

        public ResourceConsumptionCondition(
            string conditionId, string displayName, string description,
            ResourceRequirement[] requirements, int priority = 0
        ) : base(conditionId, displayName, description, priority)
        {
            _requirements = requirements ?? Array.Empty<ResourceRequirement>();
        }

        public override ExpansionConditionResult Validate()
        {
            if (_requirements == null || _requirements.Length == 0)
                return ExpansionConditionResult.Success(_conditionId);

            var inventorySystem = ServiceLocator.Get<IInventorySystem>();
            if (inventorySystem == null)
                return ExpansionConditionResult.Fail(_conditionId, "背包系统不可用", "IInventorySystem not found in ServiceLocator");

            var failedRequirements = new List<string>();
            for (int i = 0; i < _requirements.Length; i++)
            {
                var requirement = _requirements[i];
                int availableCount = inventorySystem.GetTotalItemCount(requirement.ItemId);

                if (availableCount < requirement.RequiredQuantity)
                {
                    string name = string.IsNullOrEmpty(requirement.DisplayName) ? requirement.ItemId : requirement.DisplayName;
                    failedRequirements.Add($"{name} ({availableCount}/{requirement.RequiredQuantity})");
                }
            }

            if (failedRequirements.Count > 0)
            {
                string reason = $"缺少资源：{string.Join("、", failedRequirements)}";
                return ExpansionConditionResult.Fail(_conditionId, reason, $"Missing: {string.Join(", ", failedRequirements)}");
            }

            return ExpansionConditionResult.Success(_conditionId);
        }

        public override ExpansionConsumptionResult Consume()
        {
            if (_requirements == null || _requirements.Length == 0)
                return ExpansionConsumptionResult.SuccessResult(_conditionId);

            var inventorySystem = ServiceLocator.Get<IInventorySystem>();
            if (inventorySystem == null)
                return ExpansionConsumptionResult.FailResult(_conditionId, "背包系统不可用");

            var validationResult = Validate();
            if (!validationResult.IsMet)
                return ExpansionConsumptionResult.FailResult(_conditionId, validationResult.FailedReason);

            var failedConsumptions = new List<string>();
            for (int i = 0; i < _requirements.Length; i++)
            {
                var requirement = _requirements[i];
                if (requirement.ConsumeOnSuccess)
                {
                    bool removed = inventorySystem.TryRemoveItem(requirement.ItemId, requirement.RequiredQuantity);
                    if (!removed)
                    {
                        string name = string.IsNullOrEmpty(requirement.DisplayName) ? requirement.ItemId : requirement.DisplayName;
                        failedConsumptions.Add(name);
                    }
                }
            }

            if (failedConsumptions.Count > 0)
                return ExpansionConsumptionResult.FailResult(_conditionId, $"资源消耗失败：{string.Join("、", failedConsumptions)}");

            return ExpansionConsumptionResult.SuccessResult(_conditionId);
        }

        public override string GetConditionDetails()
        {
            if (_requirements == null || _requirements.Length == 0) return "无需资源消耗";
            var details = new List<string>();
            for (int i = 0; i < _requirements.Length; i++)
            {
                var req = _requirements[i];
                string name = string.IsNullOrEmpty(req.DisplayName) ? req.ItemId : req.DisplayName;
                details.Add($"{name} ×{req.RequiredQuantity}{(req.ConsumeOnSuccess ? "（消耗）" : "（仅验证）")}");
            }
            return $"资源需求：\n{string.Join("\n", details)}";
        }
    }
    #endregion

    #region 技能等级条件
    [Serializable]
    public class SkillRequirementCondition : ExpansionConditionBase
    {
        [Serializable]
        public struct SkillRequirement
        {
            public string SkillId;
            public int RequiredLevel;
            public string DisplayName;
        }

        [SerializeField] private SkillRequirement[] _requirements;
        public override ExpansionConditionType ConditionType => ExpansionConditionType.SkillRequirement;

        public SkillRequirementCondition() : base() { }
        public SkillRequirementCondition(string conditionId, string displayName, string description,
            SkillRequirement[] requirements, int priority = 0)
            : base(conditionId, displayName, description, priority)
        {
            _requirements = requirements ?? Array.Empty<SkillRequirement>();
        }

        public override ExpansionConditionResult Validate()
        {
            // TODO: 技能系统实现后，通过 ServiceLocator.Get<ISkillSystem>() 验证
            return ExpansionConditionResult.Success(_conditionId);
        }

        public override ExpansionConsumptionResult Consume() =>
            ExpansionConsumptionResult.SuccessResult(_conditionId);

        public override string GetConditionDetails()
        {
            if (_requirements == null || _requirements.Length == 0) return "无需技能要求";
            var details = new List<string>();
            foreach (var req in _requirements)
            {
                string name = string.IsNullOrEmpty(req.DisplayName) ? req.SkillId : req.DisplayName;
                details.Add($"{name} 等级 {req.RequiredLevel}+");
            }
            return $"技能要求：\n{string.Join("\n", details)}";
        }
    }
    #endregion

    #region 游戏进度条件
    [Serializable]
    public class ProgressRequirementCondition : ExpansionConditionBase
    {
        [Serializable]
        public struct ProgressRequirement
        {
            public string ProgressId;
            public bool RequiredCompletion;
            public string DisplayName;
        }

        [SerializeField] private ProgressRequirement[] _requirements;
        public override ExpansionConditionType ConditionType => ExpansionConditionType.ProgressRequirement;

        public ProgressRequirementCondition() : base() { }
        public ProgressRequirementCondition(string conditionId, string displayName, string description,
            ProgressRequirement[] requirements, int priority = 0)
            : base(conditionId, displayName, description, priority)
        {
            _requirements = requirements ?? Array.Empty<ProgressRequirement>();
        }

        public override ExpansionConditionResult Validate()
        {
            // TODO: 进度系统实现后，通过 ServiceLocator.Get<IProgressSystem>() 验证
            return ExpansionConditionResult.Success(_conditionId);
        }

        public override ExpansionConsumptionResult Consume() =>
            ExpansionConsumptionResult.SuccessResult(_conditionId);

        public override string GetConditionDetails()
        {
            if (_requirements == null || _requirements.Length == 0) return "无需进度要求";
            var details = new List<string>();
            foreach (var req in _requirements)
            {
                string name = string.IsNullOrEmpty(req.DisplayName) ? req.ProgressId : req.DisplayName;
                details.Add($"{name}{(req.RequiredCompletion ? "（已完成）" : "（已开始）")}");
            }
            return $"进度要求：\n{string.Join("\n", details)}";
        }
    }
    #endregion

    #region 前置扩展条件
    [Serializable]
    public class PrerequisiteExpansionCondition : ExpansionConditionBase
    {
        [Serializable]
        public struct ExpansionPrerequisite
        {
            public string ExpansionId;
            public int RequiredLevel;
            public string DisplayName;
        }

        [SerializeField] private ExpansionPrerequisite[] _prerequisites;
        public override ExpansionConditionType ConditionType => ExpansionConditionType.PrerequisiteExpansion;

        public PrerequisiteExpansionCondition() : base() { }
        public PrerequisiteExpansionCondition(string conditionId, string displayName, string description,
            ExpansionPrerequisite[] prerequisites, int priority = 0)
            : base(conditionId, displayName, description, priority)
        {
            _prerequisites = prerequisites ?? Array.Empty<ExpansionPrerequisite>();
        }

        public override ExpansionConditionResult Validate()
        {
            // TODO: 扩展记录系统实现后，通过 ServiceLocator.Get<IExpansionRecordService>() 验证
            return ExpansionConditionResult.Success(_conditionId);
        }

        public override ExpansionConsumptionResult Consume() =>
            ExpansionConsumptionResult.SuccessResult(_conditionId);

        public override string GetConditionDetails()
        {
            if (_prerequisites == null || _prerequisites.Length == 0) return "无需前置扩展";
            var details = new List<string>();
            foreach (var req in _prerequisites)
            {
                string name = string.IsNullOrEmpty(req.DisplayName) ? req.ExpansionId : req.DisplayName;
                string level = req.RequiredLevel > 0 ? $" 等级{req.RequiredLevel}+" : "";
                details.Add($"{name}{level}");
            }
            return $"前置扩展：\n{string.Join("\n", details)}";
        }
    }
    #endregion

    #region 时间要求条件
    [Serializable]
    public class TimeRequirementCondition : ExpansionConditionBase
    {
        [SerializeField] private int _requiredDays;
        [SerializeField] private bool _exactDay;

        public override ExpansionConditionType ConditionType => ExpansionConditionType.TimeRequirement;

        public TimeRequirementCondition() : base() { }
        public TimeRequirementCondition(string conditionId, string displayName, string description,
            int requiredDays, bool exactDay = false, int priority = 0)
            : base(conditionId, displayName, description, priority)
        {
            _requiredDays = Mathf.Max(0, requiredDays);
            _exactDay = exactDay;
        }

        public override ExpansionConditionResult Validate()
        {
            // TODO: 游戏时间系统实现后，通过 ServiceLocator.Get<IGameTimeSystem>() 验证
            return ExpansionConditionResult.Success(_conditionId);
        }

        public override ExpansionConsumptionResult Consume() =>
            ExpansionConsumptionResult.SuccessResult(_conditionId);

        public override string GetConditionDetails() =>
            $"{(_exactDay ? "在第" : "至少需要")}{_requiredDays}天";
    }
    #endregion

    #region 玩家等级条件
    [Serializable]
    public class LevelRequirementCondition : ExpansionConditionBase
    {
        [SerializeField] private int _requiredPlayerLevel;

        public override ExpansionConditionType ConditionType => ExpansionConditionType.LevelRequirement;

        public LevelRequirementCondition() : base() { }
        public LevelRequirementCondition(string conditionId, string displayName, string description,
            int requiredPlayerLevel, int priority = 0)
            : base(conditionId, displayName, description, priority)
        {
            _requiredPlayerLevel = Mathf.Max(1, requiredPlayerLevel);
        }

        public override ExpansionConditionResult Validate()
        {
            // TODO: 玩家等级系统实现后，通过 ServiceLocator.Get<IPlayerLevelSystem>() 验证
            return ExpansionConditionResult.Success(_conditionId);
        }

        public override ExpansionConsumptionResult Consume() =>
            ExpansionConsumptionResult.SuccessResult(_conditionId);

        public override string GetConditionDetails() =>
            $"玩家等级达到{_requiredPlayerLevel}级";
    }
    #endregion

    #region 任务完成条件
    [Serializable]
    public class QuestCompletionCondition : ExpansionConditionBase
    {
        [Serializable]
        public struct QuestRequirement
        {
            public string QuestId;
            public bool RequireCompletion;
            public string DisplayName;
            public int RequiredProgress;
        }

        [SerializeField] private QuestRequirement[] _requirements;
        public override ExpansionConditionType ConditionType => ExpansionConditionType.QuestRequirement;

        public QuestCompletionCondition() : base() { }
        public QuestCompletionCondition(string conditionId, string displayName, string description,
            QuestRequirement[] requirements, int priority = 0)
            : base(conditionId, displayName, description, priority)
        {
            _requirements = requirements ?? Array.Empty<QuestRequirement>();
        }

        public override ExpansionConditionResult Validate()
        {
            // TODO: 任务系统实现后，通过 ServiceLocator.Get<IQuestSystem>() 验证
            return ExpansionConditionResult.Success(_conditionId);
        }

        public override ExpansionConsumptionResult Consume() =>
            ExpansionConsumptionResult.SuccessResult(_conditionId);

        public override string GetConditionDetails()
        {
            if (_requirements == null || _requirements.Length == 0) return "无需任务要求";
            var details = new List<string>();
            foreach (var req in _requirements)
            {
                string name = string.IsNullOrEmpty(req.DisplayName) ? req.QuestId : req.DisplayName;
                string type = req.RequireCompletion ? "完成" : "接受";
                string progress = req.RequiredProgress > 0 ? $"，进度{req.RequiredProgress}%" : "";
                details.Add($"{name}（需要{type}{progress}）");
            }
            return $"任务要求：\n{string.Join("\n", details)}";
        }
    }
    #endregion

    #region 货币消耗条件
    [Serializable]
    public class CurrencyConsumptionCondition : ExpansionConditionBase
    {
        [Serializable]
        public struct CurrencyRequirement
        {
            public string CurrencyType;
            public int RequiredAmount;
            public bool ConsumeOnSuccess;
            public string DisplayName;
        }

        [SerializeField] private CurrencyRequirement[] _requirements;
        public override ExpansionConditionType ConditionType => ExpansionConditionType.ResourceConsumption;

        public CurrencyConsumptionCondition() : base() { }
        public CurrencyConsumptionCondition(string conditionId, string displayName, string description,
            CurrencyRequirement[] requirements, int priority = 0)
            : base(conditionId, displayName, description, priority)
        {
            _requirements = requirements ?? Array.Empty<CurrencyRequirement>();
        }

        public override ExpansionConditionResult Validate()
        {
            // TODO: 货币系统实现后，通过 ServiceLocator.Get<ICurrencySystem>() 验证
            return ExpansionConditionResult.Success(_conditionId);
        }

        public override ExpansionConsumptionResult Consume()
        {
            // TODO: 货币系统实现后，通过 ServiceLocator.Get<ICurrencySystem>() 执行消耗
            return ExpansionConsumptionResult.SuccessResult(_conditionId);
        }

        public override string GetConditionDetails()
        {
            if (_requirements == null || _requirements.Length == 0) return "无需货币消耗";
            var details = new List<string>();
            foreach (var req in _requirements)
            {
                string name = string.IsNullOrEmpty(req.DisplayName) ? $"{req.CurrencyType}货币" : req.DisplayName;
                details.Add($"{name} ×{req.RequiredAmount}{(req.ConsumeOnSuccess ? "（消耗）" : "（仅验证）")}");
            }
            return $"货币需求：\n{string.Join("\n", details)}";
        }
    }
    #endregion

    #region 复合条件
    [Serializable]
    public class CompositeExpansionCondition : ExpansionConditionBase
    {
        public enum CompositeLogic { AND, OR, XOR }

        [SerializeField] private CompositeLogic _logic;
        [SerializeField] private ExpansionConditionBase[] _subConditions;

        public CompositeLogic Logic => _logic;
        public ExpansionConditionBase[] SubConditions => _subConditions;

        public override ExpansionConditionType ConditionType => ExpansionConditionType.LevelRequirement;

        public CompositeExpansionCondition() : base() { }
        public CompositeExpansionCondition(string conditionId, string displayName, string description,
            CompositeLogic logic, ExpansionConditionBase[] subConditions, int priority = 0)
            : base(conditionId, displayName, description, priority)
        {
            _logic = logic;
            _subConditions = subConditions ?? Array.Empty<ExpansionConditionBase>();
        }

        public override ExpansionConditionResult Validate()
        {
            if (_subConditions == null || _subConditions.Length == 0)
                return ExpansionConditionResult.Success(_conditionId);

            int metCount = 0;
            var subResults = new List<ExpansionConditionResult>();

            foreach (var sub in _subConditions)
            {
                var result = sub.Validate();
                subResults.Add(result);
                if (result.IsMet) metCount++;
            }

            bool isMet = _logic switch
            {
                CompositeLogic.AND => metCount == _subConditions.Length,
                CompositeLogic.OR  => metCount > 0,
                CompositeLogic.XOR => metCount == 1,
                _                  => false
            };

            if (isMet) return ExpansionConditionResult.Success(_conditionId);

            string logicText = _logic switch
            {
                CompositeLogic.AND => "AND", CompositeLogic.OR => "OR", CompositeLogic.XOR => "XOR", _ => "?"
            };
            return ExpansionConditionResult.Fail(_conditionId,
                $"复合条件（{logicText}）未满足，{metCount}/{_subConditions.Length} 个子条件通过",
                $"Composite {logicText}: {metCount}/{_subConditions.Length} met");
        }

        public override ExpansionConsumptionResult Consume()
        {
            if (_subConditions == null || _subConditions.Length == 0)
                return ExpansionConsumptionResult.SuccessResult(_conditionId);

            var validationResult = Validate();
            if (!validationResult.IsMet)
                return ExpansionConsumptionResult.FailResult(_conditionId, validationResult.FailedReason);

            var failed = new List<string>();
            foreach (var sub in _subConditions)
            {
                var result = sub.Consume();
                if (!result.Success) failed.Add(sub.DisplayName);
            }

            return failed.Count > 0
                ? ExpansionConsumptionResult.FailResult(_conditionId, $"子条件消耗失败：{string.Join("、", failed)}")
                : ExpansionConsumptionResult.SuccessResult(_conditionId);
        }

        public override string GetConditionDetails()
        {
            if (_subConditions == null || _subConditions.Length == 0) return "无子条件";
            string logicText = _logic switch
            {
                CompositeLogic.AND => "且", CompositeLogic.OR => "或", CompositeLogic.XOR => "异或", _ => "?"
            };
            var details = new List<string> { $"逻辑：{logicText}" };
            for (int i = 0; i < _subConditions.Length; i++)
                details.Add($"{i + 1}. {_subConditions[i].GetConditionDetails()}");
            return $"复合条件（{logicText}）：\n{string.Join("\n", details)}";
        }
    }
    #endregion

    #region 时间窗口条件
    [Serializable]
    public class TimeWindowCondition : ExpansionConditionBase
    {
        [Serializable]
        public struct TimeWindow
        {
            public int StartHour;
            public int StartMinute;
            public int EndHour;
            public int EndMinute;
            public bool IncludeEndTime;
        }

        [SerializeField] private TimeWindow _timeWindow;
        [SerializeField] private bool _useRealTime = true;

        public override ExpansionConditionType ConditionType => ExpansionConditionType.TimeRequirement;

        public TimeWindowCondition() : base() { }
        public TimeWindowCondition(string conditionId, string displayName, string description,
            TimeWindow timeWindow, bool useRealTime = true, int priority = 0)
            : base(conditionId, displayName, description, priority)
        {
            _timeWindow = timeWindow;
            _useRealTime = useRealTime;
        }

        public override ExpansionConditionResult Validate()
        {
            DateTime now = _useRealTime ? DateTime.Now : DateTime.Now.AddHours(-12); // TODO: 接入游戏时间系统
            int cur = now.Hour * 60 + now.Minute;
            int start = _timeWindow.StartHour * 60 + _timeWindow.StartMinute;
            int end = _timeWindow.EndHour * 60 + _timeWindow.EndMinute;

            bool inWindow = start <= end
                ? cur >= start && (_timeWindow.IncludeEndTime ? cur <= end : cur < end)
                : cur >= start || (_timeWindow.IncludeEndTime ? cur <= end : cur < end);

            if (inWindow) return ExpansionConditionResult.Success(_conditionId);

            string s = $"{_timeWindow.StartHour:D2}:{_timeWindow.StartMinute:D2}";
            string e = $"{_timeWindow.EndHour:D2}:{_timeWindow.EndMinute:D2}";
            return ExpansionConditionResult.Fail(_conditionId, $"时间窗口外（{s} - {e}）",
                $"Current {now:HH:mm} not in {s}-{e}");
        }

        public override ExpansionConsumptionResult Consume() =>
            ExpansionConsumptionResult.SuccessResult(_conditionId);

        public override string GetConditionDetails()
        {
            string s = $"{_timeWindow.StartHour:D2}:{_timeWindow.StartMinute:D2}";
            string e = $"{_timeWindow.EndHour:D2}:{_timeWindow.EndMinute:D2}";
            return $"需在{(_useRealTime ? "现实时间" : "游戏内时间")} {s} - {e} 期间进行";
        }
    }
    #endregion
}
