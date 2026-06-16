// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/05_Show/UI/WindowManager.cs
// 窗口管理器。管理所有独立窗口（UIWindow）的打开、关闭、焦点和生命周期。
// ─────────────────────────────────────────────────────────────────────
// 职责：
//   · 从 JSON 配置表加载窗口定义（IWindowConfigDataService）
//   · 实例化窗口预制体并通过 Activator.CreateInstance 构造纯 C# 窗口实例
//   · 管理窗口的 Canvas sortingOrder（z-order）
//   · 发布窗口生命周期事件
//
// 设计说明：
//   · 继承 MonoSingleton，全局唯一，同时注册到 ServiceLocator
//   · 与 UIManager 完全独立 — UIManager 管栈式面板，WindowManager 管独立窗口
//   · UIWindow 为纯 C# 类（不继承 MonoBehaviour），由 WindowManager 管理生命周期
//   · 窗口预制体从 Resources 目录加载（通过 ResourceManager）
//   · 动画参数从配置表查询后传入 UIWindow
//
// 使用方式：
//   var wm = ServiceLocator.Get<WindowManager>();
//   wm.OpenWindow("InventoryWindow");
// ══════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 窗口管理器（MonoSingleton）。
///
/// 核心数据流：
///   AppMain 注入 IWindowConfigDataService → Initialize() 构建配置映射
///   → OpenWindow(id) 加载预制体 → new WindowType(go, id) → UIWindow.Open(anim) → 播放入场动画
///   → CloseWindow(id) → UIWindow.Close(anim) → 播放退场动画 → Shutdown → Destroy
/// </summary>
public class WindowManager : MonoSingleton<WindowManager>
{
    // ══════════════════════════════════════════════════════
    // 配置注入（AppMain 在 Initialize 前设置）
    // ══════════════════════════════════════════════════════

    /// <summary>窗口配置数据服务</summary>
    public IWindowConfigDataService ConfigService { get; set; }

    /// <summary>窗口挂载的父 Canvas Transform（由 AppMain 设置或自动查找）</summary>
    public Transform WindowContainer { get; set; }

    // ══════════════════════════════════════════════════════
    // 内部状态
    // ══════════════════════════════════════════════════════

    /// <summary>windowId → 窗口配置</summary>
    private readonly Dictionary<string, WindowConfigEntryData> _configMap
        = new Dictionary<string, WindowConfigEntryData>();

    /// <summary>windowId → 当前打开的窗口实例</summary>
    private readonly Dictionary<string, UIWindow> _openWindows
        = new Dictionary<string, UIWindow>();

    /// <summary>窗口前→后排序列表（索引0为最前窗口）</summary>
    private readonly List<UIWindow> _windowOrder = new List<UIWindow>();

    /// <summary>当前最大 sortingOrder</summary>
    private int _currentMaxSortingOrder;

    /// <summary>配置已构建完成</summary>
    private bool _configBuilt;

    /// <summary>
    /// 窗口类名 → 具体 Type 的静态注册表。
    /// 新增窗口类时在此注册一行，WindowManager 通过此表解析 windowClass 配置。
    /// 使用编译期 typeof 保证 IL2CPP 安全（不依赖反射）。
    /// </summary>
    private static readonly Dictionary<string, System.Type> WindowClassRegistry
        = new Dictionary<string, System.Type>
        {
            { "MainWindow", typeof(MainWindow) },
        };

    // ══════════════════════════════════════════════════════
    // 生命周期
    // ══════════════════════════════════════════════════════

    protected override void Awake()
    {
        base.Awake();
        ServiceLocator.Register<WindowManager>(this);
    }

    /// <summary>配置注入后的初始化（ISystem）</summary>
    public override void Initialize()
    {
        InitSingleton();
        BuildConfigMap();

        // 确保有挂载容器（在 Canvas 下创建 WindowContainer）
        if (WindowContainer == null)
        {
            var go = new GameObject("WindowContainer");
            go.transform.SetParent(transform);
            WindowContainer = go.transform;
        }

        Debug.Log($"[WindowManager] 初始化完成，共 {_configMap.Count} 个窗口配置");
    }

    /// <summary>系统关闭清理（ISystem）</summary>
    public override void Shutdown()
    {
        // 关机时直接同步销毁所有窗口（跳过 DOTween 动画，避免 "GameObject is inactive" 错误）
        DestroyAllWindowsImmediate();
        _configMap.Clear();
        ServiceLocator.Unregister<WindowManager>();
        Debug.Log("[WindowManager] 已关闭");
        base.Shutdown();
    }

    /// <summary>立即销毁所有窗口实例（关机清理用，无动画）</summary>
    private void DestroyAllWindowsImmediate()
    {
        foreach (var kvp in _openWindows)
        {
            if (kvp.Value != null)
            {
                kvp.Value.OnClosed();
                kvp.Value.Shutdown();
                Destroy(kvp.Value.Root);
            }
        }
        _openWindows.Clear();
        _windowOrder.Clear();
    }

    // ══════════════════════════════════════════════════════
    // 配置构建
    // ══════════════════════════════════════════════════════

    /// <summary>从注入的数据服务构建配置映射</summary>
    private void BuildConfigMap()
    {
        if (_configBuilt) return;

        if (ConfigService == null)
        {
            Debug.LogWarning("[WindowManager] IWindowConfigDataService 尚未注入，窗口配置为空");
            _configBuilt = true;
            return;
        }

        var entries = ConfigService.GetAllEntries();
        foreach (var entry in entries)
        {
            if (string.IsNullOrEmpty(entry.WindowId))
            {
                Debug.LogWarning("[WindowManager] 跳过 windowId 为空的配置条目");
                continue;
            }

            if (_configMap.ContainsKey(entry.WindowId))
            {
                Debug.LogWarning($"[WindowManager] 重复的窗口ID: {entry.WindowId}，使用首次出现的配置");
                continue;
            }

            _configMap[entry.WindowId] = entry;
        }

        _configBuilt = true;
    }

    // ══════════════════════════════════════════════════════
    // 窗口操作
    // ══════════════════════════════════════════════════════

    /// <summary>打开指定窗口</summary>
    /// <param name="windowId">窗口ID（对应 windows.json 中的 windowId）</param>
    public void OpenWindow(string windowId)
    {
        if (string.IsNullOrEmpty(windowId))
        {
            Debug.LogWarning("[WindowManager] OpenWindow: windowId 为空");
            return;
        }

        if (!TryGetConfig(windowId, out var config))
            return;

        // 已打开则聚焦已有窗口
        if (_openWindows.TryGetValue(windowId, out var existing))
        {
            Debug.Log($"[WindowManager] 窗口 {windowId} 已打开，聚焦已有实例");
            FocusWindow(existing);
            return;
        }

        // 加载预制体
        var prefab = ResourceManager.Instance.Load<GameObject>(config.PrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[WindowManager] 无法加载窗口预制体: {config.PrefabPath} (windowId={windowId})");
            return;
        }

        var go = Instantiate(prefab, WindowContainer);
        go.name = windowId;

        // 设置窗口根 RectTransform 为撑满父容器四边
        var goRt = go.GetComponent<RectTransform>();
        if (goRt != null)
        {
            goRt.anchorMin = Vector2.zero;
            goRt.anchorMax = Vector2.one;
            goRt.offsetMin = Vector2.zero;
            goRt.offsetMax = Vector2.zero;
        }

        // 确保窗口 Canvas 为 Overlay 模式（适配 GuiCanvas 层级）
        var goCanvas = go.GetComponent<Canvas>();
        if (goCanvas != null)
        {
            goCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            goCanvas.overrideSorting = true;
        }

        // 通过窗口类名解析具体 Type，构造纯 C# 窗口实例
        if (!TryResolveWindowType(config.WindowClass, out var windowType))
        {
            Debug.LogError($"[WindowManager] 无法解析窗口类: {config.WindowClass} (windowId={windowId})");
            Destroy(go);
            return;
        }

        var window = (UIWindow)Activator.CreateInstance(windowType, go, windowId);

        // 设置初始 sortingOrder
        _currentMaxSortingOrder = Mathf.Max(_currentMaxSortingOrder, config.DefaultSortingOrder);
        window.SortingOrder = _currentMaxSortingOrder;

        // 注册并加入排序列表
        _openWindows[windowId] = window;
        _windowOrder.Insert(0, window); // 新窗口默认最前

        // 解析动画类型
        var openAnim = ParseAnimationType(config.OpenAnimType);
        var openDur = config.OpenAnimDuration > 0f ? config.OpenAnimDuration : 0.2f;

        // 打开窗口（含动画），动画完成后发布事件
        window.Open(openAnim, openDur, () =>
        {
            EventBus.Publish(new WindowOpenedEvent { WindowId = windowId });
        });
    }

    /// <summary>关闭指定窗口</summary>
    public void CloseWindow(string windowId)
    {
        if (string.IsNullOrEmpty(windowId)) return;

        if (!_openWindows.TryGetValue(windowId, out var window))
        {
            Debug.LogWarning($"[WindowManager] 窗口 {windowId} 未打开，无法关闭");
            return;
        }

        if (!TryGetConfig(windowId, out var config))
        {
            // 配置不存在时直接清理
            DestroyWindowInstance(windowId, window);
            return;
        }

        var closeAnim = ParseAnimationType(config.CloseAnimType);
        var closeDur = config.CloseAnimDuration > 0f ? config.CloseAnimDuration : 0.15f;

        // 从打开列表移除（CloseWindow 可能被重复调用）
        _openWindows.Remove(windowId);
        _windowOrder.Remove(window);

        // 关闭窗口（含动画），动画完成后清理并销毁实例
        window.Close(closeAnim, closeDur, () =>
        {
            if (window != null)
            {
                window.Shutdown();
                Destroy(window.Root);
            }
            EventBus.Publish(new WindowClosedEvent { WindowId = windowId });
        });
    }

    /// <summary>关闭所有窗口</summary>
    public void CloseAllWindows()
    {
        if (_openWindows.Count == 0)
        {
            EventBus.Publish(new AllWindowsClosedEvent());
            return;
        }

        // [PERF] 收集 windowId 列表后逐个关闭（避免字典在迭代中修改）
        var ids = new List<string>(_openWindows.Keys);
        foreach (var id in ids)
        {
            CloseWindow(id);
        }

        EventBus.Publish(new AllWindowsClosedEvent());
    }

    /// <summary>销毁窗口实例（不含动画）</summary>
    private void DestroyWindowInstance(string windowId, UIWindow window)
    {
        _openWindows.Remove(windowId);
        _windowOrder.Remove(window);

        if (window != null)
        {
            window.OnClosed();
            window.Shutdown();
            Destroy(window.Root);
        }

        EventBus.Publish(new WindowClosedEvent { WindowId = windowId });
    }

    // ══════════════════════════════════════════════════════
    // 焦点管理
    // ══════════════════════════════════════════════════════

    /// <summary>将指定窗口提至最前</summary>
    public void FocusWindow(string windowId)
    {
        if (!_openWindows.TryGetValue(windowId, out var window)) return;
        FocusWindow(window);
    }

    /// <summary>将指定窗口实例提至最前</summary>
    public void FocusWindow(UIWindow window)
    {
        if (window == null) return;

        // 从当前位置移除
        _windowOrder.Remove(window);

        // 插入最前
        _windowOrder.Insert(0, window);

        // 更新 sortingOrder
        RecalculateSortingOrders();

        // 通知窗口获得焦点
        window.OnFocusGained();

        EventBus.Publish(new WindowFocusedEvent { WindowId = window.WindowId });
    }

    /// <summary>重新计算所有打开窗口的 sortingOrder</summary>
    private void RecalculateSortingOrders()
    {
        int baseOrder = _currentMaxSortingOrder;

        // [PERF] 从后往前分配 sortingOrder（最后面的窗口获得最小 sortingOrder）
        for (int i = _windowOrder.Count - 1; i >= 0; i--)
        {
            if (_windowOrder[i] != null)
            {
                _windowOrder[i].SortingOrder = baseOrder - (_windowOrder.Count - 1 - i);
            }
        }
    }

    // ══════════════════════════════════════════════════════
    // 查询
    // ══════════════════════════════════════════════════════

    /// <summary>检查窗口是否已打开</summary>
    public bool IsWindowOpen(string windowId)
    {
        return !string.IsNullOrEmpty(windowId) && _openWindows.ContainsKey(windowId);
    }

    /// <summary>获取已打开的窗口实例（泛型版）</summary>
    public T GetWindow<T>(string windowId) where T : UIWindow
    {
        if (_openWindows.TryGetValue(windowId, out var window))
            return window as T;
        return null;
    }

    /// <summary>获取窗口配置</summary>
    public WindowConfigEntryData GetConfig(string windowId)
    {
        TryGetConfig(windowId, out var config);
        return config;
    }

    /// <summary>获取所有已配置的窗口ID</summary>
    public System.Collections.Generic.IReadOnlyList<string> GetAllWindowIds()
    {
        var ids = new List<string>(_configMap.Keys);
        return ids;
    }

    /// <summary>安全获取窗口配置</summary>
    private bool TryGetConfig(string windowId, out WindowConfigEntryData config)
    {
        if (!_configMap.TryGetValue(windowId, out config))
        {
            Debug.LogWarning($"[WindowManager] 未找到窗口配置: {windowId}");
            return false;
        }
        return true;
    }

    // ══════════════════════════════════════════════════════
    // 工具方法
    // ══════════════════════════════════════════════════════

    /// <summary>将字符串动画类型解析为 WindowAnimationType 枚举</summary>
    private static WindowAnimationType ParseAnimationType(string typeStr)
    {
        if (string.IsNullOrEmpty(typeStr))
            return WindowAnimationType.None;

        // 尝试精确匹配
        if (System.Enum.TryParse<WindowAnimationType>(typeStr, true, out var result))
            return result;

        Debug.LogWarning($"[WindowManager] 未知的动画类型: {typeStr}，使用 None");
        return WindowAnimationType.None;
    }

    /// <summary>
    /// 从静态注册表解析窗口类名 → Type。
    /// 新增窗口类时需在 WindowClassRegistry 中注册。
    /// </summary>
    /// <param name="windowClass">窗口类名（对应 JSON 配置中的 windowClass）</param>
    /// <param name="type">解析成功时返回对应 Type</param>
    /// <returns>是否解析成功</returns>
    private static bool TryResolveWindowType(string windowClass, out System.Type type)
    {
        if (string.IsNullOrEmpty(windowClass))
        {
            Debug.LogWarning("[WindowManager] windowClass 为空，无法解析窗口类型");
            type = null;
            return false;
        }

        if (WindowClassRegistry.TryGetValue(windowClass, out type))
            return true;

        Debug.LogWarning($"[WindowManager] 未注册的窗口类: {windowClass}，请在 WindowClassRegistry 中添加映射");
        return false;
    }
}
