// 📁 01_Data/ScriptableObjects/Items/_Base/ItemDefinitionSO.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>物品分类枚举</summary>
public enum ItemCategory
{
    General = 0,        // 通用物品
    Weapon = 1,         // 武器
    Armor = 2,          // 护甲
    Tool = 3,           // 工具
    Consumable = 4,     // 消耗品（食物、药品）
    Resource = 5,       // 资源（木材、矿石）
    Material = 6,       // 材料（布料、皮革）
    KeyItem = 7,        // 关键物品
    ModExtension = 100  // MOD扩展起始值
}

/// <summary>物品稀有度枚举</summary>
public enum ItemRarity
{
    Common = 0,     // 普通（白色）
    Uncommon = 1,   // 不常见（绿色）
    Rare = 2,       // 稀有（蓝色）
    Epic = 3,       // 史诗（紫色）
    Legendary = 4,  // 传说（橙色）
    Unique = 5      // 唯一（红色）
}


/// <summary>
/// 所有物品的数据定义基类。纯数据，零运行时逻辑。
/// 💡 数据驱动设计核心：新增物品只需创建.asset文件，无需改代码。
/// </summary>
public abstract class ItemDefinitionSO : ScriptableObject
{
    [Header("基础信息")]
    public string ItemId;           // 全局唯一ID (用于存档/事件传递)
    public string DisplayName;
    [TextArea] public string Description;
    public Sprite Icon;
    public GameObject WorldPrefab;  // 世界中掉落时的Prefab (Addressable Key)
    
    [Header("物品分类")]
    public ItemCategory Category = ItemCategory.General;

    [Header("背包属性")]
    public int MaxStackSize = 1;
    public float Weight = 0.1f;
    public ItemRarity Rarity = ItemRarity.Common;

    [Header("耐久度")]
    public bool HasDurability = false;
    public float MaxDurability = 100f;
    public float DurabilityConsumptionPerUse = 1f;
    public bool DestroyOnZeroDurability = true;

    [Header("交互")]
    public bool IsPickupable = true;
    
    // 💡 扩展点：子类重写此方法定义使用效果，通过ItemEffectProcessor调用
    public virtual void OnUse(GameObject user) { }
    
    // 💡 扩展点：MOD可以通过重写此方法注入自定义逻辑
    public virtual bool CanUse(GameObject user) => true;
}

// 📁 01_Data/ScriptableObjects/Items/Consumable/ConsumableItemSO.cs
/// <summary>消耗品扩展：食物、药品等</summary>
[CreateAssetMenu(fileName = "Item_Consumable_", menuName = "SurvivalGame/Items/Consumable")]
public class ConsumableItemSO : ItemDefinitionSO
{
    [Header("消耗效果")]
    public float HungerRestore;
    public float ThirstRestore;
    public float HealthRestore;
    public float DurationSeconds;               // 持续效果时长（0=即时）
    // public List<StatusEffectData> StatusEffects; // 附加状态效果（如中毒、增益）
}