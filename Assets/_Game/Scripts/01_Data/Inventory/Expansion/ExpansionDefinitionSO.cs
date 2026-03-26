// 📁 01_Data/Inventory/Expansion/ExpansionDefinitionSO.cs
// 背包扩展定义ScriptableObject，用于数据驱动配置扩展

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalGame.Data.Inventory.Expansion
{
    /// <summary>
    /// 扩展目标容器类型
    /// </summary>
    public enum ExpansionTargetContainer
    {
        MainInventory,      // 主背包
        QuickAccess,        // 快捷栏
        Both                // 两者都扩展
    }

    /// <summary>
    /// 扩展类型
    /// </summary>
    public enum ExpansionType
    {
        CapacityIncrease,           // 容量增加
        SlotTypeUpgrade,            // 槽位类型升级
        WeightLimitIncrease,        // 重量限制增加
        SpecialSlotAddition,        // 特殊槽位添加
        MultiEffect                 // 多重效果
    }

    /// <summary>
    /// 背包扩展效果定义
    /// </summary>
    [Serializable]
    public struct ExpansionEffect
    {
        public ExpansionType EffectType;

        // 容量增加效果
        public int AdditionalSlots;         // 增加的槽位数量

        // 槽位类型升级效果
        public SlotType TargetSlotType;     // 目标槽位类型
        public SlotType UpgradeSlotType;    // 升级后的槽位类型
        public int[] TargetSlotIndices;     // 目标槽位索引数组

        // 重量限制增加效果
        public float AdditionalWeightLimit; // 增加的重量限制

        // 特殊槽位添加效果
        public SlotType SpecialSlotType;    // 特殊槽位类型
        public int SpecialSlotCount;        // 特殊槽位数量

        // 通用属性
        public bool IsPermanent;            // 是否是永久效果
        public float DurationSeconds;       // 持续时间（秒），0表示永久
        public bool Stackable;              // 效果是否可叠加
        public int MaxStacks;               // 最大叠加层数
    }

    /// <summary>
    /// 背包扩展定义ScriptableObject
    /// 🏗️ 架构说明：纯数据定义，零运行时逻辑，支持数据驱动扩展配置
    /// </summary>
    [CreateAssetMenu(fileName = "Expansion_", menuName = "SurvivalGame/Inventory/Expansion")]
    public class ExpansionDefinitionSO : ScriptableObject
    {
        [Header("基础信息")]
        [SerializeField] private string _expansionId;
        [SerializeField] private string _displayName;
        [TextArea, SerializeField] private string _description;
        [SerializeField] private Sprite _icon;

        [Header("扩展目标")]
        [SerializeField] private ExpansionTargetContainer _targetContainer = ExpansionTargetContainer.MainInventory;
        [SerializeField] private string _specificContainerId;  // 指定容器ID（如果非标准容器）

        [Header("扩展效果")]
        [SerializeField] private ExpansionEffect[] _effects;

        [Header("扩展条件")]
        [SerializeField] private ExpansionConditionBase[] _conditions;

        [Header("扩展属性")]
        [SerializeField] private int _expansionLevel = 1;           // 扩展等级
        [SerializeField] private bool _repeatable = false;          // 是否可重复扩展
        [SerializeField] private int _maxRepeatCount = 1;           // 最大重复次数（如果可重复）
        [SerializeField] private float _cooldownSeconds = 0f;       // 冷却时间（秒）
        [SerializeField] private bool _requiresConfirmation = true; // 是否需要用户确认
        [SerializeField] private bool _showInUI = true;             // 是否在UI中显示
        [SerializeField] private int _sortOrder = 0;                // UI排序顺序

        [Header("扩展依赖")]
        [SerializeField] private string[] _prerequisiteExpansionIds; // 前置扩展ID
        [SerializeField] private bool _mutuallyExclusive = false;    // 是否与其他扩展互斥
        [SerializeField] private string[] _exclusiveExpansionIds;    // 互斥的扩展ID列表

        // ============ 属性访问器 ============
        public string ExpansionId => _expansionId;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public ExpansionTargetContainer TargetContainer => _targetContainer;
        public string SpecificContainerId => _specificContainerId;
        public ExpansionEffect[] Effects => _effects;
        public ExpansionConditionBase[] Conditions => _conditions;
        public int ExpansionLevel => _expansionLevel;
        public bool Repeatable => _repeatable;
        public int MaxRepeatCount => _maxRepeatCount;
        public float CooldownSeconds => _cooldownSeconds;
        public bool RequiresConfirmation => _requiresConfirmation;
        public bool ShowInUI => _showInUI;
        public int SortOrder => _sortOrder;
        public string[] PrerequisiteExpansionIds => _prerequisiteExpansionIds;
        public bool MutuallyExclusive => _mutuallyExclusive;
        public string[] ExclusiveExpansionIds => _exclusiveExpansionIds;

        // ============ 验证方法 ============
        private void OnValidate()
        {
            // 确保扩展ID不为空
            if (string.IsNullOrEmpty(_expansionId))
                _expansionId = name;

            // 确保至少有一个效果
            if (_effects == null || _effects.Length == 0)
            {
                Debug.LogWarning($"[{name}] 扩展定义没有配置效果，已添加默认容量增加效果");
                _effects = new ExpansionEffect[]
                {
                    new ExpansionEffect
                    {
                        EffectType = ExpansionType.CapacityIncrease,
                        AdditionalSlots = 4,
                        IsPermanent = true,
                        Stackable = true,
                        MaxStacks = 10
                    }
                };
            }

            // 验证效果配置
            foreach (var effect in _effects)
            {
                ValidateEffect(effect);
            }
        }

        private void ValidateEffect(ExpansionEffect effect)
        {
            switch (effect.EffectType)
            {
                case ExpansionType.CapacityIncrease:
                    if (effect.AdditionalSlots <= 0)
                        Debug.LogWarning($"[{name}] 容量增加效果的增加槽位数量必须大于0");
                    break;

                case ExpansionType.SlotTypeUpgrade:
                    if (effect.TargetSlotType == effect.UpgradeSlotType)
                        Debug.LogWarning($"[{name}] 槽位类型升级的目标类型和升级后类型相同");
                    if (effect.TargetSlotIndices == null || effect.TargetSlotIndices.Length == 0)
                        Debug.LogWarning($"[{name}] 槽位类型升级需要指定目标槽位索引");
                    break;

                case ExpansionType.WeightLimitIncrease:
                    if (effect.AdditionalWeightLimit <= 0)
                        Debug.LogWarning($"[{name}] 重量限制增加效果的增加值必须大于0");
                    break;

                case ExpansionType.SpecialSlotAddition:
                    if (effect.SpecialSlotCount <= 0)
                        Debug.LogWarning($"[{name}] 特殊槽位添加的数量必须大于0");
                    if (effect.SpecialSlotType == SlotType.Default)
                        Debug.LogWarning($"[{name}] 特殊槽位类型不应为Default");
                    break;

                case ExpansionType.MultiEffect:
                    // 多重效果需要子配置，这里只做基础验证
                    break;
            }

            if (!effect.IsPermanent && effect.DurationSeconds <= 0)
                Debug.LogWarning($"[{name}] 非永久效果需要指定大于0的持续时间");

            if (effect.Stackable && effect.MaxStacks <= 1)
                Debug.LogWarning($"[{name}] 可叠加效果的最大叠加层数应大于1");
        }

        // ============ 公共方法 ============
        /// <summary>获取扩展的总容量增加量</summary>
        public int GetTotalAdditionalSlots()
        {
            int total = 0;
            if (_effects != null)
            {
                foreach (var effect in _effects)
                {
                    if (effect.EffectType == ExpansionType.CapacityIncrease)
                        total += effect.AdditionalSlots;
                }
            }
            return total;
        }

        /// <summary>获取扩展的总重量限制增加量</summary>
        public float GetTotalAdditionalWeightLimit()
        {
            float total = 0;
            if (_effects != null)
            {
                foreach (var effect in _effects)
                {
                    if (effect.EffectType == ExpansionType.WeightLimitIncrease)
                        total += effect.AdditionalWeightLimit;
                }
            }
            return total;
        }

        /// <summary>获取扩展的槽位类型升级信息</summary>
        public List<(SlotType Target, SlotType Upgrade, int[] Indices)> GetSlotUpgradeInfo()
        {
            var upgrades = new List<(SlotType, SlotType, int[])>();
            if (_effects != null)
            {
                foreach (var effect in _effects)
                {
                    if (effect.EffectType == ExpansionType.SlotTypeUpgrade)
                        upgrades.Add((effect.TargetSlotType, effect.UpgradeSlotType, effect.TargetSlotIndices));
                }
            }
            return upgrades;
        }

        /// <summary>获取扩展的特殊槽位添加信息</summary>
        public List<(SlotType SlotType, int Count)> GetSpecialSlotAdditions()
        {
            var additions = new List<(SlotType, int)>();
            if (_effects != null)
            {
                foreach (var effect in _effects)
                {
                    if (effect.EffectType == ExpansionType.SpecialSlotAddition)
                        additions.Add((effect.SpecialSlotType, effect.SpecialSlotCount));
                }
            }
            return additions;
        }

        /// <summary>检查扩展是否是永久性的</summary>
        public bool IsPermanent()
        {
            if (_effects == null || _effects.Length == 0)
                return true;

            foreach (var effect in _effects)
            {
                if (!effect.IsPermanent)
                    return false;
            }
            return true;
        }

        /// <summary>获取扩展的效果描述（用于UI显示）</summary>
        public string GetEffectDescription()
        {
            var descriptions = new List<string>();

            if (_effects != null)
            {
                foreach (var effect in _effects)
                {
                    descriptions.Add(GetEffectDescription(effect));
                }
            }

            return string.Join("\n", descriptions);
        }

        private string GetEffectDescription(ExpansionEffect effect)
        {
            switch (effect.EffectType)
            {
                case ExpansionType.CapacityIncrease:
                    return $"+{effect.AdditionalSlots} 背包槽位";

                case ExpansionType.SlotTypeUpgrade:
                    return $"升级 {effect.TargetSlotType} 槽位为 {effect.UpgradeSlotType}";

                case ExpansionType.WeightLimitIncrease:
                    return $"+{effect.AdditionalWeightLimit} 负重上限";

                case ExpansionType.SpecialSlotAddition:
                    return $"添加 {effect.SpecialSlotCount} 个 {effect.SpecialSlotType} 槽位";

                case ExpansionType.MultiEffect:
                    return "多重扩展效果";

                default:
                    return "未知效果";
            }
        }

        /// <summary>获取扩展的条件描述（用于UI显示）</summary>
        public string GetConditionDescription()
        {
            if (_conditions == null || _conditions.Length == 0)
                return "无特殊要求";

            var descriptions = new List<string>();
            for (int i = 0; i < _conditions.Length; i++)
            {
                descriptions.Add($"{i + 1}. {_conditions[i].GetConditionDetails()}");
            }

            return string.Join("\n", descriptions);
        }

        /// <summary>检查是否有互斥的扩展</summary>
        public bool IsMutuallyExclusiveWith(string expansionId)
        {
            if (!_mutuallyExclusive || _exclusiveExpansionIds == null)
                return false;

            return Array.Exists(_exclusiveExpansionIds, id => id == expansionId);
        }
    }
}