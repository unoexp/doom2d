// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/05_Show/GameOver/GameOverPanelView.cs
// 死亡结算面板View。负责渲染结算界面。
// ══════════════════════════════════════════════════════════════════════
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 死亡结算面板 View。
///
/// 核心职责：
///   · 显示死亡原因、存活时间
///   · 提供"读取存档"和"返回主菜单"按钮
///   · 绑定 ViewModel 事件驱动显示更新
///
/// 设计说明：
///   · 继承 UIPanel，由 UIManager 栈式管理
///   · 全屏面板，打开时暂停游戏
///   · 按钮事件通过 EventBus 发布，不直接调用业务逻辑
/// </summary>
public class GameOverPanelView : UIPanel
{
    // ══════════════════════════════════════════════════════
    // UI 引用
    // ══════════════════════════════════════════════════════

    [Header("文本")]
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _deathCauseText;
    [SerializeField] private TextMeshProUGUI _survivalTimeText;

    [Header("按钮")]
    [SerializeField] private Button _loadSaveButton;
    [SerializeField] private Button _mainMenuButton;

    // ══════════════════════════════════════════════════════
    // ViewModel
    // ══════════════════════════════════════════════════════

    private GameOverViewModel _viewModel;

    /// <summary>按钮事件回调（由 Presenter 设置）</summary>
    public event System.Action OnLoadSaveClicked;
    public event System.Action OnMainMenuClicked;

    // ══════════════════════════════════════════════════════
    // 公有 API
    // ══════════════════════════════════════════════════════

    /// <summary>绑定 ViewModel</summary>
    public void Bind(GameOverViewModel viewModel)
    {
        if (_viewModel != null)
        {
            _viewModel.OnDataUpdated -= RefreshUI;
        }

        _viewModel = viewModel;

        if (_viewModel != null)
        {
            _viewModel.OnDataUpdated += RefreshUI;
        }
    }

    // ══════════════════════════════════════════════════════
    // 生命周期
    // ══════════════════════════════════════════════════════

    protected override void Awake()
    {
        base.Awake();

        if (_loadSaveButton != null)
            _loadSaveButton.onClick.AddListener(() => OnLoadSaveClicked?.Invoke());

        if (_mainMenuButton != null)
            _mainMenuButton.onClick.AddListener(() => OnMainMenuClicked?.Invoke());
    }

    private void OnDestroy()
    {
        Bind(null);

        if (_loadSaveButton != null)
            _loadSaveButton.onClick.RemoveAllListeners();

        if (_mainMenuButton != null)
            _mainMenuButton.onClick.RemoveAllListeners();
    }

    // ══════════════════════════════════════════════════════
    // 内部方法
    // ══════════════════════════════════════════════════════

    private void RefreshUI()
    {
        if (_viewModel == null) return;

        if (_titleText != null)
            _titleText.text = "你已死亡";

        if (_deathCauseText != null)
            _deathCauseText.text = _viewModel.DeathCauseText;

        if (_survivalTimeText != null)
            _survivalTimeText.text = $"存活时间: {_viewModel.SurvivalTimeText}";
    }
}
