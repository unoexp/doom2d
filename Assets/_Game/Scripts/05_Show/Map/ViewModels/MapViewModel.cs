// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/05_Show/Map/ViewModels/MapViewModel.cs
// 地图面板 ViewModel。纯C#类，持有UI状态。
// ══════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 地图 POI 显示数据
/// </summary>
public class MapPOIViewModel
{
    public string POIId;
    public POIType Type;
    public string DisplayName;
    public Vector2 Position;
    public bool IsDiscovered;
}

/// <summary>
/// 地图面板 ViewModel。
/// View 通过订阅事件响应数据变化。
/// </summary>
public class MapViewModel
{
    /// <summary>当前玩家位置</summary>
    public Vector2 PlayerPosition { get; private set; }

    /// <summary>当前深度</summary>
    public int CurrentDepth { get; private set; }

    /// <summary>已发现的 POI 列表</summary>
    public List<MapPOIViewModel> POIList { get; } = new List<MapPOIViewModel>();

    /// <summary>庇护所位置</summary>
    public Vector2 ShelterPosition { get; private set; }

    // ── 事件（View 订阅） ──

    public event Action OnDataChanged;
    public event Action<MapPOIViewModel> OnPOIAdded;

    // ── 更新方法（Presenter 调用） ──

    public void UpdatePlayerPosition(Vector2 pos)
    {
        PlayerPosition = pos;
        OnDataChanged?.Invoke();
    }

    public void UpdateDepth(int depth)
    {
        CurrentDepth = depth;
        OnDataChanged?.Invoke();
    }

    public void SetShelterPosition(Vector2 pos)
    {
        ShelterPosition = pos;
    }

    public void SetPOIList(List<MapPOIViewModel> pois)
    {
        POIList.Clear();
        POIList.AddRange(pois);
        OnDataChanged?.Invoke();
    }

    public void AddPOI(MapPOIViewModel poi)
    {
        POIList.Add(poi);
        OnPOIAdded?.Invoke(poi);
    }
}
