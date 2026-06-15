// 📁 01_Data/JsonData/Inventory/InventoryContainerData.cs
// 背包容器定义数据 JSON 模型

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SurvivalGame.Data.Inventory;

/// <summary>
/// 背包槽位定义（JSON用）
/// </summary>
[System.Serializable]
public struct InventorySlotData
{
    [JsonProperty("index")] public int Index;
    [JsonProperty("slotType")][JsonConverter(typeof(StringEnumConverter))] public SlotType SlotType;
    [JsonProperty("allowedCategories")] public string[] AllowedCategories;
}

[JsonObject(MemberSerialization.OptIn)]
public class InventoryContainerData
{
    [JsonProperty("containerId")] public string ContainerId;
    [JsonProperty("capacity")] public int Capacity;
    [JsonProperty("slots")] public InventorySlotData[] Slots;
    [JsonProperty("hasWeightLimit")] public bool HasWeightLimit;
    [JsonProperty("maxWeight")] public float MaxWeight;
}
