// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/03_Core/Spawning/SpawnManager.cs
// 刷新管理系统。根据 SpawnRuleSO 定时生成敌人/资源。
// ══════════════════════════════════════════════════════════════════════
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 中央刷新管理系统。
///
/// 核心职责：
///   · 管理所有刷新规则
///   · 按时间间隔和条件执行刷新
///   · 跟踪每条规则的存活实体数量
///   · 通过 ObjectPoolManager 管理实体生命周期
/// </summary>
public class SpawnManager : MonoBehaviour
{
    // ══════════════════════════════════════════════════════
    // 配置
    // ══════════════════════════════════════════════════════

    [Header("刷新规则")]
    [SerializeField] private SpawnRuleSO[] _spawnRules;

    // ══════════════════════════════════════════════════════
    // 内部数据
    // ══════════════════════════════════════════════════════

    private class RuleState
    {
        public SpawnRuleSO Rule;
        public float Timer;
        public readonly List<GameObject> AliveEntities = new List<GameObject>();
    }

    private readonly List<RuleState> _ruleStates = new List<RuleState>();
    private Transform _playerTransform;

    // ══════════════════════════════════════════════════════
    // 生命周期
    // ══════════════════════════════════════════════════════

    private void Awake()
    {
        ServiceLocator.Register<SpawnManager>(this);
    }

    private void Start()
    {
        // 获取玩家位置
        var player = GameObject.FindWithTag("Player");
        _playerTransform = player != null ? player.transform : null;

        // 初始化规则状态
        if (_spawnRules != null)
        {
            for (int i = 0; i < _spawnRules.Length; i++)
            {
                var rule = _spawnRules[i];
                if (rule == null || !rule.Enabled) continue;

                _ruleStates.Add(new RuleState
                {
                    Rule = rule,
                    Timer = -rule.InitialDelay // 负值表示初始延迟
                });
            }
        }
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<SpawnManager>();
    }

    private void Update()
    {
        if (_playerTransform == null) return;

        for (int i = 0; i < _ruleStates.Count; i++)
        {
            var state = _ruleStates[i];
            state.Timer += Time.deltaTime;

            if (state.Timer < state.Rule.SpawnInterval) continue;

            // 清理已销毁的实体引用
            CleanDeadEntities(state);

            // 检查是否满足生成条件
            if (!CanSpawn(state)) continue;

            // 执行生成
            int count = Random.Range(state.Rule.MinSpawnCount, state.Rule.MaxSpawnCount + 1);
            int maxCanSpawn = state.Rule.MaxAlive - state.AliveEntities.Count;
            count = Mathf.Min(count, maxCanSpawn);

            for (int j = 0; j < count; j++)
            {
                SpawnEntity(state);
            }

            state.Timer = 0f;
        }
    }

    // ══════════════════════════════════════════════════════
    // 公有 API
    // ══════════════════════════════════════════════════════

    /// <summary>设置玩家引用（运行时更新）</summary>
    public void SetPlayer(Transform player) => _playerTransform = player;

    /// <summary>启用/禁用指定规则</summary>
    public void SetRuleEnabled(string ruleId, bool enabled)
    {
        for (int i = 0; i < _ruleStates.Count; i++)
        {
            if (_ruleStates[i].Rule.RuleId == ruleId)
                _ruleStates[i].Rule.Enabled = enabled;
        }
    }

    // ══════════════════════════════════════════════════════
    // 内部方法
    // ══════════════════════════════════════════════════════

    private bool CanSpawn(RuleState state)
    {
        var rule = state.Rule;
        if (!rule.Enabled) return false;
        if (state.AliveEntities.Count >= rule.MaxAlive) return false;
        if (rule.Prefab == null) return false;

        // 昼夜阶段检查
        if (rule.AllowedPhases != null && rule.AllowedPhases.Length > 0)
        {
            if (!ServiceLocator.TryGet<DayNightCycle>(out var dayNight)) return true;

            bool phaseAllowed = false;
            for (int i = 0; i < rule.AllowedPhases.Length; i++)
            {
                if (rule.AllowedPhases[i] == dayNight.CurrentPhase)
                {
                    phaseAllowed = true;
                    break;
                }
            }
            if (!phaseAllowed) return false;
        }

        return true;
    }

    private void SpawnEntity(RuleState state)
    {
        var rule = state.Rule;

        // 计算生成位置（玩家周围，避开太近的位置）
        Vector2 spawnPos = GetSpawnPosition(rule.MinSpawnDistance, rule.MaxSpawnDistance);

        GameObject entity;
        if (ServiceLocator.TryGet<ObjectPoolManager>(out var pool))
            entity = pool.Get(rule.Prefab, spawnPos);
        else
            entity = Instantiate(rule.Prefab, spawnPos, Quaternion.identity);

        state.AliveEntities.Add(entity);
    }

    private Vector2 GetSpawnPosition(float minDist, float maxDist)
    {
        // 在玩家左右随机生成
        float dir = Random.value > 0.5f ? 1f : -1f;
        float dist = Random.Range(minDist, maxDist);
        Vector2 basePos = (Vector2)_playerTransform.position + new Vector2(dir * dist, 0f);

        return basePos;
    }

    /// <summary>清理已销毁或已回收的实体引用</summary>
    private void CleanDeadEntities(RuleState state)
    {
        for (int i = state.AliveEntities.Count - 1; i >= 0; i--)
        {
            if (state.AliveEntities[i] == null || !state.AliveEntities[i].activeInHierarchy)
                state.AliveEntities.RemoveAt(i);
        }
    }
}
