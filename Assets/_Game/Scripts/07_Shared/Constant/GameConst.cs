// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/07_Shared/Constants/GameConst.cs
// 全局游戏常量。纯静态常量，零依赖。
// ══════════════════════════════════════════════════════════════════════

/// <summary>
/// 全局游戏常量。
/// 所有层均可引用，用于避免魔法数字和字符串。
/// </summary>
public static class GameConst
{
    // ══════════════════════════════════════════════════════
    // 场景
    // ══════════════════════════════════════════════════════

    /// <summary>启动主场景（入口场景，包含 AppMain）</summary>
    public const string SCENE_MAIN = "MainGameScene";

    /// <summary>UI 场景（Additive 加载，包含 EventSystem 和 WindowContainer）</summary>
    public const string SCENE_GUI = "Gui";

    // ══════════════════════════════════════════════════════
    // 背包
    // ══════════════════════════════════════════════════════

    /// <summary>主背包默认容量</summary>
    public const int DEFAULT_INVENTORY_CAPACITY = 20;

    /// <summary>快捷栏槽位数</summary>
    public const int QUICK_SLOT_COUNT = 8;

    /// <summary>默认负重上限</summary>
    public const float DEFAULT_MAX_WEIGHT = 50f;

    // ══════════════════════════════════════════════════════
    // 生存属性
    // ══════════════════════════════════════════════════════

    /// <summary>属性默认最大值</summary>
    public const float DEFAULT_ATTRIBUTE_MAX = 100f;

    /// <summary>属性临界预警阈值（归一化）</summary>
    public const float CRITICAL_WARNING_THRESHOLD = 0.25f;

    /// <summary>属性危险阈值（归一化）</summary>
    public const float DANGER_THRESHOLD = 0.1f;

    // ══════════════════════════════════════════════════════
    // 战斗
    // ══════════════════════════════════════════════════════

    /// <summary>默认暴击倍率</summary>
    public const float DEFAULT_CRIT_MULTIPLIER = 1.5f;

    /// <summary>默认击退力度</summary>
    public const float DEFAULT_KNOCKBACK_FORCE = 5f;

    /// <summary>无敌帧持续时间（秒）</summary>
    public const float INVINCIBILITY_DURATION = 0.5f;

    // ══════════════════════════════════════════════════════
    // 世界
    // ══════════════════════════════════════════════════════

    /// <summary>一个游戏日的实际时长（秒）</summary>
    public const float GAME_DAY_DURATION = 1440f; // 24分钟

    /// <summary>物品拾取范围</summary>
    public const float ITEM_PICKUP_RANGE = 1.5f;

    /// <summary>交互检测范围</summary>
    public const float INTERACTION_RANGE = 2f;

    // ══════════════════════════════════════════════════════
    // 存档
    // ══════════════════════════════════════════════════════

    /// <summary>存档文件夹名</summary>
    public const string SAVE_FOLDER = "SaveData";

    /// <summary>自动存档文件名</summary>
    public const string AUTO_SAVE_FILE = "autosave.json";

    /// <summary>存档格式版本</summary>
    public const string SAVE_VERSION = "1.0";

    // ══════════════════════════════════════════════════════
    // 标签与图层名（避免字符串硬编码）
    // ══════════════════════════════════════════════════════

    public const string TAG_PLAYER = "Player";
    public const string TAG_ENEMY = "Enemy";
    public const string TAG_ITEM = "Item";
    public const string TAG_INTERACTABLE = "Interactable";
    public const string TAG_SHELTER = "Shelter";

    public const string LAYER_GROUND = "Ground";
    public const string LAYER_PLAYER = "Player";
    public const string LAYER_ENEMY = "Enemy";
    public const string LAYER_ITEM = "Item";
}
