// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/05_Show/Skill/Views/SkillPanelView.cs
// 技能面板 View。纯显示组件，监听 ViewModel 事件渲染UI。
// ══════════════════════════════════════════════════════════════════════
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 技能面板 View。
/// 显示所有技能的等级、经验条和效果描述。
/// </summary>
public class SkillPanelView : UIPanel
{
    [Header("UI 引用")]
    [SerializeField] private Transform _skillListContainer;
    [SerializeField] private GameObject _skillEntryPrefab;

    private SkillViewModel _viewModel;

    public void Bind(SkillViewModel viewModel)
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
        if (_viewModel == null || _skillListContainer == null || _skillEntryPrefab == null) return;

        // 清除旧内容
        for (int i = _skillListContainer.childCount - 1; i >= 0; i--)
            Destroy(_skillListContainer.GetChild(i).gameObject);

        // 创建技能条目
        for (int i = 0; i < _viewModel.Skills.Count; i++)
        {
            var data = _viewModel.Skills[i];
            var entry = Instantiate(_skillEntryPrefab, _skillListContainer);

            // 查找子组件并设置数据
            var nameText = entry.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            var levelText = entry.transform.Find("LevelText")?.GetComponent<TextMeshProUGUI>();
            var expBar = entry.transform.Find("ExpBar")?.GetComponent<Slider>();
            var effectText = entry.transform.Find("EffectText")?.GetComponent<TextMeshProUGUI>();

            if (nameText != null) nameText.text = data.Name;
            if (levelText != null) levelText.text = data.IsMaxLevel ? "MAX" : $"Lv.{data.Level}";
            if (expBar != null)
            {
                expBar.maxValue = data.ExpToNextLevel > 0 ? data.ExpToNextLevel : 1;
                expBar.value = data.CurrentExp;
            }
            if (effectText != null) effectText.text = data.PrimaryEffectText;
        }
    }
}
