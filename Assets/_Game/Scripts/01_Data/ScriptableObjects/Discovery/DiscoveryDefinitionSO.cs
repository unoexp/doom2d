// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/01_Data/ScriptableObjects/Discovery/DiscoveryDefinitionSO.cs
// 永久发现物定义数据。纯数据，零运行时逻辑。
// 💡 新增发现物只需创建 .asset 文件，无需改代码。
// ══════════════════════════════════════════════════════════════════════
using UnityEngine;

/// <summary>
/// 永久发现物定义 ScriptableObject。
/// 描述一个可在地层中发现的特殊物品，提供永久被动加成。
/// </summary>
[CreateAssetMenu(fileName = "Discovery_", menuName = "SurvivalGame/Discovery/Discovery Definition")]
public class DiscoveryDefinitionSO : ScriptableObject
{
    [Header("基础信息")]
    [Tooltip("发现物唯一ID")]
    public string DiscoveryId;

    [Tooltip("显示名称")]
    public string DisplayName;

    [TextArea]
    [Tooltip("描述")]
    public string Description;

    [Tooltip("图标")]
    public Sprite Icon;

    [Header("效果")]
    [Tooltip("效果类型")]
    public DiscoveryEffectType EffectType;

    [Tooltip("效果数值（百分比，如0.03表示+3%）")]
    public float EffectValue;

    [Header("来源")]
    [Tooltip("所在地层（L1-L6）")]
    public int SourceLayer;

    [Tooltip("所在区域描述")]
    public string SourceArea;
}
