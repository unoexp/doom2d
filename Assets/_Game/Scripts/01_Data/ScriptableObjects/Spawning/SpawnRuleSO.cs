// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/01_Data/ScriptableObjects/Spawning/SpawnRuleSO.cs
// 刷新规则数据定义。描述一种实体的生成条件和参数。
// ══════════════════════════════════════════════════════════════════════
using System;
using UnityEngine;

/// <summary>
/// 刷新规则。定义何时、何地、以什么频率生成实体。
/// </summary>
[CreateAssetMenu(fileName = "SpawnRule_", menuName = "SurvivalGame/Spawning/Spawn Rule")]
public class SpawnRuleSO : ScriptableObject
{
    [Header("生成目标")]
    [Tooltip("要生成的预制体")]
    public GameObject Prefab;

    [Tooltip("规则ID")]
    public string RuleId;

    [Header("生成条件")]
    [Tooltip("允许生成的昼夜阶段（空 = 全时段）")]
    public DayPhase[] AllowedPhases;

    [Tooltip("最小生成距离（距离玩家）")]
    public float MinSpawnDistance = 10f;

    [Tooltip("最大生成距离（距离玩家）")]
    public float MaxSpawnDistance = 30f;

    [Header("数量控制")]
    [Tooltip("场景中最大同时存在数量")]
    public int MaxAlive = 5;

    [Tooltip("每次生成的数量范围")]
    public int MinSpawnCount = 1;
    public int MaxSpawnCount = 3;

    [Header("时间控制")]
    [Tooltip("生成间隔（秒）")]
    public float SpawnInterval = 30f;

    [Tooltip("初始延迟（秒）")]
    public float InitialDelay = 5f;

    [Header("启用控制")]
    public bool Enabled = true;
}
