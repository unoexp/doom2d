// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/01_Data/ScriptableObjects/Skill/SkillDefinitionSO.cs
// 技能定义数据。纯数据，零运行时逻辑。
// 💡 新增技能类型只需创建 .asset 文件，无需改代码。
// ══════════════════════════════════════════════════════════════════════
using UnityEngine;

/// <summary>
/// 技能定义 ScriptableObject。
/// 描述一种技能的基础属性、升级参数和每级效果。
/// </summary>
[CreateAssetMenu(fileName = "Skill_", menuName = "SurvivalGame/Skill/Skill Definition")]
public class SkillDefinitionSO : ScriptableObject
{
    [Header("基础信息")]
    [Tooltip("技能类型")]
    public SkillType SkillType;

    [Tooltip("显示名称")]
    public string DisplayName;

    [TextArea]
    [Tooltip("技能描述")]
    public string Description;

    [Tooltip("技能图标")]
    public Sprite Icon;

    [Header("升级参数")]
    [Tooltip("最大等级")]
    public int MaxLevel = 10;

    [Tooltip("基础升级经验")]
    public int BaseExpToLevel = 100;

    [Tooltip("经验增长指数（升级所需经验 = 基础经验 × 当前等级 ^ 此值）")]
    public float ExpGrowthExponent = 1.5f;

    [Header("每级效果")]
    [Tooltip("主要效果百分比增量（每级）")]
    public float PrimaryBonusPerLevel = 0.05f;

    [Tooltip("次要效果百分比增量（每级）")]
    public float SecondaryBonusPerLevel = 0.03f;

    [Tooltip("主要效果描述模板（用 {0} 代表百分比）")]
    public string PrimaryEffectTemplate = "效率+{0}%";

    [Tooltip("次要效果描述模板")]
    public string SecondaryEffectTemplate;

    /// <summary>计算指定等级的升级所需经验</summary>
    public int GetExpForLevel(int level)
    {
        return Mathf.RoundToInt(BaseExpToLevel * Mathf.Pow(level, ExpGrowthExponent));
    }
}
