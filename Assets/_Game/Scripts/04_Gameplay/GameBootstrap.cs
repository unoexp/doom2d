// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/04_Gameplay/GameBootstrap.cs
// 游戏初始化引导器。确保所有系统按正确顺序启动。
// ══════════════════════════════════════════════════════════════════════
using SurvivalGame.Core.Inventory.Expansion;
using UnityEngine;

/// <summary>
/// 游戏初始化引导器。
///
/// 核心职责：
///   · 确保基础设施（MonoSingleton 实例）最先初始化
///   · 注册全局服务（CommandInvoker 等纯 C# 类）到 ServiceLocator
///   · 管理游戏启动流程（初始化 → 加载 → 就绪）
///   · 场景切换时清理 EventBus
///
/// 使用方式：
///   · 放置在场景中最高优先级的 GameObject 上
///   · 通过 Script Execution Order 确保最先执行（或手动设置 -100）
///   · 每个游戏场景一个 Bootstrap 实例
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    // ══════════════════════════════════════════════════════
    // 配置
    // ══════════════════════════════════════════════════════

    [Header("初始化选项")]
    [Tooltip("是否在此场景加载存档")]
    [SerializeField] private bool _loadSaveOnStart = false;

    [Tooltip("存档槽位索引")]
    [SerializeField] private int _saveSlotIndex = 0;

    [Header("命令系统")]
    [Tooltip("撤销历史最大容量")]
    [SerializeField] private int _commandHistorySize = 50;

    // ══════════════════════════════════════════════════════
    // 运行时
    // ══════════════════════════════════════════════════════

    private CommandInvoker _commandInvoker;
    private bool _initialized;

    // ══════════════════════════════════════════════════════
    // 生命周期
    // ══════════════════════════════════════════════════════

    private void Awake()
    {
        if (_initialized) return;

        // ── 阶段1：注册纯 C# 全局服务 ──
        _commandInvoker = new CommandInvoker(_commandHistorySize);
        ServiceLocator.Register<CommandInvoker>(_commandInvoker);

        _initialized = true;
        Debug.Log("[Bootstrap] 阶段1 完成：全局服务注册");
    }

    private void Start()
    {
        // ── 阶段2：所有 MonoBehaviour 的 Awake 已执行 ──
        // 此时 ServiceLocator 中所有系统已注册

        Debug.Log("[Bootstrap] ========== 系统初始化摘要 ==========");

        // 验证所有系统并输出日志
        ValidateAllSystems();

        // ── 阶段3：加载存档（如果需要） ──
        if (_loadSaveOnStart)
        {
            LoadSave();
        }

        // ── 阶段4：广播游戏就绪 ──
        EventBus.Publish(new GameStateChangedEvent
        {
            PreviousState = GameState.Loading,
            NewState = GameState.GamePlay
        });

        Debug.Log("[Bootstrap] ========== 初始化完成，游戏开始 ==========");
    }

    private void OnDestroy()
    {
        // 清理纯 C# 服务
        ServiceLocator.Unregister<CommandInvoker>();

        // 场景切换时清理事件订阅
        EventBus.Clear();
    }

    // ══════════════════════════════════════════════════════
    // 内部方法
    // ══════════════════════════════════════════════════════

    /// <summary>验证所有系统注册状态并输出日志</summary>
    private void ValidateAllSystems()
    {
        int registered = 0;
        int total = 0;

        Debug.Log("[Bootstrap] ── MonoSingletons ──");
        registered += CheckAndLog<GameStateManager>("GameStateManager", ref total);
        registered += CheckAndLog<TimerSystem>("TimerSystem", ref total);
        registered += CheckAndLog<ResourceManager>("ResourceManager", ref total);
        registered += CheckAndLog<ObjectPoolManager>("ObjectPoolManager", ref total);
        registered += CheckAndLog<AudioManager>("AudioManager", ref total);
        registered += CheckAndLog<UIManager>("UIManager", ref total);
        registered += CheckAndLog<VFXManager>("VFXManager", ref total);

        Debug.Log("[Bootstrap] ── Core Systems ──");
        registered += CheckAndLog<SurvivalStatusSystem>("SurvivalStatusSystem", ref total);
        registered += CheckAndLog<IInventorySystem>("IInventorySystem", ref total);
        registered += CheckAndLog<IItemDataService>("IItemDataService", ref total);
        registered += CheckAndLog<CraftingSystem>("CraftingSystem", ref total);
        registered += CheckAndLog<BuildingSystem>("BuildingSystem", ref total);
        registered += CheckAndLog<EquipmentSystem>("EquipmentSystem", ref total);
        registered += CheckAndLog<CurrencySystem>("CurrencySystem", ref total);
        registered += CheckAndLog<TradingSystem>("TradingSystem", ref total);
        registered += CheckAndLog<QuestSystem>("QuestSystem", ref total);
        registered += CheckAndLog<InteractionSystem>("InteractionSystem", ref total);
        registered += CheckAndLog<DifficultySystem>("DifficultySystem", ref total);
        registered += CheckAndLog<SkillSystem>("SkillSystem", ref total);
        registered += CheckAndLog<AchievementSystem>("AchievementSystem", ref total);
        registered += CheckAndLog<DiscoverySystem>("DiscoverySystem", ref total);
        registered += CheckAndLog<NPCRelationshipSystem>("NPCRelationshipSystem", ref total);
        registered += CheckAndLog<SaveLoadSystem>("SaveLoadSystem", ref total);
        registered += CheckAndLog<SpawnManager>("SpawnManager", ref total);

        Debug.Log("[Bootstrap] ── Expansion Services ──");
        registered += CheckAndLog<IExpansionEffectService>("IExpansionEffectService", ref total);
        registered += CheckAndLog<IExpansionValidationService>("IExpansionValidationService", ref total);
        registered += CheckAndLog<IExpansionConsumptionService>("IExpansionConsumptionService", ref total);

        Debug.Log("[Bootstrap] ── Gameplay ──");
        registered += CheckAndLog<CombatSystem>("CombatSystem", ref total);
        registered += CheckAndLog<CommandInvoker>("CommandInvoker", ref total);

        Debug.Log($"[Bootstrap] 系统注册状态：{registered}/{total} 已就绪");
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

    /// <summary>加载存档</summary>
    private void LoadSave()
    {
        if (ServiceLocator.TryGet<SaveLoadSystem>(out var saveSystem))
        {
            if (saveSystem.HasSaveData(_saveSlotIndex))
            {
                saveSystem.Load(_saveSlotIndex);
                Debug.Log($"[Bootstrap] 已加载存档 #{_saveSlotIndex}");
            }
            else
            {
                Debug.Log("[Bootstrap] 无存档，使用默认初始状态");
            }
        }
    }
}
