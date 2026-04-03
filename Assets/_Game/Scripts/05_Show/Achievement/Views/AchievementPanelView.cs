// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/05_Show/Achievement/Views/AchievementPanelView.cs
// 成就面板 View。纯显示组件，监听 ViewModel 事件渲染UI。
// ══════════════════════════════════════════════════════════════════════
using UnityEngine;
using TMPro;

/// <summary>
/// 成就面板 View。
/// 分类显示所有成就的解锁状态。
/// </summary>
public class AchievementPanelView : UIPanel
{
    [Header("UI 引用")]
    [SerializeField] private Transform _achievementListContainer;
    [SerializeField] private GameObject _achievementEntryPrefab;
    [SerializeField] private TextMeshProUGUI _progressText;

    private AchievementViewModel _viewModel;

    public void Bind(AchievementViewModel viewModel)
    {
        if (_viewModel != null)
            _viewModel.OnDataChanged -= Refresh;

        _viewModel = viewModel;

        if (_viewModel != null)
            _viewModel.OnDataChanged += Refresh;
    }

    private void OnDestroy()
    {
        if (_viewModel != null)
            _viewModel.OnDataChanged -= Refresh;
    }

    private void Refresh()
    {
        if (_viewModel == null) return;

        // 更新进度文本
        if (_progressText != null)
            _progressText.text = $"{_viewModel.UnlockedCount} / {_viewModel.TotalCount}";

        if (_achievementListContainer == null || _achievementEntryPrefab == null) return;

        // 清除旧内容
        for (int i = _achievementListContainer.childCount - 1; i >= 0; i--)
            Destroy(_achievementListContainer.GetChild(i).gameObject);

        // 创建成就条目
        for (int i = 0; i < _viewModel.Achievements.Count; i++)
        {
            var data = _viewModel.Achievements[i];

            // 隐藏成就未解锁时不显示
            if (data.IsHidden && !data.IsUnlocked) continue;

            var entry = Instantiate(_achievementEntryPrefab, _achievementListContainer);

            var nameText = entry.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            var descText = entry.transform.Find("DescText")?.GetComponent<TextMeshProUGUI>();
            var statusIcon = entry.transform.Find("StatusIcon")?.GetComponent<UnityEngine.UI.Image>();

            if (nameText != null)
                nameText.text = data.IsUnlocked ? data.DisplayName : "???";

            if (descText != null)
                descText.text = data.IsUnlocked ? data.Description : "尚未解锁";

            if (statusIcon != null)
                statusIcon.color = data.IsUnlocked ? Color.yellow : Color.gray;
        }
    }
}
