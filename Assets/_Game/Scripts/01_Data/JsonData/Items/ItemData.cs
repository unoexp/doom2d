// 📁 01_Data/JsonData/Items/ItemData.cs
// 物品数据 POCO。从 item.json 反序列化，替代 ItemDefinitionSO。
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

/// <summary>
/// 物品数据条目。纯数据，零运行时逻辑。
/// </summary>
[JsonObject(MemberSerialization.OptIn)]
public class ItemData
{
    [JsonProperty("itemId")]
    public int ItemId;

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

    [JsonProperty("maxStackSize")]
    public int MaxStackSize = 1;

    [JsonProperty("weight")]
    public float Weight = 0.1f;

    [JsonProperty("rarity")]
    [JsonConverter(typeof(StringEnumConverter))]
    public ItemRarity Rarity = ItemRarity.Common;
}
