// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/03_Core/NPC/NPCRelationshipSystem.cs
// NPC 信任度管理系统。管理所有NPC的信任度状态和交互条件。
// ══════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// NPC 信任度运行时数据
/// </summary>
[Serializable]
public class NPCTrustData
{
    public string NPCId;
    public int CurrentTrust;
}

/// <summary>
/// NPC 信任度管理系统。
///
/// 核心职责：
///   · 管理所有NPC的信任度数值
///   · 判断信任度阈值是否达成
///   · 通过 EventBus 广播信任度变化和阈值达成事件
///
/// 设计说明：
///   · NPC配置通过 Inspector 中 NPCRelationshipConfigSO 数组配置
///   · 信任度变化通过 ModifyTrust() 接口或事件触发
/// </summary>
public class NPCRelationshipSystem : MonoBehaviour, ISaveable
{
    // ══════════════════════════════════════════════════════
    // 配置
    // ══════════════════════════════════════════════════════

    [Header("NPC 关系配置")]
    [SerializeField] private NPCRelationshipConfigSO[] _npcConfigs;

    // ══════════════════════════════════════════════════════
    // 字段
    // ══════════════════════════════════════════════════════

    private readonly Dictionary<string, NPCRelationshipConfigSO> _configMap
        = new Dictionary<string, NPCRelationshipConfigSO>();

    private readonly Dictionary<string, NPCTrustData> _trustMap
        = new Dictionary<string, NPCTrustData>();

    /// <summary>已触发的阈值（NPCId + TrustLevel 组合），避免重复触发</summary>
    private readonly HashSet<string> _triggeredThresholds = new HashSet<string>();

    // ══════════════════════════════════════════════════════
    // ISaveable
    // ══════════════════════════════════════════════════════

    public string SaveKey => nameof(NPCRelationshipSystem);

    // ══════════════════════════════════════════════════════
    // 生命周期
    // ══════════════════════════════════════════════════════

    private void Awake()
    {
        ServiceLocator.Register<NPCRelationshipSystem>(this);

        if (_npcConfigs != null)
        {
            for (int i = 0; i < _npcConfigs.Length; i++)
            {
                var cfg = _npcConfigs[i];
                if (cfg == null || string.IsNullOrEmpty(cfg.NPCId)) continue;
                _configMap[cfg.NPCId] = cfg;

                _trustMap[cfg.NPCId] = new NPCTrustData
                {
                    NPCId = cfg.NPCId,
                    CurrentTrust = cfg.InitialTrust
                };
            }
        }
    }

    private void Start()
    {
        if (ServiceLocator.TryGet<SaveLoadSystem>(out var saveSystem))
            saveSystem.Register(this);
    }

    private void OnDestroy()
    {
        if (ServiceLocator.TryGet<SaveLoadSystem>(out var saveSystem))
            saveSystem.Unregister(this);

        ServiceLocator.Unregister<NPCRelationshipSystem>();
    }

    // ══════════════════════════════════════════════════════
    // 公有 API
    // ══════════════════════════════════════════════════════

    /// <summary>获取NPC当前信任度</summary>
    public int GetTrust(string npcId)
    {
        return _trustMap.TryGetValue(npcId, out var data) ? data.CurrentTrust : 0;
    }

    /// <summary>获取NPC最大信任度</summary>
    public int GetMaxTrust(string npcId)
    {
        return _configMap.TryGetValue(npcId, out var cfg) ? cfg.MaxTrust : 100;
    }

    /// <summary>是否可以与NPC交易</summary>
    public bool CanTrade(string npcId)
    {
        if (!_configMap.TryGetValue(npcId, out var cfg)) return false;
        if (!_trustMap.TryGetValue(npcId, out var data)) return false;
        return data.CurrentTrust >= cfg.TradeUnlockTrust;
    }

    /// <summary>NPC是否敌对</summary>
    public bool IsHostile(string npcId)
    {
        if (!_configMap.TryGetValue(npcId, out var cfg)) return false;
        if (!_trustMap.TryGetValue(npcId, out var data)) return false;
        return cfg.StartsHostile && data.CurrentTrust <= 0;
    }

    /// <summary>修改信任度</summary>
    public void ModifyTrust(string npcId, int amount)
    {
        if (!_configMap.TryGetValue(npcId, out var cfg)) return;
        if (!_trustMap.TryGetValue(npcId, out var data)) return;

        int oldTrust = data.CurrentTrust;
        data.CurrentTrust = Mathf.Clamp(data.CurrentTrust + amount, 0, cfg.MaxTrust);

        EventBus.Publish(new NPCTrustChangedEvent
        {
            NPCId = npcId,
            OldTrust = oldTrust,
            NewTrust = data.CurrentTrust,
            MaxTrust = cfg.MaxTrust
        });

        // 检查阈值
        if (cfg.Thresholds != null)
        {
            for (int i = 0; i < cfg.Thresholds.Length; i++)
            {
                var threshold = cfg.Thresholds[i];
                string key = $"{npcId}_{threshold.TrustLevel}";

                if (data.CurrentTrust >= threshold.TrustLevel
                    && !_triggeredThresholds.Contains(key))
                {
                    _triggeredThresholds.Add(key);

                    EventBus.Publish(new NPCTrustThresholdReachedEvent
                    {
                        NPCId = npcId,
                        ThresholdLevel = threshold.TrustLevel,
                        UnlockQuestId = threshold.UnlockQuestId,
                        UnlockDialogId = threshold.UnlockDialogId
                    });

                    Debug.Log($"[NPCRelationship] {cfg.DisplayName} 信任度阈值达成: {threshold.TrustLevel}");
                }
            }
        }
    }

    // ══════════════════════════════════════════════════════
    // ISaveable
    // ══════════════════════════════════════════════════════

    public object CaptureState()
    {
        return new NPCRelationSavePayload
        {
            TrustEntries = new List<NPCTrustData>(_trustMap.Values),
            TriggeredThresholds = new List<string>(_triggeredThresholds)
        };
    }

    public void RestoreState(object state)
    {
        NPCRelationSavePayload data;
        if (state is string json)
            data = JsonUtility.FromJson<NPCRelationSavePayload>(json);
        else if (state is NPCRelationSavePayload directData)
            data = directData;
        else
            return;

        if (data.TrustEntries != null)
        {
            for (int i = 0; i < data.TrustEntries.Count; i++)
            {
                var entry = data.TrustEntries[i];
                if (_trustMap.TryGetValue(entry.NPCId, out var existing))
                    existing.CurrentTrust = entry.CurrentTrust;
            }
        }

        _triggeredThresholds.Clear();
        if (data.TriggeredThresholds != null)
        {
            for (int i = 0; i < data.TriggeredThresholds.Count; i++)
                _triggeredThresholds.Add(data.TriggeredThresholds[i]);
        }
    }
}

/// <summary>NPC关系存档数据</summary>
[System.Serializable]
public class NPCRelationSavePayload
{
    public List<NPCTrustData> TrustEntries = new List<NPCTrustData>();
    public List<string> TriggeredThresholds = new List<string>();
}
