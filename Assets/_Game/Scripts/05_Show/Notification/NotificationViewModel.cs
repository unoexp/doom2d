// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/05_Show/Notification/NotificationViewModel.cs
// 通知Toast的ViewModel。管理通知队列和显示状态。
// ══════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单条通知的显示数据
/// </summary>
public struct NotificationDisplayData
{
    public string Message;
    public NotificationType Type;
    public Sprite Icon;
    public float Duration;
    public float CreatedTime;
}

/// <summary>
/// 通知系统 ViewModel。
///
/// 核心职责：
///   · 管理待显示的通知队列
///   · 控制同屏最大显示数量
///   · 管理每条通知的生命周期（自动消失）
///   · 暴露事件通知 View 层更新
/// </summary>
public class NotificationViewModel
{
    // ══════════════════════════════════════════════════════
    // 配置
    // ══════════════════════════════════════════════════════

    /// <summary>同屏最大通知数</summary>
    public const int MaxVisibleCount = 5;

    /// <summary>默认显示时长（秒）</summary>
    public const float DefaultDuration = 3f;

    // ══════════════════════════════════════════════════════
    // 数据
    // ══════════════════════════════════════════════════════

    /// <summary>当前显示中的通知</summary>
    private readonly List<NotificationDisplayData> _activeNotifications
        = new List<NotificationDisplayData>();

    /// <summary>等待显示的通知队列</summary>
    private readonly Queue<NotificationDisplayData> _pendingQueue
        = new Queue<NotificationDisplayData>();

    // ══════════════════════════════════════════════════════
    // 事件
    // ══════════════════════════════════════════════════════

    /// <summary>新通知被添加到显示列表</summary>
    public event Action<NotificationDisplayData> OnNotificationAdded;

    /// <summary>通知被移除（过期）</summary>
    public event Action<int> OnNotificationRemoved;

    // ══════════════════════════════════════════════════════
    // 属性
    // ══════════════════════════════════════════════════════

    public int ActiveCount => _activeNotifications.Count;

    // ══════════════════════════════════════════════════════
    // 公有 API
    // ══════════════════════════════════════════════════════

    /// <summary>入队一条通知</summary>
    public void Enqueue(string message, NotificationType type,
                        Sprite icon = null, float duration = 0f)
    {
        var data = new NotificationDisplayData
        {
            Message = message,
            Type = type,
            Icon = icon,
            Duration = duration > 0f ? duration : DefaultDuration,
            CreatedTime = Time.time
        };

        if (_activeNotifications.Count < MaxVisibleCount)
        {
            _activeNotifications.Add(data);
            OnNotificationAdded?.Invoke(data);
        }
        else
        {
            _pendingQueue.Enqueue(data);
        }
    }

    /// <summary>每帧更新，检查过期通知</summary>
    public void Update(float currentTime)
    {
        // [PERF] 倒序遍历以安全移除
        for (int i = _activeNotifications.Count - 1; i >= 0; i--)
        {
            var notif = _activeNotifications[i];
            if (currentTime - notif.CreatedTime >= notif.Duration)
            {
                _activeNotifications.RemoveAt(i);
                OnNotificationRemoved?.Invoke(i);
            }
        }

        // 从队列补充
        while (_pendingQueue.Count > 0
               && _activeNotifications.Count < MaxVisibleCount)
        {
            var next = _pendingQueue.Dequeue();
            next.CreatedTime = currentTime;
            _activeNotifications.Add(next);
            OnNotificationAdded?.Invoke(next);
        }
    }

    /// <summary>清空所有通知</summary>
    public void Clear()
    {
        _activeNotifications.Clear();
        _pendingQueue.Clear();
    }
}
