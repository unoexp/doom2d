// 📁 Assets/_Game/02_Base/ServiceLocator/ServiceLocator.cs
using System;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 轻量级服务定位器，替代全局单例泛滥问题。
/// 各核心系统在Awake中注册自身，其他系统通过此访问。
/// 🏗️ 架构说明：比单例更利于测试，可在测试时注入Mock实现。
/// </summary>
public static class ServiceLocator
{
    private static readonly Dictionary<Type, object> _services 
        = new Dictionary<Type, object>();

    /// <summary>注册服务（系统初始化时调用）</summary>
    public static void Register<T>(T service) where T : class
    {
        var type = typeof(T);
        if (_services.ContainsKey(type))
            Debug.LogWarning($"[ServiceLocator] 服务 {type.Name} 已存在，将被覆盖");
        _services[type] = service;
    }

    /// <summary>获取服务</summary>
    public static T Get<T>() where T : class
    {
        if (_services.TryGetValue(typeof(T), out var service))
            return service as T;
        
        Debug.LogError($"[ServiceLocator] 服务 {typeof(T).Name} 未注册！");
        return null;
    }

    /// <summary>尝试获取服务（不抛异常）</summary>
    public static bool TryGet<T>(out T service) where T : class
    {
        if (_services.TryGetValue(typeof(T), out var obj))
        {
            service = obj as T;
            return true;
        }
        service = null;
        return false;
    }

    public static void Unregister<T>() where T : class 
        => _services.Remove(typeof(T));
    
    public static void Clear() => _services.Clear();
}