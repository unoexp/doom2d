// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/02_Base/UI/UIPanel.cs
// 所有UI面板的基类。提供显示/隐藏、生命周期回调。
// ══════════════════════════════════════════════════════════════════════
using UnityEngine;

/// <summary>
/// UI面板基类。
///
/// 核心职责：
///   · 提供 Show/Hide 统一接口
///   · 管理 CanvasGroup 的透明度和交互状态
///   · 面板生命周期回调（OnShow/OnHide/OnFocusGained/OnFocusLost）
///
/// 设计说明：
///   · 所有场景预置面板继承此基类，与 UIWindow（动态加载窗口）共存
///   · 面板默认隐藏（Awake 中自动设置），由 Presenter 通过 Show/Hide 直接控制
///   · 不包含业务逻辑，仅管理面板自身的显示状态
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public abstract class UIPanel : MonoBehaviour
{
    // ══════════════════════════════════════════════════════
    // 配置
    // ══════════════════════════════════════════════════════

    [Header("面板配置")]
    [Tooltip("面板唯一标识（默认使用类名）")]
    [SerializeField] private string _panelId;

    [Tooltip("是否为全屏面板（全屏面板会暂停游戏）")]
    [SerializeField] private bool _isFullScreen = false;

    [Tooltip("打开时是否暂停游戏时间")]
    [SerializeField] private bool _pauseGameOnOpen = false;

    // ══════════════════════════════════════════════════════
    // 运行时状态
    // ══════════════════════════════════════════════════════

    private CanvasGroup _canvasGroup;
    private bool _isVisible;

    // ══════════════════════════════════════════════════════
    // 属性
    // ══════════════════════════════════════════════════════

    /// <summary>面板唯一标识</summary>
    public string PanelId => string.IsNullOrEmpty(_panelId) ? GetType().Name : _panelId;

    /// <summary>是否当前可见</summary>
    public bool IsVisible => _isVisible;

    /// <summary>是否全屏面板</summary>
    public bool IsFullScreen => _isFullScreen;

    /// <summary>打开时是否暂停</summary>
    public bool PauseGameOnOpen => _pauseGameOnOpen;

    /// <summary>CanvasGroup 引用</summary>
    protected CanvasGroup CanvasGroup => _canvasGroup;

    // ══════════════════════════════════════════════════════
    // 生命周期
    // ══════════════════════════════════════════════════════

    protected virtual void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        // 自动绑定 [Bind] 标记的子节点组件
        this.BindComponents();
        // 默认隐藏
        SetVisualState(false);
    }

    // ══════════════════════════════════════════════════════
    // 公有 API
    // ══════════════════════════════════════════════════════

    /// <summary>显示面板</summary>
    public void Show()
    {
        if (_isVisible) return;
        _isVisible = true;
        SetVisualState(true);
        OnShow();
    }

    /// <summary>隐藏面板</summary>
    public void Hide()
    {
        if (!_isVisible) return;
        _isVisible = false;
        OnHide();
        SetVisualState(false);
    }

    /// <summary>面板获得焦点（成为栈顶）</summary>
    public void Focus()
    {
        SetInteractable(true);
        OnFocusGained();
    }

    /// <summary>面板失去焦点（被新面板覆盖）</summary>
    public void Unfocus()
    {
        SetInteractable(false);
        OnFocusLost();
    }

    // ══════════════════════════════════════════════════════
    // 子类回调
    // ══════════════════════════════════════════════════════

    /// <summary>面板显示时调用。子类在此初始化数据、订阅事件。</summary>
    protected virtual void OnShow() { }

    /// <summary>面板隐藏时调用。子类在此清理数据、取消订阅。</summary>
    protected virtual void OnHide() { }

    /// <summary>面板获得焦点。子类在此恢复交互。</summary>
    protected virtual void OnFocusGained() { }

    /// <summary>面板失去焦点。子类在此禁用交互。</summary>
    protected virtual void OnFocusLost() { }

    // ══════════════════════════════════════════════════════
    // 内部方法
    // ══════════════════════════════════════════════════════

    /// <summary>设置面板可见/隐藏状态</summary>
    private void SetVisualState(bool visible)
    {
        if (_canvasGroup == null) return;
        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.interactable = visible;
        _canvasGroup.blocksRaycasts = visible;
        gameObject.SetActive(visible);
    }

    /// <summary>设置交互状态（不影响可见性）</summary>
    private void SetInteractable(bool interactable)
    {
        if (_canvasGroup == null) return;
        _canvasGroup.interactable = interactable;
        _canvasGroup.blocksRaycasts = interactable;
    }
}
