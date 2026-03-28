// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/04_Gameplay/Combat/LootSystem.cs
// 掉落系统。监听实体死亡事件，根据掉落表生成 WorldItem。
// ══════════════════════════════════════════════════════════════════════
using UnityEngine;

/// <summary>
/// 掉落管理系统。
///
/// 核心职责：
///   · 监听 EntityDiedEvent
///   · 查找死亡实体的 EnemyBase → EnemyDefinitionSO.Drops
///   · 按概率生成 WorldItem 到场景中
///   · 通过 ObjectPoolManager 管理 WorldItem 生命周期
/// </summary>
public class LootSystem : MonoBehaviour
{
    // ══════════════════════════════════════════════════════
    // 配置
    // ══════════════════════════════════════════════════════

    [Header("掉落物预制体")]
    [Tooltip("通用掉落物 Prefab（挂载 WorldItem 组件）")]
    [SerializeField] private GameObject _worldItemPrefab;

    [Header("掉落分散")]
    [Tooltip("掉落物水平散布范围")]
    [SerializeField] private float _dropSpreadX = 1.5f;

    [Tooltip("掉落物向上弹射力度")]
    [SerializeField] private float _dropLaunchForceY = 3f;

    // ══════════════════════════════════════════════════════
    // 生命周期
    // ══════════════════════════════════════════════════════

    private void Awake()
    {
        ServiceLocator.Register<LootSystem>(this);
    }

    private void OnEnable()
    {
        EventBus.Subscribe<EntityDiedEvent>(OnEntityDied);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<EntityDiedEvent>(OnEntityDied);
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<LootSystem>();
    }

    // ══════════════════════════════════════════════════════
    // 公有 API
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 在指定位置生成掉落物品。
    /// </summary>
    public void DropItem(string itemId, int amount, Vector3 position, Sprite icon = null)
    {
        if (string.IsNullOrEmpty(itemId) || amount <= 0) return;
        if (_worldItemPrefab == null)
        {
            Debug.LogWarning("[LootSystem] 未配置 WorldItem 预制体");
            return;
        }

        // 随机散布位置
        Vector3 dropPos = position + new Vector3(
            Random.Range(-_dropSpreadX, _dropSpreadX), 0.5f, 0f);

        GameObject obj;
        if (ServiceLocator.TryGet<ObjectPoolManager>(out var pool))
            obj = pool.Get(_worldItemPrefab, dropPos);
        else
            obj = Instantiate(_worldItemPrefab, dropPos, Quaternion.identity);

        var worldItem = obj.GetComponent<WorldItem>();
        if (worldItem != null)
        {
            worldItem.Setup(itemId, amount, icon: icon);
        }

        // 弹射效果
        var rb = obj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = new Vector2(
                Random.Range(-2f, 2f),
                _dropLaunchForceY);
        }

        EventBus.Publish(new ItemDroppedEvent
        {
            ItemId = itemId,
            Amount = amount,
            WorldPosition = dropPos
        });
    }

    // ══════════════════════════════════════════════════════
    // 事件处理
    // ══════════════════════════════════════════════════════

    private void OnEntityDied(EntityDiedEvent evt)
    {
        // 通过 InstanceID 找到死亡实体的 EnemyBase
        var enemy = FindEnemyByInstanceId(evt.EntityInstanceId);
        if (enemy == null || enemy.Definition == null) return;
        if (enemy.Definition.Drops == null || enemy.Definition.Drops.Length == 0) return;

        // [PERF] 无 LINQ
        for (int i = 0; i < enemy.Definition.Drops.Length; i++)
        {
            var drop = enemy.Definition.Drops[i];
            if (drop.Item == null) continue;

            // 概率判定
            if (Random.value > drop.DropChance) continue;

            int amount = Random.Range(drop.MinAmount, drop.MaxAmount + 1);
            if (amount <= 0) continue;

            DropItem(drop.Item.ItemId, amount, enemy.transform.position, drop.Item.Icon);
        }
    }

    /// <summary>通过 InstanceID 查找 EnemyBase（场景中搜索）</summary>
    private EnemyBase FindEnemyByInstanceId(int instanceId)
    {
        // TODO: 后续可用注册表优化，避免 FindObjectsOfType
        var enemies = FindObjectsOfType<EnemyBase>();
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i].gameObject.GetInstanceID() == instanceId)
                return enemies[i];
        }
        return null;
    }
}
