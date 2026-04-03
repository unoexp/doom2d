// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/05_Show/Map/Views/MapPanelView.cs
// 地图面板 View。纯显示组件，监听 ViewModel 事件渲染UI。
// ══════════════════════════════════════════════════════════════════════
using UnityEngine;
using TMPro;

/// <summary>
/// 地图面板 View。
/// 仅负责渲染，不包含业务逻辑。
/// </summary>
public class MapPanelView : UIPanel
{
    [Header("UI 引用")]
    [SerializeField] private RectTransform _mapContainer;
    [SerializeField] private RectTransform _playerMarker;
    [SerializeField] private RectTransform _shelterMarker;
    [SerializeField] private TextMeshProUGUI _depthText;
    [SerializeField] private TextMeshProUGUI _coordinateText;
    [SerializeField] private GameObject _poiMarkerPrefab;

    private MapViewModel _viewModel;

    public void Bind(MapViewModel viewModel)
    {
        // 解绑旧的
        if (_viewModel != null)
        {
            _viewModel.OnDataChanged -= Refresh;
            _viewModel.OnPOIAdded -= OnPOIAdded;
        }

        _viewModel = viewModel;

        if (_viewModel != null)
        {
            _viewModel.OnDataChanged += Refresh;
            _viewModel.OnPOIAdded += OnPOIAdded;
        }
    }

    private void OnDestroy()
    {
        if (_viewModel != null)
        {
            _viewModel.OnDataChanged -= Refresh;
            _viewModel.OnPOIAdded -= OnPOIAdded;
        }
    }

    private void Refresh()
    {
        if (_viewModel == null) return;

        // 更新深度文本
        if (_depthText != null)
        {
            _depthText.text = _viewModel.CurrentDepth == 0
                ? "地表"
                : $"地下 L{_viewModel.CurrentDepth}";
        }

        // 更新坐标文本
        if (_coordinateText != null)
        {
            var pos = _viewModel.PlayerPosition;
            _coordinateText.text = $"({pos.x:F0}, {pos.y:F0})";
        }

        // 更新玩家标记位置（相对地图容器）
        if (_playerMarker != null)
        {
            _playerMarker.anchoredPosition = WorldToMapPosition(_viewModel.PlayerPosition);
        }

        // 更新庇护所标记
        if (_shelterMarker != null)
        {
            _shelterMarker.anchoredPosition = WorldToMapPosition(_viewModel.ShelterPosition);
        }
    }

    private void OnPOIAdded(MapPOIViewModel poi)
    {
        if (_poiMarkerPrefab == null || _mapContainer == null) return;

        var marker = Instantiate(_poiMarkerPrefab, _mapContainer);
        var rt = marker.GetComponent<RectTransform>();
        if (rt != null)
            rt.anchoredPosition = WorldToMapPosition(poi.Position);

        var text = marker.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
            text.text = poi.DisplayName;
    }

    /// <summary>世界坐标转地图UI坐标（简易实现，需根据实际地图缩放调整）</summary>
    private Vector2 WorldToMapPosition(Vector2 worldPos)
    {
        // 简易映射：世界1m = 地图0.1像素（可配置）
        return worldPos * 0.1f;
    }
}
