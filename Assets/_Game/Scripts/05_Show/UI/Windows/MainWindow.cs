// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/05_Show/UI/Windows/MainWindow.cs
// 主窗口。游戏的核心窗口，作为其他子界面的容器。
// ══════════════════════════════════════════════════════════════════════
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 主窗口。游戏的核心窗口，作为其他子界面的容器。
/// 继承 UIWindow 基类，提供窗口的打开/关闭动画、焦点管理等基础能力。
/// </summary>
public class MainWindow : UIWindow
{
    // ══════════════════════════════════════════════════════
    // UI 控件引用（自动绑定）
    // ══════════════════════════════════════════════════════

    [Bind("btns/btn_task")]      private Button _btnTask;
    [Bind("btns/btn_inventory")] private Button _btnInventory;
    [Bind("btns/btn_char")]      private Button _btnChar;

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
    }

    // ══════════════════════════════════════════════════════
    // UI 组件初始化
    // ══════════════════════════════════════════════════════

    public override void InitUIComponents()
    {
        // 自动绑定 [Bind] 标记的字段
        Root.transform.BindComponentsOn(this);

        // 绑定按钮点击回调
        if (_btnTask != null)
            _btnTask.onClick.AddListener(OnTaskClicked);

        if (_btnInventory != null)
            _btnInventory.onClick.AddListener(OnInventoryClicked);

        if (_btnChar != null)
            _btnChar.onClick.AddListener(OnCharClicked);
    }

    // ══════════════════════════════════════════════════════
    // 按钮回调
    // ══════════════════════════════════════════════════════

    private void OnTaskClicked()
    {
        Debug.Log("[MainWindow] 任务按钮被点击");
    }

    private void OnInventoryClicked()
    {
        Debug.Log("[MainWindow] 背包按钮被点击");
    }

    private void OnCharClicked()
    {
        Debug.Log("[MainWindow] 角色按钮被点击");
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
