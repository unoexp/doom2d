// 📁 Assets/_Game/07_Shared/Constants/GameEnums.cs
// ─────────────────────────────────────────────────────────────────────
// 全局枚举定义文件
// 规则：纯枚举，零依赖，所有层均可引用
// 新增枚举值只在此处追加，不新建文件，保持统一管理
// ─────────────────────────────────────────────────────────────────────

/// <summary>
/// 生存属性类型。
/// 扩展新属性（如体温、辐射）只需在此追加枚举值，
/// SurvivalStatusSystem 会自动通过字典驱动，无需修改核心逻辑。
/// </summary>
public enum SurvivalAttributeType
{
    // ── 基础生命 ──
    Health          = 0,    // 生命值
    MaxHealth       = 1,    // 最大生命值上限（可被升级/伤病影响）

    // ── 生存需求 ──
    Hunger          = 10,   // 饱食度（0=极度饥饿）
    Thirst          = 11,   // 水分（0=极度口渴）

    // ── 环境适应 ──
    Temperature     = 20,   // 体温（过高/过低均致命）
    Stamina         = 21,   // 体力（奔跑/战斗消耗）
    Oxygen          = 22,   // 氧气（水下/密闭空间）

    // ── 精神状态 ──
    Sanity          = 30,   // 精神值（长期黑暗/恐怖事件影响）

    // ── 预留扩展槽 ──
    Radiation       = 40,   // 辐射值（核末世玩法预留）
    Infection       = 41,   // 感染度（丧尸病毒预留）
    Custom_01       = 90,   // MOD自定义属性槽位1
    Custom_02       = 91,   // MOD自定义属性槽位2
}

// ─────────────────────────────────────────────────────────────────────

/// <summary>
/// 玩家死亡原因。用于死亡结算界面、成就系统、遗物系统等。
/// </summary>
public enum DeathCause
{
    Unknown         = 0,
    Combat          = 1,    // 战斗伤害致死
    Starvation      = 2,    // 饥饿致死
    Dehydration     = 3,    // 脱水致死
    Hypothermia     = 4,    // 低温冻死
    Hyperthermia    = 5,    // 高温热死
    Suffocation     = 6,    // 窒息（氧气耗尽）
    Infection       = 7,    // 感染致死
    Radiation       = 8,    // 辐射致死
    Fall            = 9,    // 坠落伤害致死
    Insanity        = 10,   // 精神崩溃致死
    Poison          = 11,   // 中毒致死
}

// ─────────────────────────────────────────────────────────────────────

/// <summary>
/// 伤害类型。影响护甲抗性计算、特效表现、音效选择。
/// </summary>
public enum DamageType
{
    Physical        = 0,    // 物理（普通攻击、摔落）
    Fire            = 1,    // 火焰（持续灼烧）
    Poison          = 2,    // 毒素（持续中毒）
    Bleed           = 3,    // 流血（持续失血）
    Ice             = 4,    // 冰冻（减速/冰冻效果）
    Electric        = 5,    // 电击（眩晕效果）
    Radiation       = 6,    // 辐射（辐射值累积）
    True            = 99,   // 真实伤害（无视所有抗性，如饥饿/口渴扣血）
}

// ─────────────────────────────────────────────────────────────────────

/// <summary>
/// 物品类型。用于背包筛选、UI分类标签、快捷栏过滤。
/// </summary>
public enum ItemType
{
    None            = 0,
    Weapon          = 1,    // 武器
    Tool            = 2,    // 工具（斧头、镐等）
    Consumable      = 3,    // 消耗品（食物、饮料、药品）
    Material        = 4,    // 制作材料（木头、石头等）
    Equipment       = 5,    // 可穿戴装备（衣服、护甲）
    Ammo            = 6,    // 弹药
    Blueprint       = 7,    // 图纸（解锁配方）
    QuestItem       = 8,    // 任务物品（不可丢弃）
    Container       = 9,    // 容器（背包、箱子）
    Seed            = 10,   // 种子（农业系统预留）
    Fuel            = 11,   // 燃料（营地篝火系统）
    Misc            = 99,   // 杂项
}

// ─────────────────────────────────────────────────────────────────────

/// <summary>
/// 物品品质/稀有度。影响显示颜色、制作成功率、耐久度上限。
/// </summary>
public enum ItemQuality
{
    Broken          = 0,    // 破损（灰色）
    Common          = 1,    // 普通（白色）
    Uncommon        = 2,    // 良好（绿色）
    Rare            = 3,    // 稀有（蓝色）
    Epic            = 4,    // 史诗（紫色）
    Legendary       = 5,    // 传说（金色）
}

// ─────────────────────────────────────────────────────────────────────

/// <summary>
/// 全局游戏状态。驱动 GameStateManager 的顶层状态机。
/// </summary>
public enum GameState
{
    None            = 0,
    Initializing    = 1,    // 启动初始化（加载配置、注册服务）
    MainMenu        = 2,    // 主菜单
    Loading         = 3,    // 加载场景中
    GamePlay        = 4,    // 正常游戏中
    Paused          = 5,    // 暂停（ESC菜单）
    Inventory       = 6,    // 打开背包（时间可选择暂停）
    Dialogue        = 7,    // 对话中
    GameOver        = 8,    // 死亡/游戏结束
    Cinematic       = 9,    // 过场动画
}

// ─────────────────────────────────────────────────────────────────────

/// <summary>
/// 玩家行为状态。驱动 PlayerStateMachine。
/// </summary>
public enum PlayerState
{
    Idle            = 0,
    Walk            = 1,
    Run             = 2,
    Crouch          = 3,    // 匍匐/蹲伏
    Jump            = 4,
    Fall            = 5,
    Climb           = 6,    // 攀爬（梯子/绳索）
    Swim            = 7,    // 游泳
    Attack          = 10,
    Aim             = 11,   // 瞄准（远程武器）
    Block           = 12,   // 格挡
    Dodge           = 13,   // 翻滚/闪避
    Interact        = 14,   // 与物体交互（开门、拾取等）
    Crafting        = 15,   // 制作中（手动制作动画）
    Sleeping        = 16,   // 睡眠（快速时间流逝）
    Stunned         = 20,   // 眩晕
    KnockedBack     = 21,   // 被击退
    Dead            = 99,   // 死亡
}

// ─────────────────────────────────────────────────────────────────────

/// <summary>
/// 敌人 AI 行为状态。驱动 EnemyStateMachine。
/// </summary>
public enum EnemyState
{
    Idle            = 0,    // 静止待机
    Patrol          = 1,    // 巡逻（按路径点）
    Wander          = 2,    // 随机游荡
    Investigate     = 3,    // 听到声音/看到痕迹，前往调查
    Alert           = 4,    // 警觉（发现玩家前的短暂状态）
    Chase           = 5,    // 追击玩家
    Attack          = 6,    // 攻击
    Flee            = 7,    // 逃跑（血量过低）
    Stunned         = 8,    // 被击晕
    Dead            = 99,
}

// ─────────────────────────────────────────────────────────────────────

/// <summary>
/// 昼夜阶段。影响敌人行为、温度、光照、刷新规则。
/// </summary>
public enum DayPhase
{
    Dawn            = 0,    // 黎明（敌人开始撤退）
    Morning         = 1,    // 上午（安全期，温度上升）
    Noon            = 2,    // 正午（最高温，警惕中暑）
    Afternoon       = 3,    // 下午
    Dusk            = 4,    // 黄昏（敌人开始活跃）
    Night           = 5,    // 夜晚（危险期，温度下降）
    Midnight        = 6,    // 深夜（最危险，特殊事件触发）
}

// ─────────────────────────────────────────────────────────────────────

/// <summary>
/// 天气类型。影响温度、能见度、移速、特定生存属性衰减速率。
/// </summary>
public enum WeatherType
{
    Clear           = 0,    // 晴天
    Cloudy          = 1,    // 阴天
    Foggy           = 2,    // 大雾（能见度降低）
    Rainy           = 3,    // 雨天（温度降低，口渴恢复）
    Thunderstorm    = 4,    // 雷暴（危险，影响电子设备）
    Snowy           = 5,    // 降雪（体温流失加速）
    Blizzard        = 6,    // 暴风雪（极端，户外几乎致命）
    Heatwave        = 7,    // 热浪（体温升高，水分消耗加速）
    AcidRain        = 8,    // 酸雨（预留，直接造成伤害）
    RadiationStorm  = 9,    // 辐射风暴（预留，强制玩家找掩体）
}

// ─────────────────────────────────────────────────────────────────────

/// <summary>
/// 装备槽位类型。
/// </summary>
public enum EquipmentSlot
{
    None            = 0,
    Head            = 1,    // 头部
    Body            = 2,    // 身体
    Legs            = 3,    // 腿部
    Feet            = 4,    // 鞋子
    Hands           = 5,    // 手套
    Back            = 6,    // 背部（背包插槽）
    MainHand        = 10,   // 主手武器
    OffHand         = 11,   // 副手（盾牌/手电筒/武器）
    Accessory_1     = 20,   // 饰品槽1
    Accessory_2     = 21,   // 饰品槽2
}

// ─────────────────────────────────────────────────────────────────────

/// <summary>
/// 制作结果状态。
/// </summary>
public enum CraftingResult
{
    Success         = 0,    // 制作成功
    Failed_NoMaterial   = 1,  // 材料不足
    Failed_NoUnlock     = 2,  // 配方未解锁
    Failed_NoWorkbench  = 3,  // 需要工作台但未在附近
    Failed_Overloaded   = 4,  // 背包已满
    Failed_Unknown      = 99,
}

// ─────────────────────────────────────────────────────────────────────

/// <summary>
/// 交互类型。IInteractable 返回此值以驱动不同的交互动画和 UI 提示。
/// </summary>
public enum InteractionType
{
    None            = 0,
    Pickup          = 1,    // 拾取物品
    Open            = 2,    // 开启（门/箱子）
    Examine         = 3,    // 检查/阅读
    Talk            = 4,    // 对话
    Harvest         = 5,    // 采集（砍树/挖矿）
    Craft           = 6,    // 使用工作台
    Sleep           = 7,    // 睡觉（床/睡袋）
    Repair          = 8,    // 修理
    Plant           = 9,    // 种植（农业）
    Activate        = 10,   // 激活（开关/机关）
}

// ─────────────────────────────────────────────────────────────────────

/// <summary>
/// 声音组。用于 AudioManager 的分组音量控制。
/// </summary>
public enum AudioGroup
{
    Master          = 0,
    Music           = 1,    // 背景音乐
    SFX             = 2,    // 音效
    Ambient         = 3,    // 环境音（风声/鸟叫/雨声）
    UI              = 4,    // UI 音效
    Voice           = 5,    // 人物语音
}

// ─────────────────────────────────────────────────────────────────────

/// <summary>
/// 技能类型。用于 SkillSystem 管理各技能的熟练度。
/// </summary>
public enum SkillType
{
    Mining          = 0,    // 挖掘
    Combat          = 1,    // 战斗
    Crafting        = 2,    // 制作
    Cooking         = 3,    // 烹饪
    Gathering       = 4,    // 采集
    Survival        = 5,    // 生存
}

// ─────────────────────────────────────────────────────────────────────

/// <summary>
/// 成就条件类型。用于 AchievementSystem 判断触发条件。
/// </summary>
public enum AchievementConditionType
{
    SurviveTime     = 0,    // 存活指定时长
    NoDeath         = 1,    // 无死亡通关
    TripleCrisis    = 2,    // 三重危机中存活
    BuildStructure  = 3,    // 建造指定建筑
    ReachLayer      = 4,    // 到达指定地层
    DiscoverAllPOI  = 5,    // 发现所有地表POI
    CollectAllLore  = 6,    // 收集所有叙事物品
    DefeatEnemy     = 7,    // 首次击败敌人
    DefeatBoss      = 8,    // 击败指定BOSS
    PacifistRun     = 9,    // 不杀敌通关
    NPCTrustMax     = 10,   // NPC信任度满
    QuestComplete   = 11,   // 完成指定任务
}

// ─────────────────────────────────────────────────────────────────────

/// <summary>
/// 兴趣点类型。用于 POIManager 管理地表兴趣点。
/// </summary>
public enum POIType
{
    None            = 0,
    AbandonedCamp   = 1,    // 废弃营地
    HunterCabin     = 2,    // 猎人小屋
    IceLake         = 3,    // 冰湖
    TraderStop      = 4,    // 商人停靠站
    RootEmergence   = 5,    // 根脉涌出点
    MineEntrance    = 6,    // 矿井入口
    Ruins           = 7,    // 废墟
}

// ─────────────────────────────────────────────────────────────────────

/// <summary>
/// 教学触发类型。用于 TutorialSystem 管理首次触发提示。
/// </summary>
public enum TutorialTrigger
{
    FirstPickup     = 0,    // 首次拾取物品
    FirstInventory  = 1,    // 首次打开背包
    HungerWarning   = 2,    // 饥饿警告
    ColdWarning     = 3,    // 寒冷警告
    FirstCombat     = 4,    // 首次遇敌
    FirstDeath      = 5,    // 首次死亡
    FirstCraft      = 6,    // 首次制作
    FirstBuild      = 7,    // 首次建造
    FirstDig        = 8,    // 首次挖掘
}

// ─────────────────────────────────────────────────────────────────────

/// <summary>
/// 永久发现物效果类型。用于 DiscoverySystem 管理永久被动加成。
/// </summary>
public enum DiscoveryEffectType
{
    MoveSpeedBonus      = 0,    // 移速加成
    WeaponDamageBonus   = 1,    // 武器伤害加成
    BlockBonus          = 2,    // 格挡减伤加成
    EquipmentBonus      = 3,    // 装备效果加成
    ShelterRecovery     = 4,    // 庇护所内恢复加成
    DecayReduction      = 5,    // 属性衰减减缓
}

// ─────────────────────────────────────────────────────────────────────

/// <summary>
/// 玩家攻击类型。用于战斗系统区分轻击/重击。
/// </summary>
public enum AttackType
{
    Light           = 0,    // 轻击
    Heavy           = 1,    // 重击
}