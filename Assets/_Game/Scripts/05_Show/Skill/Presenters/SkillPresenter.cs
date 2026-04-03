// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/05_Show/Skill/Presenters/SkillPresenter.cs
// 技能面板 Presenter。连接 SkillSystem 与技能UI。
// ══════════════════════════════════════════════════════════════════════
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 技能面板 Presenter。
/// 订阅技能事件更新 ViewModel，处理面板打开时的数据刷新。
/// </summary>
public class SkillPresenter : MonoBehaviour
{
    [SerializeField] private SkillPanelView _view;

    private SkillViewModel _viewModel;
    private SkillSystem _skillSystem;

    private void Awake()
    {
        _viewModel = new SkillViewModel();
    }

    private void Start()
    {
        ServiceLocator.TryGet<SkillSystem>(out _skillSystem);

        if (_view != null)
        {
            _view.Bind(_viewModel);

            if (ServiceLocator.TryGet<UIManager>(out var uiManager))
                uiManager.RegisterPanel(_view);
        }
    }

    private void OnEnable()
    {
        EventBus.Subscribe<SkillExpGainedEvent>(OnExpGained);
        EventBus.Subscribe<SkillLevelUpEvent>(OnLevelUp);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<SkillExpGainedEvent>(OnExpGained);
        EventBus.Unsubscribe<SkillLevelUpEvent>(OnLevelUp);
    }

    /// <summary>刷新所有技能数据到 ViewModel</summary>
    public void RefreshAll()
    {
        if (_skillSystem == null) return;

        var allDefs = _skillSystem.GetAllRecipes(); // 无此方法，需从配置获取
        var list = new List<SkillDisplayData>();

        // 遍历所有已知技能类型
        var types = new[] { SkillType.Mining, SkillType.Combat, SkillType.Crafting,
                            SkillType.Cooking, SkillType.Gathering, SkillType.Survival };

        for (int i = 0; i < types.Length; i++)
        {
            var type = types[i];
            int level = _skillSystem.GetLevel(type);
            int exp = _skillSystem.GetCurrentExp(type);
            int expNeeded = _skillSystem.GetExpToNextLevel(type);

            list.Add(new SkillDisplayData
            {
                Type = type,
                Name = type.ToString(),
                Level = level,
                CurrentExp = exp,
                ExpToNextLevel = expNeeded,
                PrimaryBonus = _skillSystem.GetPrimaryBonus(type),
                PrimaryEffectText = $"+{_skillSystem.GetPrimaryBonus(type):P0}",
                IsMaxLevel = expNeeded == 0
            });
        }

        _viewModel.SetSkills(list);
    }

    private void OnExpGained(SkillExpGainedEvent evt)
    {
        // 面板打开时才刷新
        if (_view != null && _view.gameObject.activeInHierarchy)
            RefreshAll();
    }

    private void OnLevelUp(SkillLevelUpEvent evt)
    {
        _viewModel.NotifyLevelUp(evt.SkillType);
        if (_view != null && _view.gameObject.activeInHierarchy)
            RefreshAll();
    }
}
