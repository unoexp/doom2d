// 📁 05_Show/Inventory/Configs/IUIConfigService.cs
// UI配置服务接口，属于表现层内部服务，不应放在 02_Base

/// <summary>
/// UI配置服务接口
/// 🏗️ 定义在 05_Show 层：此接口返回 InventoryUIConfigSO（05_Show 类型），
///    不能放在 02_Base（低层不能依赖高层类型）
/// </summary>
public interface IUIConfigService
{
    InventoryUIConfigSO GetInventoryConfig();
}
