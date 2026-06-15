// 📁 01_Data/JsonData/Items/ItemData.cs
// 物品数据基类 POCO。从 JSON 反序列化，替代 ItemDefinitionSO。
// 💡 所有物品子类型共用一个 items.json 文件，通过 $type 区分。
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

/// <summary>
/// 物品数据基类。纯数据，零运行时逻辑。
/// 子类型：WeaponItemData, ArmorItemData, ConsumableItemData, MaterialItemData, ToolItemData
/// </summary>
[JsonObject(MemberSerialization.OptIn)]
public class ItemData
{
    [JsonProperty("itemId")]
    public string ItemId;

    [JsonProperty("displayName")]
    public string DisplayName;

    [JsonProperty("description")]
    public string Description;

    [JsonProperty("iconPath")]
    public string IconPath;

    [JsonProperty("worldPrefabPath")]
    public string WorldPrefabPath;

    [JsonProperty("category")]
    [JsonConverter(typeof(StringEnumConverter))]
    public ItemCategory Category = ItemCategory.General;

    [JsonProperty("maxStackSize")]
    public int MaxStackSize = 1;

    [JsonProperty("weight")]
    public float Weight = 0.1f;

    [JsonProperty("rarity")]
    [JsonConverter(typeof(StringEnumConverter))]
    public ItemRarity Rarity = ItemRarity.Common;

    [JsonProperty("hasDurability")]
    public bool HasDurability = false;

    [JsonProperty("maxDurability")]
    public float MaxDurability = 100f;

    [JsonProperty("durabilityConsumptionPerUse")]
    public float DurabilityConsumptionPerUse = 1f;

    [JsonProperty("destroyOnZeroDurability")]
    public bool DestroyOnZeroDurability = true;

    [JsonProperty("isPickupable")]
    public bool IsPickupable = true;
}
