// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/02_Base/ObjectPool/ObjectPoolManager.cs
// 通用 GameObject 对象池管理器。
// 零GC关键路径（Get/Release），基于 Prefab InstanceID 索引。
// ══════════════════════════════════════════════════════════════════════
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 中央对象池管理系统。
///
/// 核心特性：
///   · 基于 Prefab InstanceID 的池索引，避免字符串查找
///   · Get/Release 零 GC 分配
///   · 支持预热（Prewarm）
///   · 池对象自动归类到子 Transform，保持 Hierarchy 整洁
///   · 自动检测并调用 IPoolable 接口
/// </summary>
public sealed class ObjectPoolManager : MonoSingleton<ObjectPoolManager>
{
    // ══════════════════════════════════════════════════════
    // 内部数据结构
    // ══════════════════════════════════════════════════════

    private class Pool
    {
        public readonly Queue<GameObject> Inactive = new Queue<GameObject>();
        public readonly Transform Root;
        public readonly GameObject Prefab;

        public Pool(GameObject prefab, Transform parent)
        {
            Prefab = prefab;
            Root = new GameObject($"Pool_{prefab.name}").transform;
            Root.SetParent(parent);
        }
    }

    // ══════════════════════════════════════════════════════
    // 字段
    // ══════════════════════════════════════════════════════

    /// <summary>Prefab InstanceID → Pool</summary>
    private readonly Dictionary<int, Pool> _pools = new Dictionary<int, Pool>();

    /// <summary>活跃实例 InstanceID → 所属 Prefab InstanceID（用于 Release 时定位池）</summary>
    private readonly Dictionary<int, int> _instanceToPoolId = new Dictionary<int, int>();

    // ══════════════════════════════════════════════════════
    // 初始化
    // ══════════════════════════════════════════════════════

    protected override void OnInitialize()
    {
        ServiceLocator.Register<ObjectPoolManager>(this);
    }

    public override void Shutdown()
    {
        ServiceLocator.Unregister<ObjectPoolManager>();
        base.Shutdown();
    }

    // ══════════════════════════════════════════════════════
    // 公有 API —— 取出对象
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 从池中取出一个 GameObject。若池为空则实例化新对象。
    /// </summary>
    /// <param name="prefab">预制体引用</param>
    /// <param name="position">世界坐标</param>
    /// <param name="rotation">旋转</param>
    /// <param name="parent">父节点（null 表示场景根）</param>
    /// <returns>激活的 GameObject 实例</returns>
    public GameObject Get(GameObject prefab, Vector3 position = default,
                          Quaternion rotation = default, Transform parent = null)
    {
        var pool = GetOrCreatePool(prefab);
        GameObject obj;

        // [PERF] 直接从队列取出，零 GC
        if (pool.Inactive.Count > 0)
        {
            obj = pool.Inactive.Dequeue();
            var t = obj.transform;
            t.SetParent(parent);
            t.position = position;
            t.rotation = rotation;
        }
        else
        {
            obj = Instantiate(prefab, position, rotation, parent);
            int objId = obj.GetInstanceID();
            int prefabId = prefab.GetInstanceID();
            _instanceToPoolId[objId] = prefabId;
        }

        obj.SetActive(true);
        NotifySpawn(obj);
        return obj;
    }

    /// <summary>
    /// 从池中取出并返回指定组件。
    /// </summary>
    public T Get<T>(GameObject prefab, Vector3 position = default,
                    Quaternion rotation = default, Transform parent = null) where T : Component
    {
        var obj = Get(prefab, position, rotation, parent);
        return obj.GetComponent<T>();
    }

    // ══════════════════════════════════════════════════════
    // 公有 API —— 归还对象
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 将对象归还到池中。
    /// </summary>
    /// <param name="obj">要归还的 GameObject 实例</param>
    public void Release(GameObject obj)
    {
        if (obj == null) return;

        int objId = obj.GetInstanceID();

        if (!_instanceToPoolId.TryGetValue(objId, out int prefabId))
        {
            Debug.LogWarning($"[ObjectPoolManager] 尝试归还未注册的对象：{obj.name}，将直接销毁。");
            Destroy(obj);
            return;
        }

        if (!_pools.TryGetValue(prefabId, out var pool))
        {
            Debug.LogWarning($"[ObjectPoolManager] 找不到对象所属的池，将直接销毁：{obj.name}");
            _instanceToPoolId.Remove(objId);
            Destroy(obj);
            return;
        }

        NotifyDespawn(obj);
        obj.SetActive(false);
        obj.transform.SetParent(pool.Root);
        pool.Inactive.Enqueue(obj);
    }

    // ══════════════════════════════════════════════════════
    // 公有 API —— 预热与清理
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 预创建指定数量的对象到池中（场景加载时调用，避免运行时卡顿）。
    /// </summary>
    /// <param name="prefab">预制体</param>
    /// <param name="count">预创建数量</param>
    public void Prewarm(GameObject prefab, int count)
    {
        var pool = GetOrCreatePool(prefab);
        int prefabId = prefab.GetInstanceID();

        for (int i = 0; i < count; i++)
        {
            var obj = Instantiate(prefab, pool.Root);
            obj.SetActive(false);
            int objId = obj.GetInstanceID();
            _instanceToPoolId[objId] = prefabId;
            pool.Inactive.Enqueue(obj);
        }
    }

    /// <summary>
    /// 清空指定 Prefab 的池，销毁所有闲置对象。
    /// </summary>
    public void ClearPool(GameObject prefab)
    {
        int prefabId = prefab.GetInstanceID();
        if (!_pools.TryGetValue(prefabId, out var pool)) return;

        while (pool.Inactive.Count > 0)
        {
            var obj = pool.Inactive.Dequeue();
            if (obj != null)
            {
                _instanceToPoolId.Remove(obj.GetInstanceID());
                Destroy(obj);
            }
        }

        if (pool.Root != null)
            Destroy(pool.Root.gameObject);

        _pools.Remove(prefabId);
    }

    /// <summary>
    /// 清空所有池（场景切换时调用）。
    /// </summary>
    public void ClearAll()
    {
        foreach (var kv in _pools)
        {
            var pool = kv.Value;
            while (pool.Inactive.Count > 0)
            {
                var obj = pool.Inactive.Dequeue();
                if (obj != null) Destroy(obj);
            }
            if (pool.Root != null)
                Destroy(pool.Root.gameObject);
        }

        _pools.Clear();
        _instanceToPoolId.Clear();
    }

    // ══════════════════════════════════════════════════════
    // 内部方法
    // ══════════════════════════════════════════════════════

    /// <summary>获取或创建指定 Prefab 的池</summary>
    private Pool GetOrCreatePool(GameObject prefab)
    {
        int prefabId = prefab.GetInstanceID();
        if (_pools.TryGetValue(prefabId, out var pool))
            return pool;

        pool = new Pool(prefab, transform);
        _pools[prefabId] = pool;
        return pool;
    }

    /// <summary>通知所有 IPoolable 组件：对象被取出</summary>
    private static void NotifySpawn(GameObject obj)
    {
        // [PERF] GetComponents 会产生临时数组，但 Spawn 频率可控
        var poolables = obj.GetComponents<IPoolable>();
        for (int i = 0; i < poolables.Length; i++)
            poolables[i].OnSpawn();
    }

    /// <summary>通知所有 IPoolable 组件：对象被归还</summary>
    private static void NotifyDespawn(GameObject obj)
    {
        var poolables = obj.GetComponents<IPoolable>();
        for (int i = 0; i < poolables.Length; i++)
            poolables[i].OnDespawn();
    }
}
