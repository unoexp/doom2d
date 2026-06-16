// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/05_Show/UI/_Base/UIWindow.cs
// 所有窗口的抽象基类。提供打开/关闭动画、焦点管理、数据绑定等基础能力。
// ─────────────────────────────────────────────────────────────────────
// 职责：
//   · 持有并管理根 GameObject 及其 CanvasGroup / RectTransform / Canvas 组件
//   · 执行打开/关闭动画（通过 UIAnimationHelper）
//   · 提供 OnOpened/OnClosed 虚方法供子类重写
//   · 声明 Bind(ViewModel) 抽象方法强制子类实现数据绑定
//
// 设计说明：
//   · 纯 C# 类，不继承 MonoBehaviour — 窗口是逻辑控制器，操作 GameObject 身上的组件
//   · 由 WindowManager 通过 Activator.CreateInstance 构造，传入根 GameObject 和 windowId
//   · 子类通过受保护的属性（CanvasGroup / RectTransform / Canvas / Root）访问 UI 控件
//   · 动画类型和时长由 WindowManager 从配置表查询后传入 Open() / Close()
// ══════════════════════════════════════════════════════════════════════
using System;
using UnityEngine;

/// <summary>
/// 窗口抽象基类（纯 C#）。
///
/// 使用方式（子类）：
///   1. 继承 UIWindow，实现构造函数调用 base(root, windowId)
///   2. 在构造函数中绑定 UI 控件事件（通过 Root.transform.Find 等获取子节点引用）
///   3. 实现 Bind(object) 方法
///   4. 可选重写 OnOpened() / OnClosed() 做打开/关闭后的逻辑
///   5. 实现 Shutdown() 清理 EventBus 订阅
/// </summary>
public abstract class UIWindow
{
    // ══════════════════════════════════════════════════════
    // 运行时状态
    // ══════════════════════════════════════════════════════

    private readonly CanvasGroup _canvasGroup;
    private readonly RectTransform _rectTransform;
    private readonly Canvas _canvas;
    private bool _isOpen;

    // ══════════════════════════════════════════════════════
    // 构造函数
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 构造窗口实例。
    /// WindowManager 实例化预制体后调用此构造函数传入根 GameObject 和窗口标识。
    /// </summary>
    /// <param name="root">窗口根 GameObject（预制体实例，需包含 Canvas 和 CanvasGroup）</param>
    /// <param name="windowId">窗口唯一标识</param>
    protected UIWindow(GameObject root, string windowId)
    {
        Root = root ?? throw new ArgumentNullException(nameof(root));
        WindowId = string.IsNullOrEmpty(windowId) ? GetType().Name : windowId;

        _canvasGroup = root.GetComponent<CanvasGroup>();
        _rectTransform = root.GetComponent<RectTransform>();
        _canvas = root.GetComponent<Canvas>();

        if (_canvas == null)
        {
            Debug.LogWarning($"[UIWindow] {WindowId} 缺少 Canvas 组件，请确保预制体包含 Canvas");
        }

        if (_canvas != null)
        {
            _canvas.overrideSorting = true;
        }

        // 初始状态：隐藏（通过 CanvasGroup 控制，不关闭 GameObject 以支持 DOTween）
        SetVisualClosed();
    }

    // ══════════════════════════════════════════════════════
    // 属性
    // ══════════════════════════════════════════════════════

    /// <summary>窗口根 GameObject</summary>
    public GameObject Root { get; }

    /// <summary>窗口唯一标识</summary>
    public string WindowId { get; }

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
    // 公有 API（由 WindowManager 调用）
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 打开窗口（激活 + 播放入场动画）。
    /// WindowManager 调用此方法并传入从配置表查询的动画参数。
    /// </summary>
    /// <param name="animType">动画类型</param>
    /// <param name="duration">动画时长（秒）</param>
    /// <param name="onComplete">动画完成回调（含 OnOpened 之后）</param>
    public void Open(WindowAnimationType animType, float duration, Action onComplete = null)
    {
        if (_isOpen) return;
        _isOpen = true;

        SetVisualClosed(); // 确保起始状态
        PlayOpenAnimation(animType, duration, onComplete);
    }

    /// <summary>
    /// 关闭窗口（播放退场动画 → 由 WindowManager 负责 Destroy）。
    /// </summary>
    /// <param name="animType">动画类型</param>
    /// <param name="duration">动画时长（秒）</param>
    /// <param name="onComplete">动画完成回调（含 OnClosed 之后）</param>
    public void Close(WindowAnimationType animType, float duration, Action onComplete = null)
    {
        if (!_isOpen) return;
        _isOpen = false;

        PlayCloseAnimation(animType, duration, onComplete);
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
    public virtual void OnClosed() { }

    /// <summary>窗口获得焦点时调用。子类在此做视觉反馈。</summary>
    public virtual void OnFocusGained() { }

    /// <summary>窗口失去焦点时调用。</summary>
    protected virtual void OnFocusLost() { }

    /// <summary>
    /// 绑定数据（ViewModel 模式）。
    /// 子类必须实现此方法，遵循 05_Show 层的 Bind(ViewModel) 约定。
    /// </summary>
    public abstract void Bind(object viewModel);

    /// <summary>
    /// 清理窗口资源。子类在此取消 EventBus 订阅等。
    /// WindowManager 在销毁窗口 GameObject 前调用。
    /// </summary>
    public virtual void Shutdown() { }

    // ══════════════════════════════════════════════════════
    // 动画方法（DOTween 驱动，通过 onComplete 回调链式通知）
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 播放打开动画。子类可重写以实现自定义动画。
    /// 默认委托给 UIAnimationHelper.PlayOpenAnimation。
    /// 动画完成后依次调用 OnOpened() → onComplete。
    /// </summary>
    protected virtual void PlayOpenAnimation(WindowAnimationType type, float duration,
        Action onComplete = null)
    {
        UIAnimationHelper.PlayOpenAnimation(_canvasGroup, _rectTransform, type, duration, () =>
        {
            OnOpened();
            onComplete?.Invoke();
        });
    }

    /// <summary>
    /// 播放关闭动画。子类可重写以实现自定义动画。
    /// 默认委托给 UIAnimationHelper.PlayCloseAnimation。
    /// 动画完成后依次调用 OnClosed() → onComplete。
    /// </summary>
    protected virtual void PlayCloseAnimation(WindowAnimationType type, float duration,
        Action onComplete = null)
    {
        UIAnimationHelper.PlayCloseAnimation(_canvasGroup, _rectTransform, type, duration, () =>
        {
            OnClosed();
            onComplete?.Invoke();
        });
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
        // 注意：不调用 Root.SetActive(false)。
        // 窗口需要保持 active 以支持 DOTween 动画播放。
        // 视觉效果通过 CanvasGroup 的 alpha + interactable + blocksRaycasts 完全控制。
    }
}
