// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/04_Gameplay/Map/ExplorationTracker.cs
// 探索区域追踪器。记录玩家已访问的区域和地层。
// ══════════════════════════════════════════════════════════════════════
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 探索区域追踪器。
///
/// 核心职责：
///   · 记录玩家已探索的地表区域（分格子）
///   · 记录玩家到达过的最深地层
///   · 为地图面板提供已探索区域数据
///   · 通过 EventBus 广播区域探索事件
///
/// 设计说明：
///   · 地表按固定大小格子划分探索区域
///   · 地下按地层编号记录
///   · 实现 ISaveable 持久化
/// </summary>
public class ExplorationTracker : MonoBehaviour, ISaveable
{
    // ══════════════════════════════════════════════════════
    // 配置
    // ══════════════════════════════════════════════════════

    [Header("参数")]
    [Tooltip("探索格子大小（米）")]
    [SerializeField] private float _gridSize = 20f;

    [Tooltip("更新间隔（秒）")]
    [SerializeField] private float _updateInterval = 2f;

    // ══════════════════════════════════════════════════════
    // 字段
    // ══════════════════════════════════════════════════════

    /// <summary>已探索的格子坐标（格式："x_y"）</summary>
    private readonly HashSet<string> _exploredCells = new HashSet<string>();

    /// <summary>已到达的地层深度（0=地表，1-7=L1-L7）</summary>
    private readonly HashSet<int> _reachedLayers = new HashSet<int>();

    /// <summary>到达过的最深地层</summary>
    private int _deepestLayer;

    private Transform _playerTransform;
    private float _timer;

    // ══════════════════════════════════════════════════════
    // ISaveable
    // ══════════════════════════════════════════════════════

    public string SaveKey => nameof(ExplorationTracker);

    // ══════════════════════════════════════════════════════
    // 属性
    // ══════════════════════════════════════════════════════

    /// <summary>已探索的格子数量</summary>
    public int ExploredCellCount => _exploredCells.Count;

    /// <summary>到达过的最深地层</summary>
    public int DeepestLayer => _deepestLayer;

    // ══════════════════════════════════════════════════════
    // 生命周期
    // ══════════════════════════════════════════════════════

    private void Awake()
    {
        ServiceLocator.Register<ExplorationTracker>(this);
        _reachedLayers.Add(0); // 地表默认已探索
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

        _timer += Time.deltaTime;
        if (_timer < _updateInterval) return;
        _timer = 0f;

        RecordPosition();
    }

    private void OnDestroy()
    {
        if (ServiceLocator.TryGet<SaveLoadSystem>(out var saveSystem))
            saveSystem.Unregister(this);

        ServiceLocator.Unregister<ExplorationTracker>();
    }

    // ══════════════════════════════════════════════════════
    // 公有 API
    // ══════════════════════════════════════════════════════

    /// <summary>某个格子坐标是否已探索</summary>
    public bool IsCellExplored(int gridX, int gridY)
    {
        return _exploredCells.Contains($"{gridX}_{gridY}");
    }

    /// <summary>某个地层是否已到达</summary>
    public bool HasReachedLayer(int layer) => _reachedLayers.Contains(layer);

    /// <summary>获取所有已探索的格子坐标</summary>
    public HashSet<string> GetExploredCells() => _exploredCells;

    /// <summary>手动记录到达某个地层</summary>
    public void RecordLayerReached(int layer)
    {
        if (_reachedLayers.Add(layer))
        {
            if (layer > _deepestLayer)
                _deepestLayer = layer;

            EventBus.Publish(new AreaExploredEvent
            {
                AreaId = $"L{layer}",
                LayerDepth = layer
            });

            Debug.Log($"[ExplorationTracker] 到达新地层: L{layer}");
        }
    }

    // ══════════════════════════════════════════════════════
    // 内部方法
    // ══════════════════════════════════════════════════════

    private void RecordPosition()
    {
        Vector2 pos = _playerTransform.position;
        int gx = Mathf.FloorToInt(pos.x / _gridSize);
        int gy = Mathf.FloorToInt(pos.y / _gridSize);
        string key = $"{gx}_{gy}";

        _exploredCells.Add(key);
    }

    // ══════════════════════════════════════════════════════
    // ISaveable
    // ══════════════════════════════════════════════════════

    public object CaptureState()
    {
        return new ExplorationSavePayload
        {
            ExploredCells = new List<string>(_exploredCells),
            ReachedLayers = new List<int>(_reachedLayers),
            DeepestLayer = _deepestLayer
        };
    }

    public void RestoreState(object state)
    {
        ExplorationSavePayload data;
        if (state is string json)
            data = JsonUtility.FromJson<ExplorationSavePayload>(json);
        else if (state is ExplorationSavePayload directData)
            data = directData;
        else
            return;

        _exploredCells.Clear();
        if (data.ExploredCells != null)
        {
            for (int i = 0; i < data.ExploredCells.Count; i++)
                _exploredCells.Add(data.ExploredCells[i]);
        }

        _reachedLayers.Clear();
        if (data.ReachedLayers != null)
        {
            for (int i = 0; i < data.ReachedLayers.Count; i++)
                _reachedLayers.Add(data.ReachedLayers[i]);
        }

        _deepestLayer = data.DeepestLayer;
    }
}

/// <summary>探索存档数据</summary>
[System.Serializable]
public class ExplorationSavePayload
{
    public List<string> ExploredCells = new List<string>();
    public List<int> ReachedLayers = new List<int>();
    public int DeepestLayer;
}
