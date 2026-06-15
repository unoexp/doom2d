// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/06_Extensions/Mod/ModLoader.cs
// MOD 加载器。扫描、加载、管理 MOD 生命周期。
// ══════════════════════════════════════════════════════════════════════
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// MOD 信息
/// </summary>
public struct ModInfo
{
    public string ModId;
    public string ModName;
    public string Version;
    public string DirectoryPath;
    public bool IsEnabled;
}

/// <summary>
/// MOD 加载器。
///
/// 核心职责：
///   · 扫描 StreamingAssets/Mods/ 目录发现 MOD
///   · 管理 MOD 的启用/禁用状态
///   · 调用 IModEntry 生命周期方法
///   · 收集 IModDataProvider 提供的自定义数据
///
/// 设计说明：
///   · 06_Extensions 层，可引用所有低层
///   · MOD 通过约定目录结构注册（非反射加载，安全可控）
///   · [MIGRATED] 数据从 ScriptableObject 迁移到 JSON POCO
/// </summary>
public class ModLoader : MonoBehaviour
{
    // ══════════════════════════════════════════════════════
    // 配置
    // ══════════════════════════════════════════════════════

    [Header("MOD 目录")]
    [Tooltip("MOD 根目录（相对于 StreamingAssets）")]
    [SerializeField] private string _modsFolder = "Mods";

    // ══════════════════════════════════════════════════════
    // 数据
    // ══════════════════════════════════════════════════════

    /// <summary>已注册的 MOD 条目</summary>
    private readonly List<IModEntry> _registeredMods = new List<IModEntry>();

    /// <summary>已注册的数据提供者</summary>
    private readonly List<IModDataProvider> _dataProviders = new List<IModDataProvider>();

    /// <summary>已发现的 MOD 信息</summary>
    private readonly List<ModInfo> _discoveredMods = new List<ModInfo>();

    // ══════════════════════════════════════════════════════
    // 属性
    // ══════════════════════════════════════════════════════

    /// <summary>已加载的 MOD 数量</summary>
    public int LoadedModCount => _registeredMods.Count;

    /// <summary>已发现的 MOD 列表</summary>
    public IReadOnlyList<ModInfo> DiscoveredMods => _discoveredMods;

    // ══════════════════════════════════════════════════════
    // 生命周期
    // ══════════════════════════════════════════════════════

    private void Awake()
    {
        ServiceLocator.Register<ModLoader>(this);
    }

    private void Start()
    {
        ScanModDirectory();
        InitializeAllMods();
    }

    private void OnDestroy()
    {
        UnloadAllMods();
        ServiceLocator.Unregister<ModLoader>();
    }

    // ══════════════════════════════════════════════════════
    // 公有 API
    // ══════════════════════════════════════════════════════

    /// <summary>手动注册 MOD 条目（用于代码 MOD）</summary>
    public void RegisterMod(IModEntry modEntry)
    {
        if (modEntry == null) return;

        // 去重
        for (int i = 0; i < _registeredMods.Count; i++)
        {
            if (_registeredMods[i].ModId == modEntry.ModId) return;
        }

        _registeredMods.Add(modEntry);

        if (modEntry is IModDataProvider provider)
            _dataProviders.Add(provider);

        Debug.Log($"[ModLoader] 注册 MOD: {modEntry.ModName} v{modEntry.Version}");
    }

    /// <summary>获取所有 MOD 提供的自定义物品</summary>
    public List<ItemData> GetAllCustomItems()
    {
        var result = new List<ItemData>();
        for (int i = 0; i < _dataProviders.Count; i++)
        {
            var items = _dataProviders[i].GetCustomItems();
            if (items != null)
                result.AddRange(items);
        }
        return result;
    }

    // TODO: 重新实现配方系统后恢复 GetAllCustomRecipes()

    // ══════════════════════════════════════════════════════
    // 内部方法
    // ══════════════════════════════════════════════════════

    /// <summary>扫描 MOD 目录</summary>
    private void ScanModDirectory()
    {
        string modsPath = Path.Combine(Application.streamingAssetsPath, _modsFolder);
        if (!Directory.Exists(modsPath))
        {
            Debug.Log($"[ModLoader] MOD 目录不存在: {modsPath}");
            return;
        }

        var dirs = Directory.GetDirectories(modsPath);
        for (int i = 0; i < dirs.Length; i++)
        {
            string dirName = Path.GetFileName(dirs[i]);
            _discoveredMods.Add(new ModInfo
            {
                ModId = dirName,
                ModName = dirName,
                Version = "1.0",
                DirectoryPath = dirs[i],
                IsEnabled = true
            });
        }

        Debug.Log($"[ModLoader] 发现 {_discoveredMods.Count} 个 MOD 目录");
    }

    /// <summary>初始化所有已注册的 MOD</summary>
    private void InitializeAllMods()
    {
        for (int i = 0; i < _registeredMods.Count; i++)
        {
            _registeredMods[i].OnInitialize();
            _registeredMods[i].OnEnable();
        }
    }

    /// <summary>卸载所有 MOD</summary>
    private void UnloadAllMods()
    {
        for (int i = _registeredMods.Count - 1; i >= 0; i--)
        {
            _registeredMods[i].OnDisable();
            _registeredMods[i].OnUnload();
        }
        _registeredMods.Clear();
        _dataProviders.Clear();
    }
}
