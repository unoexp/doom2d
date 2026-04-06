// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/03_Core/Quest/QuestSystem.cs
// 任务系统核心。管理任务生命周期、目标进度、奖励发放。
// ══════════════════════════════════════════════════════════════════════
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 任务运行时状态
/// </summary>
public enum QuestState
{
    Inactive    = 0,    // 未激活
    Active      = 1,    // 进行中
    Completed   = 2,    // 已完成
    Failed      = 3,    // 已失败
}

/// <summary>
/// 单个任务的运行时数据
/// </summary>
public class QuestRuntimeData
{
    public string QuestId;
    public QuestState State;
    public int[] ObjectiveProgress;     // 每个目标的当前进度
    public bool[] ObjectiveCompleted;   // 每个目标是否完成
}

/// <summary>
/// 中央任务管理系统。
///
/// 核心职责：
///   · 管理所有已注册任务定义
///   · 跟踪任务激活/完成/失败状态
///   · 监听业务事件自动更新目标进度
///   · 管理任务奖励发放
///   · 通过 EventBus 广播任务状态变化
///
/// 设计说明：
///   · 任务定义通过 Inspector 中 QuestDefinitionSO 数组配置
///   · 运行时状态通过 QuestRuntimeData 字典管理
///   · 通过 QuestProgressCheckEvent 接收进度推送
///   · 同时自动订阅 ItemAddedToInventoryEvent、BuildCompletedEvent 等
/// </summary>
public class QuestSystem : MonoBehaviour, ISaveable, IQuestSystem
{
    // ══════════════════════════════════════════════════════
    // 配置
    // ══════════════════════════════════════════════════════

    [Header("任务数据")]
    [SerializeField] private QuestDefinitionSO[] _questDefinitions;

    // ══════════════════════════════════════════════════════
    // 字段
    // ══════════════════════════════════════════════════════

    /// <summary>QuestId → 定义（快速查找）</summary>
    private readonly Dictionary<string, QuestDefinitionSO> _definitionMap
        = new Dictionary<string, QuestDefinitionSO>();

    /// <summary>QuestId → 运行时数据</summary>
    private readonly Dictionary<string, QuestRuntimeData> _runtimeMap
        = new Dictionary<string, QuestRuntimeData>();

    private IInventorySystem _inventorySystem;

    // ══════════════════════════════════════════════════════
    // ISaveable
    // ══════════════════════════════════════════════════════

    public string SaveKey => nameof(QuestSystem);

    // ══════════════════════════════════════════════════════
    // 生命周期
    // ══════════════════════════════════════════════════════

    private void Awake()
    {
        ServiceLocator.Register<QuestSystem>(this);
        ServiceLocator.Register<IQuestSystem>(this);

        // 构建查找表
        if (_questDefinitions != null)
        {
            for (int i = 0; i < _questDefinitions.Length; i++)
            {
                var quest = _questDefinitions[i];
                if (quest == null || string.IsNullOrEmpty(quest.QuestId)) continue;
                _definitionMap[quest.QuestId] = quest;

                // 初始化运行时数据
                var runtime = new QuestRuntimeData
                {
                    QuestId = quest.QuestId,
                    State = QuestState.Inactive,
                    ObjectiveProgress = new int[quest.Objectives != null ? quest.Objectives.Length : 0],
                    ObjectiveCompleted = new bool[quest.Objectives != null ? quest.Objectives.Length : 0]
                };
                _runtimeMap[quest.QuestId] = runtime;
            }
        }
    }

    private void Start()
    {
        _inventorySystem = ServiceLocator.Get<IInventorySystem>();

        // 自动激活满足条件的任务
        CheckAutoActivation();
    }

    private void OnEnable()
    {
        // 通用进度推送
        EventBus.Subscribe<QuestProgressCheckEvent>(OnProgressCheck);

        // 自动监听常用业务事件
        EventBus.Subscribe<ItemAddedToInventoryEvent>(OnItemCollected);
        EventBus.Subscribe<CraftingResultEvent>(OnCraftingResult);
        EventBus.Subscribe<BuildCompletedEvent>(OnBuildCompleted);
        EventBus.Subscribe<EntityDiedEvent>(OnEntityDied);
        EventBus.Subscribe<ShelterStageChangedEvent>(OnShelterStageChanged);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<QuestProgressCheckEvent>(OnProgressCheck);
        EventBus.Unsubscribe<ItemAddedToInventoryEvent>(OnItemCollected);
        EventBus.Unsubscribe<CraftingResultEvent>(OnCraftingResult);
        EventBus.Unsubscribe<BuildCompletedEvent>(OnBuildCompleted);
        EventBus.Unsubscribe<EntityDiedEvent>(OnEntityDied);
        EventBus.Unsubscribe<ShelterStageChangedEvent>(OnShelterStageChanged);
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<QuestSystem>();
        ServiceLocator.Unregister<IQuestSystem>();
    }

    // ══════════════════════════════════════════════════════
    // 公有 API —— 查询
    // ══════════════════════════════════════════════════════

    /// <summary>获取任务定义</summary>
    public QuestDefinitionSO GetDefinition(string questId)
    {
        _definitionMap.TryGetValue(questId, out var def);
        return def;
    }

    /// <summary>获取任务运行时状态</summary>
    public QuestState GetQuestState(string questId)
    {
        return _runtimeMap.TryGetValue(questId, out var runtime)
            ? runtime.State : QuestState.Inactive;
    }

    /// <summary>获取任务运行时数据</summary>
    public QuestRuntimeData GetRuntimeData(string questId)
    {
        _runtimeMap.TryGetValue(questId, out var runtime);
        return runtime;
    }

    /// <summary>获取所有活跃任务</summary>
    public List<QuestDefinitionSO> GetActiveQuests()
    {
        var result = new List<QuestDefinitionSO>();
        foreach (var kvp in _runtimeMap)
        {
            if (kvp.Value.State == QuestState.Active)
            {
                if (_definitionMap.TryGetValue(kvp.Key, out var def))
                    result.Add(def);
            }
        }
        return result;
    }

    /// <summary>获取所有已完成任务</summary>
    public List<QuestDefinitionSO> GetCompletedQuests()
    {
        var result = new List<QuestDefinitionSO>();
        foreach (var kvp in _runtimeMap)
        {
            if (kvp.Value.State == QuestState.Completed)
            {
                if (_definitionMap.TryGetValue(kvp.Key, out var def))
                    result.Add(def);
            }
        }
        return result;
    }

    // ══════════════════════════════════════════════════════
    // 公有 API —— 任务操作
    // ══════════════════════════════════════════════════════

    /// <summary>激活/接取任务</summary>
    public bool ActivateQuest(string questId)
    {
        if (!_definitionMap.TryGetValue(questId, out var def)) return false;
        if (!_runtimeMap.TryGetValue(questId, out var runtime)) return false;

        if (runtime.State != QuestState.Inactive) return false;

        // 检查前置
        if (!string.IsNullOrEmpty(def.PrerequisiteQuestId))
        {
            if (GetQuestState(def.PrerequisiteQuestId) != QuestState.Completed)
                return false;
        }

        runtime.State = QuestState.Active;

        EventBus.Publish(new QuestActivatedEvent
        {
            QuestId = questId,
            DisplayName = def.DisplayName,
            IsMainQuest = def.IsMainQuest
        });

        Debug.Log($"[QuestSystem] 任务激活: {def.DisplayName}");
        return true;
    }

    /// <summary>强制完成任务（调试/作弊用）</summary>
    public void ForceComplete(string questId)
    {
        if (!_runtimeMap.TryGetValue(questId, out var runtime)) return;
        if (runtime.State != QuestState.Active) return;

        CompleteQuest(questId);
    }

    // ══════════════════════════════════════════════════════
    // 事件处理 —— 进度推送
    // ══════════════════════════════════════════════════════

    private void OnProgressCheck(QuestProgressCheckEvent evt)
    {
        UpdateObjectives(evt.ObjectiveType, evt.TargetId, evt.Amount);
    }

    private void OnItemCollected(ItemAddedToInventoryEvent evt)
    {
        UpdateObjectives(QuestObjectiveType.CollectItem, evt.ItemId, evt.Amount);
    }

    private void OnCraftingResult(CraftingResultEvent evt)
    {
        if (evt.Result == CraftingResult.Success)
        {
            UpdateObjectives(QuestObjectiveType.CraftItem, evt.OutputItemId, evt.OutputAmount);
        }
    }

    private void OnBuildCompleted(BuildCompletedEvent evt)
    {
        UpdateObjectives(QuestObjectiveType.BuildStructure, evt.BuildingId, 1);
    }

    private void OnEntityDied(EntityDiedEvent evt)
    {
        // 击杀目标 — 使用 InstanceId 转换（简化：传递空ID，由数量驱动）
        UpdateObjectives(QuestObjectiveType.DefeatEnemy, string.Empty, 1);
    }

    private void OnShelterStageChanged(ShelterStageChangedEvent evt)
    {
        // 庇护所阶段变化可能触发自动激活
        CheckAutoActivation();
    }

    // ══════════════════════════════════════════════════════
    // 内部方法
    // ══════════════════════════════════════════════════════

    /// <summary>更新所有活跃任务中匹配类型的目标</summary>
    private void UpdateObjectives(QuestObjectiveType type, string targetId, int amount)
    {
        foreach (var kvp in _runtimeMap)
        {
            var runtime = kvp.Value;
            if (runtime.State != QuestState.Active) continue;

            if (!_definitionMap.TryGetValue(kvp.Key, out var def)) continue;
            if (def.Objectives == null) continue;

            bool anyUpdated = false;

            for (int i = 0; i < def.Objectives.Length; i++)
            {
                if (runtime.ObjectiveCompleted[i]) continue;

                var objective = def.Objectives[i];
                if (objective.Type != type) continue;

                // 目标ID匹配（空字符串匹配所有）
                if (!string.IsNullOrEmpty(objective.TargetId)
                    && !string.IsNullOrEmpty(targetId)
                    && objective.TargetId != targetId)
                    continue;

                runtime.ObjectiveProgress[i] += amount;
                bool completed = runtime.ObjectiveProgress[i] >= objective.RequiredAmount;
                runtime.ObjectiveCompleted[i] = completed;
                anyUpdated = true;

                EventBus.Publish(new QuestObjectiveProgressEvent
                {
                    QuestId = kvp.Key,
                    ObjectiveIndex = i,
                    CurrentAmount = runtime.ObjectiveProgress[i],
                    RequiredAmount = objective.RequiredAmount,
                    IsCompleted = completed
                });
            }

            // 检查任务是否全部目标完成
            if (anyUpdated && CheckAllObjectivesCompleted(def, runtime))
            {
                CompleteQuest(kvp.Key);
            }
        }
    }

    /// <summary>检查任务所有必要目标是否完成</summary>
    private static bool CheckAllObjectivesCompleted(QuestDefinitionSO def, QuestRuntimeData runtime)
    {
        if (def.Objectives == null) return true;

        for (int i = 0; i < def.Objectives.Length; i++)
        {
            // 可选目标不影响完成
            if (def.Objectives[i].IsOptional) continue;
            if (!runtime.ObjectiveCompleted[i]) return false;
        }
        return true;
    }

    /// <summary>完成任务并发放奖励</summary>
    private void CompleteQuest(string questId)
    {
        if (!_runtimeMap.TryGetValue(questId, out var runtime)) return;
        if (!_definitionMap.TryGetValue(questId, out var def)) return;

        runtime.State = QuestState.Completed;

        // 发放奖励
        if (def.Rewards != null && _inventorySystem != null)
        {
            for (int i = 0; i < def.Rewards.Length; i++)
            {
                var reward = def.Rewards[i];
                if (reward.Item == null) continue;
                _inventorySystem.TryAddItem(reward.Item.ItemId, reward.Amount);
            }
        }

        EventBus.Publish(new QuestCompletedEvent
        {
            QuestId = questId,
            DisplayName = def.DisplayName,
            IsMainQuest = def.IsMainQuest
        });

        Debug.Log($"[QuestSystem] 任务完成: {def.DisplayName}");

        // 完成任务可能解锁新任务
        CheckAutoActivation();
    }

    /// <summary>检查并自动激活满足条件的任务</summary>
    private void CheckAutoActivation()
    {
        foreach (var kvp in _definitionMap)
        {
            var def = kvp.Value;
            if (!def.AutoAccept) continue;
            if (!_runtimeMap.TryGetValue(kvp.Key, out var runtime)) continue;
            if (runtime.State != QuestState.Inactive) continue;

            // 检查前置任务
            if (!string.IsNullOrEmpty(def.PrerequisiteQuestId)
                && GetQuestState(def.PrerequisiteQuestId) != QuestState.Completed)
                continue;

            // 检查庇护所阶段
            if (def.RequiredShelterStage > 0)
            {
                if (ServiceLocator.TryGet<BuildingSystem>(out var buildingSystem))
                {
                    if (buildingSystem.ShelterStage < def.RequiredShelterStage)
                        continue;
                }
                else
                {
                    continue;
                }
            }

            ActivateQuest(kvp.Key);
        }
    }

    // ══════════════════════════════════════════════════════
    // ISaveable
    // ══════════════════════════════════════════════════════

    public object CaptureState()
    {
        // 序列化所有任务运行时数据
        var saveData = new Dictionary<string, object>();
        foreach (var kvp in _runtimeMap)
        {
            saveData[kvp.Key] = new object[]
            {
                (int)kvp.Value.State,
                kvp.Value.ObjectiveProgress,
                kvp.Value.ObjectiveCompleted
            };
        }
        return saveData;
    }

    public void RestoreState(object state)
    {
        if (state is Dictionary<string, object> saveData)
        {
            foreach (var kvp in saveData)
            {
                if (!_runtimeMap.TryGetValue(kvp.Key, out var runtime)) continue;
                if (kvp.Value is object[] arr && arr.Length >= 3)
                {
                    runtime.State = (QuestState)(int)arr[0];
                    if (arr[1] is int[] progress)
                        runtime.ObjectiveProgress = progress;
                    if (arr[2] is bool[] completed)
                        runtime.ObjectiveCompleted = completed;
                }
            }
        }
    }
}
