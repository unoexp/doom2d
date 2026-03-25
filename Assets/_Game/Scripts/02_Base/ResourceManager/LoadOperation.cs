// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/02_Infrastructure/ResourceManager/LoadOperation.cs
// 加载操作抽象基类，支持协程等待和进度跟踪
// ══════════════════════════════════════════════════════════════════════
using System;
using UnityEngine;

/// <summary>
/// 加载操作抽象基类，提供统一的进度跟踪和状态管理
/// 继承 CustomYieldInstruction 可直接用于 Unity 协程等待
/// </summary>
/// <typeparam name="T">加载的资源类型</typeparam>
public abstract class LoadOperation<T> : CustomYieldInstruction where T : UnityEngine.Object
{
    // ══════════════════════════════════════════════════════
    // 事件委托定义
    // ══════════════════════════════════════════════════════

    /// <summary>进度更新事件</summary>
    public event Action<float> OnProgressChanged;

    /// <summary>完成事件（成功或失败）</summary>
    public event Action<LoadOperation<T>> OnCompleted;

    /// <summary>成功完成事件</summary>
    public event Action<T> OnSucceeded;

    /// <summary>失败事件</summary>
    public event Action<Exception> OnFailed;

    // ══════════════════════════════════════════════════════
    // 属性
    // ══════════════════════════════════════════════════════

    /// <summary>是否已完成（成功或失败）</summary>
    public bool IsDone { get; protected set; }

    /// <summary>是否成功</summary>
    public bool IsSuccessful { get; protected set; }

    /// <summary>是否失败</summary>
    public bool IsFailed => IsDone && !IsSuccessful;

    /// <summary>加载进度（0-1）</summary>
    public float Progress { get; protected set; }

    /// <summary>加载的资源路径</summary>
    public string ResourcePath { get; protected set; }

    /// <summary>请求ID（用于事件跟踪）</summary>
    public string RequestId { get; protected set; }

    /// <summary>加载结果（成功时）</summary>
    public T Result { get; protected set; }

    /// <summary>异常信息（失败时）</summary>
    public Exception Error { get; protected set; }

    /// <summary>加载开始时间</summary>
    public DateTime StartTime { get; protected set; }

    /// <summary>加载耗时（秒）</summary>
    public float Duration => IsDone ? (float)(DateTime.Now - StartTime).TotalSeconds : 0f;

    // ══════════════════════════════════════════════════════
    // CustomYieldInstruction 实现
    // ══════════════════════════════════════════════════════

    public override bool keepWaiting => !IsDone;

    // ══════════════════════════════════════════════════════
    // 构造函数
    // ══════════════════════════════════════════════════════

    protected LoadOperation(string resourcePath, string requestId = null)
    {
        ResourcePath = resourcePath;
        RequestId = requestId ?? GenerateRequestId();
        StartTime = DateTime.Now;
        Progress = 0f;
        IsDone = false;
        IsSuccessful = false;
    }

    // ══════════════════════════════════════════════════════
    // 受保护方法（子类调用）
    // ══════════════════════════════════════════════════════

    /// <summary>更新进度</summary>
    protected void UpdateProgress(float progress)
    {
        if (IsDone) return;

        progress = Mathf.Clamp01(progress);
        if (Mathf.Approximately(progress, Progress)) return;

        Progress = progress;
        OnProgressChanged?.Invoke(progress);

        // 发布进度事件
        EventBus.Publish(new ResourceLoadProgressEvent {
            ResourcePath = ResourcePath,
            Progress = progress,
            RequestId = RequestId
        });
    }

    /// <summary>标记为成功完成</summary>
    protected void CompleteSuccess(T result)
    {
        if (IsDone) return;

        Result = result;
        IsSuccessful = true;
        IsDone = true;
        Progress = 1f;

        // 触发事件
        OnSucceeded?.Invoke(result);
        OnCompleted?.Invoke(this);

        // 发布完成事件
        EventBus.Publish(new ResourceLoadCompletedEvent {
            ResourcePath = ResourcePath,
            FromCache = false, // 子类可覆盖
            RequestId = RequestId
        });
    }

    /// <summary>标记为失败</summary>
    protected void CompleteFailure(Exception error)
    {
        if (IsDone) return;

        Error = error;
        IsSuccessful = false;
        IsDone = true;

        // 触发事件
        OnFailed?.Invoke(error);
        OnCompleted?.Invoke(this);

        // 发布失败事件
        var loadError = error as ResourceLoadException;
        EventBus.Publish(new ResourceLoadFailedEvent {
            ResourcePath = ResourcePath,
            ErrorType = loadError?.ErrorType ?? LoadErrorType.Unknown,
            ErrorMessage = error.Message,
            RequestId = RequestId
        });
    }

    /// <summary>取消加载操作</summary>
    public virtual void Cancel()
    {
        if (IsDone) return;

        CompleteFailure(new OperationCanceledException($"加载操作被取消: {ResourcePath}"));
    }

    // ══════════════════════════════════════════════════════
    // 静态工具方法
    // ══════════════════════════════════════════════════════

    private static int _requestCounter = 0;

    private static string GenerateRequestId()
    {
        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        var counter = System.Threading.Interlocked.Increment(ref _requestCounter);
        return $"REQ_{timestamp}_{counter:000000}";
    }

    // ══════════════════════════════════════════════════════
    // 工具方法
    // ══════════════════════════════════════════════════════

    /// <summary>等待完成（协程方式）</summary>
    public System.Collections.IEnumerator WaitForCompletion()
    {
        while (!IsDone)
            yield return null;
    }

    /// <summary>获取Awaiter（支持async/await）</summary>
    public System.Runtime.CompilerServices.TaskAwaiter<T> GetAwaiter()
    {
        var tcs = new System.Threading.Tasks.TaskCompletionSource<T>();

        if (IsDone)
        {
            if (IsSuccessful)
                tcs.SetResult(Result);
            else
                tcs.SetException(Error ?? new Exception("加载失败"));
        }
        else
        {
            OnSucceeded += result => tcs.SetResult(result);
            OnFailed += error => tcs.SetException(error);
        }

        return tcs.Task.GetAwaiter();
    }

    /// <summary>ToString重写，显示加载状态</summary>
    public override string ToString()
    {
        var status = IsDone ? (IsSuccessful ? "完成" : "失败") : "加载中";
        return $"[LoadOperation<{typeof(T).Name}>] {ResourcePath} - {status} ({Progress:P0})";
    }
}