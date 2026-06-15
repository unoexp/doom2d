// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/05_Show/UI/_Base/UIWindow.cs
// 所有窗口的抽象基类。提供打开/关闭动画、焦点管理、数据绑定等基础能力。
// ─────────────────────────────────────────────────────────────────────
// 职责：
//   · 管理自身的 CanvasGroup 和 RectTransform 组件
//   · 执行打开/关闭动画（通过 UIAnimationHelper）
//   · 提供 OnOpened/OnClosed 虚方法供子类重写
//   · 声明 Bind(ViewModel) 抽象方法强制子类实现数据绑定
//
// 设计说明：
//   · 不继承 UIPanel — 窗口是独立系统，不经 UIManager 的栈式管理
//   · 窗口预制体需包含 Canvas（overrideSorting=true）和 CanvasGroup
//   · 动画类型和时长由 WindowManager 从配置表查询后传入
// ══════════════════════════════════════════════════════════════════════
using System.Collections;
using UnityEngine;

/// <summary>
/// 窗口抽象基类。
///
/// 使用方式（子类）：
///   1. 继承 UIWindow
///   2. 在 Awake 中绑定 UI 控件事件
///   3. 实现 Bind(object) 方法
///   4. 可选重写 OnOpened() / OnClosed() 做打开/关闭后的逻辑
///   5. 在 OnDestroy 中清理订阅
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public abstract class UIWindow : MonoBehaviour
{
    // ══════════════════════════════════════════════════════
    // 配置
    // ══════════════════════════════════════════════════════

    [Header("窗口配置")]
    [Tooltip("窗口唯一标识（默认使用类名）")]
    [SerializeField] private string _windowId;

    // ══════════════════════════════════════════════════════
    // 运行时状态
    // ══════════════════════════════════════════════════════

    private CanvasGroup _canvasGroup;
    private RectTransform _rectTransform;
    private Canvas _canvas;
    private bool _isOpen;

    // ══════════════════════════════════════════════════════
    // 属性
    // ══════════════════════════════════════════════════════

    /// <summary>窗口唯一标识</summary>
    public string WindowId => string.IsNullOrEmpty(_windowId) ? GetType().Name : _windowId;

    /// <summary>是否已打开</summary>
    public bool IsOpen => _isOpen;

    /// <summary>CanvasGroup 引用</summary>
    protected CanvasGroup CanvasGroup => _canvasGroup;

    /// <summary>RectTransform 引用</summary>
    protected RectTransform RectTransform => _rectTransform;

    /// <summary>Canvas 引用</summary>
    protected Canvas Canvas => _canvas;

    /// <summary>当前 sortingOrder（由 WindowManager 管理）</summary>
    public int SortingOrder
    {
        get => _canvas != null ? _canvas.sortingOrder : 0;
        set { if (_canvas != null) _canvas.sortingOrder = value; }
    }

    // ══════════════════════════════════════════════════════
    // 生命周期
    // ══════════════════════════════════════════════════════

    protected virtual void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _rectTransform = GetComponent<RectTransform>();
        _canvas = GetComponent<Canvas>();

        if (_canvas == null)
        {
            Debug.LogWarning($"[UIWindow] {WindowId} 缺少 Canvas 组件，请确保预制体包含 Canvas");
        }

        if (_canvas != null)
        {
            _canvas.overrideSorting = true;
        }

        // 初始状态：隐藏
        SetVisualClosed();
    }

    // ══════════════════════════════════════════════════════
    // 公有 API（由 WindowManager 调用）
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 设置窗口标识。WindowManager 在实例化后调用，
    /// 覆盖 _windowId 序列化字段的值。
    /// </summary>
    public void SetWindowId(string id)
    {
        if (!string.IsNullOrEmpty(id))
            _windowId = id;
    }

    /// <summary>
    /// 打开窗口（激活 + 播放入场动画）。
    /// WindowManager 调用此方法并传入从配置表查询的动画参数。
    /// </summary>
    public void Open(WindowAnimationType animType, float duration)
    {
        if (_isOpen) return;
        _isOpen = true;

        SetVisualClosed(); // 确保起始状态
        StartCoroutine(PlayOpenAnimation(animType, duration));
    }

    /// <summary>
    /// 关闭窗口（播放退场动画 → 销毁）。
    /// WindowManager 调用此方法启动关闭流程。
    /// 动画完成后会触发 OnClosed，由 WindowManager 负责 Destroy。
    /// </summary>
    public void Close(WindowAnimationType animType, float duration)
    {
        if (!_isOpen) return;
        _isOpen = false;

        StartCoroutine(PlayCloseAnimation(animType, duration));
    }

    /// <summary>
    /// 通知 WindowManager 将此窗口提到最前。
    /// 通常在用户点击窗口时由子类调用。
    /// </summary>
    public void RequestFocus()
    {
        if (ServiceLocator.TryGet<WindowManager>(out var wm))
        {
            wm.FocusWindow(this);
        }
    }

    // ══════════════════════════════════════════════════════
    // 子类回调
    // ══════════════════════════════════════════════════════

    /// <summary>窗口打开动画完成后调用。子类在此初始化数据、订阅事件。</summary>
    protected virtual void OnOpened() { }

    /// <summary>窗口关闭动画完成后调用（销毁前最后一刻）。子类在此清理数据。</summary>
    protected virtual void OnClosed() { }

    /// <summary>窗口获得焦点时调用。子类在此做视觉反馈。</summary>
    public virtual void OnFocusGained() { }

    /// <summary>窗口失去焦点时调用。</summary>
    protected virtual void OnFocusLost() { }

    /// <summary>
    /// 绑定数据（ViewModel 模式）。
    /// 子类必须实现此方法，遵循 05_Show 层的 Bind(ViewModel) 约定。
    /// </summary>
    public abstract void Bind(object viewModel);

    // ══════════════════════════════════════════════════════
    // 动画协程
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 播放打开动画。子类可重写以实现自定义动画。
    /// 默认委托给 UIAnimationHelper.PlayOpenAnimation。
    /// </summary>
    protected virtual IEnumerator PlayOpenAnimation(WindowAnimationType type, float duration)
    {
        yield return UIAnimationHelper.PlayOpenAnimation(_canvasGroup, _rectTransform, type, duration);
        OnOpened();
    }

    /// <summary>
    /// 播放关闭动画。子类可重写以实现自定义动画。
    /// 默认委托给 UIAnimationHelper.PlayCloseAnimation。
    /// </summary>
    protected virtual IEnumerator PlayCloseAnimation(WindowAnimationType type, float duration)
    {
        yield return UIAnimationHelper.PlayCloseAnimation(_canvasGroup, _rectTransform, type, duration);
        OnClosed();
    }

    // ══════════════════════════════════════════════════════
    // 内部方法
    // ══════════════════════════════════════════════════════

    /// <summary>设置初始关闭状态（不触发动画）</summary>
    private void SetVisualClosed()
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }
        gameObject.SetActive(false);
    }
}
