// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/05_Show/Map/Presenters/MapPresenter.cs
// 地图面板 Presenter。连接 POIManager/ExplorationTracker 与地图UI。
// ══════════════════════════════════════════════════════════════════════
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 地图面板 Presenter。
///
/// 核心职责：
///   · 订阅 POI 发现事件更新 ViewModel
///   · 定期更新玩家位置
///   · 从 POIManager 拉取已发现POI数据
/// </summary>
public class MapPresenter : MonoBehaviour
{
    [SerializeField] private MapPanelView _view;

    private MapViewModel _viewModel;
    private POIManager _poiManager;
    private ExplorationTracker _explorationTracker;
    private Transform _playerTransform;
    private float _updateTimer;

    private void Awake()
    {
        _viewModel = new MapViewModel();
    }

    private void Start()
    {
        ServiceLocator.TryGet<POIManager>(out _poiManager);
        ServiceLocator.TryGet<ExplorationTracker>(out _explorationTracker);

        var player = GameObject.FindWithTag(GameConst.TAG_PLAYER);
        _playerTransform = player != null ? player.transform : null;

        if (_view != null)
        {
            _view.Bind(_viewModel);

            if (ServiceLocator.TryGet<UIManager>(out var uiManager))
                uiManager.RegisterPanel(_view);
        }

        // 初始化已发现的POI
        RefreshPOIList();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<POIDiscoveredEvent>(OnPOIDiscovered);
        EventBus.Subscribe<AreaExploredEvent>(OnAreaExplored);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<POIDiscoveredEvent>(OnPOIDiscovered);
        EventBus.Unsubscribe<AreaExploredEvent>(OnAreaExplored);
    }

    private void Update()
    {
        _updateTimer += Time.deltaTime;
        if (_updateTimer < 0.5f) return;
        _updateTimer = 0f;

        if (_playerTransform != null)
            _viewModel.UpdatePlayerPosition(_playerTransform.position);

        if (_explorationTracker != null)
            _viewModel.UpdateDepth(_explorationTracker.DeepestLayer);
    }

    private void OnPOIDiscovered(POIDiscoveredEvent evt)
    {
        _viewModel.AddPOI(new MapPOIViewModel
        {
            POIId = evt.POIId,
            Type = evt.POIType,
            DisplayName = evt.DisplayName,
            Position = evt.Position,
            IsDiscovered = true
        });
    }

    private void OnAreaExplored(AreaExploredEvent evt)
    {
        _viewModel.UpdateDepth(evt.LayerDepth);
    }

    private void RefreshPOIList()
    {
        if (_poiManager == null) return;

        var discovered = _poiManager.GetDiscoveredPOIs();
        var vmList = new List<MapPOIViewModel>();

        for (int i = 0; i < discovered.Count; i++)
        {
            var poi = discovered[i];
            vmList.Add(new MapPOIViewModel
            {
                POIId = poi.POIId,
                Type = poi.Type,
                DisplayName = poi.DisplayName,
                Position = poi.Position,
                IsDiscovered = true
            });
        }

        _viewModel.SetPOIList(vmList);
    }
}
