// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/05_Show/UI/Windows/LoadingWindow.cs
// 加载窗口。游戏启动时显示，实时展示加载进度条，完成后自动关闭并回调。
// ─────────────────────────────────────────────────────────────────────
// 使用方式：
//   AppMain 在数据加载前通过 WindowManager 打开此窗口，
//   设置 OnLoadingComplete 回调（打开 main_window）。
//   加载过程中发布 LoadingProgressEvent 驱动进度条，
//   加载完成后发布 LoadingCompletedEvent 触发关闭 + 回调。
// ══════════════════════════════════════════════════════════════════════
using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 加载窗口。游戏启动时显示，实时展示加载进度，完成后自动关闭并回调。
/// 继承 UIWindow 基类，由 WindowManager 通过 Activator.CreateInstance 构造。
/// </summary>
public class LoadingWindow : UIWindow
{
    // ══════════════════════════════════════════════════════
    // UI 控件引用
    // ══════════════════════════════════════════════════════

    private readonly Image _progressFill;
    private readonly Text _stepText;
    private readonly Button _btnStart;

    // ══════════════════════════════════════════════════════
    // 回调
    // ══════════════════════════════════════════════════════

    /// <summary>加载完成后执行的回调（如打开 main_window）</summary>
    public Action OnLoadingComplete { get; set; }

    // ══════════════════════════════════════════════════════
    // 构造函数
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 构造 LoadingWindow。
    /// WindowManager 通过 Activator.CreateInstance 调用此构造函数。
    /// </summary>
    /// <param name="root">窗口根 GameObject（预制体实例，需包含 Canvas）</param>
    /// <param name="windowId">窗口唯一标识</param>
    public LoadingWindow(GameObject root, string windowId) : base(root, windowId)
    {
        // 查找进度条填充图片（预制体路径：all/progress_node/progress）
        var progressTransform = root.transform.Find("all/progress_node/progress");
        if (progressTransform != null)
        {
            _progressFill = progressTransform.GetComponent<Image>();
            if (_progressFill != null && _progressFill.type == Image.Type.Filled)
            {
                _progressFill.fillAmount = 0f;
            }
        }

        // 查找步骤描述文本（预留路径：all/step_text，预制体中可能还没有）
        var stepTextTransform = root.transform.Find("all/step_text");
        if (stepTextTransform != null)
        {
            _stepText = stepTextTransform.GetComponent<Text>();
        }

        var btnStartTransform = root.transform.Find("all/btn_start");
        if (btnStartTransform != null)
        {
            _btnStart = btnStartTransform.GetComponent<Button>();
            _btnStart.onClick.AddListener(() =>
            {
                OnLoadingComplete?.Invoke();
            });
        }
    }

    // ══════════════════════════════════════════════════════
    // 生命周期回调
    // ══════════════════════════════════════════════════════

    protected override void OnOpened()
    {
        base.OnOpened();
        EventBus.Subscribe<LoadingProgressEvent>(OnLoadingProgress);
        EventBus.Subscribe<LoadingCompletedEvent>(OnLoadingCompleted);
        Debug.Log("[LoadingWindow] 已打开，等待加载...");
    }

    public override void OnClosed()
    {
        EventBus.Unsubscribe<LoadingProgressEvent>(OnLoadingProgress);
        EventBus.Unsubscribe<LoadingCompletedEvent>(OnLoadingCompleted);
        base.OnClosed();
        Debug.Log("[LoadingWindow] 已关闭");
    }

    // ══════════════════════════════════════════════════════
    // 事件处理
    // ══════════════════════════════════════════════════════

    /// <summary>处理加载进度更新事件</summary>
    private void OnLoadingProgress(LoadingProgressEvent evt)
    {
        if (_progressFill != null)
        {
            _progressFill.fillAmount = evt.Progress;
        }

        if (_stepText != null)
        {
            _stepText.text = evt.StepDescription;
        }
    }

    /// <summary>处理加载完成事件</summary>
    private void OnLoadingCompleted(LoadingCompletedEvent evt)
    {
        // 确保进度条满
        if (_progressFill != null)
        {
            _progressFill.fillAmount = 1f;
        }

        // 执行回调（打开 main_window）
        OnLoadingComplete?.Invoke();

        // 关闭自身
        // if (ServiceLocator.TryGet<WindowManager>(out var wm))
        // {
        //     wm.CloseWindow(WindowId);
        // }
    }

    private void OnClickStart()
    {
        ServiceLocator.Get<WindowManager>().OpenWindow("main_window");
        ServiceLocator.Get<SceneLoadSystem>().LoadSceneAsync(GameConst.SCENE_MAIN);
        ServiceLocator.Get<WindowManager>().CloseWindow(WindowId);
    }


    // ══════════════════════════════════════════════════════
    // 数据绑定
    // ══════════════════════════════════════════════════════

    /// <summary>LoadingWindow 通过 EventBus 驱动，无需 ViewModel 绑定</summary>
    public override void Bind(object viewModel) { }
}
