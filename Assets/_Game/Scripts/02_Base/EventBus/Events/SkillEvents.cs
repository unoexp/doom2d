// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/02_Base/EventBus/Events/SkillEvents.cs
// 技能系统事件定义
// ══════════════════════════════════════════════════════════════════════

/// <summary>技能经验获取事件</summary>
public struct SkillExpGainedEvent : IEvent
{
    public SkillType SkillType;
    public int ExpAmount;
    public int CurrentExp;
    public int ExpToNextLevel;
}

/// <summary>技能升级事件</summary>
public struct SkillLevelUpEvent : IEvent
{
    public SkillType SkillType;
    public int NewLevel;
    public string SkillName;
}
