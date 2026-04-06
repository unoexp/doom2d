// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/03_Core/Discovery/DiscoverySystem.cs
// 永久发现物管理系统。管理发现物收集状态和永久被动加成。
// ══════════════════════════════════════════════════════════════════════
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 永久发现物管理系统。
///
/// 核心职责：
///   · 管理所有发现物的收集状态
///   · 计算累计永久被动加成
///   · 通过 EventBus 广播发现事件
///
/// 设计说明：
///   · 发现物定义通过 Inspector 中 DiscoveryDefinitionSO 数组配置
///   · 收集后永久生效，不可撤销
///   · 其他系统通过 GetBonus() 查询加成
/// </summary>
public class DiscoverySystem : MonoBehaviour, ISaveable
{
    // ══════════════════════════════════════════════════════
    // 配置
    // ══════════════════════════════════════════════════════

    [Header("发现物数据")]
    [SerializeField] private DiscoveryDefinitionSO[] _discoveries;

    // ══════════════════════════════════════════════════════
    // 字段
    // ══════════════════════════════════════════════════════

    private readonly Dictionary<string, DiscoveryDefinitionSO> _definitionMap
        = new Dictionary<string, DiscoveryDefinitionSO>();

    private readonly HashSet<string> _collected = new HashSet<string>();

    /// <summary>缓存的加成值（避免每帧重新计算）</summary>
    private readonly Dictionary<DiscoveryEffectType, float> _bonusCache
        = new Dictionary<DiscoveryEffectType, float>();

    // ══════════════════════════════════════════════════════
    // ISaveable
    // ══════════════════════════════════════════════════════

    public string SaveKey => nameof(DiscoverySystem);

    // ══════════════════════════════════════════════════════
    // 生命周期
    // ══════════════════════════════════════════════════════

    private void Awake()
    {
        ServiceLocator.Register<DiscoverySystem>(this);

        if (_discoveries != null)
        {
            for (int i = 0; i < _discoveries.Length; i++)
            {
                var def = _discoveries[i];
                if (def == null || string.IsNullOrEmpty(def.DiscoveryId)) continue;
                _definitionMap[def.DiscoveryId] = def;
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

        ServiceLocator.Unregister<DiscoverySystem>();
    }

    // ══════════════════════════════════════════════════════
    // 公有 API
    // ══════════════════════════════════════════════════════

    /// <summary>收集一个发现物</summary>
    public bool Collect(string discoveryId)
    {
        if (_collected.Contains(discoveryId)) return false;
        if (!_definitionMap.TryGetValue(discoveryId, out var def)) return false;

        _collected.Add(discoveryId);
        RecalculateBonuses();

        EventBus.Publish(new DiscoveryFoundEvent
        {
            DiscoveryId = discoveryId,
            DisplayName = def.DisplayName,
            EffectType = def.EffectType,
            EffectValue = def.EffectValue
        });

        Debug.Log($"[DiscoverySystem] 发现物获取: {def.DisplayName} ({def.EffectType} +{def.EffectValue:P0})");
        return true;
    }

    /// <summary>是否已收集</summary>
    public bool IsCollected(string discoveryId) => _collected.Contains(discoveryId);

    /// <summary>获取指定类型的累计加成百分比</summary>
    public float GetBonus(DiscoveryEffectType type)
    {
        return _bonusCache.TryGetValue(type, out float val) ? val : 0f;
    }

    /// <summary>获取已收集数量</summary>
    public int CollectedCount => _collected.Count;

    /// <summary>获取总数量</summary>
    public int TotalCount => _definitionMap.Count;

    // ══════════════════════════════════════════════════════
    // 内部方法
    // ══════════════════════════════════════════════════════

    private void RecalculateBonuses()
    {
        _bonusCache.Clear();

        foreach (var id in _collected)
        {
            if (!_definitionMap.TryGetValue(id, out var def)) continue;

            if (_bonusCache.ContainsKey(def.EffectType))
                _bonusCache[def.EffectType] += def.EffectValue;
            else
                _bonusCache[def.EffectType] = def.EffectValue;
        }
    }

    // ══════════════════════════════════════════════════════
    // ISaveable
    // ══════════════════════════════════════════════════════

    public object CaptureState()
    {
        return new List<string>(_collected);
    }

    public void RestoreState(object state)
    {
        _collected.Clear();

        if (state is List<string> list)
        {
            for (int i = 0; i < list.Count; i++)
                _collected.Add(list[i]);
        }

        RecalculateBonuses();
    }
}
