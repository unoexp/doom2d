// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/05_Show/Achievement/Presenters/AchievementPresenter.cs
// 成就面板 Presenter。连接 AchievementSystem 与成就UI。
// ══════════════════════════════════════════════════════════════════════
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 成就面板 Presenter。
/// 订阅成就解锁事件更新 ViewModel。
/// </summary>
public class AchievementPresenter : MonoBehaviour
{
    [SerializeField] private AchievementPanelView _view;

    private AchievementViewModel _viewModel;
    private AchievementSystem _achievementSystem;

    private void Awake()
    {
        _viewModel = new AchievementViewModel();
    }

    private void Start()
    {
        ServiceLocator.TryGet<AchievementSystem>(out _achievementSystem);

        if (_view != null)
        {
            _view.Bind(_viewModel);

            if (ServiceLocator.TryGet<UIManager>(out var uiManager))
                uiManager.RegisterPanel(_view);
        }
    }

    private void OnEnable()
    {
        EventBus.Subscribe<AchievementUnlockedEvent>(OnAchievementUnlocked);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<AchievementUnlockedEvent>(OnAchievementUnlocked);
    }

    /// <summary>刷新所有成就数据到 ViewModel</summary>
    public void RefreshAll()
    {
        if (_achievementSystem == null) return;

        var allDefs = _achievementSystem.GetAllAchievements();
        if (allDefs == null) return;

        var list = new List<AchievementDisplayData>();

        for (int i = 0; i < allDefs.Length; i++)
        {
            var def = allDefs[i];
            if (def == null) continue;

            list.Add(new AchievementDisplayData
            {
                AchievementId = def.AchievementId,
                DisplayName = def.DisplayName,
                Description = def.Description,
                Category = def.Category,
                IsUnlocked = _achievementSystem.IsUnlocked(def.AchievementId),
                IsHidden = def.IsHidden
            });
        }

        _viewModel.SetAchievements(list, _achievementSystem.UnlockedCount, allDefs.Length);
    }

    private void OnAchievementUnlocked(AchievementUnlockedEvent evt)
    {
        _viewModel.NotifyUnlocked(evt.AchievementId);
        RefreshAll();
    }
}
