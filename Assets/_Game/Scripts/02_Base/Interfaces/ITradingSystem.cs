// 📁 02_Base/Interfaces/ITradingSystem.cs
// 交易系统接口定义，供表现层通过ServiceLocator访问

using System.Collections.Generic;

/// <summary>
/// 交易系统接口，表现层通过此接口与业务层通信
/// 🏗️ 定义在02_Base层，03_Core实现，05_Show引用
/// </summary>
public interface ITradingSystem
{
    TradeOfferSO GetOffer(string offerId);
    List<TradeItemRuntime> GetStock(string offerId);
    bool BuyItem(string offerId, int stockIndex, int amount = 1);
    bool SellItem(string offerId, string itemId, int amount = 1);
}
