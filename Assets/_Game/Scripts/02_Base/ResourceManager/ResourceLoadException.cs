// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/02_Infrastructure/ResourceManager/ResourceLoadException.cs
// 资源加载异常类
// ══════════════════════════════════════════════════════════════════════
using System;

/// <summary>
/// 资源加载异常，封装不同类型的加载错误
/// </summary>
public class ResourceLoadException : Exception
{
    /// <summary>错误类型</summary>
    public LoadErrorType ErrorType { get; }

    /// <summary>资源路径</summary>
    public string ResourcePath { get; }

    /// <summary>重试次数（如果适用）</summary>
    public int RetryCount { get; }

    /// <summary>
    /// 创建资源加载异常
    /// </summary>
    /// <param name="errorType">错误类型</param>
    /// <param name="message">错误消息</param>
    /// <param name="resourcePath">资源路径</param>
    /// <param name="innerException">内部异常</param>
    /// <param name="retryCount">重试次数</param>
    public ResourceLoadException(
        LoadErrorType errorType,
        string message,
        string resourcePath = null,
        Exception innerException = null,
        int retryCount = 0)
        : base(FormatMessage(errorType, message, resourcePath, retryCount), innerException)
    {
        ErrorType = errorType;
        ResourcePath = resourcePath;
        RetryCount = retryCount;
    }

    private static string FormatMessage(
        LoadErrorType errorType,
        string message,
        string resourcePath,
        int retryCount)
    {
        string baseMessage = $"[ResourceLoadException] {errorType}: {message}";

        if (!string.IsNullOrEmpty(resourcePath))
            baseMessage += $" (Path: {resourcePath})";

        if (retryCount > 0)
            baseMessage += $" (Retried {retryCount} times)";

        return baseMessage;
    }

    /// <summary>
    /// 创建"资源不存在"异常
    /// </summary>
    public static ResourceLoadException NotFound(string resourcePath)
    {
        return new ResourceLoadException(
            LoadErrorType.NotFound,
            $"资源不存在",
            resourcePath);
    }

    /// <summary>
    /// 创建"网络错误"异常
    /// </summary>
    public static ResourceLoadException NetworkError(
        string resourcePath,
        string errorMessage,
        int retryCount = 0,
        Exception innerException = null)
    {
        return new ResourceLoadException(
            LoadErrorType.NetworkError,
            $"网络错误: {errorMessage}",
            resourcePath,
            innerException,
            retryCount);
    }

    /// <summary>
    /// 创建"超时"异常
    /// </summary>
    public static ResourceLoadException Timeout(string resourcePath, float timeoutSeconds)
    {
        return new ResourceLoadException(
            LoadErrorType.Timeout,
            $"加载超时 ({timeoutSeconds}s)",
            resourcePath);
    }

    /// <summary>
    /// 创建"IO错误"异常
    /// </summary>
    public static ResourceLoadException IOError(string resourcePath, string errorMessage)
    {
        return new ResourceLoadException(
            LoadErrorType.IOError,
            $"IO错误: {errorMessage}",
            resourcePath);
    }

    /// <summary>
    /// 判断是否应该重试的错误类型
    /// </summary>
    public bool ShouldRetry()
    {
        return ErrorType == LoadErrorType.NetworkError ||
               ErrorType == LoadErrorType.Timeout;
    }
}