// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/01_Data/ScriptableObjects/NPC/NPCRelationshipConfigSO.cs
// NPC 信任度配置数据。纯数据，零运行时逻辑。
// ══════════════════════════════════════════════════════════════════════
using System;
using UnityEngine;

/// <summary>
/// NPC 信任度阈值条目。定义到达指定信任度后解锁的内容。
/// </summary>
[Serializable]
public struct TrustThreshold
{
    [Tooltip("信任度阈值")]
    public int TrustLevel;

    [Tooltip("解锁描述")]
    public string UnlockDescription;

    [Tooltip("解锁的任务ID（可选）")]
    public string UnlockQuestId;

    [Tooltip("解锁的对话ID（可选）")]
    public string UnlockDialogId;
}

/// <summary>
/// NPC 信任度配置 ScriptableObject。
/// 描述一个NPC的信任度参数和阈值解锁内容。
/// </summary>
[CreateAssetMenu(fileName = "NPCRelation_", menuName = "SurvivalGame/NPC/Relationship Config")]
public class NPCRelationshipConfigSO : ScriptableObject
{
    [Header("基础信息")]
    [Tooltip("NPC 唯一ID")]
    public string NPCId;

    [Tooltip("NPC 显示名称")]
    public string DisplayName;

    [Header("信任度参数")]
    [Tooltip("初始信任度")]
    public int InitialTrust = 0;

    [Tooltip("最大信任度")]
    public int MaxTrust = 100;

    [Tooltip("交易解锁所需信任度")]
    public int TradeUnlockTrust = 10;

    [Tooltip("是否初始敌对（困难模式）")]
    public bool StartsHostile = false;

    [Header("信任度阈值")]
    [Tooltip("信任度阈值列表（按信任度升序）")]
    public TrustThreshold[] Thresholds;
}
