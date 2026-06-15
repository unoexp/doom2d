// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/05_Show/VFX/VFXManager.cs
// 特效管理器。管理粒子特效的播放、对象池回收。
// ══════════════════════════════════════════════════════════════════════
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 特效管理器。
///
/// 核心职责：
///   · 通过特效ID播放粒子特效（ID对应VFXCatalog中的注册条目）
///   · 管理特效实例的对象池，避免频繁创建/销毁
///   · 支持跟随目标的特效和世界坐标特效
///   · 自动回收播放完毕的特效实例
///
/// 设计说明：
///   · 继承 MonoSingleton，全局唯一
///   · 同时注册到 ServiceLocator
///   · 表现层组件，不包含业务逻辑
/// </summary>
public class VFXManager : MonoSingleton<VFXManager>
{
    // ══════════════════════════════════════════════════════
    // 配置
    // ══════════════════════════════════════════════════════

    public VFXEntry[] Catalog { get; set; }

    public int DefaultPoolSize { get; set; } = 3;

    // ══════════════════════════════════════════════════════
    // 数据
    // ══════════════════════════════════════════════════════

    /// <summary>特效ID → 预制体</summary>
    private readonly Dictionary<string, GameObject> _prefabMap
        = new Dictionary<string, GameObject>();

    /// <summary>特效ID → 对象池</summary>
    private readonly Dictionary<string, Queue<ParticleSystem>> _pools
        = new Dictionary<string, Queue<ParticleSystem>>();

    /// <summary>当前播放中的特效（用于自动回收）</summary>
    private readonly List<ActiveVFX> _activeFX = new List<ActiveVFX>();

    // ══════════════════════════════════════════════════════
    // 生命周期
    // ══════════════════════════════════════════════════════

    protected override void Awake()
    {
        base.Awake();
        // [AppMain 重构] 仅注册 ServiceLocator，目录构建移至 Initialize()
        ServiceLocator.Register<VFXManager>(this);
    }

    /// <summary>配置注入后的目录构建（ISystem）</summary>
    public override void Initialize()
    {
        InitSingleton();

        // 构建目录
        if (Catalog != null)
        {
            for (int i = 0; i < Catalog.Length; i++)
            {
                var entry = Catalog[i];
                if (entry.Prefab == null || string.IsNullOrEmpty(entry.VFXId)) continue;
                _prefabMap[entry.VFXId] = entry.Prefab;
            }
        }

        Debug.Log($"[VFXManager] 完整初始化完成（{_prefabMap.Count} 个特效已注册）");
    }

    /// <summary>系统关闭清理（ISystem）</summary>
    public override void Shutdown()
    {
        StopAll();
        ServiceLocator.Unregister<VFXManager>();
        Debug.Log("[VFXManager] 已关闭");
        base.Shutdown();
    }

    private void Update()
    {
        // [PERF] 倒序遍历，回收已结束的特效
        for (int i = _activeFX.Count - 1; i >= 0; i--)
        {
            var active = _activeFX[i];
            if (active.Particle == null)
            {
                _activeFX.RemoveAt(i);
                continue;
            }

            // 跟随目标
            if (active.FollowTarget != null)
            {
                active.Particle.transform.position = active.FollowTarget.position + (Vector3)active.Offset;
            }

            // 播放结束 → 回收
            if (!active.Particle.isPlaying)
            {
                ReturnToPool(active.VFXId, active.Particle);
                _activeFX.RemoveAt(i);
            }
        }
    }

    // ══════════════════════════════════════════════════════
    // 公有 API
    // ══════════════════════════════════════════════════════

    /// <summary>在世界坐标播放特效</summary>
    /// <param name="vfxId">特效ID</param>
    /// <param name="position">世界坐标</param>
    /// <param name="rotation">旋转</param>
    /// <returns>播放中的 ParticleSystem（可能为 null）</returns>
    public ParticleSystem Play(string vfxId, Vector3 position, Quaternion rotation = default)
    {
        var particle = GetFromPool(vfxId);
        if (particle == null) return null;

        particle.transform.position = position;
        particle.transform.rotation = rotation == default ? Quaternion.identity : rotation;
        particle.gameObject.SetActive(true);
        particle.Play(true);

        _activeFX.Add(new ActiveVFX
        {
            VFXId = vfxId,
            Particle = particle,
            FollowTarget = null,
            Offset = Vector2.zero
        });

        return particle;
    }

    /// <summary>跟随目标播放特效</summary>
    /// <param name="vfxId">特效ID</param>
    /// <param name="target">跟随目标</param>
    /// <param name="offset">相对偏移</param>
    public ParticleSystem PlayFollow(string vfxId, Transform target, Vector2 offset = default)
    {
        if (target == null) return null;

        var particle = GetFromPool(vfxId);
        if (particle == null) return null;

        particle.transform.position = target.position + (Vector3)offset;
        particle.gameObject.SetActive(true);
        particle.Play(true);

        _activeFX.Add(new ActiveVFX
        {
            VFXId = vfxId,
            Particle = particle,
            FollowTarget = target,
            Offset = offset
        });

        return particle;
    }

    /// <summary>停止所有特效</summary>
    public void StopAll()
    {
        for (int i = _activeFX.Count - 1; i >= 0; i--)
        {
            if (_activeFX[i].Particle != null)
            {
                _activeFX[i].Particle.Stop(true);
                ReturnToPool(_activeFX[i].VFXId, _activeFX[i].Particle);
            }
        }
        _activeFX.Clear();
    }

    // ══════════════════════════════════════════════════════
    // 对象池
    // ══════════════════════════════════════════════════════

    private ParticleSystem GetFromPool(string vfxId)
    {
        if (!_prefabMap.TryGetValue(vfxId, out var prefab))
        {
            Debug.LogWarning($"[VFXManager] 未注册的特效ID: {vfxId}");
            return null;
        }

        if (!_pools.TryGetValue(vfxId, out var pool))
        {
            pool = new Queue<ParticleSystem>();
            _pools[vfxId] = pool;
        }

        ParticleSystem particle;
        if (pool.Count > 0)
        {
            particle = pool.Dequeue();
        }
        else
        {
            var go = Instantiate(prefab, transform);
            particle = go.GetComponent<ParticleSystem>();
            if (particle == null)
            {
                Debug.LogWarning($"[VFXManager] 预制体 {vfxId} 缺少 ParticleSystem 组件");
                Destroy(go);
                return null;
            }
        }

        return particle;
    }

    private void ReturnToPool(string vfxId, ParticleSystem particle)
    {
        if (particle == null) return;
        particle.Stop(true);
        particle.gameObject.SetActive(false);

        if (!_pools.TryGetValue(vfxId, out var pool))
        {
            pool = new Queue<ParticleSystem>();
            _pools[vfxId] = pool;
        }

        pool.Enqueue(particle);
    }

    // ══════════════════════════════════════════════════════
    // 内部结构
    // ══════════════════════════════════════════════════════

    /// <summary>活跃特效追踪数据</summary>
    private struct ActiveVFX
    {
        public string VFXId;
        public ParticleSystem Particle;
        public Transform FollowTarget;
        public Vector2 Offset;
    }
}

/// <summary>
/// 特效目录条目（Inspector 配置）
/// </summary>
[System.Serializable]
public struct VFXEntry
{
    [Tooltip("特效ID")]
    public string VFXId;

    [Tooltip("特效预制体（需包含 ParticleSystem）")]
    public GameObject Prefab;
}
