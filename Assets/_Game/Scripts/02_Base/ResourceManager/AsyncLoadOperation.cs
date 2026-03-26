// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/02_Infrastructure/ResourceManager/AsyncLoadOperation.cs
// 异步加载操作实现，封装 Resources.LoadAsync
// ══════════════════════════════════════════════════════════════════════
using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Resources异步加载操作
/// 封装 Unity 的 Resources.LoadAsync，提供进度跟踪和统一的事件接口
/// </summary>
/// <typeparam name="T">资源类型</typeparam>
public sealed class AsyncLoadOperation<T> : LoadOperation<T> where T : UnityEngine.Object
{
    // ══════════════════════════════════════════════════════
    // 字段
    // ══════════════════════════════════════════════════════

    /// <summary>Unity原生的异步操作</summary>
    private ResourceRequest _resourceRequest;

    /// <summary>是否已开始加载</summary>
    private bool _started = false;

    /// <summary>是否来自缓存</summary>
    private bool _fromCache = false;

    /// <summary>协程句柄（用于取消）</summary>
    private Coroutine _coroutine;

    /// <summary>MonoBehaviour用于启动协程（如果外部未提供）</summary>
    private static MonoBehaviour _coroutineRunner;

    // ══════════════════════════════════════════════════════
    // 构造函数
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 创建异步加载操作
    /// </summary>
    /// <param name="resourcePath">资源路径</param>
    /// <param name="requestId">请求ID</param>
    /// <param name="coroutineRunner">协程运行器（可选）</param>
    public AsyncLoadOperation(string resourcePath, string requestId = null, MonoBehaviour coroutineRunner = null)
        : base(resourcePath, requestId)
    {
        // 确保有协程运行器
        if (coroutineRunner == null && _coroutineRunner == null)
        {
            _coroutineRunner = CreateCoroutineRunner();
        }

        // 发布开始事件
        EventBus.Publish(new ResourceLoadStartedEvent {
            ResourcePath = resourcePath,
            LoadType = LoadType.Async,
            RequestId = RequestId
        });

        // 开始加载
        StartLoad(coroutineRunner ?? _coroutineRunner);
    }

    // ══════════════════════════════════════════════════════
    // 加载控制
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 开始加载过程
    /// </summary>
    private void StartLoad(MonoBehaviour runner)
    {
        if (_started) return;
        _started = true;

        _coroutine = runner.StartCoroutine(LoadRoutine());
    }

    /// <summary>
    /// 加载协程
    /// </summary>
    private IEnumerator LoadRoutine()
    {
        // 开始异步加载（yield不能在try-catch内，所以分开处理）
        _resourceRequest = Resources.LoadAsync<T>(ResourcePath);

        if (_resourceRequest == null)
        {
            CompleteFailure(new InvalidOperationException($"Resources.LoadAsync返回null: {ResourcePath}"));
            yield break;
        }

        // 等待加载完成，同时更新进度
        while (!_resourceRequest.isDone)
        {
            UpdateProgress(_resourceRequest.progress);
            yield return null;
        }

        // 加载完成，处理结果
        ProcessLoadResult();
    }

    /// <summary>
    /// 处理加载结果
    /// </summary>
    private void ProcessLoadResult()
    {
        try
        {
            // 检查加载结果
            if (_resourceRequest.asset == null)
            {
                throw ResourceLoadException.NotFound(ResourcePath);
            }

            // 类型检查
            T result = _resourceRequest.asset as T;
            if (result == null)
            {
                throw new InvalidCastException(
                    $"资源类型不匹配: 期望 {typeof(T).Name}, 实际 {_resourceRequest.asset.GetType().Name}");
            }

            // 成功完成
            CompleteSuccess(result);
        }
        catch (Exception ex)
        {
            CompleteFailure(ex);
        }
    }

    // ══════════════════════════════════════════════════════
    // 重写基类方法
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 取消加载操作
    /// </summary>
    public override void Cancel()
    {
        if (IsDone) return;

        try
        {
            // 停止协程
            if (_coroutine != null && _coroutineRunner != null)
            {
                _coroutineRunner.StopCoroutine(_coroutine);
                _coroutine = null;
            }

            // 清理资源请求
            if (_resourceRequest != null)
            {
                // Resources.LoadAsync无法真正取消，但可以停止等待
                _resourceRequest = null;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[AsyncLoadOperation] 取消操作时发生异常: {ex.Message}");
        }

        base.Cancel();
    }

    // ══════════════════════════════════════════════════════
    // 工具方法
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 创建协程运行器（如果未提供）
    /// </summary>
    private static MonoBehaviour CreateCoroutineRunner()
    {
        // 尝试查找现有的MonoBehaviour
        var existing = UnityEngine.Object.FindObjectOfType<MonoBehaviour>();
        if (existing != null)
        {
            return existing;
        }

        // 创建新的GameObject来运行协程
        var go = new GameObject("ResourceLoader_CoroutineRunner");
        go.hideFlags = HideFlags.HideAndDontSave;
        UnityEngine.Object.DontDestroyOnLoad(go);

        var runner = go.AddComponent<CoroutineRunner>();
        return runner;
    }

    /// <summary>
    /// 内部协程运行器组件
    /// </summary>
    private class CoroutineRunner : MonoBehaviour
    {
        private void OnDestroy()
        {
            // 清理时重置静态引用
            _coroutineRunner = null;
        }
    }

    // ══════════════════════════════════════════════════════
    // 调试信息
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 获取详细状态信息
    /// </summary>
    public string GetDetailedStatus()
    {
        var status = new System.Text.StringBuilder();
        status.AppendLine($"AsyncLoadOperation<{typeof(T).Name}>");
        status.AppendLine($"  Path: {ResourcePath}");
        status.AppendLine($"  RequestId: {RequestId}");
        status.AppendLine($"  Started: {_started}");
        status.AppendLine($"  FromCache: {_fromCache}");
        status.AppendLine($"  Progress: {Progress:P2}");
        status.AppendLine($"  IsDone: {IsDone}");
        status.AppendLine($"  IsSuccessful: {IsSuccessful}");

        if (IsDone)
        {
            if (IsSuccessful)
            {
                status.AppendLine($"  Result: {Result} (Type: {Result?.GetType().Name})");
            }
            else
            {
                status.AppendLine($"  Error: {Error?.Message}");
            }
        }
        else if (_resourceRequest != null)
        {
            status.AppendLine($"  UnityProgress: {_resourceRequest.progress:P2}");
            status.AppendLine($"  UnityIsDone: {_resourceRequest.isDone}");
        }

        return status.ToString();
    }
}