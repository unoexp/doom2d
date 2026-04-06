// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/04_Gameplay/Tutorial/TutorialSystem.cs
// 教学引导系统。管理首次触发提示，不用教程关卡，不用弹窗说教。
// ══════════════════════════════════════════════════════════════════════
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 教学引导系统。
///
/// 核心职责：
///   · 跟踪哪些教学提示已经显示过
///   · 监听业务事件，在首次触发时发布 TutorialTriggerEvent
///   · 通过 EventBus 广播，由 UI 层的 TutorialPresenter 显示提示
///
/// 设计说明：
///   · 每个教学提示只显示一次（HashSet 记录）
///   · 提示内容通过 _tutorialMessages 字典配置
///   · 实现 ISaveable 持久化已显示的提示
/// </summary>
public class TutorialSystem : MonoBehaviour, ISaveable
{
    // ══════════════════════════════════════════════════════
    // 字段
    // ══════════════════════════════════════════════════════

    /// <summary>已触发过的教学提示</summary>
    private readonly HashSet<TutorialTrigger> _triggered = new HashSet<TutorialTrigger>();

    /// <summary>教学提示文本映射</summary>
    private static readonly Dictionary<TutorialTrigger, string> _tutorialMessages
        = new Dictionary<TutorialTrigger, string>
    {
        { TutorialTrigger.FirstPickup,    "[E] 拾取物品" },
        { TutorialTrigger.FirstInventory, "按 [Tab] 打开背包管理物品" },
        { TutorialTrigger.HungerWarning,  "你感到饥饿了，寻找食物吧" },
        { TutorialTrigger.ColdWarning,    "体温正在下降，回到庇护所取暖" },
        { TutorialTrigger.FirstCombat,    "左键攻击，右键格挡，空格闪避" },
        { TutorialTrigger.FirstDeath,     "你倒下了，将从上次存档点重新开始" },
        { TutorialTrigger.FirstCraft,     "按 [C] 打开制作面板" },
        { TutorialTrigger.FirstBuild,     "在庇护所区域内可以建造设施" },
        { TutorialTrigger.FirstDig,       "使用镐对准地面挖掘资源" },
    };

    // ══════════════════════════════════════════════════════
    // ISaveable
    // ══════════════════════════════════════════════════════

    public string SaveKey => nameof(TutorialSystem);

    // ══════════════════════════════════════════════════════
    // 生命周期
    // ══════════════════════════════════════════════════════

    private void Awake()
    {
        ServiceLocator.Register<TutorialSystem>(this);
    }

    private void Start()
    {
        if (ServiceLocator.TryGet<SaveLoadSystem>(out var saveSystem))
            saveSystem.Register(this);
    }

    private void OnEnable()
    {
        EventBus.Subscribe<ItemAddedToInventoryEvent>(OnItemPickup);
        EventBus.Subscribe<EntityDiedEvent>(OnEntityDied);
        EventBus.Subscribe<PlayerDeadEvent>(OnPlayerDead);
        EventBus.Subscribe<CraftingResultEvent>(OnCrafted);
        EventBus.Subscribe<BuildCompletedEvent>(OnBuilt);
        EventBus.Subscribe<SurvivalAttributeWarningEvent>(OnSurvivalWarning);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<ItemAddedToInventoryEvent>(OnItemPickup);
        EventBus.Unsubscribe<EntityDiedEvent>(OnEntityDied);
        EventBus.Unsubscribe<PlayerDeadEvent>(OnPlayerDead);
        EventBus.Unsubscribe<CraftingResultEvent>(OnCrafted);
        EventBus.Unsubscribe<BuildCompletedEvent>(OnBuilt);
        EventBus.Unsubscribe<SurvivalAttributeWarningEvent>(OnSurvivalWarning);
    }

    private void OnDestroy()
    {
        if (ServiceLocator.TryGet<SaveLoadSystem>(out var saveSystem))
            saveSystem.Unregister(this);

        ServiceLocator.Unregister<TutorialSystem>();
    }

    // ══════════════════════════════════════════════════════
    // 公有 API
    // ══════════════════════════════════════════════════════

    /// <summary>尝试触发一个教学提示（仅首次有效）</summary>
    public bool TryTrigger(TutorialTrigger trigger)
    {
        if (_triggered.Contains(trigger)) return false;

        _triggered.Add(trigger);

        string message = _tutorialMessages.TryGetValue(trigger, out var msg)
            ? msg : trigger.ToString();

        EventBus.Publish(new TutorialTriggerEvent
        {
            TriggerType = trigger,
            Message = message
        });

        return true;
    }

    /// <summary>某个教学提示是否已触发过</summary>
    public bool HasTriggered(TutorialTrigger trigger) => _triggered.Contains(trigger);

    // ══════════════════════════════════════════════════════
    // 事件处理
    // ══════════════════════════════════════════════════════

    private void OnItemPickup(ItemAddedToInventoryEvent evt)
        => TryTrigger(TutorialTrigger.FirstPickup);

    private void OnEntityDied(EntityDiedEvent evt)
    {
        if (evt.Cause == DeathCause.Combat && evt.KillerInstanceId != 0)
            TryTrigger(TutorialTrigger.FirstCombat);
    }

    private void OnPlayerDead(PlayerDeadEvent evt)
        => TryTrigger(TutorialTrigger.FirstDeath);

    private void OnCrafted(CraftingResultEvent evt)
    {
        if (evt.Result == CraftingResult.Success)
            TryTrigger(TutorialTrigger.FirstCraft);
    }

    private void OnBuilt(BuildCompletedEvent evt)
        => TryTrigger(TutorialTrigger.FirstBuild);

    private void OnSurvivalWarning(SurvivalAttributeWarningEvent evt)
    {
        if (evt.AttributeType == SurvivalAttributeType.Hunger)
            TryTrigger(TutorialTrigger.HungerWarning);
        else if (evt.AttributeType == SurvivalAttributeType.Temperature)
            TryTrigger(TutorialTrigger.ColdWarning);
    }

    // ══════════════════════════════════════════════════════
    // ISaveable
    // ══════════════════════════════════════════════════════

    public object CaptureState()
    {
        var list = new List<int>();
        foreach (var t in _triggered)
            list.Add((int)t);
        return list;
    }

    public void RestoreState(object state)
    {
        _triggered.Clear();
        if (state is List<int> list)
        {
            for (int i = 0; i < list.Count; i++)
                _triggered.Add((TutorialTrigger)list[i]);
        }
    }
}
