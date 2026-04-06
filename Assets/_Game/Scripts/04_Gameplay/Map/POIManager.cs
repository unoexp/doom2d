// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/04_Gameplay/Map/POIManager.cs
// 地表兴趣点管理器。管理POI的发现、标记和奖励。
// ══════════════════════════════════════════════════════════════════════
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// POI 运行时数据
/// </summary>
[System.Serializable]
public class POIRuntimeData
{
    public string POIId;
    public POIType Type;
    public string DisplayName;
    public Vector2 Position;
    public bool Discovered;
    public bool Looted;
}

/// <summary>
/// 兴趣点管理器。
///
/// 核心职责：
///   · 管理地表所有POI的位置和状态
///   · 检测玩家进入POI范围时触发发现
///   · 管理POI首次访问奖励
///   · 通过 EventBus 广播 POI 发现事件
///
/// 设计说明：
///   · POI 数据在 Inspector 中配置
///   · 玩家进入触发范围自动发现
///   · 已发现的POI在地图面板上显示
/// </summary>
public class POIManager : MonoBehaviour, ISaveable
{
    // ══════════════════════════════════════════════════════
    // 配置
    // ══════════════════════════════════════════════════════

    [Header("POI 列表")]
    [SerializeField] private POIConfig[] _poiConfigs;

    [Header("参数")]
    [SerializeField] private float _discoveryRange = 15f;

    // ══════════════════════════════════════════════════════
    // 字段
    // ══════════════════════════════════════════════════════

    private readonly Dictionary<string, POIRuntimeData> _poiMap
        = new Dictionary<string, POIRuntimeData>();

    private Transform _playerTransform;
    private float _checkTimer;
    private const float CHECK_INTERVAL = 1f; // 每秒检查一次

    // ══════════════════════════════════════════════════════
    // ISaveable
    // ══════════════════════════════════════════════════════

    public string SaveKey => nameof(POIManager);

    // ══════════════════════════════════════════════════════
    // 生命周期
    // ══════════════════════════════════════════════════════

    private void Awake()
    {
        ServiceLocator.Register<POIManager>(this);

        if (_poiConfigs != null)
        {
            for (int i = 0; i < _poiConfigs.Length; i++)
            {
                var cfg = _poiConfigs[i];
                if (string.IsNullOrEmpty(cfg.POIId)) continue;

                _poiMap[cfg.POIId] = new POIRuntimeData
                {
                    POIId = cfg.POIId,
                    Type = cfg.Type,
                    DisplayName = cfg.DisplayName,
                    Position = cfg.Position,
                    Discovered = false,
                    Looted = false
                };
            }
        }
    }

    private void Start()
    {
        var player = GameObject.FindWithTag(GameConst.TAG_PLAYER);
        _playerTransform = player != null ? player.transform : null;

        if (ServiceLocator.TryGet<SaveLoadSystem>(out var saveSystem))
            saveSystem.Register(this);
    }

    private void Update()
    {
        if (_playerTransform == null) return;

        _checkTimer += Time.deltaTime;
        if (_checkTimer < CHECK_INTERVAL) return;
        _checkTimer = 0f;

        CheckDiscovery();
    }

    private void OnDestroy()
    {
        if (ServiceLocator.TryGet<SaveLoadSystem>(out var saveSystem))
            saveSystem.Unregister(this);

        ServiceLocator.Unregister<POIManager>();
    }

    // ══════════════════════════════════════════════════════
    // 公有 API
    // ══════════════════════════════════════════════════════

    /// <summary>获取所有已发现的POI</summary>
    public List<POIRuntimeData> GetDiscoveredPOIs()
    {
        var result = new List<POIRuntimeData>();
        foreach (var kvp in _poiMap)
        {
            if (kvp.Value.Discovered)
                result.Add(kvp.Value);
        }
        return result;
    }

    /// <summary>获取已发现数量</summary>
    public int DiscoveredCount
    {
        get
        {
            int count = 0;
            foreach (var kvp in _poiMap)
                if (kvp.Value.Discovered) count++;
            return count;
        }
    }

    /// <summary>获取POI总数</summary>
    public int TotalCount => _poiMap.Count;

    /// <summary>手动标记POI为已发现</summary>
    public void DiscoverPOI(string poiId)
    {
        if (!_poiMap.TryGetValue(poiId, out var data)) return;
        if (data.Discovered) return;

        data.Discovered = true;

        EventBus.Publish(new POIDiscoveredEvent
        {
            POIId = poiId,
            POIType = data.Type,
            DisplayName = data.DisplayName,
            Position = data.Position
        });

        Debug.Log($"[POIManager] 发现兴趣点: {data.DisplayName}");
    }

    // ══════════════════════════════════════════════════════
    // 内部方法
    // ══════════════════════════════════════════════════════

    private void CheckDiscovery()
    {
        Vector2 playerPos = _playerTransform.position;

        foreach (var kvp in _poiMap)
        {
            if (kvp.Value.Discovered) continue;

            float dist = Vector2.Distance(playerPos, kvp.Value.Position);
            if (dist <= _discoveryRange)
            {
                DiscoverPOI(kvp.Key);
            }
        }
    }

    // ══════════════════════════════════════════════════════
    // ISaveable
    // ══════════════════════════════════════════════════════

    public object CaptureState()
    {
        var list = new List<POIRuntimeData>(_poiMap.Values);
        return list;
    }

    public void RestoreState(object state)
    {
        if (state is List<POIRuntimeData> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var saved = list[i];
                if (_poiMap.TryGetValue(saved.POIId, out var existing))
                {
                    existing.Discovered = saved.Discovered;
                    existing.Looted = saved.Looted;
                }
            }
        }
    }
}

/// <summary>
/// POI 配置条目（Inspector 中配置）
/// </summary>
[System.Serializable]
public struct POIConfig
{
    public string POIId;
    public POIType Type;
    public string DisplayName;
    public Vector2 Position;
}
