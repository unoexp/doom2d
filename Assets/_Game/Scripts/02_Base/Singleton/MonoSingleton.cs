// 📁 02_Infrastructure/Singleton/MonoSingleton.cs
// ─────────────────────────────────────────────────────────────────────
// MonoBehaviour 单例基类。
// 仅用于基础设施层管理器（AudioManager/VFXManager等），
// 业务逻辑系统优先使用 ServiceLocator。
//
// 重构说明（AppMain 架构）：
//   · Awake 不再自动调用 OnInitialize()，改为由 AppMain 显式调用 InitSingleton()
//   · 实现 ISystem 接口：Initialize() → OnInitialize(), Shutdown() → 清理
//   · 配置依赖逻辑从 OnInitialize 移至 Initialize() 重写
// ─────────────────────────────────────────────────────────────────────
using UnityEngine;

/// <summary>
/// MonoBehaviour 单例基类，实现 ISystem 生命周期。
/// </summary>
public abstract class MonoSingleton<T> : MonoBehaviour, ISystem where T : MonoSingleton<T>
{
    private static T _instance;
    private static readonly object _lock = new object();
    private bool _initialized;

    public static T Instance
    {
        get
        {
            lock (_lock)
            {
                if (_instance == null)
                    Debug.LogError($"[Singleton] {typeof(T).Name} 实例不存在！请检查场景中是否存在该对象。");
                return _instance;
            }
        }
    }

    /// <summary>是否已通过 InitSingleton 完成单例初始化</summary>
    public bool IsSingletonReady => _initialized;

    // ══════════════════════════════════════════════════════
    // Unity 生命周期
    // ══════════════════════════════════════════════════════

    protected virtual void Awake()
    {
        // [AppMain 重构] Awake 仅做实例注册 + DontDestroyOnLoad
        // OnInitialize() 由 AppMain 通过 InitSingleton() / Initialize() 显式调用
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = (T)this;
        DontDestroyOnLoad(gameObject);
    }

    protected virtual void OnDestroy()
    {
        // 确保 Shutdown 被调用（无论是由 AppMain 还是 Unity 触发销毁）
        Shutdown();
    }

    // ══════════════════════════════════════════════════════
    // ISystem 实现
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 配置注入后的初始化入口（ISystem 接口）。
    /// 子类重写此方法实现配置依赖的初始化逻辑。
    /// 默认调用 InitSingleton() → OnInitialize()。
    /// </summary>
    public virtual void Initialize()
    {
        InitSingleton();
    }

    /// <summary>
    /// 系统关闭清理（ISystem 接口）。
    /// 子类重写以取消 ServiceLocator 注册、EventBus 退订等。
    /// </summary>
    public virtual void Shutdown()
    {
        if (_instance == this) _instance = null;
    }

    // ══════════════════════════════════════════════════════
    // 单例初始化
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 显式完成单例初始化（由 AppMain 在配置注入后调用）。
    /// 调用子类的 OnInitialize()，幂等操作。
    /// </summary>
    public void InitSingleton()
    {
        if (_initialized) return;
        _initialized = true;
        OnInitialize();
    }

    /// <summary>
    /// 替代 Awake 的初始化入口，子类重写此方法。
    /// 由 AppMain 通过 InitSingleton() 或 Initialize() 调用。
    /// </summary>
    protected virtual void OnInitialize() { }
}