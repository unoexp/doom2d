// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/04_Gameplay/AppMain.cs
// 游戏唯一入口点（骨架版本）。场景中仅需挂载此脚本，所有后端系统通过代码创建。
// ─────────────────────────────────────────────────────────────────────
// 职责：
//   1. 在 Awake 中创建 Base 基础设施系统（MonoSingletons）
//   2. 在 Start 协程中加载 JSON 数据并创建 Core 系统
//   3. 验证所有系统注册状态
//   4. 在 OnDestroy 中逆序关闭所有系统
//
// 💡 骨架版本仅包含基础设施，游戏具体系统（背包/制作/战斗等）已移除。
//    重新实现游戏系统时在此文件中添加对应的 CreateSystem 调用。
//
// 使用方式：
//   · 在场景中创建一个 GameObject，挂载 AppMain 脚本
//   · Script Execution Order 设为 -100（确保最先执行）
// ══════════════════════════════════════════════════════════════════════
using System.Collections;
using UnityEngine;

/// <summary>
/// 游戏唯一入口点。场景中仅需此脚本，所有后端系统由代码创建。
/// </summary>
public class AppMain : MonoBehaviour
{
    // ══════════════════════════════════════════════════════
    // 配置（仅保留非数据 SO 的运行时参数）
    // ══════════════════════════════════════════════════════

    [Header("VFXManager")]
    [SerializeField] private VFXEntry[] _vfxCatalog;

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

        DontDestroyOnLoad(gameObject);

        // 创建子系统根节点（所有代码创建的系统挂载在此下）
        _systemsRoot = new GameObject("_Systems").transform;
        _systemsRoot.SetParent(transform);
        _singletonsRoot = new GameObject("_Singletons").transform;
        _singletonsRoot.SetParent(transform);

        // Awake 中只创建无数据依赖的 Base 系统
        CreateBaseSystems();

        // 启动协程：等待数据加载 → 创建业务系统 → 创建玩法系统
        StartCoroutine(BootstrapCoroutine());

        _initialized = true;
    }

    private IEnumerator BootstrapCoroutine()
    {
        Debug.Log("[AppMain] ── 开始数据加载阶段 ──");

        // Phase 1: 创建 DataLoaderSystem 并等待基础设施 JSON 数据加载
        _dataLoader = CreateSystem<DataLoaderSystem>("DataLoaderSystem");
        yield return _dataLoader.LoadAllDataAsync();

        Debug.Log("[AppMain] ── 数据加载完成，创建业务系统 ──");

        // Phase 2: 创建 Core 业务系统（骨架版本：仅 SaveLoadSystem）
        CreateCoreSystems();

        Debug.Log("[AppMain] ========== 所有系统创建完毕 ==========");

        ValidateAllSystems();

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

        // GameStateManager（无配置）
        CreateMonoSingleton<GameStateManager>("GameStateManager", null);

        // TimerSystem（无配置）
        CreateMonoSingleton<TimerSystem>("TimerSystem", null);

        // ObjectPoolManager（无配置）
        CreateMonoSingleton<ObjectPoolManager>("ObjectPoolManager", null);

        // ResourceManager（配置从 JSON 加载，在 DataLoaderSystem 中处理）
        CreateMonoSingleton<ResourceManager>("ResourceManager", rm =>
        {
            rm.CacheConfig = ServiceLocator.Get<IResourceCacheConfigDataService>().GetConfig();
            rm.Initialize();
        });

        // AudioManager（音频目录从 JSON 加载，AudioClip 按 ClipPath 动态加载）
        CreateMonoSingleton<AudioManager>("AudioManager", am =>
        {
            am.Catalogs = new[] { ServiceLocator.Get<IAudioCatalogDataService>().GetCatalog() };
            am.Initialize();
        });

        // UIManager（无配置）
        CreateMonoSingleton<UIManager>("UIManager", null);

        // VFXManager（有配置 — VFXEntry 保留，因涉及 Unity GameObject 资源）
        CreateMonoSingleton<VFXManager>("VFXManager", vm =>
        {
            vm.Catalog = _vfxCatalog;
            vm.Initialize();
        });
    }

    // ── 03_Core：业务系统（骨架版本：仅 SaveLoadSystem）──

    private void CreateCoreSystems()
    {
        Debug.Log("[AppMain] ── 创建 Core 系统 ──");

        // SaveLoadSystem（无配置）
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
        registered += CheckAndLog<UIManager>("UIManager", ref total);
        registered += CheckAndLog<VFXManager>("VFXManager", ref total);

        Debug.Log("[AppMain] ── Data Services ──");
        registered += CheckAndLog<IResourceCacheConfigDataService>("IResourceCacheConfigDataService", ref total);
        registered += CheckAndLog<IAudioCatalogDataService>("IAudioCatalogDataService", ref total);

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
