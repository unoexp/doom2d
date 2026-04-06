// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/05_Show/Skill/ViewModels/SkillViewModel.cs
// 技能面板 ViewModel。纯C#类，持有UI状态。
// ══════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;

/// <summary>
/// 单个技能的显示数据
/// </summary>
public class SkillDisplayData
{
    public SkillType Type;
    public string Name;
    public int Level;
    public int CurrentExp;
    public int ExpToNextLevel;
    public float PrimaryBonus;
    public string PrimaryEffectText;
    public string SecondaryEffectText;
    public bool IsMaxLevel;
}

/// <summary>
/// 技能面板 ViewModel。
/// </summary>
public class SkillViewModel
{
    public List<SkillDisplayData> Skills { get; } = new List<SkillDisplayData>();

    public event Action OnDataChanged;
    public event Action<SkillType> OnSkillLevelUp;

    public void SetSkills(List<SkillDisplayData> skills)
    {
        Skills.Clear();
        Skills.AddRange(skills);
        OnDataChanged?.Invoke();
    }

    public void UpdateSkill(SkillDisplayData data)
    {
        for (int i = 0; i < Skills.Count; i++)
        {
            if (Skills[i].Type == data.Type)
            {
                Skills[i] = data;
                OnDataChanged?.Invoke();
                return;
            }
        }
    }

    public void NotifyLevelUp(SkillType type)
    {
        OnSkillLevelUp?.Invoke(type);
    }
}
