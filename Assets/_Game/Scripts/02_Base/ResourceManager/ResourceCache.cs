// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/02_Infrastructure/ResourceManager/ResourceCache.cs
// LRU资源缓存系统，支持内存限制和自动清理
// ══════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// LRU（最近最少使用）资源缓存
/// 自动管理内存使用，超过限制时淘汰最久未使用的资源
/// </summary>
internal sealed class ResourceCache
{
    // ══════════════════════════════════════════════════════
    // 内部数据结构
    // ══════════════════════════════════════════════════════

    /// <summary>缓存项节点</summary>
    private class CacheNode
    {
        public string Key;                  // 缓存键（资源路径）
        public UnityEngine.Object Value;    // 缓存值
        public long MemorySize;             // 估计内存大小（字节）
        public DateTime LastAccessTime;     // 最后访问时间
        public int AccessCount;             // 访问次数

        public CacheNode(string key, UnityEngine.Object value, long memorySize)
        {
            Key = key;
            Value = value;
            MemorySize = memorySize;
            LastAccessTime = DateTime.Now;
            AccessCount = 1;
        }

        public void RecordAccess()
        {
            LastAccessTime = DateTime.Now;
            AccessCount++;
        }
    }

    // ══════════════════════════════════════════════════════
    // 字段
    // ══════════════════════════════════════════════════════

    /// <summary>缓存字典（快速查找）</summary>
    private readonly Dictionary<string, LinkedListNode<CacheNode>> _cacheMap
        = new Dictionary<string, LinkedListNode<CacheNode>>();

    /// <summary>LRU链表（最近访问在前）</summary>
    private readonly LinkedList<CacheNode> _lruList
        = new LinkedList<CacheNode>();

    /// <summary>最大缓存项数</summary>
    private readonly int _maxCacheItems;

    /// <summary>最大内存使用（字节）</summary>
    private readonly long _maxMemoryUsageBytes;

    /// <summary>当前内存使用（字节）</summary>
    private long _currentMemoryUsageBytes = 0;

    /// <summary>命中次数统计</summary>
    private int _hitCount = 0;

    /// <summary>未命中次数统计</summary>
    private int _missCount = 0;

    /// <summary>淘汰次数统计</summary>
    private int _evictionCount = 0;

    /// <summary>对象锁（线程安全）</summary>
    private readonly object _lock = new object();

    // ══════════════════════════════════════════════════════
    // 构造函数
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 创建资源缓存
    /// </summary>
    /// <param name="maxCacheItems">最大缓存项数（0表示无限制）</param>
    /// <param name="maxMemoryUsageMB">最大内存使用（MB，0表示无限制）</param>
    public ResourceCache(int maxCacheItems = 100, int maxMemoryUsageMB = 100)
    {
        _maxCacheItems = Mathf.Max(0, maxCacheItems);
        _maxMemoryUsageBytes = maxMemoryUsageMB > 0 ?
            (long)maxMemoryUsageMB * 1024 * 1024 : 0;

        if (_maxCacheItems > 0)
            Debug.Log($"[ResourceCache] 初始化: 最大 {_maxCacheItems} 项, {maxMemoryUsageMB}MB 内存限制");
        else
            Debug.Log("[ResourceCache] 初始化: 无限制缓存");
    }

    // ══════════════════════════════════════════════════════
    // 公共API
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 尝试从缓存获取资源
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="key">资源键</param>
    /// <param name="value">输出资源</param>
    /// <returns>是否命中缓存</returns>
    public bool TryGet<T>(string key, out T value) where T : UnityEngine.Object
    {
        lock (_lock)
        {
            if (_cacheMap.TryGetValue(key, out var node))
            {
                // 命中缓存，更新LRU位置
                node.Value.RecordAccess();
                _lruList.Remove(node);
                _lruList.AddFirst(node);

                value = node.Value.Value as T;
                _hitCount++;

                // 发布缓存命中事件
                EventBus.Publish(new CacheHitEvent {
                    ResourcePath = key,
                    CacheSize = _cacheMap.Count
                });

                return true;
            }

            value = null;
            _missCount++;
            return false;
        }
    }

    /// <summary>
    /// 添加资源到缓存
    /// </summary>
    /// <param name="key">资源键</param>
    /// <param name="value">资源</param>
    /// <param name="estimatedMemorySize">估计内存大小（字节，0表示自动估计）</param>
    public void Cache(string key, UnityEngine.Object value, long estimatedMemorySize = 0)
    {
        if (value == null)
        {
            Debug.LogWarning($"[ResourceCache] 尝试缓存空资源: {key}");
            return;
        }

        lock (_lock)
        {
            // 如果已存在，先移除旧项
            if (_cacheMap.ContainsKey(key))
            {
                RemoveInternal(key);
            }

            // 计算内存使用
            long memorySize = estimatedMemorySize > 0 ?
                estimatedMemorySize : EstimateMemorySize(value);

            // 创建缓存节点
            var node = new CacheNode(key, value, memorySize);
            var listNode = new LinkedListNode<CacheNode>(node);

            // 检查容量限制
            while (!CanAddItem(memorySize))
            {
                if (!EvictLeastRecentlyUsed())
                {
                    // 无法腾出空间，放弃缓存
                    Debug.LogWarning($"[ResourceCache] 无法为资源腾出空间: {key} ({memorySize}字节)");
                    return;
                }
            }

            // 添加新项
            _cacheMap[key] = listNode;
            _lruList.AddFirst(listNode);
            _currentMemoryUsageBytes += memorySize;

            Debug.Log($"[ResourceCache] 缓存添加: {key} ({memorySize}字节), 当前: {_cacheMap.Count}项, {_currentMemoryUsageBytes / (1024*1024)}MB");
        }
    }

    /// <summary>
    /// 从缓存中移除资源
    /// </summary>
    /// <param name="key">资源键</param>
    /// <param name="unloadResource">是否卸载资源对象</param>
    /// <returns>是否成功移除</returns>
    public bool Remove(string key, bool unloadResource = false)
    {
        lock (_lock)
        {
            return RemoveInternal(key, unloadResource);
        }
    }

    /// <summary>
    /// 清理缓存，移除未使用的资源
    /// </summary>
    /// <param name="force">是否强制清理所有缓存</param>
    /// <returns>清理的资源数量</returns>
    public int Cleanup(bool force = false)
    {
        lock (_lock)
        {
            int removedCount = 0;
            var keysToRemove = new List<string>();

            foreach (var kvp in _cacheMap)
            {
                var node = kvp.Value.Value;

                // 判断是否应该清理
                bool shouldRemove = force || ShouldRemoveFromCache(node);

                if (shouldRemove)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            // 批量移除
            foreach (var key in keysToRemove)
            {
                if (RemoveInternal(key, true))
                    removedCount++;
            }

            if (removedCount > 0)
            {
                Debug.Log($"[ResourceCache] 清理完成: 移除 {removedCount} 项, 剩余 {_cacheMap.Count} 项");
            }

            return removedCount;
        }
    }

    /// <summary>
    /// 清空所有缓存
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            foreach (var kvp in _cacheMap)
            {
                var node = kvp.Value.Value;
                if (node.Value != null)
                {
                    Resources.UnloadAsset(node.Value);
                }
            }

            _cacheMap.Clear();
            _lruList.Clear();
            _currentMemoryUsageBytes = 0;
            _hitCount = 0;
            _missCount = 0;
            _evictionCount = 0;

            Debug.Log("[ResourceCache] 缓存已清空");
        }
    }

    /// <summary>
    /// 获取缓存统计信息
    /// </summary>
    public CacheStats GetStats()
    {
        lock (_lock)
        {
            return new CacheStats {
                ItemCount = _cacheMap.Count,
                MemoryUsageBytes = _currentMemoryUsageBytes,
                HitCount = _hitCount,
                MissCount = _missCount
            };
        }
    }

    /// <summary>
    /// 检查键是否在缓存中
    /// </summary>
    public bool ContainsKey(string key)
    {
        lock (_lock)
        {
            return _cacheMap.ContainsKey(key);
        }
    }

    /// <summary>
    /// 获取当前缓存项数
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _cacheMap.Count;
            }
        }
    }

    // ══════════════════════════════════════════════════════
    // 内部方法
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 内部移除方法
    /// </summary>
    private bool RemoveInternal(string key, bool unloadResource = false)
    {
        if (!_cacheMap.TryGetValue(key, out var listNode))
            return false;

        var node = listNode.Value;

        // 从数据结构中移除
        _cacheMap.Remove(key);
        _lruList.Remove(listNode);
        _currentMemoryUsageBytes -= node.MemorySize;

        // 发布缓存淘汰事件
        EventBus.Publish(new CacheEvictedEvent {
            ResourcePath = key,
            RemainingCacheSize = _cacheMap.Count
        });

        // 卸载资源
        if (unloadResource && node.Value != null)
        {
            Resources.UnloadAsset(node.Value);
            Debug.Log($"[ResourceCache] 资源卸载: {key}");
        }

        Debug.Log($"[ResourceCache] 缓存移除: {key}, 剩余: {_cacheMap.Count}项");
        return true;
    }

    /// <summary>
    /// 淘汰最近最少使用的项
    /// </summary>
    private bool EvictLeastRecentlyUsed()
    {
        if (_lruList.Last == null)
            return false;

        var node = _lruList.Last.Value;
        _evictionCount++;

        Debug.Log($"[ResourceCache] LRU淘汰: {node.Key} (最后访问: {node.LastAccessTime:HH:mm:ss})");

        return RemoveInternal(node.Key, true);
    }

    /// <summary>
    /// 检查是否可以添加新项
    /// </summary>
    private bool CanAddItem(long newItemSize)
    {
        // 如果无限制，始终可以添加
        if (_maxCacheItems == 0 && _maxMemoryUsageBytes == 0)
            return true;

        // 检查项数限制
        if (_maxCacheItems > 0 && _cacheMap.Count >= _maxCacheItems)
        {
            // 需要淘汰
            return false;
        }

        // 检查内存限制
        if (_maxMemoryUsageBytes > 0 && _currentMemoryUsageBytes + newItemSize > _maxMemoryUsageBytes)
        {
            // 需要淘汰
            return false;
        }

        return true;
    }

    /// <summary>
    /// 估计资源内存大小
    /// </summary>
    private long EstimateMemorySize(UnityEngine.Object resource)
    {
        // 简单估计方法，实际项目可能需要更精确的估算
        if (resource is Texture2D texture)
        {
            // 纹理内存 = 宽度 × 高度 × 每像素字节数
            int bytesPerPixel = texture.format switch
            {
                TextureFormat.RGBA32 => 4,
                TextureFormat.RGB24 => 3,
                TextureFormat.RGBAFloat => 16,
                TextureFormat.RGBAHalf => 8,
                TextureFormat.R8 => 1,
                _ => 4 // 默认
            };
            return texture.width * texture.height * bytesPerPixel;
        }
        else if (resource is Sprite sprite && sprite.texture != null)
        {
            // Sprite使用其纹理的大小
            return EstimateMemorySize(sprite.texture);
        }
        else if (resource is Mesh mesh)
        {
            // 网格内存 = 顶点数 × 顶点大小 + 三角形数 × 索引大小
            long vertexMemory = mesh.vertexCount * 12L; // 假设Vector3 = 12字节
            long indexMemory = mesh.triangles.Length * 4L; // 假设int = 4字节
            return vertexMemory + indexMemory;
        }
        else if (resource is AudioClip audio)
        {
            // 音频内存 = 采样数 × 声道数 × 位深度
            return (long)(audio.samples * audio.channels * 2); // 假设16位 = 2字节
        }

        // 默认估计值
        return 1024 * 10; // 10KB
    }

    /// <summary>
    /// 判断缓存项是否应该被清理
    /// </summary>
    private bool ShouldRemoveFromCache(CacheNode node)
    {
        // 规则1: 资源已被销毁
        if (node.Value == null)
            return true;

        // 规则2: 长时间未访问（超过10分钟）
        var timeSinceLastAccess = DateTime.Now - node.LastAccessTime;
        if (timeSinceLastAccess.TotalMinutes > 10)
            return true;

        // 规则3: 访问次数极少（仅访问1次且超过5分钟）
        if (node.AccessCount <= 1 && timeSinceLastAccess.TotalMinutes > 5)
            return true;

        return false;
    }

    // ══════════════════════════════════════════════════════
    // 调试和监控
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 打印缓存状态
    /// </summary>
    public void PrintStatus()
    {
        var stats = GetStats();
        Debug.Log($"[ResourceCache] 状态: {stats.ItemCount}项, {stats.MemoryUsageBytes / (1024*1024):F2}MB, 命中率: {stats.HitRate:P2}");
    }

    /// <summary>
    /// 获取缓存键列表（用于调试）
    /// </summary>
    public List<string> GetCacheKeys()
    {
        lock (_lock)
        {
            return new List<string>(_cacheMap.Keys);
        }
    }
}