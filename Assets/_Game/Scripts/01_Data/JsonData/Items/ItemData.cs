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
    [JsonProperty("item_id")]
    public string ItemID;

    [JsonProperty("name")]
    public string DisplayName;

    [JsonProperty("desc")]
    public string Description;

    [JsonProperty("icon")]
    public string IconPath;

    [JsonProperty("prefab")]
    public string WorldPrefabPath;

    [JsonProperty("category")]
    [JsonConverter(typeof(StringEnumConverter))]
    public ItemCategory Category = ItemCategory.General;

    [JsonProperty("max_stack_size")]
    public int MaxStackSize = 1;

    [JsonProperty("weight")]
    public float Weight = 0.1f;

    [JsonProperty("rarity")]
    [JsonConverter(typeof(StringEnumConverter))]
    public ItemRarity Rarity = ItemRarity.Common;
}
