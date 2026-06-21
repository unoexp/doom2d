// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/04_Gameplay/AppMain.cs
// 游戏唯一入口点（骨架版本）。场景中仅需挂载此脚本，所有后端系统通过代码创建。
// ─────────────────────────────────────────────────────────────────────
// 职责：
//   1. 在 Awake 中创建 Base 基础设施系统（MonoSingletons）
//   2. 在 Start 协程中加载 JSON 数据并创建 Core 系统
//   3. 验证所有系统注册状态
//   4. 在 OnDestroy 中逆序关闭所有系统
//    重新实现游戏系统时在此文件中添加对应的 CreateSystem 调用。
// ══════════════════════════════════════════════════════════════════════
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AppMain : MonoBehaviour
{
    // ══════════════════════════════════════════════════════
    // 运行时
    // ══════════════════════════════════════════════════════

    private readonly System.Collections.Generic.List<ISystem> _allSystems
        = new System.Collections.Generic.List<ISystem>();

    private Transform _systemsRoot;
    private Transform _singletonsRoot;
    private DataLoaderSystem _dataLoader;
    private bool _initialized;

    // ══════════════════════════════════════════════════════
    // 生命周期
    // ══════════════════════════════════════════════════════

    private void Awake()
    {
        if (_initialized) return;
        AppCore.Instance.targetFrameRate = 60;
        DontDestroyOnLoad(gameObject);

        // 创建子系统根节点（所有代码创建的系统挂载在此下）
        _systemsRoot = new GameObject("_Systems").transform;
        _systemsRoot.SetParent(transform);
        _singletonsRoot = new GameObject("_Singletons").transform;
        _singletonsRoot.SetParent(transform);
        CreateBaseSystems();

        // 启动协程：等待数据加载 → 创建业务系统 → 创建玩法系统
        StartCoroutine(BootstrapCoroutine());

        _initialized = true;
    }

    private IEnumerator BootstrapCoroutine()
    {

        // ════════════════════════════════════════════════════
        // Phase 0: Additive 加载 UI 场景
        // ════════════════════════════════════════════════════
        Debug.Log("[AppMain] ── 加载 UI 场景 ──");
        yield return SceneManager.LoadSceneAsync(GameConst.SCENE_GUI, LoadSceneMode.Additive);

        // ════════════════════════════════════════════════════
        // Phase 1: 创建 DataLoaderSystem，优先加载窗口配置
        // ════════════════════════════════════════════════════
        Debug.Log("[AppMain] ── 创建 DataLoaderSystem ──");
        _dataLoader = CreateSystem<DataLoaderSystem>("DataLoaderSystem");

        // 1a. 优先加载 windows.json（WindowManager 初始化所需）
        yield return _dataLoader.LoadWindowConfigFirstAsync();

        Debug.Log("[AppMain] ── 窗口配置已加载，初始化 WindowManager ──");

        // 1b. 注入窗口配置到 WindowManager 并初始化
        {
            var wm = ServiceLocator.Get<WindowManager>();
            wm.ConfigService = ServiceLocator.Get<IWindowConfigDataService>();
            wm.Initialize();
        }

        // ════════════════════════════════════════════════════
        // Phase 2: 打开 LoadingWindow（游戏首个可见窗口）
        // ════════════════════════════════════════════════════
        Debug.Log("[AppMain] ── 打开加载窗口 ──");
        var wmRef = ServiceLocator.Get<WindowManager>();
        wmRef.OpenWindow("loading_window");

        // 广播加载开始
        EventBus.Publish(new LoadingStartedEvent { HintText = "正在加载游戏数据..." });

        // ════════════════════════════════════════════════════
        // Phase 3: 加载其余 JSON 数据（沿途发布 LoadingProgressEvent）
        // ════════════════════════════════════════════════════
        Debug.Log("[AppMain] ── 开始加载游戏数据 ──");
        yield return _dataLoader.LoadRemainingWithProgressAsync();

        Debug.Log("[AppMain] ── 数据加载完成，注入配置到 Base 系统 ──");

        // 注入其余配置到 MonoSingletons
        {
            var rm = ServiceLocator.Get<ResourceManager>();
            rm.CacheConfig = ServiceLocator.Get<IResourceCacheConfigDataService>().GetConfig();
            rm.Initialize();

            var am = ServiceLocator.Get<AudioManager>();
            am.Catalogs = new[] { ServiceLocator.Get<IAudioCatalogDataService>().GetCatalog() };
            am.Initialize();
        }

        // ════════════════════════════════════════════════════
        // Phase 4: 创建 Core 业务系统
        // ════════════════════════════════════════════════════
        Debug.Log("[AppMain] ── 创建业务系统 ──");
        CreateCoreSystems();

        Debug.Log("[AppMain] ========== 所有系统创建完毕 ==========");

        ValidateAllSystems();

        // ════════════════════════════════════════════════════
        // Phase 5: 广播加载完成 → LoadingWindow 自动关闭并回调 main_window
        // ════════════════════════════════════════════════════
        EventBus.Publish(new LoadingCompletedEvent());

        // 广播游戏就绪
        EventBus.Publish(new GameStateChangedEvent
        {
            PreviousState = GameState.Loading,
            NewState = GameState.GamePlay
        });

        Debug.Log("[AppMain] ========== 初始化完成，游戏开始 ==========");

    }

    private void OnDestroy()
    {
        // 逆序关闭所有系统
        for (int i = _allSystems.Count - 1; i >= 0; i--)
        {
            _allSystems[i]?.Shutdown();
        }
        _allSystems.Clear();

        EventBus.Clear();
    }

    // ══════════════════════════════════════════════════════
    // 系统创建
    // ══════════════════════════════════════════════════════

    // ── 02_Base：MonoSingletons ──

    private void CreateBaseSystems()
    {
        Debug.Log("[AppMain] ── 创建 Base 系统 ──");

        CreateMonoSingleton<GameStateManager>("GameStateManager", null);
        CreateMonoSingleton<TimerSystem>("TimerSystem", null);
        CreateMonoSingleton<ObjectPoolManager>("ObjectPoolManager", null);
        CreateMonoSingleton<ResourceManager>("ResourceManager", null);
        CreateMonoSingleton<AudioManager>("AudioManager", null);
        CreateMonoSingleton<VFXManager>("VFXManager", null);
        CreateMonoSingleton<WindowManager>("WindowManager", null);
        CreateMonoSingleton<SceneLoadSystem>("SceneLoadSystem", null);
    }

    // ── 03_Core：业务系统（骨架版本：仅 SaveLoadSystem）──

    private void CreateCoreSystems()
    {
        Debug.Log("[AppMain] ── 创建 Core 系统 ──");

        CreateSystem<SaveLoadSystem>("SaveLoadSystem");
    }

    // ══════════════════════════════════════════════════════
    // Helper 方法
    // ══════════════════════════════════════════════════════

    /// <summary>创建 MonoSingleton（挂载到 _singletonsRoot 下）</summary>
    private T CreateMonoSingleton<T>(string name, System.Action<T> configure)
        where T : MonoSingleton<T>
    {
        var go = new GameObject(name);
        go.transform.SetParent(_singletonsRoot);
        var instance = go.AddComponent<T>();
        instance.InitSingleton();  // 触发 OnInitialize + 设置单例实例

        if (instance is ISystem sys)
            _allSystems.Add(sys);

        configure?.Invoke(instance);

        Debug.Log($"  ✅ {name}");
        return instance;
    }

    /// <summary>创建普通系统 MonoBehaviour（挂载到 _systemsRoot 下）</summary>
    private T CreateSystem<T>(string name, System.Action<T> configure = null)
        where T : MonoBehaviour
    {
        var go = new GameObject(name);
        go.transform.SetParent(_systemsRoot);
        var instance = go.AddComponent<T>();

        if (instance is ISystem sys)
            _allSystems.Add(sys);

        configure?.Invoke(instance);

        Debug.Log($"  ✅ {name}");
        return instance;
    }

    // ══════════════════════════════════════════════════════
    // 验证
    // ══════════════════════════════════════════════════════

    /// <summary>验证所有系统注册状态并输出日志（骨架版本）</summary>
    private void ValidateAllSystems()
    {
        int registered = 0;
        int total = 0;

        Debug.Log("[AppMain] ── MonoSingletons ──");
        registered += CheckAndLog<GameStateManager>("GameStateManager", ref total);
        registered += CheckAndLog<TimerSystem>("TimerSystem", ref total);
        registered += CheckAndLog<ResourceManager>("ResourceManager", ref total);
        registered += CheckAndLog<ObjectPoolManager>("ObjectPoolManager", ref total);
        registered += CheckAndLog<AudioManager>("AudioManager", ref total);
        registered += CheckAndLog<VFXManager>("VFXManager", ref total);
        registered += CheckAndLog<WindowManager>("WindowManager", ref total);
        registered += CheckAndLog<SceneLoadSystem>("SceneLoadSystem", ref total);

        Debug.Log("[AppMain] ── Data Services ──");
        registered += CheckAndLog<IResourceCacheConfigDataService>("IResourceCacheConfigDataService", ref total);
        registered += CheckAndLog<IAudioCatalogDataService>("IAudioCatalogDataService", ref total);
        registered += CheckAndLog<IVFXCataLogDataService>("IVFXCataLogDataService", ref total);
        registered += CheckAndLog<IWindowConfigDataService>("IWindowConfigDataService", ref total);
        registered += CheckAndLog<IItemDataService>("IItemDataService", ref total);

        Debug.Log("[AppMain] ── Core Systems ──");
        registered += CheckAndLog<SaveLoadSystem>("SaveLoadSystem", ref total);

        Debug.Log($"[AppMain] 系统注册状态：{registered}/{total} 已就绪");
    }

    private int CheckAndLog<T>(string name, ref int total) where T : class
    {
        total++;
        if (ServiceLocator.TryGet<T>(out _))
        {
            Debug.Log($"  ✅ {name}");
            return 1;
        }
        else
        {
            Debug.LogWarning($"  ⚠️ {name} 未注册");
            return 0;
        }
    }
}
