// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/05_Show/Achievement/ViewModels/AchievementViewModel.cs
// 成就面板 ViewModel。纯C#类，持有UI状态。
// ══════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;

/// <summary>
/// 单个成就的显示数据
/// </summary>
public class AchievementDisplayData
{
    public string AchievementId;
    public string DisplayName;
    public string Description;
    public string Category;
    public bool IsUnlocked;
    public bool IsHidden;
}

/// <summary>
/// 成就面板 ViewModel。
/// </summary>
public class AchievementViewModel
{
    public List<AchievementDisplayData> Achievements { get; } = new List<AchievementDisplayData>();
    public int UnlockedCount { get; private set; }
    public int TotalCount { get; private set; }

    public event Action OnDataChanged;
    public event Action<string> OnAchievementUnlocked;

    public void SetAchievements(List<AchievementDisplayData> list, int unlocked, int total)
    {
        Achievements.Clear();
        Achievements.AddRange(list);
        UnlockedCount = unlocked;
        TotalCount = total;
        OnDataChanged?.Invoke();
    }

    public void NotifyUnlocked(string achievementId)
    {
        OnAchievementUnlocked?.Invoke(achievementId);
    }
}
