// 📁 Assets/_Game/03_CoreSystems/SurvivalStatus/SurvivalStatusSystem.cs
// ─────────────────────────────────────────────────────────────────────
// 生存属性系统
// 职责：统一管理所有生存属性(血量/饥饿/口渴/体温等)的数值、
//       衰减、状态效果的挂载与 Tick、属性归零的后果触发。
// 依赖：EventBus / SurvivalConfigSO / IStatusEffect
// ─────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using UnityEngine;

public class SurvivalStatusSystem : MonoBehaviour, ISaveable
{
    // ══════════════════════════════════════════════════════
    // 字段
    // ══════════════════════════════════════════════════════

    [SerializeField] private SurvivalConfigSO _config;

    /// <summary>所有属性的运行时数据，由 Config 初始化</summary>
    private readonly Dictionary<SurvivalAttributeType, StatusAttribute> _attributes
        = new Dictionary<SurvivalAttributeType, StatusAttribute>();

    /// <summary>当前所有激活的状态效果</summary>
    private readonly List<ActiveStatusEffect> _activeEffects
        = new List<ActiveStatusEffect>();

    /// <summary>待移除列表，避免 Tick 中途修改集合</summary>
    private readonly List<ActiveStatusEffect> _pendingRemove
        = new List<ActiveStatusEffect>();

    private bool _isDead;

    // ══════════════════════════════════════════════════════
    // ISaveable
    // ══════════════════════════════════════════════════════

    public string SaveKey => nameof(SurvivalStatusSystem);

    // ══════════════════════════════════════════════════════
    // 生命周期
    // ══════════════════════════════════════════════════════

    private void Awake()
    {
        InitializeAttributes();
        ServiceLocator.Register<SurvivalStatusSystem>(this);
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<SurvivalStatusSystem>();
    }

    private void Update()
    {
        if (_isDead) return;

        float dt = Time.deltaTime;
        ApplyPassiveDecay(dt);
        TickStatusEffects(dt);
        CheckCriticalThresholds();
    }

    // ══════════════════════════════════════════════════════
    // 初始化
    // ══════════════════════════════════════════════════════

    /// <summary>根据 Config 初始化所有属性字典</summary>
    private void InitializeAttributes()
    {
        if (_config == null)
        {
            Debug.LogError("[SurvivalStatusSystem] 未绑定 SurvivalConfigSO！");
            return;
        }

        foreach (var entry in _config.InitialAttributes)
        {
            _attributes[entry.AttributeType] = new StatusAttribute(
                entry.AttributeType,
                entry.InitialValue,
                entry.MaxValue,
                entry.MinValue
            );
        }
    }

    // ══════════════════════════════════════════════════════
    // 公有 API —— 属性读写
    // ══════════════════════════════════════════════════════

    /// <summary>获取属性当前值，属性不存在时返回 0</summary>
    public float GetValue(SurvivalAttributeType type)
        => _attributes.TryGetValue(type, out var attr) ? attr.CurrentValue : 0f;

    /// <summary>获取属性最大值</summary>
    public float GetMaxValue(SurvivalAttributeType type)
        => _attributes.TryGetValue(type, out var attr) ? attr.MaxValue : 0f;

    /// <summary>获取属性归一化比例 [0,1]，用于 UI 进度条</summary>
    public float GetNormalized(SurvivalAttributeType type)
    {
        if (!_attributes.TryGetValue(type, out var attr)) return 0f;
        if (attr.MaxValue <= 0f) return 0f;
        return Mathf.Clamp01(attr.CurrentValue / attr.MaxValue);
    }

    /// <summary>
    /// 修改属性值（支持正负增量）。
    /// 所有外部修改（物品使用/战斗伤害/状态效果）统一走此入口。
    /// </summary>
    public void ModifyAttribute(SurvivalAttributeType type, float delta)
    {
        if (_isDead) return;
        if (!_attributes.TryGetValue(type, out var attr)) return;

        float oldValue = attr.CurrentValue;
        attr.ApplyDelta(delta);
        float newValue = attr.CurrentValue;

        if (Mathf.Approximately(oldValue, newValue)) return;

        // [PERF] 结构体事件，零GC
        EventBus.Publish(new SurvivalAttributeChangedEvent
        {
            AttributeType = type,
            OldValue      = oldValue,
            NewValue      = newValue,
            MaxValue      = attr.MaxValue
        });
    }

    /// <summary>直接设置属性为指定值（存档读取时使用）</summary>
    public void SetValue(SurvivalAttributeType type, float value)
    {
        if (!_attributes.TryGetValue(type, out var attr)) return;

        float old = attr.CurrentValue;
        attr.SetValue(value);

        EventBus.Publish(new SurvivalAttributeChangedEvent
        {
            AttributeType = type,
            OldValue      = old,
            NewValue      = attr.CurrentValue,
            MaxValue      = attr.MaxValue
        });
    }

    /// <summary>修改属性最大值（装备/升级影响上限时使用）</summary>
    public void ModifyMaxValue(SurvivalAttributeType type, float delta)
    {
        if (!_attributes.TryGetValue(type, out var attr)) return;
        attr.ModifyMax(delta);

        EventBus.Publish(new SurvivalAttributeChangedEvent
        {
            AttributeType = type,
            OldValue      = attr.CurrentValue,
            NewValue      = attr.CurrentValue,
            MaxValue      = attr.MaxValue
        });
    }

    // ══════════════════════════════════════════════════════
    // 公有 API —— 状态效果
    // ══════════════════════════════════════════════════════

    /// <summary>施加一个状态效果</summary>
    public void ApplyEffect(IStatusEffect effect)
    {
        if (effect == null) return;

        if (!effect.IsStackable)
        {
            // 非叠加效果：刷新已有实例的剩余时间
            var existing = _activeEffects.Find(e => e.Effect.EffectId == effect.EffectId);
            if (existing != null)
            {
                existing.ResetTimer();
                return;
            }
        }

        var activeEffect = new ActiveStatusEffect(effect);
        _activeEffects.Add(activeEffect);
        effect.OnApply(this);

        EventBus.Publish(new StatusEffectAppliedEvent
        {
            EffectId    = effect.EffectId,
            DisplayName = effect.DisplayName,
            Duration    = effect.Duration
        });
    }

    /// <summary>强制移除指定 ID 的状态效果（治疗/解毒等）</summary>
    public void RemoveEffect(string effectId)
    {
        for (int i = _activeEffects.Count - 1; i >= 0; i--)
        {
            if (_activeEffects[i].Effect.EffectId != effectId) continue;

            _activeEffects[i].Effect.OnRemove(this);
            _activeEffects.RemoveAt(i);

            EventBus.Publish(new StatusEffectRemovedEvent { EffectId = effectId });
            break; // 非叠加效果只移除第一个
        }
    }

    /// <summary>查询是否处于某状态效果中</summary>
    public bool HasEffect(string effectId)
        => _activeEffects.Exists(e => e.Effect.EffectId == effectId);

    /// <summary>获取当前所有激活效果（只读）</summary>
    public IReadOnlyList<ActiveStatusEffect> GetActiveEffects()
        => _activeEffects.AsReadOnly();

    // ══════════════════════════════════════════════════════
    // 公有 API —— 衰减倍率覆盖
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 外部衰减倍率叠加表。
    /// 天气/技能/装备 通过此接口影响某属性衰减速率，
    /// 无需修改 SurvivalStatusSystem 内部逻辑。
    /// Key = 属性类型，Value = 倍率叠加列表
    /// </summary>
    private readonly Dictionary<SurvivalAttributeType, List<float>> _decayMultipliers
        = new Dictionary<SurvivalAttributeType, List<float>>();

    /// <summary>注册衰减倍率（暴风雪/高温天气时调用）</summary>
    public void AddDecayMultiplier(SurvivalAttributeType type, float multiplier)
    {
        if (!_decayMultipliers.TryGetValue(type, out var list))
        {
            list = new List<float>();
            _decayMultipliers[type] = list;
        }
        list.Add(multiplier);
    }

    /// <summary>移除衰减倍率（天气结束时调用）</summary>
    public void RemoveDecayMultiplier(SurvivalAttributeType type, float multiplier)
    {
        if (!_decayMultipliers.TryGetValue(type, out var list)) return;
        list.Remove(multiplier);
    }

    private float GetTotalDecayMultiplier(SurvivalAttributeType type)
    {
        if (!_decayMultipliers.TryGetValue(type, out var list) || list.Count == 0)
            return 1f;

        float total = 1f;
        // [PERF] for 替代 foreach，避免 List 迭代器 GC
        for (int i = 0; i < list.Count; i++)
            total *= list[i];
        return total;
    }

    // ══════════════════════════════════════════════════════
    // 内部逻辑 —— 被动衰减
    // ══════════════════════════════════════════════════════

    /// <summary>每帧按 Config 配置执行被动衰减</summary>
    private void ApplyPassiveDecay(float dt)
    {
        // [PERF] for 替代 foreach，配置列表固定，无 GC
        var decayRules = _config.PassiveDecayRules;
        for (int i = 0; i < decayRules.Count; i++)
        {
            var rule = decayRules[i];
            if (!rule.EnableDecay) continue;

            float multiplier = GetTotalDecayMultiplier(rule.AttributeType);
            float delta = -rule.DecayRatePerSecond * multiplier * dt;
            ModifyAttribute(rule.AttributeType, delta);
        }
    }

    // ══════════════════════════════════════════════════════
    // 内部逻辑 —— 状态效果 Tick
    // ══════════════════════════════════════════════════════

    private void TickStatusEffects(float dt)
    {
        _pendingRemove.Clear();

        for (int i = 0; i < _activeEffects.Count; i++)
        {
            var active = _activeEffects[i];
            active.Effect.OnTick(this, dt);

            // 永久效果(Duration == -1)不计时
            if (active.Effect.Duration < 0f) continue;

            active.ReduceTimer(dt);
            if (active.RemainingTime <= 0f)
                _pendingRemove.Add(active);
        }

        // 统一移除到期效果
        for (int i = 0; i < _pendingRemove.Count; i++)
        {
            var expired = _pendingRemove[i];
            expired.Effect.OnRemove(this);
            _activeEffects.Remove(expired);

            EventBus.Publish(new StatusEffectRemovedEvent
            {
                EffectId = expired.Effect.EffectId
            });
        }
    }

    // ══════════════════════════════════════════════════════
    // 内部逻辑 —— 临界值检测
    // ══════════════════════════════════════════════════════

    /// <summary>每帧检测各属性是否触发临界后果</summary>
    private void CheckCriticalThresholds()
    {
        // ── 死亡判定（生命值 <= 0）──
        if (GetValue(SurvivalAttributeType.Health) <= 0f)
        {
            TriggerDeath(DeathCause.Combat);
            return;
        }

        // ── 饥饿致死（饥饿值 = 0 时持续扣血）──
        if (GetValue(SurvivalAttributeType.Hunger) <= 0f)
        {
            ModifyAttribute(SurvivalAttributeType.Health,
                -_config.StarvationDamagePerSecond * Time.deltaTime);

            EventBus.Publish(new SurvivalCriticalWarningEvent
            {
                AttributeType = SurvivalAttributeType.Hunger,
                WarningLevel  = CriticalWarningLevel.Lethal
            });
        }

        // ── 脱水致死（口渴值 = 0 时持续扣血，速度快于饥饿）──
        if (GetValue(SurvivalAttributeType.Thirst) <= 0f)
        {
            ModifyAttribute(SurvivalAttributeType.Health,
                -_config.DehydrationDamagePerSecond * Time.deltaTime);

            EventBus.Publish(new SurvivalCriticalWarningEvent
            {
                AttributeType = SurvivalAttributeType.Thirst,
                WarningLevel  = CriticalWarningLevel.Lethal
            });
        }

        // ── 低体温（持续扣血 + 警告）──
        float temp = GetValue(SurvivalAttributeType.Temperature);
        if (temp <= _config.HypothermiaThreshold)
        {
            ModifyAttribute(SurvivalAttributeType.Health,
                -_config.HypotherrmiaDamagePerSecond * Time.deltaTime);

            EventBus.Publish(new SurvivalCriticalWarningEvent
            {
                AttributeType = SurvivalAttributeType.Temperature,
                WarningLevel  = CriticalWarningLevel.Lethal
            });
        }
        // ── 高体温 ──
        else if (temp >= _config.HyperthermiaThreshold)
        {
            ModifyAttribute(SurvivalAttributeType.Health,
                -_config.HyperthermiadamagePerSecond * Time.deltaTime);

            EventBus.Publish(new SurvivalCriticalWarningEvent
            {
                AttributeType = SurvivalAttributeType.Temperature,
                WarningLevel  = CriticalWarningLevel.Lethal
            });
        }

        // ── 预警阶段（值低于警告阈值但未到致死）──
        CheckWarningThreshold(SurvivalAttributeType.Hunger,  _config.HungerWarningThreshold);
        CheckWarningThreshold(SurvivalAttributeType.Thirst,  _config.ThirstWarningThreshold);
        CheckWarningThreshold(SurvivalAttributeType.Health,  _config.HealthWarningThreshold);
    }

    private void CheckWarningThreshold(SurvivalAttributeType type, float threshold)
    {
        float normalized = GetNormalized(type);
        if (normalized > threshold) return;

        EventBus.Publish(new SurvivalCriticalWarningEvent
        {
            AttributeType = type,
            WarningLevel  = normalized <= 0f
                ? CriticalWarningLevel.Lethal
                : CriticalWarningLevel.Warning
        });
    }

    // ══════════════════════════════════════════════════════
    // 内部逻辑 —— 死亡处理
    // ══════════════════════════════════════════════════════

    private void TriggerDeath(DeathCause cause)
    {
        if (_isDead) return;
        _isDead = true;

        // 判定实际死因（优先级：口渴 > 饥饿 > 低温 > 高温 > 战斗）
        DeathCause actualCause = cause;
        if (GetValue(SurvivalAttributeType.Thirst) <= 0f)
            actualCause = DeathCause.Dehydration;
        else if (GetValue(SurvivalAttributeType.Hunger) <= 0f)
            actualCause = DeathCause.Starvation;
        else if (GetValue(SurvivalAttributeType.Temperature) <= _config.HypothermiaThreshold)
            actualCause = DeathCause.Hypothermia;
        else if (GetValue(SurvivalAttributeType.Temperature) >= _config.HyperthermiaThreshold)
            actualCause = DeathCause.Hyperthermia;

        EventBus.Publish(new PlayerDeadEvent { Cause = actualCause });
    }

    // ══════════════════════════════════════════════════════
    // ISaveable 实现
    // ══════════════════════════════════════════════════════

    public object CaptureState()
    {
        var data = new SurvivalSaveData();

        foreach (var kv in _attributes)
        {
            data.AttributeValues.Add(new SurvivalAttributeEntry(kv.Key, kv.Value.CurrentValue));
            data.AttributeMaxValues.Add(new SurvivalAttributeEntry(kv.Key, kv.Value.MaxValue));
        }

        // 持久化永久状态效果（Duration == -1），临时效果不存档
        foreach (var active in _activeEffects)
        {
            if (active.Effect.Duration < 0f)
                data.PermanentEffectIds.Add(active.Effect.EffectId);
        }

        return data;
    }

    public void RestoreState(object state)
    {
        // SaveLoadSystem 传入的是 JSON 字符串
        SurvivalSaveData data;
        if (state is string json)
            data = UnityEngine.JsonUtility.FromJson<SurvivalSaveData>(json);
        else if (state is SurvivalSaveData directData)
            data = directData;
        else
            return;

        for (int i = 0; i < data.AttributeValues.Count; i++)
        {
            var entry = data.AttributeValues[i];
            SetValue(entry.Type, entry.Value);
        }

        for (int i = 0; i < data.AttributeMaxValues.Count; i++)
        {
            var entry = data.AttributeMaxValues[i];
            if (_attributes.TryGetValue(entry.Type, out var attr))
                attr.SetMax(entry.Value);
        }
        // 永久效果由 SaveLoadSystem 统一重建，此处不处理
    }
}