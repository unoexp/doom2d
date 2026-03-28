// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/04_Gameplay/World/WorldItem.cs
// 世界物品。地面上的可拾取物品，实现 IInteractable + IPoolable。
// ══════════════════════════════════════════════════════════════════════
using UnityEngine;

/// <summary>
/// 世界中的可拾取物品。
///
/// 核心职责：
///   · 在场景中展示掉落物品（Sprite + 物理碰撞）
///   · 实现 IInteractable，玩家按交互键拾取
///   · 实现 IPoolable，支持对象池回收复用
///   · 拾取时通过 InventorySystem 添加到背包
///
/// 使用方式：
///   · LootSystem / SpawnManager 通过 ObjectPoolManager.Get 生成
///   · 调用 Setup() 设置物品数据
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class WorldItem : MonoBehaviour, IInteractable, IPoolable
{
    // ══════════════════════════════════════════════════════
    // 配置
    // ══════════════════════════════════════════════════════

    [Header("拾取设置")]
    [Tooltip("自动拾取范围（0 = 需手动交互）")]
    [SerializeField] private float _autoPickupRadius = 0f;

    [Tooltip("生成后不可拾取的保护时间（秒）")]
    [SerializeField] private float _pickupDelay = 0.5f;

    // ══════════════════════════════════════════════════════
    // 运行时状态
    // ══════════════════════════════════════════════════════

    private string _itemId;
    private int _amount = 1;
    private float _durability = 1f;
    private float _spawnTime;
    private SpriteRenderer _spriteRenderer;
    private bool _isPickedUp;

    // ══════════════════════════════════════════════════════
    // IInteractable 实现
    // ══════════════════════════════════════════════════════

    public InteractionType InteractionType => InteractionType.Pickup;
    public string InteractionPrompt => $"拾取";
    public Transform Transform => transform;

    public bool CanInteract(GameObject interactor)
    {
        if (_isPickedUp) return false;
        if (string.IsNullOrEmpty(_itemId)) return false;
        return Time.time - _spawnTime >= _pickupDelay;
    }

    public void Interact(GameObject interactor)
    {
        if (!CanInteract(interactor)) return;
        TryPickup();
    }

    // ══════════════════════════════════════════════════════
    // IPoolable 实现
    // ══════════════════════════════════════════════════════

    public void OnSpawn()
    {
        _isPickedUp = false;
        _spawnTime = Time.time;
    }

    public void OnDespawn()
    {
        _itemId = null;
        _amount = 0;
    }

    // ══════════════════════════════════════════════════════
    // 生命周期
    // ══════════════════════════════════════════════════════

    private void Awake()
    {
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Start()
    {
        _spawnTime = Time.time;

        // 注册到交互系统（如果范围内有玩家）
        if (ServiceLocator.TryGet<InteractionSystem>(out var interaction))
            interaction.RegisterInteractable(this);
    }

    private void OnDestroy()
    {
        if (ServiceLocator.TryGet<InteractionSystem>(out var interaction))
            interaction.UnregisterInteractable(this);
    }

    // ══════════════════════════════════════════════════════
    // 公有 API
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 设置物品数据（生成时调用）。
    /// </summary>
    /// <param name="itemId">物品ID</param>
    /// <param name="amount">数量</param>
    /// <param name="durability">耐久度 0~1</param>
    /// <param name="icon">显示图标（可选，null则不更新）</param>
    public void Setup(string itemId, int amount = 1, float durability = 1f, Sprite icon = null)
    {
        _itemId = itemId;
        _amount = Mathf.Max(1, amount);
        _durability = Mathf.Clamp01(durability);
        _isPickedUp = false;
        _spawnTime = Time.time;

        if (icon != null && _spriteRenderer != null)
            _spriteRenderer.sprite = icon;
    }

    /// <summary>物品ID</summary>
    public string ItemId => _itemId;

    /// <summary>物品数量</summary>
    public int Amount => _amount;

    // ══════════════════════════════════════════════════════
    // 内部方法
    // ══════════════════════════════════════════════════════

    private void TryPickup()
    {
        _isPickedUp = true;

        // 通过 EventBus 通知背包系统添加物品
        EventBus.Publish(new ItemPickupRequestEvent
        {
            ItemId = _itemId,
            Amount = _amount,
            Durability = _durability,
            WorldPosition = transform.position
        });

        // 回收到对象池
        if (ServiceLocator.TryGet<ObjectPoolManager>(out var pool))
            pool.Release(gameObject);
        else
            Destroy(gameObject);
    }
}
