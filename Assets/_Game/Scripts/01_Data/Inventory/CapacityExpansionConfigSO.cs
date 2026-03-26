// 📁 01_Data/Inventory/CapacityExpansionConfigSO.cs
// 背包容量扩展配置ScriptableObject
// 🏗️ 架构层级：01_Data - 纯数据定义层
// 🔧 职责：定义背包容量扩展的配置数据，支持数据驱动和MOD扩展

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalGame.Data.Inventory
{
    /// <summary>
    /// 扩展条件类型枚举
    /// 🔄 支持MOD系统扩展（预留MOD扩展值）
    /// </summary>
    public enum ExpansionRequirementType
    {
        ResourceCost = 0,       // 资源消耗（物品）
        SkillLevel = 1,         // 技能等级要求
        PlayerLevel = 2,        // 玩家等级要求
        QuestCompletion = 3,    // 任务完成要求
        TimeOfDay = 4,          // 时间要求
        WeatherCondition = 5,   // 天气条件
        ModExtensionStart = 100 // MOD扩展起始值
    }

    /// <summary>
    /// 扩展条件定义
    /// 📊 存储单个扩展条件的数据
    /// </summary>
    [Serializable]
    public struct ExpansionRequirement
    {
        [SerializeField] private ExpansionRequirementType _type;

        // 条件具体参数（根据类型不同含义不同）
        [SerializeField] private string _targetId;           // 目标ID（物品ID、技能ID、任务ID等）
        [SerializeField] private int _requiredValue;         // 要求的值（数量、等级等）
        [SerializeField] private float _requiredFloatValue;  // 浮点要求值
        [SerializeField] private string _description;        // 条件描述（用于UI显示）

        public ExpansionRequirementType Type => _type;
        public string TargetId => _targetId;
        public int RequiredValue => _requiredValue;
        public float RequiredFloatValue => _requiredFloatValue;
        public string Description => _description;

        // 工厂方法
        public static ExpansionRequirement CreateResourceCost(string itemId, int quantity, string description = null)
        {
            return new ExpansionRequirement
            {
                _type = ExpansionRequirementType.ResourceCost,
                _targetId = itemId,
                _requiredValue = quantity,
                _description = description ?? $"需要 {quantity}个 {itemId}"
            };
        }

        public static ExpansionRequirement CreateSkillLevel(string skillId, int level, string description = null)
        {
            return new ExpansionRequirement
            {
                _type = ExpansionRequirementType.SkillLevel,
                _targetId = skillId,
                _requiredValue = level,
                _description = description ?? $"需要 {skillId}技能 {level}级"
            };
        }

        public static ExpansionRequirement CreatePlayerLevel(int level, string description = null)
        {
            return new ExpansionRequirement
            {
                _type = ExpansionRequirementType.PlayerLevel,
                _targetId = null,
                _requiredValue = level,
                _description = description ?? $"需要玩家等级 {level}"
            };
        }
    }

    /// <summary>
    /// 扩展效果定义
    /// 📈 描述容量扩展带来的好处
    /// </summary>
    [Serializable]
    public struct ExpansionEffect
    {
        [SerializeField] private int _additionalSlots;     // 增加的槽位数量
        [SerializeField] private float _weightLimitBoost;  // 重量限制提升（百分比）
        [SerializeField] private bool _unlockSpecialSlots; // 是否解锁特殊槽位
        [SerializeField] private string[] _newSlotTypes;   // 新解锁的槽位类型
        [SerializeField] private string _description;      // 效果描述

        public int AdditionalSlots => _additionalSlots;
        public float WeightLimitBoost => _weightLimitBoost;
        public bool UnlockSpecialSlots => _unlockSpecialSlots;
        public string[] NewSlotTypes => _newSlotTypes ?? Array.Empty<string>();
        public string Description => _description;

        public bool HasEffect => _additionalSlots > 0 || _weightLimitBoost > 0 || _unlockSpecialSlots;

        public static ExpansionEffect CreateSlotExpansion(int slots, string description = null)
        {
            return new ExpansionEffect
            {
                _additionalSlots = slots,
                _weightLimitBoost = 0,
                _unlockSpecialSlots = false,
                _description = description ?? $"增加 {slots}个槽位"
            };
        }

        public static ExpansionEffect CreateWeightExpansion(float boostPercent, string description = null)
        {
            return new ExpansionEffect
            {
                _additionalSlots = 0,
                _weightLimitBoost = boostPercent,
                _unlockSpecialSlots = false,
                _description = description ?? $"负重上限提升 {boostPercent:F0}%"
            };
        }
    }

    /// <summary>
    /// 扩展级别定义
    /// 📊 单个扩展级别的完整配置
    /// </summary>
    [Serializable]
    public struct ExpansionLevel
    {
        [SerializeField] private string _levelId;                  // 级别唯一标识
        [SerializeField] private int _levelNumber;                 // 级别序号（1,2,3...）
        [SerializeField] private string _displayName;              // 显示名称（如"基础扩展"）
        [SerializeField] private string _description;              // 级别描述
        [SerializeField] private ExpansionRequirement[] _requirements; // 扩展条件
        [SerializeField] private ExpansionEffect _effects;         // 扩展效果
        [SerializeField] private string[] _prerequisiteLevelIds;   // 前置级别ID

        public string LevelId => _levelId;
        public int LevelNumber => _levelNumber;
        public string DisplayName => _displayName;
        public string Description => _description;
        public ExpansionRequirement[] Requirements => _requirements ?? Array.Empty<ExpansionRequirement>();
        public ExpansionEffect Effects => _effects;
        public string[] PrerequisiteLevelIds => _prerequisiteLevelIds ?? Array.Empty<string>();

        public bool HasRequirements => Requirements.Length > 0;
        public bool HasPrerequisites => PrerequisiteLevelIds.Length > 0;
    }

    /// <summary>
    /// 背包容量扩展配置
    /// 🗃️ 通过ScriptableObject配置背包类型的扩展规则
    /// 🎮 数据驱动设计：不同背包类型（主背包、箱子、马车）可配置不同的扩展规则
    /// </summary>
    [CreateAssetMenu(fileName = "CapacityExpansion_", menuName = "SurvivalGame/Inventory/Capacity Expansion")]
    public class CapacityExpansionConfigSO : ScriptableObject
    {
        [Header("基础配置")]
        [SerializeField] private string _expansionId;               // 扩展配置唯一标识
        [SerializeField] private string _targetContainerId;         // 目标容器ID（如"MainInventory"）
        [SerializeField] private string _displayName;               // 显示名称
        [SerializeField] private string _description;               // 配置描述

        [Header("扩展级别")]
        [SerializeField] private ExpansionLevel[] _expansionLevels; // 所有扩展级别

        [Header("扩展限制")]
        [SerializeField] private int _maxTotalExpansions = 5;       // 最大扩展次数
        [SerializeField] private bool _allowParallelExpansion;      // 是否允许多个扩展同时进行
        [SerializeField] private float _expansionDuration = 0f;     // 扩展耗时（0表示立即完成）

        [Header("视觉配置")]
        [SerializeField] private Sprite _icon;                      // 扩展图标
        [SerializeField] private Color _themeColor = Color.white;   // 主题颜色

        // 属性访问器
        public string ExpansionId => _expansionId;
        public string TargetContainerId => _targetContainerId;
        public string DisplayName => _displayName;
        public string Description => _description;
        public ExpansionLevel[] ExpansionLevels => _expansionLevels ?? Array.Empty<ExpansionLevel>();
        public int MaxTotalExpansions => _maxTotalExpansions;
        public bool AllowParallelExpansion => _allowParallelExpansion;
        public float ExpansionDuration => _expansionDuration;
        public Sprite Icon => _icon;
        public Color ThemeColor => _themeColor;

        // 辅助方法
        public int GetMaxLevelNumber()
        {
            int max = 0;
            foreach (var level in ExpansionLevels)
            {
                if (level.LevelNumber > max)
                    max = level.LevelNumber;
            }
            return max;
        }

        public ExpansionLevel? GetLevelById(string levelId)
        {
            foreach (var level in ExpansionLevels)
            {
                if (level.LevelId == levelId)
                    return level;
            }
            return null;
        }

        public ExpansionLevel? GetLevelByNumber(int levelNumber)
        {
            foreach (var level in ExpansionLevels)
            {
                if (level.LevelNumber == levelNumber)
                    return level;
            }
            return null;
        }

        public ExpansionLevel[] GetAvailableLevels(HashSet<string> completedLevelIds)
        {
            var available = new List<ExpansionLevel>();

            foreach (var level in ExpansionLevels)
            {
                // 检查是否已完成
                if (completedLevelIds.Contains(level.LevelId))
                    continue;

                // 检查前置条件
                bool prerequisitesMet = true;
                foreach (var prereqId in level.PrerequisiteLevelIds)
                {
                    if (!completedLevelIds.Contains(prereqId))
                    {
                        prerequisitesMet = false;
                        break;
                    }
                }

                if (prerequisitesMet)
                    available.Add(level);
            }

            return available.ToArray();
        }

        private void OnValidate()
        {
            // 自动生成ID
            if (string.IsNullOrEmpty(_expansionId))
                _expansionId = name;

            // 验证级别数据
            var levelIds = new HashSet<string>();
            for (int i = 0; i < _expansionLevels.Length; i++)
            {
                var level = _expansionLevels[i];

                // 确保级别ID唯一
                if (string.IsNullOrEmpty(level.LevelId))
                {
                    // 自动生成级别ID
                    #if UNITY_EDITOR
                    var so = new UnityEditor.SerializedObject(this);
                    var levelsProp = so.FindProperty("_expansionLevels");
                    var levelProp = levelsProp.GetArrayElementAtIndex(i);
                    levelProp.FindPropertyRelative("_levelId").stringValue =
                        $"{_expansionId}_Level_{level.LevelNumber}";
                    so.ApplyModifiedPropertiesWithoutUndo();
                    #endif
                }
                else
                {
                    // 检查重复
                    if (levelIds.Contains(level.LevelId))
                    {
                        Debug.LogWarning($"重复的扩展级别ID: {level.LevelId}");
                    }
                    else
                    {
                        levelIds.Add(level.LevelId);
                    }
                }
            }
        }
    }
}