// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/01_Data/ScriptableObjects/Spawning/EnemyWaveConfigSO.cs
// 敌人刷新波次配置。纯数据，零运行时逻辑。
// ══════════════════════════════════════════════════════════════════════
using System;
using UnityEngine;

/// <summary>
/// 单个刷新条目。描述一种敌人的刷新参数。
/// </summary>
[Serializable]
public struct SpawnEntry
{
    [Tooltip("敌人定义")]
    public EnemyDefinitionSO Enemy;

    [Tooltip("刷新数量")]
    public int Count;

    [Tooltip("刷新概率 (0~1)")]
    [Range(0f, 1f)]
    public float SpawnChance;
}

/// <summary>
/// 敌人刷新波次配置 ScriptableObject。
/// 描述一个区域在不同时段的敌人刷新规则。
/// </summary>
[CreateAssetMenu(fileName = "EnemyWave_", menuName = "SurvivalGame/Spawning/Enemy Wave Config")]
public class EnemyWaveConfigSO : ScriptableObject
{
    [Header("区域信息")]
    [Tooltip("区域标识")]
    public string AreaId;

    [Tooltip("区域描述")]
    public string AreaDescription;

    [Header("昼间刷新")]
    [Tooltip("昼间刷新列表")]
    public SpawnEntry[] DaySpawns;

    [Header("夜间刷新")]
    [Tooltip("夜间刷新列表")]
    public SpawnEntry[] NightSpawns;

    [Header("暴风雪刷新")]
    [Tooltip("暴风雪期间额外刷新")]
    public SpawnEntry[] BlizzardSpawns;

    [Header("刷新参数")]
    [Tooltip("刷新间隔（秒）")]
    public float SpawnInterval = 60f;

    [Tooltip("同时存在最大数量")]
    public int MaxAliveCount = 5;

    [Tooltip("庇护所周围的安全半径（此范围内不刷新）")]
    public float ShelterSafeRadius = 30f;
}
