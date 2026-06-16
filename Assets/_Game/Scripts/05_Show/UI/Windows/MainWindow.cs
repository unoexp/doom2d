// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/05_Show/UI/Windows/MainWindow.cs
// 主窗口。游戏的核心窗口，作为其他子界面的容器。
// ══════════════════════════════════════════════════════════════════════
using UnityEngine;

/// <summary>
/// 主窗口。游戏的核心窗口，作为其他子界面的容器。
/// 继承 UIWindow 基类，提供窗口的打开/关闭动画、焦点管理等基础能力。
/// </summary>
public class MainWindow : UIWindow
{
    // ══════════════════════════════════════════════════════
    // UI 控件引用
    // ══════════════════════════════════════════════════════

    private readonly UnityEngine.UI.Image _backgroundImage;

    // ══════════════════════════════════════════════════════
    // 构造函数
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 构造 MainWindow。
    /// WindowManager 通过 Activator.CreateInstance 调用此构造函数。
    /// </summary>
    /// <param name="root">窗口根 GameObject</param>
    /// <param name="windowId">窗口唯一标识</param>
    public MainWindow(GameObject root, string windowId) : base(root, windowId)
    {
        _backgroundImage = Root.GetComponentInChildren<UnityEngine.UI.Image>();
    }

    // ══════════════════════════════════════════════════════
    // 生命周期回调
    // ══════════════════════════════════════════════════════

    protected override void OnOpened()
    {
        base.OnOpened();
        Debug.Log($"[MainWindow] 窗口已打开: {WindowId}");
    }

    public override void OnClosed()
    {
        base.OnClosed();
        Debug.Log($"[MainWindow] 窗口已关闭: {WindowId}");
    }

    // ══════════════════════════════════════════════════════
    // 数据绑定
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 绑定数据（ViewModel 模式）。
    /// MainWindow 作为容器窗口，通常不直接绑定业务数据。
    /// </summary>
    public override void Bind(object viewModel)
    {
        // MainWindow 作为容器窗口，子类可在此处理根级数据绑定
        Debug.Log($"[MainWindow] Bind: {viewModel}");
    }
}
