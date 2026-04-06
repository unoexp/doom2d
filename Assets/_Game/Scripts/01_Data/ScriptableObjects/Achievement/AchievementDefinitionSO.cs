// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/01_Data/ScriptableObjects/Achievement/AchievementDefinitionSO.cs
// 成就定义数据。纯数据，零运行时逻辑。
// 💡 新增成就只需创建 .asset 文件，无需改代码。
// ══════════════════════════════════════════════════════════════════════
using UnityEngine;

/// <summary>
/// 成就定义 ScriptableObject。
/// 描述一个成就的触发条件、目标值和展示信息。
/// </summary>
[CreateAssetMenu(fileName = "Achievement_", menuName = "SurvivalGame/Achievement/Achievement Definition")]
public class AchievementDefinitionSO : ScriptableObject
{
    [Header("基础信息")]
    [Tooltip("成就唯一ID")]
    public string AchievementId;

    [Tooltip("成就名称")]
    public string DisplayName;

    [TextArea]
    [Tooltip("成就描述")]
    public string Description;

    [Tooltip("成就图标")]
    public Sprite Icon;

    [Header("触发条件")]
    [Tooltip("条件类型")]
    public AchievementConditionType ConditionType;

    [Tooltip("目标ID（建筑ID/BOSS ID/NPC ID/任务ID，根据条件类型使用）")]
    public string TargetId;

    [Tooltip("目标数值（存活时长秒/击杀数量等）")]
    public float TargetValue;

    [Header("分类")]
    [Tooltip("成就分类标签（用于UI分组显示）")]
    public string Category;

    [Tooltip("是否隐藏（未解锁时不显示条件）")]
    public bool IsHidden;
}
