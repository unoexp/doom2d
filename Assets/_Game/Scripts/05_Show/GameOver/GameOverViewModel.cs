// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/05_Show/GameOver/GameOverViewModel.cs
// 死亡结算ViewModel。管理结算界面的显示数据。
// ══════════════════════════════════════════════════════════════════════
using System;

/// <summary>
/// 死亡结算 ViewModel。
///
/// 核心职责：
///   · 存储死亡相关数据（死因、存活时间等）
///   · 暴露事件通知 View 更新
/// </summary>
public class GameOverViewModel
{
    // ══════════════════════════════════════════════════════
    // 数据
    // ══════════════════════════════════════════════════════

    private DeathCause _deathCause;
    private float _survivalTimeSeconds;

    // ══════════════════════════════════════════════════════
    // 事件
    // ══════════════════════════════════════════════════════

    /// <summary>结算数据更新</summary>
    public event Action OnDataUpdated;

    // ══════════════════════════════════════════════════════
    // 属性
    // ══════════════════════════════════════════════════════

    /// <summary>死亡原因</summary>
    public DeathCause DeathCause => _deathCause;

    /// <summary>死亡原因显示文本</summary>
    public string DeathCauseText => GetDeathCauseText(_deathCause);

    /// <summary>存活时间（格式化文本）</summary>
    public string SurvivalTimeText => FormatTime(_survivalTimeSeconds);

    /// <summary>存活时间（秒）</summary>
    public float SurvivalTimeSeconds => _survivalTimeSeconds;

    // ══════════════════════════════════════════════════════
    // 公有 API
    // ══════════════════════════════════════════════════════

    /// <summary>设置结算数据</summary>
    public void SetData(DeathCause cause, float survivalTimeSeconds)
    {
        _deathCause = cause;
        _survivalTimeSeconds = survivalTimeSeconds;
        OnDataUpdated?.Invoke();
    }

    // ══════════════════════════════════════════════════════
    // 辅助方法
    // ══════════════════════════════════════════════════════

    private static string GetDeathCauseText(DeathCause cause)
    {
        switch (cause)
        {
            case DeathCause.Combat:       return "战斗中阵亡";
            case DeathCause.Starvation:   return "饥饿致死";
            case DeathCause.Dehydration:  return "脱水致死";
            case DeathCause.Hypothermia:  return "冻死";
            case DeathCause.Hyperthermia: return "热死";
            case DeathCause.Suffocation:  return "窒息";
            case DeathCause.Infection:    return "感染致死";
            case DeathCause.Radiation:    return "辐射致死";
            case DeathCause.Fall:         return "坠亡";
            case DeathCause.Insanity:     return "精神崩溃";
            case DeathCause.Poison:       return "中毒致死";
            default:                      return "死因不明";
        }
    }

    private static string FormatTime(float totalSeconds)
    {
        int hours = (int)(totalSeconds / 3600f);
        int minutes = (int)((totalSeconds % 3600f) / 60f);
        int seconds = (int)(totalSeconds % 60f);

        if (hours > 0)
            return $"{hours}时{minutes}分{seconds}秒";
        if (minutes > 0)
            return $"{minutes}分{seconds}秒";
        return $"{seconds}秒";
    }
}
