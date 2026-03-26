// 📁 02_Base/Interfaces/IItemDataService.cs
// 物品数据服务接口，供ViewModel和Presenter访问物品定义

/// <summary>
/// 物品数据服务接口
/// 🏗️ 定义在02_Base层，03_Core实现，05_Show引用
/// </summary>
public interface IItemDataService
{
    ItemDefinitionSO GetItemDefinition(string itemId);
}
