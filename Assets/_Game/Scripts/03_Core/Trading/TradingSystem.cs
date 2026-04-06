// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/03_Core/Trading/TradingSystem.cs
// 交易系统核心。管理NPC交易报价、购买和出售逻辑。
// ══════════════════════════════════════════════════════════════════════
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 交易条目运行时数据（跟踪库存变化）
/// </summary>
public class TradeItemRuntime
{
    public string ItemId;
    public int GoldPrice;
    public int RemainingStock;  // -1=无限
    public bool IsSellingToPlayer;
}

/// <summary>
/// 中央交易管理系统。
///
/// 核心职责：
///   · 管理所有NPC的交易报价
///   · 验证交易条件（金币、库存、背包空间）
///   · 执行购买/出售（扣金币、移物品）
///   · 通过 EventBus 广播交易结果
///
/// 设计说明：
///   · 交易报价通过 TradeOfferSO 配置
///   · 运行时库存通过 TradeItemRuntime 管理
///   · 通过 ICurrencySystem 管理金币，IInventorySystem 管理物品
/// </summary>
public class TradingSystem : MonoBehaviour, ITradingSystem
{
    // ══════════════════════════════════════════════════════
    // 配置
    // ══════════════════════════════════════════════════════

    [Header("交易报价数据")]
    [SerializeField] private TradeOfferSO[] _tradeOffers;

    // ══════════════════════════════════════════════════════
    // 字段
    // ══════════════════════════════════════════════════════

    /// <summary>OfferId → 报价定义</summary>
    private readonly Dictionary<string, TradeOfferSO> _offerMap
        = new Dictionary<string, TradeOfferSO>();

    /// <summary>OfferId → 运行时库存列表</summary>
    private readonly Dictionary<string, List<TradeItemRuntime>> _runtimeStock
        = new Dictionary<string, List<TradeItemRuntime>>();

    private ICurrencySystem _currencySystem;
    private IInventorySystem _inventorySystem;

    // ══════════════════════════════════════════════════════
    // 生命周期
    // ══════════════════════════════════════════════════════

    private void Awake()
    {
        ServiceLocator.Register<TradingSystem>(this);
        ServiceLocator.Register<ITradingSystem>(this);

        if (_tradeOffers != null)
        {
            for (int i = 0; i < _tradeOffers.Length; i++)
            {
                var offer = _tradeOffers[i];
                if (offer == null || string.IsNullOrEmpty(offer.OfferId)) continue;
                _offerMap[offer.OfferId] = offer;

                // 初始化运行时库存
                InitializeStock(offer);
            }
        }
    }

    private void Start()
    {
        _currencySystem = ServiceLocator.Get<ICurrencySystem>();
        _inventorySystem = ServiceLocator.Get<IInventorySystem>();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<TradeOpenRequestEvent>(OnTradeOpenRequest);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<TradeOpenRequestEvent>(OnTradeOpenRequest);
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<TradingSystem>();
        ServiceLocator.Unregister<ITradingSystem>();
    }

    // ══════════════════════════════════════════════════════
    // 公有 API —— 查询
    // ══════════════════════════════════════════════════════

    /// <summary>获取交易报价定义</summary>
    public TradeOfferSO GetOffer(string offerId)
    {
        _offerMap.TryGetValue(offerId, out var offer);
        return offer;
    }

    /// <summary>获取指定报价的运行时库存</summary>
    public List<TradeItemRuntime> GetStock(string offerId)
    {
        _runtimeStock.TryGetValue(offerId, out var stock);
        return stock;
    }

    // ══════════════════════════════════════════════════════
    // 公有 API —— 交易操作
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 玩家购买物品（从NPC处）
    /// </summary>
    public bool BuyItem(string offerId, int stockIndex, int amount = 1)
    {
        if (!_runtimeStock.TryGetValue(offerId, out var stock)) return false;
        if (stockIndex < 0 || stockIndex >= stock.Count) return false;

        var item = stock[stockIndex];
        if (!item.IsSellingToPlayer) return false;

        // 检查库存
        if (item.RemainingStock != -1 && item.RemainingStock < amount)
        {
            PublishFailed(item.ItemId, "库存不足");
            return false;
        }

        int totalPrice = item.GoldPrice * amount;

        // 检查金币
        if (_currencySystem == null || !_currencySystem.HasEnoughGold(totalPrice))
        {
            PublishFailed(item.ItemId, "金币不足");
            return false;
        }

        // 执行交易
        if (!_currencySystem.TrySpendGold(totalPrice, "交易购买"))
        {
            PublishFailed(item.ItemId, "金币扣除失败");
            return false;
        }

        // 添加物品到背包
        if (_inventorySystem != null)
        {
            _inventorySystem.TryAddItem(item.ItemId, amount);
        }

        // 扣库存
        if (item.RemainingStock != -1)
        {
            item.RemainingStock -= amount;
        }

        EventBus.Publish(new TradeExecutedEvent
        {
            ItemId = item.ItemId,
            Amount = amount,
            TotalPrice = totalPrice,
            PlayerBuying = true,
            NPCId = _offerMap.TryGetValue(offerId, out var offer) ? offer.MerchantName : ""
        });

        return true;
    }

    /// <summary>
    /// 玩家出售物品（卖给NPC）
    /// </summary>
    public bool SellItem(string offerId, string itemId, int amount = 1)
    {
        if (!_runtimeStock.TryGetValue(offerId, out var stock)) return false;

        // 查找NPC收购条目
        TradeItemRuntime buyEntry = null;
        for (int i = 0; i < stock.Count; i++)
        {
            if (!stock[i].IsSellingToPlayer && stock[i].ItemId == itemId)
            {
                buyEntry = stock[i];
                break;
            }
        }

        if (buyEntry == null)
        {
            PublishFailed(itemId, "NPC不收购此物品");
            return false;
        }

        // 检查玩家背包中是否有足够物品
        if (_inventorySystem == null) return false;
        int playerHas = _inventorySystem.GetTotalItemCount(itemId);
        if (playerHas < amount)
        {
            PublishFailed(itemId, "物品数量不足");
            return false;
        }

        int totalPrice = buyEntry.GoldPrice * amount;

        // 移除物品
        _inventorySystem.TryRemoveItem(itemId, amount);

        // 给金币
        if (_currencySystem != null)
        {
            _currencySystem.AddGold(totalPrice, "交易出售");
        }

        EventBus.Publish(new TradeExecutedEvent
        {
            ItemId = itemId,
            Amount = amount,
            TotalPrice = totalPrice,
            PlayerBuying = false,
            NPCId = _offerMap.TryGetValue(offerId, out var offer) ? offer.MerchantName : ""
        });

        return true;
    }

    // ══════════════════════════════════════════════════════
    // 事件处理
    // ══════════════════════════════════════════════════════

    private void OnTradeOpenRequest(TradeOpenRequestEvent evt)
    {
        // 验证报价是否存在
        if (!_offerMap.ContainsKey(evt.OfferId))
        {
            Debug.LogWarning($"[TradingSystem] 交易报价不存在: {evt.OfferId}");
        }
    }

    // ══════════════════════════════════════════════════════
    // 内部方法
    // ══════════════════════════════════════════════════════

    private void InitializeStock(TradeOfferSO offer)
    {
        var stockList = new List<TradeItemRuntime>();

        // NPC出售物品
        if (offer.SellingItems != null)
        {
            for (int i = 0; i < offer.SellingItems.Length; i++)
            {
                var item = offer.SellingItems[i];
                if (item.Item == null) continue;
                stockList.Add(new TradeItemRuntime
                {
                    ItemId = item.Item.ItemId,
                    GoldPrice = item.GoldPrice,
                    RemainingStock = item.Stock,
                    IsSellingToPlayer = true
                });
            }
        }

        // NPC收购物品
        if (offer.BuyingItems != null)
        {
            for (int i = 0; i < offer.BuyingItems.Length; i++)
            {
                var item = offer.BuyingItems[i];
                if (item.Item == null) continue;
                stockList.Add(new TradeItemRuntime
                {
                    ItemId = item.Item.ItemId,
                    GoldPrice = item.GoldPrice,
                    RemainingStock = item.Stock,
                    IsSellingToPlayer = false
                });
            }
        }

        _runtimeStock[offer.OfferId] = stockList;
    }

    private static void PublishFailed(string itemId, string reason)
    {
        EventBus.Publish(new TradeFailedEvent
        {
            ItemId = itemId,
            Reason = reason
        });
    }
}
