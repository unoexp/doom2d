// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/05_Show/Notification/NotificationView.cs
// 通知Toast的View层。负责通知条目的显示和动画。
// ══════════════════════════════════════════════════════════════════════
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 通知系统 View。
///
/// 核心职责：
///   · 管理通知条目 UI 对象的创建和销毁
///   · 绑定 ViewModel 事件驱动显示更新
///   · 不含任何业务逻辑
///
/// 设计说明：
///   · 挂载在 Canvas 下的通知容器 GameObject 上
///   · 通知条目通过模板复制创建，按垂直布局排列
///   · 继承 UIPanel 便于 UIManager 统一管理
/// </summary>
public class NotificationView : UIPanel
{
    // ══════════════════════════════════════════════════════
    // 引用
    // ══════════════════════════════════════════════════════

    [Header("通知配置")]
    [Tooltip("通知条目模板（默认隐藏）")]
    [SerializeField] private GameObject _notificationTemplate;

    [Tooltip("通知容器（VerticalLayoutGroup）")]
    [SerializeField] private Transform _container;

    // ══════════════════════════════════════════════════════
    // 颜色配置
    // ══════════════════════════════════════════════════════

    private static readonly Color ColorInfo    = new Color(0.9f, 0.9f, 0.9f, 1f);
    private static readonly Color ColorSuccess = new Color(0.4f, 0.9f, 0.4f, 1f);
    private static readonly Color ColorWarning = new Color(1f, 0.85f, 0.3f, 1f);
    private static readonly Color ColorError   = new Color(1f, 0.4f, 0.4f, 1f);
    private static readonly Color ColorPickup  = new Color(0.6f, 0.85f, 1f, 1f);
    private static readonly Color ColorUnlock  = new Color(1f, 0.8f, 0.3f, 1f);

    // ══════════════════════════════════════════════════════
    // ViewModel
    // ══════════════════════════════════════════════════════

    private NotificationViewModel _viewModel;

    // ══════════════════════════════════════════════════════
    // 公有 API
    // ══════════════════════════════════════════════════════

    /// <summary>绑定 ViewModel</summary>
    public void Bind(NotificationViewModel viewModel)
    {
        // 解绑旧的
        if (_viewModel != null)
        {
            _viewModel.OnNotificationAdded -= OnNotificationAdded;
            _viewModel.OnNotificationRemoved -= OnNotificationRemoved;
        }

        _viewModel = viewModel;

        if (_viewModel != null)
        {
            _viewModel.OnNotificationAdded += OnNotificationAdded;
            _viewModel.OnNotificationRemoved += OnNotificationRemoved;
        }
    }

    // ══════════════════════════════════════════════════════
    // 生命周期
    // ══════════════════════════════════════════════════════

    protected override void Awake()
    {
        base.Awake();

        // 隐藏模板
        if (_notificationTemplate != null)
        {
            _notificationTemplate.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        Bind(null);
    }

    // ══════════════════════════════════════════════════════
    // ViewModel 事件回调
    // ══════════════════════════════════════════════════════

    private void OnNotificationAdded(NotificationDisplayData data)
    {
        if (_notificationTemplate == null || _container == null) return;

        var go = Instantiate(_notificationTemplate, _container);
        go.SetActive(true);

        // 设置文本
        var text = go.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            text.text = data.Message;
            text.color = GetColorForType(data.Type);
        }

        // 设置图标
        var icon = go.transform.Find("Icon");
        if (icon != null)
        {
            var img = icon.GetComponent<Image>();
            if (img != null)
            {
                if (data.Icon != null)
                {
                    img.sprite = data.Icon;
                    img.enabled = true;
                }
                else
                {
                    img.enabled = false;
                }
            }
        }
    }

    private void OnNotificationRemoved(int index)
    {
        if (_container == null) return;
        if (index >= 0 && index < _container.childCount)
        {
            // 跳过模板（childIndex 0 可能是模板）
            int childIndex = index;
            // 查找第 index 个激活的子对象
            int activeIndex = 0;
            for (int i = 0; i < _container.childCount; i++)
            {
                var child = _container.GetChild(i);
                if (!child.gameObject.activeSelf) continue;
                if (activeIndex == childIndex)
                {
                    Destroy(child.gameObject);
                    return;
                }
                activeIndex++;
            }
        }
    }

    // ══════════════════════════════════════════════════════
    // 内部方法
    // ══════════════════════════════════════════════════════

    private static Color GetColorForType(NotificationType type)
    {
        switch (type)
        {
            case NotificationType.Success:
            case NotificationType.Craft:
            case NotificationType.Build:
                return ColorSuccess;
            case NotificationType.Warning:
                return ColorWarning;
            case NotificationType.Error:
                return ColorError;
            case NotificationType.ItemPickup:
                return ColorPickup;
            case NotificationType.Unlock:
                return ColorUnlock;
            default:
                return ColorInfo;
        }
    }
}
