// ─────────────────────────────────────────────────────────────────────
// 📁 Assets/_Game/02_Infrastructure/EventBus/Events/SurvivalEvents.cs
// 生存系统相关事件（补全 SurvivalStatusSystem 中引用的事件结构体）
// ─────────────────────────────────────────────────────────────────────

/// <summary>生存属性值变化事件</summary>
public struct SurvivalAttributeChangedEvent : IEvent
{
    public SurvivalAttributeType AttributeType;
    public float OldValue;
    public float NewValue;
    public float MaxValue;
}

/// <summary>属性进入临界区域预警事件（UI闪烁、音效提示等）</summary>
public struct SurvivalCriticalWarningEvent : IEvent
{
    public SurvivalAttributeType AttributeType;
    public CriticalWarningLevel  WarningLevel;
}

/// <summary>状态效果被施加</summary>
public struct StatusEffectAppliedEvent : IEvent
{
    public string EffectId;
    public string DisplayName;
    public float  Duration;     // -1 = 永久
}

/// <summary>状态效果被移除（到期或被治愈）</summary>
public struct StatusEffectRemovedEvent : IEvent
{
    public string EffectId;
}

/// <summary>玩家死亡事件（已在 GameEnums 中定义 DeathCause）</summary>
public struct PlayerDeadEvent : IEvent
{
    public DeathCause Cause;
}

/// <summary>生存属性预警事件（低于安全阈值时触发，用于 UI 提示和教程）</summary>
public struct SurvivalAttributeWarningEvent : IEvent
{
    public SurvivalAttributeType AttributeType;
    public float CurrentValue;
    public float WarningThreshold;
}

/// <summary>预警等级</summary>
public enum CriticalWarningLevel
{
    Warning = 0,    // 黄色预警（低于阈值）
    Danger  = 1,    // 橙色危险（低于 10%）
    Lethal  = 2,    // 红色致命（归零，正在扣血）
}
