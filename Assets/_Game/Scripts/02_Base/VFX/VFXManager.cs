// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/02_Base/VFX/VFXManager.cs
// 特效管理器。管理粒子特效的播放、对象池回收。
// ─────────────────────────────────────────────────────────────────────
// [JSON 配表重构] 特效目录由 vfx_catalog.json 配置，不再通过 Inspector
//   拖拽 VFXEntry[]。prefabPath 为 Resources 目录下的相对路径，
//   通过 ResourceManager.Load<GameObject> 加载预制体。
// ══════════════════════════════════════════════════════════════════════
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 特效管理器。
///
/// 核心职责：
///   · 从 JSON 配表加载特效目录（vfxId → prefabPath）
///   · 通过特效ID播放粒子特效
///   · 管理特效实例的对象池，避免频繁创建/销毁
///   · 支持跟随目标的特效和世界坐标特效
///   · 自动回收播放完毕的特效实例
///
/// 设计说明：
///   · 继承 MonoSingleton，全局唯一
///   · 同时注册到 ServiceLocator
///   · 表现层组件，不包含业务逻辑
///   · 特效预制体通过 ResourceManager 按路径加载，首次使用时懒初始化
/// </summary>
public class VFXManager : MonoSingleton<VFXManager>
{
    // ══════════════════════════════════════════════════════
    // 配置
    // ══════════════════════════════════════════════════════

    public int DefaultPoolSize { get; set; } = 3;

    // ══════════════════════════════════════════════════════
    // 数据
    // ══════════════════════════════════════════════════════

    /// <summary>特效ID → Resources 路径（从 JSON 配表加载）</summary>
    private Dictionary<string, string> _pathMap;

    /// <summary>特效ID → 已加载的预制体缓存</summary>
    private readonly Dictionary<string, GameObject> _prefabCache
        = new Dictionary<string, GameObject>();

    /// <summary>特效ID → 对象池</summary>
    private readonly Dictionary<string, Queue<ParticleSystem>> _pools
        = new Dictionary<string, Queue<ParticleSystem>>();

    /// <summary>当前播放中的特效（用于自动回收）</summary>
    private readonly List<ActiveVFX> _activeFX = new List<ActiveVFX>();

    /// <summary>目录是否已构建</summary>
    private bool _catalogBuilt;

    // ══════════════════════════════════════════════════════
    // 生命周期
    // ══════════════════════════════════════════════════════

    protected override void Awake()
    {
        base.Awake();
        ServiceLocator.Register<VFXManager>(this);
    }

    /// <summary>配置注入后的初始化（ISystem）</summary>
    public override void Initialize()
    {
        InitSingleton();

        // 目录构建延迟到首次使用（Initialize 在数据加载前执行）
        Debug.Log($"[VFXManager] 初始化完成（目录将在首次 Play 时构建）");
    }

    /// <summary>系统关闭清理（ISystem）</summary>
    public override void Shutdown()
    {
        StopAll();
        ServiceLocator.Unregister<VFXManager>();
        Debug.Log("[VFXManager] 已关闭");
        base.Shutdown();
    }

    // ══════════════════════════════════════════════════════
    // 目录构建（懒初始化）
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 确保特效目录已构建。
    /// 从 IVFXCataLogDataService 读取 JSON 配表数据，
    /// 构建 vfxId → prefabPath 的映射表。
    /// </summary>
    private void EnsureCatalogBuilt()
    {
        if (_catalogBuilt) return;

        if (!ServiceLocator.TryGet<IVFXCataLogDataService>(out var dataService))
        {
            Debug.LogWarning("[VFXManager] IVFXCataLogDataService 尚未就绪，无法构建目录");
            return;
        }

        var catalog = dataService.GetCatalog();
        _pathMap = new Dictionary<string, string>();

        if (catalog?.Entries != null)
        {
            foreach (var entry in catalog.Entries)
            {
                if (!string.IsNullOrEmpty(entry.VFXId) && !string.IsNullOrEmpty(entry.PrefabPath))
                    _pathMap[entry.VFXId] = entry.PrefabPath;
            }
        }

        _catalogBuilt = true;
        Debug.Log($"[VFXManager] 特效目录已构建，共 {_pathMap.Count} 条");
    }

    /// <summary>
    /// 获取特效预制体（带缓存）。
    /// 首次访问时通过 ResourceManager 加载并缓存。
    /// </summary>
    private GameObject GetPrefab(string vfxId, string path)
    {
        if (_prefabCache.TryGetValue(vfxId, out var cached))
            return cached;

        var prefab = ResourceManager.Instance.Load<GameObject>(path);
        if (prefab != null)
        {
            _prefabCache[vfxId] = prefab;
        }
        return prefab;
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
    /// <param name="vfxId">特效ID（对应 vfx_catalog.json 中的 vfxId）</param>
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
    /// <param name="vfxId">特效ID（对应 vfx_catalog.json 中的 vfxId）</param>
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
        EnsureCatalogBuilt();

        if (_pathMap == null || !_pathMap.TryGetValue(vfxId, out var path))
        {
            Debug.LogWarning($"[VFXManager] 未注册的特效ID: {vfxId}");
            return null;
        }

        var prefab = GetPrefab(vfxId, path);
        if (prefab == null)
        {
            Debug.LogWarning($"[VFXManager] 无法加载特效预制体: {path}");
            return null;
        }

        if (!_pools.TryGetValue(vfxId, out var pool))
        {
            pool = new Queue<ParticleSystem>(DefaultPoolSize);
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
                Debug.LogWarning($"[VFXManager] 预制体 {vfxId}（{path}）缺少 ParticleSystem 组件");
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
            pool = new Queue<ParticleSystem>(DefaultPoolSize);
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
