// 📁 03_Core/DataServices/ItemDataService.cs
// 物品数据服务。从 item.json 加载物品定义数组。
// 继承 JsonDataService<ItemData>，直接使用泛型基类的数组加载逻辑。
// ⚠️ ItemId 为 int，GetIdFromItem 转为 string 做字典键。

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 物品数据服务。
/// item.json 为数据条目数组，使用标准 JsonDataService&lt;T&gt; 加载流程。
/// </summary>
public class ItemDataService : JsonDataService<ItemData>, IItemDataService
{
    public override string DataFileName => "item.json";

    private void Awake()
    {
        ServiceLocator.Register<IItemDataService>(this);
    }

    protected override string GetIdFromItem(ItemData item) => item.ItemId.ToString();

    public ItemData GetByItemId(int itemId) => GetById(itemId.ToString());

    public bool TryGetByItemId(int itemId, out ItemData data) =>
        TryGetById(itemId.ToString(), out data);

    public System.Collections.Generic.IReadOnlyList<ItemData> GetAllItems() => GetAll();

    public override void Shutdown()
    {
        ServiceLocator.Unregister<IItemDataService>();
        base.Shutdown();
    }
}
