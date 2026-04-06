// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/03_Core/Skill/SkillSystem.cs
// 技能熟练度系统。管理所有技能的经验累积和等级提升。
// ══════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单个技能的运行时数据
/// </summary>
[Serializable]
public class SkillRuntimeData
{
    public SkillType SkillType;
    public int Level;
    public int CurrentExp;
}

/// <summary>
/// 技能熟练度管理系统。
///
/// 核心职责：
///   · 管理所有技能的等级和经验
///   · 监听业务事件自动累积经验
///   · 计算升级并广播升级事件
///   · 提供技能加成查询接口
///
/// 设计说明：
///   · 技能定义通过 Inspector 中 SkillDefinitionSO 数组配置
///   · 自动订阅 EntityDiedEvent（战斗经验）、CraftingResultEvent（制作经验）等
///   · 通过 EventBus 广播经验获取和升级事件
/// </summary>
public class SkillSystem : MonoBehaviour, ISaveable
{
    // ══════════════════════════════════════════════════════
    // 配置
    // ══════════════════════════════════════════════════════

    [Header("技能数据")]
    [SerializeField] private SkillDefinitionSO[] _skillDefinitions;

    // ══════════════════════════════════════════════════════
    // 字段
    // ══════════════════════════════════════════════════════

    /// <summary>SkillType → 定义</summary>
    private readonly Dictionary<SkillType, SkillDefinitionSO> _definitionMap
        = new Dictionary<SkillType, SkillDefinitionSO>();

    /// <summary>SkillType → 运行时数据</summary>
    private readonly Dictionary<SkillType, SkillRuntimeData> _runtimeMap
        = new Dictionary<SkillType, SkillRuntimeData>();

    /// <summary>存活计时器（生存技能经验）</summary>
    private float _survivalTimer;

    private const float SURVIVAL_EXP_INTERVAL = 600f; // 10分钟给一次生存经验

    // ══════════════════════════════════════════════════════
    // ISaveable
    // ══════════════════════════════════════════════════════

    public string SaveKey => nameof(SkillSystem);

    // ══════════════════════════════════════════════════════
    // 生命周期
    // ══════════════════════════════════════════════════════

    private void Awake()
    {
        ServiceLocator.Register<SkillSystem>(this);

        if (_skillDefinitions != null)
        {
            for (int i = 0; i < _skillDefinitions.Length; i++)
            {
                var def = _skillDefinitions[i];
                if (def == null) continue;
                _definitionMap[def.SkillType] = def;

                _runtimeMap[def.SkillType] = new SkillRuntimeData
                {
                    SkillType = def.SkillType,
                    Level = 0,
                    CurrentExp = 0
                };
            }
        }
    }

    private void Start()
    {
        // 注册到存档系统
        if (ServiceLocator.TryGet<SaveLoadSystem>(out var saveSystem))
            saveSystem.Register(this);
    }

    private void OnEnable()
    {
        EventBus.Subscribe<EntityDiedEvent>(OnEntityDied);
        EventBus.Subscribe<CraftingResultEvent>(OnCraftingResult);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<EntityDiedEvent>(OnEntityDied);
        EventBus.Unsubscribe<CraftingResultEvent>(OnCraftingResult);
    }

    private void OnDestroy()
    {
        if (ServiceLocator.TryGet<SaveLoadSystem>(out var saveSystem))
            saveSystem.Unregister(this);

        ServiceLocator.Unregister<SkillSystem>();
    }

    private void Update()
    {
        // 生存技能：每10分钟给一次经验
        _survivalTimer += Time.deltaTime;
        if (_survivalTimer >= SURVIVAL_EXP_INTERVAL)
        {
            _survivalTimer -= SURVIVAL_EXP_INTERVAL;
            AddExp(SkillType.Survival, 15);
        }
    }

    // ══════════════════════════════════════════════════════
    // 公有 API
    // ══════════════════════════════════════════════════════

    /// <summary>获取技能当前等级</summary>
    public int GetLevel(SkillType type)
    {
        return _runtimeMap.TryGetValue(type, out var data) ? data.Level : 0;
    }

    /// <summary>获取技能当前经验</summary>
    public int GetCurrentExp(SkillType type)
    {
        return _runtimeMap.TryGetValue(type, out var data) ? data.CurrentExp : 0;
    }

    /// <summary>获取升级所需经验</summary>
    public int GetExpToNextLevel(SkillType type)
    {
        if (!_definitionMap.TryGetValue(type, out var def)) return int.MaxValue;
        if (!_runtimeMap.TryGetValue(type, out var data)) return int.MaxValue;
        if (data.Level >= def.MaxLevel) return 0;
        return def.GetExpForLevel(data.Level + 1);
    }

    /// <summary>获取技能主要加成（百分比，如 0.15 = 15%）</summary>
    public float GetPrimaryBonus(SkillType type)
    {
        if (!_definitionMap.TryGetValue(type, out var def)) return 0f;
        if (!_runtimeMap.TryGetValue(type, out var data)) return 0f;
        return data.Level * def.PrimaryBonusPerLevel;
    }

    /// <summary>获取技能次要加成</summary>
    public float GetSecondaryBonus(SkillType type)
    {
        if (!_definitionMap.TryGetValue(type, out var def)) return 0f;
        if (!_runtimeMap.TryGetValue(type, out var data)) return 0f;
        return data.Level * def.SecondaryBonusPerLevel;
    }

    /// <summary>添加经验</summary>
    public void AddExp(SkillType type, int amount)
    {
        if (!_definitionMap.TryGetValue(type, out var def)) return;
        if (!_runtimeMap.TryGetValue(type, out var data)) return;
        if (data.Level >= def.MaxLevel) return;

        data.CurrentExp += amount;

        // 检查升级
        int expNeeded = def.GetExpForLevel(data.Level + 1);
        while (data.CurrentExp >= expNeeded && data.Level < def.MaxLevel)
        {
            data.CurrentExp -= expNeeded;
            data.Level++;

            EventBus.Publish(new SkillLevelUpEvent
            {
                SkillType = type,
                NewLevel = data.Level,
                SkillName = def.DisplayName
            });

            Debug.Log($"[SkillSystem] {def.DisplayName} 升级到 Lv.{data.Level}");

            if (data.Level >= def.MaxLevel) break;
            expNeeded = def.GetExpForLevel(data.Level + 1);
        }

        EventBus.Publish(new SkillExpGainedEvent
        {
            SkillType = type,
            ExpAmount = amount,
            CurrentExp = data.CurrentExp,
            ExpToNextLevel = data.Level < def.MaxLevel ? def.GetExpForLevel(data.Level + 1) : 0
        });
    }

    // ══════════════════════════════════════════════════════
    // 事件处理
    // ══════════════════════════════════════════════════════

    private void OnEntityDied(EntityDiedEvent evt)
    {
        // 击败敌人获得战斗经验
        if (evt.Cause == DeathCause.Combat && evt.KillerInstanceId != 0)
            AddExp(SkillType.Combat, 20);
    }

    private void OnCraftingResult(CraftingResultEvent evt)
    {
        if (evt.Result == CraftingResult.Success)
            AddExp(SkillType.Crafting, 15);
    }

    // ══════════════════════════════════════════════════════
    // ISaveable
    // ══════════════════════════════════════════════════════

    public object CaptureState()
    {
        var list = new List<SkillRuntimeData>();
        foreach (var kvp in _runtimeMap)
            list.Add(kvp.Value);
        return list;
    }

    public void RestoreState(object state)
    {
        if (state is List<SkillRuntimeData> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var saved = list[i];
                if (_runtimeMap.TryGetValue(saved.SkillType, out var runtime))
                {
                    runtime.Level = saved.Level;
                    runtime.CurrentExp = saved.CurrentExp;
                }
            }
        }
    }
}
