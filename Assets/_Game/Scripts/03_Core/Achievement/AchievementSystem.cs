// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/03_Core/Achievement/AchievementSystem.cs
// 成就系统。监听业务事件，检测成就条件，管理解锁状态。
// ══════════════════════════════════════════════════════════════════════
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 中央成就管理系统。
///
/// 核心职责：
///   · 管理所有成就定义和解锁状态
///   · 订阅业务事件检测成就条件
///   · 通过 EventBus 广播成就解锁
///
/// 设计说明：
///   · 成就定义通过 Inspector 中 AchievementDefinitionSO 数组配置
///   · 解锁状态通过 HashSet 管理，实现 ISaveable 持久化
/// </summary>
public class AchievementSystem : MonoBehaviour, ISaveable
{
    // ══════════════════════════════════════════════════════
    // 配置
    // ══════════════════════════════════════════════════════

    [Header("成就数据")]
    [SerializeField] private AchievementDefinitionSO[] _achievements;

    // ══════════════════════════════════════════════════════
    // 字段
    // ══════════════════════════════════════════════════════

    private readonly Dictionary<string, AchievementDefinitionSO> _definitionMap
        = new Dictionary<string, AchievementDefinitionSO>();

    private readonly HashSet<string> _unlocked = new HashSet<string>();

    /// <summary>累计存活时间（秒）</summary>
    private float _totalSurvivalTime;

    /// <summary>累计击杀数</summary>
    private int _totalKills;

    /// <summary>是否有过死亡</summary>
    private bool _hasDied;

    // ══════════════════════════════════════════════════════
    // ISaveable
    // ══════════════════════════════════════════════════════

    public string SaveKey => nameof(AchievementSystem);

    // ══════════════════════════════════════════════════════
    // 生命周期
    // ══════════════════════════════════════════════════════

    private void Awake()
    {
        ServiceLocator.Register<AchievementSystem>(this);

        if (_achievements != null)
        {
            for (int i = 0; i < _achievements.Length; i++)
            {
                var def = _achievements[i];
                if (def == null || string.IsNullOrEmpty(def.AchievementId)) continue;
                _definitionMap[def.AchievementId] = def;
            }
        }
    }

    private void Start()
    {
        if (ServiceLocator.TryGet<SaveLoadSystem>(out var saveSystem))
            saveSystem.Register(this);
    }

    private void OnEnable()
    {
        EventBus.Subscribe<EntityDiedEvent>(OnEntityDied);
        EventBus.Subscribe<PlayerDeadEvent>(OnPlayerDead);
        EventBus.Subscribe<BuildCompletedEvent>(OnBuildCompleted);
        EventBus.Subscribe<QuestCompletedEvent>(OnQuestCompleted);
        EventBus.Subscribe<POIDiscoveredEvent>(OnPOIDiscovered);
        EventBus.Subscribe<DiscoveryFoundEvent>(OnDiscoveryFound);
        EventBus.Subscribe<NPCTrustChangedEvent>(OnNPCTrustChanged);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<EntityDiedEvent>(OnEntityDied);
        EventBus.Unsubscribe<PlayerDeadEvent>(OnPlayerDead);
        EventBus.Unsubscribe<BuildCompletedEvent>(OnBuildCompleted);
        EventBus.Unsubscribe<QuestCompletedEvent>(OnQuestCompleted);
        EventBus.Unsubscribe<POIDiscoveredEvent>(OnPOIDiscovered);
        EventBus.Unsubscribe<DiscoveryFoundEvent>(OnDiscoveryFound);
        EventBus.Unsubscribe<NPCTrustChangedEvent>(OnNPCTrustChanged);
    }

    private void OnDestroy()
    {
        if (ServiceLocator.TryGet<SaveLoadSystem>(out var saveSystem))
            saveSystem.Unregister(this);

        ServiceLocator.Unregister<AchievementSystem>();
    }

    private void Update()
    {
        _totalSurvivalTime += Time.deltaTime;
        CheckTimedAchievements();
    }

    // ══════════════════════════════════════════════════════
    // 公有 API
    // ══════════════════════════════════════════════════════

    /// <summary>成就是否已解锁</summary>
    public bool IsUnlocked(string achievementId) => _unlocked.Contains(achievementId);

    /// <summary>获取所有成就定义</summary>
    public AchievementDefinitionSO[] GetAllAchievements() => _achievements;

    /// <summary>获取已解锁数量</summary>
    public int UnlockedCount => _unlocked.Count;

    // ══════════════════════════════════════════════════════
    // 内部方法
    // ══════════════════════════════════════════════════════

    private void Unlock(string achievementId)
    {
        if (_unlocked.Contains(achievementId)) return;
        if (!_definitionMap.TryGetValue(achievementId, out var def)) return;

        _unlocked.Add(achievementId);

        EventBus.Publish(new AchievementUnlockedEvent
        {
            AchievementId = achievementId,
            DisplayName = def.DisplayName,
            Description = def.Description
        });

        Debug.Log($"[AchievementSystem] 成就解锁: {def.DisplayName}");
    }

    private void CheckTimedAchievements()
    {
        foreach (var kvp in _definitionMap)
        {
            if (_unlocked.Contains(kvp.Key)) continue;
            var def = kvp.Value;

            if (def.ConditionType == AchievementConditionType.SurviveTime
                && _totalSurvivalTime >= def.TargetValue)
            {
                Unlock(kvp.Key);
            }
        }
    }

    private void CheckCondition(AchievementConditionType type, string targetId = null)
    {
        foreach (var kvp in _definitionMap)
        {
            if (_unlocked.Contains(kvp.Key)) continue;
            var def = kvp.Value;
            if (def.ConditionType != type) continue;

            if (!string.IsNullOrEmpty(def.TargetId)
                && !string.IsNullOrEmpty(targetId)
                && def.TargetId != targetId)
                continue;

            // 需要数值检查的类型
            if (type == AchievementConditionType.DefeatEnemy)
            {
                if (_totalKills >= def.TargetValue)
                    Unlock(kvp.Key);
            }
            else
            {
                Unlock(kvp.Key);
            }
        }
    }

    // ══════════════════════════════════════════════════════
    // 事件处理
    // ══════════════════════════════════════════════════════

    private void OnEntityDied(EntityDiedEvent evt)
    {
        if (evt.Cause == DeathCause.Combat && evt.KillerInstanceId != 0)
        {
            _totalKills++;
            CheckCondition(AchievementConditionType.DefeatEnemy);
            CheckCondition(AchievementConditionType.DefeatBoss, evt.EntityInstanceId.ToString());
        }
    }

    private void OnPlayerDead(PlayerDeadEvent evt) => _hasDied = true;

    private void OnBuildCompleted(BuildCompletedEvent evt)
        => CheckCondition(AchievementConditionType.BuildStructure, evt.BuildingId);

    private void OnQuestCompleted(QuestCompletedEvent evt)
        => CheckCondition(AchievementConditionType.QuestComplete, evt.QuestId);

    private void OnPOIDiscovered(POIDiscoveredEvent evt)
        => CheckCondition(AchievementConditionType.DiscoverAllPOI);

    private void OnDiscoveryFound(DiscoveryFoundEvent evt)
        => CheckCondition(AchievementConditionType.CollectAllLore);

    private void OnNPCTrustChanged(NPCTrustChangedEvent evt)
    {
        if (evt.NewTrust >= evt.MaxTrust)
            CheckCondition(AchievementConditionType.NPCTrustMax, evt.NPCId);
    }

    // ══════════════════════════════════════════════════════
    // ISaveable
    // ══════════════════════════════════════════════════════

    public object CaptureState()
    {
        return new AchievementSavePayload
        {
            UnlockedIds = new List<string>(_unlocked),
            TotalSurvivalTime = _totalSurvivalTime,
            TotalKills = _totalKills,
            HasDied = _hasDied
        };
    }

    public void RestoreState(object state)
    {
        AchievementSavePayload data;
        if (state is string json)
            data = JsonUtility.FromJson<AchievementSavePayload>(json);
        else if (state is AchievementSavePayload directData)
            data = directData;
        else
            return;

        _unlocked.Clear();
        if (data.UnlockedIds != null)
        {
            for (int i = 0; i < data.UnlockedIds.Count; i++)
                _unlocked.Add(data.UnlockedIds[i]);
        }
        _totalSurvivalTime = data.TotalSurvivalTime;
        _totalKills = data.TotalKills;
        _hasDied = data.HasDied;
    }
}

/// <summary>成就存档数据</summary>
[System.Serializable]
public class AchievementSavePayload
{
    public List<string> UnlockedIds = new List<string>();
    public float TotalSurvivalTime;
    public int TotalKills;
    public bool HasDied;
}
