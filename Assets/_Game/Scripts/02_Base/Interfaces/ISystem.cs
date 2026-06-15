// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/02_Base/Interfaces/ISystem.cs
// 系统生命周期接口。所有通过 AppMain 代码创建的后端系统实现此接口。
// ══════════════════════════════════════════════════════════════════════

/// <summary>
/// 系统生命周期接口。
///
/// 设计说明：
///   · AppMain 在 AddComponent / new 之后调用 Initialize()
///   · AppMain 在 OnDestroy 中逆序调用 Shutdown()
///   · Awake() 中仅做 ServiceLocator 注册（无配置依赖）
///   · 所有配置依赖的初始化逻辑放在 Initialize() 中
/// </summary>
public interface ISystem
{
    /// <summary>
    /// 配置注入后的一次性初始化。
    /// 替代 Awake 中的配置依赖逻辑和 Start 中的跨系统引用。
    /// 调用时机：AppMain 设置完所有配置属性之后。
    /// </summary>
    void Initialize();

    /// <summary>
    /// 系统关闭清理。
    /// 替代 OnDestroy：取消 ServiceLocator 注册、EventBus 退订、SaveLoadSystem 注销。
    /// </summary>
    void Shutdown();
}
