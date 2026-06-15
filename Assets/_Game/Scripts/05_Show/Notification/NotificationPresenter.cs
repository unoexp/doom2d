// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/05_Show/Notification/NotificationPresenter.cs
// 通知系统Presenter。连接业务事件与通知ViewModel。
// ══════════════════════════════════════════════════════════════════════
using UnityEngine;

/// <summary>
/// 通知系统 Presenter。
///
/// 核心职责：
///   · 订阅各种业务事件（拾取、制作、建造、解锁、预警等）
///   · 将事件转化为通知请求写入 ViewModel
///   · 每帧驱动 ViewModel 更新通知生命周期
///
/// 设计说明：
///   · 自动订阅常用事件，无需手动配置
///   · 外部系统也可直接发布 NotificationRequestEvent 显示自定义通知
///   · 注册为 HUD 常驻面板，始终可见
/// </summary>
public class NotificationPresenter : MonoBehaviour
{
    // ══════════════════════════════════════════════════════
    // 引用
    // ══════════════════════════════════════════════════════

    [SerializeField] private NotificationView _view;

    private NotificationViewModel _viewModel;

    // ══════════════════════════════════════════════════════
    // 生命周期
    // ══════════════════════════════════════════════════════

    private void Awake()
    {
        _viewModel = new NotificationViewModel();
    }

    private void Start()
    {
        if (_view != null)
        {
            _view.Bind(_viewModel);

            // 注册为 HUD（常驻显示）
            var uiManager = ServiceLocator.Get<UIManager>();
            if (uiManager != null)
            {
                uiManager.RegisterPanel(_view);
                uiManager.RegisterHUD(_view);
                _view.Show();
            }
        }
    }

    private void OnEnable()
    {
        // 通用通知入口
        EventBus.Subscribe<NotificationRequestEvent>(OnNotificationRequest);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<NotificationRequestEvent>(OnNotificationRequest);
    }

    private void Update()
    {
        _viewModel.Update(Time.time);
    }

    // ══════════════════════════════════════════════════════
    // 事件处理 —— 通用
    // ══════════════════════════════════════════════════════

    private void OnNotificationRequest(NotificationRequestEvent evt)
    {
        _viewModel.Enqueue(evt.Message, evt.Type, evt.Icon, evt.Duration);
    }

}
