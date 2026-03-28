// ─────────────────────────────────────────────────────────────────────
// 📁 Assets/_Game/02_Base/EventBus/Events/NotificationEvents.cs
// 通知/Toast系统事件定义
// ─────────────────────────────────────────────────────────────────────

/// <summary>通知类型</summary>
public enum NotificationType
{
    Info        = 0,    // 一般信息（白色）
    Success     = 1,    // 成功（绿色）
    Warning     = 2,    // 警告（黄色）
    Error       = 3,    // 错误（红色）
    ItemPickup  = 4,    // 物品拾取（专用样式）
    Craft       = 5,    // 制作完成
    Build       = 6,    // 建造完成
    Unlock      = 7,    // 解锁（配方/建筑/地层）
}

/// <summary>
/// 通知请求事件。任何系统均可发布此事件，表现层订阅并显示Toast。
/// </summary>
public struct NotificationRequestEvent : IEvent
{
    /// <summary>通知文本</summary>
    public string Message;

    /// <summary>通知类型（决定颜色和图标）</summary>
    public NotificationType Type;

    /// <summary>关联的物品图标（可选，拾取/制作时使用）</summary>
    public UnityEngine.Sprite Icon;

    /// <summary>显示时长（秒），0 = 使用默认值</summary>
    public float Duration;
}
